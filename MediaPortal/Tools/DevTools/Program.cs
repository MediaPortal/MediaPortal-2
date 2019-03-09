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

using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Database.SQLite;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider;
using MediaPortal.Utilities.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.DevTools
{
  class Program
  {
    private static ILogger _logger = null;

    private static void Exit(int code)
    {
      Console.Write("Press return to exit");
      Console.ReadLine();
      Environment.Exit(code);
    }

    private static void Usage()
    {
      Console.Error.WriteLine("Usage: DevTools [Args] <command> [options]");
      Console.Error.WriteLine("       --direct sources [list|export] <datastore file>");
      Console.Error.WriteLine("       --direct sql <datastore file>");
      Console.Error.WriteLine("       sources [list|export]");
      Console.Error.WriteLine("       sources add <name> <LOCAL|NETWORK> <path> <categories (comma separated)>");
      Console.Error.WriteLine("       sources refresh <ID ID ...>");
      Console.Error.WriteLine("       sources reload <ID ID ...>");
      Console.Error.WriteLine("       items list <ID ID ...>");
      Console.Error.WriteLine("       items search <text>");
      Console.Error.WriteLine("       items relationships <ID> <role> <linked role>");

      Exit(1);
    }

    static void Main(string[] args)
    {
      try
      {
        IList<string> argList = new List<string>(args);

        bool direct = false;
        if (argList.Count >= 1 && argList[0] == "--direct")
        {
          argList.RemoveAt(0);
          direct = true;
        }

        if (argList.Count == 0)
        {
          Usage();
        }

        var pathManager = new PathManager();
        pathManager.InitializeDefaults();
        // Override data path which could be read from MP2-Server install folder
        pathManager.SetPath("DATA", "<COMMON_APPLICATION_DATA>\\Team MediaPortal\\MP2-DevTools");

        // Set defaults if running from VS debug console (not inside MP2-Server install folder)
        if (!pathManager.Exists("CONFIG"))
        {
          pathManager.SetPath("CONFIG", "<DATA>\\Config");
          pathManager.SetPath("LOG", "<DATA>\\Log");
        }
        ServiceRegistration.Set<IPathManager>(pathManager);
        ServiceRegistration.Set(_logger = new Log4NetLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>")));

        if (direct)
        {
          if (argList.Count == 3 && argList[0] == "sources" && (argList[1] == "list" || argList[1] == "export"))
          {
            string file = argList[2];
            var database = GetSqlDatabase(file);
            if (argList[1] == "list")
              ListMediaSources(GetMediaSources(database));
            else if (argList[1] == "export")
              ExportMediaSources(GetMediaSources(database));
          }
          else if (argList.Count == 2 && argList[0] == "sql")
          {
            string file = argList[1];
            SQLiteDatabase database = GetSqlDatabase(file);
            ShowSqlConsole(database);
          }
          else
          {
            Usage();
          }
        }
        else
        {
          try
          {
            _logger.Info("Creating client...");
            Client client = new Client();
            client.Connect();

            _logger.Info("Client connected {0}", client.Connected);

            if (client.Connected)
            {
              _logger.Info("Checking arg list [{0}]", string.Join(",", argList));
              if (argList.Count == 2 && argList[0] == "sources" && (argList[1] == "list" || argList[1] == "export"))
              {
                _logger.Info("Media sources:");
                if (argList[1] == "list")
                  ListMediaSources(GetMediaSources(client));
                else if (argList[1] == "export")
                  ExportMediaSources(GetMediaSources(client));
              }

              else if (argList.Count == 6 && argList[0] == "sources" && argList[1] == "add")
              {
                _logger.Info("Media sources before add:");
                ListMediaSources(GetMediaSources(client));

                string name = argList[2];
                string type = argList[3];
                string path = argList[4];
                string[] categories = argList[5].Split(new char[] { ',' });

                ProviderPathSegment segment = null;
                if (type == "LOCAL")
                {
                  path = path.Replace('\\', '/');
                  segment = new ProviderPathSegment(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, "/" + path, true);
                }
                else if (type == "NETWORK")
                  segment = new ProviderPathSegment(NetworkNeighborhoodResourceProvider.NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, "/" + path, true);
                else
                {
                  Console.Error.WriteLine("Invalid media source type {0}", type);
                  Exit(1);
                }

                ResourcePath resourcePath = new ResourcePath(new ProviderPathSegment[] { segment });
                Share source = new Share(Guid.NewGuid(), client.GetSystemId(), resourcePath, name, true, categories);

                _logger.Info("Adding LOCAL media source name={0} path={1} categories=[{2}]", source.BaseResourcePath.BasePathSegment.Path, source.Name, string.Join(",", source.MediaCategories));
                client.GetContentDirectory().RegisterShareAsync(source).Wait();

                _logger.Info("Media sources after add:");
                ListMediaSources(GetMediaSources(client));
              }

              else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "delete")
              {
                IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));

                _logger.Info("Media sources before delete:");
                ListMediaSources(GetMediaSources(client));

                foreach (Guid id in ids)
                {
                  client.GetContentDirectory().RemoveShareAsync(id).Wait();
                }

                _logger.Info("Media sources after delete:");
                ListMediaSources(GetMediaSources(client));
              }

              else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "refresh")
              {
                IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));
                _logger.Info("Refreshing sources [{0}]", string.Join(",", ids));
                foreach (Guid id in ids)
                {
                  _logger.Info("Refreshing source {0}", id);
                  client.GetContentDirectory().ReImportShareAsync(id).Wait();
                }
              }

              else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "reload")
              {
                IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));

                foreach (Guid id in ids)
                {
                  Share preSource = client.GetContentDirectory().GetShareAsync(id).Result;
                  if (preSource == null)
                  {
                    _logger.Error("No media source {0} found", id);
                    continue;
                  }
                  ShowMediaSource(preSource);

                  _logger.Info("Removing old source");
                  client.GetContentDirectory().RemoveShareAsync(preSource.ShareId).Wait();

                  Share postSource = new Share(Guid.NewGuid(), preSource.SystemId, preSource.BaseResourcePath, preSource.Name, true, preSource.MediaCategories);

                  _logger.Info("Adding media source name={0} path={1} categories=[{2}]", postSource.Name, postSource.BaseResourcePath.BasePathSegment.Path, string.Join(",", postSource.MediaCategories));
                  client.GetContentDirectory().RegisterShareAsync(postSource).Wait();

                  _logger.Info("Media source after reload:");
                  ShowMediaSource(client.GetContentDirectory().GetShareAsync(postSource.ShareId).Result);
                }
              }

              else if (argList.Count == 3 && argList[0] == "items" && argList[1] == "list")
              {
                IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));
                ShowMediaItems(client, new MediaItemIdFilter(ids));
              }

              else if (argList.Count == 3 && argList[0] == "items" && argList[1] == "search")
              {
                string text = argList[2];
                ShowMediaItems(client, new LikeFilter(MediaAspect.ATTR_TITLE, "%" + text + "%", null));
              }

              else if (argList.Count == 5 && argList[0] == "items" && argList[1] == "relationships")
              {
                string id = argList[2];
                string role = argList[3];
                string linkedRole = argList[4];
                ShowMediaItems(client, new RelationshipFilter(new Guid(role), new Guid(linkedRole), new Guid(id)));
              }

              else
              {
                Usage();
              }
            }
            else
            {
              _logger.Error("No server found");
              Exit(1);
            }
          }
          catch (Exception e)
          {
            _logger.Error("Oh dear", e);
            Exit(1);
          }
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Error performing operation\n{0}", e);
        Exit(1);
      }

      Exit(0);
    }

    private static void ShowSqlConsole(SQLiteDatabase database)
    {
      Console.WriteLine("Direct SQL interface to SQLite. Enter query. Leave with 'exit' command.");
      var instructionList = new InstructionList();
      foreach (var query in instructionList.ParseStream(Console.In))
      {
        if (query == "exit")
          return;
        // Note: we need the direct connection to DB, not a transaction. 
        // This is because some instructions can't be done inside transaction scope (like vacuum, reindex)
        using (var connection = database.CreateOpenAndInitializeConnection())
        {
          using (var cmd = connection.CreateCommand())
          {
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = query;

            try
            {
              using (var reader = cmd.ExecuteReader())
              {
                if (reader.HasRows)
                {
                  for (int c = 0; c < reader.FieldCount; c++)
                    Console.Write(reader.GetName(c).PadRight(10) + "|");
                  Console.WriteLine();
                }
                while (reader.Read())
                {
                  for (int c = 0; c < reader.FieldCount; c++)
                  {
                    object value = reader.GetValue(c);
                    string formatted = string.Format("{0}", value).PadRight(10).Substring(0, 10);
                    Console.Write(formatted + "|");
                  }
                  Console.WriteLine();
                }
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine("Error: {0}", ex);
            }
          }
        }
      }
    }

    private static SQLiteDatabase GetSqlDatabase(string file)
    {
      if (!File.Exists(file))
      {
        Console.Error.WriteLine("Datastore {0} does not exist", file);
        Exit(1);
      }

      var directoryName = Path.GetDirectoryName(file);
      ServiceRegistration.Get<IPathManager>().SetPath("DATABASE", directoryName);

      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SQLiteSettings>();
      settings.PageSize = 4096;
      settings.DatabaseFileName = Path.GetFileName(file);
      ServiceRegistration.Get<ISettingsManager>().Save(settings);

      return new SQLiteDatabase();
    }

    private static string GetValue(MediaItemAspectMetadata.AttributeSpecification spec, object value)
    {
      if (value == null)
        return null;

      if (spec.ParentMIAM.AspectId == RelationshipAspect.ASPECT_ID && (RelationshipAspect.ATTR_ROLE.Equals(spec) || RelationshipAspect.ATTR_LINKED_ROLE.Equals(spec)))
      {
        if (EpisodeAspect.ROLE_EPISODE.Equals(value))
          return "Episode";

        if (SeasonAspect.ROLE_SEASON.Equals(value))
          return "Season";

        if (SeriesAspect.ROLE_SERIES.Equals(value))
          return "Series";
      }

      return value.ToString();
    }

    private static void ShowMediaItems(Client client, IFilter filter)
    {
      IMediaItemAspectTypeRegistration registration = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      IList<MediaItem> items = client.GetContentDirectory().SearchAsync(new MediaItemQuery(null, registration.LocallyKnownMediaItemAspectTypes.Keys, filter), true, null, true).Result;
      foreach (MediaItem item in items)
      {
        Console.WriteLine("\nItem {0}:", item.MediaItemId);
        foreach (Guid mia in item.Aspects.Keys)
        {
          MediaItemAspectMetadata metadata = registration.LocallyKnownMediaItemAspectTypes[mia];
          foreach (MediaItemAspect aspect in item.Aspects[mia])
          {
            Console.WriteLine(" {0}:", metadata.Name);
            int count = 0;
            string sb = " ";
            foreach (MediaItemAspectMetadata.AttributeSpecification spec in aspect.Metadata.AttributeSpecifications.Values)
            {
              string valueStr = null;
              if (spec.IsCollectionAttribute)
              {
                IEnumerable values = aspect.GetCollectionAttribute(spec);
                if (values != null)
                {
                  IList<string> list = new List<string>();
                  foreach (object value in values)
                    list.Add(GetValue(spec, value));
                  valueStr = string.Format("[{0}]", string.Join(",", list));
                }
              }
              else
              {
                valueStr = GetValue(spec, aspect.GetAttributeValue(spec));
              }
              if (valueStr != null)
              {
                if (count > 0)
                  sb += ",";
                //sb += string.Format(" {0}{1}{2}({3}/{4})={5}", spec.AttributeName, aspect is MultipleMediaItemAspect ? "," : "", aspect is MultipleMediaItemAspect ? ((MultipleMediaItemAspect)aspect).Index.ToString() : "", spec.AttributeType.Name, spec.Cardinality, valueStr);
                sb += string.Format(" {0}={1}", spec.AttributeName, valueStr);
                count++;
              }
            }
            Console.WriteLine(sb);
          }
        }
      }
    }

    private static ICollection<Share> GetMediaSources(ISQLDatabase database)
    {
      IList<Share> sources = new List<Share>();

      ITransaction transaction = database.BeginTransaction();

      IDbCommand sourcesCommand = transaction.CreateCommand();
      sourcesCommand.CommandText = "select share_id,base_resource_path,name from shares s";
      IDataReader sourcesReader = sourcesCommand.ExecuteReader();
      while (sourcesReader.Read())
      {
        Guid sourceId = sourcesReader.GetGuid(sourcesReader.GetOrdinal("share_id"));
        ResourcePath resourcePath = ResourcePath.Deserialize(sourcesReader.GetString(sourcesReader.GetOrdinal("base_resource_path")));

        IList<string> categories = new List<string>();
        IDbCommand categoriesCommand = transaction.CreateCommand();
        categoriesCommand.CommandText = "select categoryname from shares_categories where share_id=@share_id";
        database.AddParameter(categoriesCommand, "share_id", sourceId, typeof(Guid));
        IDataReader categoriesReader = categoriesCommand.ExecuteReader();
        while (categoriesReader.Read())
        {
          categories.Add(categoriesReader.GetString(categoriesReader.GetOrdinal("categoryname")));
        }
        categoriesReader.Close();

        string name = sourcesReader.GetString(sourcesReader.GetOrdinal("name"));
        sources.Add(new Share(sourceId, null, resourcePath, name, true, categories));
      }
      sourcesReader.Close();

      return sources;
    }

    private static ICollection<Share> GetMediaSources(Client client)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      return client.GetContentDirectory().GetSharesAsync(serverConnectionManager.HomeServerSystemId, SharesFilter.All).Result;
    }

    private static void ListMediaSources(ICollection<Share> sources)
    {
      foreach (Share source in sources)
      {
        ShowMediaSource(source);
      }
    }

    private static void ShowMediaSource(Share source)
    {
      ResourcePath resourcePath = source.BaseResourcePath;
      string path = resourcePath.BasePathSegment.Path;
      string categories = string.Join(",", source.MediaCategories);

      string providerName = "<<UNKNOWN>>";
      if (resourcePath.BasePathSegment.ProviderId == LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID)
        providerName = "LOCAL";
      else if (resourcePath.BasePathSegment.ProviderId == NetworkNeighborhoodResourceProvider.NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID)
        providerName = "NETWORK";
      else if (resourcePath.BasePathSegment.ProviderId == RemoteResourceProvider.REMOTE_RESOURCE_PROVIDER_ID)
        providerName = "REMOTE";

      _logger.Info("Media source {0} : name={1}, provider={2} path={3}, categories=[{4}]", source.ShareId, source.Name, providerName, path, categories);
    }

    private static void ExportMediaSources(ICollection<Share> sources)
    {
      XmlSerializer serialiser = new XmlSerializer(typeof(Share));
      using (StringWriter sww = new StringWriter())
      using (XmlWriter writer = XmlWriter.Create(sww))
      {
        writer.WriteStartElement("Sources"); // Wrapper around the albums
        foreach (Share source in sources)
        {
          ResourcePath resourcePath = source.BaseResourcePath;
          string path = resourcePath.BasePathSegment.Path;
          string categories = string.Join(",", source.MediaCategories);

          string providerName = "<<UNKNOWN>>";
          if (resourcePath.BasePathSegment.ProviderId == LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID)
            providerName = "LOCAL";
          else if (resourcePath.BasePathSegment.ProviderId == NetworkNeighborhoodResourceProvider.NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID)
            providerName = "NETWORK";
          else if (resourcePath.BasePathSegment.ProviderId == RemoteResourceProvider.REMOTE_RESOURCE_PROVIDER_ID)
            providerName = "REMOTE";

          Console.WriteLine("Serialising {0} : name={1}, provider={2} path={3}, categories=[{4}]", source.ShareId, source.Name, providerName, path, categories);
          source.Serialize(writer);
        }
        writer.WriteEndElement();
        var xml = sww.ToString(); // Your XML
        Console.WriteLine("XML:\n{0}", xml);
      }
    }
  }
}
