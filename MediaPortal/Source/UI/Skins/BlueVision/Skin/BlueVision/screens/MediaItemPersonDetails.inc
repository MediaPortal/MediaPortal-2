<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="-10,-15,0,0">

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
            <ColumnDefinition Width="420"/>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>

          <Label x:Name="Title" Grid.Row="0" Grid.Column="0" FontFamily="DefaultBold"
                 Content="{Binding PersonName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
          <StackPanel Orientation="Vertical" Grid.Row="1" Grid.Column="0" Margin="0,0,20,0">
            <StackPanel Orientation="Horizontal">
              <Label Color="{ThemeResource TextColor}"
                     Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              <Label Color="{ThemeResource TextColor}"
                     Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label Color="{ThemeResource TextColor}"
                     Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            </StackPanel>
            <!--Label Content="{Binding Occupation}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=Occupation,Converter={StaticResource ReferenceToBoolConverter}}"/-->
            <Label Content="{Binding Origin}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"/>
          </StackPanel>
          <Label Grid.Row="1" Grid.Column="1" Content="{Binding Biography}" TextTrimming="WordEllipsis" Wrap="True" Opacity="0.6"
                 Color="{ThemeResource TextColor}" Margin="10,0,5,30"/>
        </Grid>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
  </media:PersonAspectWrapper>
</Grid>
