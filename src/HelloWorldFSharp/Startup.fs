namespace HelloWorldFSharp

open System
open System.Threading.Tasks
open Microsoft.AspNet.Http
open Microsoft.AspNet.Builder

type Startup() = 
    let writeResponse (context : HttpContext) = 
        let ret = async {
                        let payload = "Hello World"
                        context.Response.ContentLength <- new Nullable<int64>(int64 (payload.Length))
                        context.Response.WriteAsync(payload) |> Async.AwaitIAsyncResult |> ignore
                    }
        Task.Factory.StartNew(fun () -> ret |> Async.RunSynchronously)

    member x.Configure (app : IBuilder) = 
        //let run : Func<HttpContext, unit> = new Func<HttpContext, unit>(writeResponse)
        app.Run(new RequestDelegate(writeResponse))