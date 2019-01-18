#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.WifiRemote.Messages.MediaInfo
{
    /// <summary>
    /// Hardcoded list of MpExtended providers that are supported in WifiRemote
    /// </summary>
    public enum MpExtendedProviders
    {
        FsPictures = 1,
        LocalFileSystem = 2,
        MovingPictures = 3,
        MPMusic = 4,
        MPShares = 5,
        MPTvSeries = 6,
        MPVideo = 7,
        MpMusicShare = 9,
        MpVideosShare = 10,
        MpPicturesShare = 11
    }
}
