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

using System;
using System.Threading;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
#if !DEBUG
using MediaPortal.Services.Logging;
#endif
using MediaPortal.Core.Services.Runtime;
using MediaPortal.Database;
using MediaPortal.Utilities.CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Runtime;
using MediaPortal.Core.Logging;

[assembly: CLSCompliant(true)]

namespace MediaPortal
{
  internal static class ApplicationLauncher
  {
    private static AsynchronousMessageQueue _messageQueue;
    private static ManualResetEvent _shutdownEvent;

    private static void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue("Main application method", new string[]
          {
            SystemMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private static void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        if ((SystemMessaging.MessageType) message.MessageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState state = (SystemState) message.MessageData[SystemMessaging.PARAM];
          if (state == SystemState.ShuttingDown)
            _shutdownEvent.Set();
        }
      }
    }

    /// <summary>
    /// The main entry point for the MP-II server application.
    /// </summary>
    private static void Main(params string[] args)
    {
      Thread.CurrentThread.Name = "Main Thread";

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      try
      {
        CommandLine.Parse(args, mpArgs);
      }
      catch (ArgumentException)
      {
        mpArgs.DisplayOptions();
        return;
      }

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP-II-Server\Log");
#endif

      using (new ServiceScope(true)) // Create the servicescope
      {
#if !DEBUG
        // In release mode, catch all Exceptions.
        // In Debug mode these will be left unhandled.
        try
        {
#endif

        _shutdownEvent = new ManualResetEvent(false);
        SystemStateService systemStateService = new SystemStateService();
        ServiceScope.Add<ISystemStateService>(systemStateService);
        systemStateService.SwitchSystemState(SystemState.Initializing, false);

        //Check whether the user wants to log method names in the logger
        //This adds an extra 10 to 40 milliseconds to the log call, depending on the length of the stack trace
        bool logMethods = mpArgs.IsOption(CommandLineOptions.Option.LogMethods);
        LogLevel level = LogLevel.All;
        if (mpArgs.IsOption(CommandLineOptions.Option.LogLevel))
          level = (LogLevel) mpArgs.GetOption(CommandLineOptions.Option.LogLevel);

        ApplicationCore.RegisterCoreServices(level, logMethods);
        SubscribeToMessages();
        ILogger logger = ServiceScope.Get<ILogger>();

        IPathManager pathManager = ServiceScope.Get<IPathManager>();

        // Check if user wants to override the default Application Data location.
        if (mpArgs.IsOption(CommandLineOptions.Option.Data))
          pathManager.SetPath("DATA", (string) mpArgs.GetOption(CommandLineOptions.Option.Data));

#if !DEBUG
        logPath = pathManager.GetPath("<LOG>");
#endif

        // TODO
        //ServiceScope.Add<IGlobalSharesManagement>(...);
        //ServiceScope.Add<IImporter>(...);

        BackendExtension.RegisterBackendServices();

        // Start the core
        logger.Debug("ApplicationLauncher: Starting core");

        try
        {
          IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
          IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
          pluginManager.Initialize();
          pluginManager.Startup(false);
          ApplicationCore.StartCoreServices();

          BackendExtension.StartupBackendServices();

          mediaAccessor.Initialize();

          systemStateService.SwitchSystemState(SystemState.Started, true);

          // Has to be done from somewhere outside:
          //systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);

          // Wait until someone shuts the system down
          _shutdownEvent.WaitOne();

          ServiceScope.IsShuttingDown = true; // Block ServiceScope from trying to load new services in shutdown phase

          mediaAccessor.Shutdown();

          pluginManager.Shutdown();

          BackendExtension.ShutdownBackendServices();
        }
        catch (Exception e)
        {
          logger.Critical("Error executing application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceScope.IsShuttingDown = true;
        }
        finally
        {
          BackendExtension.DisposeBackendServices();
          ApplicationCore.DisposeCoreServices();
        }

        systemStateService.SwitchSystemState(SystemState.Ending, false);
#if !DEBUG
        }
        catch (Exception ex)
        {
          ServerCrashLogger crash = new ServerCrashLogger(logPath);
          crash.CreateLog(ex);
          Application.Exit();
        }
#endif
      }
    }
  }
}
