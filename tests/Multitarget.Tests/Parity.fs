module Fable.Pyxpecto.ParityTests

open System
open System.Text.RegularExpressions
open Fable.Pyxpecto

/// Asserts that `case` fails the test, and that the failure message mentions `fragment`.
let private failsWith fragment case =
    let catch (exn: exn) = Expect.stringContains exn.Message fragment "unexpected failure message"
    Expect.throwsC case catch

let throwsTests =
    testList "throws" [
        testCase "throwsT accepts the expected exception type" <| fun _ ->
            Expect.throwsT<Model.AssertException> (fun () -> failtest "boom") "should have thrown AssertException"

        testCase "throwsT rejects another exception type" <| fun _ ->
            let case () =
                Expect.throwsT<Model.AssertException> (fun () -> raise (Exception "boom")) "wrong type"
            failsWith "Expected f to throw an exn of the expected type" case

        testCase "throwsT rejects a function that does not throw" <| fun _ ->
            let case () = Expect.throwsT<Model.AssertException> id "no throw"
            failsWith "Expected f to throw." case

        testCaseAsync "throwsAsync accepts a throwing computation" <| async {
            do! Expect.throwsAsync (fun () -> async { failtest "boom" }) "should have thrown"
        }

        testCaseAsync "throwsAsync rejects a non-throwing computation" <| async {
            let! outcome =
                async {
                    try
                        do! Expect.throwsAsync (fun () -> async { return () }) "no throw"
                        return None
                    with exn ->
                        return Some exn.Message
                }
            Expect.isSome outcome "throwsAsync should have failed"
        }

        testCaseAsync "throwsAsyncC hands the exception to the continuation" <| async {
            let mutable seen = ""
            do! Expect.throwsAsyncC
                    (fun () -> async { failtest "async boom" })
                    (fun exn -> async { seen <- exn.Message })
            Expect.equal seen "async boom" "continuation should receive the exception"
        }

        testCaseAsync "throwsAsyncT accepts the expected exception type" <| async {
            do! Expect.throwsAsyncT<Model.AssertException> (fun () -> async { failtest "boom" }) "should have thrown"
        }
    ]

let choiceTests =
    testList "Choice" [
        testCase "isChoice1Of2 works correctly" <| fun _ ->
            Expect.isChoice1Of2 (Choice1Of2 1) "should be Choice1Of2"

        testCase "isChoice1Of2 fails correctly" <| fun _ ->
            failsWith "Expected Choice1Of2" (fun () -> Expect.isChoice1Of2 (Choice2Of2 1) "should fail")

        testCase "isChoice2Of2 works correctly" <| fun _ ->
            Expect.isChoice2Of2 (Choice2Of2 1) "should be Choice2Of2"

        testCase "isChoice2Of2 fails correctly" <| fun _ ->
            failsWith "Expected Choice2Of2" (fun () -> Expect.isChoice2Of2 (Choice1Of2 1) "should fail")
    ]

let comparisonTests =
    testList "comparisons" [
        testCase "isLessThan works correctly" <| fun _ -> Expect.isLessThan 1 2 "1 < 2"
        testCase "isLessThan fails correctly" <| fun _ ->
            failsWith "to be less than" (fun () -> Expect.isLessThan 2 2 "should fail")

        testCase "isLessThanOrEqual works correctly" <| fun _ -> Expect.isLessThanOrEqual 2 2 "2 <= 2"
        testCase "isLessThanOrEqual fails correctly" <| fun _ ->
            failsWith "to be less than or equal to" (fun () -> Expect.isLessThanOrEqual 3 2 "should fail")

        testCase "isGreaterThan works correctly" <| fun _ -> Expect.isGreaterThan 2 1 "2 > 1"
        testCase "isGreaterThan fails correctly" <| fun _ ->
            failsWith "to be greater than" (fun () -> Expect.isGreaterThan 2 2 "should fail")

        testCase "isGreaterThanOrEqual works correctly" <| fun _ -> Expect.isGreaterThanOrEqual 2 2 "2 >= 2"
        testCase "isGreaterThanOrEqual fails correctly" <| fun _ ->
            failsWith "to be greater than or equal to" (fun () -> Expect.isGreaterThanOrEqual 1 2 "should fail")

        testCase "comparisons work on strings" <| fun _ ->
            Expect.isLessThan "abc" "abd" "abc < abd"
    ]

let floatTests =
    testList "floats" [
        testCase "floatEqual works correctly" <| fun _ ->
            Expect.floatEqual 1.0 1.0005 (Some 0.001) "within epsilon"

        testCase "floatEqual uses a default epsilon" <| fun _ ->
            Expect.floatEqual 1.0 1.0005 None "within default epsilon"

        testCase "floatEqual fails correctly" <| fun _ ->
            failsWith "within" (fun () -> Expect.floatEqual 1.0 2.0 None "should fail")

        testCase "isNaN works correctly" <| fun _ -> Expect.isNaN nan "nan is NaN"
        testCase "isNaN fails correctly" <| fun _ ->
            failsWith "should be a NaN" (fun () -> Expect.isNaN 1.0 "should fail")

        testCase "isInfinity works correctly" <| fun _ -> Expect.isInfinity infinity "infinity"
        testCase "isInfinity fails correctly" <| fun _ ->
            failsWith "should be infinity" (fun () -> Expect.isInfinity 1.0 "should fail")

        testCase "isPositiveInfinity works correctly" <| fun _ ->
            Expect.isPositiveInfinity infinity "positive infinity"
        testCase "isPositiveInfinity fails correctly" <| fun _ ->
            failsWith "should be positive infinity" (fun () -> Expect.isPositiveInfinity -infinity "should fail")

        testCase "isNegativeInfinity works correctly" <| fun _ ->
            Expect.isNegativeInfinity -infinity "negative infinity"
        testCase "isNegativeInfinity fails correctly" <| fun _ ->
            failsWith "should be negative infinity" (fun () -> Expect.isNegativeInfinity infinity "should fail")

        testCase "isNotPositiveInfinity works correctly" <| fun _ ->
            Expect.isNotPositiveInfinity -infinity "not positive infinity"
        testCase "isNotPositiveInfinity fails correctly" <| fun _ ->
            failsWith "was positive infinity" (fun () -> Expect.isNotPositiveInfinity infinity "should fail")

        testCase "isNotNegativeInfinity works correctly" <| fun _ ->
            Expect.isNotNegativeInfinity infinity "not negative infinity"
        testCase "isNotNegativeInfinity fails correctly" <| fun _ ->
            failsWith "was negative infinity" (fun () -> Expect.isNotNegativeInfinity -infinity "should fail")
    ]

let stringTests =
    testList "strings" [
        testCase "stringStarts works correctly" <| fun _ ->
            Expect.stringStarts "hello world" "hello" "starts with hello"

        testCase "stringStarts fails correctly" <| fun _ ->
            failsWith "to start with the prefix" (fun () -> Expect.stringStarts "hello world" "world" "should fail")

        testCase "stringStarts fails when the subject is shorter than the prefix" <| fun _ ->
            failsWith "longer or equal to prefix" (fun () -> Expect.stringStarts "he" "hello" "should fail")

        testCase "stringEnds works correctly" <| fun _ ->
            Expect.stringEnds "hello world" "world" "ends with world"

        testCase "stringEnds fails correctly" <| fun _ ->
            failsWith "to end with" (fun () -> Expect.stringEnds "hello world" "hello" "should fail")

        testCase "stringHasLength works correctly" <| fun _ ->
            Expect.stringHasLength "hello" 5 "hello has 5 chars"

        testCase "stringHasLength fails correctly" <| fun _ ->
            failsWith "to have length" (fun () -> Expect.stringHasLength "hello" 4 "should fail")

        testCase "isNotWhitespace works correctly" <| fun _ ->
            Expect.isNotWhitespace " a " "has a non-whitespace char"

        testCase "isNotWhitespace fails correctly" <| fun _ ->
            failsWith "Should not be whitespace" (fun () -> Expect.isNotWhitespace "   " "should fail")
    ]

let regexTests =
    testList "regex" [
        testCase "isMatch works correctly" <| fun _ ->
            Expect.isMatch "abc123" "\\d+" "contains digits"

        testCase "isMatch fails correctly" <| fun _ ->
            failsWith "to match pattern" (fun () -> Expect.isMatch "abc" "\\d+" "should fail")

        testCase "isRegexMatch works correctly" <| fun _ ->
            Expect.isRegexMatch "abc123" (Regex("\\d+")) "contains digits"

        testCase "isRegexMatch fails correctly" <| fun _ ->
            failsWith "to match regex" (fun () -> Expect.isRegexMatch "abc" (Regex("\\d+")) "should fail")

        testCase "isNotMatch works correctly" <| fun _ ->
            Expect.isNotMatch "abc" "\\d+" "has no digits"

        testCase "isNotMatch fails correctly" <| fun _ ->
            failsWith "not to match pattern" (fun () -> Expect.isNotMatch "abc123" "\\d+" "should fail")

        testCase "isNotRegexMatch works correctly" <| fun _ ->
            Expect.isNotRegexMatch "abc" (Regex("\\d+")) "has no digits"

        testCase "isNotRegexMatch fails correctly" <| fun _ ->
            failsWith "not to match regex" (fun () -> Expect.isNotRegexMatch "abc123" (Regex("\\d+")) "should fail")

        testCase "isMatchGroups works correctly" <| fun _ ->
            Expect.isMatchGroups "2026-08-26" "(\\d{4})-(\\d{2})-(\\d{2})" (fun groups -> groups.[1].Value = "2026") "year group"

        testCase "isMatchGroups fails correctly" <| fun _ ->
            let case () =
                Expect.isMatchGroups "2026-08-26" "(\\d{4})-(\\d{2})-(\\d{2})" (fun groups -> groups.[1].Value = "1999") "should fail"
            failsWith "group predicate" case

        testCase "isMatchRegexGroups works correctly" <| fun _ ->
            Expect.isMatchRegexGroups "2026-08-26" (Regex("(\\d{4})-(\\d{2})-(\\d{2})")) (fun groups -> groups.[3].Value = "26") "day group"

        testCase "isMatchRegexGroups fails correctly" <| fun _ ->
            let case () =
                Expect.isMatchRegexGroups "2026-08-26" (Regex("(\\d{4})")) (fun groups -> groups.[1].Value = "1999") "should fail"
            failsWith "group predicate" case
    ]

let sequenceTests =
    testList "sequences" [
        testCase "hasCountOf works correctly" <| fun _ ->
            Expect.hasCountOf [ 1; 2; 3; 4 ] 2u (fun x -> x % 2 = 0) "two even numbers"

        testCase "hasCountOf fails correctly" <| fun _ ->
            failsWith "Should be of count" (fun () -> Expect.hasCountOf [ 1; 2; 3; 4 ] 3u (fun x -> x % 2 = 0) "should fail")

        testCase "allEqual works correctly" <| fun _ ->
            Expect.allEqual [ 1; 1; 1 ] 1 "all ones"

        testCase "allEqual fails correctly" <| fun _ ->
            failsWith "don't equal to" (fun () -> Expect.allEqual [ 1; 2; 1 ] 1 "should fail")

        testCase "contains works correctly" <| fun _ ->
            Expect.contains [ 1; 2; 3 ] 2 "contains 2"

        testCase "contains fails correctly" <| fun _ ->
            failsWith "did not contain" (fun () -> Expect.contains [ 1; 2; 3 ] 4 "should fail")

        testCase "distribution works correctly" <| fun _ ->
            Expect.distribution [ 1; 1; 2 ] (Map.ofList [ 1, 2u; 2, 1u ]) "matching distribution"

        testCase "distribution fails on missing occurrences" <| fun _ ->
            failsWith "Missing elements" (fun () -> Expect.distribution [ 1; 2 ] (Map.ofList [ 1, 2u ]) "should fail")

        testCase "distribution fails on extra occurrences" <| fun _ ->
            failsWith "Extra elements" (fun () -> Expect.distribution [ 1; 1; 1 ] (Map.ofList [ 1, 2u ]) "should fail")

        testCase "sequenceEqual works correctly" <| fun _ ->
            Expect.sequenceEqual [ 1; 2; 3 ] [ 1; 2; 3 ] "same sequence"

        testCase "sequenceEqual fails correctly" <| fun _ ->
            failsWith "does not match at position 1" (fun () -> Expect.sequenceEqual [ 1; 2 ] [ 1; 3 ] "should fail")

        testCase "sequenceEqual reports a short actual" <| fun _ ->
            failsWith "shorter than expected" (fun () -> Expect.sequenceEqual [ 1 ] [ 1; 2 ] "should fail")

        testCase "sequenceEqual reports a long actual" <| fun _ ->
            failsWith "longer than expected" (fun () -> Expect.sequenceEqual [ 1; 2 ] [ 1 ] "should fail")

        testCase "sequenceStarts works correctly" <| fun _ ->
            Expect.sequenceStarts [ 1; 2; 3 ] [ 1; 2 ] "starts with 1,2"

        testCase "sequenceStarts fails correctly" <| fun _ ->
            failsWith "does not match at position 0" (fun () -> Expect.sequenceStarts [ 1; 2 ] [ 2 ] "should fail")

        testCase "sequenceContainsOrder works correctly" <| fun _ ->
            Expect.sequenceContainsOrder [ 1; 2; 3; 4 ] [ 2; 4 ] "in order"

        testCase "sequenceContainsOrder fails correctly" <| fun _ ->
            failsWith "Remainder of expected enumerable" (fun () -> Expect.sequenceContainsOrder [ 1; 2; 3 ] [ 3; 2 ] "should fail")

        testCase "isAscending works correctly" <| fun _ ->
            Expect.isAscending [ 1; 2; 2; 3 ] "ascending"

        testCase "isAscending fails correctly" <| fun _ ->
            failsWith "not ascending" (fun () -> Expect.isAscending [ 3; 1 ] "should fail")

        testCase "isDescending works correctly" <| fun _ ->
            Expect.isDescending [ 3; 2; 2; 1 ] "descending"

        testCase "isDescending fails correctly" <| fun _ ->
            failsWith "not descending" (fun () -> Expect.isDescending [ 1; 3 ] "should fail")
    ]

let failtestTests =
    testList "failtest" [
        testCase "failtest is available unqualified" <| fun _ ->
            failsWith "boom" (fun () -> failtest "boom")

        testCase "failtestf formats its message" <| fun _ ->
            failsWith "boom 42" (fun () -> failtestf "boom %i" 42)

        testCase "failtestNoStack is available unqualified" <| fun _ ->
            failsWith "boom" (fun () -> failtestNoStack "boom")

        testCase "failtestNoStackf formats its message" <| fun _ ->
            failsWith "boom 42" (fun () -> failtestNoStackf "boom %i" 42)

        testCase "skiptest raises IgnoreException" <| fun _ ->
            let isIgnore (exn: exn) =
                Expect.isTrue
                    (match exn with
                     | :? Model.IgnoreException -> true
                     | _ -> false)
                    "should be an IgnoreException"
            Expect.throwsC (fun () -> skiptest "not today") isIgnore

        testCase "skiptestf formats its message" <| fun _ ->
            let hasMessage (exn: exn) = Expect.equal exn.Message "skip 42" "formatted skip message"
            Expect.throwsC (fun () -> skiptestf "skip %i" 42) hasMessage

        testCase "a test that skips itself is reported as ignored" <| fun _ ->
            skiptest "skipped at runtime"
            failtest "should not be reached"

        testCaseAsync "an async test that skips itself is reported as ignored" <| async {
            skiptest "skipped at runtime, asynchronously"
            failtest "should not be reached"
        }
    ]

let theoryTests =
    testList "theories" [
        testTheory "testTheory names each case" [ 1; 2; 3 ] (fun x -> Expect.isGreaterThan x 0 "positive")

        testTheory "testTheory handles strings" [ "a"; "bb" ] (fun s -> Expect.isNonEmpty s "non-empty")

        ptestTheory "ptestTheory is skipped" [ 1 ] (fun _ -> failtest "should not run")

        testTheoryAsync "testTheoryAsync names each case" [ 1; 2 ] (fun x -> async { Expect.isGreaterThan x 0 "positive" })

        ptestTheoryAsync "ptestTheoryAsync is skipped" [ 1 ] (fun _ -> async { failtest "should not run" })

        testList "testFixture" (
            testFixture
                (fun (a, b) -> fun () -> Expect.equal (a + b) 3 "should sum to 3")
                [ "1 + 2", (1, 2); "2 + 1", (2, 1) ])

        testList "testFixtureAsync" (
            testFixtureAsync
                (fun (a, b) -> async { Expect.equal (a + b) 3 "should sum to 3" })
                [ "1 + 2", (1, 2); "2 + 1", (2, 1) ])

        testList "testParam" (
            testParam
                3
                [ "is greater than 2", (fun p () -> Expect.isGreaterThan p 2 "greater")
                  "is less than 4", (fun p () -> Expect.isLessThan p 4 "less") ])

        testList "testParamAsync" (
            testParamAsync
                3
                [ "is greater than 2", (fun p -> async { Expect.isGreaterThan p 2 "greater" }) ])
    ]

let sequencedGroupTests =
    testSequencedGroup "group name is documentation only" (
        testList "testSequencedGroup" [
            testCase "runs its tests" <| fun _ -> Expect.isTrue true "runs"
        ])

// Proves the erased `[<Tests>]` attribute resolves on Fable targets and leaves nothing behind
// in the generated output. On .NET the attribute belongs to Expecto, which this project does
// not reference, so the annotation is conditional.
#if FABLE_COMPILER
[<Tests>]
#endif
let all =
    testList "Expecto parity" [
        throwsTests
        choiceTests
        comparisonTests
        floatTests
        stringTests
        regexTests
        sequenceTests
        failtestTests
        theoryTests
        sequencedGroupTests
    ]
