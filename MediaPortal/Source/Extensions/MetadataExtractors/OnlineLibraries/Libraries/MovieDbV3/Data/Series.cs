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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "backdrop_path": "/bzoZjhbpriBT2N5kwgK0weUfVOX.jpg",
  //  "created_by": [
  //    {
  //      "id": 66633,
  //      "name": "Vince Gilligan",
  //      "profile_path": "/rLSUjr725ez1cK7SKVxC9udO03Y.jpg"
  //    }
  //  ],
  //  "episode_run_time": [
  //    45,
  //    47
  //  ],
  //  "first_air_date": "2008-01-19",
  //  "genres": [
  //    {
  //      "id": 18,
  //      "name": "Drama"
  //    }
  //  ],
  //  "homepage": "http://www.amctv.com/shows/breaking-bad",
  //  "id": 1396,
  //  "in_production": false,
  //  "languages": [
  //    "en",
  //    "de",
  //    "ro",
  //    "es",
  //    "fa"
  //  ],
  //  "last_air_date": "2013-09-29",
  //  "name": "Breaking Bad",
  //  "networks": [
  //    {
  //      "id": 174,
  //      "name": "AMC"
  //    }
  //  ],
  //  "number_of_episodes": 62,
  //  "number_of_seasons": 5,
  //  "origin_country": [
  //    "US"
  //  ],
  //  "original_language": "en",
  //  "original_name": "Breaking Bad",
  //  "overview": "Breaking Bad is an American crime drama television series created and produced by Vince Gilligan. Set and produced in Albuquerque, New Mexico, Breaking Bad is the story of Walter White, a struggling high school chemistry teacher who is diagnosed with inoperable lung cancer at the beginning of the series. He turns to a life of crime, producing and selling methamphetamine, in order to secure his family's financial future before he dies, teaming with his former student, Jesse Pinkman. Heavily serialized, the series is known for positioning its characters in seemingly inextricable corners and has been labeled a contemporary western by its creator.",
  //  "popularity": 7.39820387093879,
  //  "poster_path": "/4yMXf3DW6oCL0lVPZaZM2GypgwE.jpg",
  //  "production_companies": [
  //    {
  //      "name": "Gran Via Productions",
  //      "id": 2605
  //    },
  //    {
  //      "name": "Sony Pictures Television",
  //      "id": 11073
  //    },
  //    {
  //      "name": "High Bridge Entertainment",
  //      "id": 33742
  //    }
  //  ],
  //  "seasons": [
  //    {
  //      "air_date": "2009-02-17",
  //      "episode_count": 6,
  //      "id": 3577,
  //      "poster_path": "/spPmYZAq2xLKQOEIdBPkhiRxrb9.jpg",
  //      "season_number": 0
  //    },
  //    {
  //      "air_date": "2008-01-19",
  //      "episode_count": 7,
  //      "id": 3572,
  //      "poster_path": "/dHCYpEoHEjAV6Xt3eyNthkdLRl3.jpg",
  //      "season_number": 1
  //    },
  //    {
  //      "air_date": "2009-03-08",
  //      "episode_count": 13,
  //      "id": 3573,
  //      "poster_path": "/ww6cDy0dhrVEdMqielNEsYz96mg.jpg",
  //      "season_number": 2
  //    },
  //    {
  //      "air_date": "2010-03-21",
  //      "episode_count": 13,
  //      "id": 3575,
  //      "poster_path": "/rINvcsYHUprsx9L8zNr5JltALda.jpg",
  //      "season_number": 3
  //    },
  //    {
  //      "air_date": "2011-07-17",
  //      "episode_count": 13,
  //      "id": 3576,
  //      "poster_path": "/ngnE7FFQqrrLgK3yVsv3kjwtQMZ.jpg",
  //      "season_number": 4
  //    },
  //    {
  //      "air_date": "2012-07-15",
  //      "episode_count": 16,
  //      "id": 3578,
  //      "poster_path": "/ih1JKNxEzW56azeFpEQmdu4poA4.jpg",
  //      "season_number": 5
  //    }
  //  ],
  //  "status": "Ended",
  //  "type": "Scripted",
  //  "vote_average": 9.2,
  //  "vote_count": 152
  //  "external_ids": {
  //    "imdb_id": "tt0903747",
  //    "freebase_mid": "/m/03d34x8",
  //    "freebase_id": "/en/breaking_bad",
  //    "tvdb_id": "81189",
  //    "tvrage_id": "18164"
  //  },
  //  "content_ratings": {
  //    "results": [
  //      {
  //        "iso_3166_1": "DE",
  //        "rating": "16"
  //      },
  //      {
  //        "iso_3166_1": "GB",
  //        "rating": "18"
  //      },
  //      {
  //        "iso_3166_1": "US",
  //        "rating": "TV-MA"
  //      }
  //    ]
  //  },
  //  "credits": {
  //    "cast": [
  //      {
  //        "character": "Walter White",
  //        "credit_id": "52542282760ee313280017f9",
  //        "id": 17419,
  //        "name": "Bryan Cranston",
  //        "profile_path": "/fnglrIFnI5cK4OAh66AZN86mkFq.jpg",
  //        "order": 0
  //      }
  //    ],
  //    "crew": [
  //      {
  //        "credit_id": "52542287760ee31328001af1",
  //        "department": "Production",
  //        "id": 66633,
  //        "name": "Vince Gilligan",
  //        "job": "Executive Producer",
  //        "profile_path": "/rLSUjr725ez1cK7SKVxC9udO03Y.jpg"
  //      }
  //    ]
  //  }
  //}
  [DataContract]
  public class Series : SeriesSearchResult
  {
    [DataMember(Name = "created_by")]
    public List<CrewItem> CreatedBy { get; set; }

    [DataMember(Name = "episode_run_time")]
    public List<int> EpisodeRuntime { get; set; }

    [DataMember(Name = "genres")]
    public List<Genre> Genres
    {
      get
      {
        return _corectedGenres;
      }
      set
      {
        _corectedGenres = new List<Genre>();
        foreach (Genre genre in value)
        {
          if (genre.Name.Contains("&"))
          {
            foreach (string splitGenre in genre.Name.Split('&'))
              _corectedGenres.Add(new Genre() { Id = genre.Id, Name = splitGenre.Trim() });
          }
          else
          {
            _corectedGenres.Add(genre);
          }
        }
      }
    }

    private List<Genre> _corectedGenres { get; set; }

  [DataMember(Name = "homepage")]
    public string Homepage { get; set; }

    [DataMember(Name = "in_production")]
    public bool InProduction { get; set; }

    [DataMember(Name = "languages")]
    public List<string> Languages { get; set; }

    [DataMember(Name = "networks")]
    public List<ProductionCompany> Networks { get; set; }

    [DataMember(Name = "number_of_episodes")]
    public int NumberOfEpisodes { get; set; }

    [DataMember(Name = "number_of_seasons")]
    public int NumberOfSeasons { get; set; }

    [DataMember(Name = "production_companies")]
    public List<ProductionCompany> ProductionCompanies { get; set; }

    [DataMember(Name = "seasons")]
    public List<SeriesSeason> Seasons { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "external_ids")]
    public ExternalIds ExternalId { get; set; }

    [DataMember(Name = "content_ratings")]
    public SeriesRatingResult ContentRatingResults { get; set; }

    [DataMember(Name = "credits")]
    public Credits SeriesCredits { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
