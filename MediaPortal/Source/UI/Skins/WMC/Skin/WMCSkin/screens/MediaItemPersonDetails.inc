<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <WrapPanel Orientation="Vertical" Height="190" Width="1900">

    <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.RowDefinitions>
                      <RowDefinition Height="Auto"/>
                      <RowDefinition Height="Auto"/>
                      <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>

                    <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageTitleLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="Title" Grid.Row="0" Grid.Column="1" Scroll="Auto" FontSize="{ThemeResource SmallFontSize}"
                        Content="{Binding PersonName}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis"/>
                    
                    <Label x:Name="BirthLabel" Grid.Row="1" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                           Content="[Media.VideoYearLabel]" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="BirthYear" Grid.Row="1" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                            Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
                    <Label x:Name="DeathLabel" Grid.Row="2" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                           Content="[Media.VideoYearLabel]" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="DeathYear" Grid.Row="2" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                           Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>

                  </Grid>
                </ControlTemplate>
              </media:PersonAspectWrapper.Template>
          </media:PersonAspectWrapper>
            
            <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:PersonAspectWrapper.Template>
                <ControlTemplate>        
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraMakeLabel" Grid.Row="0" Grid.Column="0" 
                       Content="[Media.CameraMake]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="CameraMake" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                        Content="{Binding occupation}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>             
                  </Grid>
                </ControlTemplate>
              </media:PersonAspectWrapper.Template>
            </media:PersonAspectWrapper>

    <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraModelLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.CameraModel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="CameraModel" Grid.Row="0" Grid.Column="1"
                        Content="{Binding biography}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:PersonAspectWrapper.Template>
            </media:PersonAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageISOSpeedLabel" Grid.Row="0" Grid.Column="0"
                         Content="[Media.ImageISOSpeedLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageISOSpeed" Grid.Row="0" Grid.Column="1"
                        Content="{Binding ISOSpeedRating}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>            
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageExposureTimeLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageExposureTimeLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageExposureTime" Grid.Row="0" Grid.Column="1"
                        Content="{Binding ExposureTime}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>
                 
          
        </WrapPanel>
</Grid>
