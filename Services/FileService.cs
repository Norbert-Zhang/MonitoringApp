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

        public async Task SaveClientXmlAsync(string clientName, IBrowserFile file)
        {
            string clientDir = Path.Combine(_basePath, clientName);
            Directory.CreateDirectory(clientDir);

            string filePath = Path.Combine(clientDir, file.Name);

            using var fs = new FileStream(filePath, FileMode.Create);
            using var stream = file.OpenReadStream(long.MaxValue);
            await stream.CopyToAsync(fs);
        }

        public List<(string ClientName, string FileName)> GetAllFiles()
        {
            var list = new List<(string ClientName, string FileName)>();

            foreach (var clientDir in Directory.GetDirectories(_basePath))
            {
                string clientName = Path.GetFileName(clientDir);

                foreach (var file in Directory.GetFiles(clientDir))
                {
                    list.Add((clientName, Path.GetFileName(file)));
                }
            }

            return list;
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
