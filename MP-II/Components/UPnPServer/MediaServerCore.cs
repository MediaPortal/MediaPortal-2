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
using System.Collections;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using Intel.UPNP;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.CdsMetadata;
//using MetadataParser;

namespace Components.UPnPServer
{
  public class MediaServerCore2
  {
    // Fields
    public static int CacheTime = 0x708;
    public static string CustomUDN = "";
    private static DText DT = new DText();
    public static bool INMPR = true;
    private UPnPDeviceWatcher m_DeviceWatcher;
    //private DvMediaContainer2 m_AudioItems;
    //private DvMediaContainer2 m_ImageItems;
    //private DvMediaContainer2 m_Playlists;
    //private DvMediaContainer2 m_VideoItems;
    private Mutex m_LockRoot = new Mutex();
    private ArrayList m_MimeTypes = new ArrayList();
    private bool m_Paused;
    private MediaServerDevice2 mediaServer;
    private Hashtable permissionsTable = new Hashtable();
    private DvMediaContainer2 rootContainer;
    //private CMediaMetadataClass TheMetadataParser = null;
    private int totalDirectoryCount = 0;
    private int totalFileCount = 0;
    private UTF8Encoding UTF8 = new UTF8Encoding();
    private Hashtable watcherTable = new Hashtable();

    // Events
    public event MediaServerCore2DebugHandler OnDebugMessage;
    //public event MediaServerCore2EventHandler OnDirectoriesChanged;
    public event MediaServerCore2EventHandler OnHttpTransfersChanged;
    public event SocketDataHandler OnSocketData;
    public event MediaServerCore2EventHandler OnStatsChanged;

    // Methods
    public MediaServerCore2(string friendlyName)
    {
      DeviceInfo info = new DeviceInfo();
      info.AllowRemoteContentManagement = true;
      info.FriendlyName = friendlyName;
      info.Manufacturer = "Intel Corporation";
      info.ManufacturerURL = "http://www.intel.com";
      info.ModelName = "XPC Media Server";
      info.ModelDescription = "Provides content through UPnP ContentDirectory service";
      info.ModelURL = "http://www.intel.com/upnp/MediaServerDevice2";
      info.ModelNumber = "0.765";
      info.LocalRootDirectory = "";
      Tags instance = Tags.GetInstance();
      info.SearchCapabilities = "dc:title,dc:creator,upnp:class,upnp:album,res@protocolInfo,res@size,res@bitrate";
      info.SortCapabilities = "dc:title,dc:creator,upnp:class,upnp:album";
      info.EnableSearch = true;
      info.CacheTime = CacheTime;
      info.CustomUDN = CustomUDN;
      info.INMPR03 = INMPR;
      MediaObject.ENCODE_UTF8 = false;
      mediaServer = new MediaServerDevice2(info, null, true, "http-get:*:*:*", "");
      mediaServer.OnStatsChanged += StatsChangedChangedSink;
      mediaServer.OnHttpTransfersChanged += HttpTransfersChangedSink;
      mediaServer.OnFileNotMapped = Handle_OnRequestUnmappedFile;
      mediaServer.OnRequestAddBranch = Handle_OnRequestAddBranch;
      mediaServer.OnRequestRemoveBranch = Handle_OnRequestRemoveBranch;
      mediaServer.OnRequestChangeMetadata = Handle_OnRequestChangeMetadata;
      mediaServer.OnRequestSaveBinary = Handle_OnRequestSaveBinary;
      mediaServer.OnRequestDeleteBinary = Handle_OnRequestDeleteBinary;

      ResetCoreRoot();
      m_DeviceWatcher = new UPnPDeviceWatcher(mediaServer._Device);
      m_DeviceWatcher.OnSniff += Sink_DeviceWatcherSniff;
      mediaServer.Start();
      m_Paused = false;
    }

    public DvMediaContainer2 AddDirectory(DvMediaContainer2 parent, string subfolder)
    {
      MediaBuilder.container info = new MediaBuilder.container(subfolder);
      DvMediaContainer2 newContainer = DvMediaBuilder2.CreateContainer(info);
      if (parent == null)
      {
        rootContainer.AddObject(newContainer, true);
      }
      else
      {
        parent.AddObject(newContainer, true);
      }
      return newContainer;
    }
#if NOTUSED
    public IDvMedia AddFile(DvMediaContainer2 parent, string fileName)
    {
      IDvMedia mediaItem = CreateObjFromFile(new FileInfo(fileName), new ArrayList());
      parent.AddObject(mediaItem, true);
      return mediaItem;
    }
    public bool AddDirectory(DirectoryInfo directory)
    {
      bool flag = false;
      m_LockRoot.WaitOne();
      Exception innerException = null;
      try
      {
        flag = AddDirectoryEx(rootContainer, directory);
      }
      catch (Exception exception2)
      {
        innerException = exception2;
      }
      m_LockRoot.ReleaseMutex();
      if (innerException != null)
      {
        throw new ApplicationException("AddDirectory() Error", innerException);
      }
      return flag;
    }

    private bool AddDirectoryEx(DvMediaContainer2 container, DirectoryInfo directory)
    {
      if (!directory.Exists)
      {
        return false;
      }
      MediaBuilder.storageFolder folder = new MediaBuilder.storageFolder(directory.Name);
      folder.Searchable = true;
      folder.IsRestricted = false;
      DvMediaContainer2 container2 = DvMediaBuilder2.CreateContainer(folder);
      container2.OnChildrenRemoved += new DvDelegates.Delegate_OnChildrenRemove(Sink_OnChildRemoved);
      container2.Callback_UpdateMetadata = new DvMediaContainer2.Delegate_UpdateMetadata(Sink_UpdateContainerMetadata);
      InnerMediaDirectory directory2 = new InnerMediaDirectory();
      directory2.directory = directory;
      directory2.directoryname = directory.FullName;
      directory2.watcher = new FileSystemWatcher(directory.FullName);
      directory2.watcher.Changed += new FileSystemEventHandler(OnDirectoryChangedSink);
      directory2.watcher.Created += new FileSystemEventHandler(OnDirectoryCreatedSink);
      directory2.watcher.Deleted += new FileSystemEventHandler(OnDirectoryDeletedSink);
      directory2.watcher.Renamed += new RenamedEventHandler(OnFileSystemRenameSink);
      directory2.restricted = true;
      directory2.readOnly = true;
      watcherTable.Add(directory2.watcher, container2);
      directory2.watcher.EnableRaisingEvents = true;
      container2.Tag = directory2;
      FileInfo[] files = directory.GetFiles();
      ArrayList newObjects = new ArrayList(files.Length);
      foreach (FileInfo info in files)
      {
        IDvMedia media = CreateObjFromFile(info, new ArrayList());
        if (media != null)
        {
          newObjects.Add(media);
          totalFileCount++;
        }
      }
      container2.AddObjects(newObjects, true);
      container.AddObject(container2, true);
      foreach (IDvMedia media2 in newObjects)
      {
        if (media2.Class.IsA(MediaBuilder.StandardMediaClasses.AudioItem))
        {
          m_AudioItems.AddReference((DvMediaItem2)media2);
        }
        else if (media2.Class.IsA(MediaBuilder.StandardMediaClasses.ImageItem))
        {
          m_ImageItems.AddReference((DvMediaItem2)media2);
        }
        else if (media2.Class.IsA(MediaBuilder.StandardMediaClasses.VideoItem))
        {
          m_VideoItems.AddReference((DvMediaItem2)media2);
        }
        else if (media2.Class.IsA(MediaBuilder.StandardMediaClasses.PlaylistContainer))
        {
          DvMediaContainer2 container3 = (DvMediaContainer2)media2;
          MediaBuilder.playlistContainer container4 = new MediaBuilder.playlistContainer(container3.Title);
          DvMediaContainer2 newObject = DvMediaBuilder2.CreateContainer(container4);
          foreach (DvMediaResource resource in container3.Resources)
          {
            ResourceBuilder.AllResourceAttributes attribs = new ResourceBuilder.AllResourceAttributes();
            attribs.contentUri = resource.ContentUri;
            foreach (string str in resource.ValidAttributes)
            {
              object obj2 = resource[str];
              switch (((_RESATTRIB)Enum.Parse(typeof(_RESATTRIB), str, true)))
              {
                case _RESATTRIB.protocolInfo:
                  attribs.protocolInfo = new ProtocolInfoString(((ProtocolInfoString)obj2).ToString());
                  break;

                case _RESATTRIB.size:
                  attribs.size = (_ULong)obj2;
                  break;

                case _RESATTRIB.duration:
                  attribs.duration = (_TimeSpan)obj2;
                  break;

                case _RESATTRIB.bitrate:
                  attribs.bitrate = (_UInt)obj2;
                  break;

                case _RESATTRIB.sampleFrequency:
                  attribs.sampleFrequency = (_UInt)obj2;
                  break;

                case _RESATTRIB.bitsPerSample:
                  attribs.bitsPerSample = (_UInt)obj2;
                  break;

                case _RESATTRIB.nrAudioChannels:
                  attribs.nrAudioChannels = (_UInt)obj2;
                  break;

                case _RESATTRIB.resolution:
                  attribs.resolution = (ImageDimensions)obj2;
                  break;

                case _RESATTRIB.colorDepth:
                  attribs.colorDepth = (_UInt)obj2;
                  break;

                case _RESATTRIB.protection:
                  attribs.protection = (string)obj2;
                  break;
              }
            }
            DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, false);
            addThis.AllowImport = resource.AllowImport;
            addThis.CheckAutomapFileExists = resource.CheckAutomapFileExists;
            addThis.HideContentUri = resource.HideContentUri;
            addThis.MakeStreamAtHttpGetTime = resource.MakeStreamAtHttpGetTime;
            addThis.Tag = resource.Tag;
            newObject.AddResource(addThis);
          }
          foreach (DvMediaItem2 item in container3.CompleteList)
          {
            newObject.AddReference(item);
          }
          m_Playlists.AddObject(newObject, true);
        }
      }
      DirectoryInfo[] directories = directory.GetDirectories();
      foreach (DirectoryInfo info2 in directories)
      {
        AddDirectoryEx(container2, info2);
      }
      totalDirectoryCount++;
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
      return true;
    }


    private DvMediaResource BuildM3uResource(FileInfo file, string protInfo)
    {
      ResourceBuilder.AllResourceAttributes attribs = new ResourceBuilder.AllResourceAttributes();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName + "?format=m3u";
      attribs.protocolInfo = new ProtocolInfoString(protInfo);
      DvMediaResource resource = DvResourceBuilder.CreateResource(attribs, true);
      resource.AllowImport = false;
      resource.MakeStreamAtHttpGetTime = true;
      resource.Tag = file;
      return resource;
    }

    private DvMediaItem2 CreateAudioItemFromFormatedNameFile(FileInfo file)
    {
      string str;
      string str2;
      string str5;
      string str6;
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      DText text = new DText();
      text.ATTRMARK = "-";
      text[0] = fileNameWithoutExtension;
      if (text.DCOUNT() == 1)
      {
        str6 = "";
        str5 = text[1].Trim();
      }
      else
      {
        str6 = text[1].Trim();
        str5 = text[2].Trim();
      }
      MediaBuilder.audioItem info = new MediaBuilder.audioItem(str5);
      info.creator = str6;
      DvMediaItem2 item2 = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.VideoItem attribs = new ResourceBuilder.VideoItem();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      attribs.size = new _ULong((ulong)file.Length);
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item2.AddResource(addThis);
      return item2;
    }

    private DvMediaItem2 CreateItemFromCdsLink(FileInfo file)
    {
      if (!file.Exists)
      {
        return null;
      }
      StreamReader reader = File.OpenText(file.FullName);
      string didlLiteXml = reader.ReadToEnd();
      reader.Close();
      ArrayList list = MediaBuilder.BuildMediaBranches(didlLiteXml, typeof(DvMediaItem2), typeof(DvMediaContainer2));
      if (list.Count != 1)
      {
        return null;
      }
      if (list[0].GetType() != typeof(DvMediaItem2))
      {
        return null;
      }
      return (DvMediaItem2)list[0];
    }

    private DvMediaItem2 CreateItemFromFormatedNameFile(FileInfo file)
    {
      string str;
      string str2;
      string str5;
      string str6;
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      DText text = new DText();
      text.ATTRMARK = "-";
      text[0] = fileNameWithoutExtension;
      if (text.DCOUNT() == 1)
      {
        str6 = "";
        str5 = text[1].Trim();
      }
      else
      {
        str6 = text[1].Trim();
        str5 = text[2].Trim();
      }
      MediaBuilder.item info = new MediaBuilder.item(str5);
      info.creator = str6;
      DvMediaItem2 item2 = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.VideoItem attribs = new ResourceBuilder.VideoItem();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      attribs.size = new _ULong((ulong)file.Length);
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item2.AddResource(addThis);
      return item2;
    }

    private DvMediaItem2 CreateItemFromGenericAudioFile(FileInfo file)
    {
      string str;
      string str2;
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      string name = file.Directory.Name;
      MediaBuilder.audioItem info = new MediaBuilder.audioItem(fileNameWithoutExtension);
      info.creator = name;
      DvMediaItem2 item2 = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.VideoItem attribs = new ResourceBuilder.VideoItem();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      attribs.size = new _ULong((ulong)file.Length);
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item2.AddResource(addThis);
      return item2;
    }

    private DvMediaItem2 CreateItemFromGenericFile(FileInfo file)
    {
      string str;
      string str2;
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      string name = file.Directory.Name;
      MediaBuilder.item info = new MediaBuilder.item(fileNameWithoutExtension);
      info.creator = name;
      DvMediaItem2 item2 = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.VideoItem attribs = new ResourceBuilder.VideoItem();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      attribs.size = new _ULong((ulong)file.Length);
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item2.AddResource(addThis);
      return item2;
    }

    private DvMediaItem2 CreateItemFromGenericVideoFile(FileInfo file)
    {
      string str;
      string str2;
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      string name = file.Directory.Name;
      string str6 = null;
      long ticks = 0L;
      int num2 = 0;
      long length = file.Length;
      switch (fileNameWithoutExtension)
      {
        case null:
        case "":
          fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
          name = file.Directory.Name;
          break;
      }
      MediaBuilder.videoItem info = new MediaBuilder.videoItem(fileNameWithoutExtension);
      info.creator = name;
      if (str6 != null)
      {
        info.genre = new string[] { str6 };
      }
      DvMediaItem2 item2 = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.AllResourceAttributes attribs = new ResourceBuilder.AllResourceAttributes();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      attribs.size = new _ULong((ulong)length);
      if (num2 > 0)
      {
        attribs.bitrate = new _UInt((uint)(num2 / 8));
      }
      if (ticks > 0L)
      {
        attribs.duration = new _TimeSpan(new TimeSpan(ticks));
      }
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item2.AddResource(addThis);
      return item2;
    }

    private DvMediaItem2 CreateItemFromImageFile(FileInfo file)
    {
      string str;
      string str2;
      string str4;
      string str6;
      string str7;
      string str8;
      if (!file.Exists)
      {
        return null;
      }
      using (Image image = Image.FromFile(file.FullName))
      {
        if (image == null)
        {
          return null;
        }
        MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
        string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
        str4 = str4 = Path.GetFileNameWithoutExtension(file.Name);
        string name = file.Directory.Name;
        MediaBuilder.photo info = new MediaBuilder.photo(str4);
        info.creator = name;
        info.date = new _DateTime(file.CreationTime);
        DvMediaItem2 item = DvMediaBuilder2.CreateItem(info);
        ResourceBuilder.ImageItem attribs = new ResourceBuilder.ImageItem();
        attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
        attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
        attribs.size = new _ULong((ulong)file.Length);
        attribs.colorDepth = new _UInt(GetColorDepth(image.PixelFormat));
        attribs.resolution = new ImageDimensions(image.Width, image.Height);
        DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
        addThis.Tag = file;
        item.AddResource(addThis);
        MimeTypes.ExtensionToMimeType(".jpg", out str6, out str7);
        if (str != str6)
        {
          str8 = "image/jpeg";
          StringBuilder builder = new StringBuilder(200);
          builder.AppendFormat("{0}?{3}={1},{2}", new object[] { attribs.contentUri, image.Width, image.Height, str8 });
          attribs.contentUri = builder.ToString();
          attribs.protocolInfo = new ProtocolInfoString(new StringBuilder().AppendFormat("http-get:*:{0}:*", str8).ToString());
          DvMediaResource resource2 = DvResourceBuilder.CreateResource(attribs, true);
          resource2.MakeStreamAtHttpGetTime = true;
          resource2.OverrideFileExtenstion = ".jpg";
          item.AddResource(resource2);
        }
        float zoomFactor = GetZoomFactor(80, image);
        if (zoomFactor > 0.01)
        {
          int width = (int)(image.Width * zoomFactor);
          int height = (int)(image.Height * zoomFactor);
          str8 = "image/jpeg";
          StringBuilder builder2 = new StringBuilder(200);
          builder2.AppendFormat("{0}?{3}={1},{2}", new object[] { attribs.contentUri, width, height, str8 });
          attribs.contentUri = builder2.ToString();
          attribs.protocolInfo = new ProtocolInfoString(new StringBuilder().AppendFormat("http-get:*:{0}:*", str8).ToString());
          attribs.resolution = new ImageDimensions(width, height);
          DvMediaResource resource3 = DvResourceBuilder.CreateResource(attribs, true);
          resource3.MakeStreamAtHttpGetTime = true;
          resource3.OverrideFileExtenstion = ".jpg";
          item.AddResource(resource3);
        }
        return item;
      }
    }

    private DvMediaItem2 CreateItemFromMp3WmaFile(FileInfo file)
    {
      string str5;
      string str6;
      if (!file.Exists)
      {
        return null;
      }
      string fileNameWithoutExtension = null;
      string str2 = null;
      string str3 = null;
      string str4 = null;
      int num = -1;
      long num2 = -1L;
      long num3 = -1L;
      try
      {
        if (TheMetadataParser == null)
        {
          TheMetadataParser = new CMediaMetadataClass();
        }
        TheMetadataParser.ParseMetadata_WindowsMediaPlayerFriendly(file.FullName, out fileNameWithoutExtension, out str2, out str3, out str4, out num2, out num, out num3);
      }
      catch (Exception)
      {
        fileNameWithoutExtension = str2 = str3 = (string)(str4 = null);
        num3 = num2 = num = -1;
      }
      MimeTypes.ExtensionToMimeType(file.Extension, out str5, out str6);
      string protocolInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str5).ToString();
      if ((fileNameWithoutExtension == null) || (fileNameWithoutExtension.Length == 0))
      {
        fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      }
      if (str2 == null)
      {
        str2 = "-Unknown-";
      }
      MediaBuilder.musicTrack info = new MediaBuilder.musicTrack(fileNameWithoutExtension);
      info.creator = str2;
      if ((str3 != null) && (str3.Length > 0))
      {
        info.album = new string[] { str3 };
      }
      if ((str4 != null) && (str4.Length > 0))
      {
        info.genre = new string[] { str4 };
      }
      info.date = new _DateTime(file.CreationTime);
      DvMediaItem2 item = DvMediaBuilder2.CreateItem(info);
      ResourceBuilder.MusicTrack attribs = new ResourceBuilder.MusicTrack();
      attribs.contentUri = MediaResource.AUTOMAPFILE + file.FullName;
      attribs.protocolInfo = new ProtocolInfoString(protocolInfo);
      if (num3 >= 0L)
      {
        attribs.size = new _ULong((ulong)num3);
      }
      if (num >= 0)
      {
        attribs.bitrate = new _UInt((uint)(num / 8));
      }
      if (num2 >= 0L)
      {
        attribs.duration = new _TimeSpan(new TimeSpan(num2));
      }
      DvMediaResource addThis = DvResourceBuilder.CreateResource(attribs, true);
      addThis.Tag = file;
      item.AddResource(addThis);
      return item;
    }

    private DvMediaContainer2 CreateM3uPlaylistContainer(FileInfo file, ArrayList childPlaylists)
    {
      string str;
      string str2;
      if (!childPlaylists.Contains(file.Name))
      {
        childPlaylists.Add(file.Name);
      }
      else
      {
        return null;
      }
      MimeTypes.ExtensionToMimeType(file.Extension, out str, out str2);
      string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
      string protInfo = new StringBuilder().AppendFormat("http-get:*:{0}:*", str).ToString();
      MediaBuilder.playlistContainer container = new MediaBuilder.playlistContainer(fileNameWithoutExtension);
      DvMediaContainer2 container2 = DvMediaBuilder2.CreateContainer(container);
      container2.Tag = file;
      StreamReader reader = File.OpenText(file.FullName);
      while (reader.Peek() > -1)
      {
        string path = reader.ReadLine();
        if (!Directory.Exists(path) && File.Exists(path))
        {
          FileInfo info = new FileInfo(path);
          IDvMedia media = CreateObjFromFile(info, childPlaylists);
          media.WriteStatus = EnumWriteStatus.NOT_WRITABLE;
          media.IsRestricted = true;
          if (media != null)
          {
            if (media.IsItem)
            {
              DvMediaItem2 newObject = (DvMediaItem2)media;
              container2.AddObject(newObject, true);
            }
            else
            {
              DvMediaContainer2 container3 = (DvMediaContainer2)media;
              IList completeList = container3.CompleteList;
              container3.RemoveObjects(completeList);
              container2.AddObjects(completeList, true);
            }
          }
        }
      }
      reader.Close();
      DvMediaResource addThis = BuildM3uResource(file, protInfo);
      container2.AddResource(addThis);
      return container2;
    }

    private IDvMedia CreateObjFromFile(FileInfo file, ArrayList childPlaylists)
    {
      IDvMedia media = null;
      string extension = file.Extension.ToUpper();
      string mime = null;
      string mediaClass = null;
      switch (extension)
      {
        case ".ASF":
        case ".AVI":
        case ".WMV":
        case ".MPEG":
        case ".MPEG2":
        case ".MPG":
          media = CreateItemFromGenericVideoFile(file);
          break;

        case ".WAV":
        case ".WMA":
        case ".MP3":
          {
            DvMediaItem2 item = CreateItemFromMp3WmaFile(file);
            if (item == null)
            {
              item = CreateAudioItemFromFormatedNameFile(file);
            }
            media = item;
            break;
          }
        case ".M3U":
          media = CreateM3uPlaylistContainer(file, childPlaylists);
          break;

        case ".ASX":
          break;

        case ".GIF":
        case ".JPG":
        case ".BMP":
        case ".TIF":
        case ".PNG":
          media = CreateItemFromImageFile(file);
          break;

        case ".CDSLNK":
          media = CreateItemFromCdsLink(file);
          break;

        default:
          media = CreateItemFromGenericFile(file);
          break;
      }
      if (media != null)
      {
        MimeTypes.ExtensionToMimeType(extension, out mime, out mediaClass);
      }
      if (mime != null)
      {
        if (m_MimeTypes.Contains(mime))
        {
          return media;
        }
        m_MimeTypes.Add(mime);
        ProtocolInfoString[] strArray = new ProtocolInfoString[m_MimeTypes.Count];
        for (int i = 0; i < m_MimeTypes.Count; i++)
        {
          strArray[i] = new ProtocolInfoString("http-get:*:" + m_MimeTypes[i].ToString() + ":*");
        }
        mediaServer.SourceProtocolInfoSet = strArray;
      }
      return media;
    }
    private void AdjustContainer(DvMediaContainer2 c)
    {
      string str;
      InnerMediaDirectory tag = c.Tag as InnerMediaDirectory;
      bool flag = false;
      if (!c.IsRootContainer && (tag != null))
      {
        tag.directory = new DirectoryInfo(tag.directoryname);
        if (!tag.directory.Exists)
        {
          c.Parent.RemoveObject(c);
        }
        else
        {
          flag = true;
        }
      }
      foreach (IDvMedia media in c.CompleteList)
      {
        if (media.IsContainer)
        {
          AdjustContainer((DvMediaContainer2)media);
        }
        else if (!media.IsReference)
        {
          foreach (IDvResource resource in media.Resources)
          {
            if (resource.ContentUri.StartsWith(MediaResource.AUTOMAPFILE))
            {
              str = resource.ContentUri.Remove(0, MediaResource.AUTOMAPFILE.Length);
              if (str.LastIndexOf('?') >= 0)
              {
                str = str.Substring(0, str.LastIndexOf('?'));
              }
              if (!File.Exists(str))
              {
                media.RemoveResource(resource);
              }
            }
          }
          if (media.Resources.Length == 0)
          {
            media.Parent.RemoveObject(media);
          }
        }
      }
      if (flag)
      {
        bool flag2;
        totalDirectoryCount++;
        DirectoryInfo[] directories = tag.directory.GetDirectories();
        FileInfo[] files = tag.directory.GetFiles();
        IList containers = c.Containers;
        IList items = c.Items;
        foreach (DirectoryInfo info in directories)
        {
          flag2 = false;
          foreach (IDvContainer container in containers)
          {
            InnerMediaDirectory directory2 = container.Tag as InnerMediaDirectory;
            if ((directory2 != null) && (string.Compare(info.FullName, directory2.directoryname, true) == 0))
            {
              flag2 = true;
              break;
            }
          }
          if (!flag2)
          {
            AddDirectoryEx(c, info);
          }
        }
        foreach (FileInfo info2 in files)
        {
          flag2 = false;
          foreach (IDvItem item in items)
          {
            if (!item.IsReference)
            {
              bool flag3 = false;
              IMediaResource[] resources = item.Resources;
              int index = 0;
              while (index < resources.Length)
              {
                IDvResource resource = (IDvResource)resources[index];
                str = resource.ContentUri.Remove(0, MediaResource.AUTOMAPFILE.Length);
                if (str.LastIndexOf('?') >= 0)
                {
                  str = str.Substring(0, str.LastIndexOf('?'));
                }
                if (string.Compare(str, info2.FullName, true) == 0)
                {
                  flag3 = true;
                }
                break;
              }
              if (flag3)
              {
                flag2 = true;
                break;
              }
            }
          }
          foreach (IDvContainer container in containers)
          {
            if (container.Class.IsA(MediaBuilder.StandardMediaClasses.PlaylistContainer))
            {
              FileInfo info3 = container.Tag as FileInfo;
              if ((info3 != null) && (string.Compare(info3.FullName, info2.FullName, true) == 0))
              {
                flag2 = true;
              }
            }
          }
          if (!flag2)
          {
            IDvMedia newObject = CreateObjFromFile(info2, new ArrayList());
            c.AddObject(newObject, true);
          }
          totalFileCount++;
        }
        tag.watcher = new FileSystemWatcher(tag.directory.FullName);
        tag.watcher.Changed += new FileSystemEventHandler(OnDirectoryChangedSink);
        tag.watcher.Created += new FileSystemEventHandler(OnDirectoryCreatedSink);
        tag.watcher.Deleted += new FileSystemEventHandler(OnDirectoryDeletedSink);
        tag.watcher.Renamed += new RenamedEventHandler(OnFileSystemRenameSink);
      }
    }
#endif
    public void ChangePauseState()
    {
      if (IsPaused)
      {
        m_Paused = false;
        mediaServer.Start();
      }
      else
      {
        m_Paused = true;
        mediaServer.Stop();
      }
    }

    private void ClearContentHierarchy()
    {
      IList completeList = rootContainer.CompleteList;
      rootContainer.RemoveObjects(completeList);
      totalDirectoryCount = 0;
      totalFileCount = 0;
      MediaBuilder.SetNextID(0L);
    }

    private void CreateResources(ICollection resources)
    {
      foreach (DvMediaResource resource in resources)
      {
        if (resource.ContentUri.StartsWith(MediaResource.AUTOMAPFILE))
        {
          if (File.Exists(resource.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length)))
          {
            resource.AllowImport = false;
            resource.HideContentUri = false;
          }
          else
          {
            resource.AllowImport = true;
            resource.HideContentUri = true;
          }
        }
      }
    }

    private void Debug(string msg)
    {
      if (OnDebugMessage != null)
      {
        OnDebugMessage(this, msg);
      }
    }

    private void DeserializeContainer(DvMediaContainer2 container, BinaryFormatter formatter, FileStream fstream)
    {
      int capacity = (int)formatter.Deserialize(fstream);
      ArrayList newObjects = new ArrayList(capacity);
      for (int i = 0; i < capacity; i++)
      {
        object obj2;
        try
        {
          obj2 = formatter.Deserialize(fstream);
        }
        catch (Exception exception)
        {
          throw new SerializationException("Error deserializing a child of containerID=\"" + container.ID + "\" Title=\"" + container.Title + "\".", exception);
        }
        DvMediaItem2 item = obj2 as DvMediaItem2;
        DvMediaContainer2 container2 = obj2 as DvMediaContainer2;
        if (container2 != null)
        {
          DeserializeContainer(container2, formatter, fstream);
          newObjects.Add(container2);
        }
        else
        {
          if (item == null)
          {
            throw new ApplicationException("The MediaServer deserialized an object that is neither a DvMediaItem2 nor a DvMediaContainer2.");
          }
          newObjects.Add(item);
        }
      }
      container.AddObjects(newObjects, true);
    }

    public void DeserializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      m_LockRoot.WaitOne();
      try
      {
        ClearContentHierarchy();
        DvMediaContainer2 newObj = (DvMediaContainer2)formatter.Deserialize(fstream);
        mediaServer.Root.UpdateObject(newObj);
        DeserializeContainer(mediaServer.Root, formatter, fstream);
        Hashtable hashtable = (Hashtable)formatter.Deserialize(fstream);
        Hashtable cache = new Hashtable();
        foreach (string str in hashtable.Keys)
        {
          string id = (string)hashtable[str];
          DvMediaItem2 descendent = mediaServer.Root.GetDescendent(id, cache) as DvMediaItem2;
          DvMediaItem2 refItem = mediaServer.Root.GetDescendent(str, cache) as DvMediaItem2;
          if ((descendent == null) || (refItem == null))
          {
            throw new NullReferenceException("At least one DvMediaItem2 is null.");
          }
          DvMediaItem2.AttachRefItem(descendent, refItem);
        }
        string newBaseID = (string)formatter.Deserialize(fstream);
        MediaBuilder.PrimeNextId(newBaseID);
#if NOTUSED
        AdjustContainer(mediaServer.Root);
#endif
      }
      catch (Exception exception2)
      {
        ClearContentHierarchy();
        throw;
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    public void Dispose()
    {
      rootContainer = null;
      mediaServer.Dispose();
      mediaServer = null;
      m_DeviceWatcher.OnSniff -= Sink_DeviceWatcherSniff;
      m_DeviceWatcher = null;
    }

    private void Handle_OnRequestAddBranch(MediaServerDevice2 sender, DvMediaContainer2 parentContainer, ref IDvMedia[] addTheseBranches)
    {
      m_LockRoot.WaitOne();
      try
      {
        if (parentContainer == mediaServer.Root)
        {
          throw new Error_RestrictedObject("Cannot create objects directly in the root container.");
        }
        if (parentContainer.IsRestricted)
        {
          throw new Error_RestrictedObject("Cannot create objects in a restricted container.");
        }
        InnerMediaDirectory tag = (InnerMediaDirectory)parentContainer.Tag;
        bool allowNewLocalResources = true;
        if (tag != null)
        {
          allowNewLocalResources = Directory.Exists(tag.directory.FullName);
        }
        foreach (IDvMedia media in addTheseBranches)
        {
          ValidateBranch(parentContainer, media, allowNewLocalResources);
        }
        foreach (IDvMedia media in addTheseBranches)
        {
          if (media.IsReference)
          {
            parentContainer.AddBranch(media);
          }
          else
          {
            ModifyLocalFileSystem(parentContainer, media);
          }
        }
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    private void Handle_OnRequestChangeMetadata(MediaServerDevice2 sender, IDvMedia oldObject, IDvMedia newObject)
    {
      m_LockRoot.WaitOne();
      try
      {
        if (oldObject.IsRestricted)
        {
        }
        if (oldObject.ID != newObject.ID)
        {
          throw new Error_ReadOnlyTag("Cannot modify ID");
        }
        if (oldObject.IsContainer != newObject.IsContainer)
        {
          throw new Error_BadMetadata("Cannot change containers into items.");
        }
        if (oldObject.IsItem != newObject.IsItem)
        {
          throw new Error_BadMetadata("Cannot change items into containers.");
        }
        if (oldObject.IsRestricted != newObject.IsRestricted)
        {
          throw new Error_ReadOnlyTag("Cannot change the \"restricted\" attribute.");
        }
        if (oldObject.IsReference || newObject.IsReference)
        {
          if (oldObject.IsReference != newObject.IsReference)
          {
            throw new Error_BadMetadata("Cannot change a reference item into a non-reference.");
          }
          DvMediaItem2 item = (DvMediaItem2)oldObject;
          DvMediaItem2 item2 = (DvMediaItem2)newObject;
          string refID = item.RefID;
          string strB = item2.RefID;
          if (string.Compare(refID, strB) != 0)
          {
            throw new Error_ReadOnlyTag("Cannot change the \"refID\" attribute.");
          }
        }
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    private void Handle_OnRequestDeleteBinary(MediaServerDevice2 sender, IDvResource res)
    {
      m_LockRoot.WaitOne();
      try
      {
        if (!res.AllowImport)
        {
        }
        if (res.ContentUri.StartsWith(MediaResource.AUTOMAPFILE))
        {
          File.Delete(res.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length));
        }
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    private void Handle_OnRequestRemoveBranch(MediaServerDevice2 sender, DvMediaContainer2 parentContainer, IDvMedia removeThisBranch)
    {
      m_LockRoot.WaitOne();
      try
      {
        parentContainer.RemoveBranch(removeThisBranch);
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    private void Handle_OnRequestSaveBinary(MediaServerDevice2 sender, IDvResource res)
    {
      m_LockRoot.WaitOne();
      try
      {
        if (!res.AllowImport)
        {
          throw new Error_AccessDenied("The resource cannot be overwritten or created.");
        }
      }
      finally
      {
        m_LockRoot.ReleaseMutex();
      }
    }

    private void Handle_OnRequestUnmappedFile(MediaServerDevice2 sender, MediaServerDevice2.FileNotMapped getPacket)
    {
      string str2;
      string str3;
      string str4;
      string str = getPacket.RequestedResource.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length);
      DText text = new DText();
      if (getPacket.RequestedResource.Owner.Class.IsA(MediaBuilder.StandardMediaClasses.ImageItem))
      {
        text.ATTRMARK = "?";
        text.MULTMARK = "=";
        text.SUBVMARK = ",";
        text[0] = str;
        str2 = text[1];
        str3 = text[2];
        str4 = text[2, 1];
        string str5 = text[2, 2];
        string s = text[2, 2, 1];
        string str7 = text[2, 2, 2];
        int num = int.Parse(s);
        int num2 = int.Parse(str7);
        int num3 = Math.Max(num, num2);
        Image image2 = Image.FromFile(str2).GetThumbnailImage(num, num2, null, IntPtr.Zero);
        getPacket.RedirectedStream = new MemoryStream();
        image2.Save(getPacket.RedirectedStream, ImageFormat.Jpeg);
      }
      else if (getPacket.RequestedResource.Owner.Class.IsA(MediaBuilder.StandardMediaClasses.PlaylistContainer) || getPacket.RequestedResource.Owner.Class.IsA(MediaBuilder.StandardMediaClasses.PlaylistItem))
      {
        text.ATTRMARK = "?";
        text.MULTMARK = "=";
        text[0] = str;
        str2 = text[1];
        str3 = text[2];
        str4 = text[2, 1];
        string localInterface = getPacket.LocalInterface;
        FileInfo tag = (FileInfo)((DvMediaResource)getPacket.RequestedResource).Tag;
        MemoryStream stream = new MemoryStream(((int)tag.Length) * 5);
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        getPacket.RedirectedStream = stream;
        writer.Write("\n");
        writer.Flush();
        stream.Position = 0L;
        DvMediaContainer2 owner = (DvMediaContainer2)getPacket.RequestedResource.Owner;
        writer.WriteLine("#EXTM3U");
        foreach (DvMediaItem2 item in owner.CompleteList)
        {
          StringBuilder builder = new StringBuilder((item.Title.Length + item.Creator.Length) + 15);
          StringBuilder builder2 = new StringBuilder(0x400);
          builder.AppendFormat("#EXTINF:-1,{0} - {1}", item.Creator.Replace("-", "_"), item.Title.Replace("-", "_"));
          writer.WriteLine(builder.ToString());
          IList mergedResources = item.MergedResources;
          DvMediaResource resource = mergedResources[0] as DvMediaResource;
          if (resource != null)
          {
            if (resource.ContentUri.StartsWith(MediaResource.AUTOMAPFILE))
            {
              builder2.AppendFormat("http://{0}/{1}{2}", localInterface, mediaServer.VirtualDirName, resource.RelativeContentUri);
            }
            else
            {
              builder2.Append(resource.ContentUri);
            }
            writer.WriteLine(builder2);
          }
          else
          {
            StringBuilder builder3 = new StringBuilder();
            if (mergedResources.Count == 0)
            {
              builder3.AppendFormat("MediaServerCore2.Handle_OnRequestUnmappedFile() encountered a media object ID=\"{0}\" Title=\"{1}\" with zero resources.", item.ID, item.Title);
            }
            else
            {
              builder3.AppendFormat("MediaServerCore2.Handle_OnRequestUnmappedFile() encountered a media object ID=\"{0}\" Title=\"{1}\" with resource that is not a DvMediaResource.", item.ID, item.Title);
            }
//            EventLogger.Log(builder3.ToString());
          }
        }
        writer.Flush();
      }
    }

    public void HttpTransfersChangedSink(MediaServerDevice2 sender)
    {
      if (OnHttpTransfersChanged != null)
      {
        OnHttpTransfersChanged(this);
      }
    }

    private void ModifyLocalFileSystem(DvMediaContainer2 branchFrom, IDvMedia branch)
    {
      DvMediaContainer2 container = branchFrom;
      IList resources = branch.Resources;
      if (branch.IsContainer)
      {
        DvMediaContainer2 container2 = (DvMediaContainer2)branch;
        if (container2.Class.ToString().StartsWith("object.container.storage"))
        {
          string path = "";
          if (container2.Tag != null)
          {
            path = container2.Tag.ToString();
          }
          DirectoryInfo info = Directory.CreateDirectory(path);
          InnerMediaDirectory directory = new InnerMediaDirectory();
          directory.directory = info;
          directory.directoryname = info.FullName;
          container2.Tag = directory;
        }
        else if (container2.Class.ToString().StartsWith("object.container"))
        {
          container2.Tag = null;
        }
        foreach (IDvMedia media in container2.CompleteList)
        {
          ModifyLocalFileSystem(container2, media);
        }
      }
    }

    private void OnDirectoryChangedSink(object sender, FileSystemEventArgs e)
    {
    }

    private void OnDirectoryCreatedSink(object sender, FileSystemEventArgs e)
    {
#if NOTUSED
      FileSystemWatcher watcher = (FileSystemWatcher)sender;
      DvMediaContainer2 container = (DvMediaContainer2)watcherTable[watcher];
      if ((container == null) && (OnDebugMessage != null))
      {
        OnDebugMessage(this, "WATCH EVENT FOR UNKNOWN CONTAINER");
      }
      if (File.Exists(e.FullPath))
      {
        container.AddBranch(CreateObjFromFile(new FileInfo(e.FullPath), new ArrayList()));
        totalFileCount++;
        Debug("File System Create: " + e.Name);
        if (OnStatsChanged != null)
        {
          OnStatsChanged(this);
        }
      }
      else if (Directory.Exists(e.FullPath))
      {
        AddDirectoryEx(container, new DirectoryInfo(e.FullPath));
        Debug("File System Dir Create: " + e.Name);
      }
#endif
    }

    private void OnDirectoryDeletedSink(object sender, FileSystemEventArgs e)
    {
      #if NOTUSED
      FileSystemWatcher watcher = (FileSystemWatcher)sender;
      DvMediaContainer2 container = (DvMediaContainer2)watcherTable[watcher];
      if ((container == null) && (OnDebugMessage != null))
      {
        OnDebugMessage(this, "WATCH EVENT FOR UNKNOWN CONTAINER");
      }
      DvMediaItem2 branch = null;
      foreach (DvMediaItem2 item2 in container.Items)
      {
        foreach (DvMediaResource resource in item2.Resources)
        {
          if (resource.ContentUri.ToLower() == e.FullPath.ToLower())
          {
            branch = item2;
            break;
          }
        }
      }
      if (branch != null)
      {
        container.RemoveBranch(branch);
        totalFileCount--;
        if (OnStatsChanged != null)
        {
          OnStatsChanged(this);
        }
        Debug("File System Delete: " + e.Name);
      }
      else
      {
        DvMediaContainer2 container2 = null;
        foreach (DvMediaContainer2 container3 in container.Containers)
        {
          InnerMediaDirectory tag = (InnerMediaDirectory)container3.Tag;
          if (tag.directory.FullName.ToLower() == e.FullPath.ToLower())
          {
            container2 = container3;
            break;
          }
        }
        if (container2 != null)
        {
          RemoveContainerEx(container2);
          container.RemoveBranch(container2);
          Debug("File System Dir Delete: " + e.Name);
        }
        else
        {
          Debug("FAILED File System Delete: " + e.Name);
        }
      }
#endif
    }

    private void OnFileSystemRenameSink(object sender, RenamedEventArgs e)
    {
#if NOTUSED
      FileSystemWatcher watcher = (FileSystemWatcher)sender;
      DvMediaContainer2 container = (DvMediaContainer2)watcherTable[watcher];
      if ((container == null) && (OnDebugMessage != null))
      {
        OnDebugMessage(this, "WATCH EVENT FOR UNKNOWN CONTAINER");
      }
      if (File.Exists(e.FullPath))
      {
        DvMediaItem2 branch = null;
        foreach (DvMediaItem2 item2 in container.Items)
        {
          foreach (DvMediaResource resource in item2.Resources)
          {
            Uri uri = new Uri(resource.ContentUri);
            if (uri.LocalPath.ToLower() == e.OldFullPath.ToLower())
            {
              branch = item2;
              break;
            }
          }
        }
        if (branch != null)
        {
          container.RemoveBranch(branch);
          container.AddBranch(CreateObjFromFile(new FileInfo(e.FullPath), new ArrayList()));
          Debug("File System Rename: " + e.OldName + "->" + e.Name);
        }
        else
        {
          Debug("FAILED File System Rename: " + e.OldName + "->" + e.Name);
        }
      }
      else if (Directory.Exists(e.FullPath))
      {
        DvMediaContainer2 container2 = null;
        foreach (DvMediaContainer2 container3 in container.Containers)
        {
          InnerMediaDirectory tag = (InnerMediaDirectory)container3.Tag;
          if (tag.directory.FullName.ToLower() == e.OldFullPath.ToLower())
          {
            container2 = container3;
            break;
          }
        }
        if (container2 != null)
        {
          RemoveContainerEx(container2);
          container.RemoveBranch(container2);
          AddDirectoryEx(container, new DirectoryInfo(e.FullPath));
          Debug("File System Dir Rename: " + e.OldName + "->" + e.Name);
        }
        else
        {
          Debug("FAILED File System Dir Rename: " + e.OldName + "->" + e.Name);
        }
      }
#endif
    }

    private void RemoveContainerEx(DvMediaContainer2 container)
    {
      InnerMediaDirectory tag = (InnerMediaDirectory)container.Tag;
      tag.watcher.EnableRaisingEvents = false;
      watcherTable.Remove(tag.watcher);
      tag.watcher.Changed -= OnDirectoryChangedSink;
      tag.watcher.Created -= OnDirectoryCreatedSink;
      tag.watcher.Deleted -= OnDirectoryDeletedSink;
      tag.watcher.Renamed -= OnFileSystemRenameSink;
      tag.watcher.Dispose();
      tag.watcher = null;
      tag.directory = null;
      RemoveInnerContainers(container);
      totalDirectoryCount--;
      totalFileCount -= container.Items.Count;
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
    }

    public bool RemoveDirectory(DirectoryInfo directory)
    {
      DvMediaContainer2 container = null;
      foreach (DvMediaContainer2 container2 in rootContainer.Containers)
      {
        InnerMediaDirectory tag = (InnerMediaDirectory)container2.Tag;
        if ((tag != null) && (directory.FullName == tag.directory.FullName))
        {
          container = container2;
          break;
        }
      }
      if (container == null)
      {
        return false;
      }
      RemoveContainerEx(container);
      rootContainer.RemoveBranch(container);
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
      return true;
    }

    private void RemoveInnerContainers(DvMediaContainer2 container)
    {
      foreach (DvMediaContainer2 container2 in container.Containers)
      {
        InnerMediaDirectory tag = container2.Tag as InnerMediaDirectory;
        if (tag != null)
        {
          tag.watcher.EnableRaisingEvents = false;
          watcherTable.Remove(tag.watcher);
          tag.watcher.Changed -= OnDirectoryChangedSink;
          tag.watcher.Created -= OnDirectoryCreatedSink;
          tag.watcher.Deleted -= OnDirectoryDeletedSink;
          tag.watcher.Renamed -= OnFileSystemRenameSink;
          tag.watcher.Dispose();
          tag.watcher = null;
          tag.directory = null;
          RemoveInnerContainers(container2);
          totalFileCount -= container2.Items.Count;
          container.RemoveBranch(container2);
          totalDirectoryCount--;
          if (OnStatsChanged != null)
          {
            OnStatsChanged(this);
          }
        }
        else if (container2.Tag.GetType() == typeof(FileInfo))
        {
          totalFileCount--;
        }
      }
    }

    public void ResetCoreRoot()
    {
      MediaBuilder.SetNextID(0L);
      rootContainer = mediaServer.Root;
      /*
      MediaBuilder.container info = new MediaBuilder.container("All Image Items");
      info.IsRestricted = true;
      MediaBuilder.container container2 = new MediaBuilder.container("All Audio Items");
      container2.IsRestricted = true;
      MediaBuilder.container container3 = new MediaBuilder.container("All Video Items");
      container3.IsRestricted = true;
      MediaBuilder.container container4 = new MediaBuilder.container("All Playlists");
      container4.IsRestricted = true;
      m_ImageItems = DvMediaBuilder2.CreateContainer(info);
      m_AudioItems = DvMediaBuilder2.CreateContainer(container2);
      m_VideoItems = DvMediaBuilder2.CreateContainer(container3);
      m_Playlists = DvMediaBuilder2.CreateContainer(container4);
      rootContainer.AddObject(m_ImageItems, true);
      rootContainer.AddObject(m_AudioItems, true);
      rootContainer.AddObject(m_VideoItems, true);
      rootContainer.AddObject(m_Playlists, true);*/
    }

    private void SerializeContainer(BinaryFormatter formatter, FileStream fstream, DvMediaContainer2 container, Hashtable refItems)
    {
      IList completeList = container.CompleteList;
      formatter.Serialize(fstream, container);
      formatter.Serialize(fstream, completeList.Count);
      foreach (IUPnPMedia media in completeList)
      {
        DvMediaContainer2 container2 = media as DvMediaContainer2;
        DvMediaItem2 graph = media as DvMediaItem2;
        if (container2 != null)
        {
          SerializeContainer(formatter, fstream, container2, refItems);
        }
        else
        {
          if (graph == null)
          {
            throw new ApplicationException("The MediaServer has a IUPnPMedia item with ID=\"" + media.ID + "\" that is neither a DvMediaItem2 nor a DvMediaContainer2.");
          }
          formatter.Serialize(fstream, graph);
          if (graph.IsReference && !string.IsNullOrEmpty(graph.RefID))
          {
            refItems[graph.ID] = graph.RefID;
          }
        }
      }
    }

    public void SerializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      m_LockRoot.WaitOne();
      Exception innerException = null;
      try
      {
        Hashtable refItems = new Hashtable();
        SerializeContainer(formatter, fstream, mediaServer.Root, refItems);
        formatter.Serialize(fstream, refItems);
        string mostRecentUniqueId = MediaBuilder.GetMostRecentUniqueId();
        formatter.Serialize(fstream, mostRecentUniqueId);
      }
      catch (Exception exception2)
      {
        innerException = exception2;
      }
      if (innerException != null)
      {
        throw new Exception("SerializeTree() error", innerException);
      }
    }

    private void Sink_DeviceWatcherSniff(byte[] raw, int offset, int length)
    {
      string socketData = UTF8.GetString(raw, offset, length);
      if (OnSocketData != null)
      {
        OnSocketData(this, socketData);
      }
    }

    private void Sink_OnChildRemoved(IDvContainer parent, ICollection removedThese)
    {
      foreach (IUPnPMedia media in removedThese)
      {
        DvMediaContainer2 container = media as DvMediaContainer2;
        if (container != null)
        {
          container.OnChildrenRemoved -= new DvDelegates.Delegate_OnChildrenRemove(Sink_OnChildRemoved);
          container.Callback_UpdateMetadata = null;
        }
      }
    }

    private void Sink_UpdateContainerMetadata(DvMediaContainer2 container)
    {
    }

    public void StatsChangedChangedSink(MediaServerDevice2 sender)
    {
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
    }

    public bool UpdatePermissions(DirectoryInfo directory, bool restricted, bool readOnly)
    {
      DvMediaContainer2 container = null;
      foreach (DvMediaContainer2 container2 in rootContainer.Containers)
      {
        InnerMediaDirectory directory2 = (InnerMediaDirectory)container2.Tag;
        if ((directory2 != null) && (directory.FullName == directory2.directory.FullName))
        {
          container = container2;
          break;
        }
      }
      if (container == null)
      {
        return false;
      }
      InnerMediaDirectory tag = (InnerMediaDirectory)container.Tag;
      tag.restricted = restricted;
      tag.readOnly = readOnly;
      UpdatePermissionsEx(container, restricted, readOnly);
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
      return true;
    }

    public void UpdatePermissionsEx(DvMediaContainer2 container, bool restricted, bool readOnly)
    {
      container.IsRestricted = restricted;
      foreach (DvMediaItem2 item in container.Items)
      {
        item.IsRestricted = restricted;
        foreach (DvMediaResource resource in item.Resources)
        {
          if (!(container.IsRestricted || item.IsRestricted))
          {
            resource.AllowImport = true;
          }
          else
          {
            resource.AllowImport = false;
          }
        }
      }
      foreach (DvMediaContainer2 container2 in container.Containers)
      {
        UpdatePermissionsEx(container2, restricted, readOnly);
      }
    }

    private void ValidateBranch(DvMediaContainer2 branchFrom, IDvMedia branch, bool allowNewLocalResources)
    {
      DvMediaContainer2 container2;
      DvMediaContainer2 container = branchFrom;
      string baseDirectory = "";
      if (allowNewLocalResources)
      {
        if (container.Tag != null)
        {
          if (container.Tag.GetType() == new InnerMediaDirectory().GetType())
          {
            InnerMediaDirectory tag = (InnerMediaDirectory)container.Tag;
            baseDirectory = tag.directory.FullName + @"\";
          }
          else if (container.Tag.GetType() == typeof(string))
          {
            baseDirectory = container.Tag + @"\";
          }
        }
        if (branch.IsContainer)
        {
          container2 = (DvMediaContainer2)branch;
          if (container2.Class.ToString().StartsWith("object.container.storage"))
          {
            container2.Tag = baseDirectory + container2.Title;
          }
          else
          {
            container2.Tag = null;
          }
        }
      }
      if (branch.IsContainer)
      {
        container2 = (DvMediaContainer2)branch;
        foreach (IDvMedia media in container2.CompleteList)
        {
          ValidateBranch(container2, media, allowNewLocalResources);
        }
      }
      else if (!branch.IsItem)
      {
        throw new Exception("Error: Could not validate branch. Branch must be a container, reference, or item.");
      }
      if (!branch.IsReference)
      {
        IList resources = branch.Resources;
        if ((resources != null) && (resources.Count > 0))
        {
          foreach (DvMediaResource resource in resources)
          {
            string str2;
            if (resource.ContentUri.StartsWith(MediaResource.AUTOMAPFILE))
            {
              string str5;
              string str6;
              if (!allowNewLocalResources)
              {
                throw new Error_BadMetadata("Cannot create local http-get resources that are descendents from the specified container.");
              }
              str2 = resource.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length);
              if (!File.Exists(str2))
              {
                throw new UPnPCustomException(0x32b, "The specified local file-uri does not exist. (" + resource.ContentUri + ")");
              }
              if (Directory.Exists(str2))
              {
                throw new UPnPCustomException(810, "The specified local file-uri is a directory. (" + resource.ContentUri + ")");
              }
              FileInfo info = new FileInfo(str2);
              string str3 = "http-get";
              string str4 = "*";
              MimeTypes.ExtensionToMimeType(info.Extension, out str5, out str6);
              string str7 = "*";
              StringBuilder builder = new StringBuilder(100);
              builder.AppendFormat("{0}:{1}:{2}:{3}", new object[] { str3, str4, str5, str7 });
              ProtocolInfoString protocolInfo = new ProtocolInfoString(builder.ToString());
              resource.SetProtocolInfo(protocolInfo);
            }
            else if ((resource.ContentUri == "") && (baseDirectory != ""))
            {
              str2 = resource.GenerateLocalFilePath(baseDirectory);
              resource.SetContentUri(str2);
            }
            else
            {
              if (resource.ContentUri == "")
              {
                throw new Error_RestrictedObject("The container specified does not allow creation of storage containers or resources.");
              }
              string importUri = resource.ImportUri;
            }
          }
        }
      }
    }

    // Properties
    public IList Directories
    {
      get
      {
        ArrayList list = new ArrayList();
        foreach (DvMediaContainer2 container in rootContainer.Containers)
        {
          InnerMediaDirectory tag = (InnerMediaDirectory)container.Tag;
          if (tag != null)
          {
            SharedDirectoryInfo info = new SharedDirectoryInfo();
            info.directory = tag.directoryname;
            info.readOnly = tag.readOnly;
            info.restricted = tag.restricted;
            list.Add(info);
          }
        }
        return list;
      }
    }

    public IList HttpTransfers
    {
      get { return mediaServer.HttpTransfers; }
    }

    public bool IsPaused
    {
      get { return m_Paused; }
    }

    public string SearchCapabilities
    {
      get { return mediaServer.SearchCapabilities; }
      set { mediaServer.SearchCapabilities = value; }
    }

    public string SortCapabilities
    {
      get { return mediaServer.SortCapabilities; }
      set { mediaServer.SortCapabilities = value; }
    }

    public MediaServerDevice2.Statistics Statistics
    {
      get { return mediaServer.Stats; }
    }

    public int TotalDirectoryCount
    {
      get { return totalDirectoryCount; }
    }

    public int TotalFileCount
    {
      get { return totalFileCount; }
    }

    // Nested Types
    [Serializable]
    private class InnerMediaDirectory
    {
      // Fields
      public DirectoryInfo directory;
      public string directoryname;
      public bool readOnly;
      public bool restricted;
      [NonSerialized]
      public FileSystemWatcher watcher;
    }

    public delegate void MediaServerCore2DebugHandler(MediaServerCore2 sender, string message);

    public delegate void MediaServerCore2EventHandler(MediaServerCore2 sender);

    [Serializable]
    public class SharedDirectoryInfo
    {
      // Fields
      public string directory;
      public bool readOnly;
      public bool restricted;
    }

    public delegate void SocketDataHandler(MediaServerCore2 sender, string socketData);

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransferStruct
    {
      public bool Incoming;
      public IPEndPoint Source;
      public IPEndPoint Destination;
      public string ResourceName;
      public long ResourceLength;
      public long ResourcePosition;
    }
  }
}
