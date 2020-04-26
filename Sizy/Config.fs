module Sizy.Config

open System
open Argu

let VERSION = Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
let PROGRAM_NAME = "sizy"


type Args =
    | [<NoAppSettings>] Version
    | [<MainCommand>] Input of path: string
    | [<AltCommandLine("-p"); NoAppSettings>] Print_Only
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version _ -> sprintf "print %s version." PROGRAM_NAME
            | Input _ -> "the folder you want to analyse (default: current folder)."
            | Print_Only _ -> "output the results to screen and exit."

type ConfigOrInt =
    | Config of Argu.ParseResults<Args>
    | ReturnVal of int

let printVersion() = printfn "%s version %s" PROGRAM_NAME VERSION

let getConfiguration argv =
    let parser = ArgumentParser.Create<Args>(programName = PROGRAM_NAME, errorHandler = ProcessExiter())
    let config = parser.Parse(argv, ignoreMissing = true)

    if config.Contains Version then
        printVersion()
        ReturnVal 0
    else
        Config config
