using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;
		
		protected bool FRunning = false;

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

		override public void Process()
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

		override public void Dispose()
		{
			Enabled = false;
		}
	}
}
