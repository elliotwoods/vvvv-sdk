#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
#endregion

namespace VVVV.Nodes.OpenCV.StructuredLight
{
	public class CameraSpaceInstance : IStaticGeneratorInstance
	{
		ScanSet FScanSet = null;
		public ScanSet ScanSet
		{
			set
			{
				if (value != null)
				{
					FScanSet = value;
					ReInitialise();
					AddListeners();
				}
				else
				{
					if (FScanSet != null)
					{
						RemoveListeners();
						FScanSet = null;
					}
				}
			}
		}

		float FThreshold = 0.0f;
		public float Threshold
		{
			set
			{
				FThreshold = value;
				ReInitialise();
			}
		}

		public override void Initialise()
		{
			if (Allocated)
				FOutput.Image.Initialise(FScanSet.CameraSize, TColourFormat.L8);
		}

		public override bool NeedsThread()
		{
			return false;
		}

		bool Allocated
		{
			get
			{
				lock (this)
				{
					if (FScanSet == null)
						return false;
					else
						return FScanSet.Allocated;
				}
			}
		}

		int count = 0;
		unsafe void UpdateData()
		{
			if (Allocated)
			{
				Status = (count++).ToString();

				lock (this)
				{ 
					int PixelCount = FScanSet.CameraPixelCount;
					byte* p = (byte*)FOutput.Data.ToPointer();

					int factor = (int)(Math.Log((double)FScanSet.Payload.PixelCount) / Math.Log(2)) - 8;
					fixed (ulong* indexFixed = &FScanSet.Data[0])
					{
						fixed (float* strideFixed = &FScanSet.Stride[0])
						{
							float threshold = FThreshold * 255.0f;

							ulong* index = indexFixed;
							float* stride = strideFixed;

							ulong decoded = 0;

							if (factor > 0)
							{
								for (int i = 0; i < PixelCount; i++)
								{
									if (!FScanSet.GetValue(*index++, ref decoded))
										continue;
									if (Math.Abs(*stride++) > threshold)
										*p++ = (byte)((decoded >> factor) & ~((ulong)1 << 8));
									else
										*p++ = 0;
								}
							}
							else
							{
								for (int i = 0; i < PixelCount; i++)
								{
									decoded = FScanSet.Payload.DataInverse[*index++];
									if (Math.Abs(*stride++) > threshold)
										*p++ = (byte)((decoded << (-factor)) & ~((ulong)1 << 8));
									else
										*p++ = 0;
								}
							}

						}
					}
				}

			}

			FOutput.Send();
		}

		void AddListeners()
		{
			RemoveListeners();

			lock (this)
			{
				FScanSet.UpdateAttributes += new EventHandler(FScanSet_UpdateAttributes);
				FScanSet.UpdateData += new EventHandler(FScanSet_UpdateData);
			}
		}

		void RemoveListeners()
		{
			lock (this)
			{
				FScanSet.UpdateAttributes -= FScanSet_UpdateAttributes;
				FScanSet.UpdateData -= FScanSet_UpdateData;
			}
		}

		void FScanSet_UpdateData(object sender, EventArgs e)
		{
			UpdateData();
		}

		void FScanSet_UpdateAttributes(object sender, EventArgs e)
		{
			ReInitialise();
		}	
	}

	#region PluginInfo
	[PluginInfo(Name = "CameraSpace", Category = "Image.StructuredLight", Help = "Preview structured light data", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class CameraSpaceNode : IGeneratorNode<CameraSpaceInstance>
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<ScanSet> FPinInInput;

		[Input("Threshold", MinValue=0, MaxValue=1)]
		IDiffSpread<float> FPinInThreshold;

		[Import()]
		ILogger FLogger;

		CVImage FOutput = new CVImage();
		ScanSet FScanSet;
		bool FFirstRun = true;

		bool FDataUpdated = false;
		bool FAttributesUpdated = false;
		bool FAllocated = false;
		#endregion fields&pins

		[ImportingConstructor()]
		public CameraSpaceNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInInput.IsChanged)
				for (int i=0; i<InstanceCount; i++)
					FProcessor[i].ScanSet = FPinInInput[i];

			if (FPinInThreshold.IsChanged)
				for (int i=0; i<InstanceCount; i++)
					FProcessor[i].Threshold = FPinInThreshold[i];
		}

	}
}
