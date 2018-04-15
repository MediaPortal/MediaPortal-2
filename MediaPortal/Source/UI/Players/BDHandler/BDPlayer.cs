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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BDInfo;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.BDHandler.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// BDPlayer implements a BluRay player based on the raw files. Currently there is no menu support available.
  /// </summary>
  public class BDPlayer : VideoPlayer
  {
    #region Consts and delegates

    public const double MINIMAL_FULL_FEATURE_LENGTH = 300;

    /// <summary>
    /// Delegate for starting a BDInfo thread.
    /// </summary>
    /// <param name="path">Path to scan</param>
    /// <returns>BDInfo</returns>
    delegate BDInfoExt ScanProcess(string path);

    #endregion

    #region Fields

    protected List<TSPlaylistFile> _bdTitles;
    protected string[] _bdTitleNames;
    protected string _currentTitleName;
    protected TSPlaylistFile _manualTitleSelection;
    protected string _playlistFolder;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a BDPlayer player object.
    /// </summary>
    public BDPlayer()
    {
      PlayerTitle = "BDPlayer"; // for logging
    }

    #endregion

    #region VideoPlayer overrides

    protected override void CreateGraphBuilder()
    {
      base.CreateGraphBuilder();
      // configure EVR
      _streamCount = 2; // Allow Video and Subtitle
    }

    protected override void CreateResourceAccessor()
    {
      // BDPlayer needs an ILocalFSResourceAccessor
      ILocalFsResourceAccessor lfsra;
      if (!_resourceLocator.TryCreateLocalFsAccessor(out lfsra))
        throw new IllegalCallException("The BDPlayer can only play local file system resources");
      _resourceAccessor = lfsra;
    }

    /// <summary>
    /// Adds a source filter to the graph and sets the input.
    /// </summary>
    protected override void AddSourceFilter()
    {
      if (!IsLocalFilesystemResource)
        throw new IllegalCallException("The BDPlayer can only play local file system resources");

      using (((ILocalFsResourceAccessor)_resourceAccessor).EnsureLocalFileSystemAccess())
      {
        string strFile = ((ILocalFsResourceAccessor)_resourceAccessor).LocalFileSystemPath;

        // Render the file
        strFile = Path.Combine(strFile.ToLower(), @"BDMV\index.bdmv");

        // only continue with playback if a feature was selected or the extension was m2ts.
        if (DoFeatureSelection(ref strFile))
        {
          // find the SourceFilter
          CodecInfo sourceFilter = ServiceRegistration.Get<ISettingsManager>().Load<BDPlayerSettings>().BDSourceFilter;

          // load the SourceFilter
          if (TryAdd(sourceFilter))
          {
            IFileSourceFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder);
            // load the file
            int hr = fileSourceFilter.Load(strFile, null);
            Marshal.ReleaseComObject(fileSourceFilter);
            new HRESULT(hr).Throw();
          }
          else
          {
            BDPlayerBuilder.LogError("Unable to load DirectShowFilter: {0}", sourceFilter.Name);
            throw new Exception("Unable to load DirectShowFilter");
          }
        }
      }
    }

    protected override void OnBeforeGraphRunning()
    {
      base.OnBeforeGraphRunning();

      IFileSourceFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder);

      // First all automatically rendered pins
      FilterGraphTools.RenderOutputPins(_graphBuilder, (IBaseFilter) fileSourceFilter);

      Marshal.ReleaseComObject(fileSourceFilter);

      // MSDN: "During the connection process, the Filter Graph Manager ignores pins on intermediate filters if the pin name begins with a tilde (~)."
      // then connect the skipped "~" output pins
      FilterGraphTools.RenderAllManualConnectPins(_graphBuilder);
    }

    #endregion

    private string FormatTitle(TSPlaylistFile playlist, int counter)
    {
      return string.Format("{0} Title {1} - {2}", _mediaItemTitle, counter, FormatLength(playlist.TotalLength));
    }

    private static string FormatLength(double playLength)
    {
      TimeSpan duration = TimeSpan.FromSeconds(playLength);
      return string.Format("{0}h {1:00}min", duration.Hours, duration.Minutes);
    }

    /// <summary>
    /// Scans a bluray folder and returns a BDInfo object
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static BDInfoExt ScanWorker(string path)
    {
      BDPlayerBuilder.LogInfo("Scanning bluray structure: {0}", path);
      BDInfoExt bluray = new BDInfoExt(path.ToUpper(), true); // For title selection we need all information here, but this can cause quite big delays for remote resources!
      bluray.Scan();
      return bluray;
    }

    /// <summary>
    /// Returns wether a choice was made and changes the file path
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if playback should continue, False if user cancelled.</returns>
    private bool DoFeatureSelection(ref string filePath)
    {
      try
      {
        // If we have chosen a specific playlist, build the path directly without scanning the complete structure again
        if (_manualTitleSelection != null && !string.IsNullOrEmpty(_playlistFolder))
        {
          filePath = Path.Combine(_playlistFolder, _manualTitleSelection.Name);
          GetChapters(_manualTitleSelection);
          _manualTitleSelection = null;
          return true;
        }

        BDInfoExt bluray;
        try
        {
          bluray = ScanWorker(filePath);
        }
        catch (Exception)
        {
          // If our parsing of BD structure fails, the splitter might still be able to handle the BDMV directly, so return "success" here.
          return true;
        }

        // Store all playlist files for title selection
        _bdTitles = bluray.PlaylistFiles.Values.Where(p => p.IsValid && !p.HasLoops).Distinct().ToList();
        int counter = 0;
        _bdTitleNames = _bdTitles.Select(t => FormatTitle(t, ++counter)).ToArray();
        _playlistFolder = bluray.DirectoryPLAYLIST.FullName;

        List<TSPlaylistFile> allPlayLists = _bdTitles.OrderByDescending(p => p.TotalLength).ToList();

        // Feature selection logic 
        TSPlaylistFile listToPlay;
        if (allPlayLists.Count == 0)
        {
          BDPlayerBuilder.LogInfo("No valid playlists found, use default INDEX.BDMV.");
          return true;
        }
        if (allPlayLists.Count == 1)
        {
          // if we have only one playlist to show just move on
          BDPlayerBuilder.LogInfo("Found one valid playlist {0}.", allPlayLists[0].Name);
          listToPlay = allPlayLists[0];
        }
        else
        {
          // Show selection dialog
          BDPlayerBuilder.LogInfo("Found {0} playlists, title selection available.", allPlayLists.Count);

          // first make an educated guess about what the real features are (more than one chapter, no loops and longer than one hour)
          // todo: make a better filter on the playlists containing the real features
          List<TSPlaylistFile> playLists = allPlayLists.Where(p => (p.Chapters.Count > 1 || p.TotalLength >= MINIMAL_FULL_FEATURE_LENGTH) && !p.HasLoops).ToList();

          // if the filter yields zero results just list all playlists 
          if (playLists.Count == 0)
            playLists = allPlayLists;

          listToPlay = playLists[0];
        }

        BDPlayerBuilder.LogInfo("Using playlist {0}.", listToPlay.Name);
        for (int idx = 0; idx < _bdTitles.Count; idx++)
        {
          if (_bdTitles[idx] != listToPlay) continue;
          _currentTitleName = _bdTitleNames[idx];
          break;
        }

        GetChapters(listToPlay);

        // Combine the chosen file path (playlist)
        filePath = Path.Combine(bluray.DirectoryPLAYLIST.FullName, listToPlay.Name);
        return true;
      }
      catch (Exception e)
      {
        BDPlayerBuilder.LogError("Exception while reading bluray structure {0} {1}", e.Message, e.StackTrace);
        return false;
      }
    }

    private void GetChapters(TSPlaylistFile playlistFile)
    {
      if (playlistFile == null || playlistFile.Chapters == null)
        return;

      _chapterTimestamps = playlistFile.Chapters.ToArray();
      _chapterNames = new string[_chapterTimestamps.Length];
      for (int c = 0; c < _chapterNames.Length; c++)
        _chapterNames[c] = GetChapterName(c + 1);
    }

    #region ITitlePlayer implementation

    public override string[] Titles
    {
      get
      {
        return _bdTitleNames != null && _bdTitleNames.Length > 1 ? _bdTitleNames : EMPTY_STRING_ARRAY;
      }
    }

    /// <summary>
    /// Sets the current title.
    /// </summary>
    /// <param name="title">Title</param>
    public override void SetTitle(string title)
    {
      bool found = false;
      int idx;
      for (idx = 0; idx < Titles.Length; idx++)
        if (Titles[idx] == title)
        {
          found = true;
          break;
        }

      if (!found)
        return;

      _manualTitleSelection = _bdTitles[idx];
      Shutdown(true);
      SetMediaItem(_resourceLocator, Titles[idx]);
    }

    public override string CurrentTitle
    {
      get { return _currentTitleName; }
    }

    #endregion
  }
}