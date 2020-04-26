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

let mutable dirStack: string list = []
let mutable lstData: string [] = [||]
let mutable totSizeStr = ""
let fsEntries = ConcurrentDictionary<string, Entry>()
let errors = ConcurrentDictionary<string, string>()

let getEntries ls (fsEntries: ConcurrentDictionary<string, Entry>) =
    let getStrSeq f =
        Seq.filter f ls
        |> Seq.sort
        |> Seq.map (fun p -> sprintf "%s" (getSizeString fsEntries.[p].Name fsEntries.[p].Size))

    let fltrFolders = fun x -> fsEntries.ContainsKey x && fsEntries.[x].IsDir
    let fltrFiles = fun x -> fsEntries.ContainsKey x && not fsEntries.[x].IsDir
    Seq.append (getStrSeq fltrFolders) (getStrSeq fltrFiles) |> Seq.toArray

let getTotalSizeStr totSize =
    let totSize, totSizeUnit = getSizeUnit totSize
    sprintf "Tot. %10.0f%s" totSize totSizeUnit

let updateData path entries =
    let ls = Directory.EnumerateFileSystemEntries path
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
            if config.Contains InputPath then config.GetResult InputPath else Directory.GetCurrentDirectory()

        if config.Contains Print_Only then
            let stopWatch = Diagnostics.Stopwatch.StartNew()
            let (ls, fsEntries, totSize, errors) = sizyMain (path)

            let print f =
                PSeq.filter f ls
                |> PSeq.sort
                |> Seq.iter (fun p -> printf "%s\n" (getSizeString fsEntries.[p].Name fsEntries.[p].Size))
            print (fun x -> fsEntries.ContainsKey x && fsEntries.[x].IsDir)
            print (fun x -> fsEntries.ContainsKey x && not fsEntries.[x].IsDir)

            let totSize, totSizeUnit = getSizeUnit totSize
            printfn "%s\n%10.0f %-1s" (String.replicate 12 "-") totSize totSizeUnit

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
