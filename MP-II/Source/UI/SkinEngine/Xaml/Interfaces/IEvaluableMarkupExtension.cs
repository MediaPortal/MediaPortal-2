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

using MediaPortal.SkinEngine.Xaml.Exceptions;

namespace MediaPortal.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Represents an instance of a markup extension which can be evaluated
  /// early, i.e. during the parsing time. Implementing classes will
  /// declare constructors and properties to configure this instance. The method
  /// <see cref="IEvaluableMarkupExtension.Evaluate(IParserContext)"/>
  /// will evaluate the value implied by this markup extension instance.
  /// </summary>
  public interface IEvaluableMarkupExtension
  {
    /// <summary>
    /// Evaluates the value of this markup extension. Markup extensions, which cannot
    /// evaluate, may throw a <see cref="XamlBindingException"/>.
    /// </summary>
    /// <param name="context">The context instance during the parsing process.</param>
    object Evaluate(IParserContext context);
  }
}
