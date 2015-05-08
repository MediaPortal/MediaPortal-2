<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails"
  Margin="-20,10,-15,15">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>

    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Rectangle x:Name="mask" Fill="Black" RadiusY="15" RadiusX="15" VerticalAlignment="Center" HorizontalAlignment="Center"/>
  <Grid Grid.Row="1" Grid.Column="1">
    <Grid.OpacityMask>
      <VisualBrush Visual="{Binding ElementName=mask}"/>
    </Grid.OpacityMask>
    <Image x:Name="thumb" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="0,0,0,0"
           Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=FanArt;810;560}">
    </Image>
    <Image x:Name="fallback" Stretch="UniformToFill" HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,0,0,0"
           Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" IsVisible="{Binding ElementName=thumb, Path=!HasImage}">
    </Image>
  </Grid>
  </Grid>


  <!-- Darkened band -->
  <!--Rectangle Grid.Row="1" Grid.Column="0" Fill="#FF000000" Opacity="0.749"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,0,0"
             IsVisible="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SelectedItem,Converter={StaticResource ReferenceToBoolConverter}}"/-->

  <!-- Item label -->
  <!--Label Grid.Row="1" Grid.Column="0" x:Name="ItemLabel" HorizontalAlignment="Center" VerticalAlignment="Center"  Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
         Color="{ThemeResource TextColor}"/-->


