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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using Webradio.Models;
using Webradio.Settings;

namespace Webradio.Dialogues
{
  internal class WebradioDlgDeleteFilter : IWorkflowModel
  {
    public ItemsList FilterItems = new ItemsList();

    public void Init()
    {
      ImportFilter();
    }

    public void ImportFilter()
    {
      FilterItems.Clear();
      foreach (var mf in Filters.Instance.FilterSetupList)
      {
        var item = new ListItem
        {
          AdditionalProperties =
          {
            [NAME] = mf.Titel
          }
        };
        item.SetLabel("Name", mf.Titel);
        FilterItems.Add(item);
      }

      FilterItems.FireChange();
    }

    public void Select(ListItem item)
    {
      item.Selected = item.Selected != true;
      item.FireChange();
    }

    public void Delete()
    {
      foreach (var item in FilterItems.Where(item => item.Selected))
      {
        Filters.Instance.RemoveFilter(item.AdditionalProperties[NAME].ToString());
      }

      Filters.Instance.Save();
      WebradioFilterModel.SaveImage = "Unsaved.png";
      WebradioFilterModel.FilterTitel = "";
      ImportFilter();

      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #region Consts

    protected const string MODEL_ID_STR = "59AB04C6-6B8D-41E5-A041-7AFC8DEDEB89";
    protected const string NAME = "name";
    protected const string ID = "id";

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId => new Guid(MODEL_ID_STR);

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return Filters.Instance.CanEnterState();
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
