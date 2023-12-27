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
using System.IO;
using System.Timers;
using Cinema.Dialoges;
using Cinema.Helper;
using Cinema.OnlineLibraries.Data;
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.UserServices.FanArtService.Client.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace Cinema.Models
{
  public class CinemaHome : IWorkflowModel, IPluginStateTracker
  {
    public static Timer ATimer = new Timer();
    public static ItemsList Movies = new ItemsList();
    public static string CachedImagesFolder = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Cinema");
    public ItemsList Cinemas = new ItemsList();
    public static Movies FullMovieList { get; set; }
    

    #region Consts

    public const string MODEL_ID_STR = "78E0D999-D87A-4340-B8D1-9CF97814D2FD";
    public const string NAME = "name";
    public const string TRAILER = "trailer";

    #endregion

    #region Propertys

    #region CinemaId

    public static readonly AbstractProperty _cinemaId = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaIdProperty => _cinemaId;

    public static string CinemaId
    {
      get => (string)_cinemaId.GetValue();
      set => _cinemaId.SetValue(value);
    }

    #endregion

    #region CinemaName

    public static readonly AbstractProperty _cinemaName = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaNameProperty => _cinemaName;

    public static string CinemaName
    {
      get => (string)_cinemaName.GetValue();
      set => _cinemaName.SetValue(value);
    }

    #endregion

    #region CinemaAddress

    public static readonly AbstractProperty _cinemaAddress = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaAddressProperty => _cinemaAddress;

    public static string CinemaAddress
    {
      get => (string)_cinemaAddress.GetValue();
      set => _cinemaAddress.SetValue(value);
    }

    #endregion

    #region CinemaLocality

    public static readonly AbstractProperty _cinemaLocality = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaLocalityProperty => _cinemaLocality;

    public static string CinemaLocality
    {
      get => (string)_cinemaLocality.GetValue();
      set => _cinemaLocality.SetValue(value);
    }

    #endregion

    #region CinemaRegion

    public static readonly AbstractProperty _cinemaRegion = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaRegionProperty => _cinemaRegion;

    public static string CinemaRegion
    {
      get => (string)_cinemaRegion.GetValue();
      set => _cinemaRegion.SetValue(value);
    }

    #endregion

    #region CinemaPostalCode

    public static readonly AbstractProperty _cinemaPostalCode = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaPostalCodeProperty => _cinemaPostalCode;

    public static string CinemaPostalCode
    {
      get => (string)_cinemaPostalCode.GetValue();
      set => _cinemaPostalCode.SetValue(value);
    }

    #endregion

    #region CinemaPhone

    public static readonly AbstractProperty _cinemaPhone = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CinemaPhoneProperty => _cinemaPhone;

    public static string CinemaPhone
    {
      get => (string)_cinemaPhone.GetValue();
      set => _cinemaPhone.SetValue(value);
    }

    #endregion

    #endregion

    #region public Methods

    public static void SelectCinema(string id)
    {
      ServiceRegistration.Get<ILogger>().Debug("Cinema: Load Data for '{0}' Cinemas", FullMovieList.CinemaMovies.Count);
      foreach (var cd in FullMovieList.CinemaMovies)
      {
        if (cd.Cinema.Id == id)
        {
          ServiceRegistration.Get<ILogger>().Debug("Cinema: Load '{0}' Movies for '{1}'", cd.Movies.Count, cd.Cinema.Name);
          CinemaId = cd.Cinema.Id;
          CinemaName = cd.Cinema.Name;
          CinemaAddress = cd.Cinema.Address;
          CinemaLocality = cd.Cinema.Locality;
          CinemaRegion = cd.Cinema.Region;
          CinemaPostalCode = cd.Cinema.PostalCode;
          CinemaPhone = cd.Cinema.Phone;
          AddMoviesByCinema(cd.Cinema);
        }
      }
    }

    public static void SelectMovie(ListItem item)
    {
      var name =  (string)item.AdditionalProperties[NAME];
      DlgSelectTrailer.ReadTrailers(name);
      ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("829BD48C-9FF0-4A80-94E0-3BD811ABC226"));
    }

    public static void AddMoviesByCinema(OnlineLibraries.Data.Cinema cinema)
    {
      Movies.Clear();

      List<Movie> ml = new List<Movie>();
      foreach (var cd in FullMovieList.CinemaMovies)
      {
        if (cd.Cinema.Id == cinema.Id)
        {
          ml = cd.Movies;
        }
      }

      foreach (var m in ml)
      {
        var item = new ListItem { AdditionalProperties = { [NAME] = m.Title } };
        item.SetLabel("Name", m.Title);

        try
        {
          for (var i = 0; i <= 6; i++)
          {
            if (m.Showtimes.Count - 1 >= i)
            {
              item.SetLabel("Day" + i, m.Showtimes[i].Day);
              item.SetLabel("Day" + i + "_Time", m.Showtimes[i].Showtimes);
            }
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }

        item.SetLabel("Title", m.Title);
        item.SetLabel("ImdbId", m.ImdbId);
        item.SetLabel("TmdbId", m.TmdbId);
        item.SetLabel("Release", m.Release);
        item.SetLabel("Runtime", m.Runtime);
        item.SetLabel("Genres", m.Genres);
        item.SetLabel("Age", m.Age);
        item.SetLabel("CoverUrl", Path.Combine(CachedImagesFolder, m.TmdbId + "-cover.jpg"));
        item.SetLabel("Fanart", Path.Combine(CachedImagesFolder, m.TmdbId + "-fanart.jpg"));
        item.SetLabel("Language", m.Language);
        item.SetLabel("Description", m.Description);
        item.SetLabel("Country", m.Country);
        item.SetLabel("UserRating", m.UserRating);
        item.SetLabel("UserRatingScaled", m.UserRatingScaled);

        if (m.Trailer.Count > 0)
        {
          item.AdditionalProperties[TRAILER] = m.Trailer[0].Url;
        }

        Movies.Add(item);
      }
      Movies.FireChange();
    }

    public static void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      if (e.FirstAddedItem is ListItem selectedItem)
      {
        var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
        if (fanArtBgModel != null)
        {
          var uriSource = selectedItem.Labels["Fanart"].ToString();

          if (uriSource != "") fanArtBgModel.ImageSource = new MultiImageSource { UriSource = uriSource };
          else fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
        }
      }
    }

    #endregion

    #region private Methods

    private static void Init()
    {
      CkeckUpdate();

      FullMovieList = ServiceRegistration.Get<ISettingsManager>().Load<Movies>();
      if (FullMovieList != null && FullMovieList.CinemaMovies != null && FullMovieList.CinemaMovies.Count > 0) SelectCinema(FullMovieList.CinemaMovies[0].Cinema.Id);
    }

    private static void CkeckUpdate()
    {
      var dt1 = Convert.ToDateTime(ServiceRegistration.Get<ISettingsManager>().Load<Settings.CinemaSettings>().LastUpdate);
      var dt = DateTime.Now - dt1;
      // Is it a New Day ?
      if (dt > new TimeSpan(1, 0, 0, 0))
      {
        MakeUpdate();
      }
      else if (ServiceRegistration.Get<ISettingsManager>().Load<Locations>().Changed)
      {
        MakeUpdate();
      }
    }

    public static void MakeUpdate()
    {
      Update.LoadSettings();
      Update.StartUpdate();
    }

    private void ClearFanart()
    {
      var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
      if (fanArtBgModel != null) fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
    }

    private static void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Info("Cinema Timer Thread Check Update");
      CkeckUpdate();
    }

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
      ClearFanart();
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

    # region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      // Add Timer with event
      //ATimer.Elapsed += OnTimedEvent;
      //// Diff in sec To next Day 01.00.00
      //ATimer.Interval = DateTime.Today.AddHours(25).Subtract(DateTime.Now).TotalMilliseconds;
      //// Timer start
      //ATimer.Start();

      //// Make Update
      //CkeckUpdate(false);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    #endregion
  }
}
