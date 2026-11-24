module AuthViewModels

type LoginViewModel() =
    member val Email = "" with get, set
    member val Password = "" with get, set

type RegisterViewModel() =
    member val UserName = "" with get, set
    member val UserEmail = "" with get, set
    member val UserPassword = "" with get, set
    member val ConfirmPassword = "" with get, set

type ActivateViewModel() =
    member val Email = "" with get, set
    member val Code = "" with get, set
