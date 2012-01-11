﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VVVV.Nodes.OpenCV.StructuredLight
{
	abstract class IPayload
	{
		public IPayload(int Width, int Height, bool Balanced)
		{
			this.Balanced = Balanced;

			if (Width < 1)
				Width = 1;
			if (Height < 1)
				Height = 1;

			this.Width = (uint)Width;
			this.Height = (uint)Height;

			FMaxIndex = (ulong)this.PixelCount * 2;
			this.Data = new ulong[this.PixelCount];
			this.DataInverse = new ulong[FMaxIndex];
			Render();
		}

		public uint Width { get; private set; }
		public uint Height { get; private set; }
		public Size Size
		{
			get
			{
				return new Size((int)Width, (int)Height);
			}
		}
		public int PixelCount
		{
			get
			{
				return (int)Width * (int)Height;
			}
		}
		public uint FrameCount
		{
			get
			{
				return (FrameCountWidth + FrameCountHeight) * (Balanced ? (uint)2 : (uint)1);
			}
		}
		public uint FrameCountWidth
		{
			get
			{
				return (uint)Math.Ceiling(Math.Log((double)Width, 2.0d));
			}
		}
		public uint FrameCountHeight 
		{
			get
			{
				return (uint)Math.Ceiling(Math.Log((double)Height, 2.0d));
			}
		}

		public CVImageAttributes FrameAttributes
		{
			get
			{
				return new CVImageAttributes(this.Size, TColourFormat.L8);
			}
		}

		public bool Balanced = true;

		public ulong[] Data { get; protected set; }
		public ulong[] DataInverse { get; protected set; }

		private ulong FMaxIndex = 0;
		public ulong MaxIndex
		{
			get { return FMaxIndex; }
		}

		public abstract void Render();
	}
}
