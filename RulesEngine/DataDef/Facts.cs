using RulesEngine.Exceptions;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace RulesEngine.DataDef
{
    /// <summary>
    /// This is the complete set of facts that can be set. If a fact is
    /// not defined here it cannot be used in a rule. Note that all types
    /// must be nullable.
    /// </summary>
    public class Facts
    {
        public string Country { get; set; }
        public string Trm { get; set; }
        public decimal? HomeCcyAmount { get; set; }
        public int? Tier { get; set; }
        public bool? HighRisk { get; set; }

        // Basketball Facts
        public int? GameDuration { get; set; }
        public int? PersonalFoulCount { get; set; }
        public bool? FouledOut { get; set; }

        // Unit Test Facts
        public string StringFact { get; set; }
        public decimal? DecimalFact { get; set; }
        public int? IntFact { get; set; }
        public bool? BoolFact { get; set; }


        public override string ToString()
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            };

            return JsonSerializer.Serialize(this, jsonOptions);
        }

        /// <summary>
        /// Gets and sets member properties using reflection
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public Object this[string propertyName]
        {
            get
            {
                Type factsType = typeof(Facts);
                PropertyInfo propInfo = factsType.GetProperty(propertyName);
                if (propInfo == null)
                {
                    throw new RulesException($"Fact '{propertyName}' is not defined in the Facts.cs file");
                }
                return propInfo.GetValue(this);
            }
            set
            {
                Type factsType = typeof(Facts);
                PropertyInfo propInfo = factsType.GetProperty(propertyName);
                if (propInfo == null)
                {
                    throw new RulesException($"Fact '{propertyName}' is not defined in the Facts.cs file");
                }
                propInfo.SetValue(this, value);
            }
        }

        /// <summary>
        /// Gets the type of a member property using reflection
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Type GetType(string propertyName)
        {
            Type factsType = typeof(Facts);
            PropertyInfo propInfo = factsType.GetProperty(propertyName);
            if (propInfo == null)
            {
                throw new RulesException($"Fact '{propertyName}' is not defined in the Facts.cs file");
            }
            return Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
        }

        /// <summary>
        /// Converts member properties into a dictionary of facts using reflection
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Fact> GetFacts()
        {
            var facts = new Dictionary<string, Fact>();

            Type factsType = typeof(Facts);
            foreach (var propInfo in factsType.GetProperties())
            {
                if (propInfo.PropertyType.Name != "Object")
                {
                    var value = propInfo.GetValue(this);
                    if (value != null)
                    {
                        var fact = new Fact()
                        {
                            Id = propInfo.Name,
                        };

                        var propType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                        if (propType == typeof(string))
                        {
                            fact.Value = new StringValue((string)value);
                        }
                        else if (propType == typeof(int))
                        {
                            fact.Value = new IntValue((int)value);
                        }
                        else if (propType == typeof(decimal))
                        {
                            fact.Value = new DecimalValue((decimal)value);
                        }
                        else if (propType == typeof(bool))
                        {
                            fact.Value = new BoolValue((bool)value);
                        }
                        else
                        {
                            throw new RulesException($"Fact '{fact.Id}' has unknown type");
                        }

                        facts.Add(fact.Id, fact);
                    }
                }
            }

            return facts;
        }

        /// <summary>
        /// Sets member properties from a dictionary of facts using reflection
        /// </summary>
        /// <returns></returns>
        public void SetFacts(Dictionary<string, Fact> facts)
        {
            foreach (var fact in facts.Values)
            {
                this[fact.Id] = fact.Value.GetValue();
            }
        }
    }
}
