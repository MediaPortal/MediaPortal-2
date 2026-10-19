using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The playback section of the settings: what this machine's player answers to today - its volume and its
  /// mute - on the same view model the film screen drives, so a change here is a change to the film that is
  /// running rather than to a copy of a setting.
  /// </summary>
  /// <remarks>
  /// Built in code rather than XAML because it is five controls and no template, and because its whole content
  /// depends on whether this machine has a player at all - a section that rendered a form over no player would
  /// be a form whose every button lies. What the reference's player configuration offered beyond volume and
  /// mute - renderers, audio devices, passthrough - belongs to a player settings backend this build does not
  /// have yet; the section says so instead of pretending.
  /// </remarks>
  public sealed class PlaybackView : UserControl
  {
    private readonly NowPlayingViewModel? _playback;
    private readonly TextBlock _volumeLine = new TextBlock { FontSize = 18 };
    private readonly TextBlock _muteLine = new TextBlock { FontSize = 18 };

    public PlaybackView(NowPlayingViewModel? playback)
    {
      _playback = playback;

      StackPanel body = new StackPanel { Spacing = 14, Margin = new Avalonia.Thickness(4, 8, 4, 8) };
      body.Children.Add(new TextBlock
      {
        Text = "Playback",
        FontSize = 24,
        FontWeight = FontWeight.SemiBold
      });

      if (playback == null)
      {
        body.Children.Add(new TextBlock
        {
          Text = "This machine has no player, so there is nothing to set here.",
          FontSize = 16,
          Opacity = 0.7,
          TextWrapping = TextWrapping.Wrap
        });
        Content = body;
        return;
      }

      StackPanel volumeRow = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Spacing = 14,
        VerticalAlignment = VerticalAlignment.Center
      };
      Button quieter = new Button { Content = "- 5", FontSize = 16, Padding = new Avalonia.Thickness(16, 6) };
      Button louder = new Button { Content = "+ 5", FontSize = 16, Padding = new Avalonia.Thickness(16, 6) };
      quieter.Click += delegate { playback.SetVolume(playback.Volume - 5); Say(); };
      louder.Click += delegate { playback.SetVolume(playback.Volume + 5); Say(); };
      volumeRow.Children.Add(quieter);
      volumeRow.Children.Add(louder);
      volumeRow.Children.Add(_volumeLine);
      body.Children.Add(volumeRow);

      StackPanel muteRow = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Spacing = 14,
        VerticalAlignment = VerticalAlignment.Center
      };
      Button mute = new Button { Content = "Mute", FontSize = 16, Padding = new Avalonia.Thickness(16, 6) };
      mute.Click += delegate { playback.ToggleMute(); Say(); };
      muteRow.Children.Add(mute);
      muteRow.Children.Add(_muteLine);
      body.Children.Add(muteRow);

      body.Children.Add(new TextBlock
      {
        Text = "Everything else about playback - the renderer, the audio device, passthrough - the reference "
            + "asked the player itself; this player answers to volume and mute until those settings exist.",
        FontSize = 13,
        Opacity = 0.6,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 640,
        HorizontalAlignment = HorizontalAlignment.Left
      });

      Content = body;
      Say();
    }

    private void Say()
    {
      if (_playback == null)
        return;
      _volumeLine.Text = "Volume: " + _playback.Volume + (playback_Muted() ? " (muted)" : "");
      _muteLine.Text = playback_Muted() ? "The player is muted" : "The player is not muted";
    }

    private bool playback_Muted()
    {
      return _playback != null && _playback.Muted;
    }
  }
}