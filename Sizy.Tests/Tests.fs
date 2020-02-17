module Tests

open System
open Xunit
open FsUnit.Xunit

open Sizy

module ``Sizy tests`` =

    let getSizeStringTestData : Object [] [] =
        [|
            [| 0; 0.0; "B" |]
            [| 1024; 1.0; "k" |]
            [| 1024+103; 1.0; "k" |]
            [| 1024*1024; 1.0; "M" |]
            [| Math.Pow(1024.0,3.0); 1.0; "G" |]
        |]

    [<Fact>]
    let ``It should exit with 0`` () =
        Program.main([||]) |> should equal 0

    [<Theory>]
    [<MemberData("getSizeStringTestData")>]
    let ``getSizeString`` (input:int64, outSize:float, outUnit:string) =
        Program.getSizeString input |> should equal (outSize, outUnit)



[<EntryPoint>]
let main argv = 0
