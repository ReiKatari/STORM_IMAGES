using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormImages.Models;
using StormImages.Services;
using StormImages.Themes;

namespace StormImages.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appTitle = "STORM IMAGES";

        [ObservableProperty]
        private string _appVersion = "0.0.1";

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _activeSection = "Generation";

        [ObservableProperty]
        private BackendStatus _backend = new();

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public GenerationViewModel GenerationVM { get; }
        public GalleryViewModel GalleryVM { get; }
        public TelegramViewModel TelegramVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainViewModel()
        {
            GenerationVM = new GenerationViewModel();
            GalleryVM = new GalleryViewModel();
            TelegramVM = new TelegramViewModel();
            SettingsVM = new SettingsViewModel();

            CurrentView = GenerationVM;

            BackendService.Instance.StatusUpdated += (s, st) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Backend = st;
                });
            };

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await BackendService.Instance.CheckStatusAsync();
            });
        }

        [RelayCommand]
        public void Navigate(string section)
        {
            ActiveSection = section;
            if (section == "Gallery")
            {
                GalleryVM.Refresh();
                CurrentView = GalleryVM;
            }
            else if (section == "Telegram")
            {
                CurrentView = TelegramVM;
            }
            else if (section == "Settings")
            {
                CurrentView = SettingsVM;
            }
            else
            {
                CurrentView = GenerationVM;
            }
        }

        [RelayCommand]
        public async Task RefreshBackendStatus()
        {
            StatusMessage = "Checking backend...";
            var st = await BackendService.Instance.CheckStatusAsync();
            StatusMessage = st.Status == "ready" ? "Backend Online" : (st.Status == "offline" ? "Backend Offline" : "Backend Loading");
        }

        [RelayCommand]
        public void LaunchBackend()
        {
            BackendService.Instance.StartLocalServer();
            StatusMessage = "Starting backend server...";
        }
    }

    public partial class GenerationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _sourceImagePath = "";

        [ObservableProperty]
        private BitmapImage? _sourceImageBitmap;

        [ObservableProperty]
        private BitmapImage? _resultImageBitmap;

        [ObservableProperty]
        private string _resultFilePath = "";

        [ObservableProperty]
        private string _prompt = "beautiful masterpiece, high quality, highly detailed, photorealistic";

        [ObservableProperty]
        private string _negativePrompt = "blurry, low quality, bad anatomy, deformed, distorted, artifacts";

        [ObservableProperty]
        private double _loRAScale = 0.85;

        [ObservableProperty]
        private int _steps = 30;

        [ObservableProperty]
        private double _guidanceScale = 7.5;

        [ObservableProperty]
        private long _seed = -1;

        [ObservableProperty]
        private bool _isGenerating;

        [ObservableProperty]
        private string _generationStatus = "";

        [ObservableProperty]
        private bool _sendToTelegram;

        [ObservableProperty]
        private double _splitRatio = 0.5;

        public GenerationViewModel()
        {
            SendToTelegram = SettingsService.Instance.Settings.AutoSendToTelegram;
        }

        [RelayCommand]
        public void SelectSourceImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*",
                Title = "Select Input Image for Editing"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadImage(dlg.FileName);
            }
        }

        public void LoadImage(string path)
        {
            try
            {
                SourceImagePath = path;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                SourceImageBitmap = bmp;
                ResultImageBitmap = null;
                ResultFilePath = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void AddPromptTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(Prompt))
            {
                Prompt = tag;
            }
            else
            {
                Prompt = Prompt.TrimEnd(',', ' ') + ", " + tag;
            }
        }

        [RelayCommand]
        public void RandomizeSeed()
        {
            Seed = new Random().Next(0, int.MaxValue);
        }

        [RelayCommand]
        public void ResetSeed()
        {
            Seed = -1;
        }

        [RelayCommand]
        public async Task Generate()
        {
            if (string.IsNullOrEmpty(SourceImagePath) || !File.Exists(SourceImagePath))
            {
                MessageBox.Show("Please select an input image first!", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Prompt))
            {
                MessageBox.Show("Please enter a prompt!", "Prompt Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsGenerating = true;
            GenerationStatus = "Processing neural transformation...";

            try
            {
                byte[] bytes = File.ReadAllBytes(SourceImagePath);
                string b64 = Convert.ToBase64String(bytes);

                var settings = SettingsService.Instance.Settings;
                string outDir = settings.OutputDirectory;
                string token = settings.TelegramBotToken;
                string chatId = settings.TelegramChatId;
                string caption = settings.TelegramCaptionTemplate
                    .Replace("{prompt}", Prompt)
                    .Replace("{seed}", Seed.ToString())
                    .Replace("{lora_scale}", LoRAScale.ToString("F2"));

                var result = await BackendService.Instance.EditImageAsync(
                    imageBase64: b64,
                    prompt: Prompt,
                    negativePrompt: NegativePrompt,
                    loraScale: LoRAScale,
                    steps: Steps,
                    guidanceScale: GuidanceScale,
                    seed: Seed,
                    outputDir: outDir,
                    sendToTelegram: SendToTelegram,
                    botToken: token,
                    chatId: chatId,
                    caption: caption
                );

                string? outPath = result["file_path"]?.ToString();
                if (!string.IsNullOrEmpty(outPath) && File.Exists(outPath))
                {
                    ResultFilePath = outPath;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(outPath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    ResultImageBitmap = bmp;

                    double sec = result["generation_time_seconds"]?.ToObject<double>() ?? 0;
                    bool tgDispatched = result["telegram"]?["dispatched"]?.ToObject<bool>() ?? false;
                    string tgNote = tgDispatched ? " | Sent to Telegram" : "";
                    GenerationStatus = $"Completed in {sec:F1}s {tgNote}";
                }
                else
                {
                    GenerationStatus = "Generated successfully";
                }
            }
            catch (Exception ex)
            {
                GenerationStatus = "Generation failed";
                MessageBox.Show($"Generation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        [RelayCommand]
        public void CopyResult()
        {
            if (ResultImageBitmap != null)
            {
                Clipboard.SetImage(ResultImageBitmap);
                MessageBox.Show("Image copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        public void OpenResultFolder()
        {
            string dir = SettingsService.Instance.Settings.OutputDirectory;
            if (Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }

    public partial class GalleryViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<GenerationHistoryItem> _items = new();

        [ObservableProperty]
        private GenerationHistoryItem? _selectedItem;

        [ObservableProperty]
        private BitmapImage? _previewBitmap;

        public GalleryViewModel()
        {
            Refresh();
        }

        [RelayCommand]
        public void Refresh()
        {
            Items.Clear();
            var list = ImageStorageService.Instance.GetRecentImages();
            foreach (var item in list)
            {
                Items.Add(item);
            }
            if (Items.Count > 0)
            {
                SelectedItem = Items[0];
            }
        }

        partial void OnSelectedItemChanged(GenerationHistoryItem? value)
        {
            if (value != null && File.Exists(value.ImagePath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(value.ImagePath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    PreviewBitmap = bmp;
                }
                catch
                {
                    PreviewBitmap = null;
                }
            }
            else
            {
                PreviewBitmap = null;
            }
        }

        [RelayCommand]
        public void DeleteSelected()
        {
            if (SelectedItem != null)
            {
                var r = MessageBox.Show($"Delete {SelectedItem.Filename}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    ImageStorageService.Instance.DeleteImage(SelectedItem);
                    Items.Remove(SelectedItem);
                    SelectedItem = Items.Count > 0 ? Items[0] : null;
                }
            }
        }

        [RelayCommand]
        public void CopySelected()
        {
            if (PreviewBitmap != null)
            {
                Clipboard.SetImage(PreviewBitmap);
                MessageBox.Show("Copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        public async Task SendSelectedToTelegram()
        {
            if (SelectedItem == null || !File.Exists(SelectedItem.ImagePath)) return;

            var settings = SettingsService.Instance.Settings;
            if (string.IsNullOrWhiteSpace(settings.TelegramBotToken) || string.IsNullOrWhiteSpace(settings.TelegramChatId))
            {
                MessageBox.Show("Please configure Telegram Bot Token and Chat ID in Telegram tab first!", "Telegram Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string caption = settings.TelegramCaptionTemplate
                    .Replace("{prompt}", SelectedItem.Prompt)
                    .Replace("{seed}", SelectedItem.Seed.ToString())
                    .Replace("{lora_scale}", SelectedItem.LoRAScale.ToString("F2"));

                await BackendService.Instance.SendToTelegramAsync(
                    settings.TelegramBotToken,
                    settings.TelegramChatId,
                    SelectedItem.ImagePath,
                    caption
                );

                MessageBox.Show("Successfully sent image to Telegram!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send to Telegram: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void OpenFolder()
        {
            string dir = SettingsService.Instance.Settings.OutputDirectory;
            if (Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }

    public partial class TelegramViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _botToken = "";

        [ObservableProperty]
        private string _chatId = "";

        [ObservableProperty]
        private bool _autoSend;

        [ObservableProperty]
        private string _captionTemplate = "";

        [ObservableProperty]
        private bool _isTesting;

        [ObservableProperty]
        private string _testStatus = "";

        [ObservableProperty]
        private bool _isSuccess;

        public TelegramViewModel()
        {
            var s = SettingsService.Instance.Settings;
            BotToken = s.TelegramBotToken;
            ChatId = s.TelegramChatId;
            AutoSend = s.AutoSendToTelegram;
            CaptionTemplate = s.TelegramCaptionTemplate;
        }

        [RelayCommand]
        public void SaveSettings()
        {
            var s = SettingsService.Instance.Settings;
            s.TelegramBotToken = BotToken;
            s.TelegramChatId = ChatId;
            s.AutoSendToTelegram = AutoSend;
            s.TelegramCaptionTemplate = CaptionTemplate;
            SettingsService.Instance.Save();
            MessageBox.Show("Telegram settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        public async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(BotToken) || string.IsNullOrWhiteSpace(ChatId))
            {
                MessageBox.Show("Please enter Bot Token and Chat/Channel ID", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsTesting = true;
            TestStatus = "Verifying bot token and channel access...";
            IsSuccess = false;

            try
            {
                var res = await BackendService.Instance.TestTelegramAsync(BotToken, ChatId);
                string botUser = res["bot_username"]?.ToString() ?? "";
                string title = res["chat_title"]?.ToString() ?? "";
                TestStatus = $"Connected! Bot: @{botUser} -> Target: {title}";
                IsSuccess = true;
                SaveSettings();
            }
            catch (Exception ex)
            {
                TestStatus = $"Error: {ex.Message}";
                IsSuccess = false;
            }
            finally
            {
                IsTesting = false;
            }
        }
    }

    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _outputDirectory = "";

        [ObservableProperty]
        private ThemeType _selectedTheme;

        [ObservableProperty]
        private string _backendUrl = "http://127.0.0.1:7860";

        [ObservableProperty]
        private bool _autoStartBackend = true;

        public ObservableCollection<ThemeType> AvailableThemes { get; } = new()
        {
            ThemeType.StormMidnight,
            ThemeType.StormNight,
            ThemeType.StormDay
        };

        public SettingsViewModel()
        {
            var s = SettingsService.Instance.Settings;
            OutputDirectory = s.OutputDirectory;
            SelectedTheme = s.Theme;
            BackendUrl = s.BackendUrl;
            AutoStartBackend = s.AutoStartBackend;
        }

        [RelayCommand]
        public void BrowseOutputDirectory()
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Select Folder for Generated Images",
                InitialDirectory = OutputDirectory
            };

            if (dlg.ShowDialog() == true)
            {
                OutputDirectory = dlg.FolderName;
                SaveSettings();
            }
        }

        partial void OnSelectedThemeChanged(ThemeType value)
        {
            ThemeManager.Instance.ApplyTheme(value, Application.Current.MainWindow);
        }

        [RelayCommand]
        public void SaveSettings()
        {
            var s = SettingsService.Instance.Settings;
            s.OutputDirectory = OutputDirectory;
            s.Theme = SelectedTheme;
            s.BackendUrl = BackendUrl;
            s.AutoStartBackend = AutoStartBackend;
            SettingsService.Instance.Save();
        }

        [RelayCommand]
        public void OpenOutputFolder()
        {
            if (Directory.Exists(OutputDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", OutputDirectory);
            }
        }
    }
}