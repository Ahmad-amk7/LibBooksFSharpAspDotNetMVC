module EmailSender

open System
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open MailKit.Net.Smtp
open MailKit.Security
open MimeKit
open IEmailSender

/// Klasse zum Versenden der Aktivierungs-E-Mail über den Gmail-Dienst (Gmail SMTP) mit MailKit
/// - Port 587 mit STARTTLS (oder 465 mit direktem SSL)
/// - Tragen Sie Ihre E-Mail in SmtpUser und das App-Passwort in SmtpPassword in appsettings.json ein
type internal EmailSender(config: IConfiguration) =

    let setting (key: string) (fallback: string) =
        let v = config.["EmailSettings:" + key]
        if String.IsNullOrWhiteSpace(v) then fallback else v.Trim()

    let smtpHost = setting "SmtpHost" "smtp.gmail.com"
    let smtpPort =
        match Int32.TryParse(setting "SmtpPort" "587") with
        | true, p -> p
        | _ -> 587
    let smtpUser = setting "SmtpUser" ""
    let smtpPassword = setting "SmtpPassword" ""
    let fromName = setting "FromName" "F# Bibliothek"
    let fromAddress =
        let a = setting "FromAddress" ""
        if a = "" then smtpUser else a

    member _.IsConfigured =
        smtpUser <> "" && smtpPassword <> "" && fromAddress <> ""

    interface IEmailSender with

        member _.SendActivationEmailAsync(toEmail: string, userName: string, activationCode: string): Task = task {
            if not (smtpUser <> "" && smtpPassword <> "" && fromAddress <> "") then
                // Gmail-Daten wurden noch nicht hinterlegt (SmtpUser / SmtpPassword) - der Code wird im Entwicklungsmodus angezeigt
                ()
            else
                let greeting =
                    if String.IsNullOrWhiteSpace(userName) then "Hallo," else "Hallo " + userName + ","

                let body =
                    greeting + Environment.NewLine + Environment.NewLine +
                    "Ihr Konto-Aktivierungscode lautet:" + Environment.NewLine + Environment.NewLine +
                    activationCode + Environment.NewLine + Environment.NewLine +
                    "Geben Sie diesen Code auf der Seite „Konto aktivieren“ ein, um Ihr Konto zu aktivieren." + Environment.NewLine +
                    "Falls Sie diese E-Mail nicht angefordert haben, können Sie sie ignorieren." + Environment.NewLine + Environment.NewLine +
                    "-- " + fromName

                let message = new MimeMessage()
                message.From.Add(MailboxAddress(fromName, fromAddress))
                message.To.Add(MailboxAddress("", toEmail))
                message.Subject <- "F# Bibliothek - Aktivierungscode für Ihr Konto"
                message.Body <- new TextPart("plain", body)

                let socketOptions =
                    if smtpPort = 465 then SecureSocketOptions.SslOnConnect
                    else SecureSocketOptions.StartTls

                use client = new SmtpClient()
                do! client.ConnectAsync(smtpHost, smtpPort, socketOptions)
                do! client.AuthenticateAsync(smtpUser, smtpPassword)
                let! _ = client.SendAsync(message)
                do! client.DisconnectAsync(true)
        }
        