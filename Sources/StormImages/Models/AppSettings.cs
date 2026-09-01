using System;
using System.IO;

namespace StormImages.Models
{
    public class AppSettings
    {
        public ThemeType Theme { get; set; } = ThemeType.StormMidnight;
        
        public string OutputDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "STORM_IMAGES"
        );

        public bool AutoSave { get; set; } = true;
        public string BackendUrl { get; set; } = "http://127.0.0.1:7860";
        public bool AutoStartBackend { get; set; } = true;

        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatId { get; set; } = "";
        public bool AutoSendToTelegram { get; set; } = false;
        public string TelegramCaptionTemplate { get; set; } = "⚡ *STORM IMAGES 0.0.1*\n🎨 *Prompt*: {prompt}\n🎲 *Seed*: `{seed}`\n✨ *LoRA Scale*: `{lora_scale}`";
    }
}