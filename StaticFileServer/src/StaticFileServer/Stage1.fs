module StaticFileServer.Stage1

open Arachne.Http
open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults } |> FreyaMachine.toPipeline