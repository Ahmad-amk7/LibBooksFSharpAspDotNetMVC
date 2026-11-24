module BookRepository

open BookClass
open DbManageConnection
open MySqlConnector

open System
open System.Threading.Tasks
open IBookrepository

type internal BookRepository(connection: DbManageConnection) =

    let readString (reader: System.Data.Common.DbDataReader) (name: string) =
        let ord = reader.GetOrdinal(name)
        if reader.IsDBNull(ord) then "" else reader.GetString(ord)

    let readDate (reader: System.Data.Common.DbDataReader) (name: string) =
        let ord = reader.GetOrdinal(name)
        if reader.IsDBNull(ord) then DateTime.Today else reader.GetDateTime(ord)

    // Liest die Bücherliste aus einem vorbereiteten Abfragebefehl
    let readBooksAsync (command: MySqlCommand) = task {
        use! reader = command.ExecuteReaderAsync()
        let books = ResizeArray<BookClass>()
        let mutable hasMore = true
        while hasMore do
            let! hasData = reader.ReadAsync()
            if hasData then
                let book = BookClass()
                book.BookId <- readString reader "BookId"
                book.BookName <- readString reader "BookName"
                book.BookAuthor <- readString reader "BookAuthor"
                book.BookDate <- readDate reader "BookDate"
                book.BookFile <- readString reader "BookFile"
                book.BookImage <- readString reader "BookImage"
                book.UserId <- readString reader "UserId"
                books.Add book
            else
                hasMore <- false
        return List.ofSeq books
    }

    interface IBookRepository with

        member this.AddNewBookAsync(book: BookClass): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = """
                        INSERT INTO BookClass (BookId, BookName, BookAuthor, BookDate, BookFile, BookImage, UserId)
                        VALUES (@bookId, @bookName, @bookAuthor, @bookDate, @bookFile, @bookImage, @userId)
                    """
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@bookId", book.BookId) |> ignore
                    command.Parameters.AddWithValue("@bookName", book.BookName) |> ignore
                    command.Parameters.AddWithValue("@bookAuthor", book.BookAuthor) |> ignore
                    command.Parameters.AddWithValue("@bookDate", book.BookDate) |> ignore
                    command.Parameters.AddWithValue("@bookFile", book.BookFile) |> ignore
                    command.Parameters.AddWithValue("@bookImage", book.BookImage) |> ignore
                    command.Parameters.AddWithValue("@userId", book.UserId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.UpdateBookAsync(book: BookClass, bookId: string): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = """
                        UPDATE BookClass
                        SET BookName = @bookName, BookAuthor = @bookAuthor, BookDate = @bookDate,
                            BookFile = @bookFile, BookImage = @bookImage, UserId = @userId
                        WHERE BookId = @bookId
                    """
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@bookName", book.BookName) |> ignore
                    command.Parameters.AddWithValue("@bookAuthor", book.BookAuthor) |> ignore
                    command.Parameters.AddWithValue("@bookDate", book.BookDate) |> ignore
                    command.Parameters.AddWithValue("@bookFile", book.BookFile) |> ignore
                    command.Parameters.AddWithValue("@bookImage", book.BookImage) |> ignore
                    command.Parameters.AddWithValue("@userId", book.UserId) |> ignore
                    command.Parameters.AddWithValue("@bookId", bookId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.DeleteBookAsync(bookId: string): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = "DELETE FROM BookClass WHERE BookId = @bookId"
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@bookId", bookId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.FindBookByBookId(bookId: string): Task<BookClass option> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM BookClass WHERE BookId = @bookId"
                use command = new MySqlCommand(query, conn)
                command.Parameters.AddWithValue("@bookId", bookId) |> ignore

                use! reader = command.ExecuteReaderAsync()
                let! hasData = reader.ReadAsync()
                if hasData then
                    let book = BookClass()
                    book.BookId <- readString reader "BookId"
                    book.BookName <- readString reader "BookName"
                    book.BookAuthor <- readString reader "BookAuthor"
                    book.BookDate <- readDate reader "BookDate"
                    book.BookFile <- readString reader "BookFile"
                    book.BookImage <- readString reader "BookImage"
                    book.UserId <- readString reader "UserId"
                    return Some book
                else
                    return None
            }

        member this.GetAllBooksAsync(): Task<BookClass list> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM BookClass ORDER BY BookDate DESC"
                use command = new MySqlCommand(query, conn)
                return! readBooksAsync command
            }

        member this.GetBooksByUserIdAsync(userId: string): Task<BookClass list> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM BookClass WHERE UserId = @userId ORDER BY BookDate DESC"
                use command = new MySqlCommand(query, conn)
                command.Parameters.AddWithValue("@userId", userId) |> ignore
                return! readBooksAsync command
            }
