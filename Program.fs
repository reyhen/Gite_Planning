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

        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls(sprintf "http://0.0.0.0:%s" port)
                    |> ignore

                webHostBuilder
                    .Configure(fun app ->
                        let env = app.ApplicationServices.GetRequiredService<IHostEnvironment>()

                        if not env.IsDevelopment() then
                            app.UseExceptionHandler("/Home/Error") |> ignore

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
                    |> ignore

                webHostBuilder
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
