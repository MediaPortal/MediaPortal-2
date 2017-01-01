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
    /// <param name="showSystemResources">If set to <c>true</c>, system resources like the virtual drives and directories of the
    /// <see cref="IResourceMountingService"/> will also be returned, else removed from the result value.</param>
    /// <returns>Collection of directory accessors for all native and virtual child directories or <c>null</c>,
    /// if the given <paramref name="directoryAccessor"/> does not point to a directory and
    /// if there is no chained resource provider to unfold the given file.</returns>
    public static ICollection<IFileSystemResourceAccessor> GetChildDirectories(IFileSystemResourceAccessor directoryAccessor, bool showSystemResources)
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      IFileSystemResourceAccessor chainedResourceAccesor; // Needed in multiple source locations, that's why we declare it here
      
      // If directoryAccessor points to a directory, we return all actual subdirectories and the virtual subdirectories
      // we can create by using all available ChainedResourceProviders on all contained files.
      if (!directoryAccessor.IsFile)
      {
        ICollection<IFileSystemResourceAccessor> childDirectories = directoryAccessor.GetChildDirectories();
        ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
        if (childDirectories != null)
          // Directories are maybe filtered and then just added
          foreach (IFileSystemResourceAccessor childDirectoryAccessor in childDirectories)
          {
            if (!showSystemResources && resourceMountingService.IsVirtualResource(childDirectoryAccessor.CanonicalLocalResourcePath))
            {
              childDirectoryAccessor.Dispose();
              continue;
            }
            result.Add(childDirectoryAccessor);
          }
        ICollection<IFileSystemResourceAccessor> files = directoryAccessor.GetFiles();
        if (files != null)
          // For files, we try to chain up chained resource providers
          foreach (IFileSystemResourceAccessor fileAccessor in files)
            using (fileAccessor)
            {
              if (!showSystemResources && resourceMountingService.IsVirtualResource(fileAccessor.CanonicalLocalResourcePath))
                continue;
              if (TryUnfold(fileAccessor, out chainedResourceAccesor))
                if (!chainedResourceAccesor.IsFile)
                  result.Add(chainedResourceAccesor);
                else
                  chainedResourceAccesor.Dispose();
              // Simply ignore files because we only want to return directories
            }
        return result;
      }
      
      // Try to unfold file resource
      if (TryUnfold(directoryAccessor, out chainedResourceAccesor))
      {
        if (!chainedResourceAccesor.IsFile)
          return new List<IFileSystemResourceAccessor>(new IFileSystemResourceAccessor[] {chainedResourceAccesor});
        chainedResourceAccesor.Dispose();
        return new List<IFileSystemResourceAccessor>();
      }
      return null;
    }

    /// <summary>
    /// Returns all files in the given directory.
    /// </summary>
    /// <remarks>
    /// This method simply returns the files files of the given <paramref name="directoryAccessor"/>, filtered
    /// if <paramref name="showSystemResources"/> is set to <c>true</c>.
    /// </remarks>
    /// <param name="directoryAccessor">Directory resource accessor to get all files for.</param>
    /// <param name="showSystemResources">If set to <c>true</c>, system resources like the virtual drives and directories of the
    /// <see cref="IResourceMountingService"/> will also be returned, else removed from the result value.</param>
    /// <returns>Collection of accessors for all files or <c>null</c>,
    /// if the given <paramref name="directoryAccessor"/> is not a <see cref="IFileSystemResourceAccessor"/>.</returns>
    public static ICollection<IFileSystemResourceAccessor> GetFiles(IFileSystemResourceAccessor directoryAccessor, bool showSystemResources)
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
      foreach (IFileSystemResourceAccessor fileAccessor in directoryAccessor.GetFiles())
      {
        if (!showSystemResources && resourceMountingService.IsVirtualResource(fileAccessor.CanonicalLocalResourcePath))
        {
          fileAccessor.Dispose();
          continue;
        }
        result.Add(fileAccessor);
      }
      return result;
    }

    /// <summary>
    /// Tries to unfold the given <paramref name="fileAccessor"/> to a virtual directory.
    /// </summary>
    /// <param name="fileAccessor">File resource accessor to be used as input for a potential chained provider.
    /// The ownership of this resource accessor remains at the caller.</param>
    /// <param name="resultResourceAccessor">Chained resource accessor which was chained upon the given file resource.</param>
    public static bool TryUnfold(IFileSystemResourceAccessor fileAccessor, out IFileSystemResourceAccessor resultResourceAccessor)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IChainedResourceProvider cmp in mediaAccessor.LocalChainedResourceProviders)
        if (cmp.TryChainUp(fileAccessor, "/", out resultResourceAccessor))
          return true;
      resultResourceAccessor = null;
      return false;
    }
  }
}