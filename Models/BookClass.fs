module BookClass
open System
open Microsoft.AspNetCore.Http

type BookClass() =
    let mutable bookId: string = null
    let mutable bookName: string = null
    let mutable bookAuthor: string = null
    let mutable bookDate: DateTime = DateTime.Today
    let mutable bookFile: string = ""
    let mutable bookImage: string = ""
    let mutable userId: string = null
    let mutable bookFileUpload: IFormFile = null
    let mutable bookImageUpload: IFormFile = null

    new(bookId: string, bookName: string, bookAuthor: string, bookDate: DateTime,
        bookFile: string, bookImage: string, userId: string) as this =
        BookClass()
        then
            this.BookId <- bookId
            this.BookName <- bookName
            this.BookAuthor <- bookAuthor
            this.BookDate <- bookDate
            this.BookFile <- bookFile
            this.BookImage <- bookImage
            this.UserId <- userId

    member _.BookId
         with get() = bookId
         and set(value: string) = bookId <- value

    member _.BookName
         with get() = bookName
         and set(value: string) = bookName <- value

    member _.BookAuthor
         with get() = bookAuthor
         and set(value: string) = bookAuthor <- value

    member _.BookDate
         with get() = bookDate
         and set(value: DateTime) = bookDate <- value

    member _.BookFile
         with get() = bookFile
         and set(value: string) = bookFile <- value

    member _.BookImage
         with get() = bookImage
         and set(value: string) = bookImage <- value

    member _.UserId
         with get() = userId
         and set(value: string) = userId <- value

    member _.BookFileUpload
         with get() = bookFileUpload
         and set(value: IFormFile) = bookFileUpload <- value

    member _.BookImageUpload
         with get() = bookImageUpload
         and set(value: IFormFile) = bookImageUpload <- value
