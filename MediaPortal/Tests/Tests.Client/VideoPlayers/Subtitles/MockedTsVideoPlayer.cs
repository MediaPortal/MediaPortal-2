#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.Players.Video.Teletext;
using MediaPortal.UI.Players.Video.Tools;
using Moq;

namespace Tests.Client.VideoPlayers.Subtitles
{
  public class MockedTsVideoPlayer : TsVideoPlayer
  {
    public MockedTsVideoPlayer(ISubtitleRenderer subtitleRenderer, TsReaderStub tsReaderStub)
    {
      _tsReader = tsReaderStub;
      _subtitleRenderer = subtitleRenderer;
    }

    public void AddSubtitleFilter()
    {
      AddSubtitleFilter(true);
    } 
  }
}
