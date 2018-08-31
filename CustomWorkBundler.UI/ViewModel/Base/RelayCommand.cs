using System;
using System.Windows.Input;

namespace CustomWorkBundler.UI.ViewModel
{
    public class RelayCommand : ICommand
    {
        private Action Action { get; set; }

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action action)
        {
            Action = action;
        }

        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => Action?.Invoke();
    }
}
