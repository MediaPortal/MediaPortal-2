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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.IO;
using MediaPortal.Utilities.Graphics;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using DuoVia.FuzzyStrings;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="BaseInfo"/> contains metadata information about a thumbnail item.
  /// </summary>
  public abstract class BaseInfo
  {
    /// <summary>
    /// Maximum cover image width. Larger images will be scaled down to fit this dimension.
    /// </summary>
    public const int MAX_COVER_WIDTH = 1000;

    /// <summary>
    /// Maximum cover image height. Larger images will be scaled down to fit this dimension.
    /// </summary>
    public const int MAX_COVER_HEIGHT = 1000;

    /// <summary>
    /// Binary data for the thumbnail image.
    /// </summary>
    public byte[] Thumbnail = null;

    public abstract bool IsBaseInfoPresent { get; }

    public abstract bool HasExternalId { get; }

    public string FanArtToken = null;

    /// <summary>
    /// Used to replace all "." and "_" that are not followed by a word character.
    /// <example>Replaces <c>"Once.Upon.A.Time.S01E13"</c> to <c>"Once Upon A Time S01E13"</c>, but keeps the <c>"."</c> inside
    /// <c>"Dr. House"</c>.</example>
    /// </summary>
    private const string CLEAN_WHITESPACE_REGEX = @"[\.|_](\S|$)";
    private const string SORT_REGEX = @"(^The\s+)|(^An?\s+)|(^De[rsmn]\s+)|(^Die\s+)|(^Das\s+)|(^Ein(e[srmn]?)?\s+)";
    private const string CLEAN_REGEX = @"<[^>]+>|&nbsp;";

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public virtual bool SetThumbnailMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (Thumbnail == null)
        return false;

      try
      {
        using (MemoryStream stream = new MemoryStream(Thumbnail))
        using (MemoryStream resized = (MemoryStream)ImageUtilities.ResizeImage(stream, ImageFormat.Jpeg, MAX_COVER_WIDTH, MAX_COVER_HEIGHT))
        {
          MediaItemAspect.SetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, resized.ToArray());
        }
        return true;
      }
      // Decoding of invalid image data can fail, but main MediaItem is correct.
      catch { }

      return false;
    }

    public static string GetSortTitle(string title)
    {
      if (string.IsNullOrEmpty(title)) return null;
      return Regex.Replace(title, SORT_REGEX, "").Trim();
    }

    public static string CleanString(string value)
    {
      if (string.IsNullOrEmpty(value)) return null;
      return Regex.Replace(Regex.Replace(value, CLEAN_REGEX, "").Trim(), @"\s{2,}", " ");
    }

    public static bool MatchNames(string name1, string name2, double threshold = 0.62)
    {
      double dice = name1.ToLowerInvariant().DiceCoefficient(name2.ToLowerInvariant());
      return dice > threshold;

      //return name1.FuzzyEquals(name2);
    }

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      return str == null ? null : Regex.Replace(str, CLEAN_WHITESPACE_REGEX, " $1").Trim(' ', '-');
    }

    public static bool IsVirtualResource(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      IList<MultipleMediaItemAspect> providerResourceAspects;
      if (MediaItemAspect.TryGetAspects(aspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
      {
        string accessorPath = (string)providerResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        if (string.IsNullOrEmpty(accessorPath))
          return true;
        ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);
        if (resourcePath.BasePathSegment.ProviderId != VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        {
          return false;
        }
      }
      return true;
    }

    public abstract bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData);

    public abstract bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData);

    public virtual bool FromString(string name)
    {
      return false;
    }

    public virtual bool CopyIdsFrom<T>(T otherInstance)
    {
      return false;
    }

    public virtual T CloneBasicInstance<T>()
    {
      return default(T);
    }

    public virtual void InitFanArtToken()
    {
      if (string.IsNullOrEmpty(FanArtToken))
        FanArtToken = Guid.NewGuid().ToString();
    }

    #endregion
  }
}
