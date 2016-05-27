<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="200">

  <Grid DockPanel.Dock="Center" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,-30,0,0">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="450"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <StackPanel Orientation="Vertical" Grid.Row="0" Grid.RowSpan="2" Grid.Column="0" >
      <Label x:Name="ProgramStart" Margin="0,0,0,0"  Color="{ThemeResource TextColor}"
             Content="{Binding Path=StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"/>
      <Label x:Name="ProgramEnd" Content="{Binding Path=EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
             Color="{ThemeResource TextColor}"/>
      <Label x:Name="Channel" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}" Opacity="0.5"/>
      <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" Margin="0,10,0,0"
             Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Top" >
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                                    fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
        </Image.Source>
      </Image>
    </StackPanel>

    <Grid Grid.Row="0" Grid.Column="1" >
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <Label x:Name="Program" Scroll="Auto" FontFamily="DefaultBold" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
             Content="{Binding CurrentProgram.Title}" Margin="10,0,0,0" Grid.Column="0"
             Color="{ThemeResource TextColor}"/>
      <Label x:Name="SeriesLabel" Content="{Binding CurrentProgram.Series}" Opacity="0.5" Grid.Column="1"
             Color="{ThemeResource TextColor}" Margin="10,0,5,0" TextTrimming="WordEllipsis"/>
    </Grid>

    <Label Grid.Row="1" Grid.Column="1" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" TextTrimming="WordEllipsis" Opacity="0.5"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}" Margin="10,0,10,5" />
  </Grid>
</DockPanel>
