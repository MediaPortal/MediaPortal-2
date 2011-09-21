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

namespace MediaPortal.Extensions.MediaProviders.ZipMediaProvider
{
  /// <summary>
  /// Media provider implementation for the ZIP files.
  /// </summary>
  public class ZipMediaProvider : IChainedMediaProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the local filesystem media provider.
    /// </summary>
    protected const string ZIP_MEDIA_PROVIDER_ID_STR = "{6B042DB8-69AD-4B57-B869-1BCEA4E43C77}";

    /// <summary>
    /// Local filesystem media provider GUID.
    /// </summary>
    public static Guid ZIP_MEDIA_PROVIDER_ID = new Guid(ZIP_MEDIA_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected MediaProviderMetadata _metadata;

    #endregion

    #region Ctor

    public ZipMediaProvider()
    {
      _metadata = new MediaProviderMetadata(ZIP_MEDIA_PROVIDER_ID, "[ZipMediaProvider.Name]");
    }

    #endregion

    #region Implementation of IMediaProvider

    /// <summary>
    /// Metadata descriptor for this media provider.
    /// </summary>
    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region Implementation of IChainedMediaProvider

    /// <summary>
    /// Returns the information if this chained media provider can use the given
    /// <paramref name="potentialBaseResourceAccessor"/> as base resource accessor for providing a file system out of the
    /// input resource.
    /// </summary>
    /// <returns><c>true</c> if the given resource accessor can be used to chain this provider to, else <c>false</c></returns>
    public bool CanChainUp(IResourceAccessor potentialBaseResourceAccessor)
    {
      if (string.IsNullOrEmpty(potentialBaseResourceAccessor.ResourceName) || !potentialBaseResourceAccessor.IsFile)
        return false;
      if (".zip".Equals(Path.GetExtension(potentialBaseResourceAccessor.ResourceName), StringComparison.CurrentCultureIgnoreCase))
      {
        try
        {
          ZipFile zFile = new ZipFile(potentialBaseResourceAccessor.ResourcePathName);
          if (zFile.Count > 0)
            return true;
        }
        catch (Exception) {}
      }
      return false;
    }

    /// <summary>
    /// Returns the information if the given <paramref name="path"/> is a valid resource path in this provider, interpreted
    /// in the given <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor for the base resource, this provider should take as
    /// input.</param>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c>, if the given <paramref name="path"/> exists (i.e. can be accessed by this provider),
    /// else <c>false</c>.</returns>
    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      using (ZipFile zFile = new ZipFile(baseResourceAccessor.ResourcePathName))
      {
        if (path.Equals("/") && zFile.Count > 0) 
          return true;
        return zFile.Cast<ZipEntry>().Any(entry => entry.IsDirectory && entry.Name == path);
      }
    }

    /// <summary>
    /// Creates a resource accessor for the given <paramref name="path"/>, interpreted in the given
    /// <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor for the base resource, this provider should take as
    /// input.</param>
    /// <param name="path">Path to be accessed by the returned resource accessor.</param>
    /// <returns>Resource accessor instance or <c>null</c>, if the given <paramref name="baseResourceAccessor"/> cannot
    /// be used to chain this media provider up. The returned resource accessor may be of any interface derived
    /// from <see cref="IResourceAccessor"/>, i.e. a file system provider will return a resource accessor of interface
    /// <see cref="IFileSystemResourceAccessor"/>.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="path"/> is not a valid path or if the resource
    /// described by the path doesn't exist in the <paramref name="baseResourceAccessor"/>.</exception>
    public IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path)
    {
      return new ZipResourceAccessor(this, baseResourceAccessor, path);
    }

    #endregion
  }
}
