<?xml version="1.0" encoding="utf-8"?>
<Grid Background="Red" 
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="-90,-720,30,30">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="540"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Uniform" Margin="0,10,10,0" Height="280">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>

  <Label x:Name="MediaItemName" Grid.Row="0"
          HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
          Margin="0,0,0,0" Wrap="True"
          Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
          Color="{ThemeResource TextColor}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1"
        Scroll="Auto" ScrollSpeed="5" ScrollDelay="5"
        HorizontalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Margin="0,0,5,25"
        Color="{ThemeResource TextColor}"/>

</Grid>
