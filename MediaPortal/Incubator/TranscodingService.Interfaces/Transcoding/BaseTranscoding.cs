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

using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding
{
  public abstract class BaseTranscoding
  {
    public string TranscodeId { get; set; } = "";
    public bool ConcatSourceMediaPaths { get; set; } = false;
    public Dictionary<int, string> SourceMediaPaths { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, TimeSpan> SourceMediaDurations { get; set; } = new Dictionary<int, TimeSpan>();
    public TimeSpan SourceMediaDuration { get; set; } = TimeSpan.FromSeconds(0);
    public string TranscoderBinPath { get; set; } = "";
    public string TranscoderArguments { get; set; } = "";
  }
}
