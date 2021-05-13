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

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvMultiChannelGuideModel : SlimTvMultiChannelGuideModelBase
  {
    public const string MODEL_ID_STR = "5054408D-C2A9-451f-A702-E84AFCD29C10";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public SlimTvMultiChannelGuideModel()
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
