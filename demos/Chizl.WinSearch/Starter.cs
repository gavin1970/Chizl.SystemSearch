using Chizl.Applications;
using Chizl.Graphix;
using Chizl.SystemSearch;
using Chizl.ThreadSupport;
using Chizl.WinSearch;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Chizl.SearchSystemUI
{
    public partial class Starter : Form
    {
        // had to add this, because folder/file count was coming in so fast, the FileScanStatus event, couldn't
        // updated at the screen.  This states every MAX event for FileScanStatus event, allow it to display.
        // 100 works, but this makes it smother. The ScanComplete/ScanAborted, update the same
        // information, not through FileScanStatus event.
        private const int _maxRefreshCnt = 1000;
        private const string _stopScanText = "&Stop Scan";
        private const string _startScanText = "&Start Scan";
        private const string _reScanText = "&ReScan";
        private const string _configFile = @".\config.dat";

        private static ListBox _selListBox;
        private static bool _loaded = false;
        private static bool _shuttingDown = false;
        private static Dictionary<string, ToolStripMenuItem> 
            _scanFolders = new Dictionary<string, ToolStripMenuItem>();

        private static int resetRefreshCnt = 0;
        private static int refreshCnt = 0;

        // thread-safe boolean
        private static Bool _driveFilterOn = Bool.False;
        private static Bool _extFilterOn = Bool.False;
        private static Bool _customFilterOn = Bool.False;

        private static Bool _scanAborted = Bool.False;
        private static Bool _scanRunning = Bool.False;
        private static bool _hideErrors = false;
        private static bool _hideInformation = false;
        private static bool _hasDrives = true;
        private static int _mainSplitterDistance = -1;

        private static TimeSpan _scanTime = TimeSpan.Zero;
        private static string _lastFilteringStatus = string.Empty;

        // button background color
        private static readonly Color _gray = Color.FromArgb(192, 192, 192);
        private static readonly Color _green = Color.FromArgb(128, 255, 128);
        private static readonly Color _red = Color.FromArgb(255, 128, 128);
        private static readonly Color _fgTitleColor = Color.FromArgb(255, 168, 0);
        private static readonly Color _bgTitleColor = Color.FromArgb(16, 6, 36);

        private static DateTime _startDate = DateTime.MinValue;
        private static DateTime _endDate = DateTime.MinValue;

        private static ListViewHitTestInfo _listViewHitTest = new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);

        private static readonly ConcurrentQueue<SearchEventArgs> _msgQueue = new ConcurrentQueue<SearchEventArgs>();
        private static readonly List<ListViewItem> _unfilteredItemsList = new List<ListViewItem> { };
        private static readonly ConcurrentDictionary<string, SubFilterExclusion> _excludeItems = new ConcurrentDictionary<string, SubFilterExclusion> { };
        private static SubFilterOptions _subFilterForm;

        private readonly Color _menuTitleBBColor = Color.FromArgb(0, 0, 128);
        private readonly Brush _menuTitleFGColor = Brushes.AntiqueWhite;
        private readonly Font _menuTitleFont = new Font(FontFamily.GenericSansSerif, 9.5f);
        private readonly StringFormat _stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private Point _loc = Point.Empty;

        private static IOFinder _finder = GlobalSetup.Finder;
        private static ScanProperties _criterias = _finder.Criteria;
        private static SysNotify _systemNotify;

        private delegate void MessageDelegateEvent(SearchEventArgs e);
        private delegate void NoParmDelegateEvent();
        private delegate Tuple<int, int> NoParmWRespDelegateEvent();

        private readonly ListViewHelper _lViewHelper = new ListViewHelper();
        private static ColumnHeader[] _listViewColumns = new ColumnHeader[0];
        public Starter()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        #region Helper Methods
        private void CloseApp()
        {
            if (MessageBox.Show("Are you sure you want to exit?", GlobalSetup.WindowTitlebarText, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _shuttingDown = true;
                this.Close();
            }
        }
        private void ScanEnded()
        {
            if (InvokeRequired)
            {
                var d = new NoParmDelegateEvent(ScanEnded);
                if (!Disposing && !IsDisposed)
                {
                    try { Invoke(d); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ignore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                if (!_finder.CurrentStatus.HasFlag(LookupStatus.Completed))
                    return;
                
                _scanRunning.SetFalse();

                var appIcon = IconImageList.Images["Search_AI.ico"];
                _systemNotify.SetIcon(ModImg.ImgToIco(appIcon, new Size(32, 32)), null);

                // this allows all messages to be posted, only
                // need this setup during scan, which is too intense.
                Interlocked.Exchange(ref resetRefreshCnt, 0);
                Interlocked.Exchange(ref refreshCnt, 0);

                _endDate = DateTime.UtcNow;
                var diff = _endDate - _startDate;
                BtnFind.Enabled = !string.IsNullOrWhiteSpace(TxtSearchName.Text);
                //BtnOptions.Enabled = true;
                TxtSearchName.ReadOnly = false;

                var fullScanned = _finder.FullScanCompleted;
                BtnStartStopScan.Text = _scanAborted ? _startScanText : fullScanned ? _reScanText : _startScanText;

                if (!_scanAborted && _scanTime.Equals(TimeSpan.Zero))
                    _scanTime = diff;

                var appendMsg = _scanAborted ? "before being aborted by user." : "and completed successfully.";
                ShowMsg(SearchMessageType.StatusMessage, $"Scanned for '{diff}' {appendMsg}");

                LastScanTimer.Enabled = true;
                LastScanTimer.Start();

                if (ResultsListView.Items.Count > 0)
                {
                    _driveFilterOn.SetFalse();
                    _extFilterOn.SetFalse();
                    _customFilterOn.SetFalse();
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
                    catch { /* Ignore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                if (_scanRunning.SetVal(true))
                    return;
                else
                    _scanAborted.SetFalse();

                var appIcon = IconImageList.Images["Search_AI_Working.ico"];
                _systemNotify.SetIcon(ModImg.ImgToIco(appIcon, new Size(32, 32)), null);

                // set refresh for folder/file count
                // information to max setting for refreshes.
                Interlocked.Exchange(ref resetRefreshCnt, _maxRefreshCnt);
                Interlocked.Exchange(ref refreshCnt, 0);

                BtnFind.Enabled = false;
                //BtnOptions.Enabled = false;
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
                    catch { /* Ignore, shutting down. */ }
                }
            }
            else if (!Disposing && !IsDisposed)
            {
                try
                {
                    switch (e.MessageType)
                    {
                        case SearchMessageType.SearchQueryUsed:
                            TxtSearchName.Text = e.Message;
                            break;
                        case SearchMessageType.Exception:
                        case SearchMessageType.Error:
                            // If we are hiding information message then errors
                            // can't be seen anyway. Stop backend overhead.
                            if (!_hideErrors)
                            {
                                var msg = e.Message;
                                if (e.Message.Contains("Access to the"))
                                    ErrorList.Items.Add($"{e.Message}");
                                else
                                    ErrorList.Items.Add($"[{e.MessageType}] {e.Message}");
                                ErrorList.SelectedIndex = ErrorList.Items.Count - 1;
                            }
                            break;
                        case SearchMessageType.Warning:
                        case SearchMessageType.Info:
                            if (!_hideInformation)
                            {
                                var preMsg = e.MessageType == SearchMessageType.Warning ? $"[{DateTime.Now:HH:mm:ss}-{e.MessageType}]" : $"[{DateTime.Now:HH:mm:ss}]";
                                EventList.Items.Add($"{preMsg} {e.Message}");
                                EventList.SelectedIndex = EventList.Items.Count - 1;
                            }
                            break;
                        case SearchMessageType.SearchStatus:
                            SearchStatusToolStripStatusLabel.Text = e.Message;
                            break;
                        case SearchMessageType.FileScanStatus:
                            if (!_hasDrives && e.Message.Contains(") Files"))
                            {
                                _hasDrives = (GlobalSetup.DriveList.Length > 0);
                            }
                            else if (!_hasDrives)
                            {
                                // reset all objects, execute garbage collector, and start over.
                                _finder.StopScan();
                                _finder.ResetCache();
                                _finder.Dispose();
                                GlobalSetup.Finder.Dispose();
                                _finder = GlobalSetup.Finder;
                                _finder.EventMessaging += new SearchEventHandler(IOFinder_EventMessaging);
                            }

                            FilesAvailableToolStripStatusLabel.Text = e.Message;
                            // multi-thread so, if one stops, it might set status as complete, but
                            // the libary will auto correct if still processing in another thread.
                            // This verifies, if still running and UI refresh is done, then lets
                            // set UI back it scanning.
                            if (resetRefreshCnt.Equals(0) && _finder.CurrentStatus.Equals(LookupStatus.Running))
                                ScanStarted();
                            break;
                        case SearchMessageType.DriveScanStatus:
                            SearchStatusToolStripStatusLabel.Text = e.Message;
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
                            ResultsListView.Items.AddRange(unfileInfoList);

                            if (_excludeItems.Count == 0)
                                LoadExcludesFromForm();

                            (var added, var removed) = CheckFilterData();
                            if (added > 0 || removed > 0)
                            {
                                _driveFilterOn.SetTrue();
                                SetFilterStatus();
                            }
                            else
                                ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");

                            // Use tread safe boolean to flag that Scan is no longer running.
                            _scanRunning.SetFalse();
                            // resize all columns to fit data.
                            ResultsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                            // hide the bytes column, used only for sorting column[2]
                            ResultsListView.Columns[1].Width = 0;
                            break;
                        case SearchMessageType.StatusMessage:
                            StatusToolStripStatusLabel.Text = $"[{e.MessageType}] {e.Message}";
                            break;
                        case SearchMessageType.UpdateInProgress:
                            StatusToolStripStatusLabel.Text = $"[{e.MessageType}] {e.Message}";
                            ScanStarted();
                            break;
                        case SearchMessageType.ScanAborted:
                        case SearchMessageType.ScanComplete:
                            _scanAborted.SetVal(e.MessageType.Equals(SearchMessageType.ScanAborted));
                            FilesAvailableToolStripStatusLabel.Text = e.Message;
                            ScanEnded();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ErrorList.Items.Add($"[UI.ShowMsg()] {ex.Message}");
                }
            }
        }
        private void ShowWindow()
        {
            if (!this.Visible)
                this.Visible = true;
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            this.TopMost = true;
            this.BringToFront();
            this.Focus();
            this.TopMost = false;
        }
        private void SetupSysTray()
        {
            // haven't scanned yet, so stays working until scan is ran.
            var appIcon = IconImageList.Images["Search_AI_Working.ico"];
            Notifier.Icon = ModImg.ImgToIco(appIcon, new Size(16, 16));
            Notifier.Text = GlobalSetup.WindowTitlebarText;

            _systemNotify = new SysNotify(this, Notifier, new SysNotifyTitle(About.Title, _fgTitleColor, _bgTitleColor, new Padding(4)));
            _systemNotify.DoubleClick += Notify_DoubleClick;
        }
        private void SetupForm()
        {
            SetupSysTray();
            LoadConfig();
            //required before Finder can be used.
            SetupListView(ResultsListView, ListViewColumns());
        }
        private DriveInfo[] GetScanDriveList()
        {
            var driveInfoList = DriveInfo.GetDrives().ToList();
            foreach (ToolStripMenuItem drive in CMenuDriveOptions.Items)
            {
                if (!drive.Checked || !drive.Enabled)
                {
                    var driveInfo = driveInfoList.FirstOrDefault(f => f.Name == drive.Tag.ToString());
                    if (driveInfo != null)
                        driveInfoList.Remove(driveInfo);
                }
            }
            return driveInfoList.ToArray();
        }
        
        private bool PathIsEnabled(List<String> disabledDrives, string path, ref bool isChecked, bool defIfMissing = false)
        {
            if (!disabledDrives.Contains(path.ToLower().Substring(0, 3)))
            {
                ConfigData.GetItem<bool>(_scanFolders[path].Name, defIfMissing, out isChecked);
                return true;
            }
            else
            {
                _scanFolders[path].Enabled = false;
                return false;
            }
        }
        private void LoadConfig()
        {
            //not ready yet
            ListMenuExportList.Visible = false;

            Text = GlobalSetup.WindowTitlebarText;
            ConfigData.LoadConfig(_configFile);

            // loads, if exists.
            GlobalSetup.GetScanExclusions();

            ConfigData.GetItem<bool>("ChkFilename", true, out bool isChecked);
            _criterias.SearchFilename = isChecked;
            ConfigData.GetItem<bool>("ChkDirectoryName", false, out isChecked);
            _criterias.SearchDirectory = isChecked;

            var disabledDrives = new List<string>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                // by checking drive.IsReady, it allows drive to be added to the Menu, but disabled to show it's unaccessiable.

                var name = $"ChkScan_{drive.Name[0].ToString().ToUpper()}_Drive";
                ConfigData.GetItem<bool>(name, true, out isChecked);

                var chk = isChecked && drive.IsReady ? CheckState.Checked : CheckState.Unchecked;
                var text = $"{drive.Name} {(drive.IsReady ? "scan" : "(unavailable)")}";

                var retVal = new ToolStripMenuItem()
                {
                    Text = text,
                    Name = name,
                    Tag = drive.Name,
                    Image = null,
                    CheckOnClick = true,
                    Enabled = (drive.IsReady ? true : false),
                    CheckState = chk,
                };

                if (drive.IsReady)
                    retVal.Click += DriveScan_CheckedChanged;

                if (chk == CheckState.Unchecked)
                    disabledDrives.Add(drive.Name.ToLower());

                CMenuDriveOptions.Items.Add(retVal);
            }

            if (_scanFolders.Count > 0)
                _scanFolders.Clear();

            _scanFolders = new Dictionary<string, ToolStripMenuItem>
                {
                    { _criterias.WindowsDir, ChkWinFolder },
                    { _criterias.UserDir, ChkUserFolder },
                    { _criterias.InternetCache, ChkInternetCache },
                    { _criterias.TempDir, ChkTempFolder },
                    { _criterias.RecycleBinDir, ChkRecycleBin }
                };

            if (!_scanFolders.ContainsKey(_criterias.SystemDir))
                _scanFolders.Add(_criterias.SystemDir, ChkSystemFolder);

            GlobalSetup.DriveList = GetScanDriveList();
            _finder = GlobalSetup.Finder;
            
            bool checkIt = false;
            if (PathIsEnabled(disabledDrives, _criterias.InternetCache, ref checkIt))
                _criterias.AllowInternetCache = checkIt;

            if (PathIsEnabled(disabledDrives, _criterias.RecycleBinDir, ref checkIt))
                _criterias.AllowRecycleBin = checkIt;

            if (!string.IsNullOrWhiteSpace(_criterias.SystemDir) 
                && _criterias.SystemDir != _criterias.WindowsDir)
            {
                if (PathIsEnabled(disabledDrives, _criterias.SystemDir, ref checkIt))
                    _criterias.AllowSystem = isChecked;
            }
            else
                ChkSystemFolder.Visible = false;

            if (PathIsEnabled(disabledDrives, _criterias.TempDir, ref checkIt))
                _criterias.AllowTemp = checkIt;

            if (PathIsEnabled(disabledDrives, _criterias.UserDir, ref checkIt, true))
                _criterias.AllowUser = checkIt;
            else
                _criterias.AllowUser = false;

            if (PathIsEnabled(disabledDrives, _criterias.WindowsDir, ref checkIt, true))
                _criterias.AllowWindows = checkIt;
            else
                _criterias.AllowWindows = false;

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
                WindowState = FormWindowState.Maximized;

            if (!maxWin)
            {
                if (ConfigData.GetItem("ClientLoc", Point.Empty, out Point pt) && !pt.IsEmpty)
                    Location = pt;

                if (ConfigData.GetItem("ClientSize", Size.Empty, out Size sz) && !sz.IsEmpty)
                    Size = sz;
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
        private bool GetSelectedItems(out string[] selectedItems, bool pathOnly, bool withHeader = false)
        {
            selectedItems = new string[0] { };
            var selected = new List<string>();
            var quotes = !pathOnly ? "\"" : ""; //using this for possible change in future
            var pathColumnId = ListViewColumns().Length - 1;

            if (withHeader)
            {
                string fullHeader = "";
                foreach (ColumnHeader col in ResultsListView.Columns)
                {
                    //if (col.Width == 0 || (pathOnly && col.Index != ResultsListView.Columns.Count - 1))
                    if ((pathOnly && col.Index != pathColumnId))
                        continue;

                    if (!string.IsNullOrWhiteSpace(fullHeader))
                        fullHeader += "\t";
                    fullHeader += $"\"{col.Text}\"";
                }
                selected.Add(fullHeader);
            }

            if (ResultsListView.SelectedItems.Count.Equals(0))
                return false;

            foreach (ListViewItem lineItem in ResultsListView.SelectedItems)
            {
                if (pathOnly)
                {
                    // expects path to be the last column
                    selected.Add($"{quotes}{lineItem.SubItems[pathColumnId].Text}{quotes}");
                }
                else
                {
                    // by doing it this way, if a column is added or removed, code below will not be to be changed.
                    var lineMerged = $"{quotes}{lineItem.Text}{quotes}";
                    for (int i = 0; i < pathColumnId; i++)
                        lineMerged += $"\t{quotes}{lineItem.SubItems[i + 1].Text}{quotes}";
                    selected.Add(lineMerged);
                }
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
                    catch { /* Ignore, shutting down. */ }
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
        private Tuple<int, int> CheckFilterData()
        {
            var retVal = Tuple.Create(0, 0);

            if (InvokeRequired)
            {
                var d = new NoParmWRespDelegateEvent(CheckFilterData);
                if (!Disposing && !IsDisposed)
                {
                    try { return (Tuple<int, int>)Invoke(d); }
                    catch (ObjectDisposedException ex) { Debug.WriteLine(ex.Message); }
                    catch { /* Ignore, shutting down. */ }
                }
                return retVal;
            }
            else if (!Disposing && !IsDisposed)
            {
                var pathColumnId = ListViewColumns().Length - 1;
                var changed = false;

                // If NoExtensions is found, then remove we don't want them in keepItems.
                var remNoExt = _excludeItems.Values.Where(w => w.Type == FilterType.NoExtension).Any();

                List<string> keepItems = _unfilteredItemsList.Cast<ListViewItem>()
                                                .Where(w => (remNoExt ? w.Text.Contains(".") : w.Text.Length > 0))
                                                .Select(s => s.SubItems[pathColumnId].Text.ToLower()).ToList();

                var excExt = _excludeItems.Where(w => w.Value.Type == FilterType.Extension 
                                                   || w.Value.Type == FilterType.NoExtension)
                                          .Select(s => s.Value.Filter.ToLower())
                                          .ToArray();
                // This helps focus on mostly extension as there are some folders with . in them, but if
                // the filter starts with a '.', lets assume this is an extension.  Extensions are not Case Sensitive
                keepItems = keepItems.Where(w => !excExt.Contains(Path.GetExtension(w).ToLower())).ToList();

                // Filter drives
                keepItems = keepItems.Where(w => _excludeItems
                                            .Where(k => k.Value.Type.Equals(FilterType.Drive) 
                                                     && w.StartsWith(k.Key.ToLower()))
                                            .Count().Equals(0)).ToList();

                // Remove any left overs.
                keepItems = keepItems.Where(w => _excludeItems
                                            .Where(k => k.Value.Type.Equals(FilterType.Contains)
                                                     && w.IndexOf(k.Key.ToLower()) > -1)
                                            .Count().Equals(0)).ToList();

                if (!keepItems.Count.Equals(ResultsListView.Items.Count))
                    changed = true;
                else
                {
                    foreach (ListViewItem item in ResultsListView.Items)
                    {
                        if (!keepItems.Contains(item.SubItems[pathColumnId].Text.ToLower()))
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (changed)
                {
                    var wasPaths = ResultsListView.Items.Cast<ListViewItem>().Select(w => w.SubItems[pathColumnId].Text).ToArray();

                    // This ensure the list doesn't show rows being removed then re-added.  It's an instant replace of data.
                    ResultsListView.SuspendLayout();
                    ResultsListView.Items.Clear();
                    ResultsListView.Items.AddRange(_unfilteredItemsList.Where(w => keepItems.Contains(w.SubItems[pathColumnId].Text.ToLower())).ToArray());
                    ResultsListView.ResumeLayout(true);

                    var nowPaths = ResultsListView.Items.Cast<ListViewItem>().Select(w => w.SubItems[pathColumnId].Text).ToArray();

                    var removed = wasPaths.Where(w => !nowPaths.Contains(w)).ToList().Count();
                    var added = nowPaths.Where(w => !wasPaths.Contains(w)).ToList().Count();

                    return Tuple.Create(added, removed);
                }
            }
            
            return retVal;
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
                if (Interlocked.Decrement(ref refreshCnt) <= 0)
                {
                    ShowMsg(e);
                    Interlocked.Exchange(ref refreshCnt, resetRefreshCnt);
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

                if (e.MessageType == SearchMessageType.Error && _hideErrors)
                    return;

                if (e.MessageType == SearchMessageType.Info && _hideInformation)
                    return;

                _msgQueue.Enqueue(e);
            }
        }
        private void TxtSearchName_TextChanged(object sender, EventArgs e) => SetComponentState();
        private void Starter_Load(object sender, EventArgs e)
        {
            _finder.EventMessaging += new SearchEventHandler(IOFinder_EventMessaging);
            SetupForm();
        }
        private void Starter_FormClosing(object sender, FormClosingEventArgs e)
        {
#if DEBUG
            _shuttingDown = true;
#endif

            if (e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing ||
                _shuttingDown)
            {
                _finder.StopScan();
                _finder.Dispose();
            }
            else
            {
                e.Cancel = !_shuttingDown;
                this.Hide();
            }
        }
        private void Starter_Resize(object sender, EventArgs e)
        {
            if (!_loaded || WindowState == FormWindowState.Minimized)
                return;

            var maxWin = WindowState == FormWindowState.Maximized;
            if (!ConfigData.AddItem("WinMax", maxWin, true))
                MessageBox.Show($"WinMax: '{WindowState}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // only save size if not maxWin
            if (!maxWin && !ConfigData.AddItem("ClientSize", Size, true))
                MessageBox.Show($"ClientSize: '{Size}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void Starter_Move(object sender, EventArgs e)
        {
            if (!_loaded || WindowState == FormWindowState.Minimized)
                return;

            var maxWin = WindowState == FormWindowState.Maximized;
            // only save location if not maxWin
            if (!maxWin && !ConfigData.AddItem("ClientLoc", Location, true))
                MessageBox.Show($"ClientLoc: '{Location}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion

        #region Buttons Events
        private void BtnHide_Click(object sender, EventArgs e) => this.Close();
        private void BtnFind_Click(object sender, EventArgs e)
        {
            var search = TxtSearchName.Text.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return;
            
            _finder.Search(GetScanDriveList(), TxtSearchName.Text)
                .ContinueWith(t =>
                {
                    _driveFilterOn.SetFalse();
                    _extFilterOn.SetFalse();
                    _customFilterOn.SetFalse();
                    SetFilterStatus();
                });
        }
        private void BtnStartStopScan_Click(object sender, EventArgs e)
        {
            var reScan = _finder.FullScanCompleted;
            var driveList = GetScanDriveList();

            if (!BtnStartStopScan.Text.Equals(_stopScanText) && driveList.Count() > 0)
            {
                if (reScan && BtnStartStopScan.Text.Equals(_reScanText))
                {
                    if (MessageBox.Show("Are you sure you want to re-scan?",
                            About.Title, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.No)
                        return;

                    reScan = true;
                }

                // start over
                LastScanTimer.Stop();
                _scanTime = TimeSpan.Zero;

                _finder.ScanToCache(GetScanDriveList(), reScan)
                       .ContinueWith(t =>
                       {
                           _driveFilterOn.SetFalse();
                           _extFilterOn.SetFalse();
                           _customFilterOn.SetFalse();

                           SetFilterStatus();
                       });
            }
            else
            {
                _scanAborted.SetTrue();
                _finder.StopScan();

                if (driveList.Count() == 0)
                {
                    BtnStartStopScan.Text = _startScanText;
                    if (reScan)
                        _finder.ResetCache();
                }
            }
        }
        private void BtnStartStopScan_TextChanged(object sender, EventArgs e)
        {
            if (BtnStartStopScan.Text.Equals(_startScanText))
                BtnStartStopScan.BackColor = _green;
            else if (BtnStartStopScan.Text.Equals(_reScanText))
                BtnStartStopScan.BackColor = _gray;
            else
                BtnStartStopScan.BackColor = _red;
        }
        private void BtnOptions_Click(object sender, EventArgs e) => CMenuOptions.Show(BtnFindOptions, new Point(1, 1));
        private void BtnDriveOptions_Click(object sender, EventArgs e) => CMenuDriveOptions.Show(BtnDriveOptions, new Point(1, 1));
        private void UIOptions_CheckedChanged(object sender, EventArgs e)
        {
            var chkBox = sender as CheckBox;
            var chkBoxName = chkBox.Name;
            var isChecked = chkBox.Checked;

            if (!_loaded)
                return;

            switch (chkBoxName)
            {
                case "ChkHideErrors":
                    _hideErrors = isChecked;
                    EventListsSplitContainer.Panel2Collapsed = _hideErrors;
                    break;
                case "ChkHideInfo":
                    // If Info and Errors are visible, set Errors to hide first, since Errors are seen under Info.
                    if (isChecked && !_hideErrors)
                        ChkHideErrors.Checked = isChecked;

                    _hideInformation = isChecked;
                    MainSplitContainer.Panel2Collapsed = _hideInformation;
                    break;
                default:
                    MessageBox.Show($"'{chkBoxName}' is setup for UI Options, but not coded for it.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }

            if (!ConfigData.AddItem(chkBoxName, isChecked, true))
                MessageBox.Show($"'{chkBoxName}' failed to save to configuration file.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void DriveScan_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loaded)
                return;

            var chkBox = sender as ToolStripMenuItem;
            var isChecked = chkBox.Checked;
            var driveLetter = chkBox.Name.Replace("ChkScan_", "").Replace("_Drive", ":").ToLower();

            if (!ConfigData.AddItem(chkBox.Name, isChecked, true))
                MessageBox.Show($"'{chkBox.Name}' failed to save to configuration file.", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                foreach (var f in _scanFolders)
                {
                    var key = f.Key.ToLower();
                    if (key.StartsWith(driveLetter))
                    {
                        // so that the check event isn't kicked off, it already checked.
                        if (f.Value.Checked != isChecked)
                        {
                            _criterias.IgnoreChange = true;
                            _loc = Point.Empty;
                            // f.Value.Checked = isChecked;
                            f.Value.CheckState = isChecked ? CheckState.Checked : CheckState.Unchecked;
                        }
                        f.Value.Enabled = isChecked;
                    }
                }

                _criterias.IgnoreChange = false;

                GlobalSetup.DriveList = GetScanDriveList();
                string drive = chkBox.Tag.ToString();

                if (isChecked)
                    _finder.AddDrive(new DriveInfo(drive));
                else
                    _finder.RemoveDrive(new DriveInfo(drive));

                _hasDrives = (GlobalSetup.DriveList.Length > 0);
            }
        }
        private void Options_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loaded)
                return;

            var chkBox = sender as ToolStripMenuItem;
            var isChecked = chkBox.Checked;
            Point loc = _loc;

            switch (chkBox.Name)
            {
                case "ChkDirectoryName":
                    loc = Point.Empty;
                    _criterias.SearchDirectory = isChecked || !_criterias.SearchFilename;
                    break;
                case "ChkFilename":
                    loc = Point.Empty;
                    _criterias.SearchFilename = isChecked || !_criterias.SearchDirectory;
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

            if (!loc.IsEmpty)
                MnuAllowedFolders.Show(_loc);
        }
        #endregion

        #region Toolbar Menu Events
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => CloseApp();
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e) => MessageBox.Show(this, "Not Implemented Yet.", About.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        #endregion

        #region Search Context Menu Events
        private void ListMenuOpenLocation_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                foreach (var path in selectedItems)
                    OpenExplorerAndSelectFile(path.Replace("\"", ""));
            }
        }
        private void ListMenuFilterDrive_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItem, true))
            {
                var pathColumnId = ListViewColumns().Length - 1;
                var change = false;
                var drives = selectedItem.Select(s => s.Substring(0, 2));

                // Find all drives that wasn't selected.
                List<string> removeItems = _unfilteredItemsList.Cast<ListViewItem>()
                                                .Where(w => !drives.Contains(w.SubItems[pathColumnId].Text.Substring(0, 2)))
                                                .Select(s => s.SubItems[pathColumnId].Text.Substring(0, 2)).Distinct().ToList();

                foreach (var rm in removeItems)
                    change = _excludeItems.TryAdd(rm, new SubFilterExclusion(rm, FilterType.Drive)) || change;

                if (change)
                {
                    (var added, var removed) = CheckFilterData();
                    if (added > 0 || removed > 0)
                    {
                        _driveFilterOn.SetTrue();
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}\n" +
                                                                $"Last filter added '{added}' and removed '{removed}'.");
                        SetFilterStatus();
                    }
                    else
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
                }
                else
                    ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
            }
        }
        private void ListMenuFilterFileExtension_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true))
            {
                var pathColumnId = ListViewColumns().Length - 1;
                // Extensions are not to be Case Sensitive.
                var change = false;
                var exts = selectedItems.Select(s => Path.GetExtension(s).ToLower()).ToList();

                var currFilter = _unfilteredItemsList.Cast<ListViewItem>()
                                    .Select(s => s.SubItems[pathColumnId].Text.ToLower()).ToList();

                List<string> removeItems = currFilter
                                            .Where(w => exts.Where(ew => w.EndsWith(ew))
                                                            .Count()
                                                            .Equals(0))
                                            .Select(s => Path.GetExtension(s))
                                            .Distinct().ToList();

                foreach (var rm in removeItems)
                    change = _excludeItems.TryAdd(rm.ToLower(), new SubFilterExclusion(rm, FilterType.Extension)) || change;

                if (change)
                {
                    (var added, var removed) = CheckFilterData();
                    if (added > 0 || removed > 0)
                    {
                        _extFilterOn.SetTrue();
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}\n" +
                                                                $"Last filter added '{added}' and removed '{removed}'.");
                        SetFilterStatus();
                    }
                    else
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
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
            _extFilterOn.SetFalse();
            _driveFilterOn.SetFalse();
            _customFilterOn.SetFalse();

            SetFilterStatus();
            ShowMsg(SearchMessageType.SearchStatus, _lastFilteringStatus);
        }
        private void ListMenuClearList_Click(object sender, EventArgs e)
        {
            _unfilteredItemsList.Clear();
            ResultsListView.Items.Clear();
            _subFilterForm?.ExcludeItems.Clear();
            TxtSearchName.Text = "";
            _lastFilteringStatus = "Filtered: 0";
            ShowMsg(SearchMessageType.SearchStatus, _lastFilteringStatus);
            SetFilterStatus();

            _excludeItems.Clear();
            _extFilterOn.SetFalse();
            _driveFilterOn.SetFalse();
            _customFilterOn.SetFalse();

            SetFilterStatus();
        }
        private bool LoadExcludesFromForm()
        {
            var retVal = false;
            var extFilterOn = false;
            var drivFilterOn = false;

            if (_subFilterForm == null)
                return retVal;

            if (!_subFilterForm.ExcludeItems.Count.Equals(_excludeItems.Count))
                _excludeItems.Clear();

            foreach (var item in _subFilterForm.ExcludeItems)
            {
                if (!extFilterOn)
                    extFilterOn = item.Type.Equals(FilterType.Extension) || item.Type.Equals(FilterType.NoExtension);
                if (!drivFilterOn)
                    drivFilterOn = item.Type.Equals(FilterType.Drive);

                retVal = _excludeItems.TryAdd(item.FilterRaw, item) || retVal;  //only duplicates will fail
            }

            _extFilterOn.SetVal(extFilterOn);
            _driveFilterOn.SetVal(drivFilterOn);
            return retVal;
        }
        private void ListMenuExclude_Click(object sender, EventArgs e)
        {
            GetSelectedItems(out string[] selectedItems, true);
            var allItems = string.Join("\n", selectedItems);
            var exArr = _excludeItems.Values.ToArray();
            if (_subFilterForm == null)
                _subFilterForm = new SubFilterOptions();

            _subFilterForm.InitialPath = allItems;
            _subFilterForm.ExcludeItems.Clear();
            _subFilterForm.ExcludeItems.AddRange(exArr);

            if (_subFilterForm.ShowDialog(this) == DialogResult.OK)
            {
                var change = false;
                var removeStrItem = new List<ListViewItem>();

                change = LoadExcludesFromForm();

                if (change)
                {
                    (var added, var removed) = CheckFilterData();
                    if (added > 0 || removed > 0)
                    {
                        _customFilterOn.SetTrue();
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}\n" +
                                                                $"Last filter added '{added}' and removed '{removed}'.");
                        SetFilterStatus();
                    }
                    else
                        ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
                }
                else
                    ShowMsg(SearchMessageType.SearchStatus, $"Showing: {ResultsListView.Items.Count}, {_lastFilteringStatus}");
            }
        }
        private void LastScanTimer_Tick(object sender, EventArgs e)
        {
            if (!_scanTime.Equals(TimeSpan.Zero))
                ShowMsg(SearchMessageType.StatusMessage, $"Last full scan completed in {_scanTime.TotalSeconds} sec.");

            // Insurance
            if (_scanRunning && !BtnStartStopScan.Text.Equals(_stopScanText)
            || !_scanRunning && !(new List<string> { _reScanText, _startScanText }).Contains(BtnStartStopScan.Text))
            {
                BtnStartStopScan.Text = !_scanRunning 
                                            ? _stopScanText : _scanAborted 
                                            ? _startScanText : _finder.FullScanCompleted 
                                            ? _reScanText : _startScanText;
            }
        }
        private void ListMenuCopyPath_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, true, false))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join("\n", selectedItems));
                ShowMsg(SearchMessageType.StatusMessage, $"'{selectedItems.Count()}' paths has been copied to clipboard.");
            }
        }
        private void ListMenuCopyList_Click(object sender, EventArgs e)
        {
            if (GetSelectedItems(out string[] selectedItems, false, true))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join("\n", selectedItems));
                ShowMsg(SearchMessageType.StatusMessage, $"'{selectedItems.Length}' items were copied to clipboard.");
            }
        }
        private void ListMenuExportList_Click(object sender, EventArgs e)
        {

        }
        private void MnuSkipFolders_MouseUp(object sender, MouseEventArgs e)
        {
            _loc = new Point(CMenuOptions.Left, CMenuOptions.Top);
            MnuAllowedFolders.Show(_loc);
        }
        private void AllowFoldersTitle_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_menuTitleBBColor);
            g.DrawString("Allowed Folders", _menuTitleFont, _menuTitleFGColor, AllowFoldersTitle.ContentRectangle, _stringFormat);
        }
        #endregion

        #region All ListBox, Mouse Down Events
        private void ResultsListView_MouseUp(object sender, MouseEventArgs e)
        {
            _listViewHitTest = ResultsListView.HitTest(e.Location);
            var defaultEnable = _listViewHitTest?.Item != null;
            if (ResultsListView.Items.Count.Equals(0))
                ListMenuClearList.Enabled = false;
            else
                ListMenuClearList.Enabled = true;

            if (_listViewHitTest?.Item != null || _unfilteredItemsList.Count > 0)
            {
                CMenuList.Enabled = true;
                if (ResultsListView.Items.Count.Equals(_unfilteredItemsList.Count))
                    ListMenuFilterClear.Enabled = false;
                else
                    ListMenuFilterClear.Enabled = true;

                if (_driveFilterOn || !defaultEnable)
                    ListMenuFilterDrive.Enabled = false;
                else
                    ListMenuFilterDrive.Enabled = true;

                if (_extFilterOn || !defaultEnable)
                    ListMenuFilterFileExtension.Enabled = false;
                else
                    ListMenuFilterFileExtension.Enabled = true;
            }
            else
            {
                CMenuList.Enabled = false;
            }

            ListMenuOpenLocation.Enabled = defaultEnable;
            ListMenuClearList.Enabled = defaultEnable;
            ListMenuCopyPath.Enabled = defaultEnable;
            ListMenuCopyList.Enabled = defaultEnable;
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
        private void Notify_DoubleClick(object sender, EventArgs e) => ShowWindow();
        private void SysTrayOpen_Click(object sender, EventArgs e) => ShowWindow();
        private void SysTrayClose_Click(object sender, EventArgs e) => CloseApp();
        private void CMenuInfoErrClear_Click(object sender, EventArgs e) => _selListBox.Items.Clear();
        #endregion

        #region ListView Setup/Controls
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
        private void SetupListView(ListView lv, ColumnHeader[] ch)
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
                lv.Columns.AddRange(ch);
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }
        internal ColumnHeader[] ListViewColumns()
        {
            if (_listViewColumns != null && _listViewColumns.Length > 0)
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
                    Name = "FileExt",
                    Text = "Ext",
                    Width = 10,
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
