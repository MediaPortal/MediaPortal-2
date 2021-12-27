<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Title" Scroll="Auto" FontSize="{ThemeResource SmallerFontSize}" FontFamily="SegoeUIBold"
               DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Content="{Binding Title}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis"/>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image x:Name="YearIcon" Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
                   IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
            <Label x:Name="Year" FontSize="{ThemeResource SmallerFontSize}" Grid.Column="1" Margin="5,0,30,0" Opacity="0.6"
                   IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>
            
    <!--Resolution-->
    <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:ImageAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image x:Name="ResolutionIcon" Source="HomeContentIcons\resolution.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,5,1"
                   IsVisible="{Binding ElementName=Height, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
            <Label Content="{Binding AspectWidth}" Grid.Column="1" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}" Opacity="0.6"/>
            <Label Content=" x " Grid.Column="2" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding ElementName=Height, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>    
            <Label Content="{Binding AspectHeight}" Grid.Column="3" Color="{ThemeResource TextColor}"
                   IsVisible="{Binding ElementName=Height, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"
                   x:Name="Height" FontSize="{ThemeResource SmallerFontSize}" Opacity="0.6" Margin="0,0,30,0"/>
          </Grid>
        </ControlTemplate>
      </media:ImageAspectWrapper.Template>
    </media:ImageAspectWrapper>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image x:Name="WatchIcon" Source="HomeContentIcons\watch.png" Stretch="Uniform" Height="35" VerticalAlignment="Center" Margin="0,-1,0,1"
                   IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}"/>
            <Label VerticalAlignment="Center" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}"
                   IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}" Grid.Column="1" Margin="5,0,30,0" Opacity="0.6">
              <Label.Content>
                <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
              </Label.Content>
            </Label>
          </Grid>
        </ControlTemplate>
      </media:UserDataWrapper.Template>
    </media:UserDataWrapper>

  </StackPanel>
</Grid>
