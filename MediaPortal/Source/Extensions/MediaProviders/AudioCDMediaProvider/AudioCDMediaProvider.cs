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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.Extensions.MediaProviders.AudioCDMediaProvider
{
  /// <summary>
  /// Media provider implementation providing resource accessor for audio CD tracks.
  /// </summary>
  public class AudioCDMediaProvider : IBaseMediaProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the audio CD media provider.
    /// </summary>
    protected const string AUDIO_CD_MEDIA_PROVIDER_ID_STR = "{EB6EB821-25B9-492E-B9FD-1E8B724F8945}";

    /// <summary>
    /// Audio CD media provider GUID.
    /// </summary>
    public static Guid AUDIO_CD_MEDIA_PROVIDER_ID = new Guid(AUDIO_CD_MEDIA_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected MediaProviderMetadata _metadata;

    #endregion

    #region Ctor

    public AudioCDMediaProvider()
    {
      _metadata = new MediaProviderMetadata(AUDIO_CD_MEDIA_PROVIDER_ID, "[AudioCDMediaProvider.Name]");
    }

    public bool TryExtract(string path, out char drive, out int trackNo)
    {
      trackNo = 0;
      drive = (char) 0;
      if (string.IsNullOrEmpty(path))
        return false;
      if (path.Length < 4 || path[0] != '/' || path[2] != '/')
        return false;
      path = path.ToUpper();
      drive = path[1];
      if (drive < 'C' || drive > 'Z')
        return false;
      return int.TryParse(path.Substring(3), out trackNo);
    }

    public static string BuildPath(char drive, int trackNo)
    {
      return "/" + drive + "/" + trackNo;
    }

    #endregion

    #region IBaseMediaProvider implementation

    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      char drive;
      int trackNo;
      if (!TryExtract(path, out drive, out trackNo))
        return false;
      return BassUtils.GetNumAudioTracks(drive + ":") > trackNo;
    }

    public IResourceAccessor CreateMediaItemAccessor(string path)
    {
      char drive;
      int trackNo;
      if (!TryExtract(path, out drive, out trackNo))
        throw new ArgumentException(string.Format("Path '{0}' is not valid in the {1}", path, GetType().Name));
      return new AudioCDResourceAccessor(this, drive, trackNo);
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;
      // The input string is given by the user. We can cope with three formats:
      // 1) A media provider path of the form "/1"
      // 2) The track number
      // 3) A resource path in the resource path syntax (i.e. {[Provider-Id]}:///1)
      if (IsResource(pathStr))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(AUDIO_CD_MEDIA_PROVIDER_ID, pathStr, true), 
          });
      string modifiedPath = "/" + pathStr;
      if (IsResource(modifiedPath))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(AUDIO_CD_MEDIA_PROVIDER_ID, modifiedPath, true), 
          });
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    #endregion
  }
}
