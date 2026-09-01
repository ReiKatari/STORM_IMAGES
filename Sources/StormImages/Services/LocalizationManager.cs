using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using StormImages.Services;

namespace StormImages.Services
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentLanguage { get; private set; } = "ru";

        [IndexerName("Item")]
        public string this[string key] => Get(key);

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["ru"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "Редактор и генерация",
                ["NavGallery"] = "Галерея и история",
                ["NavTelegram"] = "Telegram-канал",
                ["NavModels"] = "Модели и нейросети",
                ["NavSettings"] = "Настройки",
                
                ["ModeTextToImage"] = "Генерация из текста",
                ["ModeImageToImage"] = "Редактирование изображения",
                ["ModeT2IDesc"] = "Создание арта с нуля по текстовому описанию (картинка не требуется)",
                ["ModeI2IDesc"] = "Трансформация загруженного изображения с помощью LoRA",
                
                ["InputImageTitle"] = "Исходное изображение",
                ["InputImageDropHint"] = "Нажмите или перетащите изображение сюда",
                ["InputImageFormats"] = "PNG, JPG, WEBP, BMP",
                ["InputImageOptional"] = "Опционально: в режиме генерации из текста картинка не требуется",
                ["BtnSelectFile"] = "Выбрать файл...",
                ["BtnClearImage"] = "Очистить",
                
                ["ModelSelectionTitle"] = "Выбор базовой модели и LoRA",
                ["BaseModelLabel"] = "Базовая модель:",
                ["LoRAAdapterLabel"] = "LoRA-адаптер:",
                ["ModelStatusReady"] = "Модель готова к работе",
                ["ModelStatusLoading"] = "Идет загрузка весов в VRAM...",
                ["ModelStatusIdle"] = "Модель не загружена",
                ["BtnLoadModel"] = "Загрузить модель в VRAM",
                
                ["PromptTitle"] = "Текстовый промпт (Prompt)",
                ["PromptPlaceholder"] = "Опишите желаемое изображение подробно...",
                ["NegativePromptTitle"] = "Негативный промпт (Negative)",
                ["NegativePromptPlaceholder"] = "Что исключить из генерации...",
                
                ["TagMasterpiece"] = "+ Шедевр",
                ["TagPhotorealism"] = "+ Фотореализм",
                ["TagCyberpunk"] = "+ Киберпанк",
                ["TagCinematic"] = "+ Кинематограф",
                ["TagNsfw"] = "+ NSFW LoRA",
                ["TagPortrait"] = "+ Портрет",
                ["TagAnime"] = "+ Аниме",
                
                ["LoRAStrength"] = "Сила LoRA:",
                ["InferenceSteps"] = "Шаги инференса:",
                ["GuidanceScale"] = "Соответствие промпту (CFG):",
                ["SeedLabel"] = "Сид генерации (Seed):",
                ["BtnRandomSeed"] = "🎲 Случайный",
                ["BtnResetSeed"] = "Сброс",
                ["TelegramAutoSend"] = "Автоматически отправить результат в Telegram-канал",
                
                ["BtnGenerate"] = "СГЕНЕРИРОВАТЬ ИЗОБРАЖЕНИЕ",
                ["BtnGenerating"] = "ГЕНЕРАЦИЯ В ПРОЦЕССЕ...",
                ["CanvasTitle"] = "Холст результатов и сравнения",
                ["CanvasEmptyTitle"] = "Готовое изображение появится здесь",
                ["CanvasEmptyDesc"] = "Выберите режим, введите промпт и нажмите 'Сгенерировать изображение'",
                ["SavedPathFormat"] = "Сохранено в: {0}",
                
                ["BtnCopy"] = "Копировать",
                ["BtnOpenFolder"] = "Открыть папку",
                ["BtnDelete"] = "Удалить",
                ["BtnSendTelegram"] = "Отправить в Telegram",
                
                ["GalleryTitle"] = "Галерея и история генераций",
                ["GalleryCountFormat"] = "{0} изображений",
                ["BtnRefresh"] = "Обновить",
                ["GalleryEmpty"] = "В галерее пока нет сохраненных генераций",
                ["GallerySelectHint"] = "Выберите изображение из списка слева для просмотра деталей",
                ["GenInfoTitle"] = "Информация о генерации",
                ["DateLabel"] = "Дата:",
                ["DimensionsLabel"] = "Разрешение:",
                
                ["TelegramTitle"] = "Интеграция с Telegram (Канал и группа)",
                ["TgTokenLabel"] = "Токен Telegram-бота (Bot Token):",
                ["TgChatIdLabel"] = "ID Чата или канала (Chat ID или @username):",
                ["TgAutoSendCheck"] = "Автоматически публиковать каждую новую генерацию",
                ["TgCaptionLabel"] = "Шаблон подписи к изображению:",
                ["TgVariablesHint"] = "Доступные переменные: {prompt}, {seed}, {lora_scale}",
                ["BtnTestConnection"] = "Проверить подключение",
                ["BtnSaveSettings"] = "Сохранить настройки",
                ["TgInstructionsTitle"] = "Инструкция по настройке Telegram",
                ["TgStep1Title"] = "1. Создание бота",
                ["TgStep1Desc"] = "Откройте @BotFather в Telegram, отправьте команду /newbot и получите API-токен.",
                ["TgStep2Title"] = "2. Добавление в канал",
                ["TgStep2Desc"] = "Добавьте созданного бота в администраторы канала с правом публикации сообщений.",
                ["TgStep3Title"] = "3. Получение ID",
                ["TgStep3Desc"] = "Для публичного канала укажите @имя_канала, для приватного ID вида -100xxxxxxxxxx.",
                
                ["SettingsTitle"] = "Настройки приложения и хранилища",
                ["LanguageSectionTitle"] = "Язык интерфейса (Language)",
                ["OutputFolderTitle"] = "Директория сохранения изображений",
                ["OutputFolderDesc"] = "Все созданные арты и JSON-метаданные будут автоматически сохраняться в эту папку.",
                ["BtnBrowseFolder"] = "Выбрать папку...",
                ["ThemeSectionTitle"] = "Тема оформления",
                ["ThemeSectionDesc"] = "Выберите визуальный стиль интерфейса экосистемы STORM SOFT:",
                ["ServerSectionTitle"] = "Параметры AI-сервера",
                ["ServerUrlLabel"] = "Адрес FastAPI / Diffusers сервера:",
                ["ServerAutoStartCheck"] = "Автоматически запускать локальный AI-сервер при старте",
                ["BtnSaveAll"] = "Сохранить все параметры",
                
                ["ServerStatusTitle"] = "AI Сервер (Qwen)",
                ["BtnStartServer"] = "Запустить сервер",
                ["StatusLabel"] = "Статус:",
                ["ModelLabel"] = "Модель:",
                ["FooterCopyright"] = "STORM TEAM © 2026",
                
                ["MsgInputRequiredTitle"] = "Требуется исходное изображение",
                ["MsgInputRequiredText"] = "В режиме редактирования (Image-to-Image) необходимо выбрать исходное изображение. Переключитесь на режим 'Генерация из текста', если хотите создать изображение с нуля.",
                ["MsgPromptRequiredTitle"] = "Требуется текстовый промпт",
                ["MsgPromptRequiredText"] = "Пожалуйста, введите текстовое описание желаемого изображения.",
                ["MsgCopiedTitle"] = "Скопировано",
                ["MsgCopiedText"] = "Изображение успешно скопировано в буфер обмена.",
                ["MsgTelegramSuccessTitle"] = "Успешная отправка",
                ["MsgTelegramSuccessText"] = "Изображение успешно опубликовано в Telegram!",
                ["MsgTelegramErrorTitle"] = "Ошибка Telegram",
                ["MsgConfirmDeleteTitle"] = "Подтверждение удаления",
                ["MsgConfirmDeleteText"] = "Вы уверены, что хотите удалить файл {0}?",
                ["MsgSavedTitle"] = "Сохранено",
                ["MsgSavedText"] = "Настройки успешно сохранены.",
                ["BtnOk"] = "Понятно",
                ["BtnCancel"] = "Отмена",
                ["BtnYes"] = "Да",
                ["BtnNo"] = "Нет"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "Editor and generation",
                ["NavGallery"] = "Gallery and history",
                ["NavTelegram"] = "Telegram channel",
                ["NavModels"] = "Models and networks",
                ["NavSettings"] = "Settings",
                
                ["ModeTextToImage"] = "Text to image generation",
                ["ModeImageToImage"] = "Image editing and LoRA",
                ["ModeT2IDesc"] = "Create artwork from scratch using text prompt (no source image needed)",
                ["ModeI2IDesc"] = "Transform an uploaded image with Qwen-Image-Edit and LoRA",
                
                ["InputImageTitle"] = "Source image",
                ["InputImageDropHint"] = "Click or drag and drop image here",
                ["InputImageFormats"] = "PNG, JPG, WEBP, BMP",
                ["InputImageOptional"] = "Optional: not required in text-to-image mode",
                ["BtnSelectFile"] = "Select file...",
                ["BtnClearImage"] = "Clear",
                
                ["ModelSelectionTitle"] = "Base model and LoRA selection",
                ["BaseModelLabel"] = "Base model:",
                ["LoRAAdapterLabel"] = "LoRA adapter:",
                ["ModelStatusReady"] = "Model ready for inference",
                ["ModelStatusLoading"] = "Loading weights into VRAM...",
                ["ModelStatusIdle"] = "Model not loaded",
                ["BtnLoadModel"] = "Load model to VRAM",
                
                ["PromptTitle"] = "Text prompt",
                ["PromptPlaceholder"] = "Describe desired artwork in detail...",
                ["NegativePromptTitle"] = "Negative prompt",
                ["NegativePromptPlaceholder"] = "Elements to exclude...",
                
                ["TagMasterpiece"] = "+ Masterpiece",
                ["TagPhotorealism"] = "+ Photorealism",
                ["TagCyberpunk"] = "+ Cyberpunk",
                ["TagCinematic"] = "+ Cinematic",
                ["TagNsfw"] = "+ NSFW LoRA",
                ["TagPortrait"] = "+ Portrait",
                ["TagAnime"] = "+ Anime",
                
                ["LoRAStrength"] = "LoRA scale:",
                ["InferenceSteps"] = "Inference steps:",
                ["GuidanceScale"] = "Prompt guidance (CFG):",
                ["SeedLabel"] = "Generation seed:",
                ["BtnRandomSeed"] = "🎲 Random",
                ["BtnResetSeed"] = "Reset",
                ["TelegramAutoSend"] = "Automatically post result to Telegram channel",
                
                ["BtnGenerate"] = "GENERATE IMAGE",
                ["BtnGenerating"] = "GENERATING IN PROGRESS...",
                ["CanvasTitle"] = "Result and comparison canvas",
                ["CanvasEmptyTitle"] = "Generated artwork will appear here",
                ["CanvasEmptyDesc"] = "Choose mode, enter prompt, and click 'Generate image'",
                ["SavedPathFormat"] = "Saved to: {0}",
                
                ["BtnCopy"] = "Copy",
                ["BtnOpenFolder"] = "Open folder",
                ["BtnDelete"] = "Delete",
                ["BtnSendTelegram"] = "Send to Telegram",
                
                ["GalleryTitle"] = "Gallery and generation history",
                ["GalleryCountFormat"] = "{0} artworks",
                ["BtnRefresh"] = "Refresh",
                ["GalleryEmpty"] = "No generated images in gallery yet",
                ["GallerySelectHint"] = "Select an image from the list on the left to inspect metadata",
                ["GenInfoTitle"] = "Generation metadata",
                ["DateLabel"] = "Date:",
                ["DimensionsLabel"] = "Dimensions:",
                
                ["TelegramTitle"] = "Telegram integration (Channel and group)",
                ["TgTokenLabel"] = "Telegram bot token:",
                ["TgChatIdLabel"] = "Chat or channel ID (@username or -100...):",
                ["TgAutoSendCheck"] = "Automatically publish every new generation",
                ["TgCaptionLabel"] = "Image caption template:",
                ["TgVariablesHint"] = "Available placeholders: {prompt}, {seed}, {lora_scale}",
                ["BtnTestConnection"] = "Test connection",
                ["BtnSaveSettings"] = "Save settings",
                ["TgInstructionsTitle"] = "Telegram setup instructions",
                ["TgStep1Title"] = "1. Create bot",
                ["TgStep1Desc"] = "Open @BotFather on Telegram, send /newbot and obtain API token.",
                ["TgStep2Title"] = "2. Add to channel",
                ["TgStep2Desc"] = "Add bot to channel administrators with 'Post messages' permission.",
                ["TgStep3Title"] = "3. Obtain ID",
                ["TgStep3Desc"] = "Use @channel_username or private ID format -100xxxxxxxxxx.",
                
                ["SettingsTitle"] = "Application and storage settings",
                ["LanguageSectionTitle"] = "Interface language",
                ["OutputFolderTitle"] = "Output image directory",
                ["OutputFolderDesc"] = "All generated artworks and JSON metadata will be stored in this directory.",
                ["BtnBrowseFolder"] = "Browse folder...",
                ["ThemeSectionTitle"] = "Visual theme",
                ["ThemeSectionDesc"] = "Select visual style of the STORM SOFT ecosystem:",
                ["ServerSectionTitle"] = "AI server parameters",
                ["ServerUrlLabel"] = "FastAPI / Diffusers server URL:",
                ["ServerAutoStartCheck"] = "Automatically launch local AI server on startup",
                ["BtnSaveAll"] = "Save all settings",
                
                ["ServerStatusTitle"] = "AI Server (Qwen)",
                ["BtnStartServer"] = "Start server",
                ["StatusLabel"] = "Status:",
                ["ModelLabel"] = "Model:",
                ["FooterCopyright"] = "STORM TEAM © 2026",
                
                ["MsgInputRequiredTitle"] = "Source image required",
                ["MsgInputRequiredText"] = "In image editing mode, a source image is required. Switch to 'Text to image generation' mode if you want to create an image from scratch without uploading a file.",
                ["MsgPromptRequiredTitle"] = "Prompt required",
                ["MsgPromptRequiredText"] = "Please provide a text prompt describing the desired image.",
                ["MsgCopiedTitle"] = "Copied",
                ["MsgCopiedText"] = "Image copied to clipboard successfully.",
                ["MsgTelegramSuccessTitle"] = "Dispatched",
                ["MsgTelegramSuccessText"] = "Image successfully dispatched to Telegram channel!",
                ["MsgTelegramErrorTitle"] = "Telegram error",
                ["MsgConfirmDeleteTitle"] = "Confirm deletion",
                ["MsgConfirmDeleteText"] = "Are you sure you want to delete {0}?",
                ["MsgSavedTitle"] = "Saved",
                ["MsgSavedText"] = "Settings successfully saved.",
                ["BtnOk"] = "Understood",
                ["BtnCancel"] = "Cancel",
                ["BtnYes"] = "Yes",
                ["BtnNo"] = "No"
            },
            ["de"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "Editor und generierung",
                ["NavGallery"] = "Galerie und verlauf",
                ["NavTelegram"] = "Telegram-kanal",
                ["NavModels"] = "Modelle und KI",
                ["NavSettings"] = "Einstellungen",
                ["ModeTextToImage"] = "Text-zu-Bild-Generierung",
                ["ModeImageToImage"] = "Bildbearbeitung und LoRA",
                ["BtnGenerate"] = "BILD GENERIEREN",
                ["BtnOk"] = "Verstanden",
                ["BtnCancel"] = "Abbrechen",
                ["BtnYes"] = "Ja",
                ["BtnNo"] = "Nein"
            },
            ["fr"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "Éditeur et génération",
                ["NavGallery"] = "Galerie et historique",
                ["NavTelegram"] = "Canal Telegram",
                ["NavModels"] = "Modèles et IA",
                ["NavSettings"] = "Paramètres",
                ["ModeTextToImage"] = "Génération texte vers image",
                ["ModeImageToImage"] = "Édition d'image et LoRA",
                ["BtnGenerate"] = "GÉNÉRER L'IMAGE",
                ["BtnOk"] = "Compris",
                ["BtnCancel"] = "Annuler",
                ["BtnYes"] = "Oui",
                ["BtnNo"] = "Non"
            },
            ["zh"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "编辑器与图像生成",
                ["NavGallery"] = "图库与历史记录",
                ["NavTelegram"] = "Telegram频道",
                ["NavModels"] = "模型与神经网络",
                ["NavSettings"] = "设置",
                ["ModeTextToImage"] = "文生图模式 (无需原图)",
                ["ModeImageToImage"] = "图生图与LoRA微调",
                ["BtnGenerate"] = "生成图像",
                ["BtnOk"] = "确定",
                ["BtnCancel"] = "取消",
                ["BtnYes"] = "是",
                ["BtnNo"] = "否"
            },
            ["ja"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "STORM IMAGES",
                ["NavGeneration"] = "エディターと画像生成",
                ["NavGallery"] = "ギャラリーと履歴",
                ["NavTelegram"] = "Telegramチャンネル",
                ["NavModels"] = "モデルとニューラルネット",
                ["NavSettings"] = "設定",
                ["ModeTextToImage"] = "テキストからの画像生成",
                ["ModeImageToImage"] = "画像編集とLoRA",
                ["BtnGenerate"] = "画像を生成する",
                ["BtnOk"] = "了解",
                ["BtnCancel"] = "キャンセル",
                ["BtnYes"] = "はい",
                ["BtnNo"] = "いいえ"
            }
        };

        private LocalizationManager()
        {
            CurrentLanguage = SettingsService.Instance.Settings.Language;
            if (string.IsNullOrEmpty(CurrentLanguage)) CurrentLanguage = "ru";
        }

        public string Get(string key)
        {
            if (_translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            {
                return val;
            }
            if (_translations["ru"].TryGetValue(key, out var ruVal))
            {
                return ruVal;
            }
            if (_translations["en"].TryGetValue(key, out var enVal))
            {
                return enVal;
            }
            return key;
        }

        public void SetLanguage(string lang)
        {
            if (_translations.ContainsKey(lang))
            {
                CurrentLanguage = lang;
                SettingsService.Instance.Settings.Language = lang;
                SettingsService.Instance.Save();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }
        }
    }
}