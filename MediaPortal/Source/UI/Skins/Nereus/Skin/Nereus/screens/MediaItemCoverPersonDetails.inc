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
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:PersonAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="MediaItemDescription" Wrap="True" TextTrimming="WordEllipsis" Opacity="0.6" Height="260"
               DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               FontSize="32" Color="{ThemeResource TextColor}" HorizontalContentAlignment="Right"
               Content="{Binding Biography}" Margin="60,4,40,2"/>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
  </media:PersonAspectWrapper>
  
  <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="1" Margin="10,0,10,0" >
    <media:PersonAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Top" Orientation="Vertical">

            <Label VerticalAlignment="Stretch" Content="{Binding PersonName}" TextTrimming="WordEllipsis"
                   Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold" Wrap="True"/>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
              <Image Source="HomeContentIcons\year.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     IsVisible="{Binding Path=BornDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Image Source="HomeContentIcons\year.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              </StackPanel>
            </Grid>

            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
              <Image Source="HomeContentIcons\place.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Label Content="{Binding Origin}" FontSize="{ThemeResource SmallFontSize}" Margin="40,0,0,0"
                     IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}"
                     Color="{ThemeResource TextColorForeground}" TextTrimming="WordEllipsis"/>
            </Grid>

        </StackPanel>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
  </media:PersonAspectWrapper>

</Grid>
