[<AutoOpen>]
module StaticFileServer.Prelude

open System.IO
open Arachne.Http
open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

let root =
    let dir = DirectoryInfo (__SOURCE_DIRECTORY__)
    DirectoryInfo(Path.Combine(dir.Parent.Parent.FullName, "root"))

// Defaults

let defaults =
    freyaMachine {
        using http
        methodsSupported (Freya.init [ GET; HEAD ]) }
