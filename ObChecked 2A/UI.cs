using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace ObChecked.UI
{

    internal static class Grid
    {

        internal static void BuildColumnSchemaFromLayout(DataTable table, IList<ColumnLayout> layout)
        {
            table.Clear();
            table.Columns.Clear();
            foreach (var col in layout)
            {
                Debug.Print(col.PropertyName + " → " + col.GetColumnType().Name);
                table.Columns.Add(new DataColumn(col.Header, col.GetColumnType())); // ← typed (double/int/string)
            }
        }

        internal static void ConfigureGridFromLayout(DataGridView dgv, DataTable table, IList<ColumnLayout> layout)
        {
            if (dgv == null || table == null || layout == null) return;

            dgv.SuspendLayout();

            // Lock down grid behavior
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Own the columns; prevents surprise auto-gen later
            dgv.Columns.Clear();

            // Pre-calc equal fill weight for unweighted FILL columns
            int fillNoWeightCount = 0;
            for (int i = 0; i < layout.Count; i++)
            {
                var w = layout[i].Width;
                if (w != null &&
                    !string.IsNullOrEmpty(w.Mode) &&
                    w.Mode.Equals("FILL", StringComparison.OrdinalIgnoreCase) &&
                    w.Value <= 0)
                {
                    fillNoWeightCount++;
                }
            }
            float equalFillWeight = (fillNoWeightCount > 0) ? (100f / fillNoWeightCount) : 100f;

            for (int i = 0; i < layout.Count; i++)
            {
                var cfg = layout[i];

                // ensure the DataTable has this column (schema drives everything)
                if (!table.Columns.Contains(cfg.Header))
                    continue;

                var dataCol = table.Columns[cfg.Header];
                Type colType = dataCol.DataType;

                // Pick the *UI* column type from the actual schema
                DataGridViewColumn col;
                if (colType == typeof(bool))
                {
                    col = new DataGridViewCheckBoxColumn
                    {
                        TrueValue = true,
                        FalseValue = false,
                        ThreeState = false,
                        ReadOnly = true // toggle if you want it editable
                    };
                }
                else
                {
                    col = new DataGridViewTextBoxColumn();
                }

                col.Name = cfg.Header;
                col.HeaderText = cfg.Header;
                col.DataPropertyName = cfg.Header;   // bind to DataTable column
                col.Visible = cfg.Visible;
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.DefaultCellStyle.NullValue = "??";

                // Formatting/alignment (based on schema, not JSON string)
                bool isNumeric = (colType == typeof(double) || colType == typeof(float) ||
                                  colType == typeof(decimal) || colType == typeof(int) ||
                                  colType == typeof(long) || colType == typeof(short));

                if (!string.IsNullOrEmpty(cfg.Format) && isNumeric)
                {
                    col.DefaultCellStyle.Format = cfg.Format;               // e.g. "0.##", "N2"
                    col.DefaultCellStyle.FormatProvider = CultureInfo.CurrentCulture;
                }

                var defaultAlign =
                    (colType == typeof(bool)) ? DataGridViewContentAlignment.MiddleCenter :
                    (isNumeric) ? DataGridViewContentAlignment.MiddleRight :
                    DataGridViewContentAlignment.MiddleCenter;

                col.DefaultCellStyle.Alignment =
                    (cfg.Alignment > 0) ? MapAlign(cfg.Alignment, defaultAlign) : defaultAlign;

                // Sizing
                var w = cfg.Width;
                string mode = (w != null && !string.IsNullOrEmpty(w.Mode)) ? w.Mode.ToUpperInvariant() : "NONE";
                int min = (w != null) ? Math.Max(0, w.Min) : 0;
                int val = (w != null) ? Math.Max(0, w.Value) : 0;

                switch (mode)
                {
                    case "FILL":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = (val > 0) ? val : equalFillWeight;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "ALLCELLS":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "ONLYCELLS": // AllCellsExceptHeader
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "DISPLAYEDCELLS":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "ONLYDISPLAYEDCELLS": // DisplayedCellsExceptHeader
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "COLUMNHEADER":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                        if (min > 0) col.MinimumWidth = min;
                        break;

                    case "NONE":
                    default:
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        if (val > 0) col.Width = val;
                        if (min > 0) col.MinimumWidth = min;
                        break;
                }

                dgv.Columns.Add(col);
            }

            // bind after defining columns
            dgv.DataSource = table;
            dgv.ResumeLayout();
        }

        private static DataGridViewContentAlignment MapAlign(int code, DataGridViewContentAlignment fallback)
        {
            switch (code)
            {
                case 1: return DataGridViewContentAlignment.TopLeft;
                case 2: return DataGridViewContentAlignment.TopCenter;
                case 4: return DataGridViewContentAlignment.TopRight;
                case 16: return DataGridViewContentAlignment.MiddleLeft;
                case 32: return DataGridViewContentAlignment.MiddleCenter;
                case 64: return DataGridViewContentAlignment.MiddleRight;
                case 256: return DataGridViewContentAlignment.BottomLeft;
                case 512: return DataGridViewContentAlignment.BottomCenter;
                case 1024: return DataGridViewContentAlignment.BottomRight;
                default: return fallback;
            }
        }

        internal static void EnsureHandlesForAllGrids(TabControl tc)
        {
            if (tc == null) return;
            // ensure TabControl handle
            if (!tc.IsHandleCreated) { var _ = tc.Handle; }

            foreach (TabPage page in tc.TabPages)
            {
                if (!page.IsHandleCreated) { var __ = page.Handle; }
                EnsureHandleRecursive(page);
            }
        }

        private static void EnsureHandleRecursive(Control c)
        {
            if (c == null) return;
            if (!c.IsHandleCreated) { var _ = c.Handle; }  // forces handle, even if not visible

            // If you only care about DGVs you can special-case:
            // if (c is DataGridView dgv) using (dgv.CreateGraphics()) { }

            foreach (Control child in c.Controls)
                EnsureHandleRecursive(child);

            c.PerformLayout();
        }

    } // Grid


    internal enum ColumnKind { ReportString, ReportDouble, ReportInt, ReportBool, User, Direct }

    internal sealed class ColumnPlan
    {
        internal ColumnKind Kind;
        internal string Name;      // report/user property name (e.g., "PROFILE", "LENGTH", UDA name)
        internal string Key;       // report/user property name IN ALL CAPS for comparisons
        internal int Ordinal;      // column index in the DataTable
        internal string DataType;  // "string" | "double" | "int" (from layout)
        internal string Source;    // "ReportProperty" | "UserProperty" | "Direct"
    }

    internal class ColumnLayout
    {
        internal string Header { get; set; }           // Column header text
        internal string PropertyName { get; set; }     // Property being sought (usually ALL CAPS)
        internal string Source { get; set; }           // "ReportProperty" | "UserProperty" | "Direct"
        internal string DataType { get; set; }         // "string" | "double" | "int" | "bool" 
        internal string Format { get; set; }           // single format property
        internal bool Visible { get; set; }            // visible in grid
        internal int Alignment { get; set; }           // "Left", "Center", "Right"

        internal ColumnWidth Width { get; set; }

        internal Type GetColumnType()
        {
            var dt = (DataType ?? "string").Trim().ToLowerInvariant();
            if (dt == "double") return typeof(double);
            if (dt == "int" || dt == "integer") return typeof(int);
            if (dt == "bool" || dt == "boolean") return typeof(bool);
            //if (dt == "datetime") return typeof(DateTime);
            return typeof(string);
        }
    } // ColumnLayout

    internal class ColumnWidth
    {
        internal string Mode { get; set; } // "Fixed" or "Auto"
        internal int Value { get; set; }   // only used for Fixed
        internal int Min { get; set; }     // only used for Auto
    } // ColumnWidth

    internal class GridLayouts
    {
        internal List<ColumnLayout> Parts { get; set; }
        internal List<ColumnLayout> Bolts { get; set; }
        internal List<ColumnLayout> Components { get; set; }
    } // GridLayouts

    internal static class ColumnPlans
    {

        /// Build once per run for this DataTable/layout
        internal static ColumnPlan[] Build(DataTable table, IList<ColumnLayout> layout)
        {
            var plans = new ColumnPlan[layout.Count];
            for (int i = 0; i < layout.Count; i++)
            {
                var col = layout[i];
                var plan = new ColumnPlan
                {
                    Ordinal = i,
                    Name = col.PropertyName,
                    Key = (col.PropertyName ?? "").ToUpperInvariant(), // ← here
                    DataType = (col.DataType ?? "string").Trim().ToLowerInvariant(),
                    Source = col.Source
                };

                if (string.Equals(col.Source, "ReportProperty", StringComparison.OrdinalIgnoreCase))
                {
                    if (plan.DataType == "double") plan.Kind = ColumnKind.ReportDouble;
                    else if (plan.DataType == "int") plan.Kind = ColumnKind.ReportInt;
                    else if (plan.DataType == "bool") plan.Kind = ColumnKind.ReportBool;
                    else plan.Kind = ColumnKind.ReportString;
                }
                else if (string.Equals(col.Source, "UserProperty", StringComparison.OrdinalIgnoreCase))
                {
                    plan.Kind = ColumnKind.User;
                }
                else
                {
                    plan.Kind = ColumnKind.Direct;
                }

                plans[i] = plan;
            }
            return plans;
        }

        /// <summary>
        /// Determines if the column plan accesses any phase properties
        /// </summary>
        /// <param name="plans"></param>
        /// <param name="needName"></param>
        /// <param name="needNumber"></param>
        /// <param name="needOthers"></param>
        internal static void ScanPhaseNeeds(ColumnPlan[] plans, out bool needName, out bool needNumber, out bool needOthers)
        {
            needName = needNumber = needOthers = false;
            for (int i = 0; i < plans.Length; i++)
            {
                if (plans[i].Kind != ColumnKind.Direct) continue;
                string k = plans[i].Key;
                if (k == "PHASE.NAME") needName = true;
                else if (k == "PHASE.NUMBER") needNumber = true;
                else if (k == "PHASE.OTHERS") needOthers = true;
            }
        }
    } // ColumnPlans




}
