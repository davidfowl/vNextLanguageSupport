namespace HelloWorldFSharp
type Program() = 
  member t.Main (args: string array) =
    printfn "Hello World from F#"
    System.Console.ReadLine()