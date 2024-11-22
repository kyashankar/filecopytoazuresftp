using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;

namespace FileCopyToAzureSFTP.WebAPI.Controllers
{
    [Route(template: "api/[controller]/[Action]")]
    [ApiController]
    public class UploadFileController : ControllerBase
    {
        // SFTP Configuration
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

    }
}
