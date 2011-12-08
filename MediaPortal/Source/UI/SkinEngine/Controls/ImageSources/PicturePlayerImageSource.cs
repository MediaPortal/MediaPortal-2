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

using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;
using RightAngledRotation = MediaPortal.UI.SkinEngine.Rendering.RightAngledRotation;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  public class PicturePlayerImageSource : MultiImageSourceBase
  {
    protected Texture _lastTexture = null;
    protected SizeF _lastTextureSize;
    protected float _lastMaxU;
    protected float _lastMaxV;

    protected Texture _currentTexture = null;
    protected SizeF _currentTextureSize;
    protected float _currentMaxU;
    protected float _currentMaxV;

    protected Texture _lastCopiedTexture = null;

    protected AbstractProperty _streamProperty;

    #region Ctor

    public PicturePlayerImageSource()
    {
      Init();
    }

    void Init()
    {
      _streamProperty = new SProperty(typeof(int), 0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PicturePlayerImageSource ppis = (PicturePlayerImageSource) source;
      Stream = ppis.Stream;
      FreeData();
    }

    public override void Dispose()
    {
      base.Dispose();
      FreeData();
    }

    #endregion

    #region Public properties

    public AbstractProperty StreamProperty
    {
      get { return _streamProperty; }
    }

    /// <summary>
    /// Gets or sets the number of the player stream to be shown.
    /// </summary>
    public int Stream
    {
      get { return (int) _streamProperty.GetValue(); }
      set { _streamProperty.SetValue(value); }
    }

    #endregion

    #region ImageSource implementation

    public override void Allocate()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>(false);
      if (playerManager == null)
      {
        FreeData();
        return;
      }

      ISlimDXPicturePlayer player = playerManager[Stream] as ISlimDXPicturePlayer;
      if (player == null) 
      {
        FreeData();
        return;
      }

      lock (player.PicturesLock)
      {
        Texture texture = player.CurrentPicture;
        if (texture != null && texture != _lastCopiedTexture)
        {
          _lastCopiedTexture = texture;
          CycleTextures(texture, player.MaxU, player.MaxV, TranslateRotation(player.Rotation));
        }
      }
    }

    #endregion

    #region Protected members

    protected override Texture LastTexture
    {
      get { return _lastTexture; }
    }

    protected override SizeF LastTextureSize
    {
      get { return _lastTextureSize; }
    }

    protected override float LastMaxU
    {
      get { return _lastMaxU; }
    }

    protected override float LastMaxV
    {
      get { return _lastMaxV; }
    }

    protected override Texture CurrentTexture
    {
      get { return _currentTexture; }
    }

    protected override SizeF CurrentTextureSize
    {
      get { return _currentTextureSize; }
    }

    protected override float CurrentMaxU
    {
      get { return _currentMaxU; }
    }

    protected override float CurrentMaxV
    {
      get { return _currentMaxV; }
    }

    public override bool IsAllocated
    {
      get { return _currentTexture != null; }
    }

    protected RightAngledRotation TranslateRotation(Presentation.Players.RightAngledRotation rotation)
    {
      return (RightAngledRotation) rotation; // Enums are compatible
    }

    protected void CycleTextures(Texture nextTexture, float nextMaxU, float nextMaxV, RightAngledRotation nextRotation)
    {
      TryDispose(ref _lastTexture);

      // Current -> Last
      _lastTexture = _currentTexture;
      _lastTextureSize = _currentTextureSize;
      _lastMaxU = _currentMaxU;
      _lastMaxV = _currentMaxV;
      _lastImageContext = _imageContext;

      // Next -> Current
      SizeF textureSize;
      _currentTexture = CreateTextureCopy(nextTexture, out textureSize);
      _currentTextureSize = new SizeF(textureSize.Width * nextMaxU, textureSize.Height * nextMaxV);
      _currentMaxU = nextMaxU;
      _currentMaxV = nextMaxV;

      _imageContext = new ImageContext
        {
            FrameSize = _frameSize,
            ShaderEffect = Effect,
            Rotation = nextRotation
        };

      StartTransition();
      FireChanged();
    }

    protected Texture CreateTextureCopy(Texture sourceTexture, out SizeF textureSize)
    {
      SurfaceDescription desc = sourceTexture.GetLevelDescription(0);
      textureSize = new SizeF(desc.Width, desc.Height);
      DeviceEx device = SkinContext.Device;
      Texture result = new Texture(device, desc.Width, desc.Height, 1, Usage.None, Format.A8R8G8B8, Pool.Default);
      using (Surface target = result.GetSurfaceLevel(0))
      using (Surface source = sourceTexture.GetSurfaceLevel(0))
        device.StretchRectangle(source, target, TextureFilter.None);
      return result;
    }

    protected override void FreeData()
    {
      base.FreeData();
      TryDispose(ref _lastTexture);
      TryDispose(ref _currentTexture);
      _lastImageContext.Clear();
    }

    #endregion
  }
}
