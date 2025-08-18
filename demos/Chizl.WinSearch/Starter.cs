using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using Chizl.ThreadSupport;
using Chizl.Applications;
using Chizl.SystemSearch;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Chizl.WinSearch;

namespace Chizl.SearchSystemUI
{
    public partial class Starter : Form
    {
        //when ready, this will be set to true.
        const bool _panelSearchAttrib_Visible = false;


        //had to add this, because folder/file count was coming in so fast, the FileScanStatus event, couldn't
        //updated at the screen.  This states every MAX event for FileScanStatus event, allow it to display.
        //100 works, but this makes it smother. The ScanComplete/ScanAborted, update the same
        //information, not through FileScanStatus event.
        const int maxRefreshCnt = 500;
        const string _stopScanText = "&Stop Scan";
        const string _startScanText = "&Start Scan";
        const string _scannedText = "&ReScan";
        const string _configFile = @".\config.dat";

        private static ListBox _selListBox;
        private static bool _loaded = false;

        private static int resetRefreshCnt = 0;
        private static int refreshCnt = 0;

        //thread-safe boolean
        private static Bool _driveFilterOn = new Bool();
        private static Bool _extFilterOn = new Bool();
        private static Bool _customFilterOn = new Bool();

        private static Bool _scanAborted = new Bool();

        private static string _lastFilteringStatus = string.Empty;

        //button background color
        readonly static Color _gray = Color.FromArgb(192, 192, 192);
        readonly static Color _green = Color.FromArgb(128, 255, 128);
        readonly static Color _red = Color.FromArgb(255, 128, 128);

        static DateTime _startDate = DateTime.MinValue;
        static DateTime _endDate = DateTime.MinValue;

        private static readonly ConcurrentQueue<SearchEventArgs> _msgQueue = new ConcurrentQueue<SearchEventArgs>();
        private static List<string> _unfilteredItemsList = new List<string> { };

        private static IOFinder _finder = GlobalSetup.Finder;
        private static ScanProperties _criterias = _finder.Criteria;

        delegate void MessageDelegateEvent(SearchEventArgs e);
        delegate void NoParmDelegateEvent();

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

                //this allows all messages to be posted, only
                //need this setup during scan, which is too intense.
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
                ShowMsg(SearchMessageType.StatusMessage, $"Scanned for '{diff}' {appendMsg}");
                
                if (ResultsList.Items.Count > 0)
                {
                    _driveFilterOn.SetVal(false);
                    _extFilterOn.SetVal(false);
                    _customFilterOn.SetVal(false);
                }
            }
        }
        private void ScanStarted()
        {
            if (BtnStartStopScan.Text.Equals(_stopScanText))
                return;

            _scanAborted.SetVal(false);

            //set refresh for folder/file count information to max setting for refeshes.
            resetRefreshCnt = maxRefreshCnt;
            refreshCnt = 0;
            BtnFind.Enabled = false;
            BtnOptions.Enabled = false;
            TxtSearchName.ReadOnly = true;

            BtnStartStopScan.Text = _stopScanText;
            TxtSearchName.Text = TxtSearchName.Text.Trim();
            ResultsList.Items.Clear();

            _startDate = DateTime.UtcNow;
            _endDate = _startDate;

            StartupTimer.Enabled = true;
        }
        private void ShowMsg(SearchMessageType messageType, string msg) => ShowMsg(new SearchEventArgs(messageType, msg));
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
                            this.ErrorList.Items.Add($"[{e.MessageType}] {e.Message}");
                            this.ErrorList.SelectedIndex = this.ErrorList.Items.Count - 1;
                            break;
                        case SearchMessageType.Warning:
                        case SearchMessageType.Info:
                            this.EventList.Items.Add($"[{e.MessageType}] {e.Message}");
                            this.EventList.SelectedIndex = this.EventList.Items.Count - 1;
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
                            if (e.Message.Contains("\n"))
                            {
                                var unfiltList = e.Message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                _unfilteredItemsList.Clear();
                                _unfilteredItemsList.AddRange(unfiltList);
                                this.ResultsList.Items.AddRange(unfiltList);
                                ScanStarted();
                            }
                            else
                                this.ResultsList.Items.Add(e.Message);
                            this.ResultsList.SelectedIndex = this.ResultsList.Items.Count - 1;
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
        }
        private void LoadConfig()
        {
            this.Text = About.TitleWithFileVersion;

            ConfigData.LoadConfig(_configFile);

            //Ignore change.. This is a flag, so that each _finder.OptionalPaths.<<Property>> doesn't start a scan.

            _criterias.IgnoreChange = true;
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
            _criterias.IgnoreChange = false;

            //in some cases, the System and Windows directory are the same.
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

            if (GlobalSetup.UseDirection.Count == 0)
                SetupCbChoices();

            SetMenuOptions();
            SetFilterStatus();
            _loaded = true;
        }
        private void SetupCbChoices()
        {
            PanelSearchAttrib.Visible = _panelSearchAttrib_Visible;
            //Search attributes
            foreach (var e in Enum.GetNames(typeof(SearchAttributes)))
                this.CbAttribute.Items.Add(GlobalSetup.CleanEnumName(e, true));
            this.CbAttribute.SelectedIndex = 0;

            //Search Options
            foreach (var e in Enum.GetNames(typeof(SearchDirecction)))
                this.CbGtLtEq.Items.Add(GlobalSetup.CleanEnumName(e, false));
            this.CbGtLtEq.SelectedIndex = 0;
            this.CbGtLtEq.Visible = false;
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
        private void CbAttribute_SelectedIndexChanged(object sender, EventArgs e) 
        {
            if (GlobalSetup.UseDirection.Count == CbAttribute.Items.Count)
                CbGtLtEq.Visible = GlobalSetup.UseDirection[CbAttribute.SelectedIndex];
        }
        private bool GetSelectedItem(out string selectedItem)
        {
            selectedItem = string.Empty;
            if (ResultsList.SelectedIndex < 0)
                return false;

            selectedItem = ResultsList.Items[ResultsList.SelectedIndex].ToString();

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
        }
        #endregion

        #region Auto or Callback Events
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
                    //something is wrong, this should be set, if we have file scans coming in.
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

            //only save size if not maxWin
            if (!maxWin && !ConfigData.AddItem("ClientSize", this.Size, true))
                MessageBox.Show($"ClientSize: '{this.Size}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void Starter_Move(object sender, EventArgs e)
        {
            if (!_loaded || this.WindowState == FormWindowState.Minimized)
                return;

            var maxWin = this.WindowState == FormWindowState.Maximized;
            //only save location if not maxWin
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
                 
                _finder.ScanToCache()
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

            //BtnStartStopScan.Enabled = !BtnStartStopScan.Text.Equals(_scannedText);
        }
        private void BtnOptions_Click(object sender, EventArgs e)
        {
            CMenuOptions.Show(BtnOptions, new Point(0, 0));
        }
        /// <summary>
        /// All BtnOptions Checkbox Events
        /// </summary>
        private void Options_CheckedChanged(object sender, EventArgs e)
        {
            var chkBox = sender as ToolStripMenuItem;
            var isChecked = chkBox.Checked;

            switch (chkBox.Name)
            {
                case "ChkDirectoryName":
                    _criterias.SearchDirectory = isChecked;
                    ChkFilename.Checked = _criterias.SearchFilename;
                    break;
                case "ChkFilename":
                    _criterias.SearchFilename = isChecked;
                    ChkDirectoryName.Checked = _criterias.SearchDirectory;
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
        private void SetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //nothing here yet.
        }
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //nothing here yet.
        }
        #endregion

        #region Search Context Menu Events
        private void ListMenuOpenLocation_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
                OpenExplorerAndSelectFile(selectedItem);
        }
        private void ListMenuFilterDrive_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
            {
                var count = ResultsList.Items.Count;
                var drive = selectedItem.Substring(0, 2).ToUpper();
                string[] items = new string[count];
                ResultsList.Items.CopyTo(items, 0);

                foreach (var item in items)
                {
                    if (!item.ToString().ToUpper().StartsWith(drive))
                        ResultsList.Items.Remove(item);
                }

                _driveFilterOn.SetVal(true);
                SetFilterStatus();
                ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsList.Items.Count}, {_lastFilteringStatus}");
            }
        }
        private void ListMenuFilterFileExtension_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
            {
                var count = ResultsList.Items.Count;
                var ext = Path.GetExtension(selectedItem).ToLower();
                string[] items = new string[count];
                ResultsList.Items.CopyTo(items, 0);

                foreach (var item in items)
                {
                    if (!item.ToString().ToLower().EndsWith(ext))
                        ResultsList.Items.Remove(item);
                }

                _extFilterOn.SetVal(true);
                SetFilterStatus();
                ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsList.Items.Count}, {_lastFilteringStatus}");
            }
            //ListMenuFilterClear
        }
        private void ListMenuFilterClear_Click(object sender, EventArgs e)
        {
            ResultsList.Items.Clear();
            ResultsList.Items.AddRange(_unfilteredItemsList.ToArray());
            _driveFilterOn.SetVal(false);
            _extFilterOn.SetVal(false);
            _customFilterOn.SetVal(false);

            SetFilterStatus();
            ShowMsg(SearchMessageType.SearchStatus, _lastFilteringStatus);
        }
        private void ListMenuClearList_Click(object sender, EventArgs e)
        {
            ResultsList.Items.Clear();
            ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsList.Items.Count}");
        }
        #endregion

        #region All ListBox, Mouse Down Events
        private void ResultsList_MouseDown(object sender, MouseEventArgs e)
        {
            var id = ResultsList.IndexFromPoint(e.Location);
            if (id >= 0)
                ResultsList.SelectedIndex = id;

            if (ResultsList.Items.Count.Equals(0))
                ListMenuClearList.Enabled = false;
            else
                ListMenuClearList.Enabled = true;

            if (id >= 0 && GetSelectedItem(out string selectedItem))
            {
                CMenuList.Enabled = true;
                if (ResultsList.Items.Count.Equals(_unfilteredItemsList.Count))
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
        private void CMenuInfoErrCopy_Click(object sender, EventArgs e)
        {

        }
        private void CMenuInfoErrIgnore_Click(object sender, EventArgs e)
        {

        }
        #endregion

        private void ListMenuExclude_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
            {
                SubFilterOptions subFilter = new SubFilterOptions(selectedItem);

                if (subFilter.ShowDialog(this) == DialogResult.OK)
                {
                    var removeStrItem = new List<string>();
                    var exItems = subFilter.ExcludeItems;

                    foreach (var item in ResultsList.Items)
                    {
                        foreach (var ex in exItems) 
                        {
                            if (item.ToString().Contains(ex))
                                removeStrItem.Add(item.ToString());
                        }
                    }

                    foreach (var item in removeStrItem)
                        ResultsList.Items.Remove(item);

                    if (removeStrItem.Count > 0)
                    {
                        _customFilterOn.SetVal(true);
                        SetFilterStatus();
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsList.Items.Count}, {_lastFilteringStatus}");
                    }
                }
            }            
        }
    }
}
