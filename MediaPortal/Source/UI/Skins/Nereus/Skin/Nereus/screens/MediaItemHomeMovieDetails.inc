<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
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
  
  <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,5,20,-5">
    <media:MovieAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              IsVisible="{Binding ElementName=MediaItemName, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
          <Label x:Name="MediaItemName" Wrap="True" HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallerFontSize}"
                 Content="{Binding Path=MovieName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
        </Grid>
      </ControlTemplate>
    </media:MovieAspectWrapper.Template>
  </media:MovieAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1">
    
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" IsVisible="{Binding ElementName=YearIcon, Path=!HasImage}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.Margin>
              <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                <Binding ElementName="YearIcon" Path="HasImage"/>
                <Binding Source="0,0,30,0"/>
                <Binding Source="0,0,0,0"/>
              </MultiBinding>
            </Grid.Margin>
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image x:Name="YearIcon" Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
                   IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
            <Label x:Name="Year" Grid.Column="1" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Margin="5,0,0,0" Opacity="0.6"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" IsVisible="{Binding ElementName=RatingIcon, Path=!HasImage}">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Grid.Margin>
              <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                <Binding ElementName="RatingIcon" Path="HasImage"/>
                <Binding Source="0,0,30,0"/>
                <Binding Source="0,0,0,0"/>
              </MultiBinding>
            </Grid.Margin>
            <Image x:Name="RatingIcon" Source="HomeContentIcons\Star.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
                   IsVisible="{Binding ElementName=RatingCountLabel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
            <Label x:Name="RatingCountLabel" Margin="5,0,0,0" VerticalAlignment="Center" Grid.Column="1"
                   FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F1}"/>
              </Label.Content>
            </Label>
          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>

    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MovieAspectWrapper.Template>
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
            </Grid.ColumnDefinitions>

            <Image x:Name="DurationIcon" Source="HomeContentIcons\duration.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"
                   IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"/>
            <Label x:Name="Duration" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" Margin="5,0,0,0"
                   Content="{Binding Path=Runtime}" Grid.Column="1" VerticalAlignment="Center"/>
            <Label Content=" " IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"
                   FontSize="{ThemeResource SmallerFontSize}" Grid.Column="2" VerticalAlignment="Center"/>
            <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                   IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" Grid.Column="3" Opacity="0.6" Margin="0,0,30,0"/>
          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>

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
