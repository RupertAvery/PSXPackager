using System.Collections.ObjectModel;
using PSXPackager.Common.Cue;

namespace PSXPackagerGUI.Models;

public class SingleModelDesignTime : SingleModel
{
    public SingleModelDesignTime() : base()
    {
        Discs = new ObservableCollection<Disc>()
        {
            new Disc()
            {
                Title = "Final Fantasy VII - Disc 1",
                Size = 123456789,
                Tracks = new ObservableCollection<Track>(
                [
                    new Track()
                    {
                        Number = 1,
                        DataType = DataTypes.DATA,
                        Status = TrackStatus.Stopped
                    },
                    new Track()
                    {
                        Number = 2,
                        DataType = DataTypes.AUDIO,
                        Status = TrackStatus.Playing,
                        IsSelected = true
                    }
                ])
            }

        };

        SelectedDisc = Discs[0];
    }
}