using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
//  {
//  "adult": false,
//  "backdrop_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
//  "belongs_to_collection": {
//      "backdrop_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
//      "id": 10,
//      "name": "Star Wars Collection",
//      "poster_path": "/6rddZZpxMQkGlpQYVVxb2LdQRI3.jpg"
//  },
//  "budget": 11000000,
//  "genres": [{
//      "id": 28,
//      "name": "Action"
//  },
//  {
//      "id": 14,
//      "name": "Fantasy"
//  },
//  {
//      "id": 878,
//      "name": "Science Fiction"
//  }],
//  "homepage": "http://www.starwars.com",
//  "id": 11,
//  "imdb_id": "tt0076759",
//  "original_title": "Star Wars: Episode IV: A New Hope",
//  "overview": "Princess Leia is captured and held hostage by the evil Imperial forces in their effort to take over the galactic Empire. Venturesome Luke Skywalker and dashing captain Han Solo team together with the loveable robot duo R2-D2 and C-3PO to rescue the beautiful princess and restore peace and justice in the Empire.",
//  "popularity": 84.8,
//  "poster_path": "/qoETrQ73Jbd2LDN8EUfNgUerhzG.jpg",
//  "production_companies": [{
//      "id": 1,
//      "name": "Lucasfilm"
//  },
//  {
//      "id": 8265,
//      "name": "Paramount"
//  }],
//  "production_countries": [{
//      "iso_3166_1": "TN",
//      "name": "Tunisia"
//  },
//  {
//      "iso_3166_1": "US",
//      "name": "United States of America"
//  }],
//  "release_date": "1977-12-27",
//  "revenue": 775398007,
//  "runtime": 121,
//  "spoken_languages": [{
//      "iso_639_1": "en",
//      "name": "English"
//  }],
//  "tagline": "A long time ago in a galaxy far, far away...",
//  "title": "Star Wars: Episode IV: A New Hope",
//  "vote_average": 8.8,
//  "vote_count": 75
//}
  [DataContract]
  public class Movie : MovieSearchResult
  {
    [DataMember(Name = "adult")]
    public bool Adult { get; set; }
    
    [DataMember(Name = "imdb_id")]
    public string ImdbId { get; set; }    
    
    [DataMember(Name = "overview")]
    public string Overview { get; set; }    
    
    [DataMember(Name = "tagline")]
    public string Tagline { get; set; }    

    [DataMember(Name = "revenue")]
    public long Revenue { get; set; }

    [DataMember(Name = "budget")]
    public long Budget { get; set; }

    [DataMember(Name = "runtime")]
    public int Runtime { get; set; }

    [DataMember(Name = "popularity")]
    public float Popularity { get; set; }
    
    [DataMember(Name = "homepage")]
    public string Homepage { get; set; }

    [DataMember(Name = "genres")]
    public List<Genre> Genres { get; set; }
  }
}
