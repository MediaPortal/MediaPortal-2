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

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  public enum ChannelType
  {
    Unsupported = -1,
    Analog,
    Atsc,
    DvbC,
    DvbS,
    DvbT,
    DvbIP,
  }

  /// <summary>
  /// ITuningDetail represents channel tuning details.
  /// </summary>
  public interface ITuningDetail
  {
    /// <summary>
    /// Gets or Sets the Tuning Detail ID.
    /// </summary>
    int TuningDetailId { get; set; }

    /// <summary>
    /// Gets or Sets the inner FEC rate.
    /// </summary>
    int InnerFecRate { get; set; }

    /// <summary>
    /// Gets or Sets the Channel ID.
    /// </summary>
    int ChannelId { get; set; }

    /// <summary>
    /// Gets or Sets the Name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or Sets the Provider.
    /// </summary>
    string Provider { get; set; }

    /// <summary>
    /// Gets or Sets the ChannelType.
    /// </summary>
    ChannelType ChannelType { get; set; }

    /// <summary>
    /// Gets or Sets the PCN.
    /// </summary>
    int PhysicalChannelNumber { get; set; }

    /// <summary>
    /// Gets or Sets the Frequency.
    /// </summary>
    int Frequency { get; set; }

    /// <summary>
    /// Gets or Sets the Country ID.
    /// </summary>
    int CountryId { get; set; }

    /// <summary>
    /// Gets or Sets the MediaType.
    /// </summary>
    MediaType MediaType { get; set; }

    /// <summary>
    /// Gets or Sets the Network ID.
    /// </summary>
    int NetworkId { get; set; }

    /// <summary>
    /// Gets or Sets the Transport ID.
    /// </summary>
    int TransportId { get; set; }

    /// <summary>
    /// Gets or Sets the Service ID.
    /// </summary>
    int ServiceId { get; set; }

    /// <summary>
    /// Gets or Sets the PMT PID.
    /// </summary>
    int PmtPid { get; set; }

    /// <summary>
    /// Gets or Sets whether the channel is encrypted or free to air.
    /// </summary>
    bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or Sets the Modulation.
    /// </summary>
    int Modulation { get; set; }

    /// <summary>
    /// Gets or Sets the Polarisation.
    /// </summary>
    int Polarisation { get; set; }

    /// <summary>
    /// Gets or Sets the Symbol rate.
    /// </summary>
    int Symbolrate { get; set; }

    /// <summary>
    /// Gets or Sets the Bandwidth.
    /// </summary>
    int Bandwidth { get; set; }

    /// <summary>
    /// Gets or Sets the LogicalChannelNumber.
    /// </summary>
    string LogicalChannelNumber { get; set; }

    /// <summary>
    /// Gets or Sets the VideoSource.
    /// </summary>
    int VideoSource { get; set; }

    /// <summary>
    /// Gets or Sets the AudioSource.
    /// </summary>
    int AudioSource { get; set; }

    /// <summary>
    /// Gets or Sets whether the channel is VCR or not.
    /// </summary>
    bool IsVCRSignal { get; set; }

    /// <summary>
    /// Gets or Sets the TuningSource.
    /// </summary>
    int TuningSource { get; set; }

    /// <summary>
    /// Gets or Sets the Pilot.
    /// </summary>
    int Pilot { get; set; }

    /// <summary>
    /// Gets or Sets the RollOff.
    /// </summary>
    int RollOff { get; set; }

    /// <summary>
    /// Gets or Sets the Url.
    /// </summary>
    string Url { get; set; }
  }
}
