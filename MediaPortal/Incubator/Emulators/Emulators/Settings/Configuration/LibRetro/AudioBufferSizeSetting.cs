using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class AudioBufferSizeSetting : LimitedNumberSelect
  {
    public override void Load()
    {
      _type = NumberType.FloatingPoint;
      _step = 0.05;
      _lowerLimit = 0.05;
      _upperLimit = 10;
      _value = SettingsManager.Load<LibRetroSettings>().AudioBufferSize;
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.AudioBufferSize = _value;
      SettingsManager.Save(settings);
    }
  }
}
