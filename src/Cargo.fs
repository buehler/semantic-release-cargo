module SemanticReleaseCargo.Cargo

open Fable.Core
open Fable.Core.JsInterop
open Node.Base

let cargoExecutable =
    match Node.Api.``process``.platform with
    | Platform.Win32 -> "cargo.exe"
    | _ -> "cargo"

type private ExecaResult =
    { stdout: string
      stderr: string
      exitCode: int }

let private execa (_: string, _: string array) : JS.Promise<ExecaResult> = import "execa" "execa"

let exec (args: string array) =
    async {
        let! { stdout = o
               stderr = e
               exitCode = exit } = execa (cargoExecutable, args) |> Async.AwaitPromise

        return (o, e, exit)
    }
