module Sizy.Filesystem

open System
open System.IO
open System.IO.Abstractions
open System.Collections.Generic
open FSharp.Collections.ParallelSeq

let SizeUnits = [ "B"; "k"; "M"; "G"; "T"; "P"; "E" ]

type Entry(path: string, size: int64, isDir: bool, sep: char) =
    member __.Name =
        Array.last (path.Split sep) + if isDir then string sep else ""
    member __.Size = size
    member __.IsDir = isDir

let rec getSize (fs: IFileSystem) (fsEntries: IDictionary<string, Entry>) (errors: IDictionary<_, _>) path =
    if fsEntries.ContainsKey path then
        fsEntries.[path].Size
    else
        try
            let attr = fs.File.GetAttributes path
            let size, isDir =
                if attr.HasFlag FileAttributes.Directory
                then fs.Directory.EnumerateFileSystemEntries path |> Seq.sumBy (getSize fs fsEntries errors), true
                else fs.FileInfo.FromFileName(path).Length, false
            fsEntries.[path] <- Entry(path, size, isDir, fs.Path.DirectorySeparatorChar)
            size
        with ex ->
            errors.[path] <- ex.Message
            0L

let getSizeUnit bytes =
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
