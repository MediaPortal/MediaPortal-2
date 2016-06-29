<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
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
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>

                    <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0" FontFamily="SegoeUIBold"
                        Content="[Media.ImageTitleLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="Title" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                        Content="{Binding Title}" Color="{ThemeResource TextColor}"/>
                    
                    <Label x:Name="YearLabel" Grid.Row="1" Grid.Column="0"
                        Content="[Media.VideoYearLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="Year" Grid.Row="1" Grid.Column="1"
                        Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>

                  </Grid>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>
            
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>        
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraMakeLabel" Grid.Row="0" Grid.Column="0" 
                       Content="[Media.CameraMake]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="CameraMake" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                        Content="{Binding EquipmentMake}"
                        Color="{ThemeResource TextColor}"/>             
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="450"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="CameraModelLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.CameraModel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="CameraModel" Grid.Row="0" Grid.Column="1"
                        Content="{Binding EquipmentModel}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageISOSpeedLabel" Grid.Row="0" Grid.Column="0"
                         Content="[Media.ImageISOSpeedLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageISOSpeed" Grid.Row="0" Grid.Column="1"
                        Content="{Binding ISOSpeedRating}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>            
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageExposureTimeLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageExposureTimeLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageExposureTime" Grid.Row="0" Grid.Column="1"
                        Content="{Binding ExposureTime}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>
                 
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate> 
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageFNumberLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageFNumberLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageFNumber" Grid.Row="0" Grid.Column="1"
                        Content="{Binding FNumber}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageMeteringModeLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageMeteringModeLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageMeteringMode" Grid.Row="0" Grid.Column="1"
                        Content="{Binding MeteringMode}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>

            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">           
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageCountryLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageCountryLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageCountry" Grid.Row="0" Grid.Column="1"
                        Content="{Binding Country}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>


            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="220"/>
                      <ColumnDefinition Width="350"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageStateLabel" Grid.Row="0" Grid.Column="0"
                        Content="[Media.ImageStateLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageState" Grid.Row="0" Grid.Column="1"
                        Content="{Binding State}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>
            
            <!--Resolution-->
            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="200"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                  </Grid.ColumnDefinitions>
                    <Label x:Name="Resolution" Grid.Row="0" Grid.Column="0"
                            Content="Resolution:" Color="{ThemeResource TextColor}" Opacity="0.6"/> 
                   <Label  Grid.Row="0" Grid.Column="1"
                      Content="{Binding AspectWidth}"
                      Color="{ThemeResource TextColor}"/>
                  <Label Content=" x " Grid.Row="0" Grid.Column="2"
                      Color="{ThemeResource TextColor}"/>    
                  <Label Content="{Binding AspectHeight}" Grid.Row="0" Grid.Column="3"
                      Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>



            <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:ImageAspectWrapper.Template>
                <ControlTemplate>  
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="Auto"/>
                      <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="ImageCityLabel" Grid.Row="8" Grid.Column="0"
                        Content="[Media.ImageCityLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
                    <Label x:Name="ImageCity" Grid.Row="8" Grid.Column="1"
                        Content="{Binding City}"
                        Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:ImageAspectWrapper.Template>
            </media:ImageAspectWrapper>       


          </WrapPanel>

</Grid>
