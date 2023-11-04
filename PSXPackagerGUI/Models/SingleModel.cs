using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Models
{
    public class SingleModel : BaseNotifyModel
    {
        private IEnumerable<Disc> _discs;
        private bool _isDirty;
        private double _progress;
        private double _maxProgress;
        private Disc _selectedDisc;
        private ResourceModel _icon0;
        private ResourceModel _icon1;
        private ResourceModel _pic0;
        private ResourceModel _pic1;
        private ResourceModel _snd0;
        private string _status;

        public SingleModel()
        {
            Discs = new List<Disc>()
            {
                new Disc()
                {
                    Title = "Final Fantasy VII",
                    Size = 123456789
                }
            };
        }
        
        public Disc SelectedDisc { get => _selectedDisc; set => SetProperty(ref _selectedDisc, value); }
        public IEnumerable<Disc> Discs { get => _discs; set => SetProperty(ref _discs, value); }

        public ResourceModel Icon0 { get => _icon0; set => SetProperty(ref _icon0, value); }
        public ResourceModel Icon1 { get => _icon1; set => SetProperty(ref _icon1, value); }
        public ResourceModel Pic0 { get => _pic0; set => SetProperty(ref _pic0, value); }
        public ResourceModel Pic1 { get => _pic1; set => SetProperty(ref _pic1, value); }
        public ResourceModel Snd0 { get => _snd0; set => SetProperty(ref _snd0, value); }


        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public double MaxProgress { get => _maxProgress; set => SetProperty(ref _maxProgress, value); }


        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }
        public bool IsBusy { get; set; }

        public bool IsNew { get; set; }
    }
}