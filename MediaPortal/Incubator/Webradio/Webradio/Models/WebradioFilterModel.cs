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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using Webradio.Helper;
using Webradio.Settings;

namespace Webradio.Models
{
  internal class WebradioFilterModel : IWorkflowModel
  {
    private static List<RadioStation> _selectedStreams = new List<RadioStation>();
    
    private void Init()
    {
      CountryItems.Clear();
      GenreItems.Clear();
      _countrysSelected.Clear();
      _genresSelected.Clear();

      if (Filters.Instance.FilterSetupList == null)
      {
        Filters.Instance.FilterSetupList = new List<Filter>();
      }

      SaveImage = "Saved.png";
      AddToLists(Radiostations.Instance.Stations);
      FillAllViews();
    }

    private static void FillAllViews()
    {
      FillCountrys();
      FillGenres();
      SetSelectedStations();
    }

    private static void SetSelectedStations()
    {
      var cList = new List<RadioStation>();
      _selectedStreams = new List<RadioStation>();

      foreach (var country in _countrysSelected)
      {
        cList.AddRange(Radiostations.Instance.Stations.FindAll(c => c.Country == country));
      }

      foreach (var genre in _genresSelected)
      {
        _selectedStreams.AddRange(cList.FindAll(c => c.Genres.Contains(genre)));
      }

      SelectedStreamsCount = _selectedStreams.Count.ToString();
    }

    private void AddToLists(List<RadioStation> stations)
    {
      _countrys = new List<string>();
      _genres = new List<string>();

      foreach (var station in stations)
      {
        if (!_countrys.Contains(station.Country))
          _countrys.Add(station.Country);

        foreach (var genre in station.Genres.Where(genre => !_genres.Contains(genre)))
        {
          _genres.Add(genre);
        }
      }

      _countrys.Sort();
      _genres.Sort();
    }

    #region from Menu

    /// <summary>
    /// Import selected Filter
    /// </summary>
    public static void SetFilter(Filter filter)
    {
      CountryItems.Clear();
      GenreItems.Clear();
      _countrysSelected = new List<string>();
      _genresSelected = new List<string>();

      FilterTitel = filter.Titel;

      foreach (var s in filter.Countrys) 
        _countrysSelected.Add(s);

      foreach (var s in filter.Genres) 
        _genresSelected.Add(s);

      FillAllViews();
    }

    /// <summary>
    ///   Refresh all Lists
    /// </summary>
    public void Clear()
    {
      CountryItems.Clear();
      GenreItems.Clear();
      _countrysSelected.Clear();
      _genresSelected.Clear();

      FillAllViews();
      FilterTitel = "";
      SaveImage = "Unsaved.png";
    }

    /// <summary>
    ///   Added a Entry
    /// </summary>
    public void Add()
    {
      Clear();

      string name = "New Filter";
      int x = 0;
      if (Filters.Instance.FilterSetupList != null)
      {
        foreach (var fn in Filters.Instance.FilterSetupList)
        {
          if (fn.Titel.Contains(name))
            x++;
        }
      }

      if (x > 0)
      {
        name = name + " (" + x +")";
      }
      
      FilterTitel = name;
      SaveImage = "Unsaved.png";
    }

    /// <summary>
    /// Save all Changes on Site
    /// </summary>
    public void Save()
    {
      if (FilterTitel == "")
      {
        WebradioDataModel.DialogMessage = "[Webradio.Filter.Save.Msg1]";
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
        return;
      }

      if (_selectedStreams.Count == 0)
      {
        WebradioDataModel.DialogMessage = "[Webradio.Filter.Save.Msg2]";
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
        return;
      }
    
      if (Filters.Instance.GetFilterByTitle(FilterTitel) == null)
      {
        Filters.Instance.FilterSetupList.Add(new Filter(FilterTitel, _countrysSelected, _genresSelected));
      }
      else
      {
        Filters.Instance.UpdateFilterByTitle(FilterTitel, _countrysSelected, _genresSelected);
      }
      
      Filters.Instance.Save();
      SaveImage = "Saved.png";
    }

    public void ChangeCountry(ListItem item)
    {
      var country = (string)item.AdditionalProperties[NAME];
      if (!item.Selected)
      {
        _countrysSelected.Add(country);
        item.Selected = true;
      }
      else
      {
        _countrysSelected.Remove(country);
        item.Selected = false;
      }

      item.FireChange();
      SetSelectedStations();
      SaveImage = "Unsaved.png";
    }

    public void ChangeGenre(ListItem item)
    {
      var genre = (string)item.AdditionalProperties[NAME];
      if (!item.Selected)
      {
        _genresSelected.Add(genre);
        item.Selected = true;
      }
      else
      {
        _genresSelected.Remove(genre);
        item.Selected = false;
      }

      item.FireChange();
      SetSelectedStations();
      SaveImage = "Unsaved.png";
    }

    #endregion

    #region Fill Lists

    private static void FillCountrys()
    {
      _countrysSelected.Sort();

      foreach (var country in _countrysSelected)
      {
        ListItem item = new ListItem { AdditionalProperties = { [NAME] = country }, Selected = true };
        item.SetLabel("Name", "[Country." + country + "]");
        CountryItems.Add(item);
      }

      foreach (var country in _countrys)
      {
        if (!_countrysSelected.Contains(country))
        {
          ListItem item = new ListItem { AdditionalProperties = { [NAME] = country }, Selected = false };
          item.SetLabel("Name", "[Country." + country + "]");
          CountryItems.Add(item);
        }
      }

      CountryItems.FireChange();
    }

    private static void FillGenres()
    {
      _genresSelected.Sort();

      foreach (var genre in _genresSelected)
      {
        ListItem item = new ListItem { AdditionalProperties = { [NAME] = genre }, Selected = true };
        item.SetLabel("Name", genre);
        GenreItems.Add(item);
      }

      foreach (var genre in _genres)
      {
        if (!_genresSelected.Contains(genre))
        {
          ListItem item = new ListItem { AdditionalProperties = { [NAME] = genre }, Selected = false };
          item.SetLabel("Name", genre);
          GenreItems.Add(item);
        }
      }

      GenreItems.FireChange();
    }

    #endregion

    #region Lists

    // Lists with all Items from Streamlist
    public static ItemsList CountryItems = new ItemsList();
    public static ItemsList GenreItems = new ItemsList();

    // Lists with Selected Items 
    private static List<string> _countrysSelected = new List<string>();
    private static List<string> _genresSelected = new List<string>();

    // Lists with all Entrys in Streamlist
    private static List<string> _countrys = new List<string>();
    private static List<string> _genres = new List<string>();

    #endregion

    #region Propertys

    #region FilterTitel

    private static AbstractProperty _filterTitelProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty FilterTitelProperty => _filterTitelProperty;

    public static string FilterTitel
    {
      get => (string)_filterTitelProperty.GetValue();
      set => _filterTitelProperty.SetValue(value);
    }

    #endregion

    #region SelectedStreamsCount

    private static AbstractProperty _selectedStreamsCountProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty SelectedStreamsCountProperty => _selectedStreamsCountProperty;

    public static string SelectedStreamsCount
    {
      get => (string)_selectedStreamsCountProperty.GetValue();
      set => _selectedStreamsCountProperty.SetValue(value);
    }

    #endregion

    #region SaveImage

    private static AbstractProperty _saveImage = new WProperty(typeof(string), string.Empty);

    public AbstractProperty SaveImageProperty => _saveImage;

    public static string SaveImage
    {
      get => (string)_saveImage.GetValue();
      set => _saveImage.SetValue(value);
    }

    #endregion

    #endregion

    #region Consts

    protected const string MODEL_ID_STR = "FF29E03E-F4A9-4E21-A299-349E79010430";
    protected const string NAME = "name";

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId => new Guid(MODEL_ID_STR);

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (Radiostations.Instance.Stations == null)
      {
        WebradioDataModel.DialogMessage = "[Webradio.Dialog.Search.NoStreams]";
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
        return false;
      }

      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Init();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Filters.Instance.Dispose();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      Filters.Instance.Dispose();
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
