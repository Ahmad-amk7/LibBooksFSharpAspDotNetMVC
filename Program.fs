namespace LibBooksFSharp

#nowarn "20"

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ConnectionMode
open DbManageConnection
open DbBuilder
open IUserrepository
open UserRepository
open IBookrepository
open BookRepository
open IEmailActivationRepository
open EmailActivationRepository
open IEmailSender
open EmailSender
open Microsoft.Extensions.Configuration

module Program =
    let exitCode = 0

    let initDatabaseAsync() = task {
        use adminConn = new DbManageConnection(ConnectionMode.CreateDatabase)
        let dbBuilder = new DbBuilder(adminConn)
        do! dbBuilder.GetCreateDatabaseAsync()

        use repoConn = new DbManageConnection(ConnectionMode.GetRepository)
        let tableBuilder = new DbBuilder(repoConn)
        do! tableBuilder.GetCreateTableAsync()
    }

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        builder
            .Services
            .AddControllersWithViews()
            .AddRazorRuntimeCompilation() |> ignore

        builder.Services.AddRazorPages() |> ignore

        // Kein Cookie-Authentication (Testphase) - der angemeldete Benutzer wird nur in der Session gespeichert
        builder.Services.AddDistributedMemoryCache() |> ignore
        builder.Services.AddSession() |> ignore

        builder.Services.AddScoped<DbManageConnection>(fun _ ->
            new DbManageConnection(ConnectionMode.GetRepository)) |> ignore

        builder.Services.AddScoped<IUserRepository>(fun sp ->
            let conn = sp.GetRequiredService<DbManageConnection>()
            new UserRepository(conn) :> IUserRepository) |> ignore

        builder.Services.AddScoped<IBookRepository>(fun sp ->
            let conn = sp.GetRequiredService<DbManageConnection>()
            new BookRepository(conn) :> IBookRepository) |> ignore

        builder.Services.AddScoped<IEmailSender>(fun sp ->
            let cfg = sp.GetRequiredService<IConfiguration>()
            new EmailSender(cfg) :> IEmailSender) |> ignore

        builder.Services.AddScoped<IEmailActivationRepository>(fun sp ->
            let conn = sp.GetRequiredService<DbManageConnection>()
            let sender = sp.GetRequiredService<IEmailSender>()
            new EmailActivationRepository(conn, sender) :> IEmailActivationRepository) |> ignore

        let app = builder.Build()

        try
            initDatabaseAsync().GetAwaiter().GetResult()
            app.Logger.LogInformation("Database and tables are ready.")
        with ex ->
            app.Logger.LogError(ex, "Database initialization failed: {Message}", ex.Message)

        if not (builder.Environment.IsDevelopment()) then
            app.UseExceptionHandler("/Home/Error")
            app.UseHsts() |> ignore

        app.UseHttpsRedirection()
        app.UseStaticFiles()
        app.UseRouting()
        app.UseSession()

        app.MapControllerRoute(name = "default", pattern = "{controller=Home}/{action=Index}/{id?}")
        app.MapRazorPages()

        app.Run()

        exitCode
