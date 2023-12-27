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
using Webradio.Helper;
using Webradio.Models;
using Webradio.Settings;

namespace Webradio.Dialogues
{
  internal class WebradioDlgShowFilter : IWorkflowModel
  {
    public static ItemsList FilterItems = new ItemsList();

    private bool _quick;
    private string _typ = string.Empty;
    private List<string> _currentFilterTitles = new List<string>();

    public void Init()
    {
      ShowFilter();
    }

    public void ShowFilter()
    {
      FilterItems.Clear();
      _typ = string.Empty;
      _quick = false;

      if (Filters.Instance != null)
      {
        if (Filters.Instance.FilterSetupList!= null)
        {
          if (Filters.Instance.FilterSetupList.Count != 0)
          {
            foreach (var f in Filters.Instance.FilterSetupList.Where(f => !_currentFilterTitles.Contains(f.Titel))) _currentFilterTitles.Add(f.Titel);
            FillItems(_currentFilterTitles);
          }
        }
      }
    }
    
    public void SelectedFilter(ListItem item)
    {
      var name = (string)item.AdditionalProperties[KEY_FILTER];
      var list = new List<RadioStation>();

      foreach (var f in Filters.Instance.FilterSetupList)
        if (f.Titel == name)
        {
          list = Radiostations.Filtered(f, Radiostations.Instance.Stations);
          Filters.Instance.ActiveFilter = f; 
          WebradioDataModel.ActiveFilter = f.Titel;
          Filters.Instance.Save();
          break;
        }

      WebradioHomeModel.FillItemList(list);
    }

    public void SelectedQuick(ListItem item)
    {
      var predicate = (Func<RadioStation, bool>)item.AdditionalProperties[KEY_FILTER];
      var filtered = Radiostations.Instance.Stations.Where(predicate).ToList();
      WebradioHomeModel.FillItemList(filtered);
    }

    private void CreateFilters(Func<RadioStation, bool> predicate, Func<RadioStation, string> selector, CreateFilterDelegate filter)
    {
      var list = Radiostations.Instance.Stations.Where(predicate).Select(selector).Distinct();
      FillItems(list, filter);
    }

    private void CreateFiltersMulti(Func<RadioStation, bool> predicate, Func<RadioStation, string[]> selector, CreateFilterDelegate filter)
    {
      var list = Radiostations.Instance.Stations.Where(predicate).SelectMany(selector).Select(s => s.Trim()).Distinct();
      FillItems(list, filter);
    }

    private void FillItems(IEnumerable<string> list, CreateFilterDelegate createFilterDelegate)
    {
      FilterItems.Clear();
      var sorted = list.ToList();
      sorted.Sort();
      foreach (var s in sorted)
      {
        var item = new ListItem();
        item.AdditionalProperties[KEY_FILTER] = createFilterDelegate(s); // Creates a dynamic filter like s => s.Titel="Radio 100"
        if (_typ == "country")
          item.SetLabel("Name", "[Country." + s + "]");
        else
          item.SetLabel("Name", s);
        FilterItems.Add(item);
      }

      FilterItems.FireChange();
    }

    private void FillItems(List<string> list)
    {
      FilterItems.Clear();
      list.Sort();
      foreach (var s in list)
      {
        var item = new ListItem();
        item.AdditionalProperties[KEY_FILTER] = s;
        if (_typ == "country")
          item.SetLabel("Name", "[Country." + s + "]");
        else
          item.SetLabel("Name", s);
        FilterItems.Add(item);
      }

      FilterItems.FireChange();
    }

    private static bool Contains2(ICollection<string> l, string s)
    {
      if (s == null) throw new ArgumentNullException("s");
      if (l.Count == 0) return true;

      var split = s.Split(',');
      return split.Any(part => l.Contains(part.Trim()));
    }

    protected delegate Func<RadioStation, bool> CreateFilterDelegate(string filter);

    #region Used by Skin

    public void SelectCountry()
    {
      _quick = true;
      _typ = "country";
      CreateFilters(s => !string.IsNullOrWhiteSpace(s.Country), s => s.Country, filterValue => s => s.Country == filterValue);
    }

    public void SelectCity()
    {
      _typ = string.Empty;
      _quick = true;
      CreateFilters(s => !string.IsNullOrWhiteSpace(s.City), s => s.City, filterValue => s => s.City == filterValue);
    }

    public void SelectGenre()
    {
      _typ = string.Empty;
      _quick = true;
      CreateFiltersMulti(s => s.Genres!= null, s => s.Genres.ToArray(), filterValue => s => Contains2(s.Genres.ToArray(), filterValue));
    }

    public void SelectMyFilter()
    {
      _typ = string.Empty;
      _quick = false;
      FillItems(_currentFilterTitles);
    }

    public void SetSelectedFilter(ListItem item)
    {
      if (_quick)
        SelectedQuick(item);
      else
        SelectedFilter(item);
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #endregion

    #region Consts

    protected const string MODEL_ID_STR = "63F1DA3E-E87F-4478-83E7-C13966447869";
    protected const string KEY_FILTER = "filter";

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
