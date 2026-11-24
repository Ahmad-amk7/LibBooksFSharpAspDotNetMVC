module IEmailActivationRepository

open System.Threading.Tasks

type IEmailActivationRepository =

    /// Löscht eine eventuell vorhandene frühere Aktivierungsanfrage dieses Benutzers, erstellt eine neue Zeile
    /// (6-stelliger Code + GUID-Zertifikat), aktualisiert das Zertifikat des Benutzers und
    /// sendet die Aktivierungs-E-Mail. Gibt den generierten Code zurück ("" falls kein Benutzer gefunden).
    abstract member RequestActivationAsync: string -> Task<string>

    /// Prüft den 6-stelligen Code und ob das Zertifikat mit dem Zertifikat des Benutzers
    /// übereinstimmt, und markiert das Konto danach als aktiviert. Gibt bei Erfolg true zurück.
    abstract member ActivateAccountAsync: string * string -> Task<bool>
