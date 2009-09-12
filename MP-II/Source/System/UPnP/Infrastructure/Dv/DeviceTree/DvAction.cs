using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Delegate which gets called when an action is invoked by a client.
  /// </summary>
  /// <param name="action">Action instance which was invoked.</param>
  /// <param name="inParams">Input parameters which match the data types described in <see cref="DvAction.InArguments"/>.</param>
  /// <param name="outParams">Output parameters which match the data types described in <see cref="DvAction.OutArguments"/>.</param>
  /// <returns><c>null</c>, if the action invocation was successful, else UPnP error instance with error code and error
  /// description.</returns>
  public delegate UPnPError ActionInvokeDlgt(DvAction action, IList<object> inParams, out IList<object> outParams);

  /// <summary>
  /// Base UPnP action class providing the functionality for an UPnP service action.
  /// Either subclasses can be implemented for a special action of a concrete UPnP service or instances of this class
  /// can be created and configured from outside.
  /// </summary>
  public class DvAction
  {
    protected string _name;
    protected DvService _parentService;
    protected ActionInvokeDlgt _actionInvoked;
    protected IList<DvArgument> _inArguments;
    protected IList<DvArgument> _outArguments;

    /// <summary>
    /// Creates a new action in a device's service.
    /// </summary>
    /// <param name="name">Name of the action. MUST NOT contain a hyphen character ("-") nor
    /// a hash character ("#"). Case sensitive. First character MUST be a USASCII
    /// letter ("A"-"Z", "a"-"z"), USASCII digit ("0"-"9"), an underscore ("_"), or a non-experimental
    /// Unicode letter or digit greater than U+007F. Succeeding characters MUST be a USASCII letter
    /// ("A"-"Z", "a"-"z"), USASCII digit ("0"-"9"), an underscore ("_"), a period ("."), a Unicode
    /// combiningchar, an extender, or a non-experimental Unicode letter or digit greater than
    /// U+007F. The first three letters MUST NOT be "XML" in any combination of case.
    /// <list type="bullet">
    /// <item>For standard actions defined by a UPnP Forum working committee, MUST NOT begin with
    /// "X_" nor "A_".</item>
    /// <item>For non-standard actions specified by a UPnP vendor and added to a standard service,
    /// MUST begin with "X_", followed by a Vendor Domain Name, followed by the underscore
    /// character ("_"), followed by the vendor-assigned action name. The vendor-assigned
    /// action name must comply with the syntax rules defined above.</item>
    /// </list>
    /// Case sensitive. SHOULD be < 32 characters.
    /// </param>
    /// <param name="onInvoke">Delegate which gets called when this action is invoked.</param>
    /// <param name="inArguments">Enumeration of formal input arguments for this action.</param>
    /// <param name="outArguments">Enumeration of formal output arguments for this action.</param>
    public DvAction(string name, ActionInvokeDlgt onInvoke,
        IEnumerable<DvArgument> inArguments, IEnumerable<DvArgument> outArguments)
    {
      _name = name;
      _actionInvoked = onInvoke;
      _inArguments = new List<DvArgument>(inArguments);
      _outArguments = new List<DvArgument>(outArguments);
    }

    public DvAction(string name, ActionInvokeDlgt actionInvoked) :
        this(name, actionInvoked, new DvArgument[] {}, new DvArgument[] {}) { }

    public string Name
    {
      get { return _name; }
    }

    public DvService ParentService
    {
      get { return _parentService; }
      internal set { _parentService = value; }
    }

    public IList<DvArgument> InArguments
    {
      get { return _inArguments; }
    }

    public IList<DvArgument> OutArguments
    {
      get { return _outArguments; }
    }

    public void AddInAgrument(DvArgument argument)
    {
      _inArguments.Add(argument);
    }

    public void AddOutAgrument(DvArgument argument)
    {
      _outArguments.Add(argument);
    }

    public UPnPError InvokeAction(IList<object> inParameters, out IList<object> outParameters, bool checkSignature)
    {
      if (!checkSignature && !MatchesSignature(inParameters))
        throw new ArgumentException(string.Format("UPnP Action '{0}' cannot be called with this signature", _name));
      return FireActionInvoked(inParameters, out outParameters);
    }

    public bool MatchesSignature(IList<object> inParameters)
    {
      for (int i=0; i<_inArguments.Count; i++)
        if (!_inArguments[i].IsValueAssignable(inParameters[i]))
          return false;
      return _inArguments.Count == inParameters.Count;
    }

    protected UPnPError FireActionInvoked(IList<object> inParams, out IList<object> outParams)
    {
      outParams = null;
      if (_actionInvoked != null)
        return _actionInvoked(this, inParams, out outParams);
      return new UPnPError(602, "Optional Action Not Implemented");
    }

    #region Description generation

    internal void AddSCDPDescriptionForAction(StringBuilder result)
    {
      result.Append(
          "<action>" +
            "<name>");
      result.Append(_name);
      result.Append("</name>");
      IList<DvArgument> arguments = new List<DvArgument>(_inArguments);
      CollectionUtils.AddAll(arguments, _outArguments);
      if (arguments.Count > 0)
      {
        result.Append(
            "<argumentList>");
        foreach (DvArgument argument in arguments)
          argument.AddSCDPDescriptionForArgument(result);
        result.Append(
            "</argumentList>");
      }
      result.Append(
          "</action>");
    }

    #endregion
  }
}
