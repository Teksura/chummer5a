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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Chummer.Annotations;

namespace Chummer
{
    [DebuggerDisplay("{DisplayName(GlobalSettings.InvariantCultureInfo, GlobalSettings.DefaultLanguage)}")]
    public sealed class CalendarWeek : IHasInternalId, IComparable, INotifyMultiplePropertyChangedAsync, IEquatable<CalendarWeek>, IComparable<CalendarWeek>, IHasNotes, IHasLockObject
    {
        private Guid _guiID;
        private int _intYear = 2072;
        private int _intWeek = 1;
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly List<PropertyChangedAsyncEventHandler> _lstPropertyChangedAsync =
            new List<PropertyChangedAsyncEventHandler>();

        public event PropertyChangedAsyncEventHandler PropertyChangedAsync
        {
            add
            {
                using (LockObject.EnterWriteLock())
                    _lstPropertyChangedAsync.Add(value);
            }
            remove
            {
                using (LockObject.EnterWriteLock())
                    _lstPropertyChangedAsync.Remove(value);
            }
        }

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string strPropertyName = null)
        {
            this.OnMultiplePropertyChanged(strPropertyName);
        }

        public Task OnPropertyChangedAsync(string strPropertyName, CancellationToken token = default)
        {
            return this.OnMultiplePropertyChangedAsync(token, strPropertyName);
        }

        public void OnMultiplePropertyChanged(IReadOnlyCollection<string> lstPropertyNames)
        {
            using (LockObject.EnterUpgradeableReadLock())
            {
                if (_lstPropertyChangedAsync.Count > 0)
                {
                    List<PropertyChangedEventArgs> lstArgsList = lstPropertyNames.Select(x => new PropertyChangedEventArgs(x)).ToList();
                    Func<Task>[] aFuncs = new Func<Task>[lstArgsList.Count * _lstPropertyChangedAsync.Count];
                    int i = 0;
                    foreach (PropertyChangedAsyncEventHandler objEvent in _lstPropertyChangedAsync)
                    {
                        foreach (PropertyChangedEventArgs objArg in lstArgsList)
                            aFuncs[i++] = () => objEvent.Invoke(this, objArg);
                    }

                    Utils.RunWithoutThreadLock(aFuncs, CancellationToken.None);
                    if (PropertyChanged != null)
                    {
                        Utils.RunOnMainThread(() =>
                        {
                            if (PropertyChanged != null)
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                foreach (PropertyChangedEventArgs objArgs in lstArgsList)
                                {
                                    PropertyChanged.Invoke(this, objArgs);
                                }
                            }
                        });
                    }
                }
                else if (PropertyChanged != null)
                {
                    Utils.RunOnMainThread(() =>
                    {
                        if (PropertyChanged != null)
                        {
                            foreach (string strPropertyToChange in lstPropertyNames)
                            {
                                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(strPropertyToChange));
                            }
                        }
                    });
                }
            }
        }

        public async Task OnMultiplePropertyChangedAsync(IReadOnlyCollection<string> lstPropertyNames, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using (await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (_lstPropertyChangedAsync.Count > 0)
                {
                    List<PropertyChangedEventArgs> lstArgsList = lstPropertyNames.Select(x => new PropertyChangedEventArgs(x)).ToList();
                    List<Task> lstTasks = new List<Task>(Math.Min(lstArgsList.Count * _lstPropertyChangedAsync.Count, Utils.MaxParallelBatchSize));
                    int i = 0;
                    foreach (PropertyChangedAsyncEventHandler objEvent in _lstPropertyChangedAsync)
                    {
                        foreach (PropertyChangedEventArgs objArg in lstArgsList)
                        {
                            lstTasks.Add(objEvent.Invoke(this, objArg, token));
                            if (++i < Utils.MaxParallelBatchSize)
                                continue;
                            await Task.WhenAll(lstTasks).ConfigureAwait(false);
                            lstTasks.Clear();
                            i = 0;
                        }
                    }
                    await Task.WhenAll(lstTasks).ConfigureAwait(false);
                    if (PropertyChanged != null)
                    {
                        await Utils.RunOnMainThreadAsync(() =>
                        {
                            if (PropertyChanged != null)
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                foreach (PropertyChangedEventArgs objArgs in lstArgsList)
                                {
                                    token.ThrowIfCancellationRequested();
                                    PropertyChanged.Invoke(this, objArgs);
                                }
                            }
                        }, token).ConfigureAwait(false);
                    }
                }
                else if (PropertyChanged != null)
                {
                    await Utils.RunOnMainThreadAsync(() =>
                    {
                        if (PropertyChanged != null)
                        {
                            // ReSharper disable once AccessToModifiedClosure
                            foreach (string strPropertyToChange in lstPropertyNames)
                            {
                                token.ThrowIfCancellationRequested();
                                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(strPropertyToChange));
                            }
                        }
                    }, token).ConfigureAwait(false);
                }
            }
        }

        #region Constructor, Save, Load, and Print Methods

        public CalendarWeek()
        {
            // Create the GUID for the new CalendarWeek.
            _guiID = Guid.NewGuid();
        }

        public CalendarWeek(int intYear, int intWeek)
        {
            // Create the GUID for the new CalendarWeek.
            _guiID = Guid.NewGuid();
            _intYear = intYear;
            _intWeek = intWeek;
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;
            using (LockObject.EnterReadLock())
            {
                objWriter.WriteStartElement("week");
                objWriter.WriteElementString("guid", _guiID.ToString("D", GlobalSettings.InvariantCultureInfo));
                objWriter.WriteElementString("year", _intYear.ToString(GlobalSettings.InvariantCultureInfo));
                objWriter.WriteElementString("week", _intWeek.ToString(GlobalSettings.InvariantCultureInfo));
                objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
                objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
                objWriter.WriteEndElement();
            }
        }

        /// <summary>
        /// Load the Calendar Week from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            using (LockObject.EnterWriteLock())
            {
                objNode.TryGetField("guid", Guid.TryParse, out _guiID);
                objNode.TryGetInt32FieldQuickly("year", ref _intYear);
                objNode.TryGetInt32FieldQuickly("week", ref _intWeek);
                objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);
                string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
                objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
                _colNotes = ColorTranslator.FromHtml(sNotesColor);
            }
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="objCulture">Culture in which to print numbers.</param>
        /// <param name="blnPrintNotes">Whether to print notes attached to the CalendarWeek.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, CultureInfo objCulture, bool blnPrintNotes = true, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            IAsyncDisposable objLocker = await LockObject.EnterHiPrioReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // <week>
                XmlElementWriteHelper objBaseElement
                    = await objWriter.StartElementAsync("week", token: token).ConfigureAwait(false);
                try
                {
                    await objWriter.WriteElementStringAsync("guid", InternalId, token: token).ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("year", Year.ToString(objCulture), token: token)
                        .ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("month", Month.ToString(objCulture), token: token)
                        .ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("week", MonthWeek.ToString(objCulture), token: token)
                        .ConfigureAwait(false);
                    if (blnPrintNotes)
                        await objWriter.WriteElementStringAsync("notes", Notes, token: token).ConfigureAwait(false);
                }
                finally
                {
                    // </week>
                    await objBaseElement.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Internal identifier which will be used to identify this Calendar Week in the Improvement system.
        /// </summary>
        public string InternalId
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);
            }
        }

        /// <summary>
        /// Year.
        /// </summary>
        public int Year
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _intYear;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _intYear, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Month.
        /// </summary>
        public int Month
        {
            get
            {
                switch (Week)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return 1;

                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        return 2;

                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        return 3;

                    case 14:
                    case 15:
                    case 16:
                    case 17:
                        return 4;

                    case 18:
                    case 19:
                    case 20:
                    case 21:
                        return 5;

                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                        return 6;

                    case 27:
                    case 28:
                    case 29:
                    case 30:
                        return 7;

                    case 31:
                    case 32:
                    case 33:
                    case 34:
                        return 8;

                    case 35:
                    case 36:
                    case 37:
                    case 38:
                    case 39:
                        return 9;

                    case 40:
                    case 41:
                    case 42:
                    case 43:
                        return 10;

                    case 44:
                    case 45:
                    case 46:
                    case 47:
                        return 11;

                    default:
                        return 12;
                }
            }
        }

        /// <summary>
        /// Week of the month.
        /// </summary>
        public int MonthWeek
        {
            get
            {
                switch (Week)
                {
                    case 1:
                    case 5:
                    case 9:
                    case 14:
                    case 18:
                    case 22:
                    case 27:
                    case 31:
                    case 35:
                    case 40:
                    case 44:
                    case 48:
                        return 1;

                    case 2:
                    case 6:
                    case 10:
                    case 15:
                    case 19:
                    case 23:
                    case 28:
                    case 32:
                    case 36:
                    case 41:
                    case 45:
                    case 49:
                        return 2;

                    case 3:
                    case 7:
                    case 11:
                    case 16:
                    case 20:
                    case 24:
                    case 29:
                    case 33:
                    case 37:
                    case 42:
                    case 46:
                    case 50:
                        return 3;

                    case 4:
                    case 8:
                    case 12:
                    case 17:
                    case 21:
                    case 25:
                    case 30:
                    case 34:
                    case 38:
                    case 43:
                    case 47:
                    case 51:
                        return 4;

                    default:
                        return 5;
                }
            }
        }

        public string CurrentDisplayName => DisplayName(GlobalSettings.CultureInfo, GlobalSettings.Language);

        /// <summary>
        /// Month and Week to display.
        /// </summary>
        public string DisplayName(CultureInfo objCulture, string strLanguage)
        {
            using (LockObject.EnterReadLock())
            {
                string strReturn = string.Format(
                    objCulture, LanguageManager.GetString("String_WeekDisplay", strLanguage)
                    , Year
                    , Month
                    , MonthWeek);
                return strReturn;
            }
        }

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) => DisplayNameAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        /// <summary>
        /// Month and Week to display.
        /// </summary>
        public async Task<string> DisplayNameAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            using (await LockObject.EnterReadLockAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                string strReturn = string.Format(
                    objCulture, await LanguageManager.GetStringAsync("String_WeekDisplay", strLanguage, token: token)
                                                     .ConfigureAwait(false)
                    , Year
                    , Month
                    , MonthWeek);
                return strReturn;
            }
        }

        /// <summary>
        /// Week.
        /// </summary>
        public int Week
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _intWeek;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _intWeek, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strNotes;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strNotes, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Forecolor to use for Notes in treeviews.
        /// </summary>
        public Color NotesColor
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _colNotes;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (_colNotes == value)
                        return;
                    using (LockObject.EnterWriteLock())
                    {
                        _colNotes = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        public Color PreferredColor
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    return !string.IsNullOrEmpty(Notes)
                        ? ColorManager.GenerateCurrentModeColor(NotesColor)
                        : ColorManager.WindowText;
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is CalendarWeek objWeek)
                return CompareTo(objWeek);
            return -string.Compare(CurrentDisplayName, obj?.ToString() ?? string.Empty, false, GlobalSettings.CultureInfo);
        }

        public int CompareTo(CalendarWeek other)
        {
            using (LockObject.EnterReadLock())
            using (other.LockObject.EnterReadLock())
            {
                int intReturn = Year.CompareTo(other.Year);
                if (intReturn == 0)
                    intReturn = Week.CompareTo(other.Week);
                return -intReturn;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            return obj is CalendarWeek objOther && Equals(objOther);
        }

        public bool Equals(CalendarWeek other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            using (LockObject.EnterReadLock())
            using (other.LockObject.EnterReadLock())
                return Year == other.Year && Week == other.Week;
        }

        public override int GetHashCode()
        {
            using (LockObject.EnterReadLock())
                return (InternalId, Year, Week).GetHashCode();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return LockObject.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            LockObject.Dispose();
        }

        public static bool operator ==(CalendarWeek left, CalendarWeek right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(CalendarWeek left, CalendarWeek right)
        {
            return !(left == right);
        }

        public static bool operator <(CalendarWeek left, CalendarWeek right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CalendarWeek left, CalendarWeek right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CalendarWeek left, CalendarWeek right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CalendarWeek left, CalendarWeek right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion Properties

        /// <inheritdoc />
        public AsyncFriendlyReaderWriterLock LockObject { get; } = new AsyncFriendlyReaderWriterLock();
    }
}
