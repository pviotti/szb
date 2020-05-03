module Sizy.Gui

open Terminal.Gui
open NStack

open System
open Sizy.Filesystem
open Sizy.Config
open System.IO.Abstractions
open System.Collections.Concurrent
open FSharp.Collections.ParallelSeq

let ustr (x: string) = ustring.Make(x)

let helpMsg = "\n- return or → or l:   browse into a directory\n\
                 - b or ← or h:        browse into the parent directory\n\
                 - j or ↓:             move down the list\n\
                 - k or ↑:             move up the list\n\
                 - d or delete:        delete file or directory (requires confirmation)\n\
                 - q:                  exit\n\
                 - ?:                  show this help message."

#region "Data and related functions"

type GuiStateEntry = {CurrPath:string; LstData: string []; TotSizeStr: string; Ls: string seq }
type GuiState = GuiStateEntry list

// Mutable state:
// - a stack (list) of GuiStateEntry
// - a hashmap of path -> Entry
let mutable guiState : GuiState = []
let fsEntries = ConcurrentDictionary<string, Entry>()

let fs = FsController(FileSystem())

let getTotalSizeStr totSize =
    let totSize, totSizeUnit = FsController.GetSizeUnit totSize
    sprintf "Tot. %5.0f %s" totSize totSizeUnit

let filterSortEntries ls filterFun = PSeq.filter filterFun ls |> PSeq.sort

let getEntries ls (fsEntries: ConcurrentDictionary<string, Entry>) =
    let createStrFun = fun p -> sprintf "%s" (FsController.GetEntryString fsEntries.[p])
    let foldersSeq = filterSortEntries ls (FsController.IsFolder fsEntries) |> PSeq.map createStrFun
    let filesSeq = filterSortEntries ls (FsController.IsFile fsEntries) |> PSeq.map createStrFun
    PSeq.append foldersSeq filesSeq |> PSeq.toArray

let addGuiState path entries guiStateTail =
    let ls = fs.List path
    let sizes = PSeq.map (fs.GetSize entries) ls
    let totSizeStr = getTotalSizeStr (PSeq.sum sizes)
    let lstData = getEntries ls entries
    guiState <- {CurrPath=path; LstData=lstData; TotSizeStr=totSizeStr; Ls=ls} :: guiStateTail

let updateCurrentState path entries =
    // TODO update size count of all ancestor directories
    addGuiState path entries (List.tail guiState)

#endregion

#region "UI components"

module Gui =

    let Window =
        { new Window(ustr PROGRAM_NAME, X = Pos.op_Implicit (0), Y = Pos.op_Implicit (0), Width = Dim.Fill(),
                     Height = Dim.Fill()) with
            member __.ProcessKey(k: KeyEvent) =
                if k.KeyValue = int 'q' then
                    Application.Top.Running <- false
                    true
                elif k.KeyValue = int '?' then
                    MessageBox.Query(76, 14, "Help", helpMsg, "OK") |> ignore
                    true
                else
                    base.ProcessKey k }

    let LblPath = Label(ustr "", X = Pos.At(0), Y = Pos.At(0), Width = Dim.Fill(), Height = Dim.Sized(1))

    let LblTotSize = Label(ustr "", X = Pos.At(0), Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = Dim.Sized(1))

    let LstView =
        { new ListView([||], X = Pos.At(0), Y = Pos.At(2), Width = Dim.Percent(50.0f), Height = Dim.Fill(1)) with
            member this.ProcessKey(k: KeyEvent) =

                let updateViews() =
                    Application.MainLoop.Invoke(fun () ->
                        let currState = List.head(guiState)
                        this.SetSource currState.LstData
                        LblPath.Text <- ustr currState.CurrPath
                        LblTotSize.Text <- ustr currState.TotSizeStr)

                let currState = List.head(guiState)
                let entryName = currState.LstData.[this.SelectedItem].Substring(13) // TODO fix IndexOutOfRangeException when list is empty
                if (k.Key = Key.Enter || k.Key = Key.CursorRight || k.KeyValue = int 'l')
                   && entryName.EndsWith fs.DirectorySeparator then
                    let newDir =
                        currState.CurrPath + string fs.DirectorySeparator
                        + entryName.TrimEnd(fs.DirectorySeparator)
                    addGuiState newDir fsEntries guiState
                    updateViews()
                    true
                elif (k.KeyValue = int 'b' || k.Key = Key.CursorLeft || k.KeyValue = int 'h')
                     && List.length guiState > 1 then
                    guiState <- List.tail guiState
                    updateViews()
                    true
                elif k.KeyValue = int 'd' || k.Key = Key.DeleteChar then
                    if 0 = MessageBox.Query(50, 7, "Delete", "Are you sure you want to delete this?", "Yes", "No") then
                        let entryToDelete =
                            currState.CurrPath + string fs.DirectorySeparator
                            + entryName.TrimEnd(fs.DirectorySeparator)
                        fs.Delete entryToDelete
                        updateCurrentState currState.CurrPath fsEntries
                        updateViews()
                    true
                elif k.KeyValue = int 'j' then
                    this.MoveDown()
                elif k.KeyValue = int 'k' then
                    this.MoveUp()
                else
                    base.ProcessKey k }

#endregion

[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains Input then config.GetResult Input else fs.GetCurrentDirectory()

        addGuiState path fsEntries guiState
        if config.Contains Print_Only then
            let stopWatch = Diagnostics.Stopwatch.StartNew()

            let printFun =
                fun path ->
                    printf "%s\n" (FsController.GetEntryString fsEntries.[path])
            let state = List.head guiState
            let ls = state.Ls
            let totSizeStr = state.TotSizeStr
            filterSortEntries ls (FsController.IsFolder fsEntries) |> Seq.iter printFun
            filterSortEntries ls (FsController.IsFile fsEntries) |> Seq.iter printFun
            printfn "%s\n%s" (String.replicate 12 "-") totSizeStr

            eprintfn "Execution time: %f" stopWatch.Elapsed.TotalMilliseconds
        else
            Application.Init()

            Application.MainLoop.Invoke(fun () ->
                let currState = List.head(guiState)
                Gui.LstView.SetSource currState.LstData
                Gui.LblPath.Text <- ustr currState.CurrPath
                Gui.LblTotSize.Text <- ustr currState.TotSizeStr)

            Gui.Window.Add(Gui.LblPath)
            Gui.Window.Add(Gui.LstView)
            Gui.Window.Add(Gui.LblTotSize)
            Application.Top.Add(Gui.Window)
            Application.Run()
        0
    | ReturnVal ret -> ret
