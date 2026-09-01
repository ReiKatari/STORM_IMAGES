using Newtonsoft.Json;
using System.IO;

namespace StormImages.Models
{
    public class BackendStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "offline";

        [JsonProperty("base_model")]
        public string BaseModel { get; set; } = "Qwen/Qwen-Image-Edit-2511";

        [JsonProperty("lora_name")]
        public string LoRAName { get; set; } = "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora";

        [JsonProperty("is_loaded")]
        public bool IsLoaded { get; set; }

        [JsonProperty("is_loading")]
        public bool IsLoading { get; set; }

        [JsonProperty("last_error")]
        public string? LastError { get; set; }

        [JsonProperty("hardware")]
        public HardwareInfo? Hardware { get; set; }

        [JsonIgnore]
        public bool IsOnline => !string.Equals(Status, "offline", System.StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public string Device => Hardware?.DeviceName ?? "CPU";

        [JsonIgnore]
        public string Model => string.IsNullOrEmpty(BaseModel) ? "Qwen-Image-Edit" : Path.GetFileName(BaseModel);

        [JsonIgnore]
        public double VramUsedGb => Hardware?.UsedVramGb ?? 0.0;
    }

    public class HardwareInfo
    {
        [JsonProperty("cuda_available")]
        public bool CudaAvailable { get; set; }

        [JsonProperty("device_name")]
        public string DeviceName { get; set; } = "CPU";

        [JsonProperty("total_vram_gb")]
        public double TotalVramGb { get; set; }

        [JsonProperty("free_vram_gb")]
        public double FreeVramGb { get; set; }

        [JsonProperty("used_vram_gb")]
        public double UsedVramGb { get; set; }
    }
}