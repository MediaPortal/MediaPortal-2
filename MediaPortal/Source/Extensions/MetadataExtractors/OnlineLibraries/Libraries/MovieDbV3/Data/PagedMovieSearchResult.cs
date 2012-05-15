using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  [DataContract]
  internal class PagedMovieSearchResult
  {
    [DataMember(Name = "page")]
    public int Page { get; set; }

    [DataMember(Name = "total_pages")]
    public int TotalPages { get; set; }

    [DataMember(Name = "total_results")]
    public int TotalResults { get; set; }

    [DataMember(Name = "results")]
    public List<MovieSearchResult> Results { get; set; }
  }
}