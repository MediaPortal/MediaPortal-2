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

// Todo: 
// Obsolete. Merge parts of this into new player and then remove:
// - stream events
// - "resumeAt" and "duration" metadata

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.Players;

using MediaPortal.Media.MediaManagement;

using Un4seen.Bass;
using Un4seen.Bass.AddOn.Cd;
using Un4seen.Bass.AddOn.Mix;

namespace Media.Players.BassPlayer
{
  public class _Stream
  {
    #region Enum
    private enum FileType
    {
      File = 0,
      Url = 1,
      LastFM = 2,
      CD = 3,
      Mod = 4,
    }
    #endregion

    #region Delegates
    private SYNCPROC PlaybackFadeOutProcDelegate = null;      // SyncProc called to initiate Crossfading
    private SYNCPROC PlaybackEndProcDelegate = null;          // SyncProc called to indicate the song has ended
    //private SYNCPROC MetaTagSyncProcDelegate = null;          // SyncProc indicating that Meta�data has been sent via an Internet Stream
    //private DOWNLOADPROC LastFmDownloadProcDelegate = null;   // Download Proc, when playing a last.fm stream
    #endregion


    #region Variables
    private int _stream;
    private IMediaItem _mediaItem;
    private BassPlayerSettings _settings;
    private FileType _filetype;

    private int _resumeAt = 0;                                // Playback should resume at the specified position
    private int _duration = 0;                                // The Duration of the Playback (needed for Cue file playback)

    private int _syncHandleFade = 0;                          // The Synchandle for Crossfading
    private int _syncHandleEnd = 0;                           // The Synchandle when a Stream Ends
    #endregion

    #region Constructor
    public _Stream(IMediaItem mediaitem, BassPlayerSettings settings)
    {
      _mediaItem = mediaitem;
      _settings = settings;
      CreateStream();
    }
    #endregion

    #region Properties
    public int ID
    {
      get { return _stream; }
    }

    public IMediaItem MediaItem
    {
      get { return _mediaItem; }
    }
    #endregion

    #region Private Methods
    private void CreateStream()
    {
      BASSFlag streamFlags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;
      _filetype = GetFileType();
      ServiceScope.Get<ILogger>().Info("BASS: Creating stream for {0}. FileType: {1}", _mediaItem.ContentUri.AbsoluteUri, Enum.GetName(typeof(FileType), _filetype));
      switch (_filetype)
      {
        case FileType.File:
          _stream = Bass.BASS_StreamCreateFile(_mediaItem.ContentUri.LocalPath, 0, 0, streamFlags);
          break;
        case FileType.Mod:
          _stream = Bass.BASS_MusicLoad(_mediaItem.ContentUri.LocalPath, 0, 0, BASSFlag.BASS_SAMPLE_SOFTWARE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MUSIC_AUTOFREE | BASSFlag.BASS_MUSIC_PRESCAN, 0);
          break;
        case FileType.CD:
          _stream = BassCd.BASS_CD_StreamCreateFile(_mediaItem.ContentUri.LocalPath, streamFlags);
          break;
        case FileType.LastFM:
        case FileType.Url:
          _stream = Bass.BASS_StreamCreateURL(_mediaItem.ContentUri.ToString(), 0, streamFlags, null, IntPtr.Zero);
          break;
      }

      if (_stream != 0)
      {
        // Do we need to resume at a specific position. e.g. Processing a Cue File
        try
        {
          _resumeAt = (int)_mediaItem.MetaData["resumeAt"];
        }
        catch (Exception)
        {
          _resumeAt = 0;
        }
        try
        {
          _duration = (int)_mediaItem.MetaData["duration"];
        }
        catch (Exception)
        {
          _duration = 0;
        }
        
        if (_resumeAt > 0)
        {
          Bass.BASS_ChannelSetPosition(_stream, Bass.BASS_ChannelSeconds2Bytes(_stream, (float)_resumeAt / 1000f));
        }

        // ToDo: Apply DSP Processing etc.

        RegisterPlaybackEvents();
      }
      else
      {
        int error = (int)Bass.BASS_ErrorGetCode();
        ServiceScope.Get<ILogger>().Error("BASS: Error creating stream for {0}. Error: {1}", _mediaItem.ContentUri.AbsoluteUri, Enum.GetName(typeof(BASSError), error));
      }
    }

    private FileType GetFileType()
    {
      if (_mediaItem.ContentUri.IsFile)
      {
        if (IsMODFile(_mediaItem.ContentUri.LocalPath))
          return FileType.Mod;
        else if (IsCDDA(_mediaItem.ContentUri.LocalPath))
          return FileType.CD;
        else
          return FileType.File;
      }
      else
      {
        if (IsLastFMStream(_mediaItem.ContentUri.ToString()))
          return FileType.LastFM;
        else
          return FileType.Url;
      }
    }

    /// <summary>
    /// Is this a MOD file?
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private bool IsMODFile(string filePath)
    {
      string ext = System.IO.Path.GetExtension(filePath).ToLower();

      switch (ext)
      {
        case ".mod":
        case ".mo3":
        case ".it":
        case ".xm":
        case ".s3m":
        case ".mtm":
        case ".umx":
          return true;

        default:
          return false;
      }
    }

    private bool IsLastFMStream(string filePath)
    {
      if (filePath.StartsWith(@"http://"))
      {
        if (filePath.IndexOf(@"/last.mp3?") > 0)
          return true;
        if (filePath.Contains(@"last.fm/"))
          return true;
      }

      return false;
    }

    private bool IsCDDA(string strFile)
    {
      if (strFile == null) return false;
      if (strFile.Length <= 0) return false;
      if (strFile.IndexOf("cdda:") >= 0) return true;
      if (strFile.IndexOf(".cda") >= 0) return true;
      return false;
    }

    private void SendInternalMessage(string action)
    {
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = action;

      Thread asyncMsgThread = new Thread(new ParameterizedThreadStart(AsyncMessageSend));
      asyncMsgThread.Start(msg);
    }

    private void AsyncMessageSend(object message)
    {
      QueueMessage msg = (QueueMessage)message;
      ServiceScope.Get<IMessageBroker>().Send("bass", msg);
    }
    #endregion


    #region SyncProcs
    /// <summary>
    /// Register the Playback Events
    /// </summary>
    private void RegisterPlaybackEvents()
    {
      ServiceScope.Get<ILogger>().Debug("BASS: Register Stream Playback events");

      // We have a Fadeout even with Gapless Playback, so that we have the next file ready on end of the first song
      PlaybackFadeOutProcDelegate = new SYNCPROC(PlaybackFadeOutProc);
      _syncHandleFade = RegisterPlaybackFadeOutEvent();

      PlaybackEndProcDelegate = new SYNCPROC(PlaybackEndProc);
      _syncHandleEnd = RegisterPlaybackEndEvent();

      ServiceScope.Get<ILogger>().Debug("BASS: Finished Registering Stream Playback events");
    }

    #region Fadeout / Crossfade Event
    /// <summary>
    /// Register the Fade out Event
    /// </summary>
    private int RegisterPlaybackFadeOutEvent()
    {
      int syncHandle = 0;
      long len = Bass.BASS_ChannelGetLength(_stream); // length in bytes
      double totaltime = Bass.BASS_ChannelBytes2Seconds(_stream, len); // the total time length

      // Did we get a Resume & Duration (Cue File)
      if (_resumeAt > 0 && _duration > 0)
        totaltime = (double)_resumeAt / 1000f + (double)_duration;

      float fadeOutSeconds = 0;

//      if (!_settings.GaplessPlayback && _settings.Crossfade > 0)
//        fadeOutSeconds = _settings.Crossfade / 1000f;
//      else
//        // Request the next file of a playlist 4 secs before the song ends. to allow bufering for gaplesss playback
//        fadeOutSeconds = 4000 / 1000f;

      long bytePos = Bass.BASS_ChannelSeconds2Bytes(_stream, totaltime - fadeOutSeconds);

      syncHandle = Bass.BASS_ChannelSetSync(_stream,
          BASSSync.BASS_SYNC_POS,
          bytePos, PlaybackFadeOutProcDelegate,
          IntPtr.Zero);

      if (syncHandle == 0)
      {
        int error = (int)Bass.BASS_ErrorGetCode();
        ServiceScope.Get<ILogger>().Debug("BASS: RegisterPlaybackFadeOutEvent of stream {0} failed with error {1}", _stream, error);
      }

      return syncHandle;
    }

    /// <summary>
    /// Fade Out  Procedure
    /// </summary>
    private void PlaybackFadeOutProc(int handle, int stream, int data, IntPtr userData)
    {
      ServiceScope.Get<ILogger>().Debug("BASS: Fade out of stream {0}", stream);

//      if (!_settings.GaplessPlayback)
//        Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, 0, _settings.Crossfade);
      SendInternalMessage("xfading");
    }

    #endregion Fadeout / Crossfade Event

    #region Playback End Event
    /// <summary>
    /// Register the Playback end Event
    /// </summary>
    private int RegisterPlaybackEndEvent()
    {
      int syncHandle = 0;

      // Did we get a Resume & Duration (Cue File)
      // Then we can't wait for the end of the stream, we need to set a sync_pos
      if (_resumeAt > 0 && _duration > 0)
      {
        float totaltime = (float)_resumeAt / 1000f + (float)_duration;
        long bytePos = Bass.BASS_ChannelSeconds2Bytes(_stream, totaltime);

        syncHandle = Bass.BASS_ChannelSetSync(_stream,
            BASSSync.BASS_SYNC_POS,
            bytePos, PlaybackEndProcDelegate,
            IntPtr.Zero);
      }
      else
      {
        syncHandle = Bass.BASS_ChannelSetSync(_stream,
            BASSSync.BASS_SYNC_END,
            0, PlaybackEndProcDelegate,
            IntPtr.Zero);
      }
      if (syncHandle == 0)
      {
        int error = (int)Bass.BASS_ErrorGetCode();
        ServiceScope.Get<ILogger>().Debug("BASS: RegisterPlaybackEndEvent of stream {0} failed with error {1}", _stream, error);
      }

      return syncHandle;
    }

    /// <summary>
    /// Playback end Procedure
    /// </summary>
    private void PlaybackEndProc(int handle, int stream, int data, IntPtr userData)
    {
      ServiceScope.Get<ILogger>().Debug("BASS: End of stream {0}", stream);
      _stream = 0;

      SendInternalMessage("ended");
    }
    #endregion Playback End Event
    #endregion SyncProcs
  }
}
