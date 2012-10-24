﻿// ----------------------------------------------------------------------------
// Original Xml type provider
// (c) Tomas Petricek - tomasP.net, Available under Apache 2.0 license.
// ----------------------------------------------------------------------------
module internal FSharpx.TypeProviders.Inference

open System
open System.Xml
open System.Xml.Linq
open FSharpx.TypeProviders.DSL
open System.Collections.Generic
open System.Globalization
open FSharpx.Strings

// ------------------------------------------------------------------------------------------------
// Representation about inferred structure
// ------------------------------------------------------------------------------------------------

type SimpleProperty = SimpleProperty of string * Type * bool

type CompoundProperty = CompoundProperty of string * bool * CompoundProperty seq * SimpleProperty seq

open System.IO
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Core.CompilerServices

/// Generate property for every inferred property
let generateProperties ownerType accessExpr checkIfOptional setterExpr optionalSetterExpr elementProperties =   
    for SimpleProperty(propertyName,propertyType,optional) in elementProperties do
        ownerType
          |+!> (if optional then
                    let newType = optionType propertyType
                    // For optional elements, we return Option value
                    let cases = Reflection.FSharpType.GetUnionCases newType
                    let some = cases |> Seq.find (fun c -> c.Name = "Some")
                    let none = cases |> Seq.find (fun c -> c.Name = "None")

                    let optionalAccessExpr =
                        (fun args ->
                            Expr.IfThenElse
                                (checkIfOptional propertyName args,
                                Expr.NewUnionCase(some, [accessExpr propertyName propertyType args]),
                                Expr.NewUnionCase(none, [])))

                    provideProperty propertyName newType optionalAccessExpr
                        |> addSetter (optionalSetterExpr propertyName propertyType)
                else
                    provideProperty propertyName propertyType (accessExpr propertyName propertyType)
                        |> addSetter (setterExpr propertyName propertyType)
                |> addPropertyXmlDoc (sprintf "Gets the %s attribute" propertyName))
          |> ignore

/// Iterates over all the sub elements, generates types for them
/// and adds member for accessing them to the parent.
let generateSublements ownerType parentType multiAccessExpr addChildExpr newChildExpr singleAccessExpr generateTypeF children =
    for CompoundProperty(childName,multi,_,_) as child in children do
        let childType = generateTypeF parentType child

        if multi then     
            let newType = seqType childType
            let niceChildName = childName |> niceName |> singularize 

            ownerType
            |+!> (provideMethod ("Get" + pluralize niceChildName) [] newType (multiAccessExpr childName)
                    |> addMethodXmlDoc (sprintf @"Gets the %s elements" childName))
            |+!> (provideMethod ("New" + niceChildName) [] childType (newChildExpr childName)
                    |> addMethodXmlDoc (sprintf @"Creates a new %s element" childName))
            |+!> (provideMethod ("Add" + niceChildName) ["element", childType] typeof<unit> (addChildExpr childName)
                    |> addMethodXmlDoc (sprintf @"Adds a %s element" childName))            
            |> ignore
        else
            ownerType
            |+!> (provideProperty childName childType (singleAccessExpr childName)
                    |> addPropertyXmlDoc (sprintf @"Gets the %s attribute" childName))
            |> ignore

    ownerType

type ExprDef = Expr list -> Expr

type GeneratedParserSettings = {
    Schema: CompoundProperty
    EmptyConstructor: ExprDef
    FileNameConstructor: ExprDef
    DocumentContentConstructor : ExprDef
    RootPropertyGetter: ExprDef
    ToStringExpr: ExprDef }

/// Generates constructors for loading data and adds type representing Root node
let createParserType<'a> typeName (generateTypeF: ProvidedTypeDefinition -> CompoundProperty -> ProvidedTypeDefinition) settings =
    let parserType = erasedType<'a> thisAssembly rootNamespace typeName
    parserType
    |+!> (provideConstructor [] settings.EmptyConstructor
           |> addConstructorXmlDoc "Initializes the document from the schema sample.")
    |+!> (provideConstructor ["filename", typeof<string>] settings.FileNameConstructor
           |> addConstructorXmlDoc "Initializes a document from the given path.")
    |+!> (provideConstructor ["documentContent", typeof<string>] settings.DocumentContentConstructor
           |> addConstructorXmlDoc "Initializes a document from the given string.")
    |+!> (provideProperty "Root" (generateTypeF parserType settings.Schema) settings.RootPropertyGetter
           |> addPropertyXmlDoc "Gets the document root")
    |+!> (provideMethod ("ToString") [] typeof<string> settings.ToStringExpr
           |> addMethodXmlDoc "Gets the string representation")