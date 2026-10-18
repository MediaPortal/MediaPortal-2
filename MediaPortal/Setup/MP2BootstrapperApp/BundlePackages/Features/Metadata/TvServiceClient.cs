#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace MP2BootstrapperApp.BundlePackages.Features.Metadata
{
  /// <summary>
  /// Metadata for the TV Service Client feature.
  /// </summary>
  public class TvServiceClient : AbstractFeatureMetadata
  {
    public TvServiceClient()
    // No related features since the TV layers were removed - the TvMosaic client that used to be
    // listed here is gone, and "install the TV service client together with the client" is what
    // the parent check in FeatureUtils already does.
    : base(FeatureId.SlimTvServiceClient, null, new string[0])
    {
    }
  }
}
