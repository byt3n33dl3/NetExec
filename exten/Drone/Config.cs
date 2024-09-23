using System.Collections.Generic;

namespace Drone;

public sealed class Config
{
    private readonly Dictionary<Setting, object> _configs = new();

    public Config()
    {
        Set(Setting.SLEEP_INTERVAL, SleepTime);
        Set(Setting.SLEEP_JITTER, SleepJitter);
    }

    public T Get<T>(Setting setting)
    {
        if (!_configs.ContainsKey(setting))
            return default;

        return (T)_configs[setting];
    }

    public void Set(Setting setting, object value)
    {
        if (!_configs.ContainsKey(setting))
            _configs.Add(setting, null);

        _configs[setting] = value;
    }

    private static int SleepTime => int.Parse("5");
    private static int SleepJitter => int.Parse("0");
}

public enum Setting
{
    SLEEP_INTERVAL,
    SLEEP_JITTER
}