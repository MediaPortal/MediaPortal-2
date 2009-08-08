using System.Collections.Generic;
using System.Xml;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// UPnP action template which gets instantiated at the client (control point) side for each UPnP action the control point wants
  /// to use.
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvAction"/>.
  /// </remarks>
  public class CpAction
  {
    protected string _name;
    protected CpService _parentService;
    protected IList<CpArgument> _inArguments = new List<CpArgument>();
    protected IList<CpArgument> _outArguments = new List<CpArgument>();
    protected bool _isOptional = true;
    protected DeviceConnection _connection = null;
    
    /// <summary>
    /// Creates a new instance of <see cref="CpAction"/>.
    /// </summary>
    /// <param name="connection">Device connection instance which attends the connection with the server side.</param>
    /// <param name="parentService">Instance of the service which contains the new action.</param>
    /// <param name="name">The name of the new action template.</param>
    public CpAction(DeviceConnection connection, CpService parentService, string name)
    {
      _connection = connection;
      _parentService = parentService;
      _name = name;
    }

    /// <summary>
    /// Gets or sets the action's name.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets or sets a flag which controls the control point's matching behaviour.
    /// If <see cref="IsOptional"/> is set to <c>true</c>, the control point will also return services from the network
    /// which don't implement this action. If this flag is set to <c>false</c>, services without an action matching this
    /// action template won't be considered as matching services.
    /// </summary>
    public bool IsOptional
    {
      get { return _isOptional; }
      set { _isOptional = value; }
    }

    /// <summary>
    /// Returns the information if this action template is connected to a matching UPnP action. Will be set by the UPnP system.
    /// </summary>
    public bool IsConnected
    {
      get { return _connection != null; }
    }

    /// <summary>
    /// Gets or sets the parent service, this action was added to.
    /// </summary>
    public CpService ParentService
    {
      get { return _parentService; }
      internal set { _parentService = value; }
    }

    /// <summary>
    /// Returns the full qualified name of this action in the form "[DeviceName].[ServiceName].[ActionName]".
    /// </summary>
    public string FullQualifiedName
    {
      get { return _parentService.FullQualifiedName + "." + _name; }
    }

    /// <summary>
    /// Returns the action URN with service type and version, in the format
    /// "urn:schemas-upnp-org:service:[service-type]:[version]#[action-name]".
    /// </summary>
    public string Action_URN
    {
      get { return _parentService.ServiceTypeVersion_URN + "#" + _name; }
    }

    /// <summary>
    /// Returns the list of input arguments of this action.
    /// </summary>
    public IList<CpArgument> InArguments
    {
      get { return _inArguments; }
    }

    /// <summary>
    /// Returns the list of output arguments of this action.
    /// </summary>
    public IList<CpArgument> OutArguments
    {
      get { return _outArguments; }
    }

    /// <summary>
    /// Invokes this action with the given <paramref name="inParameters"/>.
    /// </summary>
    /// <param name="inParameters">Input parameters for the action invocation. Must match the formal input arguments.</param>
    /// <param name="state">State which will can be used to match the async result or error to this invocation.</param>
    public void InvokeAction_Async(IList<object> inParameters, object state)
    {
      if (!IsConnected)
        throw new IllegalCallException("This UPnP action isn't connected to a UPnP network action");
      if (!MatchesSignature(inParameters))
        throw new IllegalCallException("The given parameters don't match this action's formal input arguments");
      if (_parentService == null)
        throw new IllegalCallException("This UPnP action isn't assigned to a service");
      _parentService.InvokeAction_Async(this, inParameters, state);
    }

    /// <summary>
    /// Returns the information whether the given <paramref name="inParameters"/> match the call signature of this action.
    /// </summary>
    /// <param name="inParameters">List of parameters containing the .net object instances to be used as parameter values.</param>
    /// <returns><c>true</c>, if the given parameters can be used to call this action (via method <see cref="InvokeAction_Async"/>),
    /// else <c>false</c>.</returns>
    public bool MatchesSignature(IList<object> inParameters)
    {
      for (int i=0; i<_inArguments.Count; i++)
        if (!_inArguments[i].IsValueAssignable(inParameters[i]))
          return false;
      return _inArguments.Count == inParameters.Count;
    }

    #region Connection

    /// <summary>
    /// Adds another input argument to this action.
    /// </summary>
    /// <exception cref="UPnPAlreadyConnectedException">If the action is already connected to an action in a device.</exception>
    /// <param name="argument">Argument to add.</param>
    internal void AddInAgrument(CpArgument argument)
    {
      _inArguments.Add(argument);
    }

    /// <summary>
    /// Adds another output argument to this action.
    /// </summary>
    /// <exception cref="UPnPAlreadyConnectedException">If the action is already connected to an action in a device.</exception>
    /// <param name="argument">Argument to add.</param>
    internal void AddOutAgrument(CpArgument argument)
    {
      _outArguments.Add(argument);
    }

    internal static CpAction ConnectAction(DeviceConnection connection, CpService parentService, XmlElement actionElement)
    {
      lock (connection.CPData.SyncObj)
      {
        string name = ParserHelper.SelectText(actionElement, "name/text()");
        CpAction result = new CpAction(connection, parentService, name);
        XmlNodeList argumentList = actionElement.SelectNodes("argumentList/argument");
        for (int i = 0; i < argumentList.Count; i++)
        {
          XmlElement argumentElement = (XmlElement) argumentList[i];
          CpArgument argument = CpArgument.CreateArgument(result, parentService, argumentElement);
          if (argument.Direction == ArgumentDirection.In)
            result.AddInAgrument(argument);
          else
            result.AddOutAgrument(argument);
        }
        return result;
      }
    }

    internal void Disconnect()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        return;
      lock (connection.CPData.SyncObj)
      {
        _connection = null;
      }
    }

    #endregion
  }
}
