<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" Margin="0,390,20,-60"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="600"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Row="1" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Bottom" Width="294" Height="428" Margin="0,-30,10,30">
    <Image HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Width="294" Height="428">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="Poster_large.png"/>
      </Image.Source>
    </Image>
    <Image x:Name="MoviePoster" HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Fill" Margin="8" Width="278" Height="412">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;294;412}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
           Source="video.png" IsVisible="{Binding ElementName=MoviePoster, Path=!HasImage}"/>
  </Grid>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1"  TextTrimming="WordEllipsis" Opacity="0.6"
         HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
         Margin="0,40,10,60"/>

  <StackPanel Grid.ColumnSpan="2" Margin="0,0,10,0" VerticalAlignment="Stretch" Orientation="Vertical">

    <media:MovieCollectionAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch" >
      <media:MovieCollectionAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="MediaItemName" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold"
                   Content="{Binding CollectionName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
          </Grid>
        </ControlTemplate>
      </media:MovieCollectionAspectWrapper.Template>
    </media:MovieCollectionAspectWrapper>

    <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
      <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
        <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Movies}"
               Margin="0,0,8,0"  Color="{ThemeResource TextColorForeground}"/>
        <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.MoviesItemsHeader]"
               IsVisible="{Binding Path=Movies,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
      </StackPanel>
    </StackPanel>

    <StackPanel Orientation="Horizontal">
      <StackPanel Orientation="Horizontal">
        <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="30,-5,0,0">
          <media:UserDataWrapper.Template>
            <ControlTemplate>
              <Grid RenderTransformOrigin="0.5,0.5" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Grid.RenderTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="0.7" ScaleY="0.7"/>
                  </TransformGroup>
                </Grid.RenderTransform>
                <Control Style="{ThemeResource CircularBackgroundStyle_0}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_18}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_36}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_54}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_72}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_90}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_108}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_126}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_144}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_162}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_180}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_198}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_216}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_234}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_252}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_270}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_288}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_306}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_324}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_342}"/>

                <Control Style="{ThemeResource CircularIndicatorStyle_0}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>0"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_18}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>5"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_36}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>10"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_54}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>15"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_72}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>20"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_90}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>25"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_108}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>30"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_126}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>35"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_144}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>40"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_162}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>45"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_180}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>50"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_198}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>55"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_216}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>60"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_234}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>65"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_252}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>70"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_270}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>75"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_288}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>80"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_306}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>85"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_324}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>90"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_342}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>95"/>
                  </Control.IsVisible>
                </Control>
                <Grid Margin="-70,-6,70,0">
                  <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                  </Grid.RowDefinitions>
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                  </Grid.ColumnDefinitions>
                  <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="28" Color="White" FontFamily="SegoeUIBold"
                         HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                    <Label.Content>
                      <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                    </Label.Content>
                  </Label>
                </Grid>
              </Grid>
            </ControlTemplate>
          </media:UserDataWrapper.Template>
        </media:UserDataWrapper>
        <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-40,-280,-40">
          <Image.Source>
            <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
          </Image.Source>
        </Image>
      </StackPanel>
      <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,-280,-40">
        <Image.Source>
          <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
        </Image.Source>
      </Image>
    </StackPanel>
  </StackPanel>

</Grid>
