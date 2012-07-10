#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

#endregion usings

//here you can change the vertex type
using VertexType = VVVV.Utils.SlimDX.TexturedVertex;
using DeckLinkAPI;
using System.Collections.Generic;

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
				Category = "DeckLink",
				Version = "EX9.Texture",
				Help = "Basic template which creates a texture",
				Author = "Elliot Woods",
				Credits = "Lumacoustics",
				Tags = "")]
	#endregion PluginInfo
	public class Template : DXTextureOutPluginBase, IPluginEvaluate, IDisposable
	{
		#region fields & pins

		[Input("Device")]
		IDiffSpread<IDeckLink> FPinInDevice;

		[Input("Video mode")]
		IDiffSpread<_BMDDisplayMode> FPinInMode;

		[Input("Flags")]
		IDiffSpread<_BMDVideoInputFlags> FPinInFlags;

		[Input("Flush Streams", IsBang = true)]
		ISpread<bool> FPinInFlush;

		[Output("Frames Available")]
		ISpread<int> FPinOutFramesAvailable;

		[Output("Status")]
		ISpread<string> FStatus;

		List<Capture> FCaptures = new List<Capture>();
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public Template(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			SetSliceCount(SpreadMax);
			FStatus.SliceCount = SpreadMax;
			FPinOutFramesAvailable.SliceCount = SpreadMax;

			while (FCaptures.Count < SpreadMax)
			{
				FCaptures.Add(new Capture());
			}
			while (FCaptures.Count > SpreadMax)
			{
				FCaptures[FCaptures.Count - 1].Dispose();
				FCaptures.RemoveAt(FCaptures.Count - 1);
			}

			if (FPinInMode.IsChanged || FPinInDevice.IsChanged || FPinInFlags.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
					ReOpen(i);
				Reinitialize();
			}

			bool reinitialise = false;
			foreach (var capture in FCaptures)
			{
				reinitialise |= capture.Reinitialise;
			}
			if (reinitialise)
				Reinitialize();

			bool freshdata = false;
			foreach (var capture in FCaptures)
				freshdata |= capture.FreshData;
			if (freshdata)
				Update();

			for (int i = 0; i < SpreadMax; i++)
			{
				FPinOutFramesAvailable[i] = FCaptures[i].AvailableFrameCount;
			}
		}

		void ReOpen(int index)
		{
			try
			{
				FCaptures[index].Open(FPinInDevice[index], FPinInMode[index], FPinInFlags[index]);
				FStatus[index] = "OK";
			}

			catch (Exception e)
			{
				FStatus[index] = e.Message;
			}
		}

		protected override Texture CreateTexture(int Slice, Device device)
		{
			FCaptures[Slice].Reinitialised();
			return new Texture(device, Math.Max(FCaptures[Slice].Width / 2, 1), Math.Max(FCaptures[Slice].Height, 1), 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
		}

		protected unsafe override void UpdateTexture(int Slice, Texture texture)
		{
			if (FCaptures[Slice].FreshData && FCaptures[Slice].Ready)
			{
				Surface srf = texture.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(LockFlags.Discard);
				
				FCaptures[Slice].Lock.AcquireReaderLock(500);

				try
				{
					rect.Data.WriteRange(FCaptures[Slice].Data, FCaptures[Slice].BytesPerFrame);
					FCaptures[Slice].Updated();
				}
				catch
				{

				}
				finally
				{
					srf.UnlockRectangle();
					FCaptures[Slice].Lock.ReleaseReaderLock();
				}
			}
		}

		public void Dispose()
		{
			foreach (var capture in FCaptures)
				capture.Dispose();
		}
	}
}
