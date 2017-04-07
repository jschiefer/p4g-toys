﻿module Hamstr.Ldpc.Decoder

open System
open System.IO
open System.Numerics
open Hamstr.Ldpc.DvbS2
open Hamstr.Demod

/// LDPC-decode the frame (which is an array of tuples of bit and LLR)
let decode (rate : int * int) (frame : array<(byte * float)>) =
    let parityTable = findParityTable rate
    [ 0uy; 0uy ]