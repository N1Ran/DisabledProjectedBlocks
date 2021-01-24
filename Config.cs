using System.Linq;
using System.Xml.Serialization;
using Torch;
using Torch.Collections;

namespace DisableProjectedBlocks
{
    public class Config : ViewModel
    {
        private bool _shutOffBlocks = true;
        private bool _removeScripts;
        private bool _counterPcuIncrease;
        private int _maxGridSize;
        private bool _removeProjections;
        private bool _resetTanks;
        private bool _clearInventory;
        private bool _resetJD;

        [XmlIgnore] public MtObservableList<string> RemoveBlocks { get; } = new MtObservableList<string>();

        [XmlArray(nameof(RemoveBlocks))]
        [XmlArrayItem(nameof(RemoveBlocks), ElementName = "Block")]
        public string[] RemoveBlocksSerial
        {
            get => RemoveBlocks.ToArray();
            set
            {
                RemoveBlocks.Clear();
                if (value == null) return;
                foreach (var k in value)
                    RemoveBlocks.Add(k);
            }
        }

        public bool ShutOffBlocks
        {
            get => _shutOffBlocks;
            set
            {
                _shutOffBlocks = value;
                OnPropertyChanged();
            }
        }

        public bool RemoveScripts
        {
            get => _removeScripts;
            set
            {
                _removeScripts = value;
                OnPropertyChanged();
            }
        }

        public bool RemoveProjections
        {
            get => _removeProjections;
            set
            {
                _removeProjections = value;
                OnPropertyChanged();
            }
        }
        public int MaxGridSize
        {
            get => _maxGridSize;
            set
            {
                _maxGridSize = value;
                OnPropertyChanged();
            }
        }

        public bool CounterPcuIncrease
        {
            get => _counterPcuIncrease;
            set
            {
                _counterPcuIncrease = value;
                OnPropertyChanged();
            }
        }

        public bool ClearInventory
        {
            get => _clearInventory;
            set
            {
                _clearInventory = value;
                OnPropertyChanged();
            }
        }

        public bool ResetJumpDrives
        {
            get => _resetJD;
            set
            {
                _resetJD = value;
                OnPropertyChanged();
            }
        }

        public bool ResetTanks
        {
            get => _resetTanks;
            set
            {
                _resetTanks = value;
                OnPropertyChanged();
            }
        }
    }
}
