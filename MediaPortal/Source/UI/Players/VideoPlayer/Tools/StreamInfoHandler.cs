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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DirectShow;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Subtitles;

namespace MediaPortal.UI.Players.Video.Tools
{
  public class TsReaderStreamInfoHandler : BaseStreamInfoHandler
  {
    private const int NO_STREAM_INDEX = -1;
    private readonly ISubtitleStream _subtitleStream;
    public TsReaderStreamInfoHandler(ISubtitleStream subtitleStream)
    {
      if (subtitleStream == null)
        return;

      int count = 0;
      _subtitleStream = subtitleStream;
      subtitleStream.GetSubtitleStreamCount(ref count);
      if (count > 0)
      {
        StreamInfo subStream = new StreamInfo(null, NO_STREAM_INDEX, VideoPlayer.NO_SUBTITLES, 0);
        AddUnique(subStream);
      }
      for (int i = 0; i < count; ++i)
      {
        //FIXME: language should be passed back also as LCID
        SubtitleLanguage language = new SubtitleLanguage();
        subtitleStream.GetSubtitleStreamLanguage(i, ref language);
        int lcid = BaseDXPlayer.LookupLcidFromName(language.lang);
        // Note: the "type" is no longer considered in MP1 code as well, so I guess DVBSub3 only supports Bitmap subs at all.
        string name = language.lang;
        StreamInfo subStream = new StreamInfo(null, i, name, lcid);
        AddUnique(subStream);
      }
    }

    public override bool EnableStream(string selectedStream)
    {
      if (_subtitleStream == null)
        return false;

      // Do not enumerate raw stream names again, as they were made unique (adding counters)
      for (int i = 0; i < Count; ++i)
      {
        string subtitleTrackName = this[i].Name;
        if (!subtitleTrackName.Equals(selectedStream))
          continue;

        if (this[i].StreamIndex == NO_STREAM_INDEX)
        {
          // Disable subtitles
          ServiceRegistration.Get<ILogger>().Debug("TsReaderStreamInfoHandler: Disable stream");
          lock (_syncObj)
            _currentStream = null;
          return true;
        }

        ServiceRegistration.Get<ILogger>().Debug("TsReaderStreamInfoHandler: Enable stream '{0}'", selectedStream);
        _subtitleStream.SetSubtitleStream(i);
        lock (_syncObj)
          _currentStream = this[i];
        return true;
      }
      return false;
    }

    public bool DisableSubs
    {
      get { return _currentStream == null || _currentStream.StreamIndex == NO_STREAM_INDEX; }
    }
  }

  public class MpcStreamInfoHandler : BaseStreamInfoHandler
  {
    public MpcStreamInfoHandler()
    {
      var subtitleCount = MpcSubtitles.GetCount();
      for (int i = 0; i < subtitleCount; ++i)
      {
        string subtitleTrackName = MpcSubtitles.GetTrackName(i);
        int lcid = BaseDXPlayer.LookupLcidFromName(subtitleTrackName);
        StreamInfo subStream = new StreamInfo(null, i, subtitleTrackName, lcid);
        AddUnique(subStream);
      }
      AddUnique(new StreamInfo(null, subtitleCount + 1, VideoPlayer.NO_SUBTITLES, 0));
    }

    public override bool EnableStream(string selectedStream)
    {
      // Do not enumerate raw stream names again, as they were made unique (adding counters)
      for (int i = 0; i < Count; ++i)
      {
        string subtitleTrackName = this[i].Name;
        if (subtitleTrackName.Equals(selectedStream))
        {
          ServiceRegistration.Get<ILogger>().Debug("MpcStreamInfoHandler: Enable stream '{0}'", selectedStream);
          MpcSubtitles.SetCurrent(i);
          lock (_syncObj)
            _currentStream = this[i];
          return true;
        }
      }
      return false;
    }
  }

  /// <summary>
  /// StreamInfoHandler contains list of StreamInfo objects of same kind (audio, video, subtitles). 
  /// </summary>
  public class StreamInfoHandler : BaseStreamInfoHandler
  {
    /// <summary>
    /// Enables a selected stream name by calling it associated StreamSelector.
    /// </summary>
    /// <param name="selectedStream"></param>
    public override bool EnableStream(string selectedStream)
    {
      StreamInfo streamInfo;
      lock (_syncObj)
        streamInfo = FindStream(selectedStream);

      if (streamInfo == null || streamInfo.StreamSelector == null)
        return false;

      ServiceRegistration.Get<ILogger>().Debug("StreamInfoHandler: Enable stream '{0}'", selectedStream);
      streamInfo.StreamSelector.Enable(streamInfo.StreamIndex, AMStreamSelectEnableFlags.Enable);

      lock (_syncObj)
        _currentStream = streamInfo;
      return true;
    }
  }

  /// <summary>
  /// StreamInfoHandler contains list of StreamInfo objects of same kind (audio, video, subtitles). 
  /// </summary>
  public abstract class BaseStreamInfoHandler : IEnumerable<StreamInfo>
  {
    protected readonly object _syncObj = new object();

    #region Properties

    public int Count
    {
      get
      {
        lock (_syncObj)
          return _streamInfos.Count;
      }
    }

    public StreamInfo this[int index]
    {
      get
      {
        lock (_syncObj)
          return _streamInfos[index];
      }
      set
      {
        lock (_syncObj)
        {
          _streamNamesCache = null;
          _streamInfos[index] = value;
        }
      }
    }

    /// <summary>
    /// Gets the CurrentStream name. Defaults to name of first stream, if no explicit selection was done before.
    /// </summary>
    public string CurrentStreamName
    {
      get
      {
        lock (_syncObj)
          return CurrentStream != null ? CurrentStream.Name : String.Empty;
      }
    }

    /// <summary>
    /// Gets the CurrentStream. Defaults to the first stream, if no explicit selection was done before.
    /// </summary>
    public StreamInfo CurrentStream
    {
      get
      {
        lock (_syncObj)
        {
          if (_currentStream != null)
            return _currentStream;
          return Count > 0 ? this[0] : null;
        }
      }
    }
    #endregion

    #region Variables

    protected readonly List<StreamInfo> _streamInfos = new List<StreamInfo>();
    protected StreamInfo _currentStream = null;
    protected string[] _streamNamesCache = null;

    #endregion

    #region Protected members

    protected void Add(StreamInfo item)
    {
      lock (_syncObj)
      {
        _streamNamesCache = null;
        _streamInfos.Add(item);
      }
    }

    #endregion

    #region Public members

    /// <summary>
    /// AddUnique adds a StreamInfo and avoids duplicates by adding a counting number.
    /// </summary>
    /// <param name="valueToAdd">StreamInfo to add</param>
    public void AddUnique(StreamInfo valueToAdd)
    {
      AddUnique(valueToAdd, false);
    }

    /// <summary>
    /// AddUnique adds a StreamInfo and either avoids duplicates by adding a counting number or skips existing values.
    /// </summary>
    /// <param name="valueToAdd">StreamInfo to add</param>
    /// <param name="skipExistingNames"><c>true</c> to skip existing names</param>
    /// <remarks>
    /// Use <paramref name="skipExistingNames"/> = <c>true</c> to filter different IAMStreamSelect instances that support same stream kinds.
    /// This happens i.e. with Haali splitter and FFDShow video decoder used in graph. If DirectVobSub is installed it also implements
    /// IAMStreamSelect.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="valueToAdd"/> is null</exception>
    public void AddUnique(StreamInfo valueToAdd, bool skipExistingNames)
    {
      if (valueToAdd == null)
        throw new ArgumentNullException("valueToAdd");

      lock (_syncObj)
      {
        if (_streamInfos.Find(s => (s.Name == valueToAdd.Name && (s.StreamSelector == valueToAdd.StreamSelector || skipExistingNames))) == null)
          Add(valueToAdd);
        else
        {
          // If we want to have the same stream only once, exit here. 
          if (skipExistingNames)
            return;

          // Try a maximum of 2..5 numbers to append.
          for (int i = 2; i <= 5; i++)
          {
            String countedName = String.Format("{0} ({1})", valueToAdd, i);
            if (_streamInfos.Find(s => (s.Name == countedName) && s.StreamSelector == valueToAdd.StreamSelector) == null)
            {
              Add(new StreamInfo(valueToAdd.StreamSelector, valueToAdd.StreamIndex, countedName, valueToAdd.LCID));
              return;
            }
          }
        }
      }
    }

    /// <summary>
    /// Returns a (cached) array of all stream names.
    /// </summary>
    /// <returns>String array containing the names of all streams.</returns>
    public string[] GetStreamNames()
    {
      lock (_syncObj)
        return _streamNamesCache ?? (_streamNamesCache = _streamInfos.Select(streamInfo => streamInfo.Name).ToArray());
    }

    /// <summary>
    /// Returns a (cached) array of all stream names, but without the forced subtitles.
    /// </summary>
    /// <returns>String array containing the names of all streams (except forced subtitles).</returns>
    public string[] GetStreamNamesWithoutForcedSubtitles()
    {
      lock (_syncObj)
        return _streamNamesCache ?? (_streamNamesCache = _streamInfos.FindAll(s => s.IsAutoSubtitle == false).Select(streamInfo => streamInfo.Name).ToArray());
    }

    /// <summary>
    /// Enables a selected stream name by calling it associated StreamSelector.
    /// </summary>
    /// <param name="selectedStream"></param>
    public abstract bool EnableStream(string selectedStream);

    /// <summary>
    /// Finds a stream by it's name.
    /// </summary>
    /// <param name="selectedStream">StreamName to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindStream(string selectedStream)
    {
      lock (_syncObj)
        return _streamInfos.Find(s => s.Name == selectedStream);
    }

    /// <summary>
    /// Finds the first stream that has the requested LCID (Locale ID).
    /// </summary>
    /// <param name="lcid">LCID to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindStream(int lcid)
    {
      lock (_syncObj)
        return _streamInfos.Find(s => s.LCID == lcid);
    }

    /// <summary>
    /// Finds the first stream by name part. This can be used to find "English" in "S: [English]" or "A: [English] (MP3 2ch)".
    /// </summary>
    /// <param name="selectedStream">StreamName to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindSimilarStream(string selectedStream)
    {
      lock (_syncObj)
        return String.IsNullOrEmpty(selectedStream) ? null : _streamInfos.Find(s => s.Name.ToLowerInvariant().Contains(selectedStream.ToLowerInvariant()));
    }

    /// <summary>
    /// Finds the first stream that is a forced stream
    /// </summary>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindForcedStream()
    {
      lock (_syncObj)
        return _streamInfos.Find(s => s.IsAutoSubtitle);
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator<StreamInfo> GetEnumerator()
    {
      lock (_syncObj)
        return _streamInfos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion
  }
}
