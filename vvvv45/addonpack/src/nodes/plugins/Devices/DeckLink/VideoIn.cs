#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using DeckLinkAPI;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "DeckLink", Help = "Capture video from a BlackMagic DeckLink device", Tags = "", Author = "Elliot Woods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<IDeckLink> FDevices;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public VideoInNode(IPluginHost host)
		{

		}

		bool FFirstRun = true;
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FDevices.IsChanged)
			{

			}
		}
	}
}
