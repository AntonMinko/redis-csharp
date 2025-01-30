using System.Text.Json;
using static System.Console;

namespace codecrafters_redis.UserSettings;

public interface IUserSettingsProvider
{
    Task InitializeUserSettingsAsync();
    Settings GetUserSettings();
    Task SaveUserSettingsAsync(Settings settings);
}

public class UserSettingsProvider: IUserSettingsProvider
{
    private Settings? _userSettings;
    
    public Settings GetUserSettings()
    {
        return _userSettings!;
    }
    
    public async Task InitializeUserSettingsAsync()
    {
        if (_userSettings != null) return;
        
        try
        {
            var settingsFullName = ResolveSettingsFullName();
            WriteLine($"Settings file: {settingsFullName}");
            
            await using var stream = File.OpenRead(settingsFullName);
            _userSettings = await JsonSerializer.DeserializeAsync<Settings>(stream) ?? Settings.Default;
        }
        catch (Exception)
        {
            _userSettings = Settings.Default;
        }
    }

    public async Task SaveUserSettingsAsync(Settings? userSettings = null)
    {
        if (userSettings != null)
        {
            _userSettings = userSettings;
        }
        var settingsFullName = ResolveSettingsFullName();
        await using var stream = File.OpenWrite(settingsFullName);
        await JsonSerializer.SerializeAsync(stream, _userSettings);
    }

    private string ResolveSettingsFullName()
    {
        string appDataDir = Settings.GetAppDataDir();
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }
        
        var path = Path.Combine(appDataDir, "settings.json");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{}");
        }
        
        return path;
    }
}