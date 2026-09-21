using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace WhoBuysThisResearch
{
    internal sealed class DiagnosticCollector
    {
        private readonly ManualLogSource _log;
        private readonly Type _gameBalanceType;
        private readonly Type _mainGameType;
        private readonly Type _worldMapType;
        private readonly Type _gjlType;

        internal DiagnosticCollector(ManualLogSource log)
        {
            _log = log;
            _gameBalanceType = ReflectionUtil.FindType("GameBalance");
            _mainGameType = ReflectionUtil.FindType("MainGame");
            _worldMapType = ReflectionUtil.FindType("WorldMap");
            _gjlType = ReflectionUtil.FindType("GJL");
        }

        internal bool IsRuntimeReady(out object mainGame, out object save, out object gameBalance, out IList worldObjects)
        {
            mainGame = null;
            save = null;
            gameBalance = null;
            worldObjects = null;

            object value;
            if (!ReflectionUtil.TryReadStatic(_mainGameType, "me", out value) || value == null)
                mainGame = ReflectionUtil.FindActiveUnityInstance(_mainGameType);
            else
                mainGame = value;
            if (mainGame == null) return false;

            object started;
            if (!ReflectionUtil.TryReadStatic(_mainGameType, "game_started", out started) || !(started is bool) || !(bool)started) return false;
            object starting;
            if (ReflectionUtil.TryReadStatic(_mainGameType, "game_starting", out starting) && starting is bool && (bool)starting) return false;

            if (!ReflectionUtil.TryRead(mainGame, "save", out save) || save == null) return false;
            object known;
            if (!ReflectionUtil.TryRead(save, "known_npcs", out known) || known == null) return false;

            if (!ReflectionUtil.TryReadStatic(_gameBalanceType, "me", out gameBalance) || gameBalance == null) return false;

            object objs;
            if (!ReflectionUtil.TryReadStatic(_worldMapType, "objs", out objs) || objs == null) return false;
            worldObjects = objs as IList;
            if (worldObjects == null || worldObjects.Count == 0) return false;
            return true;
        }

        internal ulong BuildKnownNpcFingerprint(object save, out int count)
        {
            count = 0;
            ulong hash = 14695981039346656037UL;
            object known;
            if (!ReflectionUtil.TryRead(save, "known_npcs", out known) || known == null) return hash;
            IEnumerable npcs = ReflectionUtil.EnumerateMember(known, "npcs");
            if (npcs == null) return hash;
            foreach (object npc in npcs)
            {
                string id = ReflectionUtil.ReadString(npc, "npc_id") ?? "";
                count++;
                for (int i = 0; i < id.Length; i++)
                {
                    hash ^= id[i];
                    hash *= 1099511628211UL;
                }
                hash ^= 255;
                hash *= 1099511628211UL;
            }
            hash ^= (ulong)count;
            hash *= 1099511628211UL;
            return hash;
        }

        internal bool Dump(string reason, out string reportPath)
        {
            reportPath = null;
            object mainGame;
            object save;
            object gameBalance;
            IList worldObjects;
            if (!IsRuntimeReady(out mainGame, out save, out gameBalance, out worldObjects)) return false;

            try
            {
                List<object> vendorDefinitions = ToList(ReflectionUtil.EnumerateMember(gameBalance, "vendors_data"));
                List<object> items = ToList(ReflectionUtil.EnumerateMember(gameBalance, "items_data"));
                HashSet<string> knownNpcIds = ReadKnownNpcIds(save);
                Dictionary<string, List<object>> wgosById = IndexWorldObjects(worldObjects);
                List<VendorSnapshot> vendors = BuildVendorSnapshots(vendorDefinitions, wgosById, knownNpcIds);

                string directory = Path.Combine(Paths.BepInExRootPath, "WhoBuysThisResearch");
                Directory.CreateDirectory(directory);
                reportPath = Path.Combine(directory, "latest-report.txt");
                string catalogPath = Path.Combine(directory, "vendor-catalog.tsv");
                string matrixPath = Path.Combine(directory, "buyer-matrix.tsv");

                MatrixSummary matrixSummary;
                string matrix = BuildBuyerMatrix(items, vendors, out matrixSummary);
                string report = BuildReport(reason, knownNpcIds, worldObjects.Count, items, vendors, matrixSummary);
                string catalog = BuildVendorCatalog(vendors);

                File.WriteAllText(reportPath, report, new UTF8Encoding(false));
                File.WriteAllText(catalogPath, catalog, new UTF8Encoding(false));
                File.WriteAllText(matrixPath, matrix, new UTF8Encoding(false));

                _log.LogInfo("WHO_BUYS_THIS_DIAGNOSTIC status=written reason=" + reason +
                             " vendors=" + vendors.Count.ToString(CultureInfo.InvariantCulture) +
                             " items=" + items.Count.ToString(CultureInfo.InvariantCulture) +
                             " known_npcs=" + knownNpcIds.Count.ToString(CultureInfo.InvariantCulture) +
                             " known_vendors=" + CountKnownVendors(vendors).ToString(CultureInfo.InvariantCulture) +
                             " report=\"" + reportPath + "\"");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError("WHO_BUYS_THIS_DIAGNOSTIC status=failed reason=" + reason + " error=" + ex);
                return false;
            }
        }

        private List<VendorSnapshot> BuildVendorSnapshots(List<object> vendorDefinitions, Dictionary<string, List<object>> wgosById, HashSet<string> knownNpcIds)
        {
            List<VendorSnapshot> result = new List<VendorSnapshot>();
            for (int i = 0; i < vendorDefinitions.Count; i++)
            {
                object def = vendorDefinitions[i];
                string id = ReflectionUtil.ReadString(def, "id") ?? "<missing-id>";
                VendorSnapshot vendor = new VendorSnapshot();
                vendor.Definition = def;
                vendor.Id = id;
                vendor.LocalizedName = Localize(id);
                vendor.StartTier = ReflectionUtil.ReadInt(def, "start_tire", -1);
                vendor.BaseProductTypes = ReflectionUtil.ReadStringList(def, "product_types");
                List<string> activeTypes;
                vendor.ActiveProductTypesResolved = TryInvokeStringList(def, "GetProductTypes", out activeTypes);
                vendor.ActiveProductTypes = activeTypes;
                if (!vendor.ActiveProductTypesResolved && vendor.BaseProductTypes.Count > 0)
                    vendor.ActiveProductTypes.AddRange(vendor.BaseProductTypes);
                vendor.AdditionalTypes = DescribeAdditionalTypes(def);
                vendor.NotBuying = DescribeNotBuying(def);

                List<object> wgos;
                if (!wgosById.TryGetValue(id, out wgos)) wgos = new List<object>();
                vendor.WorldObjects = wgos;
                vendor.KnownLookupId = id;
                vendor.ObjectType = "<none>";
                vendor.NpcAlias = "";
                vendor.NpcInList = false;
                if (wgos.Count > 0)
                {
                    object objDef;
                    if (ReflectionUtil.TryRead(wgos[0], "obj_def", out objDef) && objDef != null)
                    {
                        vendor.NpcAlias = ReflectionUtil.ReadString(objDef, "npc_alias") ?? "";
                        vendor.ObjectType = ReflectionUtil.SafeToString(ReadObject(objDef, "type"));
                        vendor.NpcInList = ReflectionUtil.ReadBool(objDef, "npc_in_list", false);
                        if (!string.IsNullOrEmpty(vendor.NpcAlias)) vendor.KnownLookupId = vendor.NpcAlias;
                    }
                    object backingVendor;
                    if (ReflectionUtil.TryRead(wgos[0], "_vendor", out backingVendor) && backingVendor != null)
                    {
                        vendor.LiveVendor = backingVendor;
                        vendor.LiveTier = ReflectionUtil.ReadInt(backingVendor, "cur_tier", -1);
                    }
                }
                vendor.IsKnown = knownNpcIds.Contains(vendor.KnownLookupId);
                result.Add(vendor);
            }
            return result;
        }

        private MatrixSummary ComputeParity(List<object> items, VendorSnapshot vendor)
        {
            MatrixSummary summary = new MatrixSummary();
            if (vendor.LiveVendor == null || vendor.LiveTier < 1) return summary;
            MethodInfo canBuy = FindCanBuyItemMethod(vendor.LiveVendor.GetType());
            if (canBuy == null) return summary;

            for (int i = 0; i < items.Count; i++)
            {
                object item = items[i];
                bool expected = CanBuyAtTier(vendor, item, vendor.LiveTier);
                bool actual;
                try { actual = (bool)canBuy.Invoke(vendor.LiveVendor, new object[] { item, true }); }
                catch { continue; }
                summary.ParityChecks++;
                if (actual != expected)
                {
                    summary.ParityMismatches++;
                    if (summary.ParityMismatchSamples.Count < 20)
                    {
                        summary.ParityMismatchSamples.Add(vendor.Id + "	" + (ReflectionUtil.ReadString(item, "id") ?? "<missing>") +
                                                                  "	tier=" + vendor.LiveTier.ToString(CultureInfo.InvariantCulture) +
                                                                  "	derived=" + expected + "	native=" + actual);
                    }
                }
            }
            return summary;
        }

        private string BuildBuyerMatrix(List<object> items, List<VendorSnapshot> vendors, out MatrixSummary summary)
        {
            summary = new MatrixSummary();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("item_id	product_tier	vendor_id	known_lookup_id	is_known	effective_tier	localized_name");
            Dictionary<string, string> qualitySignatures = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                object item = items[i];
                string itemId = ReflectionUtil.ReadString(item, "id") ?? "<missing-id>";
                int productTier = ReflectionUtil.ReadInt(item, "product_tier", -999);
                List<string> productTypes = ReflectionUtil.ReadStringList(item, "product_types");
                if (productTypes.Count > 0 && (productTier < 1 || productTier > 3))
                {
                    summary.TradeableItemsWithOddTier++;
                    if (summary.OddTierSamples.Count < 30) summary.OddTierSamples.Add(itemId + "=" + productTier.ToString(CultureInfo.InvariantCulture));
                }
                if (itemId.IndexOf(':') >= 0) summary.QualityLikeItemCount++;

                StringBuilder signature = new StringBuilder();
                bool anyBuyer = false;
                bool anyKnownBuyer = false;
                for (int v = 0; v < vendors.Count; v++)
                {
                    VendorSnapshot vendor = vendors[v];
                    int effectiveTier = FindEffectiveTier(vendor, item);
                    if (effectiveTier <= 0) continue;
                    anyBuyer = true;
                    if (vendor.IsKnown) anyKnownBuyer = true;
                    signature.Append(vendor.Id).Append(':').Append(effectiveTier).Append(';');
                    sb.Append(EscapeTsv(itemId)).Append('	')
                      .Append(productTier.ToString(CultureInfo.InvariantCulture)).Append('	')
                      .Append(EscapeTsv(vendor.Id)).Append('	')
                      .Append(EscapeTsv(vendor.KnownLookupId)).Append('	')
                      .Append(vendor.IsKnown ? "1" : "0").Append('	')
                      .Append(effectiveTier.ToString(CultureInfo.InvariantCulture)).Append('	')
                      .Append(EscapeTsv(vendor.LocalizedName)).AppendLine();
                }
                if (anyBuyer) summary.ItemsWithAnyBuyer++;
                if (anyKnownBuyer) summary.ItemsWithKnownBuyer++;

                string baseId = BaseItemId(itemId);
                string existingSignature;
                if (!qualitySignatures.TryGetValue(baseId, out existingSignature))
                    qualitySignatures[baseId] = signature.ToString();
                else if (!string.Equals(existingSignature, signature.ToString(), StringComparison.Ordinal) && itemId.IndexOf(':') >= 0)
                    summary.QualityGroupsWithBuyerDifferences.Add(baseId);
            }

            for (int v = 0; v < vendors.Count; v++)
            {
                VendorSnapshot vendor = vendors[v];
                if (vendor.AdditionalTypes.Count > 0) summary.VendorsWithAdditionalTypes++;
                if (!vendor.ActiveProductTypesResolved) summary.ProductTypeResolutionFailures++;
                if (vendor.WorldObjects.Count == 0) summary.VendorsWithoutWorldObject++;
                if (vendor.WorldObjects.Count > 1) summary.VendorsWithMultipleWorldObjects++;
                if (vendor.LocalizedName == vendor.Id || string.IsNullOrEmpty(vendor.LocalizedName)) summary.LocalizationAnomalies++;
                MatrixSummary parity = ComputeParity(items, vendor);
                summary.MergeParity(parity);
            }
            return sb.ToString();
        }

        private string BuildReport(string reason, HashSet<string> knownNpcIds, int worldObjectCount, List<object> items, List<VendorSnapshot> vendors, MatrixSummary summary)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("WHO BUYS THIS? RESEARCH DIAGNOSTIC");
            sb.AppendLine("reason=" + reason);
            sb.AppendLine("plugin_version=0.0.0-research");
            sb.AppendLine("target_game=Graveyard Keeper 1.407");
            sb.AppendLine("save_mutation=none");
            sb.AppendLine("vendor_instantiation=none");
            sb.AppendLine();
            sb.AppendLine("SUMMARY");
            sb.AppendLine("world_objects=" + worldObjectCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("known_npcs=" + knownNpcIds.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("vendor_definitions=" + vendors.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("known_vendors=" + CountKnownVendors(vendors).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("items=" + items.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("items_with_any_buyer=" + summary.ItemsWithAnyBuyer.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("items_with_known_buyer=" + summary.ItemsWithKnownBuyer.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("vendors_with_additional_types=" + summary.VendorsWithAdditionalTypes.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("vendor_product_type_resolution_failures=" + summary.ProductTypeResolutionFailures.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("vendors_without_world_object=" + summary.VendorsWithoutWorldObject.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("vendors_with_multiple_world_objects=" + summary.VendorsWithMultipleWorldObjects.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("localization_anomalies=" + summary.LocalizationAnomalies.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("tradeable_items_with_product_tier_outside_1_3=" + summary.TradeableItemsWithOddTier.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("quality_like_items=" + summary.QualityLikeItemCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("quality_groups_with_buyer_differences=" + summary.QualityGroupsWithBuyerDifferences.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("native_parity_checks=" + summary.ParityChecks.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("native_parity_mismatches=" + summary.ParityMismatches.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();

            sb.AppendLine("KNOWN_NPCS");
            List<string> known = new List<string>(knownNpcIds);
            known.Sort(StringComparer.Ordinal);
            for (int i = 0; i < known.Count; i++) sb.AppendLine(known[i]);
            sb.AppendLine();

            sb.AppendLine("ODD_TIER_SAMPLES");
            for (int i = 0; i < summary.OddTierSamples.Count; i++) sb.AppendLine(summary.OddTierSamples[i]);
            sb.AppendLine();

            sb.AppendLine("QUALITY_GROUPS_WITH_BUYER_DIFFERENCES");
            List<string> quality = new List<string>(summary.QualityGroupsWithBuyerDifferences);
            quality.Sort(StringComparer.Ordinal);
            for (int i = 0; i < quality.Count; i++) sb.AppendLine(quality[i]);
            sb.AppendLine();

            sb.AppendLine("NATIVE_PARITY_MISMATCH_SAMPLES");
            for (int i = 0; i < summary.ParityMismatchSamples.Count; i++) sb.AppendLine(summary.ParityMismatchSamples[i]);
            sb.AppendLine();

            sb.AppendLine("NOTES");
            sb.AppendLine("- Zero known merchants is a valid ready state and does not trigger structural rebuild/retry.");
            sb.AppendLine("- buyer-matrix.tsv contains all data-derived potential buyers; is_known is a separate cheap save-state filter.");
            sb.AppendLine("- vendor-catalog.tsv records vendor/WGO/known mapping and dynamic product-type evidence.");
            sb.AppendLine("- Native parity is checked only for Vendor instances that already existed naturally; the harness never accesses the lazy WorldGameObject.vendor property.");
            return sb.ToString();
        }

        private string BuildVendorCatalog(List<VendorSnapshot> vendors)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("vendor_id	localized_name	start_tier	wgo_count	object_type	npc_alias	known_lookup_id	is_known	npc_in_list	live_vendor_exists	live_tier	base_product_types	active_product_types	additional_types	not_buying");
            for (int i = 0; i < vendors.Count; i++)
            {
                VendorSnapshot v = vendors[i];
                sb.Append(EscapeTsv(v.Id)).Append('	')
                  .Append(EscapeTsv(v.LocalizedName)).Append('	')
                  .Append(v.StartTier.ToString(CultureInfo.InvariantCulture)).Append('	')
                  .Append(v.WorldObjects.Count.ToString(CultureInfo.InvariantCulture)).Append('	')
                  .Append(EscapeTsv(v.ObjectType)).Append('	')
                  .Append(EscapeTsv(v.NpcAlias)).Append('	')
                  .Append(EscapeTsv(v.KnownLookupId)).Append('	')
                  .Append(v.IsKnown ? "1" : "0").Append('	')
                  .Append(v.NpcInList ? "1" : "0").Append('	')
                  .Append(v.LiveVendor != null ? "1" : "0").Append('	')
                  .Append(v.LiveTier.ToString(CultureInfo.InvariantCulture)).Append('	')
                  .Append(EscapeTsv(string.Join(",", v.BaseProductTypes.ToArray()))).Append('	')
                  .Append(EscapeTsv(string.Join(",", v.ActiveProductTypes.ToArray()))).Append('	')
                  .Append(EscapeTsv(string.Join(" | ", v.AdditionalTypes.ToArray()))).Append('	')
                  .Append(EscapeTsv(string.Join(" | ", v.NotBuying.ToArray()))).AppendLine();
            }
            return sb.ToString();
        }

        private int FindEffectiveTier(VendorSnapshot vendor, object item)
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                if (CanBuyAtTier(vendor, item, tier)) return tier;
            }
            return 0;
        }

        private bool CanBuyAtTier(VendorSnapshot vendor, object item, int tier)
        {
            if (item == null) return false;
            List<string> itemTypes = ReflectionUtil.ReadStringList(item, "product_types");
            if (itemTypes.Count == 0) return false;
            int itemTier = ReflectionUtil.ReadInt(item, "product_tier", int.MaxValue);
            if (itemTier > tier) return false;
            bool typeMatch = false;
            for (int i = 0; i < itemTypes.Count && !typeMatch; i++)
            {
                for (int p = 0; p < vendor.ActiveProductTypes.Count; p++)
                {
                    if (string.Equals(itemTypes[i], vendor.ActiveProductTypes[p], StringComparison.Ordinal))
                    {
                        typeMatch = true;
                        break;
                    }
                }
            }
            if (!typeMatch) return false;

            string itemId = ReflectionUtil.ReadString(item, "id") ?? "";
            IEnumerable notBuying = ReflectionUtil.EnumerateMember(vendor.Definition, "not_buying");
            if (notBuying != null)
            {
                foreach (object modifier in notBuying)
                {
                    string name = ReflectionUtil.ReadString(modifier, "item_name") ?? "";
                    if (!string.Equals(name, itemId, StringComparison.Ordinal)) continue;
                    int modifierTier = ReflectionUtil.ReadInt(modifier, "tier", int.MinValue);
                    if (modifierTier < 1 || modifierTier == tier) return false;
                }
            }
            return true;
        }

        private List<string> DescribeAdditionalTypes(object vendorDef)
        {
            List<string> result = new List<string>();
            IEnumerable values = ReflectionUtil.EnumerateMember(vendorDef, "additional_types");
            if (values == null) return result;
            foreach (object value in values)
            {
                string name = ReflectionUtil.ReadString(value, "name") ?? "<unnamed>";
                object expression;
                string raw = "<unavailable>";
                if (ReflectionUtil.TryRead(value, "expression", out expression) && expression != null)
                {
                    object rawValue = ReflectionUtil.Invoke(expression, "GetRawExpressionString", new object[0]);
                    if (rawValue != null) raw = Convert.ToString(rawValue);
                    else raw = ReflectionUtil.SafeToString(expression);
                }
                result.Add(name + " => " + raw);
            }
            return result;
        }

        private List<string> DescribeNotBuying(object vendorDef)
        {
            List<string> result = new List<string>();
            IEnumerable values = ReflectionUtil.EnumerateMember(vendorDef, "not_buying");
            if (values == null) return result;
            foreach (object value in values)
            {
                result.Add((ReflectionUtil.ReadString(value, "item_name") ?? "<missing>") +
                           "@" + ReflectionUtil.ReadInt(value, "tier", int.MinValue).ToString(CultureInfo.InvariantCulture));
            }
            return result;
        }

        private bool TryInvokeStringList(object owner, string methodName, out List<string> result)
        {
            result = new List<string>();
            if (owner == null) return false;
            MethodInfo method = ReflectionUtil.FindMethod(owner.GetType(), methodName, 0, false);
            if (method == null) return false;
            object value;
            try { value = method.Invoke(owner, null); }
            catch { return false; }
            IEnumerable values = value as IEnumerable;
            if (values == null) return false;
            foreach (object item in values)
            {
                if (item != null) result.Add(Convert.ToString(item));
            }
            return true;
        }

        private string Localize(string id)
        {
            if (_gjlType == null) return "<GJL-unavailable>";
            MethodInfo[] methods = _gjlType.GetMethods(ReflectionUtil.AnyStatic);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "L" || method.ReturnType != typeof(string)) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(string)) continue;
                    if (pass == 0 && parameters.Length != 1) continue;
                    object[] args = new object[parameters.Length];
                    args[0] = id;
                    bool supported = true;
                    for (int p = 1; p < parameters.Length; p++)
                    {
                        if (Attribute.IsDefined(parameters[p], typeof(ParamArrayAttribute)))
                        {
                            Type elementType = parameters[p].ParameterType.GetElementType();
                            args[p] = Array.CreateInstance(elementType ?? typeof(object), 0);
                        }
                        else if (parameters[p].IsOptional)
                        {
                            args[p] = Type.Missing;
                        }
                        else
                        {
                            supported = false;
                            break;
                        }
                    }
                    if (!supported) continue;
                    try { return (string)method.Invoke(null, args); }
                    catch { }
                }
            }
            return "<GJL.L-compatible-overload-missing>";
        }

        private Dictionary<string, List<object>> IndexWorldObjects(IList worldObjects)
        {
            Dictionary<string, List<object>> result = new Dictionary<string, List<object>>(StringComparer.Ordinal);
            for (int i = 0; i < worldObjects.Count; i++)
            {
                object wgo = worldObjects[i];
                string id = ReflectionUtil.ReadString(wgo, "obj_id");
                if (string.IsNullOrEmpty(id)) continue;
                List<object> list;
                if (!result.TryGetValue(id, out list))
                {
                    list = new List<object>();
                    result.Add(id, list);
                }
                list.Add(wgo);
            }
            return result;
        }

        private HashSet<string> ReadKnownNpcIds(object save)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            object known;
            if (!ReflectionUtil.TryRead(save, "known_npcs", out known) || known == null) return result;
            IEnumerable npcs = ReflectionUtil.EnumerateMember(known, "npcs");
            if (npcs == null) return result;
            foreach (object npc in npcs)
            {
                string id = ReflectionUtil.ReadString(npc, "npc_id");
                if (!string.IsNullOrEmpty(id)) result.Add(id);
            }
            return result;
        }

        private static MethodInfo FindCanBuyItemMethod(Type vendorType)
        {
            MethodInfo[] methods = vendorType.GetMethods(ReflectionUtil.AnyInstance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "CanBuyItem" || method.ReturnType != typeof(bool)) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[1].ParameterType != typeof(bool)) continue;
                if (parameters[0].ParameterType.Name == "ItemDefinition") return method;
            }
            return null;
        }

        private static object ReadObject(object owner, string name)
        {
            object value;
            return ReflectionUtil.TryRead(owner, name, out value) ? value : null;
        }

        private static List<object> ToList(IEnumerable values)
        {
            List<object> result = new List<object>();
            if (values == null) return result;
            foreach (object value in values) result.Add(value);
            return result;
        }

        private static int CountKnownVendors(List<VendorSnapshot> vendors)
        {
            int count = 0;
            for (int i = 0; i < vendors.Count; i++) if (vendors[i].IsKnown) count++;
            return count;
        }

        private static string BaseItemId(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            int index = id.LastIndexOf(':');
            return index < 0 ? id : id.Substring(0, index);
        }

        private static string EscapeTsv(string value)
        {
            if (value == null) return "";
            return value.Replace((char)9, ' ').Replace((char)13, ' ').Replace((char)10, ' ');
        }

        private sealed class VendorSnapshot
        {
            internal object Definition;
            internal string Id;
            internal string LocalizedName;
            internal int StartTier;
            internal List<string> BaseProductTypes;
            internal List<string> ActiveProductTypes;
            internal bool ActiveProductTypesResolved;
            internal List<string> AdditionalTypes;
            internal List<string> NotBuying;
            internal List<object> WorldObjects;
            internal string ObjectType;
            internal string NpcAlias;
            internal bool NpcInList;
            internal string KnownLookupId;
            internal bool IsKnown;
            internal object LiveVendor;
            internal int LiveTier = -1;
        }

        private sealed class MatrixSummary
        {
            internal int ItemsWithAnyBuyer;
            internal int ItemsWithKnownBuyer;
            internal int VendorsWithAdditionalTypes;
            internal int ProductTypeResolutionFailures;
            internal int VendorsWithoutWorldObject;
            internal int VendorsWithMultipleWorldObjects;
            internal int LocalizationAnomalies;
            internal int TradeableItemsWithOddTier;
            internal int QualityLikeItemCount;
            internal int ParityChecks;
            internal int ParityMismatches;
            internal readonly List<string> OddTierSamples = new List<string>();
            internal readonly HashSet<string> QualityGroupsWithBuyerDifferences = new HashSet<string>(StringComparer.Ordinal);
            internal readonly List<string> ParityMismatchSamples = new List<string>();

            internal void MergeParity(MatrixSummary other)
            {
                ParityChecks += other.ParityChecks;
                ParityMismatches += other.ParityMismatches;
                for (int i = 0; i < other.ParityMismatchSamples.Count && ParityMismatchSamples.Count < 20; i++)
                    ParityMismatchSamples.Add(other.ParityMismatchSamples[i]);
            }
        }
    }
}
