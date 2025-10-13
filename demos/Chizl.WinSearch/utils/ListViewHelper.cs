using System;
using System.Collections;
using System.Collections.Generic;
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
    public ListViewColumnSorter LvColumnSorter { get; set; }
    private void ListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
    {
        ListView lv = (ListView)sender;
        e.NewWidth = lv.Columns[e.ColumnIndex].Width;
        e.Cancel = true;
    }
    public void SetupListView(ListView lv, List<string> columns, ListViewOptions lvo = null)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        if (lvo == null)
            lvo = new ListViewOptions();

        DefaultListView(lv, lvo);

        if (lv.Columns.Count == 0)
        {
            int i = 0;

            ColumnHeader[] cols = new ColumnHeader[columns.Count];

            foreach (string colName in columns)
            {
                cols[i++] = new ColumnHeader
                {
                    Text = colName,
                    TextAlign = HorizontalAlignment.Left
                };
            }

            lv.Columns.AddRange(cols);
            if (lvo.HideHeader)
                lv.HeaderStyle = ColumnHeaderStyle.None;

            ListView_ResizeGrid(lv, lvo.HideFirstCol, lvo.AutoSizeLastCol);
        }
    }
    /// <summary>
    /// sets default settings for any listView passed to it.
    /// </summary>
    /// <param name="lv"></param>
    private void DefaultListView(ListView lv, ListViewOptions lvo = null)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        if (lvo == null)
            lvo = new ListViewOptions();

        if (lv.View != View.Details)
        {
            lv.AllowColumnReorder = lvo.AllowColumnReorder;
            // We want to row sorting for Status List, but do for Item List.
            lv.HeaderStyle = (lvo.HeaderClickable || lvo.AllowColumnReorder ? ColumnHeaderStyle.Clickable : ColumnHeaderStyle.Nonclickable);
            // Make the border obvious
            lv.BorderStyle = BorderStyle.FixedSingle;
            // Set the view to show details.
            lv.View = View.Details;
            // Allow the user to edit item text.
            lv.LabelEdit = false;
            // Allow the user to rearrange columns.
            lv.AllowColumnReorder = false;
            // Display check boxes.
            lv.CheckBoxes = lvo.CheckBoxes;
            // Select the item and subitems when selection is made.
            lv.FullRowSelect = true;
            // Display grid lines.
            lv.GridLines = true;
            // Large Image List
            lv.LargeImageList = lvo.ImageListLarge;
            // Small Image List
            lv.SmallImageList = lvo.ImageListSmall;
            // Setup sorting, if Allow Column Reorder is working..
            if (lvo.AllowColumnReorder)
            {
                LvColumnSorter = new ListViewColumnSorter();
                lv.ListViewItemSorter = LvColumnSorter;
            }

            if (!lvo.ColSizable)
                lv.ColumnWidthChanging += new ColumnWidthChangingEventHandler(ListView_ColumnWidthChanging);
        }
    }
    public void ListView_Column_Sort(ListView lv, ColumnClickEventArgs e)
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
    public void ListView_ResizeGrid(ListView lv, bool hideFirstCol = true, bool autoSizeLastCol = true)
    {
        if (lv.IsDisposed || lv.Disposing)
            return;

        lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        if (hideFirstCol)
            lv.Columns[0].Width = 0;
        if (autoSizeLastCol)
            lv.AutoResizeColumn(lv.Columns.Count - 1, ColumnHeaderAutoResizeStyle.HeaderSize);
        if (!hideFirstCol && !autoSizeLastCol)
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
    public bool HideFirstCol { get; set; }
    public bool HideHeader { get; set; }
    public bool CheckBoxes { get; set; }
    public bool HeaderClickable { get; set; }
    public bool ColSizable { get; set; }
    public ImageList ImageListLarge { get; set; }
    public ImageList ImageListSmall { get; set; }
    public ListViewOptions(bool allowColumnReorder = false, bool autoSizeLastCol = true, bool hideFirstCol = true,
                            bool hideHeader = false, bool checkBoxes = false,
                            bool headerClickable = false, bool colSizable = false, ImageList imageListLarge = null,
                            ImageList imageListSmall = null)
    {
        AllowColumnReorder = allowColumnReorder;
        AutoSizeLastCol = autoSizeLastCol;
        HideFirstCol = hideFirstCol;
        HideHeader = hideHeader;
        CheckBoxes = checkBoxes;
        HeaderClickable = headerClickable;
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