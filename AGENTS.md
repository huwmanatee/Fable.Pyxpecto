# Agent Guide

## Intention and library design

This library is designed to build a F# Fable transpiler compatible unit test runner. It is designed to target multiple Fable targets.

## Test and validation commands

Run from repository root (`/home/runner/work/Fable.Pyxpecto/Fable.Pyxpecto`):

- `dotnet tool restore`
- `npm install`
- `dotnet build Fable.Pyxpecto.sln -c Release`
- `dotnet test Fable.Pyxpecto.sln -c Release --no-build`

Build script entrypoints:

- `./build.sh runtests`
- `./build.sh runtestsdotnet`
- `./build.sh runtestsjs`
- `./build.sh runtestspy`
- `./build.sh runmtnet`
- `./build.sh runmtjs`
- `./build.sh runmtts`
- `./build.sh runmtpy`

> Note: build script test targets rely on `uv` being available on PATH.

## Code structure overview

- `src/Prelude.fs`: terminal colors and transpiler helper operator.
- `src/Model.fs`: domain types (`TestCase`, `FlatTest`, `FocusState`), `Accuracy`, and shared failure helpers.
- `src/Assertion.fs`: low-level equality and diff formatting assertions.
- `src/CommandLine.fs`: runtime argument and process-exit abstraction for .NET, Python, JS/TS.
- `src/TestDsl.fs`: test/list constructors and computation expression builders (`test`, `testAsync`, focused/pending variants).
- `src/Expect.fs`: user-facing expectation helpers.
- `src/Suspect.fs`: Pyxpecto-specific assertion helpers.
- `src/Config.fs`: runtime language/config argument parsing and typed config.
- `src/PyxpectoRunner.fs`: test flattening, execution engine, reporting, and `runTests` entrypoints.
- `src/Interop.fs`: AutoOpen glue for suites shared with .NET Expecto — erased `[<Tests>]` attribute and cross-target `!!`.

Support areas:

- `tests/Mocha.Tests`: switchable tests for Expecto/Mocha/Pyxpecto usage.
- `tests/Multitarget.Tests`: language-agnostic multi-target test suite.
- `build/`: FAKE build orchestration for compile/test routines.


## Agent Behavior

- Never credit yourself in any actions taken on the programmers behalf.
- Never commit code without being explicitly told to, request permission.
- Record anything worth remembering about this workspace in files under the workspace. Do not write to the agent memory directory.
- Prefer the AskUserQuestion question for user feedback. Ask questions 1 at a time and produce context before hand that explains the consequences of the answers.


## Modeling

### Principles
**Weakest Faithful Contract**

The Rule Of Least Power:
> Model every boundary by the least commitment that still preserves the domain truth. Require only the capabilities the next step actually consumes; promise only the guarantees downstream reasoning may safely depend on. Any extra specificity is treated as accidental coupling unless it carries domain meaning.

This is the Rule Of Least Power.

Useful smell:

> If removing a constraint does not change the domain rule, the constraint probably belongs inside the implementation, not at the boundary.

**Smallest Owning Shape**

> Push each invariant down to the smallest type that can naturally and ergonomically own it. Correlated values should be represented together; impossible emptiness should be removed by construction; validated facts should be wrapped, not repeatedly rechecked. The right shape makes downstream code simpler because the uncertainty has already been discharged.

> Look first in the representation you already have. A shape that distinguishes the case owns the fact better than a marker that asserts it, because the shape carries the reason along with the fact, and downstream can project the consequence instead of re-deriving it.

Useful smell:

> If downstream code must remember that two values are aligned, that a list is non-empty, that a value has already been validated, or that certain combinations “cannot happen,” the invariant was modeled too late or too far away.

> If a constraint you are about to add would exclude nothing that was not already unconstructible, the fact is owned below you and the constraint is noise.

**Make invalid states unrepresentable**

> Choose the **Weakest Faithful Contract**: the narrowest representation that still truthfully captures the domain. Collapse ambiguity, encode constraints, and model stages, roles, permissions, dates, formats, or dependencies explicitly. A good representation makes the wrong answer hard to form. Protect the narrow representation by wrapping it in an ADT.

Useful smell: If correctness depends on scattered `if` checks, nullable fields, magic strings, boolean flags, or remembering which combinations are “not allowed,” the model is too permissive.

### Skills

- Use /mattpocock-skills:codebase-design along with the above principles.
- Use /mattpocock-skills:tdd when writing tests.
