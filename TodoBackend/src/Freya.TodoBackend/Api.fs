//----------------------------------------------------------------------------
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

module Freya.TodoBackend.Api

open System
open Arachne.Http
open Arachne.Http.Cors
open Arachne.Language
open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Machine.Extensions.Http.Cors
open Freya.Machine.Router
open Freya.Router
open Freya.TodoBackend.Domain

(* Route Properties

   When designing Freya applications, particularly those based on machine,
   it's helpful to think in terms of *properties* of a request, rather than
   request data. This function, memoized, essentially represents a one-time
   computed *property* of the request (and specifically, in this case, the
   route).

   It might seem slightly unwise to be doing things like Option.get on values,
   but remember that we will only evaluate this property in the case of a route
   being matched which has an id, so it can be considered safe in this context. *)

let id =
    (Option.get >> Guid.Parse) <!> Freya.Optic.get (Route.atom_ "id") |> Freya.Memo.wrap

(* Body Properties

   As with the route properties, it is helpful to think of these values
   as properties of the request. Both newTodo and patchTodo are statically inferred,
   inferring the type to be returned from the context in which they're used. *)

let newTodo =
    body () |> Freya.Memo.wrap

let patchTodo =
    body () |> Freya.Memo.wrap

(* Domain Operations

   Here we can see that we wrap the domain api, turning the functions into
   Freya<'a> functions using fromAsync and passing properties of the request
   to the functions.

   Again, we memoize the results as we don't need (or wish)
   to evaluate these more than once per request. By memoizing we can
   guarantee that these functions are idempotent within the scope of a
   request, allowing us to use them as part of multiple decisions safely. *)

let add =
    Freya.fromAsync addTodo =<< (Option.get <!> newTodo) |> Freya.Memo.wrap

let clear =
    Freya.fromAsync clearTodos =<< Freya.init () |> Freya.Memo.wrap

let delete =
    Freya.fromAsync deleteTodo =<< id |> Freya.Memo.wrap

let get =
    Freya.fromAsync getTodo =<< id |> Freya.Memo.wrap

let list =
    Freya.fromAsync listTodos =<< Freya.init () |> Freya.Memo.wrap

let update =
    Freya.fromAsync updateTodo =<< (tuple <!> id <*> (Option.get <!> patchTodo)) |> Freya.Memo.wrap

(* Machine

   We define the functions that we'll use for decisions and resources
   within our freyaMachine expressions here. We can use the results of
   operations like "add" multiple times without worrying as we memoized
   that function.

   We also define a resource (common) of common properties of a resource,
   this saves us repeating configuration multiple times (once per resource).

   Finally we define our two resources, the first for the collection of Todos,
   the second for an individual Todo. *)

let empty =
    fun _ -> ()

let addAction =
    empty <!> add

let addHandler =
    represent <!> add

let clearAction =
    empty <!> clear

let deleteAction =
    empty <!> delete

let getHandler =
    represent <!> get

let listHandler =
    represent <!> list

let updateAction =
    empty <!> update

let common =
    freyaMachine {
        using http
        using httpCors
        charsetsSupported Charset.Utf8
        corsHeadersSupported [ "accept"; "content-type" ]
        corsOriginsSupported AccessControlAllowOriginRange.Any
        languagesSupported (LanguageTag.Parse "en")
        mediaTypesSupported MediaType.Json }

let todos =
    freyaMachine {
        including common
        methodsSupported [ DELETE; GET; OPTIONS; POST ]
        doDelete clearAction
        doPost addAction
        handleCreated addHandler
        handleOk listHandler }

let todo =
    freyaMachine {
        including common
        methodsSupported [ DELETE; GET; OPTIONS; Method.Custom "PATCH" ]
        doDelete deleteAction
        doPatch updateAction
        handleOk getHandler }

(* Router

   We have our two resources, but they need to have appropriate requests
   routed to them. We route them using the freyaRouter expression, using the
   shorthand "resource" syntax defined in Freya.Machine.Router (simply shorthand
   for "route All". *)

let todoRoutes =
    freyaRouter {
        resource "/" todos
        resource "/{id}" todo }

(* API

   Finally we expose our actual API. In more complex applications than this
   we would expect to see multiple components of the application pipelined
   to form a more complex whole, but in this case we only have our single router. *)

let api =
    todoRoutes