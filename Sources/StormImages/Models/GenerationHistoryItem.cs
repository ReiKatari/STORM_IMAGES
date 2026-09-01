using System;
using System.Windows.Media.Imaging;

namespace StormImages.Models
{
    public class GenerationHistoryItem
    {
        public string ImagePath { get; set; } = "";
        public string JsonPath { get; set; } = "";
        public string Filename { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double LoRAScale { get; set; } = 0.85;
        public int Steps { get; set; } = 30;
        public double GuidanceScale { get; set; } = 7.5;
        public long Seed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int Width { get; set; }
        public int Height { get; set; }
        public BitmapImage? Thumbnail { get; set; }
    }
}