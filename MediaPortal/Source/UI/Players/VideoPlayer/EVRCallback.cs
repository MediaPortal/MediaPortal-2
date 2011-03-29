#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.DirectX;
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
    private CropSettings _cropSettings = null;
    private Size _croppedVideoSize = Size.Empty;
    private Size _originalVideoSize = Size.Empty;
    private Size _aspectRatio = Size.Empty;
    private readonly RenderDlgt _renderDlgt;
    private Texture _texture = null;
    private Surface _surface = null;
    private SizeF _surfaceMaxUV = Size.Empty;
    private bool _guiBeingReinitialized = false;

    #endregion

    public EVRCallback(RenderDlgt renderDlgt)
    {
      _renderDlgt = renderDlgt;
    }

    #region public properties

    /// <summary>
    /// Gets the size of the texture which contains the current frame.
    /// </summary>
    public Size TextureSize
    {
      get { return _croppedVideoSize; }
    }

    /// <summary>
    /// If this property is set to a not <c>null</c> value, the video image will be cropped before it is copied into
    /// the frame <see cref="Texture"/>.
    /// </summary>
    public CropSettings CropSettings
    {
      get { return _cropSettings; }
      set { _cropSettings = value; }
    }

    /// <summary>
    /// Gets the size of the video frame after it has been cropped using the provided <see cref="CropSettings"/>.
    /// </summary>
    public Size CroppedVideoSize
    {
      get { return _croppedVideoSize; }
    }

    /// <summary>
    /// Gets the size of the original video frame as it came from the EVR presenter.
    /// </summary>
    public Size OriginalVideoSize
    {
      get { return _originalVideoSize; }
    }

    /// <summary>
    /// Returns the maximum UV coords of the render frame texture.
    /// </summary>
    /// <remarks>
    /// Standard/Legacy DirectX limits texture surface/texture sizes to power-of-2. If a texture
    /// is created with a non-power-of-2 size it is rounded up, which results in an empty border 
    /// around the actual texture. By comparing the desired size with the actual size of the surface 
    /// we can determine the maximum texture coordinates that will display the whole image, in this 
    /// case a video frame, without showing any of the border. This would be [1.0f, 1.0f] for a 
    /// power-of-2 texture, and smaller for a non-power-of-2 texture.
    /// 
    /// This function returns the pre-calculated maximum texture coordinates required to display the 
    /// frame without the border.
    /// </remarks>
    public SizeF SurfaceMaxUV 
    {
      get { return _surfaceMaxUV; } 
    }

    /// <summary>
    /// Gets the aspect ratio.
    /// </summary>
    public Size AspectRatio
    {
      get { return _aspectRatio; }
    }

    #endregion

    /// <summary>
    /// The first time the <see cref="OriginalVideoSize"/> and <see cref="CroppedVideoSize"/> properties are
    /// present is when the EVR presenter delivered the first video frame. At that time, this event will be raised.
    /// </summary>
    public event VideoSizePresentDlgt VideoSizePresent;

    public void ReleaseResources()
    {
      lock (_lock)
      {
        Dispose();
        _guiBeingReinitialized = true;
      }
    }

    public void ReallocResources()
    {
      lock (_lock)
      {
        _guiBeingReinitialized = false;
      }
    }

    public void Dispose()
    {
      FreeTexture();
    }

    private void FreeTexture()
    {
      FilterGraphTools.TryDispose(ref _surface);
      FilterGraphTools.TryDispose(ref _texture);
    }

    public Texture Texture
    {
      get { return _texture; }
    }

    #region IEVRPresentCallback implementation

    public int PresentSurface(short cx, short cy, short arx, short ary, uint dwSurface)
    {
      lock (_lock)
        if (dwSurface != 0 && cx != 0 && cy != 0 && !_guiBeingReinitialized)
        {
          if (cx != _originalVideoSize.Width || cy != _originalVideoSize.Height)
          {
            FreeTexture();
            _originalVideoSize = new Size(cx, cy);
          }
          Rectangle cropRect = (CropSettings == null) ? new Rectangle(Point.Empty, _originalVideoSize) :
              _cropSettings.CropRect(_originalVideoSize);
          _croppedVideoSize = cropRect.Size;

          _aspectRatio.Width = arx;
          _aspectRatio.Height = ary;

          VideoSizePresentDlgt vsp = VideoSizePresent;
          if (vsp != null)
          {
            vsp(this);
            VideoSizePresent = null;
          }
          if (_texture == null)
          {
            int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
            AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
            _texture = new Texture(GraphicsDevice.Device, _croppedVideoSize.Width, _croppedVideoSize.Height,
                1, Usage.RenderTarget, adapterInfo.CurrentDisplayMode.Format, Pool.Default);
            _surface = _texture.GetSurfaceLevel(0);

            SurfaceDescription desc = _texture.GetLevelDescription(0);
            _surfaceMaxUV = new SizeF(_croppedVideoSize.Width / (float) desc.Width, _croppedVideoSize.Height / (float) desc.Height);
          }

          using (Surface surf = Surface.FromPointer(new IntPtr(dwSurface)))
          {
            GraphicsDevice.Device.StretchRectangle(surf, cropRect,
                _surface, new Rectangle(Point.Empty, _croppedVideoSize), TextureFilter.None);
          }
        }
      if (_renderDlgt != null)
        _renderDlgt();
      return 0;
    }

    #endregion
  }
}
