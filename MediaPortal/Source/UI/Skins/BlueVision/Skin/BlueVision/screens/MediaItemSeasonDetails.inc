<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="420"/>
  </Grid.ColumnDefinitions>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.Row="0" Margin="-10,-15,0,0">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
          <StackPanel Orientation="Horizontal"
                      DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold">
              <Label.Content>
                <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} "/>
              </Label.Content>
            </Label>
          </StackPanel>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.Row="1" Margin="-10,0,35,25">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemDescription" Wrap="True"
                 TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
                 Content="{Binding Description}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <StackPanel Grid.Column="1" Grid.Row="1" Margin="10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:SeasonAspectWrapper.Template>
        <ControlTemplate>
            <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" Content="[Media.SeasonRootViewName]"/>
              <Label x:Name="SeasonNumber" Color="{ThemeResource TextColor}">
                <Label.Content>
                  <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} {0}"/>
                </Label.Content>
              </Label>
            </StackPanel>
        </ControlTemplate>
      </media:SeasonAspectWrapper.Template>
    </media:SeasonAspectWrapper>
   
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <DockPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" LastChildFill="False">
            <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                        HorizontalAlignment="Left" VerticalAlignment="Center">
              <Label Content="{Binding Episodes}" Margin="0,0,8,0" Color="{ThemeResource TextColor}"/>
              <Label Content="[Media.EpisodeItemsHeader]" Margin="0,0,8,0" Color="{ThemeResource TextColor}"
                     IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"/>
            </StackPanel>
            <Image DockPanel.Dock="Right" HorizontalAlignment="Right" VerticalAlignment="Center"
                   Stretch="Fill" Width="37" Height="24" Margin="10,0,10,0">
              <Image.Source>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                  <Binding Path="PlayCount"/>
                  <Binding Source=""/>
                  <Binding Source="unwatched_icon.png"/>
                </MultiBinding>
              </Image.Source>
            </Image>
          </DockPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeasonAspectWrapper.Template>
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
            <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Opacity="0.4" VerticalAlignment="Center"
                   FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
              </Label.Content>
            </Label>
          </StackPanel>
        </ControlTemplate>
      </media:SeasonAspectWrapper.Template>
    </media:SeasonAspectWrapper>

  </StackPanel>
</Grid>
