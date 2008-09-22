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
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Threading;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;

using MediaPortal.Utilities.Scraper;

namespace Media.Importers.MovieImporter
{
  public class Importer : IPluginStateTracker, IImporter
  {
    //List<string> _extensions;
    IDatabase _movieDatabase;
    DateTime _lastImport = DateTime.MinValue;
    Scraper scraper;
    private IMessageQueue _queue;

    public Importer()
    {
      _queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("imdbimporters");
      //_extensions = new List<string>();
      //_extensions.Add(".wmv");
      //_extensions.Add(".mpg");
      //_extensions.Add(".avi");
      //_extensions.Add(".mkv");
      //_extensions.Add(".ts");
      //_extensions.Add(".ifo");
    }

    #region IPluginStateTracker implementation

    public void Activated()
    {
      CreateMovieDatabase();
      scraper = new Scraper();
      scraper.Load(@"scrapers\video\imdb.xml");
    }

    public bool RequestEnd()
    {
      return false; // FIXME: The importer plugin should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      //ServiceScope.Get<IImporterManager>().UnRegister(this);
    }

    #endregion

    //#region IAutoStart Members

    //public void Startup()
    //{

    //  ServiceScope.Get<IImporterManager>().Register(this);
    //}

    //#endregion

    #region IImporter Members

    /// <summary>
    /// Do any housekeeping before the first file gets imported
    /// </summary>
    /// <returns></returns>
    public void BeforeImport(int avAilableFiles)
    {
    }

    /// <summary>
    /// Do Cleanup after all files have been imported
    /// </summary>
    /// <returns></returns>
    public void AfterImport()
    {
    }

    /// <summary>
    /// Imports the file.
    /// </summary>
    /// <param name="folder">The file.</param>
    public bool FileImport(string file)
    {
      try
      {
        string ext = System.IO.Path.GetExtension(file).ToLower();
        bool isDvd = (ext == ".ifo");
        DirectoryInfo dvdFolder = null;
        string fileOrg = file;
        if (isDvd && file.ToLower().IndexOf("video_ts.ifo") < 0) return false;
        if (isDvd)
        {
          string folder = System.IO.Path.GetDirectoryName(file);
          dvdFolder = System.IO.Directory.GetParent(folder);//video_ts
          fileOrg = dvdFolder.FullName;
        }
        try
        {
          Query movieByFilename = new Query("contentURI", Operator.Same, fileOrg);
          List<IDbItem> result = _movieDatabase.Query(movieByFilename);
          if (result.Count > 0) return false;
        }
        catch (Exception)
        {
          return false;
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
        movie["size"] = info.Length;
        if (!isDvd)
        {
          movie["contentURI"] = file;
          movie["title"] = Path.GetFileNameWithoutExtension(file);
          movie["path"] = Path.GetDirectoryName(file);
          movie["CoverArt"] = file;
        }
        else
        {
          movie["title"] = dvdFolder.Name;
          movie["contentURI"] = dvdFolder.FullName;
          DirectoryInfo parent = System.IO.Directory.GetParent(dvdFolder.FullName);//video_ts
          movie["path"] = parent.FullName;
          movie["CoverArt"] = "";
        }
        movie["date"] = info.CreationTime;
        movie["isDVD"] = isDvd ? 1 : 0;
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

        #region code testing the xbmc scraper
        if (scraper.IsLoaded)
        {
          scraper.CreateSearchUrl((string)movie["title"]);
          ServiceScope.Get<ILogger>().Info("movieimporter: getting online info from: {0} ", scraper.SearchUrl);
          scraper.GetSearchResults();
          ServiceScope.Get<ILogger>().Info("movieimporter: result found {0} ", scraper.SearchResults.Count);
          if (scraper.SearchResults.Count > 0)
          {

            QueueMessage msgc = new QueueMessage();
            msgc.MessageData["action"] = "imdbchoiceneeded";
            msgc.MessageData["file"] = file;
            msgc.MessageData["title"] = (string)movie["title"];
            List<string> urlList = new List<string>();
            List<string> idList = new List<string>();
            List<string> titleList = new List<string>();
            foreach (ScraperSearchResult res in scraper.SearchResults)
            {
              urlList.Add(res.Url);
              idList.Add(res.Id);
              titleList.Add(res.Title);
            }
            msgc.MessageData["urls"] = urlList;
            msgc.MessageData["ids"] = idList;
            msgc.MessageData["titles"] = titleList;
            SendMessage(msgc);

            ServiceScope.Get<ILogger>().Info("movieimporter: getting online info for: {0} ", scraper.SearchResults[0].Title);
            scraper.GetDetails(scraper.SearchResults[0].Url, scraper.SearchResults[0].Id);
            if (scraper.Metadata.ContainsKey("genre"))
            {
              movie["title"] = scraper.Metadata["title"];
              movie["genre"] = scraper.Metadata["genre"];
              if (scraper.Metadata.ContainsKey("thumb"))
                movie["CoverArt"] = scraper.Metadata["thumb"];
              if (scraper.Metadata.ContainsKey("actors"))
                movie["actors"] = scraper.Metadata["actors"];
              if (scraper.Metadata.ContainsKey("year"))
                movie["year"] = scraper.Metadata["year"];

            }
          }
        }
        else
        {
          ServiceScope.Get<ILogger>().Info("movieimporter: no online scraper are loaded ");
        }

        #endregion

        movie.Save();
        
        // create & send message
        QueueMessage msg = new QueueMessage();
        msg.MessageData["action"] = "fileinfoupdated";
        msg.MessageData["file"] = file;
        SendMessage(msg);

        return true;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("movieimporter:error importing file:{0}", file);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return false;
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the deleted file.</param>
    public void FileDeleted(string file)
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

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the new file.</param>
    public void FileCreated(string file)
    {
      FileImport(file);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void FileChanged(string file)
    {
      FileDeleted(file);

      FileImport(file);
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
    ///// <summary>
    ///// Called by the importer manager when a full-import needs to be done from the folder
    ///// </summary>
    ///// <param name="folder">The folder.</param>
    ///// <param name="since"></param>
    //public void ImportFolder(string folder, DateTime since)
    //{
    //  since = _lastImport;

    //  try
    //  {
    //    ServiceScope.Get<ILogger>().Info("movie importer:import {0} since {1}", folder, since.ToShortDateString());
    //    DeleteNonExistingMovies();
    //    List<string> availableFiles = new List<string>();
    //    Import(folder, ref availableFiles, since);
    //    ServiceScope.Get<ILogger>().Info("movieimporter:found {0} new/changed movies", availableFiles.Count);
    //    foreach (string fileName in availableFiles)
    //    {
    //      ImportFile(fileName);
    //    }
    //    ServiceScope.Get<ILogger>().Info("movieimporter:imported {0} movies", availableFiles.Count);
    //    _lastImport = DateTime.Now;
    //  }
    //  catch (Exception ex)
    //  {
    //    ServiceScope.Get<ILogger>().Info("movieimporter:error importing {0} ", folder);
    //    ServiceScope.Get<ILogger>().Error(ex);
    //  }
    //}

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
          for (int i = 0; i < items.Count; ++i)
          {
            IAbstractMediaItem item = items[i];
            IMediaItem mediaItem = item as IMediaItem;
            IRootContainer container = item as IRootContainer;
            if (container == null)
            {
              if (mediaItem != null && mediaItem.ContentUri != null && mediaItem.ContentUri.IsFile && mediaItem.ContentUri.LocalPath == contentUri)
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
            else if (((int)dbItem.Attributes["isDVD"].Value) == 1)
            {
              string dvdPath = (string)dbItem.Attributes["contentURI"].Value;
              Uri uri = container.ContentUri;
              string containerPath = uri.LocalPath;

              if (String.Compare(containerPath, dvdPath, true) == 0)
              {
                DvdMediaItem dvditem = new DvdMediaItem(dvdPath, (string)dbItem.Attributes["title"].Value, container.Parent);

                Dictionary<string, IDbAttribute>.Enumerator enumer = dbItem.Attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                  dvditem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                }
                if (String.IsNullOrEmpty(dbItem.Attributes["CoverArt"].Value as String))
                {
                  dvditem.MetaData["CoverArt"] = "dvd_logo.png";
                }
                items[i] = dvditem;
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
      try
      {
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
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("movieimporter:error cleanup non existing movies");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    //void Import(string folder, ref List<string> availableFiles, DateTime since)
    //{

    //  ServiceScope.Get<ILogger>().Info("movieimporter   {0}", folder);
    //  try
    //  {
    //    string[] subFolders = Directory.GetDirectories(folder);
    //    for (int i = 0; i < subFolders.Length; ++i)
    //    {
    //      Import(subFolders[i], ref availableFiles, since);
    //    }
    //    string[] files = Directory.GetFiles(folder);
    //    for (int i = 0; i < files.Length; ++i)
    //    {
    //      string ext = Path.GetExtension(files[i]).ToLower();
    //      if (Extensions.Contains(ext))
    //      {
    //        if (CheckFile(files[i], since))
    //        {
    //          availableFiles.Add(files[i]);
    //        }
    //      }
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    ServiceScope.Get<ILogger>().Info("movieimporter:error importing folder:{0}", folder);
    //    ServiceScope.Get<ILogger>().Error(ex);
    //  }
    //}

    //bool CheckFile(string fileName, DateTime lastImport)
    //{
    //  if ((File.GetAttributes(fileName) & FileAttributes.Hidden) == FileAttributes.Hidden)
    //  {
    //    return false;
    //  }
    //  if (File.GetCreationTime(fileName) > lastImport || File.GetLastWriteTime(fileName) > lastImport)
    //  {
    //    return true;
    //  }
    //  return false;
    //}

    /// <summary>
    /// Creates the movie database.
    /// </summary>
    void CreateMovieDatabase()
    {
      try
      {
        IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
        IDatabaseFactory factory = builderFactory.CreateFromId("Movies");


        _movieDatabase = factory.Open("Movies");

        _movieDatabase.Add("title", typeof(string), 1024);
        _movieDatabase.Add("genre", typeof(List<string>), 40);
        _movieDatabase.Add("director", typeof(string), 40);
        _movieDatabase.Add("contentURI", typeof(string), 1000);
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
        _movieDatabase.Add("actors", typeof(List<string>), 1000);
        _movieDatabase.Add("path", typeof(string), 1000);
        _movieDatabase.Add("lastplayed", typeof(DateTime));
        _movieDatabase.Add("size", typeof(int));
        _movieDatabase.Add("dateAdded", typeof(DateTime));
        _movieDatabase.Add("isDVD", typeof(int));
        _movieDatabase.Add("year", typeof(string), 4);

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
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("movieimporter:error creating database");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }
    #endregion

    #region Messaging

    private void SendMessage(QueueMessage msg)
    {
       // asynchronously send message through queue
      ServiceScope.Get<IThreadPool>().Add(new DoWorkHandler(delegate { _queue.Send(msg); }));
    }

    #endregion
  }
}
