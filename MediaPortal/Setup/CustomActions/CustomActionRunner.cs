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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CustomActions
{
  public class CustomActionRunner
  {
    private readonly IRunnerHelper _runnerHelper;

    private const string LAV_SPLITTER_REGISTRY_PATH = @"CLSID\{171252A0-8820-4AFE-9DF8-5C92B2D66B04}\InprocServer32";
    private const string LAV_FILTERS_FILE_NAME = "LAVFilters.exe";
    private const string LAV_FILTERS_URL = "http://install.team-mediaportal.com/MP2/install/LAVFilters.exe";
    private const string LAV_FILTERS_METADATA_FILE = "http://install.team-mediaportal.com/MP2/install/metadata/LAVFilters.xml";
    private const string LAV_FILTERS_XSD_FILE = "CustomActions.LAVFilters.xsd";

    public CustomActionRunner(IRunnerHelper runnerHelper)
    {
      _runnerHelper = runnerHelper;
    }

    public bool IsLavFiltersAlreadyInstalled()
    {
      Version onlineVersion = GetOnlineVersion();

      string splitterPath = _runnerHelper.GetPathForRegistryKey(LAV_SPLITTER_REGISTRY_PATH);
      if (string.IsNullOrEmpty(splitterPath) && !_runnerHelper.Exists(splitterPath))
      {
        return false;
      }

      return IsEqualOrHigherVersion(splitterPath, onlineVersion);
    }

    public bool IsLavFiltersDownloaded()
    {
      string tempLAVFileName = Path.Combine(Path.GetTempPath(), LAV_FILTERS_FILE_NAME);
      _runnerHelper.DownloadFileAndReleaseResources(LAV_FILTERS_URL, tempLAVFileName);

      return _runnerHelper.Exists(tempLAVFileName);
    }

    public bool InstallLavFilters()
    {
      string arg = "/SILENT /SP-";
      int waitToComplete = 60000; // 1 minute
      string tempLAVFileName = Path.Combine(Path.GetTempPath(), LAV_FILTERS_FILE_NAME);

      return _runnerHelper.Start(tempLAVFileName, arg, waitToComplete);
    }

    private Version GetOnlineVersion()
    {
      Stream xsdFile = Assembly.GetExecutingAssembly().GetManifestResourceStream(LAV_FILTERS_XSD_FILE);
      XDocument metadataFile = _runnerHelper.LoadXmlDocument(LAV_FILTERS_METADATA_FILE);
      bool isMetaFileValid = IsValidXml(metadataFile, xsdFile);

      if (!isMetaFileValid)
      {
        throw new Exception("Metadata file not valid!");
      }

      int major = (int)metadataFile.Descendants("Major").First();
      int minor = (int)metadataFile.Descendants("Minor").First();
      int build = (int)metadataFile.Descendants("Build").First();
      int priv = (int)metadataFile.Descendants("Private").First();

      return new Version(major, minor, build, priv);
    }

    private bool IsValidXml(XDocument xmlFile, Stream xsdFile)
    {
      XmlSchemaSet schemas = new XmlSchemaSet();
      schemas.Add(null, XmlReader.Create(new StreamReader(xsdFile)));
      bool result = true;
      xmlFile.Validate(schemas, (sender, e) =>
      {
        result = false;
      });

      return result;
    }

    private bool IsEqualOrHigherVersion(string pathToFile, Version onlineVersion)
    {
      int majorPart = _runnerHelper.GetFileMajorVersion(pathToFile);
      int minorPart = _runnerHelper.GetFileMinorVersion(pathToFile);
      int buildPart = _runnerHelper.GetFileBuildVersion(pathToFile);
      int privatePart = _runnerHelper.GetFilePrivateVersion(pathToFile);
      Version localSplitterVersion = new Version(majorPart, minorPart, buildPart, privatePart);

      return localSplitterVersion >= onlineVersion;
    }
  }
}
