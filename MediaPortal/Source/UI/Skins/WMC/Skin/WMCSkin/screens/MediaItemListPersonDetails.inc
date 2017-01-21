<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                           HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:PersonAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="540"/>
            <ColumnDefinition Width="350"/>
          </Grid.ColumnDefinitions>

          <Image Grid.Column="1" HorizontalAlignment="Right" Stretch="Fill"
                 Margin="0,10,10,0" Height="320" Width="320">
            <Image.Source>
              <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                  Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
            </Image.Source>
            <Image.OpacityMask>
              <VisualBrush>
                <VisualBrush.Visual>
                  <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
                </VisualBrush.Visual>
              </VisualBrush>
            </Image.OpacityMask>
          </Image>

          <StackPanel Orientation="Vertical" Grid.Row="0" Grid.Column="0" Margin="0,0,20,0">
            <Label VerticalAlignment="Stretch" Content="{Binding PersonName}" TextTrimming="WordEllipsis"
                   Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold" Wrap="True"/>
            <StackPanel Orientation="Horizontal">
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            </StackPanel>
            <!--Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3">
              <Label.Content>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" 
                              ConverterParameter="{}{0} == {1} ? {6} : ({0} == {2} ? {7} : ({0} == {3} ? {8} : ({0} == {4} ? {9} : ({0} == {5} ? {10}))))">
                  <Binding Path="Occupation"/>
                  <Binding Source="ARTIST"/>
                  <Binding Source="ACTOR"/>
                  <Binding Source="WRITER"/>
                  <Binding Source="DIRECTOR"/>
                  <Binding Source="COMPOSER"/>
                  <Binding Source="[Media.ArtistOccupationName]"/>
                  <Binding Source="[Media.ActorOccupationName]"/>
                  <Binding Source="[Media.WriterOccupationName]"/>
                  <Binding Source="[Media.DirectorOccupationName]"/>
                  <Binding Source="[Media.ComposerOccupationName]"/>
                </MultiBinding>
              </Label.Content>
            </Label-->
            <Label Content="{Binding Origin}" FontSize="{ThemeResource SmallFontSize}"
                   Color="{ThemeResource MenuTextBackground}" Opacity="1.3" TextTrimming="WordEllipsis"/>
          </StackPanel>

          <Label x:Name="MediaItemDescription"
                 Grid.ColumnSpan="2" Grid.Row="1" Margin="0,30,10,10" Height="440"
                 Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}"
                 VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Biography}"
                 Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
        </Grid>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
</media:PersonAspectWrapper>
      
</Grid>
