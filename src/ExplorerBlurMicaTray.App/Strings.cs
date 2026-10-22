namespace ExplorerBlurMicaTray.App;

/// <summary>User-facing text in one place, ready to move to resources when the English UI arrives.</summary>
internal static class Strings
{
    public const string AppName = "ExplorerBlurMica Tray";
    public const string FlyoutTitle = "Эффект Проводника";

    public const string StatusActive = "Активно";
    public const string StatusDefault = "Default · без эффекта";
    public const string StatusPending = "Нужно применить";
    public const string StatusNotInstalled = "ExplorerBlurMica не установлен";
    public const string StatusBroken = "ExplorerBlurMica повреждён";
    public static string StatusWindows(int count) => count switch
    {
        0 => "",
        1 => " · 1 окно Проводника",
        < 5 => $" · {count} окна Проводника",
        _ => $" · {count} окон Проводника",
    };

    public const string Apply = "Применить";
    public const string Applying = "Применяю…";
    public const string PresetCustom = "Свой";

    public const string TransparencyOffTitle = "Прозрачность в Windows выключена";
    public const string TransparencyOffMessage = "Без неё Mica и Acrylic не рисуются. Ещё так бывает при энергосбережении.";

    public const string InstallTitle = "Нужен ExplorerBlurMica";
    public const string InstallBody = "Эффекты в Проводнике делает бесплатная утилита ExplorerBlurMica от Maplespe. Приложение скачает официальный релиз с GitHub и подключит его.";
    public const string InstallButton = "Скачать и установить";
    public const string ReinstallButton = "Переустановить";
    public const string InstallNote = "Windows попросит права администратора";
    public const string InstallChecking = "Ищу последний релиз…";
    public const string InstallDownloading = "Скачиваю…";
    public const string InstallRegistering = "Регистрирую ExplorerBlurMica…";
    public const string InstallRestarting = "Перезапускаю Проводник…";

    public const string ErrorTitle = "Не получилось";

    public const string MenuDefault = "Default";
    public const string MenuReopen = "Переоткрыть окна Проводника";
    public const string MenuExit = "Выход";
}
