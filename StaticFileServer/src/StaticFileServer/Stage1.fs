module StaticFileServer.Stage1

open Freya.Core
open Freya.Machine

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults } |> FreyaMachine.toPipeline