using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObChecked.Processing
{


    internal class MultiTaskProgress
    {
        // ---- FETCH PHASE (enumeration of model objects) ----
        private int totalObjects;   // total to fetch (set once at start)
        private int objectsDone;    // fetch done count (increment during fetch)

        // ---- CLASSIFICATION TOTALS (filled during fetch) ----
        private int partsTotal, boltsTotal, compsTotal, othersTotal;

        // ---- PROCESSING PHASE (after fetch) ----
        private int partsDone, boltsDone, compsDone;

        // -------- Lifecycle --------
        internal void ResetAll()
        {
            Interlocked.Exchange(ref totalObjects, 0);
            Interlocked.Exchange(ref objectsDone, 0);

            Interlocked.Exchange(ref partsTotal, 0);
            Interlocked.Exchange(ref boltsTotal, 0);
            Interlocked.Exchange(ref compsTotal, 0);
            Interlocked.Exchange(ref othersTotal, 0);

            Interlocked.Exchange(ref partsDone, 0);
            Interlocked.Exchange(ref boltsDone, 0);
            Interlocked.Exchange(ref compsDone, 0);
        }

        // If you need to clear only processing (keep totals from fetch)
        internal void ResetProcessing()
        {
            Interlocked.Exchange(ref partsDone, 0);
            Interlocked.Exchange(ref boltsDone, 0);
            Interlocked.Exchange(ref compsDone, 0);
        }

        // -------- FETCH PHASE API --------
        // Call at the very start of enumeration
        internal void BeginFetch(int total)
        {
            Interlocked.Exchange(ref totalObjects, total);
            Interlocked.Exchange(ref objectsDone, 0);
        }

        // Use this during fetch for each object enumerated
        internal void IncFetchDone()
        {
            Interlocked.Increment(ref objectsDone);
        }

        // Incrementally add objects to total if they were contained within components
        internal void IncFetch()
        {
            Interlocked.Increment(ref totalObjects);
        }

        // Mark fetch phase complete (if you don’t want to loop the last increments)
        internal void MarkFetchComplete()
        {
            int tot = Volatile.Read(ref totalObjects);
            Interlocked.Exchange(ref objectsDone, tot);
        }

        internal bool IsFetchComplete()
        {
            return Volatile.Read(ref objectsDone) >= Volatile.Read(ref totalObjects);
        }

        internal int GetFetchPercent()
        {
            int tot = Volatile.Read(ref totalObjects);
            int don = Volatile.Read(ref objectsDone);
            return tot > 0 ? (don * 100) / tot : 0;
        }

        internal int GetFetchDone() { return Volatile.Read(ref objectsDone); }
        internal int GetFetchTotal() { return Volatile.Read(ref totalObjects); }

        internal string OverallString()
        {
            int don = Volatile.Read(ref objectsDone);
            int tot = Volatile.Read(ref totalObjects);
            return "Objects: " + don + "/" + tot;
        }

        // -------- CLASSIFICATION TOTALS (during fetch) --------
        internal void AddPart() { Interlocked.Increment(ref partsTotal); }
        internal void AddBolt() { Interlocked.Increment(ref boltsTotal); }
        internal void AddComp() { Interlocked.Increment(ref compsTotal); }
        internal void AddOther() { Interlocked.Increment(ref othersTotal); }

        // If you prefer the shorter names during fetch:
        internal void IncPart() { Interlocked.Increment(ref partsTotal); }
        internal void IncBolt() { Interlocked.Increment(ref boltsTotal); }
        internal void IncComp() { Interlocked.Increment(ref compsTotal); }
        internal void IncOther() { Interlocked.Increment(ref othersTotal); }

        internal int GetPartsTotal() { return Volatile.Read(ref partsTotal); }
        internal int GetBoltsTotal() { return Volatile.Read(ref boltsTotal); }
        internal int GetCompsTotal() { return Volatile.Read(ref compsTotal); }
        internal int GetOthersTotal() { return Volatile.Read(ref othersTotal); }

        internal string PartString()
        {
            int don = Volatile.Read(ref partsDone);
            int tot = Volatile.Read(ref partsTotal);
            return "Parts: " + don + "/" + tot;
        }
        internal string BoltString()
        {
            int don = Volatile.Read(ref boltsDone);
            int tot = Volatile.Read(ref boltsTotal);
            return "Bolts: " + don + "/" + tot;
        }
        internal string CompString()
        {
            int don = Volatile.Read(ref compsDone);
            int tot = Volatile.Read(ref compsTotal);
            return "Components: " + don + "/" + tot;
        }
        internal string OtherString()
        {
            // Others are not processed; show count only
            int tot = Volatile.Read(ref othersTotal);
            return "Others: " + tot;
        }

        // -------- PROCESSING PHASE API --------
        // Use these while you pull properties / build DTOs / etc.
        internal void IncPartsDone() { Interlocked.Increment(ref partsDone); }
        internal void IncBoltsDone() { Interlocked.Increment(ref boltsDone); }
        internal void IncCompsDone() { Interlocked.Increment(ref compsDone); }

        internal void SetPartsDone(int done) { Interlocked.Exchange(ref partsDone, done); }
        internal void SetBoltsDone(int done) { Interlocked.Exchange(ref boltsDone, done); }
        internal void SetCompsDone(int done) { Interlocked.Exchange(ref compsDone, done); }

        internal int ProcessingTotal
        {
            get
            {
                return Volatile.Read(ref partsTotal)
                     + Volatile.Read(ref boltsTotal)
                     + Volatile.Read(ref compsTotal);
            }
        }

        internal int ProcessingDone
        {
            get
            {
                return Volatile.Read(ref partsDone)
                     + Volatile.Read(ref boltsDone)
                     + Volatile.Read(ref compsDone);
            }
        }

        internal int GetProcessingPercent()
        {
            int total = ProcessingTotal;
            int done = ProcessingDone;
            return total > 0 ? (done * 100) / total : 0;
        }

        internal int GetPartsPercent()
        {
            int tot = Volatile.Read(ref partsTotal);
            int don = Volatile.Read(ref partsDone);
            return tot > 0 ? (don * 100) / tot : 0;
        }

        internal int GetBoltsPercent()
        {
            int tot = Volatile.Read(ref boltsTotal);
            int don = Volatile.Read(ref boltsDone);
            return tot > 0 ? (don * 100) / tot : 0;
        }

        internal int GetCompsPercent()
        {
            int tot = Volatile.Read(ref compsTotal);
            int don = Volatile.Read(ref compsDone);
            return tot > 0 ? (don * 100) / tot : 0;
        }
    }
}
