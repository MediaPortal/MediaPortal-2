#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Matroska
{
  /// <summary>
  /// <see cref="MatroskaInfoReader"/> uses the mkvtoolnix utilities to extract metadata and attachments from matroska files.
  /// </summary>
  public class MatroskaInfoReader
  {
    #region Fields

    private Process _process;
    private readonly string _fileName;
    private readonly string _workingDirectory;
    private readonly List<MatroskaAttachment> _attachments;

    #endregion

    #region Helper classes

    public class MatroskaAttachment
    {
      public string FileName;
      public string MimeType;
      public int FileSize;
      public override string ToString()
      {
        return string.Format("{0} [{1}, {2}]", FileName, MimeType, FileSize);
      }
    }

    #endregion

    #region Properties

    public List<MatroskaAttachment> Attachments
    {
      get { return _attachments; }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a new <see cref="MatroskaInfoReader"/>.
    /// </summary>
    /// <param name="fileName">MKV file to extract information from</param>
    public MatroskaInfoReader(string fileName)
    {
      _fileName = fileName;
      _workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      _attachments = new List<MatroskaAttachment>();
    }

    #endregion

    #region Public Methods

    public void ReadTags(Dictionary<string, IList<string>> tagsToExtract)
    {
      String output;
      if (TryExecuteReadString(@"mkvextract.exe", string.Format("tags \"{0}\"", _fileName), out output) && !string.IsNullOrEmpty(output))
      {
        XDocument doc = XDocument.Parse(output);

        foreach (string key in new List<string>(tagsToExtract.Keys))
        {
          string[] parts = key.Split('.');
          int? targetType = null;
          string tagName;
          if (parts.Length == 2)
          {
            targetType = int.Parse(parts[0]);
            tagName = parts[1];
          }
          else
            tagName = parts[0];

          var result = from simpleTag in GetTagsForTargetType(doc, targetType).Elements("Simple")
                       where simpleTag.Element("Name").Value == tagName
                       select simpleTag.Element("String").Value;
          if (result.Any())
            tagsToExtract[key] = result.ToList();
        }
      }
    }

    #endregion

    #region Private methods

    private bool TryExecuteReadString(string executable, string arguments, out string result)
    {
      using (_process = new Process { StartInfo = new ProcessStartInfo(Path.Combine(_workingDirectory, executable), arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true } })
      {
        _process.Start();
        using (_process.StandardOutput)
        {
          result = _process.StandardOutput.ReadToEnd();
          if (_process.WaitForExit(1000))
            return _process.ExitCode == 0;
        }
        if (!_process.HasExited)
          _process.Close();
      }
      return false;
    }

    public void ReadAttachments()
    {
      String output;
      // Structure of mkvinfo attachment output
      // |+ Attachments
      // | + Attached
      // |  + File name: cover.jpg
      // |  + Mime type: image/jpeg
      // |  + File data, size: 132908
      // |  + File UID: 1495003044
      if (TryExecuteReadString(@"mkvinfo.exe", string.Format("--ui-language en --output-charset UTF-8 \"{0}\"", _fileName), out output))
      {
        StringReader reader = new StringReader(output);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          // Start of an attachment section
          if (line.Contains("Attached"))
            _attachments.Add(new MatroskaAttachment());

          if (line.Contains("File name"))
            _attachments[_attachments.Count - 1].FileName = line.Substring(line.LastIndexOf(": ") + 2);

          if (line.Contains("Mime type"))
            _attachments[_attachments.Count - 1].MimeType = line.Substring(line.LastIndexOf(": ") + 2);

          if (line.Contains("File data, size"))
            _attachments[_attachments.Count - 1].FileSize = Int32.Parse(line.Substring(line.LastIndexOf(": ") + 2));
        }
      }
    }

    static IEnumerable<XElement> GetTagsForTargetType(XDocument doc, int? targetTypeValue)
    {
      if (targetTypeValue.HasValue)
        return from simpleTag in doc.Descendants("Tags").Descendants("Tag")
               where simpleTag.Element("Targets").HasElements && Convert.ToInt32(simpleTag.Element("Targets").Element("TargetTypeValue").Value) == targetTypeValue.Value
               select simpleTag;

      return from simpleTag in doc.Descendants("Tags").Descendants("Tag")
             where !simpleTag.Element("Targets").HasElements
             select simpleTag;
    }


    #endregion
  }
}
