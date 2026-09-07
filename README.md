# Fable.Pyxpecto



| Latest | Prerelease | Downloads |
|----------------|---------|-----------|
| <a href="https://www.nuget.org/packages/Fable.Pyxpecto">![Nuget](https://img.shields.io/nuget/v/Fable.Pyxpecto?logo=nuget)</a>|<a href="https://www.nuget.org/packages/Fable.Pyxpecto/absoluteLatest">![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Fable.Pyxpecto?logo=nuget)</a>|![Nuget](https://img.shields.io/nuget/dt/Fable.Pyxpecto?label=downloads)|

> This repository is heavily inspired by [Fable.Mocha](https://github.com/Zaid-Ajaj/Fable.Mocha/) by the awesome [@Zaid-Ajaj](https://github.com/Zaid-Ajaj).

Inspired by the popular Expecto library for F# and adopts the testList, testCase and testCaseAsync primitives for defining tests.

Fable.Pyxpecto can be used to run tests in **Python**, **JavaScript**, **TypeScript** and **.NET**! Or use compiler statements to switch between Pyxpecto and keep using Fable.Mocha and Expecto!

![pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto/assets/39732517/c5d09db3-8f63-4372-8655-6330c8a00af1)

**Table of Contents**
- [Fable.Pyxpecto](#fablepyxpecto)
  - [Features](#features)
    - [Reuse Expecto/Fable.Mocha Tests](#reuse-expectofablemocha-tests)
    - [Sharing a suite with .NET Expecto](#sharing-a-suite-with-net-expecto)
    - [Expecto parity](#expecto-parity)
    - [Pending](#pending)
    - [Focused](#focused)
    - [Sequential Tests](#sequential-tests)
    - [Command Line Arguments](#command-line-arguments)
  - [Install](#install)
  - [Running tests](#running-tests)
    - [Language Agnostic](#language-agnostic)
    - [With Mocha and Expecto](#with-mocha-and-expecto)
  - [Development](#development)
    - [Requirements](#requirements)
    - [Setup](#setup)
    - [Routines](#routines)
      - [Tests](#tests)


## Features

### Reuse Expecto/Fable.Mocha Tests

```fsharp
/// Reuse unit tests from Expecto and Fable.Mocha
let tests_basic = testList "Basic" [
    testCase "testCase works with numbers" <| fun () ->
        Expect.equal (1 + 1) 2 "Should be equal"

    testCase "isFalse works" <| fun () ->
        Expect.isFalse (1 = 2) "Should be equal"

    testCase "areEqual with msg" <| fun _ ->
        Expect.equal 2 2 "They are the same"

    testCase "isOk works correctly" <| fun _ ->
        let actual = Ok true
        Expect.isOk actual "Should be Ok"
]


```

### Sharing a suite with .NET Expecto

`open Fable.Pyxpecto` brings in an `[<Erase>]`d `[<Tests>]` attribute for Fable targets and a
cross-target `!!` operator, so one source tree can run on .NET Expecto and all three Fable
targets with no glue beyond the `open` that selects the DSL:

```fsharp
#if !FABLE_COMPILER
open Expecto
#else
open Fable.Pyxpecto
#endif

[<Tests>]
let tests = testList "MyModule" [ testCase "adds" <| fun () -> Expect.equal (1 + 1) 2 "sum" ]

[<EntryPoint>]
let main argv =
#if !FABLE_COMPILER
    Expecto.Tests.runTestsWithCLIArgs [] argv all
#else
    !! Pyxpecto.runTests [||] all
#endif
```

See [docs/multi-target-testing.md](docs/multi-target-testing.md) for the project file, the
per-target run commands and the cross-target gotchas.

### Expecto parity

`Expect` and the test DSL cover the parts of Expecto's API that can be expressed on every Fable
target, so most Expecto suites compile unchanged:

- assertions: `throws`, `throwsC`, `throwsT`, `throwsAsync`, `throwsAsyncC`, `throwsAsyncT`,
  `isChoice1Of2`, `isChoice2Of2`, `isLessThan`, `isLessThanOrEqual`, `isGreaterThan`,
  `isGreaterThanOrEqual`, `floatEqual`, `floatClose`, `floatLessThanOrClose`,
  `floatGreaterThanOrClose`, `isNaN`, `isNotNaN`, `isInfinity`, `isPositiveInfinity`,
  `isNegativeInfinity`, `isNotInfinity`, `isNotPositiveInfinity`, `isNotNegativeInfinity`,
  `stringContains`, `stringStarts`, `stringEnds`, `stringHasLength`, `isNotWhitespace`, `isMatch`,
  `isRegexMatch`, `isNotMatch`, `isNotRegexMatch`, `isMatchGroups`, `isMatchRegexGroups`,
  `hasLength`, `hasCountOf`, `all`, `allEqual`, `exists`, `contains`, `containsAll`, `distribution`,
  `sequenceEqual`, `sequenceStarts`, `sequenceContainsOrder`, `isAscending`, `isDescending`.
- test constructors: `testTheory`, `ftestTheory`, `ptestTheory`, `testTheoryAsync`,
  `ftestTheoryAsync`, `ptestTheoryAsync`, `testFixture`, `testFixtureAsync`, `testParam`,
  `testParamAsync`, `testSequenced`, `testSequencedGroup`.
- failure helpers, unqualified: `failtest`, `failtestf`, `failtestNoStack`, `failtestNoStackf`,
  `skiptest`, `skiptestf`.

`skiptest` skips a test that has already started; the runner reports it as ignored and prints the
reason next to the test name.

```fsharp
testCase "not ready yet" <| fun _ ->
    skiptest "waiting on the upstream fix"

testTheory "is positive" [ 1; 2; 3 ] <| fun x ->
    Expect.isGreaterThan x 0 "should be positive"
```

Deliberately missing, because they cannot be honoured on every target: `isCase`/`wantCase`
(quotations and reflection), `streamsEqual` (`System.IO.Stream`), `isFasterThan` (Expecto's
performance harness), `isNullValue`/`isNotNullValue` (`System.Nullable`), the `float32` variants
(`*f`, `float32Close`) and the `Task`-based builders. `Suspect` keeps the Pyxpecto-only helpers.

Two constructors differ from Expecto on purpose:

- `testFixture`, `testFixtureAsync`, `testParam`, `testParamAsync` and the theory builders return a
  `TestCase list` rather than a sequence, so they can be handed straight to `testList`.
- `testSequencedGroup` takes the group name for source compatibility, but Pyxpecto runs every test
  one after another anyway, so the name is documentation only.

### Pending

Pending tests will not be run, but displayed as "skipped".

```fsharp
ptestCase "skipping this one" <| fun _ ->
    failwith "Shouldn't be running this test"

ptestCaseAsync "skipping this one async" <|
    async {
        failwith "Shouldn't be running this test"
    }
```

### Focused

If there are any focused tests all other tests will not be run and are displayed as "skipped".

> 👀 Passing the `--fail-on-focused-tests` command line argument will make the runner fail if focused tests exist. This is used to avoid passing CI chains, when accidently pushing focused tests.
>
> Example `py my_focused_tests_file.py --fail-on-focused-tests` will fail.

```fsharp
let focusedTestsCases =
    testList "Focused" [
        ftestCase "Focused sync test" <| fun _ ->
            Expect.equal (1 + 1) 2 "Should be equal"
        ftestCaseAsync "Focused async test" <|
            async {
                Expect.equal (1 + 1) 2 "Should be equal"
            }
    ]
```

### Sequential Tests

Actually all tests run with this library will be sequential. The function is only added to comply with Expecto syntax.

💬 Help wanted. I currently have a prototype implementation for parallel tests on a branch. But it breaks collecting run-tests in .NET.

### Command Line Arguments

Running any py/ts/js/net code from pyxpecto can be customized with flags:

```
Fable.Pyxtpecto (F#)
Author: Kevin Frey

Usage:
  (python/node/npx ts-node/dotnet run) <path_to_entrypoint> [options]

Options:
  --fail-on-focused-tests       Will exit with ExitCode 4 if run with this argument
                                and focused tests are found.
  --silent                      Only start and result print. No print for each test.

  --do-not-exit-with-code       Will only return integer as result and not explicitly call `Environment.Exit`.
                                This can be useful to call Pyxpecto tests from foreign test frameworks

```

These can also be given via:

```fsharp
[<EntryPoint>]
let main argv =
    !!(Pyxpecto.runTests
        [|
            ConfigArg.FailOnFocused
            ConfigArg.Silent
        |]
        all)
```

## Install

From [Nuget](https://www.nuget.org/packages/Fable.Pyxpecto) with:

- `paket add Fable.Pyxpecto`
- `<PackageReference Include="Fable.Pyxpecto" Version="0.0.0" />`

## Running tests

### Language Agnostic

Fable.Pyxpecto does not use any dependencies and tries to support as many fable languages as possible.
Check out the [multitarget test project](./tests/Multitarget.Tests) to see it fully set up!

```fsharp
open Fable.Pyxpecto

// This is possibly the most magic used to make this work.
// Js and ts cannot use `Async.RunSynchronously`, instead they use `Async.StartAsPromise`.
// Here we need the transpiler not to worry about the output type.
#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let (!!) (any: 'a) = any
#endif
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop
#endif

[<EntryPoint>]
let main argv = !!(Pyxpecto.runTests [||] all)
```

Then run it using:

- **.NET**: `dotnet run`
- **JavaScript**:
  - `dotnet fable {rootPath} -o {rootPath}/{js_folder_name}`
  - `node {rootPath}/{js_folder_name}/Main.js`
  - *Requirements*:
    - nodejs installed.
    - package.json with `"type": "module"`.
    - init with `npm init`.
    - See: [package.json](./package.json).
- **TypeScript**:
  - `dotnet fable {rootPath} --lang ts -o {rootPath}/{ts_folder_name}`
  - `npx tsx {rootPath}/{ts_folder_name}/Main.ts`
  - *Requirements*:
    - possible same as JavaScript.
    - Require tsconfig file, see: [tsconfig.json](./tsconfig.json). (💬 Help wanted)
- **Python**:
  - `dotnet fable {rootPath} --lang py -o {rootPath}/{py_folder_name}`
  - `python {rootPath}/{py_folder_name}/main.py`
  - *Requirements*:
    - python (v>=3.14) executable on your PATH, or replace `python` with `path/to/python.exe`.

        _Might work with versions as low as 3.12_
    - installed `fable-library` python dependency (`version = "5.0.0a17"`)

### With Mocha and Expecto

Use the following syntax to automatically switch between Expecto, Fable.Mocha and Pyxpecto:

```fsharp
#if FABLE_COMPILER_PYTHON
open Fable.Pyxpecto
#endif
#if FABLE_COMPILER_JAVASCRIPT
open Fable.Mocha
#endif
#if !FABLE_COMPILER
open Expecto
#endif
```

```fsharp
[<EntryPoint>]
let main argv =
    #if FABLE_COMPILER_PYTHON
    Pyxpecto.runTests [||] all
    #endif
    #if FABLE_COMPILER_JAVASCRIPT
    Mocha.runTests all
    #endif
    #if !FABLE_COMPILER
    Tests.runTestsWithCLIArgs [] [||] all
    #endif
```

⚠️ If you want to use Pyxpecto in combination with Fable.Mocha you need to conditionally set Fable.Mocha dependency as shown below. Without this fable will try to transpile Fable.Mocha to python, which will result in errors.

```xml
<!-- .fsproj file-->
<PackageReference Condition="'$(FABLE_COMPILER_JAVASCRIPT)' == 'true'" Include="Fable.Mocha" Version="2.17.0" />
```

> 👀 Everything in curly braces are placeholders

1. Transpile test project to python `dotnet fable {path/to/tests} --lang py -o {path/to/tests}/py`
2. Run tests `python {path/to/tests}/{EntryFileName.py}`

## Development

### Requirements

- [uv](https://docs.astral.sh/uv/getting-started/installation/)
  - check with `uv --version` (Tested with `0.9.13`)
  - only the `build.sh`/`build.cmd` Python targets need it; without `uv` you can run the Python
    tests from a plain virtualenv holding `fable-library` (see `pyproject.toml` for the version)
- [Dotnet SDK](https://dotnet.microsoft.com/en-us/download)
  - check with `dotnet --version` (Tested with `10.0.111`)
- Node
  - check with `node --version` (Tested with `v26`)
- npm
  - check with `npm --version` (Tested with `12.0`)

### Setup

Run all commands in root.

1. `dotnet tool restore`
1. `npm install`

### Routines

#### Tests

`./build.cmd runtests`

Can be specified to run tests for specific environment.

> Switch test project
- `./build.cmd runtestsdotnet`
- `./build.cmd runtestsjs`
- `./build.cmd runtestspy`

> Multitarget test project
- `./build.cmd runmtpy`
- `./build.cmd runmtjs`
- `./build.cmd runmtts`
- `./build.cmd runmtnet`

#### Publish

0. Verify all tests pass `./build.cmd runtests`
1. Update CHANGELOG.md version with changes.
2. Push changes to main branch.
3. tag and push tags to GitHubt
    - `git tag X.Y.Z`
    - `git push --tags`
4. `dotnet pack ./src/Fable.Pyxpecto -o ./pkg`
5. Upload file from `./pkg` to [Nuget](https://www.nuget.org/packages/Fable.Pyxpecto)
6. Make GitHub release from tag.
