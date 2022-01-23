#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PathManager;
using Microsoft.Owin;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache
{
  internal static class ImageCache
  {
    /// <summary>
    /// {0} = Guid of media item
    /// {1} = identifier from GetIdentifier()
    /// {2} = width
    /// {3} = height
    /// {4} = borders
    /// {5} = offset
    /// {6} = WebMediaType
    /// {8} = WebFileType
    /// </summary>
    private const string FILENAME_PATTERN = "imageCache_{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}";

    /// <summary>
    /// The sub dir in the DATA folder
    /// </summary>
    private const string CHACHE_DIR = "MP2ExtendedCache\\";

    private static Object _lockObject = new Object();

    private static string GetFilePath(CacheIdentifier identifier)
    {
      lock (_lockObject)
      {
        string dataDirectory = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>\\" + CHACHE_DIR);
        if (!Directory.Exists(dataDirectory))
          Directory.CreateDirectory(dataDirectory);
        string filename = string.Format(FILENAME_PATTERN, identifier.MediaItemId, identifier.ClassId, identifier.Width, identifier.Height, identifier.Borders, identifier.Offset, identifier.FanArtType, identifier.FanArtMediaType);

        return Path.Combine(dataDirectory, filename);
      }
    }

    private static DateTime GetLastModifiedTime(string filepath)
    {
      return File.GetLastWriteTime(filepath);
    }

    /// <summary>
    /// Adds an image to the cache, but only if it doesn't exist yet
    /// </summary>
    /// <param name="data">The image data as byte Array</param>
    /// <param name="id">Guid of the media item</param>
    /// <param name="identifier">identifier from GetIdentifier()</param>
    /// <param name="width">Width of the finale image</param>
    /// <param name="height">height of the final image</param>
    /// <param name="borders">borders of the final image</param>
    /// <returns>Returns true if the image was added to the cache, false if the image is already in the cache</returns>
    internal static bool AddImageToCache(IOwinContext context, byte[] data, CacheIdentifier identifier)
    {
      lock (_lockObject)
      {
        if (IsInCache(context, identifier))
          return false;
        FileStream stream = File.OpenWrite(GetFilePath(identifier));
        stream.Write(data, 0, data.Length);
        stream.Close();
        return true;
      }
    }

    internal static bool TryGetImageFromCache(IOwinContext context, CacheIdentifier identifier, out byte[] data)
    {
      lock (_lockObject)
      {
        data = new byte[0];
        string filepath = GetFilePath(identifier);
        if (!IsInCache(context, identifier))
          return false;

        FileStream stream = File.OpenRead(GetFilePath(identifier));
        data = new byte[Convert.ToInt32(stream.Length)];
        stream.Read(data, 0, Convert.ToInt32(stream.Length));
        stream.Close();
        return true;
      }
    }

    /// <summary>
    /// Checks if an image is already in the cache
    /// </summary>
    /// <param name="id">Guid of the media item</param>
    /// <param name="identifier">identifier from GetIdentifier()</param>
    /// <param name="width">Width of the finale image</param>
    /// <param name="height">height of the final image</param>
    /// <param name="borders">borders of the final image</param>
    /// <returns>Returns true if the image was added to the cache, false if the image is already in the cache</returns>
    /// <returns>true if the image is in the cache, otherwise false</returns>
    internal static bool IsInCache(IOwinContext context, CacheIdentifier identifier)
    {
      lock (_lockObject)
      {
        string filepath = GetFilePath(identifier);
        DateTime dateAdded;
        if (!identifier.IsTvRadio)
        {
          ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
          necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
          MediaItem item = MediaLibraryAccess.GetMediaItemById(context, identifier.MediaItemId, necessaryMIATypes, null);
          if (item == null)
            return false;
          dateAdded = (DateTime)item.GetAspect(ImporterAspect.Metadata)[ImporterAspect.ATTR_DATEADDED];
        }
        else
        {
          dateAdded = DateTime.Now.AddMonths(-1); // refresh image evry month
        }

        return (File.Exists(GetFilePath(identifier)) && DateTime.Compare(GetLastModifiedTime(filepath), dateAdded) >= 0);
      }
    }

    internal static CacheIdentifier GetIdentifier(Guid id, bool isTvRadio, int width, int height, string borders, int offset, string fanArtType, string fanartMediaType, [CallerFilePath] string callerFile = null)
    {
      lock (_lockObject)
      {
        string identifier = Path.GetFileNameWithoutExtension(callerFile);

        return new CacheIdentifier
        {
          ClassId = identifier,
          MediaItemId = id,
          IsTvRadio = isTvRadio,
          Width = width,
          Height = height,
          Borders = borders,
          Offset = offset,
          FanArtType = fanArtType,
          FanArtMediaType = fanartMediaType
        };
      }
    }

    internal class CacheIdentifier
    {
      internal string ClassId { get; set; }
      internal Guid MediaItemId { get; set; }
      internal bool IsTvRadio { get; set; }
      internal int Width { get; set; }
      internal int Height { get; set; }
      internal string Borders { get; set; }
      internal int Offset { get; set; }
      internal string FanArtType { get; set; }
      internal string FanArtMediaType { get; set; }
    }
  }
}
