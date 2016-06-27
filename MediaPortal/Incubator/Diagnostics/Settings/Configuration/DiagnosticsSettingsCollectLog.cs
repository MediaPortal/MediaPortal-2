#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using Microsoft.Win32;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
    internal class DiagnosticsSettingsCollectLog : YesNo
    {
        #region Methods

        public override void Load()
        {
            _yes = false;
        }

        public override void Save()
        {
            if (!_yes) return;

            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Team Mediaportal\Mediaportal 2"))
            {
                if (registryKey != null)
                {
                    string logCollectorPath = (string)registryKey.GetValue("INSTALLDIR_LOG_COLLECTOR");
                    string collectorPath = System.IO.Path.Combine(logCollectorPath, "MP2-LogCollector.exe");
                    if (System.IO.File.Exists(collectorPath))
                    {
                        System.Diagnostics.Process.Start(collectorPath);
                    }
                }
            }
        }

        #endregion Methods
    }
}