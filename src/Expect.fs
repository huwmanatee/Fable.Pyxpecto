namespace Fable.Pyxpecto

open System
open System.Text
open Helper

[<RequireQualifiedAccess>]
module Expect =
    let inline equal (actual: 'a) (expected: 'a) msg: unit = Assert.NET.equal actual expected msg
    let notEqual actual expected msg: unit = Assert.NET.notEqual actual expected msg

    let private isNull' cond =
        match cond with
        | null -> true
        | _ -> false

    let isNull cond = equal (isNull' cond) true
    let isNotNull cond = notEqual (isNull' cond) true
    let isNotNaN cond msg = if Double.IsNaN cond then failtest (msg)
    let isNotInfinity cond msg = if Double.IsInfinity cond then failtest (msg)
    let isTrue cond = equal cond true
    let isFalse cond = equal cond false
    let isZero cond = equal cond 0
    let isEmpty (x: 'a seq) msg = if not (Seq.isEmpty x) then failtestf "%s. Should be empty." msg
    let pass () = equal true true "The test passed"
    let passWithMsg (message: string) = equal true true message
    let exists (x: 'a seq) (a: 'a -> bool) msg = if not (Seq.exists a x) then failtest (msg)
    let all (x: 'a seq) (a: 'a -> bool) msg = if not (Seq.forall a x) then failtest (msg)

    /// Expect the passed sequence not to be empty.
    let isNonEmpty (x: 'a seq) msg = if Seq.isEmpty x then failtestf "%s. Should not be empty." msg
    /// Expects x to be not null nor empty
    let isNotEmpty (x: 'a seq) msg =
        isNotNull x msg
        isNonEmpty x msg

    /// Expects x to be a sequence of length `number`
    let hasLength x number msg = equal (Seq.length x) number (sprintf "%s. Expected %A to have length %i" msg x number)

    /// Expects x to be Result.Ok
    let isOk x message =
        match x with
        | Ok _ -> passWithMsg message
        | Error x' -> failtestf "%s. Expected Ok, was Error(\"%s\")." message (string x')

    /// Expects the value to be a Result.Ok value and returns it or fails the test
    let wantOk x message =
        match x with
        | Ok x' ->
            passWithMsg message
            x'
        | Error x' -> failtestf "%s. Expected Ok, was Error('%A')." message x'

    let stringContains (subject: string) (substring: string) message =
        if not (subject.Contains(substring)) then
            failtestf "%s. Expected subject string '%s' to contain substring '%s'." message subject substring
        else
            passWithMsg message

    /// Expects x to be Result.Error
    let isError x message =
        match x with
        | Error _ -> passWithMsg message
        | Ok x' -> failtestf "%s. Expected Error _, was Ok(%A)." message x'

    let isSome x message =
        match x with
        | Some _ -> passWithMsg message
        | None -> failtestf "%s. Expected Some _, was None." message

    /// Expects the value to be a Some x value and returns x or fails the test
    let wantSome x message =
        match x with
        | Some x' ->
            passWithMsg message
            x'
        | None -> failtestf "%s. Expected Some _, was None." message

    /// Expects the value to be a Result.Error value and returns it or fails the test
    let wantError (x: Result<'a, 'b>) (message: string) =
        match x with
        | Error value ->
            passWithMsg message
            value
        | Ok value -> failtestf "%s. Expected Error _, was Ok(%A)." message value

    let isNone x message =
        match x with
        | None -> passWithMsg message
        | Some x' -> failtestf "%s. Expected None, was Some(%A)." message x'

    let private throws' f =
        try
            f ()
            None
        with exn ->
            Some exn

    /// Expects the passed function to throw an exception
    let throws f msg =
        match throws' f with
        | None -> failtestf "%s. Expected f to throw." msg
        | Some _ -> ()

    /// Expects the passed function to throw, then calls `cont` with the exception
    let throwsC f cont =
        match throws' f with
        | None -> failtest "Expected f to throw."
        | Some exn -> cont exn

    /// Expects the `actual` sequence to contain all elements from `expected`
    /// It doesn't take into account the number of occurrences and the order of elements.
    /// Calling this function will enumerate both sequences; they have to be finite.
    let containsAll (actual: _ seq) (expected: _ seq) message =
        let actualEls, expectedEls = List.ofSeq actual, List.ofSeq expected

        let matchingEls =
            actualEls
            |> List.filter (fun a -> expectedEls |> List.contains a)

        let extraEls =
            actualEls
            |> List.filter (fun a -> not (matchingEls |> List.contains a))

        let missingEls =
            expectedEls
            |> List.filter (fun e -> not (matchingEls |> List.contains e))

        if List.isEmpty missingEls then
            ()
        else
            failtestf
                "%s. Sequence `actual` does not contain all `expected` elements. Missing elements from `actual`: %A. Extra elements in `actual`: %A"
                message
                missingEls
                extraEls

    /// Expects `actual` and `expected` (that are both floats) to be within a
    /// given `accuracy`.
    let floatClose accuracy actual expected message =
        if Double.IsInfinity actual then
            failtestf "%s. Expected actual to not be infinity, but it was." message
        elif Double.IsInfinity expected then
            failtestf "%s. Expected expected to not be infinity, but it was." message
        elif Accuracy.areClose accuracy actual expected |> not then
            failtestf
                "%s. Expected difference to be less than %.20g for accuracy {absolute=%.20g; relative=%.20g}, but was %.20g. actual=%.20g expected=%.20g"
                message
                (Accuracy.areCloseRhs accuracy actual expected)
                accuracy.absolute
                accuracy.relative
                (Accuracy.areCloseLhs actual expected)
                actual
                expected

    /// Expects `actual` to be less than `expected` or to be within a
    /// given `accuracy`.
    let floatLessThanOrClose accuracy actual expected message =
        if actual > expected then
            floatClose accuracy actual expected message

    /// Expects `actual` to be greater than `expected` or to be within a
    /// given `accuracy`.
    let floatGreaterThanOrClose accuracy actual expected message =
        if actual < expected then
            floatClose accuracy actual expected message

    // ---------------------------------------------------------------------------------------------
    // Expecto parity additions
    // ---------------------------------------------------------------------------------------------

    /// Expects the passed function to throw an exception of type `'texn`.
    /// `inline`, and self-contained rather than reusing the helpers above, so that Fable resolves
    /// `'texn` to a concrete type at the call site: its targets cannot type test an erased generic.
    [<RequiresExplicitTypeArguments>]
    let inline throwsT<'texn when 'texn :> exn> f message =
        let thrown =
            try
                f ()
                None
            with exn ->
                Some exn

        match thrown with
        | None -> failtestf "%s. Expected f to throw." message
        | Some exn ->
            match exn with
            | :? 'texn -> ()
            | _ -> failtestf "%s. Expected f to throw an exn of the expected type, but got: %s." message exn.Message

    let private throwsAsync' f =
        async {
            try
                do! f ()
                return None
            with exn ->
                return Some exn
        }

    /// Expects `f` to throw an exception asynchronously.
    let throwsAsync f message =
        async {
            let! thrown = throwsAsync' f

            match thrown with
            | None -> failtestf "%s. Expected f to throw." message
            | Some _ -> ()
        }

    /// Expects `f` to throw asynchronously, then calls `cont` with the exception.
    let throwsAsyncC f cont =
        async {
            let! thrown = throwsAsync' f

            match thrown with
            | None -> failtest "Expected f to throw."
            | Some exn -> do! cont exn
        }

    /// Expects `f` to throw an exception of type `'texn` asynchronously.
    /// `inline` for the same reason as `throwsT`.
    [<RequiresExplicitTypeArguments>]
    let inline throwsAsyncT<'texn when 'texn :> exn> f message =
        async {
            let! thrown =
                async {
                    try
                        do! f ()
                        return None
                    with exn ->
                        return Some exn
                }

            match thrown with
            | None -> failtestf "%s. Expected f to throw." message
            | Some exn ->
                match exn with
                | :? 'texn -> ()
                | _ -> failtestf "%s. Expected f to throw an exn of the expected type, but got: %s." message exn.Message
        }

    /// Expects the value to be a Choice1Of2 value.
    let isChoice1Of2 x message =
        match x with
        | Choice1Of2 _ -> ()
        | Choice2Of2 x' -> failtestf "%s. Expected Choice1Of2, was Choice2Of2(%A)." message x'

    /// Expects the value to be a Choice2Of2 value.
    let isChoice2Of2 x message =
        match x with
        | Choice1Of2 x' -> failtestf "%s. Expected Choice2Of2 _, was Choice1Of2(%A)." message x'
        | Choice2Of2 _ -> ()

    /// Expects `a` to be less than `b`.
    let isLessThan a b message =
        if a >= b then
            failtestf "%s. Expected a (%A) to be less than b (%A)." message a b

    /// Expects `a` to be less than or equal to `b`.
    let isLessThanOrEqual a b message =
        if a > b then
            failtestf "%s. Expected a (%A) to be less than or equal to b (%A)." message a b

    /// Expects `a` to be greater than `b`.
    let isGreaterThan a b message =
        if a <= b then
            failtestf "%s. Expected a (%A) to be greater than b (%A)." message a b

    /// Expects `a` to be greater than or equal to `b`.
    let isGreaterThanOrEqual a b message =
        if a < b then
            failtestf "%s. Expected a (%A) to be greater than or equal to b (%A)." message a b

    /// Expects `actual` and `expected` (that are both floats) to equal within a given `epsilon`.
    /// Prefer the more general `Expect.floatClose`.
    let floatEqual actual expected epsilon message =
        let epsilon = defaultArg epsilon 0.001

        if not (expected <= actual + epsilon && expected >= actual - epsilon) then
            failtestf "%s. Actual value was %f but was expected to be %f within %f epsilon." message actual expected epsilon

    /// Expects the passed float to be NaN.
    let isNaN (f: float) message =
        if not (Double.IsNaN f) then
            failtestf "%s. Float should be a NaN (not a number) value." message

    /// Expects the passed float to be positive infinity.
    let isPositiveInfinity (actual: float) message =
        if not (Double.IsPositiveInfinity actual) then
            failtestf "%s. Float should be positive infinity." message

    /// Expects the passed float to be negative infinity.
    let isNegativeInfinity (actual: float) message =
        if not (Double.IsNegativeInfinity actual) then
            failtestf "%s. Float should be negative infinity." message

    /// Expects the passed float to be infinity.
    let isInfinity (actual: float) message =
        if not (Double.IsInfinity actual) then
            failtestf "%s. Float should be infinity." message

    /// Expects the passed float not to be positive infinity.
    let isNotPositiveInfinity (actual: float) message =
        if Double.IsPositiveInfinity actual then
            failtestf "%s. Float was positive infinity." message

    /// Expects the passed float not to be negative infinity.
    let isNotNegativeInfinity (actual: float) message =
        if Double.IsNegativeInfinity actual then
            failtestf "%s. Float was negative infinity." message

    /// Expects the passed string not to be whitespace only.
    let isNotWhitespace (actual: string) message =
        isNotEmpty actual message

        if actual |> Seq.forall Char.IsWhiteSpace then
            failtestf "%s. Should not be whitespace." message

    /// Expects the count of elements satisfying `selector` in `actual` to equal `expected`.
    let hasCountOf (actual: 'a seq) (expected: uint32) (selector: 'a -> bool) message =
        let hits =
            actual
            |> Seq.fold (fun acc element -> if selector element then acc + 1u else acc) 0u

        if hits <> expected then
            failtestf "%s. Should be of count: %d, but was: %d" message expected hits

    let private formatOffenders (offenders: (int * string) list) =
        offenders
        |> List.map (fun (index, item) -> sprintf "Element at index: %d which is equal to: %s" index item)
        |> String.concat "\n"

    let private offendersOf (actual: 'a seq) asserter =
        actual
        |> Seq.indexed
        |> Seq.choose (fun (index, item) -> if asserter item then None else Some(index, sprintf "%A" item))
        |> List.ofSeq

    /// Expects that all elements from `actual` equal `equalTo`.
    let allEqual (actual: 'a seq) equalTo message =
        match offendersOf actual ((=) equalTo) with
        | [] -> ()
        | offenders ->
            failtestf "%s. Some elements don't equal to `equalTo`: %A.\n%s" message equalTo (formatOffenders offenders)

    /// Expects `sequence` to contain `element`.
    let contains sequence element message =
        match sequence |> Seq.tryFind ((=) element) with
        | Some _ -> ()
        | None -> failtestf "%s. Sequence did not contain %A." message element

    /// Expects the `actual` sequence to contain every `expected` item as many times as the map says.
    /// The order of elements is not taken into account, and both sequences have to be finite.
    let distribution (actual: 'a seq) (expected: Map<'a, uint32>) message =
        let groupByOccurrences sequence =
            sequence
            |> Seq.groupBy id
            |> Seq.map (fun (item, occurrences) -> item, occurrences |> Seq.length |> uint32)
            |> Map.ofSeq

        let groupedActual = groupByOccurrences actual

        /// Reports every element `toCheck` demands more occurrences of than `toContain` supplies.
        /// `isExcept` only decides which side of the printed `(found/expected)` pair each count goes.
        let differences (toCheck: Map<'a, uint32>) (toContain: Map<'a, uint32>) isExcept =
            toCheck
            |> Map.toList
            |> List.choose (fun (element, count) ->
                let left, right =
                    match Map.tryFind element toContain with
                    | Some found when count > found -> if isExcept then found, count else count, found
                    | Some _ -> 0u, 0u
                    | None -> if isExcept then 0u, count else count, 0u

                if right = 0u then
                    None
                else
                    Some(sprintf "'%A' (%d/%d)" element left right))

        let extra = differences groupedActual expected false
        let missing = differences expected groupedActual true

        if not (List.isEmpty missing && List.isEmpty extra) then
            let section title elements =
                if List.isEmpty elements then
                    ""
                else
                    sprintf "\n\t%s:\n\t%s" title (String.concat "\n\t" elements)

            failtestf
                "%s. Sequence `actual` does not contain every `expected` element.%s%s"
                message
                (section "Missing elements from `actual` (found/expected)" missing)
                (section "Extra elements in `actual` (found/expected)" extra)

    let private printSeq (xs: 'a seq) =
        xs
        |> Seq.mapi (fun i x -> sprintf "  [%i] %A" i x)
        |> String.concat "\n"

    /// Expects the `actual` sequence to equal the `expected` one.
    let sequenceEqual (actual: 'a seq) (expected: 'a seq) message =
        let baseMsg () =
            Assert.NET.printVerses "expected" (printSeq expected) "  actual" (printSeq actual)

        match Assert.NET.firstDiff actual expected with
        | _, None, None -> ()
        | i, Some a, Some e ->
            failtestf "%s. Sequence does not match at position %i. Expected item: %A, but got %A.%s" message i e a (baseMsg ())
        | i, None, Some e ->
            failtestf "%s. Sequence actual shorter than expected, at pos %i for expected item %A.%s" message i e (baseMsg ())
        | i, Some a, None ->
            failtestf "%s. Sequence actual longer than expected, at pos %i found item %A.%s" message i a (baseMsg ())

    /// Expects the sequence `subject` to start with `prefix`.
    let sequenceStarts (subject: 'a seq) (prefix: 'a seq) message =
        match Assert.NET.firstDiff subject prefix with
        | _, _, None -> ()
        | i, Some s, Some p ->
            failtestf "%s. Sequence does not match at position %i. Expected: %A, but got %A." message i p s
        | i, None, Some p ->
            failtestf "%s. Sequence actual shorter than expected, at pos %i for expected item %A." message i p

    /// Expects the sequence `actual` to contain every element of `expected`, in that order.
    /// Both sequences have to be finite.
    let sequenceContainsOrder (actual: 'a seq) (expected: 'a seq) message =
        let rec loop remaining consumed rest =
            match remaining with
            | [] -> ()
            | _ ->
                match rest with
                | [] ->
                    failtestf
                        "%s. Remainder of expected enumerable:\n%s\nWent through actual enumerable (%i items):\n%s"
                        message
                        (printSeq remaining)
                        (List.length consumed)
                        (printSeq (List.rev consumed))
                | head :: tail ->
                    let stillMissing =
                        match remaining with
                        | next :: rest' when next = head -> rest'
                        | _ -> remaining

                    loop stillMissing (head :: consumed) tail

        loop (List.ofSeq expected) [] (List.ofSeq actual)

    /// Expects the sequence `subject` to be ascending.
    let isAscending (subject: 'a seq) message =
        if not (subject |> Seq.windowed 2 |> Seq.forall (fun s -> s.[1] >= s.[0])) then
            failtestf "%s. Sequence is not ascending" message

    /// Expects the sequence `subject` to be descending.
    let isDescending (subject: 'a seq) message =
        if not (subject |> Seq.windowed 2 |> Seq.forall (fun s -> s.[1] <= s.[0])) then
            failtestf "%s. Sequence is not descending" message

    /// Expects the string `subject` to start with `prefix`.
    let stringStarts (subject: string) (prefix: string) message =
        match Assert.NET.firstDiff subject prefix with
        | _, _, None -> ()
        | i, None, Some p ->
            failtestf
                "%s. Expected subject string to be longer or equal to prefix. Differs at position %i with char '%c'.%s"
                message
                i
                p
                (Assert.NET.printVerses " prefix" prefix "subject" subject)
        | i, Some s, Some p ->
            failtestf
                "%s. Expected subject string to start with the prefix. Differs at position %i with subject '%c' and prefix '%c'.%s"
                message
                i
                s
                p
                (Assert.NET.printVerses " prefix" prefix "subject" subject)

    /// Expects the string `subject` to end with `suffix`.
    let stringEnds (subject: string) (suffix: string) message =
        if not (subject.EndsWith suffix) then
            failtestf "%s. Expected subject string '%s' to end with '%s'." message subject suffix

    /// Expects the string `subject` to have length `length`.
    let stringHasLength (subject: string) (length: int) message =
        if subject.Length <> length then
            failtestf "%s. Expected subject string '%s' to have length '%d'." message subject length

    /// Expects `actual` to match `pattern`.
    let isMatch actual pattern message =
        if not (RegularExpressions.Regex.Match(actual, pattern).Success) then
            failtestf "%s. Expected %s to match pattern: /%s/" message actual pattern

    /// Expects `actual` to match `regex`.
    let isRegexMatch actual (regex: RegularExpressions.Regex) message =
        if not (regex.Match(actual).Success) then
            failtestf "%s. Expected %s to match regex." message actual

    /// Expects `actual` not to match `pattern`.
    let isNotMatch actual pattern message =
        if RegularExpressions.Regex.Match(actual, pattern).Success then
            failtestf "%s. Expected %s not to match pattern: /%s/" message actual pattern

    /// Expects `actual` not to match `regex`.
    let isNotRegexMatch actual (regex: RegularExpressions.Regex) message =
        if regex.Match(actual).Success then
            failtestf "%s. Expected %s not to match regex." message actual

    /// Expects the groups captured from matching `pattern` against `actual` to satisfy `matchesOperator`.
    let isMatchGroups actual pattern (matchesOperator: RegularExpressions.GroupCollection -> bool) message =
        let groups = RegularExpressions.Regex.Match(actual, pattern).Groups

        if not (matchesOperator groups) then
            failtestf "%s. Expected %s to match pattern: /%s/ and satisfy the given group predicate." message actual pattern

    /// Expects the groups captured from matching `regex` against `actual` to satisfy `matchesOperator`.
    let isMatchRegexGroups actual (regex: RegularExpressions.Regex) (matchesOperator: RegularExpressions.GroupCollection -> bool) message =
        let groups = regex.Match(actual).Groups

        if not (matchesOperator groups) then
            failtestf "%s. Expected %s to match regex and satisfy the given group predicate." message actual
