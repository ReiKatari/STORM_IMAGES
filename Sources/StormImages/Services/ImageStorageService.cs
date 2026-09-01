using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using StormImages.Models;

namespace StormImages.Services
{
    public class ImageStorageService
    {
        private static ImageStorageService? _instance;
        public static ImageStorageService Instance => _instance ??= new ImageStorageService();

        public List<GenerationHistoryItem> GetRecentImages()
        {
            var list = new List<GenerationHistoryItem>();
            string dir = SettingsService.Instance.Settings.OutputDirectory;

            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { return list; }
            }

            var imageFiles = Directory.GetFiles(dir, "STORM_IMG_*.png")
                                      .OrderByDescending(File.GetCreationTime)
                                      .ToList();

            foreach (var imgPath in imageFiles)
            {
                var item = new GenerationHistoryItem
                {
                    ImagePath = imgPath,
                    Filename = Path.GetFileName(imgPath),
                    CreatedAt = File.GetCreationTime(imgPath)
                };

                string jsonPath = Path.ChangeExtension(imgPath, ".json");
                if (File.Exists(jsonPath))
                {
                    item.JsonPath = jsonPath;
                    try
                    {
                        string json = File.ReadAllText(jsonPath);
                        var obj = JObject.Parse(json);
                        item.Prompt = obj["prompt"]?.ToString() ?? "";
                        item.NegativePrompt = obj["negative_prompt"]?.ToString() ?? "";
                        item.LoRAScale = obj["lora_scale"]?.ToObject<double>() ?? 0.85;
                        item.Steps = obj["steps"]?.ToObject<int>() ?? 30;
                        item.GuidanceScale = obj["guidance_scale"]?.ToObject<double>() ?? 7.5;
                        item.Seed = obj["seed"]?.ToObject<long>() ?? 0;
                        item.Width = obj["width"]?.ToObject<int>() ?? 0;
                        item.Height = obj["height"]?.ToObject<int>() ?? 0;
                    }
                    catch { }
                }

                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(imgPath);
                    bmp.DecodePixelWidth = 240;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    item.Thumbnail = bmp;
                }
                catch { }

                list.Add(item);
            }

            return list;
        }

        public void DeleteImage(GenerationHistoryItem item)
        {
            try
            {
                if (File.Exists(item.ImagePath)) File.Delete(item.ImagePath);
                if (File.Exists(item.JsonPath)) File.Delete(item.JsonPath);
            }
            catch { }
        }
    }
}