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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.Extensions.ResourceProviders.AudioCDResourceProvider;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Drive handler for audio BDs, DVDs and CDs. Creates one single static sub view specification which contains all the audio
  /// items from the media.
  /// </summary>
  public class AudioCDDriveHandler : BaseDriveHandler
  {
    #region Consts

    protected static IList<MediaItem> EMPTY_MEDIA_ITEM_LIST = new List<MediaItem>(0);

    #endregion

    #region Protected fields

    protected StaticViewSpecification _audioCDSubViewSpecification;

    #endregion

    protected AudioCDDriveHandler(DriveInfo driveInfo, IEnumerable<MediaItem> tracks) : base(driveInfo)
    {
      string volumeLabel;
      try
      {
        volumeLabel = driveInfo.VolumeLabel;
      }
      catch (Exception)
      {
        volumeLabel = "Audio CD";
      }
      _audioCDSubViewSpecification = new StaticViewSpecification(
          volumeLabel + " (" + DriveUtils.GetDriveNameWithoutRootDirectory(driveInfo) + ")", new Guid[] {}, new Guid[] {});
      foreach (MediaItem track in tracks)
        _audioCDSubViewSpecification.AddMediaItem(track);
    }

    /// <summary>
    /// Creates an <see cref="AudioCDDriveHandler"/> if the drive of the given <paramref name="driveInfo"/> contains an audio CD/DVD/BD.
    /// </summary>
    /// <param name="driveInfo">Drive info object for the drive to examine.</param>
    /// <returns><see cref="AudioCDDriveHandler"/> instance for the audio CD/DVD/BD or <c>null</c>, if the given drive doesn't contain
    /// an audio media.</returns>
    public static AudioCDDriveHandler TryCreateAudioCDDriveHandler(DriveInfo driveInfo)
    {
      ICollection<MediaItem> tracks;
      ICollection<Guid> extractedMIATypeIDs;
      return DetectAudioCD(driveInfo.Name, out tracks, out extractedMIATypeIDs) ? new AudioCDDriveHandler(driveInfo, tracks) : null;
    }

    /// <summary>
    /// Detects if an audio CD/DVD/BD is contained in the given <paramref name="drive"/>.
    /// </summary>
    /// <param name="drive">The drive to be examined.</param>
    /// <param name="tracks">Returns a collection of audio tracks for the audio CD in the given <paramref name="drive"/>.</param>
    /// <param name="extractedMIATypeIDs">IDs of the media item aspect types which were extracted from the returned <paramref name="tracks"/>.</param>
    /// <returns><c>true</c>, if an audio CD was identified, else <c>false</c>.</returns>
    public static bool DetectAudioCD(string drive, out ICollection<MediaItem> tracks, out ICollection<Guid> extractedMIATypeIDs)
    {
      tracks = null;
      extractedMIATypeIDs = null;
      if (string.IsNullOrEmpty(drive) || drive.Length < 2)
        return false;
      drive = drive.Substring(0, 2); // Clip potential '\\' at the end

      try
      {
        IList<BassUtils.AudioTrack> audioTracks = BassUtils.GetAudioTracks(drive);
        if (audioTracks == null || audioTracks.Count == 0)
          return false;
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        string systemId = systemResolver.LocalSystemId;
        tracks = new List<MediaItem>(audioTracks.Count);
        char driveChar = drive[0];
        foreach (BassUtils.AudioTrack track in audioTracks)
          tracks.Add(CreateMediaItem(track, driveChar, audioTracks.Count, systemId));
        extractedMIATypeIDs = new List<Guid>
          {
              ProviderResourceAspect.ASPECT_ID,
              MediaAspect.ASPECT_ID,
              AudioAspect.ASPECT_ID,
          };
      }
      catch (IOException)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error enumerating tracks of audio CD in drive {0}", drive);
        tracks = null;
        return false;
      }
      return true;
    }

    protected static MediaItem CreateMediaItem(BassUtils.AudioTrack track, char drive, int numTracks, string systemId)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, true);
      MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(aspects, MediaAspect.Metadata);
      MediaItemAspect audioAspect = MediaItemAspect.GetOrCreateAspect(aspects, AudioAspect.Metadata);

      // TODO: Collect data from internet for the current audio CD
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
          AudioCDResourceProvider.ToResourcePath(drive, track.TrackNo).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemId);
      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, "Track " + track.TrackNo);
      audioAspect.SetAttribute(AudioAspect.ATTR_TRACK, (int) track.TrackNo);
      audioAspect.SetAttribute(AudioAspect.ATTR_DURATION, (long) track.Duration);
      audioAspect.SetAttribute(AudioAspect.ATTR_ENCODING, "PCM");
      audioAspect.SetAttribute(AudioAspect.ATTR_BITRATE, 1411200); // 44.1 kHz * 16 bit * 2 channel
      audioAspect.SetAttribute(AudioAspect.ATTR_NUMTRACKS, numTracks);

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
