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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.UiComponents.Media.Models.ScreenData;

namespace MediaPortal.Plugins.SlimTv.Client.Models.ScreenData
{
  public class RecordingsSimpleSearchScreenData : VideosSimpleSearchScreenData
  {
    public RecordingsSimpleSearchScreenData(PlayableItemCreatorDelegate playableItemCreator) :
      base(playableItemCreator)
    {
    }

    public override AbstractItemsScreenData Derive()
    {
      return new RecordingsSimpleSearchScreenData(PlayableItemCreator);
    }

    protected override IFilter BuildTextSearchFilter()
    {
      // Search in both Title and Channel
      var filter = new BooleanCombinationFilter(BooleanOperator.Or,
        new IFilter[]
        {
          new LikeFilter(MediaAspect.ATTR_TITLE, GetSearchTerm(), null),
          new LikeFilter(RecordingAspect.ATTR_CHANNEL, GetSearchTerm(), null)
        });
      return filter;
    }
  }
}
