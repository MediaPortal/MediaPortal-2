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
using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using Intel.UPNP;
using Intel.Utilities;
using Intel.UPNP.AV.MediaServer;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.MediaServer.CP;
using Intel.UPNP.AV.CdsMetadata;
//using MetadataParser;

namespace MediaPortal.UPnPServer
{
  public class UPnPMediaServer2 : MarshalByRefObject
  {
    // Fields
    public int mediaHttpTransfersUpdateId = 1;
    public int mediaServerStatsUpdateId = 1;
    public int mediaSharedDirectoryUpdateId = 1;

    // Events
    public event NotifyEvent MediaHttpTransfersChanged;
    public event DebugNotifyEvent MediaServerDebugMessage;
    public event NotifyEvent MediaServerStatsChanged;

    // Methods
    public UPnPMediaServer2()
    {
      if (MediaServerCore2.serverCore != null)
      {
        MediaServerCore2.serverCore.OnStatsChanged += new MediaServerCore2.MediaServerCore2EventHandler(this.MediaServerStatsChangedSink);
        MediaServerCore2.serverCore.OnHttpTransfersChanged += new MediaServerCore2.MediaServerCore2EventHandler(this.MediaHttpTransferssChangedSink);
        MediaServerCore2.serverCore.OnDebugMessage += new MediaServerCore2.MediaServerCore2DebugHandler(this.MediaServerCore2DebugSink);
      }
    }
#if NOTUSED
    public Exception AddDirectory(DirectoryInfo directory, bool restricted, bool allowWrite)
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          return new NullReferenceException("No MediaServer object exists for the application.");
        }
        bool flag = MediaServerCore2.serverCore.AddDirectory(directory);
        this.mediaSharedDirectoryUpdateId++;
        if (this.mediaSharedDirectoryUpdateId < 0)
        {
          this.mediaSharedDirectoryUpdateId = 1;
        }
        return null;
      }
      catch (Exception exception)
      {
//        EventLogger.Log(exception);
        return exception;
      }
    }
    public IDvMedia AddFile(DvMediaContainer2 parent, string file)
    {
      return MediaServerCore2.serverCore.AddFile(parent, file);
    }
#endif
    public DvMediaContainer2 AddDirectory(DvMediaContainer2 parent, string subfolder)
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          throw new NullReferenceException("No MediaServer object exists for the application.");
        }
        DvMediaContainer2 newContainer = MediaServerCore2.serverCore.AddDirectory(parent, subfolder);
        this.mediaSharedDirectoryUpdateId++;
        if (this.mediaSharedDirectoryUpdateId < 0)
        {
          this.mediaSharedDirectoryUpdateId = 1;
        }
        return newContainer;
      }
      catch (Exception exception)
      {
//        EventLogger.Log(exception);
        throw exception;
      }
    }

    public void DeserializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      if (MediaServerCore2.serverCore != null)
      {
        MediaServerCore2.serverCore.DeserializeTree(formatter, fstream);
        this.mediaSharedDirectoryUpdateId++;
      }
    }

    ~UPnPMediaServer2()
    {
      if (MediaServerCore2.serverCore != null)
      {
        MediaServerCore2.serverCore.OnStatsChanged -= new MediaServerCore2.MediaServerCore2EventHandler(this.MediaServerStatsChangedSink);
        MediaServerCore2.serverCore.OnHttpTransfersChanged -= new MediaServerCore2.MediaServerCore2EventHandler(this.MediaHttpTransferssChangedSink);
        MediaServerCore2.serverCore.OnDebugMessage -= new MediaServerCore2.MediaServerCore2DebugHandler(this.MediaServerCore2DebugSink);
      }
    }

    public IList GetSharedDirectories()
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          return null;
        }
        return MediaServerCore2.serverCore.Directories;
      }
      catch (Exception )
      {
//        EventLogger.Log(exception);
        return null;
      }
    }

    public string[] GetSharedDirectoryNames()
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          return null;
        }
        IList directories = MediaServerCore2.serverCore.Directories;
        string[] strArray = new string[directories.Count];
        int index = 0;
        foreach (MediaServerCore2.SharedDirectoryInfo info in directories)
        {
          strArray[index] = (string)info.directory.Clone();
          index++;
        }
        return strArray;
      }
      catch (Exception )
      {
//        EventLogger.Log(exception);
        return null;
      }
    }

    private void MediaHttpTransferssChangedSink(MediaServerCore2 sender)
    {
      this.mediaHttpTransfersUpdateId++;
      if (this.mediaHttpTransfersUpdateId < 0)
      {
        this.mediaHttpTransfersUpdateId = 1;
      }
      if (this.MediaHttpTransfersChanged != null)
      {
        this.MediaHttpTransfersChanged();
      }
    }

    private void MediaServerCore2DebugSink(MediaServerCore2 sender, string msg)
    {
      if (this.MediaServerDebugMessage != null)
      {
        this.MediaServerDebugMessage(msg);
      }
    }

    private void MediaServerStatsChangedSink(MediaServerCore2 sender)
    {
      this.mediaServerStatsUpdateId++;
      if (this.mediaServerStatsUpdateId < 0)
      {
        this.mediaServerStatsUpdateId = 1;
      }
      if (this.MediaServerStatsChanged != null)
      {
        this.MediaServerStatsChanged();
      }
    }

    public bool RemoveDirectory(DirectoryInfo directory)
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          return false;
        }
        bool flag = MediaServerCore2.serverCore.RemoveDirectory(directory);
        this.mediaSharedDirectoryUpdateId++;
        if (this.mediaSharedDirectoryUpdateId < 0)
        {
          this.mediaSharedDirectoryUpdateId = 1;
        }
        return flag;
      }
      catch (Exception )
      {
//        EventLogger.Log(exception);
        return false;
      }
    }

    public void ResetTree()
    {
      if (MediaServerCore2.serverCore != null)
      {
        MediaServerCore2.serverCore.ResetCoreRoot();
      }
    }

    public void SerializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      if (MediaServerCore2.serverCore != null)
      {
        MediaServerCore2.serverCore.SerializeTree(formatter, fstream);
      }
    }

    public bool UpdatePermissions(DirectoryInfo directory, bool restricted, bool allowWrite)
    {
      try
      {
        if (MediaServerCore2.serverCore == null)
        {
          return false;
        }
        bool flag = MediaServerCore2.serverCore.UpdatePermissions(directory, restricted, allowWrite);
        this.mediaSharedDirectoryUpdateId++;
        if (this.mediaSharedDirectoryUpdateId < 0)
        {
          this.mediaSharedDirectoryUpdateId = 1;
        }
        return flag;
      }
      catch (Exception )
      {
//        EventLogger.Log(exception);
        return false;
      }
    }

    // Properties
    public IList HttpTransfers
    {
      get
      {
        try
        {
          if (MediaServerCore2.serverCore == null)
          {
            return null;
          }
          ArrayList list = new ArrayList();
          foreach (MediaServerDevice2.HttpTransfer transfer in MediaServerCore2.serverCore.HttpTransfers)
          {
            MediaServerCore2.TransferStruct struct2 = new MediaServerCore2.TransferStruct();
            struct2.Incoming = transfer.Incoming;
            struct2.Source = transfer.Source;
            struct2.Destination = transfer.Destination;
            struct2.ResourceName = transfer.Resource.ContentUri;
            struct2.ResourceLength = transfer.TransferSize;
            struct2.ResourcePosition = transfer.Position;
            list.Add(struct2);
          }
          return list;
        }
        catch (Exception )
        {
//          EventLogger.Log(exception);
          return null;
        }
      }
    }

    public int MediaHttpTransfersUpdateId
    {
      get
      {
        return this.mediaHttpTransfersUpdateId;
      }
    }

    public int MediaServerStatsUpdateId
    {
      get
      {
        return this.mediaServerStatsUpdateId;
      }
    }

    public int MediaSharedDirectoryUpdateId
    {
      get
      {
        return this.mediaSharedDirectoryUpdateId;
      }
    }

    public int TotalDirectoryCount
    {
      get
      {
        if (MediaServerCore2.serverCore == null)
        {
          return 0;
        }
        return MediaServerCore2.serverCore.TotalDirectoryCount;
      }
    }

    public int TotalFileCount
    {
      get
      {
        if (MediaServerCore2.serverCore == null)
        {
          return 0;
        }
        return MediaServerCore2.serverCore.TotalFileCount;
      }
    }

    // Nested Types
    public delegate void DebugNotifyEvent(string message);

    public delegate void NotifyEvent();
  }


}