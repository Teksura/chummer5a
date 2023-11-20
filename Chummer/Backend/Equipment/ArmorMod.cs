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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Backend.Attributes;
using NLog;

namespace Chummer.Backend.Equipment
{
    /// <summary>
    /// A piece of Armor Modification.
    /// </summary>
    [DebuggerDisplay("{DisplayName(GlobalSettings.InvariantCultureInfo, GlobalSettings.DefaultLanguage)}")]
    public sealed class ArmorMod : IHasInternalId, IHasName, IHasSourceId, IHasXmlDataNode, IHasNotes, ICanSell, ICanEquip, IHasSource, IHasRating, ICanSort, IHasWirelessBonus, IHasStolenProperty, ICanPaste, IHasGear, ICanBlackMarketDiscount, IDisposable, IAsyncDisposable
    {
        private static readonly Lazy<Logger> s_ObjLogger = new Lazy<Logger>(LogManager.GetCurrentClassLogger);
        private static Logger Log => s_ObjLogger.Value;
        private Guid _guiID;
        private Guid _guiSourceID;
        private string _strName = string.Empty;
        private string _strCategory = string.Empty;
        private string _strArmorCapacity = "[0]";
        private string _strGearCapacity = string.Empty;
        private int _intArmorValue;
        private int _intMaxRating;
        private int _intRating;
        private string _strRatingLabel = "String_Rating";
        private string _strAvail = string.Empty;
        private string _strCost = string.Empty;
        private string _strWeight = string.Empty;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private bool _blnIncludedInArmor;
        private bool _blnEquipped = true;
        private string _strExtra = string.Empty;
        private Guid _guiWeaponID = Guid.Empty;
        private XmlNode _nodBonus;
        private XmlNode _nodWirelessBonus;
        private bool _blnWirelessOn = true;
        private readonly Character _objCharacter;
        private readonly TaggedObservableCollection<Gear> _lstGear = new TaggedObservableCollection<Gear>();
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;
        private bool _blnDiscountCost;
        private bool _blnStolen;
        private bool _blnEncumbrance = true;
        private int _intSortOrder;

        #region Constructor, Create, Save, Load, and Print Methods

        public ArmorMod(Character objCharacter)
        {
            // Create the GUID for the new Armor Mod.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;

            _lstGear.AddTaggedCollectionChanged(this, GearOnCollectionChanged);
        }

        private async Task GearOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            bool blnDoEquipped = _objCharacter?.IsLoading == false && Equipped && Parent?.Equipped == true;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Gear objNewItem in e.NewItems)
                    {
                        objNewItem.Parent = this;
                        if (blnDoEquipped)
                            await objNewItem.ChangeEquippedStatusAsync(true, token: token).ConfigureAwait(false);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Gear objOldItem in e.OldItems)
                    {
                        objOldItem.Parent = null;
                        if (blnDoEquipped)
                            await objOldItem.ChangeEquippedStatusAsync(false, token: token).ConfigureAwait(false);
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Gear objOldItem in e.OldItems)
                    {
                        objOldItem.Parent = null;
                        if (blnDoEquipped)
                            await objOldItem.ChangeEquippedStatusAsync(false, token: token).ConfigureAwait(false);
                    }

                    foreach (Gear objNewItem in e.NewItems)
                    {
                        objNewItem.Parent = this;
                        if (blnDoEquipped)
                            await objNewItem.ChangeEquippedStatusAsync(true, token: token).ConfigureAwait(false);
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (blnDoEquipped)
                        await _objCharacter.OnPropertyChangedAsync(nameof(Character.TotalCarriedWeight), token).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Create a Armor Modification from an XmlNode.
        /// </summary>
        /// <param name="objXmlArmorNode">XmlNode to create the object from.</param>
        /// <param name="intRating">Rating of the selected ArmorMod.</param>
        /// <param name="lstWeapons">List of Weapons that are created by the Armor.</param>
        /// <param name="blnSkipCost">Whether or not creating the ArmorMod should skip the Variable price dialogue (should only be used by frmSelectArmor).</param>
        /// <param name="blnSkipSelectForms">Whether or not to skip selection forms (related to improvements) when creating this ArmorMod.</param>
        public void Create(XmlNode objXmlArmorNode, int intRating, IList<Weapon> lstWeapons, bool blnSkipCost = false, bool blnSkipSelectForms = false)
        {
            if (!objXmlArmorNode.TryGetField("id", Guid.TryParse, out _guiSourceID))
            {
                Log.Warn(new object[] { "Missing id field for armor mod xmlnode", objXmlArmorNode });
                Utils.BreakIfDebug();
            }
            else
            {
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }

            _blnEquipped = !blnSkipSelectForms;
            objXmlArmorNode.TryGetStringFieldQuickly("name", ref _strName);
            objXmlArmorNode.TryGetStringFieldQuickly("category", ref _strCategory);
            objXmlArmorNode.TryGetStringFieldQuickly("armorcapacity", ref _strArmorCapacity);
            objXmlArmorNode.TryGetStringFieldQuickly("gearcapacity", ref _strGearCapacity);
            _intRating = intRating;
            objXmlArmorNode.TryGetInt32FieldQuickly("armor", ref _intArmorValue);
            objXmlArmorNode.TryGetInt32FieldQuickly("maxrating", ref _intMaxRating);
            objXmlArmorNode.TryGetStringFieldQuickly("ratinglabel", ref _strRatingLabel);
            objXmlArmorNode.TryGetStringFieldQuickly("avail", ref _strAvail);
            objXmlArmorNode.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlArmorNode.TryGetStringFieldQuickly("page", ref _strPage);
            if (!objXmlArmorNode.TryGetMultiLineStringFieldQuickly("altnotes", ref _strNotes))
                objXmlArmorNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objXmlArmorNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
            {
                Notes = CommonFunctions.GetBookNotes(objXmlArmorNode, Name, CurrentDisplayName, Source, Page,
                    DisplayPage(GlobalSettings.Language), _objCharacter);
            }

            objXmlArmorNode.TryGetBoolFieldQuickly("encumbrance", ref _blnEncumbrance);

            _nodBonus = objXmlArmorNode["bonus"];
            _nodWirelessBonus = objXmlArmorNode["wirelessbonus"];

            objXmlArmorNode.TryGetStringFieldQuickly("cost", ref _strCost);
            objXmlArmorNode.TryGetStringFieldQuickly("weight", ref _strWeight);

            // Check for a Variable Cost.
            if (!blnSkipCost && _strCost.StartsWith("Variable(", StringComparison.Ordinal))
            {
                string strFirstHalf = _strCost.TrimStartOnce("Variable(", true).TrimEndOnce(')');
                string strSecondHalf = string.Empty;
                int intHyphenIndex = strFirstHalf.IndexOf('-');
                if (intHyphenIndex != -1)
                {
                    if (intHyphenIndex + 1 < strFirstHalf.Length)
                        strSecondHalf = strFirstHalf.Substring(intHyphenIndex + 1);
                    strFirstHalf = strFirstHalf.Substring(0, intHyphenIndex);
                }

                if (!blnSkipSelectForms)
                {
                    decimal decMin;
                    decimal decMax = decimal.MaxValue;
                    if (intHyphenIndex != -1)
                    {
                        decMin = Convert.ToDecimal(strFirstHalf, GlobalSettings.InvariantCultureInfo);
                        decMax = Convert.ToDecimal(strSecondHalf, GlobalSettings.InvariantCultureInfo);
                    }
                    else
                        decMin = Convert.ToDecimal(strFirstHalf.FastEscape('+'), GlobalSettings.InvariantCultureInfo);

                    if (decMin != decimal.MinValue || decMax != decimal.MaxValue)
                    {
                        if (decMax > 1000000)
                            decMax = 1000000;

                        using (ThreadSafeForm<SelectNumber> frmPickNumber
                               = ThreadSafeForm<SelectNumber>.Get(() => new SelectNumber(_objCharacter.Settings.MaxNuyenDecimals)
                               {
                                   Minimum = decMin,
                                   Maximum = decMax,
                                   Description = string.Format(
                                       GlobalSettings.CultureInfo,
                                       LanguageManager.GetString("String_SelectVariableCost"),
                                       CurrentDisplayNameShort),
                                   AllowCancel = false
                               }))
                        {
                            if (frmPickNumber.ShowDialogSafe(_objCharacter) == DialogResult.Cancel)
                            {
                                _guiID = Guid.Empty;
                                return;
                            }
                            _strCost = frmPickNumber.MyForm.SelectedValue.ToString(GlobalSettings.InvariantCultureInfo);
                        }
                    }
                    else
                        _strCost = strFirstHalf;
                }
                else
                    _strCost = strFirstHalf;
            }

            if (objXmlArmorNode["bonus"] != null && !blnSkipSelectForms)
            {
                if (!ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.ArmorMod, _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), objXmlArmorNode["bonus"], intRating, CurrentDisplayNameShort))
                {
                    _guiID = Guid.Empty;
                    return;
                }
                if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                {
                    _strExtra = ImprovementManager.SelectedValue;
                }
            }

            // Add any Gear that comes with the Armor.
            XmlNode xmlChildrenNode = objXmlArmorNode["gears"];
            if (xmlChildrenNode != null)
            {
                XmlDocument objXmlGearDocument = _objCharacter.LoadData("gear.xml");
                using (XmlNodeList xmlUseGearList = xmlChildrenNode.SelectNodes("usegear"))
                {
                    if (xmlUseGearList != null)
                    {
                        foreach (XmlNode objXmlArmorGear in xmlUseGearList)
                        {
                            Gear objGear = new Gear(_objCharacter);
                            if (!objGear.CreateFromNode(objXmlGearDocument, objXmlArmorGear, lstWeapons, !blnSkipSelectForms))
                                continue;
                            foreach (Weapon objWeapon in lstWeapons)
                            {
                                objWeapon.ParentID = InternalId;
                            }
                            objGear.Parent = this;
                            objGear.ParentID = InternalId;
                            GearChildren.Add(objGear);
                        }
                    }
                }
            }

            // Add Weapons if applicable.
            // More than one Weapon can be added, so loop through all occurrences.
            using (XmlNodeList xmlAddWeaponList = objXmlArmorNode.SelectNodes("addweapon"))
            {
                if (xmlAddWeaponList != null)
                {
                    XmlDocument objXmlWeaponDocument = _objCharacter.LoadData("weapons.xml");

                    foreach (XmlNode objXmlAddWeapon in xmlAddWeaponList)
                    {
                        XmlNode objXmlWeapon = objXmlWeaponDocument.TryGetNodeByNameOrId("/chummer/weapons/weapon",
                            objXmlAddWeapon.InnerText);

                        if (objXmlWeapon != null)
                        {
                            int intAddWeaponRating = 0;
                            string strLoopRating = objXmlAddWeapon.Attributes?["rating"]?.InnerText;
                            if (!string.IsNullOrEmpty(strLoopRating))
                            {
                                strLoopRating = strLoopRating.CheapReplace("{Rating}",
                                                                           () => Rating.ToString(
                                                                               GlobalSettings
                                                                                   .InvariantCultureInfo));
                                int.TryParse(strLoopRating, NumberStyles.Any, GlobalSettings.InvariantCultureInfo,
                                             out intAddWeaponRating);
                            }

                            Weapon objGearWeapon = new Weapon(_objCharacter);
                            objGearWeapon.Create(objXmlWeapon, lstWeapons, true, !blnSkipSelectForms, blnSkipCost,
                                                 intAddWeaponRating);
                            objGearWeapon.ParentID = InternalId;
                            objGearWeapon.Cost = "0";
                            if (Guid.TryParse(objGearWeapon.InternalId, out _guiWeaponID))
                                lstWeapons.Add(objGearWeapon);
                            else
                                _guiWeaponID = Guid.Empty;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;

            objWriter.WriteStartElement("armormod");
            objWriter.WriteElementString("guid", InternalId);
            objWriter.WriteElementString("sourceid", SourceIDString);
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("category", _strCategory);
            objWriter.WriteElementString("armor", _intArmorValue.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("armorcapacity", _strArmorCapacity);
            objWriter.WriteElementString("gearcapacity", _strGearCapacity);
            objWriter.WriteElementString("maxrating", _intMaxRating.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("rating", _intRating.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("ratinglabel", _strRatingLabel);
            objWriter.WriteElementString("avail", _strAvail);
            objWriter.WriteElementString("cost", _strCost);
            objWriter.WriteElementString("weight", _strWeight);
            if (_lstGear.Count > 0)
            {
                objWriter.WriteStartElement("gears");
                foreach (Gear objGear in _lstGear)
                {
                    objGear.Save(objWriter);
                }
                objWriter.WriteEndElement();
            }
            if (_nodBonus != null)
                objWriter.WriteRaw(_nodBonus.OuterXml);
            else
                objWriter.WriteElementString("bonus", string.Empty);
            if (_nodWirelessBonus != null)
                objWriter.WriteRaw(_nodWirelessBonus.OuterXml);
            else
                objWriter.WriteElementString("wirelessbonus", string.Empty);
            objWriter.WriteElementString("wirelesson", _blnWirelessOn.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("included", _blnIncludedInArmor.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("equipped", _blnEquipped.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("stolen", _blnStolen.ToString(GlobalSettings.InvariantCultureInfo));
            if (_guiWeaponID != Guid.Empty)
                objWriter.WriteElementString("weaponguid", _guiWeaponID.ToString("D", GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
            objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
            objWriter.WriteElementString("discountedcost", _blnDiscountCost.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("sortorder", _intSortOrder.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the CharacterAttribute from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        /// <param name="blnCopy">Whether or not we are copying an existing node.</param>
        public void Load(XmlNode objNode, bool blnCopy = false)
        {
            if (objNode == null)
                return;
            objNode.TryGetStringFieldQuickly("name", ref _strName);
            _objCachedMyXmlNode = null;
            _objCachedMyXPathNode = null;
            Lazy<XPathNavigator> objMyNode = new Lazy<XPathNavigator>(() => this.GetNodeXPath());
            if (blnCopy || !objNode.TryGetField("guid", Guid.TryParse, out _guiID))
            {
                _guiID = Guid.NewGuid();
            }
            if (!objNode.TryGetGuidFieldQuickly("sourceid", ref _guiSourceID))
            {
                objMyNode.Value?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
            }
            objNode.TryGetStringFieldQuickly("category", ref _strCategory);
            objNode.TryGetInt32FieldQuickly("armor", ref _intArmorValue);
            objNode.TryGetStringFieldQuickly("armorcapacity", ref _strArmorCapacity);
            objNode.TryGetStringFieldQuickly("gearcapacity", ref _strGearCapacity);
            objNode.TryGetInt32FieldQuickly("maxrating", ref _intMaxRating);
            objNode.TryGetStringFieldQuickly("ratinglabel", ref _strRatingLabel);
            objNode.TryGetInt32FieldQuickly("rating", ref _intRating);
            objNode.TryGetStringFieldQuickly("avail", ref _strAvail);
            objNode.TryGetStringFieldQuickly("cost", ref _strCost);
            if (!objNode.TryGetStringFieldQuickly("weight", ref _strWeight))
                objMyNode.Value?.TryGetStringFieldQuickly("weight", ref _strWeight);
            _nodBonus = objNode["bonus"];
            _nodWirelessBonus = objNode["wirelessbonus"];
            objNode.TryGetStringFieldQuickly("source", ref _strSource);
            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetBoolFieldQuickly("included", ref _blnIncludedInArmor);
            objNode.TryGetBoolFieldQuickly("equipped", ref _blnEquipped);
            objNode.TryGetBoolFieldQuickly("stolen", ref _blnStolen);
            if (!objNode.TryGetBoolFieldQuickly("wirelesson", ref _blnWirelessOn))
                _blnWirelessOn = false;
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            objNode.TryGetField("weaponguid", Guid.TryParse, out _guiWeaponID);
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            objNode.TryGetBoolFieldQuickly("encumbrance", ref _blnEncumbrance);
            objNode.TryGetBoolFieldQuickly("discountedcost", ref _blnDiscountCost);
            objNode.TryGetInt32FieldQuickly("sortorder", ref _intSortOrder);

            XmlNode xmlChildrenNode = objNode["gears"];
            if (xmlChildrenNode != null)
            {
                using (XmlNodeList nodGears = xmlChildrenNode.SelectNodes("gear"))
                {
                    if (nodGears != null)
                    {
                        foreach (XmlNode nodGear in nodGears)
                        {
                            Gear objGear = new Gear(_objCharacter);
                            objGear.Load(nodGear, blnCopy);
                            _lstGear.Add(objGear);
                        }
                    }
                }
            }

            if (!blnCopy)
                return;
            if (!string.IsNullOrEmpty(Extra))
                ImprovementManager.ForcedValue = Extra;
            ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.ArmorMod, _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), Bonus, 1, CurrentDisplayNameShort);
            if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
            {
                Extra = ImprovementManager.SelectedValue;
            }

            if (!_blnEquipped)
            {
                _blnEquipped = true;
                Equipped = false;
            }
            RefreshWirelessBonuses();
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="objCulture">Culture in which to print.</param>
        /// <param name="strLanguageToPrint">Language in which to print</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, CultureInfo objCulture, string strLanguageToPrint, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            // <armormod>
            XmlElementWriteHelper objBaseElement = await objWriter.StartElementAsync("armormod", token).ConfigureAwait(false);
            try
            {
                await objWriter.WriteElementStringAsync("name", await DisplayNameShortAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("fullname", await DisplayNameAsync(objCulture, strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("name_english", Name, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("category", await DisplayCategoryAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("category_english", Category, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("armor", Armor.ToString(objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("maxrating", MaximumRating.ToString(objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("rating", Rating.ToString(objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("ratinglabel", RatingLabel, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("avail", await TotalAvailAsync(objCulture, strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("cost", (await GetTotalCostAsync(token).ConfigureAwait(false)).ToString(_objCharacter.Settings.NuyenFormat, objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("owncost", (await GetOwnCostAsync(token).ConfigureAwait(false)).ToString(_objCharacter.Settings.NuyenFormat, objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("weight", TotalWeight.ToString(_objCharacter.Settings.WeightFormat, objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("ownweight", OwnWeight.ToString(_objCharacter.Settings.WeightFormat, objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("source", await _objCharacter.LanguageBookShortAsync(Source, strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("page", await DisplayPageAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("included", IncludedInArmor.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("equipped", Equipped.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("wirelesson", WirelessOn.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                // <gears>
                XmlElementWriteHelper objGearsElement = await objWriter.StartElementAsync("gears", token).ConfigureAwait(false);
                try
                {
                    foreach (Gear objGear in GearChildren)
                    {
                        await objGear.Print(objWriter, objCulture, strLanguageToPrint, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    // </gears>
                    await objGearsElement.DisposeAsync().ConfigureAwait(false);
                }
                await objWriter.WriteElementStringAsync("extra", await _objCharacter.TranslateExtraAsync(_strExtra, strLanguageToPrint, token: token).ConfigureAwait(false), token).ConfigureAwait(false);
                if (GlobalSettings.PrintNotes)
                    await objWriter.WriteElementStringAsync("notes", Notes, token).ConfigureAwait(false);
            }
            finally
            {
                // </armormod>
                await objBaseElement.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Create, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Internal identifier which will be used to identify this piece of Armor in the Improvement system.
        /// </summary>
        public string InternalId => _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Identifier of the object within data files.
        /// </summary>
        public Guid SourceID => _guiSourceID;

        /// <summary>
        /// String-formatted identifier of the <inheritdoc cref="SourceID"/> from the data files.
        /// </summary>
        public string SourceIDString => _guiSourceID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Guid of a Cyberware Weapon.
        /// </summary>
        public string WeaponID
        {
            get => _guiWeaponID.ToString("D", GlobalSettings.InvariantCultureInfo);
            set
            {
                if (Guid.TryParse(value, out Guid guiTemp))
                    _guiWeaponID = guiTemp;
            }
        }

        /// <summary>
        /// Bonus node from the XML file.
        /// </summary>
        public XmlNode Bonus
        {
            get => _nodBonus;
            set => _nodBonus = value;
        }

        /// <summary>
        /// Wireless Bonus node from the XML file.
        /// </summary>
        public XmlNode WirelessBonus
        {
            get => _nodWirelessBonus;
            set => _nodWirelessBonus = value;
        }

        /// <summary>
        /// Name of the Mod.
        /// </summary>
        public string Name
        {
            get => _strName;
            set
            {
                if (Interlocked.Exchange(ref _strName, value) == value)
                    return;
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Name;

            return this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("translate")?.Value ?? Name;
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public async Task<string> DisplayNameShortAsync(string strLanguage, CancellationToken token = default)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Name;

            XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
            return objNode != null ? objNode.SelectSingleNodeAndCacheExpression("translate", token: token)?.Value ?? Name : Name;
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Qty Name (Rating) (Extra).
        /// </summary>
        public string DisplayName(CultureInfo objCulture, string strLanguage)
        {
            string strReturn = DisplayNameShort(strLanguage);
            string strSpace = LanguageManager.GetString("String_Space", strLanguage);
            if (Rating > 0)
                strReturn += strSpace + '(' + LanguageManager.GetString(RatingLabel, strLanguage) + strSpace + Rating.ToString(objCulture) + ')';
            if (!string.IsNullOrEmpty(Extra))
                strReturn += strSpace + '(' + _objCharacter.TranslateExtra(Extra, strLanguage) + ')';
            return strReturn;
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Qty Name (Rating) (Extra).
        /// </summary>
        public async Task<string> DisplayNameAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            string strReturn = await DisplayNameShortAsync(strLanguage, token).ConfigureAwait(false);
            string strSpace = await LanguageManager.GetStringAsync("String_Space", strLanguage, token: token).ConfigureAwait(false);
            if (Rating > 0)
                strReturn += strSpace + '(' + await LanguageManager.GetStringAsync(RatingLabel, strLanguage, token: token).ConfigureAwait(false) + strSpace + Rating.ToString(objCulture) + ')';
            if (!string.IsNullOrEmpty(Extra))
                strReturn += strSpace + '(' + await _objCharacter.TranslateExtraAsync(Extra, strLanguage, token: token).ConfigureAwait(false) + ')';
            return strReturn;
        }

        public string CurrentDisplayName => DisplayName(GlobalSettings.CultureInfo, GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) => DisplayNameAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        public string CurrentDisplayNameShort => DisplayNameShort(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameShortAsync(CancellationToken token = default) => DisplayNameShortAsync(GlobalSettings.Language, token);

        /// <summary>
        /// Translated Category.
        /// </summary>
        public string DisplayCategory(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Category;

            return _objCharacter.LoadDataXPath("armor.xml", strLanguage)
                                .SelectSingleNodeAndCacheExpression(
                                    "/chummer/categories/category[. = " + Category.CleanXPath() + "]/@translate")?.Value
                   ?? Category;
        }

        /// <summary>
        /// Translated Category.
        /// </summary>
        public async Task<string> DisplayCategoryAsync(string strLanguage, CancellationToken token = default)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Category;

            return (await (await _objCharacter.LoadDataXPathAsync("armor.xml", strLanguage, token: token)
                                              .ConfigureAwait(false))
                          .SelectSingleNodeAndCacheExpressionAsync(
                              "/chummer/categories/category[. = " + Category.CleanXPath() + "]/@translate",
                              token: token)
                          .ConfigureAwait(false))?.Value ?? Category;
        }

        /// <summary>
        /// Special Armor Mod Category.
        /// </summary>
        public string Category
        {
            get => _strCategory;
            set => _strCategory = value;
        }

        /// <summary>
        /// Mod's Armor value modifier.
        /// </summary>
        public int Armor
        {
            get => _intArmorValue;
            set
            {
                if (Interlocked.Exchange(ref _intArmorValue, value) != value && Equipped && Parent?.Equipped == true)
                {
                    _objCharacter?.OnPropertyChanged(nameof(Character.GetArmorRating));
                    _objCharacter?.RefreshArmorEncumbrance();
                }
            }
        }

        /// <summary>
        /// Whether or not the Armor Mod contributes to Encumbrance.
        /// </summary>
        public bool Encumbrance => _blnEncumbrance;

        /// <summary>
        /// Armor capacity.
        /// </summary>
        public string ArmorCapacity
        {
            get => _strArmorCapacity;
            set => _strArmorCapacity = value;
        }

        /// <summary>
        /// Capacity for gear plugins.
        /// </summary>
        public string GearCapacity
        {
            get => _strGearCapacity;
            set => _strGearCapacity = value;
        }

        /// <summary>
        /// Mod's Maximum Rating.
        /// </summary>
        public int MaximumRating
        {
            get => _intMaxRating;
            set => _intMaxRating = value;
        }

        /// <summary>
        /// Mod's current Rating.
        /// </summary>
        public int Rating
        {
            get => Math.Min(_intRating, MaximumRating);
            set
            {
                value = Math.Min(value, MaximumRating);
                if (Interlocked.Exchange(ref _intRating, value) != value)
                {
                    if (Equipped && Parent.Equipped && _objCharacter != null)
                    {
                        if (Weight.ContainsAny("FixedValues", "Rating") || GearChildren.Any(x => x.Equipped && x.Weight.Contains("Parent Rating")))
                            _objCharacter.OnPropertyChanged(nameof(Character.TotalCarriedWeight));
                    }
                    if (GearChildren.Count > 0)
                    {
                        foreach (Gear objChild in GearChildren)
                        {
                            if (!objChild.MaxRating.Contains("Parent") && !objChild.MinRating.Contains("Parent"))
                                continue;
                            // This will update a child's rating if it would become out of bounds due to its parent's rating changing
                            int intCurrentRating = objChild.Rating;
                            objChild.Rating = intCurrentRating;
                        }
                    }
                }
            }
        }

        public string RatingLabel
        {
            get => _strRatingLabel;
            set => _strRatingLabel = value;
        }

        /// <summary>
        /// Mod's Availability.
        /// </summary>
        public string Avail
        {
            get => _strAvail;
            set => _strAvail = value;
        }

        /// <summary>
        /// The Mod's cost.
        /// </summary>
        public string Cost
        {
            get => _strCost;
            set => _strCost = value;
        }

        /// <summary>
        /// The Mod's weight.
        /// </summary>
        public string Weight
        {
            get => _strWeight;
            set => _strWeight = value;
        }

        /// <summary>
        /// Mod's Sourcebook.
        /// </summary>
        public string Source
        {
            get => _strSource;
            set => _strSource = value;
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page
        {
            get => _strPage;
            set => _strPage = value;
        }

        /// <summary>
        /// Sourcebook Page Number using a given language file.
        /// Returns Page if not found or the string is empty.
        /// </summary>
        /// <param name="strLanguage">Language file keyword to use.</param>
        /// <returns></returns>
        public string DisplayPage(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Page;
            string s = this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("altpage")?.Value ?? Page;
            return !string.IsNullOrWhiteSpace(s) ? s : Page;
        }

        /// <summary>
        /// Sourcebook Page Number using a given language file.
        /// Returns Page if not found or the string is empty.
        /// </summary>
        /// <param name="strLanguage">Language file keyword to use.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns></returns>
        public async Task<string> DisplayPageAsync(string strLanguage, CancellationToken token = default)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Page;
            XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
            string s = objNode != null
                ? objNode.SelectSingleNodeAndCacheExpression("altpage", token: token)?.Value ?? Page
                : Page;
            return !string.IsNullOrWhiteSpace(s) ? s : Page;
        }

        /// <summary>
        /// Was the object stolen  via the Stolen Gear quality?
        /// </summary>
        public bool Stolen
        {
            get => _blnStolen;
            set => _blnStolen = value;
        }

        private SourceString _objCachedSourceDetail;

        public SourceString SourceDetail
        {
            get
            {
                if (_objCachedSourceDetail == default)
                    _objCachedSourceDetail = SourceString.GetSourceString(Source,
                        DisplayPage(GlobalSettings.Language), GlobalSettings.Language, GlobalSettings.CultureInfo,
                        _objCharacter);
                return _objCachedSourceDetail;
            }
        }

        /// <summary>
        /// Whether or not an Armor Mod is equipped and should be included in the Armor's totals.
        /// </summary>
        public bool Equipped
        {
            get => _blnEquipped;
            set
            {
                if (_blnEquipped == value)
                    return;
                _blnEquipped = value;
                if (value)
                {
                    if (Parent?.Equipped == true)
                    {
                        ImprovementManager.EnableImprovements(_objCharacter,
                                                              _objCharacter.Improvements.Where(
                                                                  x => x.ImproveSource
                                                                       == Improvement.ImprovementSource.ArmorMod
                                                                       && x.SourceName == InternalId));
                        // Add the Improvements from any Gear in the Armor.
                        foreach (Gear objGear in GearChildren)
                        {
                            if (objGear.Equipped)
                            {
                                objGear.ChangeEquippedStatus(true, true);
                            }
                        }
                    }
                }
                else
                {
                    ImprovementManager.DisableImprovements(_objCharacter,
                                                           _objCharacter.Improvements.Where(
                                                               x => x.ImproveSource == Improvement.ImprovementSource
                                                                   .ArmorMod && x.SourceName == InternalId));
                    // Add the Improvements from any Gear in the Armor.
                    foreach (Gear objGear in GearChildren)
                    {
                        objGear.ChangeEquippedStatus(false, true);
                    }
                }

                if (Parent?.Equipped == true && _objCharacter?.IsLoading == false)
                {
                    _objCharacter.OnMultiplePropertyChanged(nameof(Character.ArmorEncumbrance), nameof(Character.TotalCarriedWeight), nameof(Character.GetArmorRating));
                }
            }
        }

        /// <summary>
        /// Whether or not an Armor Mod's wireless bonus is enabled
        /// </summary>
        public bool WirelessOn
        {
            get => _blnWirelessOn;
            set
            {
                if (_blnWirelessOn == value)
                    return;
                _blnWirelessOn = value;
                RefreshWirelessBonuses();
            }
        }

        /// <summary>
        /// Whether or not this Mod is part of the base Armor configuration.
        /// </summary>
        public bool IncludedInArmor
        {
            get => _blnIncludedInArmor;
            set => _blnIncludedInArmor = value;
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get => _strNotes;
            set => _strNotes = value;
        }

        /// <summary>
        /// Forecolor to use for Notes in treeviews.
        /// </summary>
        public Color NotesColor
        {
            get => _colNotes;
            set => _colNotes = value;
        }

        /// <summary>
        /// Value that was selected during the Improvement Manager dialogue.
        /// </summary>
        public string Extra
        {
            get => _strExtra;
            set => _strExtra = _objCharacter.ReverseTranslateExtra(value);
        }

        /// <summary>
        /// Whether or not the Armor Mod's cost should be discounted by 10% through the Black Market Pipeline Quality.
        /// </summary>
        public bool DiscountCost
        {
            get => _blnDiscountCost;
            set => _blnDiscountCost = value;
        }

        /// <summary>
        /// Used by our sorting algorithm to remember which order the user moves things to
        /// </summary>
        public int SortOrder
        {
            get => _intSortOrder;
            set => _intSortOrder = value;
        }

        /// <summary>
        /// Parent Armor.
        /// </summary>
        public Armor Parent { get; set; }

        /// <summary>
        /// The Gear currently applied to the Armor.
        /// </summary>
        public TaggedObservableCollection<Gear> GearChildren
        {
            get
            {
                using (_objCharacter.LockObject.EnterReadLock())
                    return _lstGear;
            }
        }

        #endregion Properties

        #region Complex Properties

        /// <summary>
        /// Total Availability in the program's current language.
        /// </summary>
        public string DisplayTotalAvail => TotalAvail(GlobalSettings.CultureInfo, GlobalSettings.Language);

        /// <summary>
        /// Total Availability in the program's current language.
        /// </summary>
        public Task<string> GetDisplayTotalAvailAsync(CancellationToken token = default) => TotalAvailAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        /// <summary>
        /// Total Availability.
        /// </summary>
        public string TotalAvail(CultureInfo objCulture, string strLanguage)
        {
            return TotalAvailTuple().ToString(objCulture, strLanguage);
        }

        /// <summary>
        /// Calculated Availability of the Vehicle.
        /// </summary>
        public async Task<string> TotalAvailAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            return await (await TotalAvailTupleAsync(token: token).ConfigureAwait(false)).ToStringAsync(objCulture, strLanguage, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Total Availability as a triple.
        /// </summary>
        public AvailabilityValue TotalAvailTuple(bool blnCheckChildren = true)
        {
            bool blnModifyParentAvail = false;
            string strAvail = Avail;
            char chrLastAvailChar = ' ';
            int intAvail = 0;
            if (strAvail.Length > 0)
            {
                if (strAvail.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strAvail.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strAvail = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                chrLastAvailChar = strAvail[strAvail.Length - 1];
                if (chrLastAvailChar == 'F' || chrLastAvailChar == 'R')
                {
                    strAvail = strAvail.Substring(0, strAvail.Length - 1);
                }

                blnModifyParentAvail = strAvail.StartsWith('+', '-') && !IncludedInArmor;

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdAvail))
                {
                    sbdAvail.Append(strAvail.TrimStart('+'));
                    sbdAvail.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));

                    foreach (CharacterAttrib objLoopAttribute in _objCharacter.GetAllAttributes())
                    {
                        sbdAvail.CheapReplace(strAvail, objLoopAttribute.Abbrev,
                                              () => objLoopAttribute.TotalValue.ToString(
                                                  GlobalSettings.InvariantCultureInfo));
                        sbdAvail.CheapReplace(strAvail, objLoopAttribute.Abbrev + "Base",
                                              () => objLoopAttribute.TotalBase.ToString(
                                                  GlobalSettings.InvariantCultureInfo));
                    }

                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdAvail.ToString());
                    if (blnIsSuccess)
                        intAvail = ((double)objProcess).StandardRound();
                }
            }

            if (blnCheckChildren)
            {
                // Run through gear children and increase the Avail by any Mod whose Avail starts with "+" or "-".
                foreach (Gear objChild in GearChildren)
                {
                    if (objChild.ParentID != InternalId)
                    {
                        AvailabilityValue objLoopAvailTuple = objChild.TotalAvailTuple();
                        if (objLoopAvailTuple.AddToParent)
                            intAvail += objLoopAvailTuple.Value;
                        if (objLoopAvailTuple.Suffix == 'F')
                            chrLastAvailChar = 'F';
                        else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                            chrLastAvailChar = 'R';
                    }
                }
            }

            // Avail cannot go below 0. This typically happens when an item with Avail 0 is given the Second Hand category.
            if (intAvail < 0)
                intAvail = 0;

            return new AvailabilityValue(intAvail, chrLastAvailChar, blnModifyParentAvail, IncludedInArmor);
        }

        /// <summary>
        /// Total Availability as a triple.
        /// </summary>
        public async Task<AvailabilityValue> TotalAvailTupleAsync(bool blnCheckChildren = true, CancellationToken token = default)
        {
            bool blnModifyParentAvail = false;
            string strAvail = Avail;
            char chrLastAvailChar = ' ';
            int intAvail = 0;
            if (strAvail.Length > 0)
            {
                if (strAvail.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strAvail.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strAvail = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                chrLastAvailChar = strAvail[strAvail.Length - 1];
                if (chrLastAvailChar == 'F' || chrLastAvailChar == 'R')
                {
                    strAvail = strAvail.Substring(0, strAvail.Length - 1);
                }

                blnModifyParentAvail = strAvail.StartsWith('+', '-') && !IncludedInArmor;

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdAvail))
                {
                    sbdAvail.Append(strAvail.TrimStart('+'));
                    sbdAvail.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));

                    AttributeSection objAttributeSection = await _objCharacter.GetAttributeSectionAsync(token).ConfigureAwait(false);
                    await (await objAttributeSection.GetAttributeListAsync(token).ConfigureAwait(false)).ForEachAsync(async objLoopAttribute =>
                    {
                        await sbdAvail.CheapReplaceAsync(strAvail, objLoopAttribute.Abbrev,
                                                         async () => (await objLoopAttribute.GetTotalValueAsync(token)
                                                             .ConfigureAwait(false)).ToString(
                                                             GlobalSettings.InvariantCultureInfo), token: token)
                                      .ConfigureAwait(false);
                        await sbdAvail.CheapReplaceAsync(strAvail, objLoopAttribute.Abbrev + "Base",
                                                         async () => (await objLoopAttribute.GetTotalBaseAsync(token)
                                                             .ConfigureAwait(false)).ToString(
                                                             GlobalSettings.InvariantCultureInfo), token: token)
                                      .ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                    await (await objAttributeSection.GetSpecialAttributeListAsync(token).ConfigureAwait(false)).ForEachAsync(async objLoopAttribute =>
                    {
                        await sbdAvail.CheapReplaceAsync(strAvail, objLoopAttribute.Abbrev,
                                                         async () => (await objLoopAttribute.GetTotalValueAsync(token)
                                                             .ConfigureAwait(false)).ToString(
                                                             GlobalSettings.InvariantCultureInfo), token: token)
                                      .ConfigureAwait(false);
                        await sbdAvail.CheapReplaceAsync(strAvail, objLoopAttribute.Abbrev + "Base",
                                                         async () => (await objLoopAttribute.GetTotalBaseAsync(token)
                                                             .ConfigureAwait(false)).ToString(
                                                             GlobalSettings.InvariantCultureInfo), token: token)
                                      .ConfigureAwait(false);
                    }, token).ConfigureAwait(false);

                    (bool blnIsSuccess, object objProcess)
                        = await CommonFunctions.EvaluateInvariantXPathAsync(sbdAvail.ToString(), token).ConfigureAwait(false);
                    if (blnIsSuccess)
                        intAvail = ((double)objProcess).StandardRound();
                }
            }

            if (blnCheckChildren)
            {
                // Run through gear children and increase the Avail by any Mod whose Avail starts with "+" or "-".
                intAvail += await GearChildren.SumAsync(async objChild =>
                {
                    if (objChild.ParentID == InternalId)
                        return 0;
                    AvailabilityValue objLoopAvailTuple = await objChild.TotalAvailTupleAsync(token: token).ConfigureAwait(false);
                    if (objLoopAvailTuple.Suffix == 'F')
                        chrLastAvailChar = 'F';
                    else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                        chrLastAvailChar = 'R';
                    return objLoopAvailTuple.AddToParent ? objLoopAvailTuple.Value : 0;
                }, token).ConfigureAwait(false);
            }

            // Avail cannot go below 0. This typically happens when an item with Avail 0 is given the Second Hand category.
            if (intAvail < 0)
                intAvail = 0;

            return new AvailabilityValue(intAvail, chrLastAvailChar, blnModifyParentAvail, IncludedInArmor);
        }

        /// <summary>
        /// Calculated Gear Capacity of the Armor Mod.
        /// </summary>
        public string CalculatedGearCapacity
        {
            get
            {
                string strCapacity = GearCapacity;
                if (string.IsNullOrEmpty(strCapacity))
                    return "0";
                if (strCapacity.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strCapacity.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strCapacity = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                strCapacity = strCapacity
                              .CheapReplace(
                                  "Capacity",
                                  () => Parent != null
                                      ? Convert.ToDecimal(
                                                   Parent.TotalArmorCapacity(GlobalSettings.InvariantCultureInfo),
                                                   GlobalSettings.InvariantCultureInfo)
                                               .ToString(GlobalSettings.InvariantCultureInfo)
                                      : "0")
                              .Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));

                //Rounding is always 'up'. For items that generate capacity, this means making it a larger negative number.
                (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strCapacity);
                string strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strCapacity;

                return strReturn;
            }
        }

        /// <summary>
        /// The amount of Capacity remaining in the Gear.
        /// </summary>
        public decimal GearCapacityRemaining
        {
            get
            {
                decimal decCapacity;
                string strMyCapacity = CalculatedGearCapacity;
                // Get the Gear base Capacity.
                int intPos = strMyCapacity.IndexOf("/[", StringComparison.Ordinal);
                if (intPos != -1)
                {
                    // If this is a multiple-capacity item, use only the first half.
                    strMyCapacity = strMyCapacity.Substring(0, intPos);
                    decCapacity = Convert.ToDecimal(strMyCapacity, GlobalSettings.CultureInfo);
                }
                else
                    decCapacity = Convert.ToDecimal(strMyCapacity, GlobalSettings.CultureInfo);

                // Run through its Children and deduct the Capacity costs.
                foreach (Gear objChildGear in GearChildren)
                {
                    string strCapacity = objChildGear.CalculatedArmorCapacity;
                    intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                    if (intPos != -1)
                    {
                        // If this is a multiple-capacity item, use only the second half.
                        strCapacity = strCapacity.Substring(intPos + 1);
                    }

                    // Only items that contain square brackets should consume Capacity. Everything else is treated as [0].
                    strCapacity = strCapacity.StartsWith('[') ? strCapacity.Substring(1, strCapacity.Length - 2) : "0";
                    decCapacity -= Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo) * objChildGear.Quantity;
                }

                return decCapacity;
            }
        }

        /// <summary>
        /// Caculated Capacity of the Armor Mod.
        /// </summary>
        public string CalculatedCapacity
        {
            get
            {
                string strCapacity = ArmorCapacity;
                if (string.IsNullOrEmpty(strCapacity))
                    return 0.0m.ToString("#,0.##", GlobalSettings.CultureInfo);
                if (strCapacity.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strCapacity.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strCapacity = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                strCapacity = strCapacity
                              .CheapReplace(
                                  "Capacity",
                                  () => Parent != null
                                      ? Convert.ToDecimal(
                                                   Parent.TotalArmorCapacity(GlobalSettings.InvariantCultureInfo),
                                                   GlobalSettings.InvariantCultureInfo)
                                               .ToString(GlobalSettings.InvariantCultureInfo)
                                      : "0")
                              .Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));
                bool blnSquareBrackets = strCapacity.StartsWith('[');
                if (blnSquareBrackets)
                    strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);

                //Rounding is always 'up'. For items that generate capacity, this means making it a larger negative number.
                (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strCapacity);
                string strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strCapacity;
                if (blnSquareBrackets)
                    strReturn = '[' + strReturn + ']';

                return strReturn;
            }
        }

        public decimal TotalCapacity
        {
            get
            {
                string strCapacity = CalculatedCapacity;
                int intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                if (intPos != -1)
                {
                    // If this is a multiple-capacity item, use only the second half.
                    strCapacity = strCapacity.Substring(intPos + 1);
                }

                if (strCapacity.StartsWith('['))
                    strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                if (strCapacity == "*")
                    strCapacity = "0";
                return Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo);
            }
        }

        /// <summary>
        /// Total cost of the Armor Mod.
        /// </summary>
        public decimal TotalCost => OwnCost + GearChildren.Sum(x => x.TotalCost);

        /// <summary>
        /// Total cost of the Armor Mod.
        /// </summary>
        public async Task<decimal> GetTotalCostAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await GetOwnCostAsync(token).ConfigureAwait(false)
                   + await GearChildren.SumAsync(x => x.GetTotalCostAsync(token), token).ConfigureAwait(false);
        }

        public decimal StolenTotalCost => CalculatedStolenTotalCost(true);

        public decimal NonStolenTotalCost => CalculatedStolenTotalCost(false);

        public decimal CalculatedStolenTotalCost(bool blnStolen)
        {
            decimal decReturn = 0;
            if (Stolen == blnStolen)
                decReturn += OwnCost;

            // Go through all of the Gear for this piece of Armor and add the Cost value.
            decReturn += GearChildren.Sum(objGear => objGear.CalculatedStolenTotalCost(blnStolen));

            return decReturn;
        }

        public Task<decimal> GetStolenTotalCostAsync(CancellationToken token = default) => CalculatedStolenTotalCostAsync(true, token);

        public Task<decimal> GetNonStolenTotalCostAsync(CancellationToken token = default) => CalculatedStolenTotalCostAsync(false, token);

        public async Task<decimal> CalculatedStolenTotalCostAsync(bool blnStolen, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            decimal decReturn = 0;
            if (Stolen == blnStolen)
                decReturn += await GetOwnCostAsync(token).ConfigureAwait(false);

            // Go through all of the Gear for this piece of Armor and add the Cost value.
            decReturn += await GearChildren.SumAsync(objGear => objGear.CalculatedStolenTotalCostAsync(blnStolen, token), token).ConfigureAwait(false);

            return decReturn;
        }

        /// <summary>
        /// Cost for just the Armor Mod.
        /// </summary>
        public decimal OwnCost
        {
            get
            {
                decimal decReturn = 0;
                string strCostExpr = Cost;
                if (strCostExpr.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strCostExpr.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strCostExpr = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdCost))
                {
                    sbdCost.Append(strCostExpr.TrimStart('+'));
                    sbdCost.CheapReplace(strCostExpr, "Rating",
                                         () => Rating.ToString(GlobalSettings.InvariantCultureInfo));
                    sbdCost.CheapReplace(strCostExpr, "Armor Cost",
                                         () => (Parent?.OwnCost ?? 0.0m).ToString(GlobalSettings.InvariantCultureInfo));

                    foreach (CharacterAttrib objLoopAttribute in _objCharacter.GetAllAttributes())
                    {
                        sbdCost.CheapReplace(strCostExpr, objLoopAttribute.Abbrev,
                                             () => objLoopAttribute.TotalValue.ToString(
                                                 GlobalSettings.InvariantCultureInfo));
                        sbdCost.CheapReplace(strCostExpr, objLoopAttribute.Abbrev + "Base",
                                             () => objLoopAttribute.TotalBase.ToString(
                                                 GlobalSettings.InvariantCultureInfo));
                    }

                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdCost.ToString());
                    if (blnIsSuccess)
                        decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                }

                if (DiscountCost)
                    decReturn *= 0.9m;

                return decReturn;
            }
        }

        /// <summary>
        /// Cost for just the Armor Mod.
        /// </summary>
        public async Task<decimal> GetOwnCostAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            decimal decReturn = 0;
            string strCostExpr = Cost;
            if (strCostExpr.StartsWith("FixedValues(", StringComparison.Ordinal))
            {
                string[] strValues = strCostExpr.TrimStartOnce("FixedValues(", true).TrimEndOnce(')')
                                                .Split(',', StringSplitOptions.RemoveEmptyEntries);
                strCostExpr = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
            }

            using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdCost))
            {
                sbdCost.Append(strCostExpr.TrimStart('+'));
                await sbdCost.CheapReplaceAsync(strCostExpr, "Rating",
                                                () => Rating.ToString(GlobalSettings.InvariantCultureInfo), token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Armor Cost",
                                                async () => (Parent != null
                                                    ? await Parent.GetOwnCostAsync(token).ConfigureAwait(false)
                                                    : 0.0m).ToString(GlobalSettings.InvariantCultureInfo), token: token)
                             .ConfigureAwait(false);

                AttributeSection objAttributeSection
                    = await _objCharacter.GetAttributeSectionAsync(token).ConfigureAwait(false);
                await (await objAttributeSection.GetAttributeListAsync(token).ConfigureAwait(false)).ForEachAsync(
                    async objLoopAttribute =>
                    {
                        await sbdCost.CheapReplaceAsync(strCostExpr, objLoopAttribute.Abbrev,
                                                        async () => (await objLoopAttribute.GetTotalValueAsync(token)
                                                            .ConfigureAwait(false)).ToString(
                                                            GlobalSettings.InvariantCultureInfo), token: token)
                                     .ConfigureAwait(false);
                        await sbdCost.CheapReplaceAsync(strCostExpr, objLoopAttribute.Abbrev + "Base",
                                                        async () => (await objLoopAttribute.GetTotalBaseAsync(token)
                                                            .ConfigureAwait(false)).ToString(
                                                            GlobalSettings.InvariantCultureInfo), token: token)
                                     .ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                await (await objAttributeSection.GetSpecialAttributeListAsync(token).ConfigureAwait(false))
                      .ForEachAsync(async objLoopAttribute =>
                      {
                          await sbdCost.CheapReplaceAsync(strCostExpr, objLoopAttribute.Abbrev,
                                                          async () => (await objLoopAttribute.GetTotalValueAsync(token)
                                                              .ConfigureAwait(false)).ToString(
                                                              GlobalSettings.InvariantCultureInfo), token: token)
                                       .ConfigureAwait(false);
                          await sbdCost.CheapReplaceAsync(strCostExpr, objLoopAttribute.Abbrev + "Base",
                                                          async () => (await objLoopAttribute.GetTotalBaseAsync(token)
                                                              .ConfigureAwait(false)).ToString(
                                                              GlobalSettings.InvariantCultureInfo), token: token)
                                       .ConfigureAwait(false);
                      }, token).ConfigureAwait(false);

                (bool blnIsSuccess, object objProcess)
                    = await CommonFunctions.EvaluateInvariantXPathAsync(sbdCost.ToString(), token)
                                           .ConfigureAwait(false);
                if (blnIsSuccess)
                    decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
            }

            if (DiscountCost)
                decReturn *= 0.9m;

            return decReturn;
        }

        /// <summary>
        /// Total weight of the Armor Mod.
        /// </summary>
        public decimal TotalWeight => OwnWeight + GearChildren.Sum(x => x.Equipped, x => x.TotalWeight);

        /// <summary>
        /// Weight for just the Armor Mod.
        /// </summary>
        public decimal OwnWeight
        {
            get
            {
                if (IncludedInArmor)
                    return 0;
                string strWeightExpression = Weight;
                if (string.IsNullOrEmpty(strWeightExpression))
                    return 0;
                decimal decReturn = 0;
                if (strWeightExpression.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strWeightExpression.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strWeightExpression = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdWeight))
                {
                    sbdWeight.Append(strWeightExpression.TrimStart('+'));
                    sbdWeight.CheapReplace(strWeightExpression, "Rating",
                                           () => Rating.ToString(GlobalSettings.InvariantCultureInfo));
                    sbdWeight.CheapReplace(strWeightExpression, "Armor Weight",
                                           () => (Parent?.OwnWeight ?? 0.0m).ToString(GlobalSettings.InvariantCultureInfo));

                    foreach (CharacterAttrib objLoopAttribute in _objCharacter.GetAllAttributes())
                    {
                        sbdWeight.CheapReplace(strWeightExpression, objLoopAttribute.Abbrev,
                                               () => objLoopAttribute.TotalValue.ToString(
                                                   GlobalSettings.InvariantCultureInfo));
                        sbdWeight.CheapReplace(strWeightExpression, objLoopAttribute.Abbrev + "Base",
                                               () => objLoopAttribute.TotalBase.ToString(
                                                   GlobalSettings.InvariantCultureInfo));
                    }

                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdWeight.ToString());
                    if (blnIsSuccess)
                        decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                }

                return decReturn;
            }
        }

        private XmlNode _objCachedMyXmlNode;
        private string _strCachedXmlNodeLanguage = string.Empty;

        public async Task<XmlNode> GetNodeCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            XmlNode objReturn = _objCachedMyXmlNode;
            if (objReturn != null && strLanguage == _strCachedXmlNodeLanguage
                                  && !GlobalSettings.LiveCustomData)
                return objReturn;
            XmlNode objDoc = blnSync
                // ReSharper disable once MethodHasAsyncOverload
                ? _objCharacter.LoadData("armor.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataAsync("armor.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/mods/mod", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/mods/mod", Name);
                objReturn?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
            }
            _objCachedMyXmlNode = objReturn;
            _strCachedXmlNodeLanguage = strLanguage;
            return objReturn;
        }

        private XPathNavigator _objCachedMyXPathNode;
        private string _strCachedXPathNodeLanguage = string.Empty;

        public async Task<XPathNavigator> GetNodeXPathCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            XPathNavigator objReturn = _objCachedMyXPathNode;
            if (objReturn != null && strLanguage == _strCachedXPathNodeLanguage
                                  && !GlobalSettings.LiveCustomData)
                return objReturn;
            XPathNavigator objDoc = blnSync
                // ReSharper disable once MethodHasAsyncOverload
                ? _objCharacter.LoadDataXPath("armor.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataXPathAsync("armor.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/mods/mod", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/mods/mod", Name);
                objReturn?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
            }
            _objCachedMyXPathNode = objReturn;
            _strCachedXPathNodeLanguage = strLanguage;
            return objReturn;
        }

        #endregion Complex Properties

        #region Methods

        /// <summary>
        /// Method to delete an Armor object. Returns total extra cost removed unrelated to children.
        /// </summary>
        public decimal DeleteArmorMod(bool blnDoRemoval = true)
        {
            if (blnDoRemoval)
                Parent?.ArmorMods.Remove(this);

            decimal decReturn = 0.0m;
            // Remove any Improvements created by the Armor Mod's Gear.
            foreach (Gear objGear in GearChildren)
                decReturn += objGear.DeleteGear(false);

            // Remove the Cyberweapon created by the Mod if applicable.
            if (!WeaponID.IsEmptyGuid())
            {
                foreach (Weapon objDeleteWeapon in _objCharacter.Weapons.DeepWhere(x => x.Children, x => x.ParentID == InternalId).ToList())
                {
                    decReturn += objDeleteWeapon.TotalCost + objDeleteWeapon.DeleteWeapon();
                }

                decReturn += _objCharacter.Vehicles.Sum(objVehicle =>
                {
                    decimal decInnerReturn = 0;
                    foreach (Weapon objDeleteWeapon in objVehicle.Weapons
                                                                 .DeepWhere(x => x.Children,
                                                                            x => x.ParentID == InternalId).ToList())
                    {
                        decInnerReturn += objDeleteWeapon.TotalCost + objDeleteWeapon.DeleteWeapon();
                    }

                    decInnerReturn += objVehicle.Mods.Sum(objMod =>
                    {
                        decimal decInnerReturn2 = 0;
                        foreach (Weapon objDeleteWeapon in objMod.Weapons
                                                                 .DeepWhere(x => x.Children,
                                                                            x => x.ParentID == InternalId).ToList())
                        {
                            decInnerReturn2 += objDeleteWeapon.TotalCost + objDeleteWeapon.DeleteWeapon();
                        }

                        return decInnerReturn2;
                    });

                    decInnerReturn += objVehicle.WeaponMounts.Sum(objMount =>
                    {
                        decimal decInnerReturn2 = 0;
                        foreach (Weapon objDeleteWeapon in objMount.Weapons
                                                                   .DeepWhere(x => x.Children,
                                                                              x => x.ParentID == InternalId).ToList())
                        {
                            decInnerReturn2 += objDeleteWeapon.TotalCost + objDeleteWeapon.DeleteWeapon();
                        }

                        decInnerReturn2 += objMount.Mods.Sum(objMod =>
                        {
                            decimal decInnerReturn3 = 0;
                            foreach (Weapon objDeleteWeapon in objMod.Weapons
                                                                     .DeepWhere(x => x.Children,
                                                                         x => x.ParentID == InternalId).ToList())
                            {
                                decInnerReturn3 += objDeleteWeapon.TotalCost + objDeleteWeapon.DeleteWeapon();
                            }

                            return decInnerReturn3;
                        });

                        return decInnerReturn2;
                    });

                    return decInnerReturn;
                });
            }

            decReturn += ImprovementManager.RemoveImprovements(_objCharacter, Improvement.ImprovementSource.ArmorMod, InternalId);

            DisposeSelf();

            return decReturn;
        }

        /// <summary>
        /// Method to delete an Armor object. Returns total extra cost removed unrelated to children.
        /// </summary>
        public async Task<decimal> DeleteArmorModAsync(bool blnDoRemoval = true, CancellationToken token = default)
        {
            if (blnDoRemoval && Parent != null)
                await Parent.ArmorMods.RemoveAsync(this, token).ConfigureAwait(false);

            // Remove any Improvements created by the Armor Mod's Gear.
            decimal decReturn = await GearChildren.SumAsync(x => x.DeleteGearAsync(false, token), token)
                                                  .ConfigureAwait(false);

            // Remove the Cyberweapon created by the Mod if applicable.
            if (!WeaponID.IsEmptyGuid())
            {
                foreach (Weapon objDeleteWeapon in await _objCharacter.Weapons
                                                                      .DeepWhereAsync(
                                                                          x => x.Children,
                                                                          x => x.ParentID == InternalId, token)
                                                                      .ConfigureAwait(false))
                {
                    decReturn += await objDeleteWeapon.GetTotalCostAsync(token).ConfigureAwait(false)
                                 + await objDeleteWeapon.DeleteWeaponAsync(token: token).ConfigureAwait(false);
                }

                decReturn += await _objCharacter.Vehicles.SumAsync(async objVehicle =>
                {
                    decimal decInner = 0;
                    foreach (Weapon objDeleteWeapon in await objVehicle.Weapons
                                                                       .DeepWhereAsync(
                                                                           x => x.Children,
                                                                           x => x.ParentID == InternalId, token)
                                                                       .ConfigureAwait(false))
                    {
                        decInner += await objDeleteWeapon.GetTotalCostAsync(token).ConfigureAwait(false)
                                    + await objDeleteWeapon.DeleteWeaponAsync(token: token).ConfigureAwait(false);
                    }

                    decInner += await objVehicle.Mods.SumAsync(async objMod =>
                    {
                        decimal decInner2 = 0;
                        foreach (Weapon objDeleteWeapon in await objMod.Weapons
                                                                       .DeepWhereAsync(
                                                                           x => x.Children,
                                                                           x => x.ParentID == InternalId, token)
                                                                       .ConfigureAwait(false))
                        {
                            decInner2 += await objDeleteWeapon.GetTotalCostAsync(token).ConfigureAwait(false)
                                         + await objDeleteWeapon.DeleteWeaponAsync(token: token).ConfigureAwait(false);
                        }

                        return decInner2;
                    }, token).ConfigureAwait(false);

                    decInner += await objVehicle.WeaponMounts.SumAsync(async objMount =>
                    {
                        decimal decInner2 = 0;
                        foreach (Weapon objDeleteWeapon in await objMount.Weapons
                                                                         .DeepWhereAsync(
                                                                             x => x.Children,
                                                                             x => x.ParentID == InternalId, token)
                                                                         .ConfigureAwait(false))
                        {
                            decInner2 += await objDeleteWeapon.GetTotalCostAsync(token).ConfigureAwait(false)
                                         + await objDeleteWeapon.DeleteWeaponAsync(token: token).ConfigureAwait(false);
                        }

                        decInner2 += await objMount.Mods.SumAsync(async objMod =>
                        {
                            decimal decInner3 = 0;
                            foreach (Weapon objDeleteWeapon in await objMod.Weapons
                                                                           .DeepWhereAsync(
                                                                               x => x.Children,
                                                                               x => x.ParentID == InternalId, token)
                                                                           .ConfigureAwait(false))
                            {
                                decInner3 += await objDeleteWeapon.GetTotalCostAsync(token).ConfigureAwait(false)
                                             + await objDeleteWeapon.DeleteWeaponAsync(token: token)
                                                                    .ConfigureAwait(false);
                            }

                            return decInner3;
                        }, token).ConfigureAwait(false);

                        return decInner2;
                    }, token).ConfigureAwait(false);

                    return decInner;
                }, token).ConfigureAwait(false);
            }

            decReturn += await ImprovementManager
                               .RemoveImprovementsAsync(_objCharacter, Improvement.ImprovementSource.ArmorMod,
                                                        InternalId, token).ConfigureAwait(false);

            await DisposeSelfAsync().ConfigureAwait(false);

            return decReturn;
        }

        /// <summary>
        /// Toggle the Wireless Bonus for this armor mod.
        /// </summary>
        public void RefreshWirelessBonuses()
        {
            if (!string.IsNullOrEmpty(WirelessBonus?.InnerText))
            {
                if (WirelessOn && Equipped && Parent.WirelessOn)
                {
                    if (WirelessBonus.SelectSingleNodeAndCacheExpressionAsNavigator("@mode")?.Value == "replace")
                    {
                        ImprovementManager.DisableImprovements(_objCharacter,
                                                               _objCharacter.Improvements.Where(x =>
                                                                   x.ImproveSource == Improvement.ImprovementSource
                                                                       .ArmorMod &&
                                                                   x.SourceName == InternalId));
                    }

                    ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.ArmorMod, InternalId + "Wireless", WirelessBonus, Rating, CurrentDisplayNameShort);

                    if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue) && string.IsNullOrEmpty(_strExtra))
                        _strExtra = ImprovementManager.SelectedValue;
                }
                else
                {
                    if (WirelessBonus.SelectSingleNodeAndCacheExpressionAsNavigator("@mode")?.Value == "replace")
                    {
                        ImprovementManager.EnableImprovements(_objCharacter,
                                                              _objCharacter.Improvements.Where(x =>
                                                                  x.ImproveSource == Improvement.ImprovementSource
                                                                      .ArmorMod &&
                                                                  x.SourceName == InternalId));
                    }

                    string strSourceNameToRemove = InternalId + "Wireless";
                    ImprovementManager.RemoveImprovements(_objCharacter,
                                                          _objCharacter.Improvements.Where(x =>
                                                              x.ImproveSource == Improvement.ImprovementSource
                                                                  .ArmorMod &&
                                                              x.SourceName == strSourceNameToRemove).ToList());
                }
            }

            foreach (Gear objGear in GearChildren)
                objGear.RefreshWirelessBonuses();
        }

        /// <summary>
        /// Toggle the Wireless Bonus for this armor mod.
        /// </summary>
        public async Task RefreshWirelessBonusesAsync(CancellationToken token = default)
        {
            if (!string.IsNullOrEmpty(WirelessBonus?.InnerText))
            {
                if (WirelessOn && Equipped && Parent.WirelessOn)
                {
                    if (WirelessBonus.SelectSingleNodeAndCacheExpressionAsNavigator("@mode", token)?.Value == "replace")
                    {
                        await ImprovementManager.DisableImprovementsAsync(_objCharacter,
                                                                          await _objCharacter.Improvements.ToListAsync(x =>
                                                                              x.ImproveSource == Improvement.ImprovementSource
                                                                                  .ArmorMod &&
                                                                              x.SourceName == InternalId, token: token).ConfigureAwait(false), token).ConfigureAwait(false);
                    }

                    await ImprovementManager.CreateImprovementsAsync(_objCharacter,
                                                                     Improvement.ImprovementSource.ArmorMod,
                                                                     InternalId + "Wireless", WirelessBonus, Rating,
                                                                     await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false),
                                                                     token: token).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue) && string.IsNullOrEmpty(_strExtra))
                        _strExtra = ImprovementManager.SelectedValue;
                }
                else
                {
                    if (WirelessBonus.SelectSingleNodeAndCacheExpressionAsNavigator("@mode", token)?.Value == "replace")
                    {
                        await ImprovementManager.EnableImprovementsAsync(_objCharacter,
                                                                         await _objCharacter.Improvements.ToListAsync(x =>
                                                                             x.ImproveSource == Improvement.ImprovementSource
                                                                                 .ArmorMod &&
                                                                             x.SourceName == InternalId, token: token).ConfigureAwait(false), token).ConfigureAwait(false);
                    }

                    string strSourceNameToRemove = InternalId + "Wireless";
                    await ImprovementManager.RemoveImprovementsAsync(_objCharacter,
                                                                     await _objCharacter.Improvements.ToListAsync(x =>
                                                                             x.ImproveSource == Improvement
                                                                                 .ImprovementSource
                                                                                 .ArmorMod &&
                                                                             x.SourceName == strSourceNameToRemove,
                                                                         token: token).ConfigureAwait(false), token: token).ConfigureAwait(false);
                }
            }

            foreach (Gear objGear in GearChildren)
                await objGear.RefreshWirelessBonusesAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks a nominated piece of gear for Availability requirements.
        /// </summary>
        /// <param name="dicRestrictedGearLimits">Dictionary of Restricted Gear availabilities still available with the amount of items that can still use that availability.</param>
        /// <param name="sbdAvailItems">StringBuilder used to list names of gear that are currently over the availability limit.</param>
        /// <param name="sbdRestrictedItems">StringBuilder used to list names of gear that are being used for Restricted Gear.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<int> CheckRestrictedGear(IDictionary<int, int> dicRestrictedGearLimits, StringBuilder sbdAvailItems, StringBuilder sbdRestrictedItems, CancellationToken token = default)
        {
            int intRestrictedCount = 0;
            if (!IncludedInArmor)
            {
                AvailabilityValue objTotalAvail = await TotalAvailTupleAsync(token: token).ConfigureAwait(false);
                if (!objTotalAvail.AddToParent)
                {
                    int intAvailInt = objTotalAvail.Value;
                    if (intAvailInt > _objCharacter.Settings.MaximumAvailability)
                    {
                        int intLowestValidRestrictedGearAvail = -1;
                        foreach (int intValidAvail in dicRestrictedGearLimits.Keys)
                        {
                            if (intValidAvail >= intAvailInt && (intLowestValidRestrictedGearAvail < 0
                                                                 || intValidAvail < intLowestValidRestrictedGearAvail))
                                intLowestValidRestrictedGearAvail = intValidAvail;
                        }

                        string strNameToUse = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                        if (Parent != null)
                            strNameToUse += await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false) + '(' + await Parent.GetCurrentDisplayNameAsync(token).ConfigureAwait(false) + ')';

                        if (intLowestValidRestrictedGearAvail >= 0
                            && dicRestrictedGearLimits[intLowestValidRestrictedGearAvail] > 0)
                        {
                            --dicRestrictedGearLimits[intLowestValidRestrictedGearAvail];
                            sbdRestrictedItems.AppendLine().Append("\t\t").Append(strNameToUse);
                        }
                        else
                        {
                            dicRestrictedGearLimits.Remove(intLowestValidRestrictedGearAvail);
                            ++intRestrictedCount;
                            sbdAvailItems.AppendLine().Append("\t\t").Append(strNameToUse);
                        }
                    }
                }
            }

            intRestrictedCount += await GearChildren
                                        .SumAsync(
                                            async objChild =>
                                                await objChild
                                                      .CheckRestrictedGear(
                                                          dicRestrictedGearLimits, sbdAvailItems, sbdRestrictedItems,
                                                          token).ConfigureAwait(false), token: token)
                                        .ConfigureAwait(false);

            return intRestrictedCount;
        }

        #endregion Methods

        #region UI Methods

        public TreeNode CreateTreeNode(ContextMenuStrip cmsArmorMod, ContextMenuStrip cmsArmorGear)
        {
            if (IncludedInArmor && !string.IsNullOrEmpty(Source) && !_objCharacter.Settings.BookEnabled(Source))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = CurrentDisplayName,
                Tag = this,
                ContextMenuStrip = string.IsNullOrEmpty(GearCapacity) ? cmsArmorMod : cmsArmorGear,
                ForeColor = PreferredColor,
                ToolTipText = Notes.WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            foreach (Gear objGear in GearChildren)
            {
                TreeNode objLoopNode = objGear.CreateTreeNode(cmsArmorGear, null);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }
            if (lstChildNodes.Count > 0)
                objNode.Expand();

            return objNode;
        }

        public Color PreferredColor
        {
            get
            {
                if (!string.IsNullOrEmpty(Notes))
                {
                    return IncludedInArmor
                        ? ColorManager.GenerateCurrentModeDimmedColor(NotesColor)
                        : ColorManager.GenerateCurrentModeColor(NotesColor);
                }
                return IncludedInArmor
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
            }
        }

        #endregion UI Methods

        public bool Remove(bool blnConfirmDelete = true)
        {
            if (blnConfirmDelete && !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteArmor")))
                return false;

            DeleteArmorMod();
            return true;
        }

        public bool Sell(decimal percentage, bool blnConfirmDelete)
        {
            if (blnConfirmDelete && !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteArmor")))
                return false;

            if (!_objCharacter.Created)
            {
                DeleteArmorMod();
                return true;
            }

            // Record the cost of the Armor with the ArmorMod.
            Armor objParent = Parent;
            decimal decOriginal = Parent?.TotalCost ?? TotalCost;
            decimal decAmount = DeleteArmorMod() * percentage;
            decAmount += (decOriginal - (objParent?.TotalCost ?? 0)) * percentage;
            // Create the Expense Log Entry for the sale.
            ExpenseLogEntry objExpense = new ExpenseLogEntry(_objCharacter);
            objExpense.Create(decAmount, LanguageManager.GetString("String_ExpenseSoldArmorMod") + ' ' + CurrentDisplayNameShort, ExpenseType.Nuyen, DateTime.Now);
            _objCharacter.ExpenseEntries.AddWithSort(objExpense);
            _objCharacter.Nuyen += decAmount;
            return true;
        }

        /// <summary>
        /// Alias map for SourceDetail control text and tooltip assignation.
        /// </summary>
        /// <param name="sourceControl"></param>
        public void SetSourceDetail(Control sourceControl)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            SourceDetail.SetControl(sourceControl);
        }

        public Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            return SourceDetail.SetControlAsync(sourceControl, token);
        }

        public bool AllowPasteXml
        {
            get
            {
                string strGearCapacity = CalculatedGearCapacity;
                if (string.IsNullOrEmpty(strGearCapacity) || strGearCapacity == "0")
                    return false;
                switch (GlobalSettings.ClipboardContentType)
                {
                    case ClipboardContentType.Gear:
                        {
                            XPathNodeIterator xmlAddonCategoryList = this.GetNodeXPath()?.SelectAndCacheExpression("addoncategory");
                            if (!(xmlAddonCategoryList?.Count > 0))
                                return true;
                            string strGearCategory = GlobalSettings.Clipboard["category"]?.InnerText;
                            return xmlAddonCategoryList.Cast<XPathNavigator>()
                                                       .Any(xmlCategory => xmlCategory.Value == strGearCategory);
                        }
                    default:
                        return false;
                }
            }
        }

        bool ICanPaste.AllowPasteObject(object input)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (Gear objChild in _lstGear)
                objChild.Dispose();
            DisposeSelf();
        }

        private void DisposeSelf()
        {
            _lstGear.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            foreach (Gear objChild in _lstGear)
                await objChild.DisposeAsync().ConfigureAwait(false);
            await DisposeSelfAsync().ConfigureAwait(false);
        }

        private ValueTask DisposeSelfAsync()
        {
            return _lstGear.DisposeAsync();
        }
    }
}
