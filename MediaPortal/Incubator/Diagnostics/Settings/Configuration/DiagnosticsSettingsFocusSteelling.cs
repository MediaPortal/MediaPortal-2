﻿/*
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

using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
    internal class DiagnosticsSettingsFocusSteelling : YesNo
    {

        #region Public Methods

        public override void Load()
        {
            _yes = Service.DiagnosticsHandler.FocusSteelingInstance.IsMonitoring;
        }

        public override void Save()
        {
            if (_yes)
            {
                Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Debug);
                Service.DiagnosticsHandler.FocusSteelingInstance.SubscribeToMessages();
            }
            else
            {
                Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Info);
                Service.DiagnosticsHandler.FocusSteelingInstance.UnsubscribeFromMessages();
            }
        }

        #endregion Public Methods

    }
}