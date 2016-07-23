<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,30,30,-200">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="540"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" Grid.RowSpan="2" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Uniform" Height="280">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" HorizontalTextureAlignment="Right" VerticalTextureAlignment="Top"
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

  <Label x:Name="MediaItemName" Grid.Row="0" FontFamily="SegoeUIBold"
          HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
          Margin="0,0,20,0" Wrap="True" FontSize="{ThemeResource SmallFontSize}"
          Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
          Color="{ThemeResource TextColor}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="2" Grid.ColumnSpan="2"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Opacity="0.5"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Margin="0,40,5,25" TextTrimming="WordEllipsis"
        Color="{ThemeResource TextColor}"/>

</Grid>
