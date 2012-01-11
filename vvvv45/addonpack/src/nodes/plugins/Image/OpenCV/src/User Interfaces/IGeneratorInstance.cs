using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;

		public virtual bool NeedsThread()
		{
			return true;
		}

        /// <summary>
        /// Initialise image assets (e.g. FOutput, the output image)
        /// </summary>
		public virtual void Initialise() { }

        /// <summary>
        /// Open the device for capture
        /// </summary>
        protected abstract void Open();
        /// <summary>
        /// Close the capture device
        /// </summary>
        protected abstract void Close();

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

        public void ReInitialise()
        {
            FNeedsInit = true;
        }

		public void Process()
		{
			Generate();
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// For threaded generators you must override this function
		/// For non-threaded generators, you use your own function
		/// </summary>
		protected virtual void Generate() { }

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

		protected bool FEnabled;
		public bool Enabled
		{
			get
			{
				return FEnabled;
			}
			set
			{
				if (FEnabled == value)
					return;

				FEnabled = value;
				if (FEnabled)
					Open();
				else
					Close();

			}
		}

		virtual public void Dispose()
		{
            Close();
		}
	}
}
