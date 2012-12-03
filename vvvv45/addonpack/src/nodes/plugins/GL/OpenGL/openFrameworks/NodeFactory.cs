using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.IO;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.OpenGL.openFrameworks
{
	public class NodeFactory : IDisposable
	{
#region imports
		[DllImport("kernel32.dll")]
		internal static extern IntPtr LoadLibrary(String dllname);

		[DllImport("kernel32.dll")]
		internal static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, String procname);
#endregion

#region delegates
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		unsafe public delegate int NodeSetDataPath(char* path, int length);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NodeCreateDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NodeArgumentlessDelegate(int handle);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NodeCountPinsDelegate(int handle);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NodeSetSpreadSliceCountDelegate(int handle, int PinIndex, int SliceCount);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NodeSetSpreadValueDelegate(int handle, int PinIndex, int SliceIndex, double Value);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NodeGetSpreadSliceCountDelegate(int handle, int PinIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate double NodeGetSpreadValueDelegate(int handle, int PinIndex, int SliceIndex);
#endregion

		IntPtr FLibrary;
		public NodeSetDataPath SetDataPath;
		public NodeCreateDelegate Create;
		public NodeArgumentlessDelegate Destroy;
		public NodeArgumentlessDelegate Setup;
		public NodeArgumentlessDelegate Update;
		public NodeArgumentlessDelegate Draw;
		public NodeCountPinsDelegate GetInputCount;
		public NodeCountPinsDelegate GetOutputCount;
		public NodeSetSpreadSliceCountDelegate SetInputSliceCount;
		public NodeSetSpreadValueDelegate SetInputValue;
		public NodeGetSpreadSliceCountDelegate GetOutputSliceCount;
		public NodeGetSpreadValueDelegate GetOutputValue;

		List<NodeInstance> FInstances = new List<NodeInstance>();

		bool FLoaded = false;
		string FFilename = "";
		string FTemporaryFilename = "";
		DateTime FLastWrite;

		public NodeFactory(string DllFilename)
		{
			FFilename = DllFilename;
			Load();
		}

		void WaitForAccess(int iterations)
		{
			try
			{
				System.Threading.Thread.Sleep(1000);
				System.IO.File.Open(FFilename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch
			{
				iterations++;
				if (iterations > 1000)
					throw (new Exception("Cannot open " + FFilename + " for exclusive access"));
				else
					WaitForAccess(iterations);
			}
		}
		unsafe public void Load()
		{
			Unload();

			try
			{
				if (!File.Exists(FFilename))
					throw (new Exception("File not found"));

				FTemporaryFilename = System.IO.Path.GetTempPath() + Path.GetFileName(FFilename);
				if (File.Exists(FTemporaryFilename))
				{
					try
					{
						File.Delete(FTemporaryFilename);
					}
					catch
					{
						int index = 0;
						string noExtension = FTemporaryFilename.Substring(0, FTemporaryFilename.Length - 4);

						while (File.Exists(FTemporaryFilename))
						{
							FTemporaryFilename = noExtension + "~" + index.ToString() + ".dll";
							index++;
						}
					}
				}

				System.Threading.Thread.Sleep(1000);
				//WaitForAccess(0);

				File.Copy(FFilename, FTemporaryFilename);
				FLastWrite = File.GetLastWriteTime(FFilename);

				FLibrary = LoadLibrary(FTemporaryFilename);
				if (FLibrary == IntPtr.Zero)
					throw (new Exception("Failed to load temporary dll copy at " + FTemporaryFilename + ". Check that you have the openFrameworks dlls in the vvvv.exe folder"));
				SetDataPath = (NodeSetDataPath)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "SetDataPath"), typeof(NodeSetDataPath));
				Create = (NodeCreateDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeCreate"), typeof(NodeCreateDelegate));
				Destroy = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeDestroy"), typeof(NodeArgumentlessDelegate));
				Setup = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeSetup"), typeof(NodeArgumentlessDelegate));
				Update = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeUpdate"), typeof(NodeArgumentlessDelegate));
				Draw = (NodeArgumentlessDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeDraw"), typeof(NodeArgumentlessDelegate));
				GetInputCount = (NodeCountPinsDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeInputCount"), typeof(NodeCountPinsDelegate));
				GetOutputCount = (NodeCountPinsDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeOutputCount"), typeof(NodeCountPinsDelegate));
				SetInputSliceCount = (NodeSetSpreadSliceCountDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeSetInputSliceCount"), typeof(NodeSetSpreadSliceCountDelegate));
				GetOutputSliceCount = (NodeGetSpreadSliceCountDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeGetOutputSliceCount"), typeof(NodeGetSpreadSliceCountDelegate));
				SetInputValue = (NodeSetSpreadValueDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeSetInputValue"), typeof(NodeSetSpreadValueDelegate));
				GetOutputValue = (NodeGetSpreadValueDelegate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(FLibrary, "NodeGetOutputValue"), typeof(NodeGetSpreadValueDelegate));

				var dataPath = (Path.GetDirectoryName(FFilename) + "\\data").ToCharArray();
				fixed (char* pathAsChar = &dataPath[0])
					SetDataPath(pathAsChar, dataPath.Length);

				this.FLoaded = true;

				NodeInstance[] oldInstances = new NodeInstance[FInstances.Count];
				FInstances.CopyTo(oldInstances, 0);

				foreach (var instance in oldInstances)
				{
					instance.Create();
					instance.Setup();
				}
			}
			catch (Exception e)
			{
				foreach (var instance in FInstances)
					instance.Destroy();
				FInstances.Clear();

				this.FLoaded = false;
				throw (e);
			}
		}

		public void Register(NodeInstance node)
		{
			this.FInstances.Add(node);
		}

		void Unload()
		{
			if (this.FLoaded)
			{
				FreeLibrary(FLibrary);
				File.Delete(FTemporaryFilename);
			}

			FLoaded = false;
		}

		public bool Loaded
		{
			get
			{
				return this.FLoaded;
			}
		}

		public bool CheckFileUpdates()
		{
			if (FLoaded)
			{
				if (File.GetLastWriteTime(FFilename) != FLastWrite)
				{
					Load();
					return true;
				}
			}

			return false;
		}

		public NodeInstance NewNode()
		{
			var instance = new NodeInstance(this);
			this.FInstances.Add(instance);
			return instance;
		}

		public void Dispose()
		{
			Unload();
		}

		public void Unlink(NodeInstance Node)
		{
			Destroy(Node.Handle);
			this.FInstances.RemoveAt(Node.Handle);
			if (FInstances.Count == 0)
				this.Unload();
		}
	}
}
