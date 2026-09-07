namespace Fable.Pyxpecto

/// Lets one test source tree serve .NET Expecto and every Fable target, so that the only
/// per-file conditional a shared suite needs is the `open` that selects the test DSL.
[<AutoOpen>]
module Interop =

#if FABLE_COMPILER
    /// Stand-in for Expecto's `[<Tests>]` discovery attribute.
    ///
    /// On .NET the attribute belongs to Expecto, which owns discovery through
    /// `YoloDev.Expecto.TestSdk`; Pyxpecto deliberately does not define one there, so a
    /// project that opens both has nothing to disambiguate. Fable cannot compile Expecto, so
    /// the annotation would not resolve on its targets — this shim lets the same `[<Tests>]`
    /// stay in the source, and `Erase` keeps it out of the generated JavaScript, TypeScript
    /// and Python.
    [<Fable.Core.Erase>]
    type TestsAttribute() =
        inherit System.Attribute()
#endif

    /// Adapts the result of `Pyxpecto.runTests` to the `int` an `[<EntryPoint>]` must return.
    ///
    /// `runTests` finishes with `Async.StartAsPromise` on JavaScript and TypeScript, handing
    /// back a promise, and with `Async.RunSynchronously` elsewhere, handing back the exit
    /// code. This is the JsInterop cast on the first two targets and the identity function on
    /// the rest, so `!! Pyxpecto.runTests args tests` type checks everywhere.
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
    let inline (!!) (value: 'a) : 'b = Fable.Core.JsInterop.op_BangBang value
#else
    let inline (!!) (value: 'a) = value
#endif
