using System.Linq.Expressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Common.PathManager;
using System.IO;
using System.Xml;
using System;
using System.Reflection;
using System.Text.Encodings.Web;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Settings;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;
using MediaPortal.Plugins.AspNetServer;
using MediaPortal.Plugins.MP2Extended.Swagger;
using Swashbuckle.SwaggerGen.Generator;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended
{
  public class MP2ExtendedService : IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "MP2Extended";
    private const int PORT = 4322;
    private const string BASE_PATH = "/MPExtended";
    private static readonly Assembly ASS = Assembly.GetExecutingAssembly();
    internal static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(ASS.Location);

    #endregion

    #region Constructor
    public MP2ExtendedService()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStartWebApplicationAsync(
        webApplicationName: WEB_APPLICATION_NAME,
        configureServices: services =>
        {
          services.AddMvc(options =>
          {
            options.CacheProfiles.Add("nonCriticalApiCalls", new CacheProfile()
            {
              Duration = 100,
              Location = ResponseCacheLocation.Client
            });
          })
          // MVC Razor
          .AddRazorOptions(options => options.FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));

          // Swagger
          services.AddSwaggerGen(c =>
          {
            c.DescribeAllEnumsAsStrings();
            c.OperationFilter<HandleModelbinding>();
            c.OrderActionGroupsBy(new DescendingAlphabeticComparer());
            //c.IncludeXmlComments(Path.Combine(ASSEMBLY_PATH, ASS.GetName().Name+".xml"));
            c.SingleApiVersion(new Info
            {
              Title = "MP2Extended API",
              Description = "MP2Extended brings the well known MPExtended from MP1 to MP2",
              Contact = new Contact
              {
                Name = "FreakyJ"
              },
              Version = "v1"
            });
          });
        },
        configureApp: app =>
        {
          app.UseExceptionHandler(errorApp =>
          {
            // Normally you'd use MVC or similar to render a nice page.
            errorApp.Run(async context =>
            {
              context.Response.StatusCode = 500;
              context.Response.ContentType = "text/html";
              await context.Response.WriteAsync("<html><body>\r\n");
              await context.Response.WriteAsync("We're sorry, we encountered an un-expected issue with your application.<br>\r\n");

              var error = context.Features.Get<IExceptionHandlerFeature>();
              if (error != null)
              {
                // This error would not normally be exposed to the client
                await context.Response.WriteAsync("<br>Error: " + HtmlEncoder.Default.Encode(error.Error.Message) + "<br>\r\n");
              }
              await context.Response.WriteAsync("<br><a href=\"/\">Home</a><br>\r\n");
              await context.Response.WriteAsync("</body></html>\r\n");
              await context.Response.WriteAsync(new string(' ', 512)); // Padding for IE
            });
          });
          //app.UseMiddleware<ExceptionHandlerMiddleware>();
          app.UseSwaggerUi(swaggerUrl: BASE_PATH + "/swagger/v1/swagger.json");

          // File Provider
          string resourcePath = Path.Combine(ASSEMBLY_PATH, "www/swagger").TrimEnd(Path.DirectorySeparatorChar);
          app.UseFileServer(new FileServerOptions
          {
            FileProvider = new PhysicalFileProvider(resourcePath),
            RequestPath = new PathString("/swagger/ui"),
            EnableDirectoryBrowsing = true,
          });
          // MVC
          app.UseMvc();
          // Swagger
          app.UseSwaggerGen();
          // some standard output
          app.Run(context => context.Response.WriteAsync("Hello MP2Extended"));
        },
        port: PORT,
        basePath: BASE_PATH);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStopWebApplicationAsync(WEB_APPLICATION_NAME).Wait();
    }

    #endregion
  }

  public class MP2Extended : IPluginStateTracker
  {
    public static MP2ExtendedSettings Settings = new MP2ExtendedSettings();
    public static MP2ExtendedUsers Users = new MP2ExtendedUsers();
    public static OnlineVideosManager OnlineVideosManager;

    

    private void StartUp()
    {
      Logger.Debug("MP2Extended: Registering HTTP resource access module");
      //ServiceRegistration.Get<IResourceServer>().AddHttpModule(new MainRequestHandler());
      if (Settings.OnlineVideosEnabled)
        OnlineVideosManager = new OnlineVideosManager(); // must be loaded after the settings are loaded
    }

    private void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<MP2ExtendedSettings>();
      Users = settingsManager.Load<MP2ExtendedUsers>();

      ProfileManager.Profiles.Clear();
      ProfileManager.LoadProfiles(false);
      ProfileManager.LoadProfiles(true);
    }

    private void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(Settings);
      settingsManager.Save(Users);
    }

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadSettings();
      StartUp();
    }


    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
      LoadSettings();
    }

    public void Shutdown()
    {
      SaveSettings();
    }

    #endregion IPluginStateTracker


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
