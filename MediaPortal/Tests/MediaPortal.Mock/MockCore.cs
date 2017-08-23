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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Registry;

namespace MediaPortal.Mock
{
  public class MockCore
  {
    private static MockMIA_Management MANAGEMENT;
    private static MockMediaLibrary LIBRARY;
    private static MockMessageBroker BROKER;

    private static ILogger logger = null;

    public static MockMIA_Management Management
    {
      get { return MANAGEMENT; }
    }

    public static MockMediaLibrary Library
    {
      get { return LIBRARY; }
    }

    static MockCore()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_Mock/log");
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_Mock/config");
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));

      logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("Registering IMessageBroker service");
      ServiceRegistration.Set<IMessageBroker>(BROKER = new MockMessageBroker());

      logger.Debug("Registering ISettingsManager service");
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      logger.Debug("Registering ISystemResolver service");
      ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

      logger.Debug("ApplicationCore: Registering IMediaAccessor service");
      ServiceRegistration.Set<IMediaAccessor>(new MediaAccessor());

      logger.Debug("ApplicationCore: Registering IPluginManager service");
      ServiceRegistration.Set<IPluginManager>(new Common.Services.PluginManager.PluginManager());

      logger.Debug("ApplicationCore: Registering IRegistry service");
      ServiceRegistration.Set<IRegistry>(new Common.Services.Registry.Registry());

      //logger.Debug("Registering IMediaItemAspectTypeRegistration service");
      //ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MockMediaItemAspectTypeRegistration());

      logger.Debug("Creating MIA management");
      MANAGEMENT = new MockMIA_Management();
    }

    public static void Reset()
    {
      MANAGEMENT.Reset();
    }

    public static void SetupLibrary(bool updateRelationships = false)
    {
      logger.Debug("Creating test media library");
      LIBRARY = new MockMediaLibrary { UpdateRelationshipsEnabled = updateRelationships };
    }

    public static void ShutdownLibrary()
    {
      LIBRARY.Dispose();
      LIBRARY = null;
    }

    public static void AddMediaItemAspectStorage(MediaItemAspectMetadata meta)
    {
      MANAGEMENT.AddMediaItemAspectStorage(meta);
    }

    public static void ShowMediaAspects(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, MediaItemAspectMetadata> metadatas)
    {
      foreach (Guid mia in aspects.Keys)
      {
        MediaItemAspectMetadata metadata = metadatas[mia];
        foreach (MediaItemAspect aspect in aspects[mia])
        {
          logger.Debug(" {0}:", metadata.Name);
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
          logger.Debug(sb);
        }
      }
    }
  }
}
