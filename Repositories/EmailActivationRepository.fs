module EmailActivationRepository

open System
open System.Security.Cryptography
open System.Threading.Tasks
open MySqlConnector
open DbManageConnection
open IEmailActivationRepository
open IEmailSender

type internal EmailActivationRepository(connection: DbManageConnection, emailSender: IEmailSender) =

    let generateCode() =
        RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6")

    interface IEmailActivationRepository with

        member _.RequestActivationAsync(userEmail: string): Task<string> = task {
            do! connection.OpenConnAsync()
            let conn = connection.GetConnection()
            use! transaction = conn.BeginTransactionAsync()
            let mutable newCode = ""
            let mutable userFound = false
            let mutable senderName = ""
            try
                use findCmd = new MySqlCommand(
                    "SELECT UserId FROM UserAccount WHERE UserEmail = @userEmail", conn, transaction)
                findCmd.Parameters.AddWithValue("@userEmail", userEmail) |> ignore
                let! idObj = findCmd.ExecuteScalarAsync()
                let userId = if isNull idObj then "" else string idObj

                if userId <> "" then
                    use nameCmd = new MySqlCommand(
                        "SELECT UserName FROM UserAccount WHERE UserId = @userId", conn, transaction)
                    nameCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                    let! nameObj = nameCmd.ExecuteScalarAsync()
                    senderName <- if isNull nameObj then "" else string nameObj

                    use delCmd = new MySqlCommand(
                        "DELETE FROM EmailActivation WHERE UserId = @userId", conn, transaction)
                    delCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                    let! _ = delCmd.ExecuteNonQueryAsync()

                    let certificate = Guid.NewGuid().ToString()
                    newCode <- generateCode()

                    use insCmd = new MySqlCommand("""
                        INSERT INTO EmailActivation (UserId, UserEmail, RequestDateTime, ActivationCode, Certificate)
                        VALUES (@userId, @userEmail, @requestDateTime, @activationCode, @certificate)
                        """, conn, transaction)
                    insCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                    insCmd.Parameters.AddWithValue("@userEmail", userEmail) |> ignore
                    insCmd.Parameters.AddWithValue("@requestDateTime", DateTime.Now) |> ignore
                    insCmd.Parameters.AddWithValue("@activationCode", newCode) |> ignore
                    insCmd.Parameters.AddWithValue("@certificate", certificate) |> ignore
                    let! _ = insCmd.ExecuteNonQueryAsync()

                    use updCmd = new MySqlCommand(
                        "UPDATE UserAccount SET Certificate = @certificate WHERE UserId = @userId", conn, transaction)
                    updCmd.Parameters.AddWithValue("@certificate", certificate) |> ignore
                    updCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                    let! _ = updCmd.ExecuteNonQueryAsync()

                    userFound <- true

                do! transaction.CommitAsync()
            with ex ->
                do! transaction.RollbackAsync()
                raise ex

            // Aktivierungscode an die E-Mail des Benutzers senden (über die E-Mail-Versandklasse - Gmail)
            // Hinweis: Ein Sendefehler (Netzwerk / Gmail-Daten) darf das Erstellen des Kontos nicht verhindern
            if userFound then
                try
                    do! emailSender.SendActivationEmailAsync(userEmail, senderName, newCode)
                with _ ->
                    ()

            return newCode
        }

        member _.ActivateAccountAsync(userEmail: string, activationCode: string): Task<bool> = task {
            do! connection.OpenConnAsync()
            let conn = connection.GetConnection()
            use! transaction = conn.BeginTransactionAsync()
            let mutable activated = false
            try
                let code = if isNull activationCode then "" else activationCode.Trim()

                use findCmd = new MySqlCommand(
                    "SELECT UserId, Certificate FROM UserAccount WHERE UserEmail = @userEmail", conn, transaction)
                findCmd.Parameters.AddWithValue("@userEmail", userEmail) |> ignore
                use! reader = findCmd.ExecuteReaderAsync()
                let! hasUser = reader.ReadAsync()
                let mutable userId = ""
                let mutable accountCert = ""
                if hasUser then
                    userId <- if reader.IsDBNull(0) then "" else reader.GetString(0)
                    accountCert <- if reader.IsDBNull(1) then "" else reader.GetString(1)
                do! reader.CloseAsync()

                if userId <> "" && accountCert <> "" && code <> "" then
                    use codeCmd = new MySqlCommand("""
                        SELECT Certificate FROM EmailActivation
                        WHERE UserId = @userId AND ActivationCode = @activationCode
                        """, conn, transaction)
                    codeCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                    codeCmd.Parameters.AddWithValue("@activationCode", code) |> ignore
                    let! certObj = codeCmd.ExecuteScalarAsync()
                    let activationCert = if isNull certObj then "" else string certObj

                    if activationCert <> "" && activationCert = accountCert then
                        use updCmd = new MySqlCommand(
                            "UPDATE UserAccount SET IsActivated = 1 WHERE UserId = @userId", conn, transaction)
                        updCmd.Parameters.AddWithValue("@userId", userId) |> ignore
                        let! _ = updCmd.ExecuteNonQueryAsync()
                        activated <- true

                do! transaction.CommitAsync()
            with ex ->
                do! transaction.RollbackAsync()
                raise ex

            return activated
        }
