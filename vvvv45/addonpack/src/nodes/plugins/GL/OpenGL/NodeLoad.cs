using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using VVVV.Nodes.OpenGL;
using VVVV.Nodes.OpenGL.Utilities;
using VVVV.Core.Logging;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.OpenFrameworks
{
    class NodeFactorySet : IDisposable
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr LoadLibrary(String dllname);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, String procname);

        public class NodeFactory : IDisposable
        {
			public class NodeInstance : IDisposable
			{
				public NodeInstance(NodeFactory Factory)
				{
					this.Factory = Factory;
					this.Handle = Factory.CreateNode();
				}

				public int Handle {get; private set;}
				public NodeFactory Factory {get; private set;}

				public void Dispose()
				{
					Factory.DestroyNode(this.Handle);
				}
			}

            IntPtr FLibrary;
            public NodeCreateDelegate CreateNode;
			public NodeArgumentlessDelegate DestroyNode;
			public NodeArgumentlessDelegate SetupNode;
			List<NodeInstance> FInstances = new List<NodeInstance>();

            bool FLoaded = false;

            public NodeFactory(string DllFilename)
            {
				try
				{
					if (!File.Exists(DllFilename))
						throw(new Exception("File not found"));

					FLibrary = LoadLibrary(@DllFilename);
					CreateNode = (NodeCreateDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeCreate"), typeof(NodeCreateDelegate));
					DestroyNode = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeDestroy"), typeof(NodeArgumentlessDelegate));
					SetupNode = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeSetup"), typeof(NodeArgumentlessDelegate));

					this.FLoaded = true;
				}
				catch (Exception e)
				{
					this.FLoaded = false;
					throw (e);
				}
            }

			public NodeInstance CreteNode()
			{
				var instance = new NodeInstance(this);
				this.FInstances.Add(instance);
				return instance;
			}
        
			public void Dispose()
			{
				if (this.FLoaded)
				{

					FreeLibrary(FLibrary);
				}
			}
		}

        Dictionary<string, NodeFactory> FFactories = new Dictionary<string, NodeFactory>();

        public NodeFactory GetFactory(string DllFilename)
        {
            if (!FFactories.ContainsKey(DllFilename))
            {
                FFactories.Add(DllFilename, new NodeFactory(DllFilename));
            }
            return FFactories[DllFilename];
        }

        public void Dispose()
        {
            foreach(var factory in FFactories.Values)
            {
                factory.Dispose();
            }
			FFactories.Clear();
        }
	}

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int NodeCreateDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void NodeArgumentlessDelegate(int handle);

    #region PluginInfo
    [PluginInfo(Name = "NodeLoad", Category = "openFrameworks", Help = "Load an ofxVVVV Node", Tags = "")]
    #endregion PluginInfo
    public unsafe class NodeLoad : IPluginEvaluate, IDisposable
	{
        static NodeFactorySet FFactories = new NodeFactorySet();
		NodeFactorySet.NodeFactory.NodeInstance FNodeInstance = null;

        bool FIsSetup = false;
		string FFilename;
        int FHandle = -1;

		#region fields & pins
        [Input("Filename", Order = Int32.MaxValue, StringType=StringType.Filename, FileMask="dll", IsSingle=true)]
        IDiffSpread<string> FInFilename;

        [Output("Status", Order = Int32.MaxValue-1)]
        ISpread<string> FOutStatus;

        [Output("Handle", Order = Int32.MaxValue, DefaultValue=0)]
        ISpread<int> FOutHandle;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins
    
        public void Update()
        {
            if (FInFilename.IsChanged)
            {
				var factory = FFactories.GetFactory(FInFilename[0]);
            }

            if (FLoaded)
            {
                if (!FIsSetup)
                    SetupNode(FHandle);
                FIsSetup = true;
            }
        }

        public void Draw(StereoVisibility Eye)
        {
        }

        public void KeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
        }

        public void KeyUp(System.Windows.Forms.KeyEventArgs e)
        {
        }

        public void MouseDown(System.Windows.Forms.MouseEventArgs e)
        {
        }

        public void MouseUp(System.Windows.Forms.MouseEventArgs e)
        {
        }

        public void MouseMove(System.Windows.Forms.MouseEventArgs e)
        {
        }

        public void MouseDragged(System.Windows.Forms.MouseEventArgs e)
        {
        }

        public void Evaluate(int SpreadMax)
        {
            Update();
        }

        public void Dispose()
        {
            //Unload();
        }
    }
}
