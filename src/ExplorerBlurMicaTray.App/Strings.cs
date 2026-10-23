using System.Globalization;
using Microsoft.Win32;

namespace ExplorerBlurMicaTray.App;

/// <summary>
/// User-facing text in Russian and English. The language is the user's own choice if set
/// (HKCU), else the one picked in Setup (HKLM), else the Windows display language.
/// </summary>
internal static class Strings
{
    private const string SettingsKey = @"Software\ExplorerBlurMica-Tray";

    public static bool Russian { get; } = DetectRussian();

    private static bool DetectRussian()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(SettingsKey);
            if (key?.GetValue("Language") is string { Length: > 0 } language)
                return language.Equals("ru", StringComparison.OrdinalIgnoreCase);
        }
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
    }

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
    public static string ComingSoon => T("Этот раздел появится в следующей версии.", "This section is coming in the next version.");
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
    public static string DarkTheme => T("Тёмная тема", "Dark theme");
    public static string LightTheme => T("Светлая тема", "Light theme");
    public static string BlendSubtitle => T("Цвет и непрозрачность подложки поверх эффекта", "Color and opacity drawn over the effect");
    public static string AdvancedTitle => T("Дополнительно", "More options");
    public static string AdvancedSubtitle => T("Какие части окна Проводника делать прозрачными", "Which parts of File Explorer windows go transparent");
    public static string ClearAddressTitle => T("Прозрачная адресная строка", "Transparent address bar");
    public static string ClearAddressDesc => T("Убирает фон у адресной строки и поиска", "Removes the background of the address and search boxes");
    public static string ClearBarBgTitle => T("Прозрачные полосы прокрутки", "Transparent scroll bars");
    public static string ClearBarBgDesc => T("Полосы рисуются заново и могут немного отличаться от системных", "Scroll bars are redrawn and may look slightly different from the system ones");
    public static string ClearWinUIBgTitle => T("Прозрачная панель инструментов", "Transparent toolbar");
    public static string ClearWinUIBgDesc => T("Для новой панели Проводника в Windows 11", "For the new File Explorer toolbar in Windows 11");
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
    public static string MenuReopen => T("Переоткрыть окна Проводника", "Reopen File Explorer windows");
    public static string MenuExit => T("Выход", "Exit");
}
