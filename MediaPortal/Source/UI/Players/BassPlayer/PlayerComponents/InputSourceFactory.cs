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
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.Extensions.ResourceProviders.AudioCDResourceProvider;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.UI.Players.BassPlayer.InputSources;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Utils;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Creates inputsource objects.
  /// </summary>
  public class InputSourceFactory : IDisposable
  {
    protected IResourceAccessor _accessor;

    #region IDisposable Members

    public void Dispose()
    {
      if (_accessor != null)
        _accessor.Dispose();
    }

    #endregion

    #region Public members

    /// <summary>
    /// Creates an <see cref="IInputSource"/> object for a given mediaitem.
    /// </summary>
    /// <param name="resourceLocator">Locator instance to the media item to create the input source for.</param>
    /// <param name="mimeType">Mime type of the media item, if present. May be <c>null</c>.</param>
    /// <returns>Input source object for the given <paramref name="resourceLocator"/> or <c>null</c>, if no input source
    /// could be created.</returns>
    public IInputSource CreateInputSource(IResourceLocator resourceLocator, string mimeType)
    {
      if (!CanPlay(resourceLocator, mimeType))
        return null;
      IInputSource result;
      _accessor = resourceLocator.CreateAccessor();

      AudioCDResourceAccessor acdra = _accessor as AudioCDResourceAccessor;
      if (acdra != null)
        result = BassCDTrackInputSource.Create(acdra.Drive, acdra.TrackNo);
      else
      {
        string filePath = _accessor.ResourcePathName;
        // Network streams
        INetworkResourceAccessor netra = _accessor as INetworkResourceAccessor;
        if (netra != null)
        {
          result = BassWebStreamInputSource.Create(netra.URL);
        }
        // CDDA
        else if (URLUtils.IsCDDA(filePath))
        {
          ILocalFsResourceAccessor lfra = _accessor as ILocalFsResourceAccessor;
          if (lfra == null)
            return null;
          using (lfra.EnsureLocalFileSystemAccess())
            result = BassFsCDTrackInputSource.Create(lfra.LocalFileSystemPath);
        }
        else
        {
          // Filesystem resources
          IFileSystemResourceAccessor fsra = _accessor as IFileSystemResourceAccessor;
          if (fsra == null)
            return null;
          if (URLUtils.IsMODFile(filePath))
            result = BassMODFileInputSource.Create(fsra);
          else
            result = BassAudioFileInputSource.Create(fsra);
        }
      }
      Log.Debug("InputSourceFactory: Creating input source for media resource '{0}' of type '{1}'", _accessor, result.GetType());
      return result;
    }

    public static bool CanPlay(IResourceLocator locator, string mimeType)
    {
      // First check the Mime Type
      if (!string.IsNullOrEmpty(mimeType) && !mimeType.StartsWith("audio"))
        return false;

      using (IResourceAccessor accessor = locator.CreateAccessor())
      {
        if (accessor is VirtualResourceAccessor)
          return false;
        if (accessor is AudioCDResourceAccessor || accessor is INetworkResourceAccessor)
          return true;
        string ext = DosPathHelper.GetExtension(accessor.ResourcePathName).ToLowerInvariant();
        BassPlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<BassPlayerSettings>();
        return settings.SupportedExtensions.IndexOf(ext) > -1;
      }
    }

    #endregion
  }
}
