#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.Diagnostics;
using Cinema.Helper;
using Cinema.OnlineLibraries;
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
//using Locations = Cinema.Settings.Locations;

namespace Cinema.Dialoges
{
  internal class DlgStartUpdateForWeek : IWorkflowModel
  {
    private static CinemaSettings _settings = new CinemaSettings();
    private static Locations _locations;
    private static Eventhandler _handler = Eventhandler.Instance;



    private void Init()
    {
      //var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      //_settings = settingsManager.Load<Cinema.Settings.CinemaSettings>();
      //_locations = settingsManager.Load<Locations>();

      _handler.MessageReceived += HandlerOnMessageReceived;
    }

    public async void StartUpdate()
    {
      Update.LoadSettings();
      Update.StartUpdate();

      //List<string> ids = new List<string>();
      //foreach (var cinema in _locations.LocationSetupList)
      //{
      //  ids.Add(cinema.Id);
      //}

      //var ret = OnlineLibraries.Read.MoviesForAllDaysAndCinemas(
      //  _settings.ContentLanguage,
      //  _settings.LocationCountryCode,
      //  _settings.LocationPostalCode,
      //  _locations.LocationSetupList);

      //Cinema.Settings.Movies movies = new Movies(ret);

      //ServiceRegistration.Get<ISettingsManager>().Save(movies);

      //List<Task> allTasks = new List<Task>();

      //allTasks.Add(Task.Run(() => LoadImages(ret)));
      //await Task.WhenAll(allTasks);

      //_settings.LastUpdate = DateTime.Now;
      //ServiceRegistration.Get<ISettingsManager>().Save(_settings);
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    private void HandlerOnMessageReceived(object sender, string message)
    {
      Debug.WriteLine(message);

      if (message.StartsWith("[D]"))
        DayValue = message.Replace("[D]", "");

      if (message.StartsWith("[C]"))
        CinemaValue = message.Replace("[C]", "");

      if (message.StartsWith("[M]"))
        CurrentMovieValue = message.Replace("[M]", "");
    }

    //private static void LoadImages(List<OnlineLibraries.Data.CinemaMovies> movies)
    //{
    //  if (!Directory.Exists(CinemaHome.CachedImagesFolder))
    //  {
    //    Directory.CreateDirectory(CinemaHome.CachedImagesFolder);
    //  }

    //  List<CachedImage> newCachedImages = new List<CachedImage>();
    //  List<string> newFiles = new List<string>();

    //  foreach (var movie in movies)
    //  {
    //    foreach (var m in movie.Movies)
    //    {
    //      var fa = new CachedImage(m.Fanart, m.TmdbId, "fanart");
    //      if (!newFiles.Contains(fa.FullPath))
    //      {
    //        newFiles.Add(fa.FullPath);
    //        newCachedImages.Add(fa);
    //      }

    //      var co = new CachedImage(m.CoverUrl, m.TmdbId, "cover");
    //      if (!newFiles.Contains(co.FullPath))
    //      {
    //        newFiles.Add(co.FullPath);
    //        newCachedImages.Add(co);
    //      }
    //    }
    //  }

    //  var oldFile = Directory.EnumerateFiles(CinemaHome.CachedImagesFolder);

    //  foreach (var file in oldFile)
    //  {
    //    if (!newFiles.Contains(file))
    //    {
    //      File.Delete(file);
    //    }
    //  }

    //  foreach (var cim in newCachedImages)
    //  {
    //    if (!File.Exists(cim.FullPath))
    //    {
    //      cim.LoadImageFromWeb();
    //    }
    //  }
    //}

    #region Propertys

    #region Cinema
    private static readonly AbstractProperty _cinemaProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaProperty => _cinemaProperty;

    public string CinemaValue
    {
      get => (string)_cinemaProperty.GetValue();
      set => _cinemaProperty.SetValue(value);
    }
    #endregion

    #region Day
    private static readonly AbstractProperty _dayProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty DayProperty => _dayProperty;

    public string DayValue
    {
      get => (string)_dayProperty.GetValue();
      set => _dayProperty.SetValue(value);
    }
    #endregion

    #region CurrentMovie
    private static readonly AbstractProperty _currentMovieProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CurrentMovieProperty => _currentMovieProperty;

    public string CurrentMovieValue
    {
      get => (string)_currentMovieProperty.GetValue();
      set => _currentMovieProperty.SetValue(value);
    }
    #endregion

    #endregion

    #region Consts

    public const string MODEL_ID_STR = "69863E06-B955-4540-8B54-BA4BD337AD89";

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId => new Guid(MODEL_ID_STR);

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Init();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      DayValue = "";
      CinemaValue = "";
      CurrentMovieValue = "";
      //ServiceRegistration.Get<ISettingsManager>().Save(_settings);
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Todo: select any or the Last ListItem
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
