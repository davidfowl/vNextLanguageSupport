open System

[<EntryPoint>]
let main args =
    printfn "Arguments passed to function : %A" args
    Console.ReadKey() |> ignore
    // Return 0. This indicates success.
    0