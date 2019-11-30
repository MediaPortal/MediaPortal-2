using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data
{
  public class ScriptableScript
  {
    private static ILogger Logger => ServiceRegistration.Get<ILogger>();
    // Regular expression patterns used by the multipart detection and cleaning methods
    // Matches the substrings "cd/dvd/disc/disk/part #" or "(# of #)"
    private const string _rxFileStackPattern = @"(\W*\b(cd|dvd|dis[ck]|part)\W*([a-z]|\d+|i+)(\W|$))|\W\d+\W*(of|-)\W*\d+\W$";
    private const string _rxFolderStackPattern = @"^(cd|dvd|dis[ck]|part)\W*([a-z]|\d+|i+)$";
    // Regular expression pattern that matches a selection of non-word characters
    private const string _rxMatchNonWordCharacters = @"[^\w]";
    private static readonly List<string> _supportedMoviePoperties = new List<string>
    {
      "title",
      "alternate_titles",
      "sortBy",
      "directors",
      "writers",
      "actors",
      "release_date",
      "year",
      "genres",
      "certification",
      "tagline",
      "summary",
      "studios",
      "collections",
      "score",
      "popularity",
      "runtime",
      "imdb_id",
    };

    private string _category = null;
    private string _defaultUserAgent = null;

    #region Properties

    public string Name => Scraper?.Name;
    public int ScriptID => Scraper?.ID ?? 0;
    public string Version => Scraper?.Version;
    public string Author => Scraper?.Author;
    public string Language
    {
      get
      {
        try
        {
          if (Scraper != null)
            return new CultureInfo(Scraper.Language).Name;

          return null;
        }
        catch (ArgumentException)
        {
          return "";
        }
      }
    }
    public string LanguageCode => Scraper?.Language;
    public DateTime? Published => Scraper?.Published;
    public string Description => Scraper?.Description;
    public string Category
    {
      get { return _category ?? Scraper?.Category; }
      set { _category = value; }
    }
    public bool ProvidesDetails { get; private set; }
    public bool ProvidesCoverArt { get; private set; }
    public bool ProvidesBackdrops { get; private set; }
    public ScriptableScraper Scraper { get; private set; }

    #endregion

    #region Public Methods

    public ScriptableScript(string defaultUserAgent)
    {
      _defaultUserAgent = defaultUserAgent;
    }

    public bool Load(string script)
    {
      Scraper = new ScriptableScraper(script);

      if (!Scraper.LoadSuccessful)
      {
        Scraper = null;
        return false;
      }

      ProvidesDetails = Scraper.ScriptTypes.Contains("MovieDetailsFetcher");
      ProvidesCoverArt = Scraper.ScriptTypes.Contains("MovieCoverFetcher");
      ProvidesBackdrops = Scraper.ScriptTypes.Contains("MovieBackdropFetcher");

      return true;
    }

    private bool TrySetMovieProperty(string property, string value, MovieInfo movie)
    {
      if (string.IsNullOrEmpty(property))
        return false;
      if (string.IsNullOrEmpty(value))
        return false;
      if (movie == null)
        return false;

      switch (property)
      {
        case "title":
          movie.MovieName = new SimpleTitle(value, false);
          return true;
        case "alternate_titles":
          movie.OriginalName = new StringList(value).FirstOrDefault();
          return true;
        case "sortBy":
          movie.MovieNameSort = new SimpleTitle(value, false);
          return true;
        case "directors":
          movie.Directors = new StringList(value).Select(s => new PersonInfo
          {
            Name = s,
            Occupation = PersonAspect.OCCUPATION_DIRECTOR
          }).ToList();
          return true;
        case "writers":
          movie.Writers = new StringList(value).Select(s => new PersonInfo
          {
            Name = s,
            Occupation = PersonAspect.OCCUPATION_WRITER
          }).ToList();
          return true;
        case "actors":
          movie.Actors = new StringList(value).Select(s => new PersonInfo
          {
            Name = s,
            Occupation = PersonAspect.OCCUPATION_ACTOR
          }).ToList();
          return true;
        case "release_date":
          movie.ReleaseDate = Convert.ToDateTime(value);
          return true;
        case "year":
          movie.ReleaseDate = movie.ReleaseDate ?? new DateTime(Convert.ToInt32(value), 1, 1);
          return true;
        case "genres":
          movie.Genres = new StringList(value).Select(s => new GenreInfo
          {
            Name = s,
          }).ToList();
          return true;
        case "certification":
          movie.Certification = value;
          return true;
        case "tagline":
          movie.Tagline = value;
          return true;
        case "summary":
          movie.Summary = new SimpleTitle(value, false);
          return true;
        case "studios":
          movie.ProductionCompanies = new StringList(value).Select(s => new CompanyInfo
          {
            Name = s,
            Type = CompanyAspect.COMPANY_PRODUCTION
          }).ToList();
          return true;
        case "collections":
          movie.CollectionName = new StringList(value).FirstOrDefault();
          return true;
        case "score":
          movie.Score = Convert.ToDouble(value, CultureInfo.InvariantCulture);
          return true;
        case "popularity":
          movie.Popularity = Convert.ToSingle(value, CultureInfo.InvariantCulture);
          return true;
        case "runtime":
          movie.Runtime = Convert.ToInt32(value);
          return true;
        case "imdb_id":
          movie.ImdbId = value;
          return true;
      }
      return false;
    }

    private bool TryGetMovieProperty(string property, MovieInfo movie, out string value)
    {
      value = null;
      if (string.IsNullOrEmpty(property))
        return false;
      if (movie == null)
        return false;

      switch (property)
      {
        case "title":
          value = movie.MovieName.Text;
          break;
        case "alternate_titles":
          value = string.IsNullOrEmpty(movie.OriginalName) ? "" : new StringList("|" + movie.OriginalName + "|").ToString();
          break;
        case "sortBy":
          value = movie.MovieNameSort.Text;
          break;
        case "directors":
          value = new StringList(movie.Directors.Select(p => p.Name)).ToString();
          break;
        case "writers":
          value = new StringList(movie.Writers.Select(p => p.Name)).ToString();
          break;
        case "actors":
          value = new StringList(movie.Actors.Select(p => p.Name)).ToString();
          break;
        case "release_date":
          value = movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.ToString(CultureInfo.InvariantCulture) : "";
          break;
        case "year":
          value = movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.Year.ToString() : "";
          break;
        case "genres":
          value = new StringList(movie.Genres.Select(g => g.Name)).ToString();
          break;
        case "certification":
          value = movie.Certification;
          break;
        case "tagline":
          value = movie.Tagline;
          break;
        case "summary":
          value = movie.Summary.ToString();
          break;
        case "studios":
          value = new StringList(movie.ProductionCompanies.Select(c => c.Name)).ToString();
          break;
        case "collections":
          value = movie.CollectionName.IsEmpty ? "": new StringList("|" + movie.CollectionName.Text + "|").ToString();
          break;
        case "score":
          value = Convert.ToString(movie.Score, CultureInfo.InvariantCulture);
          break;
        case "popularity":
          value = Convert.ToString(movie.Popularity, CultureInfo.InvariantCulture);
          break;
        case "runtime":
          value = Convert.ToString(movie.Runtime);
          break;
        case "imdb_id":
          value = movie.ImdbId;
          break;
      }
      return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Filters non descriptive words/characters from a title so that only keywords remain.
    /// </summary>
    /// <returns></returns>
    private string GetKeywords(string title)
    {
      // Remove articles and non-descriptive words
      string newTitle = BaseInfo.CleanString(title);
      newTitle = Regex.Replace(newTitle, @"\b(and|or|of|und|en|et|y)\b", "", RegexOptions.IgnoreCase);

      // Replace non-word characters with spaces
      newTitle = Regex.Replace(newTitle, _rxMatchNonWordCharacters, " ");

      // Remove double spaces and return the keywords
      return BaseInfo.CleanupWhiteSpaces(newTitle);
    }

    private string GetFileNameWithoutExtensionAndStackMarkers(string fileName)
    {
      // Remove the file extension from the filename
      string cleanFileName = Path.GetFileNameWithoutExtension(fileName);

      // If file is classified as multipart replace the stack markers with a space.
      Regex expr = new Regex(_rxFileStackPattern, RegexOptions.IgnoreCase);
      if (expr.Match(fileName).Success)
      {
        cleanFileName = expr.Replace(cleanFileName, " ");
      }

      // Trim the double spaces and return the cleaned filename
      return cleanFileName.Trim(' ');
    }

    public List<MovieInfo> SearchMovie(MovieInfo movieSearch)
    {
      if (Scraper == null)
        return null;

      if (!ProvidesDetails)
        return null;

      List<MovieInfo> rtn = new List<MovieInfo>();
      Dictionary<string, string> paramList = new Dictionary<string, string>();
      Dictionary<string, string> results;

      var mediaFile = movieSearch.SearchFilePath;
      string file = mediaFile != null ? Path.GetFileName(mediaFile) : null;
      string folderPath = mediaFile != null ? Path.GetDirectoryName(mediaFile) : null;
      string folder = folderPath != null ? new DirectoryInfo(folderPath).Name : null;

      if (!movieSearch.MovieName.IsEmpty) paramList["search.title"] = movieSearch.MovieName.Text;
      if (!movieSearch.MovieName.IsEmpty) paramList["search.keywords"] = GetKeywords(movieSearch.MovieName.Text);
      if (movieSearch.ReleaseDate.HasValue) paramList["search.year"] = movieSearch.ReleaseDate.Value.Year.ToString();
      if (movieSearch.ImdbId != null) paramList["search.imdb_id"] = movieSearch.ImdbId;
      //if (movieSignature.DiscId != null) paramList["search.disc_id"] = movieSignature.DiscId; //String version of the Disc ID (16 character hash of a DVD)
      //if (movieSignature.MovieHash != null) paramList["search.moviehash"] = movieSignature.MovieHash; //String version of the filehash of the first movie file (16 characters)
      if (folderPath != null) paramList["search.basepath"] = folderPath; //Complete path to the base folder
      if (folder != null) paramList["search.foldername"] = folder; //The base folder name of the movie
      if (file != null) paramList["search.filename"] = file; //The filename of the movie

      //set higher level settings for script to use
      if (_defaultUserAgent != null)
        paramList["settings.defaultuseragent"] = _defaultUserAgent;
      paramList["settings.mepo_data"] = ServiceRegistration.Get<IPathManager>().GetPath(@"<CONFIG>\ScriptableScraperProvider\");
      if (!Directory.Exists(paramList["settings.mepo_data"]))
        Directory.CreateDirectory(paramList["settings.mepo_data"]);

      // this variable is the filename without extension (and a second one without stackmarkers)
      if (!String.IsNullOrEmpty(file))
      {
        paramList["search.filename_noext"] = Path.GetFileNameWithoutExtension(file);
        paramList["search.clean_filename"] = GetFileNameWithoutExtensionAndStackMarkers(file);
      }

      results = Scraper.Execute("search", paramList);
      if (results == null)
      {
        Logger.Error("ScriptableScraperProvider: " + Name + " scraper script failed to execute \"search\" node.");
        return rtn;
      }

      int count = 0;
      // The movie result is only valid if the script supplies a unique site
      while (results.ContainsKey("movie[" + count + "].site_id"))
      {
        string siteId;
        string prefix = "movie[" + count + "].";
        count++;

        // if the result does not yield a site id it's not valid so skip it
        if (!results.TryGetValue(prefix + "site_id", out siteId))
          continue;

        string existingId = null;
        if (movieSearch.CustomIds.ContainsKey(Name))
          existingId = movieSearch.CustomIds[Name];

        // if this movie was already identified skip it
        if (existingId != null && existingId != siteId)
          continue;

        // if this movie does not have a valid title, don't bother
        if (!results.ContainsKey(prefix + "title"))
          continue;

        // We passed all checks so create a new movie object
        MovieInfo newMovie = new MovieInfo();

        // store the site id in the new movie object
        newMovie.CustomIds.Add(Name, siteId);
        newMovie.DataProviders.Add(Name);

        // Try to store all other fields in the new movie object
        foreach (string property in _supportedMoviePoperties)
        {
          string value;
          if (results.TryGetValue(prefix + property, out value))
            TrySetMovieProperty(property, value.Trim(), newMovie);
        }

        // add the movie to our movie results list
        rtn.Add(newMovie);
      }

      return rtn;
    }

    public List<string> GetArtwork(MovieInfo movie)
    {
      if (Scraper == null)
        return null;

      if (!ProvidesCoverArt)
        return null;

      Dictionary<string, string> paramList = new Dictionary<string, string>();
      Dictionary<string, string> results;
      List<string> rtn = new List<string>();

      // grab coverart loading settings
      int maxCoversInSession = 5;

      // try to load the id for the movie for this script
      if (movie.CustomIds.ContainsKey(Name))
        paramList["movie.site_id"] = movie.CustomIds[Name];
      else
        return null;

      // load params for scraper
      foreach (string property in _supportedMoviePoperties)
      {
        if (TryGetMovieProperty(property, movie, out string val))
          paramList["movie." + property] = val.Trim();
      }

      //set higher level settings for script to use
      if (_defaultUserAgent != null)
        paramList["settings.defaultuseragent"] = _defaultUserAgent;
      paramList["settings.mepo_data"] = ServiceRegistration.Get<IPathManager>().GetPath(@"<CONFIG>\ScriptableScraperProvider\");
      if (!Directory.Exists(paramList["settings.mepo_data"]))
        Directory.CreateDirectory(paramList["settings.mepo_data"]);

      // run the scraper
      results = Scraper.Execute("get_cover_art", paramList);
      if (results == null)
      {
        Logger.Error("ScriptableScraperProvider: " + Name + " scraper script failed to execute \"get_cover_art\" node.");
        return null;
      }

      int count = 0;
      while (results.ContainsKey("cover_art[" + count + "].url") || results.ContainsKey("cover_art[" + count + "].file"))
      {
        // if we have hit our limit quit
        if (rtn.Count >= maxCoversInSession)
          return rtn;

        // get url for cover
        if (results.ContainsKey("cover_art[" + count + "].url"))
        {
          string coverPath = results["cover_art[" + count + "].url"];
          if (coverPath.Trim() != string.Empty)
            rtn.Add(new Uri(coverPath).ToString());
        }

        // get file for cover
        if (results.ContainsKey("cover_art[" + count + "].file"))
        {
          string coverPath = results["cover_art[" + count + "].file"];
          if (coverPath.Trim() != string.Empty)
            rtn.Add(new Uri(coverPath).ToString());
        }

        count++;
      }

      if (rtn.Count > 0)
        return rtn;

      return null;
    }

    public List<string> GetBackdrops(MovieInfo movie)
    {
      if (Scraper == null)
        return null;

      if (!ProvidesBackdrops)
        return null;

      Dictionary<string, string> paramList = new Dictionary<string, string>();
      Dictionary<string, string> results;
      List<string> rtn = new List<string>();

      // grab backdrop loading settings
      int maxBackdropsInSession = 5;

      // try to load the id for the movie for this script
      if (movie.CustomIds.ContainsKey(Name))
        paramList["movie.site_id"] = movie.CustomIds[Name];
      else
        return null;

      foreach (string property in _supportedMoviePoperties)
      {
        if (TryGetMovieProperty(property, movie, out string val))
          paramList["movie." + property] = val.Trim();
      }

      //set higher level settings for script to use
      if (_defaultUserAgent != null)
        paramList["settings.defaultuseragent"] = _defaultUserAgent;
      paramList["settings.mepo_data"] = ServiceRegistration.Get<IPathManager>().GetPath(@"<CONFIG>\ScriptableScraperProvider\");
      if (!Directory.Exists(paramList["settings.mepo_data"]))
        Directory.CreateDirectory(paramList["settings.mepo_data"]);

      // run the scraper
      results = Scraper.Execute("get_backdrop", paramList);
      if (results == null)
      {
        Logger.Error("ScriptableScraperProvider: " + Name + " scraper script failed to execute \"get_backdrop\" node.");
        return null;
      }

      // Loop through all the results until a valid backdrop is found
      int count = 0;
      while (results.ContainsKey("backdrop[" + count + "].url") || results.ContainsKey("backdrop[" + count + "].file"))
      {
        // if we have hit our limit quit
        if (rtn.Count >= maxBackdropsInSession)
          return rtn;

        // attempt to load via a URL
        if (results.ContainsKey("backdrop[" + count + "].url"))
        {
          string backdropURL = results["backdrop[" + count + "].url"];
          if (backdropURL.Trim().Length > 0)
            rtn.Add(new Uri(backdropURL).ToString());
        }

        // attempt to load via a file
        if (results.ContainsKey("backdrop[" + count + "].file"))
        {
          string backdropFile = results["backdrop[" + count + "].file"];
          if (backdropFile.Trim().Length > 0)
            rtn.Add(new Uri(backdropFile).ToString());
        }

        count++;
      }

      if (rtn.Count > 0)
        return rtn;

      return null;
    }

    public override bool Equals(object obj)
    {
      if (obj == null) return false;

      if (obj.GetType() != typeof(ScriptableScript))
        return base.Equals(obj);

      return Version.Equals(((ScriptableScript)obj).Version) &&
             Scraper.ID == ((ScriptableScript)obj).Scraper.ID;
    }

    public override int GetHashCode()
    {
      return (Version + Scraper.ID).GetHashCode();
    }

    #endregion
  }
}
