#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Plugins.MediaServer.DLNA
{
  /// <summary>
  /// DLNA Requirement [7.3.30-37]
  /// </summary>
  public class DlnaForthField
  {
    private readonly IList<DlnaForthFieldParam> _parameters;

    public DlnaForthField()
    {
      _parameters = new List<DlnaForthFieldParam>();
      ProfileParameter = new ProfileParam();
      OperationsParameter = new OperationsParam();
      _parameters.Add(OperationsParameter);
      PlaySpeedParameter = new PlaySpeedParam();
      _parameters.Add(PlaySpeedParameter);
      ConversionParameter = new ConversionParam();
      _parameters.Add(ConversionParameter);
      FlagsParameter = new FlagsParam();
      _parameters.Add(FlagsParameter);
    }

    public DlnaForthField(string profileName)
      : this()
    {
      ProfileParameter.ProfileName = profileName;
    }

    public ProfileParam ProfileParameter { get; private set; }
    public OperationsParam OperationsParameter { get; private set; }
    public PlaySpeedParam PlaySpeedParameter { get; private set; }
    public ConversionParam ConversionParameter { get; private set; }
    public FlagsParam FlagsParameter { get; private set; }

    public IList<DlnaForthFieldParam> Parameters
    {
      get { return _parameters; }
    }

    public override string ToString()
    {
      var result = "";
      if (ProfileParameter.Show == true)
      {
        result = ProfileParameter.ToString();
      }
      foreach (var p in _parameters)
      {
        var item = p.ToString();
        // Ensure there is a ';' inbetween params
        if (!string.IsNullOrEmpty(item))
        {
          if (!string.IsNullOrEmpty(result))
          {
            result = result + ";" + item;
          }
          else
          {
            result = item;
          }
        }
      }
      if (string.IsNullOrEmpty(result))
      {
        result = "*";
      }
      return result;
    }

    public abstract class DlnaForthFieldParam
    {
      protected bool _show;

      public virtual bool Show
      {
        get { return _show; }
        set { _show = value; }
      }

      protected static string BoolAsNumber(bool val)
      {
        return val ? "1" : "0";
      }
    }

    public class ProfileParam : DlnaForthFieldParam
    {
      public ProfileParam()
      {
        ProfileName = "UNKNOWN";
      }

      public string ProfileName { get; set; }

      public override bool Show
      {
        get { return true; }
        set { }
      }

      public override string ToString()
      {
        return Show ? string.Format(DLNA_PARAM_PN_FORMAT, ProfileName) : "";
      }

      public const string DLNA_PARAM_PN_FORMAT = "DLNA.ORG_PN={0}";
    }

    /// <summary>
    /// Format string for DLNA parameters. 
    ///   OP=XX: Not supporting time-based-seek, supporting Range based seek
    ///   
    /// </summary>
    public class OperationsParam : DlnaForthFieldParam
    {
      public OperationsParam()
      {
        _timeSeekRangeSupport = false;
        _byteSeekRangeSupport = true;
        _show = false;
      }

      private bool _timeSeekRangeSupport;
      private bool _byteSeekRangeSupport;

      public bool TimeSeekRangeSupport
      {
        get { return _timeSeekRangeSupport; }
        set
        {
          _timeSeekRangeSupport = value;
          Show = true;
        }
      }

      public bool ByteSeekRangeSupport
      {
        get { return _byteSeekRangeSupport; }
        set
        {
          _byteSeekRangeSupport = value;
          Show = true;
        }
      }


      public override string ToString()
      {
        return Show
                 ? string.Format(DLNA_PARAM_OP_FORMAT,
                                 BoolAsNumber(TimeSeekRangeSupport),
                                 BoolAsNumber(ByteSeekRangeSupport))
                 : "";
      }

      public const string DLNA_PARAM_OP_FORMAT = "DLNA.ORG_OP={0}{1}";
    }

    //TODO: Unknown how to implement this
    public class PlaySpeedParam : DlnaForthFieldParam
    {
      public PlaySpeedParam()
      {
      }


      public override string ToString()
      {
        return Show
                 ? string.Format(DLNA_PARAM_PS_FORMAT,
                                 "???")
                 : "";
      }

      public const string DLNA_PARAM_PS_FORMAT = "DLNA.ORG_PS={0}";
    }

    public class ConversionParam : DlnaForthFieldParam
    {
      public ConversionParam()
      {
        _convertedContent = false;
      }

      private bool _convertedContent;

      public bool ConvertedContent
      {
        get { return _convertedContent; }
        set
        {
          _convertedContent = value;
          Show = true;
        }
      }

      public override string ToString()
      {
        return Show
                 ? string.Format(DLNA_PARAM_CI_FORMAT,
                                 BoolAsNumber(ConvertedContent))
                 : "";
      }

      public const string DLNA_PARAM_CI_FORMAT = "DLNA.ORG_CI={0}";
    }

    public class FlagsParam : DlnaForthFieldParam
    {
      public FlagsParam()
      {
        _show = true;
        TimeBasedSeek = false;
        ByteBasedSeek = false;
        InteractiveMode = true;
        BackgroundMode = true;
        Dlna1Dot5Version = true;
      }

      public bool SenderPaced { get; set; }
      public const uint SenderPacedValue = 0x80000000;
      public bool TimeBasedSeek { get; set; }
      public const uint TimeBasedSeekValue = 0x40000000;
      public bool ByteBasedSeek { get; set; }
      public const uint ByteBasedSeekValue = 0x20000000;
      public bool PlayerContainer { get; set; }
      public const uint PlayerContainerValue = 0x10000000;
      public bool UcdamS0Increasing { get; set; }
      public const uint UcdamS0IncreasingValue = 0x8000000;
      public bool UcdamSnIncreasing { get; set; }
      public const uint UcdamSnIncreasingValue = 0x4000000;
      public bool RtspPauseOperation { get; set; }
      public const uint RtspPauseOperationValue = 0x2000000;
      public bool StreamingMode { get; set; }
      public const uint StreamingModeValue = 0x1000000;
      public bool InteractiveMode { get; set; }
      public const uint InteractiveModeValue = 0x800000;
      public bool BackgroundMode { get; set; }
      public const uint BackgroundModeValue = 0x400000;
      public bool HttpConnectionStalling { get; set; }
      public const uint HttpConnectionStallingValue = 0x200000;
      public bool Dlna1Dot5Version { get; set; }
      public const uint Dlna1Dot5VersionValue = 0x100000;
      public bool LinkProtectedContent { get; set; }
      public const uint LinkProtectedContentValue = 0x10000;
      public bool CleartextByteFullDataSeek { get; set; }
      public const uint CleartextByteFullDataSeekValue = 0x8000;
      public bool CleartextLimitedDataSeek { get; set; }
      public const uint CleartextLimitedDataSeekValue = 0x4000;


      public override string ToString()
      {
        if (!Show) return "";

        uint result = 0;
        if (SenderPaced) result += SenderPacedValue;
        if (TimeBasedSeek) result += TimeBasedSeekValue;
        if (ByteBasedSeek) result += ByteBasedSeekValue;
        if (PlayerContainer) result += PlayerContainerValue;
        if (UcdamS0Increasing) result += UcdamS0IncreasingValue;
        if (UcdamSnIncreasing) result += UcdamSnIncreasingValue;
        if (RtspPauseOperation) result += RtspPauseOperationValue;
        if (StreamingMode) result += StreamingModeValue;
        if (InteractiveMode) result += InteractiveModeValue;
        if (BackgroundMode) result += BackgroundModeValue;
        if (HttpConnectionStalling) result += HttpConnectionStallingValue;
        if (Dlna1Dot5Version) result += Dlna1Dot5VersionValue;
        if (LinkProtectedContent) result += LinkProtectedContentValue;
        if (CleartextByteFullDataSeek) result += CleartextByteFullDataSeekValue;
        if (CleartextLimitedDataSeek) result += CleartextLimitedDataSeekValue;

        return string.Format(DLNA_PARAM_FLAGS_FORMAT, result);
      }

      public const string DLNA_PARAM_FLAGS_FORMAT = "DLNA.ORG_FLAGS={0:X8}000000000000000000000000";
    }
  }
}
