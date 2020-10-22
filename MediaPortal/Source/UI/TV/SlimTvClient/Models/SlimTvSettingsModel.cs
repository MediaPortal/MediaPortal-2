#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.SlimTv.Client.Settings;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvSettingsModel"/> acts as base settings class for all Tv models.
  /// </summary>
  public class SlimTvSettingsModel : IDisposable
  {
    #region Protected fields

    protected AbstractProperty _showChannelNamesProperty = null;
    protected AbstractProperty _showChannelNumbersProperty = null;
    protected AbstractProperty _showChannelLogosProperty = null;
    protected AbstractProperty _showGenreColorsProperty = null;
    protected AbstractProperty _showSeriesInfoProperty = null;
    protected AbstractProperty _showRecordingStartedNotificationsProperty = null;
    protected AbstractProperty _showRecordingEndedNotificationsProperty = null;
    protected AbstractProperty _recordingNotificationDurationProperty = null;
    protected AbstractProperty _singleChannelGuideDaysProperty = null;
    protected AbstractProperty _numberOfRowsProperty = null;
    protected AbstractProperty _visibleHoursProperty = null;
    protected AbstractProperty _zapTimeoutProperty = null;
    protected AbstractProperty _zapFromGuideProperty = null;
    protected AbstractProperty _zapByChannelIndexProperty = null;
    protected AbstractProperty _hideAllChannelsGroupProperty = null;
    protected AbstractProperty _autoStartTVProperty = null;
    
    protected SettingsChangeWatcher<SlimTvClientSettings> _settings = null;

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes whether channel names should be shown by the skin.
    /// </summary>
    public bool ShowChannelNames
    {
      get { return (bool)_showChannelNamesProperty.GetValue(); }
      protected set { _showChannelNamesProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel names should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelNamesProperty
    {
      get { return _showChannelNamesProperty; }
    }

    /// <summary>
    /// Exposes whether channel numbers should be shown by the skin.
    /// </summary>
    public bool ShowChannelNumbers
    {
      get { return (bool)_showChannelNumbersProperty.GetValue(); }
      protected set { _showChannelNumbersProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel numbers should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelNumbersProperty
    {
      get { return _showChannelNumbersProperty; }
    }

    /// <summary>
    /// Exposes whether channel logos should be shown by the skin.
    /// </summary>
    public bool ShowChannelLogos
    {
      get { return (bool)_showChannelLogosProperty.GetValue(); }
      protected set { _showChannelLogosProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel logos should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelLogosProperty
    {
      get { return _showChannelLogosProperty; }
    }

    /// <summary>
    /// Exposes whether EPG genre colors should be shown by the skin.
    /// </summary>
    public bool ShowGenreColors
    {
      get { return (bool)_showGenreColorsProperty.GetValue(); }
      set { _showGenreColorsProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether EPG genre colors should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowGenreColorsProperty
    {
      get { return _showGenreColorsProperty; }
    }

    /// <summary>
    /// Defines the number of rows to be visible in EPG.
    /// </summary>
    public int NumberOfRows
    {
      get { return (int)_numberOfRowsProperty.GetValue(); }
      protected set { _numberOfRowsProperty.SetValue(value); }
    }

    /// <summary>
    /// Defines the number of rows to be visible in EPG.
    /// </summary>
    public AbstractProperty NumberOfRowsProperty
    {
      get { return _numberOfRowsProperty; }
    }

    /// <summary>
    /// Defines the number of hours to be visible in EPG.
    /// </summary>
    public double VisibleHours
    {
      get { return (double)_visibleHoursProperty.GetValue(); }
      protected set { _visibleHoursProperty.SetValue(value); }
    }

    /// <summary>
    /// Defines the number of hours to be visible in EPG.
    /// </summary>
    public AbstractProperty VisibleHoursProperty
    {
      get { return _visibleHoursProperty; }
    }

    /// <summary>
    /// Defines the zapping timeout in seconds.
    /// </summary>
    public double ZapTimeout
    {
      get { return (double)_zapTimeoutProperty.GetValue(); }
      protected set { _zapTimeoutProperty.SetValue(value); }
    }

    /// <summary>
    /// Defines the zapping timeout in seconds.
    /// </summary>
    public AbstractProperty ZapTimeoutProperty
    {
      get { return _zapTimeoutProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, the FullGuide will automatically start tuning a currently running program.
    /// Recording and further options will be only available by context menu then.
    /// </summary>
    public bool ZapFromGuide
    {
      get { return (bool)_zapFromGuideProperty.GetValue(); }
      protected set { _zapFromGuideProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, the FullGuide will automatically start tuning a currently running program.
    /// Recording and further options will be only available by context menu then.
    /// </summary>
    public AbstractProperty ZapFromGuideProperty
    {
      get { return _zapFromGuideProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, TV gets started when entering TV home state.
    /// </summary>
    public bool AutoStartTV
    {
      get { return (bool)_autoStartTVProperty.GetValue(); }
      protected set { _autoStartTVProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, TV gets started when entering TV home state.
    /// </summary>
    public AbstractProperty AutoStartTVProperty
    {
      get { return _autoStartTVProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, series info will be shown in program details.
    /// </summary>
    public bool ShowSeriesInfo
    {
      get { return (bool)_showSeriesInfoProperty.GetValue(); }
      protected set { _showSeriesInfoProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, series info will be shown in program details.
    /// </summary>
    public AbstractProperty ShowSeriesInfoProperty
    {
      get { return _showSeriesInfoProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, zapping uses the actual index of channel inside current group.
    /// If <c>false</c>, the logical channel number of the channel will be used.
    /// </summary>
    public bool ZapByChannelIndex
    {
      get { return (bool)_zapByChannelIndexProperty.GetValue(); }
      protected set { _zapByChannelIndexProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, zapping uses the actual index of channel inside current group.
    /// If <c>false</c>, the logical channel number of the channel will be used.
    /// </summary>
    public AbstractProperty ZapByChannelIndexProperty
    {
      get { return _zapByChannelIndexProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, the inbuilt "All Channels" group will be hidden.
    /// </summary>
    public bool HideAllChannelsGroup
    {
      get { return (bool)_hideAllChannelsGroupProperty.GetValue(); }
      protected set { _hideAllChannelsGroupProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, the inbuilt "All Channels" group will be hidden.
    /// </summary>
    public AbstractProperty HideAllChannelsGroupProperty
    {
      get { return _hideAllChannelsGroupProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, a notification will be shown when a recording has started.
    /// </summary>
    public bool ShowRecordingStartedNotifications
    {
      get { return (bool)_showRecordingStartedNotificationsProperty.GetValue(); }
      protected set { _showRecordingStartedNotificationsProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, a notification will be shown when a recording has started.
    /// </summary>
    public AbstractProperty ShowRecordingStartedNotificationsProperty
    {
      get { return _showRecordingStartedNotificationsProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, a notification will be shown when a recording has ended.
    /// </summary>
    public bool ShowRecordingEndedNotifications
    {
      get { return (bool)_showRecordingEndedNotificationsProperty.GetValue(); }
      protected set { _showRecordingEndedNotificationsProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, a notification will be shown when a recording has ended.
    /// </summary>
    public AbstractProperty ShowRecordingEndedNotificationsProperty
    {
      get { return _showRecordingEndedNotificationsProperty; }
    }

    /// <summary>
    /// The time in seconds to show recording notifications.
    /// </summary>
    public int RecordingNotificationDuration
    {
      get { return (int)_recordingNotificationDurationProperty.GetValue(); }
      protected set { _recordingNotificationDurationProperty.SetValue(value); }
    }

    /// <summary>
    /// The time in seconds to show recording notifications.
    /// </summary>
    public AbstractProperty RecordingNotificationDurationProperty
    {
      get { return _recordingNotificationDurationProperty; }
    }

    /// <summary>
    /// The number of days of EPG data to load when viewing the single channel guide.
    /// </summary>
    public int SingleChannelGuideDays
    {
      get { return (int)_singleChannelGuideDaysProperty.GetValue(); }
      protected set { _singleChannelGuideDaysProperty.SetValue(value); }
    }

    /// <summary>
    /// The number of days of EPG data to load when viewing the single channel guide.
    /// </summary>
    public AbstractProperty SingleChannelGuideDaysProperty
    {
      get { return _singleChannelGuideDaysProperty; }
    }

    #endregion

    #region Members

    #region Inits and Updates

    public SlimTvSettingsModel()
    {
      _showChannelNamesProperty = new WProperty(typeof(bool), true);
      _showChannelNumbersProperty = new WProperty(typeof(bool), true);
      _showChannelLogosProperty = new WProperty(typeof(bool), true);
      _showGenreColorsProperty = new WProperty(typeof(bool), false);
      _showSeriesInfoProperty = new WProperty(typeof(bool), true);
      _showRecordingStartedNotificationsProperty = new WProperty(typeof(bool), true);
      _showRecordingEndedNotificationsProperty = new WProperty(typeof(bool), true);
      _recordingNotificationDurationProperty = new WProperty(typeof(int), 5);
      _singleChannelGuideDaysProperty = new WProperty(typeof(int), 14);
      _numberOfRowsProperty = new WProperty(typeof(int), 7);
      _visibleHoursProperty = new WProperty(typeof(double), 2d);
      _zapTimeoutProperty = new WProperty(typeof(double), 2d);
      _zapFromGuideProperty = new WProperty(typeof(bool), true);
      _zapByChannelIndexProperty = new WProperty(typeof(bool), true);
      _hideAllChannelsGroupProperty = new WProperty(typeof(bool), false);
      _autoStartTVProperty = new WProperty(typeof(bool), false);

      InitSettingsWatcher();
    }

    private void InitSettingsWatcher()
    {
      if (_settings != null)
        return;
      _settings = new SettingsChangeWatcher<SlimTvClientSettings>(true);
      UpdatePropertiesFromSettings(_settings.Settings);
      _settings.SettingsChanged = OnSettingsChanged;
    }

    protected void OnSettingsChanged(object sender, EventArgs e)
    {
      UpdatePropertiesFromSettings(_settings.Settings);
    }

    protected void UpdatePropertiesFromSettings(SlimTvClientSettings settings)
    {
      ShowChannelNames = settings.EpgShowChannelNames;
      ShowChannelNumbers = settings.EpgShowChannelNumbers;
      ShowChannelLogos = settings.EpgShowChannelLogos;
      ShowGenreColors = settings.EpgShowGenreColors;
      NumberOfRows = settings.EpgNumberOfRows;
      VisibleHours = settings.EpgVisibleHours;
      ZapTimeout = settings.ZapTimeout;
      ZapFromGuide = settings.ZapFromGuide;
      AutoStartTV = settings.AutoStartTV;
      ShowSeriesInfo = settings.ShowSeriesInfo;
      ZapByChannelIndex = settings.ZapByChannelIndex;
      HideAllChannelsGroup = settings.HideAllChannelsGroup;
      ShowRecordingStartedNotifications = settings.ShowRecordingStartedNotifications;
      ShowRecordingEndedNotifications = settings.ShowRecordingEndedNotifications;
      RecordingNotificationDuration = settings.RecordingNotificationDuration;
      SingleChannelGuideDays = settings.SingleChannelGuideDays;
    }

    public void Dispose()
    {
      _settings?.Dispose();
    }

    #endregion

    #endregion
  }
}
