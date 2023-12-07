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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using Webradio.Settings;

namespace Webradio.Models
{
  internal class WebradioFavoritesModel : IWorkflowModel 
  {
    public const string DATA_ID_STR = "B8DB0672-483A-4E8B-AAF7-2CBEE3F92524";

    public WebradioFavoritesModel()
    {
    }

    public static void SetFavorite()
    {
      List<string> favoritList = ServiceRegistration.Get<ISettingsManager>().Load<Favorites>().List ?? new List<string>();
      if (Webradio.Settings.Favorites.IsFavorite(WebradioDataModel.SelectedStream))
      {
        favoritList.Remove(WebradioDataModel.SelectedStream.Id);
        IsFavorite = false;
      }
      else
      {
        favoritList.Add(WebradioDataModel.SelectedStream.Id);
        IsFavorite = true;
      }

      ServiceRegistration.Get<ISettingsManager>().Save(new Favorites(favoritList));

      WebradioHomeModel.UpdateFavorits(WebradioDataModel.SelectedStream.Id, IsFavorite);
    }

    #region IsFavorite

    protected static AbstractProperty _isFavoriteProperty = new WProperty(typeof(bool), false);

    public AbstractProperty IsFavoriteProperty => _isFavoriteProperty;

    public static bool IsFavorite
    {
      get => (bool)_isFavoriteProperty.GetValue();
      set => _isFavoriteProperty.SetValue(value);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(DATA_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
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
