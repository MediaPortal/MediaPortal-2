#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Runtime.InteropServices;

namespace Ui.Players.Video.Subtitles
{
  public enum SubtitleType
  {
    Teletext = 0,
    Bitmap = 1,
    None
  }

  // TODO: Have an AUTO subtitle option!

  public class SubtitleOption
  {
    public SubtitleType type;
    public TeletextPageEntry entry; // only for teletext
    public int bitmapIndex; // index among bitmap subs, only for bitmap subs :)
    public string language;
    public bool isAuto = false;

    public override string ToString()
    {
      if (type == SubtitleType.Bitmap)
      {
        return "Bitmap Lang " + language;
      }
      else if (type == SubtitleType.Teletext)
      {
        return "Teletext Lang\t" + entry.language + "\tpage : " + entry.page;
      }
      else if (type == SubtitleType.None)
      {
        return "None";
      }
      else
      {
        return "???";
      }
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override bool Equals(object o)
    {
      if (o == null) return false;
      if (o is SubtitleOption)
      {
        SubtitleOption other = o as SubtitleOption;
        if (other.type != this.type) return false;
        else if (other.bitmapIndex != this.bitmapIndex) return false;
        else if (!other.language.Equals(this.language)) return false;
        else if ((this.entry != null && !this.entry.Equals(other.entry)) || this.entry == null && other.entry != null) return false;
        else return true;
      }
      else return false;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  struct SUBTITLESTREAM
  {
    public int pid;
    public int subtitleType;
    public byte lang0, lang1, lang2;
    public byte termChar;
  }
#if NOTUSED
  class SubtitleSelector
  {
    private SubtitleOption autoSelectOption;
    private delegate int SubtitleStreamEventCallback(int count, IntPtr pOpts, ref int bitmapindex);
    private SubtitleStreamEventCallback subStreamCallback;
    private object syncLock = new object();

    public SubtitleSelector(ISubtitleStream dvbStreams, SubtitleRenderer subRender, TeletextSubtitleDecoder subDecoder)
    {
      Log.Debug("SubtitleSelector ctor");
      if (subRender == null)
      {
        throw new Exception("Nullpointer input not allowed ( SubtitleRenderer)");
      }
      else
      {
        this.dvbStreams = dvbStreams;
        this.subRender = subRender;
      }

      // load preferences
      using (MediaPortal.Profile.Settings reader = new MediaPortal.Profile.Settings(MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Config, "MediaPortal.xml")))
      {
        preferedLanguages = new List<string>();
        string languages = reader.GetValueAsString("tvservice", "preferredsublanguages", "");
        Log.Debug("SubtitleSelector: sublangs entry content: " + languages);
        StringTokenizer st = new StringTokenizer(languages, ";");
        while (st.HasMore)
        {
          string lang = st.NextToken();
          if (lang.Length != 3)
          {
            Log.Warn("Language {0} is not in the correct format!", lang);
          }
          else
          {
            preferedLanguages.Add(lang);
            Log.Info("Prefered language {0} is {1}", preferedLanguages.Count, lang);
          }
        }
      }

      pageEntries = new Dictionary<int, TeletextPageEntry>();

      bitmapSubtitleCache = new List<SubtitleOption>();

      lock (syncLock)
      {
        if (subDecoder != null)
        {
          subDecoder.SetPageInfoCallback(new MediaPortal.Player.Subtitles.TeletextSubtitleDecoder.PageInfoCallback(OnPageInfo));
        }

        if (dvbStreams != null)
        {
          RetrieveBitmapSubtitles();
          subStreamCallback = new SubtitleStreamEventCallback(OnSubtitleReset);
          IntPtr pSubStreamCallback = Marshal.GetFunctionPointerForDelegate(subStreamCallback);
          Log.Debug("Calling SetSubtitleStreamEventCallback");
          dvbStreams.SetSubtitleResetCallback(pSubStreamCallback);
        }

        if (preferedLanguages.Count > 0)
        {
          autoSelectOption = new SubtitleOption();
          autoSelectOption.language = "None";
          autoSelectOption.isAuto = true;
          autoSelectOption.type = SubtitleType.None;

          SetOption(0); // the autoselect mode will have index 0 (ugly)
        }
      }
      Log.Debug("End SubtitleSelector ctor");
    }

    // ONLY call from MP main thread!
    private void RetrieveBitmapSubtitles()
    {
      bitmapSubtitleCache.Clear();

      try
      {
        // collect dvb bitmap subtitle options
        int streamCount = 0;
        dvbStreams.GetSubtitleStreamCount(ref streamCount);
        Debug.Assert(streamCount >= 0 && streamCount <= 100);

        for (int i = 0; i < streamCount; i++)
        {
          TSReaderPlayer.SUBTITLE_LANGUAGE subLang = new TSReaderPlayer.SUBTITLE_LANGUAGE();
          dvbStreams.GetSubtitleStreamLanguage(i, ref subLang);
          SubtitleOption option = new SubtitleOption();
          option.type = SubtitleType.Bitmap;
          option.language = subLang.lang;
          option.bitmapIndex = i;
          bitmapSubtitleCache.Add(option);
          Log.Debug("Retrieved bitmap option Lang : " + option.ToString());
        }
      }
      catch (Exception e)
      {
        Log.Error(e);
      }
    }



    private int OnSubtitleReset(int count, IntPtr pOpts, ref int selected_bitmap_index)
    {
      Log.Debug("OnSubtitleReset");
      Log.Debug("selected_bitmap_index " + selected_bitmap_index);
      lock (syncLock)
      {
        bitmapSubtitleCache.Clear();
        pageEntries.Clear();

        Log.Debug("Number of bitmap options {0}", count);
        IntPtr current = pOpts;
        for (int i = 0; i < count; i++)
        {
          Log.Debug("Bitmap index " + i);
          SUBTITLESTREAM bOpt = (SUBTITLESTREAM)Marshal.PtrToStructure(current, typeof(SUBTITLESTREAM));
          SubtitleOption opt = new SubtitleOption();
          opt.bitmapIndex = i;
          opt.type = SubtitleType.Bitmap;
          opt.language = "" + (char)bOpt.lang0 + (char)bOpt.lang1 + (char)bOpt.lang2;
          Log.Debug(opt.ToString());
          bitmapSubtitleCache.Add(opt);
          current = (IntPtr)(((int)current) + Marshal.SizeOf(bOpt));
        }

        selected_bitmap_index = -1; // we didnt select a bitmap index

        if (currentOption != null && currentOption.isAuto)
        {
          SubtitleOption prefered = CheckForPreferedLanguage();
          if (prefered != null)
          {
            currentOption.bitmapIndex = prefered.bitmapIndex;
            currentOption.entry = prefered.entry;
            currentOption.language = prefered.language;
            currentOption.type = prefered.type;
            Log.Debug("Auto-selection of " + currentOption);
          }
          else
          {
            currentOption.language = "None";
            currentOption.type = SubtitleType.None;
          }

          subRender.SetSubtitleOption(currentOption);
          if (currentOption.type == SubtitleType.Bitmap)
          {
            selected_bitmap_index = currentOption.bitmapIndex;
            Log.Debug("Returns selected_bitmap_index == {0} to ISubStream", selected_bitmap_index);
          }
        }

      }
      return 0;
    }

    private void OnPageInfo(TeletextPageEntry entry)
    {
      lock (syncLock)
      {
        if (!pageEntries.ContainsKey(entry.page))
        {
          pageEntries.Add(entry.page, entry);
          if (currentOption != null && currentOption.isAuto)
          {
            SubtitleOption prefered = CheckForPreferedLanguage();
            if (prefered != null)
            {
              currentOption.bitmapIndex = prefered.bitmapIndex;
              currentOption.entry = prefered.entry;
              currentOption.language = prefered.language;
              currentOption.type = prefered.type;
              Log.Debug("Auto-selection of " + currentOption);
            }
            else
            {
              currentOption.type = SubtitleType.None;
              currentOption.language = "None";
            }

            subRender.SetSubtitleOption(currentOption);
            // we cannot update the bitmap sub stream here
          }
        }
      }
    }

    /// <summary>
    /// Attempts to auto choose a subtitle option
    /// based on the prefered languages
    /// </summary>
    private SubtitleOption CheckForPreferedLanguage()
    {
      Log.Debug("SubtitleSelector: CheckForPreferedLanguage");
      List<SubtitleOption> options = CollectOptions();
      Log.Debug("Has {0} options", options.Count);

      SubtitleOption prefered = null;
      int priority = int.MaxValue;
      int prefOptIndex = -1;

      for (int optIndex = 1; optIndex < options.Count; optIndex++)
      {
        SubtitleOption opt = options[optIndex];
        int index = preferedLanguages.IndexOf(opt.language);
        Log.Debug(opt + " Pref index " + index);

        if (index >= 0 && index < priority)
        {
          Log.Debug("Setting as pref");
          prefered = opt;
          priority = index;
          prefOptIndex = optIndex;
        }
      }
      return prefered;
    }

    private List<SubtitleOption> CollectOptions()
    {
      //Log.Debug("SubtitleSelector: CollectOptions");
      List<SubtitleOption> options = new List<SubtitleOption>();

      if (autoSelectOption != null)
      {
        options.Add(autoSelectOption);
      }

      options.AddRange(bitmapSubtitleCache);

      // collect teletext options
      foreach (KeyValuePair<int, TeletextPageEntry> p in pageEntries)
      {
        SubtitleOption option = new SubtitleOption();
        option.type = SubtitleType.Teletext;
        option.language = p.Value.language;
        option.entry = p.Value;
        options.Add(option);
        Log.Debug("Added Teletext option Lang : " + option.ToString());
      }
      return options;
    }


    public int CountOptions()
    {
      return CollectOptions().Count;
    }

    public int GetCurrentOption()
    {
      return lastSubtitleIndex;
    }

    /// <summary>
    /// Call only on MP main thread
    /// </summary>
    /// <param name="index"></param>
    public void SetOption(int index)
    {
      Log.Debug("SetOption {0}", index);
      List<SubtitleOption> options = CollectOptions();
      if (index >= options.Count)
      {
        Log.Error("SetOption with too large index!");
        return;
      }
      SubtitleOption option = options[index];
      lastSubtitleIndex = index;
      currentOption = option;

      if (option.isAuto)
      {
        Log.Debug("SubtitleSelector : Set autoselect mode");
        SubtitleOption prefered = CheckForPreferedLanguage();
        if (prefered != null)
        {
          option.bitmapIndex = prefered.bitmapIndex;
          option.entry = prefered.entry;
          option.language = prefered.language;
          option.type = prefered.type;
          Log.Debug("Auto-selection of " + option);
        }
        else
        {
          option.type = SubtitleType.None;
          currentOption.language = "None";
          subRender.SetSubtitleOption(option);
        }
      }

      if (option.type == SubtitleType.Bitmap)
      {
        dvbStreams.SetSubtitleStream(option.bitmapIndex);
      }
      Log.Debug("Subtitle is now " + currentOption.ToString());
      subRender.SetSubtitleOption(option);
    }

    public string GetCurrentLanguage()
    {
      if (currentOption == null)
      {
        Log.Error("Calling GetCurrentLanguage with no subtitle set!");
        return Strings.Unknown;
      }
      else if (currentOption.isAuto)
      {
        return "Auto:" + currentOption.language;
      }
      else if (currentOption.type == SubtitleType.Teletext && currentOption.entry.language.Trim().Length == 0)
      {
        return "p" + currentOption.entry.page;
      }
      else return currentOption.language;
    }

    private MediaPortal.Player.TSReaderPlayer.ISubtitleStream dvbStreams;
    private SubtitleRenderer subRender;
    private List<string> preferedLanguages;
    private int lastSubtitleIndex;
    private SubtitleOption currentOption;
    private Dictionary<int, TeletextPageEntry> pageEntries;

    private List<SubtitleOption> bitmapSubtitleCache;
  }

#endif
}