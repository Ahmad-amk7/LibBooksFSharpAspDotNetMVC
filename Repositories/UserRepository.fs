module UserRepository

open UserAccount
open DbManageConnection
open MySqlConnector

open System
open System.Threading.Tasks
open IUserrepository

type internal UserRepository(connection: DbManageConnection) =

    let readString (reader: System.Data.Common.DbDataReader) (name: string) =
        let ord = reader.GetOrdinal(name)
        if reader.IsDBNull(ord) then "" else reader.GetString(ord)

    let readBool (reader: System.Data.Common.DbDataReader) (name: string) =
        let ord = reader.GetOrdinal(name)
        (not (reader.IsDBNull(ord))) && reader.GetBoolean(ord)

    interface IUserRepository with

        member this.AddNewAccountAsync(account: UserAccount): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = """
                        INSERT INTO UserAccount (UserId, UserName, UserEmail, UserPassword, Certificate, IsActivated, IsRestricted)
                        VALUES (@userId, @userName, @userEmail, @userPassword, @certificate, @isActivated, @isRestricted)
                    """
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@userId", account.UserId) |> ignore
                    command.Parameters.AddWithValue("@userName", account.UserName) |> ignore
                    command.Parameters.AddWithValue("@userEmail", account.UserEmail) |> ignore
                    command.Parameters.AddWithValue("@userPassword", account.UserPassword) |> ignore
                    command.Parameters.AddWithValue("@certificate", account.Certificate) |> ignore
                    command.Parameters.AddWithValue("@isActivated", account.IsActivated) |> ignore
                    command.Parameters.AddWithValue("@isRestricted", account.IsRestricted) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.UpdateAccountAsync(account: UserAccount, userId: string): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = """
                        UPDATE UserAccount
                        SET UserName = @userName, UserEmail = @userEmail, UserPassword = @userPassword
                        WHERE UserId = @userId
                    """
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@userName", account.UserName) |> ignore
                    command.Parameters.AddWithValue("@userEmail", account.UserEmail) |> ignore
                    command.Parameters.AddWithValue("@userPassword", account.UserPassword) |> ignore
                    command.Parameters.AddWithValue("@userId", userId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.DeleteAccountAsync(userId: string): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = "DELETE FROM UserAccount WHERE UserId = @userId"
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@userId", userId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task

        member this.FindAccountByEmailAsync(email: string): Task<UserAccount option> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM UserAccount WHERE UserEmail = @email"
                use command = new MySqlCommand(query, conn)
                command.Parameters.AddWithValue("@email", email) |> ignore

                use! reader = command.ExecuteReaderAsync()
                let! hasData = reader.ReadAsync()
                if hasData then
                    let account = UserAccount()
                    account.UserId <- readString reader "UserId"
                    account.UserName <- readString reader "UserName"
                    account.UserEmail <- readString reader "UserEmail"
                    account.UserPassword <- readString reader "UserPassword"
                    account.Certificate <- readString reader "Certificate"
                    account.IsActivated <- readBool reader "IsActivated"
                    account.IsRestricted <- readBool reader "IsRestricted"
                    return Some account
                else
                    return None
            }

        member this.FindAccountByUserId(userId: string): Task<UserAccount option> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM UserAccount WHERE UserId = @userId"
                use command = new MySqlCommand(query, conn)
                command.Parameters.AddWithValue("@userId", userId) |> ignore

                use! reader = command.ExecuteReaderAsync()
                let! hasData = reader.ReadAsync()
                if hasData then
                    let account = UserAccount()
                    account.UserId <- readString reader "UserId"
                    account.UserName <- readString reader "UserName"
                    account.UserEmail <- readString reader "UserEmail"
                    account.UserPassword <- readString reader "UserPassword"
                    account.Certificate <- readString reader "Certificate"
                    account.IsActivated <- readBool reader "IsActivated"
                    account.IsRestricted <- readBool reader "IsRestricted"
                    return Some account
                else
                    return None
            }

        member this.GetAllAccountsAsync(): Task<UserAccount list> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT * FROM UserAccount ORDER BY UserName"
                use command = new MySqlCommand(query, conn)

                use! reader = command.ExecuteReaderAsync()
                let accounts = ResizeArray<UserAccount>()
                let mutable hasMore = true
                while hasMore do
                    let! hasData = reader.ReadAsync()
                    if hasData then
                        let account = UserAccount()
                        account.UserId <- readString reader "UserId"
                        account.UserName <- readString reader "UserName"
                        account.UserEmail <- readString reader "UserEmail"
                        account.UserPassword <- readString reader "UserPassword"
                        account.Certificate <- readString reader "Certificate"
                        account.IsActivated <- readBool reader "IsActivated"
                        account.IsRestricted <- readBool reader "IsRestricted"
                        accounts.Add account
                    else
                        hasMore <- false
                return List.ofSeq accounts
            }

        member this.CountAccountsAsync(): Task<int64> =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                let query = "SELECT COUNT(*) FROM UserAccount"
                use command = new MySqlCommand(query, conn)
                let! result = command.ExecuteScalarAsync()
                return if isNull result then 0L else Convert.ToInt64(result)
            }

        member this.UpdateRestrictionAsync(userId: string, isRestricted: bool): Task =
            task {
                do! connection.OpenConnAsync()
                let conn = connection.GetConnection()
                use! transaction = conn.BeginTransactionAsync()
                try
                    let query = "UPDATE UserAccount SET IsRestricted = @isRestricted WHERE UserId = @userId"
                    use command = new MySqlCommand(query, conn, transaction)
                    command.Parameters.AddWithValue("@isRestricted", isRestricted) |> ignore
                    command.Parameters.AddWithValue("@userId", userId) |> ignore

                    let! _ = command.ExecuteNonQueryAsync()
                    do! transaction.CommitAsync()
                with ex ->
                    do! transaction.RollbackAsync()
                    raise ex
            } :> Task
