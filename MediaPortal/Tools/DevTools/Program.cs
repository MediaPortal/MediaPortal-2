#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.DevTools
{
  class Program
  {
    private static ILogger _logger = null;

    private static void Usage()
    {
      Console.Error.WriteLine("Usage: DevTools [Args] <command> [options]");
      Console.Error.WriteLine("       [--direct] sources list");
      Console.Error.WriteLine("       sources add <name> <LOCAL|NETWORK> <path> <categories (comma separated)>");
      Console.Error.WriteLine("       sources refresh <ID ID ...>");
      Console.Error.WriteLine("       sources reload <ID ID ...>");
      Console.Error.WriteLine("       items list <ID ID ...>");
      Console.Error.WriteLine("       items search <text>");
      Environment.Exit(1);
    }

    static void Main(string[] args)
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

      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_DevTools/config");
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_DevTools/log");
      ServiceRegistration.Set(_logger = new ConsoleLogger(LogLevel.All, true));

      if (direct)
      {
        if (argList.Count == 2 && argList[0] == "sources" && argList[1] == "list")
        {
          ServiceRegistration.Get<IPathManager>().SetPath("DATABASE", "C:\\ProgramData\\Team MediaPortal\\MP2-Server\\Database");

          ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
          ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

          SQLiteSettings settings = new SQLiteSettings();
          settings.PageSize = 4096;
          settings.DatabaseFileName = "Datastore.s3db";
          ServiceRegistration.Get<ISettingsManager>().Save(settings);

          ISQLDatabase database = new SQLiteDatabase();
          ShowMediaSources(database);
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
            if (argList.Count == 2 && argList[0] == "sources" && argList[1] == "list")
            {
              _logger.Info("Media sources:");
              ShowMediaSources(client);
            }

            else if (argList.Count == 6 && argList[0] == "sources" && argList[1] == "add")
            {
              _logger.Info("Media sources before add:");
              ShowMediaSources(client);

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
                Environment.Exit(1);
              }

              ResourcePath resourcePath = new ResourcePath(new ProviderPathSegment[] { segment });
              Share source = new Share(Guid.NewGuid(), client.GetSystemId(), resourcePath, name, categories);

              _logger.Info("Adding LOCAL media source name={0} path={1} categories=[{2}]", source.BaseResourcePath.BasePathSegment.Path, source.Name, string.Join(",", source.MediaCategories));
              client.GetContentDirectory().RegisterShare(source);

              _logger.Info("Media sources after add:");
              ShowMediaSources(client);
            }

            else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "delete")
            {
              IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));

              _logger.Info("Media sources before delete:");
              ShowMediaSources(client);

              foreach (Guid id in ids)
              {
                client.GetContentDirectory().RemoveShare(id);
              }

              _logger.Info("Media sources after delete:");
              ShowMediaSources(client);
            }

            else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "refresh")
            {
              IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));
              _logger.Info("Refreshing sources [{0}]", string.Join(",", ids));
              foreach (Guid id in ids)
              {
                _logger.Info("Refreshing source {0}", id);
                client.GetContentDirectory().ReImportShare(id);
              }
            }

            else if (argList.Count == 3 && argList[0] == "sources" && argList[1] == "reload")
            {
              IList<Guid> ids = argList[2].Split(new char[] { ' ' }).ToList().ConvertAll(x => new Guid(x));

              foreach (Guid id in ids)
              {
                Share preSource = client.GetContentDirectory().GetShare(id);
                if (preSource == null)
                {
                  _logger.Error("No media source {0} found", id);
                  continue;
                }
                ShowMediaSource(preSource);

                _logger.Info("Removing old source");
                client.GetContentDirectory().RemoveShare(preSource.ShareId);

                Share postSource = new Share(Guid.NewGuid(), preSource.SystemId, preSource.BaseResourcePath, preSource.Name, preSource.MediaCategories);

                _logger.Info("Adding media source name={0} path={1} categories=[{2}]", postSource.Name, postSource.BaseResourcePath.BasePathSegment.Path, string.Join(",", postSource.MediaCategories));
                client.GetContentDirectory().RegisterShare(postSource);

                _logger.Info("Media source after reload:");
                ShowMediaSource(client.GetContentDirectory().GetShare(postSource.ShareId));
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

            else
            {
              Usage();
            }
          }
          else
          {
            _logger.Error("No server found");
            Environment.Exit(1);
          }
        }
        catch (Exception e)
        {
          _logger.Error("Oh dear", e);
          Environment.Exit(1);
        }
      }

      Environment.Exit(0);
    }

    private static void ShowMediaItems(Client client, IFilter filter)
    {
      IMediaItemAspectTypeRegistration registration = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      IList<MediaItem> items = client.GetContentDirectory().Search(new MediaItemQuery(null, registration.LocallyKnownMediaItemAspectTypes.Keys, filter), true);
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
                    list.Add(value.ToString());
                  valueStr = string.Format("[{0}]", string.Join(",", list));
                }
              }
              else
              {
                object value = aspect.GetAttributeValue(spec);
                if (value != null)
                  valueStr = value.ToString();
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

    private static void ShowMediaSources(ISQLDatabase database)
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
        sources.Add(new Share(sourceId, null, resourcePath, name, categories));
      }
      sourcesReader.Close();

      ShowMediaSources(sources);
    }

    private static void ShowMediaSources(Client client)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      ShowMediaSources(new List<Share>(client.GetContentDirectory().GetShares(serverConnectionManager.HomeServerSystemId, SharesFilter.All)));
    }

    private static void ShowMediaSources(IList<Share> sources)
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
  }
}
