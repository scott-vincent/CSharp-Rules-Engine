using RulesEngine.DataDef;
using RulesEngine.Exceptions;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace RulesEngine.Parsers
{
    static class YamlParser
    {
        public static Dictionary<string, Rule> ReadRules(string filename, string content = null)
        {
            // If content is not supplied, read from file
            if (content == null)
            {
                try
                {
                    content = File.ReadAllText(filename);
                }
                catch (Exception ex)
                {
                    throw new RulesException($"Cannot read file {filename}", ex);
                }
            }

            var rules = new Dictionary<string, Rule>();

            using var stream = new StringReader(content);
            var yaml = new YamlStream();

            try
            {
                yaml.Load(stream);
            }
            catch (Exception ex)
            {
                throw new RulesException($"Failed to parse YAML: {ex.Message}");
            }

            if (yaml.Documents.Count == 0)
            {
                throw new RulesException($"No rules found");
            }

            // Expect a sequence of rules
            YamlSequenceNode yamlRules = GetYamlSequence(yaml.Documents[0].RootNode);
            if (yamlRules == null)
            {
                throw new RulesException($"Expected a list of rules but root does not contain a sequence");
            }

            foreach (var node in yamlRules.Children)
            {
                var rule = ReadRule(node);

                try
                {
                    rules.Add(rule.Name, rule);
                }
                catch (ArgumentException)
                {
                    throw new RulesException($"Duplicate rule name '{rule.Name}'");
                }
            }

            return rules;
        }

        public static Rule ReadRule(YamlNode node)
        {
            var rule = new Rule();

            // Read rule object
            var yamlRule = GetYamlObject(node);
            if (yamlRule == null)
            {
                throw new RulesException($"Expected a rule object but found: {node}");
            }

            // Read rule attributes
            foreach (var yamlAttribute in yamlRule)
            {
                var attribName = yamlAttribute.Key.ToString();

                switch (attribName)
                {
                    case "name":
                        rule.Name = GetYamlString(yamlAttribute.Value);
                        break;
                    case "description":
                        rule.Description = GetYamlString(yamlAttribute.Value);
                        break;
                    case "conditions":
                        try
                        {
                            rule.ConditionSet = ReadConditions(yamlAttribute.Value);
                        }
                        catch (RulesException ex)
                        {
                            throw new RulesException($"{ex.Message} when reading rule: {yamlRule}");
                        }
                        break;
                    case "facts":
                        try
                        {
                            rule.Facts = ReadFacts(yamlAttribute.Value);
                        }
                        catch (RulesException ex)
                        {
                            throw new RulesException($"{ex.Message} when reading rule: {yamlRule}");
                        }
                        break;
                    case "actions":
                        try
                        {
                            rule.Actions = ReadActions(yamlAttribute.Value);
                        }
                        catch (RulesException ex)
                        {
                            throw new RulesException($"{ex.Message} when reading rule: {yamlRule}");
                        }
                        break;
                    default:
                        throw new RulesException($"Unknown attribute '{attribName}' in rule: {yamlRule}");
                }
            }

            // Check mandatory attributes
            if (rule.Name == null)
            {
                throw new RulesException($"Missing mandatory attribute 'name' in rule: {yamlRule}");
            }

            // A rule is a final rule if it needs to be evaluated after all other rules,
            // i.e if it is checking whether facts are defined/undefined.
            rule.IsFinal = false;
            if (rule.ConditionSet != null)
            {
                foreach (var condition in rule.ConditionSet.Conditions)
                {
                    if (condition.Op == Condition.Oper.IsDefined || condition.Op == Condition.Oper.NotDefined)
                    {
                        rule.IsFinal = true;
                        break;
                    }
                }
            }

            return rule;
        }

        private static ConditionSet ReadConditions(YamlNode node)
        {
            // Expect a sequence of conditions
            YamlSequenceNode yamlConditions = GetYamlSequence(node);
            if (yamlConditions == null)
            {
                throw new RulesException($"Expected a list of conditions but conditions does not contain a sequence");
            }

            // Default op for combining conditions is 'AND'.
            var conditionSet = new ConditionSet()
            {
                IsAnd = true,
                Conditions = new List<Condition>()
            };

            foreach (var child in yamlConditions.Children)
            {
                var condition = ReadCondition(child);

                if (condition.Op == Condition.Oper.And || condition.Op == Condition.Oper.Or)
                {
                    // Only allow first condition to be 'and' / 'or' (to avoid confusing rules)
                    if (conditionSet.Conditions.Count > 0)
                    {
                        throw new RulesException("Only the first condition can be 'and' / 'or' (break complex conditions into separate rules)");
                    }

                    conditionSet.IsAnd = (condition.Op == Condition.Oper.And);
                }
                else
                {

                    if (conditionSet.IsAnd && condition.Op == Condition.Oper.Equal)
                    {
                        // Trap 'AND' conditions that can never be true (to detect missing use of 'OR')
                        // e.g. country == UK AND country == US
                        foreach (var prevCondition in conditionSet.Conditions)
                        {
                            if (prevCondition.Id == condition.Id && prevCondition.Op == Condition.Oper.Equal)
                            {
                                throw new RulesException($"Found multiple 'AND ==' conditions for '{condition.Id}' (did you forget to add '- OR')");
                            }
                        }
                    }

                    conditionSet.Conditions.Add(condition);
                }
            }

            return conditionSet;
        }

        private static Condition ReadCondition(YamlNode node)
        {
            // Read condition
            var yamlCondition = GetYamlString(node);
            if (yamlCondition == null)
            {
                throw new RulesException($"Expected a condition (string) but found: {node}");
            }

            Condition.Oper? oper = null;
            var condition = new Condition();

            // Parse condition (allow 3rd token to contain spaces)
            var tokens = yamlCondition.Split(" ", 3);

            if (tokens.Length == 1)
            {
                // Special prefixes & (IsDefined) and ! (NotDefined)
                if (tokens[0][0] == '&' || tokens[0][0] == '!')
                {
                    oper = tokens[0][0] == '&' ? Condition.Oper.IsDefined : Condition.Oper.NotDefined;
                    condition.Id = tokens[0].Substring(1);

                    // Validate id (must be defined in Facts.cs)
                    Facts.GetType(condition.Id);
                }
                else
                {
                    // Boolean Combiners (And/Or)
                    var boolOp = tokens[0].ToLower();
                    if (boolOp == "and")
                    {
                        oper = Condition.Oper.And;
                    }
                    else if (boolOp == "or")
                    {
                        oper = Condition.Oper.Or;
                    }
                }
            }
            else if (tokens.Length == 3)
            {
                switch (tokens[1])
                {
                    case "==":
                        oper = Condition.Oper.Equal;
                        break;
                    case "!=":
                        oper = Condition.Oper.NotEqual;
                        break;
                    case "<":
                        oper = Condition.Oper.LessThan;
                        break;
                    case ">":
                        oper = Condition.Oper.GreaterThan;
                        break;
                    case "<=":
                        oper = Condition.Oper.LessThanOrEqual;
                        break;
                    case ">=":
                        oper = Condition.Oper.GreaterThanOrEqual;
                        break;
                    default:
                        break;
                }

                try
                {
                    var fact = ParseFact(tokens[0], tokens[2]);
                    condition.Id = fact.Id;
                    condition.Value = fact.Value;
                }
                catch (RulesException ex)
                {
                    throw new RulesException($"{ex.Message} in condition: {yamlCondition}");
                }
            }

            if (oper == null)
            {
                throw new RulesException($"Expected condition in format: 'AND', 'OR', '!id', 'id [==|!=|<|>|<=|>=] value' but found: {yamlCondition}");
            }

            condition.Op = oper ?? Condition.Oper.Equal;

            return condition;
        }

        private static Dictionary<string, Fact> ReadFacts(YamlNode node)
        {
            // Expect a sequence of facts
            YamlSequenceNode yamlFacts = GetYamlSequence(node);
            if (yamlFacts == null)
            {
                throw new RulesException($"Expected a list of facts but facts does not contain a sequence");
            }

            var facts = new Dictionary<string, Fact>();

            foreach (var child in yamlFacts.Children)
            {
                var fact = ReadFact(child);

                try
                {
                    facts.Add(fact.Id, fact);
                }
                catch (ArgumentException)
                {
                    throw new RulesException($"Duplicate fact '{fact.Id}'");
                }
            }

            return facts;
        }

        private static Fact ReadFact(YamlNode node)
        {
            var fact = new Fact();

            // Read fact
            var yamlFact = GetYamlString(node);
            if (yamlFact == null)
            {
                throw new RulesException($"Expected a fact (string) but found: {node}");
            }

            // Parse fact (allow 3rd token to contain spaces)
            var tokens = yamlFact.Split(" ", 3);
            
            if (tokens.Length != 3 || tokens[1] != "=")
            {
                throw new RulesException($"Expected fact in format: 'id = value' but found: {yamlFact}");
            }

            try
            {
                return ParseFact(tokens[0], tokens[2]);
            }
            catch (RulesException ex)
            {
                throw new RulesException($"{ex.Message} in fact: {yamlFact}");
            }
        }

        private static Dictionary<string, RuleAction> ReadActions(YamlNode node)
        {
            // Expect a sequence of actions
            YamlSequenceNode yamlActions = GetYamlSequence(node);
            if (yamlActions == null)
            {
                throw new RulesException($"Expected a list of actions but actions does not contain a sequence");
            }

            var actions = new Dictionary<string, RuleAction>();

            foreach (var child in yamlActions.Children)
            {
                var action = ReadAction(child);

                try
                {
                    var key = action.Type + " " + action.Id;
                    actions.Add(key, action);
                }
                catch (ArgumentException)
                {
                    throw new RulesException($"Duplicate action '{action.Type} {action.Id}'");
                }
            }

            return actions;
        }

        private static RuleAction ReadAction(YamlNode node)
        {
            var action = new RuleAction();

            // Read action
            var yamlAction = GetYamlString(node);
            if (yamlAction == null)
            {
                throw new RulesException($"Expected an action (string) but found: {node}");
            }

            // Parse action
            var tokens = yamlAction.Split(" ");
            if (tokens.Length != 2)
            {
                throw new RulesException($"Expected action in format: 'type id' but found: {yamlAction}");
            }

            try
            {
                return ParseAction(tokens[0], tokens[1]);
            }
            catch (RulesException ex)
            {
                throw new RulesException($"{ex.Message} in action: {yamlAction}");
            }
        }

        /// <summary>
        /// Validates and returns a fact.
        /// 
        /// Throws an exception if the id is not in Facts.cs or if the
        /// value cannot be parsed to the type specified in Facts.cs
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Fact ParseFact(string id, string value)
        {
            var fact = new Fact()
            {
                Id = id
            };

            // Validate id (must be defined in Facts.cs)
            var valueType = Facts.GetType(id);

            // Validate value (must be parsable to type specified in Facts)
            if (valueType == typeof(string))
            {
                fact.Value = new StringValue(value);
                if (fact.Value.GetValue() == null)
                {
                    throw new RulesException("Failed to parse string value");
                }
            }
            else if (valueType == typeof(int))
            {
                fact.Value = new IntValue(value);
                if (fact.Value.GetValue() == null)
                {
                    throw new RulesException("Failed to parse int value");
                }
            }
            else if (valueType == typeof(decimal))
            {
                fact.Value = new DecimalValue(value);
                if (fact.Value.GetValue() == null)
                {
                    throw new RulesException("Failed to parse decimal value");
                }
            }
            else if (valueType == typeof(bool))
            {
                fact.Value = new BoolValue(value);
                if (fact.Value.GetValue() == null)
                {
                    throw new RulesException("Failed to parse bool value");
                }
            }
            else
            {
                throw new RulesException("Don't know how to parse value");
            }

            return fact;
        }

        /// <summary>
        /// Validates and returns an action.
        /// 
        /// Throws an exception if type is not a valid action type.
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static RuleAction ParseAction(string type, string id)
        {
            // Validate action type
            if (!Enum.TryParse(type, out RuleAction.ActionType enumType))
            {
                throw new RulesException($"Action type '{type}' not defined in the RuleAction.cs file");
            }

            return new RuleAction()
            {
                Type = enumType,
                Id = id
            };
        }

        /// <summary>
        /// Reads a YAML sequence.
        /// Returns null if it is not a sequence.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static YamlSequenceNode GetYamlSequence(YamlNode node)
        {
            return node.NodeType == YamlNodeType.Sequence ? (YamlSequenceNode)node : null;
        }

        /// <summary>
        /// Reads a YAML object.
        /// Returns null if it is not an object.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static YamlMappingNode GetYamlObject(YamlNode node)
        {
            return node.NodeType == YamlNodeType.Mapping ? (YamlMappingNode)node : null;
        }

        /// <summary>
        /// Reads a YAML string.
        /// Returns null if it is not a scalar or is empty.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string GetYamlString(YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Scalar)
            {
                return null;
            }

            var scalarNode = (YamlScalarNode)node;
            return string.IsNullOrEmpty(scalarNode.Value) ? null : scalarNode.Value;
        }
    }
}
