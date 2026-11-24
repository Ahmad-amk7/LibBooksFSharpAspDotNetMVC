module UserAccount
open System

type UserAccount() =
    let mutable userId: string = null
    let mutable userName: string = null
    let mutable userEmail: string = null
    let mutable userPassword: string = null
    let mutable certificate: string = ""
    let mutable isActivated: bool = false
    let mutable isRestricted: bool = false

    new(userId: string, userName: string, userEmail: string, userPassword: string) as this =
        UserAccount()
        then
            this.UserId <- userId
            this.UserName <- userName
            this.UserEmail <- userEmail
            this.UserPassword <- userPassword

    member _.UserId
         with get() = userId
         and set(value: string) = userId <- value

    member _.UserName
         with get() = userName
         and set(value: string) = userName <- value

    member _.UserEmail
         with get() = userEmail
         and set(value: string) = userEmail <- value

    member _.UserPassword
         with get() = userPassword
         and set(value: string) = userPassword <- value

    member _.Certificate
         with get() = certificate
         and set(value: string) = certificate <- value

    member _.IsActivated
         with get() = isActivated
         and set(value: bool) = isActivated <- value

    member _.IsRestricted
         with get() = isRestricted
         and set(value: bool) = isRestricted <- value
