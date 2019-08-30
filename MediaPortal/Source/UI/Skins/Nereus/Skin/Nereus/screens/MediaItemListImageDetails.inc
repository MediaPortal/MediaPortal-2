<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" Margin="0,320,20,-60"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <StackPanel Orientation="Vertical" Margin="0">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=Title, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Label x:Name="Title" Scroll="Auto" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold"
                   Content="{Binding Title}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis" Margin="0,0,0,10"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Year" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Margin="40,0,0,0" />
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:ImageAspectWrapper.Template>
        <ControlTemplate>
        <StackPanel>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=CameraMake, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\brand.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="CameraMake" Grid.Column="1" Scroll="Auto" Margin="40,0,0,0"
                   Content="{Binding EquipmentMake}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=CameraModel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\cameramodel.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="CameraModel" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding EquipmentModel}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageISOSpeed, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\iso.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageISOSpeed" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding ISOSpeedRating}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageExposureTime, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\exposure.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageExposureTime" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding ExposureTime}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageFNumber, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\fnumber.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageFNumber" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding FNumber}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageMeteringMode, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\metering.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageMeteringMode" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding MeteringMode}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageCountry, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\place.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageCountry" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding Country}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageState, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\place.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageState" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding State}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=AspectHeight, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="40"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image Source="HomeContentIcons\resolution.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label Grid.Row="0" Grid.Column="1" Content="{Binding AspectWidth}"
                   Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"/>
            <Label Content=" x " Grid.Row="0" Grid.Column="2" FontSize="{ThemeResource SmallFontSize}" 
                   Color="{ThemeResource TextColorForeground}"/>
            <Label Content="{Binding AspectHeight}" Grid.Row="0" Grid.Column="3" Color="{ThemeResource TextColorForeground}" 
                   x:Name="AspectHeight" FontSize="{ThemeResource SmallFontSize}"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=ImageCity, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\place.png" Height="27" Width="27" Stretch="Uniform" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ImageCity" Grid.Column="1" Margin="40,0,0,0"
                   Content="{Binding City}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"/>
          </Grid>

          </StackPanel>

        </ControlTemplate>
      </media:ImageAspectWrapper.Template>
    </media:ImageAspectWrapper>

  </StackPanel>

</Grid>
