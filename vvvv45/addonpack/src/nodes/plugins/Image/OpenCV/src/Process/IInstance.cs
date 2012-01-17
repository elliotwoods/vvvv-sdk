using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IInstance : IDisposable
	{
		/// <summary>
        /// Initialise image assets (e.g. FOutput, the output image)
		/// and any other internal assets
        /// </summary>
		virtual public void Initialise() { }
		public abstract void Process();

		private Object FLockStatus = new Object();
		private string FStatus;
		public string Status
		{
			get
			{
				lock (FLockStatus)
					return FStatus;
			}
			set
			{
				lock (FLockStatus)
					FStatus = value;
			}
		}

		protected bool FNeedsInit = true;
		virtual public bool NeedsInitialise()
		{
			if (FNeedsInit)
			{
				FNeedsInit = false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Reinitialise internals
		/// </summary>
		public void ReInitialise()
		{
			FNeedsInit = true;
		}

		virtual public void Dispose()
		{

		}
	}
}
