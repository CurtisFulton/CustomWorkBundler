using CustomWorkBundler.WPF.Extensions;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CustomWorkBundler.WPF.Commands
{
    public abstract class AsyncCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public abstract bool CanExecute(object parameter);
        public abstract Task ExecuteAsync(object parameter);

        public async void Execute(object parameter) => await ExecuteAsync(parameter);

        protected void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        protected async Task RunLockedCommand(Expression<Func<bool>> updatingFlag, Func<Task> command)
        {
            // Lock so that this can only be accessed by 1 thread at a time
            lock (updatingFlag) {
                // If this flag is true, this command is already running
                if (updatingFlag.GetPropertyValue())
                    return;

                // Update the flag if it wasn't already running
                updatingFlag.SetPropertyValue(true);
            }

            try {
                // Invoke the actual command
                await command?.Invoke();
            } finally {
                // Set the flag back to false now it has finished
                updatingFlag.SetPropertyValue(false);
            }
        }
    }
}