#region Copyright (C) 2007-2019 Team MediaPortal

/*
    Copyright (C) 2007-2019 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Settings;

namespace Tests.Client.VideoPlayers.Subtitles
{
  public class FakeVideoSettings : ISettingsManager
  {
    private readonly VideoSettings _videoSettings;

    public FakeVideoSettings(VideoSettings settings)
    {
      _videoSettings = settings;
    }

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return (SettingsType)Load(typeof(SettingsType));
    }

    public object Load(Type settingsType)
    {
      return _videoSettings;
    }

    public void Save(object settingsObject)
    {
      throw new NotImplementedException();
    }

    public void StartBatchUpdate()
    {
      throw new NotImplementedException();
    }

    public void EndBatchUpdate()
    {
      throw new NotImplementedException();
    }

    public void CancelBatchUpdate()
    {
      throw new NotImplementedException();
    }

    public void ClearCache()
    {
      throw new NotImplementedException();
    }

    public void ChangeUserContext(string userName)
    {
      throw new NotImplementedException();
    }

    public void RemoveSettingsData(Type settingsType, bool user, bool global)
    {
      throw new NotImplementedException();
    }

    public void RemoveAllSettingsData(bool user, bool global)
    {
      throw new NotImplementedException();
    }
  }
}
