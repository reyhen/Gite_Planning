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

        // Services MVC + Razor
        builder.Services
            .AddControllersWithViews()
            .AddRazorRuntimeCompilation()
        |> ignore

        builder.Services.AddRazorPages()
        |> ignore

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
            app.UseHsts()
            |> ignore

        app.UseHttpsRedirection()
        |> ignore

        app.UseStaticFiles()
        |> ignore

        app.UseRouting()
        |> ignore

        app.UseAuthorization()
        |> ignore

        app.MapControllerRoute(
            name = "default",
            pattern = "{controller=Home}/{action=Index}/{id?}"
        )
        |> ignore

        app.MapRazorPages()
        |> ignore

        app.Run()

        0
