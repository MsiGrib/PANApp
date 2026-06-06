using PANApp.Models.Configs;
using PANApp.Services.Implementations;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace PANApp.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _startWithWindows;
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => this.RaiseAndSetIfChanged(ref _startWithWindows, value);
    }

    private string _settingsStatus = "Settings loaded";
    public string SettingsStatus
    {
        get => _settingsStatus;
        set => this.RaiseAndSetIfChanged(ref _settingsStatus, value);
    }

    private AppConfig _currentSettings = null!;

    public ReactiveCommand<Unit, Unit> OnStartWithWindowsChangedCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }

    public SettingsViewModel()
    {
        OnStartWithWindowsChangedCommand = ReactiveCommand.Create(OnStartWithWindowsChanged);
        ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);

        LoadSettings();
    }

    public static string GetSettingsPathForDebug()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PANApp", "appsettings.json");

    private void SaveSettings()
    {
        _currentSettings = _currentSettings with
        {
            StartWithWindows = StartWithWindows
        };

        AppSettingsService.Save(_currentSettings);
        AppSettingsService.ApplyStartWithWindows(StartWithWindows);

        SettingsStatus = $"Save ✓ {DateTime.Now:HH:mm:ss}";

        _ = Task.Delay(2000).ContinueWith(_ =>
            SettingsStatus = $"Last save: {DateTime.Now:HH:mm:ss}",
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void LoadSettings()
    {
        _currentSettings = AppSettingsService.Load();

        var settingsPath = GetSettingsPathForDebug();
        Console.WriteLine($"[SETTINGS] Config path: {settingsPath}");
        Console.WriteLine($"[SETTINGS] File exists: {File.Exists(settingsPath)}");

        Console.WriteLine($"[SETTINGS] Config value: StartWithWindows={_currentSettings.StartWithWindows}");

        StartWithWindows = _currentSettings.StartWithWindows;

        SettingsStatus = $"Loaded: {_currentSettings.LastModified:HH:mm:ss}";
    }

    private void OnStartWithWindowsChanged()
        => SaveSettings();

    private void ResetToDefaults()
    {
        StartWithWindows = false;
        SaveSettings();
        SettingsStatus = "Reset to default values ✓";
    }
}