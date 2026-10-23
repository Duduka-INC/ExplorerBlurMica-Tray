using System.Globalization;
using ExplorerBlurMicaTray.Core;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// User-facing text in Russian and English. The language is the user's own choice if set
/// (HKCU), else the one picked in Setup (HKLM), else the Windows display language.
/// It is read once at startup; changing it takes an app restart.
/// </summary>
internal static class Strings
{
    public static bool Russian { get; } = AppSettings.Language switch
    {
        LanguageChoice.Russian => true,
        LanguageChoice.English => false,
        _ => SystemIsRussian,
    };

    public static bool SystemIsRussian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

    private static string T(string ru, string en) => Russian ? ru : en;

    public const string AppName = "ExplorerBlurMica Tray";
    public static string FlyoutTitle => T("Эффект Проводника", "File Explorer effect");

    public static string StatusActive => T("Активно", "Active");
    public static string StatusDefault => T("Default · без эффекта", "Default · no effect");
    public static string StatusPending => T("Нужно применить", "Not applied yet");
    public static string StatusNotInstalled => T("ExplorerBlurMica не установлен", "ExplorerBlurMica is not installed");
    public static string StatusBroken => T("ExplorerBlurMica повреждён", "ExplorerBlurMica is damaged");

    public static string StatusWindows(int count)
    {
        if (count == 0)
            return "";
        if (!Russian)
            return count == 1 ? " · 1 Explorer window" : $" · {count} Explorer windows";
        int lastTwo = count % 100, last = count % 10;
        string noun = lastTwo is >= 11 and <= 14 ? "окон" : last switch
        {
            1 => "окно",
            >= 2 and <= 4 => "окна",
            _ => "окон",
        };
        return $" · {count} {noun} Проводника";
    }

    public static string TintHintBlur => T(
        "Blur рисуется без подкраски, затемнение на него не влияет.",
        "Blur is drawn without a tint, so this setting has no effect.");
    public static string TintHintMica => T(
        "Подкраску Mica задаёт Windows. На новых сборках Windows 11 эффект может не работать.",
        "Windows sets the Mica tint. On recent Windows 11 builds the effect may not work.");

    public static string Apply => T("Применить", "Apply");
    public static string Applying => T("Применяю…", "Applying…");
    public static string PresetCustom => T("Свой", "Custom");
    public static string PresetGlass => T("Стекло", "Glass");
    public static string PresetDeep => T("Глубокий", "Deep");
    public static string PresetClearBlur => T("Чистое размытие", "Clear blur");

    public static string TransparencyOffTitle => T("Прозрачность в Windows выключена", "Windows transparency is off");
    public static string TransparencyOffMessage => T(
        "Без неё Mica и Acrylic не рисуются. Ещё так бывает при энергосбережении.",
        "Mica and Acrylic need it. Energy saver also turns it off.");
    public static string OpenWindowsSettings => T("Открыть параметры Windows", "Open Windows settings");

    public static string InstallTitle => T("Нужен ExplorerBlurMica", "ExplorerBlurMica is needed");
    public static string InstallBody => T(
        "Эффекты в Проводнике делает бесплатная утилита ExplorerBlurMica от Maplespe. Приложение скачает официальный релиз с GitHub и подключит его.",
        "The File Explorer effects come from ExplorerBlurMica, a free tool by Maplespe. The app downloads its official release from GitHub and sets it up.");
    public static string InstallButton => T("Скачать и установить", "Download and install");
    public static string ReinstallButton => T("Переустановить", "Reinstall");
    public static string InstallNote => T("Windows попросит права администратора", "Windows will ask for administrator rights");
    public static string InstallChecking => T("Ищу последний релиз…", "Looking for the latest release…");
    public static string InstallDownloading => T("Скачиваю…", "Downloading…");
    public static string InstallRegistering => T("Регистрирую ExplorerBlurMica…", "Registering ExplorerBlurMica…");
    public static string InstallRestarting => T("Перезапускаю Проводник…", "Restarting File Explorer…");

    public static string ErrorTitle => T("Не получилось", "Something went wrong");

    public const string MenuDefault = "Default";
    public static string OpenSettings => T("Открыть настройки", "Open settings");

    // Settings window
    public static string NavEffect => T("Эффект", "Effect");
    public static string NavPresets => T("Пресеты", "Presets");
    public static string NavGeneral => T("Общие", "General");
    public static string Preview => T("Предпросмотр", "Preview");
    public static string MaterialTitle => T("Материал", "Material");
    public static string MaterialSubtitle => T("Фон окон Проводника", "Background of File Explorer windows");
    public static string MaterialDescription(Material material) => material switch
    {
        Material.Default => T("Default: обычный Проводник без эффекта.", "Default: the usual File Explorer, no effect."),
        Material.Mica => T("Mica: размытые обои ровно под окном, другие окна сквозь него не видны. На новых сборках Windows 11 может не работать.",
            "Mica: the blurred wallpaper right under the window; other windows don't show through. May not work on recent Windows 11 builds."),
        Material.MicaAlt => T("Mica Alt: как Mica, но заметнее окрашен цветом обоев. На новых сборках Windows 11 может не работать.",
            "Mica Alt: like Mica, with more of the wallpaper's color. May not work on recent Windows 11 builds."),
        Material.Acrylic => T("Acrylic: размывает всё, что позади окна, включая другие окна, и подкрашивает цветом подложки.",
            "Acrylic: blurs everything behind the window, other windows included, tinted with the blend color."),
        _ => T("Blur: чистое размытие всего, что позади окна, без подкраски.", "Blur: a plain blur of everything behind the window, with no tint."),
    };
    public static string ColorsHeader => T("Цвет подложки", "Blend color");
    public static string BlendTitle(bool light) => light
        ? T("Для светлой темы Windows", "For the Windows light theme")
        : T("Для тёмной темы Windows", "For the Windows dark theme");
    public static string BlendSystemSubtitle => T("Системный цвет, как у остальных окон Windows", "The system color, as in other Windows apps");
    public static string BlendCustomSubtitle => T("Свой цвет поверх размытия. Плотность — насколько он закрывает фон.",
        "Your color over the blur. Density is how much of the background it covers.");
    public static string BlendColorName => T("Цвет подложки", "Blend color");
    public static string BlendDensity => T("Плотность", "Density");
    public static string BlendSystem => T("Системный", "System");
    public static string AdvancedTitle => T("Дополнительно", "More options");
    public static string AdvancedSubtitle => T("Какие части окна Проводника делать прозрачными", "Which parts of File Explorer windows go transparent");
    public static string ClearAddressTitle => T("Прозрачная адресная строка", "Transparent address bar");
    public static string ClearAddressDesc => T("Для классической адресной строки, как в Windows 10", "For the classic address bar, as in Windows 10");
    public static string ClearBarBgTitle => T("Без фона под полосой прокрутки", "No background under scroll bars");
    public static string ClearBarBgDesc => T("Убирает дорожку под ползунком. Ползунок рисуется заново и может немного отличаться от системного.",
        "Removes the track under the thumb. The thumb is redrawn and may look slightly different from the system one.");
    public static string ClearWinUIBgTitle => T("Прозрачная верхняя панель", "Transparent top bar");
    public static string ClearWinUIBgDesc => T("Вкладки, адресная строка, поиск и панель команд", "Tabs, address bar, search and command bar");
    public static string ShowLineTitle => T("Линия-разделитель", "Divider line");
    public static string ShowLineDesc => T("Между деревом папок и списком файлов", "Between the folder tree and the file list");
    public static string PendingTitle => T("Изменения ещё не применены", "Changes not applied yet");
    public static string PendingRestart(bool enable) => enable
        ? T("Проводник перезапустится, чтобы включить эффект. Панель задач на секунду пропадёт. Windows попросит права администратора.",
            "File Explorer will restart to turn the effect on. The taskbar disappears for a moment. Windows will ask for administrator rights.")
        : T("Проводник перезапустится, чтобы выключить эффект. Панель задач на секунду пропадёт. Windows попросит права администратора.",
            "File Explorer will restart to turn the effect off. The taskbar disappears for a moment. Windows will ask for administrator rights.");
    public static string PendingReopen(int windows) => windows > 0
        ? T($"Окна Проводника переоткроются (открыто: {windows}).", $"File Explorer windows will reopen ({windows} open).")
        : T("Новые окна Проводника откроются уже с изменениями.", "New File Explorer windows will open with the changes.");
    public static string Discard => T("Отменить", "Discard");

    // Settings → Presets
    public static string PresetsIntro => T("Пресет хранит материал, цвета для обеих тем и дополнительные параметры.",
        "A preset keeps the material, the blend colors for both themes and the extra options.");
    public static string PresetSaveCurrent => T("Сохранить текущие как…", "Save current as…");
    public static string PresetsEmptyHint => T("Свои пресеты появятся здесь: настройте эффект и сохраните его кнопкой выше.",
        "Your presets show up here: set up the effect and save it with the button above.");
    public static string ChipActive => T("Активен", "Active");
    public static string ChipBuiltIn => T("Встроенный", "Built-in");
    public static string PresetMeta(Material material, BlendColor blend) => material switch
    {
        Material.Default => T("Обычный Проводник, без эффекта", "Plain File Explorer, no effect"),
        Material.Blur => T("Blur · без подкраски", "Blur · no tint"),
        Material.Mica => T("Mica · размытые обои под окном", "Mica · blurred wallpaper under the window"),
        Material.MicaAlt => T("Mica Alt · больше цвета обоев", "Mica Alt · more of the wallpaper's color"),
        Material.Acrylic when blend.A == 0 => T("Acrylic · системный цвет", "Acrylic · system color"),
        Material.Acrylic => T($"Acrylic · #{blend.R:X2}{blend.G:X2}{blend.B:X2} · плотность {Materials.AlphaToPercent(blend.A)} %",
            $"Acrylic · #{blend.R:X2}{blend.G:X2}{blend.B:X2} · density {Materials.AlphaToPercent(blend.A)}%"),
        _ => Materials.Label(material),
    };
    public static string Rename => T("Переименовать", "Rename");
    public static string Delete => T("Удалить", "Delete");
    public static string More => T("Ещё", "More");
    public static string Save => T("Сохранить", "Save");
    public static string Cancel => T("Отмена", "Cancel");
    public static string SavePresetTitle => T("Сохранить пресет", "Save preset");
    public static string RenamePresetTitle => T("Переименовать пресет", "Rename preset");
    public static string PresetNamePlaceholder => T("Название", "Name");
    public static string PresetDefaultName => T("Мой пресет", "My preset");
    public static string PresetNameEmpty => T("Введите название", "Enter a name");
    public static string PresetNameTaken => T("Пресет с таким названием уже есть", "A preset with this name already exists");
    public static string DeletePresetTitle(string name) => T($"Удалить пресет «{name}»?", $"Delete the “{name}” preset?");
    public static string DeletePresetBody => T("Отменить удаление будет нельзя.", "This can't be undone.");
    public static string PresetsSaveError => T("Не удалось сохранить пресеты", "Couldn't save the presets");

    // Settings → General
    public static string AppSection => T("Приложение", "App");
    public static string AutostartTitle => T("Запускать вместе с Windows", "Start with Windows");
    public static string AutostartDesc => T("Иконка появится в трее после входа в систему", "The tray icon appears after you sign in");
    public static string TrayIconTitle => T("Иконка на панели задач", "Icon on the taskbar");
    public static string TrayIconDesc => T("Windows прячет новые иконки под стрелку. Чтобы иконка всегда была видна, включите ExplorerBlurMica Tray в списке «Другие значки в трее».",
        "Windows hides new icons behind the arrow. To keep the icon visible, turn on ExplorerBlurMica Tray under \"Other system tray icons\".");
    public static string OpenTaskbarSettings => T("Открыть параметры", "Open settings");
    public static string LanguageTitle => T("Язык интерфейса", "Display language");
    public static string LanguageDesc => T("Вступит в силу после перезапуска приложения", "Takes effect after the app restarts");
    public static string LanguageSystem => T($"Как в Windows ({(SystemIsRussian ? "русский" : "английский")})",
        $"Same as Windows ({(SystemIsRussian ? "Russian" : "English")})");
    public static string RestartTitle => T("Язык сменится после перезапуска", "The language changes after a restart");
    public static string RestartApp => T("Перезапустить", "Restart");
    public static string LeftClickTitle => T("Левый клик по иконке в трее", "Left click on the tray icon");
    public static string LeftClickDesc => T("Правый клик всегда открывает меню", "A right click always opens the menu");
    public static string LeftClickFlyout => T("Мини-панель", "Quick panel");
    public static string LeftClickSettings => T("Окно настроек", "Settings window");

    public static string EbmStatusLine(EbmStatus status, bool effectOn) => status.State switch
    {
        EbmState.NotInstalled => T("Не установлен", "Not installed"),
        EbmState.Broken => T("Повреждён: файл не найден", "Damaged: its file is missing"),
        _ => T($"Установлен · версия {status.Version ?? "?"} · {(effectOn ? "эффект включён" : "Default, эффект выключен")}",
            $"Installed · version {status.Version ?? "?"} · {(effectOn ? "effect on" : "Default, effect off")}"),
    };
    public static string EbmSource(string? folder) => folder is null
        ? T("Официальный релиз Maplespe с GitHub", "The official Maplespe release from GitHub")
        : T($"Официальный релиз Maplespe с GitHub · {folder}", $"The official Maplespe release from GitHub · {folder}");
    public static string CheckUpdates => T("Проверить обновления", "Check for updates");
    public static string Checking => T("Проверяю…", "Checking…");
    public static string UpToDate => T("Установлена последняя версия", "You have the latest version");
    public static string UpdateAvailable(string version) => T($"Доступна версия {version}", $"Version {version} is available");
    public static string Update => T("Обновить", "Update");
    public static string ReinstallTitle => T("Переустановить", "Reinstall");
    public static string ReinstallDesc => T("Скачать заново и зарегистрировать. Windows попросит права администратора.",
        "Download again and register. Windows will ask for administrator rights.");
    public static string UninstallEbmTitle => T("Удалить ExplorerBlurMica", "Remove ExplorerBlurMica");
    public static string UninstallEbmDesc => T("Проводник вернётся к обычному виду. Настройки и пресеты сохранятся.",
        "File Explorer goes back to its usual look. Settings and presets are kept.");
    public static string UninstallEbmConfirm => T("Удалить ExplorerBlurMica?", "Remove ExplorerBlurMica?");
    public static string UninstallEbmConfirmBody => T("Проводник перезапустится, панель задач на секунду пропадёт. Windows попросит права администратора. Вернуть эффект можно будет из приложения.",
        "File Explorer restarts and the taskbar disappears for a moment. Windows will ask for administrator rights. You can bring the effect back from the app.");
    public static string Remove => T("Удалить", "Remove");
    public static string Working => T("Подождите…", "Please wait…");

    public static string AboutSection => T("О программе", "About");
    public static string AboutVersion(string version) => T($"Версия {version} · лицензия MIT", $"Version {version} · MIT license");
    public static string SourceCode => T("Исходный код", "Source code");
    public static string CreditTitle => T("Эффекты делает ExplorerBlurMica от Maplespe", "The effects come from ExplorerBlurMica by Maplespe");
    public static string CreditDesc => T("Это приложение только управляет его настройками. Если нравится результат, поставьте звезду оригинальному проекту.",
        "This app only manages its settings. If you like the result, give the original project a star.");
    public static string OpenProject => T("Открыть проект", "Open project");

    // The File Explorer window drawn in the preview
    public static string SketchFolder => T("Документы", "Documents");
    public static string SketchThisPc => T("Этот компьютер", "This PC");
    public static string SketchSearch => T("Поиск: Документы", "Search Documents");
    public static string SketchNew => T("Создать", "New");
    public static string SketchSort => T("Сортировать", "Sort");
    public static string SketchView => T("Просмотреть", "View");
    public static string[] SketchNav => Russian
        ? ["Главная", "Рабочий стол", "Загрузки", "Документы", "Изображения", "Этот компьютер"]
        : ["Home", "Desktop", "Downloads", "Documents", "Pictures", "This PC"];
    public static string SketchName => T("Имя", "Name");
    public static string SketchDate => T("Дата изменения", "Date modified");
    public static string SketchType => T("Тип", "Type");
    public static string SketchFolderType => T("Папка", "Folder");
    public static string[] SketchFolders => Russian ? ["Проекты", "Работа", "Учёба"] : ["Projects", "Work", "Study"];
    public static (string Name, string Type)[] SketchFiles => Russian
        ? [("Заметки.txt", "Текстовый"), ("Отчёт.docx", "Документ"), ("Отпуск.jpg", "Изображение")]
        : [("Notes.txt", "Text"), ("Report.docx", "Document"), ("Holiday.jpg", "Image")];
    public static string SketchStatus => T("6 элементов", "6 items");
    public static string MenuReopen => T("Переоткрыть окна Проводника", "Reopen File Explorer windows");
    public static string MenuExit => T("Выход", "Exit");
}
