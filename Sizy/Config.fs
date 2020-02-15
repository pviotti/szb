module Sizy.Config

open System
open Argu

let VERSION = Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
let PROGRAM_NAME = "sizy"


type Args =
    | [<NoAppSettings>] Version
    | [<AltCommandLine("-p")>] Percentage
    | [<MainCommand>] InputPath of path: string
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version _ -> sprintf "print %s version." PROGRAM_NAME
            | Percentage _ -> "show percentage values"
            | InputPath _ -> "the folder you want to analise"

type ConfigOrInt =
    | Config of Argu.ParseResults<Args>
    | ReturnVal of int

let printVersion() = printfn "%s version %s" PROGRAM_NAME VERSION


let getConfiguration argv =
    let errorHandler =
        ProcessExiter
            (colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red)

    let parser = ArgumentParser.Create<Args>(programName = PROGRAM_NAME, errorHandler = errorHandler)
    let config = parser.Parse(argv, ignoreMissing = true)

    if config.Contains Version then
        printVersion()
        ReturnVal 0
    else
        Config config
