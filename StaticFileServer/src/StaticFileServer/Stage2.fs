module StaticFileServer.Stage2

open System.IO
open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Pipeline
open Freya.Types.Http
open Freya.Machine.Extensions.Http

let getFileInfo (path: string) =
    let filePath = Path.Combine (root.FullName, path.Trim ([| '/' |]))
    let fileInfo = FileInfo (filePath)

    fileInfo

let readFile (file: FileInfo) =
    File.ReadAllBytes (file.FullName)

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults } |> FreyaMachine.toPipeline