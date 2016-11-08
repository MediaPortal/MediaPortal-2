<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <WrapPanel Orientation="Vertical" Height="190" Width="1900">
            
            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.RowDefinitions>
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
                        Content="{Binding Title}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis"/>
                    
                    <Label x:Name="YearLabel" Grid.Row="1" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                        Content="[Media.VideoYearLabel]" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="Year" Grid.Row="1" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                        Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Color="{ThemeResource TextColor}"/>

                  </Grid>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>
            
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>        
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraMakeLabel" Grid.Row="0" Grid.Column="0" 
                       Content="[Media.CameraMake]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="CameraMake" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                        Content="{Binding EquipmentMake}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>             
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
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraModelLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.CameraModel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="CameraModel" Grid.Row="0" Grid.Column="1"
                        Content="{Binding EquipmentModel}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
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
                 
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate> 
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="230"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageFNumberLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageFNumberLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageFNumber" Grid.Row="0" Grid.Column="1"
                        Content="{Binding FNumber}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
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
                    <Label x:Name="ImageMeteringModeLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageMeteringModeLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageMeteringMode" Grid.Row="0" Grid.Column="1"
                        Content="{Binding MeteringMode}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
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
                    <Label x:Name="ImageCountryLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageCountryLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageCountry" Grid.Row="0" Grid.Column="1"
                        Content="{Binding Country}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
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
                    <Label x:Name="ImageStateLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageStateLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageState" Grid.Row="0" Grid.Column="1"
                        Content="{Binding State}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>
            
            <!--Resolution-->
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                        IsVisible="{Binding Path=AspectHeight,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="230"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                  </Grid.ColumnDefinitions>
                    <Label x:Name="Resolution" Grid.Row="0" Grid.Column="0"
                           Content="Resolution:" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/> 
                   <Label  Grid.Row="0" Grid.Column="1" Content="{Binding AspectWidth}"
                           Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
                  <Label Content=" x " Grid.Row="0" Grid.Column="2" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>    
                  <Label Content="{Binding AspectHeight}" Grid.Row="0" Grid.Column="3" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
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
                      <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageCityLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageCityLabel]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
                    <Label x:Name="ImageCity" Grid.Row="0" Grid.Column="1"
                        Content="{Binding City}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>       

          </WrapPanel>
</Grid>
