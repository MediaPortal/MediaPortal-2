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
using Cinema.Models;
using Cinema.OnlineLibraries.Data;
using Cinema.Player;
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using Trailer = Cinema.Settings.Trailer;

namespace Cinema.Dialoges
{
  class DlgSelectTrailer : IWorkflowModel 
  {
    #region Consts

    public const string MODEL_ID_STR = "20D82FAF-610C-4C40-B6C8-3DEA37C6CB22";
    public const string NAME = "name";
    public const string URL = "url";

    #endregion

    public static ItemsList Trailers = new ItemsList();

    private static Movie _movie;

    public static void Init()
    {
    }

    public static void ReadTrailers(string movieName)
    {
      var list = ServiceRegistration.Get<ISettingsManager>().Load<Movies>();

      foreach (var mt in list.CinemaMovies)
      {
        foreach (var movie in mt.Movies)
        {
          if (movie.Title == movieName)
          {
            _movie = movie;
            FillTrailers(movie.Trailer);
            break;
          }
        }
      }
    }

    private static void FillTrailers(List<OnlineLibraries.Data.Trailer> trailers)
    {
      Trailers.Clear();
      if (trailers != null)
      {
        foreach (var tr in trailers)
        {
          var item = new ListItem();
          item.AdditionalProperties[NAME] = tr.Name;
          item.AdditionalProperties[URL] = tr.Url;
          item.SetLabel("Name", tr.Name);
          Trailers.Add(item);
        }
      }
      Trailers.FireChange();
    }

    public static void Select(ListItem item)
    {
      var t = new Trailer { Title = (string)item.AdditionalProperties[NAME], Url = (string)item.AdditionalProperties[URL] };
      ServiceRegistration.Get<ILogger>().Debug("Cinema: Select Trailer for '{0}' - TmdbId:{1}", _movie.Title, _movie.TmdbId);
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      if (t.Url != null ) CinemaPlayerHelper.PlayStream(t); 
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

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
