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
                        let! succeeded = context.Response.WriteAsync(payload) |> Async.AwaitIAsyncResult
                        return ()
                    }
        Async.StartAsTask ret :> Task

    member x.Configure (app : IApplicationBuilder) = 
        //let run : Func<HttpContext, unit> = new Func<HttpContext, unit>(writeResponse)
        app.Run(new RequestDelegate(writeResponse))
