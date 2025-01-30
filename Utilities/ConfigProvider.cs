using ImagePlastic.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace ImagePlastic.Utilities;

public static class ConfigProvider
{
    private static readonly JsonSerializer serializer = JsonSerializer.Create(new()
    {
        Converters = { }
    });
    public static Config LoadConfig()
    {
        if (!File.Exists(Path.GetDirectoryName(Environment.ProcessPath) + @"\IPConfig.json")) return new();

        using StreamReader configFile = new(Environment.CurrentDirectory + @"\IPConfig.json");
        using JsonReader reader = new JsonTextReader(configFile);
        Config? config = null;
        try { config = serializer.Deserialize<Config>(reader); } catch (Exception e) { Trace.WriteLine(e); }
        return config ?? new();
    }
    public static async void Save(this Config config)
    {
        await using StreamWriter configFile = new(Environment.CurrentDirectory + @"\IPConfig.json");
        await using JsonWriter writer = new JsonTextWriter(configFile);
        serializer.Serialize(writer, config);
    }
}