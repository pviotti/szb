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

let helpMsg = "\n- → or l:         browse into a directory\n\
                 - ← or h:         browse into the parent directory\n\
                 - ↓ or j:         move down the list\n\
                 - ↑ or k:         move up the list\n\
                 - d or delete:    delete file or directory (requires confirmation)\n\
                 - q:              exit\n\
                 - ?:              show this help message."

#region "Data and related functions"

type GuiStateEntry =
    { CurrPath: string
      LstData: string []
      TotSizeStr: string
      Ls: string seq }

type GuiState = GuiStateEntry list

// Mutable state:
// - a stack (list) of GuiStateEntry
// - a hashmap of path -> Entry
let mutable state: GuiState = []
let fsEntries = ConcurrentDictionary<string, Entry>()

let fs = FsController(FileSystem())

let getTotalSizeStr totSize =
    let totSize, totSizeUnit = FsController.GetSizeUnit totSize
    sprintf "Tot. %5.0f %s" totSize totSizeUnit

let getEntries (ls: seq<string>) (fsEntries: ConcurrentDictionary<string, Entry>) =

    let lsSet = Set.ofSeq ls

    let filterDirsInLs (KeyValue(path: string, value: Entry)) =
        Set.contains path lsSet && match value with
                                   | FsEntry fsEntry -> fsEntry.IsDir
                                   | Error _ -> ErrorIsDir

    let filterFilesInLs (KeyValue(path: string, value: Entry)) =
        Set.contains path lsSet && match value with
                                   | FsEntry fsEntry -> not fsEntry.IsDir
                                   | Error _ -> not ErrorIsDir

    let sortBySize (KeyValue(_: string, value: Entry)) =
        match value with
        | FsEntry fsEntry -> -fsEntry.Size
        | Error _ -> ErrorSize

    let valueEntryToString (KeyValue(_: string, value: Entry)) = FsController.GetEntryString value

    let orderedDirs =
        fsEntries
        |> PSeq.filter filterDirsInLs
        |> PSeq.sortBy sortBySize
        |> PSeq.map valueEntryToString
        |> PSeq.toArray

    let orderedFiles =
        fsEntries
        |> PSeq.filter filterFilesInLs
        |> PSeq.sortBy sortBySize
        |> PSeq.map valueEntryToString
        |> PSeq.toArray

    Array.append orderedDirs orderedFiles

let addState path entries stateNewTail =
    let ls = fs.List path
    let sizes = PSeq.map (fs.GetSize entries) ls
    let totSizeStr = getTotalSizeStr (PSeq.sum sizes)
    let lstData = getEntries ls entries
    state <-
        { CurrPath = path
          LstData = lstData
          TotSizeStr = totSizeStr
          Ls = ls }
        :: stateNewTail

let updateCurrentState path entries =
    // TODO update size count of all ancestor directories
    addState path entries (List.tail state)

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
                    MessageBox.Query(72, 14, "Help", helpMsg, "OK") |> ignore
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
                        let currState = List.head state
                        this.SetSource currState.LstData
                        LblPath.Text <- ustr currState.CurrPath
                        LblTotSize.Text <- ustr currState.TotSizeStr)

                let currState = List.head state
                let keyChar: char = char k.KeyValue
                match k.Key, keyChar with
                | Key.CursorRight, _
                | _, 'l' when not (Seq.isEmpty currState.LstData) ->
                    let entryName = currState.LstData.[this.SelectedItem].Substring(13)
                    if entryName.EndsWith fs.DirSeparator then
                        let newDir = currState.CurrPath + string fs.DirSeparator + entryName.TrimEnd(fs.DirSeparator)
                        addState newDir fsEntries state
                        updateViews()
                    true
                | Key.CursorLeft, _
                | _, 'h' ->
                    if List.length state > 1 then
                        state <- List.tail state
                        updateViews()
                    true
                | Key.DeleteChar, _
                | _, 'd' when not (Seq.isEmpty currState.LstData) ->
                    if 0 = MessageBox.Query(50, 7, "Delete", "Are you sure you want to delete this?", "Yes", "No") then
                        let entryName = currState.LstData.[this.SelectedItem].Substring(13)
                        let entryToDelete =
                            currState.CurrPath + string fs.DirSeparator + entryName.TrimEnd(fs.DirSeparator)
                        fs.Delete entryToDelete
                        updateCurrentState currState.CurrPath fsEntries
                        updateViews()
                    true
                | _, 'j' -> this.MoveDown()
                | _, 'k' -> this.MoveUp()
                | _, _ -> base.ProcessKey k }

#endregion

[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains Input then config.GetResult Input else fs.GetCurrDir()

        let stopWatch = Diagnostics.Stopwatch.StartNew()
        addState path fsEntries state
        if config.Contains Print_Only then
            let state = List.head state
            getEntries state.Ls fsEntries
            |> Array.iter (fun x ->
                printf "%s\n" x)
            printfn "%s\n%s" (String.replicate 12 "-") state.TotSizeStr

            stopWatch.Stop()
            eprintfn "Execution time: %fms" stopWatch.Elapsed.TotalMilliseconds
        else
            Application.Init()

            Application.MainLoop.Invoke(fun () ->
                let currState = List.head (state)
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
