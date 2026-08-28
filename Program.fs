namespace Gite_Planning

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
open Microsoft.AspNetCore.Server.Kestrel.Core

module Program =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        // Render impose un port dynamique
        let port =
            match System.Environment.GetEnvironmentVariable("PORT") with
            | null -> 8080
            | p -> int p

        // IMPORTANT : désactiver HTTPS et écouter uniquement en HTTP
        builder.WebHost.ConfigureKestrel(fun options ->
            options.ListenAnyIP(port) // HTTP seulement
        )

        // Services MVC + Razor
        builder.Services
            .AddControllersWithViews()
            .AddRazorRuntimeCompilation()
        |> ignore

        builder.Services.AddRazorPages() |> ignore

        builder.Services
            .AddDataProtection()
            .PersistKeysToFileSystem(
                DirectoryInfo(
                    Path.Combine(
                        builder.Environment.ContentRootPath,
                        "App_Data",
                        "DataProtectionKeys"
                    )
                )
            )
            .SetApplicationName("Gite_Planning")
        |> ignore

        let app = builder.Build()

        if not (builder.Environment.IsDevelopment()) then
            app.UseExceptionHandler("/Home/Error")
            // ⚠️ NE PAS utiliser HSTS ni HTTPS sur Render
            // app.UseHsts() |> ignore

        // ⚠️ SUPPRIMÉ : app.UseHttpsRedirection()

        app.UseStaticFiles() |> ignore
        app.UseRouting() |> ignore
        app.UseAuthorization() |> ignore

        app.MapControllerRoute(
            name = "default",
            pattern = "{controller=Home}/{action=Index}/{id?}"
        )
        |> ignore

        app.MapRazorPages() |> ignore

        app.Run()

        0
