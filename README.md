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

I also wanted a simplified rules engine that imposes certain restrictions on the rules. This stops the rules getting
overly complex / unwieldly / unintelligable and means the rules engine can just throw an exception rather than trying to deal with more complex scenarios. 

Restrictions
============

1. Facts cannot be modified

Once a fact is defined it cannot be changed.

A rule that tries to modify an established fact will throw an exception.
This situation can be avoided by adding a 'fact not defined' condition to a rule, i.e. only add a fact if it is not already added.
This also means a rule cannot do things like 'Fact += 1' as this is modifying an existing fact. Instead, the rule needs to do something like 'AdditionalFact = True' and another rule would need to check for 'Fact == 1 AND AdditionalFact == true'. 

2. Each rule is only applied once

Once all the conditions of a rule can be evaluated (all the required facts are available) it is evaluated.
The rule will then never be evaluated again (it doesn't need to be as facts never change due to restriction 1).

3. Rules that check for defined/undefined facts are evaluated last 

This is logical as a fact is only undefined if no other rule has managed to define it.
So checking to see if a fact is defined/undefined must be done as late as possible.

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
