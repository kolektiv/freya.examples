module StaticFileServer.Program

open Freya.Core
open Microsoft.Owin.Hosting

// App

let app =
    Stage5.files

// Katana

type FileServer () =
    member __.Configuration () =
        OwinAppFunc.ofFreya app

// Main

[<EntryPoint>]
let main _ = 
    let _ = WebApp.Start<FileServer> ("http://localhost:7000")
    printfn "Listening on port 7000 and looking in %s" Prelude.root.FullName
    let _ = System.Console.ReadLine ()
    0