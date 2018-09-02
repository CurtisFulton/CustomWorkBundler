using System;

namespace CustomWorkBundler.Logic.Events
{
    public class ProgressChangedEvent : Event<ProgressChangedEvent>
    {
        public string ProgressMessage { get; set; }

        public ProgressChangedEvent(string message = "")
        {
            this.ProgressMessage = message;
        }
    }
}