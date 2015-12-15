extern alias OV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using OV::OnlineVideos;
using OV::OnlineVideos.Downloading;
using OV::OnlineVideos.Helpers;
using OV::OnlineVideos.MPUrlSourceFilter;
using OV::OnlineVideos.OnlineVideosWebservice;
using OV::OnlineVideos.Sites;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  public class OnlineVideosManager
  {
    protected readonly object _syncObject = new object();
    protected IWork _currentBackgroundTask = null;
    
    /// <summary>
    /// Constructor: Checks if an update is required and makes some initializations
    /// </summary>
    internal OnlineVideosManager()
    {
      InitOnlineVideosSettings();
      
      OnlineVideoSettings.Instance.LoadSites();
      // force autoupdate when no dlls or icons or banners are found -> fresh install
      bool forceUpdate = System.IO.Directory.GetFiles(OnlineVideoSettings.Instance.DllsDir, "OnlineVideos.Sites.*.dll").Length == 0 || System.IO.Directory.GetFiles(OnlineVideoSettings.Instance.ThumbsDir, "*.png", System.IO.SearchOption.AllDirectories).Length == 0;
      Logger.Info("OnlineVideos: Run update: {0}", forceUpdate);
      if (forceUpdate || DateTime.Now - MP2Extended.Settings.OnlineVideosLastAutomaticUpdate > TimeSpan.FromHours(1))
      {
        RunUpdate();
      }
      OnlineVideoSettings.Instance.BuildSiteUtilsList();
    }

    /// <summary>
    /// Returns all Sites the User has selected
    /// </summary>
    /// <returns>Returns a list of Sites</returns>
    internal List<SiteUtilBase> GetSites()
    {
      return OnlineVideoSettings.Instance.SiteUtilsList.Values.ToList();
    }

    /// <summary>
    /// Retruns all available sites on the OnlineVideos server
    /// </summary>
    /// <returns>Returns a list of Sites</returns>
    internal List<GlobalSite> GetGlobalSites()
    {
      List<GlobalSite> output = new List<GlobalSite>();
      Site[] globalSites = Updater.OnlineSites;
      if (globalSites != null)
        foreach (var site in globalSites)
        {
          output.Add(new GlobalSite
          {
            Site = site,
            Added = OnlineVideoSettings.Instance.SiteSettingsList.FirstOrDefault(i => i.Name == site.Name) != null
          });
        }

      return output;
    }

    /// <summary>
    /// Returns all Categories for a given Site
    /// </summary>
    /// <param name="siteName"></param>
    /// <returns></returns>
    internal List<Category> GetSiteCategories(string siteName)
    {   
      SiteUtilBase site;
      if (!OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out site))
        return new List<Category>();
      if (!site.Settings.DynamicCategoriesDiscovered)
        try
        {
          site.DiscoverDynamicCategories();
        }
        catch (Exception ex)
        {
          Logger.Error("OnlineVideosManager: Error in DiscoverDynamicCategories", ex);
        }

      List<Category> categories = site.Settings.Categories.ToList();
      // be clever and already download the Thumbs in the background
      ImageDownloader.GetImages(categories);

      return categories;
    }

    internal List<Category> GetSubCategories(string siteName, string categoryRecursiveName)
    {
      List<Category> output = new List<Category>();
      
      SiteUtilBase site;
      if (!OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out site))
        return output;
      try
      { 
        if (!site.Settings.DynamicCategoriesDiscovered)
          site.DiscoverDynamicCategories();

        string[] categories = categoryRecursiveName.Split(new string[] { " / " }, StringSplitOptions.None);

        Category baseCategory = GetSiteCategories(siteName).Single(x => x.Name == categories.First());
        Category lastCategory = null;

        // if the base category doesn't have any subcategories the last category is equal to the base category
        if (categories.Length > 1)
          for (int i = 1; i < categories.Length; i++)
          {
            lastCategory = lastCategory == null ? GetSubCategories(siteName, baseCategory.RecursiveName()).Single(x => x.Name == categories[i]) : GetSubCategories(siteName, lastCategory.RecursiveName()).Single(x => x.Name == categories[i]);
          }
        else
          lastCategory = baseCategory;

        if (!lastCategory.HasSubCategories)
          return output;

        if (!lastCategory.SubCategoriesDiscovered)
          try
          {
            site.DiscoverSubCategories(lastCategory);
          }
          catch (Exception ex)
          {
            Logger.Error("OnlineVideosManager: Error in DiscoverSubCategories", ex);
          }

        output = lastCategory.SubCategories;
      }
      catch (Exception ex)
      {
        Logger.Warn("Error loading Subcategory for site '{0}', Error: {1} StackTrace: {2}", site.Settings.Name, ex.Message, ex.StackTrace);
      }

      // be clever and already download the Thumbs in the background
      ImageDownloader.GetImages(output);

      return output;
    }

    /// <summary>
    /// This function returns all Ctegories including Subcategories as a Dictionary
    /// </summary>
    /// <returns>[RecursiveName, Category]</returns>
    private Dictionary<string, Category> GetAllCategories(string siteName)
    {
      SiteUtilBase site;
      if (!OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out site))
        return new Dictionary<string, Category>();

      Dictionary<string, Category> allCategories = new Dictionary<string, Category>();
      foreach (var category in GetSiteCategories(siteName))
      {
        string mainCategoryRecursiveName = category.RecursiveName();
        if (!allCategories.ContainsKey(mainCategoryRecursiveName))
          allCategories.Add(mainCategoryRecursiveName, category);
        if (category.HasSubCategories)
          if (!category.SubCategoriesDiscovered)
            site.DiscoverSubCategories(category);
        foreach (var subCategory in category.SubCategories)
        {
          if (!allCategories.ContainsKey(subCategory.RecursiveName()))
            allCategories.Add(subCategory.RecursiveName(), subCategory);
        }
      }

      return allCategories;
    }

    /// <summary>
    /// Gets all Videos for a given Category
    /// </summary>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    /// <returns></returns>
    internal List<VideoInfo> GetCategoryVideos(string siteName, string categoryRecursiveName)
    {
      List<VideoInfo> output = new List<VideoInfo>();
      
      SiteUtilBase site;
      if (!OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out site))
        return output;

      string[] categories = categoryRecursiveName.Split(new string[] { " / " }, StringSplitOptions.None);

      Category baseCategory = GetSiteCategories(siteName).Single(x => x.Name == categories.First());
      Category lastCategory = null;

      // if the base category doesn't have any subcategories directly return the videos
      if (categories.Length == 1)
      {
        List<VideoInfo> videos = site.GetVideos(baseCategory).ToList();
        // be clever and already download the Thumbs in the background
        ImageDownloader.GetImages(videos);

        return videos;
      }

      for (int i = 1; i < categories.Length; i++)
      {
        lastCategory = lastCategory == null ? GetSubCategories(siteName, baseCategory.RecursiveName()).Single(x => x.Name == categories[i]) : GetSubCategories(siteName, lastCategory.RecursiveName()).Single(x => x.Name == categories[i]);
      }

      List<VideoInfo> videos2 = site.GetVideos(lastCategory).ToList();
      // be clever and already download the Thumbs in the background
      ImageDownloader.GetImages(videos2);

      return videos2;
    }

    /// <summary>
    /// Gets all Urls for a given Video
    /// </summary>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    /// <param name="videoUrl"></param>
    /// <returns></returns>
    public List<string> GetVideoUrls(string siteName, string categoryRecursiveName, string videoUrl)
    {
      List<string> output = new List<string>();
      
      SiteUtilBase site;
      if (!OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out site))
        return output;

      foreach (var video in GetCategoryVideos(siteName, categoryRecursiveName))
      {
        if (video.VideoUrl == videoUrl)
        {
          List<string> urls = site.GetMultipleVideoUrls(video);
          UriUtils.RemoveInvalidUrls(urls);
          output = urls;
        }
      }

      return output;
    }

    /// <summary>
    /// Returns the UserSettings for a Site by it's name. It only returns the settings, which are in the Namespace "System", are bool or enum.
    /// </summary>
    /// <param name="siteName"></param>
    /// <returns></returns>
    public List<WebOnlineVideosSiteSetting> GetSiteSettings(string siteName)
    {
      SiteUtilBase site = GetSites().Single(x => x.Settings.Name == siteName);
      List<WebOnlineVideosSiteSetting> output = new List<WebOnlineVideosSiteSetting>();
      foreach (var prop in site.GetUserConfigurationProperties())
      {
        // limit to what the UI can show
        if (prop.IsEnum || prop.IsBool || prop.Namespace == "System")
          output.Add(new WebOnlineVideosSiteSetting
          {
            SiteId = OnlineVideosIdGenerator.BuildSiteId(siteName),
            Name = prop.DisplayName,
            Description = prop.Description,
            Value = site.GetConfigValueAsString(prop),
            PossibleValues = prop.IsEnum ? prop.GetEnumValues() : new string[0],
            IsBool = prop.IsBool
          });
          //Logger.Info("SiteSetting: {0} - {1} isBool: {2} isEnum: {3} EnumValues: {4} Value: {5} NameSpace: {6}", site.Settings.Name, prop.DisplayName, prop.IsBool.ToString(), prop.IsEnum.ToString(), prop.IsEnum ? string.Join(", ", prop.GetEnumValues()) : "", site.GetConfigValueAsString(prop), prop.Namespace);
      }
      return output;
    }

    /// <summary>
    /// Changes the value of a site property
    /// </summary>
    /// <param name="siteName">The name of the Site</param>
    /// <param name="propertygName">The DisplayName of the Property</param>
    /// <param name="value">The new value</param>
    /// <returns></returns>
    public bool SetSiteSetting(string siteName, string propertygName, string value)
    {
      SiteUtilBase site = GetSites().Single(x => x.Settings.Name == siteName);
      if (site == null)
        return false;
      var property = site.GetUserConfigurationProperties().Single(x => x.DisplayName == propertygName);
      if (property == null)
        return false;
      try
      {
        site.SetConfigValueFromString(property, value);
      }
      catch (Exception ex)
      {
        Logger.Error("OnlineVideosManager: Error changing Site settings!", ex);
        return false;
      }
      return true;
    }

    #region private

    private void InitOnlineVideosSettings()
    {
      string ovConfigPath = ServiceRegistration.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}\", Environment.UserName));
      string ovDataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OnlineVideos");
      
      OnlineVideoSettings.Instance.Logger = new LogDelegator();
      OnlineVideoSettings.Instance.UserStore = new UserSiteSettingsStore();

      OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(ovDataPath, "SiteUtils");
      OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(ovDataPath, "Thumbs");
      OnlineVideoSettings.Instance.ConfigDir = ovConfigPath;

      OnlineVideoSettings.Instance.AddSupportedVideoExtensions(new List<string> { ".asf", ".asx", ".flv", ".m4v", ".mov", ".mkv", ".mp4", ".wmv" });

      // Settings configured by MP2Ext
      OnlineVideoSettings.Instance.UseAgeConfirmation = MP2Extended.Settings.OnlineVideosUseAgeConfirmation;
      OnlineVideoSettings.Instance.CacheTimeout = MP2Extended.Settings.OnlineVideosCacheTimeout;
      OnlineVideoSettings.Instance.UtilTimeout = MP2Extended.Settings.OnlineVideosUtilTimeout;
      OnlineVideoSettings.Instance.DownloadDir = MP2Extended.Settings.OnlineVideosDownloadFolder;
      
      // clear cache files that might be left over from an application crash
      Downloader.ClearDownloadCache();

      // The default connection limit is 2 in .Net on most platforms! This means downloading two files will block all other WebRequests.
      ServicePointManager.DefaultConnectionLimit = 100;
      // The default .Net implementation for URI parsing removes trailing dots, which is not correct
      DotNetFrameworkHelper.FixUriTrailingDots();
    }

    private void RunUpdate()
    {
      _currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
      {
        try
        {
          bool? updateResult = Updater.UpdateSites((m, p) =>
          {
            if (p.HasValue) Logger.Debug("OnlineVideosUpdate: Update in progress: {0}%", p.Value);
            return _currentBackgroundTask.State != WorkState.CANCELED;
          }, null, false);

          MP2Extended.Settings.OnlineVideosLastAutomaticUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
          _currentBackgroundTask.Exception = ex;
        }
      },
      (args) =>
      {
        lock (_syncObject)
        {
          _currentBackgroundTask = null;
        }
      });
    }

    #endregion private

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
