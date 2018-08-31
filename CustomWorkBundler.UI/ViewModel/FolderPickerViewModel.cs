using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomWorkBundler.UI.ViewModel
{
    public class FolderPickerViewModel : BaseViewModel
    {
        public string FolderPath { get; set; }
        public string Label { get; set; }
        public bool CanEditPath { get; set; } = true;

        public List<string> Suggestions { get; set; }

        public FolderPickerViewModel(string label)
        {
            Label = label;
        }
    }
}
