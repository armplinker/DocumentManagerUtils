using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;


namespace DocumentManagerUtil
{
    public class DocMgrUtils
    {
        private const string DocDrive = "X:\\";
        private const string DocRootFolder = "Docs";
        private class DocFolder
        {
            public string metaDataGuid { get; set; }
            // ignore the relationship codes.  We only want the string document types.
            [JsonIgnore]
            public int Id { get; set; } //1100
            [JsonIgnore]
            public int parentId { get; set; } //11

            public int level { get; set; }  // 0, 1 , 2 ,3
            public string docType { get; set; } // "1100"
            public string parentDocType { get; set; } // "11"
            public string docFolderName { get; set; }
            public string docFolderDescription { get; set; } // helptext	
            public string docFolderPath { get; set; }
            public string appSettingsKeyName;
            public string appSettingsValue;
            public string appSettingsFolderPath { get; set; }
            public List<DocFolder> docSubFolder { get; set; }

            public DocFolder()
            {
                docSubFolder = new List<DocFolder>();
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
            string appStringFmt = $"<add key=\"fldr_{{0}}\" value=\"{{1}}\" />";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(string.Format(appStringFmt, key, settingValue));
            XmlNode newNode = doc.DocumentElement;
            return (newNode);

        }

        private DataTable CreateBridgeFolderDefs(string dataTableName, string docRootFolder, string adminFolderPath, string brKey, string brGd)
        {
            DataTable theDt = new DataTable(dataTableName);

            theDt.TableName = "FolderDefinitions";

            theDt.Columns.Add("ResourceId", typeof(int));
            theDt.Columns.Add("ParentId", typeof(int));

            // JSON fields
            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "LEVEL",
                Unique = false,
                AllowDBNull = false,
                DataType = typeof(int)
            });



            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "DOCTYPE",
                Unique = true,
                AllowDBNull = false,
                DataType = typeof(string)

            }
                );
            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "PARENTDOCTYPE",
                Unique = false,
                AllowDBNull = true,
                DataType = typeof(string)

            });
            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "FOLDERNAME",
                Unique = false,
                AllowDBNull = false,
                DataType = typeof(string)

            });
            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "PARENTFOLDERNAME",
                Unique = false,
                AllowDBNull = false,
                DataType = typeof(string)

            });

            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "FOLDERPATH",
                Unique = false,
                AllowDBNull = false,
                DataType = typeof(string)
            });

            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "FOLDERDESCRIPTION",
                Unique = false,
                AllowDBNull = false,
                DataType = typeof(string)
            });

            theDt.Columns.Add(new DataColumn()
            {
                ColumnName = "METADATAGUID",
                Unique = true,
                AllowDBNull = false,
                DataType = typeof(string)
            });




            return theDt;
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
                    ColumnName = "LEVEL",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(int)
                });

                theDt.Columns.Add("ResourceId", typeof(int));

                theDt.Columns.Add("ParentId", typeof(int));

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = "FOLDERDEFINITION",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(string)
                });


                // General Types
                string BridgeFoldersRootDef = JsonConvert.SerializeObject(new { folderName = "bridge", parentFolderName = "BridgeIdentifier", folderDescription = "Bridge Folders Root" });
                string NBIRootDef = JsonConvert.SerializeObject(new { folderName = "NBI", parentFolderName = "", folderDescription = "NBI Inspection Root" });
                string FCInspsRootDef = JsonConvert.SerializeObject(new { folderName = "SPI/FC", parentFolderName = "", folderDescription = "Fracture Critical Inspection Root" });
                string UWInspsRootDef = JsonConvert.SerializeObject(new { folderName = "SPI/UW", parentFolderName = "", folderDescription = "Underwater Inspection Root" });
                string SPI_OS_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/OS", parentFolderName = "", folderDescription = "Other Special Inspection Root" });
                string SPI_DMG_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/DMG", parentFolderName = "", folderDescription = "Damage and Accident Investigation Root" });
                string SPI_PH_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/PH", parentFolderName = "", folderDescription = "Pin and Hanger Inspection Root" });
                string BridgeDesignPlans_RootDef = JsonConvert.SerializeObject(new { folderName = "DSGN", parentFolderName = "", folderDescription = "Bridge-Specific Design Plans Root" });
                string ScourRootDef = JsonConvert.SerializeObject(new { folderName = "SCOUR", parentFolderName = "", folderDescription = "Scour Program Documents Root" });
                string LoadRatingsRootDef = JsonConvert.SerializeObject(new { folderName = "LR", parentFolderName = "", folderDescription = "Load Rating Program Documents Root" });
                string CriticalFindingsRootDef = JsonConvert.SerializeObject(new { folderName = "CIF", parentFolderName = "", folderDescription = "Critical Findings Inspection Documents Root" });
                // Subtypes
                string GeneralSubfolderDef = JsonConvert.SerializeObject(new { folderName = "GEN", folderDescription = "General" });
                string ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "RPT", folderDescription = "Reports" });
                string ImagesSubfolderDef = JsonConvert.SerializeObject(new { folderName = "PIX", folderDescription = "Image Files" });
                string FormsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "FRM", folderDescription = "Form Files" });
                string Scour113ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "113", folderDescription = "113 Reports" });
                // all scour POA files go to the same folder
                string ScourOriginalPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Original" });
                string ScourAmendedPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Amended" });
                string ScourRetiredPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Retired" });

                // plan subtypes
                string CADDSubfolderDef = JsonConvert.SerializeObject(new { folderName = "DRWG", folderDescription = "CADD Drawings - electronic" });
                string PlansSubfolderDef = JsonConvert.SerializeObject(new { folderName = "PLANS", folderDescription = "Scanned plans - static" });
                string LRFieldInvestigationsTypeDef = JsonConvert.SerializeObject(new { folderName = "FLDINV", folderDescription = "Field Investigation Reports" });
                string LRBridgeStructuralModels = JsonConvert.SerializeObject(new { folderName = "MODEL", folderDescription = "Bridge Structural Models" });

                string PhotosFolderDef = JsonConvert.SerializeObject(new { folderName = "PHOTOS", folderDescription = "PHOTOS not categorized" });
                string QAQCFolderDef = JsonConvert.SerializeObject(new { folderName = "QAQC", folderDescription = "QAQC documents root" });




                theDt.Rows.Add(new object[4] { 0, 0, -1, /* "10","",*/   BridgeFoldersRootDef });
                // the codes here are the same as DOC_TYPE_KEY and DOC_SUBTYPE_KEY, so we can calculate those from the integers.
                theDt.Rows.Add(new object[4] { 1, 10, 0,  /* "10","",*/   NBIRootDef });
                theDt.Rows.Add(new object[4] { 2, 1000, 10,  /* "1000","10",*/    GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1010, 10, /* "1010", "10",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1088, 10,  /*  "1088","10",*/ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1099, 10,  /*  "1099","10",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 11, 0,  /*  "11","", */ FCInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1100, 11,  /*  "1100","11", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1110, 11,  /*  "1110","11",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1188, 11,  /*   "11","1188",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1199, 11,   /*  "1199","11", */ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 12, 0,    /*  "12","",*/ UWInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1200, 12,  /* "1200","12", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1210, 12, /*  "1210", "12", */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1288, 12,  /*  "1288","12",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1299, 12, /* "1299", "12", */ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 30, 0,    /* "30","",*/ SPI_OS_RootDef });
                theDt.Rows.Add(new object[4] { 2, 3000, 30, /* "3000", "30",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3010, 30,  /*  "3010", "30",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3088, 30, /*   "3088","30",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3099, 30,  /*  "3099", "30",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 14, 0,   /* "14","", */ ScourRootDef });
                theDt.Rows.Add(new object[4] { 2, 1400, 14,  /* "1410", "14",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1410, 14,  /* "1410", "14",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1420, 14, /*   "1420", "14",*/  Scour113ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1430, 14,  /*  "1430","14", */ ScourOriginalPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1435, 14, /*   "1435","14", */ ScourAmendedPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1440, 14,  /*  "1440","14",*/  ScourRetiredPOASubfolderDef });

                theDt.Rows.Add(new object[4] { 2, 1488, 14,  /*  "1488","14",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1499, 14, /* "1499", "14",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 15, 0,  /*"15", "",*/ CriticalFindingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1500, 15, /*  "1500", "15",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1510, 15,  /*"1510", "15", */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1588, 15, /*  "1588", "15", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1599, 15,  /* "1599", "15",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 16, 0, /* "16", "", */ LoadRatingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1600, 16,   /*"16", "1600",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1610, 16,  /* "16", "1610",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1620, 16, /*  "16", "1620", */ LRFieldInvestigationsTypeDef });
                theDt.Rows.Add(new object[4] { 2, 1630, 16,   /*"16", "1630", */ LRBridgeStructuralModels });
                theDt.Rows.Add(new object[4] { 2, 1688, 16, /*  "16", "1688",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1699, 16, /* "16", "1699",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 17, 0, /* "17", "",*/ BridgeDesignPlans_RootDef });
                theDt.Rows.Add(new object[4] { 2, 1700, 17, /*   "1700","17", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1710, 17, /*  "1710","17",  */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1720, 17,  /*  "1720","17",*/  CADDSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1788, 17, /*   "1788","17", */  PlansSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1799, 17,  /*  "1799","17",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 40, 0, /* "40","", */ SPI_PH_RootDef });
                theDt.Rows.Add(new object[4] { 2, 4000, 40,  /* "4000",  "40",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4010, 40,  /* "4010", "40",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4088, 40,   /* "4088","40", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4099, 40,  /*  "4099","40",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 50, 0,   /*  "50", "", */ SPI_DMG_RootDef });
                theDt.Rows.Add(new object[4] { 2, 5000, 50,  /*  "5000", "50",*/ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5010, 50,  /*  "5010", "50", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5088, 50, /*   "5088", "50", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5099, 50,   /* "5099", "50",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 80, 0,   /*  "80", "", */ PhotosFolderDef });
                theDt.Rows.Add(new object[4] { 2, 8088, 80,  /*  "8088", "21",*/ ImagesSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 90, 0,   /*  "90", "", */ QAQCFolderDef });
                theDt.Rows.Add(new object[4] { 2, 9000, 90,  /*  "9000", "90",*/ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 9010, 90,  /*  "9010", "90", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 9088, 90,  /*  "9088", "90",*/ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 9099, 90,   /* "9099", "90",*/ FormsSubfolderDef });

                DataView dv = theDt.DefaultView;

                dv.Sort = "parentid asc, resourceid asc";
                DataTable sortedDT = dv.ToTable();
                return sortedDT;
            }
            catch
            {
                throw;
            }

        }

        private DataTable GenerateFolderTableOLD()//(string defFileName)
        {
            try
            {
                //if (!System.IO.File.Exists(defFileName)
                //{
                //	throw new ArgumentException($@"File {defFileName} not found.");
                //}

                var theDt = new DataTable("FolderTree");

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = "LEVEL",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(int)
                });

                theDt.Columns.Add("ResourceId", typeof(int));

                theDt.Columns.Add("ParentId", typeof(int));

                theDt.Columns.Add(new DataColumn()
                {
                    ColumnName = "FOLDERDEFINITION",
                    Unique = false,
                    AllowDBNull = false,
                    DataType = typeof(string)
                });


                // General Types
                string BridgeFoldersRootDef = JsonConvert.SerializeObject(new { folderName = "bridge", parentFolderName = "bridgeIdentifier", folderDescription = "Bridge Folders Root" });
                string NBIRootDef = JsonConvert.SerializeObject(new { folderName = "NBI", parentFolderName = "", folderDescription = "NBI Inspections Root" });
                string FCInspsRootDef = JsonConvert.SerializeObject(new { folderName = "FC", parentFolderName = "", folderDescription = "Fracture Critical Inspections Root" });
                string UWInspsRootDef = JsonConvert.SerializeObject(new { folderName = "UW", parentFolderName = "", folderDescription = "Underwater Inspections Root" });
                string SPI_OS_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/OS", parentFolderName = "", folderDescription = "Other Special Inspections" });
                string SPI_DMG_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/DMG", parentFolderName = "", folderDescription = "Damage/Accident Investigations" });
                string SPI_PH_RootDef = JsonConvert.SerializeObject(new { folderName = "SPI/PH", parentFolderName = "", folderDescription = "Pin and Hanger Inspections" });
                string BridgeDesignPlans_RootDef = JsonConvert.SerializeObject(new { folderName = "DSGN", parentFolderName = "", folderDescription = "Bridge-specific design plans" });
                string ScourRootDef = JsonConvert.SerializeObject(new { folderName = "SCOUR", parentFolderName = "", folderDescription = "Scour Program Documents" });
                string LoadRatingsRootDef = JsonConvert.SerializeObject(new { folderName = "LR", parentFolderName = "", folderDescription = "Load Rating Program Documents" });
                string CriticalFindingsRootDef = JsonConvert.SerializeObject(new { folderName = "CIF", parentFolderName = "", folderDescription = "Critical Findings Inspection Documents" });
                // Subtypes
                string GeneralSubfolderDef = JsonConvert.SerializeObject(new { folderName = "GEN", folderDescription = "General" });
                string ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "RPT", folderDescription = "Reports" });
                string ImagesSubfolderDef = JsonConvert.SerializeObject(new { folderName = "PIX", folderDescription = "Image Files" });
                string FormsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "FRM", folderDescription = "Form Files" });
                string Scour113ReportsSubfolderDef = JsonConvert.SerializeObject(new { folderName = "113", folderDescription = "113 Reports" });
                // all scour POA files go to the same folder
                string ScourOriginalPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Original" });
                string ScourAmendedPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Amended" });
                string ScourRetiredPOASubfolderDef = JsonConvert.SerializeObject(new { folderName = "POA", folderDescription = "Plans of Action - Retired" });

                // plan subtypes
                string CADDSubfolderDef = JsonConvert.SerializeObject(new { folderName = "DRWG", folderDescription = "CADD Drawings - electronic" });
                string PlansSubfolderDef = JsonConvert.SerializeObject(new { folderName = "PLANS", folderDescription = "Scanned plans - static" });
                string LRFieldInvestigationsTypeDef = JsonConvert.SerializeObject(new { folderName = "FLDINV", folderDescription = "Field Investigation Reports" });


                string LRReferencePlansDef = JsonConvert.SerializeObject(new { folderName = "PLANS", folderDescription = "LR Reference Plans" });
                string LRStructuralModelsDef = JsonConvert.SerializeObject(new { folderName = "MODEL", folderDescription = "Bridge Structural Models" });
                string LRDataFilesDef = JsonConvert.SerializeObject(new { folderName = "DATA", folderDescription = "Bridge SIA Data Files" });
                
                string PhotosFolderDef = JsonConvert.SerializeObject(new { folderName = "PHOTOS", folderDescription = "PHOTOS not categorized" });
                string QAQCFolderDef = JsonConvert.SerializeObject(new { folderName = "QAQC", folderDescription = "QAQC documents root" });




                theDt.Rows.Add(new object[4] { 0, 0, -1, /* "10","",*/   BridgeFoldersRootDef });
                // the codes here are the same as DOC_TYPE_KEY and DOC_SUBTYPE_KEY, so we can calculate those from the integers.
                theDt.Rows.Add(new object[4] { 1, 10, 0,  /* "10","",*/   NBIRootDef });
                theDt.Rows.Add(new object[4] { 2, 1000, 10,  /* "1000","10",*/    GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1010, 10, /* "1010", "10",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1088, 10,  /*  "1088","10",*/ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1099, 10,  /*  "1099","10",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 11, 0,  /*  "11","", */ FCInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1100, 11,  /*  "1100","11", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1110, 11,  /*  "1110","11",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1188, 11,  /*   "11","1188",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1199, 11,   /*  "1199","11", */ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 12, 0,    /*  "12","",*/ UWInspsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1200, 12,  /* "1200","12", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1210, 12, /*  "1210", "12", */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1288, 12,  /*  "1288","12",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1299, 12, /* "1299", "12", */ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 30, 0,    /* "30","",*/ SPI_OS_RootDef });
                theDt.Rows.Add(new object[4] { 2, 3000, 30, /* "3000", "30",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3010, 30,  /*  "3010", "30",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3088, 30, /*   "3088","30",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3099, 30,  /*  "3099", "30",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 14, 0,   /* "14","", */ ScourRootDef });
                theDt.Rows.Add(new object[4] { 2, 1400, 14,  /* "1410", "14",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1410, 14,  /* "1410", "14",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1420, 14, /*   "1420", "14",*/  Scour113ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1430, 14,  /*  "1430","14", */ ScourOriginalPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1435, 14, /*   "1435","14", */ ScourAmendedPOASubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1440, 14,  /*  "1440","14",*/  ScourRetiredPOASubfolderDef });

                theDt.Rows.Add(new object[4] { 2, 1488, 14,  /*  "1488","14",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1499, 14, /* "1499", "14",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 15, 0,  /*"15", "",*/ CriticalFindingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1500, 15, /*  "1500", "15",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1510, 15,  /*"1510", "15", */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1588, 15, /*  "1588", "15", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1599, 15,  /* "1599", "15",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 16, 0, /* "16", "", */ LoadRatingsRootDef });
                theDt.Rows.Add(new object[4] { 2, 1600, 16,   /*"16", "1600",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1610, 16,  /* "16", "1610",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1620, 16, /*  "16", "1620", */ LRFieldInvestigationsTypeDef });
                theDt.Rows.Add(new object[4] { 2, 1630, 16,   /*"16", "1630", */ LRReferencePlansDef });
                theDt.Rows.Add(new object[4] { 2, 1640, 16,   /*"16", "1640", */ LRStructuralModelsDef });
                theDt.Rows.Add(new object[4] { 2, 1650, 16,   /*"16", "1640", */ LRDataFilesDef });
                theDt.Rows.Add(new object[4] { 2, 1688, 16, /*  "16", "1688",*/  ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1699, 16, /* "16", "1699",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 17, 0, /* "17", "",*/ BridgeDesignPlans_RootDef });
                theDt.Rows.Add(new object[4] { 2, 1700, 17, /*   "1700","17", */  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1710, 17, /*  "1710","17",  */  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1720, 17,  /*  "1720","17",*/  CADDSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1788, 17, /*   "1788","17", */  PlansSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 1799, 17,  /*  "1799","17",*/  FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 40, 0, /* "40","", */ SPI_PH_RootDef });
                theDt.Rows.Add(new object[4] { 2, 4000, 40,  /* "4000",  "40",*/  GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4010, 40,  /* "4010", "40",*/  ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4088, 40,   /* "4088","40", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 4099, 40,  /*  "4099","40",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 50, 0,   /*  "50", "", */ SPI_DMG_RootDef });
                theDt.Rows.Add(new object[4] { 2, 5000, 50,  /*  "5000", "50",*/ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5010, 50,  /*  "5010", "50", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5088, 50, /*   "5088", "50", */ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 5099, 50,   /* "5099", "50",*/ FormsSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 21, 0,   /*  "21", "", */ PhotosFolderDef });
                theDt.Rows.Add(new object[4] { 2, 2188, 21,  /*  "2100", "21",*/ ImagesSubfolderDef });

                theDt.Rows.Add(new object[4] { 1, 31, 0,   /*  "31", "", */ QAQCFolderDef });
                theDt.Rows.Add(new object[4] { 2, 3100, 31,  /*  "3100", "31",*/ GeneralSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3110, 31,  /*  "3110", "31", */ ReportsSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3188, 31,  /*  "3100", "21",*/ ImagesSubfolderDef });
                theDt.Rows.Add(new object[4] { 2, 3199, 31,   /* "3199", "31",*/ FormsSubfolderDef });

                return theDt;
            }
            catch
            {
                throw;
            }

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
        public string GenerateBridgeFolderDefsJson(string jsonRoot, string bridgeIdentifier, string bridge_gd, string district, string county, string bridgegroup, string docRoot)
        {

            string prefix = System.IO.Path.Combine(docRoot, district, county, bridgegroup).Replace("\\", "/");

            DataTable dtFolders = GenerateFolderTable();

            Dictionary<int, DocFolder> dict =
            dtFolders.Rows.Cast<DataRow>()
                     .Select(r => new DocFolder
                     {
                         level = r.Field<int>("LEVEL"),
                         Id = r.Field<int>("ResourceId"),
                         parentId = r.Field<int>("ParentId"),
                         docType = r.Field<int>("ResourceId").ToString(),
                         parentDocType = r.Field<int>("ParentId").ToString(),
                         docFolderName = ((r.Field<int>("LEVEL") == 0) ? $@"/{bridgeIdentifier}" :
                         string.Concat(((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderName.ToString())),
                         docFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderDescription.ToString(),
                         docFolderPath = string.Empty,
                         metaDataGuid = Guid.NewGuid().CleanGuid()
                     })
                    .ToDictionary(m => m.Id);
            //dict.Dump();

            List<DocFolder> docFolders = new List<DocFolder>();

            foreach (var kvp in dict)
            {
                List<DocFolder> folder = docFolders;
                DocFolder item = kvp.Value;
                if (item.parentId >= 0)
                {
                    folder = dict[item.parentId].docSubFolder;
                }

                if (item.level > 1) // a real subfolder e.g. UW or SPI/OS
                {
                    //Console.WriteLine(item.parentId);
                    // Console.WriteLine($@"{dict[0].docFolderName}/{dict[item.parentId].docFolderName}/{item.docFolderName}"  );
                    item.docFolderPath = Regex.Replace(string.Concat(prefix, System.IO.Path.Combine(dict[0].docFolderName, dict[item.parentId].docFolderName, item.docFolderName)), @"(?i)[\\]+", @"/", RegexOptions.IgnoreCase);
                }

                folder.Add(item);

            }

            var jsonString = $"{{ \"{jsonRoot}\": {JsonConvert.SerializeObject(docFolders, Newtonsoft.Json.Formatting.Indented)} {Environment.NewLine} }}";
            return jsonString;
        }

        private IEnumerable<JToken> GetBridgeFoldersByTypeKey(string json, string docTypeKey)
        {
            return GetBridgeFoldersByTypeKey(JObject.Parse(json), docTypeKey);
        }

        private IEnumerable<JToken> GetBridgeFoldersByTypeKey(JObject json, string docTypeKey)
        {

            //JObject hive = JObject.Parse(json);
            IEnumerable<JToken> paths = json.SelectTokens($"$..docSubFolder[?(@.parentDocType == '{docTypeKey}' )].docFolderPath");

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

            JToken path = json.SelectToken($"$..docSubFolder[?(@.docType == '{docSubTypeKey}' )].docFolderPath");

            result = path.Value<string>();
            return result;
        }

        public IEnumerable<JToken> GetAllBridgeDocumentFolders(string json)
        {
            return GetAllBridgeDocumentFolders(JObject.Parse(json));
        }

        public IEnumerable<JToken> GetAllBridgeDocumentFolders(JObject json)
        {
            IEnumerable<JToken> paths = json.SelectTokens($"$..docSubFolder[?(@.parentDocType <> '0' )].docFolderPath");
            return paths;
        }

        // return a JSON string of folder definitions and program bits
        // return a JSON string of folder definitions and program bits
        public string GetFolderSubFolderAppSettings()
        {
            string json = string.Empty;
            string jsonRoot = "folders";
            var dt = GenerateFolderTable();

            DataTable dtFolders = GenerateFolderTable();

            Dictionary<int, DocFolder> dict =
            dtFolders.Rows.Cast<DataRow>()
                     .Select(r => new DocFolder
                     {
                         level = r.Field<int>("LEVEL"),
                         Id = r.Field<int>("ResourceId"),
                         parentId = r.Field<int>("ParentId"),
                         docType = r.Field<int>("ResourceId").ToString(),
                         parentDocType = r.Field<int>("ParentId").ToString(),
                         docFolderName = ((r.Field<int>("LEVEL") == 0) ? string.Empty :
                         string.Concat(((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderName.ToString())),
                         docFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderDescription.ToString(),
                         docFolderPath = string.Empty,
                         metaDataGuid = Guid.NewGuid().CleanGuid(),
                         appSettingsKeyName = ((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderDescription.ToString().Replace(" ", "_").Replace("___", "_").Replace("__", "_").Replace("-", "_"),
                         appSettingsValue = ((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderName.ToString(),
                         appSettingsFolderPath = string.Empty
                     })
                    .ToDictionary(m => m.Id);


            List<DocFolder> docFolders = new List<DocFolder>();

            foreach (var kvp in dict)
            {
                List<DocFolder> folder = docFolders;
                DocFolder item = kvp.Value;
                if (item.parentId >= 0)
                {
                    folder = dict[item.parentId].docSubFolder;
                }
                var serialize = "";

                var regexWinBackSlashPattern = @"[\\]+";
                var replacePattern = @"/";
                var stripCharsPattern = @"(?i)(?<chars>[\W.,;_@_\- -[\\/]])+|(?<slashes>[\\/])+";
                var stripCharsReplace = @"_";

                switch (item.level)
                {
                    case 0:
                        item.docFolderPath = "/";
                        item.appSettingsFolderPath = string.Empty;
                        break;

                    case 1:
                        {
                            item.docFolderPath = Regex.Replace(string.Concat("/", item.docFolderName, "/"), regexWinBackSlashPattern, replacePattern, RegexOptions.IgnoreCase);
                            serialize = Newtonsoft.Json.JsonConvert.SerializeXmlNode(GetAppString(item.appSettingsKeyName, string.Empty, string.Concat(item.docFolderPath)), Newtonsoft.Json.Formatting.None, true);
                            item.appSettingsFolderPath = serialize;
                            break;
                        }
                    case 2:
                        {
                            item.docFolderPath = Regex.Replace(string.Concat("/", dict[item.parentId].docFolderName, "/", item.docFolderName)
                            , regexWinBackSlashPattern
                            , replacePattern, RegexOptions.IgnoreCase);
                            serialize = Newtonsoft.Json.JsonConvert.SerializeXmlNode(
                            GetAppString(string.Concat(
                             Regex.Replace(
                             dict[item.parentId].docFolderDescription.Trim(), stripCharsPattern
                             , stripCharsReplace
                             , RegexOptions.IgnoreCase)
                             .ToLower()
                             .TrimEnd('_')
                             .TrimStart('_'), stripCharsReplace, item.appSettingsKeyName).ToLower(), Regex.Replace(item.docFolderName.ToLower().Trim(), stripCharsPattern, stripCharsReplace).ToLower(), string.Concat(item.docFolderPath, "/")), Newtonsoft.Json.Formatting.None, true);
                            item.appSettingsFolderPath = serialize;
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
            var tokens = (JObject.Parse(jsonSettings)).SelectTokens($"$..docSubFolder[?(@.level >= 1 )].appSettingsFolderPath").ToList();
            List<string> settings = new List<string>();

            foreach (var token in tokens)
            {
                var setting = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(token.ToString(), "add");
                if (setting == null)
                {
                    throw new NullReferenceException("Setting token is unexpectedly nul in GetDocFolderAppSettings");
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
                       ,Br.Bridge_Gd
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

            string bridgeGd = dt.Rows[0].Field<string>("BRIDGE_GD");
            string district = dt.Rows[0].Field<string>("DISTRICT");
            string county = dt.Rows[0].Field<string>("COUNTY");
            string bridgegroup = dt.Rows[0].Field<string>("BRIDGEGROUP");
            string docRoot = System.IO.Path.Combine(DocDrive, DocRootFolder);
            string jsonRootId = @"folders";

            return GenerateBridgeFolderDefsJson(jsonRootId, bridgeIdentifier, bridgeGd, district, county, bridgegroup, docRoot);

        }

        //    public string GetBridgeFolders(string bridgeIdentifier, string bridge_gd, string district, string county, string bridgegroup, string docRoot)
        //    {
        //        return GenerateBridgeFolderDefsJson(bridgeIdentifier, bridge_gd, district, county, bridgegroup, docRoot);
        //    }

        //    public string GetBridgeFolderByKey( string bridgeIdentifier, string typeKey, string subTypeKey)
        //    {
        //        string result = string.Empty;
        //        string fldrs = GetBridgeFolders(bridgeIdentifier);

        //        Newtonsoft.Json.Linq.JObject hive = Newtonsoft.Json.Linq.JObject.Parse(fldrs);

        //        /*
        //         *
        //         *           level = r.Field<int>("LEVEL"),
        //                     Id = r.Field<int>("ResourceId"),
        //                     parentId = r.Field<int>("ParentId"),
        //                     docType = r.Field<int>("ResourceId").ToString(),
        //                     parentDocType = r.Field<int>("ParentId").ToString(),
        //                     docFolderName = ((r.Field<int>("LEVEL") == 0) ? $@"/{bridgeIdentifier}" :
        //                     string.Concat(((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderName.ToString())),
        //                     docFolderDescription = ((dynamic)(JsonConvert.DeserializeObject(r.Field<string>("FOLDERDEFINITION")))).folderDescription.ToString(),
        //                     docFolderPath = string.Empty,
        //                     metaDataGuid = System.Guid.NewGuid().CleanGuid()
        //         */



        //        return result;
        //    }

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
