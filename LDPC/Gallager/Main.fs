﻿module Hamstr.Ldpc.Main

open System
open System.IO
open System.Numerics
open FSharp.Numerics
open Hamstr.Ldpc.DvbS2
open Hamstr.Demod
open Hamstr.Ldpc.Decoder

type FileType = 
    | BitFile
    | IqFile

let testPls = 04uy
let iqDataFileName = "../Data/qpsk_testdata.out"
let bitFileName = "../Data/qpsk_testdata.bits"

let readComplexNumber (reader:BinaryReader) = 
    let real = reader.ReadSingle()
    let imaginary = reader.ReadSingle()
    Complex(float real, float imaginary)

// FIXME The demod should happen on a per-frame basis, not per-symbol
let readSymbol reader modulation = 
    let noiseVariance = 0.2
    readComplexNumber reader 
    |> demodulateSymbol noiseVariance modulation

let readFrame fileType frameType modcod reader =
    let frameLength = frameType |> FrameType.BitLength
    let codingTableEntry = findCodingTableEntry (frameType, modcod.LdpcRate)
    let nDataBits = codingTableEntry.KLdpc
    let nParityBits = codingTableEntry.NLdpc - nDataBits

    let (data, parity) = 
        match fileType with
        | IqFile ->
            let bps  = bitsPerSymbol modcod.Modulation
            let data =
                [ 1 .. nDataBits / bps ] 
                |> Seq.collect (fun _ -> readSymbol reader modcod.Modulation)
            let parity = 
                [ 1 .. nParityBits / bps ] 
                |> Seq.collect (fun _ -> readSymbol reader modcod.Modulation)
            data, parity
        | BitFile ->
            let data =
                [ 1 .. nDataBits ] 
                |> Seq.map (fun _ -> LLR.Create(reader.ReadByte()))
            let parity =
                [ 1 .. nParityBits ] 
                |> Seq.map (fun _ -> LLR.Create(reader.ReadByte()))
            data, parity
    { frameType = frameType; ldpcCode = modcod.LdpcRate; data = Array.ofSeq data; parity = Array.ofSeq parity }

let readTestFile fileType fileName frameType modcod =
    use stream = File.OpenRead(fileName)
    use reader = new BinaryReader(stream)
    readFrame fileType frameType modcod reader 

let checkForBitErrors refSeq otherSeq =
    let comparer (a:LLR) (b:LLR) =
        if a.ToBool = b.ToBool then 0 else 1
    otherSeq 
    |> Seq.compareWith comparer refSeq

let printParity (refArray: LLR array) (otherArray : LLR array) = 
    Array.zip refArray otherArray
    |> Array.iteri (fun i (a, b) -> 
        if a.ToBool = b.ToBool then () else printfn "Difference in element %A" i)

[<EntryPoint>]
let main argv =
    let frameLength = Long |> FrameType.BitLength 
    let modcod = ModCodLookup.[testPls]
    let frame = readTestFile IqFile iqDataFileName Long modcod
    let referenceFrame = readTestFile BitFile bitFileName Long modcod

    let foo = decode (Long, modcod.LdpcRate) frame
    
    0   