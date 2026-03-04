using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MrMergePdfStamper.Models;

public class PrintJobItem : INotifyPropertyChanged
{
    private int _sourcePage = 1;
    private int _copies = 1;
    private int _spreadNumber;

    public int SourcePage
    {
        get => _sourcePage;
        set
        {
            _sourcePage = value;
            OnPropertyChanged();
        }
    }

    public int Copies
    {
        get => _copies;
        set
        {
            _copies = value;
            OnPropertyChanged();
        }
    }

    public int SpreadNumber
    {
        get => _spreadNumber;
        set
        {
            _spreadNumber = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
