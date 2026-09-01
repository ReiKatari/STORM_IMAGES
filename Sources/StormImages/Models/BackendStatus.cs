namespace StormImages.Models
{
    public class BackendStatus
    {
        public string Status { get; set; } = "offline";
        public string BaseModel { get; set; } = "Qwen/Qwen-Image-Edit-2511";
        public string LoRAName { get; set; } = "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora";
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
        public string? LastError { get; set; }
        public HardwareInfo? Hardware { get; set; }
    }

    public class HardwareInfo
    {
        public bool CudaAvailable { get; set; }
        public string DeviceName { get; set; } = "CPU";
        public double TotalVramGb { get; set; }
        public double FreeVramGb { get; set; }
        public double UsedVramGb { get; set; }
    }
}