using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Azure;

namespace FunctionApp1
{
    public static class Function1
    {
        private static HttpClient httpClient = new HttpClient();
        private static readonly string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);

        [FunctionName("FetchAndStoreDataFunction")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string apiUrl = "https://api.publicapis.org/random?auth=null";
            log.LogInformation($"C# Timer trigger function started at: {DateTime.Now}");

            try
            {
                var response = await httpClient.GetStringAsync(apiUrl);

                await LogToTable(true, "Success", DateTime.Now);

                await StoreToBlob(response, DateTime.Now);

                log.LogInformation($"Data fetched and stored successfully at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await LogToTable(false, ex.Message, DateTime.Now);
                log.LogError($"Error occurred: {ex.Message}");
            }
        }

        private static async Task LogToTable(bool isSuccess, string message, DateTime timestamp)
        {
            var tableClient = new TableClient(storageConnectionString, "ApiFetchLogs");
            await tableClient.CreateIfNotExistsAsync();

            var logEntity = new LogEntity
            {
                PartitionKey = isSuccess ? "Success" : "Failure",
                RowKey = timestamp.ToString("yyyyMMddHHmmss"),
                Message = message,
                Timestamp = timestamp
            };

            await tableClient.AddEntityAsync(logEntity);
        }

        private static async Task StoreToBlob(string payload, DateTime timestamp)
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("payloads");
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient($"payload_{timestamp:yyyyMMddHHmmss}.json");
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }

    public class LogEntity : ITableEntity
    {
        public string Message { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
