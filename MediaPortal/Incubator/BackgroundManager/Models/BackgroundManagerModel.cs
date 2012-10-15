#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class BackgroundManagerModel
  {
    #region Consts

    public const string BGM_MODEL_ID_STR = "1F4CAEDE-7108-483d-B5C8-18BEC7EC58E5";
    public static Guid BGM_MODEL_ID = new Guid(BGM_MODEL_ID_STR);
    protected const string ITEM_ACTION_KEY = "MenuModel: Item-Action";

    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".png" };
    private const string DEFAULT_BACKGROUND = "defaultBackground.jpg";

    #endregion

    protected AbstractProperty _selectedItemProperty;
    protected AbstractProperty _backgroundImageProperty;

    public BackgroundManagerModel()
    {
      _selectedItemProperty = new SProperty(typeof (ListItem), null);
      _selectedItemProperty.Attach(SetBackgroundImage);
      _backgroundImageProperty = new SProperty(typeof (string), string.Empty);
      SetBackgroundImage();
    }

    public Guid ModelId
    {
      get { return BGM_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem)_selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
    }

    public AbstractProperty BackgroundImageProperty
    {
      get { return _backgroundImageProperty; }
    }

    public string BackgroundImage
    {
      get { return (string) _backgroundImageProperty.GetValue(); }
      internal set { _backgroundImageProperty.SetValue(value); }
    }

    public void SetSelectedItem(ListItem item)
    {
      SelectedItem = item;
    }

    #endregion

    private void SetBackgroundImage(AbstractProperty property, object value)
    {
      SetBackgroundImage();
    }

    private void SetBackgroundImage()
    {
      if (SelectedItem != null)
      {
        object actionObject;
        if (SelectedItem.AdditionalProperties.TryGetValue(ITEM_ACTION_KEY, out actionObject))
        {
          WorkflowAction action = (WorkflowAction) actionObject;
          foreach (string allowedImageExtension in _allowedImageExtensions)
          {
            string fileName = string.Format("{0}{1}", action.ActionId, allowedImageExtension);
            // Is the path a local skin file?
            string sourceFilePath = SkinContext.SkinResources.GetResourceFilePath(string.Format("{0}\\{1}", SkinResources.IMAGES_DIRECTORY, fileName));
            if (!String.IsNullOrEmpty(sourceFilePath))
            {
              BackgroundImage = fileName;
              return;
            }
          }
        }
      }

      string defaultFilePath = SkinContext.SkinResources.GetResourceFilePath(string.Format("{0}\\{1}", SkinResources.IMAGES_DIRECTORY, DEFAULT_BACKGROUND));
      BackgroundImage = String.IsNullOrEmpty(defaultFilePath) ? null : DEFAULT_BACKGROUND;
    }
  }
}
