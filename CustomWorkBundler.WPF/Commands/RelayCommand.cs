using System;
using System.Windows.Input;

namespace CustomWorkBundler.WPF.Commands
{
    public class RelayCommand : ICommand
    {
        private Action Command { get; set; }
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action command) => this.Command = command;

        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => this.Command?.Invoke();
    }
}