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

using System.Threading.Tasks;
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.Plugins.SlimTv.Providers.Settings.Configuration
{
  public class MPExtendedPassword : Entry
  {
    public override async Task Load()
    {
      _value = (await SettingsManager.LoadAsync<MPExtendedProviderSettings>()).Password;
    }

    public override async Task Save()
    {
      await base.Save();
      MPExtendedProviderSettings settings = await SettingsManager.LoadAsync<MPExtendedProviderSettings>();
      settings.Password = _value;
      await SettingsManager.SaveAsync(settings);
    }

    public override int DisplayLength
    {
      get { return 50; }
    }
  }
}
