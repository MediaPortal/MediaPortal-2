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
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <ListView Style="{ThemeResource MediaItemsListViewStyle}" ItemsSource="{Binding Items}"
        HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}">
      <ListView.Resources>
        <Model x:Key="LayoutManagerModel" Id="35C31826-4159-4CF9-B4F5-98DEBE04E93C"/>
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
      <ListView.Style>
        <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} &lt; 4 ? {1} : {2}">
          <Binding Source="{StaticResource LayoutManagerModel}" Path="ThumbnailSizeMode"/>
          <Binding Source="{ThemeResource MediaItemsListViewStyle}"/>
          <Binding Source="{ThemeResource MediaItemsGridViewStyle}"/>
        </MultiBinding>
      </ListView.Style>
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
