using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WhoBuysThis
{
    internal static class BuyerIndex
    {
        private static readonly Dictionary<string, List<BuyerEntry>> Index =
            new Dictionary<string, List<BuyerEntry>>(StringComparer.Ordinal);
        private static bool _ready;

        internal static void Clear()
        {
            Index.Clear();
            _ready = false;
        }

        internal static void Build()
        {
            Clear();
            IList items = GameApi.GetItems();
            IList vendorDefs = GameApi.GetVendors();
            IList worldObjects = GameApi.GetWorldObjects();
            if (items == null || vendorDefs == null || worldObjects == null)
                throw new InvalidOperationException("GameBalance or WorldMap data is not ready.");

            Dictionary<string, object> worldById = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (object wgo in worldObjects)
            {
                if (wgo == null) continue;
                string id = GameApi.WgoObjIdField.GetValue(wgo) as string;
                if (!string.IsNullOrEmpty(id) && !worldById.ContainsKey(id))
                    worldById.Add(id, wgo);
            }

            List<VendorMember> members = new List<VendorMember>();
            int order = 0;
            foreach (object vendor in vendorDefs)
            {
                string id = GameApi.GetId(vendor);
                if (!string.IsNullOrEmpty(id))
                    members.Add(CreateMember(vendor, id, order));
                order++;
            }

            List<VendorConcept> concepts = BuildConcepts(members, worldById);
            int entryCount = 0;

            foreach (object item in items)
            {
                if (item == null) continue;
                string itemId = GameApi.GetId(item);
                int itemTier = Convert.ToInt32(GameApi.ItemProductTierField.GetValue(item));
                HashSet<string> itemTypes = ReadStringSet(GameApi.ItemProductTypesField.GetValue(item) as IList);
                if (string.IsNullOrEmpty(itemId) || itemTier < 1 || itemTier > 3 || itemTypes.Count == 0)
                    continue;

                List<BuyerEntry> entries = new List<BuyerEntry>();
                foreach (VendorConcept concept in concepts)
                {
                    BuyerEntry entry = BuildEntry(concept, itemId, itemTier, itemTypes);
                    if (entry != null)
                    {
                        entries.Add(entry);
                        entryCount++;
                    }
                }

                if (entries.Count > 0)
                {
                    entries.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
                    Index[itemId] = entries;
                }
            }

            _ready = true;
            Plugin.Log.LogInfo("Buyer index ready: items=" + Index.Count +
                               ", buyer_entries=" + entryCount +
                               ", vendor_concepts=" + concepts.Count + ".");
        }

        internal static void AppendTooltip(object item, IList result)
        {
            if (!_ready) return;
            string itemId = GameApi.GetId(item);
            List<BuyerEntry> entries;
            if (string.IsNullOrEmpty(itemId) || !Index.TryGetValue(itemId, out entries)) return;

            List<string> visible = new List<string>();
            foreach (BuyerEntry entry in entries)
            {
                if (entry.IsStaged)
                {
                    if (!GameApi.AnyWorldObjectExists(entry.StageMemberIds)) continue;
                }
                else if (!GameApi.IsKnownNpc(entry.KnownNpcId))
                {
                    continue;
                }

                int tier = GetRequiredTierNow(entry);
                if (tier < 1) continue;

                string name = GameApi.Localize(entry.LocalizationId);
                if (string.IsNullOrEmpty(name) || name == entry.LocalizationId) continue;
                visible.Add(name + "\u00A0" + FormatTier(tier));
            }

            if (visible.Count == 0) return;

            string text = GameApi.GetBuyersLabel() + ": " + string.Join(", ", visible.ToArray());
            result.Add(GameApi.CreateSeparator());
            result.Add(GameApi.CreateTinyText(text));
        }

        private static BuyerEntry BuildEntry(VendorConcept concept, string itemId, int itemTier, HashSet<string> itemTypes)
        {
            List<BuyerVariant> variants = new List<BuyerVariant>();
            foreach (VendorMember member in concept.Members)
            {
                bool baseMatch = Overlaps(member.BaseTypes, itemTypes);
                if (!Overlaps(member.AllTypes, itemTypes)) continue;

                int tier = concept.IsStaged
                    ? GetStagedTier(member, itemId, itemTier)
                    : GetOrdinaryTier(member, itemId, itemTier);
                if (tier < 1) continue;

                variants.Add(new BuyerVariant
                {
                    VendorDefinition = member.Definition,
                    RequiredTier = tier,
                    NeedsDynamicCheck = !baseMatch,
                    ItemProductTypes = itemTypes
                });
            }

            if (variants.Count == 0) return null;
            return new BuyerEntry
            {
                IsStaged = concept.IsStaged,
                KnownNpcId = concept.KnownNpcId,
                LocalizationId = concept.LocalizationId,
                StageMemberIds = concept.Members.Select(m => m.Id).ToArray(),
                DisplayOrder = concept.DisplayOrder,
                Variants = variants
            };
        }

        private static int GetRequiredTierNow(BuyerEntry entry)
        {
            int best = int.MaxValue;
            foreach (BuyerVariant variant in entry.Variants)
            {
                if (variant.NeedsDynamicCheck)
                {
                    HashSet<string> active = GameApi.GetActiveProductTypes(variant.VendorDefinition);
                    if (!Overlaps(active, variant.ItemProductTypes)) continue;
                }
                if (variant.RequiredTier < best) best = variant.RequiredTier;
            }
            return best == int.MaxValue ? -1 : best;
        }

        private static int GetOrdinaryTier(VendorMember member, string itemId, int itemTier)
        {
            int first = Math.Max(1, Math.Max(itemTier, member.StartTier));
            for (int tier = first; tier <= 3; tier++)
                if (CanBuyAtTier(member, itemId, tier)) return tier;
            return -1;
        }

        private static int GetStagedTier(VendorMember member, string itemId, int itemTier)
        {
            int tier = member.StartTier;
            if (tier < 1 || tier > 3 || itemTier > tier) return -1;
            return CanBuyAtTier(member, itemId, tier) ? tier : -1;
        }

        private static bool CanBuyAtTier(VendorMember member, string itemId, int tier)
        {
            foreach (ItemBlock block in member.NotBuying)
            {
                if (!string.Equals(block.ItemId, itemId, StringComparison.Ordinal)) continue;
                if (block.Tier < 1 || block.Tier == tier) return false;
            }
            return true;
        }

        private static List<VendorConcept> BuildConcepts(
            List<VendorMember> members,
            Dictionary<string, object> worldById)
        {
            Regex stageRegex = new Regex("^(.*)_([123])$", RegexOptions.CultureInvariant);
            Dictionary<string, List<VendorMember>> candidates =
                new Dictionary<string, List<VendorMember>>(StringComparer.Ordinal);

            foreach (VendorMember member in members)
            {
                Match match = stageRegex.Match(member.Id);
                if (!match.Success) continue;
                int suffix = int.Parse(match.Groups[2].Value);
                if (member.StartTier != suffix) continue;

                List<VendorMember> group;
                string baseId = match.Groups[1].Value;
                if (!candidates.TryGetValue(baseId, out group))
                {
                    group = new List<VendorMember>();
                    candidates.Add(baseId, group);
                }
                group.Add(member);
            }

            HashSet<string> stagedIds = new HashSet<string>(StringComparer.Ordinal);
            List<VendorConcept> result = new List<VendorConcept>();

            foreach (List<VendorMember> group in candidates.Values)
            {
                if (group.Count != 3 ||
                    !group.Any(m => m.StartTier == 1) ||
                    !group.Any(m => m.StartTier == 2) ||
                    !group.Any(m => m.StartTier == 3))
                    continue;

                group.Sort((a, b) => a.StartTier.CompareTo(b.StartTier));
                string displayName = GameApi.Localize(group[0].Id);
                if (string.IsNullOrEmpty(displayName) || displayName == group[0].Id) continue;
                if (!group.All(m => string.Equals(GameApi.Localize(m.Id), displayName, StringComparison.Ordinal)))
                    continue;

                foreach (VendorMember member in group) stagedIds.Add(member.Id);
                result.Add(new VendorConcept
                {
                    IsStaged = true,
                    LocalizationId = group[0].Id,
                    DisplayOrder = group.Min(m => m.Order),
                    Members = group
                });
            }

            foreach (VendorMember member in members)
            {
                if (stagedIds.Contains(member.Id)) continue;
                object wgo;
                if (!worldById.TryGetValue(member.Id, out wgo)) continue;

                result.Add(new VendorConcept
                {
                    IsStaged = false,
                    LocalizationId = member.Id,
                    KnownNpcId = GameApi.GetKnownLookupIdForWgo(wgo, member.Id),
                    DisplayOrder = member.Order,
                    Members = new List<VendorMember> { member }
                });
            }

            result.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
            return result;
        }

        private static VendorMember CreateMember(object definition, string id, int order)
        {
            HashSet<string> baseTypes = ReadStringSet(GameApi.VendorBaseProductTypesField.GetValue(definition) as IList);
            HashSet<string> allTypes = new HashSet<string>(baseTypes, StringComparer.Ordinal);

            IList extras = GameApi.VendorAdditionalTypesField.GetValue(definition) as IList;
            if (extras != null)
            {
                foreach (object extra in extras)
                {
                    if (extra == null) continue;
                    string name = GameApi.ExpressionResNameField.GetValue(extra) as string;
                    if (!string.IsNullOrEmpty(name)) allTypes.Add(name);
                }
            }

            List<ItemBlock> blocked = new List<ItemBlock>();
            IList modifiers = GameApi.VendorNotBuyingField.GetValue(definition) as IList;
            if (modifiers != null)
            {
                foreach (object modifier in modifiers)
                {
                    if (modifier == null) continue;
                    blocked.Add(new ItemBlock
                    {
                        ItemId = GameApi.ItemModifierItemNameField.GetValue(modifier) as string,
                        Tier = Convert.ToInt32(GameApi.ItemModifierTierField.GetValue(modifier))
                    });
                }
            }

            return new VendorMember
            {
                Definition = definition,
                Id = id,
                Order = order,
                StartTier = Convert.ToInt32(GameApi.VendorStartTierField.GetValue(definition)),
                BaseTypes = baseTypes,
                AllTypes = allTypes,
                NotBuying = blocked
            };
        }

        private static HashSet<string> ReadStringSet(IList list)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            if (list == null) return result;
            foreach (object value in list)
            {
                string s = value as string;
                if (!string.IsNullOrEmpty(s)) result.Add(s);
            }
            return result;
        }

        private static bool Overlaps(HashSet<string> left, HashSet<string> right)
        {
            if (left.Count > right.Count)
            {
                HashSet<string> swap = left;
                left = right;
                right = swap;
            }
            foreach (string value in left)
                if (right.Contains(value)) return true;
            return false;
        }

        private static string FormatTier(int tier)
        {
            if (tier == 1) return "I";
            if (tier == 2) return "II";
            if (tier == 3) return "III";
            return tier.ToString();
        }

        private sealed class VendorConcept
        {
            internal bool IsStaged;
            internal string KnownNpcId;
            internal string LocalizationId;
            internal int DisplayOrder;
            internal List<VendorMember> Members;
        }

        private sealed class VendorMember
        {
            internal object Definition;
            internal string Id;
            internal int Order;
            internal int StartTier;
            internal HashSet<string> BaseTypes;
            internal HashSet<string> AllTypes;
            internal List<ItemBlock> NotBuying;
        }

        private sealed class BuyerEntry
        {
            internal bool IsStaged;
            internal string KnownNpcId;
            internal string LocalizationId;
            internal string[] StageMemberIds;
            internal int DisplayOrder;
            internal List<BuyerVariant> Variants;
        }

        private sealed class BuyerVariant
        {
            internal object VendorDefinition;
            internal int RequiredTier;
            internal bool NeedsDynamicCheck;
            internal HashSet<string> ItemProductTypes;
        }

        private sealed class ItemBlock
        {
            internal string ItemId;
            internal int Tier;
        }
    }
}
