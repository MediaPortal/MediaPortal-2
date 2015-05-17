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

  <!-- SHADOW -->
  <Grid Margin="-3.5" IsVisible="{Binding ElementName=thumb, Path=HasImage}">
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="22"/>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="22"/>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
      <RowDefinition Height="22"  />
      <RowDefinition Height="*"  />
      <RowDefinition Height="22"  />
    </Grid.RowDefinitions>
    <Image Grid.Row="0" Grid.Column="0" Stretch="Fill" Margin="0" Opacity="0.9" Source="shadow_00.png" />
    <Image Grid.Row="1" Grid.Column="0" Stretch="Fill" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Margin="0" Opacity="0.9" Source="shadow_10.png" />
    <Image Grid.Row="2" Grid.Column="0" Stretch="Fill" Margin="0" Opacity="0.9" Source="shadow_20.png" />
    <Image Grid.Row="0" Grid.Column="1" Stretch="Fill" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Margin="0" Opacity="0.9" Source="shadow_01.png" />
    <Image Grid.Row="1" Grid.Column="1" Stretch="Fill" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Margin="0" Opacity="0.9" Source="shadow_11.png" />
    <Image Grid.Row="2" Grid.Column="1" Stretch="Fill" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Margin="0" Opacity="0.9" Source="shadow_21.png" />
    <Image Grid.Row="0" Grid.Column="2" Stretch="Fill" Margin="0" Opacity="0.9" Source="shadow_02.png" />
    <Image Grid.Row="1" Grid.Column="2" Stretch="Fill" VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Margin="0" Opacity="0.9" Source="shadow_12.png" />
    <Image Grid.Row="2" Grid.Column="2" Stretch="Fill" Margin="0" Opacity="0.9" Source="shadow_22.png" />
  </Grid>
  <!-- SHADOW -->
  
  <Grid>
    <Rectangle x:Name="mask" Fill="Black" RadiusY="15" RadiusX="15" VerticalAlignment="Center" HorizontalAlignment="Center"/>
    <Grid.OpacityMask>
      <VisualBrush Visual="{Binding ElementName=mask}"/>
    </Grid.OpacityMask>
    <Image x:Name="thumb" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="0,0,0,0" FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
           Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=FanArt;810;560}">
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


