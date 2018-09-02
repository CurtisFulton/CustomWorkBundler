using CustomWorkBundler.WPF.Extensions;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CustomWorkBundler.WPF.Commands
{
    public class AsyncRelayCommand : AsyncCommand
    {
        private Func<Task> Command { get; set; }
        private Expression<Func<bool>> UpdatingFlag { get; set; }

        public AsyncRelayCommand(Func<Task> command) => this.Command = command;
        public AsyncRelayCommand(Expression<Func<bool>> updatingFlag, Func<Task> command)
        {
            this.Command = command;
            this.UpdatingFlag = updatingFlag;
        }

        public override bool CanExecute(object parameter) => true;
        public override async Task ExecuteAsync(object parameter)
        {
            // Check if there is a flag set. If there is, we need to do some checks before running.
            if (this.UpdatingFlag == null) {
                await this.Command?.Invoke();
            } else {
                await base.RunLockedCommand(this.UpdatingFlag, this.Command);
            }
        }


    }
}