/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;

namespace Chummer
{
    public partial class SelectAIProgram : Form
    {
        private string _strSelectedAIProgram = string.Empty;
        private static string _strSelectedCategory = string.Empty;

        private bool _blnLoading = true;
        private bool _blnAddAgain;
        private readonly bool _blnAdvancedProgramAllowed;
        private readonly bool _blnInherentProgram;
        private readonly Character _objCharacter;
        private List<ListItem> _lstCategory;

        private readonly XPathNavigator _xmlBaseChummerNode;
        private readonly XPathNavigator _xmlOptionalAIProgramsNode;

        #region Control Events

        public SelectAIProgram(Character objCharacter, bool blnAdvancedProgramAllowed = true, bool blnInherentProgram = false)
        {
            _objCharacter = objCharacter ?? throw new ArgumentNullException(nameof(objCharacter));
            InitializeComponent();
            this.UpdateLightDarkMode();
            this.TranslateWinForm();
            this.UpdateParentForToolTipControls();
            _blnAdvancedProgramAllowed = blnAdvancedProgramAllowed;
            _blnInherentProgram = blnInherentProgram;
            // Load the Programs information.
            _xmlBaseChummerNode = _objCharacter.LoadDataXPath("programs.xml").SelectSingleNodeAndCacheExpression("/chummer");
            _xmlOptionalAIProgramsNode = _objCharacter.GetNodeXPath().SelectSingleNodeAndCacheExpression("optionalaiprograms");
            _lstCategory = Utils.ListItemListPool.Get();
        }

        private async void SelectAIProgram_Load(object sender, EventArgs e)
        {
            // Populate the Category list.
            foreach (XPathNavigator objXmlCategory in _xmlBaseChummerNode.SelectAndCacheExpression("categories/category"))
            {
                string strInnerText = objXmlCategory.Value;
                if (string.IsNullOrEmpty(strInnerText))
                    continue;
                if (_blnInherentProgram && strInnerText != "Common Programs" && strInnerText != "Hacking Programs")
                    continue;
                if (!_blnAdvancedProgramAllowed && strInnerText == "Advanced Programs")
                    continue;
                // Make sure it is not already in the Category list.
                if (_lstCategory.Count == 0 || _lstCategory.TrueForAll(objItem => objItem.Value?.ToString() != strInnerText))
                {
                    _lstCategory.Add(new ListItem(strInnerText, objXmlCategory.SelectSingleNodeAndCacheExpression("@translate")?.Value ?? strInnerText));
                }
            }
            _lstCategory.Sort(CompareListItems.CompareNames);

            if (_lstCategory.Count > 0)
            {
                _lstCategory.Insert(0, new ListItem("Show All", await LanguageManager.GetStringAsync("String_ShowAll").ConfigureAwait(false)));
            }

            await cboCategory.PopulateWithListItemsAsync(_lstCategory).ConfigureAwait(false);
            await cboCategory.DoThreadSafeAsync(x =>
            {
                if (!string.IsNullOrEmpty(_strSelectedCategory))
                    x.SelectedValue = _strSelectedCategory;
                if (x.SelectedIndex == -1)
                    x.SelectedIndex = 0;
            }).ConfigureAwait(false);

            _blnLoading = false;

            await RefreshList().ConfigureAwait(false);
        }

        private async void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            await RefreshList().ConfigureAwait(false);
        }

        private async void chkLimitList_CheckedChanged(object sender, EventArgs e)
        {
            await RefreshList().ConfigureAwait(false);
        }

        private async void lstAIPrograms_SelectedIndexChanged(object sender, EventArgs e)
        {
            await UpdateProgramInfo().ConfigureAwait(false);
        }

        private async void cmdOK_Click(object sender, EventArgs e)
        {
            _blnAddAgain = false;
            await AcceptForm().ConfigureAwait(false);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private async void cmdOKAdd_Click(object sender, EventArgs e)
        {
            _blnAddAgain = true;
            await AcceptForm().ConfigureAwait(false);
        }

        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            await RefreshList().ConfigureAwait(false);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down when lstAIPrograms.SelectedIndex + 1 < lstAIPrograms.Items.Count:
                    ++lstAIPrograms.SelectedIndex;
                    break;

                case Keys.Down:
                    {
                        if (lstAIPrograms.Items.Count > 0)
                        {
                            lstAIPrograms.SelectedIndex = 0;
                        }

                        break;
                    }
                case Keys.Up when lstAIPrograms.SelectedIndex >= 1:
                    --lstAIPrograms.SelectedIndex;
                    break;

                case Keys.Up:
                    {
                        if (lstAIPrograms.Items.Count > 0)
                        {
                            lstAIPrograms.SelectedIndex = lstAIPrograms.Items.Count - 1;
                        }

                        break;
                    }
            }
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
                txtSearch.Select(txtSearch.TextLength, 0);
        }

        #endregion Control Events

        #region Properties

        /// <summary>
        /// Whether the user wants to add another item after this one.
        /// </summary>
        public bool AddAgain => _blnAddAgain;

        /// <summary>
        /// Program that was selected in the dialogue.
        /// </summary>
        public string SelectedProgram
        {
            get => _strSelectedAIProgram;
            set => _strSelectedAIProgram = value;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Update the Program's information based on the Program selected.
        /// </summary>
        private async Task UpdateProgramInfo(CancellationToken token = default)
        {
            if (_blnLoading)
                return;

            await this.DoThreadSafeAsync(x => x.SuspendLayout(), token: token).ConfigureAwait(false);
            try
            {
                await tlpRight.DoThreadSafeFuncAsync(x => x.Visible = false, token: token).ConfigureAwait(false);
                string strSelectedId = await lstAIPrograms.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(strSelectedId))
                {
                    // Retrieve the information for the selected piece of Cyberware.
                    XPathNavigator objXmlProgram
                        = _xmlBaseChummerNode.TryGetNodeByNameOrId("programs/program", strSelectedId);
                    if (objXmlProgram != null)
                    {
                        string strRequiresProgram = objXmlProgram.SelectSingleNodeAndCacheExpression("require", token)?.Value;
                        if (string.IsNullOrEmpty(strRequiresProgram))
                        {
                            strRequiresProgram = await LanguageManager.GetStringAsync("String_None", token: token).ConfigureAwait(false);
                        }
                        else
                        {
                            XPathNavigator objRequiresNode
                                = _xmlBaseChummerNode.TryGetNodeByNameOrId("programs/program", strRequiresProgram);
                            strRequiresProgram
                                = objRequiresNode != null
                                    ? objRequiresNode.SelectSingleNodeAndCacheExpression("translate", token)?.Value ?? strRequiresProgram
                                    : strRequiresProgram;
                        }

                        await lblRequiresProgram.DoThreadSafeAsync(x => x.Text = strRequiresProgram, token: token).ConfigureAwait(false);

                        string strSource = objXmlProgram.SelectSingleNodeAndCacheExpression("source", token)?.Value;
                        if (!string.IsNullOrEmpty(strSource))
                        {
                            string strPage
                                = objXmlProgram.SelectSingleNodeAndCacheExpression("altpage", token: token)?.Value
                                  ?? objXmlProgram.SelectSingleNodeAndCacheExpression("page", token)?.Value;
                            if (!string.IsNullOrEmpty(strPage))
                            {
                                SourceString objSource = await SourceString.GetSourceStringAsync(
                                    strSource, strPage, GlobalSettings.Language, GlobalSettings.CultureInfo,
                                    _objCharacter, token).ConfigureAwait(false);
                                await objSource.SetControlAsync(lblSource, this, token).ConfigureAwait(false);
                            }
                            else
                            {
                                string strUnknown = await LanguageManager.GetStringAsync("String_Unknown", token: token).ConfigureAwait(false);
                                await lblSource.DoThreadSafeAsync(x => x.Text = strUnknown, token: token).ConfigureAwait(false);
                                await lblSource.SetToolTipTextAsync(strUnknown, token: token).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            string strUnknown = await LanguageManager.GetStringAsync("String_Unknown", token: token).ConfigureAwait(false);
                            await lblSource.DoThreadSafeAsync(x => x.Text = strUnknown, token: token).ConfigureAwait(false);
                            await lblSource.SetToolTipTextAsync(strUnknown, token: token).ConfigureAwait(false);
                        }

                        await tlpRight.DoThreadSafeAsync(x => x.Visible = true, token: token).ConfigureAwait(false);
                    }
                }

                await lblRequiresProgramLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(lblRequiresProgram.Text), token: token).ConfigureAwait(false);
                await lblSourceLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(lblSource.Text), token: token).ConfigureAwait(false);
            }
            finally
            {
                await this.DoThreadSafeAsync(x => x.ResumeLayout(), CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Refreshes the displayed lists
        /// </summary>
        private async Task RefreshList(CancellationToken token = default)
        {
            if (_blnLoading)
                return;

            using (new FetchSafelyFromObjectPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdFilter))
            {
                sbdFilter.Append("programs/program[", await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookXPathAsync(token: token).ConfigureAwait(false));
                string strCategory = await cboCategory.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(strCategory) && strCategory != "Show All"
                                                       && (GlobalSettings.SearchInCategoryOnly
                                                           || await txtSearch.DoThreadSafeFuncAsync(x => x.TextLength == 0, token: token).ConfigureAwait(false)))
                    sbdFilter.Append(" and category = ", strCategory.CleanXPath());
                else
                {
                    using (new FetchSafelyFromObjectPool<StringBuilder>(Utils.StringBuilderPool,
                                                                  out StringBuilder sbdCategoryFilter))
                    {
                        foreach (string strItem in _lstCategory.Select(x => x.Value.ToString()))
                        {
                            if (!string.IsNullOrEmpty(strItem))
                                sbdCategoryFilter.Append("category = ", strItem.CleanXPath(), " or ");
                        }

                        if (sbdCategoryFilter.Length > 0)
                        {
                            sbdCategoryFilter.Length -= 4;
                            sbdFilter.Append(" and (", sbdCategoryFilter.ToString(), ')');
                        }
                    }
                }

                if (!string.IsNullOrEmpty(txtSearch.Text))
                    sbdFilter.Append(" and ", CommonFunctions.GenerateSearchXPath(await txtSearch.DoThreadSafeFuncAsync(x => x.Text, token: token).ConfigureAwait(false)));

                // Populate the Program list.
                await UpdateProgramList(_xmlBaseChummerNode.Select(sbdFilter.Append(']').ToString()), token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update the Program List based on a base program node list.
        /// </summary>
        private async Task UpdateProgramList(XPathNodeIterator objXmlNodeList, CancellationToken token = default)
        {
            string strSpace = await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false);
            bool blnLimitList = await chkLimitList.DoThreadSafeFuncAsync(x => x.Checked, token: token).ConfigureAwait(false);
            bool blnHasSearch = await txtSearch.DoThreadSafeFuncAsync(x => x.TextLength != 0, token: token).ConfigureAwait(false);
            using (new FetchSafelyFromSafeObjectPool<List<ListItem>>(Utils.ListItemListPool,
                                                           out List<ListItem> lstPrograms))
            {
                foreach (XPathNavigator objXmlProgram in objXmlNodeList)
                {
                    string strId = objXmlProgram.SelectSingleNodeAndCacheExpression("id", token)?.Value;
                    if (string.IsNullOrEmpty(strId))
                        continue;

                    if (blnLimitList)
                    {
                        string strRequire = objXmlProgram.SelectSingleNodeAndCacheExpression("require", token)?.Value;
                        if (!string.IsNullOrEmpty(strRequire) && await _objCharacter.AIPrograms
                                .FirstOrDefaultAsync(x => x.Name == strRequire, token).ConfigureAwait(false) == null)
                            continue;
                    }

                    string strName = objXmlProgram.SelectSingleNodeAndCacheExpression("name", token)?.Value
                                     ?? await LanguageManager.GetStringAsync("String_Unknown", token: token).ConfigureAwait(false);
                    // If this is a critter with Optional Programs, see if this Program is allowed.
                    if (_xmlOptionalAIProgramsNode?.SelectSingleNodeAndCacheExpression("program", token) != null
                        && _xmlOptionalAIProgramsNode.SelectSingleNode("program[. = " + strName.CleanXPath() + "]") == null)
                        continue;
                    string strDisplayName = objXmlProgram.SelectSingleNodeAndCacheExpression("translate", token: token)?.Value ?? strName;
                    if (!GlobalSettings.SearchInCategoryOnly && blnHasSearch)
                    {
                        string strCategory = objXmlProgram.SelectSingleNodeAndCacheExpression("category", token)?.Value;
                        if (!string.IsNullOrEmpty(strCategory))
                        {
                            ListItem objFoundItem
                                = _lstCategory.Find(objFind => objFind.Value.ToString() == strCategory);
                            if (!string.IsNullOrEmpty(objFoundItem.Name))
                            {
                                strDisplayName += strSpace + "[" + objFoundItem.Name + "]";
                            }
                        }
                    }

                    lstPrograms.Add(new ListItem(strId, strDisplayName));
                }

                lstPrograms.Sort(CompareListItems.CompareNames);
                await lstAIPrograms.PopulateWithListItemsAsync(lstPrograms, token: token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Accept the selected item and close the form.
        /// </summary>
        private async Task AcceptForm(CancellationToken token = default)
        {
            string strSelectedId = await lstAIPrograms.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(strSelectedId))
            {
                XPathNavigator xmlProgram = _xmlBaseChummerNode.TryGetNodeByNameOrId("programs/program", strSelectedId);
                if (xmlProgram == null)
                    return;

                // Check to make sure requirement is met
                if (!await xmlProgram.RequirementsMetAsync(_objCharacter, strLocalName: await LanguageManager.GetStringAsync("String_Program", token: token).ConfigureAwait(false), token: token).ConfigureAwait(false))
                {
                    return;
                }

                _strSelectedAIProgram = strSelectedId;
                _strSelectedCategory = GlobalSettings.SearchInCategoryOnly || await txtSearch.DoThreadSafeFuncAsync(x => x.TextLength == 0, token: token).ConfigureAwait(false)
                    ? await cboCategory.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false)
                    : xmlProgram.SelectSingleNodeAndCacheExpression("category", token)?.Value;

                await this.DoThreadSafeAsync(x =>
                {
                    x.DialogResult = DialogResult.OK;
                    x.Close();
                }, token: token).ConfigureAwait(false);
            }
        }

        private async void OpenSourceFromLabel(object sender, EventArgs e)
        {
            await CommonFunctions.OpenPdfFromControl(sender).ConfigureAwait(false);
        }

        #endregion Methods
    }
}
