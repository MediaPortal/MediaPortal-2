#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Globalization;

namespace MediaPortal.Common.UserProfileDataManagement
{
  public class UserDataKeysKnown
  {
    public const string KEY_PLAY_COUNT = "PlayCount"; //Number of times a user played an media item
    public const string KEY_PLAY_PERCENTAGE = "PlayPercentage"; //Percentage of media item played during last playback
    public const string KEY_PLAY_DATE = "PlayDate"; //Date the media item was last played
    public const string KEY_ALLOWED_SHARE = "AllowedShare"; //Share ID's to which a user has access
    public const string KEY_ALLOW_ALL_SHARES = "AllowAllShare"; //All user access to all shares
    public const string KEY_ALLOWED_AGE = "AllowedAge"; //The maximum age for which a user can find media items based on content certification systems
    public const string KEY_ALLOW_ALL_AGES = "AllowAllAges"; //All user access to all content regardless of content certification
    public const string KEY_RESTRICTION_GROUPS = "RestrictionGroups"; //List of group names that are allowed for this user
    public const string KEY_TEMPLATE_ID = "TemplateId"; //ID of user template
    public const string KEY_ENABLE_RESTRICTION_GROUPS = "EnableRestrictionGroups"; //Flag to control if restriction groups should be applied to user
    public const string KEY_INCLUDE_PARENT_GUIDED_CONTENT = "IncludeParentGuidedContent"; //Include media items for which a parent is required
    public const string KEY_INCLUDE_UNRATED_CONTENT = "IncludeUnratedContent"; //Include media items for which a parent is required
    public const string KEY_CHANNEL_PLAY_COUNT = "ChannelPlayCount"; //Number of hours a user played a channel
    public const string KEY_CHANNEL_PLAY_DATE = "ChannelPlayDate"; //Date the channel was last played

    public static string GetSortablePlayCountString(long value)
    {
      return value.ToString("0000000000", CultureInfo.InvariantCulture);
    }

    public static string GetSortableChannelPlayCountString(double value)
    {
      return value.ToString("0000000000.0000000000", CultureInfo.InvariantCulture);
    }

    public static string GetSortablePlayPercentageString(int value)
    {
      return value.ToString("000");
    }

    public static string GetSortablePlayDateString(DateTime value)
    {
      return value.ToString("s", CultureInfo.InvariantCulture);
    }
  }
}
