#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Cache;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Encapsulates a path expression which describes a resource provider chain in a given system
  /// to a file or directory in MediaPortal 2.
  /// </summary>
  /// <remarks>
  /// <para>
  /// In MediaPortal 2, a resource is identified by the id of a resource provider and a path which is sensible to that provider.
  /// Providers can be chained in that way that a so called "base provider" provides access to a resource (e.g. from
  /// the local HDD, form an FTP server, from an HTTP site, ...) by providing a <see cref="Stream"/>. If this
  /// resource is able to be interpreted as an archive, for example, then an archive provider (which is able to handle such
  /// an archive resource) can be chained to the base provider, reading the archive contents from the stream.
  /// Such a chain of providers is called a resource provider chain. There can be any number of providers chained
  /// to an existing resource provider chain.
  /// </para>
  /// <para>
  /// A <see cref="ResourcePath"/> consists of one or more <see cref="ProviderPathSegment"/>s, describing a resource
  /// provider chain. Each of those path segments identifies a provider id together with a path in that provider.
  /// In the simplest case, that chain only consists of a base resource provider
  /// </para>
  /// <para>
  /// <h1>Serialized form of a <see cref="ResourcePath"/></h1>
  /// Simple example:
  /// <example>
  /// A file at the local HDD with file name <c>D:\Media\R.E.M\Around the sun\R.E.M. - Electron Blue.mp3</c> will be
  /// described by the resource path:
  /// <code>{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}:///D:/Media/R.E.M/Around the sun/R.E.M. - Electron Blue.mp3</code>
  /// where <c>{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}</c> is the id of the local HDD provider.
  /// </example>
  /// A more complex example:
  /// <example>
  /// Think of a CD image file, located at the local HDD path <c>D:\Media\ISOs\MP3s.iso</c> and thus will be accessed
  /// by the local HDD provider. The image itself is navigated using the ISO provider. Inside the image, there is a
  /// RAR archive of name <c>R.E.M. - Around The Sun.rar</c>, which is accessed using the RAR provider. The complete
  /// path looks like this:
  /// <code>{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}:///D:/Media/ISOs/MP3s.iso&gt;{90C92668-DBBF-47b3-935E-B84426A96105}:///R.E.M. - Around The Sun.rar&gt;{10C18F11-854A-470e-9C47-ECF9EF867066}:///R.E.M. - Electron Blue.mp3</code>
  /// List of the providers used in that example:
  /// <list type="table">
  /// <listheader><term>Provider id</term><description>Provider name</description></listheader>
  /// <item><term><c>{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}</c></term><description>Local HDD provider</description></item>
  /// <item><term><c>{90C92668-DBBF-47b3-935E-B84426A96105}</c></term><description>ISO image provider</description></item>
  /// <item><term><c>{10C18F11-854A-470e-9C47-ECF9EF867066}</c></term><description>RAR archive provider</description></item>
  /// </list>
  /// </example>
  /// </para>
  /// </remarks>
  public class ResourcePath : IEnumerable<ProviderPathSegment>, IComparable<ResourcePath>
  {
    public const bool USE_RA_CACHE = true;
    public const int RESOURCE_PATH_CACHE_SIZE = 100;

    protected static SmallLRUCache<ResourcePath, IResourceAccessor> _cachedResourceAccessors = new SmallLRUCache<ResourcePath, IResourceAccessor>(RESOURCE_PATH_CACHE_SIZE);

    static ResourcePath()
    {
      _cachedResourceAccessors.ObjectPruned += OnResourcePathObjectsPruned;
    }

    protected List<ProviderPathSegment> _pathSegments = new List<ProviderPathSegment>();

    /// <summary>
    /// Creates a new instance of <see cref="ResourcePath"/> with the given path segments.
    /// </summary>
    /// <param name="pathSegments">Path segments describing the resource provider chain. The first path segment might be
    /// a base path segment, all following segments (if any) must be relative path segments.</param>
    public ResourcePath(IEnumerable<ProviderPathSegment> pathSegments)
    {
      CollectionUtils.AddAll(_pathSegments, pathSegments);
      for (int i = 1; i < _pathSegments.Count; i++)
        if (_pathSegments[i].IsBaseSegment)
          throw new ArgumentException(string.Format("Base path segment '{0}' cannot be appended to path '{1}'",
              _pathSegments[i].Serialize(), Serialize()));
    }

    static void OnResourcePathObjectsPruned(ILRUCache<ResourcePath, IResourceAccessor> sender, ResourcePath key, IResourceAccessor value)
    {
      value.Dispose();
    }

    protected static void AddToCache(ResourcePath path, IResourceAccessor ra)
    {
      lock (_cachedResourceAccessors.SyncObj)
      {
        if (!_cachedResourceAccessors.Contains(path))
          _cachedResourceAccessors.Add(path, ra.Clone());
      }
    }

    protected static bool TryGetCloneFromCache(ResourcePath path, out IResourceAccessor ra)
    {
      lock (_cachedResourceAccessors.SyncObj)
        if (_cachedResourceAccessors.TryGetValue(path, out ra))
        {
          _cachedResourceAccessors.Touch(path);
          ra = ra.Clone();
          return true;
        }
      return false;
    }

    /// <summary>
    /// Returns the path segments in the order of the resource provider chain (the first path segment is the base
    /// path segment, if present).
    /// </summary>
    public IList<ProviderPathSegment> PathSegments
    {
      get { return _pathSegments; }
    }

    public ProviderPathSegment BasePathSegment
    {
      get { return _pathSegments.Count == 0 ? null : _pathSegments[0]; }
    }

    public ProviderPathSegment LastPathSegment
    {
      get { return _pathSegments.Count == 0 ? null : _pathSegments[_pathSegments.Count - 1]; }
    }

    /// <summary>
    /// Gets the file name and extension of the last path segment or <c>null</c>, if this resource path is empty.
    /// </summary>
    public string FileName
    {
      get { return _pathSegments.Count == 0 ? null : ProviderPathHelper.GetFileName(StringUtils.RemoveSuffixIfPresent(_pathSegments[_pathSegments.Count-1].Path, "/")); }
    }

    /// <summary>
    /// Returns the <paramref name="index"/>'th path segment.
    /// </summary>
    /// <param name="index">Index of the path segment to return.</param>
    /// <returns>Path segment at the <paramref name="index"/>'th position.</returns>
    /// <exception cref="IndexOutOfRangeException">If the given index is lower than <c>0</c> or bigger than the length of
    /// the <see cref="PathSegments"/> list.</exception>
    public ProviderPathSegment this[int index]
    {
      get { return _pathSegments[index]; }
    }

    /// <summary>
    /// Returns the information if the resource provider chain described by this instance can be built-up, i.e. the path contains
    /// a base segment.
    /// </summary>
    public bool IsAbsolute
    {
      get { return _pathSegments.Count > 0 && _pathSegments[0].IsBaseSegment; }
    }

    /// <summary>
    /// Returns the information if this resource path points to an URL accessible network resource for which an <see cref="INetworkResourceAccessor"/>
    /// can be created locally.
    /// </summary>
    public bool IsNetworkResource
    {
      get
      {
        if (_pathSegments.Count == 0)
          return false;
        ProviderPathSegment pathSegment = _pathSegments[0];
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        IResourceProvider resourceProvider;
        return mediaAccessor.LocalResourceProviders.TryGetValue(pathSegment.ProviderId, out resourceProvider) && resourceProvider.Metadata.NetworkResource;
      }
    }

    /// <summary>
    /// Returns the information if this resource path can be used to set up a local resource provider chain.
    /// </summary>
    /// <remarks>
    /// <seealso cref="CheckValidLocalPath"/>
    /// </remarks>
    public bool IsValidLocalPath
    {
      get
      {
        try
        {
          CheckValidLocalPath();
        }
        catch (Exception)
        {
          return false;
        }
        return true;
      }
    }

    /// <summary>
    /// Convenience method for creating a <see cref="ResourcePath"/> of a single base resource provider path.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any checks if the resource provider of the given <paramref name="baseProviderId"/> is present
    /// in the system nor if it is a base resource provider. The caller has to ensure those criteria, else, the returned
    /// resource path won't work (i.e. no media accessor can be created).
    /// </remarks>
    /// <param name="baseProviderId">Id of the provider for the given path.</param>
    /// <param name="providerPath">Path in the provider, the returned resource path should represent.</param>
    /// <returns>Resource path representing the given path in the given provider.</returns>
    public static ResourcePath BuildBaseProviderPath(Guid baseProviderId, string providerPath)
    {
      return new ResourcePath(new ProviderPathSegment[] {new ProviderPathSegment(baseProviderId, providerPath, true)});
    }

    /// <summary>
    /// Convenience method for creating a resource path equal to this path with a path segment appended. This method
    /// doesn't change this instance; it creates a new <see cref="ResourcePath"/> instance.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any checks if the resource provider of the given <paramref name="chainedProviderId"/> is present
    /// in the system nor if it is a chained resource provider. The caller has to ensure those criteria, else, the returned
    /// resource path won't work (i.e. no media accessor can be created).
    /// </remarks>
    /// <param name="chainedProviderId">Id of a chained resource provider to be appended to the copy of this path.</param>
    /// <param name="providerPath">Path in the last chained provider segment which will be added to the copy of this path.</param>
    /// <returns>Resource path representing a path which is equal to this path with the given path segment appended.</returns>
    public ResourcePath ChainUp(Guid chainedProviderId, string providerPath)
    {
      ResourcePath result = new ResourcePath(_pathSegments);
      result.Append(chainedProviderId, providerPath);
      return result;
    }

    /// <summary>
    /// Appends the given provider path segment to this resource path.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any checks if the resource provider of the given <paramref name="chainedProviderId"/> is present
    /// in the system nor if it is a chained resource provider. The caller has to ensure those criteria, else, the returned
    /// resource path won't work (i.e. no media accessor can be created).
    /// </remarks>
    public void Append(Guid chainedProviderId, string providerPath)
    {
      _pathSegments.Add(new ProviderPathSegment(chainedProviderId, providerPath, false));
    }

    /// <summary>
    /// Appends the given provider path segment to this resource path.
    /// </summary>
    /// <exception cref="ArgumentException">If the given path segment is a base path segment and thus cannot be appended to
    /// this resource path.</exception>
    public void Append(ProviderPathSegment providerPathSegment)
    {
      if (providerPathSegment.IsBaseSegment)
        throw new ArgumentException(string.Format("Path segment '{0}' is a base path segment and cannot be appended to the resource path '{1}'", providerPathSegment.Serialize(), Serialize()));
      _pathSegments.Add(providerPathSegment);
    }

    /// <summary>
    /// Serializes this resource path to its string representation.
    /// </summary>
    /// <remarks>
    /// See the class docs of <see cref="ResourcePath"/> for examples of serialized resource paths.
    /// </remarks>
    /// <returns>A string instance of the form described in the class docs of this class.</returns>
    public string Serialize()
    {
      StringBuilder result = new StringBuilder(_pathSegments.Count * 200);
      foreach (ProviderPathSegment pathSegment in _pathSegments)
        result.Append(pathSegment.Serialize());
      return result.ToString();
    }

    /// <summary>
    /// Deserializes a resource path in its string representation to a <see cref="ResourcePath"/> instance.
    /// </summary>
    /// <remarks>
    /// See the class docs of <see cref="ResourcePath"/> for examples of serialized resource paths.
    /// </remarks>
    /// <param name="resourcePathStr">Resource path of the form
    /// <c>{[Base-Provider-Id]}://[Base-Provider-Path]&gt;{[Virtual-Provider1-Id]}://[Virtual-Provider-Path1]&lt;...&gt;</c></param>
    /// <returns><see cref="ResourcePath"/> instance.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="resourcePathStr"/> is malformed.</exception>
    /// <exception cref="ArgumentNullException">If <paramref name="resourcePathStr"/> is <c>null</c>.</exception>
    public static ResourcePath Deserialize(string resourcePathStr)
    {
      if (resourcePathStr == null)
        throw new ArgumentNullException("resourcePathStr", "Cannot deserialize path string with value null");
      bool firstIsBase = true;
      if (resourcePathStr.StartsWith(">"))
      {
        resourcePathStr = resourcePathStr.Substring(1);
        firstIsBase = false;
      }
      string[] pathSegmentStrs = resourcePathStr.Split('>');
      ICollection<ProviderPathSegment> pathSegments = new List<ProviderPathSegment>(pathSegmentStrs.Length);
      bool isBase = firstIsBase;
      foreach (string pathSegmentStr in pathSegmentStrs)
      {
        pathSegments.Add(ProviderPathSegment.Deserialize(pathSegmentStr, isBase));
        isBase = false;
      }
      return new ResourcePath(pathSegments);
    }

    /// <summary>
    /// Checks if a resource provider chain for this resource path can be created in the local system.
    /// This method only checks the availability of providers; it doesn't check if the given path is available
    /// in the providers.
    /// </summary>
    public void CheckValidLocalPath()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!IsAbsolute) // This will check the path itself. Below, we will check if the referenced resource providers implement the correct interfaces
        throw new ArgumentException(string.Format(
            "Can only access media files at an absolute resource path (given relative path is '{0}')", Serialize()));
      IEnumerator<ProviderPathSegment> enumer = _pathSegments.GetEnumerator();
      if (!enumer.MoveNext())
        throw new UnexpectedStateException("Cannot build resource accessor for an empty resource path");
      bool baseSegment = true;
      do
      {
        ProviderPathSegment pathSegment = enumer.Current;
        IResourceProvider resourceProvider;
        if (!mediaAccessor.LocalResourceProviders.TryGetValue(pathSegment.ProviderId, out resourceProvider))
          throw new IllegalCallException("The resource provider with id '{0}' is not accessible in the current system", pathSegment.ProviderId);
        if (baseSegment)
        {
          IBaseResourceProvider baseProvider = resourceProvider as IBaseResourceProvider;
          if (baseProvider == null)
            throw new IllegalCallException("The resource provider with id '{0}' does not implement the {1} interface", pathSegment.ProviderId, typeof(IBaseResourceProvider).Name);
          baseSegment = false;
        }
        else
        {
          IChainedResourceProvider chainedProvider = resourceProvider as IChainedResourceProvider;
          if (chainedProvider == null)
            throw new IllegalCallException("The resource provider with id '{0}' does not implement the {1} interface", pathSegment.ProviderId, typeof(IChainedResourceProvider).Name);
        }
      } while (enumer.MoveNext());
    }

    /// <summary>
    /// Creates a local resource provider chain for this resource path, if it is a local path
    /// (see <see cref="CheckValidLocalPath"/>), and returns its result in a <see cref="IResourceAccessor"/> instance.
    /// </summary>
    /// <param name="result">Returns the resource accessor to access the resource represented by this path, if the
    /// return value is <c>true</c>. Else, this parameter doesn't return a meaningful value.</param>
    /// <returns><c>true</c>, if a resource accessor could successfully be built for this path.</returns>
    /// <exception cref="IllegalCallException">If one of the referenced resource providers is not available in the system or
    /// has the wrong type.</exception>
    /// <exception cref="UnexpectedStateException">If this path is empty.</exception>
    public bool TryCreateLocalResourceAccessor(out IResourceAccessor result)
    {
      IResourceAccessor resourceAccessor = null;
      if (USE_RA_CACHE)
        if (TryGetCloneFromCache(this, out resourceAccessor))
        {
          result = resourceAccessor;
          return true;
        }
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IEnumerator<ProviderPathSegment> enumer = _pathSegments.GetEnumerator();
      if (!enumer.MoveNext())
        throw new UnexpectedStateException("Cannot build resource accessor for an empty resource path");
      try
      {
        do
        {
          ProviderPathSegment pathSegment = enumer.Current;
          IResourceProvider resourceProvider;
          if (!mediaAccessor.LocalResourceProviders.TryGetValue(pathSegment.ProviderId, out resourceProvider))
            throw new IllegalCallException("The resource provider with id '{0}' is not accessible in the current system", pathSegment.ProviderId);
          if (resourceAccessor == null)
          {
            IBaseResourceProvider baseProvider = resourceProvider as IBaseResourceProvider;
            if (baseProvider == null)
              throw new IllegalCallException("The resource provider with id '{0}' does not implement the {1} interface", pathSegment.ProviderId, typeof(IBaseResourceProvider).Name);
            if (!baseProvider.TryCreateResourceAccessor(pathSegment.Path, out resourceAccessor))
            {
              result = null;
              return false;
            }
          }
          else
          {
            IChainedResourceProvider chainedProvider = resourceProvider as IChainedResourceProvider;
            if (chainedProvider == null)
              throw new IllegalCallException("The resource provider with id '{0}' does not implement the {1} interface", pathSegment.ProviderId, typeof(IChainedResourceProvider).Name);
            IFileSystemResourceAccessor fsra = resourceAccessor as IFileSystemResourceAccessor;
            if (fsra == null)
              throw new IllegalCallException("Cannot chain up a resource provider to resource of type {0} (Path: '{1}')", resourceAccessor.GetType(), ToString());
            IFileSystemResourceAccessor chainedRa;
            if (!chainedProvider.TryChainUp(fsra, pathSegment.Path, out chainedRa))
            {
              resourceAccessor.Dispose();
              result = null;
              return false;
            }
            resourceAccessor = chainedRa;
          }
        } while (enumer.MoveNext());
      }
      catch (Exception)
      {
        if (resourceAccessor != null)
          resourceAccessor.Dispose();
        throw;
      }
      if (USE_RA_CACHE)
        AddToCache(this, resourceAccessor);
      result = resourceAccessor;
      return true;
    }

    public static void ClearResourceCache()
    {
      lock (_cachedResourceAccessors.SyncObj)
      {
        foreach (IResourceAccessor resourceAccessor in _cachedResourceAccessors.Values)
          resourceAccessor.Dispose();
        _cachedResourceAccessors.Clear();
      }
    }

    protected static ICollection<string> UnrollPathSegments(ResourcePath rp)
    {
      ICollection<string> result = new List<string>();
      foreach (ProviderPathSegment segment in rp._pathSegments)
      {
        result.Add(segment.ProviderId.ToString());
        CollectionUtils.AddAll(result, segment.Path.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries));
      }
      return result;
    }

    public bool IsSameOrParentOf(ResourcePath other)
    {
      ICollection<string> thisPathSegments = UnrollPathSegments(this);
      ICollection<string> thatPathSegments = UnrollPathSegments(other);
      return thatPathSegments.StartsWith(thisPathSegments, StringComparer.InvariantCulture);
    }

    public bool IsParentOf(ResourcePath other)
    {
      ICollection<string> thisPathSegments = UnrollPathSegments(this);
      ICollection<string> thatPathSegments = UnrollPathSegments(other);
      return thisPathSegments.Count < thatPathSegments.Count && thatPathSegments.StartsWith(thisPathSegments, StringComparer.InvariantCulture);
    }

    #region IEnumerable<ProviderPathSegment> implementation

    public IEnumerator<ProviderPathSegment> GetEnumerator()
    {
      return _pathSegments.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _pathSegments.GetEnumerator();
    }

    #endregion

    #region IComparable<ResourcePath> implementation

    public int CompareTo(ResourcePath other)
    {
      return Serialize().CompareTo(other.Serialize());
    }

    #endregion

    #region Base overrides

    public override int GetHashCode()
    {
      return Serialize().GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ResourcePath))
        return false;
      ResourcePath other = (ResourcePath) obj;
      return Serialize() == other.Serialize();
    }

    public static bool operator ==(ResourcePath path1, ResourcePath path2)
    {
      bool p2null = ReferenceEquals(path2, null);
      if (ReferenceEquals(path1, null))
        return p2null;
      if (p2null)
        return false;
      return path1.Equals(path2);
    }

    public static bool operator !=(ResourcePath path1, ResourcePath path2)
    {
      return !(path1 == path2);
    }

    public override string ToString()
    {
      return Serialize();
    }

    #endregion
  }
}
