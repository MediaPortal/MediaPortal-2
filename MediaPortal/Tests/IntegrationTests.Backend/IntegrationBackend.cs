using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using HttpServer.Authentication;
using HttpServer.HttpModules;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.SystemResolver.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Database.SQLite;
using MediaPortal.Mock;
using Test.Common;

namespace IntegrationTests.Backend
{
  internal class TestResourceServer : IResourceServer
  {

    public string AddressIPv4
    {
      get { return "localhost"; }
    }

    public int PortIPv4
    {
      get { return 0; }
    }

    public string AddressIPv6
    {
      get { return null; }
    }

    public int PortIPv6
    {
      get { return -1; }
    }

    public void Startup()
    {
      throw new NotImplementedException();
    }

    public void Shutdown()
    {
      throw new NotImplementedException();
    }

    public void RestartHttpServers()
    {
      throw new NotImplementedException();
    }

    public void AddAuthenticationModule(AuthenticationModule module)
    {
      throw new NotImplementedException();
    }

    public void RemoveHttpModule(HttpModule module)
    {
      throw new NotImplementedException();
    }

    public void AddHttpModule(HttpModule module)
    {
      throw new NotImplementedException();
    }

    public int GetPortForIP(IPAddress ipAddress)
    {
      return 0;
    }
  }

  public class IntegrationBackend
  {
    private static ILogger Logger => ServiceRegistration.Get<ILogger>();

    protected ISQLDatabase Database => ServiceRegistration.Get<ISQLDatabase>();

    public void Init(string configDir, string database)
    {
      Logger.Info("Initialising...");
      ServiceRegistration.Set<IPathManager>(new PathManager());

      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", configDir + "/Config");
      ServiceRegistration.Get<IPathManager>().SetPath("DATA", configDir + "/Data");

      // Clear the logs every time
      string logDir = configDir + "/Log";
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", logDir);
      if (Directory.Exists(logDir))
        Directory.Delete(logDir, true);

      ServiceRegistration.Get<IPathManager>().SetPath("APPLICATION_ROOT", configDir + "/Application");
      Directory.CreateDirectory(configDir + "/Application/Scripts");

      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());

      string path = Path.GetDirectoryName(database);
      if (path == "")
        path = Directory.GetCurrentDirectory();
      ServiceRegistration.Get<IPathManager>().SetPath("DATABASE", path);

      SQLiteSettings settings = new SQLiteSettings
      {
        PageSize = 4096,
        DatabaseFileName = Path.GetFileName(database),
        EnableTraceLogging = true
      };

      Logger.Info("Save settings...");
      ServiceRegistration.Get<ISettingsManager>().Save(settings);

      Logger.Info("Create DB connection...");
      ServiceRegistration.Set<ISQLDatabase>(new SQLiteDatabase());

      Logger.Info("Create DB manager...");
      ServiceRegistration.Set<IDatabaseManager>(new DatabaseManager());

      Logger.Info("Find systemId...");
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      SystemResolverSettings resolverSettings = settingsManager.Load<SystemResolverSettings>();

      using (IDataReader reader = TestQuery("SELECT DISTINCT SYSTEM_ID FROM SHARES", new Dictionary<string, object>()))
      {
        if (reader.Read())
        {
          resolverSettings.SystemId = Database.ReadDBValue<Guid>(reader, 0).ToString();
        }
      }
      settingsManager.Save(resolverSettings);

      Logger.Info("Create other services...");

      ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

      ServiceRegistration.Set<ILocalization>(new TestLocalization());

      ServiceRegistration.Set<IPluginManager>(new MockPluginManager());

      ServiceRegistration.Set<IMediaAccessor>(new MediaAccessor());

      ServiceRegistration.Set<IResourceServer>(new TestResourceServer());

      TestMediaItemAspectTypeRegistration miatr = new TestMediaItemAspectTypeRegistration();
      ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(miatr);

      miatr.RegisterLocallyKnownMediaItemAspectType(MediaAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(AudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(VideoAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(RelationshipAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(DirectoryAspect.Metadata);

      Logger.Info("Create media library...");
      MediaLibrary ml = new MediaLibrary();
      ServiceRegistration.Set<IMediaLibrary>(ml);
      Logger.Info("Starting media library...");
      ml.Startup();
    }

    public IDataReader TestQuery(string sql, IDictionary<string, object> parameters)
    {
      ITransaction transaction = Database.CreateTransaction();

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = sql;
      foreach (KeyValuePair<string, object> parameter in parameters)
      {
        Database.AddParameter(command, parameter.Key, parameter.Value, parameter.Value.GetType());
      }
      return command.ExecuteReader();
    }
  }
}
