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

namespace MediaPortal.Common.ResourceAccess
{
  public interface ITidyUpExecutor
  {
    void Execute();
  }

  /// <summary>
  /// Temporary local accessor instance for a resource which might located anywhere in an MP2 system or in the outside world.
  /// </summary>
  /// <remarks>
  /// Via this instance, the specified resource and/or its metadata can be read.
  /// To obtain a resource accessor, get an <see cref="IResourceLocator"/> and use its <see cref="IResourceLocator.CreateAccessor"/> method.
  /// The resource accessor must be disposed using its <see cref="IDisposable.Dispose"/> method when it is not needed any more.
  /// This will clean up resources which were allocated to access the resource.
  /// </remarks>
  public interface IResourceAccessor : IDisposable
  {
    /// <summary>
    /// Returns the resource provider which provides this resource, if available. If this resource accessor is not hosted
    /// by a resource provider, this property returns <c>null</c>.
    /// </summary>
    IResourceProvider ParentProvider { get; }

    /// <summary>
    /// Returns the resource provider path of this resource accessor.
    /// </summary>
    /// <value>A technical path which is interpreted by this resource accessor and by the <see cref="ParentProvider"/>.</value>
    string Path { get; }

    /// <summary>
    /// Returns a short, human readable name for this resource.
    /// </summary>
    /// <value>A human readable name of this resource. For a filesystem resource accessor,
    /// this could be the file name or directory name, for example.</value>
    string ResourceName { get; }

    /// <summary>
    /// Returns the full human readable path name for this resource.
    /// </summary>
    /// <remarks>
    /// This path does not always point to a local directory! Don't use it as directory in the file system. To obtain a local
    /// filesystem path, you need an <see cref="ILocalFsResourceAccessor"/> and use its <see cref="ILocalFsResourceAccessor.LocalFileSystemPath"/>
    /// property.
    /// </remarks>
    /// <value>A human readable name of this resource. For a filesystem resource accessor,
    /// this could be the file path, for example.</value>
    string ResourcePathName { get; }

    /// <summary>
    /// Returns the technical resource path which points to this resource.
    /// </summary>
    /// <remarks>
    /// This property always returns the path to the original resource, i.e. without any translation to local filesystem resource accessors etc.
    /// </remarks>
    ResourcePath CanonicalLocalResourcePath { get; }

    /// <summary>
    /// Doubles this instance.
    /// </summary>
    /// <returns>New resource accessor which points to the same resource as this resource accessor.</returns>
    IResourceAccessor Clone();
  }
}