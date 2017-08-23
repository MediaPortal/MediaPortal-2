<?xml version="1.0" encoding="utf-8"?>
<Grid 
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Width="{DynamicResource Cover_Width}"
  Margin="-25,-20,-77,30">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  
  <Image Grid.Row="0" Grid.Column="0" HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Margin="1" Width="{DynamicResource Cover_Width}" Height="560">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" HorizontalTextureAlignment="Right"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560;False}"
                     FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" />
    </Image.Source>
  </Image> 
</Grid>


