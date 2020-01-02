using RulesEngine.DataDef;
using RulesEngine.Exceptions;
using RulesEngine.Models;
using System.Collections.Generic;

namespace RulesEngine.Engine
{
    public static class Runner
    {
        /// <summary>
        /// Tries to evaluate each rule in turn (single pass of rules).
        /// 
        /// If a rule cannot be evaluated yet due to lack of facts it is added to
        /// the remaining rules set (which should be passed in on the next run).
        /// 
        /// If a rule is evaluated to true it adds facts and/or actions to the results.
        /// </summary>
        /// <param name="facts">The known facts</param>
        /// <param name="rules">The rules to be evaluated</param>
        /// <param name="finalRun">If true, evaluate final rules only. If false, exclude final rules from run.</param>
        /// <returns>The facts and actions added by triggered rules and the rules that cannot be evaluated yet.</returns>
        public static RunnerResults EvaluateRules(Dictionary<string, Fact> facts, Dictionary<string, Rule> rules, bool finalRun)
        {
            var results = new RunnerResults()
            {
                Facts = new List<Fact>(),
                Actions = new List<RuleAction>(),
                RemainingRules = new Dictionary<string, Rule>()
            };

            var knownFacts = new Dictionary<string, Fact>(facts);

            // Run through the rules
            foreach (var rule in rules.Values)
            {
                bool evaluated = false;

                if ((finalRun && rule.IsFinal) || (!finalRun && !rule.IsFinal))
                {
                    var result = EvaluateRule(rule, knownFacts);
                    evaluated = (result != null);

                    if (result == true)
                    {
                        if (rule.Facts != null)
                        {
                            foreach (var fact in rule.Facts.Values)
                            {
                                results.Facts.Add(fact);
                                knownFacts.TryAdd(fact.Id, fact);
                            }
                        }

                        if (rule.Actions != null)
                        {
                            results.Actions.AddRange(rule.Actions.Values);
                        }
                    }
                }

                if (!evaluated)
                {
                    results.RemainingRules.Add(rule.Name, rule);
                }
            }

            return results;
        }

        /// <summary>
        /// Evaluates a single rule and returns the result (true or false).
        /// 
        /// If there are not enough facts to evaluate the conditions of the rule
        /// then null is returned.
        /// </summary>
        private static bool? EvaluateRule(Rule rule, Dictionary<string, Fact> facts)
        {
            try
            {
                if (rule.ConditionSet == null)
                {
                    // Rule with no conditions is always true
                    return true;
                }
                else if (rule.ConditionSet.IsAnd)
                {
                    return EvaluateAndConditions(rule.ConditionSet.Conditions, facts);
                }
                else
                {
                    return EvaluateOrConditions(rule.ConditionSet.Conditions, facts);
                }
            }
            catch (RulesException ex)
            {
                throw new RulesException($"{ex.Message} in rule '{rule.Name}'");
            }
        }

        /// <summary>
        /// Returns null if any condition is not resolvable yet,
        /// true if ALL conditions are true or false if ANY
        /// condition is false.
        /// </summary>
        private static bool? EvaluateAndConditions(List<Condition> conditions, Dictionary<string, Fact> facts)
        {
            // Optimisation: Make sure all conditions are resolvable before trying to resolve any
            foreach (var condition in conditions)
            {
                if (condition.Op != Condition.Oper.NotDefined && !facts.ContainsKey(condition.Id))
                {
                    return null;
                }
            }

            // Second pass: Evaluate the conditions. Once a false condition is found
            // we don't need to evaluate any more conditions.
            foreach (var condition in conditions)
            {
                try
                {
                    var result = EvaluateCondition(condition, facts);
                    if (result == false)
                    {
                        return false;
                    }
                }
                catch (RulesException ex)
                {
                    throw new RulesException($"{ex.Message} evaluating condition '{condition}'");
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if ANY condition is resolvable and is true.
        /// Returns false if ALL conditions are resolvable and are false
        /// otherwise returns null (cannot be determined yet).
        /// </summary>
        private static bool? EvaluateOrConditions(List<Condition> conditions, Dictionary<string, Fact> facts)
        {
            bool? conditionsResult = false;

            foreach (var condition in conditions)
            {
                try
                {
                    var result = EvaluateCondition(condition, facts);
                    if (result == true)
                    {
                        return true;
                    }
                    else if (result == null)
                    {
                        conditionsResult = null;
                    }
                }
                catch (RulesException ex)
                {
                    throw new RulesException($"{ex.Message} evaluating condition '{condition}'");
                }
            }

            return conditionsResult;
        }

        /// <summary>
        /// Returns true if the condition evaluates to true, false if it evaluates to
        /// false or null if the condition is unresolvable due to a missing fact.
        /// 
        /// Note that a NotDefined condition evaluates to true if its fact is undefined.
        /// 
        /// Throws an exception if the condition is invalid (not all operations are
        /// supported for every type).
        /// </summary>
        private static bool? EvaluateCondition(Condition condition, Dictionary<string, Fact> facts)
        {
            var fact = facts.GetValueOrDefault(condition.Id);

            if (condition.Op == Condition.Oper.IsDefined)
            {
                return fact != null;
            }

            if (condition.Op == Condition.Oper.NotDefined)
            {
                return fact == null;
            }

            if (fact == null)
            {
                return null;
            }

            return fact.Value.CompareValue(condition.Op, condition.Value);
        }
    }
}
