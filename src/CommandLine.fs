namespace Fable.Pyxpecto

open System
open Fable.Core

module CommandLine =
    #if FABLE_COMPILER_PYTHON
    module Python =
        open Fable.Core.PyInterop

        [<ImportAll("sys")>]
        let sys: obj = nativeOnly

        let getArgs (): string[] =
            !!sys?argv |> Array.ofSeq

        let exitWith (exitCode: int): unit =
            !!sys?exit (nativeint (exitCode))
    #endif

    #if (FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT)
    module NodeJs =
        [<Emit("process.argv")>]
        let getArgs (): string[] = nativeOnly

        [<Emit("process.exit($0)")>]
        let exitWith (exitCode: int): unit = nativeOnly
    #endif

    #if !FABLE_COMPILER
    module NET =
        let getArgs (): string[] = Environment.GetCommandLineArgs()
        let exitWith (exitCode: int): unit = Environment.Exit(exitCode)
    #endif

    /// Used on a Fable target this library has no host bindings for yet. Without it the
    /// `#if` chains below collapse to empty function bodies and the file stops parsing,
    /// so an unsupported target fails with a syntax error in an unrelated file instead
    /// of degrading to "no arguments, no exit code".
    module Fallback =
        let getArgs (): string[] = [||]

        /// There is no portable way to set an exit code on an unknown host, so the run
        /// reports its result and returns normally instead.
        let exitWith (exitCode: int): unit = ignore exitCode

    let getArguments (): string[] =
        let args =
            #if FABLE_COMPILER_PYTHON
            Python.getArgs ()
            #endif
            #if (FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT)
            NodeJs.getArgs ()
            #endif
            #if !FABLE_COMPILER
            NET.getArgs ()
            #else
            #if !FABLE_COMPILER_PYTHON && !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
            Fallback.getArgs ()
            #endif
            #endif

        args

    let exitWith (exitCode: int): unit =
        #if FABLE_COMPILER_PYTHON
        Python.exitWith (exitCode)
        #endif
        #if (FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT)
        NodeJs.exitWith (exitCode)
        #endif
        #if !FABLE_COMPILER
        NET.exitWith (exitCode)
        #else
        #if !FABLE_COMPILER_PYTHON && !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
        Fallback.exitWith (exitCode)
        #endif
        #endif
