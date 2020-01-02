using RulesEngine.DataDef;
using RulesEngine.Engine;
using RulesEngine.Exceptions;
using System;
using System.Diagnostics;

namespace BasketballTest
{
    class Program
    {
        private const string RulesFile = "Rules.yaml";
        private static Engine _rulesEngine;

        private static void RunEngine(Facts facts)
        {
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
        }

        static void RunTestCase(Facts facts)
        {
            try
            {
                RunEngine(facts);
            }
            catch (RulesException ex)
            {
                Console.WriteLine($"\nTest case error: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            _rulesEngine = new Engine(RulesFile);

            // Run the rules against some test cases

            // Supply whatever facts we already know
            RunTestCase(new Facts() { PersonalFoulCount = 5, GameDuration = 40 });

            RunTestCase(new Facts() { PersonalFoulCount = 5, GameDuration = 48 });
        }
    }
}
