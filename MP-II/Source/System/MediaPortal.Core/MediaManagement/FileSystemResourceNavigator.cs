#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Helper class for navigating through a file system, transparently descending into chained provider resources.
  /// </summary>
  public static class FileSystemResourceNavigator
  {
    /// <summary>
    /// Returns all child directories of the given directory.
    /// </summary>
    /// <remarks>
    /// This will return all native child directories of the given directory together with all virtual child
    /// directories. The native child directories are taken directly from the given <paramref name="directoryAccessor"/>,
    /// the virtual child directories are obtained by taking the root directories of each chained media provider applied
    /// to the child files of the given directory.
    /// If, for example, the given <paramref name="directoryAccessor"/> contains a child directory "A" and a child
    /// archive file "B" which can work as input for an installed archive provider, providing the root directory "C"
    /// of that archive, this method will return the resource accessors for directories "A" and "C".
    /// </remarks>
    /// <param name="directoryAccessor">Directory resource accessor to get all child directories for.</param>
    /// <returns>Collection of directory accessors for all native and virtual child directories or <c>null</c>,
    /// if the given <paramref name="directoryAccessor"/> is not a <see cref="IFileSystemResourceAccessor"/> and
    /// if there is no chained media provider to unfold the given directory.</returns>
    public static ICollection<IFileSystemResourceAccessor> GetChildDirectories(IResourceAccessor directoryAccessor)
    {
      if (directoryAccessor is IFileSystemResourceAccessor)
      {
        IFileSystemResourceAccessor fsra = (IFileSystemResourceAccessor) directoryAccessor;
        ICollection<IFileSystemResourceAccessor> childDirectories = fsra.GetChildDirectories();
        ICollection<IFileSystemResourceAccessor> result = childDirectories == null ?
            new List<IFileSystemResourceAccessor>() :
            new List<IFileSystemResourceAccessor>(childDirectories);
        ICollection<IFileSystemResourceAccessor> files = fsra.GetFiles();
        if (files != null)
          foreach (IFileSystemResourceAccessor fileAccessor in files)
          {
            IChainedMediaProvider provider;
            if (CanBeUnfolded(fileAccessor, out provider))
            {
              IResourceAccessor ra = provider.CreateResourceAccessor(fileAccessor, "/");
              if (ra is IFileSystemResourceAccessor)
                result.Add((IFileSystemResourceAccessor) ra);
              else
                ra.Dispose();
            }
          }
        return result;
      }
      else
      { // Try to unfold simple resource
        IChainedMediaProvider provider;
        if (CanBeUnfolded(directoryAccessor, out provider))
        {
          IResourceAccessor ra = provider.CreateResourceAccessor(directoryAccessor, "/");
          if (ra is IFileSystemResourceAccessor)
            return new List<IFileSystemResourceAccessor>(new IFileSystemResourceAccessor[] {(IFileSystemResourceAccessor) ra});
          else
            ra.Dispose();
        }
      }
      return null;
    }

    /// <summary>
    /// Returns all files in the given directory.
    /// </summary>
    /// <remarks>
    /// This method simply returns 
    /// </remarks>
    /// <param name="directoryAccessor">Directory resource accessor to get all files for.</param>
    /// <returns>Collection of accessors for all files or <c>null</c>,
    /// if the given <paramref name="directoryAccessor"/> is not a <see cref="IFileSystemResourceAccessor"/>.</returns>
    public static ICollection<IFileSystemResourceAccessor> GetFiles(IResourceAccessor directoryAccessor)
    {
      if (directoryAccessor is IFileSystemResourceAccessor)
        return ((IFileSystemResourceAccessor) directoryAccessor).GetFiles();
      return null;
    }

    /// <summary>
    /// Returns the information if the given <paramref name="fileAccessor"/> can be unfolded as a virtual directory.
    /// </summary>
    /// <param name="fileAccessor">File resource accessor to be used as input for a potential chained
    /// provider.</param>
    /// <param name="provider">Chained media provider which can chain upon the given file resource.</param>
    public static bool CanBeUnfolded(IResourceAccessor fileAccessor, out IChainedMediaProvider provider)
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      foreach (IChainedMediaProvider cmp in mediaAccessor.LocalChainedMediaProviders)
        if (cmp.CanChainUp(fileAccessor))
        {
          provider = cmp;
          return true;
        }
      provider = null;
      return false;
    }
  }
}
