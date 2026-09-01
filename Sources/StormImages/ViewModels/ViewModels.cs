using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormImages.Controls;
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
        private string _appVersion = "0.0.2";

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _activeSection = "Generation";

        [ObservableProperty]
        private BackendStatus _backend = new();

        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        public LocalizationManager Loc => LocalizationManager.Instance;

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
            StatusMessage = Loc["ModelStatusLoading"];
            var st = await BackendService.Instance.CheckStatusAsync();
            StatusMessage = st.Status == "ready" ? Loc["ModelStatusReady"] : Loc["ModelStatusIdle"];
        }

        [RelayCommand]
        public void LaunchBackend()
        {
            BackendService.Instance.StartLocalServer();
            StatusMessage = Loc["ModelStatusLoading"];
        }
    }

    public partial class GenerationViewModel : ObservableObject
    {
        public LocalizationManager Loc => LocalizationManager.Instance;

        [ObservableProperty]
        private bool _isTextToImageMode = true;

        [ObservableProperty]
        private string _selectedBaseModel = "Qwen/Qwen-Image-Edit-2511";

        [ObservableProperty]
        private string _selectedLoRA = "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora";

        [ObservableProperty]
        private string _modelLoadStatus = "";

        [ObservableProperty]
        private bool _isModelLoading;

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

        public ObservableCollection<string> AvailableBaseModels { get; } = new()
        {
            "Qwen/Qwen-Image-Edit-2511",
            "Qwen/Qwen-Image-Edit",
            "black-forest-labs/FLUX.1-schnell",
            "stabilityai/stable-diffusion-xl-base-1.0"
        };

        public ObservableCollection<string> AvailableLoRAs { get; } = new()
        {
            "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora",
            "None (Без LoRA)"
        };

        public GenerationViewModel()
        {
            var s = SettingsService.Instance.Settings;
            SendToTelegram = s.AutoSendToTelegram;
            SelectedBaseModel = s.SelectedBaseModel;
            SelectedLoRA = s.SelectedLoRA;
            IsTextToImageMode = s.IsTextToImageMode;
        }

        [RelayCommand]
        public async Task LoadModel()
        {
            var st = await BackendService.Instance.CheckStatusAsync();
            if (st.Status == "offline")
            {
                StormMessageBox.Show(
                    "Локальный AI-сервер в данный момент отключен.\nПожалуйста, запустите сервер с помощью кнопки «Запустить сервер» в нижней части бокового меню.",
                    Loc["ModelSelectionTitle"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ModelLoadStatus = Loc["ModelStatusIdle"];
                return;
            }

            IsModelLoading = true;
            ModelLoadStatus = Loc["ModelStatusLoading"];

            try
            {
                string? lora = SelectedLoRA.StartsWith("None") ? null : SelectedLoRA;
                await BackendService.Instance.LoadModelAsync(SelectedBaseModel, lora);
                ModelLoadStatus = Loc["ModelStatusReady"];
                StormMessageBox.Show(Loc["ModelStatusReady"], Loc["ModelSelectionTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ModelLoadStatus = ex.Message;
                StormMessageBox.Show(ex.Message, Loc["ModelSelectionTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsModelLoading = false;
            }
        }

        [RelayCommand]
        public void SelectSourceImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*",
                Title = Loc["InputImageTitle"]
            };

            if (dlg.ShowDialog() == true)
            {
                LoadImage(dlg.FileName);
            }
        }

        [RelayCommand]
        public void ClearSourceImage()
        {
            SourceImagePath = "";
            SourceImageBitmap = null;
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
                StormMessageBox.Show(ex.Message, Loc["AppTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
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
            // If in Image Edit mode, require an input image. In Text-to-Image mode, image is NOT required!
            if (!IsTextToImageMode && (string.IsNullOrEmpty(SourceImagePath) || !File.Exists(SourceImagePath)))
            {
                StormMessageBox.Show(Loc["MsgInputRequiredText"], Loc["MsgInputRequiredTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var st = await BackendService.Instance.CheckStatusAsync();
            if (st.Status == "offline")
            {
                StormMessageBox.Show(
                    "Локальный AI-сервер в данный момент отключен.\nПожалуйста, запустите сервер с помощью кнопки «Запустить сервер» в нижней части бокового меню перед началом генерации.",
                    Loc["AppTitle"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Prompt))
            {
                StormMessageBox.Show(Loc["MsgPromptRequiredText"], Loc["MsgPromptRequiredTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsGenerating = true;
            GenerationStatus = Loc["BtnGenerating"];

            try
            {
                string b64 = "";
                if (!IsTextToImageMode && !string.IsNullOrEmpty(SourceImagePath) && File.Exists(SourceImagePath))
                {
                    byte[] bytes = File.ReadAllBytes(SourceImagePath);
                    b64 = Convert.ToBase64String(bytes);
                }

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
                    var bmpImg = new BitmapImage();
                    bmpImg.BeginInit();
                    bmpImg.UriSource = new Uri(outPath);
                    bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImg.EndInit();
                    bmpImg.Freeze();
                    ResultImageBitmap = bmpImg;

                    double sec = result["generation_time_seconds"]?.ToObject<double>() ?? 0;
                    bool tgDispatched = result["telegram"]?["dispatched"]?.ToObject<bool>() ?? false;
                    string tgNote = tgDispatched ? " | Telegram ✈️" : "";
                    GenerationStatus = $"⚡ {sec:F1}s {tgNote}";
                }
                else
                {
                    GenerationStatus = Loc["CanvasEmptyTitle"];
                }
            }
            catch (Exception ex)
            {
                GenerationStatus = ex.Message;
                StormMessageBox.Show(ex.Message, Loc["AppTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
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
                StormMessageBox.Show(Loc["MsgCopiedText"], Loc["MsgCopiedTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
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
        public LocalizationManager Loc => LocalizationManager.Instance;

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
                string msg = string.Format(Loc["MsgConfirmDeleteText"], SelectedItem.Filename);
                var r = StormMessageBox.Show(msg, Loc["MsgConfirmDeleteTitle"], MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                StormMessageBox.Show(Loc["MsgCopiedText"], Loc["MsgCopiedTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        public async Task SendSelectedToTelegram()
        {
            if (SelectedItem == null || !File.Exists(SelectedItem.ImagePath)) return;

            var settings = SettingsService.Instance.Settings;
            if (string.IsNullOrWhiteSpace(settings.TelegramBotToken) || string.IsNullOrWhiteSpace(settings.TelegramChatId))
            {
                StormMessageBox.Show(Loc["MsgTelegramErrorTitle"], Loc["TelegramTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
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

                StormMessageBox.Show(Loc["MsgTelegramSuccessText"], Loc["MsgTelegramSuccessTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StormMessageBox.Show(ex.Message, Loc["MsgTelegramErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
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
        public LocalizationManager Loc => LocalizationManager.Instance;

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
            StormMessageBox.Show(Loc["MsgSavedText"], Loc["MsgSavedTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        public async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(BotToken) || string.IsNullOrWhiteSpace(ChatId))
            {
                StormMessageBox.Show(Loc["MsgTelegramErrorTitle"], Loc["TelegramTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsTesting = true;
            TestStatus = Loc["ModelStatusLoading"];
            IsSuccess = false;

            try
            {
                var res = await BackendService.Instance.TestTelegramAsync(BotToken, ChatId);
                string botUser = res["bot_username"]?.ToString() ?? "";
                string title = res["chat_title"]?.ToString() ?? "";
                TestStatus = $"Connected: @{botUser} -> '{title}'";
                IsSuccess = true;
                SaveSettings();
            }
            catch (Exception ex)
            {
                TestStatus = ex.Message;
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
        public LocalizationManager Loc => LocalizationManager.Instance;

        [ObservableProperty]
        private string _outputDirectory = "";

        [ObservableProperty]
        private ThemeType _selectedTheme;

        [ObservableProperty]
        private string _selectedLanguage = "ru";

        [ObservableProperty]
        private string _backendUrl = "http://127.0.0.1:7860";

        [ObservableProperty]
        private bool _autoStartBackend = true;

        public ObservableCollection<ThemeType> AvailableThemes { get; } = new()
        {
            ThemeType.StormMidnight,
            ThemeType.StormDark,
            ThemeType.StormNight,
            ThemeType.StormDay,
            ThemeType.StormMatrix,
            ThemeType.StormCyberpunk,
            ThemeType.StormFantasy,
            ThemeType.StormWarhammer
        };

        public ObservableCollection<string> AvailableLanguages { get; } = new()
        {
            "ru", "en", "de", "fr", "zh", "ja"
        };

        public SettingsViewModel()
        {
            var s = SettingsService.Instance.Settings;
            OutputDirectory = s.OutputDirectory;
            SelectedTheme = s.Theme;
            SelectedLanguage = s.Language;
            BackendUrl = s.BackendUrl;
            AutoStartBackend = s.AutoStartBackend;
        }

        [RelayCommand]
        public void BrowseOutputDirectory()
        {
            var dlg = new OpenFolderDialog
            {
                Title = Loc["OutputFolderTitle"],
                InitialDirectory = OutputDirectory
            };

            if (dlg.ShowDialog() == true)
            {
                OutputDirectory = dlg.FolderName;
                SaveSettings();
            }
        }

        public bool IsRuSelected => SelectedLanguage == "ru";
        public bool IsEnSelected => SelectedLanguage == "en";
        public bool IsDeSelected => SelectedLanguage == "de";
        public bool IsFrSelected => SelectedLanguage == "fr";
        public bool IsZhSelected => SelectedLanguage == "zh";
        public bool IsJaSelected => SelectedLanguage == "ja";

        public bool IsMidnightSelected => SelectedTheme == ThemeType.StormMidnight;
        public bool IsDarkSelected => SelectedTheme == ThemeType.StormDark;
        public bool IsNightSelected => SelectedTheme == ThemeType.StormNight;
        public bool IsDaySelected => SelectedTheme == ThemeType.StormDay;
        public bool IsMatrixSelected => SelectedTheme == ThemeType.StormMatrix;
        public bool IsCyberpunkSelected => SelectedTheme == ThemeType.StormCyberpunk;
        public bool IsFantasySelected => SelectedTheme == ThemeType.StormFantasy;
        public bool IsWarhammerSelected => SelectedTheme == ThemeType.StormWarhammer;

        [RelayCommand]
        public void SetLanguage(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                SelectedLanguage = lang;
                LocalizationManager.Instance.SetLanguage(lang);
                var s = SettingsService.Instance.Settings;
                s.Language = lang;
                SettingsService.Instance.Save();
                NotifyLanguageChanges();
            }
        }

        private void NotifyLanguageChanges()
        {
            OnPropertyChanged(nameof(IsRuSelected));
            OnPropertyChanged(nameof(IsEnSelected));
            OnPropertyChanged(nameof(IsDeSelected));
            OnPropertyChanged(nameof(IsFrSelected));
            OnPropertyChanged(nameof(IsZhSelected));
            OnPropertyChanged(nameof(IsJaSelected));
        }

        private void NotifyThemeChanges()
        {
            OnPropertyChanged(nameof(IsMidnightSelected));
            OnPropertyChanged(nameof(IsDarkSelected));
            OnPropertyChanged(nameof(IsNightSelected));
            OnPropertyChanged(nameof(IsDaySelected));
            OnPropertyChanged(nameof(IsMatrixSelected));
            OnPropertyChanged(nameof(IsCyberpunkSelected));
            OnPropertyChanged(nameof(IsFantasySelected));
            OnPropertyChanged(nameof(IsWarhammerSelected));
        }

        [RelayCommand]
        public void SelectTheme(string themeName)
        {
            if (Enum.TryParse<ThemeType>(themeName, true, out var theme))
            {
                SelectedTheme = theme;
                ThemeManager.Instance.ApplyTheme(theme, Application.Current.MainWindow);
                var s = SettingsService.Instance.Settings;
                s.Theme = theme;
                SettingsService.Instance.Save();
                NotifyThemeChanges();
            }
        }

        partial void OnSelectedThemeChanged(ThemeType value)
        {
            ThemeManager.Instance.ApplyTheme(value, Application.Current.MainWindow);
            NotifyThemeChanges();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            LocalizationManager.Instance.SetLanguage(value);
            NotifyLanguageChanges();
        }

        [RelayCommand]
        public void SaveSettings()
        {
            var s = SettingsService.Instance.Settings;
            s.OutputDirectory = OutputDirectory;
            s.Theme = SelectedTheme;
            s.Language = SelectedLanguage;
            s.BackendUrl = BackendUrl;
            s.AutoStartBackend = AutoStartBackend;
            SettingsService.Instance.Save();
            StormMessageBox.Show(Loc["MsgSavedText"], Loc["MsgSavedTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
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