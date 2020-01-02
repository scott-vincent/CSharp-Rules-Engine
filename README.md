Overview
========
A rules engine works by applying a set of rules to a known list of facts.
This results in a set of actions (and possibly new facts).
The caller can then perform work based on this set of actions.

A rule contains a set of conditions which allow it to deduce new facts from existing facts and also to add Actions.

Introduction
============
I wanted to create a rules engine that was easy to use so I decided to use a single C# object to define all the facts.
This means it is necessary to modify the engine itself for your particular use case.

The facts are manipulated by the engine using reflection so no other files need changing when adding new facts.
The engine performs full validation of the rules so you will get an appropriate error message if the rules try to use a fact that hasn't been defined or contain a condition that isn't suitable for the fact's object type.

Getting Started
===============
Include RulesEngine as a Class Library in your own solution.
Modify **DataDef/Facts.cs** to define all the facts you require.

The following types are supported: string, int?, decimal?, bool?.
All types must be nullable as null is used to represent an undefined fact.

Modify **DataDef/RuleAction.cs** to define all the action types you require. 

Add a dependency to RulesEngine in your own project.
Create a **Rules.yaml** file in your project to define all your rules.
See the YAML files in Test-Basketball and Test-Trm for examples. 

Usage
=====
Run the engine by supplying whatever facts are already known.
The engine will return a final list of facts (supplied + deduced) and a final set of actions. 

```
using RulesEngine.DataDef;
using RulesEngine.Engine;


Engine engine = new Engine("Rules.yaml");
var results = engine.Run(new Facts() { myString = Abc, myInt = 123 });

Console.WriteLine(results.Facts);
results.Actions.ForEach(a => Console.WriteLine(a));
```
