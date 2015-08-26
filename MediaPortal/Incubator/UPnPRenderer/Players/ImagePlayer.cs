using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UPnPRenderer.MediaItems;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Brushes.Animation;
using MediaPortal.UI.SkinEngine.Players;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Drawing;

namespace MediaPortal.Extensions.UPnPRenderer.Players
{
    class UPnPRendererImagePlayer : ISharpDXImagePlayer, IReusablePlayer
    {
      public const string MIMETYPE = "upnpimage/upnprenderer";
      public const string DUMMY_FILE = "UPnPRenderer://localhost/UPnPRendererImage.upnp";
      protected static readonly IImageAnimator STILL_IMAGE_ANIMATION = new StillImageAnimator();

      object imageSync = new object();
      TextureAsset texture;
      protected SizeF textureMaxUV = new SizeF(1, 1);

      // Image animation effect
      IImageAnimator animator;

      string itemTitle;
      PlayerState state = PlayerState.Stopped;

      public Texture CurrentImage
      {
          get { return texture != null ? texture.Texture : null; }
      }

      public SharpDX.RectangleF GetTextureClip(Size2 outputSize)
      {
          lock (imageSync)
          {
              Size imageSize = ImageSize;
              SharpDX.RectangleF textureClip = animator.GetZoomRect(new Size2(imageSize.Width, imageSize.Height), outputSize, DateTime.Now);
              return new SharpDX.RectangleF(textureClip.X * textureMaxUV.Width, textureClip.Y * textureMaxUV.Height, textureClip.Width * textureMaxUV.Width, textureClip.Height * textureMaxUV.Height);
          }
      }

      public object ImagesLock
      {
          get { return imageSync; }
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
          get { return texture != null ? new System.Drawing.Size(texture.Width, texture.Height) : new System.Drawing.Size(); }
      }

      public RightAngledRotation Rotation
      {
          get { return RightAngledRotation.Zero; }
      }

      public string MediaItemTitle
      {
          get { lock (imageSync) return itemTitle; }
      }

      public string Name
      {
          get { return "UPnPRenderer Image"; }
      }

      public PlayerState State
      {
          get { lock (imageSync) return state; }
      }

      public void Stop()
      {
          lock (imageSync) state = PlayerState.Stopped;
      }

      public bool NextItem(MediaItem mediaItem, StartTime startTime)
      {
          ImageItem imageItem = mediaItem as ImageItem;
          if (imageItem == null || imageItem.ImageData == null)
              return false;

          UpdateTexture(imageItem);
          return true;
      }

      protected void UpdateTexture(ImageItem item)
      {
          lock (imageSync)
          {
              itemTitle = item.ImageId;
              texture = ContentManager.Instance.GetTexture(item.ImageData, item.ImageId);
              if (texture == null)
                  return;
              if (!texture.IsAllocated)
                  texture.Allocate();
              if (!texture.IsAllocated)
                  return;

              //ImagePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImagePlayerSettings>() ?? new ImagePlayerSettings();
              animator = STILL_IMAGE_ANIMATION; //settings.UseKenBurns ? new KenBurnsAnimator() : STILL_IMAGE_ANIMATION;
              SurfaceDescription desc = texture.Texture.GetLevelDescription(0);
              textureMaxUV = new SizeF(texture.Width / (float)desc.Width, texture.Height / (float)desc.Height);

              // Reset animation
              animator.Initialize();

              state = PlayerState.Active;
          }
      }

      public event RequestNextItemDlgt NextItemRequest;
    }
}
