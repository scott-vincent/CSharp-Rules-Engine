using RulesEngine.DataDef;
using RulesEngine.Engine;
using RulesEngine.Exceptions;
using RulesEngine.Models;
using System;
using System.IO;
using Xunit;

namespace RulesEngineUnitTests
{
    public class RulesEngineTests
    {
        public class ParserTests
        {
            [Fact]
            public void EmptyRules()
            {
                const string Rules = "";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("No rules found", ex.Message);
            }

            [Fact]
            public void BadRules()
            {
                const string Rules = "Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a list of rules", ex.Message);
            }

            [Fact]
            public void BadRule()
            {
                const string Rules = "-";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a rule object", ex.Message);
            }

            [Fact]
            public void MissingRuleName()
            {
                const string Rules =
                    "- description: This is a test\n" +
                    "  conditions:\n" +
                    "    - \"StringFact == Trigger Rule 1\"\n" +
                    "  facts:\n" +
                    "    - BoolFact = True\n" +
                    "  actions:\n" +
                    "    - Field Rule1";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Missing mandatory attribute 'name'", ex.Message);
            }

            [Fact]
            public void UnknownAttribute()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  blah:\n" +
                    "    - Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Unknown attribute", ex.Message);
            }

            [Fact]
            public void DuplicateRule()
            {
                Action act = () => new Engine(null, Helper.GoodRules() + Helper.GoodRules());

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Duplicate rule name", ex.Message);
            }

            [Fact]
            public void BadConditions()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a list of conditions", ex.Message);
            }

            [Fact]
            public void BadCondition()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    -";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a condition", ex.Message);
            }

            [Theory]
            [InlineData("StringFact == Blah")]
            [InlineData("StringFact != Blah")]
            [InlineData("IntFact < 1")]
            [InlineData("IntFact > 123")]
            [InlineData("IntFact <= 1")]
            [InlineData("IntFact >= 123")]
            [InlineData("DecimalFact < 1.23")]
            [InlineData("DecimalFact > 123.45")]
            [InlineData("DecimalFact <= 1.23")]
            [InlineData("DecimalFact >= 123.45")]
            [InlineData("\"&StringFact\"")]
            [InlineData("\"!StringFact\"")]
            [InlineData("AND")]
            [InlineData("And")]
            [InlineData("OR")]
            [InlineData("or")]
            public void GoodConditionFormat(string condition)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - ";

                Engine engine = new Engine(null, Rules + condition);
                var results = engine.Run(new Facts() { StringFact = "Blah" });

                Assert.Equal("Blah", results.Facts.StringFact);
            }

            [Theory]
            [InlineData("StringFact")]
            [InlineData("StringFact Blah")]
            [InlineData("StringFact = Blah")]
            [InlineData("\"#StringFact\"")]
            public void BadConditionFormat(string condition)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - ";

                Action act = () => new Engine(null, Rules + condition);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected condition in format", ex.Message);
            }

            [Theory]
            [InlineData("AND")]
            [InlineData("OR")]
            public void ConditionCombinerNotFirst(string combine)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - StringFact == Blah\n" +
                    "    - ";

                Action act = () => new Engine(null, Rules + combine);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Only the first condition can be 'and' / 'or'", ex.Message);
            }

            [Fact]
            public void BadConditionId()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - Blah == Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("not defined in the Facts.cs file", ex.Message);
            }

            [Theory]
            [InlineData("IntFact = a")]
            [InlineData("IntFact = 123.45")]
            [InlineData("DecimalFact = a")]
            [InlineData("BoolFact = a")]
            [InlineData("BoolFact = 2")]
            public void BadConditionType(string condition)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - ";

                Action act = () => new Engine(null, Rules + condition);

                var ex = Assert.Throws<RulesException>(act);

                bool badType =
                    ex.Message.Contains("Failed to parse int value") ||
                    ex.Message.Contains("Failed to parse decimal value") ||
                    ex.Message.Contains("Failed to parse bool value");

                Assert.True(badType, ex.Message);
            }

            [Fact]
            public void ConflictingConditions()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - StringFact == Blah\n" +
                    "    - StringFact == Not Blah\n";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Found multiple 'AND ==' conditions", ex.Message);
            }

            [Fact]
            public void NotConflictingConditions()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  conditions:\n" +
                    "    - or\n" +
                    "    - StringFact == Blah\n" +
                    "    - StringFact == Not Blah\n";

                Engine engine = new Engine(null, Rules);
                var results = engine.Run(new Facts() { StringFact = "Blah" });

                Assert.Equal("Blah", results.Facts.StringFact);
            }

            [Fact]
            public void DuplicateFact()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    - StringFact = Blah\n" +
                    "    - StringFact = Blah2";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Duplicate fact", ex.Message);
            }

            [Fact]
            public void BadFacts()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a list of facts", ex.Message);
            }

            [Fact]
            public void BadFact()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    -";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a fact", ex.Message);
            }

            [Fact]
            public void GoodFactFormat()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    - StringFact = Blah";

                Engine engine = new Engine(null, Rules);
                var results = engine.Run(new Facts() { StringFact = "Blah" });

                Assert.Equal("Blah", results.Facts.StringFact);
            }

            [Theory]
            [InlineData("StringFact")]
            [InlineData("StringFact Blah")]
            [InlineData("StringFact == Blah")]
            [InlineData("\"#StringFact\"")]
            public void BadFactFormat(string fact)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    - ";

                Action act = () => new Engine(null, Rules + fact);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected fact in format", ex.Message);
            }

            [Fact]
            public void BadFactId()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    - Blah = Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("not defined in the Facts.cs file", ex.Message);
            }

            [Theory]
            [InlineData("IntFact = a")]
            [InlineData("IntFact = 123.45")]
            [InlineData("DecimalFact = a")]
            [InlineData("BoolFact = a")]
            [InlineData("BoolFact = 2")]
            public void BadFactType(string fact)
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  facts:\n" +
                    "    - ";

                Action act = () => new Engine(null, Rules + fact);

                var ex = Assert.Throws<RulesException>(act);

                bool badType =
                    ex.Message.Contains("Failed to parse int value") ||
                    ex.Message.Contains("Failed to parse decimal value") ||
                    ex.Message.Contains("Failed to parse bool value");

                Assert.True(badType, ex.Message);
            }

            [Fact]
            public void DuplicateAction()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  actions:\n" +
                    "    - Field Blah\n" +
                    "    - Field Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Duplicate action", ex.Message);
            }

            [Fact]
            public void BadActions()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  actions:\n" +
                    "    Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected a list of actions", ex.Message);
            }

            [Fact]
            public void BadAction()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  actions:\n" +
                    "    -";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Expected an action", ex.Message);
            }

            [Fact]
            public void BadActionType()
            {
                const string Rules =
                    "- name: Test Rule 1\n" +
                    "  actions:\n" +
                    "    - Blah Blah";

                Action act = () => new Engine(null, Rules);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("not defined in the RuleAction.cs file", ex.Message);
            }
        }

        public class EngineTests
        {
            [Fact]
            public void FactsMustBeSupplied()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                Action act = () => engine.Run(null);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Initial facts must be supplied", ex.Message);
            }

            [Fact]
            public void RuleAddsFacts()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { StringFact = "Trigger Rule 1" });

                Assert.Equal(true, results.Facts.BoolFact);
                Assert.Equal(1.23M, results.Facts.DecimalFact);
            }

            [Fact]
            public void RuleAddsActions()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { StringFact = "Trigger Rule 1" });

                // Confirm that rule 1 was triggered
                Assert.Contains(results.Actions, x => x.Id == "Rule1-Action1");
                Assert.Contains(results.Actions, x => x.Id == "Rule1-Action2");
            }

            [Fact]
            public void RuleNotTriggered()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { StringFact = "No Trigger" });

                Assert.Null(results.Facts.BoolFact);
                Assert.DoesNotContain(results.Actions, x => x.Id == "Rule1-Action1");
                Assert.DoesNotContain(results.Actions, x => x.Id == "Rule2");
            }

            [Fact]
            public void RulesFromFile()
            {
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, Helper.GoodRules());

                Engine engine = new Engine(tempFile);
                var results = engine.Run(new Facts() { StringFact = "Trigger Rule 1" });
                File.Delete(tempFile);

                // Confirm that rule 1 was triggered
                Assert.Equal(true, results.Facts.BoolFact);
            }

            [Fact]
            public void MissingFile()
            {
                string tempFile = Path.GetTempFileName();
                File.Delete(tempFile);

                Action act = () => new Engine(tempFile);

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("Cannot read file", ex.Message);
            }

            [Fact]
            public void MultipleRulesTriggered()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { StringFact = "Trigger Rule 1", IntFact = 2 });

                Assert.Contains(results.Actions, x => x.Id == "Rule1-Action1");
                Assert.Contains(results.Actions, x => x.Id == "Rule2");
            }

            [Fact]
            public void RuleTriggersOtherRules()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { IntFact = 3 });

                Assert.Contains(results.Actions, x => x.Id == "Rule3");
                Assert.Contains(results.Actions, x => x.Id == "Rule1-Action1");
                Assert.DoesNotContain(results.Actions, x => x.Id == "Rule2");
            }

            [Fact]
            public void RuleWithNoConditionsAlwaysTriggers()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() {});

                Assert.Contains(results.Actions, x => x.Id == "Rule4");
            }

            [Fact]
            public void RuleWithIsDefinedTriggers()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { BoolFact = true });

                Assert.Contains(results.Actions, x => x.Id == "Rule5");
                Assert.DoesNotContain(results.Actions, x => x.Id == "Rule6");
            }

            [Fact]
            public void RuleWithNotDefinedTriggers()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var results = engine.Run(new Facts() { });

                Assert.Contains(results.Actions, x => x.Id == "Rule6");
                Assert.DoesNotContain(results.Actions, x => x.Id == "Rule5");
            }

            [Fact]
            public void FactsCannotBeModified()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                Action act = () => engine.Run(new Facts() { StringFact = "Trigger Rule 1", BoolFact = false });

                var ex = Assert.Throws<RulesException>(act);
                Assert.Contains("cannot be modified", ex.Message);
            }
        }

        public class OtherTests
        {
            [Fact]
            public void GetAllActions()
            {
                Engine engine = new Engine(null, Helper.GoodRules());
                var actions = engine.GetAllActions();

                Assert.Contains(actions, x => x.Id == "Rule1-Action1");
                Assert.Contains(actions, x => x.Id == "Rule6");
            }

            [Fact]
            public void ModelsToString()
            {
                var facts = new Facts() { StringFact = "abc" };
                Assert.Equal("{\"stringFact\":\"abc\"}", facts.ToString());

                var action = new RuleAction() { Type = RuleAction.ActionType.Field, Id = "Field1" };
                Assert.Equal("Field Field1", action.ToString());

                var condition = new Condition() { Id = "id", Op = Condition.Oper.LessThanOrEqual, Value = new IntValue(1) };
                Assert.Equal("id LessThanOrEqual 1", condition.ToString());
            }
        }

        private static class Helper
        {
            public static string GoodRules()
            {
                return @"
# ===== Rule =====
- name: Rule 1
  description: This is a test
  conditions:
    - ""StringFact == Trigger Rule 1""
  facts:
    - BoolFact = True
    - DecimalFact = 1.23
  actions:
    - Field Rule1-Action1
    - Field Rule1-Action2
# ===== Rule =====
- name: Rule 2
  conditions:
    - IntFact == 2
  actions:
    - Field Rule2
# ===== Rule =====
- name: Rule 3
  description: This is a test
  conditions:
    - IntFact == 3
  facts:
    - ""StringFact = Trigger Rule 1""
  actions:
    - Field Rule3
# ===== Rule =====
- name: Rule 4
  actions:
    - Field Rule4
# ===== Rule =====
- name: Rule 5
  conditions:
    - ""&BoolFact""
  actions:
    - Field Rule5
# ===== Rule =====
- name: Rule 6
  conditions:
    - ""!BoolFact""
  actions:
    - Field Rule6
# ===== Rule =====
- name: Rule 7
  conditions:
    - DecimalFact >= 1.23
    - DecimalFact <= 1000
  actions:
    - Field Rule7
";
            }
        }
    }
}
