using Newtonsoft.Json;
using ObChecked.Diagnostics;
using ObChecked.Model;
using ObChecked.Phasing;
using ObChecked.Processing;
using ObChecked.TeklaAccess;
using ObChecked.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tekla.Structures.Geometry3d;
using TSM = Tekla.Structures.Model;
using TSMUI = Tekla.Structures.Model.UI;

namespace ObChecked
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        public FormMain(TSM.Model model)
        {
            InitializeComponent();

            Model = model;
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>
        /// Determines if the Tekla Structures main window is responsive.
        /// </summary>
        /// <returns></returns>
        private bool IsTeklaMainWindowResponsive()
        {
            Process[] processes = Process.GetProcessesByName("TeklaStructures");
            if (processes.Length == 0) return false;

            IntPtr mainWindow = processes[0].MainWindowHandle;

            return IsWindowEnabled(mainWindow);
        }


        // Form Events
        
        private GridLayouts gridLayouts;
        private readonly DataTable tableParts = new();
        private readonly DataTable tableBolts = new();
        private readonly DataTable tableComponents = new();

        // progress management for multiple BGWs using one progress bar
        private readonly MultiTaskProgress multiProgress = new();

        // will be needed to select model objects
        internal readonly TSM.Model Model;

        private void FormMain_Load(object sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ColumnConfig.json");
            gridLayouts = JsonConvert.DeserializeObject<GridLayouts>(File.ReadAllText(path));

            TSM.ModelObjectEnumerator.AutoFetch = true;

            ComponentCatalog.EnsureLoaded();

            // mimic showing each tab to ensure handles are created before binding tables
            Grid.EnsureHandlesForAllGrids(tabControl1);

            // define all columns and assign datatype from .json layout
            Grid.BuildColumnSchemaFromLayout(tableParts, gridLayouts.Parts);
            Grid.BuildColumnSchemaFromLayout(tableBolts, gridLayouts.Bolts);
            Grid.BuildColumnSchemaFromLayout(tableComponents, gridLayouts.Components);

            // fill all tables with data and bind to grids from .json layout
            Grid.ConfigureGridFromLayout(dgvParts, tableParts, gridLayouts.Parts);
            Grid.ConfigureGridFromLayout(dgvBolts, tableBolts, gridLayouts.Bolts);
            Grid.ConfigureGridFromLayout(dgvComponents, tableComponents, gridLayouts.Components);
        }

        // Background workers

        private void BgwFetchObjects_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // If you plan to pass progress in, uncomment these two lines and pass the same instance:
                // var taskProgress = e.Argument as MultiTaskProgress;
                // if (taskProgress != null) multiProgress = taskProgress;

                TSMUI.ModelObjectSelector selector = new();
                TSM.ModelObjectEnumerator selectedObjects = selector.GetSelectedObjects();

                RawObjectConsolidator rawObjectConsolidator = new();

                int count = selectedObjects.GetSize();   // # of ROOTS (not children) → perfect for fetch %
                multiProgress.BeginFetch(count);         // also zeros objectsDone

                DialogResult proceed = DialogResult.OK;
                if (count > 1000)
                {
                    proceed = MessageBox.Show(
                        "You have selected " + count + " objects. This may take a while to process. Do you want to continue?",
                        Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                }

                if (proceed == DialogResult.OK)
                {
                    int pc = -1; // ensure we publish the initial 0% update

                    
                    while (selectedObjects.MoveNext())
                    {
                        TSM.ModelObject eachObj = selectedObjects.Current;
                        if (eachObj != null)
                        {
                            // Add root object and all nested children to applicable buckets
                            rawObjectConsolidator.Add(eachObj, multiProgress);
                            multiProgress.IncFetchDone();

                            int percent = multiProgress.GetFetchPercent();
                            if (percent != pc)
                            {
                                BgwFetchObjects.ReportProgress(percent);
                                pc = percent;
                            }
                        }
                    }




                    
                    multiProgress.MarkFetchComplete();
                }

                e.Result = rawObjectConsolidator;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void BgwFetchObjects_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressFetch.Value = e.ProgressPercentage;

            lblStatus.Text = multiProgress.OverallString();   // "Objects: done/total"
            lblParts.Text = multiProgress.PartString();      // "Parts: done/total"
            lblBolts.Text = multiProgress.BoltString();      // "Bolts: done/total"
            lblComponents.Text = multiProgress.CompString();      // "Components: done/total"
        }

        private void BgwFetchObjects_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is Exception ex)
            {
                MessageBox.Show(this, "Selection error: " + ex.Message, Application.ProductName,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RawObjectConsolidator rawObjectConsolidator = e.Result as RawObjectConsolidator;

            // reset progress bar
            progressFetch.Value = 0;

            if (rawObjectConsolidator != null && rawObjectConsolidator.Count > 0)
            {
                Debug.Print("Selected {0} parts.", rawObjectConsolidator.Parts.Fetch.Count);
                Debug.Print("Selected {0} bolts.", rawObjectConsolidator.Bolts.Fetch.Count);
                Debug.Print("Selected {0} components.", rawObjectConsolidator.Components.Fetch.Count);

                // Prep processing phase (sets partsDone/boltsDone/compsDone to 0; totals stay from fetch)

                PhaseCache.Clear();
                AssyMainCache.Clear();
                

                multiProgress.ResetProcessing();
                lblStatus.Text = "Retrieving Properties.";

                PhaseDiag.Reset();

                if (!BgwProcessParts.IsBusy)
                {
                    rawObjectConsolidator.Parts.ColumnLayout = gridLayouts.Parts;
                    BgwProcessParts.RunWorkerAsync(rawObjectConsolidator.Parts);
                }

                if (!BgwProcessBolts.IsBusy)
                {
                    rawObjectConsolidator.Bolts.ColumnLayout = gridLayouts.Bolts;
                    BgwProcessBolts.RunWorkerAsync(rawObjectConsolidator.Bolts);
                }

                if (!BgwProcessComponents.IsBusy)
                {
                    rawObjectConsolidator.Components.ColumnLayout = gridLayouts.Components;
                    BgwProcessComponents.RunWorkerAsync(rawObjectConsolidator.Components);
                }

                if (rawObjectConsolidator.Others.Count > 0)
                {
                    string otherTypes = string.Join(", ", rawObjectConsolidator.Others);
                    Debug.Print(otherTypes);
                }
            }
            else
            {
                lblStatus.Text = "No objects selected.";
            }
        }


        // Parts

        private void BgwProcessParts_DoWork(object sender, DoWorkEventArgs e)
        {
            var result = new DataTable { CaseSensitive = false };

            if (e.Argument is RawObjectCollection rawObjects)
            {
                result.MinimumCapacity = rawObjects.Fetch.Count;

                // 1) columns
                Grid.BuildColumnSchemaFromLayout(result, rawObjects.ColumnLayout);

                // 2) precompute column plan
                ColumnPlan[] plans = ColumnPlans.Build(result, rawObjects.ColumnLayout);

                // check if we need phase info
                ColumnPlans.ScanPhaseNeeds(plans, out bool needName, out bool needNumber, out bool needOthers);

                // 3) organize property name batches once
                ArrayList reportStringProps = new();
                ArrayList reportDoubleProps = new();
                ArrayList reportIntProps = new();

                Report.BuildPropertyBatches(
                    rawObjects.ColumnLayout,
                    reportStringProps, reportDoubleProps, reportIntProps //, userProps
                );

                // 4) speed up DataTable inserts
                result.BeginLoadData();

                // single reusable row buffer
                object[] rowBuf = new object[plans.Length];
                int pc = -1;

                // totals
                long tFetch = 0, tBuild = 0, tAdd = 0, tProgress = 0;

                // Reuse per worker (created once, cleared each row)
                Hashtable stringResults = new();
                Hashtable doubleResults = new();
                Hashtable intResults = new();
                //Hashtable userResults = new Hashtable();

                // per row uda cache
                RowUDACache udaCache = new();

                var sw = new Stopwatch();

                // use foreach loop because dictionary does not index
                foreach (var obj in rawObjects.Fetch.Values)
                {
                    if (!obj.ModelObject.IsPart(out TSM.Part part) || part == null) continue;

                    // FETCH
                    sw.Restart();

                    // per-row cache
                    var rowCache = new PartRowCache
                    {
                        needAnyPhase = (needName || needNumber || needOthers)
                    };

                    Report.FetchPropertiesForPart(part,
                        reportStringProps, reportDoubleProps, reportIntProps,
                        stringResults, doubleResults, intResults);
                    sw.Stop(); tFetch += sw.ElapsedTicks;

                    // reuse uda cache
                    udaCache.Clear();

                    // BUILD rowBuf
                    sw.Restart();

                    //// BEFORE the row loop
                    //var colTimers = new long[plans.Length];

                    // INSIDE the build loop, wrap each column fill:
                    for (int p = 0; p < plans.Length; p++)
                    {
                        //var t0 = Stopwatch.GetTimestamp();

                        var plan = plans[p];
                        object value; // = DBNull.Value;

                        switch (plan.Kind)
                        {
                            case ColumnKind.ReportString:
                                value = stringResults[plan.Name] ?? "";
                                break;

                            case ColumnKind.ReportDouble:
                                value = doubleResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.ReportInt:
                                value = intResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.ReportBool: // <- add this kind, or test plan.DataType=="bool"
                                {
                                    if (Report.TryGetBoolFromBatches(plan.Name, intResults, stringResults, out bool b))
                                        value = b;
                                    else
                                        value = DBNull.Value; // or false, if you prefer
                                    break;
                                }

                            case ColumnKind.User:
                                // your existing UDA path (extend to parse bool if needed)
                                value = UDA.GetValue(part /*or bolt/component*/, plan, udaCache);
                                break;

                            default: // Direct
                                value = Direct.Part(part, plan.Name, rowCache) ?? (object)DBNull.Value;
                                break;
                        }

                        rowBuf[plan.Ordinal] = value;

                        //colTimers[p] += Stopwatch.GetTimestamp() - t0;
                    }

                    //// AFTER the loop (convert to ms and print top offenders)
                    //for (int p = 0; p < plans.Length; p++)
                    //{
                    //    double ms = colTimers[p] * 1000.0 / Stopwatch.Frequency;
                    //    Debug.Print($"COL {p} '{rawObjects.ColumnLayout[p].Header}': {ms:n1} ms");
                    //}

                    sw.Stop(); tBuild += sw.ElapsedTicks;

                    // ADD
                    sw.Restart();
                    result.Rows.Add(rowBuf); // DataTable copies values
                    sw.Stop(); tAdd += sw.ElapsedTicks;

                    // PROGRESS (throttled)
                    sw.Restart();
                    multiProgress.IncPartsDone();
                    {
                        int pct = multiProgress.GetProcessingPercent();
                        if (pct != pc) { BgwProcessParts.ReportProgress(pct); pc = pct; }
                    }
                    sw.Stop(); tProgress += sw.ElapsedTicks;
                         
                }


                //for (int i = 0; i < rawObjects.Fetch.Count; i++)
                //{
                //    var obj = rawObjects.Fetch[i];
                //    TSM.Part part = null;
                //    if (!obj.ModelObject.IsPart(ref part) || part == null) continue;

                //    // FETCH
                //    sw.Restart();

                //    // per-row cache
                //    var rowCache = new PartRowCache
                //    {
                //        needAnyPhase = (needName || needNumber || needOthers)
                //    };

                //    Report.FetchPropertiesForPart(part,
                //        reportStringProps, reportDoubleProps, reportIntProps,
                //        stringResults, doubleResults, intResults);
                //    sw.Stop(); tFetch += sw.ElapsedTicks;

                //    // reuse uda cache
                //    udaCache.Clear();

                //    // BUILD rowBuf
                //    sw.Restart();

                //    //// BEFORE the row loop
                //    //var colTimers = new long[plans.Length];

                //    // INSIDE the build loop, wrap each column fill:
                //    for (int p = 0; p < plans.Length; p++)
                //    {
                //        //var t0 = Stopwatch.GetTimestamp();

                //        var plan = plans[p];
                //        object value; // = DBNull.Value;

                //        switch (plan.Kind)
                //        {
                //            case ColumnKind.ReportString:
                //                value = stringResults[plan.Name] ?? "";
                //                break;

                //            case ColumnKind.ReportDouble:
                //                value = doubleResults[plan.Name] ?? (object)DBNull.Value;
                //                break;

                //            case ColumnKind.ReportInt:
                //                value = intResults[plan.Name] ?? (object)DBNull.Value;
                //                break;

                //            case ColumnKind.ReportBool: // <- add this kind, or test plan.DataType=="bool"
                //                {
                //                    if (Report.TryGetBoolFromBatches(plan.Name, intResults, stringResults, out bool b))
                //                        value = b;
                //                    else
                //                        value = DBNull.Value; // or false, if you prefer
                //                    break;
                //                }

                //            case ColumnKind.User:
                //                // your existing UDA path (extend to parse bool if needed)
                //                value = UDA.GetValue(part /*or bolt/component*/, plan, udaCache);
                //                break;

                //            default: // Direct
                //                value = Direct.Part(part, plan.Name, rowCache) ?? (object)DBNull.Value;
                //                break;
                //        }

                //        rowBuf[plan.Ordinal] = value;

                //        //colTimers[p] += Stopwatch.GetTimestamp() - t0;
                //    }

                //    //// AFTER the loop (convert to ms and print top offenders)
                //    //for (int p = 0; p < plans.Length; p++)
                //    //{
                //    //    double ms = colTimers[p] * 1000.0 / Stopwatch.Frequency;
                //    //    Debug.Print($"COL {p} '{rawObjects.ColumnLayout[p].Header}': {ms:n1} ms");
                //    //}

                //    sw.Stop(); tBuild += sw.ElapsedTicks;

                //    // ADD
                //    sw.Restart();
                //    result.Rows.Add(rowBuf); // DataTable copies values
                //    sw.Stop(); tAdd += sw.ElapsedTicks;

                //    // PROGRESS (throttled)
                //    sw.Restart();
                //    multiProgress.IncPartsDone();
                //    {
                //        int pct = multiProgress.GetProcessingPercent();
                //        if (pct != pc) { BgwProcessParts.ReportProgress(pct); pc = pct; }
                //    }
                //    sw.Stop(); tProgress += sw.ElapsedTicks;
                //}

                // after loop
                double msFetch = tFetch * 1000.0 / Stopwatch.Frequency;
                double msBuild = tBuild * 1000.0 / Stopwatch.Frequency;
                double msAdd = tAdd * 1000.0 / Stopwatch.Frequency;

                double msProgress = tProgress * 1000.0 / Stopwatch.Frequency;
                Debug.Print($"Parts Build per row: {(rawObjects.Fetch.Count > 0 ? msBuild / rawObjects.Fetch.Count : 0):n2} ms/row");
                PhaseTime.Dump(); // prints the parts/component phase timing buckets

                Debug.Print($"Fetch total:    {msFetch:n1} ms");
                Debug.Print($"Build total:    {msBuild:n1} ms");
                Debug.Print($"AddRow total:   {msAdd:n1} ms");
                Debug.Print($"Progress total: {msProgress:n1} ms");

                result.EndLoadData();

                // publish final percent
                BgwProcessParts.ReportProgress(multiProgress.GetProcessingPercent());
            }

            e.Result = result;


        }

        private void BgwProcessParts_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressFetch.Value = e.ProgressPercentage;
            lblStatus.Text = multiProgress.OverallString();  //$"Retrieving properties {e.ProgressPercentage}%";
            lblParts.Text = multiProgress.PartString();
        }

        private void BgwProcessParts_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is DataTable partsTable && partsTable.Rows.Count > 0)
            {

                // pause changes
                dgvParts.SuspendLayout();
                tableParts.BeginLoadData();

                // add new rows from the result
                foreach (DataRow row in partsTable.Rows)
                {
                    tableParts.ImportRow(row);
                }

                // resume changes
                dgvParts.ResumeLayout();
                tableParts.EndLoadData();
                tableParts.AcceptChanges();

            }

            PhaseDiag.DumpToDebug();

        }

        // Bolts

        private void BgwProcessBolts_DoWork(object sender, DoWorkEventArgs e)
        {
            var result = new DataTable() { CaseSensitive = false };

            if (e.Argument is RawObjectCollection rawObjects)
            {
                result.MinimumCapacity = rawObjects.Fetch.Count;

                // 1) columns
                Grid.BuildColumnSchemaFromLayout(result, rawObjects.ColumnLayout);

                // 2) precompute column plan
                ColumnPlan[] plans = ColumnPlans.Build(result, rawObjects.ColumnLayout);

                // check if we need phase info
                ColumnPlans.ScanPhaseNeeds(plans, out bool needName, out bool needNumber, out bool needOthers);

                // 3) organize property name batches once
                ArrayList reportStringProps = new();
                ArrayList reportDoubleProps = new();
                ArrayList reportIntProps = new();
                //var userProps = new ArrayList();

                Report.BuildPropertyBatches(
                    rawObjects.ColumnLayout,
                    reportStringProps, reportDoubleProps, reportIntProps //, userProps
                );


                // 4) speed up DataTable inserts
                result.BeginLoadData();

                // single reusable row buffer
                object[] rowBuf = new object[plans.Length];
                int pc = -1;

                var sw = new Stopwatch();
                long tFetch = 0, tBuild = 0, tAdd = 0, tProgress = 0;

                // Reuse per worker (created once, cleared each row)
                Hashtable stringResults = new();
                Hashtable doubleResults = new();
                Hashtable intResults = new();
                //Hashtable userResults = new Hashtable();

                // per row uda cache
                RowUDACache udaCache = new();

                //for (int i = 0; i < rawObjects.Fetch.Count; i++)
                foreach (var obj in rawObjects.Fetch.Values)
                {
                    //TSM.BoltGroup bolt = (TSM.BoltGroup)obj.ModelObject; //rawObjects.Fetch[i] as TSM.BoltGroup;
                    //if (bolt == null) continue;

                    if (!obj.ModelObject.IsBoltGroup(out TSM.BoltGroup bolt) || bolt == null) continue;

                    // FETCH
                    sw.Restart();

                    // per-row cache
                    var rowCache = new BoltRowCache
                    {
                        needAnyPhase = (needName || needNumber || needOthers)
                    };

                    Report.FetchPropertiesForBolt(bolt,
                      reportStringProps, reportDoubleProps, reportIntProps,
                      stringResults, doubleResults, intResults);
                    sw.Stop(); tFetch += sw.ElapsedTicks;

                    // reuse uda cache
                    udaCache.Clear();

                    // BUILD rowBuf
                    sw.Restart();
                    for (int p = 0; p < plans.Length; p++)
                    {
                        var plan = plans[p];
                        object value;

                        switch (plan.Kind)
                        {
                            case ColumnKind.ReportString:
                                value = stringResults[plan.Name] ?? "";
                                break;

                            case ColumnKind.ReportDouble:
                                value = doubleResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.ReportInt:
                                value = intResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.User:
                                {
                                    value = UDA.GetValue(bolt /* or bolt/component */, plan, udaCache);
                                    break;
                                }

                            default: // Direct
                                value = Direct.Bolt(bolt, plan.Name, rowCache) ?? (object)DBNull.Value;
                                break;
                        }

                        rowBuf[plan.Ordinal] = value;
                    }
                    sw.Stop(); tBuild += sw.ElapsedTicks;

                    // ADD
                    sw.Restart();
                    result.Rows.Add(rowBuf);
                    sw.Stop(); tAdd += sw.ElapsedTicks;

                    // PROGRESS (throttled)
                    sw.Restart();
                    multiProgress.IncBoltsDone();
                    {
                        int pct = multiProgress.GetProcessingPercent();
                        if (pct != pc) { BgwProcessBolts.ReportProgress(pct); pc = pct; }
                    }
                    sw.Stop(); tProgress += sw.ElapsedTicks;
                }

                result.EndLoadData();

                // publish final percent
                BgwProcessBolts.ReportProgress(multiProgress.GetProcessingPercent());

                double msFetch = tFetch * 1000.0 / Stopwatch.Frequency;
                double msBuild = tBuild * 1000.0 / Stopwatch.Frequency;
                double msAdd = tAdd * 1000.0 / Stopwatch.Frequency;
                double msProgress = tProgress * 1000.0 / Stopwatch.Frequency;

                Debug.Print($"Bolts Fetch total:    {msFetch:n1} ms");
                Debug.Print($"Bolts Build total:    {msBuild:n1} ms");
                Debug.Print($"Bolts AddRow total:   {msAdd:n1} ms");
                Debug.Print($"Bolts Progress total: {msProgress:n1} ms");
            }

            e.Result = result;
        }

        private void BgwProcessBolts_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressFetch.Value = e.ProgressPercentage;
            lblStatus.Text = multiProgress.OverallString();  // $"Retrieving properties {e.ProgressPercentage}%";
            lblBolts.Text = multiProgress.BoltString();
        }

        private void BgwProcessBolts_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is DataTable boltsTable && boltsTable.Rows.Count > 0)
            {
                // pause changes
                dgvBolts.SuspendLayout();
                tableBolts.BeginLoadData();

                // add new rows from the result
                foreach (DataRow row in boltsTable.Rows)
                {
                    tableBolts.ImportRow(row);
                }

                // resume changes
                dgvBolts.ResumeLayout();
                tableBolts.EndLoadData();
                tableBolts.AcceptChanges();
            }

            PhaseDiag.DumpToDebug();
        }

        // Components

        private void BgwProcessComponents_DoWork(object sender, DoWorkEventArgs e)
        {
            var result = new DataTable() { CaseSensitive = false };

            if (e.Argument is RawObjectCollection rawObjects)
            {
                result.MinimumCapacity = rawObjects.Fetch.Count;

                // 1) columns
                Grid.BuildColumnSchemaFromLayout(result, rawObjects.ColumnLayout);

                // 2) precompute column plan
                ColumnPlan[] plans = ColumnPlans.Build(result, rawObjects.ColumnLayout);

                // check if we need phase info
                ColumnPlans.ScanPhaseNeeds(plans, out bool needName, out bool needNumber, out bool needOthers);

                // 3) organize property name batches once
                ArrayList reportStringProps = new();
                ArrayList reportDoubleProps = new();
                ArrayList reportIntProps = new();
                //var userProps = new ArrayList();

                Report.BuildPropertyBatches(
                    rawObjects.ColumnLayout,
                    reportStringProps, reportDoubleProps, reportIntProps //, userProps
                );

                //var udaBatches = BuildUdaBatches(rawObjects.ColumnLayout);


                // 4) speed up DataTable inserts
                result.BeginLoadData();

                // single reusable row buffer
                object[] rowBuf = new object[plans.Length];
                int pc = -1;

                var sw = new Stopwatch();
                long tFetch = 0, tBuild = 0, tAdd = 0, tProgress = 0;

                // Reuse per worker (created once, cleared each row)
                Hashtable stringResults = new();
                Hashtable doubleResults = new();
                Hashtable intResults = new();
                //Hashtable userResults = new Hashtable();

                // per row uda cache
                RowUDACache udaCache = new();

                //for (int i = 0; i < rawObjects.Fetch.Count; i++)
                foreach (var obj in rawObjects.Fetch.Values)
                {
                    //TSM.BaseComponent component = rawObjects.Fetch[i] as TSM.BaseComponent;
                    //if (component == null) continue;

                    if (!obj.ModelObject.IsComponent(out TSM.BaseComponent component) || component == null) continue;

                    // FETCH
                    sw.Restart();

                    // per-row cache
                    var rowCache = new ComponentRowCache
                    {
                        needAnyPhase = (needName || needNumber || needOthers)
                    };

                    Report.FetchPropertiesForComponent(component,
                         reportStringProps, reportDoubleProps, reportIntProps,
                         stringResults, doubleResults, intResults);
                    sw.Stop(); tFetch += sw.ElapsedTicks;

                    // reuse uda cache
                    udaCache.Clear();

                    // BUILD rowBuf
                    sw.Restart();
                    for (int p = 0; p < plans.Length; p++)
                    {
                        var plan = plans[p];
                        object value;

                        switch (plan.Kind)
                        {
                            case ColumnKind.ReportString:
                                value = stringResults[plan.Name] ?? "";
                                break;

                            case ColumnKind.ReportDouble:
                                value = doubleResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.ReportInt:
                                value = intResults[plan.Name] ?? (object)DBNull.Value;
                                break;

                            case ColumnKind.User:
                                {
                                    value = UDA.GetValue(component/* or bolt/component */, plan, udaCache);
                                    break;
                                }

                            default: // Direct
                                value = Direct.Component(component, plan.Name, rowCache) ?? (object)DBNull.Value;
                                break;
                        }

                        rowBuf[plan.Ordinal] = value;
                    }
                    sw.Stop(); tBuild += sw.ElapsedTicks;

                    // ADD
                    sw.Restart();
                    result.Rows.Add(rowBuf);
                    sw.Stop(); tAdd += sw.ElapsedTicks;

                    // PROGRESS (throttled)
                    sw.Restart();
                    multiProgress.IncCompsDone();
                    {
                        int pct = multiProgress.GetProcessingPercent();
                        if (pct != pc) { BgwProcessComponents.ReportProgress(pct); pc = pct; }
                    }
                    sw.Stop(); tProgress += sw.ElapsedTicks;
                }

                result.EndLoadData();

                // publish final percent
                BgwProcessComponents.ReportProgress(multiProgress.GetProcessingPercent());

                double msFetch = tFetch * 1000.0 / Stopwatch.Frequency;
                double msBuild = tBuild * 1000.0 / Stopwatch.Frequency;
                double msAdd = tAdd * 1000.0 / Stopwatch.Frequency;
                double msProgress = tProgress * 1000.0 / Stopwatch.Frequency;

                Debug.Print($"Components Fetch total:    {msFetch:n1} ms");
                Debug.Print($"Components Build total:    {msBuild:n1} ms");
                Debug.Print($"Components AddRow total:   {msAdd:n1} ms");
                Debug.Print($"Components Progress total: {msProgress:n1} ms");
            }

            e.Result = result;
        }

        private void BgwProcessComponents_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressFetch.Value = e.ProgressPercentage;
            lblStatus.Text = multiProgress.OverallString(); //$"Retrieving properties {e.ProgressPercentage}%";
            lblComponents.Text = multiProgress.CompString();
        }

        private void BgwProcessComponents_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is DataTable componentsTable && componentsTable.Rows.Count > 0)
            {
                // pause changes
                dgvComponents.SuspendLayout();
                tableComponents.BeginLoadData();

                // add new rows from the result
                foreach (DataRow row in componentsTable.Rows)
                {
                    tableComponents.ImportRow(row);
                }

                // resume changes
                dgvComponents.ResumeLayout();
                tableComponents.EndLoadData();
                tableComponents.AcceptChanges();
            }

            PhaseDiag.DumpToDebug();
        }



        // Button Events

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if(!IsTeklaMainWindowResponsive())
            {
                MessageBox.Show(this, "Tekla Structures main window is not responsive. Please ensure it is open and responsive before selecting objects.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (!BgwFetchObjects.IsBusy)
            {
                // reset progress bar
                progressFetch.Value = 0;
                multiProgress.ResetAll();

                // clear tables
                tableParts.Rows.Clear();
                tableBolts.Rows.Clear();
                tableComponents.Rows.Clear();

                // fetch objects
                BgwFetchObjects.RunWorkerAsync(multiProgress);
            }
            else
            {
                MessageBox.Show(this, "The background worker is already running. Please wait for it to complete.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }


        private void BtnClear_Click(object sender, EventArgs e)
        {

        }





        private void dgvParts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

    } // formMain




} // namespace
