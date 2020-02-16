module Tests

open System
open Xunit
open FsUnit.Xunit

module ``Sizy tests`` =

    type SizyTests () =

        [<Fact>]
        member __.``It should exit with 0`` () =
            Sizy.Program.main([||]) |> should equal 0


[<EntryPoint>]
let main argv = 0
