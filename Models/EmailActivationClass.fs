module EmailActivationClass
open System

type EmailActivationClass() =
    let mutable userId: string = null
    let mutable userEmail: string = null
    let mutable requestDateTime: DateTime = DateTime.Now
    let mutable activationCode: string = null
    let mutable certificate: string = null

    member _.UserId
         with get() = userId
         and set(value: string) = userId <- value

    member _.UserEmail
         with get() = userEmail
         and set(value: string) = userEmail <- value

    member _.RequestDateTime
         with get() = requestDateTime
         and set(value: DateTime) = requestDateTime <- value

    member _.ActivationCode
         with get() = activationCode
         and set(value: string) = activationCode <- value

    member _.Certificate
         with get() = certificate
         and set(value: string) = certificate <- value
