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

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Margin="-10,-15,140,0">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
                 Content="{Binding SeriesName}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"/>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>
  
  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="1" Grid.Row="1" Margin="30,0,35,25">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Label x:Name="MediaItemDescription" Wrap="True"
                 TextTrimming="WordEllipsis" HorizontalAlignment="Stretch" Opacity="0.6"
                 Content="{Binding Description}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="-10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <StackPanel Orientation="Horizontal">
      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <DockPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" LastChildFill="True">
              <Label x:Name="Year" HorizontalAlignment="Left" Margin="0,0,10,0" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
                     Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            </DockPanel>
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>
      <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:SeriesAspectWrapper.Template>
          <ControlTemplate>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,6,0,5"
                        DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="5" VerticalAlignment="Center"
                               IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <StarRatingPanel.Value>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                    <Binding Path="TotalRating"/>
                  </MultiBinding>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Opacity="0.6" VerticalAlignment="Center"
                     IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>
          </ControlTemplate>
        </media:SeriesAspectWrapper.Template>
      </media:SeriesAspectWrapper>
    </StackPanel>

    <Label x:Name="Genre" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}" Opacity="0.6">
      <Label.Content>
        <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
          <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
          <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
        </MultiBinding>
      </Label.Content>
    </Label> 
   
    <StackPanel Orientation="Vertical"
                DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <StackPanel Orientation="Horizontal">
        <Label Content="{Binding Seasons}" Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"/>
        <Label Content="[Media.SeasonItemsHeader]" Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"
               IsVisible="{Binding Path=Seasons,Converter={StaticResource ReferenceToBoolConverter}}"/>
      </StackPanel>
      <StackPanel Orientation="Horizontal">
        <Label Content="{Binding Episodes}" Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"/>
        <Label Content="[Media.EpisodeItemsHeader]" Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"
               IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"/>
      </StackPanel>
    </StackPanel>
  </StackPanel>

  <media:UserDataWrapper Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                         HorizontalAlignment="Right" VerticalAlignment="Center">
    <media:UserDataWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              VerticalAlignment="Center" HorizontalAlignment="Right" Margin="0,-10,0,0"
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
                 IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}"
                 FontSize="{ThemeResource SmallerFontSize}" Margin="0,0,0,5">
            <Label.Content>
              <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
            </Label.Content>
          </Label>
        </Grid>
      </ControlTemplate>
    </media:UserDataWrapper.Template>
  </media:UserDataWrapper>

</Grid>
