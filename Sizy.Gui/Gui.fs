module Sizy.Gui

open Terminal.Gui
open NStack

open System.IO
open Sizy.Main
open Sizy.Config
open System.Collections.Concurrent

let ustr (x: string) = ustring.Make(x)

#region "Data and related functions"
let mutable pwd = ""
let mutable lstData : string [] = [||]
let mutable totSizeStr = ""

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

let updateData path =
    let ls, entries, totSize, _ = sizyMain path
    lstData <- getEntries ls entries
    totSizeStr <- getTotalSizeStr totSize
#endregion 

#region "UI controls"
let win =
    { new Window(ustr "Sizy", X = Pos.op_Implicit (0), Y = Pos.op_Implicit (0), Width = Dim.Fill(), Height = Dim.Fill()) with
        member u.ProcessKey(k: KeyEvent) =
            if k.KeyValue = int 'q' then
                Application.Top.Running <- false
                true
            else
                base.ProcessKey k }

let lblTotSize = Label (ustr "", X = Pos.At(0), Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = Dim.Sized(1))
let lstView = { new ListView([||], X = Pos.At(0), Y = Pos.At(0), Width = Dim.Percent(50.0f), Height = Dim.Fill(1)) with
            member u.ProcessKey(k: KeyEvent) =
                let entryName = lstData.[u.SelectedItem].Substring(13)
                if k.Key = Key.Enter && entryName.EndsWith "/" then
                    pwd <- pwd + "/" + entryName.TrimEnd('/')
                    updateData pwd
                    Application.MainLoop.Invoke(fun () ->
                            u.SetSource lstData
                            lblTotSize.Text <- ustr totSizeStr
                        )
                    true
                else
                    base.ProcessKey k }
#endregion

[<EntryPoint>]
let main argv =
    match getConfiguration argv with
    | Config config ->
        pwd <- if config.Contains InputPath then config.GetResult InputPath else Directory.GetCurrentDirectory()
        Application.Init()

        updateData pwd
        Application.MainLoop.Invoke(fun () ->
                lstView.SetSource lstData
                lblTotSize.Text <- ustr totSizeStr
            )

        win.Add(lstView)
        win.Add(lblTotSize)
        Application.Top.Add(win)
        Application.Run()
        0
    | ReturnVal ret -> ret
