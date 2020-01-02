using RulesEngine.DataDef;
using RulesEngine.Engine;
using RulesEngine.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;

namespace TestRulesEngine
{
    class Program
    {
        private const string RulesFile = "Rules.yaml";
        private static Engine _rulesEngine;


        private static void ShowAllActions()
        {
            Console.WriteLine("\nAll Actions");
            Console.WriteLine("-----------");

            foreach (var action in _rulesEngine.GetAllActions())
            {
                Console.WriteLine(action);
            }
        }

        private static Results RunEngine(Facts facts)
        {
            Console.WriteLine($"\n{DateTime.Now} RUN ENGINE TEST CASE");

            // Show the supplied facts
            Console.WriteLine("\nSupplied Facts");
            Console.WriteLine("--------------");
            Console.WriteLine(facts);

            // Run the engine
            Console.WriteLine($"\n{DateTime.Now} Running engine..");
            var watch = Stopwatch.StartNew();
            var results = _rulesEngine.Run(facts);
            watch.Stop();
            if (watch.ElapsedMilliseconds > 0)
            {
                Console.WriteLine($"{DateTime.Now} Engine took {watch.ElapsedMilliseconds} millisecs");
            }
            else
            {
                var microsecs = (watch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
                Console.WriteLine($"{DateTime.Now} Engine took {microsecs} microsecs");
            }

            // Show the final list of facts (supplied + deduced)
            Console.WriteLine("\nFinal Facts");
            Console.WriteLine("-----------");
            Console.WriteLine(results.Facts);

            // Show the list of actions that were triggered
            Console.WriteLine("\nTriggered Actions");
            Console.WriteLine("-----------------");
            foreach (var action in results.Actions)
            {
                Console.WriteLine(action);
            }

            return results;
        }

        static Results RunTestCase(Facts facts)
        {
            try
            {
                return RunEngine(facts);
            }
            catch (RulesException ex)
            {
                Console.WriteLine($"\nTest case error: {ex.Message}");
                return null;
            }
        }

        static void Main(string[] args)
        {
            _rulesEngine = new Engine(RulesFile);

            // Show list of all possible actions
            ShowAllActions();

            // Run the rules against some test cases

            // Supply whatever facts we already know
            RunTestCase(new Facts()
            {
                Country = "DE",
                HomeCcyAmount = 123.45M
            });

            RunTestCase(new Facts() {
                Country = "DE",
                HomeCcyAmount = 1234
            });

            // Additional supplied facts can trigger additional rules
            RunTestCase(new Facts() {
                Country = "DE",
                HomeCcyAmount = 1234,
                HighRisk = true
            });

            // But if the supplied facts contradict the rules an error is thrown
            RunTestCase(new Facts()
            {
                Country = "DE",
                HomeCcyAmount = 123.45M,
                Tier = 1
            });

            // The rules can validate themselves. There is a rule to ensure a tier gets set.
            var results = RunTestCase(new Facts()
            {
                Country = "DE"
            });

            if (results.Actions.Any(x => x.Id == "UnknownTier"))
            {
                Console.WriteLine("\nTest case result: Rules did not set a tier");
            }

            // There is also a rule to ensure a TRM gets set.
            results = RunTestCase(new Facts()
            {
                Country = "XX"
            });

            if (results.Actions.Any(x => x.Id == "UnsupportedCountry"))
            {
                Console.WriteLine("\nTest case result: Specified country is not supported");
            }
        }
    }
}
