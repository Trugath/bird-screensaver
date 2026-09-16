using System.Windows;
using System.Windows.Controls;
using BirdScreensaver.Detection;
using BirdScreensaver.Settings;
using BirdScreensaver.Tools;

namespace BirdScreensaver.Windows;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    internal SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        MicrophoneBox.ItemsSource = new[] { "(default)" }.Concat(Microphones.List());
        MicrophoneBox.SelectedItem = string.IsNullOrWhiteSpace(settings.Microphone)
            ? "(default)"
            : settings.Microphone;
        FillRegions(settings.Region);
        Confidence.Value = settings.Confidence;
        Lookback.Text = settings.LookbackMinutes.ToString("0.#");
        MaxBirds.Text = settings.MaxBirds.ToString();
        ShowNames.IsChecked = settings.ShowNames;
        Demo.IsChecked = settings.Demo;
        RefreshModelStatus();
    }

    private void OnConfidence(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        ConfidenceLabel.Text = e.NewValue.ToString("0.00");

    private void OnRegionChanged(object sender, SelectionChangedEventArgs e) =>
        RefreshModelStatus();

    private void FillRegions(string selected)
    {
        RegionBox.Items.Clear();
        ComboBoxItem? pick = null;
        foreach (var region in ModelCatalog.Regions)
        {
            var item = new ComboBoxItem { Content = region.Title, Tag = region.Id };
            RegionBox.Items.Add(item);
            if (region.Id == selected)
                pick = item;
        }

        RegionBox.SelectedItem = pick ?? RegionBox.Items.OfType<ComboBoxItem>().FirstOrDefault();
    }

    private string SelectedRegion() =>
        (RegionBox.SelectedItem as ComboBoxItem)?.Tag as string ?? _settings.Region;

    private void RefreshModelStatus()
    {
        var region = SelectedRegion();
        ModelStatus.Text = ModelCatalog.IsPresent(region)
            ? "BirdNET model is on this PC."
            : "No model yet. About 150 MB, downloaded once.";
    }

    private async void OnDownload(object sender, RoutedEventArgs e)
    {
        var region = SelectedRegion();
        ModelStatus.Text = "Starting download…";
        var progress = new Progress<string>(s => ModelStatus.Text = s);
        try
        {
            await ModelDownloader.DownloadAsync(region, progress, CancellationToken.None);
        }
        catch (Exception ex)
        {
            ModelStatus.Text = ex.Message;
        }
    }

    private async void OnFetch(object sender, RoutedEventArgs e)
    {
        ModelStatus.Text = "Fetching plates…";
        try
        {
            await Task.Run(() => FetchAssets.Run(new Progress<string>(s =>
                Dispatcher.Invoke(() => ModelStatus.Text = s))));
        }
        catch (Exception ex)
        {
            ModelStatus.Text = ex.Message;
        }
    }

    private void OnInstall(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Installer.Install();
            MessageBox.Show(this, "Screensaver registered:\n" + path, "Installed");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Install failed");
        }
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        _settings.Microphone = MicrophoneBox.SelectedItem as string is "(default)" or null
            ? null
            : MicrophoneBox.SelectedItem as string;
        _settings.Region = SelectedRegion();
        _settings.Confidence = (float)Confidence.Value;
        if (float.TryParse(Lookback.Text, out var minutes))
            _settings.LookbackMinutes = minutes;
        if (int.TryParse(MaxBirds.Text, out var max))
            _settings.MaxBirds = max;
        _settings.ShowNames = ShowNames.IsChecked == true;
        _settings.Demo = Demo.IsChecked == true;
        _settings.Save();
        Close();
    }
}
