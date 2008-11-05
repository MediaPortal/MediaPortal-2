#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

// Todo: do we still need this with new bassplayer?

//using MediaPortal.Core;
//using MediaPortal.Core.Messaging;
//using MediaPortal.Core.Settings;

//using Media.Players.BassPlayer;

//namespace Models.Settings
//{
//  public class MusicSettings 
//  {
//    BassPlayerSettings _settings;
    
//    public MusicSettings()
//    {
//      // Load the Settings
//      _settings = new BassPlayerSettings();
//      ServiceScope.Get<ISettingsManager>().Load(_settings);
//    }

//    /// <summary>
//    /// Sends a Notification that the setting has changed
//    /// </summary>
//    private void NotifySettingsChanged()
//    {
//      QueueMessage msg = new QueueMessage();
//      msg.MessageData["action"] = "settingschanged";
//      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("bass");
//      queue.Send(msg);
//    }

//    public bool IsEnabledFadeOut
//    {
//      get { return _settings.SoftStop; }
//    }

//    public void EnableFadeOut()
//    {
//      _settings.SoftStop = true;
//      ServiceScope.Get<ISettingsManager>().Save(_settings);
//      NotifySettingsChanged();
//    }
//    public void DisableFadeOut()
//    {
//      _settings.SoftStop = false;
//      ServiceScope.Get<ISettingsManager>().Save(_settings);
//      NotifySettingsChanged();
//    }

//    public bool IsGaplessPlayback
//    {
//      get { return _settings.GaplessPlayback; }
//    }

//    public void DisableGapless()
//    {
//      _settings.GaplessPlayback = false;
//      ServiceScope.Get<ISettingsManager>().Save(_settings);
//      NotifySettingsChanged();
//    }
//    public void EnableGapless()
//    {
//      _settings.GaplessPlayback = true;
//      ServiceScope.Get<ISettingsManager>().Save(_settings);
//      NotifySettingsChanged();
//    }
//  }
//}
