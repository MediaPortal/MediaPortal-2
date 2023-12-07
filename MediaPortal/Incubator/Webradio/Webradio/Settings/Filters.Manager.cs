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
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Workflow;
using Webradio.Models;

namespace Webradio.Settings
{
  public partial class Filters : IDisposable
  {
    private static Filters _instance;

    public static Filters Instance
    {
      get { return _instance = (_instance ?? Load()); }
    }

    public void Save()
    {
      ServiceRegistration.Get<ISettingsManager>().Save(_instance);
      Dispose();
    }

    public void ClearActiveFilter()
    {
      ActiveFilter = null;
      WebradioDataModel.ActiveFilter = "";
      Save();
    }

    public void RemoveFilter(string filterTitle)
    {
      foreach (var mf in FilterSetupList)
      {
        if (mf.Titel != filterTitle) continue;
        FilterSetupList.Remove(mf);
        if (ActiveFilter != null)
        {
          if (ActiveFilter.Titel == mf.Titel)
          {
            ActiveFilter = null;
          }
        }
        break;
      }
      _instance = this;
    }

    public Filter GetFilterByTitle(string filterTitle)
    {
      foreach (var fn in FilterSetupList)
      {
        if (fn.Titel == filterTitle)
        {
          return fn;
        }
      }
      return null;
    }

    public void UpdateFilterByTitle(string filterTitle, List<string> countrys, List<string> genres)
    {
      foreach (var fn in FilterSetupList)
      {
        if (fn.Titel == filterTitle)
        {
          fn.Countrys = countrys;
          fn.Genres = genres;
        }
      }
    }

    public bool CanEnterState()
    {
      bool ret = FilterSetupList != null;

      if (FilterSetupList != null)
      {
        ret = FilterSetupList.Count != 0;
      }

      if (ret == false)
      {
        WebradioDataModel.DialogMessage = "[Webradio.Filter.Msg1]";
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
      }

      return ret;
    }

    private static Filters Load()
    {
      return ServiceRegistration.Get<ISettingsManager>().Load<Filters>();
    }

    public void Dispose()
    {
      _instance = null;
    }
  }
}
