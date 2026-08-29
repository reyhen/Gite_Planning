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

        let port =
            match Environment.GetEnvironmentVariable("PORT") with
            | null -> "8080"
            | p -> p

        let builder =
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(fun webHostBuilder ->

                    webHostBuilder.UseUrls(sprintf "http://0.0.0.0:%s" port)

                    webHostBuilder.ConfigureServices(fun services ->

                        services.AddControllersWithViews()
                            .AddRazorRuntimeCompilation()
                        |> ignore

                        services.AddRazorPages()
                        |> ignore

                        services.AddDataProtection()
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

                    webHostBuilder.Configure(fun app ->

                        let env =
                            app.ApplicationServices
                                .GetRequiredService<IHostEnvironment>()

                        if not env.IsDevelopment() then
                            app.UseExceptionHandler("/Home/Error")
                            |> ignore

                        app.UseStaticFiles()
                        |> ignore

                        app.UseRouting()
                        |> ignore

                        app.UseAuthorization()
                        |> ignore

                        app.UseEndpoints(fun endpoints ->

                            endpoints.MapControllerRoute(
                                "default",
                                "{controller=Home}/{action=Index}/{id?}"
                            )
                            |> ignore

                            endpoints.MapRazorPages()
                            |> ignore
                        )
                        |> ignore
                    )
                )

        let host = builder.Build()

        host.Run()

        0
