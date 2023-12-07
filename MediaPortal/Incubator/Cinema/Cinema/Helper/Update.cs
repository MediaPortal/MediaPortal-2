using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cinema.Models;
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;

namespace Cinema.Helper
{
  internal class Update
  {
    private static Settings.CinemaSettings _settings = new Settings.CinemaSettings();
    private static Locations _locations;

    public static void LoadSettings()
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      _settings = settingsManager.Load<Settings.CinemaSettings>();
      _locations = settingsManager.Load<Locations>();
    }

    public static async void StartUpdate()
    {
      List<string> ids = new List<string>();
      foreach (var cinema in _locations.LocationSetupList)
      {
        ids.Add(cinema.Id);
      }

      var ret = OnlineLibraries.Read.MoviesForAllDaysAndCinemas(
        _settings.ContentLanguage,
        _settings.LocationCountryCode,
        _settings.LocationPostalCode,
        _locations.LocationSetupList);

      Movies movies = new Movies(ret);

      ServiceRegistration.Get<ISettingsManager>().Save(movies);

      List<Task> allTasks = new List<Task>();

      allTasks.Add(Task.Run(() => LoadImages(ret)));
      await Task.WhenAll(allTasks);

      _settings.LastUpdate = DateTime.Now;
      ServiceRegistration.Get<ISettingsManager>().Save(_settings);
    }


    private static void LoadImages(List<OnlineLibraries.Data.CinemaMovies> movies)
    {
      if (!Directory.Exists(CinemaHome.CachedImagesFolder))
      {
        Directory.CreateDirectory(CinemaHome.CachedImagesFolder);
      }

      List<CachedImage> newCachedImages = new List<CachedImage>();
      List<string> newFiles = new List<string>();

      foreach (var movie in movies)
      {
        foreach (var m in movie.Movies)
        {
          var fa = new CachedImage(m.Fanart, m.TmdbId, "fanart");
          if (!newFiles.Contains(fa.FullPath))
          {
            newFiles.Add(fa.FullPath);
            newCachedImages.Add(fa);
          }

          var co = new CachedImage(m.CoverUrl, m.TmdbId, "cover");
          if (!newFiles.Contains(co.FullPath))
          {
            newFiles.Add(co.FullPath);
            newCachedImages.Add(co);
          }
        }
      }

      var oldFile = Directory.EnumerateFiles(CinemaHome.CachedImagesFolder);

      foreach (var file in oldFile)
      {
        if (!newFiles.Contains(file))
        {
          File.Delete(file);
        }
      }

      foreach (var cim in newCachedImages)
      {
        if (!File.Exists(cim.FullPath))
        {
          cim.LoadImageFromWeb();
        }
      }
    }

  }
}
