#region usings
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using OpenTK.Graphics.OpenGL;
using VVVV.Nodes.OpenGL.Utilities;
using OpenTK;
using OpenTK.Graphics;
#endregion usings

namespace VVVV.Nodes.OpenGL
{
	enum OpenGLVersion { OpenGL2, OpenGL3};

	#region PluginInfo
	[PluginInfo(Name = "Renderer",
	Category = "OpenGL",
	Help = "Test OpenGL",
	Tags = "",
	AutoEvaluate = true)]
	#endregion PluginInfo
	public class RendererNode : UserControl, IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ILayerNode> FPinInLayer;

		[Input("Background", DefaultColor = new double[] { 0, 0, 0, 1 }, IsSingle = true)]
		IDiffSpread<RGBAColor> FPinInBackground;

		[Input("View")]
		ISpread<Matrix4x4> FPinInView;

		[Input("Projection")]
		ISpread<Matrix4x4> FPinInProjection;

		[Input("Mode", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<GraphicsMode> FPinInGraphicsMode;

		[Input("Version", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<OpenGLVersion> FPinInOpenGLVersion;

		[Import()]
		ILogger FLogger;

		SharedContext FSharedContext = new SharedContext();

		//gui controls
		private OpenTK.GLControl FGLControl;
		private GraphicsMode FMode = new GraphicsMode(new ColorFormat(8, 8, 8, 8));
		private int FVersion = 2;
		private bool FHasControl = false;
		private bool FLoaded = false;
		
		//rendering properties
		private Color FBackground = Color.Black;
		private bool FBackgroundChange = false;

		#endregion fields & pins
		
		#region constructor and init
		
		public RendererNode()
		{
			//setup the gui
			InitializeComponent();
		}
		
		void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// RendererNode
			// 
			this.Name = "Renderer";
			this.ResumeLayout();
		}

		void InitialiseGLControl(bool SuspendLayout = false)
		{
			RemoveGLControl();

			if (SuspendLayout)
				this.SuspendLayout();

			this.FGLControl = new OpenTK.GLControl(FMode, FVersion, 0, GraphicsContextFlags.ForwardCompatible);
			
			// 
			// FGLControl
			//
			this.FGLControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.FGLControl.BackColor = System.Drawing.Color.Black;
			this.FGLControl.Location = new System.Drawing.Point(0, 0);
			this.FGLControl.Size = this.Size;
			this.FGLControl.Name = "FGLControl";
			this.FGLControl.TabIndex = 0;
			this.FGLControl.VSync = true;
			this.FGLControl.Load += new System.EventHandler(this.FGLControl_Load);
			this.FGLControl.Paint += new System.Windows.Forms.PaintEventHandler(this.FGLControl_Paint);
			this.FGLControl.Resize += new System.EventHandler(this.FGLControl_Resize);
			this.FGLControl.Disposed += new EventHandler(FGLControl_Disposed);
			this.FGLControl.KeyPress += new KeyPressEventHandler(FGLControl_KeyPress);
			this.FGLControl.KeyUp += new KeyEventHandler(FGLControl_KeyUp);
			this.FGLControl.MouseDown += new System.Windows.Forms.MouseEventHandler(FGLControl_MouseDown);
			this.FGLControl.MouseMove += new System.Windows.Forms.MouseEventHandler(FGLControl_MouseMove);
			this.FGLControl.MouseUp +=new System.Windows.Forms.MouseEventHandler(FGLControl_MouseUp);

			this.Controls.Add(this.FGLControl);
			this.Resize += new EventHandler(RendererNode_Resize);
			if (SuspendLayout)
				this.ResumeLayout(false);

			FHasControl = true;
		}

		void FGLControl_KeyUp(object sender, KeyEventArgs e)
		{
			foreach (var Layer in FPinInLayer)
				Layer.KeyUp(e);
		}

		System.Windows.Forms.MouseEventArgs TransformMouse(System.Windows.Forms.MouseEventArgs e)
		{
			return new System.Windows.Forms.MouseEventArgs(e.Button, e.Clicks, e.X - this.FGLControl.Location.X, e.Y - this.FGLControl.Location.Y, e.Delta);
		}

		void FGLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			e = TransformMouse(e);
			foreach (var Layer in FPinInLayer)
				if (e.Button.HasFlag(MouseButtons.None))
					Layer.MouseMove(e);
				else
					Layer.MouseDragged(e);
		}

		void FGLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			e = TransformMouse(e);
			foreach (var Layer in FPinInLayer)
				Layer.MouseUp(e);
		}

		void FGLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			e = TransformMouse(e);
			foreach (var Layer in FPinInLayer)
				Layer.MouseDown(e);

		}

		void FGLControl_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			foreach (var Layer in FPinInLayer)
				Layer.KeyPress(e);
		}

		void RendererNode_Resize(object sender, EventArgs e)
		{
			FGLControl.Size = this.Size;
		}

		void FGLControl_Disposed(object sender, EventArgs e)
		{
			ContextRegister.Remove(FGLControl);
		}

		void RemoveGLControl()
		{
			if (FHasControl)
			{
				this.Controls.Remove(this.FGLControl);
				FGLControl.Dispose();
				FHasControl = false;
				FLoaded = false;
			}
		}
		
		#endregion constructor and init
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			CheckConfigChanges();

			//draw every frame
			FGLControl.Invalidate();

			if (FPinInBackground.IsChanged)
			{
				FBackground = FPinInBackground[0].Color;
				FBackgroundChange = true;
			}
		}

		void CheckConfigChanges()
		{
			if (!(FPinInOpenGLVersion.IsChanged || FPinInGraphicsMode.IsChanged))
				return;

			FMode = FPinInGraphicsMode[0] == null ? GraphicsMode.Default : FPinInGraphicsMode[0];
			
			switch (FPinInOpenGLVersion[0])
			{
				case OpenGLVersion.OpenGL2:
					FVersion = 2;
					break;
				case OpenGLVersion.OpenGL3:
					FVersion = 3;
					break;
			}
			
			InitialiseGLControl(true);
		}

		private void FGLControl_Load(object sender, EventArgs e)
		{
			FLoaded = true;
			ContextRegister.Add(FGLControl);

			GL.ClearColor(Color.Black);
			SetupViewport();
		}

		private void FGLControl_Paint(object sender, PaintEventArgs e)
		{
			if (!FLoaded)
				return;
			FGLControl.MakeCurrent();

			if (FBackgroundChange)
			{
				FBackgroundChange = false;
				GL.ClearColor(FBackground);
			}
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Matrix4d mat;
			int nViewports = Math.Max(FPinInProjection.SliceCount, FPinInView.SliceCount);
			for (int i = 0; i < nViewports; i++)
			{
				GL.MatrixMode(MatrixMode.Projection);
				mat = UMath.ToGL(FPinInProjection[i]);
				GL.LoadMatrix(ref mat);

				GL.MatrixMode(MatrixMode.Modelview);
				mat = UMath.ToGL(FPinInView[i]);
				GL.LoadMatrix(ref mat);

				foreach(ILayerNode layer in FPinInLayer)
				{
					if (layer!=null)
						layer.Draw();
				}
			}

			FGLControl.SwapBuffers();
		}

		private void SetupViewport()
		{
			ContextRegister.BindContext(FGLControl.Context);
			GL.Viewport(0, 0, FGLControl.Width, FGLControl.Height); // Use all of the glControl painting area
		}

		private void FGLControl_Resize(object sender, EventArgs e)
		{
			if (FLoaded)
				SetupViewport();
		}
	}
}