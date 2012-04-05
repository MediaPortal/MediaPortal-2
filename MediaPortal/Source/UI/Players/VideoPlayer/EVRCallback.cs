#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Drawing;
using System.Runtime.InteropServices;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX.Direct3D9;

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
    /// <param name="dwSurface">Address of the DirectX surface.</param>
    /// <returns><c>0</c>, if the method succeeded, <c>!= 0</c> else.</returns>
    [PreserveSig]
    int PresentSurface(Int16 cx, Int16 cy, Int16 arx, Int16 ary, uint dwSurface);
  }

  public delegate void RenderDlgt();

  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.None)]
  public class EVRCallback : IEVRPresentCallback, IDisposable
  {
    #region Variables

    private readonly object _lock = new object();
    private Size _originalVideoSize = Size.Empty;
    private SizeF _aspectRatio = SizeF.Empty;
    private Surface _surface = null;

    private readonly DeviceEx _device;
    private readonly RenderDlgt _renderDlgt;
    private readonly Action _onTextureInvalidated;

    #endregion

    public EVRCallback(RenderDlgt renderDlgt, Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
      _renderDlgt = renderDlgt;
      _device = SkinContext.Device;
    }

    public void Dispose()
    {
      VideoSizePresent = null;
      FreeSurface();
    }

    #region Public properties and events

    /// <summary>
    /// The first time the <see cref="OriginalVideoSize"/> property is present is when the EVR presenter
    /// delivered the first video frame. At that time, this event will be raised.
    /// </summary>
    public event VideoSizePresentDlgt VideoSizePresent;

    public Surface Surface
    {
      get { return _surface; }
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

    #endregion

    private void FreeSurface()
    {
      lock (_lock)
        FilterGraphTools.TryDispose(ref _surface);
    }

    #region IEVRPresentCallback implementation

    public int PresentSurface(short cx, short cy, short arx, short ary, uint dwSurface)
    {
      if (dwSurface != 0 && cx != 0 && cy != 0)
      {
        if (cx != _originalVideoSize.Width || cy != _originalVideoSize.Height)
          _originalVideoSize = new Size(cx, cy);

        _aspectRatio.Width = arx;
        _aspectRatio.Height = ary;

        using (Surface surf = Surface.FromPointer(new IntPtr(dwSurface)))
        {
          SurfaceDescription surfaceDesc = _surface == null ? new SurfaceDescription() : _surface.Description;
          SurfaceDescription surfDesc = surf.Description;
          lock (_lock)
          {
            if (surfaceDesc.Width != surfDesc.Width || surfaceDesc.Height != surfDesc.Height)
            {
              if (_surface != null)
                _surface.Dispose();
              _surface = Surface.CreateRenderTarget(_device, surfDesc.Width, surfDesc.Height, Format.A8R8G8B8, MultisampleType.None, 0, false);
            }
            _device.StretchRectangle(surf, _surface, TextureFilter.None);
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

    #endregion
  }
}
