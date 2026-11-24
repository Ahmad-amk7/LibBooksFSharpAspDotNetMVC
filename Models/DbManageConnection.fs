module DbManageConnection

open System.Data
open System
open MySqlConnector
open ConnectionMode

type internal DbManageConnection(connectionMode: ConnectionMode) =

    let builder = MySqlConnectionStringBuilder()

    let setting key defaultValue =
        match Environment.GetEnvironmentVariable(key) with
        | null -> defaultValue
        | value when String.IsNullOrWhiteSpace(value) -> defaultValue
        | value -> value

    do
        builder.Server <- setting "LIBBOOKS_DB_SERVER" "localhost"
        builder.UserID <- setting "LIBBOOKS_DB_USER" "root"
        builder.Password <- setting "LIBBOOKS_DB_PASSWORD" ""
        builder.Port <- UInt32.Parse(setting "LIBBOOKS_DB_PORT" "3306")
        builder.CharacterSet <- "utf8mb4"

        match connectionMode with
        | ConnectionMode.CreateDatabase ->
            ()
        | ConnectionMode.GetRepository ->
            builder.Database <- "DBLibBooksFSharp"
        | _ -> ()

    let connection = new MySqlConnection(builder.ConnectionString)

    // Die Verbindung gehört dieser Klasse — sie wird am Ende der Anfrage automatisch freigegeben (Scoped DI)
    interface IDisposable with
        member _.Dispose() = connection.Dispose()

    member _.OpenConnAsync() = task {
        if isNull connection || connection.State <> ConnectionState.Open then
            do! connection.OpenAsync()
    }

    member _.CloseConnAsync() = task {
        if not (isNull connection) && connection.State = ConnectionState.Open then
            do! connection.CloseAsync()
    }

    member _.GetConnection(): MySqlConnection = connection
