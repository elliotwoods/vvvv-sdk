using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IProcess<T>
	{
		protected Spread<T> FProcess = new Spread<T>(0);

		protected Thread FThread;
		protected bool FThreadRunning = false;
		protected Object FLockProcess = new Object();
	}
}
