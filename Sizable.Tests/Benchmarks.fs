namespace Sizable.Benchmarks

open BenchmarkDotNet.Running
open BenchmarkDotNet.Attributes

open Sizable.FileSystem

type Benchmarks () =
    [<ParamsSource("NBytesValues")>]
    member val public NBytes = 0L with get, set

    member val public NBytesValues = seq {0L .. 10010L .. 100000L}

    [<Benchmark>]
    member this.GetSizeUnit () = FsManager.GetSizeUnit(this.NBytes) |> ignore

module Program =
    [<EntryPoint>]
    let main _ =
        BenchmarkRunner.Run<Benchmarks>() |> ignore
        0

