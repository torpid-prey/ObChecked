// THINGS TO DO

// all main methods should have xml descriptions
// comment descriptions should be included above blocks of code so I can follow along with what each chunk is doing

//======================
// UI:
//======================

// GUID must appear in each table/schema (this allows user to make it last and visible if they want)
// GUID in datagridview must be set to unique (and hence repeats are already skipped or replaced)
// GUID in returned object properties must not be null, or object is skipped and no row created
// if GUID is not present in table schema, then table is not built, dgv remains empty, and tab is hidden too.
// this allows user to hide tabs simply by not including a Parts, Bolts, or Components section of the .json file

// ColumnPlan[] Build() has DataTable table that is currently unused...
// why is it there?
// what could it be doing instead of nothing?

// Does ColumnPlans class need to be static? 
// The ScanPhaseNeeds could be an instance method (I've made it an extension and works so maybe instance works better)

// The name ColumnPlan isn't clear, what does it do? Is this the Schema?
// I've already renamed ColumnLayout to ColumnDefinition because it represents a single column
// List variables are still called ColumnLayout because the list of columns is the layout

// BuildColumnSchemaFromLayout and ConfigureGridFromLayout could be combined
// any reason for them to be separate? They are called at the same time.
// I noticed buildColumnSchemaFromLayout is called during fetch as well
// this means if .json changes, the table will be correct but the grid will not
// but is there a way to detect if layout has changed, and only update if required?
// we could store the .json last modified and only rebuild schema and grid if modified
// otherwise we can load it at startup and not update it each time unless necessary
// this will tie in with GUID instructions earlier, tabs can be shown or hidden based on inclusion of section and GUID column.

// we seem to have a lot of checks for column type using strings.
// Shouldn't we just have one function GetColumnType and use that each time?
// I'd prefer an overloaded function to return different types if required
// i want to always check int/integer, and bool/boolean

//======================
// Phasing
//======================

// PhaseBase and PhaseInfo both have bool Has;
// phasebase and phaseinfo are very similar
// is there a difference? do we need both?
// every object will have a phase, it will never be null, but some object types do not support phase changes
// for such objects, phase number will only be 0. Otherwise for all valid objects, phases start from 1.
// we should rename this so it is clearer. No need for null phase check, and can safely disregard 0.
// valid objects (parts, bolts, components, welds) cannot have phase set to 0.

// PartRowCachel and BoltRowCache are identical and ComponentRowCache is almost identical
// Should we just use a generic RowCache

// EnsurePhase.Part/Bolt/Component are almost identical
// are there necessary differences?
// I know part determines phase from assy main part, but bolt and component i think are the same
// What does it actually do? We should include xml tags so i can remember
// we should remove all diagnostics until it works, then redisgn diagnostics and add them back.
// I think Ensure is the wrong word, 
// but i'm also using the word cache a lot. i want to distinguish between the levels
// of cache, which is the base cache receptacle, and which are the gettters of the cache
// there is also PartRowCache etc... can any of these be combined, simplified, made generic, reused etc

// i want run a temp check which objects have a phase and which do not
// then i can safely ignore phase of non-phased objects, and only get phase for phased objects
// then the zero phase check can be a fall-back for potential changes in different versions


//======================
// TeklaAccess:
//======================

// We often use the same groups of ArrayList for reportProperties and hashtables for reportResults.
// is it worth creating a class for each to simplify the BuildPropertyBatches and FetchProperties calls?
// this can include a clear method to easy reuse and not keep making new ones fo each fetch
// perhaps the same groups can be utilised for UDA fetch as well?
// maybe a little harder because UDA retrieval is not separated by type but report retrieval is

// looks like RowUDACache uses Lambda expressions but I want these simplified into non-lambda forms
// even if the => syntax is not technically a lambda, I prefer it long form
// also for compleness we should support boolean/bool in UDAs same as we do for reports
// this means if datatype in .json is bool, it tries to get string.lowercase and if its "1" or "true" then set True else False
// setting datatype to bool makes ui gridview show tickbox
// maybe invalid boolean values could show the indeterminate value to distinguish neither true or false.
// that would need to apply to reports as well as UDAs

// in the Direct class, case "GUID" supports null and returns empty string.
// this should perhaps return DBNull so any object with a null GUID is skipped / warned
// Null GUID is not valid because every object must have a GUID
// GUID in datagridview must be set to unique

// maybe can remove lock on component catalog because it now gets initialised on form_Load
// should also remove the call to initialise within TryGet and throw exception instead
// one built it shouldn't need rebuilding unless user changes project. But support for that will be added later


//======================
// Model:
//======================

// currently wiring each RawObjectCollection to its owning RawObjectStore
// not sure if this was old logic but no longer required, I don't think it serves any purpose at the moment



//======================
// Processing:
//======================

// It looks like AddPart() and IncPart() (etc for bolt, comp, other) is doing the same thing
// we should change to IncrementPart() etc. I like concise method names, not abbreviated ones
// there are lots of methods here not in use. I'm not sure if they are not required
// or just not implemented properly yet. We should consolidate this but not until other updates are implemented
// I don't think we need to include Other parts in the processing counter or progress bar
// Other objects don't get processed into any list, only their unique types are added to a dataset
// and displayed, but no processing is done aside from phase checks.

// this will change significantly after we rewrite the background workers so no need to address it yet
// but when working on new methods elsewhere, it would be good to include a commented line
// within new sections to show where the new multiprogress increments and totals will fit
