using System;
using System.IO;
using System.Linq;
using System.Drawing;
using Chizl.WinSearch;
using Chizl.Applications;
using Chizl.SystemSearch;
using System.Diagnostics;
using Chizl.ThreadSupport;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Chizl.SearchSystemUI
{
    public partial class Starter : Form
    {
        // had to add this, because folder/file count was coming in so fast, the FileScanStatus event, couldn't
        // updated at the screen.  This states every MAX event for FileScanStatus event, allow it to display.
        // 100 works, but this makes it smother. The ScanComplete/ScanAborted, update the same
        // information, not through FileScanStatus event.
        const int maxRefreshCnt = 1000;
        const string _stopScanText = "&Stop Scan";
        const string _startScanText = "&Start Scan";
        const string _scannedText = "&ReScan";
        const string _configFile = @".\config.dat";

        private static ListBox _selListBox;
        private static bool _loaded = false;

        private static int resetRefreshCnt = 0;
        private static int refreshCnt = 0;

        // thread-safe boolean
        private static Bool _driveFilterOn = new Bool();
        private static Bool _extFilterOn = new Bool();
        private static Bool _customFilterOn = new Bool();

        private static Bool _scanAborted = new Bool(false);
        private static Bool _scanRunning = new Bool(false);
        private static bool _hideErrors = false;
        private static bool _hideInformation = false;
        private static int _mainSplitterDistance = -1;

        private static TimeSpan _scanTime = TimeSpan.Zero;
        private static string _lastFilteringStatus = string.Empty;

        // button background color
        readonly static Color _gray = Color.FromArgb(192, 192, 192);
        readonly static Color _green = Color.FromArgb(128, 255, 128);
        readonly static Color _red = Color.FromArgb(255, 128, 128);

        static DateTime _startDate = DateTime.MinValue;
        static DateTime _endDate = DateTime.MinValue;

        private static ListViewHitTestInfo _listViewHitTest = new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);

        private static readonly ConcurrentQueue<SearchEventArgs> _msgQueue = new ConcurrentQueue<SearchEventArgs>();
        private static readonly List<ListViewItem> _unfilteredItemsList = new List<ListViewItem> { };
        private static readonly ConcurrentDictionary<string, byte> _excludeItems = new ConcurrentDictionary<string, byte> { };

        private static IOFinder _finder = GlobalSetup.Finder;
        private static ScanProperties _criterias = _finder.Criteria;

        delegate void MessageDelegateEvent(SearchEventArgs e);
        delegate void NoParmDelegateEvent();

        private readonly ListViewHelper _lViewHelper = new ListViewHelper();
        private static ColumnHeader[] _listViewColumns = new ColumnHeader[0];

        public Starter()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        #region Helper Methods
        private void ScanEnded()
        {
            if (InvokeRequired)
            {
                var d = new NoParmDelegateEvent(ScanEnded);
                if (!Disposing && !IsDisposed)
                {
                    try { Invoke(d); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ingore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                if ((_finder.CurrentStatus & LookupStatus.Ended) == 0)
                    return;

                _scanRunning.SetFalse();

                // this allows all messages to be posted, only
                // need this setup during scan, which is too intense.
                resetRefreshCnt = 0;
                refreshCnt = 0;

                _endDate = DateTime.UtcNow;
                var diff = _endDate - _startDate;
                BtnFind.Enabled = !string.IsNullOrWhiteSpace(TxtSearchName.Text);
                BtnOptions.Enabled = true;
                TxtSearchName.ReadOnly = false;

                var fullScanned = _finder.FullScanCompleted;
                BtnStartStopScan.Text = _scanAborted ? _startScanText : fullScanned ? _scannedText : _startScanText;

                var appendMsg = _scanAborted ? "before being aborted by user." : "and completed successfully.";

                if (!_scanAborted && _scanTime.Equals(TimeSpan.Zero))
                    _scanTime = diff;

                ShowMsg(SearchMessageType.StatusMessage, $"Scanned for '{diff}' {appendMsg}");

                LastScanTimer.Enabled = true;
                LastScanTimer.Start();

                if (ResultsListView.Items.Count > 0)
                {
                    _driveFilterOn.SetVal(false);
                    _extFilterOn.SetVal(false);
                    _customFilterOn.SetVal(false);
                }
            }
        }
        private void ScanStarted()
        {

            if (InvokeRequired)
            {
                var d = new NoParmDelegateEvent(ScanStarted);
                if (!Disposing && !IsDisposed)
                {
                    try { Invoke(d); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ingore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                if (_scanRunning.SetVal(true))
                    return;
                else
                    _scanAborted.SetFalse();

                // set refresh for folder/file count information to max setting for refeshes.
                resetRefreshCnt = maxRefreshCnt;
                refreshCnt = 0;
                BtnFind.Enabled = false;
                BtnOptions.Enabled = false;
                TxtSearchName.ReadOnly = true;

                BtnStartStopScan.Text = _stopScanText;
                TxtSearchName.Text = TxtSearchName.Text.Trim();
                ResultsListView.Items.Clear();

                _startDate = DateTime.UtcNow;
                _endDate = _startDate;
                StartupTimer.Enabled = true;
            }
        }
        private void ShowMsg(SearchMessageType messageType, string msg) 
            => ShowMsg(new SearchEventArgs(messageType, msg));
        private void ShowMsg(SearchEventArgs e)
        {
            if (InvokeRequired)
            {
                var d = new MessageDelegateEvent(ShowMsg);
                if (!Disposing && !IsDisposed)
                {
                    try { Invoke(d, e); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ingore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                try
                {
                    switch (e.MessageType)
                    {
                        case SearchMessageType.Exception:
                        case SearchMessageType.Error:
                            // If we are hiding information message then errors
                            // can't be seen anyway. Stop backend overhead.
                            if (!_hideErrors && !_hideInformation)
                            {
                                var msg = e.Message;
                                if (e.Message.Contains("Access to the"))
                                    this.ErrorList.Items.Add($"{e.Message}");
                                else
                                    this.ErrorList.Items.Add($"[{e.MessageType}] {e.Message}");
                                this.ErrorList.SelectedIndex = this.ErrorList.Items.Count - 1;
                            }
                            break;
                        case SearchMessageType.Warning:
                        case SearchMessageType.Info:
                            if (!_hideInformation)
                            {
                                this.EventList.Items.Add($"[{e.MessageType}] {e.Message}");
                                this.EventList.SelectedIndex = this.EventList.Items.Count - 1;
                            }
                            break;
                        case SearchMessageType.SearchStatus:
                            this.SearchStatusToolStripStatusLabel.Text = e.Message;
                            break;
                        case SearchMessageType.FileScanStatus:
                            this.FilesAvailableToolStripStatusLabel.Text = e.Message;
                            break;
                        case SearchMessageType.DriveScanStatus:
                            this.SearchStatusToolStripStatusLabel.Text = e.Message;
                            break;
                        case SearchMessageType.SearchResults:
                            if (_scanAborted)
                                break;

                            // clear last unfiltered list.
                            _unfilteredItemsList.Clear();
                            // clear sub filter items.
                            _excludeItems.Clear();

                            // this will cover single line or multi response.
                            var unfiltList = e.Message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            // build ListViewItem[] list
                            var unfileInfoList = GlobalSetup.GetFileInfo(unfiltList);
                            // add to list, for sub filter refresh.
                            _unfilteredItemsList.AddRange(unfileInfoList);
                            // add to ListView
                            this.ResultsListView.Items.AddRange(unfileInfoList);
                            // resize all columns to fit data.
                            ResultsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                            // hide the bytes column, used only for sorting column[2]
                            this.ResultsListView.Columns[1].Width = 0;
                            // auto select the last record in the ListView.
                            this.ResultsListView.SelectedItems[this.ResultsListView.Items.Count - 1].Selected = true;
                            // Use tread safe boolean to flag that Scan is no longer running.
                            _scanRunning.SetFalse();
                            break;
                        case SearchMessageType.StatusMessage:
                            this.StatusToolStripStatusLabel.Text = $"[{e.MessageType}] {e.Message}";
                            break;
                        case SearchMessageType.UpdateInprogress:
                            this.StatusToolStripStatusLabel.Text = $"[{e.MessageType}] {e.Message}";
                            ScanStarted();
                            break;
                        case SearchMessageType.ScanAborted:
                        case SearchMessageType.ScanComplete:
                            _scanAborted.SetVal(e.MessageType.Equals(SearchMessageType.ScanAborted));
                            this.FilesAvailableToolStripStatusLabel.Text = e.Message;
                            ScanEnded();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.StatusToolStripStatusLabel.Text = ex.Message;
                }
            }
        }
        private void SetupForm()
        {
            LoadConfig();
            SetupListView(ResultsListView);
        }
        private void LoadConfig()
        {
            //not ready yet
            ListMenuExportList.Visible = false;


            this.Text = About.TitleWithFileVersion;
            ConfigData.LoadConfig(_configFile);

            ConfigData.GetItem<bool>("ChkFilename", true, out bool isChecked);
            _criterias.SearchFilename = isChecked;
            ConfigData.GetItem<bool>("ChkDirectoryName", false, out isChecked);
            _criterias.SearchDirectory = isChecked;
            ConfigData.GetItem<bool>("ChkInternetCache", false, out isChecked);
            _criterias.AllowInternetCache = isChecked;
            ConfigData.GetItem<bool>("ChkRecycleBin", false, out isChecked);
            _criterias.AllowRecycleBin = isChecked;
            ConfigData.GetItem<bool>("ChkSystemFolder", false, out isChecked);
            _criterias.AllowSystem = isChecked;
            ConfigData.GetItem<bool>("ChkTempFolder", false, out isChecked);
            _criterias.AllowTemp = isChecked;
            ConfigData.GetItem<bool>("ChkUserFolder", true, out isChecked);
            _criterias.AllowUser = isChecked;
            ConfigData.GetItem<bool>("ChkWinFolder", true, out isChecked);
            _criterias.AllowWindows = isChecked;
            ConfigData.GetItem<bool>("ChkHideInfo", false, out isChecked);
            _hideInformation = isChecked;
            ConfigData.GetItem<bool>("ChkHideErrors", true, out isChecked);
            _hideErrors = isChecked || _hideInformation;

            ConfigData.GetItem<int>("MainSplitterDistance", -1, out int splitterDistance);
            _mainSplitterDistance = splitterDistance;

            // This library is setup to auto add to cache when presets are checked or auto remove when unchecked.
            // Ignore change is to prevent the scan from starting to load into cache during startup of the application.
            _criterias.IgnoreChange = false;    //default is true

            // in some cases, the System and Windows directory are the same.
            if (_criterias.WindowsDir.Equals(_criterias.SystemDir))
                ChkSystemFolder.Visible = false;

            if (ConfigData.GetItem("WinMax", false, out bool maxWin) && maxWin)
                this.WindowState = FormWindowState.Maximized;

            if(!maxWin)
            {
                if (ConfigData.GetItem("ClientLoc", Point.Empty, out Point pt) && !pt.IsEmpty)
                    this.Location = pt;

                if (ConfigData.GetItem("ClientSize", Size.Empty, out Size sz) && !sz.IsEmpty)
                    this.Size = sz;
            }

            SetMenuOptions();
            SetFilterStatus();
            _loaded = true;
        }
        private void SetComponentState()
        {
            if (string.IsNullOrWhiteSpace(TxtSearchName.Text)
                || _criterias.IgnoreChange
                || _finder.CurrentStatus.Equals(LookupStatus.Running))
            {
                BtnFind.Enabled = false;
            }
            else
                BtnFind.Enabled = true;

            SetFilterStatus();
        }
        private bool GetSelectedItems(out string[] selectedItems, bool pathOnly)
        {
            selectedItems = new string[0] { };
            var selected = new List<string>();
            var quotes = !pathOnly ? "\"" : ""; //using this for possible change in future

            if (ResultsListView.SelectedItems.Count.Equals(0))
                return false;

            foreach (ListViewItem lineItem in ResultsListView.SelectedItems)
            {
                if(pathOnly)
                    selected.Add($"{quotes}{lineItem.SubItems[5].Text}{quotes}");
                else
                    selected.Add($"{quotes}{lineItem.Text}{quotes}\t{quotes}{lineItem.SubItems[2].Text}{quotes}\t{quotes}{lineItem.SubItems[3].Text}{quotes}\t{quotes}{lineItem.SubItems[4].Text}{quotes}\t{quotes}{lineItem.SubItems[5].Text}{quotes}");
            }

            selectedItems = selected.ToArray();

            return true;

        }
        private void OpenExplorerAndSelectFile(string filePath)
        {
            if (File.Exists(filePath))
                // The /select parameter tells explorer.exe to open to the file's directory and select the specified file.
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            else
                // Handle the case where the file does not exist (e.g., log an error, show a message).
                MessageBox.Show($"Error: File not found at '{filePath}'", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void SetFilterStatus()
        {
            if (InvokeRequired)
            {
                var d = new NoParmDelegateEvent(SetFilterStatus);
                if (!Disposing && !IsDisposed)
                {
                    try { Invoke(d); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ingore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                if (_extFilterOn || _driveFilterOn || _customFilterOn)
                    ToolStripStatusFilterLabel.Visible = true;
                else
                    ToolStripStatusFilterLabel.Visible = false;

                if (_driveFilterOn)
                    StatusToolStripDriveFiltered.Visible = true;
                else
                    StatusToolStripDriveFiltered.Visible = false;

                if (_extFilterOn)
                    StatusToolStripExtFiltered.Visible = true;
                else
                    StatusToolStripExtFiltered.Visible = false;

                if (_customFilterOn)
                    StatusToolStripSubFiltered.Visible = true;
                else
                    StatusToolStripSubFiltered.Visible = false;
            }
        }
        private void SetMenuOptions()
        {
            ChkInternetCache.Checked = _criterias.AllowInternetCache;
            ChkRecycleBin.Checked = _criterias.AllowRecycleBin;
            ChkSystemFolder.Checked = _criterias.AllowSystem;
            ChkTempFolder.Checked = _criterias.AllowTemp;
            ChkUserFolder.Checked = _criterias.AllowUser;
            ChkWinFolder.Checked = _criterias.AllowWindows;
            ChkDirectoryName.Checked = _criterias.SearchDirectory;
            ChkFilename.Checked = _criterias.SearchFilename;
            ChkHideInfo.Checked = _hideInformation;
            ChkHideErrors.Checked = _hideErrors;

            MainSplitContainer.Panel2Collapsed = _hideInformation;
            EventListsSplitContainer.Panel2Collapsed = _hideErrors;
            if (_mainSplitterDistance != -1)
                MainSplitContainer.SplitterDistance = _mainSplitterDistance;
        }
        private (int Added, int Removed) CheckFilterData()
        {
            // found there are files without extensions, this cause just about all fiels to be filtered
            // as most of them are in a folder or the filename has a space in it.  This fixed that issue.
            List<string> keepItems = _unfilteredItemsList.Cast<ListViewItem>()
                                            .Where(w => w.Text.IndexOf(".") > -1)
                                            .Select(s => s.SubItems[5].Text).ToList();

            // This helps focus on mostly extension as there are some folders with . in them, but if
            // the filter starts with a '.', lets assume this is an extension.
            keepItems = keepItems.Where(w => _excludeItems.Keys
                                        .Where(k => k.StartsWith(".") && w.EndsWith(k))
                                        .Count().Equals(0)).ToList();

            // Filter drives
            keepItems = keepItems.Where(w => _excludeItems.Keys
                                        .Where(k => k.Length.Equals(2) && w.Substring(0, 2).Equals(k))
                                        .Count().Equals(0)).ToList();

            keepItems = keepItems.Where(w => _excludeItems.Keys
                                        .Where(k => w.Contains(k))
                                        .Count().Equals(0)).ToList();

            if (!keepItems.Count.Equals(ResultsListView.Items.Count))
            {
                var wasPaths = ResultsListView.Items.Cast<ListViewItem>().Select(w => w.SubItems[5].Text).ToArray();

                ResultsListView.SuspendLayout();
                ResultsListView.Items.Clear();
                ResultsListView.Items.AddRange(_unfilteredItemsList.Where(w => keepItems.Contains(w.SubItems[5].Text)).ToArray());
                ResultsListView.ResumeLayout(true);

                var nowPaths = ResultsListView.Items.Cast<ListViewItem>().Select(w => w.SubItems[5].Text).ToArray();

                var removed = wasPaths.Where(w => !nowPaths.Contains(w)).ToList().Count();
                var added = nowPaths.Where(w => !wasPaths.Contains(w)).ToList().Count();

                return (added, removed);
            }

            return (0, 0);
        }
        #endregion

        #region Auto or Callback Events
        private void MainSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (!_loaded)
                return;

            _mainSplitterDistance = MainSplitContainer.SplitterDistance;
            if (!ConfigData.AddItem("MainSplitterDistance", _mainSplitterDistance, true))
                MessageBox.Show($"'MainSplitterDistance' failed to save to configuration file.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void StartupTimer_Tick(object sender, EventArgs e)
        {
            if (_msgQueue.TryDequeue(out SearchEventArgs msg) &&
               (!_scanAborted || _msgQueue.Count < 10))
                ShowMsg(msg);
        }
        private void IOFinder_EventMessaging(object sender, SearchEventArgs e)
        {
            if (e.MessageType.Equals(SearchMessageType.FileScanStatus))
            {
                if (--refreshCnt <= 0)
                {
                    ShowMsg(e);
                    refreshCnt = resetRefreshCnt;
                    // something is wrong, this should be set, if we have file scans coming in.
                    if (_finder.CurrentStatus.Equals(LookupStatus.Running) && 
                        !BtnStartStopScan.Text.Equals(_stopScanText))
                        ScanStarted();
                }
            }
            else
            {
                if (e.MessageType.Equals(SearchMessageType.SearchStatus) && e.Message.StartsWith("Filtered: "))
                    _lastFilteringStatus = e.Message;

                _msgQueue.Enqueue(e);
            }
        }
        private void TxtSearchName_TextChanged(object sender, EventArgs e) => SetComponentState();
        private void Starter_Load(object sender, EventArgs e)
        {
            _finder.EventMessaging += new SearchEventHandler(this.IOFinder_EventMessaging);
            SetupForm();
        }
        private void Starter_FormClosing(object sender, FormClosingEventArgs e)
        {
            _finder.StopScan();
            _finder.Dispose();
        }
        private void Starter_Resize(object sender, EventArgs e)
        {
            if (!_loaded || this.WindowState == FormWindowState.Minimized)
                return;

            var maxWin = this.WindowState == FormWindowState.Maximized;
            if (!ConfigData.AddItem("WinMax", maxWin, true))
                MessageBox.Show($"WinMax: '{this.WindowState}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // only save size if not maxWin
            if (!maxWin && !ConfigData.AddItem("ClientSize", this.Size, true))
                MessageBox.Show($"ClientSize: '{this.Size}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void Starter_Move(object sender, EventArgs e)
        {
            if (!_loaded || this.WindowState == FormWindowState.Minimized)
                return;

            var maxWin = this.WindowState == FormWindowState.Maximized;
            // only save location if not maxWin
            if (!maxWin && !ConfigData.AddItem("ClientLoc", this.Location, true))
                MessageBox.Show($"ClientLoc: '{this.Location}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion

        #region Buttons Events
        private void BtnFind_Click(object sender, EventArgs e)
        {
            var search = TxtSearchName.Text.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return;

            _finder.Search(TxtSearchName.Text)
                .ContinueWith(t =>
                {
                    _driveFilterOn.SetVal(false);
                    _extFilterOn.SetVal(false);
                    _customFilterOn.SetVal(false);

                    SetFilterStatus();
                });
        }
        private void BtnStartStopScan_Click(object sender, EventArgs e)
        {
            var reScan = BtnStartStopScan.Text.Equals(_scannedText);
            if (BtnStartStopScan.Text.Equals(_startScanText) || reScan)
            {
                if (reScan && 
                    MessageBox.Show("Are you sure you want to rescan?", 
                        About.Title, MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Question) == DialogResult.No)
                        return;

                // start over
                LastScanTimer.Stop();
                _scanTime = TimeSpan.Zero;

                _finder.ScanToCache(reScan)
                    .ContinueWith(t =>
                    {
                        _driveFilterOn.SetVal(false);
                        _extFilterOn.SetVal(false);
                        _customFilterOn.SetVal(false);

                        SetFilterStatus();
                    });
            }
            else
            {
                _scanAborted.SetVal(true);
                _finder.StopScan();
            }
        }
        private void BtnStartStopScan_TextChanged(object sender, EventArgs e)
        {
            if (BtnStartStopScan.Text.Equals(_startScanText))
                BtnStartStopScan.BackColor = _green;
            else if (BtnStartStopScan.Text.Equals(_scannedText))
                BtnStartStopScan.BackColor = _gray;
            else
                BtnStartStopScan.BackColor = _red;
        }
        private void BtnOptions_Click(object sender, EventArgs e) => CMenuOptions.Show(BtnOptions, new Point(1, 1));
        private void UIOptions_CheckedChanged(object sender, EventArgs e)
        {
            var chkBox = sender as CheckBox;
            var isChecked = chkBox.Checked;

            if (!_loaded)
                return;

            switch (chkBox.Name)
            {
                case "ChkHideErrors":
                    _hideErrors = isChecked;
                    EventListsSplitContainer.Panel2Collapsed = _hideErrors;
                    break;
                case "ChkHideInfo":
                    _hideInformation = isChecked;
                    MainSplitContainer.Panel2Collapsed = _hideInformation;
                    break;
                default:
                    MessageBox.Show($"'{chkBox.Name}' is setup for UI Options, but not coded for it.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }

            if (!ConfigData.AddItem(chkBox.Name, isChecked, true))
                MessageBox.Show($"'{chkBox.Name}' failed to save to configuration file.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void Options_CheckedChanged(object sender, EventArgs e)
        {
            var chkBox = sender as ToolStripMenuItem;
            var isChecked = chkBox.Checked;

            switch (chkBox.Name)
            {
                case "ChkDirectoryName":
                    _criterias.SearchDirectory = isChecked;
                    ChkFilename.Checked = _criterias.SearchFilename;
                    if (!ConfigData.AddItem("ChkFilename", !isChecked, true))
                        MessageBox.Show($"'ChkFilename' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "ChkFilename":
                    _criterias.SearchFilename = isChecked;
                    ChkDirectoryName.Checked = _criterias.SearchDirectory;
                    if (!ConfigData.AddItem("ChkDirectoryName", !isChecked, true))
                        MessageBox.Show($"'ChkDirectoryName' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "ChkInternetCache":
                    _criterias.AllowInternetCache = isChecked;
                    break;
                case "ChkRecycleBin":
                    _criterias.AllowRecycleBin = isChecked;
                    break;
                case "ChkSystemFolder":
                    _criterias.AllowSystem = isChecked;
                    break;
                case "ChkTempFolder":
                    _criterias.AllowTemp = isChecked;
                    break;
                case "ChkUserFolder":
                    _criterias.AllowUser = isChecked;
                    break;
                case "ChkWinFolder":
                    _criterias.AllowWindows = isChecked;
                    break;
                default:
                    MessageBox.Show($"'{chkBox.Name}' is setup for options, but not coded for it.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }

            if (!ConfigData.AddItem(chkBox.Name, isChecked, true))
                MessageBox.Show($"'{chkBox.Name}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion

        #region Toolbar Menu Events
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", About.TitleWithFileVersion, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                this.Close();
        }
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Not Implemented Yet.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion

        #region Search Context Menu Events
        private void ListMenuOpenLocation_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                foreach(var path in selectedItems)
                    OpenExplorerAndSelectFile(path.Replace("\"", ""));
            }
        }
        private void ListMenuFilterDrive_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItem, true))
            {
                var change = false;
                var drives = selectedItem.Select(s => s.Substring(0, 2));

                // Find all drives that wasn't selected.
                List<string> removeItems = _unfilteredItemsList.Cast<ListViewItem>()
                                                .Where(w => !drives.Contains(w.SubItems[5].Text.Substring(0, 2)))
                                                .Select(s => s.SubItems[5].Text.Substring(0,2)).Distinct().ToList();

                foreach (var rm in removeItems)
                    change = _excludeItems.TryAdd(rm, 0) || change;

                if (change && CheckFilterData().Added > 0)
                {
                    _driveFilterOn.SetVal(true);
                    SetFilterStatus();
                }
                else
                    ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
            }
        }
        private void ListMenuFilterFileExtension_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                var change = false;
                //var count = ResultsListView.Items.Count;
                var ext = Path.GetExtension(selectedItems[0].Replace("\"", "")).ToLower();
                
                var exts = selectedItems.Select(s => Path.GetExtension(s)).ToList();

                List<string> removeItems = _unfilteredItemsList.Cast<ListViewItem>()
                                                .Where(w => exts.Where(ew => w.SubItems[5].Text.EndsWith(ew)).Count().Equals(0))
                                                .Select(s => Path.GetExtension(s.SubItems[5].Text)).Distinct().ToList();

                foreach (var rm in removeItems)
                    change = _excludeItems.TryAdd(rm, 0) || change;

                //ListViewItem[] items = new ListViewItem[count];
                //ResultsListView.Items.CopyTo(items, 0);

                //foreach (var item in items)
                //{
                //    if (!item.Text.ToLower().EndsWith(ext))
                //    {
                //        var remExt = Path.GetExtension(item.Text).ToLower();
                //        _excludeItems.TryAdd(remExt, 0);

                //        ResultsListView.Items.Remove(item);
                //    }
                //}

                if (change && CheckFilterData().Removed > 0)
                {
                    _extFilterOn.SetVal(true);
                    SetFilterStatus();
                }
                else
                    ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
            }
        }
        private void ListMenuFilterClear_Click(object sender, EventArgs e)
        {
            ResultsListView.Items.Clear();
            ResultsListView.Items.AddRange(_unfilteredItemsList.ToArray());

            _excludeItems.Clear();
            _driveFilterOn.SetVal(false);
            _extFilterOn.SetVal(false);
            _customFilterOn.SetVal(false);

            SetFilterStatus();
            ShowMsg(SearchMessageType.SearchStatus, _lastFilteringStatus);
        }
        private void ListMenuClearList_Click(object sender, EventArgs e)
        {
            ResultsListView.Items.Clear();
            ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}");
        }
        private void ListMenuExclude_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                var exArr = _excludeItems.Keys;
                SubFilterOptions subFilterForm = new SubFilterOptions(selectedItems[0].Replace("\"", ""));
                subFilterForm.ExcludeItems.Clear();
                subFilterForm.ExcludeItems.AddRange(exArr);
                if (subFilterForm.ShowDialog(this) == DialogResult.OK)
                {
                    var extFilterOn = false;
                    var drivFilterOn = false;
                    var removeStrItem = new List<ListViewItem>();

                    //checking to see if what came back from the exclusion form where removed from the list. 
                    if (subFilterForm.RemovedFromExcludeItems.Count() > 0)
                    {
                        _excludeItems.Clear();
                        _driveFilterOn.SetVal(false);
                        _extFilterOn.SetVal(false);
                        _customFilterOn.SetVal(false);
                    }

                    foreach (var item in subFilterForm.ExcludeItems)
                    {
                        if (!extFilterOn)
                            extFilterOn = item.StartsWith(".");
                        if (!drivFilterOn)
                            drivFilterOn = item.Length.Equals(2) && item.EndsWith(":");

                        _excludeItems.TryAdd(item, 0);  //only duplicates will fail
                    }

                    _extFilterOn.SetVal(extFilterOn);
                    _driveFilterOn.SetVal(drivFilterOn);

                    (var added, var removed) = CheckFilterData();

                    if (added > 0 || removed > 0)
                    {
                        _customFilterOn.SetVal(true);
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}\n" +
                                                                $"Last filter added '{added}' and removed '{removed}'.");
                    }
                    else
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");

                    // refresh filter status
                    SetFilterStatus();
                }
            }
        }
        private void LastScanTimer_Tick(object sender, EventArgs e)
        {
            if (!_scanTime.Equals(TimeSpan.Zero))
            {
                ShowMsg(SearchMessageType.StatusMessage, $"Last full scan completed in {_scanTime.TotalSeconds} sec.");
                LastScanTimer.Stop();
            }
        }
        private void ListMenuCopyPath_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join("\n", selectedItems));
                ShowMsg(SearchMessageType.StatusMessage, $"'{selectedItems.Count()}' paths has been copied to clipboard.");
            }
        }
        private void ListMenuCopyList_Click(object sender, EventArgs e)
        {

            if (GetSelectedItems(out string[] selectedItems, false))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join("\n", selectedItems));
                ShowMsg(SearchMessageType.StatusMessage, $"'{selectedItems.Count()}' items were copied to clipboard.");
            }
        }
        private void ListMenuExportList_Click(object sender, EventArgs e)
        {

        }
        private void MnuSkipFolders_MouseUp(object sender, MouseEventArgs e)
        {
            MnuAllowedFolders.Show(new Point(CMenuOptions.Left, CMenuOptions.Top));
        }
        #endregion

        #region All ListBox, Mouse Down Events
        private void ResultsListView_MouseUp(object sender, MouseEventArgs e)
        {
            _listViewHitTest = ResultsListView.HitTest(e.Location);

            if (ResultsListView.Items.Count.Equals(0))
                ListMenuClearList.Enabled = false;
            else
                ListMenuClearList.Enabled = true;

            // var selItem = GetSelectedItem(out string selectedItem);

            if (_listViewHitTest?.Item != null)
            {
                CMenuList.Enabled = true;
                if (ResultsListView.Items.Count.Equals(_unfilteredItemsList.Count))
                    ListMenuFilterClear.Enabled = false;
                else
                    ListMenuFilterClear.Enabled = true;

                if (_driveFilterOn)
                    ListMenuFilterDrive.Enabled = false;
                else
                    ListMenuFilterDrive.Enabled = true;

                if (_extFilterOn)
                    ListMenuFilterFileExtension.Enabled = false;
                else
                    ListMenuFilterFileExtension.Enabled = true;
            }
            else
            {
                CMenuList.Enabled = false;
            }
        }
        private void InfoError_MouseDown(object sender, MouseEventArgs e)
        {
            _selListBox = sender as ListBox;
            var id = _selListBox.IndexFromPoint(e.Location);
            if (id >= 0)
                _selListBox.SelectedIndex = id;
        }
        #endregion

        #region Information, Warning, & Error Context Menu Events
        private void CMenuInfoErrClear_Click(object sender, EventArgs e)
        {
            _selListBox.Items.Clear();
        }
        private void CMenuInfoErrRemoveType_Click(object sender, EventArgs e)
        {
            var grab = 15;
            var id = _selListBox.SelectedIndex;
            if (id < 0)
                return;

            var text = _selListBox.Items[id].ToString();
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (text.Length < grab)
                grab = text.Length;

            if (text.Length >= grab)
            {
                text = text.Substring(0, grab);
                var itemList = _selListBox.Items.OfType<string>().Where(w=>w.StartsWith(text)).ToArray();
                foreach(var item in itemList)
                {
                    _selListBox.Items.Remove(item);
                }
            }
        }
        #endregion

        #region ListView Setup/Controls
        private void ListView_Checked(object sender, ItemCheckedEventArgs e)
        {
            var lv = sender as ListView;
            var isChecked = e.Item.Checked ? 1 : 0;
            var id = e.Item.SubItems[e.Item.SubItems.Count-1].Text;
            // TODO: What is needed if I use Checked.
        }
        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var lv = (ListView)sender;
            ColumnClickEventArgs colClickEvtArgs = e;

            if (e.Column == 2)
                colClickEvtArgs = new ColumnClickEventArgs(1); 

            _lViewHelper.ListView_Column_Sort(lv, colClickEvtArgs);
        }
        private void ListView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!_loaded)
                return;
            _loaded = false;

            var lv = (ListView)sender;
            if (lv.Columns.Count > 0 && e.ColumnIndex != lv.Columns.Count - 1)
            {
                lv.Columns[1].Width = 0;
                lv.AutoResizeColumn(lv.Columns.Count - 1, ColumnHeaderAutoResizeStyle.HeaderSize);
            }

            _loaded = true;
        }
        private void SetupListView(ListView lv)
        {
            if (lv.View != View.Details)
            {
                // setup for sorting..
                lv.HeaderStyle = ColumnHeaderStyle.Clickable;
                // sort based on column click
                lv.ColumnClick += ListView_ColumnClick;
                // resizes last column, when any other columns are changed.
                lv.ColumnWidthChanged += ListView_ColumnWidthChanged;

                // removed multi-select
                lv.MultiSelect = true;
                // allow scrolls
                lv.Scrollable = true;
                // Set the view to show details.
                lv.View = View.Details;
                // Allow the user to edit item text.
                lv.LabelEdit = false;
                // Allow the user to rearrange columns.
                lv.AllowColumnReorder = false;
                // Display check boxes.
                lv.CheckBoxes = false;
                // Select the item and subitems when selection is made.
                lv.FullRowSelect = true;
                // Display grid lines.
                lv.GridLines = true;
                // Sort the items in the list in ascending order.
                lv.Sorting = SortOrder.Descending;
                // Text color
                lv.ForeColor = Color.Black;

                // more sorting
                _lViewHelper.LvColumnSorter = new ListViewColumnSorter();
                lv.ListViewItemSorter = _lViewHelper.LvColumnSorter;
            }

            if (lv.Columns.Count == 0)
            {
                /*
                List<ColumnHeader> cols = ListViewColumns();
                foreach (ColumnHeader header in cols)
                {
                    header.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    if (header.Text != "")
                    {
                        if (string.IsNullOrWhiteSpace(header.Name))
                            header.Name = header.Text;
                        lv.Columns.Add(header);
                    }
                }
                /**/

                lv.Columns.AddRange(ListViewColumns());
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }
        internal ColumnHeader[] ListViewColumns()
        {
            if (_listViewColumns != null && _listViewColumns.Count() > 0)
                return _listViewColumns;

            ColumnHeader[] cols = new ColumnHeader[]
            {
                new ColumnHeader
                {
                    Name = "FileName",
                    Text = "File Name",
                    Width = 100,
                    TextAlign = HorizontalAlignment.Left,
                },
                new ColumnHeader
                {
                    Name = "SizeSort",
                    Text = "Size Bytes",
                    Width = 0,
                },
                new ColumnHeader
                {
                    Name = "Size",
                    Text = "Size",
                    Width = 30,
                    TextAlign = HorizontalAlignment.Left,
                },
                new ColumnHeader
                {
                    Name = "CreatedDate",
                    Text = "Created",
                    Width = 30,
                    TextAlign = HorizontalAlignment.Left,
                },
                new ColumnHeader
                {
                    Name = "ModifiedDate",
                    Text = "Modified",
                    Width = 30,
                    TextAlign = HorizontalAlignment.Left,
                },
                new ColumnHeader
                {
                    Name = "FullFilePath",
                    Text = "Full Path",
                    Width = 150,
                    TextAlign = HorizontalAlignment.Left,
                },
            };

            _listViewColumns = cols;
            return cols;
        }
        #endregion
    }
}
