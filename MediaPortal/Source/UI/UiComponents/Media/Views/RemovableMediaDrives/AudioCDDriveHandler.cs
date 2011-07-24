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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UiComponents.Media.Models.MediaItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  public class AudioCDDriveHandler : BaseDriveHandler
  {
    #region Consts

    protected static IList<MediaItem> EMPTY_MEDIA_ITEM_LIST = new List<MediaItem>(0);

    #endregion

    #region Protected fields

    protected StaticViewSpecification _audioCDSubViewSpecification;

    #endregion

    protected AudioCDDriveHandler(DriveInfo driveInfo, IEnumerable<MediaItem> tracks,
        IEnumerable<Guid> extractedMIATypeIds) : base(driveInfo)
    {
      _audioCDSubViewSpecification = new StaticViewSpecification(driveInfo.VolumeLabel, extractedMIATypeIds, new Guid[] {});
      foreach (MediaItem track in tracks)
        _audioCDSubViewSpecification.AddMediaItem(track);
    }

    /// <summary>
    /// Creates an <see cref="AudioCDDriveHandler"/> if the drive of the given <paramref name="driveInfo"/> contains an audio CD/DVD/BD.
    /// </summary>
    /// <param name="driveInfo">Drive info object for the drive to examine.</param>
    /// <param name="extractedMIATypeIds">Media item aspect types to be extracted from the audio items. The given MIAs will be present
    /// in the created instance's media items.</param>
    /// <returns><see cref="AudioCDDriveHandler"/> instance for the audio CD/DVD/BD or <c>null</c>, if the given drive doesn't contain
    /// an audio media.</returns>
    public static AudioCDDriveHandler TryCreateAudioDriveHandler(DriveInfo driveInfo,
        IEnumerable<Guid> extractedMIATypeIds)
    {
      ICollection<MediaItem> tracks;
      return DetectAudioCD(driveInfo.Name, out tracks) ? new AudioCDDriveHandler(driveInfo, tracks, extractedMIATypeIds) : null;
    }

    /// <summary>
    /// Detects if an audio CD/DVD/BD is contained in the given <paramref name="drive"/>.
    /// </summary>
    /// <param name="drive">The drive to be examined.</param>
    /// <param name="tracks">Returns a collection of audio tracks for the audio CD in the given <paramref name="drive"/>.</param>
    /// <returns><c>true</c>, if an audio CD was identified, else <c>false</c>.</returns>
    public static bool DetectAudioCD(string drive, out ICollection<MediaItem> tracks)
    {
      tracks = null;
      if (string.IsNullOrEmpty(drive) || drive.Length < 2)
        return false;
      drive = drive.Substring(0, 2); // Clip potential '\\' at the end

      int numTracks = BassUtils.GetNumTracks(drive);
      if (numTracks <= 0)
        return false;
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      string systemId = systemResolver.LocalSystemId;
      IList<MediaItem> resultTracks = new List<MediaItem>(numTracks);
      int track = 1;
      // FIXME Albert, 2011-07-24:
      // Directory.GetFiles and DirectoryInfo.GetFiles() crash sometimes (using .net 3.5). I don't know why. -> To be fixed.
      foreach (string file in new DirectoryInfo(drive).GetFiles().Select(fileInfo => fileInfo.FullName))
        resultTracks.Add(CreateMediaItem(file, track++, numTracks, systemId));
      tracks = resultTracks;
      return true;
    }

    protected static MediaItem CreateMediaItem(string file, int track, int numTracks, string systemId)
    {
      IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();
      // TODO: The creation of new media item aspects could be moved to a general method
      MediaItemAspect providerResourceAspect;
      aspects[ProviderResourceAspect.ASPECT_ID] = providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
      MediaItemAspect mediaAspect;
      aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
      MediaItemAspect audioAspect;
      aspects[AudioAspect.ASPECT_ID] = audioAspect = new MediaItemAspect(AudioAspect.Metadata);
      MediaItemAspect specialSortAspect;
      aspects[SpecialSortAspect.ASPECT_ID] = specialSortAspect = new MediaItemAspect(SpecialSortAspect.Metadata);

      // TODO: Collect data from internet for the current audio CD
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
          LocalFsMediaProviderBase.ToProviderResourcePath(file).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemId);
      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, "Track " + track);
      audioAspect.SetAttribute(AudioAspect.ATTR_TRACK, track);
      audioAspect.SetAttribute(AudioAspect.ATTR_NUMTRACKS, numTracks);
      specialSortAspect.SetAttribute(SpecialSortAspect.ATTR_SORT_STRING, StringUtils.Pad(track.ToString(), 5, '0', true));

      return new MediaItem(Guid.Empty, aspects);
    }

    #region IRemovableDriveHandler implementation

    public override IList<MediaItem> MediaItems
    {
      get { return EMPTY_MEDIA_ITEM_LIST; }
    }

    public override IList<ViewSpecification> SubViewSpecifications
    {
      get { return new List<ViewSpecification> {_audioCDSubViewSpecification}; }
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      return _audioCDSubViewSpecification.GetAllMediaItems();
    }

    #endregion
  }
}