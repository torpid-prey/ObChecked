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

        // need to consolidate Build methods

        // need to convert to metadata first, then use that to define other things

        // resolve / cache source and type enums once

        // mark flags like IsGUID, IsPhased, and assign Ordinal using index

        // Separate Width Mode (ALLCELLS etc) into its own static function
        // maybe change alignment to strings instead of integer to make reading json easier

        internal static void BuildColumnSchemaFromLayout(DataTable table, IList<ColumnDefinition> layout)
        {
            table.Clear();
            table.Columns.Clear();
            foreach (var col in layout)
            {
                //Debug.Print(col.PropertyName + " → " + col.GetColumnType().Name);

                // this will be replaced with the metadata systemType
                table.Columns.Add(new DataColumn(col.Header, col.GetColumnType())); // ← typed (double/int/string/bool)
            }
        }

        internal static void ConfigureGridFromLayout(DataGridView dgv, DataTable table, IList<ColumnDefinition> layout)
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


        internal static IList<ColumnMetadata> BuildSchema(IList<ColumnDefinition> layout)
        {
            var result = new List<ColumnMetadata>();

            for (int i = 0; i < layout.Count; i++)
            {
                ColumnMetadata metadata = new ColumnMetadata(layout[i]);

                metadata.Ordinal = i;
                metadata.Column

            }

            return result;
        }


    } // Grid




    //internal enum ColumnKind { ReportString, ReportDouble, ReportInt, ReportBool, User, Direct }

    internal static class ColumnPlans
    {

        /// Build once per run for this DataTable/layout
        internal static ColumnMetadata[] Build(DataTable table, IList<ColumnDefinition> layout)
        {
            var plans = new ColumnMetadata[layout.Count];
            for (int i = 0; i < layout.Count; i++)
            {
                var col = layout[i];
                var plan = new ColumnMetadata
                {
                    Ordinal = i,
                    Name = col.PropertyName,
                    DataType = (col.DataType ?? "string").Trim().ToLowerInvariant(),
                    //Source = col.Source
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
        internal static void ScanPhaseNeeds(this ColumnMetadata[] plans, out bool needName, out bool needNumber, out bool needOthers)
        {
            needName = needNumber = needOthers = false;
            for (int i = 0; i < plans.Length; i++)
            {
                if (plans[i].Kind != ColumnKind.Direct) continue;
                string k = plans[i].Column.PropertyName;
                if (k == "PHASE.NAME") needName = true;
                else if (k == "PHASE.NUMBER") needNumber = true;
                else if (k == "PHASE.OTHERS") needOthers = true;
            }
        }
    } // ColumnPlans






    /// <summary>
    /// Runtime value type to use in <see cref="DataTable"/>
    /// </summary>
    internal enum ColumnType { String, Integer, Double, Boolean }

    /// <summary>
    /// Determines the method for retrieving property values from model objects.
    /// </summary>
    internal enum ColumnSource { Report, User, Direct }


    /// <summary>
    /// Collection of processed Column Metadata Items
    /// </summary>
    internal sealed class ColumnSet
    {
        /// <summary>
        /// Collection of processed Column Metadata Items
        /// </summary>
        List<ColumnMetadata> Items { get; }

        /// <summary>
        /// Index of the required GUID column
        /// </summary>
        int PrimaryIndex { get; }

        internal ColumnSet(List<ColumnDefinition> columns)
        {
            Items = new List<ColumnMetadata>();
            PrimaryIndex = -1;

            // loop through each json column definition
            for (int i = 0; i < columns.Count; i++)
            {
                // get the next available index
                int index = Items.Count;

                // derive the column source
                ColumnSource colSource = GetColumnSource(columns[i].Source);

                // derive the column data type and system type
                ColumnType colType = GetColumnType(columns[i].DataType, out Type sysType);

                // determine isGUID and isPhased property name flags
                GetPropertyFlags(columns[i].PropertyName, out bool isGUID, out bool isLocalPhase, out bool isOtherPhase);
                
                // only store GUID once
                if (isGUID)
                {
                    // only store one GUID column
                    if (PrimaryIndex > -1) continue;

                    // store primary index
                    PrimaryIndex = index;
                }
               
                // create column metadata
                ColumnMetadata item = new(columns[i], index, colSource, colType, sysType, isGUID, isLocalPhase, isOtherPhase);

                // add to collection
                Items.Add(item);
            }
        }

        /// <summary>
        /// Determines the state of isGUID and isPhased based on property name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="isGUID"></param>
        /// <param name="isPhased"></param>
        private void GetPropertyFlags(string propertyName, out bool isGUID, out bool isLocalPhase, out bool isOtherPhase)
        {
            // default false
            isGUID = false;
            isLocalPhase = false;
            isOtherPhase = false;

            // case-sensitive check for GUID
            if (propertyName.Equals("GUID"))
                isGUID = true;

            // case-insensitive check for local phase data (name and number for current object)
            if (propertyName.Equals("PHASE.NAME") ||
                propertyName.Equals("PHASE.NUMBER"))
                isLocalPhase = true;

            // case-insensitive check for other phase data (phase name for child objects)
            if (propertyName.Equals("PHASE.OTHERS"))
                isOtherPhase = true;
        }

        /// <summary>
        /// Determines the available source. <br>Defaults to ReportProperty if empty or unrecognised.</br>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private ColumnSource GetColumnSource(string source)
        {
            string s = (source ?? "reportproperty").Trim().ToLowerInvariant();
            if (s == "userproperty") return ColumnSource.User;
            if (s == "direct") return ColumnSource.Direct;
            return ColumnSource.Report;
        }

        /// <summary>
        /// Returns column <see cref="Type"/> 
        /// </summary>
        /// <returns></returns>
        private ColumnType GetColumnType(string dataType, out Type systemType)
        {
            var dt = (dataType ?? "string").Trim().ToLowerInvariant();
            if (dt == "double")
            {
                systemType = typeof(double);
                return ColumnType.Double;
            }
            if (dt == "int" || dt == "integer")
            {
                systemType = typeof(int);
                return ColumnType.Integer;
            }
            if (dt == "bool" || dt == "boolean")
            {
                systemType = typeof(bool);
                return ColumnType.Boolean;
            }
            systemType = typeof(string);
            return ColumnType.String;
        }
    }

    /// <summary>
    /// Derived metadata from the column definition used for ordinals, flags, pointers.
    /// <para>Clarifies column definition within the table in a lightweigth supplementary class.</para>
    /// </summary>
    internal sealed class ColumnMetadata
    {
        /// <summary>
        /// The original .json definition data
        /// </summary>
        internal readonly ColumnDefinition Column;  // the column object
        /// <summary>
        /// The column index in the data table
        /// </summary>
        internal readonly int Ordinal;              // column index in the DataTable
        /// <summary>
        /// The retrieval point for property values <br>(Report, User, Direct)</br>
        /// </summary>
        internal readonly ColumnSource Source;      // Report, User, Direct
        /// <summary>
        /// The supported data type assigned to the column <br>(String, Integer, Double, Boolean)</br>
        /// </summary>
        internal readonly ColumnType DataType;      // String, Integer, Double, Boolean
        /// <summary>
        /// The cached <see cref="Type"/> that will be assigned to the grid cell
        /// </summary>
        internal readonly Type SystemType;
        /// <summary>
        /// Determines if this is the unique GUID column
        /// </summary>
        internal readonly bool IsGUID;              // Determines if this is the GUID column
        /// <summary>
        /// Determines if this column requires phase data to be cached for the local object
        /// </summary>
        internal readonly bool IsLocalPhase;        // Determines if this column requires phase data
        /// <summary>
        /// Determines if this column requires phase data to be cached for child objects
        /// </summary>
        internal readonly bool IsOtherPhase;

        internal ColumnMetadata(ColumnDefinition column, int index, ColumnSource source, ColumnType datatype, Type systemtype, bool isGuid, bool isLocalPhase, bool isOtherPhase)
        {
            Column = column;
            Ordinal = index;
            Source = source;
            DataType = datatype;
            SystemType = systemtype;
            IsGUID = isGuid;
            IsLocalPhase = isLocalPhase;
            IsOtherPhase = isOtherPhase;
        }
    }

    /// <summary>
    /// Definition of each column from the JSON layout file
    /// <para>A ColumnLayout is a List(Of ColumnDefinition)</para>
    /// </summary>
    internal sealed class ColumnDefinition
    {
        /// <summary>Column header text</summary>
        internal string Header { get; set; }           // Column header text
        /// <summary>Name of property being sought </summary>
        internal string PropertyName { get; set; }     // Property being sought (usually ALL CAPS)
        /// <summary>Retrieval point for property values <br>(Report, User, Direct)</br></summary>
        internal string Source { get; set; }           // "ReportProperty" | "UserProperty" | "Direct"
        /// <summary>Supported data type assigned to the column <br>(String, Integer, Double, Boolean)</br></summary>
        internal string DataType { get; set; }         // "string" | "double" | "int" | "bool" 
        /// <summary>Format string for numerical values</summary>
        internal string Format { get; set; }           // specify number of decimal places
        /// <summary>Set grid column visibility</summary>
        internal bool Visible { get; set; }            // set column visibility
        /// <summary>Set cell text alignment</summary>
        internal int Alignment { get; set; }           // uses DataGridViewContentAlignment

        /// <summary>Define column width values</summary>
        internal ColumnWidth Width { get; set; }
    } // ColumnDefinition

    internal class ColumnWidth
    {
        /// <summary>AutoSize mode, including None, AllCells and Fill</summary>
        internal string Mode { get; set; } // "column auto size mode
        /// <summary>Define width if mode is None, or a proportion of remainder if mode is Fill</summary>
        internal int Value { get; set; }   // only used for Fixed
        /// <summary>Define the smallest width for autosize column modes</summary>
        internal int Min { get; set; }     // only used for Auto
    } // ColumnWidth

    internal class GridLayout
    {
        internal List<ColumnDefinition> Parts { get; set; }
        internal List<ColumnDefinition> Bolts { get; set; }
        internal List<ColumnDefinition> Components { get; set; }
    } // GridLayout

}
