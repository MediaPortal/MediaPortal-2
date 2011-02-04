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
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using TV4Home.Server.TVEInteractionLibrary.Interfaces;

namespace MediaPortal.Plugins.SlimTvClient.Providers.Items
{
  public class Program : IProgram
  {
    public Program()
    {}

    public Program(WebProgramDetailed webProgram)
    {
      Description = webProgram.Description;
      StartTime = webProgram.StartTime;
      EndTime = webProgram.EndTime;
      Genre = webProgram.Genre;
      Title = webProgram.Title;
      ChannelId = webProgram.IdChannel;
      ProgramId = webProgram.IdProgram;
    }

    #region IProgram Member

    public int ProgramId { get; set; }
    public int ChannelId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Genre { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    #endregion
  }
}