#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;

namespace MediaPortal.UI.SkinEngine.Utils
{
  /// <summary>
  /// The <see cref="ImageSourceFactory"/> creates <see cref="ImageSource"/>s to be used for <see cref="Image"/>. It allows adding new factories by plugins
  /// (<see cref="RegisterCustomImageSource"/> and <see cref="RemoveCustomImageSource"/>).
  /// </summary>
  public static class ImageSourceFactory
  {
    public delegate ImageSource CreateCustomImageSourceDelegate(object source, int width, int height);

    private static readonly List<CreateCustomImageSourceDelegate> _imageSourceFactories = new List<CreateCustomImageSourceDelegate>();

    static ImageSourceFactory()
    {
      RegisterCustomImageSource(CreateMediaItemThumbnailAspectSource);
      RegisterCustomImageSource(ResourceLocatorSource);
      RegisterCustomImageSource(ImageSource);
    }

    /// <summary>
    /// Tries to replace the <paramref name="oldCreator"/> by the <paramref name="newCreator"/> at the original position in the factory list.
    /// If the <paramref name="oldCreator"/> doesn't exists, the <paramref name="newCreator"/> will be added to the end of factory list.
    /// </summary>
    /// <param name="oldCreator">Old creator</param>
    /// <param name="newCreator">New creator</param>
    public static void ReplaceCustomImageSource(CreateCustomImageSourceDelegate oldCreator, CreateCustomImageSourceDelegate newCreator)
    {
      int oldIndex = _imageSourceFactories.IndexOf(oldCreator);
      if (oldIndex >= 0)
        _imageSourceFactories[oldIndex] = newCreator;
      else
        RegisterCustomImageSource(newCreator);
    }

    public static void RegisterCustomImageSource(CreateCustomImageSourceDelegate creator)
    {
      if (!_imageSourceFactories.Contains(creator))
        _imageSourceFactories.Add(creator);
    }

    public static void RemoveCustomImageSource(CreateCustomImageSourceDelegate creator)
    {
      if (_imageSourceFactories.Contains(creator))
        _imageSourceFactories.Remove(creator);
    }

    public static bool TryCreateImageSource(object source, int width, int height, out ImageSource imageSource)
    {
      foreach (var factory in _imageSourceFactories)
      {
        imageSource = factory(source, width, height);
        if (imageSource != null)
          return true;
      }
      imageSource = null;
      return false;
    }

    #region Inbuilt factories

    public static ImageSource ImageSource(object source, int width, int height)
    {
      return source as ImageSource;
    }

    public static ImageSource ResourceLocatorSource(object source, int width, int height)
    {
      IResourceLocator locator = source as IResourceLocator;
      if (locator == null)
        return null;
      IResourceLocator resourceLocator = locator;
      IResourceAccessor ra = resourceLocator.CreateAccessor();
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        ra.Dispose();
      else
        return new ResourceAccessorTextureImageSource(fsra, RightAngledRotation.Zero);
      return null;
    }

    /// <summary>
    /// Constructs a <see cref="BinaryTextureImageSource"/> for thumbnails of the given size from MediaItems.
    /// </summary>
    public static ImageSource CreateMediaItemThumbnailAspectSource(object source, int width, int height)
    {
      MediaItem mediaItem = source as MediaItem;
      if (mediaItem == null)
        return null;

      Guid id = mediaItem.MediaItemId;
      // Local media items don't have an item id
      string key = (id == Guid.Empty ? Guid.NewGuid() : id).ToString();
      byte[] textureData = null;

      // Each resolution is cached separately. If we read cache only and our favourite resolution is not yet in cache,
      // we try to find any other existing.
      SingleMediaItemAspect mediaAspect;
      if (!MediaItemAspect.TryGetAspect(mediaItem.Aspects, ThumbnailLargeAspect.Metadata, out mediaAspect))
        return null;

      textureData = (byte[])mediaAspect.GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);

      ImageRotation miRotation;
      bool flipX;
      bool flipY;
      ImageAspect.GetOrientationMetadata(mediaItem, out miRotation, out flipX, out flipY);
      RightAngledRotation rotation = RotationTranslator.TranslateToRightAngledRotation(miRotation);
      return new BinaryTextureImageSource(textureData, rotation, key);
    }

    #endregion
  }
}
