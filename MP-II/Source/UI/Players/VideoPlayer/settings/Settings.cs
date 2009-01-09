#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Presentation.DataObjects;
using Microsoft.Win32; // for 'RegistryKey'
using MediaPortal.Core;
using MediaPortal.Core.Settings;

namespace MediaPortal.SkinEngine.Players
{
  public class Settings
  {
    readonly ItemsList _mpeg2Codecs;
    readonly ItemsList _h264Codecs;
    readonly ItemsList _divxCodecs;
    readonly ItemsList _audioCodecs;
    readonly ItemsList _defaultAudioLanguages;
    readonly ItemsList _defaultSubtitleLanguages;
    AudioSettings _audioSettings;
    Property _propertyIsDigital;
    Property _propertyIsAnalog;
    Property _propertyIsBuildIn;
    Property _propertyIsStereo;
    Property _propertyIs51;
    Property _propertyIs71;

    public Settings()
    {

      _propertyIsDigital = new Property(typeof(bool), false);
      _propertyIsAnalog = new Property(typeof(bool), false);
      _propertyIsBuildIn = new Property(typeof(bool), false);
      _propertyIsStereo = new Property(typeof(bool), false);
      _propertyIs51 = new Property(typeof(bool), false);
      _propertyIs71 = new Property(typeof(bool), false);


      _audioSettings = ServiceScope.Get<ISettingsManager>().Load<AudioSettings>();
      switch (_audioSettings.ConnectionType)
      {
        case SpeakerConnectionType.Analog:
          IsAnalog = true;
          break;
        case SpeakerConnectionType.BuildIn:
          IsBuildIn = true;
          break;
        case SpeakerConnectionType.Digital:
          IsDigital = true;
          break;
      }
      switch (_audioSettings.Count)
      {
        case SpeakerAmount.Stereo:
          IsStereo = true;
          break;
        case SpeakerAmount.Speakers_5_1:
          Is51 = true;
          break;
        case SpeakerAmount.Speakers_7_1:
          Is71 = true;
          break;
      }
      _propertyIsDigital.Attach(OnSpeakerConnectionChanged);
      _propertyIsAnalog.Attach(OnSpeakerConnectionChanged);
      _propertyIsBuildIn.Attach(OnSpeakerConnectionChanged);

      _propertyIsStereo.Attach(OnSpeakerAmountChanged);
      _propertyIs51.Attach(OnSpeakerAmountChanged);
      _propertyIs71.Attach(OnSpeakerAmountChanged);

      _mpeg2Codecs = new ItemsList();
      _h264Codecs = new ItemsList();
      _divxCodecs = new ItemsList();
      _audioCodecs = new ItemsList();
      _defaultAudioLanguages = new ItemsList();
      _defaultSubtitleLanguages = new ItemsList();

      AddCodec(_mpeg2Codecs, "CyberLink Video/SP Decoder (PDVD7)", "{8ACD52ED-9C2D-4008-9129-DCE955D86065}");
      AddCodec(_mpeg2Codecs, "CyberLink Video/SP Decoder", "{C81C8C5A-B354-4DEB-96D3-8BD8D0C8ABD0}");
      AddCodec(_mpeg2Codecs, "Microsoft MPEG-2 Video Decoder", "{212690FB-83E5-4526-8FD7-74478B7939CD}");
      AddCodec(_mpeg2Codecs, "NVIDIA Video Decoder", "{71E4616A-DB5E-452B-8CA5-71D9CC7805E9}");
      AddCodec(_mpeg2Codecs, "MPV Decoder Filter", "{39F498AF-1A09-4275-B193-673B0BA3D478}");
      AddCodec(_mpeg2Codecs, "InterVideo Video Decoder", "{0246CA20-776D-11D2-8010-00104B9B8592}");

      AddCodec(_h264Codecs, "CyberLink H.264/AVC Decoder (PDVD7.x)", "{F2E3D920-0F9B-4319-BE87-EB94CCEB6C09}");
      AddCodec(_h264Codecs, "CyberLink H.264/AVC Decoder (PDVD7)", "{D12E285B-3B29-4416-BA8E-79BD81D193CC}");
      AddCodec(_h264Codecs, "CoreAVC Video Decoder", "{09571A4B-F1FE-4C60-9760-DE6D310C7C31}");

      AddCodec(_audioCodecs, "CyberLink Audio Decoder (PDVD7.x)", "{D5DBA1A7-61A0-437E-B6AB-C9C422F466B5}");
      AddCodec(_audioCodecs, "CyberLink Audio Decoder (PDVD7 UPnP)", "{706E503A-EB19-4106-9D7C-0384359D511A}");
      AddCodec(_audioCodecs, "CyberLink Audio Decoder", "{B60C424E-AB7B-429F-9B9B-93684E51EA75}");
      AddCodec(_audioCodecs, "CyberLink Audio Decoder", "{03EC05EA-C2A7-49A8-971F-580D5891F2FB}");
      AddCodec(_audioCodecs, "CyberLink Audio Decoder  (PDVD7)", "{284DC28A-4A7D-442C-BC2E-D7480556E4D8}");
      AddCodec(_audioCodecs, "NVIDIA Audio Decoder", "{6C0BDF86-C36A-4D83-8BDB-312D2EAF409E}");
      AddCodec(_audioCodecs, "MPA Decoder Filter", "{3D446B6F-71DE-4437-BE15-8CE47174340F}");
      AddCodec(_audioCodecs, "ffdshow Audio Decoder", "{0F40E1E5-4F79-4988-B1A9-CC98794E6B55}");
      AddCodec(_audioCodecs, "Microsoft MPEG-1/DD Audio Decoder", "{E1F1A0B8-BEEE-490D-BA7C-066C40B5E2B9}");
      AddCodec(_audioCodecs, "InterVideo Audio Decoder", "{7E2E0DC1-31FD-11D2-9C21-00104B3801F6}");

      AddCodec(_divxCodecs, "ffdshow Video Decoder", "{04FE9017-F873-410E-871E-AB91661A4EF7}");
      AddCodec(_divxCodecs, "DivX Decoder Filter", "{78766964-0000-0010-8000-00AA00389B71}");
      AddCodec(_divxCodecs, "Xvid MPEG-4 Video Decoder", "{64697678-0000-0010-8000-00AA00389B71}");
    }

    #region default audio language setting
    public ItemsList DefaultAudioLanguages
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        if (_defaultAudioLanguages.Count == 0)
        {
          CultureInfo[] culturesInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
          for (int i = 0; i < culturesInfos.Length; ++i)
          {
            ListItem item = new ListItem("Name", culturesInfos[i].EnglishName);
            _defaultAudioLanguages.Add(item);
          }
        }
        foreach (ListItem item in _defaultAudioLanguages)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.AudioLanguage, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _defaultAudioLanguages;
      }
    }
    public void SetDefaultAudioLanguage(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string name = item.Label("Name", "").Evaluate();
      settings.AudioLanguage = name;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }
    #endregion

    #region default subtitle language setting
    public ItemsList DefaultSubtitleLanguages
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        if (_defaultSubtitleLanguages.Count == 0)
        {
          CultureInfo[] culturesInfos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
          for (int i = 0; i < culturesInfos.Length; ++i)
          {
            ListItem item = new ListItem("Name", culturesInfos[i].EnglishName);
            _defaultSubtitleLanguages.Add(item);
          }
        }
        foreach (ListItem item in _defaultSubtitleLanguages)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.SubtitleLanguage, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _defaultSubtitleLanguages;
      }
    }
    public void SetDefaultSubtitleLanguage(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string name = item.Label("Name", "").Evaluate();
      settings.SubtitleLanguage = name;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }
    #endregion

    #region default audio/video codecs settings
    public ItemsList Mpeg2Codecs
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        foreach (ListItem item in _mpeg2Codecs)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.Mpeg2Codec, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _mpeg2Codecs;
      }
    }
    public ItemsList H264Codecs
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        foreach (ListItem item in _h264Codecs)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.H264Codec, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _h264Codecs;
      }
    }
    public ItemsList DivxCodecs
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        foreach (ListItem item in _divxCodecs)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.DivXCodec, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _divxCodecs;
      }
    }
    public ItemsList AudioCodecs
    {
      get
      {
        VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
        foreach (ListItem item in _audioCodecs)
        {
          string name = item.Label("Name", "").Evaluate();
          if (String.Compare(name, settings.AudioCodec, true) == 0)
            item.Selected = true;
          else
            item.Selected = false;
        }
        return _audioCodecs;
      }
    }
    public void SetMpeg2Codec(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.Mpeg2Codec = item.Label("Name", "").Evaluate();
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }
    public void SetH264Codec(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.H264Codec = item.Label("Name", "").Evaluate();
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }
    public void SetDivXCodec(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.DivXCodec = item.Label("Name", "").Evaluate();
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }
    public void SetAudioCodec(ListItem item)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.AudioCodec = item.Label("Name", "").Evaluate();
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    static void AddCodec(ICollection<ListItem> collection, string name, string CLSID)
    {
      if (DoesComObjectExists(CLSID))
      {
        ListItem item = new ListItem("Name", name);
        collection.Add(item);
      }
    }

    // checks to see if a COM object is registered and exists on the filesystem
    static bool DoesComObjectExists(string CLSID)
    {
      using (RegistryKey myRegKey = Registry.LocalMachine)
      {
        Object val;

        try
        {
          // get the pathname to the COM server DLL if the key exists
          using (RegistryKey subKey = myRegKey.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + CLSID + @"\InprocServer32"))
          {
            if (subKey == null)
            {
              return false;
            }
            val = subKey.GetValue(null); // the null gets default
          }
        }
        catch
        {
          return false;
        }

        try
        {
          // parse out the version number embedded in the resource
          // in the DLL
          if (!System.IO.File.Exists(val.ToString()))
            return false;
        }
        catch
        {
          return false;
        }

        return true;
      }
    }
    #endregion

    #region speaker setup
    public AudioSettings Speakers
    {
      get
      {
        return _audioSettings;
      }
    }


    #region connection type properties
    public bool IsDigital
    {
      get
      {
        return (bool)_propertyIsDigital.GetValue();
      }
      set
      {
        _propertyIsDigital.SetValue(value);
      }
    }
    public Property IsDigitalProperty
    {
      get
      {
        return _propertyIsDigital;
      }
      set
      {
        _propertyIsDigital = value;
      }
    }
    public bool IsAnalog
    {
      get
      {
        return (bool)_propertyIsAnalog.GetValue();
      }
      set
      {
        _propertyIsAnalog.SetValue(value);
      }
    }
    public Property IsAnalogProperty
    {
      get
      {
        return _propertyIsAnalog;
      }
      set
      {
        _propertyIsAnalog = value;
      }
    }
    public bool IsBuildIn
    {
      get
      {
        return (bool)_propertyIsBuildIn.GetValue();
      }
      set
      {
        _propertyIsBuildIn.SetValue(value);
      }
    }
    public Property IsBuildInProperty
    {
      get
      {
        return _propertyIsBuildIn;
      }
      set
      {
        _propertyIsBuildIn = value;
      }
    }

    #region speaker amount properties
    public bool IsStereo
    {
      get
      {
        return (bool)_propertyIsStereo.GetValue();
      }
      set
      {
        _propertyIsStereo.SetValue(value);
      }
    }
    public Property IsStereoProperty
    {
      get
      {
        return _propertyIsStereo;
      }
      set
      {
        _propertyIsStereo = value;
      }
    }
    public bool Is51
    {
      get
      {
        return (bool)_propertyIs51.GetValue();
      }
      set
      {
        _propertyIs51.SetValue(value);
      }
    }
    public Property Is51Property
    {
      get
      {
        return _propertyIs51;
      }
      set
      {
        _propertyIs51 = value;
      }
    }
    public bool Is71
    {
      get
      {
        return (bool)_propertyIs71.GetValue();
      }
      set
      {
        _propertyIs71.SetValue(value);
      }
    }
    public Property Is71Property
    {
      get
      {
        return _propertyIs71;
      }
      set
      {
        _propertyIs71 = value;
      }
    }
    #endregion

    #endregion

    void OnSpeakerConnectionChanged(Property prop, object oldValue)
    {
      if ((bool)prop.GetValue() == false) return;
      if (prop == IsBuildInProperty)
      {
        IsDigital = false;
        IsAnalog = false;
        _audioSettings.ConnectionType = SpeakerConnectionType.BuildIn;
      }
      else if (prop == IsDigitalProperty)
      {
        IsBuildIn = false;
        IsAnalog = false;
        _audioSettings.ConnectionType = SpeakerConnectionType.Digital;
      }
      else if (prop == IsAnalogProperty)
      {
        IsBuildIn = false;
        IsDigital = false;
        _audioSettings.ConnectionType = SpeakerConnectionType.Analog;
      }
      ServiceScope.Get<ISettingsManager>().Save(_audioSettings);
    }

    void OnSpeakerAmountChanged(Property prop, object oldValue)
    {
      if ((bool)prop.GetValue() == false) return;
      if (prop == IsStereoProperty)
      {
        Is51 = false;
        Is71 = false;
        _audioSettings.Count = SpeakerAmount.Stereo;
      }
      else if (prop == Is51Property)
      {
        IsStereo = false;
        Is71 = false;
        _audioSettings.Count = SpeakerAmount.Speakers_5_1;
      }
      else if (prop == Is71Property)
      {
        IsStereo = false;
        Is51 = false;
        _audioSettings.Count = SpeakerAmount.Speakers_7_1;
      }
      ServiceScope.Get<ISettingsManager>().Save(_audioSettings);
    }
    #endregion
  }
}
