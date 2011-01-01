#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using DirectShowLib;

namespace MediaPortal.UI.Players.Video.Tools
{
  /// <summary>
  /// StreamInfoHandler contains list of StreamInfo objects of same kind (audio, video, subtitles). 
  /// </summary>
  public class StreamInfoHandler : IEnumerable<StreamInfo>, IDisposable
  {
    #region Properties

    public int Count
    {
      get { return _streamInfos.Count; }
    }

    public StreamInfo this[int index]
    {
      get { return _streamInfos[index]; }
      set
      {
        _streamNamesCache = null;
        _streamInfos[index] = value;
      }
    }

    /// <summary>
    /// Gets the CurrentStream name. Defaults to name of first stream, if no explicit selection was done before.
    /// </summary>
    public string CurrentStreamName
    {
      get { return CurrentStream != null ? CurrentStream.Name : String.Empty; }
    }

    /// <summary>
    /// Gets the CurrentStream. Defaults to the first stream, if no explicit selection was done before.
    /// </summary>
    public StreamInfo CurrentStream
    {
      get
      {
        if (_currentStream != null)
          return _currentStream;
        return Count > 0 ? this[0]: null;
      }
    }
    #endregion

    #region Variables

    private readonly List<StreamInfo> _streamInfos = new List<StreamInfo>();
    private StreamInfo _currentStream = null;
    private string[] _streamNamesCache = null;

    #endregion

    #region Protected members

    protected void Add(StreamInfo item)
    {
      _streamNamesCache = null;
      _streamInfos.Add(item);
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
    /// <param name="skipExistingNames">True to skip existing names</param>
    /// <remarks>
    /// Use <paramref name="skipExistingNames"/> = true to filter different IAMStreamSelect instances that support same stream kinds.
    /// This happens i.e. with Haali splitter and FFDShow video decoder used in graph. If DirectVobSub is installed it also implements
    /// IAMStreamSelect.
    /// </remarks>
    public void AddUnique(StreamInfo valueToAdd, bool skipExistingNames)
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

    /// <summary>
    /// Returns a (cached) array of all stream names.
    /// </summary>
    /// <returns>String array containing the names of all streams.</returns>
    public string[] GetStreamNames()
    {
      if (_streamNamesCache == null)
        _streamNamesCache = _streamInfos.Select(streamInfo => streamInfo.Name).ToArray();
      return _streamNamesCache;
    }

    /// <summary>
    /// Enables a selected stream name by calling it associated StreamSelector.
    /// </summary>
    /// <param name="selectedStream"></param>
    public bool EnableStream(string selectedStream)
    {
      StreamInfo streamInfo = FindStream(selectedStream);
      if (streamInfo == null || streamInfo.StreamSelector == null)
        return false;

      streamInfo.StreamSelector.Enable(streamInfo.StreamIndex, AMStreamSelectEnableFlags.Enable);
      _currentStream = streamInfo;
      return true;
    }

    /// <summary>
    /// Finds a stream by it's name.
    /// </summary>
    /// <param name="selectedStream">StreamName to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindStream(string selectedStream)
    {
      return _streamInfos.Find(s => s.Name == selectedStream);
    }

    /// <summary>
    /// Finds the first stream that has the requested LCID (Locale ID).
    /// </summary>
    /// <param name="lcid">LCID to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindStream(int lcid)
    {
      return _streamInfos.Find(s => s.LCID == lcid);
    }
    
    /// <summary>
    /// Finds the first stream by name part. This can be used to find "English" in "S: [English]" or "A: [English] (MP3 2ch)".
    /// </summary>
    /// <param name="selectedStream">StreamName to find</param>
    /// <returns>StreamInfo or null.</returns>
    public StreamInfo FindSimilarStream(string selectedStream)
    {
      return _streamInfos.Find(s => s.Name.ToLowerInvariant().Contains(selectedStream.ToLowerInvariant()));
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      // Dispose every StreamInfo to allow Marshal.ReleaseComObject of IAMStreamSelect.
      foreach (StreamInfo streamInfo in this)
        streamInfo.Dispose();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator<StreamInfo> GetEnumerator()
    {
      return _streamInfos.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion
  }
}