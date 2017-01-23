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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Shares;

namespace MediaPortal.Test.CodeTest
{
  public class RATestModel
  {
    public readonly string STR_ID_MODEL = "81679E2D-C267-4E7E-97F6-792989123DAF";

    protected AbstractProperty _resourcePathStrProperty = new WProperty(typeof(string), string.Empty);

    public RATestModel()
    {
      ResourcePathStr = "{e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}:///Path.To.iso>{112728b1-f71d-4284-9e5c-3462e8d3c74d}:///";
    }

    public AbstractProperty ResourcePathStrProperty
    {
      get { return _resourcePathStrProperty; }
    }

    public string ResourcePathStr
    {
      get { return (string) _resourcePathStrProperty.GetValue(); }
      set { _resourcePathStrProperty.SetValue(value); }
    }

    public void DoImport()
    {
      ILocalSharesManagement lsm = ServiceRegistration.Get<ILocalSharesManagement>();
      Share videoShare = lsm.Shares.Values.FirstOrDefault(share => share.Name.Contains("Video"));
      if (videoShare == null)
        return;
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.ScheduleImport(videoShare.BaseResourcePath, videoShare.MediaCategories, true);
    }

    public void CreateResourceAccessor()
    {
      try
      {
        string resourcePathStr = ResourcePathStr;
        ResourcePath path = ResourcePath.Deserialize(resourcePathStr);
        IResourceAccessor ra;
        if (path.TryCreateLocalResourceAccessor(out ra))
          ra.Dispose();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error creating resource accessor", e);
      }
    }
  }
}