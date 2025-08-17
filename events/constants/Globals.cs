namespace Chizl.SystemSearch
{
    /// <summary>
    /// Handler for messages sent.
    /// <code>
    /// Example:
    /// Lookup lookUp = new Lookup();
    /// lookUp.EventMessaging += Lookup_EventMessaging;
    /// ...
    /// private void Lookup_EventMessaging(object sender, SearchEventArgs e) 
    /// {
    ///     var dtTime = e.MessageTime;
    ///     var msgType = e.MessageType;
    ///     var msg = e.Message;
    /// }
    /// </code>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SearchEventHandler(object sender, SearchEventArgs e);

    /// <summary>
    /// Message Types sent to the UI via registered apps to Lookup.EventMessaging
    /// </summary>
    public enum SearchMessageType
    {
        Info = 0,
        Warning,
        Error,
        Exception,
        SearchResults,
        StatusMessage,
        SearchStatus,
        DriveScanStatus,
        FileScanStatus,
        ScanComplete,
        ScanAborted,
        UpdateInprogress
    }
}
