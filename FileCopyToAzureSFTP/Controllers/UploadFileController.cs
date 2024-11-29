using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Renci.SshNet;

namespace FileCopyToAzureSFTP.WebAPI.Controllers
{
    [Route(template: "api/[controller]/[Action]")]
    [ApiController]
    public class UploadFileController : ControllerBase
    {
        private readonly string _sftpHost = "rpncdevops1.blob.core.windows.net";
        private readonly int _sftpPort = 22; // Default SFTP port
        private readonly string _sftpUsername = "rpncdevops1.sftpadmin";
        private readonly string _sftpPassword = "UcwZIvUIYG13pWEG9gDLuCmrkWb7+lf7"; // Use secure storage for sensitive info
        private readonly string _remoteDirectory = "/";

        [Microsoft.AspNetCore.Mvc.HttpPost]
        public async Task<IActionResult> UploadFileToAzure(IFormFile file)
        {
            try
            {
                // Create an SFTP client instance
                using (var sftp = new SftpClient(_sftpHost, _sftpUsername, _sftpPassword))
                {
                    // Connect to the SFTP server
                    sftp.Connect();

                    // Get the file name from the path
                    string fileName = Path.GetFileName(file.FileName);

                    // Upload the file to the SFTP server
                    using (Stream fileStream = file.OpenReadStream())
                    {
                        sftp.UploadFile(fileStream, $"{_remoteDirectory}/{fileName}");
                    }

                    Console.WriteLine("File uploaded successfully!");

                    // Disconnect from the SFTP server
                    sftp.Disconnect();
                }
                return Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            finally
            {
            }
        }

        [HttpGet]
        public IActionResult GetAllFilesAndFolders(/*[FromQuery] string path = "/"*/)
        {
            var path = "/";
            try
            {
                using (var sftp = new SftpClient(_sftpHost, _sftpPort, _sftpUsername, _sftpPassword))
                {
                    sftp.Connect();

                    // List all files and folders recursively
                    var allFilesAndFolders = new List<string>();
                    TraverseDirectory(sftp, path, allFilesAndFolders);

                    sftp.Disconnect();


                    var result = new Dictionary<string, object>
                    {
                        { "message", "" },
                        { "result", true },
                        { "data", new List<Dictionary<string, string>>() }
                    };

                    // Populate the 'data' list with each file path as "role"
                    foreach (var path1 in allFilesAndFolders)
                    {
                        var fileData = new Dictionary<string, string>
                        {
                            { "role", path1 }
                        };
                        ((List<Dictionary<string, string>>)result["data"]).Add(fileData);
                    }
                    
                    // Convert the result object to JSON
                    string jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);

                    return Ok(jsonResult);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private void TraverseDirectory(SftpClient sftp, string path, List<string> results)
        {
            var filesAndFolders = sftp.ListDirectory(path);

            foreach (var item in filesAndFolders)
            {
                // Skip the current and parent directory references
                if (item.Name == "." || item.Name == "..") continue;

                // Add the full path to the result list
                results.Add(item.FullName);

                // If it's a directory, recursively traverse it
                if (item.IsDirectory)
                {
                    TraverseDirectory(sftp, item.FullName, results);
                }
            }
        }

        [HttpGet("download")]
        public IActionResult DownloadFileFromSFTP(string remoteFilePath)
        {
            try
            {
                var fileStream = DownloadFileFromSFTP1(remoteFilePath);

                return File(fileStream, "application/octet-stream", Path.GetFileName(remoteFilePath));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private Stream DownloadFileFromSFTP1(string remoteFilePath)
        {
            var sftpClient = new SftpClient(_sftpHost, _sftpUsername, _sftpPassword);
            sftpClient.Connect();

            var memoryStream = new MemoryStream();
            sftpClient.DownloadFile(remoteFilePath, memoryStream);
            memoryStream.Position = 0;

            sftpClient.Disconnect();
            return memoryStream;
        }
    }
}
