#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
#endregion

namespace VVVV.Nodes.OpenCV.StructuredLight
{
	public class DecodeInstance : IDestinationInstance
	{
		CVImage FGreyscale = new CVImage();
		CVImage FHigh = new CVImage();
		CVImage FLow = new CVImage();
		public ScanSet ScanSet = new ScanSet();
		public IPayload Payload
		{
			set
			{
				ScanSet.Payload = value;
				ReInitialise();				
			}
		}

		public int Frame = 0;
		public bool Apply = false;

		public bool Ready
		{
			get
			{
				return ScanSet.Payload != null && FInput.Allocated && FGreyscale.Allocated;
			}
		}

		public override void Initialise()
		{
			FGreyscale.Initialise(FInput.ImageAttributes.Size, TColourFormat.L8);
			FHigh.Initialise(FGreyscale.ImageAttributes);
			FLow.Initialise(FGreyscale.ImageAttributes);
			ScanSet.Allocate(FInput.ImageAttributes.Size);
		}

		public override void Process()
		{
			if (!Ready)
				return;

			if (FNeedsReset)
			{
				FNeedsReset = false;
				ResetMaps();
			}

			if (ScanSet.Payload.Balanced)
			{
				bool positive = Frame % 2 == 0;
				FInput.GetImage(positive ? FHigh : FLow);

				if (!positive)
					ApplyBalanced(Frame / 2);
			}

			ScanSet.OnUpdateData();
		}

		unsafe void ApplyBalanced(int frame)
		{
			uint CameraPixelCount = FInput.ImageAttributes.PixelsPerFrame;

			lock (ScanSet)
			{
				fixed (ulong* dataFixed = &ScanSet.Data[0])
				{
					fixed (float* strideFixed = &ScanSet.Stride[0])
					{
						ulong* data = dataFixed;
						float* stride = strideFixed;

						byte* high = (byte*)FHigh.Data.ToPointer();
						byte* low = (byte*)FLow.Data.ToPointer();

						for (uint i = 0; i < CameraPixelCount; i++)
						{
							*stride++ = (float)(*high - *low);

							if (*high++ > *low++)
								*data++ |= (ulong)1 << frame;
							else
								*data++ &= ~((ulong)1 << frame);
						}
					}
				}
			}
		}

		[DllImport("msvcrt.dll")]
		private static unsafe extern void memset(void* dest, int c, int count);

		unsafe void ResetMaps()
		{
			if (!FInput.Allocated || !FGreyscale.Allocated)
				return;

			int CameraPixelCount = (int) FInput.ImageAttributes.PixelsPerFrame;

			fixed (ulong* dataFixed = &ScanSet.Data[0])
			{
				fixed (float* strideFixed = &ScanSet.Stride[0])
				{
					memset((void*) dataFixed, 0, sizeof(ulong) * CameraPixelCount);
					memset((void*) strideFixed, 0, sizeof(float) * CameraPixelCount);
				}
			}

			byte* high = (byte*)FHigh.Data.ToPointer();
			byte* low = (byte*)FLow.Data.ToPointer();

			memset((void*)high, 0, CameraPixelCount);
			memset((void*)low, 0, CameraPixelCount);
		}

		bool FNeedsReset = false;
		public void Reset()
		{
			FNeedsReset = true;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Decode", Category = "Image.StructuredLight", Help = "Decode structured light patterns", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class DecodeNode : IDestinationNode<DecodeInstance>
	{
		#region fields & pins
		[Input("Frame", MinValue = 0)]
		IDiffSpread<int> FPinInFrame;

		[Input("Apply")]
		IDiffSpread<bool> FPinInApply;

		[Input("Reset", IsBang=true)]
		IDiffSpread<bool> FPinInReset;

		[Input("Properties")]
		IDiffSpread<IPayload> FPinInProperties;

		[Output("Output")]
		ISpread<ScanSet> FPinOutOutput;

		[Import()]
		ILogger FLogger;

		bool FFirstRun = true;
		#endregion fields&pins

		[ImportingConstructor()]
		public DecodeNode()
		{

		}

		protected override void Update(int InstanceCount)
		{
			if (FPinInFrame.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Frame = FPinInFrame[i];

			if (FPinInApply.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Apply = FPinInApply[i];

			if (FPinInReset.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					if (FPinInReset[i])
						FProcessor[i].Reset();

			if (FPinInProperties.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Payload = FPinInProperties[i];

			FPinOutOutput.SliceCount = InstanceCount;
			for (int i = 0; i < InstanceCount; i++)
				FPinOutOutput[i] = FProcessor[i].ScanSet;
		}

	}
}
