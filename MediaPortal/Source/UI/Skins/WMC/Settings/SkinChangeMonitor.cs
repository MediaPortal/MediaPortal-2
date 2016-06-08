using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings
{
  public class SkinChangeMonitor
  {
    public const string SKIN_NAME = "WMCSkin";

    public static readonly SkinChangeMonitor Instance = new SkinChangeMonitor();

    protected AsynchronousMessageQueue _messageQueue;
    protected List<ConfigBase> _configs;
    protected bool _skinLoaded;

    public SkinChangeMonitor()
    {
      _configs = new List<ConfigBase>();
      SubscribeToMessages();
      UpdateSkinLoaded();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           SkinResourcesMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SkinResourcesMessaging.CHANNEL)
      {
        SkinResourcesMessaging.MessageType messageType = (SkinResourcesMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SkinResourcesMessaging.MessageType.SkinOrThemeChanged:
            UpdateSkinLoaded();
            UpdateConfigItems();
            break;
        }
      }
    }

    public void RegisterConfiguration(ConfigBase config)
    {
      config.Visible = _skinLoaded;
      _configs.Add(config);
    }

    public void UnregisterConfiguration(ConfigBase config)
    {
      _configs.Remove(config);
    }

    protected void UpdateConfigItems()
    {
      foreach (var config in _configs)
        config.Visible = _skinLoaded;
    }

    protected void UpdateSkinLoaded()
    {
      _skinLoaded = false;
      var srb = ServiceRegistration.Get<IScreenManager>().CurrentSkinResourceBundle;
      while (srb != null)
      {
        if (srb.Name == SKIN_NAME)
        {
          _skinLoaded = true;
          break;
        }
        srb = srb.InheritedSkinResources;
      }
    }
  }
}
