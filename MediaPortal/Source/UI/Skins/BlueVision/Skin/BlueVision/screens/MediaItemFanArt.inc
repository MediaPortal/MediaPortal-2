<?xml version="1.0" encoding="utf-8"?>
<Grid Width="830"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails"
  Margin="-20,10,-20,20">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.OpacityMask>
    <VisualBrush Visual="{Binding ElementName=mask}"/>
  </Grid.OpacityMask>
  
  <Rectangle x:Name="mask" Fill="Black" RadiusY="15" RadiusX="15" VerticalAlignment="Center" HorizontalAlignment="Center"/>

  <Image x:Name="thumb" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="0,0,0,0">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                      Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=FanArt;810;560;False}"
                      FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" />
    </Image.Source>
  </Image>
</Grid>


