#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Database;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MediaManagement.Views;
using MediaPortal.Utilities;
using Media.Importers.MusicImporter.Freedb;
using Media.Importers.MusicImporter.Tags;

namespace Media.Importers.MusicImporter
{
  public class MusicImporter : IPluginStateTracker, IImporter
  {
    #region Variables

    private IDatabase _musicDatabase = null;
    private readonly string[] ArtistNamePrefixes = new string[]
      {
        "the",
        "les",
        "die"
      };

    DateTime _startTime;
    private int _processedFiles = 0;
    private IList<IDbItem> _tracks;

    #endregion

    #region Enums

    public enum Errors
    {
      ERROR_OK = 317,
      ERROR_CANCEL = 0,
      ERROR_DATABASE = 315,
      ERROR_REORG_SONGS = 319,
      ERROR_REORG_ARTIST = 321,
      ERROR_REORG_ALBUMARTIST = 322,
      ERROR_REORG_GENRE = 323,
      ERROR_REORG_PATH = 325,
      ERROR_REORG_ALBUM = 327,
      ERROR_WRITING_CHANGES = 329,
      ERROR_COMPRESSING = 332
    }

    #endregion

    public MusicImporter() { }

    #region IPluginStateTracker implementation

    public void Activated()
    {
      CreateMusicDatabase();
    }

    public bool RequestEnd()
    {
      return false; // FIXME: The importer plugin should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    #region IImporter Members

    public void BeforeImport(int avAilableFiles)
    {
      _startTime = DateTime.Now;
      _processedFiles = 0;
      _tracks = new List<IDbItem>();

      ServiceScope.Get<ILogger>().Info("MusicImporter: Processing {0} songs", avAilableFiles);
      DeleteNonExistingSongs();
    }

    public void AfterImport()
    {
      DateTime stopTime = DateTime.Now;
      TimeSpan ts = stopTime - _startTime;
      float fSecsPerTrack = ((float)ts.TotalSeconds / _processedFiles);
      string trackPerSecSummary = "";
      _musicDatabase.Save(_tracks);

      if (_processedFiles > 0)
        trackPerSecSummary = string.Format(" ({0} seconds per track)", fSecsPerTrack);
      ServiceScope.Get<ILogger>().Info("MusicImporter: Music database reorganization done. Processed {0} tracks in: {1:d2}:{2:d2}:{3:d2}{4}", _processedFiles, ts.Hours, ts.Minutes, ts.Seconds, trackPerSecSummary);
    }

    public bool FileImport(string filePath)
    {
      IDbItem track;
      if (!SongExists(filePath))
      {
        //The song does not exist, we will add it.
        track = AddSong(filePath);
      }
      else
      {
        track = UpdateSong(filePath);
      }

      if (track != null)
      {
        _tracks.Add(track);
        _processedFiles++;
        if (_processedFiles % 1000 == 0)
        {
          ServiceScope.Get<ILogger>().Info("MusicImporter: {0} songs processed so far", _processedFiles);
          _musicDatabase.Save(_tracks);
          _tracks.Clear();
        }
        return true;
      }
      return false;
    }

    public void FileDeleted(string filePath)
    {
      ServiceScope.Get<ILogger>().Info("MusicImporter: Song deleted {0}", filePath);

      DeleteSong(filePath);
    }

    public void FileCreated(string filePath)
    {
      try
      {
        ServiceScope.Get<ILogger>().Info("MusicImporter: Song Created {0}", filePath);
        //string ext = Path.GetExtension(file).ToLower();
        //if (Extensions.Contains(ext))
        //{
        // Has the song already be added? 
        // This happens when a full directory is copied into the share.
        if (SongExists(filePath))
          return;
        // For some reason the Create is fired already by windows while the file is still copied.
        // This happens especially on large songs copied via WLAN.
        // The result is that Tagreader is throwing an IO Exception.
        // I'm trying to open the file here. In case of an exception the file is processed by the Change Event.
        try
        {
          FileInfo fileInfo = new FileInfo(filePath);
          Stream s = null;
          s = fileInfo.OpenRead();
          s.Close();
        }
        catch (Exception)
        {
          // The file is not closed yet. Ignore the event, it will be processed by the Change event
          return;
        }
        IDbItem track = AddSong(filePath);
        if (track != null) track.Save();
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error FileCreated:{0}", filePath);
        ServiceScope.Get<ILogger>().Error(ex);
      }

    }

    public void FileChanged(string filePath)
    {
      try
      {
        ServiceScope.Get<ILogger>().Info("MusicImporter: Song Changed {0}", filePath);
        //string ext = Path.GetExtension(file).ToLower();
        //if (Extensions.Contains(ext))
        //{
        if (SongExists(filePath))
        {
          IDbItem track = UpdateSong(filePath);
          if (track != null) track.Save();
        }
        else
        {
          IDbItem track = AddSong(filePath);
          if (track != null) track.Save();
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error FileChanged:{0}", filePath);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    public void FileRenamed(string filePath, string oldFilePath)
    {
      try
      {
        ServiceScope.Get<ILogger>().Info("MusicImporter: Song / Directory Renamed {0} to {1}", filePath, oldFilePath);

        // The rename may have been on a directory or a file
        FileInfo fi = new FileInfo(filePath);
        if (fi.Exists)
        {
          IList<IDbItem> result;
          try
          {
            Query trackByFilename = new Query("contenturi", Operator.Same, oldFilePath);
            result = _musicDatabase.Query(trackByFilename);
            if (result.Count > 0)
            {

              IDbItem track = result[0];
              track["contenturi"] = filePath;
              track.Save();
            }
          }
          catch (Exception)
          {
            return;
          }
        }
        else
        {
          // Must be a directory, so let's change the path entries, containing the old
          // name with the new name
          DirectoryInfo di = new DirectoryInfo(filePath);
          if (di.Exists)
          {
            IList<IDbItem> result;
            try
            {
              Query trackByFilename = new Query("contenturi", Operator.Like, String.Format("{0}%", oldFilePath));
              result = _musicDatabase.Query(trackByFilename);
              if (result.Count > 0)
              {
                // We might have changed a Top directory, so we get a lot of path entries returned
                for (int i = 0; i < result.Count; i++)
                {
                  IDbItem track = result[i];
                  string strPath = track["contenturi"].ToString().Replace(oldFilePath, filePath);
                  track["contenturi"] = strPath;
                  track.Save();
                }
              }
            }
            catch (Exception)
            {
              return;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("Musicimporter: Error FileRenamed: {0}", filePath);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    public void DirectoryDeleted(string directory)
    {
      ServiceScope.Get<ILogger>().Info("MusicImporter: Song Folder deleted {0}", directory);
      try
      {
        Query songByFilename = new Query("contentURI", Operator.Like, String.Format("{0}%", directory));
        IList<IDbItem> result = _musicDatabase.Query(songByFilename);
        if (result.Count > 0)
        {
          foreach (IDbItem item in result)
          {
            item.Delete();
          }
        }
      }
      catch (Exception)
      {
        return;
      }
    }

    public void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items)
    {
      try
      {
        // Is the given Folder a Redbook Audio CD? Then we need to query FreeDB
        if (folder.Length < 4)
        {
          if (CdUtils.isARedBookCD(folder))
          {
            QueryFreeDB(folder, items);
            return;
          }
        }

        bool foundCueFile = false;
        List<IAbstractMediaItem> cueFileItems = new List<IAbstractMediaItem>();
        List<string> cueFiles = new List<string>();

        Query songsByPath = new Query("path", Operator.Same, folder);
        IList<IDbItem> results = _musicDatabase.Query(songsByPath);
        foreach (IAbstractMediaItem item in items)
        {
          if (item.ContentUri == null) continue;
          if (item.ContentUri.IsFile == false) continue;
          if (item.ContentUri.LocalPath.ToLower().IndexOf("folder.jpg") >= 0) continue;
          
          if (Path.GetExtension(item.ContentUri.LocalPath.ToLower()) == ".cue")
          {
            // We've got a cue file, so let's analyse it and add the tracks to the items
            foundCueFile = true;
            List<IAbstractMediaItem> cueItems = ProcessCueSheet(item);
            if (cueItems.Count > 0)
            {
              cueFileItems.AddRange(cueItems);
              cueFiles.Add(cueItems[0].ContentUri.LocalPath);
            }

            // No need to check for the queuue files in the database
            continue;
          }

          bool found = false;
          IMediaItem mediaItem = item as IMediaItem;
          if (mediaItem != null)
          {
            foreach (IDbItem dbItem in results)
            {
              string contentUri = dbItem.Attributes["contenturi"].Value.ToString();
              if (mediaItem.ContentUri != null && mediaItem.ContentUri.IsFile && mediaItem.ContentUri.LocalPath == contentUri)
              {
                found = true;
                IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = dbItem.Attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                  mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                }
                mediaItem.Title = dbItem.Attributes["title"].Value as string;
                break;
              }
            }
            if (!found)
            {
              IDbItem dbItem = AddSong(mediaItem.ContentUri.LocalPath);
              if (dbItem != null)
              {
                dbItem.Save();
                IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = dbItem.Attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                  mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                }
                mediaItem.Title = dbItem.Attributes["title"].Value as string;
              }
            }
          }
        }
        
        // Now search through the Item and remove the file that the Cue is pointing from the list
        if (foundCueFile)
        {
          for (int i = 0; i < items.Count; i++)
          {
            foreach (string cuefile in cueFiles)
            {
              if (items[i].ContentUri.LocalPath == cuefile)
              {
                items.RemoveAt(i);
                i--;
                continue;
              }
            }
          }
        }

        // Add the Content of Cue File Items to the List
        if (cueFileItems.Count > 0)
          CollectionUtils.AddAll(items, cueFileItems);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    #endregion

    #region Private methods

    bool SongExists(string fileName)
    {
      try
      {
        Query trackByFilename = new Query("contenturi", Operator.Same, fileName);
        IList<IDbItem> result = _musicDatabase.Query(trackByFilename);
        return (result != null && result.Count > 0);
      }
      catch (Exception ex)
      {
      }
      return false;
    }

    /// <summary>
    /// Opens / Creates the Music database
    /// </summary>
    void CreateMusicDatabase()
    {
      try
      {
        IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
        IDatabaseFactory factory = builderFactory.CreateFromId("Music");

        _musicDatabase = factory.Open("Music");

        // Add the attributes
        _musicDatabase.Add("contenturi", typeof(string), 255);
        _musicDatabase.Add("path", typeof(string), 1000);
        _musicDatabase.Add("artist", typeof(List<string>), 1000);
        _musicDatabase.Add("albumArtist", typeof(List<string>), 1000);
        _musicDatabase.Add("album", typeof(string), 255);
        _musicDatabase.Add("genre", typeof(List<string>), 255);
        _musicDatabase.Add("title", typeof(string), 255);
        _musicDatabase.Add("track", typeof(int));
        _musicDatabase.Add("trackCount", typeof(int));
        _musicDatabase.Add("disc", typeof(int));
        _musicDatabase.Add("discCount", typeof(int));
        _musicDatabase.Add("duration", typeof(int));
        _musicDatabase.Add("year", typeof(int));
        _musicDatabase.Add("timesPlayed", typeof(int));
        _musicDatabase.Add("rating", typeof(int));
        _musicDatabase.Add("favorite", typeof(int));
        _musicDatabase.Add("resumeAt", typeof(int));
        _musicDatabase.Add("gainTrack", typeof(double));
        _musicDatabase.Add("peakTrack", typeof(double));
        _musicDatabase.Add("lyrics", typeof(string), 8192);
        _musicDatabase.Add("musicBrainzID", typeof(string), 255);
        _musicDatabase.Add("dateLastPlayed", typeof(DateTime));
        _musicDatabase.Add("dateAdded", typeof(DateTime));

        _musicDatabase.AddIndex("Music", "contenturi", "asc");
        _musicDatabase.AddIndex("Music", "artist", "asc");
        _musicDatabase.AddIndex("Music", "albumArtist", "asc");
        _musicDatabase.AddIndex("Music", "album", "asc");
        _musicDatabase.AddIndex("Music", "genre", "asc");
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error creating database");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Queries FreeDB for the Audio CD inserted
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="items"></param>
    private void QueryFreeDB(string folder, IList<IAbstractMediaItem> items)
    {
      string discId = string.Empty;
      CDInfoDetail MusicCD = new CDInfoDetail();
      char driveLetter = System.IO.Path.GetFullPath(folder).ToCharArray()[0];
      try
      {
        FreeDBQuery freedb = new FreeDBQuery();
        freedb.Connect();
        Freedb.CDInfo[] cds = freedb.GetDiscInfo(driveLetter);
        if (cds != null)
        {
          if (cds.Length == 1)
          {
            MusicCD = freedb.GetDiscDetails(cds[0].Category, cds[0].DiscId);
            discId = cds[0].DiscId;
          }
        }
        freedb.Disconnect();
      }
      catch (Exception)
      {
        MusicCD = null;
      }

      if (MusicCD != null)
      {
        // Update the Items with the Track Details
        int i = 0;
        foreach (IAbstractMediaItem item in items)
        {
          IMediaItem mediaItem = item as IMediaItem;
          if (mediaItem != null)
          {
            mediaItem.Title = MusicCD.Tracks[i].Title;
            mediaItem.MetaData["album"] = MusicCD.Title;
            mediaItem.MetaData["duration"] = MusicCD.Tracks[i].Duration;
            mediaItem.MetaData["track"] = MusicCD.Tracks[i].TrackNumber;

            if (MusicCD.Tracks[i].Artist != null)
              mediaItem.MetaData["album"] = MusicCD.Tracks[i].Artist;
            else
              mediaItem.MetaData["artist"] = MusicCD.Artist;
          }
          i++;
        }
      }
    }

    /// <summary>
    /// Opens a queue file and adds all the tracks found to the items
    /// </summary>
    /// <param name="strFileName"></param>
    /// <returns>List of Tracks found in the CueSheet</returns>
    private List<IAbstractMediaItem> ProcessCueSheet(IAbstractMediaItem item)
    {
      List<IAbstractMediaItem> items = new List<IAbstractMediaItem>();
      CueSheet cuesheet = new CueSheet(item.ContentUri.LocalPath);
      string defaultCueAudioFile = cuesheet.Tracks[0].DataFile.Filename;

      if (Path.GetDirectoryName(defaultCueAudioFile) == String.Empty)
        defaultCueAudioFile = Path.Combine(Path.GetDirectoryName(item.ContentUri.LocalPath), defaultCueAudioFile);

      // Get the tag from the original file, so that we have a duration
      MusicTag tag = GetTag(defaultCueAudioFile);

      for (int i = 0; i < cuesheet.Tracks.Length; i++)
      {
        Track track, nextTrack;
        int startPosition, endPosition;

        track = cuesheet.Tracks[i];

        if (track.TrackDataType != DataType.AUDIO)
          continue;

        if (i + 1 < cuesheet.Tracks.Length)
          nextTrack = cuesheet.Tracks[i + 1];
        else
          nextTrack = cuesheet.Tracks[i];

        string cueAudioFile = track.DataFile.Filename;
        if (cueAudioFile == null)
          cueAudioFile = defaultCueAudioFile;
        else
        {
          if (Path.GetDirectoryName(cueAudioFile) == String.Empty)
            cueAudioFile = Path.Combine(Path.GetDirectoryName(item.ContentUri.LocalPath), cueAudioFile);
        }

        CueMediaItem mediaitem = new CueMediaItem(cueAudioFile);
        mediaitem.Title = track.Title;
        mediaitem.MetaData["artist"] = track.Performer;
        mediaitem.MetaData["track"] = track.TrackNumber;
        mediaitem.MetaData["album"] = cuesheet.Title;
        // Calculate Startpoint in ms 
        // Note: 1 second = 75 Frames in a cuesheet
        startPosition = track.Indices[0].Minutes * 60 * 1000 + track.Indices[0].Seconds * 1000 + (int)((double)track.Indices[0].Frames / 75.0 * 1000.0);

        if (i + 1 < cuesheet.Tracks.Length)
          endPosition = nextTrack.Indices[0].Minutes * 60 * 1000 + nextTrack.Indices[0].Seconds * 1000 + (int)((double)nextTrack.Indices[0].Frames / 75.0 * 1000.0);
        else
          endPosition = tag.Duration * 1000;

        mediaitem.MetaData["resumeAt"] = startPosition;
        mediaitem.MetaData["duration"] = (endPosition - startPosition) / 1000;
        items.Add(mediaitem);
      }
      return items;
    }

    /// <summary>
    /// Remove songs, which are not existing anymore, because they have been moved, deleted.
    /// </summary>
    /// <returns></returns>
    private void DeleteNonExistingSongs()
    {
      try
      {
        IList<IDbItem> result;
        try
        {
          Query tracks = new Query();
          result = _musicDatabase.Query(tracks);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("MusicImporter: Unable to retrieve songs from database in DeleteNonExistingSongs()", ex);
          return;
        }

        int removed = 0;
        ServiceScope.Get<ILogger>().Info("MusicImporter: starting song cleanup for {0} songs", result.Count);
        for (int i = 0; i < result.Count; ++i)
        {
          string strFileName = (string)result[i].Attributes["contenturi"].Value;
          if (!File.Exists(strFileName))
          {
            /// song doesn't exist anymore, delete it
            removed++;
            DeleteSong(strFileName);
          }
        }
        ServiceScope.Get<ILogger>().Info("MusicImporter: DeleteNonExistingSongs completed. Removed {0} non-existing songs", removed);

      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("Musicimporter:error deleting non existing songs");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Delete a song from the database
    /// </summary>
    /// <param name="strFileName"></param>
    private void DeleteSong(string strFileName)
    {
      try
      {
        IList<IDbItem> result = new List<IDbItem>();
        try
        {
          Query tracks = new Query("contenturi", Operator.Same, strFileName);
          result = _musicDatabase.Query(tracks);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("MusicImporter: Unable to delete song.", ex);
        }
        if (result.Count > 0)
        {
          IDbItem track = result[0];
          track.Delete();
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error DeleteSong:{0}", strFileName);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return;
    }

    /// <summary>
    /// Retrieves the Tags from a file and formats them suiteable for insertion into the databse
    /// </summary>
    /// <param name="strFileName"></param>
    /// <returns></returns>
    private MusicTag GetTag(string strFileName)
    {
      try
      {
        TagReader tagreader = new TagReader();
        MusicTag tag = tagreader.ReadTag(strFileName);
        if (tag != null)
        {
          // When we got Multiple Entries of either Artist, Genre, Albumartist in WMP notation, separated by ";",
          // we will store them separeted by "|"
          tag.Artist = tag.Artist.Replace(';', '|');
          tag.AlbumArtist = tag.AlbumArtist.Replace(';', '|');
          tag.Genre = tag.Genre.Replace(';', '|');

          if (tag.AlbumArtist == "unknown" || tag.AlbumArtist == String.Empty)
          {
            tag.AlbumArtist = tag.Artist;
          }

          // Extract the Coverart
          //ExtractCoverArt(tag);

          return tag;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error GetTag:{0}", strFileName);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return null;
    }

    /// <summary>
    /// Adds a Song to the Database
    /// </summary>
    /// <param name="strFileName"></param>
    /// <returns></returns>
    private IDbItem AddSong(string strFileName)
    {
      try
      {
        // Get the Tags from the file
        MusicTag tag = GetTag(strFileName);
        if (tag != null)
        {
          // Todo: Handle Artist Prefix based on Settings
          //string sortableArtist = tag.Artist;
          //StripArtistNamePrefix(ref sortableArtist, true);

          IDbItem track = _musicDatabase.CreateNew();
          track["contenturi"] = tag.FileName;
          track["path"] = Path.GetDirectoryName(tag.FileName);
          track["artist"] = tag.Artist;
          track["albumArtist"] = tag.AlbumArtist;
          track["album"] = tag.Album;
          track["genre"] = tag.Genre;
          track["title"] = tag.Title;
          track["track"] = tag.Track;
          track["trackCount"] = tag.TrackTotal;
          track["disc"] = tag.DiscID;
          track["discCount"] = tag.DiscTotal;
          track["duration"] = tag.Duration;
          track["year"] = tag.Year;
          track["timesPlayed"] = 0;
          track["rating"] = tag.Rating;
          track["favorite"] = 0;
          track["resumeAt"] = 0;
          track["gainTrack"] = 0;
          track["peakTrack"] = 0;
          track["lyrics"] = tag.Lyrics;
          track["musicBrainzID"] = String.Empty;
          track["dateLastPlayed"] = DateTime.MinValue;
          track["dateAdded"] = DateTime.Now;
          return track;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error AddSong:{0}", strFileName);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return null;
    }

    /// <summary>
    /// Update an existing song with the Tags from the file
    /// </summary>
    /// <param name="strFileName"></param>
    /// <returns></returns>
    private IDbItem UpdateSong(string strFileName)
    {
      try
      {
        // Get the Tags from the file
        MusicTag tag = GetTag(strFileName);
        if (tag != null)
        {
          IList<IDbItem> result;

          try
          {
            Query trackByFilename = new Query("contenturi", Operator.Same, strFileName);
            result = _musicDatabase.Query(trackByFilename);
          }
          catch (Exception ex)
          {
            ServiceScope.Get<ILogger>().Error(
              "MusicImporter: Update finished with error (exception in db select query)", ex);
            return null;
          }

          if (result.Count > 0)
          {
            // Todo: Handle Artist Prefix based on Settings
            //string sortableArtist = tag.Artist;
            //StripArtistNamePrefix(ref sortableArtist, true);

            IDbItem track = result[0];
            track["artist"] = tag.Artist;
            track["albumArtist"] = tag.AlbumArtist;
            track["album"] = tag.Album;
            track["genre"] = tag.Genre;
            track["title"] = tag.Title;
            track["track"] = tag.Track;
            track["trackCount"] = tag.TrackTotal;
            track["disc"] = tag.DiscID;
            track["discCount"] = tag.DiscTotal;
            track["year"] = tag.Year;
            track["rating"] = tag.Rating;
            track["lyrics"] = tag.Lyrics;
            return track;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("musicimporter:error UpdateSong:{0}", strFileName);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return null;
    }

    /// <summary>
    /// Move the Prefix of an artist to the end of the string for better sorting
    /// i.e. "The Rolling Stones" -> "Rolling Stones, The" 
    /// </summary>
    /// <param name="artistName"></param>
    /// <param name="appendPrefix"></param>
    /// <returns></returns>
    private void StripArtistNamePrefix(ref string artistName, bool appendPrefix)
    {
      string temp = artistName.ToLower();

      foreach (string s in ArtistNamePrefixes)
      {
        if (s.Length == 0)
        {
          continue;
        }

        string prefix = s;
        prefix = prefix.Trim().ToLower();
        int pos = temp.IndexOf(prefix + " ");
        if (pos == 0)
        {
          string tempName = artistName.Substring(prefix.Length).Trim();

          if (appendPrefix)
          {
            artistName = string.Format("{0}, {1}", tempName, artistName.Substring(0, prefix.Length));
          }

          else
          {
            artistName = temp;
          }

          return;
        }
      }

      return;
    }

    #endregion
  }
}
