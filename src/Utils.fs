module SemanticReleaseCargo.Utils

#if FABLE_COMPILER
open Fable.Core

[<Emit("!!$0[$1]")>]
let mapContainsKey (map: Map<_, _>) key = jsNative

[<Emit("$0[$1]")>]
let mapGetKey (map: Map<_, _>) key = map[key]
#else
let mapContainsKey (map: Map<_, _>) key = map.ContainsKey key

let mapGetKey (map: Map<_, _>) key = map[key]
#endif
