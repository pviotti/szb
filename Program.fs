open System
open System.IO

// Returns all files in a certain directory, recursively.
let rec allFiles dirs =
    if Seq.isEmpty dirs then Seq.empty else
        seq {   yield! dirs |> Seq.collect Directory.EnumerateFiles
                yield! dirs |> Seq.collect Directory.EnumerateDirectories |> allFiles }

let getFileSize filePath =
    (new FileInfo(filePath)).Length

let getDirectorySize dirPath =
    allFiles (Seq.singleton dirPath) |>
                Seq.map getFileSize |>
                Seq.sum

let lsFiles dir = Directory.EnumerateFiles dir
let lsDirs dir = Directory.EnumerateDirectories dir

let getSize path =
    let attr = File.GetAttributes path
    if attr.HasFlag FileAttributes.Directory then
        getDirectorySize path
    else
        getFileSize path

let printFormatted (path:string, size:int64) =
    let name = Array.last (path.Split '/')
    printfn "%10iB  %s" size name


[<EntryPoint>]
let main argv =
    let path = Directory.GetCurrentDirectory()
    let lsF = lsFiles path
    let lsD = lsDirs path
    let print entries =
        Seq.map getSize entries |>
            Seq.zip entries |>
            Seq.iter printFormatted
    print lsD
    print lsF
    0
