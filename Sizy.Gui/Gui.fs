module Sizy.Gui

open Terminal.Gui
open NStack

open System.IO
open Sizy.Main

let ustr (x:string) = ustring.Make(x)

let win = { new Window(ustr "Sizy", X=Pos.op_Implicit(0), Y=Pos.op_Implicit(1), Width=Dim.Fill(), Height=Dim.Fill()) with
    override u.ProcessKey(k : KeyEvent) =
        if k.KeyValue = int 'q' then
            Application.Top.Running <- false
            true
        else base.ProcessKey k
}

let getArray() =
    let ls, entries , _ , _ = sizyMain(Directory.GetCurrentDirectory())
    entries.Values 
    |> Seq.map ( fun v -> v.Name + "\t" + (string v.Size))
    |> Seq.toArray 


[<EntryPoint>]
let main argv =
    Application.Init ()
    let top = Application.Top
    win.Add(ListView (Rect (1, 0, 64, 16), getArray()))
    top.Add (win)
    Application.Run ()
    0 