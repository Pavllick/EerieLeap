namespace EerieLeap;

public static class AppConstants {
    public static string ConfigDirPath => GetConfigDirPath();

    public const string SettingsConfigFileName = "settings";
    public const string AdcConfigFileName = "adc";
    public const string SensorsConfigFileName = "sensors";
    public const string AdcConfigScriptFileName = "adc_config_script";

    private static string GetConfigDirPath() {
        string configDirPath;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            configDirPath = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            configDirPath = AppDomain.CurrentDomain.BaseDirectory;
        else
            throw new PlatformNotSupportedException("Unsupported platform");

        return Path.Combine(configDirPath, "configuration");
    }
}
