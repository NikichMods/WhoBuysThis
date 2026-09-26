using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WhoBuysThis
{
    internal static class GameApi
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Type MainGameType;
        internal static Type ItemDefinitionType;

        private static Type _gameBalanceType;
        private static Type _worldMapType;
        private static Type _worldGameObjectType;
        private static Type _vendorDefinitionType;
        private static Type _knownNpcListType;
        private static Type _gjlType;
        private static Type _gameSettingsType;
        private static Type _bubbleTextType;
        private static Type _bubbleSeparatorType;

        private static MemberInfo _mainGameMe;
        private static MemberInfo _mainGameSave;
        private static MemberInfo _saveKnownNpcs;
        private static MemberInfo _gameBalanceMe;

        internal static FieldInfo BalanceItemsField;
        internal static FieldInfo BalanceVendorsField;
        internal static FieldInfo IdField;
        internal static FieldInfo ItemProductTierField;
        internal static FieldInfo ItemProductTypesField;
        internal static FieldInfo VendorStartTierField;
        internal static FieldInfo VendorBaseProductTypesField;
        internal static FieldInfo VendorAdditionalTypesField;
        internal static FieldInfo VendorNotBuyingField;
        internal static FieldInfo ExpressionResNameField;
        internal static FieldInfo ItemModifierItemNameField;
        internal static FieldInfo ItemModifierTierField;
        internal static FieldInfo WgoObjIdField;
        internal static FieldInfo WgoObjDefField;
        internal static FieldInfo ObjectNpcAliasField;

        private static PropertyInfo _worldObjectsProperty;
        private static MethodInfo _worldGetByObjId;
        private static MethodInfo _vendorGetProductTypes;
        private static MethodInfo _knownNpcGet;
        private static MethodInfo _getCurrentLanguage;
        private static MethodInfo _localize;
        private static ConstructorInfo _textCtor;
        private static object _tinyStyle;
        private static object _centerAlignment;

        internal static void Bind()
        {
            MainGameType = RequireType("MainGame");
            _gameBalanceType = RequireType("GameBalance");
            ItemDefinitionType = RequireType("ItemDefinition");
            _vendorDefinitionType = RequireType("VendorDefinition");
            _worldMapType = RequireType("WorldMap");
            _worldGameObjectType = RequireType("WorldGameObject");
            _knownNpcListType = RequireType("KnownNPCList");
            _gjlType = RequireType("GJL");
            _gameSettingsType = RequireType("GameSettings");
            _bubbleTextType = RequireType("BubbleWidgetTextData");
            _bubbleSeparatorType = RequireType("BubbleWidgetSeparatorData");

            _mainGameMe = RequireMember(MainGameType, "me", AnyStatic);
            _mainGameSave = RequireMember(MainGameType, "save", AnyInstance);
            _saveKnownNpcs = RequireMember(GetMemberType(_mainGameSave), "known_npcs", AnyInstance);
            _gameBalanceMe = RequireMember(_gameBalanceType, "me", AnyStatic);

            BalanceItemsField = RequireField(_gameBalanceType, "items_data");
            BalanceVendorsField = RequireField(_gameBalanceType, "vendors_data");
            IdField = RequireField(RequireType("BalanceBaseObject"), "id");
            ItemProductTierField = RequireField(ItemDefinitionType, "product_tier");
            ItemProductTypesField = RequireField(ItemDefinitionType, "product_types");

            VendorStartTierField = RequireField(_vendorDefinitionType, "start_tire");
            VendorBaseProductTypesField = RequireField(_vendorDefinitionType, "product_types");
            VendorAdditionalTypesField = RequireField(_vendorDefinitionType, "additional_types");
            VendorNotBuyingField = RequireField(_vendorDefinitionType, "not_buying");
            _vendorGetProductTypes = RequireMethod(_vendorDefinitionType, "GetProductTypes", Type.EmptyTypes);

            ExpressionResNameField = RequireField(RequireType("ExpressionRes"), "name");
            Type modifierType = VendorNotBuyingField.FieldType.GetGenericArguments()[0];
            ItemModifierItemNameField = RequireField(modifierType, "item_name");
            ItemModifierTierField = RequireField(modifierType, "tier");

            WgoObjIdField = RequireField(_worldGameObjectType, "obj_id");
            WgoObjDefField = RequireField(_worldGameObjectType, "obj_def");
            ObjectNpcAliasField = RequireField(RequireType("ObjectDefinition"), "npc_alias");

            _worldObjectsProperty = _worldMapType.GetProperty("objs", AnyStatic);
            if (_worldObjectsProperty == null)
                throw new MissingMemberException("WorldMap.objs");

            _worldGetByObjId = _worldMapType.GetMethod(
                "GetWorldGameObjectByObjId",
                AnyStatic,
                null,
                new[] { typeof(string), typeof(bool) },
                null);
            if (_worldGetByObjId == null)
                throw new MissingMethodException("WorldMap.GetWorldGameObjectByObjId(string,bool)");

            _knownNpcGet = RequireMethod(_knownNpcListType, "GetNPC", new[] { typeof(string) });
            _getCurrentLanguage = RequireMethod(_gameSettingsType, "GetCurrentLanguage", Type.EmptyTypes);
            _localize = FindLocalizationMethod();
            BindTooltipCtor();
        }

        internal static IList GetItems()
        {
            object balance = GetMemberValue(_gameBalanceMe, null);
            return balance == null ? null : BalanceItemsField.GetValue(balance) as IList;
        }

        internal static IList GetVendors()
        {
            object balance = GetMemberValue(_gameBalanceMe, null);
            return balance == null ? null : BalanceVendorsField.GetValue(balance) as IList;
        }

        internal static IList GetWorldObjects()
        {
            return _worldObjectsProperty.GetValue(null, null) as IList;
        }

        internal static string GetId(object value)
        {
            return value == null ? null : IdField.GetValue(value) as string;
        }

        internal static string GetKnownLookupIdForWgo(object wgo, string fallback)
        {
            if (wgo == null) return fallback;
            object objDef = WgoObjDefField.GetValue(wgo);
            string alias = objDef == null ? null : ObjectNpcAliasField.GetValue(objDef) as string;
            return string.IsNullOrEmpty(alias) ? fallback : alias;
        }

        internal static bool IsKnownNpc(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            object main = GetMemberValue(_mainGameMe, null);
            if (main == null) return false;
            object save = GetMemberValue(_mainGameSave, main);
            if (save == null) return false;
            object known = GetMemberValue(_saveKnownNpcs, save);
            return known != null && _knownNpcGet.Invoke(known, new object[] { id }) != null;
        }

        internal static bool AnyWorldObjectExists(IEnumerable<string> ids)
        {
            foreach (string id in ids)
            {
                if (_worldGetByObjId.Invoke(null, new object[] { id, true }) != null)
                    return true;
            }
            return false;
        }

        internal static HashSet<string> GetActiveProductTypes(object vendor)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            IList values = _vendorGetProductTypes.Invoke(vendor, null) as IList;
            if (values == null) return result;
            foreach (object value in values)
            {
                string s = value as string;
                if (!string.IsNullOrEmpty(s)) result.Add(s);
            }
            return result;
        }

        internal static string Localize(string key)
        {
            if (string.IsNullOrEmpty(key)) return key ?? string.Empty;
            try
            {
                ParameterInfo[] p = _localize.GetParameters();
                object[] args = new object[p.Length];
                args[0] = key;
                for (int i = 1; i < p.Length; i++)
                {
                    if (p[i].GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                        args[i] = Array.CreateInstance(p[i].ParameterType.GetElementType() ?? typeof(object), 0);
                    else if (p[i].HasDefaultValue)
                        args[i] = p[i].DefaultValue;
                    else
                        args[i] = p[i].ParameterType.IsValueType ? Activator.CreateInstance(p[i].ParameterType) : null;
                }
                return _localize.Invoke(null, args) as string ?? key;
            }
            catch
            {
                return key;
            }
        }

        internal static string GetBuyersLabel()
        {
            string language = string.Empty;
            try { language = _getCurrentLanguage.Invoke(null, null) as string ?? string.Empty; }
            catch { }
            return GetBuyersLabelForLanguage(language);
        }

        internal static string GetBuyersLabelForLanguage(string language)
        {
            string normalized = string.IsNullOrEmpty(language)
                ? "en"
                : language.Trim().ToLowerInvariant().Replace('_', '-');

            switch (normalized)
            {
                case "de": return "Käufer";
                case "es": return "Compradores";
                case "fr": return "Acheteurs";
                case "it": return "Acquirenti";
                case "ja": return "買い手";
                case "ko": return "구매자";
                case "pl": return "Kupujący";
                case "pt-br": return "Compradores";
                case "ru": return "Покупают";
                case "zh-cn": return "买家";
                default: return "Buyers";
            }
        }

        internal static object CreateSeparator()
        {
            return Activator.CreateInstance(_bubbleSeparatorType);
        }

        internal static object CreateTinyText(string text)
        {
            return _textCtor.Invoke(new[] { (object)text, _tinyStyle, _centerAlignment, -1 });
        }

        private static void BindTooltipCtor()
        {
            Type styleType = RequireType("UITextStyles+TextStyle");
            Type alignmentType = RequireType("NGUIText+Alignment");
            _tinyStyle = Enum.Parse(styleType, "TinyDescription");
            _centerAlignment = Enum.Parse(alignmentType, "Center");
            _textCtor = _bubbleTextType.GetConstructor(new[] { typeof(string), styleType, alignmentType, typeof(int) });
            if (_textCtor == null)
                throw new MissingMethodException("BubbleWidgetTextData(string,TextStyle,Alignment,int)");
        }

        private static MethodInfo FindLocalizationMethod()
        {
            MethodInfo exact = _gjlType.GetMethod("L", AnyStatic, null, new[] { typeof(string) }, null);
            if (exact != null) return exact;

            foreach (MethodInfo method in _gjlType.GetMethods(AnyStatic))
            {
                if (method.Name != "L" || method.ReturnType != typeof(string)) continue;
                ParameterInfo[] p = method.GetParameters();
                if (p.Length == 0 || p[0].ParameterType != typeof(string)) continue;
                bool compatible = true;
                for (int i = 1; i < p.Length; i++)
                {
                    if (!p[i].HasDefaultValue &&
                        p[i].GetCustomAttributes(typeof(ParamArrayAttribute), false).Length == 0)
                    {
                        compatible = false;
                        break;
                    }
                }
                if (compatible) return method;
            }
            throw new MissingMethodException("Compatible GJL.L overload not found.");
        }

        private static Type RequireType(string name)
        {
            Type type = AccessTools.TypeByName(name);
            if (type == null) throw new TypeLoadException(name);
            return type;
        }

        private static FieldInfo RequireField(Type type, string name)
        {
            FieldInfo field = type.GetField(name, AnyInstance | AnyStatic);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static MethodInfo RequireMethod(Type type, string name, Type[] args)
        {
            MethodInfo method = type.GetMethod(name, AnyInstance | AnyStatic, null, args, null);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }

        private static MemberInfo RequireMember(Type type, string name, BindingFlags flags)
        {
            FieldInfo field = type.GetField(name, flags);
            if (field != null) return field;
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null) return property;
            throw new MissingMemberException(type.FullName, name);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            FieldInfo field = member as FieldInfo;
            return field != null ? field.FieldType : ((PropertyInfo)member).PropertyType;
        }

        private static object GetMemberValue(MemberInfo member, object instance)
        {
            FieldInfo field = member as FieldInfo;
            return field != null ? field.GetValue(instance) : ((PropertyInfo)member).GetValue(instance, null);
        }
    }
}
