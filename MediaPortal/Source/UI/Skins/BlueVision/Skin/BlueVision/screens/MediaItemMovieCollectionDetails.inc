<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="420"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:MovieCollectionAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                      Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Margin="-10,-15,140,0">
    <media:MovieCollectionAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemName" FontFamily="DefaultBold"
                 Content="{Binding CollectionName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
        </Grid>
      </ControlTemplate>
    </media:MovieCollectionAspectWrapper.Template>
  </media:MovieCollectionAspectWrapper>
  
  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" Opacity="0.6"
        TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" Margin="30,0,35,25"/>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="-10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <Label Content="{Binding Movies}" Opacity="0.6"
             Margin="0,0,8,0" Color="{ThemeResource TextColor}"/>
      <Label Content="[Media.MoviesItemsHeader]" Opacity="0.6"
             IsVisible="{Binding Path=Movies,Converter={StaticResource ReferenceToBoolConverter}}"
             Margin="0,0,8,0" Color="{ThemeResource TextColor}"/>
    </StackPanel>
    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                   HorizontalAlignment="Left" VerticalAlignment="Center">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                VerticalAlignment="Center" HorizontalAlignment="Left" Margin="0,5,0,5"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <ProgressBar Height="12" Width="120" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,10,0"
                         Value="{Binding PlayPercentage}" Style="{ThemeResource PercentageProgressBarStyle}"/>
            <Label Grid.Column="1" Color="{ThemeResource TextColor}" Opacity="0.6" VerticalAlignment="Center"
                   FontSize="{ThemeResource SmallerFontSize}" Margin="0,0,0,5">
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
