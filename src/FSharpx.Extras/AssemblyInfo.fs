﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharpx.Extras")>]
[<assembly: AssemblyProductAttribute("FSharpx.Extras")>]
[<assembly: AssemblyDescriptionAttribute("FSharpx.Extras implements general functional constructs on top of the F# core library. Its main target is F# but it aims to be compatible with all .NET languages wherever possible.")>]
[<assembly: AssemblyVersionAttribute("2.1.1")>]
[<assembly: AssemblyFileVersionAttribute("2.1.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "2.1.1"
    let [<Literal>] InformationalVersion = "2.1.1"