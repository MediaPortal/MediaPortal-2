using Emulators.Common.Emulators;
using Emulators.Common.Games;
using Emulators.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emulators.Emulator
{
  public class EmulatorConfigurationManager : IEmulatorManager
  {
    protected AsynchronousMessageQueue _messageQueue = null;

    public EmulatorConfigurationManager()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    ~EmulatorConfigurationManager()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType type = (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (type)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            SyncWithServer(Load());
            break;
        }
      }
    }

    public bool TryGetConfiguration(string mimeType, string extension, out EmulatorConfiguration configuration)
    {
      var configurations = Load().Where(c=>c.Platforms.Any(p => GameCategory.CategoryNameToMimeType(p) == mimeType));
      configuration = configurations.FirstOrDefault(c => c.FileExtensions.Contains(extension));
      if (configuration == null)
        configuration = configurations.FirstOrDefault(c => c.FileExtensions.Count == 0);
      return configuration != null;
    }

    public List<EmulatorConfiguration> Load()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      CommonSettings settings = sm.Load<CommonSettings>();
      var configurations = new List<EmulatorConfiguration>(settings.ConfiguredEmulators);
      return configurations;
    }

    public void Save(List<EmulatorConfiguration> configurations)
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      CommonSettings settings = sm.Load<CommonSettings>();
      settings.ConfiguredEmulators = configurations;
      sm.Save(settings);
      SyncWithServer(configurations);
    }

    protected void SyncWithServer(List<EmulatorConfiguration> configurations)
    {
      try
      {
        IServerSettingsClient ssc = ServiceRegistration.Get<IServerSettingsClient>();
        CommonSettings serverSettings = ssc.Load<CommonSettings>();
        List<EmulatorConfiguration> serverConfigurations = serverSettings.ConfiguredEmulators;
        string localSystemId = ServiceRegistration.Get<ISystemResolver>().LocalSystemId;
        serverConfigurations.RemoveAll(c => c.LocalSystemId == localSystemId);
        serverConfigurations.AddRange(configurations);
        ssc.Save(serverSettings);
      }
      catch
      { }
    }

    public void AddOrUpdate(EmulatorConfiguration configuration)
    {
      List<EmulatorConfiguration> configurations = Load();
      configurations.RemoveAll(c => c.Id == configuration.Id);
      configurations.Add(configuration);
      Save(configurations);
    }

    public void Remove(EmulatorConfiguration configuration)
    {
      List<EmulatorConfiguration> configurations = Load();
      if (configurations.RemoveAll(c => c.Id == configuration.Id) > 0)
        Save(configurations);
    }
  }
}
