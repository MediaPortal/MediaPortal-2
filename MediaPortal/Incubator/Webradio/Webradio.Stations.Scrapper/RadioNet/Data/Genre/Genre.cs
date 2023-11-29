using System.Text.Json.Serialization;

namespace Webradio.Stations.RadioNet.Data.Genre;

public class Tag
{
  [JsonPropertyName("systemName")] public string systemName { get; set; }

  [JsonPropertyName("name")] public string name { get; set; }

  [JsonPropertyName("slug")] public string slug { get; set; }

  [JsonPropertyName("count")] public int count { get; set; }
}

public class Data
{
  [JsonPropertyName("tags")] public IList<Tag> tags { get; set; }
}

public class Ads
{
  [JsonPropertyName("pageType")] public string pageType { get; set; }
}

public class PageProps
{
  [JsonPropertyName("data")] public Data data { get; set; }

  [JsonPropertyName("ads")] public Ads ads { get; set; }
}

public class Props
{
  [JsonPropertyName("pageProps")] public PageProps pageProps { get; set; }

  [JsonPropertyName("__N_SSG")] public bool __N_SSG { get; set; }
}

public class Query
{
}

public class RuntimeConfig
{
}

public class DomainLocale
{
  [JsonPropertyName("domain")] public string domain { get; set; }

  [JsonPropertyName("defaultLocale")] public string defaultLocale { get; set; }
}

public class Root
{
  [JsonPropertyName("props")] public Props props { get; set; }

  [JsonPropertyName("page")] public string page { get; set; }

  [JsonPropertyName("query")] public Query query { get; set; }

  [JsonPropertyName("buildId")] public string buildId { get; set; }

  [JsonPropertyName("runtimeConfig")] public RuntimeConfig runtimeConfig { get; set; }

  [JsonPropertyName("isFallback")] public bool isFallback { get; set; }

  [JsonPropertyName("gsp")] public bool gsp { get; set; }

  [JsonPropertyName("locale")] public string locale { get; set; }

  [JsonPropertyName("locales")] public IList<string> locales { get; set; }

  [JsonPropertyName("defaultLocale")] public string defaultLocale { get; set; }

  [JsonPropertyName("domainLocales")] public IList<DomainLocale> domainLocales { get; set; }

  [JsonPropertyName("scriptLoader")] public IList<object> scriptLoader { get; set; }
}
