﻿using System;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ImGuiNET
{
	public class Program
	{
		private static AetherSense.Plugin plugin = new AetherSense.Plugin(null);

		private static Sdl2Window _window;
		private static GraphicsDevice _gd;
		private static CommandList _cl;
		private static ImGuiController _controller;
		private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);

		public static void Main(string[] args)
		{
			LoggerConfiguration loggers = new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext();
			loggers.WriteTo.Logger(logger => logger.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose));
			Log.Logger = loggers.CreateLogger();
			Log.Logger.Information("Logger is initialized");

			Action a = plugin.InitializeMock();

			// Create window, GraphicsDevice, and all resources necessary for the demo.
			VeldridStartup.CreateWindowAndGraphicsDevice(
				new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program"),
				new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
				out _window,
				out _gd);
			_window.Resized += () =>
			{
				_gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
				_controller.WindowResized(_window.Width, _window.Height);
			};
			_cl = _gd.ResourceFactory.CreateCommandList();
			_controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
			Random random = new Random();

			// Main application loop
			while (_window.Exists)
			{
				InputSnapshot snapshot = _window.PumpEvents();
				if (!_window.Exists) { break; }
				_controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

				a.Invoke();

				ImGui.ShowDemoWindow();

				_cl.Begin();
				_cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
				_cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
				_controller.Render(_gd, _cl);
				_cl.End();
				_gd.SubmitCommands(_cl);
				_gd.SwapBuffers(_gd.MainSwapchain);
			}

			// Clean up Veldrid resources
			_gd.WaitForIdle();
			_controller.Dispose();
			_cl.Dispose();
			_gd.Dispose();
		}
	}
}
