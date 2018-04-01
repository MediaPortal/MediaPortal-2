<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
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
    <ColumnDefinition Width="700"/>
  </Grid.ColumnDefinitions>

  <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                            Grid.Column="0" Grid.Row="0" Margin="0,-15,-200,0">
    <media:MovieAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemName" Wrap="True"
            HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
            Content="{Binding Path=MovieName}"
            Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:MovieAspectWrapper.Template>
  </media:MovieAspectWrapper>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="0,0,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal">
      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>
              <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                  Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
            </Grid>
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>

      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Center" >
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" 
                          Margin="20,0,0,0">
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
                      <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                    </TransformGroup>
                  </StarRatingPanel.LayoutTransform>
                </StarRatingPanel>
                <Label x:Name="RatingCountLabel" Margin="15,0,5,0" VerticalAlignment="Center" Opacity="1.3"
                       FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>
    </StackPanel>

      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal">
                <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"
                       Content="{Binding Path=Runtime}" Opacity="1.3"/>
                <Label Content=" " IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.3"/>                
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Image HorizontalAlignment="Left" VerticalAlignment="Top" Stretch="Fill" Width="85" Height="85" Margin="0,5,30,0">
              <Image.Source>
                <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
              </Image.Source>
            </Image>
          </Grid>
        </ControlTemplate>
      </media:UserDataWrapper.Template>
    </media:UserDataWrapper>
    
  </StackPanel>


  
</Grid>
