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

using System.Collections.Generic;

namespace MediaPortal.Common.ResourceAccess
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
    /// the virtual child directories are obtained by taking the root directories of each chained resource provider applied
    /// to the child files of the given directory.
    /// If, for example, the given <paramref name="directoryAccessor"/> contains a child directory "A" and a child
    /// archive file "B" which can work as input for an installed archive provider, providing the root directory "C"
    /// of that archive, this method will return the resource accessors for directories "A" and "C".
    /// </remarks>
    /// <param name="directoryAccessor">Directory resource accessor to get all child directories for.</param>
    /// <returns>Collection of directory accessors for all native and virtual child directories or <c>null</c>,
    /// if the given <paramref name="directoryAccessor"/> is not a <see cref="IFileSystemResourceAccessor"/> and
    /// if there is no chained resource provider to unfold the given directory.</returns>
    public static ICollection<IFileSystemResourceAccessor> GetChildDirectories(IResourceAccessor directoryAccessor)
    {
      IResourceAccessor chainedResourceAccesor; // Needed in multiple source locations, that's why we declare it here
      if (directoryAccessor is IFileSystemResourceAccessor)
      {
        IFileSystemResourceAccessor dirFsra = (IFileSystemResourceAccessor) directoryAccessor;
        ICollection<IFileSystemResourceAccessor> childDirectories = dirFsra.GetChildDirectories();
        ICollection<IFileSystemResourceAccessor> result = childDirectories == null ?
            new List<IFileSystemResourceAccessor>() : new List<IFileSystemResourceAccessor>(childDirectories);
        ICollection<IFileSystemResourceAccessor> files = dirFsra.GetFiles();
        if (files != null)
          foreach (IFileSystemResourceAccessor fileAccessor in files)
          {
            if (TryUnfold(fileAccessor, out chainedResourceAccesor))
            {
              IFileSystemResourceAccessor chainedFsra = chainedResourceAccesor as IFileSystemResourceAccessor;
              if (chainedFsra != null)
                result.Add(chainedFsra);
              else
                chainedResourceAccesor.Dispose();
            }
            else
              fileAccessor.Dispose();
          }
        return result;
      }
      // Try to unfold simple resource
      IResourceAccessor dra = directoryAccessor.Clone();
      try
      {
        if (TryUnfold(dra, out chainedResourceAccesor))
        {
          IFileSystemResourceAccessor chainedFsra = chainedResourceAccesor as IFileSystemResourceAccessor;
          if (chainedFsra != null)
            return new List<IFileSystemResourceAccessor>(new IFileSystemResourceAccessor[] {chainedFsra});
          chainedResourceAccesor.Dispose();
        }
        else
          dra.Dispose();
      }
      catch
      {
        dra.Dispose();
        throw;
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
    /// Tries to unfold the given <paramref name="fileAccessor"/> to a virtual directory.
    /// </summary>
    /// <param name="fileAccessor">File resource accessor to be used as input for a potential chained provider.</param>
    /// <param name="resultResourceAccessor">Chained resource accessor which was chained upon the given file resource.</param>
    public static bool TryUnfold(IResourceAccessor fileAccessor, out IResourceAccessor resultResourceAccessor)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IChainedResourceProvider cmp in mediaAccessor.LocalChainedResourceProviders)
        if (cmp.TryChainUp(fileAccessor, "/", out resultResourceAccessor))
        {
          resultResourceAccessor.Dispose();
          return true;
        }
      resultResourceAccessor = null;
      return false;
    }
  }
}