#region usings

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2.EX9;

using OpenNI;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Drawing.Imaging;

#endregion usings

namespace VVVV.Nodes
{

	#region PluginInfo
	[PluginInfo(Name = "XYZ",
				Category = "Kinect",
				Version = "OpenNI")]
	#endregion PluginInfo
	public class XYZ : DXTextureOutPluginBase, IPluginEvaluate
	{
		[DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
		static extern void CopyMemory(IntPtr dest, IntPtr src, int size);

		#region fields & pins
		[Input("XYZ", IsSingle = true)]
		IDiffSpread<WorldData> FWorld;
		
		[Import()]
		ILogger FLogger;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public XYZ(IPluginHost host)
			: base(host)
		{ }

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FWorld.IsChanged)
				Reinitialize();

			if (FWorld[0] == null)
				return;

			if (FWorld[0].Fresh)
			{
				lock (FWorld[0].Lock)
				{
					Update();
					FWorld[0].Fresh = false;
				}
			}
		}

		#region IPluginDXTexture Members
		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int Slice, SlimDX.Direct3D9.Device device)
		{
			return new Texture(device, 640, 480, 1, Usage.None, Format.A32B32G32R32F, Pool.Managed);
		}

		//this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its texture, here you fill the texture with the actual data
		//this is called for each renderer, careful here with multiscreen setups, in that case
		//calculate the pixels in evaluate and just copy the data to the device texture here
		unsafe protected override void UpdateTexture(int Slice, Texture texture)
		{
			var rect = texture.LockRectangle(0, LockFlags.Discard).Data;
			fixed (float * dataFixed = & FWorld[0].data[0])
			{
				IntPtr data = (IntPtr) dataFixed;
				CopyMemory(rect.DataPointer, data, 640 * 480 * 4 * 4);
			}
			texture.UnlockRectangle(0);
		}

		#endregion IPluginDXResource Members
	}
}
