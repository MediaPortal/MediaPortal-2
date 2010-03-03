#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using MediaPortal.Core.MediaManagement;
using Ui.Players.BassPlayer.InputSources;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;

namespace Ui.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Creates inputsource objects.
  /// </summary>
  public class InputSourceFactory : IDisposable
  {
    #region IDisposable Members

    public void Dispose()
    {
      // Maybe needed in the future?
    }

    #endregion

    #region Public members

    /// <summary>
    /// Creates an IInputSource object for a given mediaitem.
    /// </summary>
    /// <param name="resourceLocator">Locator instance to the media item to create the input source for.</param>
    /// <returns>Input source object for the given <paramref name="resourceLocator"/> or <c>null</c>, if no input source
    /// could be created.</returns>
    public IInputSource CreateInputSource(IResourceLocator resourceLocator)
    {
      IResourceAccessor accessor = resourceLocator.CreateAccessor();
      string filePath = accessor.ResourcePathName;
      IInputSource result;
      if (URLUtils.IsCDDA(filePath))
      {
        ILocalFsResourceAccessor lfra = accessor as ILocalFsResourceAccessor;
        if (lfra == null)
          return null;
        result = BassCDTrackInputSource.Create(lfra.LocalFileSystemPath);
      }
      else if (URLUtils.IsMODFile(filePath))
        result = BassMODFileInputSource.Create(accessor);
      else
        result = BassAudioFileInputSource.Create(accessor);
      // TODO: We could cope with web streams if resource locators would be able to hold web URLs: BassWebStreamInputSource.Create(...);

      Log.Debug("InputSourceFactory: Creating input source for media resource '{0}' of type '{1}'", accessor, result.GetType());
      return result;
    }

    #endregion
  }
}
