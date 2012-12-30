using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaItemAspectModelBuilder
{
  class Program
  {
    static void Main(string[] args)
    {
      const string classNamespace = "MediaPortal.UiComponents.Media.Models.AspectWrappers";
      const bool createAsControl = true;
      const bool exposeNullables = true;

      List<Type> typeList = new List<Type> { typeof(MediaAspect), typeof(VideoAspect), typeof(AudioAspect), typeof(ImageAspect), typeof(MovieAspect), typeof(SeriesAspect) };

      foreach (Type aspectType in typeList)
      {
        AspectModelBuilder amb = new AspectModelBuilder();
        string source = amb.BuildCodeTemplate(aspectType, classNamespace, createAsControl, exposeNullables);
        string targetFileName = string.Format("{0}Wrapper.cs", aspectType.Name);
        string targetPath = @"..\..\..\..\..\Source\UI\UiComponents\Media\Models\AspectWrappers\" + targetFileName;
        using (FileStream file = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
        using(StreamWriter sw = new StreamWriter(file))
          sw.Write(source);
      }
    }
  }
}
