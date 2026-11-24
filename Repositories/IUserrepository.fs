module IUserrepository

open System.Threading.Tasks
open UserAccount

type IUserRepository =
    abstract member AddNewAccountAsync: UserAccount -> Task

    abstract member UpdateAccountAsync: UserAccount * string -> Task

    abstract member DeleteAccountAsync: string -> Task

    abstract member FindAccountByEmailAsync: string -> Task<UserAccount option>

    abstract member FindAccountByUserId: string -> Task<UserAccount option>

    abstract member GetAllAccountsAsync: unit -> Task<UserAccount list>

    /// Anzahl der Benutzer - dient zur Feststellung, ob dies das erste Konto ist
    abstract member CountAccountsAsync: unit -> Task<int64>

    /// Den Zugriff eines Benutzers sperren oder wieder freigeben
    abstract member UpdateRestrictionAsync: string * bool -> Task
