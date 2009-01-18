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
  public class ThemeConfiguration : CustomConfiguration
  {
    #region Protected fields

    protected const string KEY_TECHNAME = "TechName";
    protected const string KEY_NAME = "Name";
    protected const string KEY_IMAGESRC = "ImageSrc";

    protected ItemsList _allThemes;
    protected ListItem _choosenThemeItem;

    #endregion

    public ThemeConfiguration()
    {
      _allThemes = new ItemsList();
    }

    public ItemsList AllThemes
    {
      get { return _allThemes; }
    }

    public ListItem ChoosenItem
    {
      get { return _choosenThemeItem; }
      set { _choosenThemeItem = value; }
    }

    public string ChoosenTheme
    {
      get { return _choosenThemeItem[KEY_TECHNAME]; }
    }

    #region Public Methods

    public override void Load()
    {
      _allThemes.Clear();
      SkinManager skinManager = ServiceScope.Get<ISkinResourceManager>() as SkinManager;
      if (skinManager == null)
        return;
      SkinSettings settings = SettingsManager.Load<SkinSettings>();
      string currentSkinName = settings.Skin;
      SkinManagement.Skin currentSkin = skinManager.Skins.ContainsKey(currentSkinName) ? skinManager.Skins[currentSkinName] : null;
      if (currentSkin == null)
        return;
      string currentThemeName = settings.Theme;
      foreach (Theme theme in currentSkin.Themes.Values)
      {
        if (!theme.IsValid)
          continue;
        ListItem themeItem = new ListItem(KEY_NAME, theme.ShortDescription);
        themeItem.SetLabel(KEY_TECHNAME, theme.Name);
        string preview = theme.GetResourceFilePath(theme.PreviewResourceKey, false);
        themeItem.SetLabel(KEY_IMAGESRC, preview);
        _allThemes.Add(themeItem);
        if (currentThemeName == theme.Name)
          _choosenThemeItem = themeItem;
      }
    }

    public override void Save()
    {
      SkinSettings settings = SettingsManager.Load<SkinSettings>();
      settings.Theme = ChoosenTheme;
      SettingsManager.Save(settings);
    }

    public override void Apply()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.SwitchTheme(ChoosenTheme);
    }

    #endregion
  }
}
