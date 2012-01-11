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
using System.Runtime.InteropServices;

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

		protected override void Open()
		{
			Close();

			if (!FEnabled)
				return;

			try
			{
				FCapture.Open(DeviceID, Framerate, Width, Height);
				FOutput.Image.Initialise(new Size(FCapture.GetWidth(), FCapture.GetHeight()), TColourFormat.RGB8);

				FRunning = true;
				Status = "OK";
			}
			catch (Exception e)
			{
				FRunning = false;
				Status = e.Message;
			}
		}

        protected override void Close()
		{
			if (!FRunning)
				return;

			try
			{
				FCapture.Close();
				FRunning = false;
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
			FCapture.ShowSettings();
		}

		protected override void Generate()
		{
			GetPixels();
			FOutput.Send();
		}

        [DllImport("msvcrt.dll")]
        private static unsafe extern void memset(IntPtr dest, int c, int count);

		private unsafe void GetPixels()
		{
			void* data = FOutput.Image.Data.ToPointer();
			FCapture.GetPixels(data);
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
					FProcessor[i].Height = FPinInHeight[i];
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
					FProcessor[i].ReInitialise();

			for (int i = 0; i < InstanceCount; i++)
				if (FPinInShowSettings[i])
					FProcessor[i].ShowSettings();
		}
	}
}
