namespace Chizl.SearchSystemUI
{
    partial class Starter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Starter));
            this.PanelSearchText = new System.Windows.Forms.Panel();
            this.TxtSearchName = new System.Windows.Forms.TextBox();
            this.PanelSearchAttrib = new System.Windows.Forms.Panel();
            this.CbAttribute = new System.Windows.Forms.ComboBox();
            this.CbGtLtEq = new System.Windows.Forms.ComboBox();
            this.PanelFindButton = new System.Windows.Forms.Panel();
            this.BtnFind = new System.Windows.Forms.Button();
            this.BtnOptions = new System.Windows.Forms.Button();
            this.EventList = new System.Windows.Forms.ListBox();
            this.CMenuInfoErr = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CMenuInfoErrClear = new System.Windows.Forms.ToolStripMenuItem();
            this.CMenuInfoErrCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.CMenuInfoErrFilter = new System.Windows.Forms.ToolStripMenuItem();
            this.CMenuInfoErrRemoveType = new System.Windows.Forms.ToolStripMenuItem();
            this.CMenuInfoErrIgnore = new System.Windows.Forms.ToolStripMenuItem();
            this.ResultsPanel = new System.Windows.Forms.Panel();
            this.MainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.ResultsListView = new System.Windows.Forms.ListView();
            this.CMenuList = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ListMenuFilters = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuFilterClear = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuFilterDrive = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuFilterFileExtension = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuOpenLocation = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuClearList = new System.Windows.Forms.ToolStripMenuItem();
            this.ListMenuExclude = new System.Windows.Forms.ToolStripMenuItem();
            this.EventListsSplitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ChkHideErrors = new System.Windows.Forms.CheckBox();
            this.ErrorList = new System.Windows.Forms.ListBox();
            this.ChkSystemFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.CMenuOptions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ChkFilename = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkDirectoryName = new System.Windows.Forms.ToolStripMenuItem();
            this.MnuSkipFolders = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkRecycleBin = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkTempFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkInternetCache = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkWinFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.ChkUserFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.PanelScanButton = new System.Windows.Forms.Panel();
            this.BtnStartStopScan = new System.Windows.Forms.Button();
            this.StartupMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StartupStatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusFilterLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusToolStripDriveFiltered = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusToolStripExtFiltered = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusToolStripSubFiltered = new System.Windows.Forms.ToolStripStatusLabel();
            this.SearchStatusToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FilesAvailableToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.PanelSearchBar = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.StartupTimer = new System.Windows.Forms.Timer(this.components);
            this.LastScanTimer = new System.Windows.Forms.Timer(this.components);
            this.PanelSearchText.SuspendLayout();
            this.PanelSearchAttrib.SuspendLayout();
            this.PanelFindButton.SuspendLayout();
            this.CMenuInfoErr.SuspendLayout();
            this.ResultsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).BeginInit();
            this.MainSplitContainer.Panel1.SuspendLayout();
            this.MainSplitContainer.Panel2.SuspendLayout();
            this.MainSplitContainer.SuspendLayout();
            this.CMenuList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EventListsSplitContainer)).BeginInit();
            this.EventListsSplitContainer.Panel1.SuspendLayout();
            this.EventListsSplitContainer.Panel2.SuspendLayout();
            this.EventListsSplitContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            this.CMenuOptions.SuspendLayout();
            this.PanelScanButton.SuspendLayout();
            this.StartupMenuStrip.SuspendLayout();
            this.StartupStatusStrip.SuspendLayout();
            this.PanelSearchBar.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // PanelSearchText
            // 
            this.PanelSearchText.Controls.Add(this.TxtSearchName);
            this.PanelSearchText.Controls.Add(this.PanelSearchAttrib);
            this.PanelSearchText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PanelSearchText.Location = new System.Drawing.Point(87, 0);
            this.PanelSearchText.Name = "PanelSearchText";
            this.PanelSearchText.Padding = new System.Windows.Forms.Padding(5, 5, 5, 0);
            this.PanelSearchText.Size = new System.Drawing.Size(955, 30);
            this.PanelSearchText.TabIndex = 5;
            // 
            // TxtSearchName
            // 
            this.TxtSearchName.BackColor = System.Drawing.SystemColors.Control;
            this.TxtSearchName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtSearchName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TxtSearchName.ForeColor = System.Drawing.SystemColors.MenuText;
            this.TxtSearchName.Location = new System.Drawing.Point(5, 5);
            this.TxtSearchName.Name = "TxtSearchName";
            this.TxtSearchName.Size = new System.Drawing.Size(740, 20);
            this.TxtSearchName.TabIndex = 3;
            this.TxtSearchName.TextChanged += new System.EventHandler(this.TxtSearchName_TextChanged);
            // 
            // PanelSearchAttrib
            // 
            this.PanelSearchAttrib.Controls.Add(this.CbAttribute);
            this.PanelSearchAttrib.Controls.Add(this.CbGtLtEq);
            this.PanelSearchAttrib.Dock = System.Windows.Forms.DockStyle.Right;
            this.PanelSearchAttrib.Location = new System.Drawing.Point(745, 5);
            this.PanelSearchAttrib.Name = "PanelSearchAttrib";
            this.PanelSearchAttrib.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.PanelSearchAttrib.Size = new System.Drawing.Size(205, 25);
            this.PanelSearchAttrib.TabIndex = 4;
            // 
            // CbAttribute
            // 
            this.CbAttribute.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CbAttribute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CbAttribute.FormattingEnabled = true;
            this.CbAttribute.Location = new System.Drawing.Point(5, 0);
            this.CbAttribute.Name = "CbAttribute";
            this.CbAttribute.Size = new System.Drawing.Size(104, 21);
            this.CbAttribute.TabIndex = 0;
            this.CbAttribute.SelectedIndexChanged += new System.EventHandler(this.CbAttribute_SelectedIndexChanged);
            // 
            // CbGtLtEq
            // 
            this.CbGtLtEq.Dock = System.Windows.Forms.DockStyle.Right;
            this.CbGtLtEq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CbGtLtEq.FormattingEnabled = true;
            this.CbGtLtEq.Location = new System.Drawing.Point(109, 0);
            this.CbGtLtEq.Name = "CbGtLtEq";
            this.CbGtLtEq.Size = new System.Drawing.Size(96, 21);
            this.CbGtLtEq.TabIndex = 1;
            // 
            // PanelFindButton
            // 
            this.PanelFindButton.Controls.Add(this.BtnFind);
            this.PanelFindButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PanelFindButton.Location = new System.Drawing.Point(21, 3);
            this.PanelFindButton.Name = "PanelFindButton";
            this.PanelFindButton.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.PanelFindButton.Size = new System.Drawing.Size(88, 24);
            this.PanelFindButton.TabIndex = 6;
            // 
            // BtnFind
            // 
            this.BtnFind.BackColor = System.Drawing.SystemColors.Control;
            this.BtnFind.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BtnFind.Enabled = false;
            this.BtnFind.ForeColor = System.Drawing.SystemColors.MenuText;
            this.BtnFind.Location = new System.Drawing.Point(3, 0);
            this.BtnFind.Name = "BtnFind";
            this.BtnFind.Size = new System.Drawing.Size(85, 24);
            this.BtnFind.TabIndex = 4;
            this.BtnFind.Text = "Find";
            this.BtnFind.UseVisualStyleBackColor = true;
            this.BtnFind.Click += new System.EventHandler(this.BtnFind_Click);
            // 
            // BtnOptions
            // 
            this.BtnOptions.Dock = System.Windows.Forms.DockStyle.Left;
            this.BtnOptions.Location = new System.Drawing.Point(3, 3);
            this.BtnOptions.Name = "BtnOptions";
            this.BtnOptions.Size = new System.Drawing.Size(18, 24);
            this.BtnOptions.TabIndex = 5;
            this.BtnOptions.Text = "🔰";
            this.BtnOptions.UseVisualStyleBackColor = true;
            this.BtnOptions.Click += new System.EventHandler(this.BtnOptions_Click);
            // 
            // EventList
            // 
            this.EventList.ContextMenuStrip = this.CMenuInfoErr;
            this.EventList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EventList.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EventList.FormattingEnabled = true;
            this.EventList.IntegralHeight = false;
            this.EventList.ItemHeight = 14;
            this.EventList.Location = new System.Drawing.Point(0, 0);
            this.EventList.Name = "EventList";
            this.EventList.Size = new System.Drawing.Size(1134, 59);
            this.EventList.TabIndex = 1;
            this.EventList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.InfoError_MouseDown);
            // 
            // CMenuInfoErr
            // 
            this.CMenuInfoErr.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CMenuInfoErrClear,
            this.CMenuInfoErrCopy,
            this.CMenuInfoErrFilter});
            this.CMenuInfoErr.Name = "CMenuInfoErr";
            this.CMenuInfoErr.Size = new System.Drawing.Size(106, 70);
            // 
            // CMenuInfoErrClear
            // 
            this.CMenuInfoErrClear.Name = "CMenuInfoErrClear";
            this.CMenuInfoErrClear.Size = new System.Drawing.Size(105, 22);
            this.CMenuInfoErrClear.Text = "&Clear";
            this.CMenuInfoErrClear.Click += new System.EventHandler(this.CMenuInfoErrClear_Click);
            // 
            // CMenuInfoErrCopy
            // 
            this.CMenuInfoErrCopy.Name = "CMenuInfoErrCopy";
            this.CMenuInfoErrCopy.Size = new System.Drawing.Size(105, 22);
            this.CMenuInfoErrCopy.Text = "&Copy";
            this.CMenuInfoErrCopy.Click += new System.EventHandler(this.CMenuInfoErrCopy_Click);
            // 
            // CMenuInfoErrFilter
            // 
            this.CMenuInfoErrFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CMenuInfoErrRemoveType,
            this.CMenuInfoErrIgnore});
            this.CMenuInfoErrFilter.Name = "CMenuInfoErrFilter";
            this.CMenuInfoErrFilter.Size = new System.Drawing.Size(105, 22);
            this.CMenuInfoErrFilter.Text = "&Filters";
            // 
            // CMenuInfoErrRemoveType
            // 
            this.CMenuInfoErrRemoveType.Name = "CMenuInfoErrRemoveType";
            this.CMenuInfoErrRemoveType.Size = new System.Drawing.Size(180, 22);
            this.CMenuInfoErrRemoveType.Text = "&Remove Type";
            this.CMenuInfoErrRemoveType.Click += new System.EventHandler(this.CMenuInfoErrRemoveType_Click);
            // 
            // CMenuInfoErrIgnore
            // 
            this.CMenuInfoErrIgnore.Name = "CMenuInfoErrIgnore";
            this.CMenuInfoErrIgnore.Size = new System.Drawing.Size(180, 22);
            this.CMenuInfoErrIgnore.Text = "&Ignore Type";
            this.CMenuInfoErrIgnore.Click += new System.EventHandler(this.CMenuInfoErrIgnore_Click);
            // 
            // ResultsPanel
            // 
            this.ResultsPanel.BackColor = System.Drawing.Color.DimGray;
            this.ResultsPanel.Controls.Add(this.MainSplitContainer);
            this.ResultsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ResultsPanel.Location = new System.Drawing.Point(0, 54);
            this.ResultsPanel.Name = "ResultsPanel";
            this.ResultsPanel.Padding = new System.Windows.Forms.Padding(5);
            this.ResultsPanel.Size = new System.Drawing.Size(1154, 567);
            this.ResultsPanel.TabIndex = 14;
            // 
            // MainSplitContainer
            // 
            this.MainSplitContainer.BackColor = System.Drawing.Color.DimGray;
            this.MainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplitContainer.Location = new System.Drawing.Point(5, 5);
            this.MainSplitContainer.Name = "MainSplitContainer";
            this.MainSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // MainSplitContainer.Panel1
            // 
            this.MainSplitContainer.Panel1.BackColor = System.Drawing.Color.DarkGray;
            this.MainSplitContainer.Panel1.Controls.Add(this.ResultsListView);
            this.MainSplitContainer.Panel1.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            // 
            // MainSplitContainer.Panel2
            // 
            this.MainSplitContainer.Panel2.BackColor = System.Drawing.Color.DarkGray;
            this.MainSplitContainer.Panel2.Controls.Add(this.EventListsSplitContainer);
            this.MainSplitContainer.Panel2.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.MainSplitContainer.Size = new System.Drawing.Size(1144, 557);
            this.MainSplitContainer.SplitterDistance = 405;
            this.MainSplitContainer.SplitterWidth = 10;
            this.MainSplitContainer.TabIndex = 16;
            // 
            // ResultsListView
            // 
            this.ResultsListView.ContextMenuStrip = this.CMenuList;
            this.ResultsListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ResultsListView.HideSelection = false;
            this.ResultsListView.Location = new System.Drawing.Point(5, 0);
            this.ResultsListView.Name = "ResultsListView";
            this.ResultsListView.Size = new System.Drawing.Size(1134, 405);
            this.ResultsListView.TabIndex = 2;
            this.ResultsListView.UseCompatibleStateImageBehavior = false;
            this.ResultsListView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ResultsListView_MouseUp);
            // 
            // CMenuList
            // 
            this.CMenuList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ListMenuFilters,
            this.ListMenuOpenLocation,
            this.ListMenuClearList,
            this.ListMenuExclude});
            this.CMenuList.Name = "CMenuList";
            this.CMenuList.Size = new System.Drawing.Size(177, 92);
            // 
            // ListMenuFilters
            // 
            this.ListMenuFilters.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ListMenuFilterClear,
            this.ListMenuFilterDrive,
            this.ListMenuFilterFileExtension});
            this.ListMenuFilters.Name = "ListMenuFilters";
            this.ListMenuFilters.Size = new System.Drawing.Size(176, 22);
            this.ListMenuFilters.Text = "&Sub-Filter";
            // 
            // ListMenuFilterClear
            // 
            this.ListMenuFilterClear.Name = "ListMenuFilterClear";
            this.ListMenuFilterClear.Size = new System.Drawing.Size(252, 22);
            this.ListMenuFilterClear.Text = "&Clear all Sub-Filters";
            this.ListMenuFilterClear.Click += new System.EventHandler(this.ListMenuFilterClear_Click);
            // 
            // ListMenuFilterDrive
            // 
            this.ListMenuFilterDrive.Name = "ListMenuFilterDrive";
            this.ListMenuFilterDrive.Size = new System.Drawing.Size(252, 22);
            this.ListMenuFilterDrive.Text = "Show Selected &Drive Only";
            this.ListMenuFilterDrive.Click += new System.EventHandler(this.ListMenuFilterDrive_Click);
            // 
            // ListMenuFilterFileExtension
            // 
            this.ListMenuFilterFileExtension.Name = "ListMenuFilterFileExtension";
            this.ListMenuFilterFileExtension.Size = new System.Drawing.Size(252, 22);
            this.ListMenuFilterFileExtension.Text = "Show Selected &File Extension Only";
            this.ListMenuFilterFileExtension.Click += new System.EventHandler(this.ListMenuFilterFileExtension_Click);
            // 
            // ListMenuOpenLocation
            // 
            this.ListMenuOpenLocation.Name = "ListMenuOpenLocation";
            this.ListMenuOpenLocation.Size = new System.Drawing.Size(176, 22);
            this.ListMenuOpenLocation.Text = "&Open Location";
            this.ListMenuOpenLocation.Click += new System.EventHandler(this.ListMenuOpenLocation_Click);
            // 
            // ListMenuClearList
            // 
            this.ListMenuClearList.Name = "ListMenuClearList";
            this.ListMenuClearList.Size = new System.Drawing.Size(176, 22);
            this.ListMenuClearList.Text = "&Clear Results List";
            this.ListMenuClearList.Click += new System.EventHandler(this.ListMenuClearList_Click);
            // 
            // ListMenuExclude
            // 
            this.ListMenuExclude.Name = "ListMenuExclude";
            this.ListMenuExclude.Size = new System.Drawing.Size(176, 22);
            this.ListMenuExclude.Text = "&Excluded (Subfilter)";
            this.ListMenuExclude.Click += new System.EventHandler(this.ListMenuExclude_Click);
            // 
            // EventListsSplitContainer
            // 
            this.EventListsSplitContainer.BackColor = System.Drawing.Color.DimGray;
            this.EventListsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EventListsSplitContainer.Location = new System.Drawing.Point(5, 0);
            this.EventListsSplitContainer.Name = "EventListsSplitContainer";
            this.EventListsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // EventListsSplitContainer.Panel1
            // 
            this.EventListsSplitContainer.Panel1.BackColor = System.Drawing.Color.DarkGray;
            this.EventListsSplitContainer.Panel1.Controls.Add(this.EventList);
            this.EventListsSplitContainer.Panel1.Controls.Add(this.panel1);
            // 
            // EventListsSplitContainer.Panel2
            // 
            this.EventListsSplitContainer.Panel2.BackColor = System.Drawing.Color.DarkGray;
            this.EventListsSplitContainer.Panel2.Controls.Add(this.ErrorList);
            this.EventListsSplitContainer.Size = new System.Drawing.Size(1134, 142);
            this.EventListsSplitContainer.SplitterDistance = 77;
            this.EventListsSplitContainer.SplitterWidth = 10;
            this.EventListsSplitContainer.TabIndex = 15;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Gray;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.ChkHideErrors);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 59);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1134, 18);
            this.panel1.TabIndex = 2;
            // 
            // ChkHideErrors
            // 
            this.ChkHideErrors.AutoSize = true;
            this.ChkHideErrors.Checked = true;
            this.ChkHideErrors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkHideErrors.Dock = System.Windows.Forms.DockStyle.Right;
            this.ChkHideErrors.Location = new System.Drawing.Point(1054, 0);
            this.ChkHideErrors.Name = "ChkHideErrors";
            this.ChkHideErrors.Size = new System.Drawing.Size(78, 16);
            this.ChkHideErrors.TabIndex = 0;
            this.ChkHideErrors.Text = "Hide Errors";
            this.ChkHideErrors.UseVisualStyleBackColor = true;
            this.ChkHideErrors.CheckedChanged += new System.EventHandler(this.UIOptions_CheckedChanged);
            // 
            // ErrorList
            // 
            this.ErrorList.ContextMenuStrip = this.CMenuInfoErr;
            this.ErrorList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ErrorList.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ErrorList.FormattingEnabled = true;
            this.ErrorList.IntegralHeight = false;
            this.ErrorList.ItemHeight = 14;
            this.ErrorList.Location = new System.Drawing.Point(0, 0);
            this.ErrorList.Name = "ErrorList";
            this.ErrorList.Size = new System.Drawing.Size(1134, 55);
            this.ErrorList.TabIndex = 0;
            this.ErrorList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.InfoError_MouseDown);
            // 
            // ChkSystemFolder
            // 
            this.ChkSystemFolder.Checked = true;
            this.ChkSystemFolder.CheckOnClick = true;
            this.ChkSystemFolder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkSystemFolder.Name = "ChkSystemFolder";
            this.ChkSystemFolder.Size = new System.Drawing.Size(151, 22);
            this.ChkSystemFolder.Text = "System";
            this.ChkSystemFolder.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // CMenuOptions
            // 
            this.CMenuOptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChkFilename,
            this.ChkDirectoryName,
            this.MnuSkipFolders});
            this.CMenuOptions.Name = "CMenuOptions";
            this.CMenuOptions.Size = new System.Drawing.Size(172, 70);
            // 
            // ChkFilename
            // 
            this.ChkFilename.Checked = true;
            this.ChkFilename.CheckOnClick = true;
            this.ChkFilename.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkFilename.Name = "ChkFilename";
            this.ChkFilename.Size = new System.Drawing.Size(171, 22);
            this.ChkFilename.Text = "Search File Name";
            this.ChkFilename.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // ChkDirectoryName
            // 
            this.ChkDirectoryName.CheckOnClick = true;
            this.ChkDirectoryName.Name = "ChkDirectoryName";
            this.ChkDirectoryName.Size = new System.Drawing.Size(171, 22);
            this.ChkDirectoryName.Text = "Search Path Name";
            this.ChkDirectoryName.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // MnuSkipFolders
            // 
            this.MnuSkipFolders.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChkRecycleBin,
            this.ChkTempFolder,
            this.ChkInternetCache,
            this.ChkWinFolder,
            this.ChkSystemFolder,
            this.ChkUserFolder});
            this.MnuSkipFolders.Name = "MnuSkipFolders";
            this.MnuSkipFolders.Size = new System.Drawing.Size(171, 22);
            this.MnuSkipFolders.Text = "Allowed Folders";
            // 
            // ChkRecycleBin
            // 
            this.ChkRecycleBin.CheckOnClick = true;
            this.ChkRecycleBin.Name = "ChkRecycleBin";
            this.ChkRecycleBin.Size = new System.Drawing.Size(151, 22);
            this.ChkRecycleBin.Text = "Recycle Bin";
            this.ChkRecycleBin.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // ChkTempFolder
            // 
            this.ChkTempFolder.CheckOnClick = true;
            this.ChkTempFolder.Name = "ChkTempFolder";
            this.ChkTempFolder.Size = new System.Drawing.Size(151, 22);
            this.ChkTempFolder.Text = "Temp Folder";
            this.ChkTempFolder.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // ChkInternetCache
            // 
            this.ChkInternetCache.CheckOnClick = true;
            this.ChkInternetCache.Name = "ChkInternetCache";
            this.ChkInternetCache.Size = new System.Drawing.Size(151, 22);
            this.ChkInternetCache.Text = "Internet Cache";
            this.ChkInternetCache.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // ChkWinFolder
            // 
            this.ChkWinFolder.Checked = true;
            this.ChkWinFolder.CheckOnClick = true;
            this.ChkWinFolder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkWinFolder.Name = "ChkWinFolder";
            this.ChkWinFolder.Size = new System.Drawing.Size(151, 22);
            this.ChkWinFolder.Text = "Windows";
            this.ChkWinFolder.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // ChkUserFolder
            // 
            this.ChkUserFolder.Checked = true;
            this.ChkUserFolder.CheckOnClick = true;
            this.ChkUserFolder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkUserFolder.Name = "ChkUserFolder";
            this.ChkUserFolder.Size = new System.Drawing.Size(151, 22);
            this.ChkUserFolder.Text = "User";
            this.ChkUserFolder.Click += new System.EventHandler(this.Options_CheckedChanged);
            // 
            // PanelScanButton
            // 
            this.PanelScanButton.Controls.Add(this.BtnStartStopScan);
            this.PanelScanButton.Dock = System.Windows.Forms.DockStyle.Left;
            this.PanelScanButton.Location = new System.Drawing.Point(0, 0);
            this.PanelScanButton.Name = "PanelScanButton";
            this.PanelScanButton.Padding = new System.Windows.Forms.Padding(3);
            this.PanelScanButton.Size = new System.Drawing.Size(87, 30);
            this.PanelScanButton.TabIndex = 7;
            // 
            // BtnStartStopScan
            // 
            this.BtnStartStopScan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.BtnStartStopScan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BtnStartStopScan.Location = new System.Drawing.Point(3, 3);
            this.BtnStartStopScan.Name = "BtnStartStopScan";
            this.BtnStartStopScan.Size = new System.Drawing.Size(81, 24);
            this.BtnStartStopScan.TabIndex = 2;
            this.BtnStartStopScan.Text = "&Start Scan";
            this.BtnStartStopScan.UseVisualStyleBackColor = false;
            this.BtnStartStopScan.TextChanged += new System.EventHandler(this.BtnStartStopScan_TextChanged);
            this.BtnStartStopScan.Click += new System.EventHandler(this.BtnStartStopScan_Click);
            // 
            // StartupMenuStrip
            // 
            this.StartupMenuStrip.BackColor = System.Drawing.SystemColors.ControlDark;
            this.StartupMenuStrip.ForeColor = System.Drawing.SystemColors.MenuText;
            this.StartupMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.StartupMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.StartupMenuStrip.Name = "StartupMenuStrip";
            this.StartupMenuStrip.Size = new System.Drawing.Size(1154, 24);
            this.StartupMenuStrip.TabIndex = 9;
            this.StartupMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.X)));
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ExitToolStripMenuItem.Text = "E&xit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AboutToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.HelpToolStripMenuItem.Text = "&Help";
            // 
            // AboutToolStripMenuItem
            // 
            this.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            this.AboutToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.AboutToolStripMenuItem.Text = "&About";
            this.AboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // StartupStatusStrip
            // 
            this.StartupStatusStrip.BackColor = System.Drawing.SystemColors.ControlDark;
            this.StartupStatusStrip.ForeColor = System.Drawing.SystemColors.ControlText;
            this.StartupStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusToolStripStatusLabel,
            this.ToolStripStatusFilterLabel,
            this.StatusToolStripDriveFiltered,
            this.StatusToolStripExtFiltered,
            this.StatusToolStripSubFiltered,
            this.SearchStatusToolStripStatusLabel,
            this.FilesAvailableToolStripStatusLabel});
            this.StartupStatusStrip.Location = new System.Drawing.Point(0, 621);
            this.StartupStatusStrip.Name = "StartupStatusStrip";
            this.StartupStatusStrip.Size = new System.Drawing.Size(1154, 24);
            this.StartupStatusStrip.TabIndex = 10;
            this.StartupStatusStrip.Text = "statusStrip1";
            // 
            // StatusToolStripStatusLabel
            // 
            this.StatusToolStripStatusLabel.Name = "StatusToolStripStatusLabel";
            this.StatusToolStripStatusLabel.Size = new System.Drawing.Size(717, 19);
            this.StatusToolStripStatusLabel.Spring = true;
            this.StatusToolStripStatusLabel.Text = "Ready...";
            this.StatusToolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ToolStripStatusFilterLabel
            // 
            this.ToolStripStatusFilterLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.ToolStripStatusFilterLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.ToolStripStatusFilterLabel.Name = "ToolStripStatusFilterLabel";
            this.ToolStripStatusFilterLabel.Size = new System.Drawing.Size(53, 19);
            this.ToolStripStatusFilterLabel.Text = "Filtered:";
            this.ToolStripStatusFilterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // StatusToolStripDriveFiltered
            // 
            this.StatusToolStripDriveFiltered.AutoSize = false;
            this.StatusToolStripDriveFiltered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.StatusToolStripDriveFiltered.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.StatusToolStripDriveFiltered.Name = "StatusToolStripDriveFiltered";
            this.StatusToolStripDriveFiltered.Size = new System.Drawing.Size(65, 19);
            this.StatusToolStripDriveFiltered.Text = "Drive";
            // 
            // StatusToolStripExtFiltered
            // 
            this.StatusToolStripExtFiltered.AutoSize = false;
            this.StatusToolStripExtFiltered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.StatusToolStripExtFiltered.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.StatusToolStripExtFiltered.Name = "StatusToolStripExtFiltered";
            this.StatusToolStripExtFiltered.Size = new System.Drawing.Size(65, 19);
            this.StatusToolStripExtFiltered.Text = "Extension";
            // 
            // StatusToolStripSubFiltered
            // 
            this.StatusToolStripSubFiltered.AutoSize = false;
            this.StatusToolStripSubFiltered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.StatusToolStripSubFiltered.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.StatusToolStripSubFiltered.Name = "StatusToolStripSubFiltered";
            this.StatusToolStripSubFiltered.Size = new System.Drawing.Size(53, 19);
            this.StatusToolStripSubFiltered.Text = "Custom";
            // 
            // SearchStatusToolStripStatusLabel
            // 
            this.SearchStatusToolStripStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.SearchStatusToolStripStatusLabel.Name = "SearchStatusToolStripStatusLabel";
            this.SearchStatusToolStripStatusLabel.Size = new System.Drawing.Size(94, 19);
            this.SearchStatusToolStripStatusLabel.Text = "Search Count: 0";
            // 
            // FilesAvailableToolStripStatusLabel
            // 
            this.FilesAvailableToolStripStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.FilesAvailableToolStripStatusLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.FilesAvailableToolStripStatusLabel.MergeAction = System.Windows.Forms.MergeAction.Replace;
            this.FilesAvailableToolStripStatusLabel.Name = "FilesAvailableToolStripStatusLabel";
            this.FilesAvailableToolStripStatusLabel.Size = new System.Drawing.Size(92, 19);
            this.FilesAvailableToolStripStatusLabel.Text = "Cache Count: 0";
            // 
            // PanelSearchBar
            // 
            this.PanelSearchBar.Controls.Add(this.PanelSearchText);
            this.PanelSearchBar.Controls.Add(this.panel2);
            this.PanelSearchBar.Controls.Add(this.PanelScanButton);
            this.PanelSearchBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelSearchBar.Location = new System.Drawing.Point(0, 24);
            this.PanelSearchBar.Name = "PanelSearchBar";
            this.PanelSearchBar.Size = new System.Drawing.Size(1154, 30);
            this.PanelSearchBar.TabIndex = 12;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.PanelFindButton);
            this.panel2.Controls.Add(this.BtnOptions);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Location = new System.Drawing.Point(1042, 0);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(3);
            this.panel2.Size = new System.Drawing.Size(112, 30);
            this.panel2.TabIndex = 6;
            // 
            // StartupTimer
            // 
            this.StartupTimer.Enabled = true;
            this.StartupTimer.Interval = 1;
            this.StartupTimer.Tick += new System.EventHandler(this.StartupTimer_Tick);
            // 
            // LastScanTimer
            // 
            this.LastScanTimer.Interval = 10000;
            this.LastScanTimer.Tick += new System.EventHandler(this.LastScanTimer_Tick);
            // 
            // Starter
            // 
            this.AcceptButton = this.BtnFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(1154, 645);
            this.Controls.Add(this.ResultsPanel);
            this.Controls.Add(this.PanelSearchBar);
            this.Controls.Add(this.StartupMenuStrip);
            this.Controls.Add(this.StartupStatusStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Starter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chizl.SystemSearch Framework 4.8.1 Demo";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Starter_FormClosing);
            this.Load += new System.EventHandler(this.Starter_Load);
            this.Move += new System.EventHandler(this.Starter_Move);
            this.Resize += new System.EventHandler(this.Starter_Resize);
            this.PanelSearchText.ResumeLayout(false);
            this.PanelSearchText.PerformLayout();
            this.PanelSearchAttrib.ResumeLayout(false);
            this.PanelFindButton.ResumeLayout(false);
            this.CMenuInfoErr.ResumeLayout(false);
            this.ResultsPanel.ResumeLayout(false);
            this.MainSplitContainer.Panel1.ResumeLayout(false);
            this.MainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).EndInit();
            this.MainSplitContainer.ResumeLayout(false);
            this.CMenuList.ResumeLayout(false);
            this.EventListsSplitContainer.Panel1.ResumeLayout(false);
            this.EventListsSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.EventListsSplitContainer)).EndInit();
            this.EventListsSplitContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.CMenuOptions.ResumeLayout(false);
            this.PanelScanButton.ResumeLayout(false);
            this.StartupMenuStrip.ResumeLayout(false);
            this.StartupMenuStrip.PerformLayout();
            this.StartupStatusStrip.ResumeLayout(false);
            this.StartupStatusStrip.PerformLayout();
            this.PanelSearchBar.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel PanelSearchText;
        private System.Windows.Forms.TextBox TxtSearchName;
        private System.Windows.Forms.Panel PanelFindButton;
        private System.Windows.Forms.Button BtnFind;
        private System.Windows.Forms.Button BtnOptions;
        private System.Windows.Forms.ListBox EventList;
        private System.Windows.Forms.Panel ResultsPanel;
        private System.Windows.Forms.ToolStripMenuItem ChkSystemFolder;
        private System.Windows.Forms.ContextMenuStrip CMenuOptions;
        private System.Windows.Forms.ToolStripMenuItem ChkFilename;
        private System.Windows.Forms.ToolStripMenuItem ChkDirectoryName;
        private System.Windows.Forms.ToolStripMenuItem MnuSkipFolders;
        private System.Windows.Forms.ToolStripMenuItem ChkRecycleBin;
        private System.Windows.Forms.ToolStripMenuItem ChkTempFolder;
        private System.Windows.Forms.ToolStripMenuItem ChkInternetCache;
        private System.Windows.Forms.ToolStripMenuItem ChkWinFolder;
        private System.Windows.Forms.ToolStripMenuItem ChkUserFolder;
        private System.Windows.Forms.Panel PanelScanButton;
        private System.Windows.Forms.Button BtnStartStopScan;
        private System.Windows.Forms.MenuStrip StartupMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.StatusStrip StartupStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusToolStripStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel SearchStatusToolStripStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel FilesAvailableToolStripStatusLabel;
        private System.Windows.Forms.Panel PanelSearchBar;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Timer StartupTimer;
        private System.Windows.Forms.ListBox ErrorList;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip CMenuList;
        private System.Windows.Forms.ToolStripMenuItem ListMenuFilters;
        private System.Windows.Forms.ToolStripMenuItem ListMenuFilterDrive;
        private System.Windows.Forms.ToolStripMenuItem ListMenuOpenLocation;
        private System.Windows.Forms.ToolStripMenuItem ListMenuFilterFileExtension;
        private System.Windows.Forms.ToolStripMenuItem ListMenuFilterClear;
        private System.Windows.Forms.ToolStripStatusLabel StatusToolStripDriveFiltered;
        private System.Windows.Forms.ToolStripStatusLabel ToolStripStatusFilterLabel;
        private System.Windows.Forms.ToolStripStatusLabel StatusToolStripExtFiltered;
        private System.Windows.Forms.ToolStripMenuItem ListMenuClearList;
        private System.Windows.Forms.ContextMenuStrip CMenuInfoErr;
        private System.Windows.Forms.ToolStripMenuItem CMenuInfoErrClear;
        private System.Windows.Forms.ToolStripMenuItem CMenuInfoErrRemoveType;
        private System.Windows.Forms.ToolStripMenuItem CMenuInfoErrCopy;
        private System.Windows.Forms.ToolStripMenuItem CMenuInfoErrFilter;
        private System.Windows.Forms.ToolStripMenuItem CMenuInfoErrIgnore;
        private System.Windows.Forms.Panel PanelSearchAttrib;
        private System.Windows.Forms.ComboBox CbAttribute;
        private System.Windows.Forms.ComboBox CbGtLtEq;
        private System.Windows.Forms.ToolStripMenuItem ListMenuExclude;
        private System.Windows.Forms.ToolStripStatusLabel StatusToolStripSubFiltered;
        private System.Windows.Forms.ListView ResultsListView;
        private System.Windows.Forms.Timer LastScanTimer;
        private System.Windows.Forms.SplitContainer EventListsSplitContainer;
        private System.Windows.Forms.SplitContainer MainSplitContainer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox ChkHideErrors;
    }
}

