module StaticFileServer.Stage1

open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Pipeline
open Freya.Types.Http
open Freya.Machine.Extensions.Http

// Defaults

let defaults =
    freyaMachine {
        using http
        methodsSupported (Freya.init [ GET ]) }

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults } |> FreyaMachine.toPipeline