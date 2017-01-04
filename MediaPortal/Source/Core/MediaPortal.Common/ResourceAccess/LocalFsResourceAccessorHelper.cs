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
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Helper class to ensure correct creation and disposal of <see cref="ILocalFsResourceAccessor"/>. This class especially works in combination with
  /// the <see cref="StreamedResourceToLocalFsAccessBridge"/> that requires disposal of accessor only if they are not <see cref="ILocalFsResourceAccessor"/>.
  /// </summary>
  public class LocalFsResourceAccessorHelper : IDisposable
  {
    #region Fields

    private readonly ILocalFsResourceAccessor _disposeLfsra;
    private readonly ILocalFsResourceAccessor _localFsra;

    #endregion

    /// <summary>
    /// Gets the <see cref="ILocalFsResourceAccessor"/> to be used for resource access.
    /// </summary>
    public ILocalFsResourceAccessor LocalFsResourceAccessor
    {
      get
      {
        return _localFsra;
      }
    }

    /// <summary>
    /// Creates a new <see cref="LocalFsResourceAccessor"/> instance. The given <paramref name="mediaItemAccessor"/> will be either directly used or
    /// given over to the <see cref="StreamedResourceToLocalFsAccessBridge"/>. The caller must call <see cref="Dispose"/> on the created <see cref="LocalFsResourceAccessorHelper"/>
    /// instance but must not dispose the given <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <param name="mediaItemAccessor">IResourceAccessor.</param>
    public LocalFsResourceAccessorHelper(IResourceAccessor mediaItemAccessor)
    {
      _localFsra = mediaItemAccessor as ILocalFsResourceAccessor;
      _disposeLfsra = null;
      if (_localFsra != null)
        return;

      IFileSystemResourceAccessor localFsra = (IFileSystemResourceAccessor) mediaItemAccessor.Clone();
      try
      {
        _localFsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(localFsra);
        _disposeLfsra = _localFsra;
      }
      catch (Exception)
      {
        localFsra.Dispose();
        throw;
      }
    }

    public void Dispose()
    {
      if (_disposeLfsra != null)
        _disposeLfsra.Dispose();
    }
  }
}