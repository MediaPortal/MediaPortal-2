#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Core.Configuration;
using MediaPortal.UI.Presentation.DataObjects;
using UiComponents.Configuration.ConfigurationControllers;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance.Skin
{
  public class SkinConfigurationController : DialogConfigurationController
  {
    #region Protected fields

    protected const string KEY_TECHNAME = "TechName";
    protected const string KEY_NAME = "Name";
    protected const string KEY_IMAGESRC = "ImageSrc";

    protected ItemsList _items;
    protected ListItem _choosenItem;

    #endregion

    public SkinConfigurationController()
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
      SkinConfigSetting skinSetting = (SkinConfigSetting) _setting;
      foreach (SkinManagement.Skin skin in skinSetting.Skins)
      {
        ListItem skinItem = new ListItem(KEY_NAME, skin.ShortDescription);
        skinItem.SetLabel(KEY_TECHNAME, skin.Name);
        string preview = skin.GetResourceFilePath(skin.PreviewResourceKey, false);
        skinItem.SetLabel(KEY_IMAGESRC, preview);
        _items.Add(skinItem);
        if (skinSetting.CurrentSkinName == skin.Name)
          _choosenItem = skinItem;
      }
      _items.FireChange();
      base.SettingChanged();
    }

    protected override void UpdateSetting()
    {
      SkinConfigSetting skinSetting = (SkinConfigSetting) _setting;
      skinSetting.CurrentSkinName = ChoosenItemName;
      base.UpdateSetting();
    }

    public override bool IsSettingSupported(ConfigSetting setting)
    {
      return setting == null ? false : setting is SkinConfigSetting;
    }

    protected override string DialogScreen
    {
      get { return "skinengine_config_skinresource_dialog"; }
    }

    public override System.Type ConfigSettingType
    {
      get { return typeof(SkinConfigSetting); }
    }

    #endregion
  }
}
