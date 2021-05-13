#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvClientModelBase"/> is the main entry model for SlimTV. It provides channel group and channel selection and 
  /// acts as backing model for the Live-TV OSD to provide program information.
  /// </summary>
  public class SlimTvClientModel : SlimTvClientModelBase
  {
    public const string MODEL_ID_STR = "8BEC1372-1C76-484c-8A69-C7F3103708EC";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public SlimTvClientModel()
    {
      _mediaMode = MediaMode.Tv;
    }

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    #endregion
  }
}
