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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectShow;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.Players.Video.Teletext;
using MediaPortal.UI.Presentation.Geometries;
using Moq;
using NUnit.Framework;

namespace Tests.Client.VideoPlayers.Subtitles
{
  [TestFixture]
  public class AddSubtitleFilterTests
  {
    [Test, Description("Add CC filter to graph if closed captions setting is enabled and TS stream does not contains teletext or DVB subtitles")]
    public void ShouldAddClosedCaptionsFilter()
    {
      // Arranges
      SetMockServices();
      FakeVideoSettings settings = new FakeVideoSettings(new VideoSettings
      {
        EnableAtscClosedCaptions = true, EnableDvbSubtitles = true, EnableTeletextSubtitles = true
      });
      ServiceRegistration.Set<ISettingsManager>(settings);

      ISubtitleRenderer mockSubtitleRenderer = Mock.Of<ISubtitleRenderer>();
      TsReaderStub tsReaderStub = new TsReaderStub
      {
        TeletextStreamCount = 0,
        SubtitleStreamCount = 0
      };
      MockedTsVideoPlayer tsVideoPlayer = new MockedTsVideoPlayer(mockSubtitleRenderer, tsReaderStub);

      // Act
      tsVideoPlayer.AddSubtitleFilter();

      // Assert
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddClosedCaptionsFilter(It.IsAny<IGraphBuilder>()), Times.Once);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddDvbSubtitleFilter(It.IsAny<IGraphBuilder>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddTeletextSubtitleDecoder(It.IsAny<ITeletextSource>()), Times.Never);
    }

    [Test, Description("Add DvbSub3 filter to graph if DVB subtitles setting is enabled and TS stream contains only DVB subtitles")]
    public void ShouldAddDvbSubsFilter()
    {
      // Arranges
      SetMockServices();
      FakeVideoSettings settings = new FakeVideoSettings(new VideoSettings
      {
        EnableDvbSubtitles = true, EnableTeletextSubtitles = true, EnableAtscClosedCaptions = true
      });
      ServiceRegistration.Set<ISettingsManager>(settings);

      ISubtitleRenderer mockSubtitleRenderer = Mock.Of<ISubtitleRenderer>();
      TsReaderStub tsReaderStub = new TsReaderStub
      {
        TeletextStreamCount = 0,
        SubtitleStreamCount = 1
      };
      MockedTsVideoPlayer tsVideoPlayer = new MockedTsVideoPlayer(mockSubtitleRenderer, tsReaderStub);

      // Act
      tsVideoPlayer.AddSubtitleFilter();

      // Assert
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddDvbSubtitleFilter(It.IsAny<IGraphBuilder>()), Times.Once);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddClosedCaptionsFilter(It.IsAny<IGraphBuilder>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddTeletextSubtitleDecoder(It.IsAny<ITeletextSource>()), Times.Never);
    }

    [Test, Description("Render teletext subtitles if teletext subtitles setting is enabled and TS stream contains only teletext subtitles")]
    public void ShouldRenderTeletextSubtitles()
    {
      // Arranges
      SetMockServices();
      FakeVideoSettings settings = new FakeVideoSettings(new VideoSettings
      {
        EnableTeletextSubtitles = true, EnableDvbSubtitles = false, EnableAtscClosedCaptions = true
      });
      ServiceRegistration.Set<ISettingsManager>(settings);

      ISubtitleRenderer mockSubtitleRenderer = Mock.Of<ISubtitleRenderer>();
      TsReaderStub tsReaderStub = new TsReaderStub
      {
        TeletextStreamCount = 1,
        SubtitleStreamCount = 0
      };
      MockedTsVideoPlayer tsVideoPlayer = new MockedTsVideoPlayer(mockSubtitleRenderer, tsReaderStub);

      // Act
      tsVideoPlayer.AddSubtitleFilter();

      // Assert
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddTeletextSubtitleDecoder(It.IsAny<ITeletextSource>()), Times.Once);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddDvbSubtitleFilter(It.IsAny<IGraphBuilder>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddClosedCaptionsFilter(It.IsAny<IGraphBuilder>()), Times.Never);
    }

    [Test, Description("Add DvbSub3 filter to graph if both (DVB and Txt) subtitles setting are enabled and TS stream contains DVB and teletext subtitles ")]
    public void PreferDvbSubtitlesOverTeletextSubtitles()
    {
      // Arranges
      SetMockServices();
      FakeVideoSettings settings = new FakeVideoSettings(new VideoSettings
      {
        EnableTeletextSubtitles = true,
        EnableDvbSubtitles = true,
        EnableAtscClosedCaptions = false
      });
      ServiceRegistration.Set<ISettingsManager>(settings);

      ISubtitleRenderer mockSubtitleRenderer = Mock.Of<ISubtitleRenderer>();
      TsReaderStub tsReaderStub = new TsReaderStub
      {
        TeletextStreamCount = 1,
        SubtitleStreamCount = 1
      };
      MockedTsVideoPlayer tsVideoPlayer = new MockedTsVideoPlayer(mockSubtitleRenderer, tsReaderStub);

      // Act
      tsVideoPlayer.AddSubtitleFilter();

      // Assert
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddDvbSubtitleFilter(It.IsAny<IGraphBuilder>()), Times.Once);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddTeletextSubtitleDecoder(It.IsAny<ITeletextSource>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddClosedCaptionsFilter(It.IsAny<IGraphBuilder>()), Times.Never);
    }

    [Test, Description("Do NOT add CC filter to graph if TS stream contains DVB or teletext subtitles")]
    public void DoNotAddClosedCaptionsFilter()
    {
      // Arranges
      SetMockServices();
      FakeVideoSettings settings = new FakeVideoSettings(new VideoSettings
      {
        EnableAtscClosedCaptions = true, EnableDvbSubtitles = true, EnableTeletextSubtitles = true
      });
      ServiceRegistration.Set<ISettingsManager>(settings);

      ISubtitleRenderer mockSubtitleRenderer = Mock.Of<ISubtitleRenderer>();
      TsReaderStub tsReaderStub = new TsReaderStub
      {
        TeletextStreamCount = 1,
        SubtitleStreamCount = 1
      };
      MockedTsVideoPlayer tsVideoPlayer = new MockedTsVideoPlayer(mockSubtitleRenderer, tsReaderStub);

      // Act
      tsVideoPlayer.AddSubtitleFilter();

      // Assert
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddClosedCaptionsFilter(It.IsAny<IGraphBuilder>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddTeletextSubtitleDecoder(It.IsAny<ITeletextSource>()), Times.Never);
      Mock.Get(mockSubtitleRenderer).Verify(x => x.AddDvbSubtitleFilter(It.IsAny<IGraphBuilder>()), Times.Once);
    }

    private static void SetMockServices()
    {
      ServiceRegistration.Set(new ConsoleLogger(LogLevel.Debug, true));
      ServiceRegistration.Set(Mock.Of<IGeometryManager>());
      ServiceRegistration.Set(Mock.Of<IMessageBroker>());
    }
  }
}
