<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="170">
  
  <Grid DockPanel.Dock="Top" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
        DataContext="{Binding Source={StaticResource SlimTvMultiChannelGuide}}">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="280"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Label x:Name="Channel" Grid.Row="0" Grid.Column="0" Scroll="Auto" Margin="25,0,10,5" HorizontalAlignment="Left"
           Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"/>

    <Image x:Name="ChannelLogo" Grid.Column="0" Grid.Row="1" Width="{ThemeResource ChannelLogoLarge}" Margin="25,10,10,45"
           Height="{ThemeResource ChannelLogoLarge}" Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Top">
      <Image.Source>
        <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner" fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
      </Image.Source>
    </Image>

    <!-- Common control for program duration formatting -->
    <Control Style="{StaticResource ProgramDurationControlStyle}" DataContext="{Binding Path=CurrentProgram}" Grid.Column="1" Grid.ColumnSpan="3" />

    <Label x:Name="Program" Grid.Row="0" Grid.Column="4" Scroll="Auto" Margin="15,0,0,0"
        Content="{Binding CurrentProgram.Title}"
        Color="{ThemeResource TextColor}"/>
    <Label x:Name="SeriesLabel" Grid.Column="5" Grid.Row="0" Color="{ThemeResource TextColor}" Margin="5,0,0,0"
           IsVisible="{Binding Path=CurrentProgram.Series, Converter={StaticResource EmptyStringToFalseConverter}}">
      <Label.Content>
        <Binding Path="CurrentProgram.Series" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} ({0})"/>
      </Label.Content>
    </Label>

    <Label Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="5" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" Scroll="Auto" Opacity="0.6"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}" Margin="0,0,10,25" />
  </Grid>
</DockPanel>
