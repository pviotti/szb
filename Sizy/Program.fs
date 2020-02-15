module Sizy.Program

open Sizy.Config

open System
open System.IO
open System.Collections.Concurrent
open FSharp.Collections.ParallelSeq

type Dir = Directory
let sizeUnits = [ "B"; "k"; "M"; "G"; "T"; "P"; "E" ]

type Entry(path: string, size: int64, isDir: bool) =
    member this.path = Array.last (path.Split Path.DirectorySeparatorChar) + if isDir then "/" else ""
    member this.size = size
    member this.isDir = isDir

let errors = ConcurrentDictionary<string, string>()
let entries = ConcurrentDictionary<string, Entry>()

let rec getSize path =
    try
        let attr = File.GetAttributes path
        let size, isDir =
            if attr.HasFlag FileAttributes.Directory then
                Dir.EnumerateFileSystemEntries path |> Seq.sumBy getSize, true
            else
                FileInfo(path).Length, false
        entries.[path] <- Entry(path, size, isDir)
        size
    with ex ->
        errors.[path] <- ex.Message
        0L

let getSizeString (bytes: int64) =
    if bytes = 0L then
        (0.0, sizeUnits.[0])
    else
        let bytesF = float (bytes)
        let sizeUnitsIdx = Math.Floor(Math.Log(bytesF, 1024.0))
        let num = Math.Round(bytesF / Math.Pow(1024.0, sizeUnitsIdx), 0)
        (num, sizeUnits.[int (sizeUnitsIdx)])

let printFormatted (path: string) =
    let name = entries.[path].path
    let newSize, sizeUnit = getSizeString entries.[path].size
    printfn "%10.0f %-1s %s" newSize sizeUnit name

let sizyMain path =
    let ls = Dir.EnumerateFileSystemEntries path
    let sizes = PSeq.map getSize ls
    let totSize, sizeUnit = getSizeString (PSeq.sum sizes)
    PSeq.filter (fun x -> entries.ContainsKey x && entries.[x].isDir) ls |> PSeq.sort |> Seq.iter printFormatted
    PSeq.filter (fun x -> entries.ContainsKey x && not entries.[x].isDir) ls |> PSeq.sort |> Seq.iter printFormatted
    printfn "%s\n%10.0f %-1s" (String.replicate 12 "-") totSize sizeUnit

[<EntryPoint>]
let main argv =
    match Config.getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains InputPath then config.GetResult InputPath else Dir.GetCurrentDirectory()
        let stopWatch = Diagnostics.Stopwatch.StartNew()
        sizyMain path
        eprintfn "Exec time: %f" stopWatch.Elapsed.TotalMilliseconds
        Seq.iter (fun x -> eprintfn "\n\t%s - %s" x errors.[x]) errors.Keys
        0
    | ReturnVal ret ->
        ret
