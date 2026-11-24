namespace LibBooksFSharp.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open BookClass
open IBookrepository

type BookController(bookRepository: IBookRepository, env: IWebHostEnvironment) =
    inherit Controller()

    let uploadsFolder = "uploads"

    let toPublicPath (fileName: string) = "/" + uploadsFolder + "/" + fileName

    member private _.SaveUploadAsync(file: IFormFile): Task<string option> = task {
        if isNull file || file.Length = 0L then
            return None
        else
            let folderPath = Path.Combine(env.WebRootPath, uploadsFolder)
            Directory.CreateDirectory(folderPath) |> ignore
            let rawName = if isNull file.FileName then "" else file.FileName
            let rawExt = Path.GetExtension(rawName)
            let ext = if rawExt.Length <= 10 && rawExt.StartsWith(".") then rawExt else ""
            let fileName = Guid.NewGuid().ToString("N") + ext
            let fullPath = Path.Combine(folderPath, fileName)
            use stream = new FileStream(fullPath, FileMode.Create)
            do! file.CopyToAsync(stream)
            return Some (toPublicPath fileName)
    }

    member private _.DeleteUploadByPath(publicPath: string) =
        if not (String.IsNullOrEmpty(publicPath)) then
            let prefix = "/" + uploadsFolder + "/"
            if publicPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                let name = publicPath.Substring(prefix.Length)
                if name.Length > 0 && not (name.Contains("/")) && not (name.Contains("\\")) && not (name.Contains("..")) then
                    let full = Path.Combine(env.WebRootPath, uploadsFolder, name)
                    if File.Exists(full) then
                        try File.Delete(full) with _ -> ()

    // Testphase: der angemeldete Benutzer wird in der Session gespeichert (kein Cookie-Authentication)
    member private this.CurrentUserId() =
        let userId = this.HttpContext.Session.GetString("UserId")
        if isNull userId then "" else userId

    // Prüft, ob ein Benutzer angemeldet ist (Session)
    member private this.RequireLogin(): bool =
        String.IsNullOrEmpty(this.CurrentUserId()) |> not

    // Darf der aktuelle Benutzer dieses Buch bearbeiten/löschen? (nur der Eigentümer)
    member private this.CanManage(bookUserId: string) =
        not (String.IsNullOrEmpty(bookUserId)) && bookUserId = this.CurrentUserId()

    member this.Index(): Task<IActionResult> = task {
        let! books = bookRepository.GetAllBooksAsync()
        return this.View(books) :> IActionResult
    }

    // Nur die Bücher des aktuellen Benutzers (anhand der UserId)
    member this.MyBooks(): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        else
            let! books = bookRepository.GetBooksByUserIdAsync(this.CurrentUserId())
            return this.View(books) :> IActionResult
    }

    [<HttpGet>]
    member this.Create(): IActionResult =
        if not (this.RequireLogin()) then
            this.RedirectToAction("Login", "User") :> IActionResult
        else
            this.View() :> IActionResult

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Create(book: BookClass): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        elif String.IsNullOrWhiteSpace(book.BookName) then
            this.ModelState.AddModelError("BookName", "Titel ist erforderlich.")
            return this.View(book) :> IActionResult
        elif not this.ModelState.IsValid then
            return this.View(book) :> IActionResult
        else
            book.UserId <- this.CurrentUserId()

            // Buch-ID automatisch generieren: GUID, in Text umwandeln und in der Spalte BookId speichern
            book.BookId <- Guid.NewGuid().ToString()

            let! fileOpt = this.SaveUploadAsync(book.BookFileUpload)
            match fileOpt with
            | Some p -> book.BookFile <- p
            | None -> book.BookFile <- if isNull book.BookFile then "" else book.BookFile

            let! imgOpt = this.SaveUploadAsync(book.BookImageUpload)
            match imgOpt with
            | Some p -> book.BookImage <- p
            | None -> book.BookImage <- if isNull book.BookImage then "" else book.BookImage

            do! bookRepository.AddNewBookAsync(book)
            this.TempData["Message"] <- "Das Buch wurde erfolgreich hinzugefügt und erscheint jetzt in Ihren Büchern."
            // Nach dem Hinzufügen: Weiterleitung zur eigenen Bücherseite des Benutzers
            return this.RedirectToAction("MyBooks") :> IActionResult
    }

    member this.Details(bookId: string): Task<IActionResult> = task {
        let! book = bookRepository.FindBookByBookId(bookId)
        match book with
        | Some b -> return this.View(b) :> IActionResult
        | None -> return this.NotFound() :> IActionResult
    }

    [<HttpGet>]
    member this.Edit(bookId: string): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        else
            let! book = bookRepository.FindBookByBookId(bookId)
            match book with
            | Some b when this.CanManage(b.UserId) -> return this.View(b) :> IActionResult
            | Some _ -> return this.Forbid() :> IActionResult
            | None -> return this.NotFound() :> IActionResult
    }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member this.Edit(bookId: string, book: BookClass): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        else
            let! existing = bookRepository.FindBookByBookId(bookId)
            match existing with
            | None -> return this.NotFound() :> IActionResult
            | Some old when not (this.CanManage(old.UserId)) -> return this.Forbid() :> IActionResult
            | Some old ->
                // Der Bucheigentümer darf nicht über das Formular geändert werden
                book.UserId <- old.UserId

                if String.IsNullOrWhiteSpace(book.BookName) then
                    this.ModelState.AddModelError("BookName", "Titel ist erforderlich.")

                if not this.ModelState.IsValid then
                    return this.View(book) :> IActionResult
                else
                    let oldFile = book.BookFile
                    let oldImage = book.BookImage

                    let! fileOpt = this.SaveUploadAsync(book.BookFileUpload)
                    let! imgOpt = this.SaveUploadAsync(book.BookImageUpload)

                    match fileOpt with
                    | Some p ->
                        this.DeleteUploadByPath(oldFile)
                        book.BookFile <- p
                    | None -> book.BookFile <- if isNull oldFile then "" else oldFile

                    match imgOpt with
                    | Some p ->
                        this.DeleteUploadByPath(oldImage)
                        book.BookImage <- p
                    | None -> book.BookImage <- if isNull oldImage then "" else oldImage

                    do! bookRepository.UpdateBookAsync(book, bookId)
                    this.TempData["Message"] <- "Das Buch wurde erfolgreich aktualisiert."
                    return this.RedirectToAction("MyBooks") :> IActionResult
    }

    [<HttpGet>]
    member this.Delete(bookId: string): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        else
            let! book = bookRepository.FindBookByBookId(bookId)
            match book with
            | Some b when this.CanManage(b.UserId) -> return this.View(b) :> IActionResult
            | Some _ -> return this.Forbid() :> IActionResult
            | None -> return this.NotFound() :> IActionResult
    }

    [<HttpPost>]
    [<ActionName("Delete")>]
    [<ValidateAntiForgeryToken>]
    member this.DeleteConfirmed(bookId: string): Task<IActionResult> = task {
        if not (this.RequireLogin()) then
            return this.RedirectToAction("Login", "User") :> IActionResult
        else
            let! existing = bookRepository.FindBookByBookId(bookId)
            match existing with
            | Some b when not (this.CanManage(b.UserId)) -> return this.Forbid() :> IActionResult
            | _ ->
                do! bookRepository.DeleteBookAsync(bookId)
                let backAction =
                    match existing with
                    | Some b when b.UserId = this.CurrentUserId() -> "MyBooks"
                    | _ -> "Index"
                match existing with
                | Some b ->
                    this.DeleteUploadByPath(b.BookFile)
                    this.DeleteUploadByPath(b.BookImage)
                    this.TempData["Message"] <- "Das Buch wurde erfolgreich gelöscht."
                | None -> ()
                return this.RedirectToAction(backAction) :> IActionResult
    }
