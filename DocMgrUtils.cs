using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Linq.Dynamic.Core;
using System.Net.Sockets;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace DocumentManagerUtil
{
    public class DocMgrUtils
    {

        private const string DocDrive = "X:\\";
        private const string DocRootFolder = "Docs";

        private DocMgrUtils()
        {
            DocFoldersTable = GenerateFolderTable("FoldersTree");
            DocTypeSubTypePairs = (from row in DocFoldersTable.AsEnumerable().OrderBy(r1 => r1.Field<int>(@"RESOURCE_ID").ToString()).ThenBy(r2 => r2.Field<int>(@"PARENT_ID").ToString())
                                   where row.Field<int>(@"RESOURCE_ID") != 0
                                   select new
                                   {
                                       Id = row.Field<int>(@"RESOURCE_ID").ToString(),
                                       Parent_Id = row.Field<int>(@"PARENT_ID").ToString()
                                   }).ToDictionary(key => key.Id, value => value.Parent_Id);
        }

        public static DocMgrUtils Instance { get; } = new DocMgrUtils();

        public DataTable DocFoldersTable { get; set; }
        public Dictionary<string, string> DocTypeSubTypePairs { get; private set; }


        public class DocFolder
        {
            public string MetaDataGuid { get; set; }
            // ignore the relationship codes.  We only want the string document types.
            [JsonIgnore]
            public int Id { get; set; } //2000
            [JsonIgnore]
            public int Parent_Id { get; set; } //20

            public int Level { get; set; }  // 0, 1, 2, 3 ...
            public string DocType { get; set; } // "2000" - A FC General Document Folder .../20/2000...
            public string Parent_DocType { get; set; } // "20"  - A  FC Documents Root Folder .../20/...
            public string DocFolderName { get; set; }
            public string DocFolderDescription { get; set; } // about text	
            public string DocFolderPath { get; set; }
            public string AppSettingsKeyName;
            public string AppSettingsValue;
            public string AppSettingsFolderPath { get; set; }
            public List<DocFolder> DocSubFolder { get; set; }

            public DocFolder()
            {
                DocSubFolder = new List<DocFolder>();
            }

        }

        public XmlNode GetAppString(string name1, string name2, string settingValue)
        {
            name1 = name1.ToLower();
            name2 = name2.ToLower();



            if (!string.IsNullOrEmpty(name2))
            {
                name1 = name1.ToLower().Trim().Replace("general", "")
                    .Replace("reports", "")
                    .Replace("image", "")
                    .Replace("form_files", "")
                    .Replace("inspection", "")
                    .Replace("underwater", "uw")
                    .Replace("fracture_critical", "fc")
                    .Replace("other_special", "spi_os")
                    .Replace("pin_and_hanger", "spi_ph")
                    .Replace("critical_findings", "cif")
                    .Replace("specific_design_plans", "plans")
                    .Replace("damage_and_accident_investigation", "spi_dmg")
                    .Replace("load_rating_program", "lr")
                    .Replace("plans_of_action", "")
                    .Replace("113", "")
                    .Replace("scour_program", "scour")
                    .Replace("bridge", "br")
                    .Replace("root", "")
                    .Replace("image files", "img")
                    .Replace("files", "")
                    .Replace("documents", "docs").TrimEnd('_').TrimStart('_');

            }

            var stripCharsPattern = @"(?i)(?<chars>[\W.,;_@_\- -[\\/]])+|(?<slashes>[\\/])+";
            var stripCharsReplace = @"_";
            string key = Regex.Replace($"{name1}_{name2}".Trim(), stripCharsPattern, stripCharsReplace, RegexOptions.IgnoreCase).TrimEnd('_').TrimStart('_').ToLower();
            string appStringFmt = $@"<add key=""fldr_{{0}}"" value=""{{1}}"" />";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(string.Format(appStringFmt, key, settingValue));
            XmlNode newNode = doc.DocumentElement;
            return (newNode);

        }

        private DataTable GenerateFolderTable(string dataTableName = "", string defFileName = "")
        {
            try
            {
                //if (!System.IO.File.Exists(defFileName)
                //{
                //	throw new ArgumentException($@"File {defFileName} not found.");
                //}

                var theDt = new DataTable(dataTableName ?? "FolderTree");

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = @"LEVEL",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(int)
                });

                theDt.Columns.Add(@"RESOURCE_ID", typeof(int));

                theDt.Columns.Add(@"PARENT_ID", typeof(int));

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = @"FOLDER_DEFINITION",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(string)
                });

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = @"BRIDGE_FOLDER",
                    Unique = false,
                    AllowDBNull = false,
                    DefaultValue = true,
                    DataType = typeof(bool)
                });

                // General Types
                string BridgeFoldersRootDef = JsonConvert.SerializeObject(new { folderName = @"bridge", parentfolderName = @"BridgeIdentifier", folderDescription = @"Bridge Folders Root" });
                string FormFoldersRootDef = JsonConvert.SerializeObject(new { folderName = @"formtype", parentfolderName = @"forms", folderDescription = @"Form Folders Root" });
                string NBIRootDef = JsonConvert.SerializeObject(new { folderName = @"NBI", parentfolderName = @"", folderDescription = @"NBI Inspection Root" });
                string FCInspsRootDef = JsonConvert.SerializeObject(new { folderName = @"SPI/FC", parentfolderName = @"", folderDescription = @"Fracture Critical Inspection Root" });
                string UWInspsRootDef = JsonConvert.SerializeObject(new { folderName = @"SPI/UW", parentfolderName = @"", folderDescription = @"Underwater Inspection Root" });
                string SPI_OS_RootDef = JsonConvert.SerializeObject(new { folderName = @"SPI/OS", parentfolderName = @"", folderDescription = @"Other Special Inspection Root" });
                string SPI_DMG_RootDef = JsonConvert.SerializeObject(new { folderName = @"SPI/DMG", parentfolderName = @"", folderDescription = @"Damage and Accident Investigation Root" });
                string SPI_PH_RootDef = JsonConvert.SerializeObject(new { folderName = @"SPI/PH", parentfolderName = @"", folderDescription = @"Pin and Hanger Inspection Root" });
                string BridgeDesignPlans_RootDef = JsonConvert.SerializeObject(new { folderName = @"DSGN", parentfolderName = @"", folderDescription = @"Bridge-Specific Design Plans Root" });
                string ScourRootDef = JsonConvert.SerializeObject(new { folderName = @"SCOUR", parentfolderName = @"", folderDescription = @"Scour Program Documents Root" });
                string LoadRatingsRootDef = JsonConvert.SerializeObject(new { folderName = @"LR", parentfolderName = @"", folderDescription = @"Load Rating Program Documents Root" });
                string CriticalFindingsRootDef = JsonConvert.SerializeObject(new { folderName = @"CIF", parentfolderName = @"", folderDescription = @"Critical Findings Inspection Documents Root" });
                // Subtypes
                string AllSubsFolderDef = JsonConvert.SerializeObject(new { folderName = @"ALL", folderDescription = @"All Documents" });
                string GeneralSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"GEN", folderDescription = @"General" });
                string ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"RPT", folderDescription = @"Reports" });
                string ImagesSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"PIX", folderDescription = @"Image Files" });
                string FormsSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"FRM", folderDescription = @"Form Files" });
                string Scour113ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"113", folderDescription = @"113 Reports" });
                // all scour POA files go to the same folder
                string ScourOriginalPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = @"POA", folderDescription = @"Plans of Action - Original" });
                string ScourAmendedPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = @"POA", folderDescription = @"Plans of Action - Amended" });
                string ScourRetiredPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = @"POA", folderDescription = @"Plans of Action - Retired" });

                // plan subtypes
                string CADDSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"DRWG", folderDescription = @"CADD Drawings - electronic" });
                string PlansSubfolderDef = JsonConvert.SerializeObject(new { folderName = @"PLANS", folderDescription = @"Scanned plans - static" });
                string LRFieldInvestigationsTypeDef = JsonConvert.SerializeObject(new { folderName = @"FLDINV", folderDescription = @"Field Investigation Reports" });
                string LRBridgeStructuralModels = JsonConvert.SerializeObject(new { folderName = @"MODEL", folderDescription = @"Bridge Structural Models" });
                string LRBridgeDataFiles = JsonConvert.SerializeObject(new { folderName = @"DATA", folderDescription = @"Bridge Files" });
                string PhotosFolderDef = JsonConvert.SerializeObject(new { folderName = @"PHOTOS", folderDescription = @"PHOTOS not categorized Root" });
                string QAQCFolderDef = JsonConvert.SerializeObject(new { folderName = @"QAQC", folderDescription = @"QAQC documents Root" });




                theDt.Rows.Add(new object[4] { 0, 0, -1, /* "10","", */ BridgeFoldersRootDef });
                // the codes here are the same as DOC_TYPE_KEY and DOC_SUBTYPE_KEY, so we can calculate those from the integers.
                theDt.Rows.Add(new object[4] { 1, 10, 0, /*  "10","", */ NBIRootDef });
                theDt.Rows.Add(new object[4] { 2, 1000, 10, /*  "1000","10", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 1010, 10, /*  "1010","10", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1020, 10, /* "1020", "10", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1088, 10, /*  "1088","10", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 1099, 10, /*  "1099","10", */ FormsSubfolderDef, false });

                // Special inspections (SPI) - Fracture Critical Inspections
                theDt.Rows.Add(new object[4] { 1, 20, 0, /*  "20","", */ FCInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 2000, 20, /*  "2000","20", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 2010, 20, /*  "2010","20", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 2020, 20, /*  "2020","20", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 2088, 20, /*  "20","2088", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 2099, 20,   /* "2099","20", */ FormsSubfolderDef, false });


                // Special inspections (SPI) - Underwater Inspections (Dive/Wade)
                theDt.Rows.Add(new object[4] { 1, 25, 0,    /* "25","", */ UWInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 2500, 25, /*  "2500","25", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 2510, 25, /*  "2510","25", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 2520, 25, /* "2520", "25", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 2588, 25, /*  "2588","25", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 2599, 25, /* "2599", "25", */ FormsSubfolderDef, false });

                // Special inspections (SPI) - OS special inspections - Other, not Damage or P&H
                theDt.Rows.Add(new object[4] { 1, 30, 0,    /* "30","", */ SPI_OS_RootDef });
                theDt.Rows.Add(new object[4] { 2, 3010, 30, /* "3010", "30", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3020, 30, /*  "3020", "30", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3088, 30, /* "3088","30", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 3099, 30, /*  "3099", "30", */ FormsSubfolderDef, false });

                // Critical Findings Documents
                theDt.Rows.Add(new object[4] { 1, 35, 0, /* "35", "", */ CriticalFindingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 3500, 35, /*  "3500","35", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 3510, 35, /* "3510", "35", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3520, 35, /* "3520", "35", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3588, 35, /* "3588", "35", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 3599, 35, /*  "3599", "35", */ FormsSubfolderDef, false });

                // Special inspections (SPI) - ACCIDENT AND DAMAGE Investigations
                theDt.Rows.Add(new object[4] { 1, 40, 0, /* "40","", */ SPI_PH_RootDef });
                theDt.Rows.Add(new object[4] { 2, 4000, 40, /*  "4000","40", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 4010, 40, /*  "4010",  "40", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4020, 40, /*  "4020", "40", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4088, 40,   /* "4088","40", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 4099, 40, /*  "4099","40", */ FormsSubfolderDef, false });

                // Special inspections (SPI)  - PIN AND HANGER
                theDt.Rows.Add(new object[4] { 1, 45, 0,   /* "45", "", */ SPI_DMG_RootDef });
                theDt.Rows.Add(new object[4] { 2, 4500, 45, /*  "4500","45", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 4510, 45, /*  "4510", "45", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4520, 45, /*  "4520", "45", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4588, 45, /* "4588", "45", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 4599, 45,   /* "4599", "45", */ FormsSubfolderDef, false });



                // Scour Program Documents - reports, evals, plans of action
                theDt.Rows.Add(new object[4] { 1, 50, 0,   /* "50","", */ ScourRootDef });
                theDt.Rows.Add(new object[4] { 2, 5000, 50, /*  "5000","50", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 5010, 50, /*  "5010", "50", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5020, 50, /*  "5020", "50", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5030, 50, /* "5030", "50", */ Scour113ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5040, 50, /*  "5040","50", */ ScourOriginalPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5045, 50, /* "5045","50", */ ScourAmendedPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5050, 50, /*  "5050","50", */ ScourRetiredPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5088, 50, /*  "5088","50", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 5096, 50, /* "5096", "50", */ FormsSubfolderDef, false });
                theDt.Rows.Add(new object[5] { 2, 5097, 50, /* "5097", "50", */ FormsSubfolderDef, false });
                theDt.Rows.Add(new object[5] { 2, 5098, 50, /* "5098", "50", */ FormsSubfolderDef, false });
                theDt.Rows.Add(new object[5] { 2, 5099, 50, /* "5099", "50", */ FormsSubfolderDef, false });



                //Load Rating Program documents
                theDt.Rows.Add(new object[4] { 1, 60, 0, /* "60", "", */ LoadRatingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 6000, 60, /*  "6000","60", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 6010, 60, /* "60", "6010", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 6020, 60, /* "60", "6020", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 6030, 60, /* "60", "6030", */ LRFieldInvestigationsTypeDef });
                theDt.Rows.Add(new object[4] { 2, 6040, 60, /* "60", "6040", */ LRBridgeStructuralModels });
                theDt.Rows.Add(new object[4] { 2, 6050, 60, /* "60", "6050", */ LRBridgeDataFiles });
                theDt.Rows.Add(new object[4] { 2, 6088, 60, /* "60", "6088", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 6099, 60, /* "60", "6099", */ FormsSubfolderDef, false });

                //Bridge Plans (per bridge) - electronic CADD and scans
                theDt.Rows.Add(new object[4] { 1, 70, 0, /* "70", "", */ BridgeDesignPlans_RootDef });
                theDt.Rows.Add(new object[4] { 2, 7000, 70, /*  "7000","70", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 7010, 70, /* "7010","70", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 7020, 70, /* "7020","70",  */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 7030, 70, /*  "7030","70", */ CADDSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 7040, 70, /* "7040","70", */ PlansSubfolderDef }); // no pix.
                theDt.Rows.Add(new object[5] { 2, 7099, 70, /*  "7099","70", */ FormsSubfolderDef, false });


                // Generic photos folders for bridges - if not stored elsewhere.
                theDt.Rows.Add(new object[4] { 1, 80, 0,   /* "80", "", */ PhotosFolderDef });
                theDt.Rows.Add(new object[4] { 2, 8000, 80, /*  "8000","80", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 8010, 80,   /* "8010", "80", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 8088, 80, /*  "8088", "80", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 8099, 80, /*  "8099", "80", */ FormsSubfolderDef, false });

                // QAQC documents and photos for bridges
                theDt.Rows.Add(new object[4] { 1, 90, 0,   /* "90", "", */ QAQCFolderDef });
                theDt.Rows.Add(new object[4] { 2, 9000, 90, /*  "9000","90", */ AllSubsFolderDef });
                theDt.Rows.Add(new object[4] { 2, 9010, 90, /*  "9010", "90", */ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 9020, 90, /*  "9020", "90", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 9088, 90, /*  "9088", "90", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[5] { 2, 9099, 90,   /* "9099", "90", */ FormsSubfolderDef, false });

                var dv = theDt.DefaultView;

                dv.Sort = "PARENT_ID asc, RESOURCE_ID asc";
                var sortedDt = dv.ToTable("SortedFolderDefinitions");
                return sortedDt;
            }
            catch
            {
                throw;
            }

        }

        /// <summary>
        /// Generate the form folder definitions by major type e.g. NBI/FRM, SPI/UW/FRM etc.
        /// </summary>
        /// <param name="jsonRoot"></param>
        /// <param name="formsRoot"></param>
        /// <returns></returns>
        public string GenerateFormFolderDefs(string jsonRoot, string formsRoot)
        {
            //var dtFolders = GenerateFolderTable();

            Dictionary<int, DocFolder> dict = (from row in DocFoldersTable.AsEnumerable()
                                               where row.Field<bool>(@"BRIDGE_FOLDER") == false
                                                     || (row.Field<int>(@"RESOURCE_ID").ToString() != "0" && row.Field<int>("PARENT_ID").ToString() == "0") // 0,0
                                                     || (row.Field<int>(@"RESOURCE_ID").ToString() == "0" && row.Field<int>("PARENT_ID").ToString() == "-1")
                                               select new DocFolder
                                               {
                                                   Level = row.Field<int>(@"LEVEL"),
                                                   Id = row.Field<int>(@"RESOURCE_ID"),
                                                   Parent_Id = row.Field<int>(@"PARENT_ID"),
                                                   DocType = row.Field<int>(@"RESOURCE_ID").ToString(),
                                                   Parent_DocType = row.Field<int>(@"PARENT_ID").ToString(),
                                                   DocFolderName = ((row.Field<int>(@"LEVEL") == 0) ? $@"/" :
                                                       string.Concat(((dynamic)(JsonConvert.DeserializeObject(row.Field<string>(@"FOLDER_DEFINITION"))))?.folderName.ToString())),
                                                   DocFolderDescription = @"",
                                                   DocFolderPath = (row.Field<int>(@"LEVEL") == 0) ? "/" : string.Empty,
                                                   MetaDataGuid = Guid.NewGuid().CleanGuid()
                                               }).ToDictionary(t => t.Id);


            //var dict =
            //dtFolders.Rows.Cast<DataRow>()
            //         .Where (r2 => (r2.Field<string>("BRIDGE_FOLDER").ToString().Contains("T") &&
            //                               r2.Field<int>("PARENT_ID").ToString() == r2.Field<int>("RESOURCE_ID").ToString().Substring(0, 2)) //1099,10
            //         || (r2.Field<int>("RESOURCE_ID").ToString() != "0" && r2.Field<int>("PARENT_ID").ToString() == "0") // 0,0
            //         || (r2.Field<int>("RESOURCE_ID").ToString() == "0" && r2.Field<int>("PARENT_ID").ToString() == "-1")) // 10, 0
            //         .Select(r => new DocFolder
            //         {
            //             Level = row.Field<int>("LEVEL"),
            //             Id = row.Field<int>("RESOURCE_ID"),
            //             Parent_Id = row.Field<int>("PARENT_ID"),
            //             DocType = row.Field<int>("RESOURCE_ID").ToString(),
            //             Parent_DocType = row.Field<int>("PARENT_ID").ToString(),
            //             DocFolderName = ((row.Field<int>("LEVEL") == 0) ? $@"/" :
            //             string.Concat(((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION"))))?.folderName.ToString())),
            //             DocfolderDescription = @"",
            //             DocFolderPath = (row.Field<int>("LEVEL") == 0) ? "/" : string.Empty,
            //             MetaDataGuid = Guid.NewGuid().CleanGuid()
            //         })
            //        .ToDictionary(m => m.Id);

            var docFolders = new List<DocFolder>();

            foreach (var kvp in dict)
            {
                var folder = docFolders;
                var item = kvp.Value;
                // item.Id.Dump();
                //item.parentId.Dump();
                //  kvp.Value.Dump();

                if (item.Parent_Id == 0)
                {
                    item.DocFolderPath = Regex.Replace(string.Concat(formsRoot, System.IO.Path.Combine(dict[0].DocFolderName, dict[item.Parent_Id].DocFolderName, item.DocFolderName)), @"(?i)[\\]+", @"/", RegexOptions.IgnoreCase);
                    item.DocFolderDescription = string.Concat(item?.DocFolderName, @" Forms - Main");

                }



                if (item.Level > 1) // a real subfolder e.g. UW or SPI/OS
                {
                    //Console.WriteLine(item.parentId);
                    // Console.WriteLine($@"{dict[0].docFolderName}/{dict[item.parentId].docFolderName}/{item.docFolderName}"  );
                    item.DocFolderPath = Regex.Replace(string.Concat(formsRoot, System.IO.Path.Combine(dict[0].DocFolderName, dict[item.Parent_Id].DocFolderName, item.DocFolderName)), @"(?i)[\\]+", @"/", RegexOptions.IgnoreCase);
                    item.DocFolderDescription = string.Concat(dict[item.Parent_Id].DocFolderName, " Forms And Templates");
                }

                if (item.Parent_Id >= 0)
                {
                    folder = dict[item.Parent_Id].DocSubFolder;
                }

                folder.Add(item);
            }

            var jsonString = $"{{ \"{jsonRoot}\": {JsonConvert.SerializeObject(docFolders, Newtonsoft.Json.Formatting.Indented)} {Environment.NewLine} }}";
            return jsonString;
        }

        /// <summary>
        /// Return a Json string of folder definitions for a given bridge identifiers (entire tree)
        /// </summary>
        /// <param name="jsonRoot"></param>
        /// <param name="bridgeIdentifier"></param>
        /// <param name="bridge_gd"></param>
        /// <param name="district"></param>
        /// <param name="county"></param>
        /// <param name="bridgegroup"></param>
        /// <param name="docRoot"></param>
        /// <returns></returns>
        public string GenerateBridgeFolderDefsJson(string jsonRoot, string bridgeIdentifier, string district, string county, string bridgeGroup, string docRoot)
        {
            try
            {


                var prefix = System.IO.Path.Combine(docRoot, district, county, bridgeGroup).Replace("\\", "/");
                //var dtFolders = GenerateFolderTable();

                // skip document folders that are not column BRIDGEFOLDER=true for bridges - just real bridge documents
                var dict = (from row in DocFoldersTable.AsEnumerable()
                            where row.Field<bool>(@"BRIDGE_FOLDER")
                                  || (row.Field<int>(@"RESOURCE_ID").ToString() != "0" && row.Field<int>("PARENT_ID").ToString() == "0") // 0,0
                                  || (row.Field<int>(@"RESOURCE_ID").ToString() == "0" && row.Field<int>("PARENT_ID").ToString() == "-1")
                            select new DocFolder
                            {
                                Level = row.Field<int>(@"LEVEL"),
                                Id = row.Field<int>(@"RESOURCE_ID"),
                                Parent_Id = row.Field<int>(@"PARENT_ID"),
                                DocType = row.Field<int>(@"RESOURCE_ID").ToString(),
                                Parent_DocType = row.Field<int>(@"PARENT_ID").ToString(),
                                DocFolderName = ((row.Field<int>(@"LEVEL") == 0) ? $@"/" :
                                    string.Concat(((dynamic)(JsonConvert.DeserializeObject(row?.Field<string>(@"FOLDER_DEFINITION"))))?.folderName.ToString())),
                                DocFolderDescription = @"",
                                DocFolderPath = (row.Field<int>(@"LEVEL") == 0) ? "/" : string.Empty,
                                MetaDataGuid = Guid.NewGuid().CleanGuid()
                            }).ToDictionary(t => t.Id);

                //var dict =
                //dtFolders.Rows.Cast<DataRow>()
                //    .Where(r2 => !(r2.Field<string>("BRIDGE_FOLDER").ToString().Contains( "T" ) &&
                //                     r2.Field<int>("PARENT_ID").ToString() == r2.Field<int>("RESOURCE_ID").ToString().Substring(0, 2)) //1099,10
                //                 || (r2.Field<int>("RESOURCE_ID").ToString() != "0" && r2.Field<int>("PARENT_ID").ToString() == "0") // 0,0
                //                 || (r2.Field<int>("RESOURCE_ID").ToString() == "0" && r2.Field<int>("PARENT_ID").ToString() == "-1")) // 10, 0     
                //    .Select(r => new DocFolder
                //    {
                //        Level = row.Field<int>("LEVEL"),
                //        Id = row.Field<int>("RESOURCE_ID"),
                //        Parent_Id = row.Field<int>("PARENT_ID"),
                //        DocType = row.Field<int>("RESOURCE_ID").ToString(),
                //        Parent_DocType = row.Field<int>("PARENT_ID").ToString(),
                //        DocFolderName = ((row.Field<int>("LEVEL") == 0) ? $@"/{bridgeIdentifier}" :
                //             string.Concat(((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION"))))?.folderName.ToString())),
                //        DocFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION"))))?.folderDescription.ToString(),
                //        DocFolderPath = string.Empty,
                //        MetaDataGuid = Guid.NewGuid().CleanGuid()
                //    })
                //        .ToDictionary(m => m.Id);
                //dict.Dump();

                var docFolders = new List<DocFolder>();

                foreach (var kvp in dict)
                {
                    var folder = docFolders;
                    var item = kvp.Value;
                    if (item.Parent_Id >= 0)
                    {
                        folder = dict[item.Parent_Id].DocSubFolder;
                    }

                    if (item.Level > 1) // a real subfolder e.g. UW or SPI/OS
                    {
                        //Console.WriteLine(item.parentId);
                        // Console.WriteLine($@"{dict[0].docFolderName}/{dict[item.parentId].docFolderName}/{item.docFolderName}"  );
                        item.DocFolderPath = Regex.Replace(string.Concat(prefix, System.IO.Path.Combine(dict[0].DocFolderName, dict[item.Parent_Id].DocFolderName, item.DocFolderName)), @"(?i)[\\]+", @"/", RegexOptions.IgnoreCase);
                    }

                    folder.Add(item);

                }

                var jsonString = $"{{ \"{jsonRoot}\": {JsonConvert.SerializeObject(docFolders, Newtonsoft.Json.Formatting.Indented)} {Environment.NewLine} }}";
                return jsonString;

            }
            catch (NullReferenceException nullEx)
            {
                Console.WriteLine(nullEx);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public IEnumerable<JToken> GetBridgeFoldersByTypeKey(string json, string docTypeKey)
        {
            return GetBridgeFoldersByTypeKey(JObject.Parse(json), docTypeKey);
        }

        private IEnumerable<JToken> GetBridgeFoldersByTypeKey(JObject json, string docTypeKey)
        {

            //JObject hive = JObject.Parse(json);
            IEnumerable<JToken> paths = json.SelectTokens($"$..DocSubFolder[?(@.Parent_DocType == '{docTypeKey}' )].DocFolderPath");

            return paths;

        }

        public string GetDocFolder(string bridgeIdentifier, string docSubTypeKey)
        {
            return GetBridgeFolderBySubTypeKey(GetAllBridgeFoldersJson(bridgeIdentifier), docSubTypeKey);
        }

        public string GetBridgeFolderBySubTypeKey(string json, string docSubTypeKey)
        {
            return GetBridgeFolderBySubTypeKey(JObject.Parse(json), docSubTypeKey);
        }

        public string GetBridgeFolderBySubTypeKey(JObject json, string docSubTypeKey)
        {
            var result = string.Empty;

            JToken token = json.SelectToken($"$..DocSubFolder[?(@.DocType == '{docSubTypeKey}' )].DocFolderPath");

            // result = token?.Value<string>();
            if (!string.IsNullOrEmpty(token?.Value<string>()))
            {
                result = string.Concat(token.Value<string>().Trim(), token.Value<string>().Trim().EndsWith("/") ? "" : "/");
            }

            return result;
        }

        /// <summary>
        /// return a IEnumerable&lt;JToken&gt; of all the  form folder paths in the JSON string
        /// </summary>
        /// <param name="json">A JSON string of folder paths </param>
        /// <returns>IEnumerable&lt;JToken&gt;</returns>
        public IEnumerable<JToken> GetAllDocumentFolders(string json)
        {
            return GetAllDocumentFolders(JObject.Parse(json));
        }

        /// <summary>
        /// return a IEnumerable&lt;JToken&gt; of all the document paths in the JSON string
        /// </summary>
        /// <param name="json">A JSON string of folder paths</param>
        /// <returns>IEnumerable&lt;JToken&gt;</returns>
        public IEnumerable<JToken> GetAllDocumentFolders(JObject json)
        {

            IEnumerable<JToken> paths = json.SelectTokens($"$..DocSubFolder[?(@.Parent_DocType <> '0' )].DocFolderPath");
            return paths;
        }

        // return a JSON string of folder definitions and program bits
        // return a JSON string of folder definitions and program bits
        public string GetFolderSubFolderAppSettings()
        {
            var json = string.Empty;
            var jsonRoot = "folders";

            //var dtFolders = GenerateFolderTable();
            var dict = (from row in DocFoldersTable.AsEnumerable()
                        select new DocFolder
                        {
                            Level = row.Field<int>("LEVEL"),
                            Id = row.Field<int>("RESOURCE_ID"),
                            Parent_Id = row.Field<int>("PARENT_ID"),
                            DocType = row.Field<int>("RESOURCE_ID").ToString(),
                            Parent_DocType = row.Field<int>("PARENT_ID").ToString(),
                            DocFolderName = ((row.Field<int>("LEVEL") == 0) ? string.Empty :
                                         string.Concat(((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderName.ToString())),
                            DocFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderDescription.ToString(),
                            DocFolderPath = string.Empty,
                            MetaDataGuid = Guid.NewGuid().CleanGuid(),
                            AppSettingsKeyName = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderDescription.ToString().Replace(" ", "_").Replace("___", "_").Replace("__", "_").Replace("-", "_"),
                            AppSettingsValue = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderName.ToString(),
                            //             AppSettingsFolderPath = string.Empty
                        }).ToDictionary(t => t.Id);

            //var dict =
            //dtFolders.Rows.Cast<DataRow>()
            //         .Select(r => new DocFolder
            //         {
            //             Level = row.Field<int>("LEVEL"),
            //             Id = row.Field<int>("RESOURCE_ID"),
            //             Parent_Id = row.Field<int>("PARENT_ID"),
            //             DocType = row.Field<int>("RESOURCE_ID").ToString(),
            //             Parent_DocType = row.Field<int>("PARENT_ID").ToString(),
            //             DocFolderName = ((row.Field<int>("LEVEL") == 0) ? string.Empty :
            //             string.Concat(((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderName.ToString())),
            //             DocFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderDescription.ToString(),
            //             DocFolderPath = string.Empty,
            //             MetaDataGuid = Guid.NewGuid().CleanGuid(),
            //             AppSettingsKeyName = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderDescription.ToString().Replace(" ", "_").Replace("___", "_").Replace("__", "_").Replace("-", "_"),
            //             AppSettingsValue = ((dynamic)(JsonConvert.DeserializeObject(row.Field<string>("FOLDER_DEFINITION")))).folderName.ToString(),
            //             AppSettingsFolderPath = string.Empty
            //         })
            //        .ToDictionary(m => m.Id);


            var docFolders = new List<DocFolder>();

            foreach (var kvp in dict)
            {
                var folder = docFolders;
                var item = kvp.Value;
                if (item.Parent_Id >= 0)
                {
                    folder = dict[item.Parent_Id].DocSubFolder;
                }
                var serialize = "";

                const string regexWinBackSlashPattern = @"[\\]+";
                const string replacePattern = @"/";
                const string stripCharsPattern = @"(?i)(?<chars>[\W.,;_@_\- -[\\/]])+|(?<slashes>[\\/])+";
                const string stripCharsReplace = @"_";

                switch (item.Level)
                {
                    case 0:
                        item.DocFolderPath = "/";
                        item.AppSettingsFolderPath = string.Empty;
                        break;

                    case 1:
                        {
                            item.DocFolderPath = Regex.Replace(string.Concat("/", item.DocFolderName, "/"), regexWinBackSlashPattern, replacePattern, RegexOptions.IgnoreCase);
                            serialize = Newtonsoft.Json.JsonConvert.SerializeXmlNode(GetAppString(item.AppSettingsKeyName + "_" + item.Id.ToString(), string.Empty, item.DocFolderPath), Newtonsoft.Json.Formatting.None, true);
                            item.AppSettingsFolderPath = serialize;
                            break;
                        }

                    case 2:
                        {
                            item.DocFolderPath = Regex.Replace(string.Concat("/", dict[item.Parent_Id].DocFolderName, "/", item.DocFolderName)
                            , regexWinBackSlashPattern
                            , replacePattern, RegexOptions.IgnoreCase);
                            serialize = Newtonsoft.Json.JsonConvert.SerializeXmlNode(
                            GetAppString(string.Concat(
                             Regex.Replace(
                             dict[item.Parent_Id].DocFolderDescription.Trim(), stripCharsPattern
                             , stripCharsReplace
                             , RegexOptions.IgnoreCase)
                             .ToLower()
                             .TrimEnd('_')
                             .TrimStart('_'), stripCharsReplace, item.AppSettingsKeyName).ToLower(), Regex.Replace(item.DocFolderName.ToLower().Trim() + "_" + item.Id.ToString(), stripCharsPattern, stripCharsReplace).ToLower(), string.Concat(item.DocFolderPath, "/")), Newtonsoft.Json.Formatting.None, true);
                            item.AppSettingsFolderPath = serialize;
                            break;

                        }
                }
                //serialize.Dump();


                folder.Add(item);

            }

            json = $"{{  \"{jsonRoot}\": {JsonConvert.SerializeObject(docFolders, Newtonsoft.Json.Formatting.Indented)} {Environment.NewLine} }} ";
            return json;
        }


        /// <summary>
        /// get a generated list of app settings key value pairs for every type of document folder (root or detail)
        /// </summary>
        /// <para>
        /// {"@key":"doc_fldr_load_rating_program_documents_bridge_structural_models_model","@value":"/LR/MODEL/"}
        /// becomes
        /// &lt;add key="doc_fldr_load_rating_program_documents_bridge_structural_models_model" value="/LR/MODEL/" /&gt;
        ///</para>
        /// <returns></returns>
        public List<string> GetDocFolderAppSettings()
        {
            var jsonSettings = GetFolderSubFolderAppSettings();
            var tokens = (JObject.Parse(jsonSettings)).SelectTokens($"$..DocSubFolder[?(@.Level >= 1 )].AppSettingsFolderPath").ToList();
            var settings = new List<string>();

            foreach (var setting in tokens.Select(token => Newtonsoft.Json.JsonConvert.DeserializeXmlNode(token.ToString(), "add")))
            {
                if (setting == null)
                {
                    throw new NullReferenceException("Setting token is unexpectedly null in GetDocFolderAppSettings");
                }
                settings.Add(setting.InnerXml.ToString());
            }
            return settings;
        }

        /// <summary>
        /// Return the generated folders for a given bridge identifier (brkey)
        /// </summary>
        /// <param name="bridgeIdentifier"></param>
        /// <returns>JSon string of folder tree</returns>
        public string GetAllBridgeFoldersJson(string bridgeIdentifier)
        {

            var conn = new OracleConnection()
            {
                ConnectionString = "Data Source=10.181.74.44:1521/ESOADEV.WORLD; User ID=KDOT_BLP; Password=eis3nh0wer;"
            };

            var cmd = new OracleCommand()
            {
                CommandText = @"SELECT DISTINCT Br.Brkey
                      -- ,Br.Bridge_Gd
                       ,P2.Shortdesc AS District
                       ,P1.Shortdesc AS County
                       ,Nvl(Br.Bridgegroup, 'UNASSIGNED') AS Bridgegroup
                       ,P1.Parmvalue AS Kdot_County
                       ,Br.County AS Nbi_County
          FROM Kdotblp_Documents Kd
         INNER JOIN Bridge Br
            ON Kd.Bridge_Gd = Br.Bridge_Gd
         INNER JOIN Paramtrs P1
            ON P1.Longdesc = Br.County
           AND P1.Table_Name = 'bridge'
           AND P1.Field_Name = 'county'
         INNER JOIN Paramtrs P2
            ON P2.Parmvalue = Br.District
           AND P2.Table_Name = 'bridge'
           AND P2.Field_Name = 'district'
         WHERE Kd.Doc_Status = '1' AND kd.BRKEY =:the_brkey  -- <=1000
         GROUP BY Br.Brkey
                 ,Br.Bridge_Gd
                 ,P2.Shortdesc
                 ,P1.Shortdesc
                 ,Nvl(Br.Bridgegroup, 'UNASSIGNED')
                 ,P1.Parmvalue
                 ,Br.County
        ",

                CommandType = CommandType.Text,
                BindByName = true,
                Connection = conn
            };

            var parm = new OracleParameter()
            {
                ParameterName = "the_brkey",
                OracleDbType = OracleDbType.Varchar2,
                Size = 15,
                Direction = ParameterDirection.Input,
                Value = bridgeIdentifier
            };

            cmd.Parameters.Add(parm);

            cmd.Connection.Open();
            var dt = new DataTable();
            using (cmd)
            {
                var a = new OracleDataAdapter(cmd);
                var rows = a.Fill(dt);
                cmd.Connection.Close();
            }

            // string bridgeGd = dt.Rows[0].Field<string>("BRIDGE_GD");
            string district = dt.Rows[0].Field<string>("DISTRICT");
            string county = dt.Rows[0].Field<string>("COUNTY");
            string bridgeGroup = dt.Rows[0].Field<string>("BRIDGEGROUP");
            string docRoot = System.IO.Path.Combine(DocDrive, DocRootFolder);
            string jsonRootId = @"folders";

            return GenerateBridgeFolderDefsJson(jsonRootId, bridgeIdentifier, district, county, bridgeGroup, docRoot);

        }

        /// <summary>
        /// Generate the retrieval url for a given bridge by doc_type_key and doc_subtype_key, optionally by inspevnt 
        /// </summary>
        /// <param name="theDocTypeKey">e.g. 20 for FC</param>
        /// <param name="theDocSubtypeKey">e.g. 2010 for FC reports</param>
        /// <param name="theDocViewerPageName">name of the aspx page for the docviewer</param>
        /// <param name="theBridgeGd">Required the given bridge_gd </param>
        /// <param name="theInspevntGd">Optional to only pick documents related to a specific inspection by inspevnt_gd</param>
        /// <param name="theShowWaitDialog">Optionally show the Wait dialog</param>
        /// <returns></returns>
        public string GenDocViewerNavigateUrl(string theDocTypeKey, string theDocSubtypeKey, string theDocViewerPageName = "DocViewer.aspx", string theBridgeGd = "", string theInspevntGd = "", bool theShowWaitDialog = true)
        {
            if (!theDocTypeKey.EndsWith("99"))
            {
                Debug.Assert(!string.IsNullOrEmpty(theBridgeGd));
            }

            Debug.Assert(!string.IsNullOrEmpty(theDocTypeKey)); // absolute bare minimum...
            //Debug.Assert(!string.IsNullOrEmpty(theDocSubtypeKey));


            theDocViewerPageName = theDocViewerPageName ?? "DocViewer.aspx";
            var urlStem = (theShowWaitDialog ? $@"Wait.aspx?redirectPage={ theDocViewerPageName }" : theDocViewerPageName + "?"); // start arguments to docViewerPageName after ? if not using wait dialog



            
            var docTypes = "";
            if (string.Equals(theDocTypeKey, "*.*"))
            {
                docTypes = $@"&doc_type_key={"*.*"}";
            }
            else
            {
                theDocTypeKey = (theDocSubtypeKey.EndsWith("99")) ? theDocSubtypeKey.Substring(0, 2) : theDocTypeKey;
                theDocSubtypeKey = (string.IsNullOrEmpty(theDocSubtypeKey) || theDocSubtypeKey.EndsWith("00",StringComparison.OrdinalIgnoreCase)) ? "" : theDocSubtypeKey;

                docTypes = string.IsNullOrEmpty(theDocSubtypeKey)? $"&doc_type_key={theDocTypeKey}" : "" + (!string.IsNullOrEmpty(theDocSubtypeKey) ? $"&doc_subtype_key={theDocSubtypeKey}" : "");
            }

            var identifiers = (!(theDocTypeKey == "99" || (theDocSubtypeKey).EndsWith("99"))) ? string.IsNullOrEmpty(theBridgeGd) ? string.Empty : ($"&bridge_gd={theBridgeGd}" + (string.IsNullOrEmpty(theInspevntGd) ? "" : $"&inspevnt_gd={theInspevntGd}")) : string.Empty;


            var url = string.Concat("~/", urlStem, docTypes, identifiers).Replace("?&", "?");

            return url;
        }

        public bool IsAllowedTypeSubTypePair(string theDocTypeKey, string theDocSubtypeKey)
        {
            return (from kvp in DocTypeSubTypePairs
                    where kvp.Key == theDocTypeKey && kvp.Value == theDocSubtypeKey
                    select kvp).Any();
        }

        public bool IsAllowedType(string theDocTypeKey)
        {
            return (from kvp in DocTypeSubTypePairs
                    where kvp.Key == theDocTypeKey
                    select kvp).Any();
        }

        public bool IsAllowedSubType(string theDocSubtypeKey)
        {
            return (from kvp in DocTypeSubTypePairs
                    where kvp.Key == theDocSubtypeKey
                    select kvp).Any();
        }
    }

    public static class Helpers
    {
        /// <summary>
        /// Static constructor for Helpers
        /// A static constructor is used to initialize any static data, or to perform a particular action that needs performed once only. It is called automatically before the first instance is created or any static members are referenced.
        /// Static constructors have the following properties:
        ///   A static constructor does not take access modifiers or have parameters.
        ///   A static constructor is called automatically to initialize the class before the first instance is created or any static members are referenced.
        ///   A static constructor cannot be called directly.
        ///   The user has no control on when the static constructor is executed in the program.
        ///   A typical use of static constructors is when the class is using a log file and the constructor is used to write entries to this file.
        ///   Static constructors are also useful when creating wrapper classes for unmanaged code, when the constructor can call the LoadLibrary method.
        /// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors
        /// </summary>
        static Helpers()
        {
            ;
        }
        /// <summary>
        /// Helper method to return a generated GUID as a string formatted for BrM
        /// </summary>
        /// <param name="aGuid"></param>
        /// <returns>a string that is a Guid formatted for BrM</returns>
        public static string CleanGuid(this Guid aGuid)
        {
            return aGuid.ToString().ToUpper().Replace("-", "");
        }

        public static string CleanGuid(this string aGuidString, int length = 32, string regexPattern = @"[^0-9A-Z]+")
        {
            if (!(aGuidString.Length <= length))
                throw new ArgumentException(
                    $"The starter string for Guid generation is > the limit of {length} characters long {aGuidString.Length}");
            aGuidString = string.IsNullOrEmpty(aGuidString)
                ? Guid.NewGuid().ToString().ToUpper()
                : aGuidString.ToUpper();
            return Regex.Replace(aGuidString, regexPattern, string.Empty);
        }
    }
}
