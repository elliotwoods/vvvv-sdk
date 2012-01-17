using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IFilterInstance : IInstance, IInstanceInput, IInstanceOutput, IDisposable
	{
		protected CVImageInput FInput;
		protected CVImageOutput FOutput;

		public void SetInput(CVImageInput input)
		{
			FInput = input;
		}

		public bool HasInput(CVImageInput input)
		{
			return FInput == input;
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// Override this with false if your filter
		/// doesn't need to run every frame
		/// </summary>
		/// <returns></returns>
		virtual public bool IsFast()
		{
			return true;
		}
	}
}
