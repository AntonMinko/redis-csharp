using System.Text.Json;
using static System.Console;

namespace codecrafters_redis.UserSettings;

public interface IUserSettingsProvider
{
    UserSettings GetUserSettings();
    Task SaveUserSettingsAsync(UserSettings userSettings);
}

public class UserSettingsProvider: IUserSettingsProvider
{
    private UserSettings? _userSettings = null;
    
    public UserSettings GetUserSettings()
    {
        return _userSettings!;
    }
    
    public async Task InitializeUserSettingsAsync()
    {
        try
        {
            var settingsFullName = ResolveSettingsFullName();
            WriteLine($"Settings file: {settingsFullName}");
            
            await using var stream = File.OpenRead(settingsFullName);
            _userSettings = await JsonSerializer.DeserializeAsync<UserSettings>(stream) ?? UserSettings.Default;
        }
        catch (Exception)
        {
            _userSettings = UserSettings.Default;
        }
    }

    public async Task SaveUserSettingsAsync(UserSettings? userSettings = null)
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
        string appDataDir = UserSettings.GetAppDataDir();
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