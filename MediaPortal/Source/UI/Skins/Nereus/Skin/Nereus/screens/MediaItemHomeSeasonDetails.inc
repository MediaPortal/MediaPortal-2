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

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,5,20,-5">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}"
                 Content="{Binding SeriesName}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem,Converter={StaticResource ReferenceToBoolConverter}}"/>
        </Grid>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1">

    <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeasonAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\series.png" Height="25" Width="25" Stretch="Uniform" Margin="2,-1,5,1"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>

            <Label Grid.Column="1" Content="[Media.SeasonRootViewName]" Color="{ThemeResource TextColor}"
                   Opacity="0.6"  VerticalAlignment="Center" FontSize="{ThemeResource SmallerFontSize}"/>
            
            <Label x:Name="SeasonLabel" Grid.Column="2" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}" 
                   VerticalAlignment="Center" Margin="7,0,0,0" Opacity="0.6"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Content="{Binding Season}"/>

            <Label Grid.Column="3" FontSize="{ThemeResource SmallerFontSize}" Content=" | "
                   DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   Color="{ThemeResource TextColor}" VerticalAlignment="Center" Opacity="0.6" />           
            
            <Label Grid.Column="4" FontSize="{ThemeResource SmallerFontSize}" Content="{Binding Episodes}"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                   Margin="0,0,8,0" Color="{ThemeResource TextColor}" VerticalAlignment="Center" Opacity="0.6" />

            <Label Grid.Column="5" FontSize="{ThemeResource SmallerFontSize}" Content="[Media.EpisodeItemsHeader]"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   Margin="0,0,30,0" Color="{ThemeResource TextColor}" VerticalAlignment="Center" Opacity="0.6" />
          </Grid>
        </ControlTemplate>
      </media:SeasonAspectWrapper.Template>
    </media:SeasonAspectWrapper>


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
                   Grid.Column="1" Margin="5,0,30,0" Opacity="0.6" IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
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
