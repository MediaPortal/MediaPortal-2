<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="700"/>
  </Grid.ColumnDefinitions>

  <StackPanel Orientation="Vertical" Margin="0,-15,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <StackPanel Orientation="Vertical">
              <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                     Content="{Binding Title}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,-200,0"
                     IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                     DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
                     Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}"/>
            </StackPanel>
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>

      <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:VideoStreamAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal">
                <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                      <Binding Path="Duration"/>
                    </MultiBinding>
                  </Label.Content>
                </Label>
                <Label Content=" min." FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:VideoStreamAspectWrapper.Template>
      </media:VideoStreamAspectWrapper>

      <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" HorizontalAlignment="Left">
        <media:UserDataWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image HorizontalAlignment="Left" VerticalAlignment="Center" Stretch="Fill" Width="85" Height="85" Margin="0,10,0,0">
                <Image.Source>
                  <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                </Image.Source>
              </Image>
            </Grid>
          </ControlTemplate>
        </media:UserDataWrapper.Template>
      </media:UserDataWrapper>

  </StackPanel>
    
</Grid>
