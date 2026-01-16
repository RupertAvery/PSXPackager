using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Popstation.Database;
using PSXPackagerGUI.Models;
using Path = System.IO.Path;

namespace PSXPackagerGUI
{
    /// <summary>
    /// Interaction logic for GameListWindow.xaml
    /// </summary>
    public partial class GameListWindow : Window
    {
        private GameListWindowModel _model;
        private GameDB _gameDb;

        public GameListWindow()
        {
            InitializeComponent();

            _model = new GameListWindowModel();

            DataContext = _model;

            _gameDb = new GameDB(Path.Combine(ApplicationInfo.AppPath, "Resources", "gameInfo.db"));

            _model.Entries = new ObservableCollection<GameEntry>(_gameDb.GameEntries);

            _model.PropertyChanged += ModelOnPropertyChanged;


        }

        public GameEntry SelectedGame => _model.SelectedGame; 

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameListWindowModel.SearchText))
            {
                var searchText = _model.SearchText.Trim().ToLower();

                if (searchText.Length > 0)
                {
                    _model.Entries =
                        new ObservableCollection<GameEntry>(_gameDb.GameEntries.Where(d =>
                            d.GameID.ToLower().Contains(searchText) || d.GameName.ToLower().Contains(searchText)
                            ));
                }
                else
                {
                    _model.Entries = new ObservableCollection<GameEntry>(_gameDb.GameEntries);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
