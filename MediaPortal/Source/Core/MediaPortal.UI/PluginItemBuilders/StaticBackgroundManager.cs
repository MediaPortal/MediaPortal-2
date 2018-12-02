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

using MediaPortal.Common;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.PluginItemBuilders
{
  public class StaticBackgroundManager : IBackgroundManager
  {
    #region Protected fields

    protected string _backgroundScreenName;

    #endregion

    public StaticBackgroundManager()
    {
      _backgroundScreenName = null;
    }

    public StaticBackgroundManager(string screenName)
    {
      _backgroundScreenName = screenName;
    }

    #region IBackgroundManager implementation

    public void Install()
    {
      if (!string.IsNullOrEmpty(_backgroundScreenName))
      {
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        screenManager.SetBackgroundLayer(_backgroundScreenName);
      }
    }

    public void Uninstall()
    {
    }

    #endregion
  }
}