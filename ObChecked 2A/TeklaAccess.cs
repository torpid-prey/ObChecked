using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Tekla.Structures.Catalogs;
using TSM = Tekla.Structures.Model;
using ObChecked.Model;
using ObChecked.Phasing;
using ObChecked.UI;

namespace ObChecked.TeklaAccess
{

    internal static class Report
    {

        internal static bool TryGetBoolFromBatches(
            string name,
            Hashtable intResults,
            Hashtable stringResults,
            out bool value)
        {
            // Prefer integer batch: 0/1
            object iv = (intResults != null) ? intResults[name] : null;
            if (iv is int)
            {
                value = ((int)iv) != 0;
                return true;
            }

            // Fallback: parse string batch: "1/0", "TRUE/FALSE", "YES/NO", "Y/N", "ON/OFF"
            object sv = (stringResults != null) ? stringResults[name] : null;
            if (sv != null)
            {
                string s = sv.ToString().Trim();
                if (s.Length > 0)
                {
                    string u = s.ToUpperInvariant();
                    if (u == "1" || u == "TRUE" || u == "T" || u == "YES" || u == "Y" || u == "ON")
                    {
                        value = true; return true;
                    }
                    if (u == "0" || u == "FALSE" || u == "F" || u == "NO" || u == "N" || u == "OFF")
                    {
                        value = false; return true;
                    }
                }
            }

            value = false;
            return false;
        }

        internal static void BuildPropertyBatches(
            IList<ColumnLayout> layout,
            ArrayList reportStringProps,
            ArrayList reportDoubleProps,
            ArrayList reportIntProps)
        {
            for (int i = 0; i < layout.Count; i++)
            {
                ColumnLayout col = layout[i];
                if (!"ReportProperty".Equals(col.Source, StringComparison.OrdinalIgnoreCase))
                    continue;

                string dt = (col.DataType ?? "string").Trim().ToLowerInvariant();
                string name = col.PropertyName;

                if (dt == "double")
                {
                    if (!reportDoubleProps.Contains(name)) reportDoubleProps.Add(name);
                }
                else if (dt == "int")
                {
                    if (!reportIntProps.Contains(name)) reportIntProps.Add(name);
                }
                else if (dt == "bool")
                {
                    // Ask for it in both integer and string batches
                    if (!reportIntProps.Contains(name)) reportIntProps.Add(name);
                    if (!reportStringProps.Contains(name)) reportStringProps.Add(name);
                }
                else
                {
                    if (!reportStringProps.Contains(name)) reportStringProps.Add(name);
                }
            }
        }


        internal static void FetchPropertiesForPart(
            TSM.Part part,
            ArrayList reportStringProps,
            ArrayList reportDoubleProps,
            ArrayList reportIntProps,
            Hashtable stringResults,
            Hashtable doubleResults,
            Hashtable intResults)
        {
            stringResults.Clear();
            doubleResults.Clear();
            intResults.Clear();

            if (reportStringProps != null && reportStringProps.Count > 0)
                part.GetStringReportProperties(reportStringProps, ref stringResults);

            if (reportDoubleProps != null && reportDoubleProps.Count > 0)
                part.GetDoubleReportProperties(reportDoubleProps, ref doubleResults);

            if (reportIntProps != null && reportIntProps.Count > 0)
                part.GetIntegerReportProperties(reportIntProps, ref intResults);

        }

        internal static void FetchPropertiesForBolt(
            TSM.BoltGroup bolt,
            ArrayList reportStringProps,
            ArrayList reportDoubleProps,
            ArrayList reportIntProps,
            Hashtable stringResults,
            Hashtable doubleResults,
            Hashtable intResults)
        {
            stringResults.Clear();
            doubleResults.Clear();
            intResults.Clear();

            if (reportStringProps != null && reportStringProps.Count > 0)
                bolt.GetStringReportProperties(reportStringProps, ref stringResults);

            if (reportDoubleProps != null && reportDoubleProps.Count > 0)
                bolt.GetDoubleReportProperties(reportDoubleProps, ref doubleResults);

            if (reportIntProps != null && reportIntProps.Count > 0)
                bolt.GetIntegerReportProperties(reportIntProps, ref intResults);

        }

        internal static void FetchPropertiesForComponent(
            TSM.BaseComponent component,
            ArrayList reportStringProps,
            ArrayList reportDoubleProps,
            ArrayList reportIntProps,
            Hashtable stringResults,
            Hashtable doubleResults,
            Hashtable intResults)
        {
            stringResults.Clear();
            doubleResults.Clear();
            intResults.Clear();

            if (reportStringProps != null && reportStringProps.Count > 0)
                component.GetStringReportProperties(reportStringProps, ref stringResults);

            if (reportDoubleProps != null && reportDoubleProps.Count > 0)
                component.GetDoubleReportProperties(reportDoubleProps, ref doubleResults);

            if (reportIntProps != null && reportIntProps.Count > 0)
                component.GetIntegerReportProperties(reportIntProps, ref intResults);

        }
    }



    internal sealed class RowUDACache
    {
        private readonly Dictionary<string, string> _s = new Dictionary<string, string>(8, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _d = new Dictionary<string, double>(8, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _i = new Dictionary<string, int>(8, StringComparer.OrdinalIgnoreCase);

        internal bool TryGetString(string k, out string v) => _s.TryGetValue(k, out v);
        internal bool TryGetDouble(string k, out double v) => _d.TryGetValue(k, out v);
        internal bool TryGetInt(string k, out int v) => _i.TryGetValue(k, out v);

        internal void SetString(string k, string v) { _s[k] = v; }
        internal void SetDouble(string k, double v) { _d[k] = v; }
        internal void SetInt(string k, int v) { _i[k] = v; }

        internal void Clear() { _s.Clear(); _d.Clear(); _i.Clear(); }
    }

    internal sealed class  UDA
    {
        internal static object GetValue(TSM.ModelObject obj, ColumnPlan plan, RowUDACache cache)
        {
            string name = plan.Name;

            switch (plan.DataType) // "double" | "int" | (default string)
            {
                case "double":
                    if (cache != null && cache.TryGetDouble(name, out var dCached)) return dCached;
                    double d = 0.0;
                    if (obj.GetUserProperty(name, ref d))
                    {
                        cache?.SetDouble(name, d);
                        return d;
                    }
                    return DBNull.Value;

                case "int":
                    if (cache != null && cache.TryGetInt(name, out var iCached)) return iCached;
                    int i = 0;
                    if (obj.GetUserProperty(name, ref i))
                    {
                        cache?.SetInt(name, i);
                        return i;
                    }
                    return DBNull.Value;

                default: // string
                    if (cache != null && cache.TryGetString(name, out var sCached)) return sCached ?? "";
                    string s = null;
                    if (obj.GetUserProperty(name, ref s))
                    {
                        cache?.SetString(name, s);
                        return s ?? "";
                    }
                    return ""; // string columns get empty string on missing
            }
        }

    }

    internal static class Direct
    {

        internal static object Part(TSM.Part part, string propertyName, PartRowCache cache)
        {
            if (part == null || string.IsNullOrEmpty(propertyName)) return "";

            string key = propertyName.ToUpperInvariant();
            switch (key)
            {
                case "GUID":
                    return part.Identifier != null ? (object)part.Identifier.GUID.ToString() : "";

                case "CLASS":
                    return part.Class;

                case "ROTATION":
                    return part.Position != null ? (object)part.Position.Rotation.ToString() : "";

                case "ANGLE":
                    return part.Position != null ? (object)part.Position.RotationOffset : (object)0.0;

                case "PHASE.NAME":
                    if (cache.needAnyPhase) PhaseResolve.EnsurePartPhase(part, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Name : (object)"";

                case "PHASE.NUMBER":
                    if (cache.needAnyPhase) PhaseResolve.EnsurePartPhase(part, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Number : (object)DBNull.Value;

                case "PHASE.OTHERS":
                    if (cache.needAnyPhase) PhaseResolve.EnsurePartPhase(part, true, cache); // compute expensive “others” lazily
                    return cache.Phase.Has ? (object)(cache.Phase.Others ?? "") : (object)"";

                default:
                    return DBNull.Value; // safer if layout’s DataType is numeric
            }
        }

        internal static object Bolt(TSM.BoltGroup bolt, string propertyName, BoltRowCache cache)
        {
            if (bolt == null || string.IsNullOrEmpty(propertyName)) return "";

            string key = propertyName.ToUpperInvariant();
            switch (key)
            {
                case "GUID": return bolt.Identifier != null ? (object)bolt.Identifier.GUID.ToString() : "";
                case "BOLTSIZE": return bolt.BoltSize;
                case "STANDARD": return bolt.BoltStandard;
                case "TOLERANCE": return bolt.Tolerance;
                case "CONTAINS_BOLT": return bolt.Bolt;
                case "HOLE_TYPE": return bolt.PlainHoleType.ToString();

                case "PHASE.NAME":
                    if (cache.needAnyPhase) PhaseResolve.EnsureBoltPhase(bolt, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Name : (object)"";

                case "PHASE.NUMBER":
                    if (cache.needAnyPhase) PhaseResolve.EnsureBoltPhase(bolt, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Number : (object)DBNull.Value;

                case "PHASE.OTHERS":
                    if (cache.needAnyPhase) PhaseResolve.EnsureBoltPhase(bolt, true, cache);
                    return cache.Phase.Has ? (object)(cache.Phase.Others ?? "") : (object)"";

                default:
                    return DBNull.Value; // safer if layout’s DataType is numeric
            }
        }

        internal static object Component(TSM.BaseComponent component, string propertyName, ComponentRowCache cache)
        {
            if (component == null || string.IsNullOrEmpty(propertyName)) return "";

            string key = propertyName.ToUpperInvariant();
            switch (key)
            {
                case "GUID":
                    return component.Identifier != null ? (object)component.Identifier.GUID.ToString() : "";

                case "NAME":
                    if (!cache.NameFetched)
                    {
                        cache.Name = component.Name;
                        if (string.IsNullOrEmpty(cache.Name))
                        {
                            ComponentCatalog.TryGet(component.Number, out cache.Name);
                        }
                        cache.NameFetched = true;
                    }
                    return cache.Name ?? "";

                case "NUMBER":
                    return component.Number;

                case "TYPE":
                    return component.GetType().ToString().Replace("Tekla.Structures.Model.", "");

                case "PHASE.NAME":
                    if (cache.needAnyPhase) PhaseResolve.EnsureComponentPhase(component, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Name : (object)"";

                case "PHASE.NUMBER":
                    if (cache.needAnyPhase) PhaseResolve.EnsureComponentPhase(component, false, cache);
                    return cache.Phase.Has ? (object)cache.Phase.Number : (object)DBNull.Value;

                case "PHASE.OTHERS":
                    if (cache.needAnyPhase) PhaseResolve.EnsureComponentPhase(component, true, cache);
                    return cache.Phase.Has ? (object)(cache.Phase.Others ?? "") : (object)"";

                default:
                    return DBNull.Value; // safer if layout’s DataType is numeric
            }
        }

    }

    //internal static class Catalog
    //{
    //    private static readonly object _compCatalogLock = new();
    //    private static Dictionary<int, string> _compCatalog;
    //    private static bool _compCatalogBuilt;


    //    /// <summary>
    //    /// Returns the internal catalog name for a given component number.
    //    /// </summary>
    //    /// <param name="number"></param>
    //    /// <returns></returns>
    //    internal static string GetConnectionName(int number)
    //    {
    //        lock (_compCatalogLock)
    //        {
    //            if (!_compCatalogBuilt)
    //            {
    //                _compCatalog = new Dictionary<int, string>(2048);
    //                CatalogHandler handler = new CatalogHandler();
    //                var items = handler.GetComponentItems();
    //                while (items.MoveNext())
    //                {
    //                    var ci = items.Current;
    //                    if (ci != null && !_compCatalog.ContainsKey(ci.Number))
    //                        _compCatalog.Add(ci.Number, ci.UIName);
    //                }
    //                _compCatalogBuilt = true;
    //            }

    //            if (_compCatalog != null && _compCatalog.TryGetValue(number, out string ui))
    //                return ui;
    //        }
    //        return null;
    //    }

    //}


    // Model.cs (or wherever you keep catalog helpers)
    internal static class ComponentCatalog
    {
        private static readonly object _lock = new();
        private static Dictionary<int, string> _map;
        private static bool _built;

        internal static void EnsureLoaded()
        {
            if (_built) return;
            lock (_lock)
            {
                if (_built) return;
                _map = new Dictionary<int, string>(2048);
                var handler = new CatalogHandler();
                var items = handler.GetComponentItems();
                while (items.MoveNext())
                {
                    var ci = items.Current;
                    if (ci != null) _map[ci.Number] = ci.UIName;
                }
                _built = true;
            }
        }

        internal static bool TryGet(int number, out string name)
        {
            EnsureLoaded();
            return _map.TryGetValue(number, out name);
        }
    }




    internal static class Extensions
    {

        /// <summary>
        /// Determines if the model object is a part and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        internal static bool IsPart(this TSM.ModelObject modelObject, out TSM.Part part)
        {
            if (modelObject is TSM.Part)
            {
                part = modelObject as TSM.Part;
                return true;
            }

            part = null;
            return false;
        }

        /// <summary>
        /// Determines if the model object is a bolt group and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="boltGroup"></param>
        /// <returns></returns>
        internal static bool IsBoltGroup(this TSM.ModelObject modelObject, out TSM.BoltGroup boltGroup)
        {
            if (modelObject is TSM.BoltGroup)
            {
                boltGroup = modelObject as TSM.BoltGroup;
                return true;
            }

            boltGroup = null;
            return false;
        }

        /// <summary>
        /// Determines if the model object is a bolt group and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        internal static bool IsComponent(this TSM.ModelObject modelObject, out TSM.BaseComponent component)
        {
            if (modelObject is TSM.BaseComponent)
            {
                component = modelObject as TSM.BaseComponent;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Determines if the model object is a detail and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="boltGroup"></param>
        /// <returns></returns>
        internal static bool IsDetail(this TSM.ModelObject modelObject, ref TSM.Detail detail)
        {
            if (modelObject is TSM.Detail)
            {
                detail = modelObject as TSM.Detail;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the model object is a connection and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="boltGroup"></param>
        /// <returns></returns>
        internal static bool IsConnection(this TSM.ModelObject modelObject, ref TSM.Connection connection)
        {
            if (modelObject is TSM.Connection)
            {
                connection = modelObject as TSM.Connection;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the model object is a custom part and returns it.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="customPart"></param>
        /// <returns></returns>
        internal static bool IsCustomPart(this TSM.ModelObject modelObject, ref TSM.CustomPart customPart)
        {
            if (modelObject is TSM.CustomPart)
            {
                customPart = modelObject as TSM.CustomPart;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the model object has a valid identifier and returns its GUID
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        internal static bool IsIdentified(this TSM.ModelObject obj, out Guid guid)
        {
            guid = Guid.Empty;
            if (obj is null) return false;

            try
            {
                var id = obj.Identifier;
                if (id == null) return false;

                var g = id.GUID;
                if (g == Guid.Empty) return false;

                guid = g;
                return true;
            }
            catch
            {
                // Consider logging here if you have Diagnostics available.
                return false;
            }
        }


    }
}
