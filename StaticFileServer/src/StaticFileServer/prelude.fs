[<AutoOpen>]
module StaticFileServer.Prelude

open System.IO

let root =
    let dir = DirectoryInfo (__SOURCE_DIRECTORY__)
    DirectoryInfo(Path.Combine(dir.Parent.Parent.FullName, "root"))
