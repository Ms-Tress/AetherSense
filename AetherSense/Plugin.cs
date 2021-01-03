﻿using Buttplug;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Threading.Tasks;
using AetherSense.Triggers;
using System.IO;
using AetherSense.Patterns;

namespace AetherSense
{
	public class Plugin : IDalamudPlugin
	{
		public static DalamudPluginInterface DalamudPluginInterface;
		public static Configuration Configuration;
		public static Devices Devices = new Devices();
		public static ButtplugClient Buttplug;

		public string Name => "AetherSense";

		private bool enabled;
		private bool debugVisible;
		private bool configurationIsVisible;
		private bool triggersLoaded = false;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			DalamudPluginInterface = pluginInterface;
			////Configuration = DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			Configuration = Configuration.Load();
			DalamudPluginInterface.CommandManager.AddHandler("/sense", "Opens the Aether Sense configuration window", this.OnSense);
			DalamudPluginInterface.CommandManager.AddHandler("/senseDebug", "Opens the Aether Sense debug window", this.OnDebug);
			DalamudPluginInterface.UiBuilder.OnBuildUi += OnGui;
			DalamudPluginInterface.UiBuilder.OnOpenConfigUi += (s, e) => this.configurationIsVisible = true;

			Task.Run(this.InitializeAsync);

			this.LoadTriggers();
		}

		/// <summary>
		/// Initializes the plugin in mock test mode outside of the game
		/// </summary>
		/// <param name="configuration">the plugin configuration to use</param>
		/// <returns>an action to be invoked for ImGUI drawing</returns>
		public Action InitializeMock()
		{
			this.configurationIsVisible = true;
			this.debugVisible = true;
			Configuration = Configuration.Load();
			Task.Run(this.InitializeAsync);
			return this.OnGui;
		}

		public async Task InitializeAsync()
		{
			PluginLog.Information("Initializing Buttplug Interface");
			this.enabled = true;

			try
			{
				Buttplug = new ButtplugClient("Aether Sense");
				Buttplug.DeviceAdded += this.OnDeviceAdded;
				Buttplug.DeviceRemoved += this.OnDeviceRemoved;
				Buttplug.ScanningFinished += (o, e) =>
				{
					PluginLog.Information("Scan for devices complete");
					/*Task.Run(async () =>
					{
						await client.StopScanningAsync();
						await client.StartScanningAsync();
					});*/
				};

				PluginLog.Information("Connect to embedded buttplug server");
				ButtplugEmbeddedConnectorOptions connectorOptions = new ButtplugEmbeddedConnectorOptions();
				connectorOptions.ServerName = "Aether Sense Server";
				await Buttplug.ConnectAsync(connectorOptions);

				PluginLog.Information("Scan for devices");
				await Buttplug.StartScanningAsync();

				while (this.enabled)
				{
					await Devices.Write(32);

					// 33 ms = 30fps max
					await Task.Delay(32);
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Buttplug exception");
			}
		}

		private void LoadTriggers()
		{
			PluginLog.Information("Loading Triggers");

			// Don't attempt to load triggers if we're not actually in-game
			if (Plugin.DalamudPluginInterface == null)
				return;

			this.triggersLoaded = true;
			foreach (TriggerBase trigger in Configuration.Triggers)
			{
				if (!trigger.Enabled)
					continue;

				PluginLog.Information("    > " + trigger.Name);
				trigger.Attach();
			}
		}

		private void Unloadtriggers()
		{
			PluginLog.Information("Unloading Triggers");
			this.triggersLoaded = false;
			foreach (TriggerBase trigger in Configuration.Triggers)
			{
				trigger.Detach();
			}
		}

		public void Dispose()
		{
			DalamudPluginInterface.CommandManager.ClearHandlers();
			DalamudPluginInterface.Dispose();

			this.enabled = false;
		}

		public void OnGui()
		{
			if (this.debugVisible)
			{
				if (ImGui.Begin("Aether Sense Status", ref this.debugVisible))
				{
					DebugWindow.OnGui();
				}
			}

			if (!this.configurationIsVisible)
			{
				if (!this.triggersLoaded)
				{
					Configuration.Save();
					this.LoadTriggers();
				}

				return;
			}

			if (this.triggersLoaded)
				this.Unloadtriggers();

			if (ImGui.Begin("Aether Sense", ref this.configurationIsVisible))
			{
				ConfigurationEditor.OnGui();
			}

			ImGui.End();
		}

		private void OnDeviceAdded(object sender, DeviceAddedEventArgs e)
		{
			PluginLog.Information("Device {0} added", e.Device.Name);
			Devices.AddDevice(e.Device);
		}

		private void OnDeviceRemoved(object sender, DeviceRemovedEventArgs e)
		{
			PluginLog.Information("Device {0} removed", e.Device.Name);
			Devices.RemoveDevice(e.Device);
		}

		private void OnSense(string args)
		{
			this.configurationIsVisible = true;
		}

		private void OnDebug(string args)
		{
			this.debugVisible = true;
		}
	}
}
