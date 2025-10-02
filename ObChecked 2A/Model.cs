using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TSM = Tekla.Structures.Model;
using ObChecked.Processing;
using ObChecked.TeklaAccess;
using ObChecked.UI;

namespace ObChecked.Model
{

    internal class RawObjectConsolidator
    {
        /// <summary>
        /// Collection of part objects and property bags
        /// </summary>
        internal RawObjectCollection Parts { get; set; }

        /// <summary>
        /// Collection of bolt group objects and property bags
        /// </summary>
        internal RawObjectCollection Bolts { get; set; }

        /// <summary>
        /// Collection of component objects and property bags
        /// </summary>
        internal RawObjectCollection Components { get; set; }
        
        /// <summary>
        /// Collection of unsupported object types
        /// </summary>
        internal HashSet<string> Others { get; set; }

         /// <summary>
        /// Collection of GUIDs of all objects processed for current fetch.
        /// </summary>
        internal HashSet<Guid> History { get; } = new();

        internal RawObjectConsolidator()
        {
            Parts = new RawObjectCollection();
            Bolts = new RawObjectCollection();
            Components = new RawObjectCollection();

            Others = new HashSet<string>();
            History = new HashSet<Guid>();
     
            // wire owners
            Parts.Owner = this;
            Bolts.Owner = this;
            Components.Owner = this;
        }

        /// <summary>
        /// Clears all collections and visited cache (use if reusing the same instance for a new run).
        /// </summary>
        internal void ClearAll()
        {
            Parts.ClearFetch();
            Bolts.ClearFetch();
            Components.ClearFetch();
            Others.Clear();
            History.Clear();
        }

        /// <summary>
        /// Add a root object and traverse ALL nested component children iteratively.
        /// Note: Do NOT call fetch progress here; increment fetch per root outside.
        /// </summary>
        internal void Add(TSM.ModelObject modelObject, MultiTaskProgress progress)
        {
            if (modelObject == null) return;

            var stack = new Stack<TSM.ModelObject>();
            stack.Push(modelObject);

            while (stack.Count > 0)
            {
                TSM.ModelObject mo = stack.Pop();
                if (mo == null) continue;

                // skip unidentified objects (missing GUID is invalid)
                if (!mo.IsIdentified(out Guid guid)) continue;

                // skip duplicates within same fetch
                if (History.Contains(guid)) continue;

                // add unique GUID to history
                History.Add(guid);

                if (mo is TSM.Part)
                {
                    Parts.FetchObject(mo);
                    progress.IncPart();
                }
                else if (mo is TSM.BoltGroup)
                {
                    Bolts.FetchObject(mo);
                    progress.IncBolt();
                }
                else if (mo is TSM.BaseComponent)
                {
                    // Record the component itself
                    Components.FetchObject(mo);
                    progress.IncComp();

                    // Expand ALL children (covers nested components recursively)
                    TSM.ModelObjectEnumerator children = mo.GetChildren();
                    if (children != null)
                    {
                        while (children.MoveNext())
                        {
                            TSM.ModelObject child = children.Current;
                            if (child != null) stack.Push(child);
                        }
                    }
                  
                }
                else
                {
                    // Unhandled object types
                    string typeName = mo.GetType().Name;
                    if (!string.IsNullOrEmpty(typeName)) Others.Add(typeName);
                    progress.IncOther();
                }
            }
        }

        internal void AddRange(IEnumerable<TSM.ModelObject> roots, MultiTaskProgress progress)
        {
            if (roots == null) return;
            foreach (TSM.ModelObject mo in roots) Add(mo, progress);
        }

        internal int Count
        {
            get { return Parts.Fetch.Count + Bolts.Fetch.Count + Components.Fetch.Count; }
        }
    } // RawObjectConsolidator


    /// <summary>
    /// Individual collection of related raw model objects and cached property values
    /// </summary>
    internal class RawObjectCollection
    {
        internal RawObjectConsolidator Owner; // set this when constructing RawObjects for some reason

        /// <summary>
        /// Column layout for the current group
        /// </summary>
        internal List<ColumnLayout> ColumnLayout { get; set; }

        /// <summary>
        /// Master collection of raw model objects
        /// </summary>
        internal Dictionary<Guid, RawObject> Master { get; }

        /// <summary>
        /// Collection of GUIDs for the current fetch
        /// </summary>
        internal Dictionary<Guid, RawObject> Fetch{ get; }

        internal RawObjectCollection()
        {
            //ModelObjects = new List<TSM.ModelObject>(256); // modest initial capacity
            ColumnLayout = new List<ColumnLayout>();
            Master = new Dictionary<Guid, RawObject>();
            Fetch = new Dictionary<Guid, RawObject>();
        }

        /// <summary>
        /// Clear fetch cache (use before starting a new fetch)
        /// </summary>
        internal void ClearFetch()
        {
            Fetch.Clear();
        }

        /// <summary>
        /// Adds model object to the fetch collection if it has a unique GUID.
        /// </summary>
        /// <param name="modelObject"></param>
        internal void FetchObject(TSM.ModelObject modelObject)
        {
            // skip if null or unidentified
            if (modelObject == null || !modelObject.IsIdentified(out Guid guid)) return;

            if (Fetch.ContainsKey(guid)) return;

            // add unique GUID to fetch, returns false if already added during same fetch
            Fetch.Add(guid, new RawObject(modelObject));
        }

        internal void Consolidate()
        {
            // this will consolidate the fetch items into the master list and clear the fetch


        }


        //internal void EnsureCapacity(int additional)
        //{
        //    if (additional <= 0) return;
        //    int needed = ModelObjects.Count + additional;
        //    if (needed > ModelObjects.Capacity) ModelObjects.Capacity = needed;
        //}

        //internal void Add(TSM.ModelObject modelObject)
        //{
        //    // skip if null or unidentified
        //    if (modelObject == null || !modelObject.IsIdentified(out Guid guid)) return;

        //    // add to model object collection
        //    ModelObjects.Add(modelObject);

        //    // add new property bag with assigned identifier
        //    Bags.Add(new PropertyCache(guid));
        //}

        //internal int Count
        //{
        //    get { return Master.Count; }
        //}
    } // RawObjectCollection


    /// <summary>
    /// Individual raw object with cached property values
    /// </summary>
    internal class RawObject
    {
        internal Guid Guid { get; }

        internal TSM.ModelObject ModelObject { get; set; }

        internal PropertyCache Cache { get; }

        /// <summary>
        /// Create a new raw object by assigning the GUID, the model object reference, and an empty property cache.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal RawObject(TSM.ModelObject modelObject)
        {
            if (modelObject == null) throw new ArgumentNullException(nameof(modelObject));
            if (!modelObject.IsIdentified(out Guid guid)) throw new ArgumentException("Model object must be identified.", nameof(modelObject));
            Guid = guid;
            ModelObject = modelObject;
            Cache = new PropertyCache(guid);
        }

        /// <summary>
        /// Update an existing raw object by maintaining the GUID, updating the model object reference, and clearing the property cache.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal void Update(TSM.ModelObject modelObject)
        {
            if (modelObject == null) throw new ArgumentNullException(nameof(modelObject));
            if (!modelObject.IsIdentified(out Guid guid)) throw new ArgumentException("Model object must be identified.", nameof(modelObject));
            ModelObject = modelObject;
            Cache.Clear();
        }

        ///// <summary>
        ///// Clear property cache
        ///// </summary>
        //internal void ClearCache()
        //{
        //    Cache.Clear();
        //}

    } // RawObject


    /// <summary>
    /// Collection of cached property values retrieved for a <see cref="TSM.ModelObject"/>
    /// </summary>
    internal sealed class PropertyCache
    {
        internal Guid Guid { get; }
        internal Hashtable Strings { get; } = new();
        internal Hashtable Doubles { get; } = new();
        internal Hashtable Integers { get; } = new();
        internal RowUDACache UDAs { get; } = new();
        internal PropertyCache(Guid guid)
        {
            if (guid == Guid.Empty) throw new ArgumentException("GUID must be non-empty.", nameof(guid));
            Guid = guid;
        }

        /// <summary>
        /// Clears the property cache of all values
        /// </summary>
        public void Clear()
        {
            Strings.Clear();
            Doubles.Clear();
            Integers.Clear();
            UDAs.Clear();
        }

    } // PropertyCache

}
