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
    <ColumnDefinition Width="480"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:MovieCollectionAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" 
                            VerticalAlignment="Center" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="0" Margin="0,0,0,0" >
    <media:MovieCollectionAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemName" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                 Content="{Binding CollectionName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
        </Grid>
      </ControlTemplate>
    </media:MovieCollectionAspectWrapper.Template>
</media:MovieCollectionAspectWrapper>
  
  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource MenuTextBackground}" Margin="10,0,5,35"/>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="0,0,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Movies}" Opacity="1.3"
             Margin="0,0,8,0" Color="{ThemeResource MenuTextBackground}"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.MoviesItemsHeader]"
             IsVisible="{Binding Path=Movies,Converter={StaticResource ReferenceToBoolConverter}}"
             Margin="0,0,8,0" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
    </StackPanel>
  </StackPanel>

  <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                         Grid.Column="0" Grid.Row="0" Grid.RowSpan="2" HorizontalAlignment="Right" VerticalAlignment="Bottom">
    <media:UserDataWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
          <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="85" Height="85" Margin="0,-25,40,0">
            <Image.Source>
              <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
            </Image.Source>
          </Image>
        </Grid>
      </ControlTemplate>
    </media:UserDataWrapper.Template>
  </media:UserDataWrapper>
  
</Grid>
