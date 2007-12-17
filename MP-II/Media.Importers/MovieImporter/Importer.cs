#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
//using MediaManager.Views;
using MediaPortal.Core;
using MediaPortal.Core.Database.Interfaces;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManager.Views;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Importers;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.MediaManager;
namespace MovieImporter
{
  public class Importer : IPlugin, IAutoStart, IImporter
  {
    #region IPlugin Members
    List<string> _extensions;
    IDatabase _movieDatabase;
    DateTime _lastImport = DateTime.MinValue;

    public Importer()
    {
      _extensions = new List<string>();
      _extensions.Add(".wmv");
      _extensions.Add(".mpg");
      _extensions.Add(".avi");
      _extensions.Add(".mkv");
      _extensions.Add(".ts");
      _extensions.Add(".ifo");
    }

    public void Initialize(string id)
    {
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      ServiceScope.Get<IImporterManager>().UnRegister(this);
    }

    #endregion

    #region IAutoStart Members

    public void Startup()
    {
      CreateMovieDatabase();
      ServiceScope.Get<IImporterManager>().Register(this);
    }

    #endregion

    #region IImporter Members

    /// <summary>
    /// Gets the importer name.
    /// </summary>
    /// <value>The importer name.</value>
    public string Name
    {
      get
      {
        return "MovieImporter";
      }
    }

    /// <summary>
    /// Gets the file-extensions the importer supports
    /// </summary>
    /// <value>The file-extensions.</value>
    public List<string> Extensions
    {
      get
      {
        return _extensions;
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the deleted file.</param>
    public void FileDeleted(string file)
    {
      string ext = Path.GetExtension(file).ToLower();
      if (Extensions.Contains(ext))
      {
        try
        {
          Query movieByFilename = new Query("contentURI", Operator.Same, file);
          List<IDbItem> result = _movieDatabase.Query(movieByFilename);
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
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the new file.</param>
    public void FileCreated(string file)
    {
      string ext = Path.GetExtension(file).ToLower();
      if (Extensions.Contains(ext))
      {
        ImportFile(file);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void FileChanged(string file)
    {
      FileDeleted(file);

      string ext = Path.GetExtension(file).ToLower();
      if (Extensions.Contains(ext))
      {
        ImportFile(file);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file / directory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the renamed file / folder.</param>
    /// <param name="olfdFile">The previous filename of the renamed file / folder.</param>
    public void FileRenamed(string file, string oldFile)
    {
      // The rename may have been on a directory or a file
      FileInfo fi = new FileInfo(file);
      if (fi.Exists)
      {
        List<IDbItem> result;
        try
        {
          Query movieByFilename = new Query("contenturi", Operator.Same, oldFile);
          result = _movieDatabase.Query(movieByFilename);
          if (result.Count > 0)
          {

            IDbItem movie = result[0];
            movie["contenturi"] = file;
            movie.Save();
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
        DirectoryInfo di = new DirectoryInfo(file);
        if (di.Exists)
        {
          List<IDbItem> result;
          try
          {
            Query movieByFilename = new Query("contenturi", Operator.Like, String.Format("{0}%", oldFile));
            result = _movieDatabase.Query(movieByFilename);
            if (result.Count > 0)
            {
              // We might have changed a Top directory, so we get a lot of path entries returned
              for (int i = 0; i < result.Count; i++)
              {
                IDbItem movie = result[i];
                string strPath = movie["contenturi"].ToString().Replace(oldFile, file);
                movie["contenturi"] = strPath;
                movie.Save();
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

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="directory">The name of the deleted folder.</param>
    public void DirectoryDeleted(string directory)
    {
      try
      {
        Query movieByFilename = new Query("contentURI", Operator.Like, String.Format("{0}%", directory));
        List<IDbItem> result = _movieDatabase.Query(movieByFilename);
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
    /// <summary>
    /// Called by the importer manager when a full-import needs to be done from the folder
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="since"></param>
    public void ImportFolder(string folder, DateTime since)
    {
      since = _lastImport;
      ServiceScope.Get<ILogger>().Info("movie importer:import {0} since {1}", folder, since.ToShortDateString());
      DeleteNonExistingMovies();
      List<string> availableFiles = new List<string>();
      Import(folder, ref availableFiles, since);
      ServiceScope.Get<ILogger>().Info("movieimporter:found {0} new/changed movies", availableFiles.Count);
      foreach (string fileName in availableFiles)
      {
        ImportFile(fileName);
      }
      ServiceScope.Get<ILogger>().Info("movieimporter:imported {0} movies", availableFiles.Count);
      _lastImport = DateTime.Now;
    }

    /// <summary>
    /// Gets the meta data for.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="items">The items.</param>
    public void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items)
    {
      try
      {
        Query moviesByPath = new Query("path", Operator.Same, folder);
        List<IDbItem> results = _movieDatabase.Query(moviesByPath);
        foreach (IDbItem dbItem in results)
        {
          string contentUri = dbItem.Attributes["contentURI"].Value.ToString();
          foreach (IAbstractMediaItem item in items)
          {
            IMediaItem mediaItem = item as IMediaItem;
            if (mediaItem != null)
            {
              if (mediaItem.ContentUri != null && mediaItem.ContentUri.IsFile && mediaItem.ContentUri.LocalPath == contentUri)
              {
                Dictionary<string, IDbAttribute>.Enumerator enumer = dbItem.Attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                  mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                }
                mediaItem.Title = dbItem.Attributes["title"].Value as string;
                break;
              }
            }
          }
        }
      }
      catch (Exception)
      {
      }
    }
    #endregion

    #region importer private methods
    /// <summary>
    /// Remove songs, which are not existing anymore, because they have been moved, deleted.
    /// </summary>
    /// <returns></returns>
    private void DeleteNonExistingMovies()
    {
      List<IDbItem> result;
      try
      {
        Query movies = new Query();
        result = _movieDatabase.Query(movies);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("movieimporter: Unable to retrieve movies from database in DeleteNonExistingMovies()", ex);
        return;
      }

      int removed = 0;
      ServiceScope.Get<ILogger>().Info("movieimporter: starting cleanup for {0} movies", result.Count);
      for (int i = 0; i < result.Count; ++i)
      {
        string strFileName = (string)result[i].Attributes["contentURI"].Value;
        if (!File.Exists(strFileName) && !String.IsNullOrEmpty(strFileName))
        {
          /// song doesn't exist anymore, delete it
          removed++;
          FileDeleted(strFileName);
        }
      } //for (int i=0; i < results.Rows.Count;++i)
      ServiceScope.Get<ILogger>().Info("movieimporter: DeleteNonExistingMovies completed. Removed {0} non-existing movies", removed);
    }

    void Import(string folder, ref List<string> availableFiles, DateTime since)
    {
      ServiceScope.Get<ILogger>().Info("movieimporter   {0}", folder);
      try
      {
        string[] subFolders = Directory.GetDirectories(folder);
        for (int i = 0; i < subFolders.Length; ++i)
        {
          Import(subFolders[i], ref availableFiles, since);
        }
        string[] files = Directory.GetFiles(folder);
        for (int i = 0; i < files.Length; ++i)
        {
          string ext = Path.GetExtension(files[i]).ToLower();
          if (Extensions.Contains(ext))
          {
            if (CheckFile(files[i], since))
            {
              availableFiles.Add(files[i]);
            }
          }
        }
      }
      catch (Exception)
      {
      }
    }

    bool CheckFile(string fileName, DateTime lastImport)
    {
      if ((File.GetAttributes(fileName) & FileAttributes.Hidden) == FileAttributes.Hidden)
      {
        return false;
      }
      if (File.GetCreationTime(fileName) > lastImport || File.GetLastWriteTime(fileName) > lastImport)
      {
        return true;
      }
      return false;
    }

    /// <summary>
    /// Imports the file.
    /// </summary>
    /// <param name="folder">The file.</param>
    int ImportFile(string file)
    {
      string ext = System.IO.Path.GetExtension(file).ToLower();
      bool isDvd = (ext == ".ifo");
      if (isDvd && file.ToLower().IndexOf("video_ts.ifo") < 0) return 0;
      try
      {
        Query movieByFilename = new Query("contentURI", Operator.Same, file);
        List<IDbItem> result = _movieDatabase.Query(movieByFilename);
        if (result.Count > 0) return 0;
      }
      catch (Exception)
      {
        return 0;
      }
      FileInfo info = new FileInfo(file);
      MediaInfo mediaInfo = new MediaInfo();
      try
      {
        mediaInfo.Open(file);
      }
      catch (Exception)
      {
        mediaInfo = null;
      }
      IDbItem movie = _movieDatabase.CreateNew(); ;
      movie["contentURI"] = file;
      movie["CoverArt"] = file;
      movie["size"] = info.Length;
      if (!isDvd)
      {
        movie["title"] = Path.GetFileNameWithoutExtension(file);
      }
      else
      {
        string videoTsdir = System.IO.Path.GetDirectoryName(file);
        DirectoryInfo dirInfo = System.IO.Directory.GetParent(videoTsdir);
        movie["title"] = dirInfo.Name;
      }
      movie["date"] = info.CreationTime;
      movie["path"] = Path.GetDirectoryName(file);
      movie["isDVD"] = isDvd;
      movie["dateAdded"] = info.CreationTime;
      if (mediaInfo != null)
      {
        movie["VideoCodec"] = mediaInfo.getVidCodec();
        movie["VideoBitRate"] = mediaInfo.getVidBitrate();
        movie["Width"] = mediaInfo.getWidth();
        movie["Height"] = mediaInfo.getHeight();
        movie["FPS"] = mediaInfo.getFPS();
        movie["AudioStreams"] = mediaInfo.getAudioCount();
        movie["AudioCodec"] = mediaInfo.getAudioCodec();
        movie["AspectRatio"] = mediaInfo.getAR();
        movie["AudioBitRate"] = mediaInfo.getAudioBitrate();
        string playtime = mediaInfo.getPlaytime();
        int playtimeSecs = 0;
        Int32.TryParse(playtime, out playtimeSecs);
        movie["duration"] = playtimeSecs;
        mediaInfo.Close();
      }
      movie.Save();

      return 1;
    }

    /// <summary>
    /// Creates the movie database.
    /// </summary>
    void CreateMovieDatabase()
    {
      IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
      IDatabaseFactory factory = builderFactory.Create(@"sqlite:Data Source=Databases\movies3.db3");


      _movieDatabase = factory.Open("Movies");

      _movieDatabase.Add("title", typeof(string), 1024);
      _movieDatabase.Add("genre", typeof(List<string>), 40);
      _movieDatabase.Add("director", typeof(string), 40);
      _movieDatabase.Add("contentURI", typeof(string), 1024);
      _movieDatabase.Add("CoverArt", typeof(string), 1024);
      _movieDatabase.Add("VideoCodec", typeof(string), 100);
      _movieDatabase.Add("VideoBitRate", typeof(string), 100);
      _movieDatabase.Add("Width", typeof(string), 100);
      _movieDatabase.Add("Height", typeof(string), 100);
      _movieDatabase.Add("AspectRatio", typeof(string), 100);
      _movieDatabase.Add("FPS", typeof(string), 100);
      _movieDatabase.Add("AudioStreams", typeof(string), 100);
      _movieDatabase.Add("AudioCodec", typeof(string), 100);
      _movieDatabase.Add("AudioBitRate", typeof(string), 100);
      _movieDatabase.Add("date", typeof(DateTime));
      _movieDatabase.Add("rating", typeof(int));
      _movieDatabase.Add("duration", typeof(int));
      _movieDatabase.Add("tagline", typeof(string), 60);
      _movieDatabase.Add("plot", typeof(string), 1024);
      _movieDatabase.Add("actors", typeof(List<string>), 1024);
      _movieDatabase.Add("path", typeof(string), 1024);
      _movieDatabase.Add("lastplayed", typeof(DateTime));
      _movieDatabase.Add("size", typeof(int));
      _movieDatabase.Add("dateAdded", typeof(DateTime));
      _movieDatabase.Add("isDVD", typeof(int));

      //get date/time of last import done....
      Query lastDateQuery = new Query();
      lastDateQuery.Sort = SortOrder.Descending;
      lastDateQuery.SortFields.Add("dateAdded");
      lastDateQuery.Limit = 1;
      List<IDbItem> lastItems = _movieDatabase.Query(lastDateQuery);
      if (lastItems.Count == 0)
        _lastImport = DateTime.MinValue;
      else
        _lastImport = (DateTime)lastItems[0]["dateAdded"];
    }
    #endregion
  }
}
