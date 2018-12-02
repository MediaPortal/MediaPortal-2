#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using RightAngledRotation = MediaPortal.UI.SkinEngine.Rendering.RightAngledRotation;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  public class ImagePlayerImageSource : MultiImageSourceBase
  {
    protected Bitmap1 _lastTexture = null;
    protected Size2F _lastRawSourceSize;
    protected RawRectangleF _lastTextureClip;

    protected Bitmap1 _currentTexture = null;
    protected Size2F _currentTextureSize; // Size2 of the texture, can be bigger than the actual image in the texture because of DX. _currentTextureClip is based on this size.
    protected RectangleF _currentTextureClip; // Clipping rectangle to be used from the _currentTexture. Values go from 0 to 1.
    protected Size2F _currentClippedSize; // Size2 of the raw image part in the texture to be shown.

    protected Bitmap1 _lastCopiedTexture = null;

    protected AbstractProperty _streamProperty;

    #region Ctor

    public ImagePlayerImageSource()
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
      ImagePlayerImageSource ppis = (ImagePlayerImageSource) source;
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

    public override void VisibilityLost()
    {
      base.VisibilityLost();
      // Delete the current and the last image to avoid image transitions the next time we are visible again
      _lastCopiedTexture = null;
      CycleTextures(null, RectangleF.Empty, RightAngledRotation.Zero);
      CycleTextures(null, RectangleF.Empty, RightAngledRotation.Zero);
    }

    public override void Allocate()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>(false);
      if (playerContextManager == null)
      {
        FreeData();
        return;
      }

      ISharpDXImagePlayer player = playerContextManager[Stream] as ISharpDXImagePlayer;
      if (player == null) 
      {
        FreeData();
        return;
      }

      lock (player.ImagesLock)
      {
        Bitmap1 texture = player.CurrentImage;
        // It's a bit stupid because the Image calls Allocate() before Setup() and thus, at the first call of this method,
        // _frameSize is empty and so we cannot calculate a proper size for this image source...
        RectangleF textureClip = player.GetTextureClip(new Size2((int) _frameSize.Width, (int) _frameSize.Height));
        // TODO: Interface between DX/D2D
        if (texture != null)
        {
          if (texture != _lastCopiedTexture)
          {
            _lastCopiedTexture = texture;
            // The SharpDX player also supports the FlipX, FlipY values, which which tells us the image should be flipped
            // in horizontal or vertical direction after the rotation. Very few images have those flags; we don't implement them here.
            CycleTextures(texture, textureClip, TranslateRotation(player.Rotation));
          }
          else if (textureClip != _currentTextureClip)
            UpdateTextureClip(textureClip);
        }
      }
    }

    #endregion

    #region Protected members

    protected override Bitmap1 LastTexture
    {
      get { return _lastTexture; }
    }

    protected override Size2F LastRawSourceSize
    {
      get { return _lastRawSourceSize; }
    }

    protected override RawRectangleF LastTextureClip
    {
      get { return _lastTextureClip; }
    }

    protected override Bitmap1 CurrentTexture
    {
      get { return _currentTexture; }
    }

    protected override Size2F CurrentRawSourceSize
    {
      get { return _currentClippedSize; }
    }

    protected override RawRectangleF CurrentTextureClip
    {
      get { return _currentTextureClip; }
    }

    public override bool IsAllocated
    {
      get { return _currentTexture != null; }
    }

    protected RightAngledRotation TranslateRotation(Presentation.Players.RightAngledRotation rotation)
    {
      return (RightAngledRotation) rotation; // Enums are compatible
    }

    protected void CycleTextures(Bitmap1 nextTexture, RectangleF textureClip, RightAngledRotation nextRotation)
    {
      TryDispose(ref _lastTexture);

      // Current -> Last
      _lastTexture = _currentTexture;
      _lastRawSourceSize = _currentClippedSize;
      _lastTextureClip = _currentTextureClip;
      _lastImageContext = _imageContext;

      // Next -> Current
      Size2F textureSize;
      _currentTexture = CreateTextureCopy(nextTexture, out textureSize);
      _currentTextureSize = textureSize;
      UpdateTextureClip(textureClip);

      _imageContext = new ImageContext
        {
            FrameSize = _frameSize,
            ShaderEffect = Effect,
            Rotation = nextRotation,
            HorizontalTextureAlignment = HorizontalTextureAlignment,
            VerticalTextureAlignment = VerticalTextureAlignment
        };

      StartTransition();
      FireChanged();
    }

    protected void UpdateTextureClip(RectangleF textureClip)
    {
      _currentClippedSize = new Size2F(_currentTextureSize.Width * textureClip.Width, _currentTextureSize.Height * textureClip.Height);
      _currentTextureClip = textureClip;
    }

    protected static Bitmap1 CreateTextureCopy(Bitmap1 sourceTexture, out Size2F textureSize)
    {
      if (sourceTexture == null)
      {
        textureSize = new Size2F();
        return null;
      }

      var desc = new BitmapProperties1(sourceTexture.PixelFormat);
      var result = new Bitmap1(GraphicsDevice11.Instance.Context2D1, sourceTexture.PixelSize, desc);
      result.CopyFromBitmap(sourceTexture);
      textureSize = result.Size;
      return result;
    }

    protected override void FreeData()
    {
      base.FreeData();
      _lastCopiedTexture = null;
      TryDispose(ref _lastTexture);
      TryDispose(ref _currentTexture);
      _lastImageContext.Clear();
    }

    #endregion
  }
}
