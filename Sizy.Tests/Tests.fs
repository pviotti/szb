namespace Sizy.Test

open System
open System.Collections.Concurrent
open System.IO
open System.IO.Abstractions
open System.IO.Abstractions.TestingHelpers
open Xunit
open FsUnit.Xunit

open FSharp.Collections.ParallelSeq

open Sizy.FileSystem

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

    let TestFileContent = "Hello world"

    let MockFs() =
        [ """/test/myfile.txt""", TestFileContent ]
        |> Seq.map (fun (k, v) -> k, MockFileData v)
        |> Map.ofSeq
        |> MockFileSystem

    let MockFsRootFolder = """/test"""

    let createTestFolder nBytes =
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

    let getSizeUnitTestData: Object [] [] =
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

    let checkSizeHelper (fs: IFileSystem) inputFolder = 
        let fsEntries = ConcurrentDictionary<string, Entry>()
        let fsManager = FsManager fs
        let ls = fsManager.List inputFolder
        let sizes = PSeq.map (fsManager.GetSize fsEntries) ls
        PSeq.sum sizes

    [<Fact>]
    let checkTotalSize() =
        let numWrittenBytes = r.Next MaxNumBytes
        let rootFolder = createTestFolder numWrittenBytes
        let totSize = checkSizeHelper (FileSystem()) rootFolder
        totSize |> should equal (int64 numWrittenBytes)
        Directory.Delete(rootFolder, true)

    [<Fact>]
    let checkTotalSizeMock() =
        let totSize = checkSizeHelper (MockFs()) MockFsRootFolder
        totSize |> should equal (int64 TestFileContent.Length)

    [<Theory>]
    [<MemberData("getSizeUnitTestData")>]
    let getSizeUnit (input: int64, outSize: float, outUnit: string) =
        FsManager.GetSizeUnit input |> should equal (outSize, outUnit)

