using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CustomWorkBundler.UI.ViewModel
{
    public class RelayCommandAsync : AsyncCommand
    {
        private readonly Func<Task> command;

        public RelayCommandAsync(Func<Task> command)
        {
            this.command = command;
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }

        public override Task ExecuteAsync(object parameter)
        {
            return this.command();
        }
    }
}
