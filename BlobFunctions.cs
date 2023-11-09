using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

public static class BlobFunctions
{
    private static readonly string storageConnectionString = Environment.GetEnvironmentVariable("MyBlobStorageConnection", EnvironmentVariableTarget.Process);

    [FunctionName("GetBlobPayload")]
    public static async Task<IActionResult> GetBlobPayload(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payload/{timestamp}")] HttpRequest req,
        string timestamp,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function to fetch blob payload.");

        var blobServiceClient = new BlobServiceClient(storageConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("payloads");

        var blobName = $"payload_{timestamp}.json";
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        if (await blobClient.ExistsAsync())
        {
            var blobDownloadInfo = await blobClient.DownloadAsync();

            using (var streamReader = new StreamReader(blobDownloadInfo.Value.Content))
            {
                string payload = await streamReader.ReadToEndAsync();
                return new OkObjectResult(payload);
            }
        }
        else
        {
            return new NotFoundResult();
        }
    }
}
