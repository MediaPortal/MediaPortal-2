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

using MediaPortal.Common.Configuration;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UiComponents.Configuration.ConfigurationControllers;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance.Skin
{
  public class ThemeConfigurationController : DialogConfigurationController
  {
    #region Protected fields

    protected const string KEY_TECHNAME = "TechName";
    protected const string KEY_NAME = "Name";
    protected const string KEY_IMAGESRC = "ImageSrc";

    protected ItemsList _items;
    protected ListItem _choosenItem;

    #endregion

    public ThemeConfigurationController()
    {
      _items = new ItemsList();
    }

    public ItemsList Items
    {
      get { return _items; }
    }

    public ListItem ChoosenItem
    {
      get { return _choosenItem; }
      set { _choosenItem = value; }
    }

    public string ChoosenItemName
    {
      get { return _choosenItem == null ? null : _choosenItem[KEY_TECHNAME]; }
    }

    #region Base overrides

    protected override void SettingChanged()
    {
      _items.Clear();
      ThemeConfigSetting themeSetting = (ThemeConfigSetting) _setting;
      foreach (Theme theme in themeSetting.Themes)
      {
        ListItem themeItem = new ListItem(KEY_NAME, theme.ShortDescription);
        themeItem.SetLabel(KEY_TECHNAME, theme.Name);
        ISkinResourceBundle resourceBundle;
        string preview = theme.GetResourceFilePath(theme.PreviewResourceKey, false, out resourceBundle);
        themeItem.SetLabel(KEY_IMAGESRC, preview);
        _items.Add(themeItem);
        if (themeSetting.CurrentThemeName == theme.Name)
        {
          themeItem.Selected = true;
          _choosenItem = themeItem;
        }
      }
      _items.FireChange();
      base.SettingChanged();
    }

    protected override void UpdateSetting()
    {
      ThemeConfigSetting themeSetting = (ThemeConfigSetting) _setting;
      themeSetting.CurrentThemeName = ChoosenItemName;
      base.UpdateSetting();
    }

    public override bool IsSettingSupported(ConfigSetting setting)
    {
      return setting != null && setting is ThemeConfigSetting;
    }

    protected override string DialogScreen
    {
      get { return "skinengine_config_skinresource_dialog"; }
    }

    public override System.Type ConfigSettingType
    {
      get { return typeof(ThemeConfigSetting); }
    }

    #endregion
  }
}
