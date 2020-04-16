module Sizy.Gui

open Terminal.Gui
open NStack

open System.IO
open Sizy.Main
open Sizy.Config
open System.Collections.Concurrent

let ustr (x: string) = ustring.Make(x)

let win =
    { new Window(ustr "Sizy", X = Pos.op_Implicit (0), Y = Pos.op_Implicit (0), Width = Dim.Fill(), Height = Dim.Fill()) with
        member u.ProcessKey(k: KeyEvent) =
            if k.KeyValue = int 'q' then
                Application.Top.Running <- false
                true
            else
                base.ProcessKey k }

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

[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        let path =
            if config.Contains InputPath then config.GetResult InputPath else Directory.GetCurrentDirectory()
        Application.Init()
        let ls, entries, totSize, _ = sizyMain (path)
        let top = Application.Top
        let data = getEntries ls entries
        let lstOne = ListView(data, X = Pos.At(0), Y = Pos.At(0), Width = Dim.Percent(50.0f), Height = Dim.Fill(1))
        //let lstTwo =ListView (getEntries(), X=Pos.At(0), Y=Pos.At(0), Width=Dim.Percent(50.0f), Height=Dim.Fill(1))
        let lblTotSize =
            Label
                (ustr (getTotalSizeStr totSize), X = Pos.At(0), Y = Pos.AnchorEnd(1), Width = Dim.Fill(),
                 Height = Dim.Sized(1))
        win.Add(lstOne)
        win.Add(lblTotSize)
        top.Add(win)
        Application.Run()
        0
    | ReturnVal ret -> ret
