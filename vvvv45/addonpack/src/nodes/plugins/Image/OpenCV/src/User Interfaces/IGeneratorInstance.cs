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
		
		protected bool FRunning = false;

        /// <summary>
        /// Initialise image assets (e.g. FOutput, the output image)
        /// </summary>
		public virtual void Initialise() { }

        /// <summary>
        /// Open the device for capture. This is called from inside the thread
        /// </summary>
        protected abstract void Open();
        /// <summary>
        /// Close the capture device. This is called from inside the thread
        /// </summary>
        protected abstract void Close();

		bool FNeedsOpen = false;
		bool FNeedsClose = false;
		/// <summary>
		/// Message the thread to start the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Start()
		{
			FNeedsOpen = true;
		}
		/// <summary>
		/// Message the thread to stop the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Stop()
		{
			FNeedsClose = true;
		}
		/// <summary>
		/// Used to restart the device (e.g. you change a setting)
		/// </summary>
		public void Restart()
		{
			FNeedsClose = true;
			FNeedsOpen = true;
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

        public void ReInitialise()
        {
            FNeedsInit = true;
        }

		public void Process()
		{
			if (FNeedsClose)
			{
				FNeedsClose = false;
				Close();
				FEnabled = false;
				return;
			}

			if (FNeedsOpen)
			{
				FNeedsOpen = false;
				Open();
				Initialise();
			}

			if (FEnabled)
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

		private bool FEnabled;
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

				if (value)
				{
					FEnabled = true;
					Start();
				}
				else
				{
					Stop();
				}
			}
		}

		virtual public void Dispose()
		{
			Enabled = false;
		}
	}
}
