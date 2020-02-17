module Tests

open System
open System.IO
open Xunit
open FsUnit.Xunit

open Sizy

let r = Random()

let chars =
    Array.concat
        ([ [| 'a' .. 'z' |]
           [| 'A' .. 'Z' |]
           [| '0' .. '9' |] ])

let charsLen = Array.length chars
let rndStr n = String(Array.init n (fun _ -> chars.[r.Next charsLen]))
let sep = string Path.DirectorySeparatorChar

module ``Sizy tests`` =

    let createTestFileSystem n =
        let tmpPath = Path.GetTempPath()
        let rootFolder = Directory.CreateDirectory(tmpPath + sep + (rndStr 5))
        use fs1 = new FileStream(rootFolder.FullName + sep + (rndStr 5), FileMode.CreateNew)
        use bw1 = new BinaryWriter(fs1)
        for x in [ 1 .. n ] do
            bw1.Write 1uy
        rootFolder.FullName

    let getSizeStringTestData: Object [] [] =
        [| [| -12345; 0.0; "B" |]
           [| 0; 0.0; "B" |]
           [| 1024; 1.0; "k" |]
           [| 1024 + 103
              1.0
              "k" |]
           [| 1024 * 1024
              1.0
              "M" |]
           [| Math.Pow(1024.0, 3.0)
              1.0
              "G" |] |]

    [<Fact>]
    let checkSize() =
        let rootFolder = createTestFileSystem 100
        Program.sizyMain rootFolder |> should equal 100.0

    //[<Fact>]
    //let ``It should exit with 0`` () =
    //    Program.main([||]) |> should equal 0

    [<Theory>]
    [<MemberData("getSizeStringTestData")>]
    let getSizeString (input: int64, outSize: float, outUnit: string) =
        Program.getSizeString input |> should equal (outSize, outUnit)



[<EntryPoint>]
let main argv = 0
