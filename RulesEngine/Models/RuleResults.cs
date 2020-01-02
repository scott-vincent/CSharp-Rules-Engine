using RulesEngine.DataDef;
using System.Collections.Generic;

namespace RulesEngine.Models
{
    public class RuleResults
    {
        public List<Fact> Facts { get; set; }
        public List<RuleAction> Actions { get; set; }
    }
}
