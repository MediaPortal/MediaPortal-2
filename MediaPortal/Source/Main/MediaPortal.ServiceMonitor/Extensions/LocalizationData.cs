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

using System;
using System.ComponentModel;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Localization;


namespace MediaPortal.ServiceMonitor.Extensions
{
  public class LocalizationData : IMessageReceiver, INotifyPropertyChanged, IDisposable
  {
    private readonly string _key;

    #region ctor

    public LocalizationData(string key)
    {
      _key = key;
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(LocalizationMessaging.CHANNEL, this);
    }

    #endregion

    #region public properties

    public object Value
    {
      get { return ServiceRegistration.Get<ILocalization>().ToString(_key); }
    }

    #endregion

    #region IMessageReceiver implementation

    public void Receive(SystemMessage message)
    {
      if (message.ChannelName == LocalizationMessaging.CHANNEL)
        if (((LocalizationMessaging.MessageType) message.MessageType) ==
            LocalizationMessaging.MessageType.LanguageChanged)
          OnLanguageChanged();
    }

    #endregion

    #region INotifyPropertyChanged implementation

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnLanguageChanged()
    {
      var handler = PropertyChanged;
      if (handler != null)
        handler(this, new PropertyChangedEventArgs("Value"));
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      ServiceRegistration.Get<IMessageBroker>().UnregisterMessageReceiver(LocalizationMessaging.CHANNEL, this);
    }

    #endregion
  }
}

