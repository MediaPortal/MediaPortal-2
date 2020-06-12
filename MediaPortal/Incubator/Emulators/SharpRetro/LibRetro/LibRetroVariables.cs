using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class LibRetroVariables
  {
    protected Dictionary<string, VariableDescription> _variables = new Dictionary<string, VariableDescription>();
    protected bool _updated;

    public bool Updated
    {
      get { return _updated; }
    }

    public List<VariableDescription> GetAllVariables()
    {
      return _variables.Values.ToList();
    }

    public bool Contains(string variableName)
    {
      return _variables.ContainsKey(variableName);
    }

    public void AddOrUpdate(VariableDescription variable)
    {
      VariableDescription vd;
      if (_variables.TryGetValue(variable.Name, out vd))
        variable.SelectedOption = vd.SelectedOption;
      _variables[variable.Name] = variable;
      _updated = true;
    }

    public void AddOrUpdate(string variableName, string selectedOption)
    {
      VariableDescription vd;
      if (_variables.TryGetValue(variableName, out vd))
        vd.SelectedOption = selectedOption;
      else
        _variables[variableName] = new VariableDescription() { Name = variableName, SelectedOption = selectedOption };
      _updated = true;
    }

    public bool TryGet(string variableName, out VariableDescription variable)
    {
      _updated = false;
      return _variables.TryGetValue(variableName, out variable);
    }
  }

  public class VariableDescription
  {
    protected string _selectedOption;

    public VariableDescription() { }

    public VariableDescription(IntPtr keyPtr, IntPtr variablePtr)
    {
      string key = Marshal.PtrToStringAnsi(keyPtr);
      string[] parts = Marshal.PtrToStringAnsi(variablePtr).Split(';');

      Name = key;
      Description = parts[0];
      Options = parts[1].TrimStart(' ').Split('|');
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Options { get; set; }

    public string DefaultOption
    {
      get { return Options != null && Options.Length > 0 ? Options[0] : ""; }
    }

    public string SelectedOption
    {
      get { return string.IsNullOrEmpty(_selectedOption) ? DefaultOption : _selectedOption; }
      set
      {
        if (Options != null && Options.Contains(value))
          _selectedOption = value;
      }
    }

    public override string ToString()
    {
      return string.Format("Name: {0}, Description: {1}, Options: {2}", Name, Description, string.Join("|", Options));
    }
  }
}