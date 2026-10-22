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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The watching screen: the picture, the strip of programme data along the bottom, the digits somebody is
  /// typing, and the list of channels that goes over the picture when somebody asks for it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The list's rows are built in code because they are the reference's miniguide rows and those are exactly
  /// specified rather than styled: fifty pixels high, the channel's number and name at one side and the
  /// programme running on it and the one after it beside them, with the running one filled in a lighter grey than
  /// the rest (<c>SlimTvClient-miniguide.xaml</c> and <c>SlimTvStyles.xaml:1389-1436</c>, where
  /// <c>EpgSlimRowHeight</c> is fifty and the row's two fills are <c>#2c2c2c</c> and <c>#00757575</c>). A press on
  /// a row tunes that channel and closes the list, which is what the reference's row did - it was called a
  /// miniguide, and you watched from it; recording came from a different key.
  /// </para>
  /// <para>
  /// The digits have one timer, started afresh with every digit and running for the reference's zap timeout of
  /// two seconds (<c>ChannelZapModel.cs:148</c>, which rearmed its <c>DelayedEvent</c> the same way). It is the
  /// only way a keyboard - which has no key release to hold a channel on and no "input done" key of a remote -
  /// can tell "1" then "4" then "8" from three separate channels.
  /// </para>
  /// </remarks>
  /// <remarks>
  /// For the reference's own numbers: see <see cref="TvTunerViewModel"/>.
  /// </remarks>
  public partial class TvWatchView : UserControl
  {
    /// <summary>How tall one row of the list is - the reference's <c>EpgSlimRowHeight</c>.</summary>
    private const double ROW_HEIGHT = 50;

    /// <summary>The row's fill, which is the reference's <c>BaseFill</c> (#2c2c2c).</summary>
    private static readonly IBrush ROW_FILL = new SolidColorBrush(Color.Parse("#2c2c2c"));

    /// <summary>The fill of the row that is on the screen, the reference's running row colour.</summary>
    private static readonly IBrush ROW_RUNNING = new SolidColorBrush(Color.Parse("#757575"));

    private readonly TvTunerViewModel? _model;
    private DispatcherTimer? _zapTimer;
    private DispatcherTimer? _creep;
    private bool _listWanted;

    /// <summary>For the XAML loader and the designer.</summary>
    public TvWatchView()
        : this(new TvTunerViewModel(TvMediaType.Television))
    {
    }

    public TvWatchView(TvTunerViewModel model, VideoFrameSink? frames = null)
    {
      _model = model;
      DataContext = model;
      InitializeComponent();

      // The picture is the player's; this screen only watches the surface it hands out.
      videoSurface.Follow(frames);

      upButton.Click += delegate { model.ZapNext(); };
      downButton.Click += delegate { model.ZapPrevious(); };
      backChannelButton.Click += delegate { model.ZapBack(); };
      stopButton.Click += delegate { model.Stop(); };
      listButton.Click += delegate { ShowList(!_listWanted); };

      // The picker would otherwise print a bouquet's whole record; its name is the only part anybody reads.
      groupPicker.ItemTemplate = new FuncDataTemplate<TvChannelGroup>(delegate (TvChannelGroup? group,
          INameScope? scope)
      {
        return new TextBlock { Text = group == null ? "" : group.Name, FontSize = 14 };
      });

      // Every digit starts the reference's two seconds - or whatever the viewer's settings say - again. Typing 1,
      // 4, 8 in a row is one channel; typing 1, waiting three seconds, typing 4 is two channels.
      _zapTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(model.ZapTimeoutSeconds) };
      _zapTimer.Tick += delegate
      {
        if (_zapTimer != null)
          _zapTimer.Stop();
        model.CommitDigits();
      };

      // The strip's bar crawls, the rows' bars crawl, and a programme that has finished has to leave both. Ten
      // seconds is what the guide's now-line is given and it is often enough for an evening.
      _creep = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
      _creep.Tick += delegate { RebuildRows(); };
      _creep.Start();

      model.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs e)
      {
        if (e.PropertyName == nameof(TvTunerViewModel.Digits))
        {
          if (_zapTimer != null)
          {
            _zapTimer.Stop();
            if (model.IsTypingANumber)
            {
              // Read again every time, so that a viewer who changes the timeout in the settings gets the new one
              // the next time they type rather than the one that was current when this screen opened.
              _zapTimer.Interval = TimeSpan.FromSeconds(model.ZapTimeoutSeconds);
              _zapTimer.Start();
            }
          }
          return;
        }

        if (e.PropertyName == nameof(TvTunerViewModel.WatchRows)
            || e.PropertyName == nameof(TvTunerViewModel.Tuned))
          RebuildRows();
      };

      Unloaded += delegate
      {
        if (_zapTimer != null)
        {
          _zapTimer.Stop();
          _zapTimer = null;
        }
        if (_creep != null)
        {
          _creep.Stop();
          _creep = null;
        }
        videoSurface.Follow(null);
      };

      List<Control> ring = new List<Control>();
      ring.Add(upButton);
      ring.Add(downButton);
      ring.Add(backChannelButton);
      ring.Add(listButton);
      ring.Add(groupPicker);
      ring.Add(stopButton);
      DpadNavigation.Attach(this, ring);

      RebuildRows();
    }

    /// <summary>Whether the list of channels is over the picture.</summary>
    public bool IsListShowing
    {
      get { return _listWanted; }
    }

    /// <summary>
    /// Puts the list over the picture, or takes it away. The picture keeps playing the whole time - which is how
    /// the reference did it, by opening the miniguide as a dialog whose host is transparent rather than by
    /// switching to another screen, so that the viewer never loses the channel they were watching while choosing
    /// another.
    /// </summary>
    public void ShowList(bool show)
    {
      _listWanted = show;
      listPanel.IsVisible = show;
      if (show)
      {
        RebuildRows();
        DpadNavigation.Attach(this, BuildRing(true));
      }
      else
      {
        DpadNavigation.Attach(this, BuildRing(false));
      }
    }

    /// <summary>
    /// A digit pressed on the keyboard. The remote's digit keys have no equivalent here that does not fight with
    /// typing in a text box, so the keys are handled by whoever has the screen - which is the same arrangement as
    /// the reference's, where the skin's keys are bound to the screen rather than to the model.
    /// </summary>
    public void PressDigit(int digit)
    {
      _model?.PressDigit(digit);
    }

    /// <summary>The rows, rebuilt whenever the channels or their programme data change.</summary>
    private void RebuildRows()
    {
      if (_model == null)
        return;

      rowHost.Children.Clear();
      foreach (TvWatchRow row in _model.WatchRows)
        rowHost.Children.Add(BuildRow(row));
    }

    /// <summary>
    /// One channel in the list: its number and name, what is running on it, and what comes next. Fifty pixels,
    /// filled grey, lighter while it is the channel on the screen, and pressing it watches it.
    /// </summary>
    private Control BuildRow(TvWatchRow row)
    {
      bool tuned = _model != null && _model.Tuned != null
          && string.Equals(_model.Tuned.Id, row.Channel.Id, StringComparison.Ordinal);

      Button press = new Button
      {
        Background = tuned ? ROW_RUNNING : ROW_FILL,
        BorderThickness = new Thickness(0),
        CornerRadius = new CornerRadius(2),
        Height = ROW_HEIGHT,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Padding = new Thickness(6, 0),
        Tag = row
      };
      press.Click += delegate
      {
        _model?.Tune(row.Channel);
        ShowList(false);
      };

      Grid line = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(70)),
          new ColumnDefinition(new GridLength(240)),
          new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
          new ColumnDefinition(new GridLength(1, GridUnitType.Star)) },
          ColumnSpacing = 12 };

      StackPanel head = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        VerticalAlignment = VerticalAlignment.Center
      };
      head.Children.Add(new TextBlock
      {
        Text = row.Number,
        FontSize = 13,
        Opacity = 0.4,
        MinWidth = 26,
        TextAlignment = TextAlignment.Right
      });
      head.Children.Add(new TextBlock
      {
        Text = row.Name,
        FontSize = 15,
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center
      });
      Grid.SetColumn(head, 1);
      line.Children.Add(head);

      StackPanel now = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
      now.Children.Add(new TextBlock
      {
        Text = row.NowTitle,
        FontSize = 14,
        TextTrimming = TextTrimming.CharacterEllipsis
      });
      if (row.HasProgramme)
      {
        now.Children.Add(new ProgressBar
        {
          Minimum = 0,
          Maximum = 1,
          Value = row.Progress,
          Height = 3,
          Background = new SolidColorBrush(Color.Parse("#22FFFFFF"))
        });
      }
      Grid.SetColumn(now, 2);
      line.Children.Add(now);

      TextBlock next = new TextBlock
      {
        Text = row.NextTitle,
        FontSize = 14,
        Opacity = 0.65,
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center
      };
      Grid.SetColumn(next, 3);
      line.Children.Add(next);

      press.Content = line;
      return press;
    }

    /// <summary>
    /// The buttons the dpad walks. With the list open its rows join the ring, because a list that cannot be
    /// reached without a mouse is not a list a person on a sofa can use.
    /// </summary>
    private List<Control> BuildRing(bool withList)
    {
      List<Control> ring = new List<Control>();
      ring.Add(upButton);
      ring.Add(downButton);
      ring.Add(backChannelButton);
      ring.Add(listButton);
      ring.Add(groupPicker);
      ring.Add(stopButton);
      if (withList)
      {
        foreach (Control child in rowHost.Children)
          ring.Add(child);
      }
      return ring;
    }
  }
}
