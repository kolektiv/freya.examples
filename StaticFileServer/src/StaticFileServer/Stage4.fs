module StaticFileServer.Stage4

open System.IO
open Arachne.Http
open Freya.Core
open Freya.Core.Operators
open Freya.Lenses.Http
open Freya.Machine
open Freya.Machine.Extensions.Http

let getFileInfo (path: string) =
    let filePath = Path.Combine (root.FullName, path.Trim ([| '/' |]))
    let fileInfo = FileInfo (filePath)

    fileInfo

let readFile (file: FileInfo) =
    File.ReadAllBytes (file.FullName)

// Response

let fileTypes =
    [ ".jpg", MediaType.Parse "image/jpeg"
      ".png", MediaType.Parse "image/png"
      ".json", MediaType.Json
      ".txt",  MediaType.Text ] |> Map.ofList

let represent (n: Specification) x =
    { Description =
        { Charset = None
          Encodings = None
          MediaType = Some ((function | Negotiated x -> List.head x 
                                      | _ -> MediaType.Text) n.MediaTypes)
          Languages= None }
      Data = x }

// Freya

let path =
    Freya.memo (Freya.getLens Request.Path_)

let fileInfo =
    Freya.memo (getFileInfo <!> path)

let file =
    Freya.memo (readFile <!> fileInfo)

let fileType =
    Freya.memo ((function | (x: FileInfo) when x.Exists -> [ Map.find x.Extension fileTypes ]
                          | _ -> [ MediaType.Text ]) <!> fileInfo)

// Machine

let existsDecision =
    (fun (x: FileInfo) -> x.Exists) <!> fileInfo

let fileHandler n =
    represent n <!> file

let mediaTypesConfiguration =
    fileType

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults
        mediaTypesSupported mediaTypesConfiguration
        exists existsDecision
        handleOk fileHandler } |> FreyaMachine.toPipeline
