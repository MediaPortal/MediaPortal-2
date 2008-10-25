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
using System.Runtime.Serialization.Formatters.Binary;
//using MetadataParser;

namespace Components.UPnPServer
{
  public class UPnPMediaServer2 : MarshalByRefObject
  {
    // Fields
    public int _mediaHttpTransfersUpdateId = 1;
    public int _mediaServerStatsUpdateId = 1;
    public int _mediaSharedDirectoryUpdateId = 1;
    protected MediaServerCore2 _mediaServerCore;

    // Events
    public event NotifyEvent MediaHttpTransfersChanged;
    public event DebugNotifyEvent MediaServerDebugMessage;
    public event NotifyEvent MediaServerStatsChanged;

    // Methods
    public UPnPMediaServer2(MediaServerCore2 mediaServerCore)
    {
      if (mediaServerCore == null)
        throw new NullReferenceException("MediaServer core object is null");
      _mediaServerCore = mediaServerCore;
      _mediaServerCore.OnStatsChanged += MediaServerStatsChangedSink;
      _mediaServerCore.OnHttpTransfersChanged += MediaHttpTransfersChangedSink;
      _mediaServerCore.OnDebugMessage += MediaServerCore2DebugSink;
    }

    ~UPnPMediaServer2()
    {
      _mediaServerCore.OnStatsChanged -= MediaServerStatsChangedSink;
      _mediaServerCore.OnHttpTransfersChanged -= MediaHttpTransfersChangedSink;
      _mediaServerCore.OnDebugMessage -= MediaServerCore2DebugSink;
      _mediaServerCore.Dispose();
    }

    public void Dispose()
    {
      _mediaServerCore.Dispose();
    }

#if NOTUSED
    public Exception AddDirectory(DirectoryInfo directory, bool restricted, bool allowWrite)
    {
      try
      {
        if (_mediaServerCore == null)
        {
          return new NullReferenceException("No MediaServer object exists for the application.");
        }
        bool flag = _mediaServerCore.AddDirectory(directory);
        mediaSharedDirectoryUpdateId++;
        if (mediaSharedDirectoryUpdateId < 0)
        {
          mediaSharedDirectoryUpdateId = 1;
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
      return _mediaServerCore.AddFile(parent, file);
    }
#endif
    public DvMediaContainer2 AddDirectory(DvMediaContainer2 parent, string subfolder)
    {
      DvMediaContainer2 newContainer = _mediaServerCore.AddDirectory(parent, subfolder);
      _mediaSharedDirectoryUpdateId++;
      if (_mediaSharedDirectoryUpdateId < 0)
      {
        _mediaSharedDirectoryUpdateId = 1;
      }
      return newContainer;
    }

    public void DeserializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      _mediaServerCore.DeserializeTree(formatter, fstream);
      _mediaSharedDirectoryUpdateId++;
    }

    public IList GetSharedDirectories()
    {
      try
      {
        return _mediaServerCore.Directories;
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
        IList directories = _mediaServerCore.Directories;
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

    private void MediaHttpTransfersChangedSink(MediaServerCore2 sender)
    {
      _mediaHttpTransfersUpdateId++;
      if (_mediaHttpTransfersUpdateId < 0)
      {
        _mediaHttpTransfersUpdateId = 1;
      }
      if (MediaHttpTransfersChanged != null)
      {
        MediaHttpTransfersChanged();
      }
    }

    private void MediaServerCore2DebugSink(MediaServerCore2 sender, string msg)
    {
      if (MediaServerDebugMessage != null)
      {
        MediaServerDebugMessage(msg);
      }
    }

    private void MediaServerStatsChangedSink(MediaServerCore2 sender)
    {
      _mediaServerStatsUpdateId++;
      if (_mediaServerStatsUpdateId < 0)
      {
        _mediaServerStatsUpdateId = 1;
      }
      if (MediaServerStatsChanged != null)
      {
        MediaServerStatsChanged();
      }
    }

    public bool RemoveDirectory(DirectoryInfo directory)
    {
      try
      {
        bool flag = _mediaServerCore.RemoveDirectory(directory);
        _mediaSharedDirectoryUpdateId++;
        if (_mediaSharedDirectoryUpdateId < 0)
        {
          _mediaSharedDirectoryUpdateId = 1;
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
      _mediaServerCore.ResetCoreRoot();
    }

    public void SerializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      _mediaServerCore.SerializeTree(formatter, fstream);
    }

    public bool UpdatePermissions(DirectoryInfo directory, bool restricted, bool allowWrite)
    {
      try
      {
        bool flag = _mediaServerCore.UpdatePermissions(directory, restricted, allowWrite);
        _mediaSharedDirectoryUpdateId++;
        if (_mediaSharedDirectoryUpdateId < 0)
        {
          _mediaSharedDirectoryUpdateId = 1;
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
          ArrayList list = new ArrayList();
          foreach (MediaServerDevice2.HttpTransfer transfer in _mediaServerCore.HttpTransfers)
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
      get { return _mediaHttpTransfersUpdateId; }
    }

    public int MediaServerStatsUpdateId
    {
      get { return _mediaServerStatsUpdateId; }
    }

    public int MediaSharedDirectoryUpdateId
    {
      get { return _mediaSharedDirectoryUpdateId; }
    }

    public int TotalDirectoryCount
    {
      get { return _mediaServerCore.TotalDirectoryCount; }
    }

    public int TotalFileCount
    {
      get { return _mediaServerCore.TotalFileCount; }
    }

    // Nested Types
    public delegate void DebugNotifyEvent(string message);

    public delegate void NotifyEvent();
  }
}
