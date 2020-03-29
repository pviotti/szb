//namespace Sizy.Benchmarks

open BenchmarkDotNet.Running
open BenchmarkDotNet.Attributes

open Sizy

type Benchmarks () =
    [<ParamsSource("NBytesValues")>]
    member val public NBytes = 0L with get, set

    member val public NBytesValues = seq {0L .. 10010L .. 100000L}

    [<Benchmark>]
    member this.GetSizeUnit () = Program.getSizeUnit(this.NBytes) |> ignore

    [<Benchmark>]
    member this.GetSizeUnit2 () = Program.getSizeUnit2(this.NBytes) |> ignore

    
[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Benchmarks>() |> ignore
    0
    