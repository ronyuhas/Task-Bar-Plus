using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TaskBarPlus.ViewModels.BaseClasses;

namespace TaskBarPlus.Models
{
    class ApplicationItem : BaseViewModel
    {
        private string _title = String.Empty;
        public required string Title
        {
            get => _title; 
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
        public required BitmapImage Icon { get; set; }
        public required int ProcessId { get; set; }
        public required IntPtr MainWindowHandle { get; set; }
        public required string ExecutablePath { get; set; }
        public required string ProcessName { get; set; }
    }
}
