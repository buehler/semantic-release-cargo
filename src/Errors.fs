module SemanticReleaseCargo.Errors

type SemanticReleaseError(message: string, code: string, details: string option) =
    inherit System.Exception(message)
    member _.code = code
    member _.details = details
    member _.semanticRelease = true

    interface Node.Base.Error with
        member _.message = message
        member _.message with set _ = ()
        member _.name = "SemanticReleaseError"
        member _.name with set _ = ()
        member _.stack = "N/A FABLE JS"
        member _.stack with set _ = ()
