#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
#else
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Tracing;
using Owin;
using System.IO;
using System.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Http
{
  public static class HttpServer
  {
#if NET5_0_OR_GREATER
    public static IDisposable BuildAndStartServer(string servicePrefix, RequestDelegate handler, out string[] urls)
    {
      return BuildAndStartServer(servicePrefix, UPnPConfiguration.IP_ADDRESS_BINDINGS, UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER, handler, out urls);
    }

    public static IDisposable BuildAndStartServer(string servicePrefix, List<string> filters, int port, RequestDelegate handler, out string[] urls)
    {
      urls = BuildUrls(servicePrefix, filters, port);
      IDisposable server = null;
      try
      {
        var host = CreateWebHostBuilder(urls)
              .Configure(app => { app.Run(handler); })
              .Build();
        host.Start();
        server = host;
      }
      catch (Exception)
      {
        server?.Dispose();
        throw;
      }

      return server;
    }

    public static string[] BuildUrls(string servicePrefix, List<string> filters, int port)
    {
      ICollection<IPAddress> listenAddresses = new HashSet<IPAddress>();
      if (UPnPConfiguration.USE_IPV4)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters))
          listenAddresses.Add(address);
      if (UPnPConfiguration.USE_IPV6)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters))
          listenAddresses.Add(address);

      List<string> urls = new List<string>();
      foreach (IPAddress address in listenAddresses)
      {
        var bindableAddress = NetworkHelper.TranslateBindableAddress(address);
        string formattedAddress = $"http://{bindableAddress}:{port}{servicePrefix}";
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
          if (Equals(address, IPAddress.IPv6Any))
            continue;
          formattedAddress = $"http://[{bindableAddress}]:{port}{servicePrefix}";
        }
        urls.Add(formattedAddress);
      }

      // If no explicit url bindings defined, use the wildcard binding
      if (urls.Count == 0)
      {
        var formattedAddress = $"http://+:{port}{servicePrefix}";
        urls.Add(formattedAddress);
      }

      return urls.ToArray();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] urls)
    {
#pragma warning disable CA1416 // Validate platform compatibility
      return WebHost.CreateDefaultBuilder()
        // HttpSys allows port sharing so multiple instances of the web host
        // can listen on the same port under different base paths
        .UseHttpSys(options =>
        {
          // Synchronous IO, which allows synchronous read/writes to the request/response streams,
          // is disabled by default, currently the XmlSerializer in GENAClientController.HandleEventNotification
          // does synchronous reads of the request stream so we need this enabled.
          // ToDo: Make all read/writes asynchronous, see here for how the MVC formatter does this for Xml deserialization
          // https://github.com/dotnet/aspnetcore/blob/093df67c06297c20edb422fe6d3a555008e152a9/src/Mvc/Mvc.Formatters.Xml/src/XmlSerializerInputFormatter.cs#L102-L117
          options.AllowSynchronousIO = true;
        })
        .UseUrls(urls);
#pragma warning restore CA1416 // Validate platform compatibility
    }

#else
    public static IDisposable BuildAndStartServer(string servicePrefix, Func<IOwinContext, Task> handler, out IList<string> urls)
    {
      return BuildAndStartServer(servicePrefix, UPnPConfiguration.IP_ADDRESS_BINDINGS, UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER, handler, out urls);
    }

    public static IDisposable BuildAndStartServer(string servicePrefix, List<string> filters, int port, Func<IOwinContext, Task> handler, out IList<string> urls)
    {
      var startOptions = BuildStartOptions(servicePrefix, filters, port);
      urls = startOptions.Urls;
      IDisposable server = null;
      try
      {
        server = WebApp.Start(startOptions, builder => { builder.Use((context, func) => handler(context)); });
      }
      catch (Exception)
      {
        server?.Dispose();
        throw;
      }
      return server;
    }

    public static StartOptions BuildStartOptions(string servicePrefix)
    {
      return BuildStartOptions(servicePrefix, UPnPConfiguration.IP_ADDRESS_BINDINGS, UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER);
    }

    public static StartOptions BuildStartOptions(string servicePrefix, List<string> filters, int port)
    {
      ICollection<IPAddress> listenAddresses = new HashSet<IPAddress>();
      if (UPnPConfiguration.USE_IPV4)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters))
          listenAddresses.Add(address);
      if (UPnPConfiguration.USE_IPV6)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters))
          listenAddresses.Add(address);

      StartOptions startOptions = new StartOptions();
      foreach (IPAddress address in listenAddresses)
      {
        var bindableAddress = NetworkHelper.TranslateBindableAddress(address);
        string formattedAddress = $"http://{bindableAddress}:{port}{servicePrefix}";
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
          if (Equals(address, IPAddress.IPv6Any))
            continue;
          formattedAddress = $"http://[{bindableAddress}]:{port}{servicePrefix}";
        }
        startOptions.Urls.Add(formattedAddress);
      }

      // If no explicit url bindings defined, use the wildcard binding
      if (startOptions.Urls.Count == 0)
      {
        var formattedAddress = $"http://+:{port}{servicePrefix}";
        startOptions.Urls.Add(formattedAddress);
      }

      // Disable built-in owin tracing by using a null traceoutput. It causes crashes by concurrency issues.
      // See: https://stackoverflow.com/questions/17948363/tracelistener-in-owin-self-hosting
      startOptions.Settings.Add(
        typeof(ITraceOutputFactory).FullName,
        typeof(NullTraceOutputFactory).AssemblyQualifiedName);
      return startOptions;
    }

    public class NullTraceOutputFactory : ITraceOutputFactory
    {
      public TextWriter Create(string outputFile)
      {
        // Beware that there's a multi threaded race condition using StreamWriter.Null, since it's also used by Console.Write* when no console is attached, e.g. from Windows Services.
        // It's better to use TextWriter.Synchronized(new StreamWriter(Stream.Null)) instead.
        return TextWriter.Synchronized(new StreamWriter(Stream.Null));
      }
    }
#endif
  }
}
