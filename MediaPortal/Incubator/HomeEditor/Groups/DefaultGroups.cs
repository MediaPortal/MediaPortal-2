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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Groups
{
  public static class DefaultGroups
  {
    public const string DEFAULT_OTHERS_GROUP_NAME = "[HomeEditor.OthersMenuItem]";

    public static List<HomeMenuGroup> Create()
    {
      return new List<HomeMenuGroup>
      {
        new HomeMenuGroup("[Media.ImagesMenuItem]", new Guid("E9463404-FF36-4255-91FD-4742ECDBAF6A"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[Media.ImagesMenuItem]", new Guid("55556593-9FE9-436C-A3B6-A971E10C9D44"))
          }
        },
        new HomeMenuGroup("[Media.AudioMenuItem]", new Guid("B527E507-2B32-437D-8CD7-D670950BAD39"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[Media.AudioMenuItem]", new Guid("30715D73-4205-417F-80AA-E82F0834171F")),
            new HomeMenuAction("[PartyMusicPlayer.MenuItem]", new Guid("E00B8442-8230-4D7B-B871-6AC77755A0D5"))
          }
        },
        new HomeMenuGroup("[Media.VideosMenuItem]", new Guid("99B80A99-419C-4FA8-97E1-1B4E6406E09A"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[Media.VideosMenuItem]", new Guid("A4DF2DF6-8D66-479A-9930-D7106525EB07")),
            new HomeMenuAction("[Media.MoviesMenuItem]", new Guid("80D2E2CC-BAAA-4750-807B-F37714153751")),
            new HomeMenuAction("[Media.SeriesMenuItem]", new Guid("30F57CBA-459C-4202-A587-09FFF5098251")),
            new HomeMenuAction("OnlineVideos", new Guid("C33E39CC-910E-41C8-BFFD-9ECCD340B569")),
            new HomeMenuAction("[Media.BrowseMediaMenuItem]", new Guid("93442DF7-186D-42E5-A0F5-CF1493E68F49"))
          }
        },
        new HomeMenuGroup("[SlimTvClient.Main]", new Guid("C556242C-A97D-4947-9821-B3E71F866836"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[SlimTvClient.Main]", new Guid("B4A9199F-6DD4-4BDA-A077-DE9C081F7703")),
            new HomeMenuAction("[SlimTvClient.TvGuide]", new Guid("A298DFBE-9DA8-4C16-A3EA-A9B354F3910C")),
            new HomeMenuAction("[SlimTvClient.Schedules]", new Guid("87355E05-A15B-452A-85B8-98D4FC80034E")),
            new HomeMenuAction("[SlimTvClient.RecordingsMenuItem]", new Guid("7F52D0A1-B7F8-46A1-A56B-1110BBFB7D51")),
            new HomeMenuAction("[SlimTvClient.ProgramSearch]", new Guid("D91738E9-3F85-443B-ABBD-EF01731734AD"))
          }
        },
        new HomeMenuGroup("[News.Title]", new Guid("F41D7ACB-EA54-42D3-993C-E9770762931F"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[News.Title]", new Guid("BB49A591-7705-408F-8177-45D633FDFAD0")),
            new HomeMenuAction("[Weather.Title]", new Guid("E34FDB62-1F3E-4AA9-8A61-D143E0AF77B5"))
          }
        },
        new HomeMenuGroup("[Configuration.Name]", new Guid("DF5A84B0-2DE1-4DD2-B0BC-982751DB9EEB"))
        {
          Actions = new List<HomeMenuAction>
          {
            new HomeMenuAction("[Configuration.Name]", new Guid("F6255762-C52A-4231-9E67-14C28735216E")),
            new HomeMenuAction("[SystemState.ShutdownDialogStateDisplayLabel]", new Guid("C5B14E9C-7F54-4B75-BEB0-8BE2EA1F4394")),
            new HomeMenuAction("[HomeEditor.Configuration]", new Guid("AEBAE6B5-71B3-4164-992F-296E2ACF8E5C")),
            new HomeMenuAction("[SkinSettings.Configuration.SkinSettings]", new Guid("0983AB07-5EC6-4F38-B657-F07FF0819A4E"))
          }
        },
      };
    }
  }
}