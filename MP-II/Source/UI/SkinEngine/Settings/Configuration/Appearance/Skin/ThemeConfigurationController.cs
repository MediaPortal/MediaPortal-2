#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Configuration;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.SkinManagement;
using UiComponents.Configuration.ConfigurationControllers;

namespace MediaPortal.SkinEngine.Settings.Configuration.Appearance.Skin
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
        string preview = theme.GetResourceFilePath(theme.PreviewResourceKey, false);
        themeItem.SetLabel(KEY_IMAGESRC, preview);
        _items.Add(themeItem);
        if (themeSetting.CurrentThemeName == theme.Name)
          _choosenItem = themeItem;
      }
    }

    protected override void UpdateSetting()
    {
      ThemeConfigSetting themeSetting = (ThemeConfigSetting) _setting;
      themeSetting.CurrentThemeName = ChoosenItemName;
      base.UpdateSetting();
    }

    public override bool IsSettingSupported(ConfigSetting setting)
    {
      return setting == null ? false : setting is ThemeConfigSetting;
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
