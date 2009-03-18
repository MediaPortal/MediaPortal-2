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
using System.IO;
using System.Collections.Generic;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Database;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MediaManagement.Views;

namespace Media.Importers.PictureImporter
{
  public class PictureImporter : IPluginStateTracker, IImporter
  {
    IList<string> _extensions;
    IDatabase _pictureDatabase;

    public PictureImporter()
    {
      _extensions = new List<string>();
      _extensions.Add(".jpg");
      _extensions.Add(".jpeg");
      _extensions.Add(".png");
      _extensions.Add(".gif");
      _extensions.Add(".tga");
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      CreatePictureDatabase();
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

    #region IImporter Members

    public void BeforeImport(int avAilableFiles) { }

    public void AfterImport() { }

    public bool FileImport(string filePath)
    {
      try
      {
        if (String.IsNullOrEmpty(filePath)) return false;
        if (filePath.ToLower().IndexOf("folder.jpg") >= 0) return false;
        string fName = System.IO.Path.GetFileName(filePath);
        if (fName.ToLower().StartsWith("albumart")) return false;
        string ext = System.IO.Path.GetExtension(filePath).ToLower();
        if (!_extensions.Contains(ext)) return false;
        try
        {
          Query imageByFilename = new Query("contentURI", Operator.Same, filePath);
          IList<IDbItem> result = _pictureDatabase.Query(imageByFilename);
          if (result.Count > 0) return false;
        }
        catch (Exception)
        {
          return false;
        }
        IDbItem picture = GetExifFor(filePath);
        if (picture == null)
          return false;

        picture.Save();
        return true;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error ImportFile:{0}", filePath);
        ServiceScope.Get<ILogger>().Error(ex);
        return false;
      }
    }

    public void FileDeleted(string filePath)
    {
      try
      {
        Query imageByFilename = new Query("contentURI", Operator.Same, filePath);
        IList<IDbItem> result = _pictureDatabase.Query(imageByFilename);
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

    public void FileCreated(string filePath)
    {
      FileImport(filePath);
    }

    public void FileChanged(string filePath)
    {
      FileDeleted(filePath);

      FileImport(filePath);
    }

    public void FileRenamed(string filePath, string oldFilePath)
    {
      try
      {
        // The rename may have been on a directory or a file
        FileInfo fi = new FileInfo(filePath);
        if (fi.Exists)
        {
          IList<IDbItem> result;
          try
          {
            Query imageByFilename = new Query("contenturi", Operator.Same, oldFilePath);
            result = _pictureDatabase.Query(imageByFilename);
            if (result.Count > 0)
            {

              IDbItem picture = result[0];
              picture["contenturi"] = filePath;
              picture.Save();
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
              Query imageByFilename = new Query("contenturi", Operator.Like, String.Format("{0}%", oldFilePath));
              result = _pictureDatabase.Query(imageByFilename);
              if (result.Count > 0)
              {
                // We might have changed a Top directory, so we get a lot of path entries returned
                for (int i = 0; i < result.Count; i++)
                {
                  IDbItem picture = result[i];
                  string strPath = picture["contenturi"].ToString().Replace(oldFilePath, filePath);
                  picture["contenturi"] = strPath;
                  picture.Save();
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
        ServiceScope.Get<ILogger>().Info("pictureimporter:error FileRenamed:{0}", filePath);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    public void DirectoryDeleted(string directory)
    {
      try
      {
        Query imageByFilename = new Query("contentURI", Operator.Like, String.Format("{0}%", directory));
        IList<IDbItem> result = _pictureDatabase.Query(imageByFilename);
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
        Query imagesByPath = new Query("path", Operator.Same, folder);
        IList<IDbItem> results = _pictureDatabase.Query(imagesByPath);
        foreach (IAbstractMediaItem item in items)
        {
          if (item.ContentUri == null) continue;
          if (item.ContentUri.IsFile == false) continue;
          if (item.ContentUri.LocalPath.ToLower().IndexOf("folder.jpg") >= 0) continue;
          string fName = Path.GetFileName(item.ContentUri.LocalPath);
          if (fName.ToLower().StartsWith("albumart")) continue;
          //string ext = Path.GetExtension(item.ContentUri.LocalPath).ToLower();
          //if (Extensions.Contains(ext))
          //{
            bool found = false;
            IMediaItem mediaItem = item as IMediaItem;
            if (mediaItem != null)
            {
              foreach (IDbItem dbItem in results)
              {
                string contentUri = dbItem.Attributes["contentURI"].Value.ToString();

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
                IDbItem dbItem = GetExifFor(mediaItem.ContentUri.LocalPath);
                if (dbItem != null)
                {
                  IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = dbItem.Attributes.GetEnumerator();
                  while (enumer.MoveNext())
                  {
                    mediaItem.MetaData[enumer.Current.Key] = enumer.Current.Value.Value;
                  }
                  mediaItem.Title = dbItem.Attributes["title"].Value as string;

                }
              }
            }
          //}
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
    private void DeleteNonExistingPictures()
    {
      try
      {
        IList<IDbItem> result;
        try
        {
          Query pictures = new Query();
          result = _pictureDatabase.Query(pictures);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("PictureImporter: Unable to retrieve pictures from database in DeleteNonExistingPictures()", ex);
          return;
        }

        int removed = 0;
        ServiceScope.Get<ILogger>().Info("PictureImporter: starting cleanup for {0} pictures", result.Count);
        for (int i = 0; i < result.Count; ++i)
        {
          string strFileName = (string)result[i].Attributes["contentURI"].Value;
          if (!File.Exists(strFileName))
          {
            /// song doesn't exist anymore, delete it
            removed++;
            FileDeleted(strFileName);
          }
        } //for (int i=0; i < results.Rows.Count;++i)
        ServiceScope.Get<ILogger>().Info("PictureImporter: DeleteNonExistingPictures completed. Removed {0} non-existing pictures", removed);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error DeleteNonExistingPictures");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

 
    IDbItem GetExifFor(string file)
    {
      try
      {
        string ext = System.IO.Path.GetExtension(file).ToLower();
        if (!_extensions.Contains(ext)) return null;

        using (ExifMetaInfo exif = new ExifMetaInfo(file))
        {
          FileInfo info = new FileInfo(file);
          IDbItem picture = _pictureDatabase.CreateNew(); ;
          picture["contentURI"] = file;
          picture["CoverArt"] = file;
          picture["title"] = Path.GetFileNameWithoutExtension(file);
          picture["Date"] = info.CreationTime;
          picture["path"] = Path.GetDirectoryName(file);

          picture["EquipmentMake"] = exif.EquipMake;
          picture["CameraModel"] = exif.EquipModel;
          picture["title"] = Path.GetFileNameWithoutExtension(file);

          if (exif.DTOrig != DateTime.MinValue)
            picture["Date"] = exif.DTOrig;
          else
            picture["Date"] = info.CreationTime;

          picture["ExposureCompensation"] = ((double)exif.ExposureBias).ToString();
          picture["ExposureTime"] = exif.ExposureTime.ToString() + "s";
          picture["Flash"] = exif.FlashMode;
          picture["Fstop"] = "F" + ((double)exif.FNumber).ToString();
          picture["ImgDimensions"] = string.Format("{0}x{1}", exif.PixXDim, exif.PixYDim);
          picture["ISOSpeed"] = exif.ISOSpeed;
          picture["Orientation"] = exif.Orientation;
          picture["MeteringMod"] = exif.MeteringMode.ToString();

          //picture["Resolutions"] = tag.Resolutions;
          picture["ShutterSpeed"] = exif.ExposureTime.ToString() + "s";
          picture["ViewComment"] = exif.ImageDescription;
          picture["path"] = Path.GetDirectoryName(file);
          picture["dateAdded"] = info.CreationTime;
          return picture;
        }
      }
      catch (Exception)
      {
      }
      return null;
    }

    /// <summary>
    /// Creates the picture database.
    /// </summary>
    void CreatePictureDatabase()
    {
      try
      {
        IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
        IDatabaseFactory factory = builderFactory.CreateFromId("Pictures");

        _pictureDatabase = factory.Open("Pictures");

        _pictureDatabase.Add("CameraModel", typeof(string), 40);
        _pictureDatabase.Add("EquipmentMake", typeof(string), 40);
        _pictureDatabase.Add("ExposureCompensation", typeof(string), 1024);
        _pictureDatabase.Add("ExposureTime", typeof(string), 1024);
        _pictureDatabase.Add("Flash", typeof(string), 40);
        _pictureDatabase.Add("Fstop", typeof(string), 40);
        _pictureDatabase.Add("ImgDimensions", typeof(string), 40);
        _pictureDatabase.Add("title", typeof(string), 60);
        _pictureDatabase.Add("MeteringMod", typeof(string), 1024);
        _pictureDatabase.Add("Resolutions", typeof(string), 1024);
        _pictureDatabase.Add("ShutterSpeed", typeof(string), 1024);
        _pictureDatabase.Add("ViewComment", typeof(string), 1024);
        _pictureDatabase.Add("ISOSpeed", typeof(string), 1024);
        _pictureDatabase.Add("Orientation", typeof(int));
        _pictureDatabase.Add("PictureTags", typeof(List<string>), 1000);
        _pictureDatabase.Add("Date", typeof(DateTime), 1024);
        _pictureDatabase.Add("contentURI", typeof(string), 1024);
        _pictureDatabase.Add("CoverArt", typeof(string), 1024);
        _pictureDatabase.Add("Updated", typeof(string), 1);
        _pictureDatabase.Add("path", typeof(string), 1024);
        _pictureDatabase.Add("dateAdded", typeof(DateTime));
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("pictureimporter:error CreatePictureDatabase");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }
    #endregion
  }
}
