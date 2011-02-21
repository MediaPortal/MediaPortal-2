#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Threading;
using BDInfo;
using DirectShowLib;
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Settings;
using MediaPortal.Plugins.BDHandler.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// BDPlayer implements a BluRay player based on the raw files. Currently there is no menu support available.
  /// </summary>
  public class BDPlayer : VideoPlayer, IDVDPlayer
  {
    #region Consts and delegates
       
    // "MPC - Mpeg Source (Gabest)
    public static CodecInfo MpcMpegSourceFilterInfo = new CodecInfo()
                                                        {
                                                          CLSID = "{1365BE7A-C86A-473C-9A41-C0A6E82C9FA3}",
                                                          Name = "MPC - Mpeg Source (Gabest)"
                                                        };

    public const double MINIMAL_FULL_FEATURE_LENGTH = 3000;
    public const string RES_PLAYBACK_CHAPTER = "[Playback.Chapter]";

    /// <summary>
    /// Delegate for starting a BDInfo thread.
    /// </summary>
    /// <param name="path">Path to scan</param>
    /// <returns>BDInfo</returns>
    delegate BDInfoExt ScanProcess(string path);

    #endregion

    #region Variables

    private double[] _chapterTimestamps;
    private string[] _chapterNames;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a BDPlayer player object.
    /// </summary>
    public BDPlayer()
    {
      PlayerTitle = "BDPlayer"; // for logging
      _requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.AudioMPEG;
    }

    #endregion

    #region VideoPlayer overrides 

    protected override void CreateGraphBuilder()
    {
      base.CreateGraphBuilder();
      // configure EVR
      _streamCount = 2; // Allow Video and Subtitle
    }

    /// <summary>
    /// Adds preferred audio/video codecs.
    /// </summary>
    protected override void AddPreferredCodecs()
    {
      BDInfoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<BDInfoSettings>();
      if (settings == null)
        return;

      //IAMPluginControl is supported in Win7 and later only.
      try
      {
        IAMPluginControl pc = new DirectShowPluginControl() as IAMPluginControl;
        if (pc != null)
        {
          if (settings.AVCCodec != null)
            pc.SetPreferredClsid(CodecHandler.MEDIASUBTYPE_AVC, settings.AVCCodec.GetCLSID());
        }
      }
      catch
      {
      }
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      string strFile = _resourceAccessor.LocalFileSystemPath;
      
      // Render the file
      strFile = Path.Combine(strFile.ToLower(), @"BDMV\index.bdmv");

      // only continue with playback if a feature was selected or the extension was m2ts.
      if (DoFeatureSelection(ref strFile))
      {
        // load the source filter         
        if (TryAdd(MpcMpegSourceFilterInfo))
        {
          IFileSourceFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder);
          // load the file
          int hr = fileSourceFilter.Load(strFile, null);
          DsError.ThrowExceptionForHR(hr);
        }
        else
        {
          BDPlayerBuilder.LogError("Unable to load DirectShowFilter: {0}", MpcMpegSourceFilterInfo.Name);
          throw new Exception("Unable to load DirectShowFilter");
        }
      }
    }


    protected override void OnBeforeGraphRunning()
    {
      base.OnBeforeGraphRunning();

      IBaseFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder) as IBaseFilter;

      // first all automatically rendered pins
      FilterGraphTools.RenderOutputPins(_graphBuilder, fileSourceFilter);

      // MSDN: "During the connection process, the Filter Graph Manager ignores pins on intermediate filters if the pin name begins with a tilde (~)."
      // then connect the skipped "~" output pins
      FilterGraphTools.RenderAllManualConnectPins(_graphBuilder);

      AnalyseStreams();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Analyzes the current graph and extracts information about chapter markers and subtitle streams.
    /// </summary>
    /// <returns></returns>
    public bool AnalyseStreams()
    {
      BDPlayerBuilder.LogDebug("Analyzing streams to filter duplicates...");
      try
      {
        IAMExtendedSeeking pEs = FilterGraphTools.FindFilterByInterface<IAMExtendedSeeking>(_graphBuilder);
        if (pEs != null)
        {
          int markerCount;
          if (pEs.get_MarkerCount(out markerCount) == 0 && markerCount > 0)
          {
            _chapterTimestamps = new double[markerCount];
            _chapterNames = new string[markerCount];
            for (int i = 1; i <= markerCount; i++)
            {
              double markerTime;
              pEs.GetMarkerTime(i, out markerTime);
              _chapterTimestamps[i - 1] = markerTime;
              _chapterNames[i - 1] = GetChapterName(i);
            }
          }
        }
      }
      catch { }
      return true;
    }

    #endregion

    /// <summary>
    /// Scans a bluray folder and returns a BDInfo object
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private BDInfoExt ScanWorker(string path)
    {
      BDPlayerBuilder.LogInfo("Scanning bluray structure: {0}", path);
      BDInfoExt bluray = new BDInfoExt(path.ToUpper());
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
        ScanProcess scanner = ScanWorker;
        IAsyncResult result = scanner.BeginInvoke(filePath, null, scanner);

        // Show the wait cursor during scan
        //GUIWaitCursor.Init();
        //GUIWaitCursor.Show();
        while (result.IsCompleted == false)
        {
          //GUIWindowManager.Process();
          Thread.Sleep(100);
        }

        BDInfoExt bluray = scanner.EndInvoke(result);
        List<TSPlaylistFile> allPlayLists = bluray.PlaylistFiles.Values.Where(p => p.IsValid).OrderByDescending(p => p.TotalLength).Distinct().ToList();

        // this will be the title of the dialog, we strip the dialog of weird characters that might wreck the font engine.
        string heading = (bluray.Title != string.Empty) ? Regex.Replace(bluray.Title, @"[^\w\s\*\%\$\+\,\.\-\:\!\?\(\)]", "").Trim() : "Bluray: Select Feature";

        //GUIWaitCursor.Hide();

        // Feature selection logic 
        TSPlaylistFile listToPlay;
        if (allPlayLists.Count == 0)
        {
          BDPlayerBuilder.LogInfo("No playlists found, bypassing dialog.", allPlayLists.Count);
          return true;
        }
        if (allPlayLists.Count == 1)
        {
          // if we have only one playlist to show just move on
          BDPlayerBuilder.LogInfo("Found one valid playlist, bypassing dialog.", filePath);
          listToPlay = allPlayLists[0];
        }
        else
        {
          // Show selection dialog
          BDPlayerBuilder.LogInfo("Found {0} playlists, showing selection dialog.", allPlayLists.Count);

          // first make an educated guess about what the real features are (more than one chapter, no loops and longer than one hour)
          // todo: make a better filter on the playlists containing the real features
          List<TSPlaylistFile> playLists = allPlayLists.Where(p => (p.Chapters.Count > 1 || p.TotalLength >= MINIMAL_FULL_FEATURE_LENGTH) && !p.HasLoops).ToList();

          // if the filter yields zero results just list all playlists 
          if (playLists.Count == 0)
          {
            playLists = allPlayLists;
          }

          //IDialogbox dialog = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
          //while (true)
          //{
          //  dialog.Reset();
          //  dialog.SetHeading(heading);

          //  int count = 1;

          //  for (int i = 0; i < playLists.Count; i++)
          //  {
          //    TSPlaylistFile playList = playLists[i];
          //    TimeSpan lengthSpan = new TimeSpan((long)(playList.TotalLength * 10000000));
          //    string length = string.Format("{0:D2}:{1:D2}:{2:D2}", lengthSpan.Hours, lengthSpan.Minutes, lengthSpan.Seconds);
          //    // todo: translation
          //    string feature = string.Format("Feature #{0}, {2} Chapter{3} ({1})", count, length, playList.Chapters.Count, (playList.Chapters.Count > 1) ? "s" : string.Empty);
          //    dialog.Add(feature);
          //    count++;
          //  }

          //  if (allPlayLists.Count > playLists.Count)
          //  {
          //    // todo: translation
          //    dialog.Add("List all features...");
          //  }

          //  dialog.DoModal(GUIWindowManager.ActiveWindow);

          //  if (dialog.SelectedId == count)
          //  {
          //    // don't filter the playlists and continue to display the dialog again
          //    playLists = allPlayLists;
          //    continue;

          //  }
          //  else if (dialog.SelectedId < 1)
          //  {
          //    // user cancelled so we return
          //    BDHandlerCore.LogDebug("User cancelled dialog.");
          //    return false;
          //  }

          //  // end dialog
          //  break;
          //}

          //listToPlay = playLists[dialog.SelectedId - 1];
          listToPlay = playLists[0];
        }
        GetChapters(listToPlay);

        // load the chapters
        //chapters = listToPlay.Chapters.ToArray();
        //BDHandlerCore.LogDebug("Selected: Playlist={0}, Chapters={1}", listToPlay.Name, chapters.Length);

        // create the chosen file path (playlist)
        filePath = Path.Combine(bluray.DirectoryPLAYLIST.FullName, listToPlay.Name);

        //#region Refresh Rate Changer

        //// Because g_player reads the framerate from the iniating media path we need to
        //// do a re-check of the framerate after the user has chosen the playlist. We do
        //// this by grabbing the framerate from the first video stream in the playlist as
        //// this data was already scanned.
        //using (Settings xmlreader = new MPSettings())
        //{
        //  bool enabled = xmlreader.GetValueAsBool("general", "autochangerefreshrate", false);
        //  if (enabled)
        //  {
        //    TSFrameRate framerate = listToPlay.VideoStreams[0].FrameRate;
        //    if (framerate != TSFrameRate.Unknown)
        //    {
        //      double fps = 0;
        //      switch (framerate)
        //      {
        //        case TSFrameRate.FRAMERATE_59_94:
        //          fps = 59.94;
        //          break;
        //        case TSFrameRate.FRAMERATE_50:
        //          fps = 50;
        //          break;
        //        case TSFrameRate.FRAMERATE_29_97:
        //          fps = 29.97;
        //          break;
        //        case TSFrameRate.FRAMERATE_25:
        //          fps = 25;
        //          break;
        //        case TSFrameRate.FRAMERATE_24:
        //          fps = 24;
        //          break;
        //        case TSFrameRate.FRAMERATE_23_976:
        //          fps = 23.976;
        //          break;
        //      }

        //      BDHandlerCore.LogDebug("Initiating refresh rate change: {0}", fps);
        //      //RefreshRateChanger.SetRefreshRateBasedOnFPS(fps, filePath, RefreshRateChanger.MediaType.Video);
        //    }
        //  }
        //}

        //#endregion

        return true;
      }
      catch (Exception e)
      {
        BDPlayerBuilder.LogError("Exception while reading bluray structure {0} {1}", e.Message, e.StackTrace);
        return true;
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

    #region IDVDPlayer Member

    private readonly string[] _emptyStringArray = new string[0];

    public string[] DvdTitles
    {
      get { return _emptyStringArray; }
    }

    public void SetDvdTitle(string title)
    { }

    public string CurrentDvdTitle
    {
      get { return null; }
    }

    /// <summary>
    /// Returns a localized chapter name.
    /// </summary>
    /// <param name="chapterNumber">0 based chapter number.</param>
    /// <returns>Localized chapter name.</returns>
    private static String GetChapterName(int chapterNumber)
    {
      //Idea: we could scrape chapter names and store them in MediaAspects. When they are available, return the full names here.
      return ServiceRegistration.Get<ILocalization>().ToString(RES_PLAYBACK_CHAPTER, chapterNumber);
    }

    public string[] DvdChapters
    {
      get { return _chapterNames; }
    }

    public void SetDvdChapter(string chapter)
    {
      string[] chapters = DvdChapters;
      for (int i = 0; i < chapters.Length; i++)
      {
        if (chapter == chapters[i])
        {
          SetDvdChapter(i);
          return;
        }
      }
    }

    private void SetDvdChapter(Int32 chapterIndex)
    {
      if (chapterIndex > _chapterTimestamps.Length || chapterIndex < 0)
        return;
      TimeSpan seekTo = TimeSpan.FromSeconds(_chapterTimestamps[chapterIndex]);
      CurrentTime = seekTo;
      return;
    }

    public bool ChaptersAvailable
    {
      get { return _chapterNames != null; }
    }

    private bool GetCurrentChapterIndex(out Int32 chapterIndex)
    {
      double currentTimestamp = CurrentTime.TotalSeconds;
      for (int c = _chapterTimestamps.Length - 1; c >= 0; c--)
      {
        if (currentTimestamp > _chapterTimestamps[c])
        {
          chapterIndex = c;
          return true;
        }
      }
      chapterIndex = 0;
      return false;
    }

    public void NextChapter()
    {
      Int32 currentChapter;
      if (GetCurrentChapterIndex(out currentChapter))
      {
        SetDvdChapter(currentChapter + 1);
      }
    }

    public void PrevChapter()
    {
      Int32 currentChapter;
      if (GetCurrentChapterIndex(out currentChapter))
      {
        SetDvdChapter(currentChapter - 1);
      }
    }

    public string CurrentDvdChapter
    {
      get { return null; }
    }

    public bool IsHandlingUserInput
    {
      get { return false; }
    }

    public void ShowDvdMenu()
    { }

    public void OnMouseMove(float x, float y)
    { }

    public void OnMouseClick(float x, float y)
    { }

    public void OnKeyPress(Control.InputManager.Key key)
    { }

    #endregion
  }
}