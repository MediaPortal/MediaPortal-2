#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

// Class for running MP in separate Application domain
// Currently in-active. Not decided if will be used again
// 2008-03-24 James
//using System;
//using System.Diagnostics;
//using MediaPortal;
//using MediaPortal.Core;
//using MediaPortal.Core.Threading;
//using MediaPortal.Core.Localisation;
//using MediaPortal.Core.Logging;
//using MediaPortal.Core.PluginManager;
//using MediaPortal.Core.Settings;
//using MediaPortal.Core.DeviceManager;
//using MediaPortal.Core.Messaging;
//using MediaPortal.Core.MPIManager;
//using MediaPortal.Presentation.Players;

//using MediaPortal.Media.Importers;
//using MediaPortal.Media.MetaData;

//using MediaPortal.Services.Threading;
//using MediaPortal.Services.Localisation;
//using MediaPortal.Services.Logging;
//using MediaPortal.Services.PluginManager;
//using MediaPortal.Services.Settings;
//using MediaPortal.Services.Burning;
//using MediaPortal.Services.Messaging;
//using MediaPortal.Services.MPIManager;
//using MediaPortal.Services.MetaData;

////using MediaPortal.Utilities.CommandLine;

///// <summary>
///// FIXME: Please document the meaning of this class! Should it be deleted?
///// Mar-16-2008 Albert78
///// </summary>
//public class MPApplication : MarshalByRefObject
//{
//  public static bool Run(CommandLineOptions mpArgs)
//  {
//    using (new ServiceScope(true)) //This is the first servicescope
//    {
//      //Check whether the user wants to log method names in the logger
//      //This adds an extra 10 to 40 milliseconds to the log call, depending on the length of the stack trace
//      bool logMethods = mpArgs.IsOption(CommandLineOptions.Option.LogMethods);
//      LogLevel level = LogLevel.All;
//      if (mpArgs.IsOption(CommandLineOptions.Option.LogLevel))
//      {
//        level = (LogLevel)mpArgs.GetOption(CommandLineOptions.Option.LogLevel);
//      }
//      ILogger logger = new FileLogger(@"log\MediaPortal.log", level, logMethods);
//      ServiceScope.Add(logger);
//      logger.Info("MPApplication: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);
//      //Debug.Assert(AppDomain.CurrentDomain.FriendlyName == "MPApplication",
//      //             "Some code change has caused MP2 to load in the wrong AppDomain.  Crash recovery will fail now...");


//      //MPInstaller - for testing only 
//      logger.Debug("MPApplication: Executing MPInstaller");
//      MPInstaller Installer = new MPInstaller();
//      ServiceScope.Add<IMPInstaller>(Installer);
//      Installer.LoadQueue();
//      Installer.ExecuteQueue(false);


//      //register core service implementations
//      logger.Debug("MPApplication: Registering ThreadPool");
//      MediaPortal.Services.Threading.ThreadPool pool = new MediaPortal.Services.Threading.ThreadPool();
//      pool.ErrorLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Error);
//      pool.WarnLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Warn);
//      pool.InfoLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Info);
//      pool.DebugLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Debug);
//      ServiceScope.Add<MediaPortal.Core.Threading.IThreadPool>(pool);

//      logger.Debug("MPApplication: Registering Message Broker");
//      ServiceScope.Add<IMessageBroker>(new MessageBroker());

//      logger.Debug("MPApplication: Registering Plugin Manager");
//      ServiceScope.Add<IPluginManager>(new PluginManager());

//      logger.Debug("MPApplication: Registering Settings Manager");
//      ServiceScope.Add<ISettingsManager>(new SettingsManager());

//      logger.Debug("MPApplication: Registering Strings Manager");
//      ServiceScope.Add<ILocalisation>(new StringManager());

//      //meta data mapper services
//      ServiceScope.Add<IMetaDataFormatterCollection>(new MetaDataFormatterCollection());
//      ServiceScope.Add<IMetadataMappingProvider>(new MetadataMappingProvider());

//      BurnManager burnManager = new BurnManager();
//      logger.Debug("MPApplication: Registering BurnManager");
//      ServiceScope.Add<IBurnManager>(burnManager);
//      EventHelper.Init(); // only for quick test simulating a plugin (trying to stay clean before the preview)...

//      //logger.Debug("MPApplication: Registering ImporterManager");
//      //ServiceScope.Add<IImporterManager>(new ImporterManager());

//      // Start the core
//      logger.Debug("MPApplication: Starting core");
//      ApplicationCore core = new ApplicationCore();
//      core.Start();
//      return false;
//    }
//  }
//}
