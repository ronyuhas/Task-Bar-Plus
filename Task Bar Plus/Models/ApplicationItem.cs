using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TaskBarPlus.Models
{
    class ApplicationItem
    {
        public required string Title { get; set; }
        public required BitmapImage Icon { get; set; }
        public required int ProcessId { get; set; }
        public required IntPtr MainWindowHandle { get; set; }
        public required string ExecutablePath { get; set; }

    }
}
