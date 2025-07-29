using TaskBarPlus.ViewModels.BaseClasses;

namespace TaskBarPlus.ViewModels
{
    class UtilitiesViewModel : BaseViewModel
    {
		private string _imageSource = "pack://application:,,,/Task Bar Plus;component/Resources/SVGs/Gear.svg";

		public string ImageSource
		{
			get { return _imageSource; }
			set { _imageSource = value;
				OnPropertyChanged();
			}
		}

	}
}
