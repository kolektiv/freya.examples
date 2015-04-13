module StaticFileServer.Stage5

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
    Freya.memo (Freya.getLens Request.path)

let fileInfo =
    Freya.memo (getFileInfo <!> path)

let file =
    Freya.memo (readFile <!> fileInfo)

let fileType =
    Freya.memo ((function | (x: FileInfo) when x.Exists -> [ Map.find x.Extension fileTypes ]
                          | _ -> []) <!> fileInfo)

// Machine

let existsDecision =
    (fun (x: FileInfo) -> x.Exists) <!> fileInfo

let fileHandler n =
    represent n <!> file

let lastModifiedConfiguration =
    (fun (x: FileInfo) -> x.LastWriteTimeUtc) <!> fileInfo

let mediaTypesConfiguration =
    fileType

// Defaults

let defaults =
    freyaMachine {
        using http
        methodsSupported (Freya.init [ GET ]) }

// Resources

let files : FreyaPipeline =
    freyaMachine {
        including defaults
        mediaTypesSupported mediaTypesConfiguration
        //lastModified lastModifiedConfiguration
        exists existsDecision
        handleOk fileHandler } |> FreyaMachine.toPipeline
