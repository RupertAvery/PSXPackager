using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        private Action _searchDebounced;

        public GameListWindow(bool showActions = true)
        {
            InitializeComponent();

            _model = new GameListWindowModel();

            DataContext = _model;

            _gameDb = new GameDB(Path.Combine(ApplicationInfo.AppPath, "Resources", "gameInfo.db"));

            _model.Entries = new ObservableCollection<GameEntry>(_gameDb.GameEntries);

            _model.PropertyChanged += ModelOnPropertyChanged;

            Actions.Visibility = showActions ? Visibility.Visible : Visibility.Hidden;

            if (!showActions)
            {
                Grid.RowDefinitions.RemoveAt(2);
            }

            _searchDebounced = Debounce(() =>
            {
                var searchText = _model.SearchText.Trim().ToLower();

                if (searchText.Length > 0)
                {
                    _model.Entries =
                        new ObservableCollection<GameEntry>(_gameDb.GameEntries.Where(d =>
                            d.SerialID.ToLower().Contains(searchText) || d.Title.ToLower().Contains(searchText)
                        ));
                }
                else
                {
                    _model.Entries = new ObservableCollection<GameEntry>(_gameDb.GameEntries);
                }
            });
        }

        public GameEntry? SelectedGame => _model.SelectedGame; 

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameListWindowModel.SearchText))
            {
                _searchDebounced();
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

        private void CopyGameID_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedGame != null)
            {
                Clipboard.SetText(_model.SelectedGame.GameID);
            }
        }

        private void CopyTitle_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedGame != null)
            {
                Clipboard.SetText(_model.SelectedGame.Title);
            }
        }

        private void CopyMainGameID_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedGame != null)
            {
                Clipboard.SetText(_model.SelectedGame.MainGameID);
            }
        }

        private void CopyMainTitle_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedGame != null)
            {
                Clipboard.SetText(_model.SelectedGame.MainGameTitle);
            }
        }

        public static Action Debounce(Action func, int milliseconds = 300)
        {
            CancellationTokenSource? cancelTokenSource = null;

            return () =>
            {
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                Task.Delay(milliseconds, cancelTokenSource.Token)
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            func();
                        }
                    }, TaskScheduler.Default);
            };
        }

    }
}
