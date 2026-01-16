using System.Collections.ObjectModel;
using Popstation.Database;

namespace PSXPackagerGUI.Models;

public class GameListWindowModel : BaseNotifyModel
{
    private ObservableCollection<GameEntry> _entries;
    private string _searchText;
    private GameEntry _selectedGame;

    public GameEntry SelectedGame
    {
        get => _selectedGame;
        set => SetProperty(ref _selectedGame, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ObservableCollection<GameEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }
}