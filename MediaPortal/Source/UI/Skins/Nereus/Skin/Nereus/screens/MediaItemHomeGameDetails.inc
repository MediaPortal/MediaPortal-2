<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <Grid Margin="0,5,20,-5"
        IsVisible="{Binding ElementName=GameName, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
    <Label x:Name="GameName" Content="{Binding SimpleTitle}"
           FontSize="{ThemeResource SmallerFontSize}" Wrap="True" TextTrimming="WordEllipsis"
           Color="{ThemeResource TextColor}" HorizontalAlignment="Stretch" />
  </Grid>

  <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,10,0">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Image x:Name="YearIcon" Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
             IsVisible="{Binding Path=Year,Converter={StaticResource ReferenceToBoolConverter}}"/>
      <Label x:Name="Year" Grid.Column="1" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
             IsVisible="{Binding Path=Year,Converter={StaticResource ReferenceToBoolConverter}}"
             Content="{Binding Year}" Margin="5,0,30,0" Opacity="0.6"/>
    </Grid>

    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Image x:Name="RatingIcon" Source="HomeContentIcons\Star.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
             IsVisible="{Binding ElementName=RatingCountLabel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
      <Label x:Name="RatingCountLabel" Margin="5,0,30,0" VerticalAlignment="Center" Grid.Column="1"
             FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
             IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
        <Label.Content>
          <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F1}"/>
        </Label.Content>
      </Label>
    </Grid>

    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Image x:Name="PlatformIcon" Source="HomeContentIcons\Game.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-2,0,2"
             IsVisible="{Binding ElementName=PlatformLabel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
      <Label x:Name="PlatformLabel" Margin="5,0,30,0" VerticalAlignment="Center" Grid.Column="1" Content="{Binding Platform}"
             FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
             IsVisible="{Binding Path=Platform,Converter={StaticResource ReferenceToBoolConverter}}">
      </Label>
    </Grid>

    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Image x:Name="DeveloperIcon" Source="HomeContentIcons\Brand.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
             IsVisible="{Binding ElementName=DeveloperLabel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
      <Label x:Name="DeveloperLabel" Margin="5,0,30,0" VerticalAlignment="Center" Grid.Column="1" Content="{Binding Developer}"
             FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
             IsVisible="{Binding Path=Developer,Converter={StaticResource ReferenceToBoolConverter}}">
      </Label>
    </Grid>

  </StackPanel>

</Grid>