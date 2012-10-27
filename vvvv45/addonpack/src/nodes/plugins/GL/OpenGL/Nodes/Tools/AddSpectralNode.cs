using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;
using OpenTK.Graphics.OpenGL;
using VVVV.Nodes.OpenGL.Utilities;
using OpenTK;
using OpenTK.Graphics;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace VVVV.Nodes.OpenGL
{
	#region PluginInfo
	[PluginInfo(Name = "+", Category = "OpenGL", Version = "Spectral", Help = "Combine many draw slices into a single slice", Tags = "")]
	#endregion PluginInfo
	public class AddSpectralNode : IPluginEvaluate
	{
		class CombinedLayer : ILayer
		{
			IEnumerable<ILayer> Layers;

			public CombinedLayer(IEnumerable<ILayer> Layers)
			{
				this.Layers = Layers;
			}

			public void Update()
			{
				foreach (var Layer in Layers)
					Layer.Update();
			}

			public void Draw()
			{
				foreach (var Layer in Layers)
					Layer.Draw();
			}
		}

		#region pins
		[Input("Input")]
		IDiffSpread<ISpread<ILayer>> FInInput;

		[Output("Output")]
		ISpread<ILayer> FOutOutput;
		#endregion

		bool FFirstRun = true;
		public void Evaluate(int SpreadMax)
		{
			if (FInInput.IsChanged)
			{
				FOutOutput.SliceCount = FInInput.SliceCount;

				for (int i = 0; i < FInInput.SliceCount; i++)
					FOutOutput[i] = new CombinedLayer(FInInput[i]);
			}
		}
	}
}
