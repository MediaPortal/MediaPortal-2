<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="2" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0" Grid.Column="0" Margin="-10,0,0,0">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>

          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold" Grid.Column="0" Grid.Row="0"
                 Content="{Binding SeriesName}" />

          <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1"
                Scroll="Auto" ScrollSpeed="5" ScrollDelay="5" HorizontalAlignment="Stretch"
                Content="{Binding Description}"
                Color="{ThemeResource TextColor}" Margin="0,10,0,0"/>
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

</Grid>
