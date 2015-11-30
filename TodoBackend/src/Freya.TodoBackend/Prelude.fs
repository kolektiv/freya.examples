﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2014
//
//    Ryan Riley (@panesofglass) and Andrew Cherry (@kolektiv)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//----------------------------------------------------------------------------

[<AutoOpen>]
module Freya.TodoBackend.Prelude

open System.IO
open System.Text
open Arachne.Http
open Arachne.Language
open Chiron
open Freya.Core
open Freya.Lenses.Http
open Freya.Machine.Extensions.Http

module Either =
    let Success = Choice1Of2
    let Failure = Choice2Of2

    let (|Success|Failure|) m =
        match m with
        | Choice1Of2 x -> Success x
        | Choice2Of2 x -> Failure x

    let bind f m =
        match m with
        | Success x -> f x
        | Failure err -> Failure err

    let apply f m =
        match f, m with
        | Success f', Success m' -> Success <| f' m'
        | Failure e1, Failure e2 -> Failure <| String.concat System.Environment.NewLine [e1; e2]
        | Failure e1, _ -> Failure e1
        | _, Failure e2 -> Failure e2

    let map f m =
        bind (f >> Success) m

    let toOption =
        function Success x -> Some x | _ -> None

(* Utility

   Useful functions that it's handy to have around but not defined
   elsewhere in F# *)

let tuple x y =
    x, y

(* Request Body Helper

   Freya doesn't provide built-in ways of extracting data from the body of
   a request, as it's usually very specific to an application, and the Freya
   way is to let the developer choose the most suitable approach.

   We've used Chiron in this example, so we can use that to define the body
   function below, which (following from Chiron) uses static inference to
   determine the type of return value needed. *)

let readStream (x: Stream) =
    use reader = new StreamReader (x)
    reader.ReadToEndAsync()
    |> Async.AwaitTask

let readBody =
    freya {
        let! body = Freya.Optic.get Request.body_
        return! Freya.fromAsync readStream body }

let inline body () =
    freya {
        let! body = readBody
        return (Json.tryParse body |> Either.bind Json.tryDeserialize |> Either.toOption) }

(* Content Negotiation/Representation Helper

   Freya is also agnostic about data serialization in the response direction as
   well, believing it to be a choice for the developer.

   Here we've taken a simple approach, defining a function which always returns
   UTF-8 encoded JSON, English language, provided that the argument can
   be serialized to JSON using Chiron. *)

let inline represent x =
    { Description =
        { Charset = Some Charset.Utf8
          Encodings = None
          MediaType = Some MediaType.Json
          Languages = Some [ LanguageTag.Parse "en" ] }
      Data = (Json.serialize >> Json.format >> Encoding.UTF8.GetBytes) x }