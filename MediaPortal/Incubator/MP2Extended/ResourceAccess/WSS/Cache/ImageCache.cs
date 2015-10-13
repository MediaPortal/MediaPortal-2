using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Utilities.FileSystem;

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
    /// </summary>
    private const string FILENAME_PATTERN = "imageCache_{0}_{1}_{2}_{3}_{4}";

    /// <summary>
    /// The sub dir in the DATA folder
    /// </summary>
    private const string CHACHE_DIR = "MP2ExtendedCache\\";

    private static string GetFilePath(Guid id, CacheIdentifier identifier, int width, int height, string borders)
    {
      string dataDirectory = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>\\"+CHACHE_DIR);
      if (!Directory.Exists(dataDirectory))
        Directory.CreateDirectory(dataDirectory);
      string filename = string.Format(FILENAME_PATTERN, id, identifier.Id, width, height, borders);

      return Path.Combine(dataDirectory, filename);
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
    internal static bool AddImageToCache(byte[] data, Guid id, CacheIdentifier identifier, int width, int height, string borders)
    {
      if (IsInCache(id, identifier, width, height, borders))
        return false;
      FileStream stream = File.OpenWrite(GetFilePath(id, identifier, width, height, borders));
      stream.Write(data, 0, data.Length);
      stream.Close();
      return true;
    }

    internal static bool TryGetImageFromCache(Guid id, CacheIdentifier identifier, int width, int height, string borders, out byte[] data)
    {
      data = new byte[0];
      string filepath = GetFilePath(id, identifier, width, height, borders);
      if (!IsInCache(id, identifier, width, height, borders))
        return false;

      FileStream stream = File.OpenRead(GetFilePath(id, identifier, width, height, borders));
      data = new byte[Convert.ToInt32(stream.Length)];
      stream.Read(data, 0, Convert.ToInt32(stream.Length));
      stream.Close();
      return true;
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
    internal static bool IsInCache(Guid id, CacheIdentifier identifier, int width, int height, string borders)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);
      if (item == null)
        return false;
      SingleMediaItemAspect importerAspect = MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata);
      DateTime dateAdded = (DateTime)importerAspect[ImporterAspect.ATTR_DATEADDED];
      
      string filepath = GetFilePath(id, identifier, width, height, borders);
      return (File.Exists(GetFilePath(id, identifier, width, height, borders)) && DateTime.Compare(GetLastModifiedTime(filepath), dateAdded) >= 0);
    }

    internal static CacheIdentifier GetIdentifier()
    {
      var mth = new StackTrace().GetFrame(1).GetMethod();
      var identifier = mth.ReflectedType == null ? mth.Name : mth.ReflectedType.Name;

      return new CacheIdentifier { Id = identifier };
    }

    internal class CacheIdentifier
    {
      internal string Id { get; set; }
    }
  }
}
