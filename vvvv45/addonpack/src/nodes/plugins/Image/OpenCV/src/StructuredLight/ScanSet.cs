using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;

namespace VVVV.Nodes.OpenCV.StructuredLight
{
	public class ScanSet
	{
		/// <summary>
		/// Encoded data that has been scanned.
		/// EncodedData[iCameraPixel]
		/// </summary>
		public ulong[] EncodedData;

		/// <summary>
		/// Raw data result of scan
		/// ProjectorInCamera[iCameraPixel] = iProjectorPixel
		/// </summary>
		public ulong[] ProjectorInCamera;

		/// <summary>
		/// Inverted Raw data result of scan
		/// RawData[iProjectorPixel] = iCameraPixel
		/// </summary>
		public ulong[] CameraInProjector;

		/// <summary>
		/// How far on average the pixel value stepped
		/// </summary>
		public float[] Distance;

		public IPayload Payload;
		public Size CameraSize;
		public int CameraPixelCount
		{
			get
			{
				lock (this)
					return CameraSize.Width * CameraSize.Height;
			}
		}
		public int ProjectorPixelCount
		{
			get
			{
				lock (this)
					return Payload.PixelCount;
			}
		}
		public Size ProjectorSize
		{
			get
			{
				lock (this)
					return Payload.Size;
			}
		}

		public event EventHandler UpdateAttributes;
		public void OnUpdateAttributes()
		{
			FInitialised = true;
			if (UpdateAttributes != null)
				UpdateAttributes(this, EventArgs.Empty);
		}

		public event EventHandler UpdateData;
		public void OnUpdateData()
		{
			FDataAvailable = true;
			if (UpdateData != null)
				UpdateData(this, EventArgs.Empty);
		}

		bool FDataAvailable = false;
		public bool DataAvailable
		{
			get
			{
				return FDataAvailable;
			}
		}

		bool FInitialised = false;
		public bool Allocated
		{
			get
			{
				return FInitialised && Payload != null;
			}
		}

		public void Allocate(Size CameraSize)
		{
			lock (this)
			{
				this.CameraSize = CameraSize;
				this.EncodedData = new ulong[CameraPixelCount];
				this.ProjectorInCamera = new ulong[CameraPixelCount];
				this.CameraInProjector = new ulong[ProjectorPixelCount];
				this.Distance = new float[CameraSize.Width * CameraSize.Height];
				this.OnUpdateAttributes();
				FInitialised = true;
			}
		}

		public unsafe void Clear()
		{
			lock (this)
			{
				if (!this.Allocated)
					return;

				int n = this.CameraPixelCount;
				fixed (ulong* indexFixed = &this.ProjectorInCamera[0])
				{
					ulong* index = indexFixed;
					for (int i = 0; i < n; i++)
						*index++ = 0;
				}

				FDataAvailable = false;
			}
		}

		public bool GetValue(ulong index, ref ulong output)
		{
			lock (this)
			{
				if (index > Payload.MaxIndexCached)
					return false;

				output = Payload.DataInverse[index];

				return true;
			}
		}

		/// <summary>
		/// Calculates the maps
		/// </summary>
		public unsafe void Evaluate()
		{
			fixed (ulong* encodedFixed = &EncodedData[0])
			{
				fixed (ulong* rawFixed = &ProjectorInCamera[0])
				{
					ulong* encoded = encodedFixed;
					ulong* raw = rawFixed;
					ulong projCount = (ulong) ProjectorPixelCount;
					ulong cameraCount = (ulong)CameraPixelCount;

					for (ulong i = 0; i < cameraCount; i++)
					{
						*raw = Payload.DataInverse[*encoded++];

						if (*raw < projCount)
							CameraInProjector[*raw] = i;

						raw++;
					}
				}
			}

			OnUpdateData();
		}
	}
}
