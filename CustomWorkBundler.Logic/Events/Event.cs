using System;
using System.Collections.Generic;

namespace CustomWorkBundler.Logic.Events
{
    public abstract class Event<EventType> where EventType : class
    {
        public delegate void EventCallback(EventType args);

        private static SortedList<int, List<EventCallback>> Listeners { get; set; } = new SortedList<int, List<EventCallback>>();
        private static string GenericTypeName { get; set; } = typeof(EventType).Name;

        #region Add Listener

        /// <summary>
        /// Adds a listener for this event type with the lowest priority.
        /// </summary>
        /// <param name="callback">Function to call when the event is fired.</param>
        public static void RegisterListener(EventCallback callback) => RegisterListener(int.MaxValue, callback);
        /// <summary>
        /// Adds a listener for this event type.
        /// </summary>
        /// <param name="priority">Priority of the listener. A lower value means the callback will be called earlier.</param>
        /// <param name="callback">Function to call when the event is fired.</param>
        public static void RegisterListener(int priority, EventCallback callback)
        {
            if (callback == null) {
                throw new ArgumentException(nameof(callback), $"Callback for event '{GenericTypeName}' cannot be null.");
            }

            // Check there is a list of listeners for this priority. If there isn't create one
            if (Listeners.TryGetValue(priority, out List<EventCallback> listeners)) {
                listeners.Add(callback);
            } else {
                Listeners.Add(priority, new List<EventCallback> { callback });
            }
        }

        #endregion

        #region Remove Listener

        /// <summary>
        /// Removes the listener from all priority levels.
        /// </summary>
        /// <param name="callback">Function that was registered as a listener.</param>
        public static void RemoveListener(EventCallback callback)
        {
            // Try to remove it from each priority
            foreach (var priority in Listeners.Keys) {
                RemoveListener(priority, callback);
            }
        }

        /// <summary>
        /// Removes the listener from the specified priority level.
        /// </summary>
        /// <param name="priority">Priority that the listener was registered under.</param>
        /// <param name="callback">Function that was registered as a listener.</param>
        public static void RemoveListener(int priority, EventCallback callback)
        {
            if (callback == null) {
                throw new ArgumentException(nameof(callback), $"Trying to remove a callback with null value from event type '{GenericTypeName}'.");
            }

            // Check there is a list of listeners for this priority
            if (Listeners.TryGetValue(priority, out List<EventCallback> listeners)) {
                listeners.Remove(callback);

                // Remove this priority if there are no more listeners
                if (listeners == null || listeners.Count == 0) {
                    Listeners.RemoveAt(priority);
                }
            }
        }
        
        #endregion

        #region Create Event

        /// <summary>
        /// Sends an event of type <typeparamref name="EventType"/> to any listeners. Creates a default data object to send.
        /// </summary>
        /// <param name="sender">Object that is creating the event. Event will not be created if it is null.</param>
        public static void RaiseEvent(object sender) => RaiseEvent(sender, default(EventType));
        /// <summary>
        /// Sends an event of type <typeparamref name="EventType"/> to any listeners. Creates a default data object to send.
        /// </summary>
        /// <param name="sender">Object that is creating the event. Event will not be created if it is null.</param>
        public static void RaiseEvent(object sender, EventType eventArgs)
        {
            // We don't create the event if the sender is null
            if (sender == null)
                return;

            // Loop over all the listener priorities and run the listeners
            foreach (var listenerGroup in Listeners) {
                // Cache values so they don't create constant lookups
                List<EventCallback> callbacks = listenerGroup.Value;
                int callbackCount = callbacks.Count;

                // For loop is slightly faster than foreach
                for (int i = 0; i < callbackCount; i++) {
                    callbacks[i]?.Invoke(eventArgs);
                }
            }
        }

        #endregion


        #region Helper Instance Functions

        public void Raise(object sender) => RaiseEvent(sender, this as EventType);

        #endregion
    }
}