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
    public static string MenuReopen => T("Переоткрыть окна Проводника", "Reopen File Explorer windows");
    public static string MenuExit => T("Выход", "Exit");
}
