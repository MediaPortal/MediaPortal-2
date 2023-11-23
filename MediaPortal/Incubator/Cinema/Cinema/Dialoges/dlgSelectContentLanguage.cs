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
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace Cinema.Dialoges
{
  internal class DlgSelectContentLanguage : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "A1529F09-767B-4164-B149-46F6D5729126";
    public const string NAME = "name";
    public const string CODE = "code";

    #endregion

    public CinemaSettings Settings = new CinemaSettings();

    public ItemsList items = new ItemsList();
    
    private void Init()
    {
      items.Clear();
      AddItem("[Language.de]", "de");
      AddItem("[Language.en]", "en");
      AddItem("[Language.es]", "es");
      AddItem("[Language.fr]", "fr");
      AddItem("[Language.it]", "it");
      AddItem("[Language.pt]", "pt");

      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<CinemaSettings>();
    }

    private void AddItem(string name, string code)
    {
      var item = new ListItem();
      item.AdditionalProperties[NAME] = name;
      item.AdditionalProperties[CODE] = code;
      item.SetLabel("Name", name);
      items.Add(item);
    }

    public void Select(ListItem item)
    {
      Settings.ContentLanguage = (string)item.AdditionalProperties[CODE];
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
      ServiceRegistration.Get<ISettingsManager>().Save(Settings);
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
