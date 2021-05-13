#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.SlimTv.Service;
using MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider;
using NUnit.Framework;
using OpenCvSharp.Dnn;

namespace Tests.Server.TvServer
{
  [TestFixture]
  public class TestTvServer
  {
    private DateTime _start = new DateTime(2000, 1, 3, 0, 0, 0);

    [SetUp]
    public void SetUp()
    {
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));
    }

    private MockTvServer GetTvServer()
    {
      MockTvServer tvServer = new MockTvServer(_start);

      tvServer.AddCard(1, "CAM Card (Transponder)", true, true);
      tvServer.AddCard(2, "Free Card (Transponder)", false, true);
      tvServer.AddCard(3, "Free Card", false, false);

      tvServer.AddTvChannel(1, "Channel 1", "Test");
      tvServer.AddTvChannel(2, "Channel 2", "Test");
      tvServer.AddTvChannel(3, "Channel 3", "Test");
      tvServer.AddTvChannel(4, "Channel 4", "Test");
      tvServer.AddTvChannel(5, "Channel 5", "Test");
      tvServer.AddTvChannel(6, "Channel 6", "Test");

      tvServer.AddDvbCTvTuningDetail(1, "Card 1 Channel 1", 1, 1, 100, 16, 6000, true);
      tvServer.AddDvbCTvTuningDetail(2, "Card 1 Channel 2", 1, 2, 100, 16, 6000, true);
      tvServer.AddDvbCTvTuningDetail(5, "Card 2 Channel 3", 2, 3, 101, 16, 6000, false);
      tvServer.AddDvbCTvTuningDetail(6, "Card 2 Channel 4", 2, 4, 101, 16, 6000, false);
      tvServer.AddDvbCTvTuningDetail(10, "Card 3 Channel 5", 3, 5, 102, 16, 6000, false);
      tvServer.AddDvbCTvTuningDetail(11, "Card 3 Channel 6", 3, 6, 103, 16, 6000, false);

      int progIs = 1;
      DateTime offset = _start;

      //Channel 1
      offset = _start;
      tvServer.AddSeriesProgram(progIs++, 1, "Series 1", "", offset.AddDays(0), offset.AddDays(0).AddHours(1), "Genre 1", 3, 1, 1, "Series 1 Episode S01E01");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 1", "", offset.AddDays(2), offset.AddDays(2).AddHours(1), "Genre 1", 3, 1, 1, "Series 1 Episode S01E01");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 1", "", offset.AddDays(7), offset.AddDays(7).AddHours(1), "Genre 1", 3, 1, 2, "Series 1 Episode S01E02");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 1", "", offset.AddDays(9), offset.AddDays(9).AddHours(1), "Genre 1", 3, 1, 2, "Series 1 Episode S01E02");

      offset = _start.AddHours(1);
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(0), offset.AddDays(0).AddHours(1), "Genre 2", 3, 1, 1, "Series 2 Episode S01E01");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(1), offset.AddDays(1).AddHours(1), "Genre 2", 3, 1, 2, "Series 2 Episode S01E02");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(2), offset.AddDays(2).AddHours(1), "Genre 2", 3, 1, 3, "Series 2 Episode S01E03");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(3), offset.AddDays(3).AddHours(1), "Genre 2", 3, 1, 4, "Series 2 Episode S01E04");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(4), offset.AddDays(4).AddHours(1), "Genre 2", 3, 1, 5, "Series 2 Episode S01E05");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(5), offset.AddDays(5).AddHours(1), "Genre 2", 3, 1, 6, "Series 2 Episode S01E06");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(6), offset.AddDays(6).AddHours(1), "Genre 2", 3, 1, 7, "Series 2 Episode S01E07");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(7), offset.AddDays(7).AddHours(1), "Genre 2", 3, 1, 8, "Series 2 Episode S01E08");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(8), offset.AddDays(8).AddHours(1), "Genre 2", 3, 1, 9, "Series 2 Episode S01E09");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(9), offset.AddDays(9).AddHours(1), "Genre 2", 3, 1, 10, "Series 2 Episode S01E10");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(10), offset.AddDays(10).AddHours(1), "Genre 2", 3, 1, 11, "Series 2 Episode S01E11");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(11), offset.AddDays(11).AddHours(1), "Genre 2", 3, 1, 12, "Series 2 Episode S01E12");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(12), offset.AddDays(12).AddHours(1), "Genre 2", 3, 1, 13, "Series 2 Episode S01E13");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 2", "", offset.AddDays(13), offset.AddDays(13).AddHours(1), "Genre 2", 3, 1, 14, "Series 2 Episode S01E14");

      offset = _start.AddHours(2);
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(0), offset.AddDays(0).AddHours(1), "Genre 3", 3, 1, 1, "Series 3 Episode S01E01");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(1), offset.AddDays(1).AddHours(1), "Genre 3", 3, 1, 2, "Series 3 Episode S01E02");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(2), offset.AddDays(2).AddHours(1), "Genre 3", 3, 1, 3, "Series 3 Episode S01E03");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(3), offset.AddDays(3).AddHours(1), "Genre 3", 3, 1, 4, "Series 3 Episode S01E04");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(4), offset.AddDays(4).AddHours(1), "Genre 3", 3, 1, 5, "Series 3 Episode S01E05");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(5), offset.AddDays(5).AddHours(1), "Genre 3", 3, 1, 6, "Series 3 Episode S01E06");
      tvServer.AddSeriesProgram(progIs++, 1, "Series 3", "", offset.AddDays(6), offset.AddDays(6).AddHours(1), "Genre 3", 3, 1, 7, "Series 3 Episode S01E07");

      //Channel 2
      offset = _start;
      tvServer.AddProgram(progIs++, 2, "Movie 1", "", offset.AddDays(0), offset.AddDays(0).AddHours(2), "Genre 4", 5);
      tvServer.AddProgram(progIs++, 2, "Movie 2", "", offset.AddDays(1), offset.AddDays(1).AddHours(2), "Genre 5", 4);
      tvServer.AddProgram(progIs++, 2, "Movie 3", "", offset.AddDays(2), offset.AddDays(2).AddHours(2), "Genre 6", 3);
      tvServer.AddProgram(progIs++, 2, "Movie 1", "", offset.AddDays(3), offset.AddDays(3).AddHours(2), "Genre 4", 5);
      tvServer.AddProgram(progIs++, 2, "Movie 2", "", offset.AddDays(4), offset.AddDays(4).AddHours(2), "Genre 5", 3);
      tvServer.AddProgram(progIs++, 2, "Movie 3", "", offset.AddDays(5), offset.AddDays(5).AddHours(2), "Genre 6", 3);
      tvServer.AddProgram(progIs++, 2, "Movie 4", "", offset.AddDays(6), offset.AddDays(6).AddHours(2), "Genre 7", 5);

      offset = _start.AddHours(2);
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E06", offset.AddDays(7), offset.AddDays(7).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E06");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E07", offset.AddDays(8), offset.AddDays(8).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E07");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E08", offset.AddDays(9), offset.AddDays(9).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E08");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E09", offset.AddDays(10), offset.AddDays(10).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E09");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E10", offset.AddDays(11), offset.AddDays(11).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E10");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E11", offset.AddDays(12), offset.AddDays(12).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E11");
      tvServer.AddSeriesProgram(progIs++, 2, "Series 3", "Series 3 S01E12", offset.AddDays(13), offset.AddDays(13).AddHours(1), "Genre 3", 3, -1, -1, "Series 3 Episode S01E12");

      //Channel 3
      offset = _start;
      tvServer.AddProgram(progIs++, 3, "Movie 7", "", offset.AddDays(0), offset.AddDays(0).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 8", "", offset.AddDays(1), offset.AddDays(1).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 9", "", offset.AddDays(2), offset.AddDays(2).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 10", "", offset.AddDays(3), offset.AddDays(3).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 11", "", offset.AddDays(4), offset.AddDays(4).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 12", "", offset.AddDays(5), offset.AddDays(5).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 3, "Movie 13", "", offset.AddDays(6), offset.AddDays(6).AddHours(2), "Genre 10", 3);

      offset = _start.AddHours(2);
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 14", offset.AddDays(0), offset.AddDays(0).AddHours(1), "Genre 4", 3, 2, 7, "Series 4 Episode S02E07");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 13", offset.AddDays(1), offset.AddDays(1).AddHours(1), "Genre 4", 3, 2, 6, "Series 4 Episode S02E06");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 12", offset.AddDays(2), offset.AddDays(2).AddHours(1), "Genre 4", 3, 2, 5, "Series 4 Episode S02E05");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 11", offset.AddDays(3), offset.AddDays(3).AddHours(1), "Genre 4", 3, 2, 4, "Series 4 Episode S02E04");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 10", offset.AddDays(4), offset.AddDays(4).AddHours(1), "Genre 4", 3, 2, 3, "Series 4 Episode S02E03");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 9", offset.AddDays(5), offset.AddDays(5).AddHours(1), "Genre 4", 3, 2, 2, "Series 4 Episode S02E02");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 8", offset.AddDays(6), offset.AddDays(6).AddHours(1), "Genre 4", 3, 2, 1, "Series 4 Episode S02E01");

      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 7", offset.AddDays(7), offset.AddDays(7).AddHours(1), "Genre 4", 3, 1, 7, "Series 4 Episode S01E07");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 6", offset.AddDays(8), offset.AddDays(8).AddHours(1), "Genre 4", 3, 1, 6, "Series 4 Episode S01E06");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 5", offset.AddDays(9), offset.AddDays(9).AddHours(1), "Genre 4", 3, 1, 5, "Series 4 Episode S01E5");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 4", offset.AddDays(10), offset.AddDays(10).AddHours(1), "Genre 4", 3, 1, 4, "Series 4 Episode S01E4");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 3", offset.AddDays(11), offset.AddDays(11).AddHours(1), "Genre 4", 3, 1, 3, "Series 4 Episode S01E3");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 2", offset.AddDays(12), offset.AddDays(12).AddHours(1), "Genre 4", 3, 1, 2, "Series 4 Episode S01E2");
      tvServer.AddSeriesProgram(progIs++, 3, "Series 4", "Description 1", offset.AddDays(13), offset.AddDays(13).AddHours(1), "Genre 4", 3, 1, 1, "Series 4 Episode S01E1");

      //Channel 4
      offset = _start;
      tvServer.AddProgram(progIs++, 4, "Movie 7", "", offset.AddDays(0), offset.AddDays(0).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 8", "", offset.AddDays(1), offset.AddDays(1).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 9", "", offset.AddDays(2), offset.AddDays(2).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 10", "", offset.AddDays(3), offset.AddDays(3).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 11", "", offset.AddDays(4), offset.AddDays(4).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 12", "", offset.AddDays(5), offset.AddDays(5).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 4, "Movie 13", "", offset.AddDays(6), offset.AddDays(6).AddHours(2), "Genre 10", 3);

      offset = _start.AddHours(3);
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(0), offset.AddDays(0).AddHours(1), "Genre 5", 3, 1, 1, "Series 5 Episode S01E01");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(1), offset.AddDays(1).AddHours(1), "Genre 5", 3, 1, 2, "Series 5 Episode S01E02");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(2), offset.AddDays(2).AddHours(1), "Genre 5", 3, 1, 3, "Series 5 Episode S01E03");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(3).AddHours(-0.2), offset.AddDays(3).AddHours(0.8), "Genre 5", 3, 1, 4, "Series 5 Episode S01E04");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(4), offset.AddDays(4).AddHours(1), "Genre 5", 3, 1, 5, "Series 5 Episode S01E05");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(5), offset.AddDays(5).AddHours(1), "Genre 5", 3, 1, 6, "Series 5 Episode S01E06");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(6).AddHours(0.1), offset.AddDays(6).AddHours(1.1), "Genre 5", 3, 1, 7, "Series 5 Episode S01E07");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(7), offset.AddDays(7).AddHours(1), "Genre 5", 3, 1, 8, "Series 5 Episode S01E08");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(8), offset.AddDays(8).AddHours(1), "Genre 5", 3, 1, 9, "Series 5 Episode S01E09");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(9), offset.AddDays(9).AddHours(1), "Genre 5", 3, 1, 10, "Series 5 Episode S01E10");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(10).AddHours(0.3), offset.AddDays(10).AddHours(1.3), "Genre 5", 3, 1, 11, "Series 5 Episode S01E11");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(11), offset.AddDays(11).AddHours(1), "Genre 5", 3, 1, 12, "Series 5 Episode S01E12");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(12), offset.AddDays(12).AddHours(1), "Genre 5", 3, 1, 13, "Series 5 Episode S01E13");
      tvServer.AddSeriesProgram(progIs++, 4, "Series 5", "", offset.AddDays(13), offset.AddDays(13).AddHours(1), "Genre 5", 3, 1, 14, "Series 5 Episode S01E14");

      //Channel 5
      offset = _start;
      tvServer.AddProgram(progIs++, 5, "Movie 14", "", offset.AddDays(0), offset.AddDays(0).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 5, "Movie 15", "", offset.AddDays(1), offset.AddDays(1).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 5, "Movie 16", "", offset.AddDays(2), offset.AddDays(2).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 5, "Movie 17", "", offset.AddDays(3), offset.AddDays(3).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 5, "Movie 18", "", offset.AddDays(4), offset.AddDays(4).AddHours(2), "Genre 9", 5);
      tvServer.AddProgram(progIs++, 5, "Movie 19", "", offset.AddDays(5), offset.AddDays(5).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 5, "Movie 20", "", offset.AddDays(6).AddMinutes(-15), offset.AddDays(6).AddHours(2).AddMinutes(-15), "Genre 10", 3);

      //Channel 6
      offset = _start;
      tvServer.AddProgram(progIs++, 6, "Movie 21", "", offset.AddDays(0), offset.AddDays(0).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 22", "", offset.AddDays(1), offset.AddDays(1).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 23", "", offset.AddDays(2), offset.AddDays(2).AddHours(2), "Genre 8", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 24", "", offset.AddDays(3), offset.AddDays(3).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 25", "", offset.AddDays(4), offset.AddDays(4).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 26", "", offset.AddDays(5), offset.AddDays(5).AddHours(2), "Genre 9", 3);
      tvServer.AddProgram(progIs++, 6, "Movie 27", "", offset.AddDays(6), offset.AddDays(6).AddHours(2), "Genre 10", 5);

      tvServer.Init();

      return tvServer;
    }

    [Test]
    public async Task TestSeriesWeeklySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.WeeklyEveryTimeOnThisChannel, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(2).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.WeeklyEveryTimeOnThisChannel, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
        result = await tvServer.GetProgramsForScheduleAsync(schedResult2.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 4, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 1"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesManagementWeeklySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.NewEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.WeeklyEveryTimeOnThisChannel, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(2).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.WeeklyEveryTimeOnThisChannel, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
        result = await tvServer.GetProgramsForScheduleAsync(schedResult2.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      var seriesId = Guid.NewGuid();
      tvServer.AddSeriesMediaItem(seriesId, "Series 1");
      int season = 1;
      int episode = 1;
      var recordingEnd = _start.AddDays(0).AddHours(1);
      for (int days = 0; days < 14; days++) //Simulate checks
      {
        var now = _start.AddDays(days);
        if (now >= recordingEnd)
        {
          recordingEnd = recordingEnd.AddDays(7);
          tvServer.AddSeriesEpisodeMediaItem(seriesId, $"Series 1 Episode S0{season}E{episode}", season, episode); //Simulate import
          episode++;
        }
        await tvServer.PreCheckSchedulesAsync(now, true);
      }

      //Assert
      Assert.IsTrue(programs.Count == 2, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 1"), "Wrongly recorded programs");
      Assert.IsTrue(tvServer.CancelledSchedules.Count == 2, "Found wrong number of cancelled programs");
    }

    [Test]
    public async Task TestSeriesDailySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 2", _start.AddDays(0).AddHours(1), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Daily, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 2"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesManagementNewDailySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.NewEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 4", _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), ScheduleRecordingType.Daily, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      var seriesId = Guid.NewGuid();
      tvServer.AddSeriesMediaItem(seriesId, "Series 4");
      int day = _start.Day;
      int season = 2;
      int episode = 7;
      for (int days = 0; days < 14; days++) //Simulate checks
      {
        await tvServer.PreCheckSchedulesAsync(_start.AddDays(days).AddHours(2), true);
        tvServer.AddSeriesEpisodeMediaItem(seriesId, $"Series 4 Episode S0{season}E{episode}", season, episode); //Simulate import

        episode--;
        if (episode == 0)
        {
          season--;
          episode = 7;
        }

        if (season == 0)
          break;
      }
      
      //Assert
      Assert.IsTrue(programs.Count == 1, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
      Assert.IsTrue(tvServer.CancelledSchedules.Count == 13, "Found wrong number of cancelled programs");
    }

    [Test]
    public async Task TestSeriesManagementMissingDailySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 4", _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), ScheduleRecordingType.Daily, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
      Assert.IsTrue(tvServer.CancelledSchedules.Count == 0, "Found wrong number of cancelled programs");
    }

    [Test]
    public async Task TestSeriesManagementMissingDailyScheduleWithMediaLibrary()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var seriesId = Guid.NewGuid();
      tvServer.AddSeriesMediaItem(seriesId, "Series 4");
      tvServer.AddSeriesEpisodeMediaItem(seriesId, "Series 4 Episode S01E3", 1, 3);
      tvServer.AddSeriesEpisodeMediaItem(seriesId, "Series 4 Episode S01E7", 1, 7);
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 4", _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), ScheduleRecordingType.Daily, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      for (int days = 0; days < 14; days++) //Simulate checks
        await tvServer.PreCheckSchedulesAsync(_start.AddDays(days).AddHours(2), true);

      //Assert
      Assert.IsTrue(programs.Count == 12, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
      Assert.IsTrue(tvServer.CancelledSchedules.Count == 2, "Found wrong number of cancelled programs");
    }

    [Test]
    public async Task TestSeriesWorkdaySchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 2", _start.AddDays(0).AddHours(1), _start.AddDays(0).AddHours(2), ScheduleRecordingType.WorkingDays, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 10, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 2"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesWeekendSchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 2", _start.AddDays(0).AddHours(1), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Weekends, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 4, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 2"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesCrossChannelSchedule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 3", _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), ScheduleRecordingType.EveryTimeOnEveryChannel, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 3"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestScrambledChannelSameTransponderSchedules()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 2);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<ISchedule> schedules = new List<ISchedule>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetRecordedSchedulesAsync(14);
        schedules.AddRange(result);
      }

      //Assert
      Assert.IsTrue(schedules.Count == 1, "Same transponder recording on scrambled channel should not be possible");
    }

    [Test]
    public async Task TestSameTransponderSchedules()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 4);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<ISchedule> schedules = new List<ISchedule>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetRecordedSchedulesAsync(14);
        schedules.AddRange(result);
      }

      //Assert
      Assert.IsTrue(schedules.Count == 2, "Same transponder recording on channels failed");
    }

    [Test]
    public async Task TestParallelSchedules()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 2);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<ISchedule> schedules = new List<ISchedule>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetRecordedSchedulesAsync(14);
        schedules.AddRange(result);
      }

      //Assert
      Assert.IsTrue(schedules.Count == 2, "Parallel recording failed");
    }

    [Test]
    public async Task TestSchedulePriority()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 5);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 6);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 14", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", (int)PriorityType.Lowest);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel, "Manual", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", (int)PriorityType.Normal);
      var schedResult3 = await tvServer.CreateScheduleDetailedAsync(channel2, "Manual", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", (int)PriorityType.High);
      var schedResult4 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 21", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", (int)PriorityType.Highest);
      List<ISchedule> schedules = new List<ISchedule>();
      if (schedResult.Success && schedResult2.Success && schedResult3.Success && schedResult4.Success)
      {
        var result = await tvServer.GetRecordedSchedulesAsync(14);
        schedules.AddRange(result);
      }

      //Assert
      Assert.IsTrue(schedules.Count == 1, "Parallel recording should not be possible");
      Assert.IsTrue(schedules.First().Name == "Movie 21", "Wrong schedule won by priority");
    }

    [Test]
    public async Task TestProgramGenres()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 2);
      tvServer.AddGenreMapping(EpgGenre.Series, "Genre 1");
      tvServer.AddGenreMapping(EpgGenre.Movie, "Genre 6");

      //Act
      var programResult = await tvServer.GetProgramsAsync(channel, _start, _start.AddDays(14));
      var programResult2 = await tvServer.GetProgramsAsync(channel2, _start, _start.AddDays(14));
      IProgram program1 = null;
      if (programResult.Success)
        program1 = programResult.Result.FirstOrDefault(p => p.Genre == "Genre 1");
      IProgram program2 = null;
      if (programResult2.Success)
        program2 = programResult2.Result.FirstOrDefault(p => p.Genre == "Genre 6");

      //Assert
      Assert.IsTrue(program1?.EpgGenreId == (int)EpgGenre.Series, "Wrong genre id for program 1");
      Assert.IsTrue(program2?.EpgGenreId == (int)EpgGenre.Movie, "Wrong genre id for program 2");
    }

    [Test]
    public async Task TestMovingProgram()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.DetectMovedPrograms = true;
      tvServer.MovedProgramsDetectionWindow = 30; //minutes
      var channel = tvServer.Channels.First(c => c.ChannelId == 4);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 5", _start.AddDays(0).AddHours(3), _start.AddDays(0).AddHours(4), ScheduleRecordingType.Daily, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 5"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestPreCheck()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.DetectMovedPrograms = true;
      tvServer.MovedProgramsDetectionWindow = 30; //minutes
      tvServer.MovedProgramsDetectionOffset = 30;
      var channel = tvServer.Channels.First(c => c.ChannelId == 5);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 20", _start.AddDays(6), _start.AddDays(6).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      await tvServer.PreCheckSchedulesAsync(_start);
      await tvServer.PreCheckSchedulesAsync(_start.AddDays(6).AddMinutes(-tvServer.MovedProgramsDetectionOffset).AddMinutes(5));
      
      //Assert
      Assert.IsTrue(!tvServer.Schedules.Contains(schedResult.Result), "Moved program was not detected by pre-check");
      Assert.IsTrue(tvServer.Schedules.Count == 1, "Pre-check did not create schedule for moved program");
    }

    [Test]
    public async Task TestSeriesRule()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 2",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateScheduleRuleAsync("Series 2 Rule", targets, null, channel, _start.AddDays(0).AddHours(1), _start.AddDays(0).AddHours(2), null, null, 
        RuleRecordingType.AllOnSameChannel, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 2"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesManagementNewRule()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.NewEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "Series",
        SearchTarget = RuleSearchTarget.Titel
      });
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "4",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 4 Rule", targets, null, null, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 4", null, null, null, null, RuleEpisodeInfoFallback.None, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesManagementMissingRule()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "Series",
        SearchTarget = RuleSearchTarget.Titel
      });
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "4",
        SearchTarget = RuleSearchTarget.Titel
      });
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "Description",
        SearchTarget = RuleSearchTarget.Description
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 4 Rule", targets, null, channel, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 4", null, null, null, null, RuleEpisodeInfoFallback.None, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 14, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesManagementMissingRuleWithMediaLibrary()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var seriesId = Guid.NewGuid();
      tvServer.AddSeriesMediaItem(seriesId, "Series 4");
      tvServer.AddSeriesEpisodeMediaItem(seriesId, "Series 4 Episode S01E3", 1, 3);
      tvServer.AddSeriesEpisodeMediaItem(seriesId, "Series 4 Episode S01E7", 1, 7);
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 4",
        SearchTarget = RuleSearchTarget.Titel
      });
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Genre 4",
        SearchTarget = RuleSearchTarget.Genre
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 4 Rule", targets, null, channel, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 4", null, null, null, null, RuleEpisodeInfoFallback.None, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 12, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesRuleRegex()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var channelGroup = tvServer.ChannelGroups.First(c => c.Name == "Test");

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 3",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 3 Rule", targets, channelGroup, null, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 3", null, null, null, @".*S(?<SeasonNo>\d{1,2})E(?<EpisodeNo>\d{1,2})", RuleEpisodeInfoFallback.DescriptionContainsSeasonEpisodeRegex, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 12, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 3"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesRuleSameChannel()
    {
      //Arrange
      var tvServer = GetTvServer();

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 3",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 3 Rule", targets, null, null, null, null, null, null,
        "Series 3", null, null, null, null, RuleEpisodeInfoFallback.None, RuleRecordingType.AllOnSameChannel, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 7, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 3"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestSeriesRuleSeason()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 4",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 4 Season 2 Rule", targets, null, channel, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 4", "2", null, null, null, RuleEpisodeInfoFallback.None, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 7, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
      Assert.IsTrue(programs.All(p => (p as IProgramSeries).SeasonNumber == "2"), "Wrongly recorded episode season");
    }

    [Test]
    public async Task TestSeriesRuleEpisodeTitle()
    {
      //Arrange
      var tvServer = GetTvServer();
      tvServer.EpisodeManagement = EpisodeManagementScheme.MissingEpisodesByEpisodeNumber;
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Series 4",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateSeriesScheduleRuleAsync("Series 4 Season 2 Rule", targets, null, channel, _start.AddDays(0).AddHours(2), _start.AddDays(0).AddHours(3), null, null,
        "Series 4", null, null, "Series 4 Episode S01E4", null, RuleEpisodeInfoFallback.None, RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Series 4"), "Wrongly recorded programs");
      Assert.IsTrue((programs.First() as IProgramSeries).EpisodeTitle == "Series 4 Episode S01E4", "Wrongly recorded episode title");
    }

    [Test]
    public async Task TestMovieRuleGoodGenre()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 2);
      tvServer.AddRecording("Movie 1");

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "Movie",
        SearchTarget = RuleSearchTarget.Titel
      });
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Include,
        SearchText = "5",
        SearchTarget = RuleSearchTarget.StarRating
      });
      var schedResult = await tvServer.CreateScheduleRuleAsync("Good Genre Movie Rule", targets, null, channel, null, null, null, null,
        RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title.Contains("Movie") && p.StarRating == 5), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestMovieRuleAutoDisable()
    {
      //Arrange
      var tvServer = GetTvServer();

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Movie 1",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateScheduleRuleAsync("Single Movie Rule", targets, null, null, null, null, null, null,
        RuleRecordingType.Once, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success)
      {
        var result = await tvServer.GetProgramsForScheduleRuleAsync(schedResult.Result);
        if (result.Success)
          programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Found wrong number of programs");
      Assert.IsTrue(programs.All(p => p.Title == "Movie 1"), "Wrongly recorded programs");
    }

    [Test]
    public async Task TestRuleRestart()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Movie 7",
        SearchTarget = RuleSearchTarget.Titel
      });
      var schedResult = await tvServer.CreateScheduleRuleAsync("Movie Rule", targets, null, channel, null, null, null, null,
        RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      await tvServer.PreCheckSchedulesAsync(_start);
      await tvServer.PreCheckSchedulesAsync(_start, true);

      //Assert
      Assert.IsTrue(tvServer.Schedules.Count == 1, "Rule restart recording same items");
    }

    [Test]
    public async Task TestScrambledChannelSameTransponderConflict()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 1);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 2);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Series 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(1), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 1", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetConflictsForScheduleAsync(schedResult2.Result);
        programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Same transponder recording on scrambled channel should have conflict");
      Assert.IsTrue(programs.All(p => p.Title == "Series 1"), "Wrong conflict");
    }

    [Test]
    public async Task TestSameTransponderConflict()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 4);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetConflictsForScheduleAsync(schedResult2.Result);
        programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 0, "Same transponder recording on channels should have no conflict");
    }

    [Test]
    public async Task TestConflict()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 5);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 6);

      //Act
      var schedResult = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 14", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel2, "Movie 21", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), ScheduleRecordingType.Once, 5, 5, "", 1);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetConflictsForScheduleAsync(schedResult2.Result);
        programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 1, "Same card recording on channels should have conflict");
      Assert.IsTrue(programs.All(p => p.Title == "Movie 14"), "Wrong conflict");
    }

    [Test]
    public async Task TestRuleConflict()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 5);
      var channel2 = tvServer.Channels.First(c => c.ChannelId == 6);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Genre 9",
        SearchTarget = RuleSearchTarget.Genre
      });
      var schedResult = await tvServer.CreateScheduleRuleAsync("Movie Rule 1", targets, null, channel, null, null, null, null,
        RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      var schedResult2 = await tvServer.CreateScheduleRuleAsync("Movie Rule 2", targets, null, channel2, null, null, null, null,
        RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);
      List<IProgram> programs = new List<IProgram>();
      if (schedResult.Success && schedResult2.Success)
      {
        var result = await tvServer.GetConflictsForScheduleRuleAsync(schedResult2.Result);
        programs.AddRange(result.Result);
      }

      //Assert
      Assert.IsTrue(programs.Count == 3, "Same card recording on channel should have conflicts");
      Assert.IsTrue(programs.All(p => p.Genre == "Genre 9"), "Wrong conflicts");
    }

    [Test]
    public async Task TestProgramScheduleStatus()
    {
      //Arrange
      var tvServer = GetTvServer();
      var channel = tvServer.Channels.First(c => c.ChannelId == 3);

      //Act
      List<IScheduleRuleTarget> targets = new List<IScheduleRuleTarget>();
      targets.Add(new ScheduleRuleTarget
      {
        SearchMatch = RuleSearchMatch.Exact,
        SearchText = "Genre 9",
        SearchTarget = RuleSearchTarget.Genre
      });
      var schedResult1 = await tvServer.CreateScheduleDetailedAsync(channel, "Series 4", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(1), 
        ScheduleRecordingType.EveryTimeOnThisChannel, 5, 5, "", 1);
      var schedResult2 = await tvServer.CreateScheduleDetailedAsync(channel, "Movie 7", _start.AddDays(0).AddHours(0), _start.AddDays(0).AddHours(2), 
        ScheduleRecordingType.Once, 5, 5, "", 1);
      var schedResult3 = await tvServer.CreateScheduleRuleAsync("Movie Rule", targets, null, channel, null, null, null, null,
        RuleRecordingType.All, 5, 5, 1, KeepMethodType.Always, null);

      await tvServer.PreCheckSchedulesAsync(_start, true);
      var programResult = await tvServer.GetProgramsAsync(channel, _start, _start.AddDays(14));
      List<(IProgram Program, RecordingStatus Status)> programStatus = new List<(IProgram Program, RecordingStatus Status)>();
      if (schedResult1.Success && schedResult2.Success && schedResult3.Success && programResult.Success)
      {
        foreach (var prog in programResult.Result)
        {
          programStatus.Add((prog, (await tvServer.GetRecordingStatusAsync(prog)).Result));
        }
      }

      var seriesRecordings = programStatus.Where(p => p.Program.Title == "Series 4").ToList();
      var movieRecordings = programStatus.Where(p => p.Program.Title == "Movie 7").ToList();
      var ruleRecordings = programStatus.Where(p => p.Program.Genre == "Genre 9").ToList();

      //Assert
      Assert.IsTrue(seriesRecordings.Count > 0 && seriesRecordings.All(p => p.Status.HasFlag(RecordingStatus.SeriesScheduled)), "Invalid recording status for series");
      Assert.IsTrue(movieRecordings.Count > 0 && movieRecordings.All(p => p.Status.HasFlag(RecordingStatus.Scheduled)), "Invalid recording status for movies");
      Assert.IsTrue(ruleRecordings.Count > 0 && ruleRecordings.All(p => p.Status.HasFlag(RecordingStatus.RuleScheduled)), "Invalid recording status for rule");
    }
  }
}
