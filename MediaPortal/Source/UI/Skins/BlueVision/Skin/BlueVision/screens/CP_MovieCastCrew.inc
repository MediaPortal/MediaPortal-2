<?xml version="1.0" encoding="utf-8"?>
<Grid 
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:mp_special_controls="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Controls;assembly=SkinEngine">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Row="0" Grid.Column="0" >
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="200"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

      <Label Grid.Row="0" Grid.Column="0" Margin="20,20,0,10" x:Name="DirectorsLabel" Content="Director(s): " Color="{ThemeResource TextColor}"/>
      <Label Grid.Row="0" Grid.Column="1" Margin="20,20,20,10" x:Name="Directors" Content="{Binding VideoDirectors}" Wrap="True" Color="{ThemeResource TextColor}"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Scroll="Auto"/>
    
      <Label Grid.Row="1" Grid.Column="0" Margin="20,20,0,10" x:Name="WritersLabel" Content="Writer(s): " Color="{ThemeResource TextColor}"/>
      <Label Grid.Row="1" Grid.Column="1" Margin="20,20,20,10" x:Name="Writers" Content="{Binding VideoWriters}" Wrap="True" Color="{ThemeResource TextColor}"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Scroll="Auto"/>

      <Label Grid.Row="2" Grid.Column="0" Margin="20,20,0,10" x:Name="ArtistsLabel" Content="Artist(s): " Color="{ThemeResource TextColor}"/>
      <Label Grid.Row="2" Grid.Column="1" Margin="20,20,20,10" x:Name="Artists" Content="{Binding VideoArtists}" Wrap="True" Color="{ThemeResource TextColor}"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Scroll="Auto"/>

      <Label Grid.Row="3" Grid.Column="0" Margin="20,20,0,20" x:Name="ActorsLabel" Content="Actor(s): " Color="{ThemeResource TextColor}"/>
      <Label Grid.Row="3" Grid.Column="1" Margin="20,20,20,20" x:Name="Actors" Content="{Binding VideoActors}" Wrap="True" Color="{ThemeResource TextColor}" Scroll="Auto"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>

  </Grid>
</Grid>
