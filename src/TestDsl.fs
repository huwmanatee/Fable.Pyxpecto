namespace Fable.Pyxpecto

open System
open Model

[<AutoOpen>]
module Test =
    let testCase name body = SyncTest(name, body, Normal)
    let ptestCase name body = SyncTest(name, body, Pending)
    let ftestCase name body = SyncTest(name, body, Focused)
    let testCaseAsync name body = AsyncTest(name, body, Normal)
    let ptestCaseAsync name body = AsyncTest(name, body, Pending)
    let ftestCaseAsync name body = AsyncTest(name, body, Focused)
    let testList name tests = TestList(name, tests, Normal)
    let ftestList name tests = TestList(name, tests, Focused)
    let ptestList name tests = TestList(name, tests, Pending)

    let testSequenced test =
        match test with
        | SyncTest(name, test, state) -> TestListSequential(name, [ SyncTest(name, test, state) ], state)
        | AsyncTest(name, test, state) -> TestListSequential(name, [ AsyncTest(name, test, state) ], state)
        | TestList(name, tests, focused) -> TestListSequential(name, tests, focused)
        | TestListSequential(name, tests, focused) -> TestListSequential(name, tests, focused)

    /// Test case or list needs to run sequenced with the other tests in this group.
    /// Pyxpecto runs every test one after another, so the group name is documentation only.
    let testSequencedGroup (_groupName: string) test = testSequenced test

    /// Fails the currently running test.
    let inline failtest msg = Helper.failtest msg
    /// Fails the currently running test.
    let inline failtestf fmt = Printf.ksprintf Helper.failtest fmt
    /// Fails the currently running test. Behaves like `failtest`; Pyxpecto never attaches a stack
    /// trace to assertion failures.
    let inline failtestNoStack msg = Helper.failtestNoStack msg
    /// Fails the currently running test. Behaves like `failtestf`; Pyxpecto never attaches a stack
    /// trace to assertion failures.
    let inline failtestNoStackf fmt = Printf.ksprintf Helper.failtestNoStack fmt
    /// Skips the currently running test, reporting it as ignored.
    let inline skiptest msg = Helper.skiptest msg
    /// Skips the currently running test, reporting it as ignored.
    let inline skiptestf fmt = Printf.ksprintf Helper.skiptest fmt

    /// Test case computation expression builder
    type TestCaseBuilder(name: string, focusState: FocusState) =
        member _.Zero() = ()
        member _.Delay fn = fn
        member _.Using(disposable: #IDisposable, fn) = using disposable fn
        member _.While(condition, fn) = while condition () do fn ()
        member _.For(sequence, fn) = for i in sequence do fn i
        member _.Combine(fn1, fn2) = fn2(); fn1

        member _.TryFinally(fn, compensation) =
            try fn()
            finally compensation()

        member _.TryWith(fn, catchHandler) =
            try fn()
            with e -> catchHandler e

        member _.Run fn = SyncTest(name, fn, focusState)

    /// Builds a test case
    let inline test name = TestCaseBuilder(name, Normal)
    /// Builds a test case that will ignore other unfocused tests
    let inline ftest name = TestCaseBuilder(name, Focused)
    /// Builds a test case that will be ignored
    let inline ptest name = TestCaseBuilder(name, Pending)

    /// Async test case computation expression builder
    type TestAsyncBuilder(name: string, focusState: FocusState) =
        member _.Zero() = async.Zero ()
        member _.Delay fn = async.Delay fn
        member _.Return x = async.Return x
        member _.ReturnFrom x = async.ReturnFrom x
        member _.Bind(computation, fn) = async.Bind(computation, fn)
        member _.Using(disposable: #IDisposable, fn) = async.Using(disposable, fn)
        member _.While(condition, fn) = async.While(condition, fn)
        member _.For(sequence, fn) = async.For(sequence, fn)
        member _.Combine(fn1, fn2) = async.Combine(fn1, fn2)
        member _.TryFinally(fn, compensation) = async.TryFinally(fn, compensation)
        member _.TryWith(fn, catchHandler) = async.TryWith(fn, catchHandler)
        member _.Run fn = AsyncTest(name, fn, focusState)

    /// Builds an async test case
    let inline testAsync name = TestAsyncBuilder(name, Normal)
    /// Builds an async test case that will ignore other unfocused tests
    let inline ftestAsync name = TestAsyncBuilder(name, Focused)
    /// Builds an async test case that will be ignored
    let inline ptestAsync name = TestAsyncBuilder(name, Pending)

    /// Names a theory case for its test. Strings are quoted so an empty case stays visible.
    let private stringify (value: 'a) =
        match box value with
        | null -> "null"
        | :? string as s -> "\"" + s + "\""
        | boxed -> string boxed

    /// Applies `setup` to a list of named partial tests to build test cases.
    /// `setup partialTest` is applied when the test runs, not when the list is built, so a setup
    /// that throws is reported against its own test. The eta expansion is also what keeps Fable's
    /// Python output from mis-currying the two-step application.
    let testFixture setup namedPartialTests =
        namedPartialTests
        |> Seq.map (fun (name, partialTest) -> testCase name (fun () -> setup partialTest ()))
        |> List.ofSeq

    /// Applies `setupAsync` to a list of named partial tests to build async test cases.
    let testFixtureAsync setupAsync namedPartialTests =
        namedPartialTests
        |> Seq.map (fun (name, partialTest) -> testCaseAsync name (setupAsync partialTest))
        |> List.ofSeq

    /// Applies `param` to a list of named partial tests.
    let testParam param namedPartialTests =
        namedPartialTests
        |> Seq.map (fun (name, partialTest) -> testCase name (fun () -> partialTest param ()))
        |> List.ofSeq

    /// Applies `param` to a list of named partial async tests.
    let testParamAsync param namedPartialTests =
        namedPartialTests
        |> Seq.map (fun (name, partialTest) -> testCaseAsync name (partialTest param))
        |> List.ofSeq

    let private theory listCtor caseCtor name cases test =
        cases
        |> Seq.map (fun case -> caseCtor (stringify case) (fun () -> test case |> ignore))
        |> List.ofSeq
        |> listCtor name

    /// Builds one test case per entry in `cases`, named after the case.
    let testTheory name cases test = theory testList testCase name cases test
    /// Builds a theory whose cases will ignore other unfocused tests.
    let ftestTheory name cases test = theory ftestList ftestCase name cases test
    /// Builds a theory whose cases will be ignored.
    let ptestTheory name cases test = theory ptestList ptestCase name cases test

    let private theoryAsync listCtor caseCtor name cases test =
        cases
        |> Seq.map (fun case -> caseCtor (stringify case) (async { do! test case }))
        |> List.ofSeq
        |> listCtor name

    /// Builds one async test case per entry in `cases`, named after the case.
    let testTheoryAsync name cases test = theoryAsync testList testCaseAsync name cases test
    /// Builds an async theory whose cases will ignore other unfocused tests.
    let ftestTheoryAsync name cases test = theoryAsync ftestList ftestCaseAsync name cases test
    /// Builds an async theory whose cases will be ignored.
    let ptestTheoryAsync name cases test = theoryAsync ptestList ptestCaseAsync name cases test
