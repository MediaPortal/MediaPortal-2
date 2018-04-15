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

  <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                            Grid.ColumnSpan="2" Margin="-10,-15,140,0">
    <media:MovieAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemName" Margin="0,0,0,0" TextTrimming="WordEllipsis"
                  VerticalAlignment="Stretch" FontFamily="DefaultBold"
                  Content="{Binding MovieName}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:MovieAspectWrapper.Template>
  </media:MovieAspectWrapper>
  
  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1"
        TextTrimming="WordEllipsis" HorizontalAlignment="Stretch" Opacity="0.6"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" Margin="30,0,35,25"/>

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

      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:MovieAspectWrapper.Template>
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
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>
    </StackPanel>

    <Label x:Name="Genre" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}" Opacity="0.6">
      <Label.Content>
        <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
          <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
          <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
        </MultiBinding>
      </Label.Content>
    </Label>
    
    <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <StackPanel Orientation="Horizontal">
              <Label x:Name="Duration" Color="{ThemeResource TextColor}" Opacity="0.6"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label Content="[Media.Minutes]" Color="{ThemeResource TextColor}" Opacity="0.6"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>

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
