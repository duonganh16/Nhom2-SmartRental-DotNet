
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartRental.Desktop
{
    public class DesktopHost
    {
        private Process? _serverProcess;

        private const string AppUrl = "http://127.0.0.1:5088";

        public async Task StartAsync()
        {
            string basePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", ".."));

            string webProjectPath = Path.Combine(
                basePath, "RoomRentalManagement");

            string projectFile = Path.Combine(
                webProjectPath,
                "RoomRentalManagement.csproj");

            if (!File.Exists(projectFile))
            {
                throw new FileNotFoundException(
                    "Không tìm thấy dự án RoomRentalManagement.",
                    projectFile);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = webProjectPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectFile);
            startInfo.ArgumentList.Add("--no-launch-profile");
            startInfo.ArgumentList.Add("--urls");
            startInfo.ArgumentList.Add(AppUrl);

            _serverProcess = Process.Start(startInfo);

            if (_serverProcess == null)
                throw new Exception("Không thể khởi động SmartRental.");

            using var client = new HttpClient();

            for (int i = 0; i < 60; i++)
            {
                if (_serverProcess.HasExited)
                    throw new Exception("Máy chủ SmartRental đã dừng.");

                try
                {
                    var response = await client.GetAsync(AppUrl);
                    if (response.IsSuccessStatusCode ||
                        (int)response.StatusCode < 500)
                        return;
                }
                catch
                {
                    await Task.Delay(500);
                }
            }

            throw new TimeoutException(
                "SmartRental khởi động quá lâu.");
        }

        public void Stop()
        {
            if (_serverProcess != null &&
                !_serverProcess.HasExited)
            {
                _serverProcess.Kill(true);
                _serverProcess.Dispose();
            }
        }
    }
}
