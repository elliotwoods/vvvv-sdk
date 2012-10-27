#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;

using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenGL
{
	public interface ILayer
	{
		/// <summary>
		/// This update call is performed once per frame
		/// </summary>
		void Update();

		/// <summary>
		/// This draw call is performed once per device.
		/// </summary>
		void Draw();
	}

	public abstract class ILayerNode : IPluginEvaluate, ILayer
	{
		#region fields & pins
		[Output("Layer")]
		ISpread<ILayerNode> FPinOutLayer;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ILayerNode()
		{

		}

		protected int SpreadMax = 0;

		private bool FFirstRun = true;
		public void Evaluate(int SpreadMax)
		{
			this.SpreadMax = SpreadMax;

			if (FFirstRun)
			{
				FPinOutLayer[0] = this;
				FFirstRun = false;
			}

			Update();
		}

		public virtual void Update() { }
		public abstract void Draw();
	}
}