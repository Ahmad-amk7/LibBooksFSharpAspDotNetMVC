namespace LibBooksFSharp.Controllers

open System.Diagnostics
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open LibBooksFSharp.Models

type HomeController(logger: ILogger<HomeController>) =
    inherit Controller()

    member this.Index() =
        this.View() :> IActionResult

    member this.Privacy() =
        this.View() :> IActionResult

    [<ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)>]
    member this.Error() =
        let reqId =
            if isNull Activity.Current then
                this.HttpContext.TraceIdentifier
            else
                Activity.Current.Id

        this.View({ RequestId = reqId }) :> IActionResult
