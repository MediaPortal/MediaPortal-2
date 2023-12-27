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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using Webradio.Helper;
using Webradio.Models;

namespace Webradio.Dialogues
{
  internal class WebradioDlgSearchInStreams : IWorkflowModel
  {
    public void SearchTitel()
    {
      if (SearchText != "")
        FillList(Radiostations.Instance.Stations.Where(ms => ms.Name.ToUpper().IndexOf(SearchText.ToUpper(), StringComparison.Ordinal) >= 0).ToList());
      else
        InfoLabel = ENTER_TEXT;
    }

    public void FillList(List<RadioStation> list)
    {
      if (list.Count > 0)
      {
        WebradioHomeModel.FillItemList(list);
        ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      }
      else
      {
        InfoLabel = NO_STREAMS;
      }
    }

    #region Consts

    protected const string MODEL_ID_STR = "7AE86A07-DB55-4AA6-9FBF-B1888A4FF6DA";
    protected const string NO_STREAMS = "[Webradio.Dialog.Search.NoStreams]";
    protected const string ENTER_TEXT = "[Webradio.Dialog.Search.NoSearchText]";

    #endregion

    #region Propertys

    protected static AbstractProperty _searchTextProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty SearchTextProperty => _searchTextProperty;

    public static string SearchText
    {
      get => (string)_searchTextProperty.GetValue();
      set => _searchTextProperty.SetValue(value);
    }

    protected static AbstractProperty _infoLabelProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty InfoLabelProperty => _infoLabelProperty;

    public static string InfoLabel
    {
      get => (string)_infoLabelProperty.GetValue();
      set => _infoLabelProperty.SetValue(value);
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
      InfoLabel = "";
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
