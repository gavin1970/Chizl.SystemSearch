using System;

namespace Chizl.SystemSearch
{
    public class SearchEventArgs : EventArgs
    {
        public SearchEventArgs() { }
        public SearchEventArgs(SearchMessageType messageType, string message)
        {
            MessageTime = DateTime.Now;
            MessageType = messageType;
            Message = message;
        }
        public bool IsEmpty { get { return string.IsNullOrWhiteSpace(Message); } }
        public DateTime MessageTime { get; }
        public SearchMessageType MessageType { get; }
        public string Message { get; } = string.Empty;
    }
}
