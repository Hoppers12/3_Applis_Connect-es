using Minio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BDD.Models;

namespace BDD.Services
{
    public class MinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioService(string endpoint, string accessKey, string secretKey, string bucketName)
        {
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
            _bucketName = bucketName;
        }

        public async Task UploadNumbersDirectAsync(string objectName, IEnumerable<int> numbers)
        {
            // Conversion des nombres en chaîne
            var numbersString = string.Join(",", numbers);
            var byteArray = Encoding.UTF8.GetBytes(numbersString);

            // Préparation du flux
            using var memoryStream = new MemoryStream(byteArray);

            // Assurez-vous que le bucket existe
            bool bucketExists = await _minioClient.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(_bucketName));
            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(_bucketName));
            }

            // Téléversement
            await _minioClient.PutObjectAsync(new Minio.DataModel.Args.PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(memoryStream)
                .WithObjectSize(memoryStream.Length)
                .WithContentType("text/plain"));
        }

        public async Task<List<int>> DownloadNumbersDirectAsync(string objectName)
        {
            MemoryStream memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(new Minio.DataModel.Args.GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                }));

            // Lire le flux et convertir en liste
            var numbersString = Encoding.UTF8.GetString(memoryStream.ToArray());
            return numbersString.Split(',').Select(int.Parse).ToList();
        }
    }
}
