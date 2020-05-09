module Sizy.Gui

open Terminal.Gui
open NStack

open Sizy.FileSystem
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


type TuiStateEntry =
    { CurrPath: string
      LstData: string []
      TotSizeStr: string
      Ls: string seq }

let fs = FsManager(FileSystem())

type StateManager() =
    (* Mutable state:
        - a stack (list) of TuiStateEntry containing the data to visualise in the TUI for each browsed path
        - a hashmap of path -> Entry holding the file system data for all file system entries *)
    let mutable state: TuiStateEntry list = []
    let fsEntries = ConcurrentDictionary<string, Entry>()

    let getTotalSizeStr totSize =
        let totSize, totSizeUnit = FsManager.GetSizeUnit totSize
        sprintf "Tot. %5.0f %s" totSize totSizeUnit

    let getPath state idx =
        let name = state.LstData.[idx].Substring(13)
        state.CurrPath + string fs.DirSeparator + name.TrimEnd(fs.DirSeparator)

    member _.GetListViewEntries(ls: seq<string>) =

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

        let valueEntryToString (KeyValue(_: string, value: Entry)) = FsManager.GetEntryString value

        let getOrderedEntries filterFunction =
            fsEntries
            |> PSeq.filter filterFunction
            |> PSeq.sortBy sortBySize
            |> PSeq.map valueEntryToString
            |> PSeq.toArray

        Array.append (getOrderedEntries filterDirsInLs) (getOrderedEntries filterFilesInLs)

    member _.CurrentState = List.head state

    member _.Length = List.length state

    member _.RemoveCurrentState() = state <- state.Tail

    member this.IsSelectedItemDir selectedItemIdx =
        let currState = this.CurrentState
        let path = getPath currState selectedItemIdx
        FsManager.IsDir fsEntries path

    member this.CreateState path =
        let ls = fs.List path
        let sizes = PSeq.map (fs.GetSize fsEntries) ls
        let totSizeStr = getTotalSizeStr (PSeq.sum sizes)
        let lstData = this.GetListViewEntries ls
        { CurrPath = path
          LstData = lstData
          TotSizeStr = totSizeStr
          Ls = ls }

    member this.AddNewState(path: string) =
        let newState = this.CreateState path
        state <- newState :: state

    member this.AddNewState(selectedItemIdx: int) =
        let currState = this.CurrentState
        let path = getPath currState selectedItemIdx
        this.AddNewState path

    member this.DeleteEntry selectedItemIdx =
        let currState = this.CurrentState
        let pathToDelete = getPath currState selectedItemIdx
        fs.Delete fsEntries pathToDelete

        (* Delete current path, deleted entry and its ancestor paths
            from fsEntry dictionary so that their sizes are recomputed
            in fs.GetSize *)
        let updateState oldStateEntry: TuiStateEntry =
            fsEntries.TryRemove oldStateEntry.CurrPath |> ignore
            this.CreateState oldStateEntry.CurrPath

        fsEntries.TryRemove pathToDelete |> ignore
        fsEntries.TryRemove currState.CurrPath |> ignore
        let newTail = List.tail state |> List.map updateState

        let newState = this.CreateState currState.CurrPath
        state <- newState :: newTail


type Tui(state: StateManager) =
    let state = state

    let window =
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

    let lblPath = Label(ustr "", X = Pos.At(0), Y = Pos.At(0), Width = Dim.Fill(), Height = Dim.Sized(1))

    let lblTotSize =
        Label(ustr "Tot.", X = Pos.At(0), Y = Pos.AnchorEnd(1), Width = Dim.Percent(10.0f), Height = Dim.Sized(1))

    let lblError =
        Label(ustr "", X = Pos.Percent(20.0f), Y = Pos.AnchorEnd(1), Width = Dim.Percent(80.0f), Height = Dim.Sized(1))

    let lstView =
        { new ListView([||], X = Pos.At(0), Y = Pos.At(2), Width = Dim.Percent(50.0f), Height = Dim.Fill(1)) with
            member this.ProcessKey(k: KeyEvent) =

                // TODO this function is duplicate and shouldn't be here
                let updateViews() =
                    Application.MainLoop.Invoke(fun () ->
                        this.SetSource state.CurrentState.LstData
                        lblPath.Text <- ustr state.CurrentState.CurrPath
                        lblTotSize.Text <- ustr state.CurrentState.TotSizeStr)

                let keyChar: char = char k.KeyValue
                try
                    match k.Key, keyChar with
                    | Key.CursorRight, _
                    | _, 'l' when this.Source.Count <> 0 ->
                        if state.IsSelectedItemDir this.SelectedItem then
                            state.AddNewState this.SelectedItem
                            updateViews()
                        true
                    | Key.CursorLeft, _
                    | _, 'h' ->
                        if state.Length > 1 then
                            state.RemoveCurrentState()
                            updateViews()
                        true
                    | Key.DeleteChar, _
                    | _, 'd' when this.Source.Count <> 0 ->
                        if 0 = MessageBox.Query(50, 7, "Delete", "Are you sure you want to delete this?", "Yes", "No") then
                            state.DeleteEntry this.SelectedItem
                            Application.MainLoop.Invoke(fun () ->
                                let prevSelectedItem = this.SelectedItem
                                this.SetSource state.CurrentState.LstData
                                if this.Source.Count <> 0 && prevSelectedItem > 0 then
                                    this.SelectedItem <- prevSelectedItem - 1
                                lblPath.Text <- ustr state.CurrentState.CurrPath
                                lblTotSize.Text <- ustr state.CurrentState.TotSizeStr)
                        true
                    | _, 'j' -> this.MoveDown()
                    | _, 'k' -> this.MoveUp()
                    | _, _ -> base.ProcessKey k
                with ex ->
                    lblError.Text <- ustr ("Error: " + ex.Message)
                    true }

    do
        Application.Init()

        Application.MainLoop.Invoke(fun () ->
            lstView.SetSource state.CurrentState.LstData
            lblPath.Text <- ustr state.CurrentState.CurrPath
            lblTotSize.Text <- ustr state.CurrentState.TotSizeStr)

        lblError.TextColor <- Application.Driver.MakeAttribute(Color.Red, Color.Blue)
        window.Add(lblPath)
        window.Add(lstView)
        window.Add(lblTotSize)
        window.Add(lblError)
        Application.Top.Add(window)
        Application.Run()


[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains Input then config.GetResult Input else fs.GetCurrDir()

        let state = StateManager()
        try
            state.AddNewState path
            if config.Contains Print_Only then
                state.GetListViewEntries state.CurrentState.Ls
                |> Array.iter (fun x ->
                    printf "%s\n" x)
                printfn "%s\n%s" (String.replicate 12 "-") state.CurrentState.TotSizeStr
            else
                Tui(state) |> ignore
            0
        with ex -> 
            eprintfn "Error: %s" ex.Message
            1
    | ReturnVal ret -> ret
