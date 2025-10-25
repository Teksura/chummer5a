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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Chummer.Backend.Attributes;
using Chummer.Backend.Enums;
using Chummer.Backend.Skills;
using Chummer.Backend.Uniques;
using NLog;

namespace Chummer
{
    [DebuggerDisplay("{" + nameof(DisplayDebug) + "()}")]
    public class Improvement : IHasNotes, IHasInternalId, ICanSort, IHasCharacterObject
    {
        private static readonly Lazy<Logger> s_ObjLogger = new Lazy<Logger>(LogManager.GetCurrentClassLogger);
        private static Logger Log => s_ObjLogger.Value;

        private string DisplayDebug()
        {
            return string.Format(GlobalSettings.InvariantCultureInfo, "{0} ({1}, {2}) 🡐 {3}, {4}, {5}",
                                 _eImprovementType, _decVal, _intRating, _eImprovementSource, _strSourceName,
                                 _strImprovedName);
        }

        public enum ImprovementType
        {
            None,
            Attribute,
            Text,
            Armor,
            FireArmor,
            ColdArmor,
            ElectricityArmor,
            AcidArmor,
            FallingArmor,
            Dodge,
            Reach,
            Nuyen,
            NuyenExpense,
            PhysicalCM,
            StunCM,
            UnarmedDV,
            InitiativeDice,
            MatrixInitiative,
            MatrixInitiativeDice,
            LifestyleCost,
            CMThreshold,
            EnhancedArticulation,
            WeaponCategoryDV,
            WeaponCategoryDice,
            WeaponCategoryAP,
            WeaponCategoryAccuracy,
            WeaponCategoryReach,
            WeaponSpecificDice,
            WeaponSpecificDV,
            WeaponSpecificAP,
            WeaponSpecificAccuracy,
            WeaponSpecificRange,
            CyberwareEssCost,
            CyberwareTotalEssMultiplier,
            CyberwareEssCostNonRetroactive,
            CyberwareTotalEssMultiplierNonRetroactive,
            SpecialTab,
            Initiative,
            LivingPersonaDeviceRating,
            LivingPersonaProgramLimit,
            LivingPersonaAttack,
            LivingPersonaSleaze,
            LivingPersonaDataProcessing,
            LivingPersonaFirewall,
            LivingPersonaMatrixCM,
            Smartlink,
            BiowareEssCost,
            BiowareTotalEssMultiplier,
            BiowareEssCostNonRetroactive,
            BiowareTotalEssMultiplierNonRetroactive,
            GenetechCostMultiplier,
            BasicBiowareEssCost,
            SoftWeave,
            DisableBioware,
            DisableCyberware,
            DisableBiowareGrade,
            DisableCyberwareGrade,
            ConditionMonitor,
            UnarmedDVPhysical,
            Adapsin,
            FreePositiveQualities,
            FreeNegativeQualities,
            FreeKnowledgeSkills,
            NuyenMaxBP,
            CMOverflow,
            FreeSpiritPowerPoints,
            AdeptPowerPoints,
            ArmorEncumbrancePenalty,
            Art,
            Metamagic,
            Echo,
            Skillwire,
            DamageResistance,
            JudgeIntentions,
            JudgeIntentionsOffense,
            JudgeIntentionsDefense,
            LiftAndCarry,
            Memory,
            Concealability,
            SwapSkillAttribute,
            DrainResistance,
            FadingResistance,
            MatrixInitiativeDiceAdd,
            InitiativeDiceAdd,
            Composure,
            UnarmedAP,
            CMThresholdOffset,
            CMSharedThresholdOffset,
            Restricted,
            Notoriety,
            SpellCategory,
            SpellCategoryDamage,
            SpellCategoryDrain,
            SpellDescriptorDamage,
            SpellDescriptorDrain,
            SpellDicePool,
            ThrowRange,
            ThrowRangeSTR,
            SkillsoftAccess,
            AddSprite,
            BlackMarketDiscount,
            ComplexFormLimit,
            SpellLimit,
            QuickeningMetamagic,
            BasicLifestyleCost,
            ThrowSTR,
            IgnoreCMPenaltyStun,
            IgnoreCMPenaltyPhysical,
            CyborgEssence,
            EssenceMax,
            SpecificQuality,
            MartialArt,
            LimitModifier,
            PhysicalLimit,
            MentalLimit,
            SocialLimit,
            FriendsInHighPlaces,
            Erased,
            Fame,
            MadeMan,
            Overclocker,
            RestrictedGear,
            TrustFund,
            ExCon,
            ContactForceGroup,
            Attributelevel,
            AddContact,
            Seeker,
            PublicAwareness,
            PrototypeTranshuman,
            Hardwire,
            DealerConnection,
            Skill, //Improve pool of skill based on name
            SkillGroup, //Group
            SkillCategory, //category
            SkillAttribute, //attribute
            SkillLinkedAttribute, //linked attribute
            SkillLevel, //Karma points in skill
            SkillGroupLevel, //group
            SkillBase, //base points in skill
            SkillGroupBase, //group
            Skillsoft, // A knowledge or language skill gained from a knowsoft
            Activesoft, // An active skill gained from an activesoft
            ReplaceAttribute, //Alter the base metatype or metavariant of a character. Used for infected.
            SpecialSkills,
            ReflexRecorderOptimization,
            RemoveSkillCategoryDefaultPenalty,
            RemoveSkillGroupDefaultPenalty,
            RemoveSkillDefaultPenalty,
            BlockSkillCategoryDefault,
            BlockSkillGroupDefault,
            BlockSkillDefault,
            AllowSkillDefault,
            Ambidextrous,
            UnarmedReach,
            SkillSpecialization,
            SkillExpertise, // SASS' Inspired, adds a specialization that gives a +3 bonus instead of the usual +2
            SkillSpecializationOption,
            NativeLanguageLimit,
            AdeptPowerFreeLevels,
            AdeptPowerFreePoints,
            AIProgram,
            CritterPowerLevel,
            CritterPower,
            SwapSkillSpecAttribute,
            SpellResistance,
            AllowSpellCategory,
            LimitSpellCategory,
            AllowSpellRange,
            LimitSpellRange,
            BlockSpellDescriptor,
            LimitSpellDescriptor,
            LimitSpiritCategory,
            WalkSpeed,
            RunSpeed,
            SprintSpeed,
            WalkMultiplier,
            RunMultiplier,
            SprintBonus,
            WalkMultiplierPercent,
            RunMultiplierPercent,
            SprintBonusPercent,
            EssencePenalty,
            EssencePenaltyT100,
            EssencePenaltyMAGOnlyT100,
            EssencePenaltyRESOnlyT100,
            EssencePenaltyDEPOnlyT100,
            SpecialAttBurn,
            SpecialAttTotalBurnMultiplier,
            FreeSpellsATT,
            FreeSpells,
            DrainValue,
            FadingValue,
            Spell,
            ComplexForm,
            Gear,
            Weapon,
            MentorSpirit,
            Paragon,
            FreeSpellsSkill,
            DisableSpecializationEffects, // Disable the effects of specializations for a skill
            FatigueResist,
            RadiationResist,
            SonicResist,
            ToxinContactResist,
            QualityLevel,
            ToxinIngestionResist,
            ToxinInhalationResist,
            ToxinInjectionResist,
            PathogenContactResist,
            PathogenIngestionResist,
            PathogenInhalationResist,
            PathogenInjectionResist,
            ToxinContactImmune,
            ToxinIngestionImmune,
            ToxinInhalationImmune,
            ToxinInjectionImmune,
            PathogenContactImmune,
            PathogenIngestionImmune,
            PathogenInhalationImmune,
            PathogenInjectionImmune,
            PhysiologicalAddictionFirstTime,
            PsychologicalAddictionFirstTime,
            PhysiologicalAddictionAlreadyAddicted,
            PsychologicalAddictionAlreadyAddicted,
            StunCMRecovery,
            PhysicalCMRecovery,
            AddESStoStunCMRecovery,
            AddESStoPhysicalCMRecovery,
            MentalManipulationResist,
            PhysicalManipulationResist,
            ManaIllusionResist,
            PhysicalIllusionResist,
            DetectionSpellResist,
            DirectManaSpellResist,
            DirectPhysicalSpellResist,
            DecreaseBODResist,
            DecreaseAGIResist,
            DecreaseREAResist,
            DecreaseSTRResist,
            DecreaseCHAResist,
            DecreaseINTResist,
            DecreaseLOGResist,
            DecreaseWILResist,
            AddLimb,
            StreetCredMultiplier,
            StreetCred,
            AttributeKarmaCostMultiplier,
            AttributeKarmaCost,
            ActiveSkillKarmaCostMultiplier,
            SkillGroupKarmaCostMultiplier,
            KnowledgeSkillKarmaCostMultiplier,
            ActiveSkillKarmaCost,
            SkillGroupKarmaCost,
            SkillGroupDisable,
            SkillDisable,
            KnowledgeSkillKarmaCost,
            KnowledgeSkillKarmaCostMinimum,
            SkillCategorySpecializationKarmaCostMultiplier,
            SkillCategorySpecializationKarmaCost,
            SkillCategoryKarmaCostMultiplier,
            SkillCategoryKarmaCost,
            SkillGroupCategoryKarmaCostMultiplier,
            SkillGroupCategoryDisable,
            SkillGroupCategoryKarmaCost,
            AttributePointCostMultiplier,
            AttributePointCost,
            ActiveSkillPointCostMultiplier,
            SkillGroupPointCostMultiplier,
            KnowledgeSkillPointCostMultiplier,
            ActiveSkillPointCost,
            SkillGroupPointCost,
            KnowledgeSkillPointCost,
            SkillCategoryPointCostMultiplier,
            SkillCategoryPointCost,
            SkillGroupCategoryPointCostMultiplier,
            SkillGroupCategoryPointCost,
            NewSpellKarmaCostMultiplier,
            NewSpellKarmaCost,
            NewComplexFormKarmaCostMultiplier,
            NewComplexFormKarmaCost,
            NewAIProgramKarmaCostMultiplier,
            NewAIProgramKarmaCost,
            NewAIAdvancedProgramKarmaCostMultiplier,
            NewAIAdvancedProgramKarmaCost,
            BlockSkillSpecializations,
            BlockSkillCategorySpecializations,
            FocusBindingKarmaCost,
            FocusBindingKarmaMultiplier,
            MagiciansWayDiscount,
            BurnoutsWay,
            ContactForcedLoyalty,
            ContactMakeFree,
            FreeWare,
            WeaponAccuracy,
            WeaponSkillAccuracy,
            MetageneticLimit,
            Tradition,
            ActionDicePool,
            SpecialModificationLimit,
            AddSpirit,
            ContactKarmaDiscount,
            ContactKarmaMinimum,
            GenetechEssMultiplier,
            AllowSpriteFettering,
            DisableDrugGrade,
            DrugDuration,
            DrugDurationMultiplier,
            Surprise,
            EnableCyberzombie,
            AllowCritterPowerCategory,
            LimitCritterPowerCategory,
            AttributeMaxClamp,
            MetamagicLimit,
            DisableQuality,
            FreeQuality,
            AstralReputation,
            AstralReputationWild,
            CyberadeptDaemon,
            PenaltyFreeSustain,
            WeaponRangeModifier,
            ReplaceSkillSpell,
            Availability,
            SkillEnableMovement, // Enables skills that require fly/swim movement even without that movement type
            CyberlimbAttributeBonus, // Cyberlimb attribute bonus for strength or agility (similar to redliner but without cyberlimb dependency)
            NumImprovementTypes // 🡐 This one should always be the last defined enum
        }

        public enum ImprovementSource
        {
            Quality,
            Power,
            Metatype,
            Cyberware,
            Metavariant,
            Bioware,
            ArmorEncumbrance,
            Gear,
            VehicleMod,
            Spell,
            Initiation,
            Submersion,
            Metamagic,
            Echo,
            Armor,
            ArmorMod,
            EssenceLoss,
            EssenceLossChargen,
            CritterPower,
            ComplexForm,
            MutantCritter,
            Cyberzombie,
            StackedFocus,
            AttributeLoss,
            Art,
            Enhancement,
            Custom,
            Heritage,
            MartialArt,
            MartialArtTechnique,
            AIProgram,
            SpiritFettering,
            MentorSpirit,
            Drug,
            Tradition,
            Weapon,
            WeaponAccessory,
            AstralReputation,
            CyberadeptDaemon,
            BurnedEdge,
            Encumbrance,
            NumImprovementSources // 🡐 This one should always be the last defined enum
        }

        private readonly Character _objCharacter;
        private string _strImprovedName = string.Empty;
        private string _strSourceName = string.Empty;
        private int _intMin;
        private int _intMax;
        private decimal _decAug;
        private int _intAugMax;
        private decimal _decVal;
        private int _intRating = 1;
        private string _strExclude = string.Empty;
        private string _strCondition = string.Empty;
        private string _strUniqueName = string.Empty;
        private string _strTarget = string.Empty;
        private ImprovementType _eImprovementType;
        private ImprovementSource _eImprovementSource;
        private bool _blnCustom;
        private string _strCustomName = string.Empty;
        private string _strCustomId = string.Empty;
        private string _strCustomGroup = string.Empty;
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;
        private int _intAddToRating;
        private int _intEnabled = 1;

        // Start with Improvement disabled, then enable it after all properties are set up at creation
        private bool _blnSetupComplete;

        private int _intOrder;

        #region Helper Methods

        /// <summary>
        /// Convert a string to an ImprovementType.
        /// </summary>
        /// <param name="strValue">String value to convert.</param>
        public static ImprovementType ConvertToImprovementType(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return ImprovementType.None;
            if (strValue.Contains("InitiativePass"))
            {
                strValue = strValue.Replace("InitiativePass", "InitiativeDice");
            }

            if (strValue == "ContactForceLoyalty")
                strValue = "ContactForcedLoyalty";
            return (ImprovementType)Enum.Parse(typeof(ImprovementType), strValue);
        }

        /// <summary>
        /// Convert a string to an ImprovementSource.
        /// </summary>
        /// <param name="strValue">String value to convert.</param>
        public static ImprovementSource ConvertToImprovementSource(string strValue)
        {
            if (strValue == "MartialArtAdvantage")
                strValue = "MartialArtTechnique";
            return (ImprovementSource)Enum.Parse(typeof(ImprovementSource), strValue);
        }

        #endregion Helper Methods

        #region Save and Load Methods

        public Improvement(Character objCharacter)
        {
            _objCharacter = objCharacter;
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;
            objWriter.WriteStartElement("improvement");
            if (!string.IsNullOrEmpty(_strUniqueName))
                objWriter.WriteElementString("unique", _strUniqueName);
            objWriter.WriteElementString("target", _strTarget);
            objWriter.WriteElementString("improvedname", _strImprovedName);
            objWriter.WriteElementString("sourcename", _strSourceName);
            objWriter.WriteElementString("min", _intMin.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("max", _intMax.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("aug", _decAug.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("augmax", _intAugMax.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("val", _decVal.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("rating", _intRating.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("exclude", _strExclude);
            objWriter.WriteElementString("condition", _strCondition);
            objWriter.WriteElementString("improvementttype", _eImprovementType.ToString());
            objWriter.WriteElementString("improvementsource", _eImprovementSource.ToString());
            objWriter.WriteElementString("custom", _blnCustom.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("customname", _strCustomName);
            objWriter.WriteElementString("customid", _strCustomId);
            objWriter.WriteElementString("customgroup", _strCustomGroup);
            objWriter.WriteElementString("addtorating", _intAddToRating.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("enabled", _intEnabled.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("order", _intOrder.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("notes", _strNotes.CleanOfXmlInvalidUnicodeChars());
            objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the CharacterAttribute from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            if (objNode == null)
                return;
            objNode.TryGetStringFieldQuickly("unique", ref _strUniqueName);
            objNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objNode.TryGetStringFieldQuickly("improvedname", ref _strImprovedName);
            objNode.TryGetStringFieldQuickly("sourcename", ref _strSourceName);
            objNode.TryGetInt32FieldQuickly("min", ref _intMin);
            objNode.TryGetInt32FieldQuickly("max", ref _intMax);
            objNode.TryGetDecFieldQuickly("aug", ref _decAug);
            objNode.TryGetInt32FieldQuickly("augmax", ref _intAugMax);
            objNode.TryGetDecFieldQuickly("val", ref _decVal);
            objNode.TryGetInt32FieldQuickly("rating", ref _intRating);
            objNode.TryGetStringFieldQuickly("exclude", ref _strExclude);
            objNode.TryGetStringFieldQuickly("condition", ref _strCondition);
            if (objNode["improvementttype"] != null)
                _eImprovementType = ConvertToImprovementType(objNode["improvementttype"].InnerTextViaPool());
            if (objNode["improvementsource"] != null)
                _eImprovementSource = ConvertToImprovementSource(objNode["improvementsource"].InnerTextViaPool());
            // Legacy shims
            if (_objCharacter.LastSavedVersion <= new ValueVersion(5, 214, 112)
                && (_eImprovementSource == ImprovementSource.Initiation
                    || _eImprovementSource == ImprovementSource.Submersion)
                && _eImprovementType == ImprovementType.Attribute
                && _intMax > 1 && _intRating == 1)
            {
                _intRating = _intMax;
                _intMax = 1;
            }

            switch (_eImprovementType)
            {
                case ImprovementType.LimitModifier
                    when string.IsNullOrEmpty(_strCondition) && !string.IsNullOrEmpty(_strExclude):
                    _strCondition = _strExclude;
                    _strExclude = string.Empty;
                    break;

                case ImprovementType.RestrictedGear when _decVal == 0:
                    _decVal = 24;
                    break;

                case ImprovementType.BlockSkillDefault when _objCharacter.LastSavedVersion <= new ValueVersion(5, 224, 39):
                    _eImprovementType = ImprovementType.BlockSkillGroupDefault;
                    break;

                case ImprovementType.PhysicalLimit when string.IsNullOrEmpty(_strImprovedName):
                    _strImprovedName = "Physical";
                    break;

                case ImprovementType.MentalLimit when string.IsNullOrEmpty(_strImprovedName):
                    _strImprovedName = "Mental";
                    break;

                case ImprovementType.SocialLimit when string.IsNullOrEmpty(_strImprovedName):
                    _strImprovedName = "Social";
                    break;
            }

            objNode.TryGetBoolFieldQuickly("custom", ref _blnCustom);
            objNode.TryGetStringFieldQuickly("customname", ref _strCustomName);
            objNode.TryGetStringFieldQuickly("customid", ref _strCustomId);
            objNode.TryGetStringFieldQuickly("customgroup", ref _strCustomGroup);
            if (objNode.TryGetInt32FieldQuickly("addtorating", ref _intAddToRating))
            {
                bool blnTemp = false;
                if (objNode.TryGetBoolFieldQuickly("addtorating", ref blnTemp))
                    _intAddToRating = blnTemp.ToInt32();
            }
            if (objNode.TryGetInt32FieldQuickly("enabled", ref _intEnabled))
            {
                bool blnTemp = false;
                if (objNode.TryGetBoolFieldQuickly("enabled", ref blnTemp))
                    _intEnabled = blnTemp.ToInt32();
            }
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            objNode.TryGetInt32FieldQuickly("order", ref _intOrder);
        }

        #endregion Save and Load Methods

        #region Properties

        public Character CharacterObject => _objCharacter;

        /// <summary>
        /// Whether this is a custom-made (manually created) Improvement.
        /// </summary>
        public bool Custom
        {
            get => _blnCustom;
            set => _blnCustom = value;
        }

        /// <summary>
        /// User-entered name for the custom Improvement.
        /// </summary>
        public string CustomName
        {
            get => _strCustomName;
            set => _strCustomName = value;
        }

        /// <summary>
        /// ID from the Improvements file. Only used for custom-made (manually created) Improvements.
        /// </summary>
        public string CustomId
        {
            get => _strCustomId;
            set => _strCustomId = value;
        }

        /// <summary>
        /// Group name for the Custom Improvement.
        /// </summary>
        public string CustomGroup
        {
            get => _strCustomGroup;
            set => _strCustomGroup = value;
        }

        /// <summary>
        /// User-entered notes for the custom Improvement.
        /// </summary>
        public string Notes
        {
            get => _strNotes;
            set => _strNotes = value;
        }

        public Task<string> GetNotesAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<string>(token);
            return Task.FromResult(_strNotes);
        }

        public Task SetNotesAsync(string value, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);
            _strNotes = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forecolor to use for Notes in treeviews.
        /// </summary>
        public Color NotesColor
        {
            get => _colNotes;
            set => _colNotes = value;
        }

        public Task<Color> GetNotesColorAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<Color>(token);
            return Task.FromResult(_colNotes);
        }

        public Task SetNotesColorAsync(Color value, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);
            _colNotes = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Name of the Skill or CharacterAttribute that the Improvement is improving.
        /// </summary>
        public string ImprovedName
        {
            get => _strImprovedName;
            set
            {
                string strOldValue = Interlocked.Exchange(ref _strImprovedName, value);
                if (strOldValue != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, strOldValue);
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, value);
                    using (TemporaryStringArray strYielded = strOldValue.YieldAsPooled())
                        this.ProcessRelevantEvents(lstExtraImprovedName: strYielded);
                }
            }
        }

        /// <summary>
        /// Name of the source that granted this Improvement.
        /// </summary>
        public string SourceName
        {
            get => _strSourceName;
            set => _strSourceName = value;
        }

        /// <summary>
        /// The type of Object that the Improvement is improving.
        /// </summary>
        public ImprovementType ImproveType
        {
            get => _eImprovementType;
            set
            {
                ImprovementType eOldType = InterlockedExtensions.Exchange(ref _eImprovementType, value);
                if (eOldType != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, eOldType, ImprovedName);
                    ImprovementManager.ClearCachedValue(_objCharacter, value, ImprovedName);
                    using (TemporaryArray<ImprovementType> eYielded = eOldType.YieldAsPooled())
                        this.ProcessRelevantEvents(lstExtraImprovementTypes: eYielded);
                }
            }
        }

        /// <summary>
        /// The type of Object that granted this Improvement.
        /// </summary>
        public ImprovementSource ImproveSource
        {
            get => _eImprovementSource;
            set
            {
                if (InterlockedExtensions.Exchange(ref _eImprovementSource, value) != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// Minimum value modifier.
        /// </summary>
        public int Minimum
        {
            get => _intMin;
            set
            {
                if (Interlocked.Exchange(ref _intMin, value) != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// Maximum value modifier.
        /// </summary>
        public int Maximum
        {
            get => _intMax;
            set
            {
                if (Interlocked.Exchange(ref _intMax, value) != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// Augmented Maximum value modifier.
        /// </summary>
        public int AugmentedMaximum
        {
            get => _intAugMax;
            set
            {
                if (Interlocked.Exchange(ref _intAugMax, value) != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// Augmented score modifier.
        /// </summary>
        public decimal Augmented
        {
            get => _decAug;
            set
            {
                if (_decAug != value)
                {
                    _decAug = value;
                    if (Enabled)
                    {
                        ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                        this.ProcessRelevantEvents();
                    }
                }
            }
        }

        /// <summary>
        /// Value modifier.
        /// </summary>
        public decimal Value
        {
            get => _decVal;
            set
            {
                if (_decVal != value)
                {
                    _decVal = value;
                    if (Enabled)
                    {
                        ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                        this.ProcessRelevantEvents();
                    }
                }
            }
        }

        public Task SetValueAsync(decimal value, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);
            if (_decVal != value)
            {
                _decVal = value;
                if (Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName, token);
                    return this.ProcessRelevantEventsAsync(token: token);
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The Rating value for the Improvement. This is 1 by default.
        /// </summary>
        public int Rating
        {
            get => _intRating;
            set
            {
                if (Interlocked.Exchange(ref _intRating, value) != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        public Task SetRatingAsync(int value, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);
            if (Interlocked.Exchange(ref _intRating, value) != value && Enabled)
            {
                ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName, token);
                return this.ProcessRelevantEventsAsync(token: token);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// A list of child items that should not receive the Improvement's benefit (typically for excluding a Skill from a Skill Group bonus).
        /// </summary>
        public string Exclude
        {
            get => _strExclude;
            set
            {
                if (Interlocked.Exchange(ref _strExclude, value) != value && Enabled)
                    this.ProcessRelevantEvents();
            }
        }

        /// <summary>
        /// String containing the condition for when the bonus applies (e.g. a dicepool bonus to a skill that only applies to certain types of tests).
        /// </summary>
        public string Condition
        {
            get => _strCondition;
            set
            {
                string strOldValue = Interlocked.Exchange(ref _strCondition, value);
                if (strOldValue != value && Enabled)
                {
                    if (string.IsNullOrEmpty(strOldValue) || string.IsNullOrEmpty(value))
                        ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// A Unique name for the Improvement. Only the highest value of any one Improvement that is part of this Unique Name group will be applied.
        /// </summary>
        public string UniqueName
        {
            get => _strUniqueName;
            set
            {
                string strOldValue = Interlocked.Exchange(ref _strUniqueName, value);
                if (strOldValue != value && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    using (TemporaryStringArray strYielded = strOldValue.YieldAsPooled())
                        this.ProcessRelevantEvents(lstExtraUniqueName: strYielded);
                }
            }
        }

        /// <summary>
        /// Whether the bonus applies directly to a Skill's Rating
        /// </summary>
        public bool AddToRating
        {
            get => _intAddToRating > 0;
            set
            {
                int intNewValue = value.ToInt32();
                if (Interlocked.Exchange(ref _intAddToRating, intNewValue) != intNewValue && Enabled)
                {
                    ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                    this.ProcessRelevantEvents();
                }
            }
        }

        /// <summary>
        /// The target of an improvement, e.g. the skill whose attributes should be swapped
        /// </summary>
        public string Target
        {
            get => _strTarget;
            set
            {
                string strOldValue = Interlocked.Exchange(ref _strTarget, value);
                if (strOldValue != value && Enabled)
                {
                    using (TemporaryStringArray strYielded = strOldValue.YieldAsPooled())
                        this.ProcessRelevantEvents(lstExtraTarget: strYielded);
                }
            }
        }

        /// <summary>
        /// Whether the Improvement is enabled and provided its bonus.
        /// </summary>
        public bool Enabled
        {
            get => _intEnabled > 0;
            set
            {
                int intNewValue = value.ToInt32();
                if (Interlocked.Exchange(ref _intEnabled, intNewValue) == intNewValue)
                    return;
                ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName);
                this.ProcessRelevantEvents();
            }
        }

        public Task SetEnabledAsync(bool value, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);
            int intNewValue = value.ToInt32();
            if (Interlocked.Exchange(ref _intEnabled, intNewValue) == intNewValue)
                return Task.CompletedTask;
            ImprovementManager.ClearCachedValue(_objCharacter, ImproveType, ImprovedName, token);
            return this.ProcessRelevantEventsAsync(token: token);
        }

        /// <summary>
        /// Whether we have completed our first setup. Needed to skip superfluous event updates at startup
        /// </summary>
        public bool SetupComplete
        {
            get => _blnSetupComplete;
            set => _blnSetupComplete = value;
        }

        /// <summary>
        /// Sort order for Custom Improvements.
        /// </summary>
        public int SortOrder
        {
            get => _intOrder;
            set => _intOrder = value;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Get an enumerable of events to fire related to this specific improvement.
        /// TODO: Merge parts or all of this function with ImprovementManager methods that enable, disable, add, or remove improvements.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> GetRelevantPropertyChangers(IReadOnlyCollection<string> lstExtraImprovedName = null, ImprovementType eOverrideType = ImprovementType.None, IReadOnlyCollection<string> lstExtraUniqueName = null, IReadOnlyCollection<string> lstExtraTarget = null)
        {
            switch (eOverrideType != ImprovementType.None ? eOverrideType : ImproveType)
            {
                case ImprovementType.Attribute:
                {
                    string strTargetAttribute = ImprovedName;
                    if (string.Equals(UniqueName, "enableattribute", StringComparison.OrdinalIgnoreCase))
                    {
                        switch (strTargetAttribute.ToUpperInvariant())
                        {
                            case "MAG":
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                    _objCharacter, nameof(Character.MAGEnabled));
                                break;
                            case "RES":
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                    _objCharacter, nameof(Character.RESEnabled));
                                break;
                            case "DEP":
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                    _objCharacter, nameof(Character.DEPEnabled));
                                break;
                        }
                    }
                    using (new FetchSafelyFromSafeObjectPool<HashSet<string>>(Utils.StringHashSetPool,
                                                                    out HashSet<string>
                                                                        setAttributePropertiesChanged))
                    {
                        // Always refresh these, just in case (because we cannot appropriately detect when augmented values might be set or unset)
                        setAttributePropertiesChanged.Add(nameof(CharacterAttrib.AttributeModifiers));
                        setAttributePropertiesChanged.Add(nameof(CharacterAttrib.HasModifiers));
                        if (AugmentedMaximum != 0)
                            setAttributePropertiesChanged.Add(nameof(CharacterAttrib.AugmentedMaximumModifiers));
                        if (Maximum != 0)
                            setAttributePropertiesChanged.Add(nameof(CharacterAttrib.MaximumModifiers));
                        if (Minimum != 0)
                            setAttributePropertiesChanged.Add(nameof(CharacterAttrib.MinimumModifiers));
                        List<string> lstAddonImprovedNames = null;
                        if (lstExtraImprovedName != null)
                        {
                            lstAddonImprovedNames = new List<string>(lstExtraImprovedName.Count);
                            foreach (string strExtraAttribute in lstExtraImprovedName.Where(x => x.EndsWith("Base", StringComparison.Ordinal)))
                            {
                                lstAddonImprovedNames.Add(strExtraAttribute.TrimEndOnce("Base", true));
                            }
                        }
                        strTargetAttribute = strTargetAttribute.TrimEndOnce("Base");
                        if (setAttributePropertiesChanged.Count > 0)
                        {
                            foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                            {
                                if (objCharacterAttrib.Abbrev != strTargetAttribute
                                    && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true
                                    && lstAddonImprovedNames?.Contains(objCharacterAttrib.Abbrev) != true)
                                    continue;
                                foreach (string strPropertyName in setAttributePropertiesChanged)
                                {
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        objCharacterAttrib,
                                        strPropertyName);
                                }
                            }
                        }
                    }
                }
                    break;

                case ImprovementType.AttributeMaxClamp:
                {
                    string strTargetAttribute = ImprovedName;
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if (objCharacterAttrib.Abbrev != strTargetAttribute && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true)
                            continue;
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                            objCharacterAttrib,
                            nameof(CharacterAttrib.AttributeModifiers));
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                            objCharacterAttrib,
                            nameof(CharacterAttrib.TotalAugmentedMaximum));
                    }
                }
                    break;

                case ImprovementType.Armor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.GetArmorRating));
                }
                    break;

                case ImprovementType.FireArmor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalFireArmorRating));
                }
                    break;

                case ImprovementType.ColdArmor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalColdArmorRating));
                }
                    break;

                case ImprovementType.ElectricityArmor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalElectricityArmorRating));
                }
                    break;

                case ImprovementType.AcidArmor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalAcidArmorRating));
                }
                    break;

                case ImprovementType.FallingArmor:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalFallingArmorRating));
                }
                    break;

                case ImprovementType.Dodge:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalBonusDodgeRating));
                }
                    break;

                case ImprovementType.Reach:
                    break;

                case ImprovementType.Nuyen:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalStartingNuyen));
                    if (ImprovedName == "Stolen")
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.HasStolenNuyen));
                }
                    break;

                case ImprovementType.PhysicalCM:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PhysicalCM));
                }
                    break;

                case ImprovementType.StunCM:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.StunCM));
                }
                    break;

                case ImprovementType.UnarmedDV:
                    break;

                case ImprovementType.InitiativeDiceAdd:
                case ImprovementType.InitiativeDice:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.InitiativeDice));
                }
                    break;

                case ImprovementType.MatrixInitiative:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.MatrixInitiativeValue));
                }
                    break;

                case ImprovementType.MatrixInitiativeDiceAdd:
                case ImprovementType.MatrixInitiativeDice:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.MatrixInitiativeDice));
                }
                    break;

                case ImprovementType.LifestyleCost:
                    break;

                case ImprovementType.CMThreshold:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CMThreshold));
                }
                    break;

                case ImprovementType.IgnoreCMPenaltyPhysical:
                case ImprovementType.IgnoreCMPenaltyStun:
                case ImprovementType.CMThresholdOffset:
                case ImprovementType.CMSharedThresholdOffset:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CMThresholdOffsets));
                }
                    break;

                case ImprovementType.EnhancedArticulation:
                    break;

                case ImprovementType.WeaponCategoryDV:
                    break;

                case ImprovementType.WeaponCategoryDice:
                    break;

                case ImprovementType.WeaponCategoryAP:
                    break;

                case ImprovementType.WeaponCategoryAccuracy:
                    break;

                case ImprovementType.WeaponCategoryReach:
                    break;

                case ImprovementType.WeaponSpecificDice:
                    break;

                case ImprovementType.WeaponSpecificDV:
                    break;

                case ImprovementType.WeaponSpecificAP:
                    break;

                case ImprovementType.WeaponSpecificAccuracy:
                    break;

                case ImprovementType.WeaponSpecificRange:
                    break;

                case ImprovementType.SpecialTab:
                {
                    switch (UniqueName.ToUpperInvariant())
                    {
                        case "ENABLETAB":
                            switch (ImprovedName.ToUpperInvariant())
                            {
                                case "MAGICIAN":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.MagicianEnabled));
                                    break;
                                case "ADEPT":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.AdeptEnabled));
                                    break;
                                case "TECHNOMANCER":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.TechnomancerEnabled));
                                    break;
                                case "ADVANCED PROGRAMS":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.AdvancedProgramsEnabled));
                                    break;
                                case "CRITTER":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.CritterEnabled));
                                    break;
                            }
                            break;
                        case "DISABLETAB":
                            switch (ImprovedName)
                            {
                                case "CYBERWARE":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.CyberwareDisabled));
                                    break;
                                case "INITIATION":
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.InitiationForceDisabled));
                                    break;
                            }
                            break;
                    }
                }
                    break;

                case ImprovementType.Initiative:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.InitiativeValue));
                }
                    break;

                case ImprovementType.LivingPersonaDeviceRating:
                    break;

                case ImprovementType.LivingPersonaProgramLimit:
                    break;

                case ImprovementType.LivingPersonaAttack:
                    break;

                case ImprovementType.LivingPersonaSleaze:
                    break;

                case ImprovementType.LivingPersonaDataProcessing:
                    break;

                case ImprovementType.LivingPersonaFirewall:
                    break;

                case ImprovementType.LivingPersonaMatrixCM:
                    break;

                case ImprovementType.Smartlink:
                    break;

                case ImprovementType.CyberwareEssCostNonRetroactive:
                case ImprovementType.CyberwareTotalEssMultiplierNonRetroactive:
                case ImprovementType.BiowareEssCostNonRetroactive:
                case ImprovementType.BiowareTotalEssMultiplierNonRetroactive:
                {
                    if (!_objCharacter.Created)
                    {
                        // Immediately reset cached essence to make sure this fires off before any other property changers would
                        _objCharacter.ResetCachedEssence();
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Essence));
                    }
                    break;
                }
                case ImprovementType.GenetechCostMultiplier:
                    break;

                case ImprovementType.SoftWeave:
                    break;

                case ImprovementType.DisableBioware:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AddBiowareEnabled));
                }
                    break;

                case ImprovementType.DisableCyberware:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AddCyberwareEnabled));
                }
                    break;

                case ImprovementType.DisableBiowareGrade:
                    break;

                case ImprovementType.DisableCyberwareGrade:
                    break;

                case ImprovementType.ConditionMonitor:
                    break;

                case ImprovementType.UnarmedDVPhysical:
                    break;

                case ImprovementType.Adapsin:
                    break;

                case ImprovementType.FreePositiveQualities:
                    break;

                case ImprovementType.FreeNegativeQualities:
                    break;

                case ImprovementType.FreeKnowledgeSkills:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.SkillsSection,
                        nameof(SkillsSection.KnowledgeSkillPoints));
                }
                    break;

                case ImprovementType.NuyenMaxBP:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalNuyenMaximumBP));
                }
                    break;

                case ImprovementType.CMOverflow:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CMOverflow));
                }
                    break;

                case ImprovementType.FreeSpiritPowerPoints:
                    break;

                case ImprovementType.AdeptPowerPoints:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PowerPointsTotal));
                }
                    break;

                case ImprovementType.ArmorEncumbrancePenalty:
                    break;

                case ImprovementType.Art:
                    break;

                case ImprovementType.Metamagic:
                    break;

                case ImprovementType.Echo:
                    break;

                case ImprovementType.DamageResistance:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.DamageResistancePool));
                }
                    break;

                case ImprovementType.JudgeIntentions:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.JudgeIntentions));
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.JudgeIntentionsResist));
                }
                    break;

                case ImprovementType.JudgeIntentionsOffense:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.JudgeIntentions));
                }
                    break;

                case ImprovementType.JudgeIntentionsDefense:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.JudgeIntentionsResist));
                }
                    break;

                case ImprovementType.LiftAndCarry:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.LiftAndCarry));
                }
                    break;

                case ImprovementType.Memory:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Memory));
                }
                    break;

                case ImprovementType.Concealability:
                    break;

                case ImprovementType.SwapSkillAttribute:
                case ImprovementType.SwapSkillSpecAttribute:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultAttribute)))
                    {
                        yield return result;
                    }
                    
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultAttribute)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.DrainResistance:
                case ImprovementType.FadingResistance:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.MagicTradition,
                        nameof(Tradition.DrainValue));
                }
                    break;

                case ImprovementType.Composure:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Composure));
                }
                    break;

                case ImprovementType.UnarmedAP:
                    break;

                case ImprovementType.Restricted:
                    break;

                case ImprovementType.Notoriety:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CalculatedNotoriety));
                }
                    break;

                case ImprovementType.SpellCategory:
                    break;

                case ImprovementType.SpellCategoryDamage:
                    break;

                case ImprovementType.SpellCategoryDrain:
                    break;

                case ImprovementType.ThrowRange:
                    break;
                case ImprovementType.Hardwire:
                case ImprovementType.Skillwire:
                case ImprovementType.SkillsoftAccess:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (var result in ProcessAllSkillsWithProperty(_objCharacter.SkillsSection.Skills, nameof(Skill.CyberwareRating)))
                    {
                        yield return result;
                    }

                    foreach (var result in ProcessAllSkillsWithProperty(_objCharacter.SkillsSection.KnowledgeSkills, nameof(Skill.CyberwareRating)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.AddSprite:
                    break;

                case ImprovementType.BlackMarketDiscount:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.BlackMarketDiscount));
                }
                    break;

                case ImprovementType.ComplexFormLimit:
                    break;

                case ImprovementType.SpellLimit:
                    break;

                case ImprovementType.QuickeningMetamagic:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.QuickeningEnabled));
                }
                    break;

                case ImprovementType.BasicLifestyleCost:
                    break;

                case ImprovementType.ThrowSTR:
                    break;

                case ImprovementType.EssenceMax:
                {
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if (objCharacterAttrib.Abbrev == "ESS")
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.MetatypeMaximum));
                        }
                    }
                }
                    break;

                case ImprovementType.SpecificQuality:
                    break;

                case ImprovementType.MartialArt:
                    break;

                case ImprovementType.LimitModifier:
                    break;

                case ImprovementType.PhysicalLimit:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.LimitPhysical));
                }
                    break;

                case ImprovementType.MentalLimit:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.LimitMental));
                }
                    break;

                case ImprovementType.SocialLimit:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.LimitSocial));
                }
                    break;

                case ImprovementType.FriendsInHighPlaces:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.FriendsInHighPlaces));
                }
                    break;

                case ImprovementType.Erased:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Erased));
                }
                    break;

                case ImprovementType.Fame:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Fame));
                }
                    break;

                case ImprovementType.MadeMan:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.MadeMan));
                }
                    break;

                case ImprovementType.Overclocker:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Overclocker));
                }
                    break;

                case ImprovementType.RestrictedGear:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.RestrictedGear));
                }
                    break;

                case ImprovementType.TrustFund:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TrustFund));
                }
                    break;

                case ImprovementType.ExCon:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.ExCon));
                }
                    break;

                case ImprovementType.ContactForceGroup:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (Contact objTargetContact in _objCharacter.Contacts)
                        {
                            if (objTargetContact.UniqueId == ImprovedName
                                || lstExtraImprovedName.Contains(objTargetContact.UniqueId))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                    nameof(Contact.GroupEnabled));
                            }
                        }
                    }
                    else
                    {
                        Contact objTargetContact
                            = _objCharacter.Contacts.FirstOrDefault(x => x.UniqueId == ImprovedName);
                        if (objTargetContact != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                nameof(Contact.GroupEnabled));
                        }
                    }
                }
                    break;

                case ImprovementType.Attributelevel:
                {
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if (objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.FreeBase));
                        }
                    }
                }
                    break;

                case ImprovementType.AddContact:
                    break;

                case ImprovementType.Seeker:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.RedlinerBonus));
                }
                    break;

                case ImprovementType.PublicAwareness:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CalculatedPublicAwareness));
                }
                    break;

                case ImprovementType.PrototypeTranshuman:
                    break;

                case ImprovementType.DealerConnection:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.DealerConnectionDiscount));
                }
                    break;

                case ImprovementType.BlockSkillDefault:
                case ImprovementType.AllowSkillDefault:
                {
                    if (string.IsNullOrEmpty(ImprovedName))
                    {
                        // Kludgiest of kludges, but it fits spec and Sapience isn't exactly getting turned off and on constantly.
                        foreach (var result in ProcessAllSkillsWithProperty(_objCharacter.SkillsSection.Skills, nameof(Skill.Default)))
                        {
                            yield return result;
                        }

                        foreach (var result in ProcessAllSkillsWithProperty(_objCharacter.SkillsSection.KnowledgeSkills, nameof(Skill.Default)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Default)))
                        {
                            yield return result;
                        }
                        
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Default)))
                        {
                            yield return result;
                        }
                    }
                }
                    break;

                case ImprovementType.Skill:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.RelevantImprovements)))
                    {
                        yield return result;
                    }
                    
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.RelevantImprovements)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillGroup:
                {
                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.SkillGroup))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.BlockSkillGroupDefault:
                {
                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.Default), skill => skill.SkillGroup))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillCategory:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.SkillCategory))
                    {
                        yield return result;
                    }

                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.SkillCategory))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.BlockSkillCategoryDefault:
                {
                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.Default), skill => skill.SkillCategory))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillLinkedAttribute:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.Attribute))
                    {
                        yield return result;
                    }

                    foreach (var result in ProcessSkillsByPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.Attribute))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillLevel:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeKarma)))
                    {
                        yield return result;
                    }
                    
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeKarma)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillGroupLevel:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                        {
                            if (objTargetGroup.Name == ImprovedName || lstExtraImprovedName.Contains(objTargetGroup.Name))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.FreeLevels));
                            }
                        }
                    }
                    else
                    {
                        SkillGroup objTargetGroup =
                            _objCharacter.SkillsSection.SkillGroups.FirstOrDefault(x => x.Name == ImprovedName);
                        if (objTargetGroup != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.FreeLevels));
                        }
                    }
                }
                    break;

                case ImprovementType.SkillBase:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeBase)))
                        {
                            yield return result;
                        }
                        
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeBase)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        // When no specific target, process all skills
                        foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.FreeBase));
                        }
                        
                        foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.FreeBase));
                        }
                    }
                }
                    break;

                case ImprovementType.SkillGroupBase:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.SkillGroups, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(SkillGroup.FreeBase)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        // When no specific target, process all skill groups
                        foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.FreeBase));
                        }
                    }
                }
                    break;

                case ImprovementType.Skillsoft:
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating)))
                        {
                            yield return result;
                        }
                    }
                    break;

                case ImprovementType.Activesoft:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.ReplaceAttribute:
                {
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if ((objCharacterAttrib.Abbrev != ImprovedName && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true)
                            || objCharacterAttrib.MetatypeCategory == AttributeCategory.Shapeshifter)
                            continue;
                        if (Maximum != 0)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.MetatypeMaximum));
                        if (Minimum != 0)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.MetatypeMinimum));
                        if (AugmentedMaximum != 0)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.MetatypeAugmentedMaximum));
                    }
                }
                    break;

                case ImprovementType.SpecialSkills:
                    // We directly modify the ForceDisabled property for these improvements, so we don't need to return anything
                    break;

                case ImprovementType.SkillAttribute:
                {
                    foreach (Skill objSkill in _objCharacter.SkillsSection.Skills)
                    {
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                            nameof(Skill.PoolModifiers));
                    }
                }
                    break;
                case ImprovementType.RemoveSkillCategoryDefaultPenalty:
                {
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.DefaultModifier));
                    }
                }
                    break;
                case ImprovementType.RemoveSkillGroupDefaultPenalty:
                {
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillGroup == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillGroup) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.DefaultModifier));
                    }
                }
                    break;
                case ImprovementType.RemoveSkillDefaultPenalty:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultModifier)))
                    {
                        yield return result;
                    }
                }
                    break;
                case ImprovementType.ReflexRecorderOptimization:
                {
                    foreach (Skill objSkill in _objCharacter.SkillsSection.Skills)
                    {
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                            nameof(Skill.DefaultModifier));
                    }
                }
                    break;

                case ImprovementType.Ambidextrous:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Ambidextrous));
                }
                    break;

                case ImprovementType.UnarmedReach:
                    break;

                case ImprovementType.SkillExpertise:
                case ImprovementType.SkillSpecialization:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Specializations)))
                    {
                        yield return result;
                    }
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Specializations)))
                    {
                        yield return result;
                    }

                    break;
                }

                case ImprovementType.SkillSpecializationOption:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CGLSpecializations)))
                    {
                        yield return result;
                    }
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CGLSpecializations)))
                    {
                        yield return result;
                    }

                    break;
                }
                case ImprovementType.NativeLanguageLimit:
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.SkillsSection,
                        nameof(SkillsSection.HasAvailableNativeLanguageSlots));
                    break;

                case ImprovementType.AdeptPowerFreePoints:
                {
                    // Get the power improved by this improvement
                    if (lstExtraImprovedName?.Count > 0 || lstExtraUniqueName?.Count > 0)
                    {
                        foreach (Power objImprovedPower in _objCharacter.Powers)
                        {
                            if ((objImprovedPower.Name == ImprovedName || lstExtraImprovedName?.Contains(objImprovedPower.Name) == true)
                                && (objImprovedPower.Extra == UniqueName || lstExtraUniqueName?.Contains(objImprovedPower.Extra) == true))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                    nameof(Power.FreePoints));
                            }
                        }
                    }
                    else
                    {
                        Power objImprovedPower = _objCharacter.Powers.FirstOrDefault(objPower =>
                            objPower.Name == ImprovedName && objPower.Extra == UniqueName);
                        if (objImprovedPower != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                nameof(Power.FreePoints));
                        }
                    }
                }
                    break;

                case ImprovementType.AdeptPowerFreeLevels:
                {
                    // Get the power improved by this improvement
                    if (lstExtraImprovedName?.Count > 0 || lstExtraUniqueName?.Count > 0)
                    {
                        foreach (Power objImprovedPower in _objCharacter.Powers)
                        {
                            if ((objImprovedPower.Name == ImprovedName
                                 || lstExtraImprovedName?.Contains(objImprovedPower.Name) == true)
                                && (objImprovedPower.Extra == UniqueName
                                    || lstExtraUniqueName?.Contains(objImprovedPower.Extra) == true))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                    nameof(Power.FreeLevels));
                            }
                        }
                    }
                    else
                    {
                        Power objImprovedPower = _objCharacter.Powers.FirstOrDefault(objPower =>
                            objPower.Name == ImprovedName && objPower.Extra == UniqueName);
                        if (objImprovedPower != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                nameof(Power.FreeLevels));
                        }
                    }
                }
                    break;

                case ImprovementType.AIProgram:
                    break;

                case ImprovementType.CritterPowerLevel:
                    break;

                case ImprovementType.CritterPower:
                    break;

                case ImprovementType.SpellResistance:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellResistance));
                }
                    break;

                case ImprovementType.LimitSpellCategory:
                    break;

                case ImprovementType.LimitSpellDescriptor:
                    break;

                case ImprovementType.LimitSpiritCategory:
                    break;

                case ImprovementType.WalkSpeed:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.WalkingRate));
                }
                    break;

                case ImprovementType.RunSpeed:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.RunningRate));
                }
                    break;

                case ImprovementType.SprintSpeed:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SprintingRate));
                }
                    break;

                case ImprovementType.WalkMultiplier:
                case ImprovementType.WalkMultiplierPercent:
                case ImprovementType.RunMultiplier:
                case ImprovementType.RunMultiplierPercent:
                case ImprovementType.SprintBonus:
                case ImprovementType.SprintBonusPercent:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CalculatedMovement));
                }
                    break;

                case ImprovementType.EssencePenalty:
                case ImprovementType.EssencePenaltyT100:
                case ImprovementType.EssencePenaltyMAGOnlyT100:
                case ImprovementType.EssencePenaltyRESOnlyT100:
                case ImprovementType.EssencePenaltyDEPOnlyT100:
                case ImprovementType.SpecialAttBurn:
                case ImprovementType.SpecialAttTotalBurnMultiplier:
                case ImprovementType.CyborgEssence:
                case ImprovementType.CyberwareEssCost:
                case ImprovementType.CyberwareTotalEssMultiplier:
                case ImprovementType.BiowareEssCost:
                case ImprovementType.BiowareTotalEssMultiplier:
                case ImprovementType.BasicBiowareEssCost:
                case ImprovementType.GenetechEssMultiplier:
                    // Immediately reset cached essence to make sure this fires off before any other property changers would
                    _objCharacter.ResetCachedEssence();
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Essence));
                    break;

                case ImprovementType.FreeSpellsATT:
                    break;

                case ImprovementType.FreeSpells:
                    break;

                case ImprovementType.DrainValue:
                    break;

                case ImprovementType.FadingValue:
                    break;

                case ImprovementType.Spell:
                    break;

                case ImprovementType.ComplexForm:
                    break;

                case ImprovementType.Gear:
                    break;

                case ImprovementType.Weapon:
                    break;

                case ImprovementType.MentorSpirit:
                    break;

                case ImprovementType.Paragon:
                    break;

                case ImprovementType.FreeSpellsSkill:
                    break;

                case ImprovementType.DisableSpecializationEffects:
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.GetSpecializationBonus)))
                        {
                            yield return result;
                        }
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.GetSpecializationBonus)))
                        {
                            yield return result;
                        }
                    }
                    break;

                case ImprovementType.PhysiologicalAddictionFirstTime:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PhysiologicalAddictionResistFirstTime));
                }
                    break;

                case ImprovementType.PsychologicalAddictionFirstTime:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PsychologicalAddictionResistFirstTime));
                }
                    break;

                case ImprovementType.PhysiologicalAddictionAlreadyAddicted:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PhysiologicalAddictionResistAlreadyAddicted));
                }
                    break;

                case ImprovementType.PsychologicalAddictionAlreadyAddicted:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PsychologicalAddictionResistAlreadyAddicted));
                }
                    break;

                case ImprovementType.AddESStoStunCMRecovery:
                case ImprovementType.StunCMRecovery:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.StunCMNaturalRecovery));
                }
                    break;

                case ImprovementType.AddESStoPhysicalCMRecovery:
                case ImprovementType.PhysicalCMRecovery:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.PhysicalCMNaturalRecovery));
                }
                    break;

                case ImprovementType.MentalManipulationResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseManipulationMental));
                }
                    break;

                case ImprovementType.PhysicalManipulationResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseManipulationPhysical));
                }
                    break;

                case ImprovementType.ManaIllusionResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseIllusionMana));
                }
                    break;

                case ImprovementType.PhysicalIllusionResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseIllusionPhysical));
                }
                    break;

                case ImprovementType.DetectionSpellResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDetection));
                }
                    break;

                case ImprovementType.DirectManaSpellResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDirectSoakMana));
                }
                    break;

                case ImprovementType.DirectPhysicalSpellResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDirectSoakPhysical));
                }
                    break;

                case ImprovementType.DecreaseBODResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseBOD));
                }
                    break;

                case ImprovementType.DecreaseAGIResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseAGI));
                }
                    break;

                case ImprovementType.DecreaseREAResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseREA));
                }
                    break;

                case ImprovementType.DecreaseSTRResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseSTR));
                }
                    break;

                case ImprovementType.DecreaseCHAResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseCHA));
                }
                    break;

                case ImprovementType.DecreaseINTResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseINT));
                }
                    break;

                case ImprovementType.DecreaseLOGResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseLOG));
                }
                    break;

                case ImprovementType.DecreaseWILResist:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellDefenseDecreaseWIL));
                }
                    break;

                case ImprovementType.AddLimb:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.LimbCount));
                }
                    break;

                case ImprovementType.StreetCredMultiplier:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.CalculatedStreetCred));
                }
                    break;

                case ImprovementType.StreetCred:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.TotalStreetCred));
                }
                    break;

                case ImprovementType.AttributeKarmaCostMultiplier:
                case ImprovementType.AttributeKarmaCost:
                {
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if (string.IsNullOrEmpty(ImprovedName) || objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.UpgradeKarmaCost));
                        }
                    }
                }
                    break;

                case ImprovementType.ActiveSkillKarmaCost:
                case ImprovementType.ActiveSkillKarmaCostMultiplier:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.UpgradeKarmaCost)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.UpgradeKarmaCost));
                        }
                    }
                }
                    break;

                case ImprovementType.KnowledgeSkillKarmaCost:
                case ImprovementType.KnowledgeSkillKarmaCostMinimum:
                case ImprovementType.KnowledgeSkillKarmaCostMultiplier:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.UpgradeKarmaCost)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.UpgradeKarmaCost));
                        }
                    }
                }
                    break;

                case ImprovementType.SkillGroupKarmaCost:
                case ImprovementType.SkillGroupKarmaCostMultiplier:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                            {
                                if (objTargetGroup.Name == ImprovedName || lstExtraImprovedName.Contains(objTargetGroup.Name))
                                {
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                        nameof(SkillGroup.UpgradeKarmaCost));
                                }
                            }
                        }
                        else
                        {
                            SkillGroup objTargetGroup =
                                _objCharacter.SkillsSection.SkillGroups.FirstOrDefault(x => x.Name == ImprovedName);
                            if (objTargetGroup != null)
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.UpgradeKarmaCost));
                            }
                        }
                    }
                    else
                    {
                        foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.UpgradeKarmaCost));
                        }
                    }
                }
                    break;

                case ImprovementType.SkillGroupDisable:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                        {
                            if (objTargetGroup.Name == ImprovedName || lstExtraImprovedName.Contains(objTargetGroup.Name))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.IsDisabled));
                            }
                        }
                    }
                    else
                    {
                        SkillGroup objTargetGroup =
                            _objCharacter.SkillsSection.SkillGroups.FirstOrDefault(x => x.Name == ImprovedName);
                        if (objTargetGroup != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.IsDisabled));
                        }
                    }

                    break;
                }
                case ImprovementType.SkillDisable:
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled)))
                        {
                            yield return result;
                        }
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled)))
                        {
                            yield return result;
                        }
                    }
                    break;

                case ImprovementType.SkillEnableMovement:
                {
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled)))
                    {
                        yield return result;
                    }
                    foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled)))
                    {
                        yield return result;
                    }
                }
                    break;

                case ImprovementType.SkillCategorySpecializationKarmaCost:
                case ImprovementType.SkillCategorySpecializationKarmaCostMultiplier:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CanAffordSpecialization));
                    }

                    foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CanAffordSpecialization));
                    }
                }
                    break;

                case ImprovementType.SkillCategoryKarmaCost:
                case ImprovementType.SkillCategoryKarmaCostMultiplier:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.UpgradeKarmaCost));
                    }

                    foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.UpgradeKarmaCost));
                    }
                }
                    break;

                case ImprovementType.SkillGroupCategoryDisable:
                {
                    foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.IsDisabled));
                        }
                    }
                }
                    break;

                case ImprovementType.SkillGroupCategoryKarmaCostMultiplier:
                case ImprovementType.SkillGroupCategoryKarmaCost:
                {
                    foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.UpgradeKarmaCost));
                        }
                    }
                }
                    break;

                case ImprovementType.AttributePointCost:
                case ImprovementType.AttributePointCostMultiplier:
                {
                    foreach (CharacterAttrib objCharacterAttrib in _objCharacter.GetAllAttributes())
                    {
                        if (string.IsNullOrEmpty(ImprovedName) || objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.SpentPriorityPoints));
                        }
                    }
                }
                    break;

                case ImprovementType.ActiveSkillPointCost:
                case ImprovementType.ActiveSkillPointCostMultiplier:
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CurrentSpCost)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        // When no specific target, process all skills
                        foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost));
                        }
                    }
                    break;

                case ImprovementType.SkillGroupPointCost:
                case ImprovementType.SkillGroupPointCostMultiplier:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                            {
                                if (objTargetGroup.Name == ImprovedName || lstExtraImprovedName.Contains(objTargetGroup.Name))
                                {
                                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                        nameof(SkillGroup.CurrentSpCost));
                                }
                            }
                        }
                        else
                        {
                            SkillGroup objTargetGroup =
                                _objCharacter.SkillsSection.SkillGroups.FirstOrDefault(x => x.Name == ImprovedName);
                            if (objTargetGroup != null)
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.CurrentSpCost));
                            }
                        }
                    }
                    else
                    {
                        foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.CurrentSpCost));
                        }
                    }
                }
                    break;

                case ImprovementType.KnowledgeSkillPointCost:
                case ImprovementType.KnowledgeSkillPointCostMultiplier:
                    {
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(KnowledgeSkill.CurrentSpCost)))
                            {
                                yield return result;
                            }
                        }
                        else
                        {
                            // When no specific target, process all knowledge skills
                            foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(KnowledgeSkill.CurrentSpCost));
                            }
                        }
                    }
                    break;

                case ImprovementType.SkillCategoryPointCost:
                case ImprovementType.SkillCategoryPointCostMultiplier:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost));
                    }

                    foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost));
                    }
                }
                    break;

                case ImprovementType.SkillGroupCategoryPointCost:
                case ImprovementType.SkillGroupCategoryPointCostMultiplier:
                {
                    foreach (SkillGroup objTargetGroup in _objCharacter.SkillsSection.SkillGroups)
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.CurrentSpCost));
                        }
                    }
                }
                    break;

                case ImprovementType.NewSpellKarmaCost:
                case ImprovementType.NewSpellKarmaCostMultiplier:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpellKarmaCost));
                }
                    break;

                case ImprovementType.NewComplexFormKarmaCost:
                case ImprovementType.NewComplexFormKarmaCostMultiplier:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.ComplexFormKarmaCost));
                }
                    break;

                case ImprovementType.NewAIProgramKarmaCost:
                case ImprovementType.NewAIProgramKarmaCostMultiplier:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AIProgramKarmaCost));
                }
                    break;

                case ImprovementType.NewAIAdvancedProgramKarmaCost:
                case ImprovementType.NewAIAdvancedProgramKarmaCostMultiplier:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AIAdvancedProgramKarmaCost));
                }
                    break;

                case ImprovementType.BlockSkillSpecializations:
                {
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
    
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.Skills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CanHaveSpecs)))
                        {
                            yield return result;
                        }
                        foreach (var result in ProcessSkillsWithPropertyComprehensive(_objCharacter.SkillsSection.KnowledgeSkills, ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CanHaveSpecs)))
                        {
                            yield return result;
                        }
                    }
                    else
                    {
                        // When no specific target, process all skills
                        foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CanHaveSpecs));
                        }
                    }
                }
                    break;

                case ImprovementType.BlockSkillCategorySpecializations:
                {
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    foreach (Skill objTargetSkill in _objCharacter.SkillsSection.Skills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CanHaveSpecs));
                    }

                    foreach (KnowledgeSkill objTargetSkill in _objCharacter.SkillsSection.KnowledgeSkills)
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName || lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CanHaveSpecs));
                    }
                }
                    break;

                case ImprovementType.FocusBindingKarmaCost:
                    break;

                case ImprovementType.FocusBindingKarmaMultiplier:
                    break;

                case ImprovementType.MagiciansWayDiscount:
                {
                    foreach (Power objLoopPower in _objCharacter.Powers)
                    {
                        if (objLoopPower.AdeptWayDiscount != 0)
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objLoopPower,
                                nameof(Power.AdeptWayDiscountEnabled));
                    }
                }
                    break;

                case ImprovementType.BurnoutsWay:
                    break;

                case ImprovementType.ContactForcedLoyalty:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (Contact objTargetContact in _objCharacter.Contacts)
                        {
                            if (objTargetContact.UniqueId == ImprovedName || lstExtraImprovedName.Contains(objTargetContact.UniqueId))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                    nameof(Contact.ForcedLoyalty));
                            }
                        }
                    }
                    else
                    {
                        Contact objTargetContact = _objCharacter.Contacts.FirstOrDefault(x => x.UniqueId == ImprovedName);
                        if (objTargetContact != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                nameof(Contact.ForcedLoyalty));
                        }
                    }
                }
                    break;

                case ImprovementType.ContactMakeFree:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (Contact objTargetContact in _objCharacter.Contacts)
                        {
                            if (objTargetContact.UniqueId == ImprovedName
                                || lstExtraImprovedName.Contains(objTargetContact.UniqueId))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                    nameof(Contact.Free));
                            }
                        }
                    }
                    else
                    {
                        Contact objTargetContact
                            = _objCharacter.Contacts.FirstOrDefault(x => x.UniqueId == ImprovedName);
                        if (objTargetContact != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                nameof(Contact.Free));
                        }
                    }
                }
                    break;

                case ImprovementType.FreeWare:
                    break;

                case ImprovementType.WeaponSkillAccuracy:
                    break;

                case ImprovementType.WeaponAccuracy:
                    break;

                case ImprovementType.SpecialModificationLimit:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SpecialModificationLimit));
                }
                    break;

                case ImprovementType.MetageneticLimit:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.MetagenicLimit));
                }
                    break;

                case ImprovementType.DisableQuality:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (Quality objQuality in _objCharacter.Qualities)
                        {
                            if (objQuality.Name == ImprovedName
                                || string.Equals(objQuality.SourceIDString, ImprovedName, StringComparison.OrdinalIgnoreCase)
                                || lstExtraImprovedName.Contains(objQuality.Name)
                                || lstExtraImprovedName.Contains(objQuality.SourceIDString))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.Suppressed));
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                    nameof(Character.Qualities));
                            }
                        }
                    }
                    else
                    {
                        Quality objQuality = _objCharacter.Qualities.FirstOrDefault(x =>
                            x.Name == ImprovedName || string.Equals(x.SourceIDString, ImprovedName, StringComparison.OrdinalIgnoreCase));
                        if (objQuality != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                nameof(Quality.Suppressed));
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                nameof(Character.Qualities));
                        }
                    }
                }
                    break;

                case ImprovementType.FreeQuality:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        foreach (Quality objQuality in _objCharacter.Qualities)
                        {
                            if (objQuality.Name == ImprovedName
                                || string.Equals(objQuality.SourceIDString, ImprovedName, StringComparison.OrdinalIgnoreCase)
                                || lstExtraImprovedName.Contains(objQuality.Name)
                                || lstExtraImprovedName.Contains(objQuality.SourceIDString))
                            {
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.ContributeToBP));
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.ContributeToLimit));
                                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                    nameof(Character.Qualities));
                            }
                        }
                    }
                    else
                    {
                        Quality objQuality = _objCharacter.Qualities.FirstOrDefault(x =>
                            x.Name == ImprovedName || string.Equals(x.SourceIDString, ImprovedName, StringComparison.OrdinalIgnoreCase));
                        if (objQuality != null)
                        {
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                nameof(Quality.ContributeToBP));
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                nameof(Quality.ContributeToLimit));
                            yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                nameof(Character.Qualities));
                        }
                    }
                }
                    break;

                case ImprovementType.AllowSpriteFettering:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AllowSpriteFettering));
                    break;
                }
                case ImprovementType.Surprise:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Surprise));
                    break;
                }
                case ImprovementType.AstralReputation:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.AstralReputation));
                    break;
                }
                case ImprovementType.AstralReputationWild:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.WildReputation));
                    break;
                }
                case ImprovementType.CyberadeptDaemon:
                {
                    if (_objCharacter.Settings.SpecialKarmaCostBasedOnShownValue)
                        yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CyberwareEssence));
                    break;
                }
                case ImprovementType.PenaltyFreeSustain:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.SustainingPenalty));
                    break;
                }
                case ImprovementType.QualityLevel:
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.HasAnyQualitiesWithQualityLevels));
                    break;
                }
            }
        }

        /// <summary>
        /// Get an enumerable of events to fire related to this specific improvement.
        /// TODO: Merge parts or all of this function with ImprovementManager methods that enable, disable, add, or remove improvements.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>>> GetRelevantPropertyChangersAsync(IReadOnlyCollection<string> lstExtraImprovedName = null, ImprovementType eOverrideType = ImprovementType.None, IReadOnlyCollection<string> lstExtraUniqueName = null, IReadOnlyCollection<string> lstExtraTarget = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> lstReturn =
                new List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>>(8);
            switch (eOverrideType != ImprovementType.None ? eOverrideType : ImproveType)
            {
                case ImprovementType.Attribute:
                    {
                        string strTargetAttribute = ImprovedName;
                        if (string.Equals(UniqueName, "enableattribute", StringComparison.OrdinalIgnoreCase))
                        {
                            switch (strTargetAttribute.ToUpperInvariant())
                            {
                                case "MAG":
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.MAGEnabled)));
                                    break;
                                case "RES":
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.RESEnabled)));
                                    break;
                                case "DEP":
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        _objCharacter, nameof(Character.DEPEnabled)));
                                    break;
                            }
                        }
                        using (new FetchSafelyFromSafeObjectPool<HashSet<string>>(Utils.StringHashSetPool,
                                                                        out HashSet<string>
                                                                            setAttributePropertiesChanged))
                        {
                            // Always refresh these, just in case (because we cannot appropriately detect when augmented values might be set or unset)
                            setAttributePropertiesChanged.Add(nameof(CharacterAttrib.AttributeModifiers));
                            setAttributePropertiesChanged.Add(nameof(CharacterAttrib.HasModifiers));
                            if (AugmentedMaximum != 0)
                                setAttributePropertiesChanged.Add(nameof(CharacterAttrib.AugmentedMaximumModifiers));
                            if (Maximum != 0)
                                setAttributePropertiesChanged.Add(nameof(CharacterAttrib.MaximumModifiers));
                            if (Minimum != 0)
                                setAttributePropertiesChanged.Add(nameof(CharacterAttrib.MinimumModifiers));
                            List<string> lstAddonImprovedNames = null;
                            if (lstExtraImprovedName != null)
                            {
                                lstAddonImprovedNames = new List<string>(lstExtraImprovedName.Count);
                                foreach (string strExtraAttribute in lstExtraImprovedName.Where(x => x.EndsWith("Base", StringComparison.Ordinal)))
                                {
                                    token.ThrowIfCancellationRequested();
                                    lstAddonImprovedNames.Add(strExtraAttribute.TrimEndOnce("Base", true));
                                }
                            }
                            strTargetAttribute = strTargetAttribute.TrimEndOnce("Base");
                            if (setAttributePropertiesChanged.Count > 0)
                            {
                                foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                                {
                                    if (objCharacterAttrib.Abbrev != strTargetAttribute
                                        && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true
                                        && lstAddonImprovedNames?.Contains(objCharacterAttrib.Abbrev) != true)
                                        continue;
                                    foreach (string strPropertyName in setAttributePropertiesChanged)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            objCharacterAttrib,
                                            strPropertyName));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case ImprovementType.AttributeMaxClamp:
                    {
                        string strTargetAttribute = ImprovedName;
                        foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                        {
                            if (objCharacterAttrib.Abbrev != strTargetAttribute && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true)
                                continue;
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objCharacterAttrib,
                                nameof(CharacterAttrib.AttributeModifiers)));
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objCharacterAttrib,
                                nameof(CharacterAttrib.TotalAugmentedMaximum)));
                        }
                    }
                    break;

                case ImprovementType.Armor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.GetArmorRating)));
                    }
                    break;

                case ImprovementType.FireArmor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalFireArmorRating)));
                    }
                    break;

                case ImprovementType.ColdArmor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalColdArmorRating)));
                    }
                    break;

                case ImprovementType.ElectricityArmor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalElectricityArmorRating)));
                    }
                    break;

                case ImprovementType.AcidArmor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalAcidArmorRating)));
                    }
                    break;

                case ImprovementType.FallingArmor:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalFallingArmorRating)));
                    }
                    break;

                case ImprovementType.Dodge:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalBonusDodgeRating)));
                    }
                    break;

                case ImprovementType.Reach:
                    break;

                case ImprovementType.Nuyen:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalStartingNuyen)));
                        if (ImprovedName == "Stolen")
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                nameof(Character.HasStolenNuyen)));
                    }
                    break;

                case ImprovementType.PhysicalCM:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PhysicalCM)));
                    }
                    break;

                case ImprovementType.StunCM:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.StunCM)));
                    }
                    break;

                case ImprovementType.UnarmedDV:
                    break;

                case ImprovementType.InitiativeDiceAdd:
                case ImprovementType.InitiativeDice:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.InitiativeDice)));
                    }
                    break;

                case ImprovementType.MatrixInitiative:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.MatrixInitiativeValue)));
                    }
                    break;

                case ImprovementType.MatrixInitiativeDiceAdd:
                case ImprovementType.MatrixInitiativeDice:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.MatrixInitiativeDice)));
                    }
                    break;

                case ImprovementType.LifestyleCost:
                    break;

                case ImprovementType.CMThreshold:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CMThreshold)));
                    }
                    break;

                case ImprovementType.IgnoreCMPenaltyPhysical:
                case ImprovementType.IgnoreCMPenaltyStun:
                case ImprovementType.CMThresholdOffset:
                case ImprovementType.CMSharedThresholdOffset:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CMThresholdOffsets)));
                    }
                    break;

                case ImprovementType.EnhancedArticulation:
                    break;

                case ImprovementType.WeaponCategoryDV:
                    break;

                case ImprovementType.WeaponCategoryDice:
                    break;

                case ImprovementType.WeaponCategoryAP:
                    break;

                case ImprovementType.WeaponCategoryAccuracy:
                    break;

                case ImprovementType.WeaponCategoryReach:
                    break;

                case ImprovementType.WeaponSpecificDice:
                    break;

                case ImprovementType.WeaponSpecificDV:
                    break;

                case ImprovementType.WeaponSpecificAP:
                    break;

                case ImprovementType.WeaponSpecificAccuracy:
                    break;

                case ImprovementType.WeaponSpecificRange:
                    break;

                case ImprovementType.SpecialTab:
                    {
                        switch (UniqueName.ToUpperInvariant())
                        {
                            case "ENABLETAB":
                                switch (ImprovedName.ToUpperInvariant())
                                {
                                    case "MAGICIAN":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.MagicianEnabled)));
                                        break;
                                    case "ADEPT":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.AdeptEnabled)));
                                        break;
                                    case "TECHNOMANCER":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.TechnomancerEnabled)));
                                        break;
                                    case "ADVANCED PROGRAMS":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.AdvancedProgramsEnabled)));
                                        break;
                                    case "CRITTER":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.CritterEnabled)));
                                        break;
                                }
                                break;
                            case "DISABLETAB":
                                switch (ImprovedName.ToUpperInvariant())
                                {
                                    case "CYBERWARE":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.CyberwareDisabled)));
                                        break;
                                    case "INITIATION":
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            _objCharacter, nameof(Character.InitiationForceDisabled)));
                                        break;
                                }
                                break;
                        }
                    }
                    break;

                case ImprovementType.Initiative:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.InitiativeValue)));
                    }
                    break;

                case ImprovementType.LivingPersonaDeviceRating:
                    break;

                case ImprovementType.LivingPersonaProgramLimit:
                    break;

                case ImprovementType.LivingPersonaAttack:
                    break;

                case ImprovementType.LivingPersonaSleaze:
                    break;

                case ImprovementType.LivingPersonaDataProcessing:
                    break;

                case ImprovementType.LivingPersonaFirewall:
                    break;

                case ImprovementType.LivingPersonaMatrixCM:
                    break;

                case ImprovementType.Smartlink:
                    break;

                case ImprovementType.CyberwareEssCostNonRetroactive:
                case ImprovementType.CyberwareTotalEssMultiplierNonRetroactive:
                case ImprovementType.BiowareEssCostNonRetroactive:
                case ImprovementType.BiowareTotalEssMultiplierNonRetroactive:
                    {
                        if (!await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                        {
                            // Immediately reset cached essence to make sure this fires off before any other property changers would
                            await _objCharacter.ResetCachedEssenceAsync(token).ConfigureAwait(false);
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                nameof(Character.Essence)));
                        }
                        break;
                    }
                case ImprovementType.GenetechCostMultiplier:
                    break;

                case ImprovementType.SoftWeave:
                    break;

                case ImprovementType.DisableBioware:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AddBiowareEnabled)));
                    }
                    break;

                case ImprovementType.DisableCyberware:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AddCyberwareEnabled)));
                    }
                    break;

                case ImprovementType.DisableBiowareGrade:
                    break;

                case ImprovementType.DisableCyberwareGrade:
                    break;

                case ImprovementType.ConditionMonitor:
                    break;

                case ImprovementType.UnarmedDVPhysical:
                    break;

                case ImprovementType.Adapsin:
                    break;

                case ImprovementType.FreePositiveQualities:
                    break;

                case ImprovementType.FreeNegativeQualities:
                    break;

                case ImprovementType.FreeKnowledgeSkills:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.SkillsSection,
                            nameof(SkillsSection.KnowledgeSkillPoints)));
                    }
                    break;

                case ImprovementType.NuyenMaxBP:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalNuyenMaximumBP)));
                    }
                    break;

                case ImprovementType.CMOverflow:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CMOverflow)));
                    }
                    break;

                case ImprovementType.FreeSpiritPowerPoints:
                    break;

                case ImprovementType.AdeptPowerPoints:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PowerPointsTotal)));
                    }
                    break;

                case ImprovementType.ArmorEncumbrancePenalty:
                    break;

                case ImprovementType.Art:
                    break;

                case ImprovementType.Metamagic:
                    break;

                case ImprovementType.Echo:
                    break;

                case ImprovementType.Skillwire:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                            nameof(Skill.CyberwareRating)));
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.DamageResistance:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.DamageResistancePool)));
                    }
                    break;

                case ImprovementType.JudgeIntentions:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.JudgeIntentions)));
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.JudgeIntentionsResist)));
                    }
                    break;

                case ImprovementType.JudgeIntentionsOffense:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.JudgeIntentions)));
                    }
                    break;

                case ImprovementType.JudgeIntentionsDefense:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.JudgeIntentionsResist)));
                    }
                    break;

                case ImprovementType.LiftAndCarry:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.LiftAndCarry)));
                    }
                    break;

                case ImprovementType.Memory:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Memory)));
                    }
                    break;

                case ImprovementType.Concealability:
                    break;

                case ImprovementType.SwapSkillAttribute:
                case ImprovementType.SwapSkillSpecAttribute:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultAttribute), lstReturn, token).ConfigureAwait(false);
                    await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultAttribute), lstReturn, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.DrainResistance:
                case ImprovementType.FadingResistance:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.MagicTradition,
                            nameof(Tradition.DrainValue)));
                    }
                    break;

                case ImprovementType.Composure:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Composure)));
                    }
                    break;

                case ImprovementType.UnarmedAP:
                    break;

                case ImprovementType.Restricted:
                    break;

                case ImprovementType.Notoriety:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CalculatedNotoriety)));
                    }
                    break;

                case ImprovementType.SpellCategory:
                    break;

                case ImprovementType.SpellCategoryDamage:
                    break;

                case ImprovementType.SpellCategoryDrain:
                    break;

                case ImprovementType.ThrowRange:
                    break;

                case ImprovementType.SkillsoftAccess:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                                nameof(Skill.CyberwareRating)));
                        }, token).ConfigureAwait(false);

                        await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                                nameof(Skill.CyberwareRating)));
                        }, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.AddSprite:
                    break;

                case ImprovementType.BlackMarketDiscount:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.BlackMarketDiscount)));
                    }
                    break;

                case ImprovementType.ComplexFormLimit:
                    break;

                case ImprovementType.SpellLimit:
                    break;

                case ImprovementType.QuickeningMetamagic:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.QuickeningEnabled)));
                    }
                    break;

                case ImprovementType.BasicLifestyleCost:
                    break;

                case ImprovementType.ThrowSTR:
                    break;

                case ImprovementType.EssenceMax:
                    {
                        foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                        {
                            if (objCharacterAttrib.Abbrev == "ESS")
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.MetatypeMaximum)));
                            }
                        }
                    }
                    break;

                case ImprovementType.SpecificQuality:
                    break;

                case ImprovementType.MartialArt:
                    break;

                case ImprovementType.LimitModifier:
                    break;

                case ImprovementType.PhysicalLimit:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.LimitPhysical)));
                    }
                    break;

                case ImprovementType.MentalLimit:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.LimitMental)));
                    }
                    break;

                case ImprovementType.SocialLimit:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.LimitSocial)));
                    }
                    break;

                case ImprovementType.FriendsInHighPlaces:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.FriendsInHighPlaces)));
                    }
                    break;

                case ImprovementType.Erased:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Erased)));
                    }
                    break;

                case ImprovementType.Fame:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Fame)));
                    }
                    break;

                case ImprovementType.MadeMan:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.MadeMan)));
                    }
                    break;

                case ImprovementType.Overclocker:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Overclocker)));
                    }
                    break;

                case ImprovementType.RestrictedGear:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.RestrictedGear)));
                    }
                    break;

                case ImprovementType.TrustFund:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TrustFund)));
                    }
                    break;

                case ImprovementType.ExCon:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.ExCon)));
                    }
                    break;

                case ImprovementType.ContactForceGroup:
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await _objCharacter.Contacts.ForEachAsync(async objTargetContact =>
                            {
                                string strId = await objTargetContact.GetUniqueIdAsync(token).ConfigureAwait(false);
                                if (strId == ImprovedName || lstExtraImprovedName.Contains(strId))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        objTargetContact,
                                        nameof(Contact.GroupEnabled)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Contact objTargetContact
                                = await _objCharacter.Contacts.FirstOrDefaultAsync(x => x.UniqueId == ImprovedName, token: token).ConfigureAwait(false);
                            if (objTargetContact != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                    nameof(Contact.GroupEnabled)));
                            }
                        }
                    }
                    break;

                case ImprovementType.Attributelevel:
                    {
                        foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                        {
                            if (objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.FreeBase)));
                            }
                        }
                    }
                    break;

                case ImprovementType.AddContact:
                    break;

                case ImprovementType.Seeker:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.RedlinerBonus)));
                    }
                    break;

                case ImprovementType.PublicAwareness:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CalculatedPublicAwareness)));
                    }
                    break;

                case ImprovementType.PrototypeTranshuman:
                    break;

                case ImprovementType.Hardwire:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.DealerConnection:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.DealerConnectionDiscount)));
                    }
                    break;

                case ImprovementType.BlockSkillDefault:
                case ImprovementType.AllowSkillDefault:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(ImprovedName))
                        {
                            // Kludgiest of kludges, but it fits spec and Sapience isn't exactly getting turned off and on constantly.
                            await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                                    nameof(Skill.Default)));
                            }, token).ConfigureAwait(false);

                            await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                                    nameof(Skill.Default)));
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Default), lstReturn, token).ConfigureAwait(false);
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Default), lstReturn, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.Skill:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.RelevantImprovements), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.RelevantImprovements), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillGroup:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.PoolModifiers), skill => skill.SkillGroup, lstReturn, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.BlockSkillGroupDefault:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.Default), skill => skill.SkillGroup, lstReturn, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.SkillCategory:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                            nameof(Skill.PoolModifiers), skill => skill.SkillCategory, lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                            nameof(Skill.PoolModifiers), skill => skill.SkillCategory, lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.BlockSkillCategoryDefault:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                        nameof(Skill.Default), skill => skill.SkillCategory, lstReturn, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.SkillLinkedAttribute:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                            nameof(Skill.PoolModifiers), skill => skill.GetAttributeAsync(token), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsByPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, 
                            nameof(Skill.PoolModifiers), skill => skill.GetAttributeAsync(token), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillLevel:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeKarma), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeKarma), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillGroupLevel:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(async objTargetGroup =>
                            {
                                string strName = await objTargetGroup.GetNameAsync(token).ConfigureAwait(false);
                                if (strName == ImprovedName ||
                                    lstExtraImprovedName.Contains(strName))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                        nameof(SkillGroup.FreeLevels)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            SkillGroup objTargetGroup =
                                await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).FirstOrDefaultAsync(async x => await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                            if (objTargetGroup != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.FreeLevels)));
                            }
                        }
                    }
                    break;

                case ImprovementType.SkillBase:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeBase), lstReturn, token).ConfigureAwait(false);
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.FreeBase), lstReturn, token).ConfigureAwait(false);
                        }
                        else
                        {
                            // When no specific target, process all skills
                            await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.FreeBase)));
                            }, token).ConfigureAwait(false);
                            
                            await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.FreeBase)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.SkillGroupBase:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(SkillGroup.FreeBase), lstReturn, token).ConfigureAwait(false);
                        }
                        else
                        {
                            // When no specific target, process all skill groups
                            await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.FreeBase)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.Skillsoft:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.Activesoft:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CyberwareRating), lstReturn, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.ReplaceAttribute:
                    {
                        foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                        {
                            if ((objCharacterAttrib.Abbrev != ImprovedName && lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) != true)
                                || objCharacterAttrib.MetatypeCategory == AttributeCategory.Shapeshifter)
                                continue;
                            if (Maximum != 0)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.MetatypeMaximum)));
                            if (Minimum != 0)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.MetatypeMinimum)));
                            if (AugmentedMaximum != 0)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.MetatypeAugmentedMaximum)));
                        }
                    }
                    break;

                case ImprovementType.SpecialSkills:
                    // We directly modify the ForceDisabled property for these improvements, so we don't need to return anything
                    break;

                case ImprovementType.SkillAttribute:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                            nameof(Skill.PoolModifiers)));
                    }, token).ConfigureAwait(false);
                }
                    break;
                case ImprovementType.RemoveSkillCategoryDefaultPenalty:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName ||
                            lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.DefaultModifier)));
                    }, token).ConfigureAwait(false);
                }
                    break;
                case ImprovementType.RemoveSkillGroupDefaultPenalty:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                    {
                        if (objTargetSkill.SkillGroup == ImprovedName ||
                            lstExtraImprovedName?.Contains(objTargetSkill.SkillGroup) == true)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.DefaultModifier)));
                    }, token).ConfigureAwait(false);
                }
                    break;
                case ImprovementType.RemoveSkillDefaultPenalty:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.DefaultModifier), lstReturn, token).ConfigureAwait(false);
                }
                    break;
                case ImprovementType.ReflexRecorderOptimization:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objSkill =>
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill,
                            nameof(Skill.DefaultModifier)));
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.Ambidextrous:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Ambidextrous)));
                    }
                    break;

                case ImprovementType.UnarmedReach:
                    break;

                case ImprovementType.SkillExpertise:
                case ImprovementType.SkillSpecialization:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Specializations), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Specializations), lstReturn, token).ConfigureAwait(false);
                        break;
                    }

                case ImprovementType.SkillSpecializationOption:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CGLSpecializations), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CGLSpecializations), lstReturn, token).ConfigureAwait(false);
                        break;
                    }
                case ImprovementType.NativeLanguageLimit:
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter.SkillsSection,
                        nameof(SkillsSection.HasAvailableNativeLanguageSlots)));
                    break;

                case ImprovementType.AdeptPowerFreePoints:
                    {
                        // Get the power improved by this improvement
                        if (lstExtraImprovedName?.Count > 0 || lstExtraUniqueName?.Count > 0)
                        {
                            await _objCharacter.Powers.ForEachAsync(async objImprovedPower =>
                            {
                                string strName = await objImprovedPower.GetNameAsync(token).ConfigureAwait(false);
                                string strExtra = await objImprovedPower.GetExtraAsync(token).ConfigureAwait(false);
                                if ((strName == ImprovedName || lstExtraImprovedName?.Contains(strName) == true)
                                    && (strExtra == UniqueName || lstExtraUniqueName?.Contains(strExtra) == true))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        objImprovedPower,
                                        nameof(Power.FreePoints)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Power objImprovedPower = await _objCharacter.Powers.FirstOrDefaultAsync(async objPower =>
                                await objPower.GetNameAsync(token).ConfigureAwait(false) == ImprovedName && await objPower.GetExtraAsync(token).ConfigureAwait(false) == UniqueName, token).ConfigureAwait(false);
                            if (objImprovedPower != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                    nameof(Power.FreePoints)));
                            }
                        }
                    }
                    break;

                case ImprovementType.AdeptPowerFreeLevels:
                    {
                        // Get the power improved by this improvement
                        if (lstExtraImprovedName?.Count > 0 || lstExtraUniqueName?.Count > 0)
                        {
                            await _objCharacter.Powers.ForEachAsync(async objImprovedPower =>
                            {
                                string strLoop = await objImprovedPower.GetNameAsync(token).ConfigureAwait(false);
                                if (strLoop == ImprovedName || lstExtraImprovedName?.Contains(strLoop) == true)
                                {
                                    strLoop = await objImprovedPower.GetExtraAsync(token).ConfigureAwait(false);
                                    if (strLoop == UniqueName || lstExtraUniqueName?.Contains(strLoop) == true)
                                    {
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            objImprovedPower,
                                            nameof(Power.FreeLevels)));
                                    }
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Power objImprovedPower = await _objCharacter.Powers.FirstOrDefaultAsync(async objPower =>
                                await objPower.GetNameAsync(token).ConfigureAwait(false) == ImprovedName && await objPower.GetExtraAsync(token).ConfigureAwait(false) == UniqueName, token).ConfigureAwait(false);
                            if (objImprovedPower != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objImprovedPower,
                                    nameof(Power.FreeLevels)));
                            }
                        }
                    }
                    break;

                case ImprovementType.AIProgram:
                    break;

                case ImprovementType.CritterPowerLevel:
                    break;

                case ImprovementType.CritterPower:
                    break;

                case ImprovementType.SpellResistance:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellResistance)));
                    }
                    break;

                case ImprovementType.LimitSpellCategory:
                    break;

                case ImprovementType.LimitSpellDescriptor:
                    break;

                case ImprovementType.LimitSpiritCategory:
                    break;

                case ImprovementType.WalkSpeed:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.WalkingRate)));
                    }
                    break;

                case ImprovementType.RunSpeed:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.RunningRate)));
                    }
                    break;

                case ImprovementType.SprintSpeed:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SprintingRate)));
                    }
                    break;

                case ImprovementType.WalkMultiplier:
                case ImprovementType.WalkMultiplierPercent:
                case ImprovementType.RunMultiplier:
                case ImprovementType.RunMultiplierPercent:
                case ImprovementType.SprintBonus:
                case ImprovementType.SprintBonusPercent:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CalculatedMovement)));
                    }
                    break;

                case ImprovementType.EssencePenalty:
                case ImprovementType.EssencePenaltyT100:
                case ImprovementType.EssencePenaltyMAGOnlyT100:
                case ImprovementType.EssencePenaltyRESOnlyT100:
                case ImprovementType.EssencePenaltyDEPOnlyT100:
                case ImprovementType.SpecialAttBurn:
                case ImprovementType.SpecialAttTotalBurnMultiplier:
                case ImprovementType.CyborgEssence:
                case ImprovementType.CyberwareEssCost:
                case ImprovementType.CyberwareTotalEssMultiplier:
                case ImprovementType.BiowareEssCost:
                case ImprovementType.BiowareTotalEssMultiplier:
                case ImprovementType.BasicBiowareEssCost:
                case ImprovementType.GenetechEssMultiplier:
                    // Immediately reset cached essence to make sure this fires off before any other property changers would
                    await _objCharacter.ResetCachedEssenceAsync(token).ConfigureAwait(false);
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                        nameof(Character.Essence)));
                    break;

                case ImprovementType.FreeSpellsATT:
                    break;

                case ImprovementType.FreeSpells:
                    break;

                case ImprovementType.DrainValue:
                    break;

                case ImprovementType.FadingValue:
                    break;

                case ImprovementType.Spell:
                    break;

                case ImprovementType.ComplexForm:
                    break;

                case ImprovementType.Gear:
                    break;

                case ImprovementType.Weapon:
                    break;

                case ImprovementType.MentorSpirit:
                    break;

                case ImprovementType.Paragon:
                    break;

                case ImprovementType.FreeSpellsSkill:
                    break;

                case ImprovementType.DisableSpecializationEffects:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.GetSpecializationBonus), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.GetSpecializationBonus), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.PhysiologicalAddictionFirstTime:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PhysiologicalAddictionResistFirstTime)));
                    }
                    break;

                case ImprovementType.PsychologicalAddictionFirstTime:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PsychologicalAddictionResistFirstTime)));
                    }
                    break;

                case ImprovementType.PhysiologicalAddictionAlreadyAddicted:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PhysiologicalAddictionResistAlreadyAddicted)));
                    }
                    break;

                case ImprovementType.PsychologicalAddictionAlreadyAddicted:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PsychologicalAddictionResistAlreadyAddicted)));
                    }
                    break;

                case ImprovementType.AddESStoStunCMRecovery:
                case ImprovementType.StunCMRecovery:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.StunCMNaturalRecovery)));
                    }
                    break;

                case ImprovementType.AddESStoPhysicalCMRecovery:
                case ImprovementType.PhysicalCMRecovery:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.PhysicalCMNaturalRecovery)));
                    }
                    break;

                case ImprovementType.MentalManipulationResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseManipulationMental)));
                    }
                    break;

                case ImprovementType.PhysicalManipulationResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseManipulationPhysical)));
                    }
                    break;

                case ImprovementType.ManaIllusionResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseIllusionMana)));
                    }
                    break;

                case ImprovementType.PhysicalIllusionResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseIllusionPhysical)));
                    }
                    break;

                case ImprovementType.DetectionSpellResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDetection)));
                    }
                    break;

                case ImprovementType.DirectManaSpellResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDirectSoakMana)));
                    }
                    break;

                case ImprovementType.DirectPhysicalSpellResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDirectSoakPhysical)));
                    }
                    break;

                case ImprovementType.DecreaseBODResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseBOD)));
                    }
                    break;

                case ImprovementType.DecreaseAGIResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseAGI)));
                    }
                    break;

                case ImprovementType.DecreaseREAResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseREA)));
                    }
                    break;

                case ImprovementType.DecreaseSTRResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseSTR)));
                    }
                    break;

                case ImprovementType.DecreaseCHAResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseCHA)));
                    }
                    break;

                case ImprovementType.DecreaseINTResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseINT)));
                    }
                    break;

                case ImprovementType.DecreaseLOGResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseLOG)));
                    }
                    break;

                case ImprovementType.DecreaseWILResist:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellDefenseDecreaseWIL)));
                    }
                    break;

                case ImprovementType.AddLimb:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.LimbCount)));
                    }
                    break;

                case ImprovementType.StreetCredMultiplier:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.CalculatedStreetCred)));
                    }
                    break;

                case ImprovementType.StreetCred:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.TotalStreetCred)));
                    }
                    break;

                case ImprovementType.AttributeKarmaCostMultiplier:
                case ImprovementType.AttributeKarmaCost:
                    {
                        foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                        {
                            if (string.IsNullOrEmpty(ImprovedName) || objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                    nameof(CharacterAttrib.UpgradeKarmaCost)));
                            }
                        }
                    }
                    break;

                case ImprovementType.ActiveSkillKarmaCost:
                case ImprovementType.ActiveSkillKarmaCostMultiplier:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.UpgradeKarmaCost), lstReturn, token).ConfigureAwait(false);
                    }
                    else
                    {
                        // When no specific target, process all skills
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.UpgradeKarmaCost)));
                        }, token).ConfigureAwait(false);
                    }
                }
                    break;

                case ImprovementType.KnowledgeSkillKarmaCost:
                case ImprovementType.KnowledgeSkillKarmaCostMinimum:
                case ImprovementType.KnowledgeSkillKarmaCostMultiplier:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.UpgradeKarmaCost), lstReturn, token).ConfigureAwait(false);
                        }
                        else
                        {
                            // When no specific target, process all knowledge skills
                            await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.UpgradeKarmaCost)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.SkillGroupKarmaCost:
                case ImprovementType.SkillGroupKarmaCostMultiplier:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            if (lstExtraImprovedName?.Count > 0)
                            {
                                await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(async objTargetGroup =>
                                {
                                    string strName = await objTargetGroup.GetNameAsync(token).ConfigureAwait(false);
                                    if (strName == ImprovedName ||
                                        lstExtraImprovedName.Contains(strName))
                                    {
                                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                            objTargetGroup,
                                            nameof(SkillGroup.UpgradeKarmaCost)));
                                    }
                                }, token).ConfigureAwait(false);
                            }
                            else
                            {
                                SkillGroup objTargetGroup =
                                    await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).FirstOrDefaultAsync(async x => await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                                if (objTargetGroup != null)
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                        nameof(SkillGroup.UpgradeKarmaCost)));
                                }
                            }
                        }
                        else
                        {
                            await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.UpgradeKarmaCost)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.SkillGroupDisable:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(async objTargetGroup =>
                            {
                                string strName = await objTargetGroup.GetNameAsync(token).ConfigureAwait(false);
                                if (strName == ImprovedName ||
                                    lstExtraImprovedName.Contains(strName))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                        nameof(SkillGroup.IsDisabled)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            SkillGroup objTargetGroup =
                                await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).FirstOrDefaultAsync(async x => await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                            if (objTargetGroup != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.IsDisabled)));
                            }
                        }

                        break;
                    }
                case ImprovementType.SkillDisable:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillEnableMovement:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled), lstReturn, token).ConfigureAwait(false);
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.Enabled), lstReturn, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillCategorySpecializationKarmaCost:
                case ImprovementType.SkillCategorySpecializationKarmaCostMultiplier:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.CanAffordSpecialization)));
                        }, token).ConfigureAwait(false);

                        await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.CanAffordSpecialization)));
                        }, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillCategoryKarmaCost:
                case ImprovementType.SkillCategoryKarmaCostMultiplier:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.UpgradeKarmaCost)));
                        }, token).ConfigureAwait(false);

                        await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.UpgradeKarmaCost)));
                        }, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.SkillGroupCategoryDisable:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.IsDisabled)));
                        }
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.SkillGroupCategoryKarmaCostMultiplier:
                case ImprovementType.SkillGroupCategoryKarmaCost:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.UpgradeKarmaCost)));
                        }
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.AttributePointCost:
                case ImprovementType.AttributePointCostMultiplier:
                {
                    foreach (CharacterAttrib objCharacterAttrib in await _objCharacter.GetAllAttributesAsync(token).ConfigureAwait(false))
                    {
                        if (string.IsNullOrEmpty(ImprovedName) || objCharacterAttrib.Abbrev == ImprovedName || lstExtraImprovedName?.Contains(objCharacterAttrib.Abbrev) == true)
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objCharacterAttrib,
                                nameof(CharacterAttrib.SpentPriorityPoints)));
                        }
                    }
                }
                    break;

                case ImprovementType.ActiveSkillPointCost:
                case ImprovementType.ActiveSkillPointCostMultiplier:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CurrentSpCost), lstReturn, token).ConfigureAwait(false);
                    }
                    else
                    {
                        // When no specific target, process all skills
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost)));
                        }, token).ConfigureAwait(false);
                    }
                }
                    break;

                case ImprovementType.SkillGroupPointCost:
                case ImprovementType.SkillGroupPointCostMultiplier:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(ImprovedName))
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(async objTargetGroup =>
                            {
                                string strName = await objTargetGroup.GetNameAsync(token).ConfigureAwait(false);
                                if (strName == ImprovedName ||
                                    lstExtraImprovedName.Contains(strName))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        objTargetGroup,
                                        nameof(SkillGroup.CurrentSpCost)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            SkillGroup objTargetGroup =
                                await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).FirstOrDefaultAsync(async x => await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                            if (objTargetGroup != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                    nameof(SkillGroup.CurrentSpCost)));
                            }
                        }
                    }
                    else
                    {
                        await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup,
                                nameof(SkillGroup.CurrentSpCost)));
                        }, token).ConfigureAwait(false);
                    }
                }
                    break;

                case ImprovementType.KnowledgeSkillPointCost:
                case ImprovementType.KnowledgeSkillPointCostMultiplier:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(KnowledgeSkill.CurrentSpCost), lstReturn, token).ConfigureAwait(false);
                        }
                        else
                        {
                            // When no specific target, process all knowledge skills
                            await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(KnowledgeSkill.CurrentSpCost)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.SkillCategoryPointCost:
                case ImprovementType.SkillCategoryPointCostMultiplier:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    // Keeping two enumerations separate helps avoid extra heap allocations
                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName ||
                            lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost)));
                    }, token).ConfigureAwait(false);

                    await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                    {
                        if (objTargetSkill.SkillCategory == ImprovedName ||
                            lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                nameof(Skill.CurrentSpCost)));
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.SkillGroupCategoryPointCost:
                case ImprovementType.SkillGroupCategoryPointCostMultiplier:
                {
                    SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                    await (await objSkillsSection.GetSkillGroupsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetGroup =>
                    {
                        if (objTargetGroup.GetRelevantSkillCategories.Contains(ImprovedName)
                            || (lstExtraImprovedName != null
                                && objTargetGroup.GetRelevantSkillCategories.Any(
                                    lstExtraImprovedName.Contains)))
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                objTargetGroup, nameof(SkillGroup.CurrentSpCost)));
                        }
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.NewSpellKarmaCost:
                case ImprovementType.NewSpellKarmaCostMultiplier:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpellKarmaCost)));
                    }
                    break;

                case ImprovementType.NewComplexFormKarmaCost:
                case ImprovementType.NewComplexFormKarmaCostMultiplier:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.ComplexFormKarmaCost)));
                    }
                    break;

                case ImprovementType.NewAIProgramKarmaCost:
                case ImprovementType.NewAIProgramKarmaCostMultiplier:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AIProgramKarmaCost)));
                    }
                    break;

                case ImprovementType.NewAIAdvancedProgramKarmaCost:
                case ImprovementType.NewAIAdvancedProgramKarmaCostMultiplier:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AIAdvancedProgramKarmaCost)));
                    }
                    break;

                case ImprovementType.BlockSkillSpecializations:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(ImprovedName))
                        {
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CanHaveSpecs), lstReturn, token).ConfigureAwait(false);
                            await ProcessSkillsWithPropertyComprehensiveAsync(await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false), ImprovedName, Target, lstExtraImprovedName, lstExtraTarget, nameof(Skill.CanHaveSpecs), lstReturn, token).ConfigureAwait(false);
                        }
                        else
                        {
                            // When no specific target, process all skills
                            await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.CanHaveSpecs)));
                            }, token).ConfigureAwait(false);
                        }
                    }
                    break;

                case ImprovementType.BlockSkillCategorySpecializations:
                    {
                        SkillsSection objSkillsSection = await _objCharacter.GetSkillsSectionAsync(token).ConfigureAwait(false);
                        // Keeping two enumerations separate helps avoid extra heap allocations
                        await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.CanHaveSpecs)));
                        }, token).ConfigureAwait(false);

                        await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false)).ForEachAsync(objTargetSkill =>
                        {
                            if (objTargetSkill.SkillCategory == ImprovedName ||
                                lstExtraImprovedName?.Contains(objTargetSkill.SkillCategory) == true)
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill,
                                    nameof(Skill.CanHaveSpecs)));
                        }, token).ConfigureAwait(false);
                    }
                    break;

                case ImprovementType.FocusBindingKarmaCost:
                    break;

                case ImprovementType.FocusBindingKarmaMultiplier:
                    break;

                case ImprovementType.MagiciansWayDiscount:
                {
                    await _objCharacter.Powers.ForEachAsync(async objLoopPower =>
                    {
                        if (await objLoopPower.GetAdeptWayDiscountAsync(token).ConfigureAwait(false) != 0)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objLoopPower,
                                nameof(Power.AdeptWayDiscountEnabled)));
                    }, token).ConfigureAwait(false);
                }
                    break;

                case ImprovementType.BurnoutsWay:
                    break;

                case ImprovementType.ContactForcedLoyalty:
                {
                    if (lstExtraImprovedName?.Count > 0)
                    {
                        await _objCharacter.Contacts.ForEachAsync(async objTargetContact =>
                        {
                            string strLoop = await objTargetContact.GetUniqueIdAsync(token).ConfigureAwait(false);
                            if (strLoop == ImprovedName || lstExtraImprovedName.Contains(strLoop))
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                    objTargetContact,
                                    nameof(Contact.ForcedLoyalty)));
                            }
                        }, token).ConfigureAwait(false);
                    }
                    else
                    {
                        Contact objTargetContact =
                            await _objCharacter.Contacts.FirstOrDefaultAsync(
                                async x => await x.GetUniqueIdAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                        if (objTargetContact != null)
                        {
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                nameof(Contact.ForcedLoyalty)));
                        }
                    }
                }
                    break;

                case ImprovementType.ContactMakeFree:
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await _objCharacter.Contacts.ForEachAsync(async objTargetContact =>
                            {
                                string strLoop = await objTargetContact.GetUniqueIdAsync(token).ConfigureAwait(false);
                                if (strLoop == ImprovedName || lstExtraImprovedName.Contains(strLoop))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(
                                        objTargetContact,
                                        nameof(Contact.Free)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Contact objTargetContact =
                                await _objCharacter.Contacts.FirstOrDefaultAsync(
                                    async x => await x.GetUniqueIdAsync(token).ConfigureAwait(false) == ImprovedName, token).ConfigureAwait(false);
                            if (objTargetContact != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetContact,
                                    nameof(Contact.Free)));
                            }
                        }
                    }
                    break;

                case ImprovementType.FreeWare:
                    break;

                case ImprovementType.WeaponSkillAccuracy:
                    break;

                case ImprovementType.WeaponAccuracy:
                    break;

                case ImprovementType.SpecialModificationLimit:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SpecialModificationLimit)));
                    }
                    break;

                case ImprovementType.MetageneticLimit:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.MetagenicLimit)));
                    }
                    break;

                case ImprovementType.DisableQuality:
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await _objCharacter.Qualities.ForEachAsync(async objQuality =>
                            {
                                string strName = await objQuality.GetNameAsync(token).ConfigureAwait(false);
                                string strSourceId = await objQuality.GetSourceIDStringAsync(token).ConfigureAwait(false);
                                if (strName == ImprovedName
                                    || string.Equals(strSourceId, ImprovedName, StringComparison.OrdinalIgnoreCase)
                                    || lstExtraImprovedName.Contains(strName)
                                    || lstExtraImprovedName.Contains(strSourceId))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                        nameof(Quality.Suppressed)));
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                        nameof(Character.Qualities)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Quality objQuality = await _objCharacter.Qualities.FirstOrDefaultAsync(async x =>
                                await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName || string.Equals(await x.GetSourceIDStringAsync(token).ConfigureAwait(false), ImprovedName, StringComparison.OrdinalIgnoreCase), token).ConfigureAwait(false);
                            if (objQuality != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.Suppressed)));
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                    nameof(Character.Qualities)));
                            }
                        }
                    }
                    break;

                case ImprovementType.FreeQuality:
                    {
                        if (lstExtraImprovedName?.Count > 0)
                        {
                            await _objCharacter.Qualities.ForEachAsync(async objQuality =>
                            {
                                string strName = await objQuality.GetNameAsync(token).ConfigureAwait(false);
                                string strSourceId = await objQuality.GetSourceIDStringAsync(token).ConfigureAwait(false);
                                if (strName == ImprovedName
                                    || string.Equals(strSourceId, ImprovedName, StringComparison.OrdinalIgnoreCase)
                                    || lstExtraImprovedName.Contains(strName)
                                    || lstExtraImprovedName.Contains(strSourceId))
                                {
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                        nameof(Quality.ContributeToBP)));
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                        nameof(Quality.ContributeToLimit)));
                                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                        nameof(Character.Qualities)));
                                }
                            }, token).ConfigureAwait(false);
                        }
                        else
                        {
                            Quality objQuality = await _objCharacter.Qualities.FirstOrDefaultAsync(async x =>
                                await x.GetNameAsync(token).ConfigureAwait(false) == ImprovedName || string.Equals(await x.GetSourceIDStringAsync(token).ConfigureAwait(false), ImprovedName, StringComparison.OrdinalIgnoreCase), token).ConfigureAwait(false);
                            if (objQuality != null)
                            {
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.ContributeToBP)));
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objQuality,
                                    nameof(Quality.ContributeToLimit)));
                                lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                    nameof(Character.Qualities)));
                            }
                        }
                    }
                    break;

                case ImprovementType.AllowSpriteFettering:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AllowSpriteFettering)));
                        break;
                    }
                case ImprovementType.Surprise:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.Surprise)));
                        break;
                    }
                case ImprovementType.AstralReputation:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.AstralReputation)));
                        break;
                    }
                case ImprovementType.AstralReputationWild:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.WildReputation)));
                        break;
                    }
                case ImprovementType.CyberadeptDaemon:
                    {
                        if (_objCharacter.Settings.SpecialKarmaCostBasedOnShownValue)
                            lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                                nameof(Character.CyberwareEssence)));
                        break;
                    }
                case ImprovementType.PenaltyFreeSustain:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.SustainingPenalty)));
                        break;
                    }
                case ImprovementType.QualityLevel:
                    {
                        lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(_objCharacter,
                            nameof(Character.HasAnyQualitiesWithQualityLevels)));
                        break;
                    }
            }

            return lstReturn;
        }

        #region UI Methods

        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsImprovement, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = CustomName,
                Tag = this,
                ContextMenuStrip = cmsImprovement,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };
            return objNode;
        }

        public Color PreferredColor
        {
            get
            {
                if (!string.IsNullOrEmpty(Notes))
                {
                    return !Enabled
                        ? ColorManager.GenerateCurrentModeDimmedColor(NotesColor)
                        : ColorManager.GenerateCurrentModeColor(NotesColor);
                }

                return !Enabled
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
            }
        }

        public async Task<Color> GetPreferredColorAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(await GetNotesAsync(token).ConfigureAwait(false)))
            {
                return !Enabled
                    ? ColorManager.GenerateCurrentModeDimmedColor(await GetNotesColorAsync(token).ConfigureAwait(false))
                    : ColorManager.GenerateCurrentModeColor(await GetNotesColorAsync(token).ConfigureAwait(false));
            }
            return !Enabled
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
        }

        #endregion UI Methods

        #endregion Methods

        public string InternalId => SourceName;

        /// <summary>
        /// Helper method to process skills with comprehensive target checking (sync overload)
        /// </summary>
        private static IEnumerable<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> ProcessSkillsWithPropertyComprehensive(
            IEnumerable<Skill> skills, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, string strPropertyName)
        {
            foreach (Skill objSkill in skills)
            {
                string strKey = objSkill.DictionaryKey;
                string strDisplayName = objSkill.CurrentDisplayName;

                // Check against ImprovedName
                if (strKey == strImprovedName || strImprovedName == objSkill.InternalId || strDisplayName == strImprovedName)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }

                // Check against Target
                if (strKey == strTarget || strTarget == objSkill.InternalId || strDisplayName == strTarget)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }

                // Check against lstExtraImprovedName
                if (lstExtraImprovedName != null
                    && (lstExtraImprovedName.Contains(strKey)
                        || lstExtraImprovedName.Contains(objSkill.InternalId)
                        || lstExtraImprovedName.Contains(strDisplayName)))
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }

                // Check against lstExtraTarget
                if (lstExtraTarget != null
                    && (lstExtraTarget.Contains(strKey)
                        || lstExtraTarget.Contains(objSkill.InternalId)
                        || lstExtraTarget.Contains(strDisplayName)))
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                }
            }
        }

        /// <summary>
        /// Helper method to process skills by group/category/attribute with comprehensive target checking
        /// </summary>
        private static IEnumerable<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> ProcessSkillsByPropertyComprehensive(
            IEnumerable<Skill> skills, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, 
            string strPropertyName, Func<Skill, string> propertySelector)
        {
            foreach (Skill objSkill in skills)
            {
                string strPropertyValue = propertySelector(objSkill);
                
                // Check against ImprovedName
                if (strPropertyValue == strImprovedName)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }
                
                // Check against Target
                if (strPropertyValue == strTarget)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }
                
                // Check against lstExtraImprovedName
                if (lstExtraImprovedName?.Contains(strPropertyValue) == true)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                    continue;
                }
                
                // Check against lstExtraTarget
                if (lstExtraTarget?.Contains(strPropertyValue) == true)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
                }
            }
        }

        /// <summary>
        /// Helper method to process all skills with a specific property name (unified sync/async)
        /// </summary>
        private static IEnumerable<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> ProcessAllSkillsWithProperty(
            IEnumerable<Skill> skills, string strPropertyName)
        {
            foreach (Skill objSkill in skills)
            {
                yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkill, strPropertyName);
            }
        }

        /// <summary>
        /// Helper method to process skills with comprehensive target checking (async version)
        /// </summary>
        /// <summary>
        /// Helper method to process skills with comprehensive target checking (async overload)
        /// </summary>
        private static Task ProcessSkillsWithPropertyComprehensiveAsync<T>(IAsyncEnumerable<T> skills, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, string strPropertyName, List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> lstReturn, CancellationToken token = default) where T : Skill
        {
            return skills.ForEachAsync(async objTargetSkill =>
            {
                string strKey = await objTargetSkill.GetDictionaryKeyAsync(token).ConfigureAwait(false);
                string strDisplayName = await objTargetSkill.GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                
                // Check against ImprovedName
                if (strKey == strImprovedName || strImprovedName == objTargetSkill.InternalId || strDisplayName == strImprovedName)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }
                
                // Check against Target
                if (strKey == strTarget || strTarget == objTargetSkill.InternalId || strDisplayName == strTarget)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }
                
                // Check against lstExtraImprovedName
                if (lstExtraImprovedName != null
                    && (lstExtraImprovedName.Contains(strKey)
                        || lstExtraImprovedName.Contains(objTargetSkill.InternalId)
                        || lstExtraImprovedName.Contains(strDisplayName)))
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }

                // Check against lstExtraTarget
                if (lstExtraTarget != null
                    && (lstExtraTarget.Contains(strKey)
                        || lstExtraTarget.Contains(objTargetSkill.InternalId)
                        || lstExtraTarget.Contains(strDisplayName)))
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                }
            }, token);
        }

        /// <summary>
        /// Helper method to process skills by group/category/attribute with comprehensive target checking (async version)
        /// </summary>
        private static Task ProcessSkillsByPropertyComprehensiveAsync<T>(IAsyncEnumerable<T> skills, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, 
            string strPropertyName, Func<T, string> propertySelector, List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> lstReturn, CancellationToken token = default) where T : Skill
        {
            return skills.ForEachAsync(objTargetSkill =>
            {
                string strPropertyValue = propertySelector(objTargetSkill);
                
                // Check against ImprovedName
                if (strPropertyValue == strImprovedName)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }
                
                // Check against Target
                if (strPropertyValue == strTarget)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }
                
                // Check against lstExtraImprovedName
                if (lstExtraImprovedName?.Contains(strPropertyValue) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }
                
                // Check against lstExtraTarget
                if (lstExtraTarget?.Contains(strPropertyValue) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                }
            }, token);
        }

        /// <summary>
        /// Helper method to process skills by group/category/attribute with comprehensive target checking (async version)
        /// </summary>
        private static Task ProcessSkillsByPropertyComprehensiveAsync<T>(IAsyncEnumerable<T> skills, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget,
            string strPropertyName, Func<T, Task<string>> propertySelector, List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> lstReturn, CancellationToken token = default) where T : Skill
        {
            return skills.ForEachAsync(async objTargetSkill =>
            {
                string strPropertyValue = await propertySelector(objTargetSkill).ConfigureAwait(false);

                // Check against ImprovedName
                if (strPropertyValue == strImprovedName)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }

                // Check against Target
                if (strPropertyValue == strTarget)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }

                // Check against lstExtraImprovedName
                if (lstExtraImprovedName?.Contains(strPropertyValue) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                    return;
                }

                // Check against lstExtraTarget
                if (lstExtraTarget?.Contains(strPropertyValue) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetSkill, strPropertyName));
                }
            }, token);
        }

        /// <summary>
        /// Helper method to process skill groups with comprehensive target checking (sync overload)
        /// </summary>
        private static IEnumerable<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> ProcessSkillsWithPropertyComprehensive(
            IEnumerable<SkillGroup> skillGroups, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, string strPropertyName)
        {
            foreach (SkillGroup objSkillGroup in skillGroups)
            {
                string strName = objSkillGroup.Name;

                // Check against ImprovedName
                if (strName == strImprovedName)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkillGroup, strPropertyName);
                    continue;
                }

                // Check against Target
                if (strName == strTarget)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkillGroup, strPropertyName);
                    continue;
                }

                // Check against lstExtraImprovedName
                if (lstExtraImprovedName?.Contains(strName) == true)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkillGroup, strPropertyName);
                    continue;
                }

                // Check against lstExtraTarget
                if (lstExtraTarget?.Contains(strName) == true)
                {
                    yield return new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objSkillGroup, strPropertyName);
                }
            }
        }

        /// <summary>
        /// Helper method to process skill groups with comprehensive target checking (async overload)
        /// </summary>
        private static Task ProcessSkillsWithPropertyComprehensiveAsync(
            IAsyncEnumerable<SkillGroup> skillGroups, string strImprovedName, string strTarget, IReadOnlyCollection<string> lstExtraImprovedName, IReadOnlyCollection<string> lstExtraTarget, 
            string strPropertyName, List<ValueTuple<INotifyMultiplePropertiesChangedAsync, string>> lstReturn, CancellationToken token = default)
        {
            return skillGroups.ForEachAsync(async objTargetGroup =>
            {
                string strName = await objTargetGroup.GetNameAsync(token).ConfigureAwait(false);

                // Check against ImprovedName
                if (strName == strImprovedName)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup, strPropertyName));
                    return;
                }

                // Check against Target
                if (strName == strTarget)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup, strPropertyName));
                    return;
                }

                // Check against lstExtraImprovedName
                if (lstExtraImprovedName?.Contains(strName) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup, strPropertyName));
                    return;
                }

                // Check against lstExtraTarget
                if (lstExtraTarget?.Contains(strName) == true)
                {
                    lstReturn.Add(new ValueTuple<INotifyMultiplePropertiesChangedAsync, string>(objTargetGroup, strPropertyName));
                }
            }, token);
        }
    }
}
