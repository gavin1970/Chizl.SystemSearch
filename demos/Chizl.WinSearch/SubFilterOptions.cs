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
        private readonly ListViewHelper _lViewHelper = new ListViewHelper();
        private ColumnHeader[] _listViewColumns = new ColumnHeader[0];
        private bool _loaded = false;

        private readonly List<string> _removedFromExclusionItems = new List<string>();

        private string _path = string.Empty;
        private List<SubFilterExclusion> _excludeItems = new List<SubFilterExclusion>();

        public SubFilterOptions(string path)
        {
            InitializeComponent();
            _path = path;
        }

        /// <summary>
        /// Accessible from the outside.
        /// </summary>
        public List<SubFilterExclusion> ExcludeItems { get { return _excludeItems; } }
        public List<string> RemovedFromExcludeItems { get { return _removedFromExclusionItems; } }

        private void SubFilterOptions_Load(object sender, EventArgs e)
        {
            this.TextPath.Text = _path;
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

            ListViewSubFilters.ResumeLayout(true);
            ListViewSubFilters.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            int start = this.TextPath.SelectionStart;
            int len = this.TextPath.SelectionLength;
            var sel = this.TextPath.SelectedText;

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
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void ButtonOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void toolStripMenuRemoveItem_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
            {
                _removedFromExclusionItems.Add(selectedItem);
                _excludeItems = _excludeItems.Where(w => !w.FilterRaw.Equals(selectedItem)).ToList();
                ListViewSubFilters.Items.RemoveAt(ListViewSubFilters.SelectedIndices[0]);
            }
        }
        //private void ListBoxSubFilters_MouseDown(object sender, MouseEventArgs e)
        //{
        //    var id = ListBoxSubFilters.IndexFromPoint(e.Location);
        //    if (id >= 0)
        //        ListBoxSubFilters.SelectedIndex = id;
        //}

        private bool GetSelectedItem(out string selectedItem)
        {
            selectedItem = string.Empty;
            if (ListViewSubFilters.SelectedItems.Count < 0)
                return false;

            selectedItem = ListViewSubFilters.SelectedItems[0].Text;
            
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
    }
}
