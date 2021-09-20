<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

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

  <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,5,20,-5">
    <media:PersonAspectWrapper.Template>
      <ControlTemplate>
          <Label Content="{Binding PersonName}" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" HorizontalAlignment="Stretch"/>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
  </media:PersonAspectWrapper>

    <media:PersonAspectWrapper Grid.Row="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal">
            
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>
              <Image x:Name="BornIcon" Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" Width="25" 
                     HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,-1,0,1"
                     IsVisible="{Binding Path=BornDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Image x:Name="DeathIcon" Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" Width="25"
                     HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,-1,0,1"
                     IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label Grid.Column="1" Opacity="0.6" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Margin="5,0,30,0"
                     IsVisible="{Binding Path=BornDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              <Label Grid.Column="2" Opacity="0.6" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}"
                     Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label Grid.Column="3" Opacity="0.6" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Margin="5,0,30,0"
                     IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            </Grid>

            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>
              <Image Source="HomeContentIcons\place.png" Height="26" Width="26" Stretch="Uniform"
                     IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Label Grid.Column="1" Content="{Binding Origin}" FontSize="{ThemeResource SmallerFontSize}" Margin="5,0,30,0"
                     IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"
                     Color="{ThemeResource TextColor}" Opacity="0.6"/>
            </Grid>
            
          </StackPanel>      
        </ControlTemplate>
      </media:PersonAspectWrapper.Template>
    </media:PersonAspectWrapper>

     


    
</Grid>
