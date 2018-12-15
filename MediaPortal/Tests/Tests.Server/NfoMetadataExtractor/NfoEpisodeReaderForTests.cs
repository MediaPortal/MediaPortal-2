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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;

namespace Tests.Server.NfoMetadataExtractor
{
  class NfoEpisodeReaderForTests : NfoSeriesEpisodeReader
  {
    protected SeriesEpisodeStub _episodeStub;
    protected SeriesStub _seriesStub;

    public NfoEpisodeReaderForTests(bool includeEpisodeStub)
      : base(new ConsoleLogger(LogLevel.All, true), 1, false, false, null, new NfoSeriesMetadataExtractorSettings())
    {
      _episodeStub = includeEpisodeStub ? CreateEpisodeStub(1, 1) : new SeriesEpisodeStub();
      _seriesStub = CreateSeriesStub();
      _stubs.Add(_episodeStub);
      SetSeriesStubs(new List<SeriesStub> { _seriesStub });
    }

    public SeriesEpisodeStub EpisodeStub
    {
      get { return _episodeStub; }
    }

    public SeriesStub SeriesStub
    {
      get { return _seriesStub; }
    }

    protected SeriesEpisodeStub CreateEpisodeStub(int season, int episode)
    {
      return new SeriesEpisodeStub
      {
        Title = "TestEpisode",
        ShowTitle = "TestEpisodeShowTitle",
        Season = season,
        DisplaySeason = season,
        Episodes = new HashSet<int> { episode },
        DisplayEpisode = episode,
        PlayCount = 1,
        Watched = true,
        LastPlayed = new DateTime(2000, 1, 1),
        Actors = new HashSet<PersonStub>(CreateTestActors("Episode")),
        Director = "TestEpisodeDirector",
        Credits = new HashSet<string> { "TestEpidodeWriter1", "TestEpidodeWriter2" },
        Plot = "TestEpisodePlot",
        Outline = "TestEpisodeOutline",
        Id = 1,
        Year = new DateTime(2000, 1, 1),
        DvdEpisodes = new HashSet<decimal> { episode },
        Aired = new DateTime(2000, 1, 31),
        Rating = 1.1m,
        Votes = 1,
      };
    }

    protected SeriesStub CreateSeriesStub()
    {
      return new SeriesStub
      {
        Title = "TestSeries",
        ShowTitle = "TestSeriesShowTitle",
        Episodes = new HashSet<SeriesEpisodeStub> { CreateEpisodeStub(1,1), CreateEpisodeStub(1,2) },
        Actors = new HashSet<PersonStub>(CreateTestActors("Series")),
        Plot = "TestSeriesPlot",
        Outline = "TestSeriesOutline",
        Id = 2,
        Year = new DateTime(2000, 1, 1),
        Rating = 2.2m,
        Votes = 2,
      };
    }

    protected IList<PersonStub> CreateTestActors(string prefix)
    {
      return new List<PersonStub>
      {
        new PersonStub
        {
          Name = prefix + "Actor1",
          Role = prefix + "Role1",
          Biography = prefix + "Bio1",
          Birthdate = new DateTime(2000,1,1),
          Deathdate = new DateTime(2001,1,1),
          Order = 1,
          ImdbId = "tt11111",
        },
        new PersonStub
        {
          Name = prefix + "Actor2",
          Role = prefix + "Role2",
          Biography = prefix + "Bio2",
          Birthdate = new DateTime(2002,1,1),
          Deathdate = new DateTime(2003,1,1),
          Order = 2,
          ImdbId = "tt22222",
        }};
    }
  }
}
