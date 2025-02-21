using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace DataGridViewGeneric
{
    public partial class FindAndReplaceForm : KryptonForm
    {
        public DataGridView dataGridViewActive { get; set; }
        private int searchSelectionIndexSelectionFind;
        private List<DataGridViewCell> m_SelectedCells;
        private bool isSearchModeFindInSelectedCells = true;

        private Dictionary<CellLocation, DataGridViewGenericCell> updatedCells;

        #region FindAndReplaceForm
        public FindAndReplaceForm(DataGridView dataGridView, bool setReplaceTabAsActive)
        {
            m_SelectedCells = new List<DataGridViewCell>();
            InitializeComponent();            
            InitializeForm(dataGridView, setReplaceTabAsActive);
        }
        #endregion

        #region InitializeForm
        public void InitializeForm( DataGridView dataGridView, bool setReplaceTabAsActive)
        {
            if (dataGridViewActive != dataGridView) dataGridViewActive = dataGridView;
            
            comboBoxFindMode.SelectedIndex = 0;
            searchSelectionIndexSelectionFind = 0;

            m_SelectedCells.Clear();

            var array = new DataGridViewCell[dataGridViewActive.SelectedCells.Count];
            dataGridViewActive.SelectedCells.CopyTo(array, 0);
            m_SelectedCells = array.OrderBy(r => r.RowIndex).ThenBy(c => c.ColumnIndex).ToList<DataGridViewCell>();

            if (dataGridView.SelectedCells.Count == 1)
            {
                this.Text = "Find and Replace text in table";
                isSearchModeFindInSelectedCells = false;
                checkBoxSearchAlsoRowHeaders.Visible = true;
                checkBoxSearchAlsoRowHeaders.Checked = true;
                if (dataGridViewActive.SelectedCells[dataGridViewActive.SelectedCells.Count - 1].Value != null)
                    this.FindWhatTextBox1.Text = dataGridViewActive.SelectedCells[dataGridViewActive.SelectedCells.Count - 1].Value.ToString();
            }
            else
            {
                this.Text = "Find and Replace text in selected cells";
                isSearchModeFindInSelectedCells = true;
                checkBoxSearchAlsoRowHeaders.Visible = false;
                checkBoxSearchAlsoRowHeaders.Checked = false;
            }

            if (setReplaceTabAsActive)
            {
                kryptonNavigatorFindAndReplace.SelectedIndex = 1;
                FindWhatTextBox2.Focus();
                FindWhatTextBox2.Select();
            }
            else
            {
                kryptonNavigatorFindAndReplace.SelectedIndex = 0;
                FindWhatTextBox1.Focus();
                FindWhatTextBox1.Select();
            }

            if (dataGridViewActive.CurrentCell != null) updatedCells = new Dictionary<CellLocation, DataGridViewGenericCell>();
        }
        #endregion

        #region Sync Find with Find and Replace tab
        void FindWhatTextBox1_TextChanged(object sender, System.EventArgs e)
        {
            this.FindWhatTextBox2.Text = this.FindWhatTextBox1.Text;
        }

        void FindWhatTextBox2_TextChanged(object sender, System.EventArgs e)
        {
            this.FindWhatTextBox1.Text = this.FindWhatTextBox2.Text;
        }

        void MatchCaseCheckBox1_CheckedChanged(object sender, System.EventArgs e)
        {
            this.MatchCaseCheckBox2.Checked = this.MatchCaseCheckBox1.Checked;
        }

        void MatchCaseCheckBox2_CheckedChanged(object sender, System.EventArgs e)
        {
            this.MatchCaseCheckBox1.Checked = this.MatchCaseCheckBox2.Checked;
        }

        void MatchCellCheckBox1_CheckedChanged(object sender, System.EventArgs e)
        {
            this.MatchCellCheckBox2.Checked = this.MatchCellCheckBox1.Checked;
        }

        void MatchCellCheckBox2_CheckedChanged(object sender, System.EventArgs e)
        {
            this.MatchCellCheckBox1.Checked = this.MatchCellCheckBox2.Checked;
        }

        void UseComboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.UseComboBox2.SelectedIndex = this.comboBoxFindMode.SelectedIndex;
        }

        void UseComboBox2_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.comboBoxFindMode.SelectedIndex = this.UseComboBox2.SelectedIndex;
        }

        private void radioButtonSearchUp1_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonSearchUp2.Checked = radioButtonSearchUp1.Checked;
        }

        private void radioButtonSearchUp2_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonSearchUp1.Checked = radioButtonSearchUp2.Checked;
        }

        private void radioButtonSearchDown1_CheckedChanged(object sender, EventArgs e)
        {            
            radioButtonSearchDown2.Checked = radioButtonSearchDown1.Checked;
        }

        private void radioButtonSearchDown2_CheckedChanged(object sender, EventArgs e)
        {            
            radioButtonSearchDown1.Checked = radioButtonSearchDown2.Checked;
        }


        #endregion

        #region FindButton2_Click
        void FindButton2_Click(object sender, System.EventArgs e)
        {
            FindButton1_Click(sender, e);
        }
        #endregion

        #region FindButton1_Click
        void FindButton1_Click(object sender, System.EventArgs e)
        {
            DataGridViewCell findCell = null;
            if (isSearchModeFindInSelectedCells)
                findCell = FindAndReplaceInSelection(false, true, false, null);
            else
                findCell = FindAndReplaceInTable(false, true, false, this.checkBoxSearchAlsoRowHeaders.Checked, null);


            if (findCell != null)
            {
                //lastCellLocationUsed = findCell;  //When close, this cell will become active

                dataGridViewActive.ClearSelection();
                dataGridViewActive.CurrentCell = findCell;
            }
        }
        #endregion

        #region buttonFindAll2_Click
        private void buttonFindAll2_Click(object sender, EventArgs e)
        {
            buttonFindAll1_Click(sender, e);
        }
        #endregion

        #region buttonFindAll1_Click
        private void buttonFindAll1_Click(object sender, EventArgs e)
        {
            DataGridViewCell FindCell = null;
            if (isSearchModeFindInSelectedCells)
                FindCell = FindAndReplaceInSelection(false, false, true, null); //Just mark cells where text is found
            else
                FindCell = FindAndReplaceInTable(false, false, true, this.checkBoxSearchAlsoRowHeaders.Checked, null); //Just mark cells where text is found
            Close();
        }
        #endregion

        #region ReplaceAllButton_Click
        void ReplaceAllButton_Click(object sender, System.EventArgs e)
        {
            DataGridViewCell FindCell = null;

            if (isSearchModeFindInSelectedCells)
                FindCell = FindAndReplaceInSelection(true, false, true, this.ReplaceWithTextBox.Text);
            else
                FindCell = FindAndReplaceInTable(true, false, true, false, this.ReplaceWithTextBox.Text);

            Close();
        }
        #endregion

        #region ReplaceButton_Click
        void ReplaceButton_Click(object sender, System.EventArgs e)
        {
            DataGridViewCell findCell = null;
            if (isSearchModeFindInSelectedCells)
                findCell = FindAndReplaceInSelection(true, true, false, this.ReplaceWithTextBox.Text);
            else
                findCell = FindAndReplaceInTable(true, true, false, false, this.ReplaceWithTextBox.Text);
            
            if (findCell != null)
            {
                dataGridViewActive.ClearSelection();
                dataGridViewActive.CurrentCell = findCell;
            }
        }
        #endregion

        #region FindAndReplaceInSelection
        DataGridViewCell FindAndReplaceInSelection(bool bReplace, bool bStopOnFind, bool markCell, String replaceString)
        {
            
            // Search criterions
            String sFindWhat = this.FindWhatTextBox1.Text;
            bool bMatchCase = this.MatchCaseCheckBox1.Checked;
            bool bMatchCell = this.MatchCellCheckBox1.Checked;
            bool bSearchUp = this.radioButtonSearchUp1.Checked;
            int iSearchMethod = this.comboBoxFindMode.SelectedIndex;

            bool bSearchAndReplace = bReplace;

            // If out of bound 

            int iSearchIndex;
            if (bSearchUp) iSearchIndex = (m_SelectedCells.Count - 1) - searchSelectionIndexSelectionFind;
            else iSearchIndex = searchSelectionIndexSelectionFind;

            int indexStart = searchSelectionIndexSelectionFind;

            //If show lot of selection, then don't replace cell content, just fint first cell that can become replace
            
            //Replace text in active cell
            if (bReplace && dataGridViewActive.SelectedCells.Count == 0) FindAndReplaceString(bReplace, m_SelectedCells[iSearchIndex], sFindWhat, replaceString, bMatchCase, bMatchCell, iSearchMethod);
            
            do
            {
                DataGridViewCell FindCell = null;


                if (bStopOnFind && dataGridViewActive.SelectedCells.Count > 1)
                    bReplace = false;                       //Find first cell
                else
                    searchSelectionIndexSelectionFind++;    //Find next cell

                if (searchSelectionIndexSelectionFind > m_SelectedCells.Count - 1) searchSelectionIndexSelectionFind = 0;


                //Convert to counter to cell index
                if (bSearchUp) iSearchIndex = (m_SelectedCells.Count - 1) - searchSelectionIndexSelectionFind;
                else iSearchIndex = searchSelectionIndexSelectionFind;

                if (!(bSearchAndReplace && m_SelectedCells[iSearchIndex].ReadOnly))
                {
                    //m_SelectedCells[iSearchIndex].Selected = false;
                    if (bStopOnFind) bReplace = false;
                    if (FindAndReplaceString(bReplace, m_SelectedCells[iSearchIndex], sFindWhat, replaceString, bMatchCase, bMatchCell, iSearchMethod))
                    {
                        FindCell = m_SelectedCells[iSearchIndex];
                        //if (markCell) FindCell.Selected = true;
                        if ((markCell && !bReplace) || (markCell && bReplace && !FindCell.ReadOnly)) 
                            FindCell.Selected = true;
                        //bReplace, bool bStopOnFind, bool markCell
                       
                    }
                    //else FindCell.Selected = true;
                }

                if (bStopOnFind && FindCell != null)
                {                    
                    return FindCell;
                }

            } while (searchSelectionIndexSelectionFind != indexStart);

            return null;
        }
        #endregion

        #region FindAndReplaceInTable
        DataGridViewCell FindAndReplaceInTable(bool bReplace, bool bStopOnFind, bool markCell, bool bSearchRow, String replaceString)
        {
            if (dataGridViewActive.CurrentCell == null) return null;
            
            // Search criterions
            String sFindWhat = this.FindWhatTextBox1.Text;
            bool bMatchCase = this.MatchCaseCheckBox1.Checked;
            bool bMatchCell = this.MatchCellCheckBox1.Checked;
            bool bSearchUp = this.radioButtonSearchUp1.Checked;
            int iSearchMethod = this.comboBoxFindMode.SelectedIndex;

            // Start of search            
            int iSearchStartRow = dataGridViewActive.CurrentCell.RowIndex;
            int iSearchStartColumn = dataGridViewActive.CurrentCell.ColumnIndex;
            int iRowIndex = dataGridViewActive.CurrentCell.RowIndex;
            int iColIndex = dataGridViewActive.CurrentCell.ColumnIndex;

            bool bSearchAndReplace = bReplace;

            //When "Find and Replace" - If found text search for in currect cell, the replace the text.
            if (bReplace) FindAndReplaceString(bReplace, dataGridViewActive[iColIndex, iRowIndex], sFindWhat, replaceString, bMatchCase, bMatchCell, iSearchMethod);

            bool rowChanged = true;
            do
            {
                if (bSearchUp)
                    iColIndex--;
                else
                    iColIndex++;

                if (iColIndex >= dataGridViewActive.ColumnCount)
                {
                    iColIndex = 0;
                    iRowIndex++;
                    rowChanged = true;
                }
                else if (iColIndex < 0)
                {
                    iColIndex = dataGridViewActive.ColumnCount - 1;
                    iRowIndex--;
                    rowChanged = true;
                }
                if (iRowIndex >= dataGridViewActive.RowCount)
                {
                    iRowIndex = 0;
                    rowChanged = true;
                }
                else if (iRowIndex < 0)
                {
                    iRowIndex = dataGridViewActive.RowCount - 1;
                    rowChanged = true;
                }

                DataGridViewCell FindCell = null;
                if (!(bSearchAndReplace && dataGridViewActive[iColIndex, iRowIndex].ReadOnly))
                {
                    if (bStopOnFind) bReplace = false;
                    if (FindAndReplaceString(bReplace, dataGridViewActive[iColIndex, iRowIndex], sFindWhat, replaceString, bMatchCase, bMatchCell, iSearchMethod))
                    {
                        FindCell = dataGridViewActive[iColIndex, iRowIndex];
                        if ((markCell && !bReplace) || (markCell && bReplace && !FindCell.ReadOnly)) FindCell.Selected = true;
                    }
                }

                if (bSearchRow && rowChanged)
                {
                    if (dataGridViewActive.Rows[iRowIndex].HeaderCell.Value != null && FindStringInRow(dataGridViewActive.Rows[iRowIndex].HeaderCell.Value.ToString(), sFindWhat, bMatchCase, bMatchCell, iSearchMethod))
                    {
                        dataGridViewActive.Rows[iRowIndex].Selected = true;
                    }
                    rowChanged = false;
                }

                if (bStopOnFind && FindCell != null) return FindCell;
            } while (!(iRowIndex == iSearchStartRow && iColIndex == iSearchStartColumn));

            return null;
        }
        #endregion

        #region FindStringInRow
        bool FindStringInRow(string SearchInString, String Find, bool bMatchCase, bool bMatchCell, int iSearchMethod)
        {
            
            // Regular string search
            if (iSearchMethod == 0)
            {
                // Match Cell
                if (bMatchCell)
                {
                    if (!bMatchCase)
                        if (SearchInString.ToLowerInvariant() == Find.ToLowerInvariant()) return true;                        
                    else
                        if (SearchInString == Find)return true;                                        
                }
                // No Match Cell
                else
                {
                    StringComparison strCompare = StringComparison.InvariantCulture;
                    if (!bMatchCase) strCompare = StringComparison.InvariantCultureIgnoreCase;                    
                    return SearchInString.IndexOf(Find, 0, strCompare) != -1;
                }
            }
            else
            {
                // Regular Expression
                String RegexPattern = Find;
                // Wildcards
                if (iSearchMethod == 2)
                {
                    // Convert wildcard to regex:
                    RegexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(Find).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                }

                System.Text.RegularExpressions.RegexOptions strCompare = System.Text.RegularExpressions.RegexOptions.None;
                if (!bMatchCase)
                {
                    strCompare = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                }

                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(RegexPattern, strCompare);
                if (regex.IsMatch(SearchInString)) return true;                
                return false;
            }
            return false;
        }
        #endregion

        #region FindAndReplaceString
        bool FindAndReplaceString(bool bReplace, DataGridViewCell SearchCell, String FindString, String ReplaceString, bool bMatchCase, bool bMatchCell, int iSearchMethod )
        {
            String SearchString = SearchCell.FormattedValue.ToString();
            // Regular string search
            if (iSearchMethod == 0)
            {
                // Match Cell
                if ( bMatchCell )
                {
                    if ( !bMatchCase )
                    {
                        if ( SearchString.ToLowerInvariant() == FindString.ToLowerInvariant() )
                        {
                            if (bReplace && !SearchCell.ReadOnly)
                            {
                                if (!updatedCells.ContainsKey(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex)))
                                    updatedCells.Add(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex), DataGridViewHandler.CopyCellDataGridViewGenericCell(SearchCell));
                                SearchCell.Value = Convert.ChangeType(ReplaceString, SearchCell.ValueType);
                            }
                            return true;
                        }
                    }
                    else
                    {
                        if ( SearchString == FindString )
                        {
                            if (bReplace && !SearchCell.ReadOnly)
                            {
                                if (!updatedCells.ContainsKey(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex)))
                                    updatedCells.Add(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex), DataGridViewHandler.CopyCellDataGridViewGenericCell(SearchCell));
                                SearchCell.Value = Convert.ChangeType(ReplaceString, SearchCell.ValueType);
                            }
                            return true;
                        }
                    }
                }
                // No Match Cell
                else
                {
                    bool bFound = false;
                    
                    StringComparison strCompare = StringComparison.InvariantCulture;
                    if (!bMatchCase)
                    {
                        strCompare = StringComparison.InvariantCultureIgnoreCase;
                    }

                    if (bReplace && !SearchCell.ReadOnly)
                    {
                        String NewString = null;
                        int strIndex = 0;
                        while (strIndex != -1)
                        {
                            int nextStrIndex = SearchString.IndexOf(FindString, strIndex, strCompare);
                            if (nextStrIndex != -1)
                            {
                                bFound = true;
                                NewString += SearchString.Substring(strIndex, nextStrIndex - strIndex);
                                NewString += ReplaceString;
                                nextStrIndex = nextStrIndex + FindString.Length;
                            }
                            else
                            {
                                NewString += SearchString.Substring(strIndex);
                            }
                            strIndex = nextStrIndex;
                        }
                        if ( bFound )
                        {
                            if (!updatedCells.ContainsKey(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex)))
                                updatedCells.Add(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex), DataGridViewHandler.CopyCellDataGridViewGenericCell(SearchCell));
                            SearchCell.Value = Convert.ChangeType(NewString, SearchCell.ValueType);
                        }
                    }
                    else
                    {
                        bFound = SearchString.IndexOf(FindString, 0, strCompare) != -1;
                    }
                    return bFound;  
                }                
            }            
            else
            {
                // Regular Expression
                String RegexPattern = FindString;
                // Wildcards
                if (iSearchMethod == 2)
                {
                    // Convert wildcard to regex:
                    RegexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(FindString).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                }
                System.Text.RegularExpressions.RegexOptions strCompare = System.Text.RegularExpressions.RegexOptions.None;
                if (!bMatchCase)
                {
                    strCompare = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                }
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(RegexPattern, strCompare);
                if ( regex.IsMatch( SearchString ) )
                {
                    if (bReplace && !SearchCell.ReadOnly)
                    {
                        if (!updatedCells.ContainsKey(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex)))
                            updatedCells.Add(new CellLocation(SearchCell.ColumnIndex, SearchCell.RowIndex), DataGridViewHandler.CopyCellDataGridViewGenericCell(SearchCell));
                        String NewString = regex.Replace(SearchString, ReplaceString );
                        SearchCell.Value = Convert.ChangeType(NewString, SearchCell.ValueType);
                    }
                    return true;
                }
                return false;
            }
            return false;
        }
        #endregion

        #region FindAndReplaceForm_FormClosing - Put changes to UndoStack
        private void FindAndReplaceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (updatedCells.Count > 0) ClipboardUtility.PushToUndoStack(dataGridViewActive, updatedCells);
            
            foreach (CellLocation cellLocation in updatedCells.Keys)
            {
                DataGridViewHandler.SetColumnDirtyFlag(dataGridViewActive, cellLocation.ColumnIndex, true); //IsDataGridViewColumnDirty(dataGridView, columnIndex, out string diffrences), diffrences);
            }
        }
        #endregion

        #region kryptonNavigatorFindAndReplace_TabIndexChanged
        private void kryptonNavigatorFindAndReplace_TabIndexChanged(object sender, EventArgs e)
        {
            switch (kryptonNavigatorFindAndReplace.SelectedIndex)
            {
                case 0:
                    this.AcceptButton = FindButton1;
                    break;
                case 1:
                    this.AcceptButton = FindButton2;
                    break;
            }
        }
        #endregion
    }
}