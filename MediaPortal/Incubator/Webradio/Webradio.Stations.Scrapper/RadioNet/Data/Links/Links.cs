using System.Text.Json.Serialization;

namespace Webradio.Stations.RadioNet.Data.Links;

public class Ads
{
  [JsonPropertyName("pageType")] public string pageType { get; set; }
}

public class CityTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class CountryTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class Data
{
  [JsonPropertyName("stations")] public Stations? stations { get; set; }

  [JsonPropertyName("page")] public int page { get; set; }

  [JsonPropertyName("toplistData")] public ToplistData toplistData { get; set; }

  [JsonPropertyName("seoText")] public string seoText { get; set; }
}

public class DomainLocale
{
  [JsonPropertyName("domain")] public string domain { get; set; }

  [JsonPropertyName("defaultLocale")] public string defaultLocale { get; set; }
}

public class Frequency
{
  [JsonPropertyName("area")] public string area { get; set; }

  [JsonPropertyName("type")] public string type { get; set; }

  [JsonPropertyName("value")] public double value { get; set; }
}

public class GenreTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class LanguageTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class MostFavorited
{
  [JsonPropertyName("id")] public string id { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("lastModified")] public int lastModified { get; set; }

  [JsonPropertyName("logo44x44")] public string logo44x44 { get; set; }

  [JsonPropertyName("logo100x100")] public string logo100x100 { get; set; }

  [JsonPropertyName("logo175x175")] public string logo175x175 { get; set; }

  [JsonPropertyName("logo300x300")] public string logo300x300 { get; set; }

  [JsonPropertyName("logo630x630")] public string logo630x630 { get; set; }

  [JsonPropertyName("logo1200x1200")] public string logo1200x1200 { get; set; }

  [JsonPropertyName("hasValidStreams")] public bool hasValidStreams { get; set; }

  [JsonPropertyName("streams")] public List<Stream> streams { get; set; }

  [JsonPropertyName("city")] public string city { get; set; }

  [JsonPropertyName("country")] public string country { get; set; }

  [JsonPropertyName("genres")] public List<string> genres { get; set; }

  [JsonPropertyName("type")] public string type { get; set; }

  [JsonPropertyName("description")] public string description { get; set; }

  [JsonPropertyName("homepageUrl")] public string homepageUrl { get; set; }

  [JsonPropertyName("adParams")] public string adParams { get; set; }

  [JsonPropertyName("hideReferer")] public bool hideReferer { get; set; }

  [JsonPropertyName("continent")] public string continent { get; set; }

  [JsonPropertyName("languages")] public List<string> languages { get; set; }

  [JsonPropertyName("families")] public List<string> families { get; set; }

  [JsonPropertyName("region")] public string region { get; set; }

  [JsonPropertyName("genreTags")] public List<GenreTag> genreTags { get; set; }

  [JsonPropertyName("cityTag")] public CityTag cityTag { get; set; }

  [JsonPropertyName("languageTags")] public List<LanguageTag> languageTags { get; set; }

  [JsonPropertyName("regionTag")] public RegionTag regionTag { get; set; }

  [JsonPropertyName("countryTag")] public CountryTag countryTag { get; set; }

  [JsonPropertyName("frequencies")] public List<Frequency> frequencies { get; set; }

  [JsonPropertyName("rank")] public int rank { get; set; }

  [JsonPropertyName("shortDescription")] public string shortDescription { get; set; }

  [JsonPropertyName("enabled")] public bool enabled { get; set; }

  [JsonPropertyName("seoRelevantIn")] public List<string> seoRelevantIn { get; set; }

  [JsonPropertyName("aliases")] public List<string> aliases { get; set; }
}

public class MostVisited
{
  [JsonPropertyName("id")] public string id { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("lastModified")] public int lastModified { get; set; }

  [JsonPropertyName("logo44x44")] public string logo44x44 { get; set; }

  [JsonPropertyName("logo100x100")] public string logo100x100 { get; set; }

  [JsonPropertyName("logo175x175")] public string logo175x175 { get; set; }

  [JsonPropertyName("logo300x300")] public string logo300x300 { get; set; }

  [JsonPropertyName("logo630x630")] public string logo630x630 { get; set; }

  [JsonPropertyName("logo1200x1200")] public string logo1200x1200 { get; set; }

  [JsonPropertyName("hasValidStreams")] public bool hasValidStreams { get; set; }

  [JsonPropertyName("streams")] public List<Stream> streams { get; set; }

  [JsonPropertyName("city")] public string city { get; set; }

  [JsonPropertyName("country")] public string country { get; set; }

  [JsonPropertyName("genres")] public List<string> genres { get; set; }

  [JsonPropertyName("type")] public string type { get; set; }

  [JsonPropertyName("description")] public string description { get; set; }

  [JsonPropertyName("homepageUrl")] public string homepageUrl { get; set; }

  [JsonPropertyName("adParams")] public string adParams { get; set; }

  [JsonPropertyName("hideReferer")] public bool hideReferer { get; set; }

  [JsonPropertyName("continent")] public string continent { get; set; }

  [JsonPropertyName("languages")] public List<string> languages { get; set; }

  [JsonPropertyName("families")] public List<string> families { get; set; }

  [JsonPropertyName("region")] public string region { get; set; }

  [JsonPropertyName("genreTags")] public List<GenreTag> genreTags { get; set; }

  [JsonPropertyName("cityTag")] public CityTag cityTag { get; set; }

  [JsonPropertyName("languageTags")] public List<LanguageTag> languageTags { get; set; }

  [JsonPropertyName("regionTag")] public RegionTag regionTag { get; set; }

  [JsonPropertyName("countryTag")] public CountryTag countryTag { get; set; }

  [JsonPropertyName("frequencies")] public List<Frequency> frequencies { get; set; }

  [JsonPropertyName("rank")] public int rank { get; set; }

  [JsonPropertyName("shortDescription")] public string shortDescription { get; set; }

  [JsonPropertyName("enabled")] public bool enabled { get; set; }

  [JsonPropertyName("seoRelevantIn")] public List<string> seoRelevantIn { get; set; }

  [JsonPropertyName("aliases")] public List<string> aliases { get; set; }

  [JsonPropertyName("topics")] public List<string> topics { get; set; }

  [JsonPropertyName("topicTags")] public List<TopicTag> topicTags { get; set; }
}

public class PageProps
{
  [JsonPropertyName("data")] public Data data { get; set; }

  [JsonPropertyName("ads")] public Ads ads { get; set; }
}

public class Playable
{
  [JsonPropertyName("city")] public string city { get; set; }

  [JsonPropertyName("country")] public string country { get; set; }

  [JsonPropertyName("genres")] public List<string> genres { get; set; }

  [JsonPropertyName("id")] public string id { get; set; }

  [JsonPropertyName("logo100x100")] public string logo100x100 { get; set; }

  [JsonPropertyName("logo300x300")] public string logo300x300 { get; set; }

  [JsonPropertyName("logo630x630")] public string logo630x630 { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("streams")] public List<Stream> streams { get; set; }

  [JsonPropertyName("hasValidStreams")] public bool hasValidStreams { get; set; }

  [JsonPropertyName("adParams")] public string adParams { get; set; }

  [JsonPropertyName("type")] public string type { get; set; }

  [JsonPropertyName("seoRelevantIn")] public List<string> seoRelevantIn { get; set; }

  [JsonPropertyName("topics")] public List<string> topics { get; set; }
}

public class PopularSearch
{
  [JsonPropertyName("id")] public string id { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("lastModified")] public int lastModified { get; set; }

  [JsonPropertyName("logo44x44")] public string logo44x44 { get; set; }

  [JsonPropertyName("logo100x100")] public string logo100x100 { get; set; }

  [JsonPropertyName("logo175x175")] public string logo175x175 { get; set; }

  [JsonPropertyName("logo300x300")] public string logo300x300 { get; set; }

  [JsonPropertyName("logo630x630")] public string logo630x630 { get; set; }

  [JsonPropertyName("logo1200x1200")] public string logo1200x1200 { get; set; }

  [JsonPropertyName("hasValidStreams")] public bool hasValidStreams { get; set; }

  [JsonPropertyName("streams")] public List<Stream> streams { get; set; }

  [JsonPropertyName("city")] public string city { get; set; }

  [JsonPropertyName("country")] public string country { get; set; }

  [JsonPropertyName("genres")] public List<string> genres { get; set; }

  [JsonPropertyName("type")] public string type { get; set; }

  [JsonPropertyName("description")] public string description { get; set; }

  [JsonPropertyName("homepageUrl")] public string homepageUrl { get; set; }

  [JsonPropertyName("adParams")] public string adParams { get; set; }

  [JsonPropertyName("hideReferer")] public bool hideReferer { get; set; }

  [JsonPropertyName("continent")] public string continent { get; set; }

  [JsonPropertyName("languages")] public List<string> languages { get; set; }

  [JsonPropertyName("families")] public List<string> families { get; set; }

  [JsonPropertyName("region")] public string region { get; set; }

  [JsonPropertyName("genreTags")] public List<GenreTag> genreTags { get; set; }

  [JsonPropertyName("cityTag")] public CityTag cityTag { get; set; }

  [JsonPropertyName("languageTags")] public List<LanguageTag> languageTags { get; set; }

  [JsonPropertyName("regionTag")] public RegionTag regionTag { get; set; }

  [JsonPropertyName("countryTag")] public CountryTag countryTag { get; set; }

  [JsonPropertyName("frequencies")] public List<Frequency> frequencies { get; set; }

  [JsonPropertyName("rank")] public int rank { get; set; }

  [JsonPropertyName("shortDescription")] public string shortDescription { get; set; }

  [JsonPropertyName("enabled")] public bool enabled { get; set; }

  [JsonPropertyName("seoRelevantIn")] public List<string> seoRelevantIn { get; set; }

  [JsonPropertyName("aliases")] public List<object> aliases { get; set; }

  [JsonPropertyName("topics")] public List<string> topics { get; set; }

  [JsonPropertyName("topicTags")] public List<TopicTag> topicTags { get; set; }
}

public class Props
{
  [JsonPropertyName("pageProps")] public PageProps pageProps { get; set; }

  [JsonPropertyName("__N_SSP")] public bool __N_SSP { get; set; }
}

public class Query
{
  [JsonPropertyName("slug")] public string slug { get; set; }
}

public class RegionTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class Root
{
  [JsonPropertyName("props")] public Props props { get; set; }

  [JsonPropertyName("page")] public string page { get; set; }

  [JsonPropertyName("query")] public Query query { get; set; }

  [JsonPropertyName("buildId")] public string buildId { get; set; }

  [JsonPropertyName("runtimeConfig")] public RuntimeConfig runtimeConfig { get; set; }

  [JsonPropertyName("isFallback")] public bool isFallback { get; set; }

  [JsonPropertyName("gssp")] public bool gssp { get; set; }

  [JsonPropertyName("locale")] public string locale { get; set; }

  [JsonPropertyName("locales")] public List<string> locales { get; set; }

  [JsonPropertyName("defaultLocale")] public string defaultLocale { get; set; }

  [JsonPropertyName("domainLocales")] public List<DomainLocale> domainLocales { get; set; }

  [JsonPropertyName("scriptLoader")] public List<object> scriptLoader { get; set; }
}

public class RuntimeConfig
{
}

public class SearchTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class Stations
{
  [JsonPropertyName("totalCount")] public int totalCount { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }

  [JsonPropertyName("offset")] public int offset { get; set; }

  [JsonPropertyName("searchTag")] public SearchTag searchTag { get; set; }

  [JsonPropertyName("playables")] public List<Playable> playables { get; set; }
}

public class Stream
{
  [JsonPropertyName("url")] public string url { get; set; }

  [JsonPropertyName("contentFormat")] public string contentFormat { get; set; }

  [JsonPropertyName("status")] public string status { get; set; }
}

public class TopicTag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class ToplistData
{
  [JsonPropertyName("popular_searches")] public List<PopularSearch> popular_searches { get; set; }

  [JsonPropertyName("most_visited")] public List<MostVisited> most_visited { get; set; }

  [JsonPropertyName("most_favorited")] public List<MostFavorited> most_favorited { get; set; }
}
