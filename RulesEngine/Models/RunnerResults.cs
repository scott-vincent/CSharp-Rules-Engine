using RulesEngine.DataDef;
using System.Collections.Generic;

namespace RulesEngine.Models
{
    public class RunnerResults
    {
        public List<Fact> Facts { get; set; }
        public List<RuleAction> Actions { get; set; }
        public Dictionary<string, Rule> RemainingRules { get; set; }
    }
}
