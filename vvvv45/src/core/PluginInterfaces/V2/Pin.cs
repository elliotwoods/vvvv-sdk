﻿using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using SlimDX;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;
using VVVV.Utils.VMath;

namespace VVVV.PluginInterfaces.V2
{
    [ComVisible(false)]
	public delegate void PinUpdatedEventHandler(object sender, EventArgs args);
	
	[ComVisible(false)]
	public delegate void PinConnectionEventHandler(object sender, PinConnectionEventArgs args);
	
	[ComVisible(false)]
	public class PinConnectionEventArgs : EventArgs
	{
		public IPin OtherPin
		{
			get;
			private set;
		}
		
		public PinConnectionEventArgs(IPin otherPin)
		{
			OtherPin = otherPin;
		}
	}
	
	[ComVisible(false)]
	public class Pin<T> : Spread<T>, ISpread<T>, IDisposable
	{
	    private readonly IIOFactory FFactory;
		private readonly IPluginIO FPluginIO;
        private bool FConnected;
		
		public Pin(IIOFactory factory, IPluginIO pluginIO, MemoryIOStream<T> stream)
			: base(stream)
		{
		    FFactory = factory;
			FPluginIO = pluginIO;
            // Can be null in case of Texture and Mesh pins which call CreateTextureOutput2/CreateMeshOutput2 internally
            if (pluginIO != null)
                FConnected = pluginIO.IsConnected;
			
			FFactory.Connected += HandleConnected;
			FFactory.Disconnected += HandleDisconnected;
		}
		
		public void Dispose()
		{
		    FFactory.Connected -= HandleConnected;
			FFactory.Disconnected -= HandleDisconnected;
		}

		public override string ToString()
		{
			return base.ToString() + ": " + FPluginIO.Name;
		}

        public bool IsConnected
        {
            get { return FConnected; }
        }
		
		public IPluginIO PluginIO
		{
			get
			{
				return FPluginIO;
			}
		}
		
		public event PinConnectionEventHandler Connected;
		
        protected virtual void OnConnected(PinConnectionEventArgs args)
        {
            if (Connected != null) 
            {
                Connected(this, args);
            }
        }
		
		public event PinConnectionEventHandler Disconnected;
		
        protected virtual void OnDisconnected(PinConnectionEventArgs args)
        {
            if (Disconnected != null) 
            {
                Disconnected(this, args);
            }
        }
        
        void HandleConnected(object sender, ConnectionEventArgs e)
		{
            if (e.PluginIO == FPluginIO)
            {
                FConnected = this.PluginIO.IsConnected;
                OnConnected(new PinConnectionEventArgs(e.OtherPin));
            }
		}
		
		void HandleDisconnected(object sender, ConnectionEventArgs e)
		{
            if (e.PluginIO == FPluginIO)
            {
                FConnected = this.PluginIO.IsConnected;
                OnDisconnected(new PinConnectionEventArgs(e.OtherPin));
            }
		}
	}
}
