using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Models
{
    /// <summary>
    /// Defines a set of 'AND' conditions or a set of 'OR' conditions
    /// </summary>
    public class ConditionSet
    {
        public bool IsAnd { get; set; }
        public List<Condition> Conditions { get; set; }
    }
}
