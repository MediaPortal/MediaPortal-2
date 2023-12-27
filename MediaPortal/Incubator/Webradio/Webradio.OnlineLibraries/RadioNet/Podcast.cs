using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Webradio.OnlineLibraries.RadioNet
{
  [DataContract]
  public class Stream
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "parentId")]
    public string ParentId { get; set; }

    [DataMember(Name = "parentTitle")]
    public string ParentTitle { get; set; }

    [DataMember(Name = "publishDate")]
    public int PublishDate { get; set; }

    [DataMember(Name = "duration")]
    public int Duration { get; set; }

    [DataMember(Name = "parentLogo")]
    public string ParentLogo { get; set; }

    [DataMember(Name = "parentLogo44x44")]
    public string ParentLogo44x44 { get; set; }

    [DataMember(Name = "parentLogo100x100")]
    public string ParentLogo100x100 { get; set; }

    [DataMember(Name = "parentLogo175x175")]
    public string ParentLogo175x175 { get; set; }

    [DataMember(Name = "parentLogo300x300")]
    public string ParentLogo300x300 { get; set; }

    [DataMember(Name = "parentLogo630x630")]
    public string ParentLogo630x630 { get; set; }

    [DataMember(Name = "parentLogo1200x1200")]
    public string ParentLogo1200x1200 { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "contentFormat")]
    public string ContentFormat { get; set; }

    [DataMember(Name = "seoRelevant")]
    public bool SeoRelevant { get; set; }
  }

  [DataContract]
  public class TopicTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class GenreTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class CityTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class LanguageTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class RegionTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class CountryTag
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }
  }

  [DataContract]
  public class Broadcast
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "lastModified")]
    public int LastModified { get; set; }

    [DataMember(Name = "logo44x44")]
    public string Logo44x44 { get; set; }

    [DataMember(Name = "logo100x100")]
    public string Logo100x100 { get; set; }

    [DataMember(Name = "logo175x175")]
    public string Logo175x175 { get; set; }

    [DataMember(Name = "logo300x300")]
    public string Logo300x300 { get; set; }

    [DataMember(Name = "logo630x630")]
    public string Logo630x630 { get; set; }

    [DataMember(Name = "logo1200x1200")]
    public string Logo1200x1200 { get; set; }

    [DataMember(Name = "hasValidStreams")]
    public bool HasValidStreams { get; set; }

    [DataMember(Name = "streams")]
    public IList<Stream> Streams { get; set; }

    [DataMember(Name = "city")]
    public string City { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "topics")]
    public IList<string> Topics { get; set; }

    [DataMember(Name = "genres")]
    public IList<string> Genres { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "homepageUrl")]
    public string HomepageUrl { get; set; }

    [DataMember(Name = "adParams")]
    public string AdParams { get; set; }

    [DataMember(Name = "hideReferer")]
    public bool HideReferer { get; set; }

    [DataMember(Name = "continent")]
    public string Continent { get; set; }

    [DataMember(Name = "languages")]
    public IList<string> Languages { get; set; }

    [DataMember(Name = "families")]
    public IList<string> Families { get; set; }

    [DataMember(Name = "region")]
    public string Region { get; set; }

    [DataMember(Name = "topicTags")]
    public IList<TopicTag> TopicTags { get; set; }

    [DataMember(Name = "genreTags")]
    public IList<GenreTag> GenreTags { get; set; }

    [DataMember(Name = "cityTag")]
    public CityTag CityTag { get; set; }

    [DataMember(Name = "languageTags")]
    public IList<LanguageTag> LanguageTags { get; set; }

    [DataMember(Name = "regionTag")]
    public RegionTag RegionTag { get; set; }

    [DataMember(Name = "countryTag")]
    public CountryTag CountryTag { get; set; }

    [DataMember(Name = "rank")]
    public int Rank { get; set; }

    [DataMember(Name = "aliases")]
    public IList<object> Aliases { get; set; }

    [DataMember(Name = "enabled")]
    public bool Enabled { get; set; }

    [DataMember(Name = "seoRelevantIn")]
    public IList<string> SeoRelevantIn { get; set; }
  }

  [DataContract]
  public class Episode
  {

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "parentId")]
    public string ParentId { get; set; }

    [DataMember(Name = "parentTitle")]
    public string ParentTitle { get; set; }

    [DataMember(Name = "publishDate")]
    public int PublishDate { get; set; }

    [DataMember(Name = "duration")]
    public int Duration { get; set; }

    [DataMember(Name = "parentLogo")]
    public string ParentLogo { get; set; }

    [DataMember(Name = "parentLogo44x44")]
    public string ParentLogo44x44 { get; set; }

    [DataMember(Name = "parentLogo100x100")]
    public string ParentLogo100x100 { get; set; }

    [DataMember(Name = "parentLogo175x175")]
    public string ParentLogo175x175 { get; set; }

    [DataMember(Name = "parentLogo300x300")]
    public string ParentLogo300x300 { get; set; }

    [DataMember(Name = "parentLogo630x630")]
    public string ParentLogo630x630 { get; set; }

    [DataMember(Name = "parentLogo1200x1200")]
    public string ParentLogo1200x1200 { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "contentFormat")]
    public string ContentFormat { get; set; }

    [DataMember(Name = "seoRelevant")]
    public bool SeoRelevant { get; set; }
  }

  [DataContract]
  public class Episodes
  {

    [DataMember(Name = "totalCount")]
    public int TotalCount { get; set; }

    [DataMember(Name = "episodes")]
    public IList<Episode> EpisodeList { get; set; }
  }

  [DataContract]
  public class PodcastsInFamily
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "playables")]
    public IList<object> Playables { get; set; }

    [DataMember(Name = "displayType")]
    public string DisplayType { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "offset")]
    public int Offset { get; set; }

    [DataMember(Name = "totalCount")]
    public int TotalCount { get; set; }
  }

  //[DataContract]
  //public class Stream
  //{

  //  [DataMember(Name = "url")]
  //  public string Url { get; set; }

  //  [DataMember(Name = "contentFormat")]
  //  public string ContentFormat { get; set; }

  //  [DataMember(Name = "status")]
  //  public string Status { get; set; }
  //}

  [DataContract]
  public class Playable
  {

    [DataMember(Name = "city")]
    public string City { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "genres")]
    public IList<string> Genres { get; set; }

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "logo100x100")]
    public string Logo100x100 { get; set; }

    [DataMember(Name = "logo300x300")]
    public string Logo300x300 { get; set; }

    [DataMember(Name = "logo630x630")]
    public string Logo630x630 { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "topics")]
    public IList<string> Topics { get; set; }

    [DataMember(Name = "streams")]
    public IList<Stream> Streams { get; set; }

    [DataMember(Name = "hasValidStreams")]
    public bool HasValidStreams { get; set; }

    [DataMember(Name = "adParams")]
    public string AdParams { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "seoRelevantIn")]
    public IList<string> SeoRelevantIn { get; set; }
  }

  [DataContract]
  public class StationsInFamily
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "playables")]
    public IList<Playable> Playables { get; set; }

    [DataMember(Name = "displayType")]
    public string DisplayType { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "offset")]
    public int Offset { get; set; }

    [DataMember(Name = "totalCount")]
    public int TotalCount { get; set; }
  }

  //[DataContract]
  //public class Stream
  //{

  //  [DataMember(Name = "url")]
  //  public string Url { get; set; }

  //  [DataMember(Name = "contentFormat")]
  //  public string ContentFormat { get; set; }

  //  [DataMember(Name = "status")]
  //  public string Status { get; set; }
  //}

  //[DataContract]
  //public class Playable
  //{

  //  [DataMember(Name = "city")]
  //  public string City { get; set; }

  //  [DataMember(Name = "country")]
  //  public string Country { get; set; }

  //  [DataMember(Name = "genres")]
  //  public IList<string> Genres { get; set; }

  //  [DataMember(Name = "id")]
  //  public string Id { get; set; }

  //  [DataMember(Name = "logo100x100")]
  //  public string Logo100x100 { get; set; }

  //  [DataMember(Name = "logo300x300")]
  //  public string Logo300x300 { get; set; }

  //  [DataMember(Name = "logo630x630")]
  //  public string Logo630x630 { get; set; }

  //  [DataMember(Name = "name")]
  //  public string Name { get; set; }

  //  [DataMember(Name = "topics")]
  //  public IList<string> Topics { get; set; }

  //  [DataMember(Name = "streams")]
  //  public IList<Stream> Streams { get; set; }

  //  [DataMember(Name = "hasValidStreams")]
  //  public bool HasValidStreams { get; set; }

  //  [DataMember(Name = "adParams")]
  //  public string AdParams { get; set; }

  //  [DataMember(Name = "type")]
  //  public string Type { get; set; }

  //  [DataMember(Name = "seoRelevantIn")]
  //  public IList<string> SeoRelevantIn { get; set; }
  //}

  [DataContract]
  public class SimilarPodcasts
  {

    [DataMember(Name = "systemName")]
    public string SystemName { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "count")]
    public int Count { get; set; }

    [DataMember(Name = "offset")]
    public int Offset { get; set; }

    [DataMember(Name = "totalCount")]
    public int TotalCount { get; set; }

    [DataMember(Name = "playables")]
    public IList<Playable> Playables { get; set; }

    [DataMember(Name = "displayType")]
    public string DisplayType { get; set; }
  }

  [DataContract]
  public class Data
  {

    [DataMember(Name = "broadcast")]
    public Broadcast Broadcast { get; set; }

    [DataMember(Name = "episodes")]
    public Episodes Episodes { get; set; }

    [DataMember(Name = "podcastsInFamily")]
    public PodcastsInFamily PodcastsInFamily { get; set; }

    [DataMember(Name = "stationsInFamily")]
    public StationsInFamily StationsInFamily { get; set; }

    [DataMember(Name = "similarPodcasts")]
    public SimilarPodcasts SimilarPodcasts { get; set; }
  }

  [DataContract]
  public class Ads
  {

    [DataMember(Name = "pageType")]
    public string PageType { get; set; }

    [DataMember(Name = "params")]
    public string Params { get; set; }
  }

  [DataContract]
  public class PageProps
  {

    [DataMember(Name = "locale")]
    public string Locale { get; set; }

    [DataMember(Name = "data")]
    public Data Data { get; set; }

    [DataMember(Name = "ads")]
    public Ads Ads { get; set; }
  }

  [DataContract]
  public class Props
  {

    [DataMember(Name = "pageProps")]
    public PageProps PageProps { get; set; }

    [DataMember(Name = "__N_SSG")]
    public bool NSSG { get; set; }
  }

  [DataContract]
  public class Query
  {

    [DataMember(Name = "slug")]
    public string Slug { get; set; }
  }

  [DataContract]
  public class RuntimeConfig
  {
  }

  [DataContract]
  public class DomainLocale
  {

    [DataMember(Name = "domain")]
    public string Domain { get; set; }

    [DataMember(Name = "defaultLocale")]
    public string DefaultLocale { get; set; }
  }

  [DataContract]
  public class Podcast
  {

    [DataMember(Name = "props")]
    public Props Props { get; set; }

    [DataMember(Name = "page")]
    public string Page { get; set; }

    [DataMember(Name = "query")]
    public Query Query { get; set; }

    [DataMember(Name = "buildId")]
    public string BuildId { get; set; }

    [DataMember(Name = "runtimeConfig")]
    public RuntimeConfig RuntimeConfig { get; set; }

    [DataMember(Name = "isFallback")]
    public bool IsFallback { get; set; }

    [DataMember(Name = "gsp")]
    public bool Gsp { get; set; }

    [DataMember(Name = "locale")]
    public string Locale { get; set; }

    [DataMember(Name = "locales")]
    public IList<string> Locales { get; set; }

    [DataMember(Name = "defaultLocale")]
    public string DefaultLocale { get; set; }

    [DataMember(Name = "domainLocales")]
    public IList<DomainLocale> DomainLocales { get; set; }

    [DataMember(Name = "scriptLoader")]
    public IList<object> ScriptLoader { get; set; }
  }


}
