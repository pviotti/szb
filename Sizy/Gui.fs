module Sizy.Gui

open Terminal.Gui
open NStack

open System
open System.IO
open Sizy.Filesystem
open Sizy.Config
open System.IO.Abstractions
open System.Collections.Concurrent
open FSharp.Collections.ParallelSeq

let ustr (x: string) = ustring.Make(x)

#region "Data and related functions"

// XXX Mutable shared state
let mutable dirStack: string list = []
let mutable lstData: string [] = [||]
let mutable totSizeStr = ""
let mutable ls: string seq = Seq.empty<string>
let fsEntries = ConcurrentDictionary<string, Entry>()
let errors = ConcurrentDictionary<string, string>()

let getSizeStr name size =
    let newSize, sizeUnit = getSizeUnit size
    sprintf "%10.0f %-1s %s" newSize sizeUnit name

let getTotalSizeStr totSize =
    let totSize, totSizeUnit = getSizeUnit totSize
    sprintf "Tot. %5.0f %s" totSize totSizeUnit

let filterSortEntries ls filterFun = PSeq.filter filterFun ls |> PSeq.sort

let fltrFoldersFun = fun x -> fsEntries.ContainsKey x && fsEntries.[x].IsDir
let fltrFilesFun = fun x -> fsEntries.ContainsKey x && not fsEntries.[x].IsDir

let getEntries ls (fsEntries: ConcurrentDictionary<string, Entry>) =
    let createStrFun = fun p -> sprintf "%s" (getSizeStr fsEntries.[p].Name fsEntries.[p].Size)
    let foldersSeq = filterSortEntries ls fltrFoldersFun |> PSeq.map createStrFun
    let filesSeq = filterSortEntries ls fltrFilesFun |> PSeq.map createStrFun
    PSeq.append foldersSeq filesSeq  |> PSeq.toArray

let updateData path entries =
    ls <- Directory.EnumerateFileSystemEntries path
    let sizes = PSeq.map (getSize (FileSystem()) entries errors) ls
    totSizeStr <- getTotalSizeStr (PSeq.sum sizes)
    lstData <- getEntries ls entries

#endregion

#region "UI components"

let window =
    { new Window(ustr "Sizy", X = Pos.op_Implicit (0), Y = Pos.op_Implicit (0), Width = Dim.Fill(), Height = Dim.Fill()) with
        member __.ProcessKey(k: KeyEvent) =
            if k.KeyValue = int 'q' then
                Application.Top.Running <- false
                true
            else
                base.ProcessKey k }

let lblTotSize = Label(ustr "", X = Pos.At(0), Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = Dim.Sized(1))

let lstView =
    { new ListView([||], X = Pos.At(0), Y = Pos.At(0), Width = Dim.Percent(50.0f), Height = Dim.Fill(1)) with
        member u.ProcessKey(k: KeyEvent) =
            let entryName = lstData.[u.SelectedItem].Substring(13)
            if (k.Key = Key.Enter || k.Key = Key.CursorRight) && entryName.EndsWith "/" then
                let newDir = List.head (dirStack) + "/" + entryName.TrimEnd('/')
                dirStack <- newDir :: dirStack
                updateData newDir fsEntries
                Application.MainLoop.Invoke(fun () ->
                    u.SetSource lstData
                    lblTotSize.Text <- ustr totSizeStr)
                true
            elif (k.KeyValue = int 'b' || k.Key = Key.CursorLeft) && List.length dirStack > 1 then
                dirStack <-
                    match dirStack with
                    | _ :: tl -> tl
                    | [] -> []
                updateData (List.head dirStack) fsEntries
                Application.MainLoop.Invoke(fun () ->
                    u.SetSource lstData
                    lblTotSize.Text <- ustr totSizeStr)
                true
            else
                base.ProcessKey k }

#endregion

[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains Input then config.GetResult Input else Directory.GetCurrentDirectory()

        if config.Contains Print_Only then
            let stopWatch = Diagnostics.Stopwatch.StartNew()

            updateData path fsEntries
            let printFun = fun p -> printf "%s\n" (getSizeStr fsEntries.[p].Name fsEntries.[p].Size)
            filterSortEntries ls fltrFoldersFun |> Seq.iter printFun
            filterSortEntries ls fltrFilesFun |> Seq.iter printFun
            printfn "%s\n%s" (String.replicate 12 "-") totSizeStr

            Seq.iter (fun x ->
                eprintfn "\n\t%s - %s" x errors.[x]) errors.Keys
            eprintfn "Exec time: %f" stopWatch.Elapsed.TotalMilliseconds
        else
            Application.Init()

            dirStack <- [ path ]
            updateData path fsEntries
            Application.MainLoop.Invoke(fun () ->
                lstView.SetSource lstData
                lblTotSize.Text <- ustr totSizeStr)

            window.Add(lstView)
            window.Add(lblTotSize)
            Application.Top.Add(window)
            Application.Run()
        0
    | ReturnVal ret -> ret
