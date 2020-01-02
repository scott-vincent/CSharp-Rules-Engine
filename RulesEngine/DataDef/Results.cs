using System.Collections.Generic;

namespace RulesEngine.DataDef
{
    public class Results
    {
        // The final set of defined facts (supplied + deduced) after rules have been fully applied
        public Facts Facts { get; set; }

        // The final set of actions from all triggered rules
        public List<RuleAction> Actions { get; set; }
    }
}
