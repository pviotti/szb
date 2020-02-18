namespace Sizy.Test

open System
open System.IO
open Xunit
open FsUnit.Xunit

open Sizy

module ``Sizy Test`` =

    let r = Random()

    let Chars =
        Array.concat
            ([ [| 'a' .. 'z' |]
               [| 'A' .. 'Z' |]
               [| '0' .. '9' |] ])

    let rndStr n = String(Array.init n (fun _ -> Chars.[r.Next(Array.length Chars)]))
    let Sep = string Path.DirectorySeparatorChar
    let MaxNumBytes = 100

    let createTestFileSystem nBytes =
        let tmpPath = Path.GetTempPath()
        let rootFolder = Directory.CreateDirectory(tmpPath + Sep + (rndStr 5)).FullName
        let mutable remBytes = nBytes
        while remBytes > 0 do
            let bytes = r.Next(remBytes + 1)
            use fs = new FileStream(rootFolder + Sep + (rndStr 5), FileMode.CreateNew)
            use bw = new BinaryWriter(fs)
            Array.init bytes (fun _ -> 1uy) |> Array.iter bw.Write
            remBytes <- remBytes - bytes
        rootFolder

    let getSizeStringTestData: Object [] [] =
        [| [| -12345; 0.0; "B" |]
           [| 0; 0.0; "B" |]
           [| 1024; 1.0; "k" |]
           [| (1024 + 103)
              1.0
              "k" |]
           [| (1024 * 1024)
              1.0
              "M" |]
           [| Math.Pow(1024.0, 3.0)
              1.0
              "G" |] |]

    [<Fact>]
    let checkTotalSize() =
        let numWrittenBytes = r.Next MaxNumBytes
        let rootFolder = createTestFileSystem numWrittenBytes
        Program.sizyMain rootFolder |> should equal (float numWrittenBytes)
        Directory.Delete(rootFolder, true)

    [<Theory>]
    [<MemberData("getSizeStringTestData")>]
    let getSizeString (input: int64, outSize: float, outUnit: string) =
        Program.getSizeString input |> should equal (outSize, outUnit)


    [<EntryPoint>]
    let main _ = 0
