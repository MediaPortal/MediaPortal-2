#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Base class for all bindings. A binding in this context is an object
  /// describing a reference to a binding source object, which is evaluated
  /// automatically.
  /// </summary>
  /// <remarks>
  /// The binding is located in a binding context, which is
  /// the starting point for the search of the binding source object.
  /// There is also a target data descriptor which may reference a target
  /// property to be updated automatically, but this feature doesn't need to
  /// be used.
  /// There is a dictionary which stores all bindings which have been attached
  /// to target objects.
  /// </remarks>
  public abstract class BindingBase: DependencyObject, IBinding
  {
    #region Protected fields

    // State variables
    protected bool _active = false; // Should the binding react to changes of source properties?
    protected DependencyObject _contextObject = null; // Bound to which object? May be null.
    protected IDataDescriptor _targetDataDescriptor = null; // Bound to which target property?

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new, uninitialized <see cref="BindingBase"/> object.
    /// The returned instance will have to be initialized either by a call to
    /// <see cref="SetTargetDataDescriptor(IDataDescriptor)"/> or by a deep copying procedure
    /// (call to <see cref="DeepCopy(IDeepCopyable,ICopyManager)"/>).
    /// </summary>
    protected BindingBase()
    { }

    /// <summary>
    /// Creates a new <see cref="BindingBase"/>, targeted at the specified
    /// <paramref name="contextObject"/>. This will be used for
    /// <see cref="BindingExtension"/>s which are used as data context,
    /// for example.
    /// </summary>
    protected BindingBase(DependencyObject contextObject)
    {
      AttachToTargetObject(contextObject);
    }

    public override void Dispose()
    {
      _active = false;
      DetachFromTargetObject();
      base.Dispose();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BindingBase bb = (BindingBase) source;
      if (bb._targetDataDescriptor != null)
      {
        // Copy values initialized by the Prepare(IDataDescriptor) call,
        // retargeted to the newTarget.
        object newTarget = copyManager.GetCopy(bb._targetDataDescriptor.TargetObject);
        _targetDataDescriptor = bb._targetDataDescriptor.Retarget(newTarget);
      }
      else
        _targetDataDescriptor = null;
      AttachToTargetObject(copyManager.GetCopy(bb._contextObject));
      // Bindings have to be activated manually after copy procedure
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the information if our data descriptor has a binding type, which means
    /// this binding instance has to be assigned rather than its resolved value.
    /// </summary>
    protected bool KeepBinding
    {
      get { return _targetDataDescriptor == null || typeof(IBinding).IsAssignableFrom(_targetDataDescriptor.DataType); }
    }

    #endregion

    #region Protected properties and methods

    protected internal void AttachToTargetObject(DependencyObject obj)
    {
      if (obj == null)
        return;
      // obj may be of arbitrary type; The type isn't fixed to DependencyObject
      _contextObject = obj;
      _contextObject.AddToBindingCollection(this);
    }

    protected void DetachFromTargetObject()
    {
      if (_contextObject == null)
        return;
      _contextObject.RemoveFromBindingCollection(this);
    }

    #endregion

    #region IBinding implementation

    public bool Active
    {
      get { return _active; }
    }

    public virtual void SetTargetDataDescriptor(IDataDescriptor dd)
    {
      _targetDataDescriptor = dd;
      DependencyObject depObj = dd == null ? null : dd.TargetObject as DependencyObject;
      if (depObj != null)
        AttachToTargetObject(depObj);
    }

    public virtual void Activate()
    {
      _active = true;
    }

    public IBinding CopyAndRetarget(IDataDescriptor newDd)
    {
      IDictionary<object, object> exceptionalIdentities = new Dictionary<object, object>
        {
            {LogicalParent, null}
        };
      if (_contextObject != null)
        exceptionalIdentities.Add(_contextObject, newDd.TargetObject);
      IDataDescriptor targetSave = _targetDataDescriptor;
      try
      {
        _targetDataDescriptor = null; // We have to detach temporarily, else the copy won't work
        BindingBase result = MpfCopyManager.DeepCopyWithIdentities(this, exceptionalIdentities);
        result.SetTargetDataDescriptor(newDd);
        return result;
      }
      finally
      {
        // Restore this binding attachment
        _targetDataDescriptor = targetSave;
      }
    }

    #endregion
  }
}
