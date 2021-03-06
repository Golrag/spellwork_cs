using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DBFilesClient.NET;
using SpellWork.Database;
using SpellWork.DBC.Structures;
using SpellWork.Spell;

namespace SpellWork.DBC
{
    public static class DBC
    {
        public const string Version = "SpellWork 6.x.x (19000)";
        public const string DbcPath = @"dbc";
        public const uint MaxLevel  = 100;

        public const int MaxDbcLocale                 = 16;
        //public const int MaxEffectIndex               = 32;
        public const int SpellEntryForDetectLocale    = 1;

        public static DBCStorage<AreaGroupEntry> AreaGroup = new DBCStorage<AreaGroupEntry>();
        public static DBCStorage<AreaTableEntry> AreaTable = new DBCStorage<AreaTableEntry>();
        public static DBCStorage<gtSpellScalingEntry> gtSpellScaling = new DBCStorage<gtSpellScalingEntry>();
        public static DB2Storage<OverrideSpellDataEntry> OverrideSpellData = new DB2Storage<OverrideSpellDataEntry>();
        public static DBCStorage<ScreenEffectEntry> ScreenEffect = new DBCStorage<ScreenEffectEntry>();
        public static DBCStorage<SkillLineAbilityEntry> SkillLineAbility = new DBCStorage<SkillLineAbilityEntry>();
        public static DBCStorage<SkillLineEntry> SkillLine = new DBCStorage<SkillLineEntry>();
        public static DBCStorage<SpellEntry> Spell = new DBCStorage<SpellEntry>();
        public static DBCStorage<SpellAuraOptionsEntry> SpellAuraOptions = new DBCStorage<SpellAuraOptionsEntry>();
        public static DB2Storage<SpellAuraRestrictionsEntry> SpellAuraRestrictions = new DB2Storage<SpellAuraRestrictionsEntry>();
        public static DB2Storage<SpellCastingRequirementsEntry> SpellCastingRequirements = new DB2Storage<SpellCastingRequirementsEntry>();
        public static DBCStorage<SpellCastTimesEntry> SpellCastTimes = new DBCStorage<SpellCastTimesEntry>();
        public static DBCStorage<SpellCategoriesEntry> SpellCategories = new DBCStorage<SpellCategoriesEntry>();
        public static DB2Storage<SpellClassOptionsEntry> SpellClassOptions = new DB2Storage<SpellClassOptionsEntry>();
        public static DBCStorage<SpellCooldownsEntry> SpellCooldowns = new DBCStorage<SpellCooldownsEntry>();
        public static DBCStorage<SpellDescriptionVariablesEntry> SpellDescriptionVariables = new DBCStorage<SpellDescriptionVariablesEntry>();
        public static DBCStorage<SpellDurationEntry> SpellDuration = new DBCStorage<SpellDurationEntry>();
        public static DBCStorage<SpellEffectEntry> SpellEffect = new DBCStorage<SpellEffectEntry>();
        public static DBCStorage<SpellEffectScalingEntry> SpellEffectScaling = new DBCStorage<SpellEffectScalingEntry>();
        public static DB2Storage<SpellMiscEntry> SpellMisc = new DB2Storage<SpellMiscEntry>();
        public static DBCStorage<SpellEquippedItemsEntry> SpellEquippedItems = new DBCStorage<SpellEquippedItemsEntry>();
        public static DBCStorage<SpellInterruptsEntry> SpellInterrupts = new DBCStorage<SpellInterruptsEntry>();
        public static DBCStorage<SpellLevelsEntry> SpellLevels = new DBCStorage<SpellLevelsEntry>();
        public static DB2Storage<SpellPowerEntry> SpellPower = new DB2Storage<SpellPowerEntry>();
        //public static Dictionary<uint, List<SpellPowerEntry>> _spellPower = new Dictionary<uint, List<SpellPowerEntry>>();
        public static DBCStorage<SpellRadiusEntry> SpellRadius = new DBCStorage<SpellRadiusEntry>();
        public static DBCStorage<SpellRangeEntry> SpellRange = new DBCStorage<SpellRangeEntry>();
        public static DB2Storage<SpellRuneCostEntry> SpellRuneCost = new DB2Storage<SpellRuneCostEntry>();
        public static DBCStorage<SpellScalingEntry> SpellScaling = new DBCStorage<SpellScalingEntry>();
        public static DBCStorage<SpellShapeshiftEntry> SpellShapeshift = new DBCStorage<SpellShapeshiftEntry>();
        public static DBCStorage<SpellTargetRestrictionsEntry> SpellTargetRestrictions = new DBCStorage<SpellTargetRestrictionsEntry>();
        public static Dictionary<uint, List<SpellTargetRestrictionsEntry>> _spellTargetRestrictions = new Dictionary<uint, List<SpellTargetRestrictionsEntry>>();
        public static DB2Storage<SpellTotemsEntry> SpellTotems = new DB2Storage<SpellTotemsEntry>();

        public static DB2Storage<ItemEntry> Item = new DB2Storage<ItemEntry>();
        public static DB2Storage<SpellReagentsEntry> SpellReagents = new DB2Storage<SpellReagentsEntry>();
        public static DB2Storage<SpellMissileEntry> SpellMissile = new DB2Storage<SpellMissileEntry>();
        public static DB2Storage<SpellMissileMotionEntry> SpellMissileMotion = new DB2Storage<SpellMissileMotionEntry>();
        public static DB2Storage<SpellVisualEntry> SpellVisual = new DB2Storage<SpellVisualEntry>();

        [DataStoreFileName("Item-sparse")]
        public static DB2Storage<ItemSparseEntry> ItemSparse = new DB2Storage<ItemSparseEntry>();

        public static Dictionary<uint, SpellInfoHelper> SpellInfoStore = new Dictionary<uint, SpellInfoHelper>();
        public static Dictionary<uint, List<SpellEffectEntry>> SpellEffectLists = new Dictionary<uint, List<SpellEffectEntry>>();
        public static Dictionary<uint, List<uint>> SpellTriggerStore = new Dictionary<uint, List<uint>>();
        
        public static void Load()
        {
            foreach (var dbc in typeof(DBC).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (!dbc.FieldType.IsGenericType)
                    continue;

                string extension;
                if (dbc.FieldType.GetGenericTypeDefinition() == typeof(DBCStorage<>))
                    extension = "dbc";
                else if (dbc.FieldType.GetGenericTypeDefinition() == typeof(DB2Storage<>))
                    extension = "db2";
                else
                    continue;

                string name = dbc.Name;

                DataStoreFileNameAttribute[] attributes = dbc.GetCustomAttributes(typeof(DataStoreFileNameAttribute), false) as DataStoreFileNameAttribute[];
                if (attributes.Length == 1)
                    name = attributes[0].FileName;

                try
                {
                    using (var strm = new FileStream(String.Format("{0}\\{1}.{2}", DbcPath, name, extension), FileMode.Open))
                        dbc.FieldType.GetMethod("Load", new Type[] { typeof(FileStream) }).Invoke(dbc.GetValue(null), new object[] { strm });
                }
                catch (DirectoryNotFoundException)
                {
                    throw new DirectoryNotFoundException(String.Format("Could not open {0}.dbc!", dbc.Name));
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException is ArgumentException)
                        throw new ArgumentException(String.Format("Failed to load {0}.dbc: {1}", dbc.Name, tie.InnerException.Message));

                    throw;
                }
            }

            // this is to speedup spelleffect lookups
            foreach (var effect in SpellEffect)
            {
                uint EffectSpellId = effect.SpellId;
                uint effIdx = effect.Index;
                Difficulty diff = (Difficulty)effect.Difficulty;
                if (!SpellEffectLists.ContainsKey(EffectSpellId))
                {
                    List<SpellEffectEntry> temp = new List<SpellEffectEntry>();
                    SpellEffectLists.Add(EffectSpellId, temp);
                }
                SpellEffectLists[EffectSpellId].Add(effect);

                // triggered spell store
                uint triggerid = effect.TriggerSpell;
                if (DBC.SpellTriggerStore.ContainsKey(triggerid))
                {
                    DBC.SpellTriggerStore[triggerid].Add(EffectSpellId);
                }
                else
                {
                    List<uint> ids = new List<uint>();
                    ids.Add(EffectSpellId);
                    DBC.SpellTriggerStore.Add(triggerid, ids);
                }
            }

            /*foreach (var sp in DBC.SpellPower)
            {
                if (!DBC._spellPower.ContainsKey(sp.SpellId))
                {
                    List<SpellPowerEntry> spl = new List<SpellPowerEntry>();
                    DBC._spellPower.Add(sp.SpellId, spl);
                }
                DBC._spellPower[sp.SpellId].Add(sp);
            }
            DBC.SpellPower.Clear();*/

            foreach (var tr in DBC.SpellTargetRestrictions)
            {
                if (!DBC._spellTargetRestrictions.ContainsKey(tr.SpellId))
                {
                    List<SpellTargetRestrictionsEntry> str = new List<SpellTargetRestrictionsEntry>();
                    DBC._spellTargetRestrictions.Add(tr.SpellId, str);
                }
                DBC._spellTargetRestrictions[tr.SpellId].Add(tr);
            }
            DBC.SpellTargetRestrictions.Clear();

            foreach (var dbcInfo in Spell.Records)
                SpellInfoStore.Add(dbcInfo.Id, new SpellInfoHelper(dbcInfo));

            foreach (var item in ItemSparse)
            {
                ItemTemplate.Add(new Item
                {
                    Entry = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    /*SpellId = new[] 
                    {
                        item.SpellId[0],
                        item.SpellId[1],
                        item.SpellId[2],
                        item.SpellId[3],
                        item.SpellId[4]
                    }*/
                    SpellId = new[] { 0, 0, 0, 0, 0 }
                });
            }
        }

        // DB
        public static List<Item> ItemTemplate = new List<Item>();

        public static uint SelectedLevel = MaxLevel;
    }
}
