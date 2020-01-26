open System
open System.IO

let enumAllEntries dir =
    let enOpt = new EnumerationOptions()
    enOpt.RecurseSubdirectories <- true
    Directory.GetFileSystemEntries(dir, "*", enOpt)

let getFileSize filePath =
    (new FileInfo(filePath)).Length

let getDirectorySize dirPath =
    enumAllEntries dirPath |>
                Seq.map getFileSize |>
                Seq.sum

let lsFiles dir = Directory.EnumerateFiles dir
let lsDirs dir = Directory.EnumerateDirectories dir

let getSize path =
    try
        let attr = File.GetAttributes path
        if attr.HasFlag FileAttributes.Directory then
            getDirectorySize path
        else
            getFileSize path
    with
    | :? Exception as ex ->
        printfn "Error: %s" ex.Message
        0L

let printFormatted (path:string, size:int64) =
    let name = Array.last (path.Split '/')
    printfn "%10iB  %s" size name


[<EntryPoint>]
let main argv =
    let path = if argv.Length > 0 then argv.[0] else Directory.GetCurrentDirectory()
    let lsF = lsFiles path
    let lsD = lsDirs path
    let sizeF = Seq.map getSize lsF
    let sizeD = Seq.map getSize lsD
    let sizeTot = Seq.append sizeF sizeD |> Seq.sum
    let print ls sizes =
            Seq.zip ls sizes |>
            Seq.iter printFormatted
    print lsD sizeD
    print lsF sizeF
    printfn "-----------\n%10iB" sizeTot
    0
