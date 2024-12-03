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
using BDD.Services;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    // Ajout de MinioService avec la configuration
                    services.AddSingleton<MinioService>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var minioConfig = configuration.GetSection("Minio");
                        return new MinioService(
                            minioConfig["Endpoint"],
                            minioConfig["AccessKey"],
                            minioConfig["SecretKey"],
                            minioConfig["BucketName"]);
                    });

                    // Configuration du DbContext pour SQL Server
                    services.AddDbContext<ResultsDbContext>(options =>
                        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ResultsDb;Trusted_Connection=True;"));
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Endpoint pour téléverser une liste de nombres dans MinIO
                        endpoints.MapPost("/upload_numbers", async context =>
                        {
                            try
                            {
                                // Résolution du service MinioService
                                var minioService = context.RequestServices.GetRequiredService<MinioService>();

                                // Génération de la liste de nombres
                                List<int> numbers = new List<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };

                                // Nom de l'objet dans le bucket
                                string objectName = "numbers_list";

                                // Téléversement dans MinIO
                                await minioService.UploadNumbersDirectAsync(objectName, numbers);

                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                await context.Response.WriteAsync("Suite de nombres téléversée avec succès !");
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                await context.Response.WriteAsync($"Erreur : {ex.Message}");
                            }
                        });

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
                                        ComputedResult = result1,
                                        val1 = val1,
                                        val2 = val2,
                                        IsPair = isPair,
                                        IsPremier = isPremier,
                                        IsParfait = isParfait,
                                        Timestamp = DateTime.UtcNow
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
