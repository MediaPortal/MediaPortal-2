<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

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
            <ColumnDefinition Width="480"/>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          
          <Label x:Name="Title" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                 Content="{Binding PersonName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>          
          <StackPanel Orientation="Vertical" Grid.Row="1" Grid.Column="0" Margin="0,0,20,0">
            <StackPanel Orientation="Horizontal">
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                     Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            </StackPanel>
            <!--Label Content="{Binding Occupation}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
                   IsVisible="{Binding Path=Occupation,Converter={StaticResource ReferenceToBoolConverter}}"/-->
            <Label Content="{Binding Origin}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
                   IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"/>
          </StackPanel>   
          <Label Grid.Row="1" Grid.Column="1" Content="{Binding Biography}" TextTrimming="WordEllipsis" Wrap="True" Opacity="1.3"
                 FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Margin="10,0,5,25"/>
        </Grid>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
  </media:PersonAspectWrapper>
</Grid>
