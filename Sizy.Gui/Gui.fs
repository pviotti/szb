module Sizy.Gui

open Terminal.Gui
open NStack

open System.IO
open Sizy.Main

let ustr (x:string) = ustring.Make(x)

let win = { new Window(ustr "Sizy", X=Pos.op_Implicit(0), Y=Pos.op_Implicit(0), Width=Dim.Fill(), Height=Dim.Fill()) with
    override u.ProcessKey(k : KeyEvent) =
        if k.KeyValue = int 'q' then
            Application.Top.Running <- false
            true
        else base.ProcessKey k
}

let getEntries() =
    let ls, fsEntries , _ , _ = sizyMain(Directory.GetCurrentDirectory())
    let getStrSeq f =
            Seq.filter f ls
            |> Seq.sort
            |> Seq.map (fun p -> sprintf "%s" (getSizeString fsEntries.[p].Name fsEntries.[p].Size))
    let fltrFolders = fun x -> fsEntries.ContainsKey x && fsEntries.[x].IsDir
    let fltrFiles = fun x -> fsEntries.ContainsKey x && not fsEntries.[x].IsDir
    Seq.append (getStrSeq fltrFolders) (getStrSeq fltrFiles)
    |> Seq.toArray 


[<EntryPoint>]
let main argv =
    Application.Init ()
    let top = Application.Top
    win.Add(ListView (Rect (0, 0, 64, 30), getEntries()))
    top.Add (win)
    Application.Run ()
    0 