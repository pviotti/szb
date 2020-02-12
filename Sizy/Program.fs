module Sizy.Program

open Sizy.Config

open System
open System.IO
open FSharp.Collections.ParallelSeq

type Dir = Directory
let sizeUnits = [ "B"; "KiB"; "MiB"; "GiB"; "TiB"; "PiB"; "EiB" ]

let mutable errors = List.empty<string>

let rec getSize path =
    try
        let attr = File.GetAttributes path
        if attr.HasFlag FileAttributes.Directory then
            Dir.EnumerateFileSystemEntries path
            |> Seq.sumBy getSize
        else
            FileInfo(path).Length
    with ex ->
        errors <- List.singleton path |> List.append errors 
        //eprintfn "Error: %s" ex.Message
        0L

let getSizeString (bytes: int64) =
    if bytes = 0L then
        (0.0, sizeUnits.[0])
    else
        let bytesF = float (bytes)
        let sizeUnitsIdx = Math.Floor(Math.Log(bytesF, 1024.0))
        let num = Math.Round(bytesF / Math.Pow(1024.0, sizeUnitsIdx), 0)
        (num, sizeUnits.[int (sizeUnitsIdx)])

let printFormatted (path: string, size: int64) =
    let name = Array.last (path.Split Path.DirectorySeparatorChar)
    let (newSize, sizeUnit) = getSizeString size
    printfn "%10.0f %-3s %s" newSize sizeUnit name

let sizyMain path =
    let lsF = Dir.EnumerateFiles path
    let lsD = Dir.EnumerateDirectories path
    let sizeF = PSeq.map getSize lsF
    let sizeD = PSeq.map getSize lsD
    let sizeTot = PSeq.append sizeF sizeD |> PSeq.sum
    let print ls sizes = PSeq.zip ls sizes |> Seq.iter printFormatted
    print lsD sizeD
    print lsF sizeF
    printfn "%s" (String.replicate 14 "-")
    printFormatted ("", sizeTot)

let sizyMain2 path =
    let ls = Dir.EnumerateFileSystemEntries path
    let sizes = PSeq.map getSize ls
    let tot = PSeq.sum sizes
    printFormatted("", tot)

[<EntryPoint>]
let main argv =
    match Config.getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains InputPath then config.GetResult InputPath else Dir.GetCurrentDirectory()
        let stopWatch = Diagnostics.Stopwatch.StartNew()
        sizyMain2 path
        eprintfn "Exec time: %f" stopWatch.Elapsed.TotalMilliseconds
        String.concat " - " errors |> eprintfn "Path errors: %s" 
        0
    | ReturnVal ret ->
        ret
