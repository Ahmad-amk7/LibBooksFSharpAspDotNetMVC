namespace LibBooksFSharp.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open UserAccount
open IUserrepository
open IBookrepository
open IEmailActivationRepository
open AuthViewModels
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting

type UserController(userRepository: IUserRepository, bookRepository: IBookRepository,
                    emailActivationRepository: IEmailActivationRepository,
                    env: IWebHostEnvironment) as this =
    inherit Controller()

    let safeText (s: string) = if isNull s then "" else s

    // Testphase: kein Cookie-Authentication - der angemeldete Benutzer wird in der Session gespeichert
    let signInUser (account: UserAccount) =
        this.HttpContext.Session.SetString("UserId", safeText account.UserId)
        this.HttpContext.Session.SetString("UserName", safeText account.UserName)
        this.HttpContext.Session.SetString("UserEmail", safeText account.UserEmail)

    let deleteUploadByPath (publicPath: string) =
        if not (String.IsNullOrEmpty(publicPath)) then
            let prefix = "/uploads/"
            if publicPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                let name = publicPath.Substring(prefix.Length)
                if name.Length > 0 && not (name.Contains("/")) && not (name.Contains("\\")) && not (name.Contains("..")) then
                    let envSvc = this.HttpContext.RequestServices.GetService(typeof<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>)
                    let webRoot =
                        match envSvc with
                        | :? Microsoft.AspNetCore.Hosting.IWebHostEnvironment as e -> e.WebRootPath
                        | _ -> Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                    let full = Path.Combine(webRoot, "uploads", name)
                    if File.Exists(full) then
                        try File.Delete(full) with _ -> ()

    member private this.CurrentUserId() =
        let userId = this.HttpContext.Session.GetString("UserId")
        if isNull userId then "" else userId

    // Prüft, ob ein Benutzer angemeldet ist (Session) - sonst Weiterleitung zur Anmeldung
    member private this.RequireLogin(): bool =
        String.IsNullOrEmpty(this.CurrentUserId()) |> not

    // ---------- Registrierung ----------

    [<HttpGet>]
    member this.Register(): IActionResult =
        this.View(RegisterViewModel()) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Register(model: RegisterViewModel): Task<IActionResult> = task {
        if model.UserPassword <> model.ConfirmPassword then
            this.ModelState.AddModelError("ConfirmPassword", "Die Passwörter stimmen nicht überein.")

        let! existing = userRepository.FindAccountByEmailAsync(model.UserEmail)
        match existing with
        | Some _ -> this.ModelState.AddModelError("UserEmail", "Diese E-Mail ist bereits registriert.")
        | None -> ()

        if not this.ModelState.IsValid then
            return this.View(model) :> IActionResult
        else
            let account = UserAccount()
            account.UserId <- Guid.NewGuid().ToString()
            account.UserName <- safeText model.UserName
            account.UserEmail <- safeText model.UserEmail
            account.UserPassword <- safeText model.UserPassword
            do! userRepository.AddNewAccountAsync(account)

            // Aktivierungsanfrage erstellen + Aktivierungscode per E-Mail an den Benutzer senden (Gmail)
            let! code = emailActivationRepository.RequestActivationAsync(account.UserEmail)

            this.TempData["Message"] <-
                "Ihr Konto wurde erfolgreich erstellt. Bitte prüfen Sie Ihre E-Mail auf den Aktivierungscode, um Ihr Konto zu aktivieren."
            if env.IsDevelopment() && not (String.IsNullOrEmpty(code)) then
                this.TempData["DevCode"] <- code
            return this.RedirectToAction("Login") :> IActionResult
    }

    // ---------- Anmeldung / Abmeldung ----------

    [<HttpGet>]
    member this.Login(): IActionResult =
        this.View(LoginViewModel()) :> IActionResult

    // Testphase: die Anmeldung erfolgt nur durch Prüfung des Passworts in der Datenbank
    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Login(model: LoginViewModel): Task<IActionResult> = task {
        let! account = userRepository.FindAccountByEmailAsync(safeText model.Email)
        match account with
        | Some u when u.UserPassword = safeText model.Password ->
            signInUser u
            return this.RedirectToAction("Index", "Home") :> IActionResult
        | _ ->
            this.ModelState.AddModelError("", "Ungültige E-Mail oder ungültiges Passwort.")
            return this.View(model) :> IActionResult
    }

    member this.Logout(): Task<IActionResult> = task {
        this.HttpContext.Session.Clear()
        return this.RedirectToAction("Index", "Home") :> IActionResult
    }

    // ---------- Aktivierung ----------

    [<HttpGet>]
    member this.Activate(): IActionResult =
        let model = ActivateViewModel()
        match this.TempData["Email"] with
        | :? string as email when not (String.IsNullOrEmpty(email)) -> model.Email <- email
        | _ -> ()
        this.View(model) :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Activate(model: ActivateViewModel): Task<IActionResult> = task {
        let! ok = emailActivationRepository.ActivateAccountAsync(safeText model.Email, safeText model.Code)
        if ok then
            this.TempData["Message"] <- "Ihr Konto wurde erfolgreich aktiviert. Sie können sich jetzt anmelden."
            return this.RedirectToAction("Login") :> IActionResult
        else
            this.ModelState.AddModelError("", "Ungültiger Aktivierungscode. Bitte prüfen Sie den Code in der neuesten E-Mail oder fordern Sie unten einen neuen an.")
            return this.View(model) :> IActionResult
    }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.ResendCode(email: string): Task<IActionResult> = task {
        let! code = emailActivationRepository.RequestActivationAsync(safeText email)
        if not (String.IsNullOrEmpty(code)) then
            this.TempData["Message"] <- "Ein neuer Aktivierungscode wurde an Ihre E-Mail gesendet."
            if env.IsDevelopment() then
                this.TempData["DevCode"] <- code
            this.TempData["Email"] <- safeText email
        else
            this.TempData["Message"] <- "Falls diese E-Mail existiert, wurde ein neuer Aktivierungscode gesendet."
        return this.RedirectToAction("Activate") :> IActionResult
    }

    // ---------- Eigenes Konto ----------

    /// Zeigt die eigenen Kontodaten des angemeldeten Benutzers
    member this.Profile(): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login") :> IActionResult
        else
            let! account = userRepository.FindAccountByUserId(this.CurrentUserId())
            match account with
            | Some u -> return this.View(u) :> IActionResult
            | None -> return this.NotFound() :> IActionResult
    }

    [<HttpGet>]
    member this.Edit(): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login") :> IActionResult
        else
            let! account = userRepository.FindAccountByUserId(this.CurrentUserId())
            match account with
            | Some u -> return this.View(u) :> IActionResult
            | None -> return this.NotFound() :> IActionResult
    }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Edit(user: UserAccount): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login") :> IActionResult
        elif this.ModelState.IsValid then
            let userId = this.CurrentUserId()
            // Das Konto kann nicht neu zugewiesen werden - das eigene Konto wird aktualisiert
            do! userRepository.UpdateAccountAsync(user, userId)
            this.TempData["Message"] <- "Ihre Kontodaten wurden erfolgreich aktualisiert."
            return this.RedirectToAction("Profile") :> IActionResult
        else
            return this.View(user) :> IActionResult
    }

    [<HttpGet>]
    member this.Delete(): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login") :> IActionResult
        else
            let! account = userRepository.FindAccountByUserId(this.CurrentUserId())
            match account with
            | Some u -> return this.View(u) :> IActionResult
            | None -> return this.NotFound() :> IActionResult
    }

    [<HttpPost>]
    [<ActionName("Delete")>]
    [<ValidateAntiForgeryToken>]
    member this.DeleteConfirmed(): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login") :> IActionResult
        else
            let userId = this.CurrentUserId()
            let! books = bookRepository.GetBooksByUserIdAsync(userId)
            do! userRepository.DeleteAccountAsync(userId)
            for book in books do
                deleteUploadByPath book.BookFile
                deleteUploadByPath book.BookImage
            this.HttpContext.Session.Clear()
            this.TempData["Message"] <- "Ihr Konto wurde erfolgreich gelöscht."
            return this.RedirectToAction("Index", "Home") :> IActionResult
    }
