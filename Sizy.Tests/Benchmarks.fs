namespace Sizy.Benchmarks

open BenchmarkDotNet.Running
open BenchmarkDotNet.Attributes

open Sizy.Main

type Benchmarks () =
    [<ParamsSource("NBytesValues")>]
    member val public NBytes = 0L with get, set

    member val public NBytesValues = seq {0L .. 10010L .. 100000L}

    [<Benchmark>]
    member this.GetSizeUnit () = getSizeUnit(this.NBytes) |> ignore

module Program =
    [<EntryPoint>]
    let main argv =
        BenchmarkRunner.Run<Benchmarks>() |> ignore
        0
    