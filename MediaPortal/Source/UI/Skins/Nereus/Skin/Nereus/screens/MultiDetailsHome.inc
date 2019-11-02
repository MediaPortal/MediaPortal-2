<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,0,0">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid.Resources>
    <Model x:Key="BackgroundManagerModel" Id="1F4CAEDE-7108-483d-B5C8-18BEC7EC58E5"/>
    <Model x:Key="MenuModel" Id="9E9D0CD9-4FDB-4c0f-A0C4-F356E151BDE0"/>
    <Model x:Key="SkinSettingsModel" Id="13961014-16C4-4361-9751-8E2C585EFD44"/>
  </Grid.Resources>

  <Control x:Name="AudioDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:AudioAspect.ASPECT_ID}" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemHomeAudioDetails.inc"/>
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
      <Include Source="screens\MediaItemHomeImageDetails.inc"/>
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
      <Include Source="screens\MediaItemHomeMovieDetails.inc"/>
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
      <Include Source="screens\MediaItemHomeEpisodeDetails.inc"/>
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
      <Include Source="screens\MediaItemHomeSeriesDetails.inc"/>
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
      <Include Source="screens\MediaItemHomeAudioAlbumDetails.inc"/>
    </Control.TemplateControl>
  </Control>

  <Control x:Name="AllOtherDetails">
    <Control.IsVisible>
      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" 
         ConverterParameter="{}!(({0}=={VisibilityEnum}.{Visible})||({1}=={VisibilityEnum}.{Visible})||({2}=={VisibilityEnum}.{Visible})||({3}=={VisibilityEnum}.{Visible})||({4}=={VisibilityEnum}.{Visible})||({5}=={VisibilityEnum}.{Visible}))">
        <Binding ElementName="AudioDetails" Path="Visibility" />
        <Binding ElementName="ImageDetails" Path="Visibility" />
        <Binding ElementName="MovieDetails" Path="Visibility" />
        <Binding ElementName="EpisodeDetails" Path="Visibility" />
        <Binding ElementName="SeriesDetails" Path="Visibility" />
        <Binding ElementName="AudioAlbumDetails" Path="Visibility" />
      </MultiBinding>
    </Control.IsVisible>
    <Control.TemplateControl>
      <Include Source="screens\MediaItemHomeOthersDetails.inc"/>
    </Control.TemplateControl>
  </Control>

  <!-- Show help label only in "Home" workflow state to avoid overlapping with contents in other screens-->
  <Grid>
    <Grid.IsVisible>
      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{} {0} &amp;&amp; {1}">
        <Binding Source="{StaticResource MenuModel}" Path="IsMenuOpen"/>
        <Binding Source="{StaticResource SkinSettingsModel}" Path="![Nereus].EnableHelpTexts"/>
      </MultiBinding>
    </Grid.IsVisible>
    <Image Source="HomeContentIcons\helptext.png" Height="26" Width="26" Stretch="Uniform" Margin="0,10,5,-5"
           VerticalAlignment="Top" HorizontalAlignment="Left"/>
    <Label DataContext="{Binding Source={StaticResource BackgroundManagerModel}, Path=SelectedItem}" Opacity="0.6" Margin="38,5,0,-5"
           Content="{Binding [Help]}" Color="{ThemeResource TextColor}" Wrap="True" FontSize="{ThemeResource SmallerFontSize}"/> 
  </Grid>

</Grid>
