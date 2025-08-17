using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Chizl.WinSearch
{
    public partial class SubFilterOptions : Form
    {
        private string _path = string.Empty;
        private List<string> _excludeItems = new List<string>();

        public string[] ExcludeItems { get; private set; } = new string[0];

        public SubFilterOptions(string path)
        {
            InitializeComponent();
            _path = path;
        }

        private void SubFilterOptions_Load(object sender, EventArgs e)
        {
            this.TextPath.Text = _path;
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            int start = this.TextPath.SelectionStart;
            int len = this.TextPath.SelectionLength;
            var sel = this.TextPath.SelectedText;
            if (!_excludeItems.Contains(sel))
            {
                _excludeItems.Add(sel);
                ListBoxSubFilters.Items.Add(sel);
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            ExcludeItems = _excludeItems.ToArray();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void toolStripMenuRemoveItem_Click(object sender, EventArgs e)
        {
            if (GetSelectedItem(out string selectedItem))
            {
                _excludeItems.Remove(selectedItem);
                ListBoxSubFilters.Items.RemoveAt(ListBoxSubFilters.SelectedIndex);
            }
        }
        private bool GetSelectedItem(out string selectedItem)
        {
            selectedItem = string.Empty;
            if (ListBoxSubFilters.SelectedIndex < 0)
                return false;

            selectedItem = ListBoxSubFilters.Items[ListBoxSubFilters.SelectedIndex].ToString();

            return true;
        }

        private void ListBoxSubFilters_MouseDown(object sender, MouseEventArgs e)
        {
            var id = ListBoxSubFilters.IndexFromPoint(e.Location);
            if (id >= 0)
                ListBoxSubFilters.SelectedIndex = id;
        }
    }
}
