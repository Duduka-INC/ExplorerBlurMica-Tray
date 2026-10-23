using System.Diagnostics;
using System.Reflection;
using ExplorerBlurMicaTray.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExplorerBlurMicaTray.App;

/// <summary>Settings → General: autostart, the tray icon, language, ExplorerBlurMica itself, and about.</summary>
public sealed partial class GeneralPage : UserControl
{
    private static readonly LanguageChoice[] Languages = [LanguageChoice.System, LanguageChoice.Russian, LanguageChoice.English];

    private EffectController? _effect;
    private bool _updating;
    private bool _checking;
    private bool _upToDate;
    private EbmRelease? _update;
    private string? _error;

    public GeneralPage()
    {
        InitializeComponent();

        HeaderText.Text = Strings.NavGeneral;
        AppHeader.Text = Strings.AppSection;
        AutostartTitle.Text = Strings.AutostartTitle;
        AutostartDesc.Text = Strings.AutostartDesc;
        TrayIconTitle.Text = Strings.TrayIconTitle;
        TrayIconDesc.Text = Strings.TrayIconDesc;
        TaskbarSettingsButton.Content = Strings.OpenTaskbarSettings;
        LanguageTitle.Text = Strings.LanguageTitle;
        LanguageDesc.Text = Strings.LanguageDesc;
        LeftClickTitle.Text = Strings.LeftClickTitle;
        LeftClickDesc.Text = Strings.LeftClickDesc;
        RestartBar.Title = Strings.RestartTitle;
        RestartButton.Content = Strings.RestartApp;
        ErrorBar.Title = Strings.ErrorTitle;
        ErrorBar.Closed += (_, _) =>
        {
            _error = null;
            _effect?.ClearError();
        };
        CheckButton.Content = Strings.CheckUpdates;
        ReinstallTitle.Text = Strings.ReinstallTitle;
        ReinstallDesc.Text = Strings.ReinstallDesc;
        ReinstallButton.Content = Strings.ReinstallButton;
        UninstallTitle.Text = Strings.UninstallEbmTitle;
        UninstallDesc.Text = Strings.UninstallEbmDesc;
        UninstallButton.Content = Strings.Remove;
        AboutHeader.Text = Strings.AboutSection;
        VersionText.Text = Strings.AboutVersion(AppVersion());
        SourceLink.Content = Strings.SourceCode;
        CreditTitle.Text = Strings.CreditTitle;
        CreditDesc.Text = Strings.CreditDesc;
        ProjectLink.Content = Strings.OpenProject;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(AutostartSwitch, Strings.AutostartTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(LanguageBox, Strings.LanguageTitle);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(LeftClickBox, Strings.LeftClickTitle);

        _updating = true;
        LanguageBox.ItemsSource = new[] { Strings.LanguageSystem, "Русский", "English" };
        LanguageBox.SelectedIndex = Array.IndexOf(Languages, AppSettings.Language);
        LeftClickBox.ItemsSource = new[] { Strings.LeftClickFlyout, Strings.LeftClickSettings };
        LeftClickBox.SelectedIndex = AppSettings.LeftClickOpensSettings ? 1 : 0;
        _updating = false;
    }

    public void Attach(EffectController effect)
    {
        _effect = effect;
        effect.Changed += () =>
        {
            if (Visibility == Visibility.Visible)
                Refresh();
        };
    }

    public void Refresh()
    {
        if (_effect is not { } effect)
            return;
        _updating = true;
        try
        {
            AutostartSwitch.IsOn = Autostart.ForCurrentUser.IsEnabled;
            ShowRestartBar();

            string? error = _error ?? effect.Error;
            ErrorBar.Message = error ?? "";
            ErrorBar.IsOpen = error is not null;
            ErrorBar.Visibility = error is not null ? Visibility.Visible : Visibility.Collapsed;

            var status = effect.Status;
            bool installed = effect.Installed;
            bool present = status.State != EbmState.NotInstalled;
            bool busy = effect.Busy;
            EbmStatusText.Text = Strings.EbmStatusLine(status, effect.Applied.Material != Material.Default);
            EbmSourceText.Text = Strings.EbmSource(status.DllPath is { } dll ? Path.GetDirectoryName(dll) : null);

            UpdateText.Text = busy && effect.InstallProgress is { } progress
                ? progress.Step switch
                {
                    EbmInstallStep.CheckingRelease => Strings.InstallChecking,
                    EbmInstallStep.Downloading => Strings.InstallDownloading,
                    _ => Strings.InstallRegistering,
                }
                : busy ? Strings.Working
                : _checking ? Strings.Checking
                : _update is { } update ? Strings.UpdateAvailable(update.Version)
                : _upToDate ? Strings.UpToDate
                : "";
            UpdateText.Visibility = UpdateText.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Not installed: one button installs. Installed: check, and update when there's something newer.
            UpdateButton.Content = installed ? Strings.Update : Strings.InstallButton;
            UpdateButton.Visibility = !installed || _update is not null ? Visibility.Visible : Visibility.Collapsed;
            UpdateButton.IsEnabled = !busy;
            CheckButton.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            CheckButton.IsEnabled = !busy && !_checking;

            var rows = present ? Visibility.Visible : Visibility.Collapsed;
            ReinstallLine.Visibility = rows;
            ReinstallRow.Visibility = rows;
            UninstallLine.Visibility = rows;
            UninstallRow.Visibility = rows;
            ReinstallButton.IsEnabled = !busy;
            UninstallButton.IsEnabled = !busy;
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>The language is read once at startup, so a different choice needs a restart.</summary>
    private void ShowRestartBar()
    {
        bool russian = AppSettings.Language switch
        {
            LanguageChoice.Russian => true,
            LanguageChoice.English => false,
            _ => Strings.SystemIsRussian,
        };
        bool needed = russian != Strings.Russian;
        RestartBar.IsOpen = needed;
        RestartBar.Visibility = needed ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string AppVersion()
    {
        string version = typeof(GeneralPage).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        return version.Split('+')[0];
    }

    private void OnAutostartToggled(object sender, RoutedEventArgs e)
    {
        if (_updating)
            return;
        try
        {
            if (AutostartSwitch.IsOn && Environment.ProcessPath is { } exe)
                Autostart.ForCurrentUser.Enable(exe);
            else
                Autostart.ForCurrentUser.Disable();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _error = ex.Message;
        }
        Refresh();
    }

    private void OnTaskbarSettings(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true })?.Dispose();

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || LanguageBox.SelectedIndex < 0)
            return;
        AppSettings.Language = Languages[LanguageBox.SelectedIndex];
        ShowRestartBar();
    }

    private void OnLeftClickChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || LeftClickBox.SelectedIndex < 0)
            return;
        AppSettings.LeftClickOpensSettings = LeftClickBox.SelectedIndex == 1;
    }

    private void OnRestart(object sender, RoutedEventArgs e) => ((App)Application.Current).Restart();

    private async void OnCheckUpdates(object sender, RoutedEventArgs e)
    {
        if (_effect is not { } effect)
            return;
        _checking = true;
        _error = null;
        Refresh();
        try
        {
            _update = await effect.Installer.CheckForUpdateAsync();
            _upToDate = _update is null;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _checking = false;
        }
        Refresh();
    }

    /// <summary>Install, update and reinstall all fetch the latest release.</summary>
    private async void OnInstallLatest(object sender, RoutedEventArgs e)
    {
        if (_effect is not { } effect)
            return;
        _update = null;
        _upToDate = false;
        await effect.InstallAsync();
        Refresh();
    }

    private async void OnUninstall(object sender, RoutedEventArgs e)
    {
        if (_effect is not { } effect)
            return;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            Title = Strings.UninstallEbmConfirm,
            Content = new TextBlock { Text = Strings.UninstallEbmConfirmBody, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = Strings.Remove,
            CloseButtonText = Strings.Cancel,
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;
        _update = null;
        _upToDate = false;
        await effect.UninstallAsync();
        Refresh();
    }
}
