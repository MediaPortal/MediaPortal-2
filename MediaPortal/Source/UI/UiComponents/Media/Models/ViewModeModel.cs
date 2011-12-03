#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models
{
  public class ViewModeModel
  {
    #region Consts

    public const string VM_MODEL_ID_STR = "6997CD35-63F0-4F96-8997-E18C9382E2DC";
    public static Guid VM_MODEL_ID = new Guid(VM_MODEL_ID_STR);

    #endregion

    protected readonly AbstractProperty _layoutTypeProperty;
    protected readonly AbstractProperty _layoutSizeProperty;
    protected readonly ItemsList _viewModeItemsList = new ItemsList();


    public ViewModeModel()
    {
      ViewSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      _layoutTypeProperty = new WProperty(typeof(LayoutType), settings.LayoutType);
      _layoutSizeProperty = new WProperty(typeof(LayoutSize), settings.LayoutSize);

      ListItem smallList = new ListItem(Consts.KEY_NAME, Consts.RES_SMALL_LIST)
        {
            Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Small))
        };
      _viewModeItemsList.Add(smallList);
      ListItem mediumlList = new ListItem(Consts.KEY_NAME, Consts.RES_MEDIUM_LIST)
        {
            Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Medium))
        };
      _viewModeItemsList.Add(mediumlList);
      ListItem largeList = new ListItem(Consts.KEY_NAME, Consts.RES_LARGE_LIST)
        {
            Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.ListLayout, LayoutSize.Large))
        };
      _viewModeItemsList.Add(largeList);
      ListItem largeGrid = new ListItem(Consts.KEY_NAME, Consts.RES_LARGE_Grid)
        {
            Command = new MethodDelegateCommand(() => SetViewMode(LayoutType.GridLayout, LayoutSize.Large))
        };
      _viewModeItemsList.Add(largeGrid);
    }

    protected void SetViewMode(LayoutType layoutType, LayoutSize layoutSize)
    {
      LayoutType = layoutType;
      LayoutSize = layoutSize;

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ViewSettings settings = settingsManager.Load<ViewSettings>();
      settings.LayoutType = layoutType;
      settings.LayoutSize = layoutSize;
      settingsManager.Save(settings);
    }

    #region Members to be accessed from the GUI

    public ItemsList ViewModeItemsList
    {
      get { return _viewModeItemsList; }
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

    #endregion
  }
}
