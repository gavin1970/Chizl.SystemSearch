using Chizl.SearchSystemUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Chizl.WinSearch
{
    public partial class SubFilterOptions : Form
    {
        private readonly Color _bgAlwaysExclude = Color.FromArgb(255, 192, 192);
        private readonly ListViewHelper _lViewHelper = new ListViewHelper();
        private ColumnHeader[] _listViewColumns = new ColumnHeader[0];
        private bool _loaded = false;

        private readonly List<string> _removedFromExclusionItems = new List<string>();

        private string _path = string.Empty;
        private List<SubFilterExclusion> _excludeItems = new List<SubFilterExclusion>();
        private List<SubFilterExclusion> _alwaysExcludeItems = new List<SubFilterExclusion>();

        public SubFilterOptions(string path)
        {
            InitializeComponent();
            _path = string.IsNullOrWhiteSpace(path) ? "" : path;
        }

        /// <summary>
        /// Accessible from the outside.
        /// </summary>
        public List<SubFilterExclusion> ExcludeItems { get { return _excludeItems; } }
        public List<SubFilterExclusion> AlwaysExcludeItems { get { return _alwaysExcludeItems; } }
        public List<string> RemovedFromExcludeItems { get { return _removedFromExclusionItems; } }

        private void SubFilterOptions_Load(object sender, EventArgs e)
        {
            TextPath.Text = _path;
            SetupListView(ListViewSubFilters, ListViewColumns());
            RefreshListView();
        }
        private void RefreshListView()
        {
            ListViewSubFilters.SuspendLayout();

            if (ListViewSubFilters.Items.Count > 0)
                ListViewSubFilters.Items.Clear();

            if (_excludeItems.Count > 0)
                ListViewSubFilters.Items.AddRange(GlobalSetup.GetFilterInfo(_excludeItems.ToArray()));

            if (_alwaysExcludeItems.Count == 0)
            {
                var exList = GlobalSetup.GetScanExclusions().Result;
                if (exList.Length > 0)
                    _alwaysExcludeItems.AddRange(exList.Select(s => new SubFilterExclusion(s, FilterType.Contains)));
            }

            if (_alwaysExcludeItems.Count > 0)
                ListViewSubFilters.Items.AddRange(GlobalSetup.GetFilterInfo(_alwaysExcludeItems.ToArray(), _bgAlwaysExclude));

            ListViewSubFilters.ResumeLayout(true);
            ListViewSubFilters.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextPath.Text))
                return;

            int start = TextPath.SelectionStart;
            int len = TextPath.SelectionLength;
            var sel = TextPath.SelectedText;

            if (string.IsNullOrWhiteSpace(sel))
            {
                TextPath.SelectAll();
                start = TextPath.SelectionStart;
                len = TextPath.SelectionLength;
                sel = TextPath.SelectedText;
            }

            if (string.IsNullOrWhiteSpace(sel))
                return;

            // add to temp if not exists, but if cancel is clicked,
            // we don't want to add it to the return list.
            if (_excludeItems.Where(w => w.FilterRaw.Equals(sel)).Count() == 0)
            {
                _excludeItems.Add(new SubFilterExclusion(sel, FilterType.Unknown));
                RefreshListView();
            }
        }
        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        private void ButtonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        private void toolStripMenuRemoveItem_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem, out _))
            {
                _removedFromExclusionItems.Add(selectedItem);
                _excludeItems = _excludeItems.Where(w => !w.FilterRaw.Equals(selectedItem)).ToList();
                if (GlobalSetup.RemoveScanExclusion(selectedItem).Result)
                    _alwaysExcludeItems = _alwaysExcludeItems.Where(w => !w.FilterRaw.Equals(selectedItem)).ToList();
                ListViewSubFilters.Items.RemoveAt(ListViewSubFilters.SelectedIndices[0]);
            }
        }

        //private void ListBoxSubFilters_MouseDown(object sender, MouseEventArgs e)
        //{
        //    var id = ListBoxSubFilters.IndexFromPoint(e.Location);
        //    if (id >= 0)
        //        ListBoxSubFilters.SelectedIndex = id;
        //}

        private bool GetSelectedItem(out string selectedItem, out int index)
        {
            selectedItem = string.Empty;
            index = -1;

            if (ListViewSubFilters.SelectedItems.Count == 0)
                return false;

            var item = ListViewSubFilters.SelectedItems[0];

            selectedItem = item.Text;
            index = item.Index;

            return true;
        }

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
                lv.MultiSelect = false;
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

            if (lv.Columns.Count == 0 && ch.Length > 0)
            {
                lv.Columns.AddRange(ch);
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }
        internal ColumnHeader[] ListViewColumns()
        {
            ColumnHeader[] cols = new ColumnHeader[]
            {
                new ColumnHeader
                {
                    Name = "FilterText",
                    Text = "Filter",
                    Width = 200,
                    TextAlign = HorizontalAlignment.Left,
                },
                new ColumnHeader
                {
                    Name = "FilterType",
                    Text = "Filter Type",
                    Width = 50,
                },
            };

            return cols;
        }
        #endregion

        private void toolStripMenuAlwaysExclude_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem, out int ndx))
                AlwaysExclude(selectedItem, ndx);
        }

        private void AlwaysExclude(string item, int ndx = -1)
        {
            if (GlobalSetup.AddScanExclusion(item).Result)
            {
                _excludeItems = _excludeItems.Where(w => !w.FilterRaw.Equals(item)).ToList();
                _alwaysExcludeItems.Add(new SubFilterExclusion(item, FilterType.Contains));
                if (ndx > -1)
                    ListViewSubFilters.Items[ndx].BackColor = _bgAlwaysExclude;
            }
        }
    }
}
