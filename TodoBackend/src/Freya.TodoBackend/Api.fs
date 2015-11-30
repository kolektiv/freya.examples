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

module Freya.TodoBackend.Api

open System
open Arachne.Http
open Arachne.Http.Cors
open Arachne.Language
open Freya.Core
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
    freya {
        let! id = Freya.Optic.get (Route.atom_ "id")
        return (Option.get >> Guid.Parse) id } |> Freya.Memo.wrap

(* Body Properties

   As with the route properties, it is helpful to think of these values
   as properties of the request. Both newTodo and patchTodo are statically inferred,
   inferring the type to be returned from the context in which they're used. *)

let newTodo =
    Freya.Memo.wrap (body ())

let patchTodo =
    Freya.Memo.wrap (body ())

(* Domain Operations

   Here we can see that we wrap the domain api, turning the functions into
   Freya<'a> functions using fromAsync and passing properties of the request
   to the functions.

   Again, we memoize the results as we don't need (or wish)
   to evaluate these more than once per request. By memoizing we can
   guarantee that these functions are idempotent within the scope of a
   request, allowing us to use them as part of multiple decisions safely. *)

let add =
    freya {
        let! newTodo = newTodo
        return! (Freya.fromAsync addTodo) newTodo.Value } |> Freya.Memo.wrap

let clear =
    freya {
        return! (Freya.fromAsync clearTodos) () } |> Freya.Memo.wrap

let delete =
    freya {
        let! id = id
        return! (Freya.fromAsync deleteTodo) id } |> Freya.Memo.wrap

let get =
    freya {
        let! id = id
        return! (Freya.fromAsync getTodo) id } |> Freya.Memo.wrap

let list =
    freya {
        return! (Freya.fromAsync listTodos) () } |> Freya.Memo.wrap

let update =
    freya {
        let! id = id
        let! patchTodo = patchTodo
        return! (Freya.fromAsync updateTodo) (id, patchTodo.Value) } |> Freya.Memo.wrap

(* Machine

   We define the functions that we'll use for decisions and resources
   within our freyaMachine expressions here. We can use the results of
   operations like "add" multiple times without worrying as we memoized
   that function.

   We also define a resource (common) of common properties of a resource,
   this saves us repeating configuration multiple times (once per resource).

   Finally we define our two resources, the first for the collection of Todos,
   the second for an individual Todo. *)

let addAction =
    freya {
        let! _ = add
        return () }

let addedHandler =
    freya {
        let! todo = add
        return represent todo }

let clearAction =
    freya {
        let! _ = clear
        return () }

let deleteAction =
    freya {
        let! _ = delete
        return () }

let getHandler =
    freya {
        let! todo = get
        return represent todo }

let listHandler =
    freya {
        let! todos = list
        return represent todos }

let updateAction =
    freya {
        let! _ = update
        return () }

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
        handleCreated addedHandler
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