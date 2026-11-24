module IEmailSender

open System.Threading.Tasks

type IEmailSender =

    /// Sendet die E-Mail zur Kontoaktivierung (Benutzername, Empfänger-E-Mail, Aktivierungscode)
    abstract member SendActivationEmailAsync: string * string * string -> Task
