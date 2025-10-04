using System;
using System.Diagnostics;
using System.Threading;

namespace ObChecked.Diagnostics
{




    //private void WireGridDebug(DataGridView dgv, string name)
    //{
    //    dgv.HandleCreated += (s, e) => DumpCols(dgv, name, "HandleCreated");
    //    dgv.VisibleChanged += (s, e) => DumpCols(dgv, name, "VisibleChanged=" + dgv.Visible);
    //    dgv.ParentChanged += (s, e) => DumpCols(dgv, name, "ParentChanged");
    //    dgv.DataBindingComplete += (s, e) => DumpCols(dgv, name, "DataBindingComplete");
    //    dgv.DataSourceChanged += (s, e) => DumpCols(dgv, name, "DataSourceChanged");
    //    dgv.ColumnAdded += (s, e) =>
    //        Debug.WriteLine($"[{name}] ColumnAdded '{e.Column.HeaderText}' AutoGen={dgv.AutoGenerateColumns}");
    //}

    //private void DumpCols(DataGridView dgv, string name, string where)
    //{
    //    Debug.WriteLine($"[{name}] {where}; AutoGenerateColumns={dgv.AutoGenerateColumns}, Handle={dgv.IsHandleCreated}");
    //    foreach (DataGridViewColumn c in dgv.Columns)
    //        Debug.WriteLine($"  Col '{c.HeaderText}' vis={c.Visible} mode={c.AutoSizeMode} DP='{c.DataPropertyName}'");
    //}



    static class PhaseDiag
    {
        // Cache stats
        internal static long CacheLookups;
        internal static long CacheHits;
        internal static long CacheMisses;
        internal static long CacheRaceWon;   // we inserted after miss
        internal static long CacheRaceLost;  // someone else inserted while we computed

        // “Base phase only” vs “others” requests
        internal static long BasePhaseRequested_Parts;
        internal static long OthersRequested_Parts;
        internal static long OthersComputed_Parts;
        internal static long OthersServedFromCache_Parts; // reused OthersComputed

        // Work done to compute Others (Parts)
        internal static long MainChecked_Parts;
        internal static long MainPhaseMismatch_Parts;
        internal static long ChildrenEnumerations_Parts;      // times we iterated children
        internal static long ChildrenVisited_Parts;           // total children visited
        internal static long ChildrenPhaseMismatch_Parts;     // added to “other”
        internal static long FatherChecked_Parts;
        internal static long FatherPhaseMismatch_Parts;

        // Do the same sets for Bolts/Components if you want split stats
        // (or keep everything aggregated—up to you).

        internal static void Reset()
        {
            Interlocked.Exchange(ref CacheLookups, 0);
            Interlocked.Exchange(ref CacheHits, 0);
            Interlocked.Exchange(ref CacheMisses, 0);
            Interlocked.Exchange(ref CacheRaceWon, 0);
            Interlocked.Exchange(ref CacheRaceLost, 0);

            Interlocked.Exchange(ref BasePhaseRequested_Parts, 0);
            Interlocked.Exchange(ref OthersRequested_Parts, 0);
            Interlocked.Exchange(ref OthersComputed_Parts, 0);
            Interlocked.Exchange(ref OthersServedFromCache_Parts, 0);

            Interlocked.Exchange(ref MainChecked_Parts, 0);
            Interlocked.Exchange(ref MainPhaseMismatch_Parts, 0);
            Interlocked.Exchange(ref ChildrenEnumerations_Parts, 0);
            Interlocked.Exchange(ref ChildrenVisited_Parts, 0);
            Interlocked.Exchange(ref ChildrenPhaseMismatch_Parts, 0);
            Interlocked.Exchange(ref FatherChecked_Parts, 0);
            Interlocked.Exchange(ref FatherPhaseMismatch_Parts, 0);
        }

        internal static void DumpToDebug()
        {
            Debug.Print("== Phase diagnostics ==");
            Debug.Print($"Cache: lookups={CacheLookups:n0}, hits={CacheHits:n0} ({Pct(CacheHits, CacheLookups):n1}%), misses={CacheMisses:n0}, raceWon={CacheRaceWon:n0}, raceLost={CacheRaceLost:n0}");

            Debug.Print($"Parts: basePhaseRequested={BasePhaseRequested_Parts:n0}, othersRequested={OthersRequested_Parts:n0}");
            Debug.Print($"Parts: othersComputed={OthersComputed_Parts:n0}, othersFromCache={OthersServedFromCache_Parts:n0}");

            Debug.Print($"Parts work: mainChecked={MainChecked_Parts:n0} (mismatch={MainPhaseMismatch_Parts:n0}), " +
                        $"childrenEnums={ChildrenEnumerations_Parts:n0}, childrenVisited={ChildrenVisited_Parts:n0}, childrenMismatch={ChildrenPhaseMismatch_Parts:n0}, " +
                        $"fatherChecked={FatherChecked_Parts:n0} (mismatch={FatherPhaseMismatch_Parts:n0})");
        }

        private static double Pct(long a, long b) => b == 0 ? 0.0 : (100.0 * a / b);
    }




    // ---------- diagnostics support ----------
    internal static class D
    {
        // ---------- counters ----------
        [Conditional("DIAG")]
        internal static void Inc(ref long counter) => Interlocked.Increment(ref counter);

        [Conditional("DIAG")]
        internal static void Add(ref long counter, long value) => Interlocked.Add(ref counter, value);

        [Conditional("DIAG")]
        internal static void Zero(ref long counter) => Interlocked.Exchange(ref counter, 0);

        [Conditional("DIAG")]
        internal static void Print(string msg) => Debug.Print(msg);

        // ---------- scope timers (aggregated) ----------
#if DIAG
            private sealed class Scope : IDisposable
            {
                private readonly string _key;
                private readonly long _start;
                internal Scope(string key)
                {
                    _key = key;
                    _start = Stopwatch.GetTimestamp();
                }
                internal void Dispose()
                {
                    long ticks = Stopwatch.GetTimestamp() - _start;
                    _ticks.AddOrUpdate(_key, ticks, static (_, old) => old + ticks);
                }
            }

            // one shared bucket; keys are strings like "Parts.Fetch", "Phase.Part.Main"
            private static readonly ConcurrentDictionary<string, long> _ticks = new();

            internal static IDisposable Time(string key) => new Scope(key);

            internal static void DumpTimes(params string[] prefixFilters)
            {
                double toMs(long t) => t * 1000.0 / Stopwatch.Frequency;

                foreach (var kv in _ticks)
                {
                    if (prefixFilters == null || prefixFilters.Length == 0)
                    {
                        Debug.Print($"[time] {kv.Key} = {toMs(kv.Value):n1} ms");
                        continue;
                    }

                    foreach (var p in prefixFilters)
                    {
                        if (kv.Key.StartsWith(p, StringComparison.Ordinal))
                        {
                            Debug.Print($"[time] {kv.Key} = {toMs(kv.Value):n1} ms");
                            break;
                        }
                    }
                }
            }

            internal static void ClearTimes(params string[] prefixFilters)
            {
                if (prefixFilters == null || prefixFilters.Length == 0)
                {
                    _ticks.Clear();
                    return;
                }

                foreach (var kv in _ticks)
                    foreach (var p in prefixFilters)
                        if (kv.Key.StartsWith(p, StringComparison.Ordinal))
                            _ticks.TryRemove(kv.Key, out _);
            }
#else
        // no-op versions (so call sites compile cleanly)
        private sealed class Empty : IDisposable { public void Dispose() { } }
        private static readonly Empty _empty = new();

        public static IDisposable Time(string key) => _empty;
        internal static void DumpTimes(params string[] prefixFilters) { }
        internal static void ClearTimes(params string[] prefixFilters) { }
#endif

        // ---------- convenience for gated dumps ----------
        [Conditional("DIAG")]
        internal static void DumpPhaseDiag(Action dump) => dump();
    }



    static class PhaseTime
    {
        internal static long Parts_MainTicks, Parts_ChildEnumTicks, Parts_ChildPhaseTicks, Parts_FatherTicks;
        internal static long Comps_MainTicks, Comps_ChildEnumTicks, Comps_ChildPhaseTicks, Comps_FatherTicks;

        internal static void Dump()
        {
            Func<long, double> ms = t => t * 1000.0 / Stopwatch.Frequency;
            Debug.Print($"[PhaseTime Parts] main={ms(Parts_MainTicks):n1} ms, enum={ms(Parts_ChildEnumTicks):n1} ms, childPhase={ms(Parts_ChildPhaseTicks):n1} ms, father={ms(Parts_FatherTicks):n1} ms");
            Debug.Print($"[PhaseTime Comps] main={ms(Comps_MainTicks):n1} ms, enum={ms(Comps_ChildEnumTicks):n1} ms, childPhase={ms(Comps_ChildPhaseTicks):n1} ms, father={ms(Comps_FatherTicks):n1} ms");
        }
    }


}
