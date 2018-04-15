<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
 
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  
  <Grid Grid.Column="1" Margin="20,30,20,15" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="180"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                           Grid.Column="1" Grid.Row="0" Grid.RowSpan="2" HorizontalAlignment="Stretch" VerticalAlignment="Top">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,0,0,0" >
              <Image.Source>
                <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
              </Image.Source>
            </Image>
          </Grid>
        </ControlTemplate>
      </media:UserDataWrapper.Template>
    </media:UserDataWrapper>
    
    <StackPanel Orientation="Horizontal" Grid.Row="1" Grid.Column="0">
      <Label FontSize="{ThemeResource NormalFontSize}" Content="[Media.MoviesItemsHeader]"
             Margin="0,0,8,0" Color="{ThemeResource EmphasisTextColor}"/>
      <Label FontSize="{ThemeResource NormalFontSize}" Content=": "
       Margin="0,0,8,0" Color="{ThemeResource EmphasisTextColor}"/>
    </StackPanel>
    <Label Grid.Row="1" Grid.Column="1" FontSize="{ThemeResource NormalFontSize}" Content="{Binding Movies}"
           Margin="0,0,8,0" Color="{ThemeResource TextColor}" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"/>

    <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="2" TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource NormalFontSize}"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" Margin="0,0,10,10"/>
  </Grid>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:MovieCollectionAspectWrapper 
      MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MovieCollectionAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="MediaItemName" FontSize="{ThemeResource NormalFontSize}"
                   Content="{Binding CollectionName}" Color="Black"/>
          </Grid>
        </ControlTemplate>
      </media:MovieCollectionAspectWrapper.Template>
    </media:MovieCollectionAspectWrapper>
  </Grid>
  
  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30" 
         Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
