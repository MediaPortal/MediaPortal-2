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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities.Events;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// ChannelZapModel model provides additional seeking methods for media playback.
  /// </summary>
  public class ChannelZapModel : IDisposable
  {
    protected DelayedEvent _zapTimer;
    protected AbstractProperty _channelNumberOrIndexProperty;
    private const string CHANNEL_ZAP_SUPERLAYER_SCREEN_NAME = "ChannelZapOSD";

    public const string MODEL_ID_STR = "1C7DCFFE-E34E-41FD-9104-9AA594E49375";

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public ChannelZapModel()
    {
      _channelNumberOrIndexProperty = new WProperty(typeof(string), string.Empty);
    }

    public void Dispose()
    {
      if (_zapTimer != null)
        _zapTimer.Dispose();
    }

    #region GUI Properties

    /// <summary>
    /// Contains the user inputs of numbers which are either treated as channel index or absolute (logical) channel number.
    /// </summary>
    public string ChannelNumberOrIndex
    {
      get { return (string) _channelNumberOrIndexProperty.GetValue(); }
      internal set { _channelNumberOrIndexProperty.SetValue(value); }
    }

    public AbstractProperty ChannelNumberOrIndexProperty
    {
      get { return _channelNumberOrIndexProperty; }
    }

    public void ZapByNumber(string key)
    {
      int number;
      if (!int.TryParse(key, out number) || ChannelNumberOrIndex.Length >= 4)
        return;

      ChannelNumberOrIndex += number.ToString();
      ReSetZapTimer();
    }

    #endregion

    #region Timer handling

    private void ReSetZapTimer()
    {
      ShowZapOSD();
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      if (_zapTimer == null)
      {
        _zapTimer = new DelayedEvent(settings.ZapTimeout * 1000);
        _zapTimer.OnEventHandler += ZapTimerElapsed;
      }
      _zapTimer.EnqueueEvent(this, EventArgs.Empty);
    }

    private void ShowZapOSD()
    {
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      ISuperLayerManager superLayerManager = ServiceRegistration.Get<ISuperLayerManager>();
      superLayerManager.ShowSuperLayer(CHANNEL_ZAP_SUPERLAYER_SCREEN_NAME, TimeSpan.FromSeconds(settings.ZapTimeout));
    }

    private void HideZapOSD()
    {
      ChannelNumberOrIndex = string.Empty;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.SetSuperLayer(null);
    }

    private void ClearZapTimer()
    {
      if (_zapTimer != null)
        _zapTimer.Stop();

      ChannelNumberOrIndex = string.Empty;
      HideZapOSD();
    }

    private void ZapTimerElapsed(object sender, EventArgs eventArgs)
    {
      // Zap to channel
      int number;
      if (!int.TryParse(ChannelNumberOrIndex, out number))
        return;

      ClearZapTimer();

#if DEBUG_FOCUS
      ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Debug("EPG: ChannelZapModel goto {0}", number);
#endif
      SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager.CurrentNavigationContext.WorkflowModelId == SlimTvMultiChannelGuideModel.MODEL_ID)
      {
        SlimTvMultiChannelGuideModel guide = workflowManager.GetModel(SlimTvMultiChannelGuideModel.MODEL_ID) as SlimTvMultiChannelGuideModel;
        if (guide == null)
          return;
        if (settings.ZapByChannelIndex)
        {
          // Channel index starts by 0, user enters 1 based numbers
          number--;
          guide.GoToChannelIndex(number);
        } else
        {
          guide.GoToChannelNumber(number);
        }
        return;
      }
      SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
      if (model == null)
        return;

      // Special case "0", we use it for "zap back" to tune previous watched channel
      if (number == 0)
      {
        _ = model.ZapBack();
        return;
      }

      if (settings.ZapByChannelIndex)
      {
        // Channel index starts by 0, user enters 1 based numbers
        number--;
        _ = model.TuneByIndex(number);
      }
      else
      {
        _ = model.TuneByChannelNumber(number);
      }
    }

    #endregion
  }
}
