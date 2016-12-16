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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.Aspects;

namespace MediaItemAspectModelBuilder
{
  class Program
  {
    static void Main(string[] args)
    {
      const bool createAsControl = true;
      const bool exposeNullables = true;

      Dictionary<Type, Type[]> childTypes = new Dictionary<Type, Type[]>
      {
        { typeof(VideoAspect), new [] { typeof(VideoStreamAspect), typeof(VideoAudioStreamAspect), typeof(SubtitleAspect) } }
      };

      List<Type> typeList = new List<Type> {
        typeof(MediaAspect),
        typeof(ImporterAspect),
        typeof(VideoAspect), typeof(GenreAspect),
        typeof(VideoStreamAspect), typeof(VideoAudioStreamAspect),
        typeof(AudioAspect), typeof(AudioAlbumAspect),
        typeof(ImageAspect),
        typeof(MovieAspect), typeof(MovieCollectionAspect),
        typeof(SeriesAspect), typeof(SeasonAspect), typeof(EpisodeAspect),
        typeof(PersonAspect),
        typeof(CharacterAspect),
        typeof(CompanyAspect),
        typeof(SubtitleAspect)};
      string classNamespace = "MediaPortal.UiComponents.Media.Models.AspectWrappers";
      string codeBasePath = @"..\..\..\..\..\Source\UI\UiComponents\Media\Models\AspectWrappers\";
      string aspectNamespace = "MediaPortal.Common.MediaManagement.DefaultItemAspects";

      BuildWrappers(typeList, childTypes, classNamespace, aspectNamespace, createAsControl, exposeNullables, codeBasePath);

      typeList = new List<Type> { typeof(RecordingAspect) };
      classNamespace = "MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers";
      codeBasePath = @"..\..\..\..\..\Source\UI\TV\SlimTvClient\Models\AspectWrappers\";
      aspectNamespace = "MediaPortal.Extensions.MetadataExtractors.Aspects";

      BuildWrappers(typeList, childTypes, classNamespace, aspectNamespace, createAsControl, exposeNullables, codeBasePath);
    }

    private static void BuildWrappers(List<Type> typeList, Dictionary<Type, Type[]> childTypeMap, string classNamespace, string aspectNamespace, bool createAsControl, bool exposeNullables, string codeBasePath)
    {
      foreach (Type aspectType in typeList)
      {
        Type[] childTypes;
        if (!childTypeMap.TryGetValue(aspectType, out childTypes))
          childTypes = null;
        AspectModelBuilder amb = new AspectModelBuilder();
        string source = amb.BuildCodeTemplate(aspectType, childTypes, classNamespace, aspectNamespace, createAsControl, exposeNullables);
        string targetFileName = string.Format("{0}Wrapper.cs", aspectType.Name);
        if (!Directory.Exists(codeBasePath))
          Directory.CreateDirectory(codeBasePath);

        string targetPath = codeBasePath + targetFileName;
        using (FileStream file = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
        using (StreamWriter sw = new StreamWriter(file))
          sw.Write(source);
      }
    }
  }
}
