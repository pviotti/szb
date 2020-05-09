module Sizy.FileSystem

open System
open System.IO
open System.IO.Abstractions
open System.Collections.Generic


type Error = {Name: string; Message: string} 
type FsEntry = {Name: string; Size: int64; IsDir: bool} 
type Entry = 
    | Error of Error 
    | FsEntry of FsEntry

// Whether we represent as directories file system entries
// which we couldn't analyse because of errors
let ErrorIsDir = true 

// The size we give to file system entries we couldn't analyse
let ErrorSize = 0L   

let SizeUnits = [ "B"; "k"; "M"; "G"; "T"; "P"; "E" ]

type FsManager(fs: IFileSystem) = 
    let fs = fs

    member this.GetSize (fsEntries: IDictionary<string, Entry>) path =
        if fsEntries.ContainsKey path then
             match fsEntries.[path] with
             | FsEntry {Name=_; Size=s; IsDir=_} -> s
             | Error _ -> ErrorSize
        else
            try
                let attr = fs.File.GetAttributes path
                let size, isDir =
                    if attr.HasFlag FileAttributes.Directory
                    then fs.Directory.EnumerateFileSystemEntries path |> Seq.sumBy (this.GetSize fsEntries), true
                    else fs.FileInfo.FromFileName(path).Length, false
                fsEntries.[path] <- FsEntry {Name=this.GetEntryName path isDir; Size=size; IsDir=isDir}
                size
            with ex ->
                fsEntries.[path] <- Error {Name=path; Message=ex.Message}
                ErrorSize

    member this.GetEntryName (path:string) (isDir:bool) =
        Array.last (path.Split this.DirSeparator) + if isDir then string this.DirSeparator else ""

    member _.Delete (fsEntries: IDictionary<string, Entry>) path  =
        if FsManager.IsDir fsEntries path then
            fs.Directory.Delete(path, true)
        else
            fs.File.Delete(path)

    member _.List(path: string) = 
        fs.Directory.EnumerateFileSystemEntries path

    member _.DirSeparator : char = fs.Path.DirectorySeparatorChar

    member _.GetCurrDir = fs.Directory.GetCurrentDirectory

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

    static member GetEntryString (entry:Entry) : string=
        match entry with
        | FsEntry {Name=name; Size=size; IsDir=_} ->
            let newSize, sizeUnit = FsManager.GetSizeUnit size
            sprintf "%10.0f %-1s %s" newSize sizeUnit name
        | Error {Name=name; Message=msg} ->
            let newSize, sizeUnit = FsManager.GetSizeUnit ErrorSize
            sprintf "%10.0f %-1s %s \tError: %s" newSize sizeUnit name msg

    static member IsDir (fsEntries: IDictionary<string, Entry>) (path: string) =
        fsEntries.ContainsKey path && 
            match fsEntries.[path] with
            | FsEntry {Name=_; Size=_; IsDir=isDir} -> isDir
            | Error _ -> ErrorIsDir

    static member IsFile (fsEntries: IDictionary<string, Entry>) (path: string) =
        fsEntries.ContainsKey path && 
            match fsEntries.[path] with
            | FsEntry {Name=_; Size=_; IsDir=isDir} -> not isDir
            | Error _ -> not ErrorIsDir
