using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace BlazorWebApp.Services
{
    public class FileService
    {
        private readonly string _basePath;

        public FileService(IWebHostEnvironment env)
        {
            _basePath = Path.Combine(env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(_basePath);
        }

        public List<string?> GetExistingClients()
        {
            if (!Directory.Exists(_basePath))
                return new List<string?>();

            return Directory.GetDirectories(_basePath)
                            .Select(Path.GetFileName)
                            .OrderBy(x => x)
                            .ToList();
        }

        public List<(string ClientName, string FileName)> GetAllFiles()
        {
            var list = new List<(string ClientName, string FileName)>();
            // Order by Directory Name (Customer Name)
            foreach (var clientDir in Directory.GetDirectories(_basePath).OrderBy(dir => dir))
            {
                string clientName = Path.GetFileName(clientDir);

                foreach (var file in Directory.GetFiles(clientDir))
                {
                    list.Add((clientName, Path.GetFileName(file)));
                }
            }

            return list;
        }

        public void BackupFileFromDir(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer directory cannot be empty.", nameof(customerName));

            string customerNameDir = Path.Combine(_basePath, customerName);
            if (!Directory.Exists(customerNameDir))
                return;

            string backupDir = Path.Combine(customerNameDir, "Backup");
            // Ensure backup directory exists
            Directory.CreateDirectory(backupDir);
            // Get only files directly under the directory (not subdirectories)
            var files = Directory.GetFiles(customerNameDir);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string name = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                string ts = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string destPath = Path.Combine(backupDir, $"{name}_{ts}{ext}");

                // Retry logic: sometimes file is still locked by another process
                const int maxRetries = 5;
                const int delayMs = 200;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        File.Move(file, destPath);
                        break; // success
                    }
                    catch (IOException)
                    {
                        if (attempt == maxRetries)
                            throw; // give up

                        // wait before retry
                        Thread.Sleep(delayMs);
                    }
                }
            }
        }

        public async Task SaveClientXmlAsync(string clientName, IBrowserFile file)
        {
            BackupFileFromDir(clientName);
            string clientDir = Path.Combine(_basePath, clientName);
            Directory.CreateDirectory(clientDir);

            string filePath = Path.Combine(clientDir, file.Name);

            using var fs = new FileStream(filePath, FileMode.Create);
            using var stream = file.OpenReadStream(long.MaxValue);
            await stream.CopyToAsync(fs);
        }

        public bool DeleteFile(string client, string file)
        {
            try
            {
                var folder = Path.Combine(_basePath, client);
                var filePath = Path.Combine(folder, file);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

    }
}
