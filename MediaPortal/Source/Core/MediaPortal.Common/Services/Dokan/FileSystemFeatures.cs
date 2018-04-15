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

namespace MediaPortal.Common.Services.Dokan
{
  [Flags]
#pragma warning disable 3009
  public enum FileSystemFeatures : uint
#pragma warning restore 3009
  {
    None = 0,
    CaseSensitiveSearch = 1,
    CasePreservedNames = 2,
    UnicodeOnDisk = 4,
    PersistentAcls = 8,
    SupportsRemoteStorage = 256,
    VolumeQuotas = 32,
    SupportsSparseFiles = 64,
    SupportsReparsePoints = 128,
    VolumeIsCompressed = 32768,
    SupportsObjectIDs = 65536,
    SupportsEncryption = 131072,
    NamedStreams = 262144,
    ReadOnlyVolume = 524288,
    SequentialWriteOnce = 1048576,
    SupportsTransactions = 2097152,
  }
}
