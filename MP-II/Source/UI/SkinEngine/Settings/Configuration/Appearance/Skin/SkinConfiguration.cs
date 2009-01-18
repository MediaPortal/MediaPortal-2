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

using MediaPortal.Configuration.ConfigurationClasses;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screen;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Settings.Configuration.Appearance.Skin
{
  /// <summary>
  /// Custom configuration setting class to change the current skin.
  /// </summary>
  public class SkinConfiguration : CustomConfiguration
  {
    #region Protected fields

    protected const string KEY_TECHNAME = "TechName";
    protected const string KEY_NAME = "Name";
    protected const string KEY_IMAGESRC = "ImageSrc";

    protected ItemsList _allSkins;
    protected ListItem _choosenSkinItem;

    #endregion

    public SkinConfiguration()
    {
      _allSkins = new ItemsList();
    }

    public ItemsList AllSkins
    {
      get { return _allSkins; }
    }

    public ListItem ChoosenItem
    {
      get { return _choosenSkinItem; }
      set { _choosenSkinItem = value; }
    }

    public string ChoosenSkin
    {
      get { return _choosenSkinItem[KEY_TECHNAME]; }
    }

    #region Public Methods

    public override void Load()
    {
      _allSkins.Clear();
      SkinManager skinManager = ServiceScope.Get<ISkinResourceManager>() as SkinManager;
      if (skinManager == null)
        return;

      string currentSkinName = SettingsManager.Load<SkinSettings>().Skin;
      foreach (SkinManagement.Skin skin in skinManager.Skins.Values)
      {
        if (!skin.IsValid)
          continue;
        ListItem skinItem = new ListItem(KEY_NAME, skin.ShortDescription);
        skinItem.SetLabel(KEY_TECHNAME, skin.Name);
        string preview = skin.GetResourceFilePath(skin.PreviewResourceKey, false);
        skinItem.SetLabel(KEY_IMAGESRC, preview);
        _allSkins.Add(skinItem);
        if (currentSkinName == skin.Name)
          _choosenSkinItem = skinItem;
      }
    }

    public override void Save()
    {
      SkinSettings settings = SettingsManager.Load<SkinSettings>();
      settings.Skin = ChoosenSkin;
      SettingsManager.Save(settings);
    }

    public override void Apply()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.SwitchSkin(ChoosenSkin);
    }

    #endregion
  }
}
