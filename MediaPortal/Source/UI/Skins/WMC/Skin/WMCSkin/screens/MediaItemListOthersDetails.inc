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

  <media:MediaAspectWrapper Grid.Row="0" Margin="0,0,20,0" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
               DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label.Content>
            <Binding Path="Title"/>
          </Label.Content>
        </Label>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="2" Grid.ColumnSpan="2"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Opacity="0.5"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Margin="0,40,5,25" TextTrimming="WordEllipsis"
        Color="{ThemeResource TextColor}"/>

</Grid>
