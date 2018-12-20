#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Settings
{
  public class SlimTvLogoSettings
  {
    private List<string> _logoThemes = new List<string>();

    public SlimTvLogoSettings()
    {
      _logoThemes = new List<string>();
      if (ServiceRegistration.Get<ISystemResolver>().SystemType == SystemType.Server)
      {
        string designsFolder = FileUtils.BuildAssemblyRelativePath("Designs");
        if (Directory.Exists(designsFolder))
          foreach (var file in Directory.GetFiles(designsFolder, "*.logotheme"))
            _logoThemes.Add(Path.GetFileNameWithoutExtension(file));
      }
    }

    /// <summary>
    /// Helper property to transport available themes from Server to Client.
    /// Note: No SettingAttribute here, otherwise the value is saved and not new created!
    /// </summary>
    public List<string> LogoThemes
    {
      get { return _logoThemes; }
      set { _logoThemes = new List<string>(value); }
    }

    [Setting(SettingScope.Global, "http://logomanager.team-mediaportal.com/")]
    public string RepositoryUrl { get; set; }

    [Setting(SettingScope.Global, "Flat-default")]
    public string LogoTheme { get; set; }
  }
}
