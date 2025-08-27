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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Chummer.Backend.Equipment;
using Chummer.Properties;
using Chummer.UI.Table;

// ReSharper disable StringCompareToIsCultureSpecific

namespace Chummer.UI.Powers
{
    public partial class PowersTabUserControl : UserControl, IHasCharacterObject
    {
        private TableView<Power> _table;

        public Character CharacterObject => _objCharacter;

        private CancellationTokenSource _objFilterCancellationTokenSource;

        public PowersTabUserControl() : this(default)
        {
            // Need to set up constructors like this so that the WinForms designer doesn't freak out
        }

        public PowersTabUserControl(CancellationToken objMyToken)
        {
            _objMyToken = objMyToken;
            InitializeComponent();

            Disposed += (sender, args) =>
            {
                CancellationTokenSource objOldSource = Interlocked.Exchange(ref _objFilterCancellationTokenSource, null);
                if (objOldSource != null)
                {
                    objOldSource.Cancel(false);
                    objOldSource.Dispose();
                }
                UnbindPowersTabUserControl();
            };

            this.UpdateLightDarkMode(token: objMyToken);
            this.TranslateWinForm(token: objMyToken);

            _dropDownList = GenerateDropdownFilter(objMyToken);

            SuspendLayout();
            try
            {
                InitializeTable();
            }
            finally
            {
                ResumeLayout();
            }
        }

        private Character _objCharacter;
        private List<Tuple<string, Func<Power, CancellationToken, Task<bool>>>> _dropDownList;
        private bool _blnSearchMode;

        private CancellationToken _objMyToken;

        public CancellationToken MyToken
        {
            get => _objMyToken;
            set
            {
                if (_objMyToken == value)
                    return;
                _objMyToken = value;
                _dropDownList = GenerateDropdownFilter(value);
            }
        }

        public Character CachedCharacter { get; set; }

        private async void PowersTabUserControl_Load(object sender, EventArgs e)
        {
            if (_objCharacter != null)
                return;
            CursorWait objCursorWait = await CursorWait.NewAsync(this, token: MyToken).ConfigureAwait(false);
            try
            {
                await RealLoad(MyToken, MyToken).ConfigureAwait(false);
            }
            finally
            {
                await objCursorWait.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task RealLoad(CancellationToken objMyToken = default, CancellationToken token = default)
        {
            if (CachedCharacter != null)
            {
                if (Interlocked.CompareExchange(ref _objCharacter, CachedCharacter, null) != null)
                    return;
            }
            else if (ParentForm is CharacterShared frmParent && frmParent.CharacterObject != null)
            {
                if (Interlocked.CompareExchange(ref _objCharacter, frmParent.CharacterObject, null) != null)
                    return;
            }
            else
            {
                Character objCharacter = new Character();
                if (Interlocked.CompareExchange(ref _objCharacter, objCharacter, null) != null)
                {
                    await objCharacter.DisposeAsync().ConfigureAwait(false);
                    return;
                }

                await this.DoThreadSafeAsync(x => x.Disposed += (sender, args) => objCharacter.Dispose(), token)
                    .ConfigureAwait(false);
                Utils.BreakIfDebug();
            }

            MyToken = objMyToken;

            if (Utils.IsDesignerMode || Utils.IsRunningInVisualStudio)
                return;

            IAsyncDisposable objLocker = await _objCharacter.LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThreadSafeBindingList<Power>
                    lstPowers = await _objCharacter.GetPowersAsync(token).ConfigureAwait(false);
#if DEBUG
                Stopwatch sw = Utils.StopwatchPool.Get();
                try
                {
                    sw.Start();
#endif
                    //Keep everything visible until ready to display everything. This
                    //seems to prevent redrawing everything each time anything is added
                    //Not benched, but should be faster

                    //Might also be useless horseshit, 2 lines

                    //Visible = false;
                    await this.DoThreadSafeAsync(x => x.SuspendLayout(), token: token).ConfigureAwait(false);
                    try
                    {
                        using (new FetchSafelyFromSafeObjectPool<Stopwatch>(Utils.StopwatchPool, out Stopwatch parts))
                        {
                            parts.Start();
                            parts.TaskEnd("MakePowerDisplay()");

                            await cboDisplayFilter.DoThreadSafeAsync(x =>
                            {
                                x.BeginUpdate();
                                try
                                {
                                    x.DataSource = null;
                                    x.ValueMember = "Item2";
                                    x.DisplayMember = "Item1";
                                    x.DataSource = _dropDownList;
                                    x.SelectedIndex = 1;
                                    x.MaxDropDownItems = _dropDownList.Count;
                                }
                                finally
                                {
                                    x.EndUpdate();
                                }
                            }, token: token).ConfigureAwait(false);

                            parts.TaskEnd("_ddl databind");

                            //Visible = true;
                            //this.ResumeLayout(false);
                            //this.PerformLayout();
                            parts.TaskEnd("visible");

                            await _table.SetItemsAsync(lstPowers, token).ConfigureAwait(false);

                            parts.TaskEnd("resize");
                        }
                    }
                    finally
                    {
                        await this.DoThreadSafeAsync(x => x.ResumeLayout(true), token: token).ConfigureAwait(false);
                    }

                    await lblPowerPoints.RegisterOneWayAsyncDataBindingAsync((x, y) => x.Text = y, _objCharacter,
                        nameof(Character.DisplayPowerPointsRemaining),
                        x => x.GetDisplayPowerPointsRemainingAsync(MyToken),
                        token).ConfigureAwait(false);
#if DEBUG
                }
                finally
                {
                    sw.Stop();
                    Debug.WriteLine("RealLoad() in {0} ms", sw.Elapsed.TotalMilliseconds);
                    Utils.StopwatchPool.Return(ref sw);
                }
#endif

                lstPowers.ListChangedAsync += OnPowersListChanged;
                _objCharacter.MultiplePropertiesChangedAsync += OnCharacterPropertyChanged;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void UnbindPowersTabUserControl()
        {
            Character objCharacter = _objCharacter; // for thread safety
            if (objCharacter?.IsDisposed == false)
            {
                objCharacter.MultiplePropertiesChangedAsync -= OnCharacterPropertyChanged;
                ThreadSafeBindingList<Power> lstPowers = objCharacter.Powers;
                if (lstPowers?.IsDisposed == false)
                    lstPowers.ListChangedAsync -= OnPowersListChanged;
            }
        }

        private async Task OnCharacterPropertyChanged(object sender, MultiplePropertiesChangedEventArgs e,
            CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (e.PropertyNames.Contains(nameof(Character.PowerPointsTotal))
                    || e.PropertyNames.Contains(nameof(Character.PowerPointsUsed)))
                    await CalculatePowerPoints(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //swallow this
            }
        }

        private async Task OnPowersListChanged(object sender, ListChangedEventArgs e, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: token).ConfigureAwait(false);
                try
                {
                    switch (e.ListChangedType)
                    {
                        case ListChangedType.ItemChanged:
                        {
                            string propertyName = e.PropertyDescriptor?.Name;
                            if (propertyName == nameof(Power.FreeLevels) || propertyName == nameof(Power.TotalRating))
                            {
                                // recalculation of power points on rating/free levels change
                                await CalculatePowerPoints(token).ConfigureAwait(false);
                            }

                            break;
                        }
                        case ListChangedType.Reset:
                        case ListChangedType.ItemAdded:
                        case ListChangedType.ItemDeleted:
                            await CalculatePowerPoints(token).ConfigureAwait(false);
                            break;
                    }
                }
                finally
                {
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                //swallow this
            }
        }

        private static List<Tuple<string, Func<Power, CancellationToken, Task<bool>>>> GenerateDropdownFilter(
            CancellationToken objMyToken = default)
        {
            List<Tuple<string, Func<Power, CancellationToken, Task<bool>>>> ret = new List<Tuple<string, Func<Power, CancellationToken, Task<bool>>>>(4)
            {
                new Tuple<string, Func<Power, CancellationToken, Task<bool>>>(
                    LanguageManager.GetString("String_Search", token: objMyToken),
                    null),
                new Tuple<string, Func<Power, CancellationToken, Task<bool>>>(
                    LanguageManager.GetString("String_PowerFilterAll", token: objMyToken),
                    (x, t) => Task.FromResult(true)),
                new Tuple<string, Func<Power, CancellationToken, Task<bool>>>(
                    LanguageManager.GetString("String_PowerFilterRatingAboveZero", token: objMyToken),
                    async (power, t) => await power.GetRatingAsync(t).ConfigureAwait(false) > 0),
                new Tuple<string, Func<Power, CancellationToken, Task<bool>>>(
                    LanguageManager.GetString("String_PowerFilterRatingZero", token: objMyToken),
                    async (power, t) => await power.GetRatingAsync(t).ConfigureAwait(false) == 0)
            };

            /*
            using (XmlNodeList xmlPowerCategoryList = XmlManager.Load("powers.xml", objCharacter.Settings.CustomDataDictionary).SelectNodes("/chummer/categories/category"))
                if (xmlPowerCategoryList != null)
                    foreach (XmlNode xmlCategoryNode in xmlPowerCategoryList)
                    {
                        string strName = xmlCategoryNode.InnerText;
                        ret.Add(new Tuple<string, Predicate<Power>>(
                            LanguageManager.GetString("Label_Category") + LanguageManager.GetString("String_Space") + (xmlCategoryNode.Attributes?["translate"]?.InnerText ?? strName),
                            power => power.Category == strName));
                    }
                    */

            return ret;
        }

        private async void cboDisplayFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            CancellationTokenSource objNewSource = new CancellationTokenSource();
            CancellationTokenSource objOldSource = Interlocked.Exchange(ref _objFilterCancellationTokenSource, objNewSource);
            if (objOldSource != null)
            {
                objOldSource.Cancel(false);
                objOldSource.Dispose();
            }
            try
            {
                using (CancellationTokenSource objJoinedSource = CancellationTokenSource.CreateLinkedTokenSource(MyToken, objNewSource.Token))
                {
                    CancellationToken objJoinedToken = objJoinedSource.Token;
                    CursorWait objCursorWait = await CursorWait.NewAsync(this, token: objJoinedToken).ConfigureAwait(false);
                    try
                    {
                        if (!(cboDisplayFilter.SelectedItem is Tuple<string, Func<Power, CancellationToken, Task<bool>>> selectedItem))
                            return;
                        if (selectedItem.Item2 == null)
                        {
                            cboDisplayFilter.DropDownStyle = ComboBoxStyle.DropDown;
                            _blnSearchMode = true;
                            cboDisplayFilter.Text = string.Empty;
                        }
                        else
                        {
                            cboDisplayFilter.DropDownStyle = ComboBoxStyle.DropDownList;
                            _blnSearchMode = false;
                            await _table.SetFilterAsync(selectedItem.Item2, objJoinedToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await objCursorWait.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //swallow this
            }
        }

        private async void cboDisplayFilter_TextUpdate(object sender, EventArgs e)
        {
            if (_blnSearchMode)
            {
                CancellationTokenSource objNewSource = new CancellationTokenSource();
                CancellationTokenSource objOldSource = Interlocked.Exchange(ref _objFilterCancellationTokenSource, objNewSource);
                if (objOldSource != null)
                {
                    objOldSource.Cancel(false);
                    objOldSource.Dispose();
                }
                try
                {
                    using (CancellationTokenSource objJoinedSource = CancellationTokenSource.CreateLinkedTokenSource(MyToken, objNewSource.Token))
                    {
                        CancellationToken objJoinedToken = objJoinedSource.Token;
                        CursorWait objCursorWait = await CursorWait.NewAsync(this, token: objJoinedToken).ConfigureAwait(false);
                        try
                        {
                            await _table.SetFilterAsync(
                                async (power, t) =>
                                {
                                    try
                                    {
                                        return GlobalSettings.InvariantCultureInfo.CompareInfo.IndexOf(
                                            await power.GetCurrentDisplayNameAsync(token: t).ConfigureAwait(false),
                                            await cboDisplayFilter.DoThreadSafeFuncAsync(x => x.Text, token: t)
                                                .ConfigureAwait(false),
                                            CompareOptions.IgnoreCase) >= 0;
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        return true;
                                    }
                                }, objJoinedToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            await objCursorWait.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    //swallow this
                }
            }
        }

        private async void cmdAddPower_Click(object sender, EventArgs e)
        {
            try
            {
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: MyToken).ConfigureAwait(false);
                try
                {
                    // Open the Cyberware XML file and locate the selected piece.
                    XmlDocument objXmlDocument = await _objCharacter.LoadDataAsync("powers.xml", token: MyToken)
                        .ConfigureAwait(false);
                    bool blnAddAgain;

                    do
                    {
                        using (ThreadSafeForm<SelectPower> frmPickPower = await ThreadSafeForm<SelectPower>
                                   .GetAsync(
                                       () => new SelectPower(_objCharacter),
                                       MyToken).ConfigureAwait(false))
                        {
                            // Make sure the dialogue window was not canceled.
                            if (await frmPickPower.ShowDialogSafeAsync(_objCharacter, MyToken).ConfigureAwait(false)
                                == DialogResult.Cancel)
                                break;

                            blnAddAgain = frmPickPower.MyForm.AddAgain;

                            XmlNode objXmlPower = objXmlDocument.TryGetNodeByNameOrId("/chummer/powers/power",
                                                          frmPickPower.MyForm.SelectedPower)
                                                      ?? throw new AbortedException();

                            Power objPower = new Power(_objCharacter);
                            try
                            {
                                if (await objPower.CreateAsync(objXmlPower, token: MyToken).ConfigureAwait(false))
                                    await _objCharacter.Powers.AddAsync(objPower, MyToken).ConfigureAwait(false);
                                else
                                    await objPower.DeletePowerAsync(MyToken).ConfigureAwait(false);
                            }
                            catch
                            {
                                await objPower.DeletePowerAsync(CancellationToken.None).ConfigureAwait(false);
                                throw;
                            }
                        }
                    } while (blnAddAgain);
                }
                finally
                {
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                //swallow this
            }
        }

        /// <summary>
        /// Calculate the number of Adept Power Points used.
        /// </summary>
        public async Task CalculatePowerPoints(CancellationToken token = default)
        {
            CursorWait objCursorWait = await CursorWait.NewAsync(this, token: token).ConfigureAwait(false);
            try
            {
                decimal decPowerPointsTotal = await _objCharacter.GetPowerPointsTotalAsync(token).ConfigureAwait(false);
                decimal decPowerPointsRemaining = decPowerPointsTotal -
                                                  await _objCharacter.GetPowerPointsUsedAsync(token)
                                                      .ConfigureAwait(false);
                string strText = string.Format(GlobalSettings.CultureInfo, "{1}{0}({2}{0}{3})",
                    await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false),
                    decPowerPointsTotal,
                    decPowerPointsRemaining,
                    await LanguageManager.GetStringAsync("String_Remaining", token: token).ConfigureAwait(false));
                await lblPowerPoints.DoThreadSafeAsync(x => x.Text = strText, token: token).ConfigureAwait(false);
            }
            finally
            {
                await objCursorWait.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void InitializeTable()
        {
            _table = this.DoThreadSafeFunc(() => new TableView<Power>
            {
                Dock = DockStyle.Top
            });
            Disposed += (sender, args) => _table.Dispose();
            // create columns
            TableColumn<Power> nameColumn = this.DoThreadSafeFunc(
                () =>
                {
                    return new TableColumn<Power>(() => new TextTableCell())
                    {
                        Text = "Power",
                        Extractor = SpecifyName,
                        Tag = "String_Power",
                        Sorter = Sorter
                    };

                    async Task<int> Sorter(Task<object> name1, Task<object> name2, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return string.Compare((await name1.ConfigureAwait(false)).ToString(),
                                    (await name2.ConfigureAwait(false)).ToString(), GlobalSettings.CultureInfo,
                                    CompareOptions.Ordinal);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }

                    async Task<object> SpecifyName(Power power, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await power.GetCurrentDisplayNameAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }
                });
            nameColumn.AddDependency(nameof(Power.CurrentDisplayName));

            TableColumn<Power> actionColumn = this.DoThreadSafeFunc(
                () =>
                {
                    return new TableColumn<Power>(() => new TextTableCell())
                    {
                        Text = "Action",
                        Extractor = Extractor,
                        Tag = "ColumnHeader_Action",
                        Sorter = Sorter
                    };

                    async Task<int> Sorter(Task<object> action1, Task<object> action2, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return string.Compare((await action1.ConfigureAwait(false)).ToString(),
                                    (await action2.ConfigureAwait(false)).ToString(), GlobalSettings.CultureInfo,
                                    CompareOptions.Ordinal);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }

                    async Task<object> Extractor(Power power, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await power.GetDisplayActionAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }
                });
            actionColumn.AddDependency(nameof(Power.DisplayAction));

            TableColumn<Power> ratingColumn = this.DoThreadSafeFunc(() =>
            {
                return new TableColumn<Power>(
                    () =>
                    {
                        return new SpinnerTableCell<Power>(_table, MyToken)
                        {
                            EnabledExtractor = (p, t) =>
                                p.GetLevelsEnabledAsync(t),
                            MaxExtractor = MaxExtractor,
                            ValueUpdater = ValueUpdater,
                            MinExtractor = MinExtractor,
                            ValueGetter = ValueGetter
                        };

                        async Task ValueUpdater(Power p, decimal newRating, CancellationToken t)
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                await p.SetRatingAsync(newRating.StandardRound(), t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync()
                                    .ConfigureAwait(false);
                            }
                        }

                        Task<decimal> MinExtractor(Power p, CancellationToken t) =>
                            t.IsCancellationRequested
                                ? Task.FromCanceled<decimal>(t)
                                : Task.FromResult<decimal>(0);

                        async Task<decimal> ValueGetter(Power p, CancellationToken t) =>
                            await p.GetRatingAsync(t).ConfigureAwait(false);

                        async Task<decimal> MaxExtractor(Power p, CancellationToken t) =>
                            Math.Max(await p.GetTotalMaximumLevelsAsync(t).ConfigureAwait(false) - await p
                                .GetFreeLevelsAsync(t)
                                .ConfigureAwait(false), 0);
                    })
                {
                    Text = "Rating",
                    Tag = "String_Rating",
                    Sorter = Sorter
                };

                async Task<int> Sorter(Task<object> o1, Task<object> o2, CancellationToken t)
                {
                    try
                    {
                        CursorWait objCursorWait =
                            await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                        try
                        {
                            if (await o1.ConfigureAwait(false) is Power objPower1 &&
                                await o2.ConfigureAwait(false) is Power objPower2)
                                return await objPower1.GetRatingAsync(t).ConfigureAwait(false) -
                                       await objPower2.GetRatingAsync(t).ConfigureAwait(false);
                        }
                        finally
                        {
                            await objCursorWait.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return 0;
                    }

                    string strMessage = "Can't sort an Object of Type " + o1.GetType() +
                                        " against another one of Type " + o2.GetType() + " in the ratingColumn." +
                                        Environment.NewLine + "Both objects SHOULD be of the type \"Power\".";
                    throw new ArgumentException(strMessage, nameof(o1));
                }
            });

            ratingColumn.AddDependency(nameof(Power.LevelsEnabled));
            ratingColumn.AddDependency(nameof(Power.FreeLevels));
            ratingColumn.AddDependency(nameof(Power.TotalMaximumLevels));
            ratingColumn.AddDependency(nameof(Power.TotalRating));
            TableColumn<Power> totalRatingColumn = this.DoThreadSafeFunc(
                () =>
                {
                    return new TableColumn<Power>(() => new TextTableCell())
                    {
                        Text = "Total Rating",
                        Extractor = Extractor,
                        Tag = "String_TotalRating",
                        ToolTipExtractor = ToolTipExtractor,
                        Sorter = Sorter
                    };

                    async Task<object> Extractor(Power power, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await power.GetTotalRatingAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }

                    async Task<string> ToolTipExtractor(Power item, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await item.GetTotalRatingToolTipAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return string.Empty;
                        }
                    }

                    async Task<int> Sorter(Task<object> o1, Task<object> o2, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                if (await o1.ConfigureAwait(false) is Power objPower1 &&
                                    await o2.ConfigureAwait(false) is Power objPower2)
                                    return await objPower1.GetTotalRatingAsync(t).ConfigureAwait(false) -
                                           await objPower2.GetTotalRatingAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return 0;
                        }

                        string strMessage = "Can't sort an Object of Type " + o1.GetType() +
                                            " against another one of Type " + o2.GetType() +
                                            " in the totalRatingColumn." + Environment.NewLine +
                                            "Both objects SHOULD be of the type \"Power\".";
                        throw new ArgumentException(strMessage, nameof(o1));
                    }
                });
            totalRatingColumn.AddDependency(nameof(Power.TotalRating));
            totalRatingColumn.AddDependency(nameof(Power.TotalRatingToolTip));

            TableColumn<Power> powerPointsColumn = this.DoThreadSafeFunc(
                () =>
                {
                    return new TableColumn<Power>(() => new TextTableCell())
                    {
                        Text = "Power Points",
                        Extractor = Extractor,
                        Tag = "ColumnHeader_Power_Points",
                        ToolTipExtractor = ToolTipExtractor
                    };

                    async Task<object> Extractor(Power power, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await power.GetDisplayPointsAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return default;
                        }
                    }

                    async Task<string> ToolTipExtractor(Power item, CancellationToken t)
                    {
                        try
                        {
                            CursorWait objCursorWait =
                                await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                            try
                            {
                                return await item.GetDisplayPointsToolTipAsync(t).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objCursorWait.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return string.Empty;
                        }
                    }
                });
            powerPointsColumn.AddDependency(nameof(Power.DisplayPoints));
            powerPointsColumn.AddDependency(nameof(Power.DisplayPointsToolTip));

            TableColumn<Power> sourceColumn = this.DoThreadSafeFunc(() =>
            {
                return new TableColumn<Power>(() => new TextTableCell
                {
                    Cursor = Cursors.Hand
                })
                {
                    Text = "Source",
                    Extractor = Extractor,
                    Tag = "Label_Source",
                    ToolTipExtractor = ToolTipExtractor
                };

                async Task<object> Extractor(Power power, CancellationToken t)
                {
                    try
                    {
                        CursorWait objCursorWait =
                            await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                        try
                        {
                            return await power.GetSourceDetailAsync(t).ConfigureAwait(false);
                        }
                        finally
                        {
                            await objCursorWait.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return default;
                    }
                }

                async Task<string> ToolTipExtractor(Power item, CancellationToken t)
                {
                    try
                    {
                        CursorWait objCursorWait =
                            await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                        try
                        {
                            return await (await item.GetSourceDetailAsync(t).ConfigureAwait(false)).GetLanguageBookTooltipAsync(t).ConfigureAwait(false);
                        }
                        finally
                        {
                            await objCursorWait.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                }
            });
            powerPointsColumn.AddDependency(nameof(Power.Source));

            TableColumn<Power> adeptWayColumn = this.DoThreadSafeFunc(() => new TableColumn<Power>(
                () =>
                {
                    return new CheckBoxTableCell<Power>(objMyToken: MyToken)
                    {
                        ValueGetter = (p, t) =>
                            p.GetDiscountedAdeptWayAsync(t),
                        ValueUpdater = (p, check, t) =>
                            p.SetDiscountedAdeptWayAsync(check,
                                t),
                        VisibleExtractor = (p, t) =>
                            p.GetAdeptWayDiscountEnabledAsync(
                                t),
                        EnabledExtractor = EnabledExtractor,
                        Alignment = Alignment.Center
                    };

                    async Task<bool> EnabledExtractor(Power p, CancellationToken t) =>
                        await p.CharacterObject.GetAllowAdeptWayPowerDiscountAsync(t).ConfigureAwait(false) || await p
                            .GetDiscountedAdeptWayAsync(t)
                            .ConfigureAwait(false);
                })
            {
                Text = "Adept Way",
                Tag = "Checkbox_Power_AdeptWay"
            });
            adeptWayColumn.AddDependency(nameof(Power.DiscountedAdeptWay));
            adeptWayColumn.AddDependency(nameof(Power.AdeptWayDiscountEnabled));
            adeptWayColumn.AddDependency(nameof(Character.AllowAdeptWayPowerDiscount));
            adeptWayColumn.AddDependency(nameof(Power.Rating));

            /*
             TableColumn<Power> geasColumn = new TableColumn<Power>(() => new CheckBoxTableCell<Power>()
            {
                ValueGetter = (p => p.DiscountedGeas),
                ValueUpdater = (p, check) => p.DiscountedGeas = check,
                Alignment = Alignment.Center
            })
            {
                Text = "Geas",
                Tag = "Checkbox_Power_Geas"
            };
            geasColumn.AddDependency(nameof(Power.DiscountedGeas));
            */

            TableColumn<Power> noteColumn = this.DoThreadSafeFunc(() =>
            {
                return new TableColumn<Power>(
                    () =>
                    {
                        DpiFriendlyImagedButton cmdReturn
                            = new DpiFriendlyImagedButton
                            {
                                Dock = DockStyle.Fill,
                                AutoSize = true,
                                FlatStyle = FlatStyle.Flat
                            };
                        cmdReturn.BatchSetImages(Resources.note_edit_16, Resources.note_edit_20, Resources.note_edit_24,
                            Resources.note_edit_32,
                            Resources.note_edit_48, Resources.note_edit_64);
                        cmdReturn.FlatAppearance.BorderSize = 0;

                        return new ButtonTableCell<Power>(cmdReturn, MyToken)
                        {
                            ClickHandler = ClickHandler,
                            Alignment = Alignment.Center
                        };

                        async Task ClickHandler(Power p, CancellationToken t)
                        {
                            try
                            {
                                CursorWait objCursorWait =
                                    await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                                try
                                {
                                    string strNotes = await p.GetNotesAsync(t).ConfigureAwait(false);
                                    Color objColor = await p.GetNotesColorAsync(t).ConfigureAwait(false);
                                    using (ThreadSafeForm<EditNotes> frmPowerNotes = await ThreadSafeForm<EditNotes>
                                               .GetAsync(() => new EditNotes(strNotes, objColor, t), t)
                                               .ConfigureAwait(false))
                                    {
                                        if (await frmPowerNotes.ShowDialogSafeAsync(_objCharacter, t)
                                                .ConfigureAwait(false) == DialogResult.OK)
                                        {
                                            await p.SetNotesAsync(frmPowerNotes.MyForm.Notes, t).ConfigureAwait(false);
                                            await p.SetNotesColorAsync(frmPowerNotes.MyForm.NotesColor, t).ConfigureAwait(false);
                                        }
                                    }
                                }
                                finally
                                {
                                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                //swallow this
                            }
                        }
                    })
                {
                    Text = "Notes",
                    Tag = "ColumnHeader_Notes",
                    ToolTipExtractor = ToolTipExtractor
                };

                async Task<string> ToolTipExtractor(Power p, CancellationToken t)
                {
                    try
                    {
                        CursorWait objCursorWait =
                            await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                        try
                        {
                            string strTooltip = await LanguageManager
                                .GetStringAsync("Tip_Power_EditNotes", token: t).ConfigureAwait(false);
                            string strNotes = await p.GetNotesAsync(t).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(strNotes))
                                strTooltip += Environment.NewLine + Environment.NewLine +
                                              await strNotes.RtfToPlainTextAsync(token: t).ConfigureAwait(false);
                            return strTooltip.WordWrap();
                        }
                        finally
                        {
                            await objCursorWait.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                }
            });
            noteColumn.AddDependency(nameof(Power.Notes));

            TableColumn<Power> deleteColumn = this.DoThreadSafeFunc(() =>
            {
                return new TableColumn<Power>(
                    () =>
                    {
                        DpiFriendlyImagedButton cmdReturn
                            = new DpiFriendlyImagedButton
                            {
                                Dock = DockStyle.Fill,
                                AutoSize = true,
                                FlatStyle = FlatStyle.Flat
                            };
                        cmdReturn.BatchSetImages(Resources.delete_16, Resources.delete_20, Resources.delete_24,
                            Resources.delete_32,
                            Resources.delete_48, Resources.delete_64);
                        cmdReturn.FlatAppearance.BorderSize = 0;

                        return new ButtonTableCell<Power>(cmdReturn, MyToken)
                        {
                            ClickHandler = ClickHandler,
                            EnabledExtractor = EnabledExtractor
                        };

                        async Task ClickHandler(Power p, CancellationToken t)
                        {
                            try
                            {
                                CursorWait objCursorWait =
                                    await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                                try
                                {
                                    //Cache the parentform prior to deletion, otherwise the relationship is broken.
                                    Form frmParent = await this.DoThreadSafeFuncAsync(x => x.ParentForm, token: t)
                                        .ConfigureAwait(false);
                                    if (await p.GetFreeLevelsAsync(t).ConfigureAwait(false) > 0)
                                    {
                                        string strExtra = await p.GetExtraAsync(t).ConfigureAwait(false);
                                        string strImprovementSourceName =
                                            (await ImprovementManager
                                                .GetCachedImprovementListForValueOfAsync(p.CharacterObject,
                                                    Improvement.ImprovementType.AdeptPowerFreePoints, await p.GetNameAsync(t).ConfigureAwait(false),
                                                    token: t).ConfigureAwait(false))
                                            .Find(x => x.UniqueName == strExtra)?.SourceName;
                                        if (!string.IsNullOrWhiteSpace(strImprovementSourceName))
                                        {
                                            Gear objGear = await (await p.CharacterObject.GetGearAsync(t).ConfigureAwait(false)).FindByIdAsync(strImprovementSourceName, t).ConfigureAwait(false);
                                            if (objGear?.Bonded == true)
                                            {
                                                await objGear.SetEquippedAsync(false, t).ConfigureAwait(false);
                                                objGear.Extra = string.Empty;
                                            }
                                        }
                                    }

                                    await p.DeletePowerAsync(t).ConfigureAwait(false);

                                    if (frmParent is CharacterShared objParent)
                                        objParent.RequestCharacterUpdate(t);
                                }
                                finally
                                {
                                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                //swallow this
                            }
                        }

                        async Task<bool> EnabledExtractor(Power p, CancellationToken t) =>
                            await p.GetFreeLevelsAsync(t).ConfigureAwait(false) == 0;
                    })
                {
                    Text = string.Empty,
                    ToolTipExtractor = ToolTipExtractor
                };

                async Task<string> ToolTipExtractor(Power p, CancellationToken t)
                {
                    try
                    {
                        return (await LanguageManager.GetStringAsync("String_Delete", token: t)
                            .ConfigureAwait(false)).WordWrap();
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                }
            });
            deleteColumn.AddDependency(nameof(Power.FreeLevels));

            TableColumn<Power> reapplyImprovementsColumn = this.DoThreadSafeFunc(() =>
            {
                return new TableColumn<Power>(
                    () =>
                    {
                        DpiFriendlyImagedButton cmdReturn
                            = new DpiFriendlyImagedButton
                            {
                                Dock = DockStyle.Fill,
                                AutoSize = true,
                                FlatStyle = FlatStyle.Flat
                            };
                        cmdReturn.BatchSetImages(Resources.page_refresh_16, Resources.page_refresh_20,
                            Resources.page_refresh_24,
                            Resources.page_refresh_32,
                            Resources.page_refresh_48, Resources.page_refresh_64);
                        cmdReturn.FlatAppearance.BorderSize = 0;

                        return new ButtonTableCell<Power>(cmdReturn, MyToken)
                        {
                            ClickHandler = ClickHandler,
                            Alignment = Alignment.Center
                        };

                        async Task ClickHandler(Power p, CancellationToken t)
                        {
                            try
                            {
                                CursorWait objCursorWait =
                                    await CursorWait.NewAsync(this, token: t).ConfigureAwait(false);
                                try
                                {
                                    switch (ParentForm)
                                    {
                                        case CharacterCreate frmCreate:
                                            await frmCreate.ReapplySpecificImprovements(p.InternalId,
                                                    await p.GetCurrentDisplayNameAsync(t).ConfigureAwait(false),
                                                    t)
                                                .ConfigureAwait(false);
                                            break;
                                        case CharacterCareer frmCareer:
                                            await frmCareer.ReapplySpecificImprovements(p.InternalId,
                                                    await p.GetCurrentDisplayNameAsync(t).ConfigureAwait(false),
                                                    t)
                                                .ConfigureAwait(false);
                                            break;
                                    }
                                }
                                finally
                                {
                                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                //swallow this
                            }
                        }
                    })
                {
                    Text = string.Empty,
                    ToolTipExtractor = ToolTipExtractor
                };

                async Task<string> ToolTipExtractor(Power p, CancellationToken t)
                {
                    try
                    {
                        return (await LanguageManager.GetStringAsync("Menu_SpecialReapplyImprovements", token: t)
                            .ConfigureAwait(false)).WordWrap();
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                }
            });

            try
            {
                _table.Columns.Add(nameColumn, MyToken);
                _table.Columns.Add(actionColumn, MyToken);
                _table.Columns.Add(ratingColumn, MyToken);
                _table.Columns.Add(totalRatingColumn, MyToken);
                _table.Columns.Add(powerPointsColumn, MyToken);
                _table.Columns.Add(adeptWayColumn, MyToken);
                //_table.Columns.Add(geasColumn, MyToken);
                _table.Columns.Add(noteColumn, MyToken);
                _table.Columns.Add(sourceColumn, MyToken);
                _table.Columns.Add(deleteColumn, MyToken);
                _table.Columns.Add(reapplyImprovementsColumn, MyToken);
                _table.UpdateLightDarkMode(token: MyToken);
                _table.TranslateWinForm(token: MyToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            pnlPowers.Controls.Add(_table);
        }
    }
}
