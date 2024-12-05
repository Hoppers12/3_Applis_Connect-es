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
                            minioConfig["Endpoint"]!,
                            minioConfig["AccessKey"]!,
                            minioConfig["SecretKey"]!,
                            minioConfig["BucketName"]!);
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
                        // Endpoint pour téléverser une liste de nombres dans MinIO (syracuse ici)
            endpoints.MapPost("/upload_numbers", async context =>
                {
                    Console.WriteLine("Entrée dans l'endpoint /upload_numbers");
                    try
                    {
                        // Étape 1 : Lire le corps de la requête
                        Console.WriteLine("[DEBUG] Lecture du corps de la requête...");
                        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                        Console.WriteLine($"[DEBUG] Contenu brut du body : {body}");

                        // Étape 2 : Désérialiser en liste d'entiers
                        var numbers = JsonConvert.DeserializeObject<List<int>>(body);
                        Console.WriteLine($"[DEBUG] Nombres désérialisés : {string.Join(", ", numbers ?? new List<int>())}");

                        // Étape 3 : Validation de la liste
                        if (numbers == null || numbers.Count == 0)
                        {
                            Console.WriteLine("[ERREUR] Liste de nombres vide ou invalide.");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            await context.Response.WriteAsync("La liste des nombres est vide ou invalide.");
                            return;
                        }

                        // Étape 4 : Générer un nom unique pour l'objet
                        var uniqueId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");  // Utilise la date et l'heure actuelle pour générer un identifiant unique
                        string objectName = $"numbers_list_{uniqueId}";  // Exemple : "numbers_list_20241205123045999"

                        // Étape 5 : Utiliser le service MinIO pour téléverser les données
                        var minioService = context.RequestServices.GetRequiredService<MinioService>();
                        Console.WriteLine("[DEBUG] Téléversement des données dans MinIO...");
                        await minioService.UploadNumbersDirectAsync(objectName, numbers);
                        Console.WriteLine("[DEBUG] Téléversement réussi.");

                        // Réponse OK
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteAsync($"Suite de nombres téléversée avec succès ! Nom de l'objet : {objectName}");
                    }
                    catch (JsonException jsonEx)
                    {
                        // Spécifique aux erreurs de désérialisation
                        Console.WriteLine($"[ERREUR] Erreur de désérialisation JSON : {jsonEx.Message}");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync("Erreur de format JSON : " + jsonEx.Message);
                    }
                    catch (Exception ex)
                    {
                        // Erreurs générales
                        Console.WriteLine($"[ERREUR] Erreur lors du téléversement : {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync($"Erreur interne du serveur : {ex.Message}");
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
