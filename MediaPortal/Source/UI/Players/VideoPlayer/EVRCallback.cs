#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

/****************************************************************************
While the underlying libraries are covered by LGPL, this sample is released 
as public domain.  It is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  
*****************************************************************************/

using System;
using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.Players.Video
{
  public delegate void VideoSizePresentDlgt(EVRCallback sender);

  [ComVisible(true), ComImport,
   Guid("324FAA1F-7DA6-4778-833B-3993D8FF4151"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IEVRPresentCallback
  {
    /// <summary>
    /// Callback from EVRPresenter.dll to display a DirectX surface.
    /// </summary>
    /// <param name="cx">Video width.</param>
    /// <param name="cy">Video height.</param>
    /// <param name="arx">Aspect Ratio X.</param>
    /// <param name="ary">Aspect Ratio Y.</param>
    /// <param name="sharedHandle">Address of the DirectX surface.</param>
    /// <returns><c>0</c>, if the method succeeded, <c>!= 0</c> else.</returns>
    [PreserveSig]
    int PresentSurface(Int16 cx, Int16 cy, Int16 arx, Int16 ary, ref IntPtr sharedHandle);
  }

  public delegate void RenderDlgt();

  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.None)]
  public class EVRCallback : IEVRPresentCallback, IDisposable
  {
    #region Variables

    private readonly object _lock = new object();
    private Size _originalVideoSize;
    private SizeF _aspectRatio;
    protected IBitmapAsset2D _bitmapAsset2D;

    private readonly RenderDlgt _renderDlgt;
    private readonly Action _onTextureInvalidated;
    private string _instanceKey;

    #endregion

    public EVRCallback(RenderDlgt renderDlgt, Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
      _renderDlgt = renderDlgt;
    }

    public void Dispose()
    {
      VideoSizePresent = null;
    }

    #region Public properties and events

    /// <summary>
    /// The first time the <see cref="OriginalVideoSize"/> property is present is when the EVR presenter
    /// delivered the first video frame. At that time, this event will be raised.
    /// </summary>
    public event VideoSizePresentDlgt VideoSizePresent;

    public IBitmapAsset2D Surface
    {
      get
      {
        return _bitmapAsset2D;
      }
    }

    public object SurfaceLock
    {
      get { return _lock; }
    }

    /// <summary>
    /// Gets the size of the original video frame as it came from the EVR presenter.
    /// </summary>
    public Size OriginalVideoSize
    {
      get { return _originalVideoSize; }
    }

    /// <summary>
    /// Gets the aspect ratio.
    /// </summary>
    public SizeF AspectRatio
    {
      get { return _aspectRatio; }
    }

    /// <summary>
    /// Sets the current presenter instance. This is used to generate an unique asset key.
    /// </summary>
    public IntPtr PresenterInstance
    {
      set
      {
        _instanceKey = "EVRCallbackSurface_" + value; // Unique texture per EVR instance
      }
    }

    #endregion

    #region IEVRPresentCallback implementation

    public int PresentSurface(short cx, short cy, short arx, short ary, ref IntPtr sharedHandle)
    {
      try
      {
        lock (_lock)
          if (sharedHandle != IntPtr.Zero && cx != 0 && cy != 0)
          {
            if (cx != _originalVideoSize.Width || cy != _originalVideoSize.Height)
              _originalVideoSize = new Size(cx, cy);

            _aspectRatio.Width = arx;
            _aspectRatio.Height = ary;

            using (Texture2D tex = GraphicsDevice11.Instance.Device3D1.OpenSharedResource<Texture2D>(sharedHandle))
            using (SharpDX.DXGI.Surface surface10 = tex.QueryInterface<SharpDX.DXGI.Surface>())
            {
              using (var texBitmap = new Bitmap1(GraphicsDevice11.Instance.Context2D1, surface10))
              {
                _bitmapAsset2D = ContentManager.Instance.GetRenderTarget2D(_instanceKey);
                ((RenderTarget2DAsset)_bitmapAsset2D).AllocateRenderTarget(cx, cy);
                if (!_bitmapAsset2D.IsAllocated)
                  return 0;

                _bitmapAsset2D.Bitmap.CopyFromBitmap(texBitmap);
              }
            }
          }

        VideoSizePresentDlgt vsp = VideoSizePresent;
        if (vsp != null)
        {
          vsp(this);
          VideoSizePresent = null;
        }

        // Inform caller that we have changed the texture
        if (_onTextureInvalidated != null)
          _onTextureInvalidated();

        if (_renderDlgt != null)
          _renderDlgt();
        return 0;
      }
      catch (SharpDXException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: DirectX Exception", e);
        GraphicsDevice11.Instance.HandleDeviceLost(e, true);
        return 0;
      }
    }

    #endregion
  }
}
