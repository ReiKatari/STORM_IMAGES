using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StormImages.Models;

namespace StormImages.Services
{
    public class BackendService
    {
        private static BackendService? _instance;
        public static BackendService Instance => _instance ??= new BackendService();

        private readonly HttpClient _http;
        private readonly HttpClient _longHttp;
        private Process? _serverProcess;
        private System.Threading.Timer? _pollTimer;

        public event EventHandler<BackendStatus>? StatusUpdated;

        private BackendService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            _longHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

            _pollTimer = new System.Threading.Timer(async _ =>
            {
                await CheckStatusAsync();
            }, null, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(3));
        }

        public async Task<BackendStatus> CheckStatusAsync()
        {
            string url = SettingsService.Instance.Settings.BackendUrl.TrimEnd('/') + "/v1/status";
            try
            {
                var resp = await _http.GetAsync(url);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<BackendStatus>(json);
                    if (status != null)
                    {
                        StatusUpdated?.Invoke(this, status);
                        return status;
                    }
                }
            }
            catch { }

            var offline = new BackendStatus { Status = "offline" };
            StatusUpdated?.Invoke(this, offline);
            return offline;
        }

        public async Task<JObject> LoadModelAsync(string baseModel, string? loraPath)
        {
            string url = SettingsService.Instance.Settings.BackendUrl.TrimEnd('/') + "/v1/model/load";
            string query = $"?base_model={Uri.EscapeDataString(baseModel)}";
            if (!string.IsNullOrEmpty(loraPath))
            {
                query += $"&lora_path={Uri.EscapeDataString(loraPath)}";
            }
            var resp = await _longHttp.PostAsync(url + query, null);
            string respContent = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to load model: {respContent}");
            }
            return JObject.Parse(respContent);
        }

        public async Task<JObject> EditImageAsync(
            string imageBase64,
            string prompt,
            string negativePrompt,
            double loraScale,
            int steps,
            double guidanceScale,
            long seed,
            string outputDir,
            bool sendToTelegram,
            string botToken,
            string chatId,
            string caption)
        {
            string url = SettingsService.Instance.Settings.BackendUrl.TrimEnd('/') + "/v1/edit";
            var payload = new
            {
                image_base64 = imageBase64,
                prompt = prompt,
                negative_prompt = negativePrompt,
                lora_scale = loraScale,
                steps = steps,
                guidance_scale = guidanceScale,
                seed = seed,
                output_dir = outputDir,
                send_to_telegram = sendToTelegram,
                telegram_bot_token = botToken,
                telegram_chat_id = chatId,
                telegram_caption = caption
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _longHttp.PostAsync(url, content);

            string respContent = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Server error ({resp.StatusCode}): {respContent}");
            }

            return JObject.Parse(respContent);
        }

        public async Task<JObject> TestTelegramAsync(string botToken, string chatId)
        {
            string url = SettingsService.Instance.Settings.BackendUrl.TrimEnd('/') + "/v1/telegram/test";
            var payload = new { bot_token = botToken, chat_id = chatId };
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(url, content);
            string respContent = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Telegram verification error: {respContent}");
            }

            return JObject.Parse(respContent);
        }

        public async Task<JObject> SendToTelegramAsync(string botToken, string chatId, string imagePath, string caption)
        {
            string url = SettingsService.Instance.Settings.BackendUrl.TrimEnd('/') + "/v1/telegram/send";
            var payload = new
            {
                bot_token = botToken,
                chat_id = chatId,
                image_path = imagePath,
                caption = caption
            };
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(url, content);
            string respContent = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Telegram dispatch error: {respContent}");
            }

            return JObject.Parse(respContent);
        }

        public void StartLocalServer()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] potentialBatPaths = new string[]
                {
                    Path.Combine(baseDir, "StormImagesServer", "run_server.bat"),
                    Path.Combine(baseDir, "run_server.bat"),
                    Path.Combine(baseDir, "Sources", "StormImagesServer", "run_server.bat"),
                    Path.Combine(baseDir, "..", "StormImagesServer", "run_server.bat"),
                    Path.Combine(baseDir, "..", "Sources", "StormImagesServer", "run_server.bat"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "Sources", "StormImagesServer", "run_server.bat"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "StormImagesServer", "run_server.bat"),
                    @"E:\STORM IMAGES\Sources\StormImagesServer\run_server.bat",
                    @"E:\STORM IMAGES\Assembling\StormImagesServer\run_server.bat",
                    @"C:\Program Files\STORM IMAGES\StormImagesServer\run_server.bat",
                    @"C:\Program Files (x86)\STORM IMAGES\StormImagesServer\run_server.bat"
                };

                string? foundScript = null;
                foreach (var p in potentialBatPaths)
                {
                    if (File.Exists(p))
                    {
                        foundScript = Path.GetFullPath(p);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(foundScript))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"STORM IMAGES Server\" \"{foundScript}\"",
                        WorkingDirectory = Path.GetDirectoryName(foundScript),
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    _serverProcess = Process.Start(psi);
                }
                else
                {
                    // Direct python / uv fallback
                    string[] potentialPyPaths = new string[]
                    {
                        Path.Combine(baseDir, "StormImagesServer", "app.py"),
                        Path.Combine(baseDir, "app.py"),
                        Path.Combine(baseDir, "Sources", "StormImagesServer", "app.py"),
                        Path.Combine(baseDir, "..", "StormImagesServer", "app.py"),
                        Path.Combine(baseDir, "..", "..", "..", "..", "Sources", "StormImagesServer", "app.py"),
                        @"E:\STORM IMAGES\Sources\StormImagesServer\app.py",
                        @"E:\STORM IMAGES\Assembling\StormImagesServer\app.py"
                    };

                    string? foundPy = null;
                    foreach (var p in potentialPyPaths)
                    {
                        if (File.Exists(p))
                        {
                            foundPy = Path.GetFullPath(p);
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(foundPy))
                    {
                        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string uvPath = Path.Combine(userProfile, ".local", "bin", "uv.exe");
                        if (!File.Exists(uvPath))
                        {
                            uvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "uv", "uv.exe");
                        }

                        if (File.Exists(uvPath))
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = uvPath,
                                Arguments = $"run --with fastapi,uvicorn,httpx,pillow,pydantic,diffusers,torch python -m uvicorn app:app --host 127.0.0.1 --port 7860",
                                WorkingDirectory = Path.GetDirectoryName(foundPy),
                                UseShellExecute = true,
                                CreateNoWindow = false
                            };
                            _serverProcess = Process.Start(psi);
                        }
                    }
                }

                // Trigger rapid health checks after launching
                Task.Run(async () =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        await Task.Delay(1500);
                        var st = await CheckStatusAsync();
                        if (st.Status != "offline") break;
                    }
                });
            }
            catch { }
        }

        public void StopLocalServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(true);
                }
            }
            catch { }
        }
    }
}