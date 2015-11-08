using System;
using System.Drawing;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Brushes.Animation;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UPnPRenderer.MediaItems;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UPnPRenderer.Players
{
  class UPnPRendererImagePlayer : ISharpDXImagePlayer, IReusablePlayer
  {
    public const string MIMETYPE = "upnpimage/upnprenderer";
    public const string DUMMY_FILE = "UPnPRenderer://localhost/UPnPRendererImage.upnp";
    protected static readonly IImageAnimator STILL_IMAGE_ANIMATION = new StillImageAnimator();

    readonly object _imageSync = new object();
    TextureAsset _texture;
    protected SizeF _textureMaxUv = new SizeF(1, 1);

    // Image animation effect
    IImageAnimator _animator;

    string _itemTitle;
    PlayerState _state = PlayerState.Stopped;

    public Texture CurrentImage
    {
      get { return _texture != null ? _texture.Texture : null; }
    }

    public SharpDX.RectangleF GetTextureClip(Size2 outputSize)
    {
      lock (_imageSync)
      {
        Size imageSize = ImageSize;
        SharpDX.RectangleF textureClip = _animator.GetZoomRect(new Size2(imageSize.Width, imageSize.Height), outputSize, DateTime.Now);
        return new SharpDX.RectangleF(textureClip.X * _textureMaxUv.Width, textureClip.Y * _textureMaxUv.Height, textureClip.Width * _textureMaxUv.Width, textureClip.Height * _textureMaxUv.Height);
      }
    }

    public object ImagesLock
    {
      get { return _imageSync; }
    }

    public IResourceLocator CurrentImageResourceLocator
    {
      get { return null; }
    }

    public bool FlipX
    {
      get { return false; }
    }

    public bool FlipY
    {
      get { return false; }
    }

    public Size ImageSize
    {
      get { return _texture != null ? new Size(_texture.Width, _texture.Height) : Size.Empty; }
    }

    public RightAngledRotation Rotation
    {
      get { return RightAngledRotation.Zero; }
    }

    public string MediaItemTitle
    {
      get { lock (_imageSync) return _itemTitle; }
    }

    public string Name
    {
      get { return "UPnPRenderer Image"; }
    }

    public PlayerState State
    {
      get { lock (_imageSync) return _state; }
    }

    public void Stop()
    {
      lock (_imageSync) _state = PlayerState.Stopped;
    }

    public bool NextItem(MediaItem mediaItem, StartTime startTime)
    {
      byte[] imageData;
      string imageId;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, UPnPImageAspect.ATTR_IMAGE, out imageData) ||
          !MediaItemAspect.TryGetAttribute(mediaItem.Aspects, UPnPImageAspect.ATTR_IMAGE_ID, out imageId))
        return false;

      UpdateTexture(imageId, imageData);
      return true;
    }

    protected void UpdateTexture(string imageId, byte[] imageData)
    {
      lock (_imageSync)
      {
        _itemTitle = imageId;
        _texture = ContentManager.Instance.GetTexture(imageData, imageId);
        if (_texture == null)
          return;
        if (!_texture.IsAllocated)
          _texture.Allocate();
        if (!_texture.IsAllocated)
          return;

        //ImagePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImagePlayerSettings>() ?? new ImagePlayerSettings();
        _animator = STILL_IMAGE_ANIMATION; //settings.UseKenBurns ? new KenBurnsAnimator() : STILL_IMAGE_ANIMATION;
        SurfaceDescription desc = _texture.Texture.GetLevelDescription(0);
        _textureMaxUv = new SizeF(_texture.Width / (float)desc.Width, _texture.Height / (float)desc.Height);

        // Reset animation
        _animator.Initialize();

        _state = PlayerState.Active;
      }
    }

    public event RequestNextItemDlgt NextItemRequest;
  }
}
