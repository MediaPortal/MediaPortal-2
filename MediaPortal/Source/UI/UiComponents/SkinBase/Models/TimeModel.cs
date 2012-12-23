#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UiComponents.SkinBase.Settings;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which provides data for the skin clock.
  /// </summary>
  public class TimeModel : BaseTimerControlledModel
  {
    public const string STR_TIME_MODEL_ID = "E821B1C8-0666-4339-8027-AA45A4F6F107";
    public static readonly Guid TIME_MODEL_ID = new Guid(STR_TIME_MODEL_ID);

    #region Protected fields

    protected string _dateFormat = "D";
    protected string _timeFormat = "t";

    protected AbstractProperty _currentTimeProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _currentDateProperty = new WProperty(typeof(string), string.Empty);

    protected AbstractProperty _hourAngleProperty = new WProperty(typeof(double), 0.0);
    protected AbstractProperty _minuteAngleProperty = new WProperty(typeof(double), 0.0);

    #endregion

    public TimeModel() : base(500)
    {
      ReadSettings();

      Update();
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(SkinMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    protected void ReadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      SkinBaseSettings settings = settingsManager.Load<SkinBaseSettings>();
      _dateFormat = settings.DateFormat;
      _timeFormat = settings.TimeFormat;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SkinMessaging.CHANNEL)
      {
        if (((SkinMessaging.MessageType) message.MessageType) ==
            SkinMessaging.MessageType.DateTimeFormatChanged)
            // The DateFormat and TimeFormat configuration classes will send this message when they
            // changed the formats, so we have to update our format here
          ReadSettings();
      }
    }

    public Guid ModelId
    {
      get { return TIME_MODEL_ID; }
    }

    protected override void Update()
    {
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      CultureInfo culture = localization.CurrentCulture;

      DateTime now = DateTime.Now;

      double angle = now.Hour * 30;
      HourAngle = angle + 12 * now.Minute / 60.0;

      MinuteAngle = now.Minute * 6;

      CurrentTime = now.ToString(_timeFormat, culture);
      CurrentDate = now.ToString(_dateFormat, culture);
    }

    public AbstractProperty CurrentDateProperty
    {
      get { return _currentDateProperty; }
    }

    public string CurrentDate
    {
      get { return (string) _currentDateProperty.GetValue(); }
      set { _currentDateProperty.SetValue(value); }
    }

    public AbstractProperty CurrentTimeProperty
    {
      get { return _currentTimeProperty; }
    }

    public string CurrentTime
    {
      get { return (string) _currentTimeProperty.GetValue(); }
      set { _currentTimeProperty.SetValue(value); }
    }

    public AbstractProperty HourAngleProperty
    {
      get { return _hourAngleProperty; }
    }

    public double HourAngle
    {
      get { return (double)_hourAngleProperty.GetValue(); }
      set { _hourAngleProperty.SetValue(value); }
    }

    public AbstractProperty MinuteAngleProperty
    {
      get { return _minuteAngleProperty; }
    }

    public double MinuteAngle
    {
      get { return (double)_minuteAngleProperty.GetValue(); }
      set { _minuteAngleProperty.SetValue(value); }
    }
  }
}