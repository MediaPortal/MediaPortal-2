using Emulators.LibRetro;
using Emulators.LibRetro.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class LibRetroProxy
  {
    public const string KEY_OPTION_VALUE = "OptionValue";
    public const string KEY_VARIABLE_NAME = "LibRetroVariableName";
    public const string DIALOG_OPTION_SELECT = "dialog_libretro_option_select";

    protected AbstractProperty _selectedVariableNameProperty = new WProperty(typeof(string), null);

    protected string _corePath;
    protected LibRetroEmulator _retro;
    protected string _name;
    protected List<string> _extensions;
    protected List<VariableDescription> _variables;
    protected ItemsList _variableItems = new ItemsList();
    protected ItemsList _selectedVariableOptions = new ItemsList();

    public LibRetroProxy(string corePath)
    {
      _corePath = corePath;
    }

    public string Name
    {
      get { return _name; }
    }

    public List<string> Extensions
    {
      get { return _extensions; }
    }

    public List<VariableDescription> Variables
    {
      get { return _variables; }
    }

    public ItemsList VariableItems
    {
      get { return _variableItems; }
    }

    public ItemsList SelectedVariableOptions
    {
      get { return _selectedVariableOptions; }
    }

    public AbstractProperty SelectedVariableNameProperty
    {
      get { return _selectedVariableNameProperty; }
    }

    public string SelectedVariableName
    {
      get { return (string)_selectedVariableNameProperty.GetValue(); }
      set { _selectedVariableNameProperty.SetValue(value); }
    }

    public bool Init()
    {
      bool canLoad = ServiceRegistration.Get<ILibRetroCoreInstanceManager>().TrySetCoreLoading(_corePath);
      if (!canLoad)
      {
        ShowLoadErrorDialog();
        return false;
      }

      try
      {
        _retro = new LibRetroEmulator(_corePath);
        _retro.Init();
        InitializeProperties();
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroProxy: Exception initialising LibRetro core '{0}'", ex, _corePath);
      }
      finally
      {
        _retro.Dispose();
        _retro = null;
        ServiceRegistration.Get<ILibRetroCoreInstanceManager>().SetCoreUnloaded(_corePath);
      }
      return false;
    }

    public void VariableItemSelected(ListItem variableItem)
    {
      SelectedVariableName = variableItem.Label(Consts.KEY_NAME, "").Evaluate();
      CreateVariableOptionsItems(variableItem);
      ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_OPTION_SELECT);
    }

    public void OptionItemSelected(ListItem optionItem)
    {
      VariableDescription selectedVariable;
      if (TryGetVariable(optionItem, out selectedVariable))
      {
        selectedVariable.SelectedOption = optionItem.Label(Consts.KEY_NAME, selectedVariable.DefaultOption).Evaluate();
        CreateVariableItems();
      }
    }

    protected void InitializeProperties()
    {
      if (_retro == null)
        return;
      SystemInfo coreInfo = _retro.SystemInfo;
      _name = coreInfo.LibraryName;
      if (coreInfo.ValidExtensions != null)
        _extensions = coreInfo.ValidExtensions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => "." + s.ToLowerInvariant()).ToList();
      else
        _extensions = new List<string>();

      _variables = _retro.Variables.GetAllVariables();
      UpdateVariablesFromSettings();
      CreateVariableItems();
    }

    protected void UpdateVariablesFromSettings()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      CoreSetting coreSetting;
      if (!sm.Load<LibRetroCoreSettings>().TryGetCoreSetting(_corePath, out coreSetting) || coreSetting.Variables == null)
        return;
      foreach (VariableDescription variable in coreSetting.Variables)
      {
        VariableDescription coreVariable = _variables.FirstOrDefault(v => v.Name == variable.Name);
        if (coreVariable != null)
          coreVariable.SelectedOption = variable.SelectedOption;
      }
    }

    protected void CreateVariableItems()
    {
      _variableItems.Clear();
      if (_variables != null)
      {
        foreach (VariableDescription variable in _variables)
        {
          ListItem item = new ListItem(Consts.KEY_NAME, variable.Description);
          item.SetLabel(KEY_OPTION_VALUE, variable.SelectedOption);
          item.SetLabel(KEY_VARIABLE_NAME, variable.Name);
          item.Command = new MethodDelegateCommand(() => VariableItemSelected(item));
          _variableItems.Add(item);
        }
      }
      _variableItems.FireChange();
    }

    protected void CreateVariableOptionsItems(ListItem variableItem)
    {
      _selectedVariableOptions.Clear();
      VariableDescription selectedVariable;
      if (TryGetVariable(variableItem, out selectedVariable))
      {
        foreach (string option in selectedVariable.Options)
        {
          ListItem item = new ListItem(Consts.KEY_NAME, option);
          item.SetLabel(KEY_VARIABLE_NAME, selectedVariable.Name);
          _selectedVariableOptions.Add(item);
        }
      }
      _selectedVariableOptions.FireChange();
    }

    protected bool TryGetVariable(ListItem selectedVariableItem, out VariableDescription variable)
    {
      variable = null;
      if (_variables != null && selectedVariableItem != null)
      {
        string name = selectedVariableItem.Label(KEY_VARIABLE_NAME, "").Evaluate();
        if (!string.IsNullOrEmpty(name))
          variable = _variables.FirstOrDefault(v => v.Name == name);
      }
      return variable != null;
    }

    protected void ShowLoadErrorDialog()
    {
      ServiceRegistration.Get<IThreadPool>().Add(() =>
      {
        ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.Dialog.Error.Header]", "[Emulators.LibRetro.CoreAlreadyLoaded]", DialogType.OkDialog, false, DialogButtonType.Ok);
      });
    }
  }
}
