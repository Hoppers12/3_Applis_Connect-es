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
                                
                                // Récupération de tab_result
                                var tabResult = data.tab_result.ToObject<List<object>>();
                                int result1 = Convert.ToInt32(tabResult[0]);
                                int val1 = Convert.ToInt32(tabResult[1]);
                                int val2 = Convert.ToInt32(tabResult[2]);
                                bool isPair = Convert.ToBoolean(tabResult[3]);
                                bool isPremier = Convert.ToBoolean(tabResult[4]);
                                bool isParfait = Convert.ToBoolean(tabResult[5]);

                                Console.WriteLine($"Result: {result1},val1 {val1}, val2 {val2} IsPair: {isPair}, IsPremier: {isPremier}, IsParfait: {isParfait}");

                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    // Récupération du DbContext
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
                                    
                                // Création d'une nouvelle entité Result
                                var result = new BDD.Models.Result
                                {
                                    ComputedResult = result1,  // Le résultat du calcul
                                    val1 = val1,
                                    val2=val2,
                                    IsPair = isPair,          // Ajout de la vérification si le nombre est pair
                                    IsPremier = isPremier,    // Ajout de la vérification si le nombre est premier
                                    IsParfait = isParfait,    // Ajout de la vérification si le nombre est parfait
                                    Timestamp = DateTime.UtcNow  // Date et heure actuelles
                                };

                                    // Ajout et sauvegarde dans la base de données
                                    dbContext.Results.Add(result);
                                    await dbContext.SaveChangesAsync();
                                }

                                Console.WriteLine($"Résultat reçu et sauvegardé : {data.tab_result}");

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