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

using System.Collections.Generic;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.General;
using Presentation.SkinEngine.XamlParser.Interfaces;

namespace Presentation.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Base class for all bindings. A binding in this context is a dependency
  /// between two objects which is maintained automatically.
  /// There is a dictionary which stores all bindings which have been attached
  /// to target objects.
  /// </summary>
  public abstract class BindingBase: IBinding, IDeepCopyable
  {
    #region Protected fields

    protected static IDictionary<object, ICollection<BindingBase>> _objects2Bindings =
        new Dictionary<object, ICollection<BindingBase>>();

    // Empty enumerator for optimized use in method GetBindingsOfObject()
    protected static readonly IEnumerable<BindingBase> EMPTY_BINDING_ENUMERABLE =
      new List<BindingBase>();

    // State variables
    protected bool _active = false; // Should the binding react to changes of source properties?
    protected object _contextObject = null; // Bound to which object?
    protected IDataDescriptor _targetDataDescriptor = null; // Bound to which target property?

    #endregion

    #region Ctor

    public BindingBase()
    { }

    /// <summary>
    /// Creates a new <see cref="BindingBase"/>, targeted at the specified
    /// <paramref name="contextObject"/>. This will be used for
    /// <see cref="BindingMarkupExtension"/>s which are used as data context,
    /// for example.
    /// </summary>
    public BindingBase(DependencyObject contextObject)
    {
      AttachToTargetObject(contextObject);
    }

    public virtual void Dispose()
    {
      DetachFromTargetObject();
    }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      BindingBase bb = source as BindingBase;
      if (bb._targetDataDescriptor != null)
      {
        // Copy values initialized by the Prepare(IParserContext,IDataDescriptor) call,
        // retargeted to the newTarget.
        object newTarget = copyManager.GetCopy(bb._targetDataDescriptor.TargetObject);
        _targetDataDescriptor = bb._targetDataDescriptor.Retarget(newTarget);
      }
      AttachToTargetObject(copyManager.GetCopy(bb._contextObject));
      // _active property should be initialized after the copying procedure has ended
      // by calling Bind()
    }

    #endregion

    #region Properties

    #endregion

    #region Protected properties and methods

    protected virtual void AttachToTargetObject(object obj)
    {
      // We could check here if obj is a DependencyObject and throw an Exception if not.
      // But by now, we will permit objects of arbitrary types.
      _contextObject = obj;
      ICollection<BindingBase> bindingsOfObject;
      if (_objects2Bindings.ContainsKey(_contextObject))
        bindingsOfObject = _objects2Bindings[_contextObject];
      else
        _objects2Bindings[_contextObject] = bindingsOfObject = new List<BindingBase>();
      bindingsOfObject.Add(this);
    }

    protected virtual void DetachFromTargetObject()
    {
      if (_objects2Bindings.ContainsKey(_contextObject))
        _objects2Bindings[_contextObject].Remove(this);
    }

    #endregion

    /// <summary>
    /// Returns all bindings which bind to properties of the specified
    /// <paramref name="obj">object</paramref>, or which are its data context.
    /// </summary>
    /// <param name="obj">Object, whose bindings should be returned.</param>
    /// <returns>Collection of bindings bound to the specified object.</returns>
    public static IEnumerable<BindingBase> GetBindingsOfObject(object obj)
    {
      if (_objects2Bindings.ContainsKey(obj))
        return _objects2Bindings[obj];
      else
        return EMPTY_BINDING_ENUMERABLE;
    }

    #region IBinding implementation

    public bool Active
    {
      get { return _active; }
    }

    public virtual void Prepare(IParserContext context, IDataDescriptor dd)
    {
      AttachToTargetObject(context.ContextStack.CurrentElementContext.Instance);
      _targetDataDescriptor = dd;
    }

    public virtual bool Bind()
    {
      _active = true;
      return true;
    }

    #endregion
  }
}
