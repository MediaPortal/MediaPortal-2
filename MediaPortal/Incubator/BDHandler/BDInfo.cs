using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using BDInfo;

namespace MediaPortal.UI.Players.Video
{
  public class BDInfo : BDROM
  {

    public string Title = string.Empty;

    public BDInfo(string path)
      : base(path)
    {

    }

    public new void Scan()
    {

      // perform the BDInfo scan
      base.Scan();

      // get the bd title from the meta xml
      string metaFilePath = Path.Combine(DirectoryBDMV.FullName, @"META\DL\bdmt_eng.xml");
      if (!File.Exists(metaFilePath))
        return;

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
          {
            string title = node.ToString().Trim();
            if (title != string.Empty)
            {
              Title = title;
              BDHandlerCore.LogDebug("Bluray Metafile='{0}', Title= '{1}'", metaFilePath, title);
            }
            else
            {
              BDHandlerCore.LogDebug("Bluray Metafile='{0}': No Title Found.", metaFilePath);
            }
          }
        }
      }
      catch (Exception e)
      {
        BDHandlerCore.LogError("Meta File Error: ", e);
      }

    }

  }
}