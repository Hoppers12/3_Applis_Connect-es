using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using BDD.Data;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Configuration des services, notamment la BDD
                webBuilder.ConfigureServices(services =>
                {
                    services.AddDbContext<ResultsDbContext>(options =>
                        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ResultsDb;Trusted_Connection=True;"));
                });

                // Configuration de l'application
                webBuilder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Endpoint pour recevoir et sauvegarder un résultat
                        endpoints.MapPost("/receive_result", async context =>
                        {
                            try
                            {
                                // Lecture et désérialisation de la requête
                                var requestBody = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
                                var data = JsonConvert.DeserializeObject<dynamic>(requestBody);

                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    // Récupération du DbContext
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
                                    
                                    // Création d'une nouvelle entité Result
                                    var result = new BDD.Models.Result
                                    {
                                        ComputedResult = (int)data.result,
                                        Timestamp = DateTime.UtcNow
                                    };

                                    // Ajout et sauvegarde dans la base de données
                                    dbContext.Results.Add(result);
                                    await dbContext.SaveChangesAsync();
                                }

                                Console.WriteLine($"Résultat reçu et sauvegardé : {data.result}");

                                // Réponse au client
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                await context.Response.WriteAsync("Résultat reçu et sauvegardé !");
                            }
                            catch (Exception ex)
                            {
                                // Gestion des erreurs
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await context.Response.WriteAsync($"Erreur : {ex.Message}");
                            }
                        });


                        // Endpoint pour récupérer tous les résultats stockés dans la base de données
                        endpoints.MapGet("/get_results", async context =>
                        {
                            try
                            {
                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    // Récupération du DbContext
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
                                    
                                    // Récupérer tous les résultats de la base de données
                                    var results = await dbContext.Results.ToListAsync();

                                    // Retourner les résultats sous forme de JSON
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(JsonConvert.SerializeObject(results));
                                }
                            }
                            catch (Exception ex)
                            {
                                // Gestion des erreurs
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await context.Response.WriteAsync($"Erreur : {ex.Message}");
                            }
                        });

                    });
                });
            });
}
