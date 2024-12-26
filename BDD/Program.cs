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
                webBuilder.UseUrls("http://0.0.0.0:5225");

                webBuilder.ConfigureServices(services =>
                {
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

                    services.AddDbContext<ResultsDbContext>(options =>
                        options.UseSqlServer("Server=sqlserver,1433;Database=ResultsDb;User Id=sa;Password=YourStrong!Password123;"));

                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Endpoint pour la racine
                        endpoints.MapGet("/", async context =>
                        {
                            await context.Response.WriteAsync("Je suis la racine");
                        });

                        // Endpoint pour vérifier la connexion avec la base de données
                        endpoints.MapGet("/check_db_connection", async context =>
                        {
                            try
                            {
                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
                                    var canConnect = await dbContext.Database.CanConnectAsync();

                                    if (canConnect)
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                                        await context.Response.WriteAsync("Connexion à la base de données réussie.");
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                        await context.Response.WriteAsync("Impossible de se connecter à la base de données.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                await context.Response.WriteAsync($"Erreur lors de la vérification de la connexion à la base de données : {ex.Message}");
                            }
                        });
                       
                        // Endpoints qui permet d'ajouter la suite de syracuse dans un bucket s3
                        endpoints.MapPost("/upload_numbers", async context =>
                        {
                            try
                            {
                                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                                var numbers = JsonConvert.DeserializeObject<List<int>>(body);

                                if (numbers == null || numbers.Count == 0)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    await context.Response.WriteAsync("La liste des nombres est vide ou invalide.");
                                    return;
                                }

                                var uniqueId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                                string objectName = $"numbers_list_{uniqueId}";

                                var minioService = context.RequestServices.GetRequiredService<MinioService>();
                                await minioService.UploadNumbersDirectAsync(objectName, numbers);

                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                await context.Response.WriteAsync($"Suite de nombres téléversée avec succès ! Nom de l'objet : {objectName}");
                            }
                            catch (JsonException jsonEx)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await context.Response.WriteAsync("Erreur de format JSON : " + jsonEx.Message);
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                await context.Response.WriteAsync($"Erreur interne du serveur : {ex.Message}");
                            }
                        });

                        // Fonction qui récupère les résultas des calculs venant de py et les stocke en bdd
                        endpoints.MapPost("/receive_result", async context =>
                        {
                            try
                            {
                                var requestBody = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
                                var data = JsonConvert.DeserializeObject<dynamic>(requestBody);

                                var tabResult = data.tab_result.ToObject<List<object>>();
                                int result1 = Convert.ToInt32(tabResult[0]);
                                int val1 = Convert.ToInt32(tabResult[1]);
                                int val2 = Convert.ToInt32(tabResult[2]);
                                bool isPair = Convert.ToBoolean(tabResult[3]);
                                bool isPremier = Convert.ToBoolean(tabResult[4]);
                                bool isParfait = Convert.ToBoolean(tabResult[5]);

                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
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

                                    dbContext.Results.Add(result);
                                    await dbContext.SaveChangesAsync();
                                }

                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                await context.Response.WriteAsync("Résultat reçu et sauvegardé !");
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await context.Response.WriteAsync($"Erreur : {ex.Message}");
                            }
                        });

                        // Endpoint qui permet de récupérer les resultats stockés en bdd
                        endpoints.MapGet("/get_results", async context =>
                        {
                            try
                            {
                                using (var scope = app.ApplicationServices.CreateScope())
                                {
                                    var dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
                                    var results = await dbContext.Results.ToListAsync();

                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(JsonConvert.SerializeObject(results));
                                }
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await context.Response.WriteAsync($"Erreur : {ex.Message}");
                            }
                        });

                    });
                });
            });
}
