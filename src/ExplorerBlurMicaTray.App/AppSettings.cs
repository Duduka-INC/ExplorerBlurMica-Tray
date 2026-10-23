using Microsoft.Win32;

namespace ExplorerBlurMicaTray.App;

public enum LanguageChoice
{
    System,
    Russian,
    English,
}

/// <summary>The app's own per-user settings, kept in HKCU next to the language Setup wrote to HKLM.</summary>
internal static class AppSettings
{
    public const string KeyPath = @"Software\ExplorerBlurMica-Tray";

    /// <summary>The user's choice if any, else the language picked in Setup, else the Windows language.</summary>
    public static LanguageChoice Language
    {
        get
        {
            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                if (Read(hive, "Language") is { Length: > 0 } value)
                    return value.ToLowerInvariant() switch
                    {
                        "ru" => LanguageChoice.Russian,
                        "en" => LanguageChoice.English,
                        _ => LanguageChoice.System,
                    };
            }
            return LanguageChoice.System;
        }
        set => Write("Language", value switch
        {
            LanguageChoice.Russian => "ru",
            LanguageChoice.English => "en",
            // Written rather than deleted, so it also overrides the language picked in Setup.
            _ => "system",
        });
    }

    /// <summary>A left click on the tray icon opens the settings window instead of the flyout.</summary>
    public static bool LeftClickOpensSettings
    {
        get => Read(Registry.CurrentUser, "LeftClick") == "settings";
        set => Write("LeftClick", value ? "settings" : "flyout");
    }

    /// <summary>The key of the preset the user picked last, or null.</summary>
    public static string? SelectedPreset
    {
        get => Read(Registry.CurrentUser, "Preset");
        set
        {
            if (value is null)
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
                key?.DeleteValue("Preset", throwOnMissingValue: false);
            }
            else
            {
                Write("Preset", value);
            }
        }
    }

    private static string? Read(RegistryKey hive, string name)
    {
        using var key = hive.OpenSubKey(KeyPath);
        return key?.GetValue(name) as string;
    }

    private static void Write(string name, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(name, value);
    }
}
