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

using System;
using System.IO;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using ICSharpCode.SharpZipLib.Zip;

namespace MediaPortal.Extensions.ResourceProviders.ZipResourceProvider
{
  /// <summary>
  /// Resource provider implementation for the ZIP files.
  /// </summary>
  public class ZipResourceProvider : IChainedResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the ZIP resource provider.
    /// </summary>
    protected const string ZIP_RESOURCE_PROVIDER_ID_STR = "{6B042DB8-69AD-4B57-B869-1BCEA4E43C77}";

    /// <summary>
    /// ZIP resource provider GUID.
    /// </summary>
    public static Guid ZIP_RESOURCE_PROVIDER_ID = new Guid(ZIP_RESOURCE_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public ZipResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(ZIP_RESOURCE_PROVIDER_ID, "[ZipResourceProvider.Name]", false);
    }

    #endregion

    public static string ToEntryPath(string providerPath)
    {
      if (providerPath == "/")
        return null;
      if (providerPath.StartsWith("/"))
        return providerPath.Substring(1);
      throw new ArgumentException(string.Format("ZipResourceProvider: '{0}' is not a valid provider path", providerPath));
    }

    public static string ToProviderPath(string entryPath)
    {
      if (entryPath.StartsWith("/"))
        throw new ArgumentException(string.Format("ZipResourceProvider: '{0}' is not a valid entry path", entryPath));
      return '/' + entryPath;
    }

    #region IResourceProvider implementation

    /// <summary>
    /// Metadata descriptor for this resource provider.
    /// </summary>
    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IChainedResourceProvider implementation

    public bool CanChainUp(IResourceAccessor potentialBaseResourceAccessor)
    {
      string resourcePathName = potentialBaseResourceAccessor.ResourcePathName;
      if (string.IsNullOrEmpty(resourcePathName) || !potentialBaseResourceAccessor.IsFile ||
          !".zip".Equals(PathHelper.GetExtension(resourcePathName), StringComparison.OrdinalIgnoreCase))
        return false;

      using (Stream resourceStream = potentialBaseResourceAccessor.OpenRead()) // Not sure if the ZipFile will close the stream so we dispose it here
        try
        {
          using (new ZipFile(resourceStream))
            return true;
        }
        catch (ZipException) {} // Thrown if the file doesn't contain a valid ZIP archive
      return false;
    }

    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      string entryPath = ToEntryPath(path);
      using (Stream resourceStream = baseResourceAccessor.OpenRead()) // Not sure if the ZipFile will close the stream so we dispose it here
      using (ZipFile zFile = new ZipFile(resourceStream))
        return path.Equals("/") || zFile.Cast<ZipEntry>().Any(entry => entry.IsDirectory && entry.Name == entryPath);
    }

    public IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path)
    {
      return new ZipResourceAccessor(this, baseResourceAccessor, path);
    }

    #endregion
  }
}
