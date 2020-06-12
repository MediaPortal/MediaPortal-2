using MediaPortal.Common.Settings;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Settings
{
  public class LibRetroCoreSettings
  {
    protected List<CoreSetting> _coreSettings;
    [Setting(SettingScope.User, null)]
    public List<CoreSetting> CoreSettings
    {
      get
      {
        if (_coreSettings == null)
          _coreSettings = new List<CoreSetting>();
        return _coreSettings;
      }
      set { _coreSettings = value; }
    }

    public bool TryGetCoreSetting(string corePath, out CoreSetting coreSetting)
    {
      coreSetting = CoreSettings.FirstOrDefault(s => s.CorePath == corePath);
      return coreSetting != null;
    }

    public void AddOrUpdateCoreSetting(string corePath, List<VariableDescription> variables)
    {
      CoreSetting coreSetting = CoreSettings.FirstOrDefault(s => s.CorePath == corePath);
      if (coreSetting != null)
        coreSetting.Variables = variables;
      else
        CoreSettings.Add(new CoreSetting() { CorePath = corePath, Variables = variables });
    }
  }

  public class CoreSetting
  {
    protected List<VariableDescription> _variables;
    public string CorePath { get; set; }
    public List<VariableDescription> Variables
    {
      get
      {
        if (_variables == null)
          _variables = new List<VariableDescription>();
        return _variables;
      }
      set { _variables = value; }
    }
  }
}