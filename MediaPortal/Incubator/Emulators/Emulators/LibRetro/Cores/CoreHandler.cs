using Emulators.Common.GoodMerge;
using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using SharpRetro.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  public class CoreHandler
  {
    protected string _baseUrl;
    protected string _latestUrl;
    protected string _infoUrl;
    protected string _customCoresUrl;
    protected string _coresDirectory;
    protected string _infoDirectory;
    protected List<CustomCore> _customCores;
    protected List<LocalCore> _cores;
    protected HashSet<string> _unsupportedCores;
    protected HtmlDownloader _downloader;

    public CoreHandler(string coresDirectory, string infoDirectory)
    {
      _downloader = new HtmlDownloader();
      _cores = new List<LocalCore>();

      _coresDirectory = coresDirectory;
      _infoDirectory = infoDirectory;

      CoreUpdaterSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<CoreUpdaterSettings>();
      _baseUrl = settings.BaseUrl;
      _latestUrl = settings.CoresUrl;
      _infoUrl = settings.CoreInfoUrl;
      _customCoresUrl = settings.CustomCoresUrl;
      _unsupportedCores = new HashSet<string>(CoreUpdaterSettings.DEFAULT_UNSUPPORTED);
    }

    public List<LocalCore> Cores
    {
      get { return _cores; }
    }

    public void Update()
    {
      UpdateCustomCores();
      UpdateCores();
    }

    public bool DownloadCore(LocalCore core)
    {
      if (string.IsNullOrEmpty(core.Url))
        return false;
      if (!TryCreateDirectory(_coresDirectory))
        return false;

      string path = Path.Combine(_coresDirectory, core.ArchiveName);
      return _downloader.DownloadFileAsync(core.Url, path, true).Result && ExtractCore(path);
    }

    public static CoreInfo LoadCoreInfo(string coreName, string infoDirectory)
    {
      try
      {
        string path = Path.Combine(infoDirectory, Path.GetFileNameWithoutExtension(coreName) + ".info");
        if (File.Exists(path))
          return new CoreInfo(coreName, File.ReadAllText(path));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception loading core info for '{0}'", ex, coreName);
      }
      return null;
    }

    protected void UpdateCores()
    {
      List<OnlineCore> onlineCores = new List<OnlineCore>();
      string url = _baseUrl + _latestUrl + ".index-extended";
      CoreList coreList = _downloader.Download<CoreList>(url);
      if (coreList != null)
        onlineCores.AddRange(coreList.CoreUrls.OrderBy(c => c.Name));


      if (!TryCreateDirectory(_infoDirectory))
        return;

      foreach (OnlineCore core in onlineCores)
      {
        string coreName = core.Name.EndsWith(".zip") ? Path.GetFileNameWithoutExtension(core.Name) : core.Name;
        coreName = Path.GetFileNameWithoutExtension(coreName) + ".info";
        string infoUrl = _baseUrl + _infoUrl + coreName;
        string path = Path.Combine(_infoDirectory, coreName);
        _downloader.DownloadFileAsync(infoUrl, path).Wait();
      }

      CreateLocalCores(onlineCores);
    }

    protected void UpdateCustomCores()
    {
      _customCores = CustomCoreHandler.GetCustomCores(_customCoresUrl);

      if (!TryCreateDirectory(_infoDirectory))
        return;

      foreach (CustomCore customCore in _customCores)
      {
        if (string.IsNullOrEmpty(customCore.InfoUrl))
          continue;
        string path = Path.Combine(_infoDirectory, Path.GetFileNameWithoutExtension(customCore.CoreName) + ".info");
        _downloader.DownloadFileAsync(customCore.InfoUrl, path).Wait();
      }
    }

    protected void CreateLocalCores(IEnumerable<OnlineCore> onlineCores)
    {
      _cores = new List<LocalCore>();
      foreach (CustomCore customCore in _customCores)
      {
        LocalCore core = new LocalCore()
        {
          Url = customCore.CoreUrl,
          CoreName = customCore.CoreName,
          ArchiveName = customCore.CoreName,
          Info = LoadCoreInfo(customCore.CoreName, _infoDirectory)
        };
        _cores.Add(core);
      }

      foreach (OnlineCore onlineCore in onlineCores)
      {
        Uri uri;
        if (!TryCreateAbsoluteUrl(_baseUrl, _latestUrl + onlineCore.Name, out uri))
          continue;

        string coreName = Path.GetFileNameWithoutExtension(onlineCore.Name);
        LocalCore core = new LocalCore()
        {
          Url = uri.AbsoluteUri,
          ArchiveName = onlineCore.Name,
          CoreName = coreName,
          Supported = !_unsupportedCores.Contains(coreName),
          Info = LoadCoreInfo(coreName, _infoDirectory)
        };
        _cores.Add(core);
      }
    }

    protected bool ExtractCore(string path)
    {
      bool extracted;
      using (IExtractor extractor = ExtractorFactory.Create(path))
      {
        if (!extractor.IsArchive())
          return true;
        extracted = extractor.ExtractAll(Path.GetDirectoryName(path));
      }
      if (extracted)
        TryDeleteFile(path);
      return extracted;
    }

    protected static bool TryCreateAbsoluteUrl(string baseUrl, string url, out Uri uri)
    {
      if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
        return false;
      if (uri.IsAbsoluteUri)
        return true;
      Uri baseUri;
      if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri))
        return false;

      uri = new Uri(baseUri, uri);
      return true;
    }

    protected static bool TryCreateDirectory(string directory)
    {
      try
      {
        Directory.CreateDirectory(directory);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreHandler: Exception creating directory '{0}'", ex, directory);
      }
      return false;
    }

    protected static bool TryDeleteFile(string path)
    {
      try
      {
        File.Delete(path);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreHandler: Exception deleting file '{0}'", ex, path);
      }
      return false;
    }
  }
}
