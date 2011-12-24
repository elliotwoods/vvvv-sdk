﻿using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.EmguCV
{
	public class GaussianBlurInstance : IFilterInstance
	{
		public int Width = 3;

		public override void Initialise()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (Width == 0)
				FOutput.Image.SetImage(FInput.Image);
			else
			{
				if (!FInput.LockForReading())
					return;
				CvInvoke.cvSmooth(FInput.CvMat, FOutput.CvMat, SMOOTH_TYPE.CV_GAUSSIAN, Width*2+1, 0, 0, 0);
				FInput.ReleaseForReading();
			}
			
			FOutput.Send();

		}

	}

	#region PluginInfo
	[PluginInfo(Name = "GaussianBlur", Category = "EmguCV", Help = "Perform LK optical flow on image", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class GaussianBlurNode : IFilterNode<GaussianBlurInstance>
	{
		[Input("Width", IsSingle = true, DefaultValue=3, MinValue=0, MaxValue=64)]
		IDiffSpread<int> FPinInWidth;

		protected override void Update(int SpreadMax)
		{
			if (FPinInWidth.IsChanged)
				for (int i=0; i<SpreadMax; i++)
					FProcessor[i].Width = FPinInWidth[0];
		}
	}
}
