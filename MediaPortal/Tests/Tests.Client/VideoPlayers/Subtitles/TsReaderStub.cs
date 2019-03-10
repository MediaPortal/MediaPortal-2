#region Copyright (C) 2007-2019 Team MediaPortal

/*
    Copyright (C) 2007-2019 Team MediaPortal
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Teletext;

namespace Tests.Client.VideoPlayers.Subtitles
{
  public class TsReaderStub : ITsReader, ITeletextSource, ISubtitleStream
  {
    private int _teletextStreamCount;
    private int _teletextStreamType;
    private string _teletextSubtitleLanguage;
    private int _subtitleStreamCount;
    private int _subtitleStreamType;
    private string _subtitleLanguage;

    public int TeletextStreamCount
    {
      set { _teletextStreamCount = value; }
    }

    public int TeletextStreamType
    {
      set { _teletextStreamType = value; }
    }

    public string TeletextSubtitleLanguage
    {
      set { _teletextSubtitleLanguage = value; }
    }

    public int SubtitleStreamCount
    {
      set { _subtitleStreamCount = value; }
    }

    public int SubtitleStreamType
    {
      set { _subtitleStreamType = value; }
    }

    public string SubtitleLanguage
    {
      set { _subtitleLanguage = value; }
    }

    public int SetTsReaderCallback(ITsReaderCallback callback)
    {
      throw new NotImplementedException();
    }

    public int SetRequestAudioChangeCallback(ITsReaderCallbackAudioChange callback)
    {
      throw new NotImplementedException();
    }

    public int SetRelaxedMode(int relaxedReading)
    {
      throw new NotImplementedException();
    }

    public void OnZapping(int info)
    {
      throw new NotImplementedException();
    }

    public void OnGraphRebuild(ChangedMediaType info)
    {
      throw new NotImplementedException();
    }

    public void SetMediaPosition(long mediaPos)
    {
      throw new NotImplementedException();
    }

    public void SetTeletextTSPacketCallback(IntPtr callback)
    {
      throw new NotImplementedException();
    }

    public void SetTeletextEventCallback(IntPtr callback)
    {
      throw new NotImplementedException();
    }

    public void SetTeletextServiceInfoCallback(IntPtr callback)
    {
      throw new NotImplementedException();
    }

    public void GetTeletextStreamType(int stream, ref int type)
    {
      type = _teletextStreamType;
    }

    public void GetTeletextStreamCount(ref int count)
    {
      count = _teletextStreamCount;
    }

    public void GetTeletextStreamLanguage(int stream, ref SubtitleLanguage szLanguage)
    {
      szLanguage.lang = _teletextSubtitleLanguage;
    }

    public void SetSubtitleStream(int stream)
    {
      throw new NotImplementedException();
    }

    public void GetSubtitleStreamType(int stream, ref int type)
    {
      type = _subtitleStreamType;
    }

    public void GetSubtitleStreamCount(ref int count)
    {
      count = _subtitleStreamCount;
    }

    public void GetCurrentSubtitleStream(ref int stream)
    {
      throw new NotImplementedException();
    }

    public void GetSubtitleStreamLanguage(int stream, ref SubtitleLanguage szLanguage)
    {
      szLanguage.lang = _subtitleLanguage;
    }
  }
}
