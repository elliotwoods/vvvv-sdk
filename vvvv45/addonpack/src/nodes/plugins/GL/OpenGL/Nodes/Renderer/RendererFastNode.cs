using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;
using OpenTK.Graphics.OpenGL;
using VVVV.Nodes.OpenGL.Utilities;
using OpenTK;
using VVVV.Core.Logging;
using System.ComponentModel.Composition;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Threading;

namespace VVVV.Nodes.OpenGL
{
	#region PluginInfo
	[PluginInfo(Name = "Renderer", Category = "OpenGL", Version = "Fast", Help = "Render external to VVVV window", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class RendererFastNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ILayerNode> FPinInLayer;

		[Input("Background", DefaultColor = new double[] { 0, 0, 0, 1 }, IsSingle = true)]
		IDiffSpread<RGBAColor> FPinInBackground;

		[Input("Fullscreen", IsSingle = true)]
		IDiffSpread<bool> FPinInFullscreen;

		[Input("View")]
		ISpread<Matrix4x4> FPinInView;

		[Input("Projection")]
		ISpread<Matrix4x4> FPinInProjection;

		[Input("View Left", Visibility=PinVisibility.OnlyInspector)]
		ISpread<Matrix4x4> FPinInViewLeft;

		[Input("Projection Left", Visibility = PinVisibility.OnlyInspector)]
		ISpread<Matrix4x4> FPinInProjectionLeft;

		[Input("View Right", Visibility = PinVisibility.OnlyInspector)]
		ISpread<Matrix4x4> FPinInViewRight;

		[Input("Projection Right", Visibility = PinVisibility.OnlyInspector)]
		ISpread<Matrix4x4> FPinInProjectionRight;

		[Input("Mode", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<GraphicsMode> FPinInGraphicsMode;

		[Input("Version", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<OpenGLVersion> FPinInOpenGLVersion;

		[Input("Width", IsSingle=true, DefaultValue=800)]
		IDiffSpread<int> FPinInWidth;

		[Input("Height", IsSingle = true, DefaultValue=600)]
		IDiffSpread<int> FPinInHeight;

		[Import()]
		ILogger FLogger;

		GameWindow FWindow = null;
		bool FBackgroundChange = false;
		#endregion fields & pins

		[ImportingConstructor]
		RendererFastNode()
		{
			
		}

		public void Evaluate(int SpreadMax)
		{
			if (FPinInBackground.IsChanged)
				FBackgroundChange = true;

			bool FullscreenResChanged = false;

			if (FPinInWidth.IsChanged || FPinInHeight.IsChanged)
			{
				if (FPinInFullscreen[0])
				{
					FullscreenResChanged = true;
				}
				else
				{
					if (FWindow != null)
					{
						FWindow.Width = FPinInWidth[0];
						FWindow.Height = FPinInHeight[0];
					}
				}
			}

			if (FPinInFullscreen.IsChanged || FPinInGraphicsMode.IsChanged || FPinInOpenGLVersion.IsChanged || FullscreenResChanged)
				Start();

			Render();
		}

		void Start()
		{
			Stop();

			var Mode = FPinInGraphicsMode[0] == null ? GraphicsMode.Default : FPinInGraphicsMode[0];
			int Version = FPinInOpenGLVersion[0] == OpenGLVersion.OpenGL2 ? 2 : 3;
			GameWindowFlags Flags = FPinInFullscreen[0] ? GameWindowFlags.Fullscreen : GameWindowFlags.Default;

			FWindow = new GameWindow(800, 600, Mode, "Renderer", Flags);
			FWindow.Visible = true;
		}

		void Stop()
		{
			if (FWindow != null)
			{
				FWindow.Exit();
				FWindow.Dispose();
				FWindow = null;
			}
		}

		void Render()
		{
			FWindow.MakeCurrent();
			GL.Viewport(FWindow.ClientSize);

			if (FBackgroundChange)
			{
				FBackgroundChange = false;
				GL.ClearColor(FPinInBackground[0].Color);
			}

			if (FWindow.Context.GraphicsMode.Stereo)
			{
				GL.DrawBuffer(DrawBufferMode.BackLeft);
				RenderEye(FPinInViewLeft, FPinInProjectionLeft);

				GL.DrawBuffer(DrawBufferMode.BackRight);
				RenderEye(FPinInViewRight, FPinInProjectionRight);
			}
			else
			{
				RenderEye(FPinInView, FPinInProjection);
			}

			FWindow.SwapBuffers();
		}
		
		void RenderEye(ISpread<Matrix4x4> View, ISpread<Matrix4x4> Projection)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Matrix4d mat;
			int nViewports = Math.Max(View.SliceCount, Projection.SliceCount);
			for (int i = 0; i < nViewports; i++)
			{
				GL.MatrixMode(MatrixMode.Projection);
				mat = UMath.ToGL(Projection[i]);
				GL.LoadMatrix(ref mat);

				GL.MatrixMode(MatrixMode.Modelview);
				mat = UMath.ToGL(View[i]);
				GL.LoadMatrix(ref mat);

				foreach (var Layer in FPinInLayer)
					Layer.Draw();
			}
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
