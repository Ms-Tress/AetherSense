using AetherSense.Triggers;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AetherSense
{
	[Serializable]
	public class Configuration
	{
		public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
		{
			TypeNameHandling = TypeNameHandling.Objects,
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
			Formatting = Formatting.Indented,
		};

		public bool Enabled { get; set; } = true;
		public string ServerAddress { get; set; } = "ws://127.0.0.1:12345";
		public List<TriggerBase> Triggers { get; set; } = new List<TriggerBase>();
		public Dictionary<string, int> DeviceGroups { get; set; } = new Dictionary<string, int>();

		public static Configuration Load()
		{
			string path = GetPath();

			if (!File.Exists(path))
			{
				PluginLog.Information("Loading default configuration");
				return GetDefaultConfiguration();
			}

			string json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<Configuration>(json, Settings);
		}

		public static Configuration GetDefaultConfiguration()
		{
			string dir = Plugin.DalamudPluginInterface.GetPluginDirectory();
			string path = Path.Combine(dir, "DefaultConfiguration.json");

			if (!File.Exists(path))
			{
				throw new Exception($"Unable to locate default configuration at {path}");
			}

			string json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<Configuration>(json, Settings);
		}

		public void Save()
		{
			string json = JsonConvert.SerializeObject(this, Settings);
			string path = GetPath();
			File.WriteAllText(path, json);
			////Plugin.DalamudPluginInterface.SavePluginConfig(this);
		}

		private static string GetPath()
		{
			if (Plugin.DalamudPluginInterface == null)
				return "config.json";

			string path = Path.Combine(Plugin.DalamudPluginInterface.GetPluginConfigDirectory() + "/config.json");

			if (File.Exists(path))
				return path;

			// Migrate old config name
			string oldPath = Path.Combine(Plugin.DalamudPluginInterface.GetPluginConfigDirectory() + "_custom.json");
			if (File.Exists(oldPath))
				File.Move(oldPath, path);

			// Magrate other old config name
			string oldPath2 = Path.Combine(Plugin.DalamudPluginInterface.GetPluginConfigDirectory() + "config.json");
			if (File.Exists(oldPath2))
				File.Move(oldPath2, path);

			return path;
		}
	}
}
