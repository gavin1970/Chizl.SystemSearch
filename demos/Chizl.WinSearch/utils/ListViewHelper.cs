using Chizl.ThreadSupport;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public enum LISTVIEW_ACTION
{
    SEARCH,
    ADD,
    ADD_DUPLICATE,
    REMOVE
}

public class ListViewHelper
{
    //unable to get virtual to work, so I use this flag to disable for now.
    readonly Bool _virtual = Bool.False;
    readonly Bool _loaded = Bool.False;

    #region Virtual: If true True
    public ListViewItem[] _myCache;
    private int _firstItem; // Index of the first item in the cache
    private const int _cacheSize = 100; // Define a suitable cache size
    private const int _totalItems = 100000; // Define the total number of items
    #endregion

    public ListViewColumnSorter LvColumnSorter { get; set; }
    private void Lv_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
    {
        ListView lv = (ListView)sender;
        e.NewWidth = lv.Columns[e.ColumnIndex].Width;
        e.Cancel = true;
    }
    public void SetupListView(ListView lv, ColumnHeader[] columns, ListViewOptions lvo = null)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        if (lvo == null)
            _listViewOptions = new ListViewOptions();
        else
            _listViewOptions = lvo;

        DefaultListView(lv);

        if (lv.Columns.Count == 0)
        {
            lv.Columns.AddRange(columns);
            if (_listViewOptions.HideHeader)
                lv.HeaderStyle = ColumnHeaderStyle.None;

            Lv_ResizeGrid(lv, _listViewOptions.HideColumns.Select(s=>(uint)s.DisplayIndex).ToArray(), _listViewOptions.AutoSizeLastCol);

            if (lv.Columns.Count == 0)
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
    }
    private ListViewOptions _listViewOptions { get; set; }
    /// <summary>
    /// sets default settings for any listView passed to it.
    /// </summary>
    /// <param name="lv"></param>
    private void DefaultListView(ListView lv)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        if (_listViewOptions == null)
            _listViewOptions = new ListViewOptions();

        if (lv.View != View.Details)
        {
            // setup for sorting..
             lv.HeaderStyle = ColumnHeaderStyle.Clickable;

            // We want to row sorting for Status List, but do for Item List.
            //lv.HeaderStyle = (_listViewOptions.HeaderClickable || _listViewOptions.AllowColumnReorder ? ColumnHeaderStyle.Clickable : ColumnHeaderStyle.Nonclickable);
            // Make the border obvious
            lv.BorderStyle = BorderStyle.FixedSingle;

            // sort based on column click
            lv.ColumnClick += Lv_ColumnClick;
            // resizes last column, when any other columns are changed.
            lv.ColumnWidthChanged += Lv_ColumnWidthChanged;

            // removed multi-select
            lv.MultiSelect = true;
            // allow scrolls
            lv.Scrollable = true;

            // Set the view to show details.
            lv.View = View.Details;
            // Allow the user to edit item text.
            lv.LabelEdit = false;
            // Allow the user to rearrange columns.
            lv.AllowColumnReorder = false;// _listViewOptions.AllowColumnReorder;
            // Display check boxes.
            lv.CheckBoxes = false;// _listViewOptions.CheckBoxes;
            // Select the item and subitems when selection is made.
            lv.FullRowSelect = true;
            // Display grid lines.
            lv.GridLines = true;
            // Sort the items in the list in ascending order.
            lv.Sorting = SortOrder.Descending;
            // Text color
            lv.ForeColor = Color.Black;

            //// Large Image List
            //lv.LargeImageList = _listViewOptions.ImageListLarge;
            //// Small Image List
            //lv.SmallImageList = _listViewOptions.ImageListSmall;

            #region Virtual: True
            // Setup Virtual, easier to set view state for large data.
            lv.VirtualMode = _virtual.Value;
            if (_virtual)
            {
                // Set the total count of items
                lv.VirtualListSize = _totalItems;
                // Hook up the required events
                lv.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler(Lv_RetrieveVirtualItem);
                // Caching is optional but recommended for performance
                lv.CacheVirtualItems += new CacheVirtualItemsEventHandler(Lv_CacheVirtualItems);
            }
            #endregion

            // Setup sorting, if Allow Column Reorder is working..
            if (_listViewOptions.AllowColumnReorder)
            {
                LvColumnSorter = new ListViewColumnSorter();
                lv.ListViewItemSorter = LvColumnSorter;
            }

            if (!_listViewOptions.ColSizable)
                lv.ColumnWidthChanging += new ColumnWidthChangingEventHandler(Lv_ColumnWidthChanging);
        }
    }

    private void Lv_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        var lv = (ListView)sender;
        ColumnClickEventArgs colClickEvtArgs = e;

        if (e.Column == 2)
            colClickEvtArgs = new ColumnClickEventArgs(1);

        Lv_Column_Sort(lv, colClickEvtArgs);
    }

    private void Lv_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
    {
        if (_loaded.TrySetTrue())
        {
            var lv = (ListView)sender;

            if (lv.Items.Count > 0 && lv.Columns.Count > 0 && e.ColumnIndex != lv.Columns.Count - 1)
            {
                if (_listViewOptions.HideColumns != null)
                {
                    foreach (var col in _listViewOptions.HideColumns)
                        lv.Columns[col.DisplayIndex].Width = 0;
                }

                lv.AutoResizeColumn(lv.Columns.Count - 1, ColumnHeaderAutoResizeStyle.HeaderSize);
            }

            _loaded.SetFalse();
        }
    }

    #region Virtual: True - View Point with ListView - Helps Millions of records in one ListView with speed.
    public void Lv_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
        if (!_virtual)
            return;

        // Check if the requested item is in the cache
        if (_myCache != null && e.ItemIndex >= _firstItem && e.ItemIndex < _firstItem + _myCache.Length)
        {
            // A cache hit, so get the ListViewItem from the cache
            e.Item = _myCache[e.ItemIndex - _firstItem];
        }
        else
        {
            // A cache miss, so create a new ListViewItem
            // In a real application, you would fetch data from your data source here
            int itemValue = e.ItemIndex * e.ItemIndex;
            ListViewItem lvi = new ListViewItem(itemValue.ToString());
            lvi.SubItems.Add(e.ItemIndex.ToString());
            e.Item = lvi;
        }
    }

    public void Lv_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
    {
        if (!_virtual)
            return;

        // If the requested range is outside the current cache, rebuild the cache
        if (_myCache == null || e.StartIndex < _firstItem || e.EndIndex >= _firstItem + _myCache.Length)
        {
            _firstItem = e.StartIndex;
            _myCache = new ListViewItem[_cacheSize];

            // Fill the cache
            for (int i = 0; i < _cacheSize; i++)
            {
                int itemIndex = _firstItem + i;
                if (itemIndex >= 0 && itemIndex < _totalItems)
                {
                    int itemValue = itemIndex * itemIndex;
                    ListViewItem lvi = new ListViewItem(itemValue.ToString());
                    lvi.SubItems.Add(itemIndex.ToString());
                    _myCache[i] = lvi;
                }
                else
                {
                    // Handle edge cases if necessary
                    break;
                }
            }
        }
    }
    #endregion

    public void Lv_Column_Sort(ListView lv, ColumnClickEventArgs e)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        if (e.Column == LvColumnSorter.SortColumn)
        {
            // Reverse the current sort direction for this column.
            if (LvColumnSorter.Order == SortOrder.Ascending)
            {
                LvColumnSorter.Order = SortOrder.Descending;
            }
            else
            {
                LvColumnSorter.Order = SortOrder.Ascending;
            }
        }
        else
        {
            // Set the column number that is to be sorted; default to ascending.
            LvColumnSorter.SortColumn = e.Column;
            LvColumnSorter.Order = SortOrder.Ascending;
        }

        // Perform the sort with these new sort options.
        lv.Sort();
    }
    
    public void Lv_ResizeGrid(ListView lv, uint[] hideColumnIndex = null, bool autoSizeLastCol = true)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        var hasHiddenColumns = hideColumnIndex != null && hideColumnIndex.Length > 0;

        if (hasHiddenColumns)
        {
            foreach (int ndx in hideColumnIndex)
            {
                if (ndx < lv.Columns.Count)
                    lv.Columns[ndx].Width = 0;
            }
        }

        if(!_virtual)
            lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

        if (autoSizeLastCol && !_virtual)
            lv.AutoResizeColumn(lv.Columns.Count - 1, ColumnHeaderAutoResizeStyle.HeaderSize);
        if (!hasHiddenColumns && !autoSizeLastCol)
            lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
    }

    public bool ListViewSearch(ListView lv, string findText, LISTVIEW_ACTION action = LISTVIEW_ACTION.SEARCH, ListViewItem lvItem = null)
    {
        bool retVal = false;
        ListViewItem foundItem = null;
        if (lv.Items.Count > 0)
            foundItem = lv.FindItemWithText(findText, true, 0);

        switch (action)
        {
            case LISTVIEW_ACTION.SEARCH:
                if (foundItem != null)
                    retVal = true;
                break;
            case LISTVIEW_ACTION.ADD_DUPLICATE:
            case LISTVIEW_ACTION.ADD:
                if (lvItem != null)
                {
                    if (foundItem == null ||
                        action == LISTVIEW_ACTION.ADD_DUPLICATE)
                    {
                        lv.Items.Add(lvItem);
                        retVal = true;
                    }
                }
                break;
            case LISTVIEW_ACTION.REMOVE:
                if (foundItem != null)
                {
                    lv.Items.Remove(foundItem);
                    retVal = true;
                }
                break;
        }

        return retVal;
    }
}

public class ListViewOptions
{
    public bool AllowColumnReorder { get; set; }
    public bool AutoSizeLastCol { get; set; }
    public bool HideHeader { get; set; }
    public bool CheckBoxes { get; set; }
    public bool HeaderClickable { get; set; }
    public bool ColSizable { get; set; }
    public ColumnHeader[] HideColumns { get; set; }
    public ImageList ImageListLarge { get; set; }
    public ImageList ImageListSmall { get; set; }
    public ListViewOptions(bool allowColumnReorder = false, bool autoSizeLastCol = true, bool hideHeader = false, 
                            bool checkBoxes = false, bool headerClickable = false, bool colSizable = false,
                            ColumnHeader[] hideColumns = null, ImageList imageListLarge = null, ImageList imageListSmall = null)
    {
        AllowColumnReorder = allowColumnReorder;
        AutoSizeLastCol = autoSizeLastCol;
        HideHeader = hideHeader;
        CheckBoxes = checkBoxes;
        HeaderClickable = headerClickable;
        HideColumns = hideColumns;
        ImageListLarge = imageListLarge;
        ImageListSmall = imageListSmall;
        ColSizable = colSizable;
    }
}

/// <summary>
/// This class is an implementation of the 'IComparer' interface.
/// </summary>
public class ListViewColumnSorter : IComparer
{
    /// <summary>
    /// Specifies the column to be sorted
    /// </summary>
    private int ColumnToSort;
    /// <summary>
    /// Specifies the order in which to sort (i.e. 'Ascending').
    /// </summary>
    private SortOrder OrderOfSort;
    /// <summary>
    /// Case insensitive comparer object
    /// </summary>
    private CaseInsensitiveComparer ObjectCompare;
    /// <summary>
    /// Class constructor. Initializes various elements
    /// </summary>
    public ListViewColumnSorter()
    {
        // Initialize the column to '0'
        ColumnToSort = 0;

        // Initialize the sort order to 'none'
        OrderOfSort = SortOrder.None;

        // Initialize the CaseInsensitiveComparer object
        ObjectCompare = new CaseInsensitiveComparer();
    }
    /// <summary>
    /// This method is inherited from the IComparer interface. It compares the two objects passed using a case insensitive comparison.
    /// </summary>
    /// <param name="x">First object to be compared</param>
    /// <param name="y">Second object to be compared</param>
    /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
    public int Compare(object x, object y)
    {
        int compareResult;
        // get string values from parameters
        string xVal = ((ListViewItem)x).SubItems[ColumnToSort].Text;
        string yVal = ((ListViewItem)y).SubItems[ColumnToSort].Text;

        // check if values are int, long, double, or floats
        if (float.TryParse(xVal, out float xFloat) && float.TryParse(yVal, out float yFloat))
            compareResult = ObjectCompare.Compare(xFloat, yFloat);
        // check if values are dates
        else if (DateTime.TryParse(xVal, out DateTime xDateTime) && DateTime.TryParse(yVal, out DateTime yDateTime))
            compareResult = ObjectCompare.Compare(xDateTime, yDateTime);
        // compare as strings
        else
            compareResult = ObjectCompare.Compare(xVal, yVal);

        // Calculate correct return value based on object comparison
        if (OrderOfSort == SortOrder.Ascending)
        {
            // Ascending sort is selected, return normal result of compare operation
            return compareResult;
        }
        else if (OrderOfSort == SortOrder.Descending)
        {
            // Descending sort is selected, return negative result of compare operation
            return (-compareResult);
        }
        else
        {
            // Return '0' to indicate they are equal
            return 0;
        }
    }
    /// <summary>
    /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
    /// </summary>
    public int SortColumn
    {
        set
        {
            ColumnToSort = value;
        }
        get
        {
            return ColumnToSort;
        }
    }
    /// <summary>
    /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
    /// </summary>
    public SortOrder Order
    {
        set
        {
            OrderOfSort = value;
        }
        get
        {
            return OrderOfSort;
        }
    }
}