using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.Info
{
  public class CoreInfo
  {
    protected const string DISPLAY_NAME = "display_name";
    protected const string SUPPORTED_EXTENSIONS = "supported_extensions";
    protected const string CORENAME = "corename";
    protected const string MANUFACTURER = "manufacturer";
    protected const string CATEGORIES = "categories";
    protected const string SYSTEMNAME = "systemname";
    protected const string DATABASE = "database";
    protected const string LICENCE = "license";
    protected const string PERMISSIONS = "permissions";
    protected const string DISPLAY_VERSION = "display_version";
    protected const string SUPPORTS_NO_GAME = "supports_no_game";
    protected const string FIRMWARECOUNT = "firmware_count";
    protected const string FIRMWARE_DESC = "firmware{0}_desc";
    protected const string FIRMWARE_PATH = "firmware{0}_path";
    protected const string FIRMWARE_OPT = "firmware{0}_opt";
    protected const string NOTES = "notes";

    protected static readonly char[] TRIM_CARS = new[] { ' ', '"' };

    protected Dictionary<string, string> _coreInfos;
    protected List<Firmware> _firmwares;
    protected string _coreName;
    protected string _displayName;
    protected string _supportedExtensions;
    protected string _manufacturer;
    protected string _categories;
    protected string _systemName;
    protected string _database;
    protected string _licence;
    protected string _permissions;
    protected string _displayVersion;
    protected string _supportsNoGame;
    protected string _notes;

    public CoreInfo(string coreName, string infoText)
    {
      _coreInfos = new Dictionary<string, string>();
      _coreName = coreName;
      Parse(infoText);
      InitProperties();
      InitFirmare();
    }

    public string CoreName
    {
      get { return _coreName; }
    }

    public string DisplayName
    {
      get { return _displayName; }
    }

    public string SupportedExtensions
    {
      get { return _supportedExtensions; }
    }

    public string Manufacturer
    {
      get { return _manufacturer; }
    }

    public string Categories
    {
      get { return _categories; }
    }

    public string SystemName
    {
      get { return _systemName; }
    }

    public string Database
    {
      get { return _database; }
    }

    public string Licence
    {
      get { return _licence; }
    }

    public string Permissions
    {
      get { return _permissions; }
    }

    public string DisplayVersion
    {
      get { return _displayVersion; }
    }

    public string SupportsNoGame
    {
      get { return _supportsNoGame; }
    }

    public string Notes
    {
      get { return _notes; }
    }

    public List<Firmware> Firmware
    {
      get { return _firmwares; }
    }

    protected void Parse(string infoText)
    {
      if (string.IsNullOrEmpty(infoText))
        return;

      using (StringReader sr = new StringReader(infoText))
      {
        string line;
        while ((line = sr.ReadLine()) != null)
          ParseInfo(line);
      }
    }

    protected void ParseInfo(string infoLine)
    {
      string[] keyVal = infoLine.Split('=');
      string key = keyVal[0].Trim();
      string value = keyVal.Length > 1 ? CleanString(keyVal[1]) : string.Empty;
      _coreInfos[key] = value;
    }

    protected void InitProperties()
    {
      _coreInfos.TryGetValue(DISPLAY_NAME, out _displayName);
      _coreInfos.TryGetValue(SUPPORTED_EXTENSIONS, out _supportedExtensions);
      _coreInfos.TryGetValue(MANUFACTURER, out _manufacturer);
      _coreInfos.TryGetValue(CATEGORIES, out _categories);
      _coreInfos.TryGetValue(SYSTEMNAME, out _systemName);
      _coreInfos.TryGetValue(DATABASE, out _database);
      _coreInfos.TryGetValue(LICENCE, out _licence);
      _coreInfos.TryGetValue(PERMISSIONS, out _permissions);
      _coreInfos.TryGetValue(DISPLAY_VERSION, out _displayVersion);
      _coreInfos.TryGetValue(SUPPORTS_NO_GAME, out _supportsNoGame);
      _coreInfos.TryGetValue(NOTES, out _notes);
    }

    protected void InitFirmare()
    {
      _firmwares = new List<Firmware>();

      string stringCount;
      int count;
      if (!_coreInfos.TryGetValue(FIRMWARECOUNT, out stringCount) || !int.TryParse(stringCount, out count))
        return;

      for (int i = 0; i < count; i++)
      {
        Firmware firmware;
        if (TryParseFirmare(i, out firmware))
          _firmwares.Add(firmware);
      }
    }

    protected bool TryParseFirmare(int index, out Firmware firmware)
    {
      firmware = null;

      string path;
      if (!_coreInfos.TryGetValue(string.Format(FIRMWARE_PATH, index), out path))
        return false;

      string description;
      if (!_coreInfos.TryGetValue(string.Format(FIRMWARE_DESC, index), out description))
        description = null;

      string stringOptional;
      bool optional = false;
      if (_coreInfos.TryGetValue(string.Format(FIRMWARE_PATH, index), out stringOptional))
        bool.TryParse(stringOptional, out optional);

      firmware = new Firmware(path, description, optional);
      return true;
    }

    protected string CleanString(string dirty)
    {
      return dirty.Trim(TRIM_CARS);
    }
  }
}