<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:MediaItemAspectToBoolConverter x:Key="MediaItemAspectToBoolConverter" />
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
  <Control x:Name="AllOtherDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}!({0} &amp;&amp; {1} &amp;&amp; {2} &amp;&amp; {3})">
        <Binding ElementName="AudioDetails" Path="IsVisible" />
        <Binding ElementName="ImageDetails" Path="IsVisible" />
        <Binding ElementName="MovieDetails" Path="IsVisible" />
        <Binding ElementName="SeriesDetails" Path="IsVisible" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemOthersDetails.inc"/>
    </Control.TemplateControl>
  </Control>
</Grid>
