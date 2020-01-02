using RulesEngine.DataDef;
using System.Collections.Generic;

namespace RulesEngine.Models
{
    /// <summary>
    /// Rules are trigger-once, i.e. once they have been triggered
    /// they will not trigger again (in the same Run).
    ///
    /// A final rule is one that should only be evaluated once all
    /// other (non-final) rules have been evaluated. This ensures
    /// that rules checking whether facts are defined or not don't
    /// trigger prematurely.
    /// </summary>
    public class Rule
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ConditionSet ConditionSet { get; set; }
        public Dictionary<string, Fact> Facts { get; set; }
        public Dictionary<string, RuleAction> Actions { get; set; }

        public bool IsFinal;
    }
}
