module StaticFileServer.Program

open Freya.Core
open Freya.Inspector
open Freya.Machine.Inspector
open Freya.Pipeline.Operators
open Microsoft.Owin.Hosting

// App

let config =
    { Inspectors = 
        [ freyaRequestInspector
          freyaMachineInspector ] }

let app =
    freyaInspector config >?= Stage4.files

// Katana

type FileServer () =
    member __.Configuration () =
        OwinAppFunc.ofFreya (app)

// Main
[<EntryPoint>]
let main _ = 
    let _ = WebApp.Start<FileServer> ("http://localhost:7000")
    printfn "Listening on port 7000 and looking in %s" Prelude.root.FullName
    let _ = System.Console.ReadLine ()
    0