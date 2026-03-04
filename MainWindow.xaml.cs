using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MrMergePdfStamper.Models;
using MrMergePdfStamper.Services;

namespace MrMergePdfStamper;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PrintJobItem> _items = [];
    private readonly ImpositionService _impositionService = new(new ExternalToolsRunner());

    public MainWindow()
    {
        InitializeComponent();

        _items.Add(new PrintJobItem { SourcePage = 1, Copies = 1, SpreadNumber = 1 });
        JobsDataGrid.ItemsSource = _items;
        RecalculateSpreadNumbers();
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Выберите исходный PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            InputFileTextBox.Text = dialog.FileName;
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        _items.Add(new PrintJobItem { SourcePage = 1, Copies = 1 });
        RecalculateSpreadNumbers();
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (JobsDataGrid.SelectedItem is PrintJobItem selected)
        {
            _items.Remove(selected);
            RecalculateSpreadNumbers();
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var inputFile = InputFileTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
            {
                MessageBox.Show("Выберите существующий PDF-файл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var validRows = _items.Where(x => x.SourcePage > 0 && x.Copies > 0).ToList();
            if (validRows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну корректную строку в таблицу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadStampSettings(out var rightMm, out var topMm, out var fontName, out var fontSize))
            {
                return;
            }

            var outputFile = Path.Combine(
                Path.GetDirectoryName(inputFile)!,
                $"{Path.GetFileNameWithoutExtension(inputFile)}_stamped_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            var toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools");
            var qpdfPath = Path.Combine(toolsDir, "qpdf.exe");
            var gsPath = Path.Combine(toolsDir, "gswin64c.exe");

            MainProgressBar.Value = 0;
            StatusTextBlock.Text = "Запуск...";
            ToggleUi(false);

            var progress = new Progress<string>(message =>
            {
                StatusTextBlock.Text = message;
                var current = MainProgressBar.Value;
                MainProgressBar.Value = Math.Min(95, current + 100.0 / Math.Max(validRows.Count * 2, 1));
            });

            var finalPath = await _impositionService.BuildAsync(
                inputFile,
                outputFile,
                validRows,
                qpdfPath,
                gsPath,
                rightMm,
                topMm,
                fontName,
                fontSize,
                progress);

            MainProgressBar.Value = 100;
            StatusTextBlock.Text = $"Готово: {finalPath}";

            MessageBox.Show($"Файл создан:\n{finalPath}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFolder(Path.GetDirectoryName(finalPath)!);
        }
        catch (Exception ex)
        {
            MainProgressBar.Value = 0;
            StatusTextBlock.Text = "Ошибка.";
            MessageBox.Show(ex.Message, "Ошибка обработки", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ToggleUi(true);
        }
    }

    private static void OpenFolder(string folderPath)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }

    private bool TryReadStampSettings(out double rightMm, out double topMm, out string fontName, out double fontSize)
    {
        var ok =
            double.TryParse(RightOffsetTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out rightMm) &&
            double.TryParse(TopOffsetTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out topMm) &&
            double.TryParse(FontSizeTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out fontSize);

        fontName = FontNameTextBox.Text.Trim();

        if (!ok || rightMm < 0 || topMm < 0 || fontSize <= 0 || string.IsNullOrWhiteSpace(fontName))
        {
            MessageBox.Show("Проверьте настройки штампа (числа и имя шрифта).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void JobsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var cols = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 2)
                {
                    continue;
                }

                if (int.TryParse(cols[0], out var sourcePage) && int.TryParse(cols[1], out var copies))
                {
                    _items.Add(new PrintJobItem { SourcePage = sourcePage, Copies = copies });
                }
            }

            RecalculateSpreadNumbers();
            e.Handled = true;
        }
    }

    private void RecalculateSpreadNumbers()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].SpreadNumber = i + 1;
        }
    }

    private void ToggleUi(bool enabled)
    {
        InputFileTextBox.IsEnabled = enabled;
        JobsDataGrid.IsEnabled = enabled;
        RightOffsetTextBox.IsEnabled = enabled;
        TopOffsetTextBox.IsEnabled = enabled;
        FontNameTextBox.IsEnabled = enabled;
        FontSizeTextBox.IsEnabled = enabled;
    }
}
