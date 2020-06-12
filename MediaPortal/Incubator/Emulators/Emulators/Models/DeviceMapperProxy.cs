using Emulators.LibRetro.Controllers.Mapping;
using Emulators.Models.Navigation;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class DeviceMapperProxy : IDisposable
  {
    protected AbstractProperty _supportsDeadZoneProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _currentDeadZoneProperty = new WProperty(typeof(int), -1);
    protected AbstractProperty _selectedInputProperty = new WProperty(typeof(MappedInput), null);

    protected IDeviceMapper _deviceMapper;
    protected RetroPadMapping _currentMapping;
    protected IList<ListItem> _inputList = new List<ListItem>();

    public DeviceMapperProxy(IDeviceMapper deviceMapper, RetroPadMapping mapping)
    {
      _deviceMapper = deviceMapper;
      _currentMapping = mapping;
      CreateInputList();
      if (deviceMapper.SupportsDeadZone)
      {
        SupportsDeadZone = true;
        CurrentDeadZone = mapping.DeadZone;
      }
    }

    public RetroPadMapping Mapping
    {
      get { return _currentMapping; }
    }

    public IList<ListItem> Inputs
    {
      get { return _inputList; }
    }

    public AbstractProperty SelectedInputProperty
    {
      get { return _selectedInputProperty; }
    }

    public MappedInput SelectedInput
    {
      get { return (MappedInput)_selectedInputProperty.GetValue(); }
      set { _selectedInputProperty.SetValue(value); }
    }

    public AbstractProperty SupportsDeadZoneProperty
    {
      get { return _supportsDeadZoneProperty; }
    }

    public bool SupportsDeadZone
    {
      get { return (bool)_supportsDeadZoneProperty.GetValue(); }
      protected set { _supportsDeadZoneProperty.SetValue(value); }
    }

    public AbstractProperty CurrentDeadZoneProperty
    {
      get { return _currentDeadZoneProperty; }
    }

    public int CurrentDeadZone
    {
      get { return (int)_currentDeadZoneProperty.GetValue(); }
      set { _currentDeadZoneProperty.SetValue(value); }
    }

    public bool TryMap()
    {
      MappedInput selectedInput = SelectedInput;
      if (selectedInput != null)
      {
        DeviceInput input = _deviceMapper.GetPressedInput();
        if (input != null)
        {
          selectedInput.Input = input;
          _currentMapping.Map(selectedInput);
          return true;
        }
      }
      return false;
    }

    public void UpdateInputs()
    {
      foreach (MappedInputItem item in _inputList)
        item.Update();
    }

    public void Save()
    {
      if (_deviceMapper.SupportsDeadZone)
        _currentMapping.DeadZone = CurrentDeadZone;
    }

    protected void CreateInputList()
    {
      _inputList.Clear();
      if (_currentMapping != null)
      {
        foreach (MappedInput mappedInput in _currentMapping.AvailableInputs)
        {
          DeviceInput input;
          if (_currentMapping.TryGetMapping(mappedInput, out input))
            mappedInput.Input = input;

          MappedInputItem inputItem = new MappedInputItem(mappedInput.Name, mappedInput);
          inputItem.Command = new MethodDelegateCommand(() => SelectedInput = inputItem.MappedInput);
          _inputList.Add(inputItem);
        }
      }
    }

    public void Dispose()
    {
      IDisposable disposable = _deviceMapper as IDisposable;
      if (disposable != null)
        disposable.Dispose();
    }
  }
}
