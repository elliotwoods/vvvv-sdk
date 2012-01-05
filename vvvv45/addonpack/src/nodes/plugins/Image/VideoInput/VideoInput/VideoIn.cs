using System;
using System.ComponentModel.Composition;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;


using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using VVVV.Nodes.OpenCV;
using VideoInputSharp;

namespace VVVV.Nodes.VideoInput
{
	public class VideoInInstance : IGeneratorInstance
	{
		bool FRunning = false;

		Capture FCapture = new Capture();

		public int DeviceID = 0;
		public int Width = 640;
		public int Height = 480;
		public int Framerate = 30;

		protected override void Enable()
		{
			Open();
		}

		protected override void Disable()
		{
			Close();
		}

		public void Open()
		{
			Close();

			if (!FEnabled)
				return;

			try
			{
				FCapture.Open(DeviceID, Framerate, Width, Height);
				FRunning = true;
				Status = "OK";
			}
			catch (Exception e)
			{
				FRunning = false;
				Status = e.Message;
			}
		}

		void Close()
		{
			if (!FRunning)
				return;

			try
			{
				FCapture.Close();
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
			FRunning = false;
		}

		public void ShowSettings()
		{
			FCapture.Settings();
		}

		protected override void Generate()
		{
			
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "OpenCV", Version = "DirectShow", Help = "Captures video from DirectShow devices", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins
		[Input("Device ID")]
		IDiffSpread<int> FPinInDeviceID;

		[Input("Width", MinValue=32, MaxValue=8192, DefaultValue=640)]
		IDiffSpread<int> FPinInWidth;
		
		[Input("Height", MinValue=32, MaxValue=8192, DefaultValue=480)]
		IDiffSpread<int> FPinInHeight;

		[Input("FPS", MinValue=1, DefaultValue=30)]
		IDiffSpread<int> FPinInFPS;

		[Input("Show Settings", IsBang = true)]
		ISpread<bool> FPinInShowSettings;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public VideoInNode(IPluginHost host)
		{
		
		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount)
		{
			bool needsOpen = false;

			if (FPinInDeviceID.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DeviceID = FPinInDeviceID[i];
				needsOpen = true;
			}

			if (FPinInWidth.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FPinInWidth[i];
				needsOpen = true;
			}

			if (FPinInHeight.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FPinInDeviceID[i];
				needsOpen = true;
			}

			if (FPinInFPS.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Framerate = FPinInFPS[i];
				needsOpen = true;
			}

			if (needsOpen)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Open();

			for (int i = 0; i < InstanceCount; i++)
				if (FPinInShowSettings[i])
					FProcessor[i].ShowSettings();
		}
	}
}
