module DbBuilder

open System
open MySqlConnector
open DbManageConnection

type internal DbBuilder(connection: DbManageConnection) =

    let conn = connection

    let CreateUserTable =
        """
        CREATE TABLE IF NOT EXISTS UserAccount(
            UserId NVARCHAR(100) NOT NULL,
            UserName NVARCHAR(50),
            UserEmail NVARCHAR(150),
            UserPassword NVARCHAR(90),
            Certificate NVARCHAR(100) NOT NULL DEFAULT '',
            IsActivated BIT NOT NULL DEFAULT 0,
            IsRestricted BIT NOT NULL DEFAULT 0,
            PRIMARY KEY(UserId)
        ) ENGINE=InnoDB;"""

    let CreateBookTable =
        """
        CREATE TABLE IF NOT EXISTS BookClass(
            BookId NVARCHAR(100) NOT NULL,
            BookName NVARCHAR(100),
            BookAuthor NVARCHAR(100),
            BookDate DATETIME,
            BookFile NVARCHAR(700),
            BookImage NVARCHAR(700),
            UserId NVARCHAR(100),
            PRIMARY KEY(BookId),
            FOREIGN KEY(UserId) REFERENCES UserAccount(UserId)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) ENGINE=InnoDB;"""

    // Tabelle der Kontoaktivierungsanfragen - eins zu eins (1:1) mit der Benutzertabelle über UserId
    let CreateEmailActivationTable =
        """
        CREATE TABLE IF NOT EXISTS EmailActivation(
            UserId NVARCHAR(100) NOT NULL,
            UserEmail NVARCHAR(150) NOT NULL,
            RequestDateTime DATETIME NOT NULL,
            ActivationCode NVARCHAR(6) NOT NULL,
            Certificate NVARCHAR(100) NOT NULL,
            PRIMARY KEY(UserId),
            FOREIGN KEY(UserId) REFERENCES UserAccount(UserId)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) ENGINE=InnoDB;"""

    let executeAsync (query: string) = task {
        use command = new MySqlCommand(query, conn.GetConnection())
        let! _ = command.ExecuteNonQueryAsync()
        ()
    }

    // Prüft, ob eine Spalte in einer Tabelle existiert - funktioniert mit MySQL und MariaDB
    let columnExistsAsync (table: string) (column: string) = task {
        let query = """
            SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table
                  AND COLUMN_NAME = @column
        """
        use command = new MySqlCommand(query, conn.GetConnection())
        command.Parameters.AddWithValue("@table", table) |> ignore
        command.Parameters.AddWithValue("@column", column) |> ignore
        let! result = command.ExecuteScalarAsync()
        let count = if isNull result then 0L else Convert.ToInt64(result)
        return count > 0L
    }

    member _.GetCreateDatabaseAsync() = task {
        let query = "CREATE DATABASE IF NOT EXISTS DBLibBooksFSharp CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"
        do! conn.OpenConnAsync()
        use command = new MySqlCommand(query, conn.GetConnection())
        let! _ = command.ExecuteNonQueryAsync()
        do! conn.CloseConnAsync()
    }

    member _.GetCreateTableAsync() = task {
        do! conn.OpenConnAsync()
        conn.GetConnection().ChangeDatabase("DBLibBooksFSharp")

        do! executeAsync CreateUserTable

        // Fügt die neuen Spalten zu bereits erstellten Tabellen hinzu (keine Wirkung, falls sie schon existieren)
        let! hasCertificate = columnExistsAsync "UserAccount" "Certificate"
        if not hasCertificate then
            do! executeAsync "ALTER TABLE UserAccount ADD COLUMN Certificate NVARCHAR(100) NOT NULL DEFAULT ''"

        let! hasIsActivated = columnExistsAsync "UserAccount" "IsActivated"
        if not hasIsActivated then
            do! executeAsync "ALTER TABLE UserAccount ADD COLUMN IsActivated BIT NOT NULL DEFAULT 0"

        let! hasIsRestricted = columnExistsAsync "UserAccount" "IsRestricted"
        if not hasIsRestricted then
            do! executeAsync "ALTER TABLE UserAccount ADD COLUMN IsRestricted BIT NOT NULL DEFAULT 0"

        do! executeAsync CreateBookTable
        do! executeAsync CreateEmailActivationTable
        do! conn.CloseConnAsync()
    }
