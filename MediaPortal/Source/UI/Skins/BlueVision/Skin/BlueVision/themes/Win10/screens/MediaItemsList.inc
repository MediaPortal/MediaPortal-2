<?xml version="1.0" encoding="utf-8"?>
<Grid
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    > <!-- Model = MediaNavigationModel -->
  <Grid.Resources>
    <Model x:Key="MediaModel" Id="4CDD601F-E280-43b9-AD0A-6D7B2403C856"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}" Margin="-50,5,20,44">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <Rectangle Grid.Row="0" Fill="Black" Opacity="0.5" Margin="-37,13,41,-1">
      <Rectangle.Effect>
        <ZoomBlurEffect BlurAmount="0.02" CenterX="0.25" CenterY="0.5" />
      </Rectangle.Effect>
      <Rectangle.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Rectangle.IsVisible>
    </Rectangle>

    <Border Grid.Row="0" Margin="-30,20,50,6" >
      <Border.Background>
        <SolidColorBrush Color="{StaticResource GrayBackgroundWithBlurBehind}"/>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border>

    <ListView x:Name="MediaItemsList" Style="{ThemeResource MediaItemsListListViewStyle}" ItemsSource="{Binding Items}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" >
      <ListView.Resources>
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
      <ListView.Margin>
        <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} == {LayoutType}.{CoverLayout} ? {1} : {2}">
          <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"/>
          <Binding Source="-38,22,-120,-3"/>
          <Binding Source="-29,22,-110,-3"/>
        </MultiBinding>
      </ListView.Margin>
    </ListView>

    <Label IsVisible="{Binding ShowListHint}"
        HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
        Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
      HorizontalAlignment="Center" VerticalAlignment="Center"
      Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
  <KeyBinding Key="F5" Command="{Command Reload}"/>
</Grid>
