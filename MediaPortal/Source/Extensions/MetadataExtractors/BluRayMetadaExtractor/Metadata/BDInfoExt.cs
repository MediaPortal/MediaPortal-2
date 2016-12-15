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
using System.Xml;
using System.Xml.XPath;
using BDInfo;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// BDInfoExt extends the BDInfo library with features to extract more information.
  /// </summary>
  public class BDInfoExt : BDROM
  {
    
    #region Constructor

    public BDInfoExt(string path, bool autoScan)
      : base(path, autoScan)
    { }

    public BDInfoExt(string path)
      : this(path, true)
    { }

    #endregion

    #region Properties
       
    public string Title { get; set; }

    #endregion

    #region Overrides

    public new void Scan()
    {
      // perform the BDInfo scan
      base.Scan();

      // get the bd title from the meta xml
      Title = GetTitle();
      if (!String.IsNullOrEmpty(Title))
        ServiceRegistration.Get<ILogger>().Debug("BDMEx: Bluray Title= '{0}'", Title);
      else
        ServiceRegistration.Get<ILogger>().Debug("BDMEx: Bluray: No Title Found.");

    }

    /// <summary>
    /// Tries to extract the BluRay title.
    /// </summary>
    /// <returns></returns>
    public string GetTitle()
    {
      string metaFilePath = Path.Combine(DirectoryBDMV.FullName, @"META\DL\bdmt_eng.xml");
      if (!File.Exists(metaFilePath))
        return null;

      try
      {
        XPathDocument metaXML = new XPathDocument(metaFilePath);
        XPathNavigator navigator = metaXML.CreateNavigator();
        if (navigator.NameTable != null)
        {
          XmlNamespaceManager ns = new XmlNamespaceManager(navigator.NameTable);
          ns.AddNamespace("", "urn:BDA:bdmv;disclib");
          ns.AddNamespace("di", "urn:BDA:bdmv;discinfo");
          navigator.MoveToFirst();
          XPathNavigator node = navigator.SelectSingleNode("//di:discinfo/di:title/di:name", ns);
          if (node != null)
            return node.ToString().Trim();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("BDMEx: Meta File Error: ", e);
      }
      return null;
    }

    public FileInfo GetBiggestThumb()
    {
      string metaFolder = Path.Combine(DirectoryBDMV.FullName, @"META\DL\");
      DirectoryInfo directory = new DirectoryInfo(metaFolder);
      if (!directory.Exists)
        return null;

      FileInfo thumb = directory.GetFiles("*.jpg").OrderByDescending(f => f.Length).FirstOrDefault();
      return thumb;
    }

    #endregion
  }
}