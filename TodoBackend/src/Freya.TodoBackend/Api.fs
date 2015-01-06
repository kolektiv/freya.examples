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

[<AutoOpen>]
module Freya.TodoBackend.Api

open System
open Freya.Core
open Freya.Core.Operators
open Freya.Machine
open Freya.Machine.Router
open Freya.Router
open Freya.Types.Http

// Route

let id =
    memoM ((Option.get >> Guid.Parse) <!> getPLM (Route.valuesKey "id"))

// Body

let newTodo =
    memoM (body ())

let patchTodo =
    memoM (body ())

// Domain

let add =
    memoM (asyncM addTodo =<< (Option.get <!> newTodo))

let clear =
    memoM (asyncM clearTodos =<< returnM ())

let delete =
    memoM (asyncM deleteTodo =<< id)

let get =
    memoM (asyncM getTodo =<< id)

let list =
    memoM (asyncM listTodos =<< returnM ())

let update =
    memoM (asyncM updateTodo =<< (tuple <!> id <*> (Option.get <!> patchTodo)))

// Machine

let addAction =
    ignore <!> add

let addedHandler _ =
    represent <!> add

let clearAction =
    ignore <!> clear

let deleteAction =
    ignore <!> delete

let getHandler _ =
    represent <!> get

let listHandler _ =
    represent <!> list

let updateAction =
    ignore <!> update

// Resources

let common =
    freyaMachine {
        charsetsSupported utf8
        corsHeadersSupported corsHeaders
        corsOriginsSupported corsOrigins
        languagesSupported en
        mediaTypesSupported json }

let todosMethods =
    returnM [ 
        DELETE
        GET
        OPTIONS
        POST ]

let todos =
    freyaMachine {
        including common
        corsMethodsSupported todosMethods
        methodsSupported todosMethods
        doDelete clearAction
        doPost addAction
        handleCreated addedHandler
        handleOk listHandler } |> compileFreyaMachine

let todoMethods =
    returnM [
        DELETE
        GET
        OPTIONS
        PATCH ]

let todo =
    freyaMachine {
        including common
        corsMethodsSupported todoMethods
        methodsSupported todoMethods
        doDelete deleteAction
        doPatch updateAction
        handleOk getHandler } |> compileFreyaMachine

// Routes

let todoRoutes =
    freyaRouter {
        resource "/" todos
        resource "/:id" todo } |> compileFreyaRouter

// API

let api =
    todoRoutes