<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,5,20,-5">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}"
                 Content="{Binding Title}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem,Converter={StaticResource ReferenceToBoolConverter}}"/>
        </Grid>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"/>
            <Label x:Name="Year" Grid.Column="1" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Margin="5,0,0,0" Opacity="0.6"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

      <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:VideoStreamAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>

              <Image Source="HomeContentIcons\duration.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="30,-1,0,1"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label x:Name="Duration" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Margin="5,0,0,0"
                     Grid.Column="1" VerticalAlignment="Center">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"
                     FontSize="{ThemeResource SmallerFontSize}" Grid.Column="2" VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Grid.Column="3" Opacity="0.6"/>
            </Grid>
          </ControlTemplate>
        </media:VideoStreamAspectWrapper.Template>
      </media:VideoStreamAspectWrapper>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\watch.png" Stretch="Uniform" Height="35" VerticalAlignment="Center" Margin="30,-1,0,1"/>
            <Label VerticalAlignment="Center" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}"
                   Grid.Column="1" Margin="5,0,0,0" Opacity="0.6">
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
