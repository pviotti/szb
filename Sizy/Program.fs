open System
open System.IO
open FSharp.Collections.ParallelSeq

type Dir = Directory
let sizeUnits = [ "B"; "KiB"; "MiB"; "GiB"; "TiB"; "PiB"; "EiB" ]

let rec getSize path =
    try
        let attr = File.GetAttributes path
        if attr.HasFlag FileAttributes.Directory then
            Dir.EnumerateFileSystemEntries path
            |> Seq.sumBy getSize
        else
            FileInfo(path).Length
    with ex ->
        eprintfn "Error: %s" ex.Message
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

[<EntryPoint>]
let main argv =
    let path =
        if argv.Length > 0 then argv.[0] else Dir.GetCurrentDirectory()
    let stopWatch = Diagnostics.Stopwatch.StartNew()
    sizyMain path
    printfn "%f" stopWatch.Elapsed.TotalMilliseconds
    0
