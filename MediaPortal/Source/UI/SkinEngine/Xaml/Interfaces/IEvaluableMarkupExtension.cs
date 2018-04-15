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

using MediaPortal.UI.SkinEngine.Xaml.Exceptions;

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Represents an instance of a markup extension which can be evaluated to a value, typically during the parsing time.
  /// Implementing classes will declare constructors and properties to configure this instance. The method
  /// <see cref="Initialize"/> will be called during parsing time, <see cref="Evaluate"/> will be called some time later and
  /// will evaluate the value implied by this markup extension instance.
  /// </summary>
  public interface IEvaluableMarkupExtension
  {
    /// <summary>
    /// Evaluates the value of this markup extension. Markup extensions, which cannot
    /// evaluate, may throw a <see cref="XamlBindingException"/>.
    /// </summary>
    /// <param name="context">The context instance during the parsing process.</param>
    void Initialize(IParserContext context);

    /// <summary>
    /// Returns the value of this evaluatable markup extension. This will be called after <see cref="Initialize"/> was
    /// called.
    /// </summary>
    /// <param name="value">Value which was evaluated by this markup extension.</param>
    /// <returns><c>true</c>, if this markup extension could be evaluated, else <c>false</c>.</returns>
    bool Evaluate(out object value);
  }
}
