<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <StackPanel Orientation="Vertical">
            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <StackPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Label x:Name="Title" Scroll="Auto" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                        Content="{Binding Title}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis"/>
                    <Label x:Name="Year" FontSize="{ThemeResource SmallFontSize}"
                        Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Color="{ThemeResource TextColor}"/>
                  </StackPanel>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>
            
            <!--Resolution-->
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                        IsVisible="{Binding Path=AspectHeight,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                  </Grid.ColumnDefinitions>
                   <Label  Content="{Binding AspectWidth}"
                           Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"/>
                  <Label Content=" x " Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>    
                  <Label Content="{Binding AspectHeight}" Grid.Column="2" Color="{ThemeResource MenuTextBackground}" 
                         FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>    

          </StackPanel>
</Grid>
