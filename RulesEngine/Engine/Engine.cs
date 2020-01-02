using RulesEngine.DataDef;
using RulesEngine.Exceptions;
using RulesEngine.Models;
using RulesEngine.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RulesEngine.Engine
{
    public class Engine
    {
        private static Dictionary<string, Rule> _rules = new Dictionary<string, Rule>();

        /// <summary>
        /// Reads and validates all the YAML rules 
        /// </summary>
        /// <param name="filename">YAML file containing all rules. Ignored if content is supplied.</param>
        /// <param name="content">Content in YAML format. If null, read content from specified filename instead.</param>
        public Engine(string filename, string content = null)
        {
            try
            {
                _rules = YamlParser.ReadRules(filename, content);
            }
            catch (RulesException ex)
            {
                throw new RulesException($"Cannot read YAML rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns every action from every rule so the caller can validate
        /// the ids against an external list.
        /// 
        /// Action types are validated against the list of known action types
        /// but the engine cannot validate action ids.
        /// 
        /// Can be filtered to return actions of a specific type only.
        /// </summary>
        /// <param name="type">Optional type to filter actions</param>
        public List<RuleAction> GetAllActions(RuleAction.ActionType? type = null)
        {
            var actions = new List<RuleAction>();

            foreach (var rule in _rules.Values)
            {
                if (rule.Actions == null)
                {
                    continue;
                }

                foreach (var actionSet in rule.Actions)
                {
                    var action = actionSet.Value;

                    if (type == null || action.Type == type)
                    {
                        actions.Add(action);
                    }
                }
            }

            // Sort actions by type and id
            return actions.OrderBy(o => o.Type).ThenBy(o => o.Id).ToList();
        }

        /// <summary>
        /// Runs the set of rules against the supplied facts.
        /// The supplied facts can trigger rules which may add more facts and trigger more rules.
        /// </summary>
        /// <param name="suppliedFacts">The facts that are already known.</param>
        /// <returns></returns>
        public Results Run(Facts suppliedFacts)
        {
            if (suppliedFacts == null)
            {
                throw new RulesException("Initial facts must be supplied");
            }

            // A runner performs a single run through the rules. If there are enough
            // facts defined to evaluate all conditions of a rule then that rule is
            // triggered. When a rule is triggered and evaluates to true it adds
            // facts and/or actions.
            //
            // If facts get added during a run-through then further run-throughs are
            // required. If no facts get added, the rules have been fully evaluated.

            // Once a rule has been triggered it cannot be triggered again. If all
            // rules have been triggered then the rules have been fully evaluated.

            // Special case: Final rules are only evaluated if a run-through adds
            // no new facts.

            // Setup initial states
            var rules = _rules;
            var facts = suppliedFacts.GetFacts();
            var actions = new Dictionary<string, RuleAction>();

            // Keep evaluating the rules until no more run-throughs are required.
            bool finalRun = false;
            while (true)
            {
                var results = Runner.EvaluateRules(facts, rules, finalRun);

                // Facts cannot be modified so ignore any identical facts but throw
                // an error if the value has changed.
                foreach (var fact in results.Facts)
                {
                    try
                    {
                        facts.Add(fact.Id, fact);
                    }
                    catch (ArgumentException)
                    {
                        var value = facts.GetValueOrDefault(fact.Id).Value;

                        if (!value.CompareValue(Condition.Oper.Equal, fact.Value))
                        {
                            throw new RulesException($"Established fact '{fact.Id} = {value}' cannot be modified to '{fact.Id} = {fact.Value}'");
                        }
                    }
                }

                // Ignore duplicate actions
                foreach (var action in results.Actions)
                {
                    var key = action.Type + " " + action.Id;
                    actions.TryAdd(key, action);
                }

                // If all the rules have been triggered we have finished
                if (results.RemainingRules.Count == 0)
                    break;

                /// Note that 'final' rules should not really add new facts but,
                /// if they do, the order of rules becomes significant and there
                /// will be further runs after this final run!
                if (finalRun && results.Facts.Count == 0)
                    break;

                // If no facts were added we need to perform a final run
                finalRun = (results.Facts.Count == 0);

                // Rules are only triggered once
                rules = results.RemainingRules;
            }

            // Populate results
            var finalFacts = new Facts();
            finalFacts.SetFacts(facts);

            return new Results()
            {
                Facts = finalFacts,
                Actions = actions.Values.OrderBy(o => o.Type).ThenBy(o => o.Id).ToList()
            };
        }
    }
}
