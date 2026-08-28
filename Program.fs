namespace Gite_Planning

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation

module Program =
    [<EntryPoint>]
    let main args =

        // Render impose un port dynamique
        let port =
            match Environment.GetEnvironmentVariable("PORT") with
            | null -> "8080"
            | p -> p

        // Pipeline ASP.NET Core F# classique
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls($"http://0.0.0.0:{port}")
                    .Configure(fun app ->
                        let env = app.ApplicationServices.GetRequiredService<IHostEnvironment>()

                        if not env.IsDevelopment() then
                            app.UseExceptionHandler("/Home/Error") |> ignore

                        // NE PAS utiliser HTTPS sur Render
                        // app.UseHttpsRedirection() |> ignore

                        app.UseStaticFiles() |> ignore
                        app.UseRouting() |> ignore
                        app.UseAuthorization() |> ignore

                        app.UseEndpoints(fun endpoints ->
                            endpoints.MapControllerRoute(
                                name = "default",
                                pattern = "{controller=Home}/{action=Index}/{id?}"
                            )
                            |> ignore

                            endpoints.MapRazorPages() |> ignore
                        )
                    )
                    .ConfigureServices(fun services ->
                        services
                            .AddControllersWithViews()
                            .AddRazorRuntimeCompilation()
                        |> ignore

                        services.AddRazorPages() |> ignore

                        services
                            .AddDataProtection()
                            .PersistKeysToFileSystem(
                                DirectoryInfo(
                                    Path.Combine(
                                        Directory.GetCurrentDirectory(),
                                        "App_Data",
                                        "DataProtectionKeys"
                                    )
                                )
                            )
                            .SetApplicationName("Gite_Planning")
                        |> ignore
                    )
                |> ignore
            )
            .Build()
            .Run()

        0
