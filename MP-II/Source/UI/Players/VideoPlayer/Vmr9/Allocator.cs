#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
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
using MediaPortal.Core;
using MediaPortal.Presentation.Players;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.Effects;
using MediaPortal.SkinEngine.DirectX;

namespace Ui.Players.VideoPlayer.Vmr9
{

  #region IVMR9PresentCallback interface

  [ComVisible(true), ComImport,
   Guid("324FAA1F-7DA6-4778-833B-3993D8FF4151"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IVMR9PresentCallback
  {
    [PreserveSig]
    int PresentImage(Int16 cx, Int16 cy, Int16 arx, Int16 ary, uint dwImg);

    [PreserveSig]
    int PresentSurface(Int16 cx, Int16 cy, Int16 arx, Int16 ary, uint dwImg);
  }

  #endregion

  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.None)]
  public class Allocator : IVMR9PresentCallback
  {
    #region variables

    private Size _videoSize;
    private Size _aspectRatio;
    private uint _textureAddress;
    private Texture _texture;
    private Surface _surface;
    private object _lock;
    private bool _usingEvr = false;
    private IPlayerCollection _players;
    private IPlayer _player;
    private bool _guiBeingReinitialized = false;
    EffectAsset _normalEffect;

    #endregion

    #region ctor/dtor

    /// <summary>
    /// Initializes a new instance of the <see cref="Allocator"/> class.
    /// </summary>
    /// <param name="usingEvr">if set to <c>true</c> [using evr].</param>
    public Allocator(IPlayer player, bool usingEvr)
    {
      _players = ServiceScope.Get<IPlayerCollection>();
      _player = player;
      _usingEvr = usingEvr;
      _lock = new Object();
      _normalEffect = ContentManager.GetEffect("normal");
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the size of the texture.
    /// </summary>
    /// <value>The size of the texture.</value>
    public Size TextureSize
    {
      get { return _videoSize; }
    }

    /// <summary>
    /// Gets the size of the video.
    /// </summary>
    /// <value>The size of the video.</value>
    public Size VideoSize
    {
      get { return _videoSize; }
    }

    /// <summary>
    /// Gets the aspect ratio.
    /// </summary>
    /// <value>The aspect ratio.</value>
    public Size AspectRatio
    {
      get { return _aspectRatio; }
    }

    #endregion

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
    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
      if (_surface != null)
      {
        _surface.Dispose();
        _surface = null;
        ContentManager.TextureReferences--;
      }
      if (_texture != null)
      {
        _texture.Dispose();
        _texture = null;

        ContentManager.TextureReferences--;
      }
    }
    public Texture Texture
    {
      get
      {
        return _texture;
      }
    }

    /// <summary>
    /// Renders the video texture.
    /// </summary>
    public void Render(EffectAsset effect)
    {
      //render the texture
      lock (_lock)
      {
        if (_guiBeingReinitialized) return;
        if (_texture != null)
        {
          if (effect != null)
          {
            effect.Render(_texture);
          }
          else
          {
            _normalEffect.StartRender(_texture);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
            _normalEffect.EndRender();
          }
        }
      }
    }

    #region IVMR9PresentCallback Members

    /// <summary>
    /// callback from vmr9helper.dll to display a DirectX.Texture
    /// </summary>
    /// <param name="cx">video width</param>
    /// <param name="cy">video height</param>
    /// <param name="arx">Aspect Ratio X</param>
    /// <param name="ary">Aspect Ratio Y</param>
    /// <param name="dwImg">address of the DirectX.Texture.</param>
    /// <returns></returns>
    public int PresentImage(short cx, short cy, short arx, short ary, uint dwImg)
    {
      return 0;
    }

    /// <summary>
    /// callback from vmr9helper.dll to display a DirectX.Surface
    /// </summary>
    /// <param name="cx">video width</param>
    /// <param name="cy">video height</param>
    /// <param name="arx">Aspect Ratio X</param>
    /// <param name="ary">Aspect Ratio Y</param>
    /// <param name="dwImg">address of the DirectX.Surface.</param>
    /// <returns></returns>
    public int PresentSurface(short cx, short cy, short arx, short ary, uint dwImg)
    {
      lock (_lock)
      {
        if (dwImg == 0 || cx == 0 || cy == 0 || _guiBeingReinitialized)
        {
          return 0;
        }
        if (cx != _videoSize.Width || cy != _videoSize.Height)
        {
          if (_surface != null)
          {
            _surface.Dispose();
            _surface = null;
            ContentManager.TextureReferences--;
          }

          if (_texture != null)
          {
            _texture.Dispose();
            _texture = null;
            ContentManager.TextureReferences--;
          }
          _textureAddress = dwImg;
          _videoSize = new Size(cx, cy);
          _aspectRatio = new Size(arx, ary);
        }

        _textureAddress = dwImg;
        _videoSize = new Size(cx, cy);
        _aspectRatio = new Size(arx, ary);
        if (_texture == null)
        {
          int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
          AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
          _texture = new Texture(GraphicsDevice.Device, cx, cy, 1, Usage.RenderTarget, adapterInfo.CurrentDisplayMode.Format, Pool.Default);
          ContentManager.TextureReferences++;
          _surface = _texture.GetSurfaceLevel(0);

          ContentManager.TextureReferences++;
        }

        IntPtr ptr = new IntPtr(dwImg);
        //Marshal.AddRef(ptr);
        unsafe
        {
          using (Surface surf = Surface.FromPointer(ptr))
          {
            GraphicsDevice.Device.StretchRectangle(surf,
                                                   new Rectangle(Point.Empty, _videoSize),
                                                   _surface,
                                                   new Rectangle(Point.Empty, _videoSize),
                                                   TextureFilter.None
              );
          }
        }
      }


      //if we're are using VMR9 instead of vista's new EVR
      //then we need to perform all rendering from the vmr9 thread 
      //this is because a vmr9 architecture bug.
      //When vmr9 is in mixing mode it changes the directx rendertarget during decoding/rendering (thanks microsoft!!!)
      //we need the mixing mode for yuv mixing (lower cpu) and de-interlacing
      //and so.. only way to get flickerfree drawing can be achieved by rendering the whole direct3d scene from here
      //since only here we know that vmr9 wont change the rendertarget
      //      if (false == _usingEvr)

      bool isPIP = false;
      if (_players.Count > 0 && _players[0] != _player)
      {
        isPIP = true;
      }
      if (isPIP == false)
      {
        //render the entire direct3d scene (grrr)
        GraphicsDevice.Render(false);
      }
      return 0;
    }

    #endregion
  }
}
