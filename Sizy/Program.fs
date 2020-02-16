module Sizy.Program

open Sizy.Config

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open FSharp.Collections.ParallelSeq

type Dir = Directory

let SizeUnits = [ "B"; "k"; "M"; "G"; "T"; "P"; "E" ]

type Entry(path: string, size: int64, isDir: bool) =

    member this.name =
        Array.last (path.Split Path.DirectorySeparatorChar) + if isDir then string (Path.DirectorySeparatorChar) else ""

    member this.size = size
    member this.isDir = isDir

let rec getSize (fsEntries: IDictionary<_, _>) (errors: IDictionary<_, _>) path =
    try
        let attr = File.GetAttributes path

        let size, isDir =
            if attr.HasFlag FileAttributes.Directory
            then Dir.EnumerateFileSystemEntries path |> Seq.sumBy (getSize fsEntries errors), true
            else FileInfo(path).Length, false
        fsEntries.[path] <- Entry(path, size, isDir)
        size
    with ex ->
        errors.[path] <- ex.Message
        0L

let getSizeString bytes =
    if bytes = 0L then
        (0.0, SizeUnits.[0])
    else
        let bytesF = float (bytes)
        let sizeUnitsIdx = Math.Floor(Math.Log(bytesF, 1024.0))
        let num = Math.Round(bytesF / Math.Pow(1024.0, sizeUnitsIdx), 0)
        (num, SizeUnits.[int (sizeUnitsIdx)])

let printFormatted path name size =
    let newSize, sizeUnit = getSizeString size
    printfn "%10.0f %-1s %s" newSize sizeUnit name

let sizyMain path fsEntries errors =
    let ls = Dir.EnumerateFileSystemEntries path
    let sizes = PSeq.map (getSize fsEntries errors) ls
    let totSize, sizeUnit = getSizeString (PSeq.sum sizes)

    let print filter =
        PSeq.filter filter ls
        |> PSeq.sort
        |> Seq.iter (fun p -> printFormatted path fsEntries.[p].name fsEntries.[p].size)
    print (fun x -> fsEntries.ContainsKey x && fsEntries.[x].isDir)
    print (fun x -> fsEntries.ContainsKey x && not fsEntries.[x].isDir)
    printfn "%s\n%10.0f %-1s" (String.replicate 12 "-") totSize sizeUnit

[<EntryPoint>]
let main argv =
    match Config.getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains InputPath then config.GetResult InputPath else Dir.GetCurrentDirectory()

        let fsEntries = ConcurrentDictionary<string, Entry>()
        let errors = ConcurrentDictionary<string, string>()
        let stopWatch = Diagnostics.Stopwatch.StartNew()
        sizyMain path fsEntries errors
        eprintfn "Exec time: %f" stopWatch.Elapsed.TotalMilliseconds
        Seq.iter (fun x ->
            eprintfn "\n\t%s - %s" x errors.[x]) errors.Keys
        0
    | ReturnVal ret -> ret
