#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.IO;
using System.Linq;

namespace MediaPortal.Common.PluginManager.Packages.DataContracts.Authors
{
  public class PublishPackageModel
  {
    public string FileName { get; set; }
    public string Content { get; set; }
    public string PackageType { get; set; }
    public ICollection<string> CategoryTags { get; set; }

    public PublishPackageModel()
    {
    }

    public PublishPackageModel(string fileName, string content, string packageType, IEnumerable<string> categoryTags)
    {
      FileName = fileName;
      Content = content;
      PackageType = packageType;
      CategoryTags = categoryTags != null ? categoryTags.ToList() : null;
    }

    public PublishPackageModel(string packageFilePath, string packageType, IEnumerable<string> categoryTags)
    {
      FileName = Path.GetFileName(packageFilePath);
      Content = Convert.ToBase64String(File.ReadAllBytes(packageFilePath), Base64FormattingOptions.InsertLineBreaks);
      PackageType = packageType;
      CategoryTags = categoryTags != null ? categoryTags.ToList() : null;
    }
  }
}