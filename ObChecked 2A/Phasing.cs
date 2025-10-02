using System;
using System.Collections.Generic;
using System.Diagnostics;
using TSM = Tekla.Structures.Model;
using ObChecked.Diagnostics;

namespace ObChecked.Phasing
{
    // ---------------- Settings & Filters ----------------

    internal static class PhaseSettings
    {
        internal const bool StopAfterFirstChildMismatch = true;
    }

    internal static class PhaseTypeFilter
    {
        internal static bool ChildTypeHasPhase(TSM.ModelObject m) =>
            m is TSM.Weld || m is TSM.BooleanPart || m is TSM.Fitting || m is TSM.BoltGroup;
    }

    // ---------------- Phase DTOs & Row Caches ----------------

    internal struct PhaseBase
    {
        internal bool Has;
        internal int Number;
        internal string Name;
    }

    internal struct PhaseInfo
    {
        internal bool Has;
        internal int Number;
        internal string Name;
        internal string Others;
        internal bool OthersComputed;
    }

    internal sealed class PartRowCache
    {
        internal bool needAnyPhase;
        internal bool PhaseFetched;
        internal PhaseInfo Phase;
    }

    internal sealed class BoltRowCache
    {
        internal bool needAnyPhase;
        internal bool PhaseFetched;
        internal PhaseInfo Phase;
    }

    internal sealed class ComponentRowCache
    {
        internal bool needAnyPhase;
        internal bool PhaseFetched;
        internal PhaseInfo Phase;

        // Used by Direct.Component for NAME fallback to catalog
        internal bool NameFetched;
        internal string Name;
    }

    // ---------------- Phase Cache (single source of truth) ----------------

    internal static class PhaseCache
    {
        private static readonly object _lock = new();
        private static readonly Dictionary<Guid, PhaseBase> _cache = new(8192);

        internal static void Clear()
        {
            lock (_lock) _cache.Clear();
        }

        internal static PhaseBase Get(TSM.ModelObject obj)
        {
            if (obj?.Identifier == null) return default;

            D.Inc(ref PhaseDiag.CacheLookups);

            var id = obj.Identifier.GUID;

            // Fast path
            lock (_lock)
            {
                if (_cache.TryGetValue(id, out var pb))
                {
                    D.Inc(ref PhaseDiag.CacheHits);
                    return pb;
                }
            }

            // Miss → Tekla call outside the lock
            obj.GetPhase(out TSM.Phase ph);
            var fresh = new PhaseBase
            {
                Has = ph != null,
                Number = ph?.PhaseNumber ?? 0,
                Name = ph?.PhaseName ?? ""
            };

            // Try insert
            lock (_lock)
            {
                if (_cache.TryGetValue(id, out var existing))
                {
                    D.Inc(ref PhaseDiag.CacheRaceLost);
                    return existing;
                }
                _cache[id] = fresh;
                D.Inc(ref PhaseDiag.CacheMisses);
                D.Inc(ref PhaseDiag.CacheRaceWon);
                return fresh;
            }
        }
    }

    // ---------------- Assembly-Main Cache ----------------

    internal struct AssyMainInfo
    {
        internal Guid MainGuid;
        internal PhaseBase Phase;   // Phase of the assembly main part
    }

    internal static class AssyMainCache
    {
        private static readonly object _lock = new();
        private static readonly Dictionary<Guid, AssyMainInfo> _cache = new(2048);

        internal static void Clear()
        {
            lock (_lock) _cache.Clear();
        }

        internal static AssyMainInfo Get(TSM.Assembly assy)
        {
            if (assy == null || assy.Identifier == null) return default;
            Guid assyGuid = assy.Identifier.GUID;

            // Fast path
            lock (_lock)
            {
                if (_cache.TryGetValue(assyGuid, out var hit)) return hit;
            }

            // Miss → compute without holding the lock
            var main = assy.GetMainPart();
            var fresh = new AssyMainInfo
            {
                MainGuid = (main?.Identifier != null) ? main.Identifier.GUID : Guid.Empty,
                Phase = PhaseCache.Get(main) // <-- single phase source
            };

            // Write-back
            lock (_lock)
            {
                if (_cache.TryGetValue(assyGuid, out var existing)) return existing;
                _cache[assyGuid] = fresh;
                return fresh;
            }
        }
    }

    // ---------------- Phase Resolution (Ensure*) ----------------

    internal static class PhaseResolve
    {
        internal static void EnsurePartPhase(TSM.Part part, bool needOthers, PartRowCache cache)
        {
            if (!cache.PhaseFetched)
            {
                var self = PhaseCache.Get(part);
                cache.PhaseFetched = true;
                cache.Phase.Has = self.Has;
                cache.Phase.Number = self.Number;
                cache.Phase.Name = self.Name ?? "";
            }

            if (!cache.Phase.Has)
                return;

            if (!needOthers)
            {
                D.Inc(ref PhaseDiag.BasePhaseRequested_Parts);
                return;
            }

            D.Inc(ref PhaseDiag.OthersRequested_Parts);

            if (cache.Phase.OthersComputed)
            {
                D.Inc(ref PhaseDiag.OthersServedFromCache_Parts);
                return;
            }

            var other = new HashSet<string>();

            // Compare to assembly main (cached)
            long t0 = Stopwatch.GetTimestamp();
            var assy = part.GetAssembly();
            var mi = AssyMainCache.Get(assy);
            long t1 = Stopwatch.GetTimestamp();
            D.Add(ref PhaseTime.Parts_MainTicks, t1 - t0);

            var selfGuid = (part.Identifier != null) ? part.Identifier.GUID : Guid.Empty;
            if (mi.MainGuid != Guid.Empty && selfGuid != mi.MainGuid)
            {
                D.Inc(ref PhaseDiag.MainChecked_Parts);

                if (mi.Phase.Has && mi.Phase.Number > 0 && mi.Phase.Number != cache.Phase.Number)
                {
                    D.Inc(ref PhaseDiag.MainPhaseMismatch_Parts);
                    other.Add((mi.Phase.Name ?? "") + "*");
                }
            }

            // Children (welds/cuts/bolts/etc.)
            D.Inc(ref PhaseDiag.ChildrenEnumerations_Parts);
            t0 = Stopwatch.GetTimestamp();
            var ch = part.GetChildren();
            t1 = Stopwatch.GetTimestamp();
            D.Add(ref PhaseTime.Parts_ChildEnumTicks, t1 - t0);

            while (ch.MoveNext())
            {
                var child = ch.Current;
                if (child == null) continue;

                if (!PhaseTypeFilter.ChildTypeHasPhase(child))
                    continue;

                D.Inc(ref PhaseDiag.ChildrenVisited_Parts);

                long t2 = Stopwatch.GetTimestamp();
                var cp = PhaseCache.Get(child);
                long t3 = Stopwatch.GetTimestamp();
                D.Add(ref PhaseTime.Parts_ChildPhaseTicks, t3 - t2);

                if (cp.Has && cp.Number > 0 && cp.Number != cache.Phase.Number)
                {
                    D.Inc(ref PhaseDiag.ChildrenPhaseMismatch_Parts);
                    other.Add((cp.Name ?? "") + "~");
                    if (PhaseSettings.StopAfterFirstChildMismatch) break;
                }
            }

            // Father component
            t0 = Stopwatch.GetTimestamp();
            var parent = part.GetFatherComponent();
            if (parent != null)
            {
                D.Inc(ref PhaseDiag.FatherChecked_Parts);
                var pp = PhaseCache.Get(parent);
                if (pp.Has && pp.Number > 0 && pp.Number != cache.Phase.Number)
                {
                    D.Inc(ref PhaseDiag.FatherPhaseMismatch_Parts);
                    other.Add((pp.Name ?? "") + "^");
                }
            }
            t1 = Stopwatch.GetTimestamp();
            D.Add(ref PhaseTime.Parts_FatherTicks, t1 - t0);

            // Finalize
            string val = null;
            foreach (var s in other) val = val == null ? s : (val + ", " + s);
            cache.Phase.Others = val ?? "";
            cache.Phase.OthersComputed = true;
            D.Inc(ref PhaseDiag.OthersComputed_Parts);
        }

        internal static void EnsureBoltPhase(TSM.BoltGroup bolt, bool needOthers, BoltRowCache cache)
        {
            if (!cache.PhaseFetched)
            {
                var self = PhaseCache.Get(bolt);
                cache.PhaseFetched = true;
                cache.Phase.Has = self.Has;
                cache.Phase.Number = self.Number;
                cache.Phase.Name = self.Name ?? "";
            }

            if (!needOthers || !cache.Phase.Has || cache.Phase.OthersComputed)
            {
                if (needOthers && cache.Phase.OthersComputed)
                    D.Inc(ref PhaseDiag.OthersServedFromCache_Parts); // if you want a separate counter for bolts, add it
                return;
            }

            var other = new HashSet<string>();

            // children → '~'
            var ch = bolt.GetChildren();
            while (ch.MoveNext())
            {
                var child = ch.Current;
                if (child == null) continue;

                var cp = PhaseCache.Get(child);
                if (cp.Has && cp.Number > 0 && cp.Number != cache.Phase.Number)
                    other.Add((cp.Name ?? "") + "~");
            }

            // father → '^'
            var parent = bolt.GetFatherComponent();
            if (parent != null)
            {
                var pp = PhaseCache.Get(parent);
                if (pp.Has && pp.Number > 0 && pp.Number != cache.Phase.Number)
                    other.Add((pp.Name ?? "") + "^");
            }

            string val = null;
            foreach (var s in other) val = val == null ? s : (val + ", " + s);
            cache.Phase.Others = val ?? "";
            cache.Phase.OthersComputed = true;
        }

        internal static void EnsureComponentPhase(TSM.BaseComponent comp, bool needOthers, ComponentRowCache cache)
        {
            if (!cache.PhaseFetched)
            {
                var self = PhaseCache.Get(comp);
                cache.PhaseFetched = true;
                cache.Phase.Has = self.Has;
                cache.Phase.Number = self.Number;
                cache.Phase.Name = self.Name ?? "";
            }

            if (!needOthers || !cache.Phase.Has || cache.Phase.OthersComputed)
                return;

            var other = new HashSet<string>();

            // children → '~'
            var ch = comp.GetChildren();
            while (ch.MoveNext())
            {
                var child = ch.Current;
                if (child == null) continue;

                var cp = PhaseCache.Get(child);
                if (cp.Has && cp.Number > 0 && cp.Number != cache.Phase.Number)
                    other.Add((cp.Name ?? "") + "~");
            }

            // father → '^'
            var parent = comp.GetFatherComponent();
            if (parent != null)
            {
                var pp = PhaseCache.Get(parent);
                if (pp.Has && pp.Number > 0 && pp.Number != cache.Phase.Number)
                    other.Add((pp.Name ?? "") + "^");
            }

            string val = null;
            foreach (var s in other) val = val == null ? s : (val + ", " + s);
            cache.Phase.Others = val ?? "";
            cache.Phase.OthersComputed = true;
        }
    }
}
