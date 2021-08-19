#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;
using System;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Container class containing the necessary parameters for calling SetViewMode from skins.
  /// </summary>
  public class SetViewModeParameter
  {
    public LayoutType Layout { get; set; }
    public LayoutSize Size { get; set; }
  }

  /// <summary>
  /// Container containing the necessary parameters for calling SetAdditionalProperty from a skin file.
  /// </summary>
  public class SetAdditionalPropertyParameter
  {
    public string Key { get; set; }
    public string Value { get; set; }
  }

  public class ViewModeModel
  {
    #region Consts

    public const string VM_MODEL_ID_STR = "6997CD35-63F0-4F96-8997-E18C9382E2DC";
    public static Guid VM_MODEL_ID = new Guid(VM_MODEL_ID_STR);

    #endregion

    protected readonly AbstractProperty _layoutTypeProperty;
    protected readonly AbstractProperty _layoutSizeProperty;
    protected readonly ObservableDictionary<string, KeyValueItem<string, string>> _additionalProperties;
    protected readonly ItemsList _viewModeItemsList = new ItemsList();

    public ViewModeModel()
    {
      _layoutTypeProperty = new WProperty(typeof(LayoutType), ViewSettings.DEFAULT_LAYOUT_TYPE);
      _layoutSizeProperty = new WProperty(typeof(LayoutSize), ViewSettings.DEFAULT_LAYOUT_SIZE);
      _additionalProperties = new ObservableDictionary<string, KeyValueItem<string, string>>(k => new KeyValueItem<string, string>(k, null, OnAdditionalPropertyChanged));

      ListItem smallList = new ListItem(Consts.KEY_NAME, Consts.RES_SMALL_LIST)
        {
          Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Small)),
        };
      smallList.AdditionalProperties[Consts.KEY_LAYOUT_TYPE] = LayoutType.ListLayout;
      smallList.AdditionalProperties[Consts.KEY_LAYOUT_SIZE] = LayoutSize.Small;
      _viewModeItemsList.Add(smallList);

      ListItem mediumlList = new ListItem(Consts.KEY_NAME, Consts.RES_MEDIUM_LIST)
        {
          Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Medium))
        };
      mediumlList.AdditionalProperties[Consts.KEY_LAYOUT_TYPE] = LayoutType.ListLayout;
      mediumlList.AdditionalProperties[Consts.KEY_LAYOUT_SIZE] = LayoutSize.Medium;
      _viewModeItemsList.Add(mediumlList);

      ListItem largeList = new ListItem(Consts.KEY_NAME, Consts.RES_LARGE_LIST)
        {
          Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Large))
        };
      largeList.AdditionalProperties[Consts.KEY_LAYOUT_TYPE] = LayoutType.ListLayout;
      largeList.AdditionalProperties[Consts.KEY_LAYOUT_SIZE] = LayoutSize.Large;
      _viewModeItemsList.Add(largeList);

      ListItem largeGrid = new ListItem(Consts.KEY_NAME, Consts.RES_LARGE_GRID)
        {
          Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.GridLayout, LayoutSize.Large))
        };
      largeGrid.AdditionalProperties[Consts.KEY_LAYOUT_TYPE] = LayoutType.GridLayout;
      largeGrid.AdditionalProperties[Consts.KEY_LAYOUT_SIZE] = LayoutSize.Large;
      _viewModeItemsList.Add(largeGrid);

      ListItem coverLarge = new ListItem(Consts.KEY_NAME, Consts.RES_LARGE_COVER)
        {
          Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.CoverLayout, LayoutSize.Large))
        };
      coverLarge.AdditionalProperties[Consts.KEY_LAYOUT_TYPE] = LayoutType.CoverLayout;
      coverLarge.AdditionalProperties[Consts.KEY_LAYOUT_SIZE] = LayoutSize.Large;
      _viewModeItemsList.Add(coverLarge);
    }

    private void OnAdditionalPropertyChanged(string key, string value)
    {
      MediaNavigationModel model = MediaNavigationModel.GetCurrentInstance();
      NavigationData navigationData = model.NavigationData;
      if (navigationData == null)
        return;
      MediaDictionary<string, string> currentNavigationProperties = navigationData.AdditionalProperties ?? new MediaDictionary<string, string>();
      currentNavigationProperties[key] = value;
      // Always set the property, the property change triggers a save
      navigationData.AdditionalProperties = currentNavigationProperties;
    }

    public void Update()
    {
      MediaNavigationModel model = MediaNavigationModel.GetCurrentInstance();
      NavigationData navigationData = model.NavigationData;
      if (navigationData == null)
        return;

      LayoutType = navigationData.LayoutType;
      LayoutSize = navigationData.LayoutSize;

      _additionalProperties.Clear();
      if (navigationData.AdditionalProperties != null)
        foreach (var kvp in navigationData.AdditionalProperties)
          _additionalProperties.Add(kvp.Key, new KeyValueItem<string, string>(kvp.Key, kvp.Value, OnAdditionalPropertyChanged));
      _additionalProperties.FireChange();
    }

    public void SetAdditionalProperty(SetAdditionalPropertyParameter additionalProperty)
    {
      if (string.IsNullOrEmpty(additionalProperty.Key))
      {
        ServiceRegistration.Get<ILogger>().Error("ViewModeModel: Unable to set additional property with null or empty key, value was '{0}'", additionalProperty.Value);
        return;
      }

      // If this is a new property we'll fire the collection changed event (in addition to the property changed event)
      bool fireChange = !_additionalProperties.ContainsKey(additionalProperty.Key);
      // The property is added automatically if it doesn't exist, and modifying the value will
      // trigger the OnAdditionalPropertyChanged handler which will persist it
      _additionalProperties[additionalProperty.Key].Value = additionalProperty.Value;
      // Notify any listeners that a new property has been added
      if (fireChange)
        _additionalProperties.FireChange();
    }

    public void SetViewMode(SetViewModeParameter viewMode)
    {
      SetViewMode(viewMode.Layout, viewMode.Size);
    }

    protected void SetViewMode(LayoutType layoutType, LayoutSize layoutSize)
    {
      LayoutType = layoutType;
      LayoutSize = layoutSize;

      MediaNavigationModel model = MediaNavigationModel.GetCurrentInstance();
      NavigationData navigationData = model.NavigationData;
      if (navigationData == null)
        return;

      navigationData.LayoutType = layoutType;
      navigationData.LayoutSize = layoutSize;
    }

    protected void UpdateSelectedFlag(ItemsList itemsList)
    {
      foreach (ListItem item in itemsList)
      {
        object layout;
        object size;
        if (item.AdditionalProperties.TryGetValue(Consts.KEY_LAYOUT_TYPE, out layout) && item.AdditionalProperties.TryGetValue(Consts.KEY_LAYOUT_SIZE, out size))
          item.Selected = LayoutType.Equals(layout) && LayoutSize.Equals(size);
      }
    }

    #region Members to be accessed from the GUI

    public ItemsList ViewModeItemsList
    {
      get
      {
        UpdateSelectedFlag(_viewModeItemsList);
        return _viewModeItemsList;
      }
    }

    public AbstractProperty LayoutTypeProperty
    {
      get { return _layoutTypeProperty; }
    }

    public LayoutType LayoutType
    {
      get { return (LayoutType) _layoutTypeProperty.GetValue(); }
      set { _layoutTypeProperty.SetValue(value); }
    }

    public AbstractProperty LayoutSizeProperty
    {
      get { return _layoutSizeProperty; }
    }

    public LayoutSize LayoutSize
    {
      get { return (LayoutSize) _layoutSizeProperty.GetValue(); }
      set { _layoutSizeProperty.SetValue(value); }
    }

    public ObservableDictionary<string, KeyValueItem<string, string>> AdditionalProperties
    {
      get { return _additionalProperties; }
    }

    #endregion
  }
}
