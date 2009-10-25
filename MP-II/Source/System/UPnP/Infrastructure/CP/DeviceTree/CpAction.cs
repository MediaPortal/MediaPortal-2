#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;
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
    #region Inner classes

    public class AsyncActionCallResult : IAsyncResult, IDisposable
    {
      protected object _syncObj = new object();
      protected EventWaitHandle _waitHandle = null;
      protected IList<object> _outParams = null;
      protected UPnPError _error = null;
      protected AsyncCallback _callback;
      protected object _asyncState;

      public AsyncActionCallResult(AsyncCallback callback, object asyncState)
      {
        _callback = callback;
        _asyncState = asyncState;
      }

      ~AsyncActionCallResult()
      {
        Dispose();
      }
      
      public void Dispose()
      {
        lock (_syncObj)
          if (_waitHandle != null)
          {
            _waitHandle.Close();
            _waitHandle = null;
          }
      }

      internal void ActionResultPresent(IList<object> outParams)
      {
        lock (_syncObj)
          _outParams = outParams;
        ActionFinished();
      }

      internal void ActionErrorResultPresent(UPnPError error)
      {
        lock (_syncObj)
          _error = error;
        ActionFinished();
      }

      internal IList<object> GetOutParams()
      {
        if (!IsCompleted)
          AsyncWaitHandle.WaitOne();
        lock (_syncObj)
        {
          try
          {
            if (_error != null)
              throw new UPnPRemoteException(_error);
            return _outParams;
          }
          finally
          {
            Dispose();
          }
        }
      }

      protected void ActionFinished()
      {
        lock (_syncObj)
          if (_waitHandle != null)
            _waitHandle.Set();
        if (_callback != null)
          _callback(this);
      }

      public object SyncObj
      {
        get { return _syncObj; }
      }

      #region IAsyncResult implementation

      public object AsyncState
      {
        get { return _asyncState; }
      }

      public WaitHandle AsyncWaitHandle
      {
        get
        {
          lock (_syncObj)
            if (_waitHandle == null)
              _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
          return _waitHandle;
        }
      }

      public bool CompletedSynchronously
      {
        get { return false; }
      }

      public bool IsCompleted
      {
        get
        {
          lock (_syncObj)
            return _outParams != null || _error != null;
        }
      }

      #endregion
    }

    #endregion

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
    /// Returns the information whether the given <paramref name="inParameters"/> match the call signature of this action.
    /// </summary>
    /// <param name="inParameters">List of parameters containing the .net object instances to be used as parameter values.</param>
    /// <returns><c>true</c>, if the given parameters can be used to call this action (via method <see cref="BeginInvokeAction"/>
    /// or <see cref="InvokeAction"/>), else <c>false</c>.</returns>
    public bool MatchesSignature(IList<object> inParameters)
    {
      for (int i=0; i<_inArguments.Count; i++)
        if (!_inArguments[i].IsValueAssignable(inParameters[i]))
          return false;
      return _inArguments.Count == inParameters.Count;
    }

    /// <summary>
    /// Invokes this action with the given <paramref name="inParameters"/> asynchronously.
    /// </summary>
    /// <remarks>
    /// This method will start the action call request asynchronously and will return immediately.
    /// The UPnP system invokes the provided <paramref name="callback"/> when the action result (or error message)
    /// returns. The call structure corresponds to the asynchronous call pattern used in the .net library.
    /// </remarks>
    /// <param name="inParameters">Input parameters for the action invocation. Must match the formal input arguments.</param>
    /// <param name="callback">Callback delegate to be invoked when the action result is present.</param>
    /// <param name="state">This object can be used to pass state information for the asynchronous operation.</param>
    /// <returns>Async result object which should be used as parameter for the <see cref="EndInvokeAction"/> method.</returns>
    public IAsyncResult BeginInvokeAction(IList<object> inParameters, AsyncCallback callback, object state)
    {
      if (_parentService == null)
        throw new IllegalCallException("This UPnP action isn't assigned to a service");
      if (!MatchesSignature(inParameters))
        throw new IllegalCallException("The given parameters don't match this action's formal input arguments");
      if (!IsConnected)
        throw new IllegalCallException("This UPnP action isn't connected to a UPnP network action");
      AsyncActionCallResult ar = new AsyncActionCallResult(callback, state);
      _connection.OnActionCalled(this, inParameters, ar);
      return ar;
    }

    /// <summary>
    /// Gets the output parameters of an formerly invoked action call.
    /// </summary>
    /// <remarks>
    /// If the action invocation isn't completed yet, this method blocks the calling thread until the action invocation
    /// either completes successfully or with an error.
    /// </remarks>
    /// <param name="result">Return value from the former <see cref="BeginInvokeAction"/> call.</param>
    /// <returns>List of output arguments of the called action.</returns>
    /// <exception cref="UPnPException">If an error occured during the action call.</exception>
    public IList<object> EndInvokeAction(IAsyncResult result)
    {
      AsyncActionCallResult ar = result as AsyncActionCallResult;
      if (ar == null)
        throw new IllegalCallException("Provided 'result' parameter doesn't belong to a 'BeginInvokeAction' call");
      return ar.GetOutParams();
    }

    /// <summary>
    /// Invokes this action with the given <paramref name="inParameters"/> synchronously.
    /// </summary>
    /// <param name="inParameters">Input parameters for the action invocation. Must match the formal input arguments.</param>
    /// <returns>List of output arguments of the called action.</returns>
    /// <exception cref="UPnPException">If an error occured during the action call.</exception>
    public IList<object> InvokeAction(IList<object> inParameters)
    {
      AsyncActionCallResult ar = (AsyncActionCallResult) BeginInvokeAction(inParameters, null, null);
      return EndInvokeAction(ar);
    }

    internal void ActionResultPresent(IList<object> outParams, object handle)
    {
      AsyncActionCallResult asyncResult = (AsyncActionCallResult) handle;
      asyncResult.ActionResultPresent(outParams);
    }

    internal void ActionErrorResultPresent(UPnPError error, object handle)
    {
      AsyncActionCallResult asyncResult = (AsyncActionCallResult) handle;
      asyncResult.ActionErrorResultPresent(error);
    }

    #region Connection

    /// <summary>
    /// Adds a new formal input argument to this action.
    /// </summary>
    /// <exception cref="UPnPAlreadyConnectedException">If the action is already connected to an action in a device.</exception>
    /// <param name="argument">Argument to add.</param>
    internal void AddInAgrument(CpArgument argument)
    {
      _inArguments.Add(argument);
    }

    /// <summary>
    /// Adds a new formal output argument to this action.
    /// </summary>
    /// <exception cref="UPnPAlreadyConnectedException">If the action is already connected to an action in a device.</exception>
    /// <param name="argument">Argument to add.</param>
    internal void AddOutAgrument(CpArgument argument)
    {
      _outArguments.Add(argument);
    }

    internal static CpAction ConnectAction(DeviceConnection connection, CpService parentService, XPathNavigator actionNav,
        IXmlNamespaceResolver nsmgr)
    {
      lock (connection.CPData.SyncObj)
      {
        string name = ParserHelper.SelectText(actionNav, "s:name/text()", nsmgr);
        CpAction result = new CpAction(connection, parentService, name);
        XPathNodeIterator argumentIt = actionNav.Select("s:argumentList/s:argument", nsmgr);
        while (argumentIt.MoveNext())
        {
          CpArgument argument = CpArgument.CreateArgument(result, parentService, argumentIt.Current, nsmgr);
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
