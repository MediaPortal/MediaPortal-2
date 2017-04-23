<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  Margin="{ThemeResource MultiDetailsFooterMargin}">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid.Resources>
    <Model x:Key="BackgroundManagerModel" Id="1F4CAEDE-7108-483d-B5C8-18BEC7EC58E5"/>
  </Grid.Resources>

  <Control x:Name="AudioDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:AudioAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemAudioDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="AudioAlbumDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:AudioAlbumAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemAudioAlbumDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="ImageDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:ImageAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemImageDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="MovieDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:MovieAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemMovieDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="MovieCollectionDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:MovieCollectionAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemMovieCollectionDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="SeriesDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:SeriesAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemSeriesDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="SeasonDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:SeasonAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemSeasonDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="EpisodeDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:EpisodeAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemEpisodeDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="PersonDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:PersonAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemPersonDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  <Control x:Name="AllOtherDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" 
         ConverterParameter="{}!(({0}=={VisibilityEnum}.{Visible})||({1}=={VisibilityEnum}.{Visible})||({2}=={VisibilityEnum}.{Visible})||({3}=={VisibilityEnum}.{Visible})||({4}=={VisibilityEnum}.{Visible})||({5}=={VisibilityEnum}.{Visible})||({6}=={VisibilityEnum}.{Visible})||({7}=={VisibilityEnum}.{Visible})||({8}=={VisibilityEnum}.{Visible}))">
        <Binding ElementName="AudioDetails" Path="Visibility" />
        <Binding ElementName="AudioAlbumDetails" Path="Visibility" />
        <Binding ElementName="ImageDetails" Path="Visibility" />
        <Binding ElementName="MovieDetails" Path="Visibility" />
        <Binding ElementName="MovieCollectionDetails" Path="Visibility" />
        <Binding ElementName="SeriesDetails" Path="Visibility" />
        <Binding ElementName="SeasonDetails" Path="Visibility" />
        <Binding ElementName="EpisodeDetails" Path="Visibility" />
        <Binding ElementName="PersonDetails" Path="Visibility" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemOthersDetails.inc"/>
    </Control.TemplateControl>
  </Control>
  
</Grid>
