module Sizy.Filesystem

open System
open System.IO
open System.IO.Abstractions
open System.Collections.Generic

let SizeUnits = [ "B"; "k"; "M"; "G"; "T"; "P"; "E" ]

type Entry(path: string, size: int64, isDir: bool, sep: char) =
    member _.Name =
        Array.last (path.Split sep) + if isDir then string sep else ""
    member _.Size = size
    member _.IsDir = isDir

type FsController(fs0: IFileSystem) = 
    let fs = fs0

    member this.GetSize (fsEntries: IDictionary<string, Entry>) (errors: IDictionary<_, _>) path =
        if fsEntries.ContainsKey path then
            fsEntries.[path].Size
        else
            try
                let attr = fs.File.GetAttributes path
                let size, isDir =
                    if attr.HasFlag FileAttributes.Directory
                    then fs.Directory.EnumerateFileSystemEntries path |> Seq.sumBy (this.GetSize fsEntries errors), true
                    else fs.FileInfo.FromFileName(path).Length, false
                fsEntries.[path] <- Entry(path, size, isDir, fs.Path.DirectorySeparatorChar)
                size
            with ex ->
                errors.[path] <- ex.Message
                0L

    member _.Delete(path:string) =
        if path.EndsWith fs.Path.DirectorySeparatorChar then
            fs.Directory.Delete(path, true)
        else
            fs.File.Delete(path)

    member _.List(path: string) = 
        fs.Directory.EnumerateFileSystemEntries path

    member _.DirectorySeparatorChar = fs.Path.DirectorySeparatorChar

    member _.GetCurrentDirectory = fs.Directory.GetCurrentDirectory

    static member GetSizeUnit bytes =
        if bytes <= 0L then
            0.0, SizeUnits.[0]
        elif bytes >= 0x1000000000000000L then
            Math.Round(float(bytes >>> 50) / 1024.0, 0), SizeUnits.[6]
        elif bytes >= 0x4000000000000L then
            Math.Round(float(bytes >>> 40) / 1024.0), SizeUnits.[5]
        elif bytes >= 0x10000000000L then
            Math.Round(float(bytes >>> 30) / 1024.0), SizeUnits.[4]
        elif bytes >= 0x40000000L then
            Math.Round(float(bytes >>> 20) / 1024.0), SizeUnits.[3]
        elif bytes >= 0x100000L then
            Math.Round(float(bytes >>> 10) / 1024.0), SizeUnits.[2]
        elif bytes >= 0x400L then
            Math.Round(float(bytes) / 1024.0), SizeUnits.[1]
        else
            float(bytes), SizeUnits.[0]

    static member IsFolder (fsEntries: IDictionary<string, Entry>) (path: string) = fsEntries.ContainsKey path && fsEntries.[path].IsDir
    static member IsFile (fsEntries: IDictionary<string, Entry>) (path: string) = fsEntries.ContainsKey path && not fsEntries.[path].IsDir
