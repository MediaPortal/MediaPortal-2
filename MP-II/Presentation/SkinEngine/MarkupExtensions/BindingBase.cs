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
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.Controls;

namespace Presentation.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Base class for all bindings. A binding in this context is a dependency
  /// between two objects which is maintained automatically.
  /// There is a dictionary which stores all bindings which have been attached
  /// to target objects.
  /// </summary>
  public abstract class BindingBase: IBinding
  {
    #region Protected fields

    protected static IDictionary<object, ICollection<BindingBase>> _objects2Bindings =
        new Dictionary<object, ICollection<BindingBase>>();

    // State variables
    protected bool _bound = false; // Did we already bind?
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

    /// <summary>
    /// Creates a new <see cref="BindingBase"/> as a copy of the
    /// specified <paramref name="other"/> binding. The new binding instance
    /// will be re-targeted to the specified <paramref name="newTarget"/> object,
    /// which means that its <see cref="_contextObject"/> and
    /// <see cref="_targetDataDescriptor"/> will be changed to the
    /// <paramref name="newTarget"/> object.
    /// </summary>
    /// <param name="other">Other Binding to copy.</param>
    /// <param name="newTarget">New target object for this Binding.</param>
    public BindingBase(BindingBase other, object newTarget)
    {
      // Copy values initialized by the Prepare(IParserContext,IDataDescriptor) call,
      // retargeted to the newTarget.
      _targetDataDescriptor = other._targetDataDescriptor == null ? null :
          other._targetDataDescriptor.Retarget(newTarget);
      AttachToTargetObject(newTarget);
    }

    /// <summary>
    /// Given a <paramref name="sourceObject"/>, which has possibly attached
    /// bindings targeted to it, this method will copy those bindings
    /// retargeted at the specified <paramref name="targetObject"/>.
    /// </summary>
    /// <remarks>
    /// A typical usage of this method would be the cloning of a gui object,
    /// where the clone should behave exactly as the original object.
    /// </remarks>
    /// <param name="sourceObject">Object, whose bindings (which are targeted
    /// at it) will be copied.</param>
    /// <param name="targetObject">Object, to which the copied bindings will
    /// be retargeted.</param>
    public static void CopyBindings(object sourceObject, object targetObject)
    {
      if (_objects2Bindings.ContainsKey(sourceObject))
      {
        ICollection<BindingBase> bindings = _objects2Bindings[sourceObject];
        foreach (BindingBase binding in bindings)
        {
          BindingBase newBinding = binding.CloneAndRetarget(targetObject);
          if (binding.Bound)
            newBinding.Bind();
        }
      }
    }

    /// <summary>
    /// Will clone this binding and retarget it to the specified new target object.
    /// </summary>
    /// <param name="newTarget">Target object the new binding should bind to.</param>
    /// <returns>New, retargeted binding instance. The new binding instance has the same
    /// function to the new target object as this binding has on the associated
    /// <see cref="_contextObject"/>.</returns>
    public abstract BindingBase CloneAndRetarget(object newTarget);

    public virtual void Dispose()
    {
      DetachFromTargetObject();
    }

    #endregion

    #region Properties

    #endregion

    #region Protected properties and methods

    protected virtual void AttachToTargetObject(object obj)
    {
      // We could check here if obj is a DependencyObject and throw an Exception.
      // But by now, we will permit an arbitrary object.
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

    #region IBinding implementation

    public bool Bound
    {
      get { return _bound; }
    }

    public virtual void Prepare(IParserContext context, IDataDescriptor dd)
    {
      AttachToTargetObject(context.ContextStack.CurrentElementContext.Instance);
      _targetDataDescriptor = dd;
    }

    public virtual bool Bind()
    {
      _bound = true;
      return true;
    }

    #endregion
  }
}
