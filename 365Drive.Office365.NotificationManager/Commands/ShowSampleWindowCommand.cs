using System.Windows;
using System.Windows.Input;

namespace _365Drive.Office365.NotificationManager
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class ShowSampleWindowCommand : CommandBase<ShowSampleWindowCommand>
    {
        
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).Show();
            CommandManager.InvalidateRequerySuggested();
        }


        public override bool CanExecute(object parameter)
        {
            Window win = GetTaskbarWindow(parameter);
            return win != null && !win.IsVisible;
        }
    }
}