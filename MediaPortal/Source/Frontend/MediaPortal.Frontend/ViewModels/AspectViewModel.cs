/*
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

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One media item aspect as the home screen shows it: its name, how many items carry it, and what it is made
  /// of.
  /// </summary>
  /// <remarks>
  /// <see cref="Count"/> is separate from the aspect itself because the server answers those two questions at
  /// two URLs: the schema arrives in one request, while every count is its own query against the database. A
  /// screen that waited for all the counts before showing anything would sit blank for as long as the slowest
  /// one takes, so the names appear first and the numbers arrive as they come in.
  /// </remarks>
  public partial class AspectViewModel : ObservableObject
  {
    private readonly MediaAspect _aspect;

    [ObservableProperty]
    private int? _count;

    public AspectViewModel(MediaAspect aspect)
    {
      _aspect = aspect;
    }

    public string Name
    {
      get { return _aspect.Name; }
    }

    public Guid Id
    {
      get { return _aspect.Id; }
    }

    /// <summary>
    /// Whether the aspect is part of the server rather than added by a plugin.
    /// </summary>
    public bool IsSystemAspect
    {
      get { return _aspect.SystemAspect; }
    }

    /// <summary>
    /// Attributes that can be filtered or sorted on cheaply, because the server keeps an index over them. The
    /// screen shows them first; a query builder offers the rest in a second group.
    /// </summary>
    public IReadOnlyList<AspectAttribute> IndexedAttributes
    {
      get
      {
        List<AspectAttribute> indexed = new List<AspectAttribute>();
        foreach (AspectAttribute attribute in _aspect.Attributes)
          if (attribute.Indexed)
            indexed.Add(attribute);
        return indexed;
      }
    }

    public IReadOnlyList<AspectAttribute> Attributes
    {
      get { return _aspect.Attributes; }
    }

    /// <summary>
    /// <c>null</c> until the count has arrived, and <c>null</c> afterwards too if the server refused to answer
    /// it - which is why the screen writes "unknown" rather than a zero.
    /// </summary>
    public string CountText
    {
      get { return Count.HasValue ? Count.Value.ToString("N0") : "?"; }
    }

    partial void OnCountChanged(int? value)
    {
      OnPropertyChanged(nameof(CountText));
    }
  }
}
