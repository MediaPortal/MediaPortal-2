#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Drawing;
using System.Linq;
using MediaPortal.Helpers.SkinHelper.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;

namespace MediaPortal.Helpers.SkinHelper.Models
{
  public class ThemeColorsModel : IWorkflowModel
  {
    #region Consts

    protected const string MODEL_ID_STR = "1C27C172-AD96-45A5-9E72-736E6D4B1ED5";

    protected static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected ItemsList _themeColors;

    #endregion

    public ThemeColorsModel()
    {
      _themeColors = new ItemsList();
    }

    #region Public members to be called from the GUI

    public ItemsList ThemeColors
    {
      get { return _themeColors; }
    }

    public void Update()
    {
      lock (_syncObj)
      {
        _themeColors.Clear();
        foreach (ListItem item in CollectSkinResourcesColors(SkinContext.SkinResources).OrderBy(item => item.Labels[Consts.KEY_NAME]))
          _themeColors.Add(item);
        _themeColors.FireChange();
      }
    }

    #endregion

    protected ICollection<ListItem> CollectSkinResourcesColors(SkinResources resourceBundle)
    {
      ICollection<ListItem> result = new List<ListItem>();
      foreach (KeyValuePair<object, object> localStyleResource in resourceBundle.LocalStyleResources)
      {
        object key = localStyleResource.Key;
        object value = localStyleResource.Value;
        object c;
        object testString;
        int testInt;
        if (TypeConverter.Convert(value, typeof(Color), out c))
          // Avoid conversion of strings containing int values
          if (!TypeConverter.Convert(value, typeof(string), out testString) || !int.TryParse((string) testString, out testInt))
          {
            Color color = (Color) c;
            result.Add(BuildColorListItem(key.ToString(), color));
          }
      }
      SkinResources inherited = resourceBundle.InheritedSkinResources;
      if (inherited != null)
        CollectionUtils.AddAll(result, CollectSkinResourcesColors(inherited));
      return result;
    }

    protected ListItem BuildColorListItem(string name, Color color)
    {
      ListItem result = new ListItem(Consts.KEY_NAME, name);
      result.AdditionalProperties[Consts.KEY_COLOR] = color;
      return result;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Update();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
