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
using System.Globalization;
using System.Windows.Input;
using Avalonia.Data.Converters;

namespace MediaPortal.Frontend.Workflow
{
  /// <summary>
  /// What a dialog's data says to the frame's <b>answer row</b>: the standing answers that belong under the
  /// contents rather than among them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept these two things apart. Its generic dialog (<c>GenericDialog.xaml</c>) laid its answers
  /// in a row of its own under the contents - <c>DialogType.OkDialog</c> and its <c>YesNoDialog</c> sibling were
  /// built from <c>DialogButtonType</c> by <c>DialogManager</c>, and the button list was drawn below the master,
  /// in a grid row of its own - and the wide master frames (<c>master_configuration_dialog_wb</c>) docked their
  /// <c>Ok | Cancel</c> pair at the bottom for the same reason: an answer is not a piece of the content, and a
  /// viewer who has read the content looks under it for the way out.
  /// </para>
  /// <para>
  /// A dialog that says no words of its own is answered by Back alone, which is what most of this front end's
  /// dialogs still are; the frame then draws no row at all. That is why the words are asked for through this
  /// interface and not left to each dialog's own body: the row is the frame's shape to draw - under the contents
  /// plate, the same place in every dialog - while the words and what they do stay the dialog's own business.
  /// </para>
  /// </remarks>
  public interface IDialogAnswers
  {
    /// <summary>The word of the keeping answer - "OK" - or the empty string when this dialog does not keep.</summary>
    string DialogOkWord { get; }

    /// <summary>What pressing the keeping answer does, or null alongside an empty word.</summary>
    ICommand? DialogOkCommand { get; }

    /// <summary>The word of the throwing-away answer, or the empty string. Back already throws away everywhere.</summary>
    string DialogCancelWord { get; }

    /// <summary>What pressing the throwing-away answer does, or null alongside an empty word.</summary>
    ICommand? DialogCancelCommand { get; }
  }

  /// <summary>
  /// Asks whether a dialog's data carries an answer row at all: a data object that speaks no keeping and no
  /// throwing-away word gets no row, and the frame leaves the ten of the row and its five of padding out of the
  /// dialog entirely rather than showing an empty band where the answers would stand.
  /// </summary>
  public sealed class DialogAnswersRowIsUp : IValueConverter
  {
    public static readonly DialogAnswersRowIsUp Instance = new DialogAnswersRowIsUp();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      return value is IDialogAnswers answers
          && (!string.IsNullOrEmpty(answers.DialogOkWord) || !string.IsNullOrEmpty(answers.DialogCancelWord));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
