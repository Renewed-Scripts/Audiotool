using Audiotool.viewmodel;
using Audiotool.Services;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace Audiotool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ThemeManager _themeManager;
    private readonly NativeAudio _viewModel;

    public MainWindow()
    {
        _viewModel = new NativeAudio();
        DataContext = _viewModel;
        _themeManager = ThemeManager.Instance;
        InitializeComponent();

        UpdateThemeComboBox();

        _themeManager.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ThemeManager.CurrentTheme))
            {
                UpdateThemeComboBox();
            }
        };
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var themeTag = selectedItem.Tag.ToString();
            if (Enum.TryParse<ThemeType>(themeTag, out var theme))
            {
                _themeManager.SetTheme(theme);
            }
        }
    }

    private void UpdateThemeComboBox()
    {
        if (ThemeComboBox != null)
        {
            var targetTag = _themeManager.CurrentTheme.ToString();
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag.ToString() == targetTag)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void AudioFilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            _viewModel.SelectedItems = dataGrid.SelectedItems;
        }
    }
}
