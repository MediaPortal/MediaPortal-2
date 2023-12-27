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
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace Cinema.Dialoges
{
  class DlgSelectCinema : IWorkflowModel 
  {
    #region Consts

    public const string MODEL_ID_STR = "5CE9C2B8-FF38-4FBE-A3F8-66F5B5F3FA13";
    public const string NAME = "name";

    #endregion

    public static ItemsList items = new ItemsList();

    public static void Init()
    {
      items.Clear();
      var oneItemSelected = false;
      var cinemas = ServiceRegistration.Get<ISettingsManager>().Load<Locations>();
      if (cinemas != null)
      {
        if (cinemas.LocationSetupList != null)
        {
          foreach (var cd in cinemas.LocationSetupList)
          {
            var item = new ListItem();
            item.AdditionalProperties[NAME] = cd.Id;
            item.SetLabel("Name", cd.Name + " - " + cd.Address);
            items.Add(item);
            if (oneItemSelected) continue;
            CinemaHome.SelectCinema(cd.Id);
            oneItemSelected = true;
          }
        }
      }
      items.FireChange();
    }

    public static void Select(ListItem item)
    {
      CinemaHome.SelectCinema((string)item.AdditionalProperties[NAME]);
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();  
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
