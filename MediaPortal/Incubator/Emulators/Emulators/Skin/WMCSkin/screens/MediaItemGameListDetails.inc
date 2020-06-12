<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="90,0,30,0"
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
    <ColumnDefinition Width="590"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch"
         Margin="0,10,10,0" Width="380" Height="380">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <Image Grid.Row="0" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Uniform"
       Margin="0,10,10,0">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            HorizontalTextureAlignment="Right" VerticalTextureAlignment="Top"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520;False}"/>
      </Image.Source>
      <Image.OpacityMask>
        <VisualBrush>
          <VisualBrush.Visual>
            <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
          </VisualBrush.Visual>
        </VisualBrush>
      </Image.OpacityMask>
    </Image>
  </Grid>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="1" Opacity="1.3" TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding [Emulators.Description]}" IsVisible="{Binding Path=[Emulators.Description],Converter={StaticResource ReferenceToBoolConverter}}"
        Color="{ThemeResource MenuTextBackground}" Margin="0,30,10,10"/>
  
  <emulators:GameAspectWrapper Grid.Column="0" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                               MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <emulators:GameAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Margin="0,0,10,0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="GameName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding GameName}" FontSize="{ThemeResource SmallFontSize}" Wrap="True"
                 VerticalAlignment="Stretch" FontFamily="SegoeUIBold"
                 Color="{ThemeResource TextColor}"/>
          <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,0,0,5" VerticalAlignment="Center">
            <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True"
                             StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}" Maximum="5"
                             IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
              <StarRatingPanel.Value>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                  <Binding Path="Rating"/>
                </MultiBinding>
              </StarRatingPanel.Value>
              <StarRatingPanel.LayoutTransform>
                <TransformGroup>
                  <ScaleTransform ScaleX="1.65" ScaleY="1.5"/>
                </TransformGroup>
              </StarRatingPanel.LayoutTransform>
            </StarRatingPanel>
            <Label x:Name="RatingCountLabel" Color="{ThemeResource MenuTextBackground}" Opacity="1.3" VerticalAlignment="Center"
                   IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}"
                   FontSize="{ThemeResource SmallerFontSize}" Margin="8,0,0,5">
              <Label.Content>
                <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
              </Label.Content>
            </Label>
          </StackPanel>
          <Label x:Name="Year" Content="{Binding Year}"
                 Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
          <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"
                 Content="{Binding Genres}" Color="{ThemeResource MenuTextBackground}" />
          <Label x:Name="Platform" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3"
                 Content="{Binding Platform}" Color="{ThemeResource MenuTextBackground}"/>
          <Label x:Name="Developer" Content="{Binding Developer}" Opacity="1.3"
                 Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
        </StackPanel>
      </ControlTemplate>
    </emulators:GameAspectWrapper.Template>
  </emulators:GameAspectWrapper>




</Grid>