namespace Gite_Planning

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation

module Program =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        // Render impose un port dynamique
        let port =
            match System.Environment.GetEnvironmentVariable("PORT") with
            | null -> 8080
            | p -> int p

        // IMPORTANT : écouter uniquement en HTTP via UseUrls
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}")

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
            // NE PAS utiliser HSTS sur Render
            // app.UseHsts() |> ignore

        // NE PAS utiliser HTTPS sur Render
        // app.UseHttpsRedirection() |> ignore

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
