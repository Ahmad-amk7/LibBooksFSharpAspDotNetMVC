module IBookrepository

open System.Threading.Tasks
open BookClass

type IBookRepository =
    abstract member AddNewBookAsync: BookClass -> Task

    abstract member UpdateBookAsync: BookClass * string -> Task

    abstract member DeleteBookAsync: string -> Task

    abstract member FindBookByBookId: string -> Task<BookClass option>

    abstract member GetAllBooksAsync: unit -> Task<BookClass list>

    /// Bücher eines bestimmten Benutzers (anhand der UserId)
    abstract member GetBooksByUserIdAsync: string -> Task<BookClass list>
