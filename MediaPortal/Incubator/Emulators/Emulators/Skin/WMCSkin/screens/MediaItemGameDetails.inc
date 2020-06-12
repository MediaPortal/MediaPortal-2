<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails"
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
    <ColumnDefinition Width="480"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Wrap="True" Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Margin="0,-15,0,0"
    HorizontalAlignment="Stretch" Content="{Binding SimpleTitle}" FontFamily="SegoeUIBold"
    Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" Opacity="1.3" TextTrimming="WordEllipsis"
         HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" 
         IsVisible="{Binding Path=[Emulators.Description],Converter={StaticResource ReferenceToBoolConverter}}"
         Content="{Binding [Emulators.Description]}" Color="{ThemeResource MenuTextBackground}" Margin="10,0,5,35"/>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="0,0,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal">
      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>
              <Label x:Name="Year" Grid.Row="0" Grid.Column="0" HorizontalAlignment="Left"
                  Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
            </Grid>
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>

      <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <emulators:GameAspectWrapper.Template>
          <ControlTemplate>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Column="0" Grid.Row="4" Margin="15,0,0,0">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                               StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}" Maximum="5"
                               IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
                <StarRatingPanel.Value>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                    <Binding Path="Rating"/>
                  </MultiBinding>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource MenuTextBackground}"  Margin="15,0,5,0" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Opacity="1.3" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                     IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>
          </ControlTemplate>
        </emulators:GameAspectWrapper.Template>
      </emulators:GameAspectWrapper>
    </StackPanel>
    
    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="Genre" Grid.Row="0" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"
                   Content="{Binding Genres}" Color="{ThemeResource MenuTextBackground}" />
            <Label x:Name="Platform" Grid.Row="1" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"
                   Content="{Binding Platform}" Color="{ThemeResource MenuTextBackground}"/>
            <Label x:Name="Developer" Grid.Row="2" Content="{Binding Developer}" Opacity="1.3"
                   Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
          </Grid>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>
    
  </StackPanel>

</Grid>
