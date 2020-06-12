<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  x:Name="MediaItemDetails" Margin="0,0,-20,0"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>

  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="420"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Grid.ColumnSpan="2" Margin="-10,-15,0,0"
    HorizontalAlignment="Stretch" VerticalAlignment="Stretch" TextTrimming="CharacterEllipsis"
    Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
    Content="{Binding SimpleTitle}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" Margin="30,0,35,25"
         TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
         Color="{ThemeResource TextColor}" Opacity="0.6"
         Content="{Binding [Emulators.Description]}"/>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="-10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Year" Grid.Row="0" Grid.Column="0" HorizontalAlignment="Left" Color="{ThemeResource TextColor}"
              Content="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" />
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment ="Stretch"
                      DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,6,0,5"
                        IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="5" VerticalAlignment="Center">
                <StarRatingPanel.Value>
                  <Binding Path="Rating" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}/2"/>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Opacity="0.6" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0">
                <Label.Content>
                  <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <Label x:Name="Genre" Color="{ThemeResource TextColor}" Content="{Binding Genres}" Opacity="0.6" TextTrimming="WordEllipsis"/>
            <Label x:Name="Platform" Color="{ThemeResource TextColor}" Content="{Binding Platform}" Opacity="0.6" TextTrimming="WordEllipsis"/>
            <Label x:Name="Developer" Color="{ThemeResource TextColor}" Content="{Binding Developer}" Opacity="0.6" TextTrimming="WordEllipsis"/>
          </StackPanel>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>

  </StackPanel>

</Grid>
