# Sharing one test suite between .NET Expecto and Fable.Pyxpecto

This is the whole integration for a test project that runs the same F# suite on .NET,
JavaScript, TypeScript and Python.

Expecto owns the test DSL on .NET; Fable.Pyxpecto owns it on the Fable targets. They are separate
packages and neither can see the other, so one `#if` at the top of each test file swaps the
`open`. Everything else that used to need per-project glue now lives in Fable.Pyxpecto's AutoOpen
`Interop` module, which `open Fable.Pyxpecto` already brings in:

- **`[<Tests>]`** — Expecto's discovery attribute, which `YoloDev.Expecto.TestSdk` bridges to
  `dotnet test`. Pyxpecto supplies an `[<Erase>]`d stand-in on Fable targets only, so the
  annotation stays in shared source, resolves everywhere, and emits nothing into the generated
  JavaScript, TypeScript or Python. It is deliberately absent on .NET, where Expecto's own
  attribute is the one you want and a second definition would only be ambiguous.
- **`!!`** — `Pyxpecto.runTests` ends in `Async.StartAsPromise` on JavaScript and TypeScript,
  handing back a promise, and in `Async.RunSynchronously` elsewhere, handing back the exit code.
  `!!` is the JsInterop cast on the first two targets and the identity function on the rest, so
  one `[<EntryPoint>]` body type checks on all four.

## 1. The project file

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <!-- Main.fs supplies the entry point for every target. -->
    <GenerateProgramFile>false</GenerateProgramFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="MyModuleTests.fs" />
    <Compile Include="Tests.fs" />
    <Compile Include="Main.fs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/MyLib/MyLib.fsproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- The DSL on Fable targets, plus [<Tests>] and `!!`. -->
    <PackageReference Include="Fable.Pyxpecto" Version="2.2.0" />
    <!-- The DSL on .NET, and [<Tests>] discovery for `dotnet test`. Expecto arrives with it. -->
    <PackageReference Include="YoloDev.Expecto.TestSdk" Version="0.16.1" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.9.0" />
    <!-- Compile-time bindings the Fable Python target needs. -->
    <PackageReference Include="Fable.Python" Version="5.4.0" />
  </ItemGroup>

</Project>
```

Every package the library under test depends on must ship F# sources in a `fable/` folder.
Fable cannot compile a dll-only package, and the error names the *library* file that used it, not
the package:

```
error FABLE: Cannot reference entity from .dll reference,
Fable packages must include F# sources: SomePackage.SomeType`2
```

## 2. Test files

```fsharp
module MyLib.Tests.MyModuleTests

open MyLib

#if !FABLE_COMPILER
open Expecto
#else
open Fable.Pyxpecto
#endif

[<Tests>]
let tests =
    testList "MyModule" [
        testCase "adds integers" <| fun () ->
            Expect.equal (1 + 1) 2 "sum"
    ]
```

## 3. The aggregate

```fsharp
/// Not marked [<Tests>]: the individual suites already carry it, and annotating the
/// aggregate too would make `dotnet test` discover every test twice.
module MyLib.Tests.All

#if !FABLE_COMPILER
open Expecto
#else
open Fable.Pyxpecto
#endif

let all = testList "MyLib" [ MyModuleTests.tests ]
```

Do not call the aggregate module `Tests`. On .NET that name collides with Expecto's own `Tests`
module, which is where `runTestsWithCLIArgs` lives.

## 4. The entry point

```fsharp
module MyLib.Tests.Main

#if !FABLE_COMPILER
open Expecto
#else
open Fable.Pyxpecto
#endif

[<EntryPoint>]
let main argv =
#if !FABLE_COMPILER
    Expecto.Tests.runTestsWithCLIArgs [] argv All.all
#else
    !!(Pyxpecto.runTests [||] All.all)
#endif
```

Both .NET paths work from here: `dotnet run` executes the aggregate through Expecto's runner, and
`dotnet test` discovers the `[<Tests>]` values separately through `YoloDev.Expecto.TestSdk`.

Pyxpecto's runner reads the process arguments itself, so `[||]` is the usual second argument;
pass `ConfigArg.fromStrings argv` only to add arguments the process did not receive.

## 5. Running each target

Pin Fable as a local tool so every target compiles with the same version:

```json
// .config/dotnet-tools.json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "fable": { "version": "5.15.0", "commands": [ "fable" ], "rollForward": false }
  }
}
```

Run Fable from the directory holding the `.fsproj`, and delete the output directory first — stale
output makes failures much harder to read.

```bash
# .NET
dotnet run --project Tests/MyLib.Tests
dotnet test Tests/MyLib.Tests

# JavaScript
rm -rf fable-js && dotnet tool run fable --lang js -o fable-js && node fable-js/Main.js

# TypeScript, bundled so Node can execute it without a tsconfig
rm -rf fable-ts && dotnet tool run fable --lang ts -o fable-ts
npx esbuild fable-ts/Main.ts --bundle --platform=node --format=esm --outfile=bundle/Program.mjs
node bundle/Program.mjs

# Python
rm -rf fable-py && dotnet tool run fable --lang py -o fable-py
python -m venv .venv-fable-py
.venv-fable-py/bin/python -m pip install "fable-library==5.15.0"
.venv-fable-py/bin/python fable-py/main.py
```

JavaScript and TypeScript need a `package.json` with `"type": "module"` at the root for Node's ES
module resolution. Keep the `fable-library` version in step with the pinned Fable tool.

## Gotchas

- **Inline SRTP helpers fix their type at the first call site.** A local helper that wraps an
  inline statically-resolved function is no longer generic, so a second call with a different
  type fails to compile. Call the inline function directly at each site instead.
- **Fable erases generic type parameters on every target.** A helper doing `:? 'T` silently
  evaluates to `false`; mark it `inline` so Fable resolves the type at the call site. The same
  erasure means `GetType()` and generic reflection do not survive, so assertions that depend on
  them need a `#if !FABLE_COMPILER` branch.
- **`%A` on a string prints it quoted**, so a format string that wraps `%A` in quotes of its own
  produces doubled quotes. Assert on message fragments rather than whole strings when a
  third-party library formats them.
- **Name ignored lambda parameters** (`fun _v -> ...`, not `fun _ -> ...`) when the body captures
  an outer variable. Fable's Python output can otherwise reuse the captured name as the
  parameter and overwrite it.
- **`[<Struct>]` records that use copy expressions** (`{ s with ... }`) break on Fable Python,
  which lowercases the field names in the class but emits them PascalCase in the copy. Strip
  `[<Struct>]` under `FABLE_COMPILER_PYTHON`; it has no effect on any Fable target anyway.

## Generated output

None of it is checked in. Add:

```
**/bin/
**/obj/
**/fable-js/
**/fable-ts/
**/fable-py/
**/fable_modules/
__pycache__/
node_modules/
```
