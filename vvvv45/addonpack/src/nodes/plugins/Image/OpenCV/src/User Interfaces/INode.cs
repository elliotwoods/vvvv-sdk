using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV
{
	public abstract class INode : IPluginEvaluate
	{
		/// <summary>
		/// The call from VVVV. This is handled by IGeneratorNode, IFilterNode, IDestinationNode, etc
		/// </summary>
		/// <param name="SpreadMax"></param>
		public abstract void Evaluate(int SpreadMax);

		/// <summary>
		/// The internal call to update. You need to override this in your node definition
		/// </summary>
		/// <param name="InstanceCount">SliceCount of FProcessor</param>
		/// <param name="SpreadChanged">true if instances in FProcessor have changed</param>
		protected abstract void Update(int InstanceCount, bool SpreadChanged);

	}
}
