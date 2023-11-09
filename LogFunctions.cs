using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Globalization;

namespace FunctionApp1
{
    public static class LogFunctions
    {
        private static readonly string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
        [FunctionName("GetLogs")]
        public static async Task<IActionResult> GetLogs(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "logs/{from}/{to}")] HttpRequest req,
            string from,
            string to,
            ILogger log)
        {
            DateTime fromDate;
            DateTime toDate;

            if (!DateTime.TryParseExact(from, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate) ||
                !DateTime.TryParseExact(to, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
            {
                return new BadRequestObjectResult("Invalid date format. Please use yyyyMMdd format.");
            }

            var tableClient = new TableClient(storageConnectionString, "ApiFetchLogs");
            await tableClient.CreateIfNotExistsAsync();

            string filter = TableClient.CreateQueryFilter($"Timestamp ge {fromDate:O} and Timestamp le {toDate:O}");
            var pages = tableClient.QueryAsync<LogEntity>(filter);

            var logs = await pages.AsPages().FirstOrDefaultAsync();


            return new OkObjectResult(logs.Values);
        }
    }

}
