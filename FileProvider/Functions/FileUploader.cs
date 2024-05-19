using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;

namespace FileProvider.Functions
{
    public class FileUploader(ILogger<FileUploader> logger)
    {
        private readonly ILogger<FileUploader> _logger = logger;

        [Function("FileUploader")]

        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {

                if (req.Form.Files["file"] is IFormFile file)
                {
                    string connectionString = Environment.GetEnvironmentVariable("FileStorageAccount")!;
                    string containerName = Environment.GetEnvironmentVariable("FileContainerName")!;

                    BlobServiceClient client = new BlobServiceClient(connectionString);
                    BlobContainerClient container = client.GetBlobContainerClient(containerName);
                    await container.CreateIfNotExistsAsync();

                    string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    BlobClient blob = container.GetBlobClient(fileName);

                    var headers = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    };

                    using (Stream stream = file.OpenReadStream())
                    {
                        await blob.UploadAsync(stream, headers);
                    }

                    return new OkObjectResult(blob.Uri);
                }

                return new BadRequestObjectResult("No file uploaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return new BadRequestResult();
        }
    }
}


