using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using DocumentManagerUtil;
using Oracle.ManagedDataAccess.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Dynamic.Core;


namespace DocMgrUtilTestProject1
{


    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        private void TestMethod1()
        {
        }

        [TestMethod]
        public void TestGetBridgeFoldersJson()
        {
             
            var utils =DocMgrUtils.Instance;
            var brkey = @"413700012101002";
            var json = utils.GetAllBridgeFoldersJson(brkey);

            Console.WriteLine(json);
            
        }

        [TestMethod]
        public void TestIsAllowedTypeSubTypePairES()
        {

            var utils = DocMgrUtils.Instance;
            var key = "5010";
            var value = "50";
            Assert.IsTrue(utils.IsAllowedTypeSubTypePair(key, value), $"Combination of type {key} and subtype {value} not allowed");

            Console.WriteLine($"Allowed combination type { key} / subtype { value} - expected");


        }

        [TestMethod]
        public void TestIsAllowedTypeSubTypePair2EF()
        {

            var utils = DocMgrUtils.Instance;
            var key = "4010";
            var value = "50";
            Assert.IsFalse(utils.IsAllowedTypeSubTypePair(key, value), $"Invalid combination of type {key} and subtype {value} found.");

            Console.WriteLine($"Type {key} / subtype {value} not found - expected");


        }


        [TestMethod]
        public void TestIsAllowedTypeKeyES()
        {

            var utils = DocMgrUtils.Instance;
            var key = "50";
           // var value = "5010";
            Assert.IsTrue(utils.IsAllowedType(key ), $"Type {key}   not allowed");

            Console.WriteLine($"Allowed type {key}- expected");


        }

        [TestMethod]
        public void TestIsAllowedTypeKey2EF()
        {

            var utils = DocMgrUtils.Instance;
            var key = "NB";
            // var value = "5010";
            Assert.IsFalse(utils.IsAllowedType(key), $"Invalid type {key}  found.");

            Console.WriteLine($"Invalid type {key}- expected");


        }


        [TestMethod]
        public void TestIsAllowedSubTypeKeyES()
        {

            var utils = DocMgrUtils.Instance;
            var key = "5010";
           
            Assert.IsTrue(utils.IsAllowedSubType(key), $"Type {key}   not allowed");

            Console.WriteLine($"Allowed subtype {key}- expected");


        }

        [TestMethod]
        public void TestIsAllowedSubTypeKey2EF()
        {

            var utils = DocMgrUtils.Instance;
            
            var key = "1910";
            Assert.IsFalse(utils.IsAllowedSubType(key), $"Invalid type {key}  found.");

            Console.WriteLine($"Invalid subtype {key} - expected");


        }


        [TestMethod]
        public void TestGetDocFolder()
        {
            //  public string GetBridgeFolders(string bridgeIdentifier, OracleConnection conn)
            var utils = DocMgrUtils.Instance;
            var brkey = @"413700012101002"; //413700012101002
            var folderName = utils.GetDocFolder(brkey, "1100");

            Console.WriteLine(folderName);


        }

        [TestMethod]
        public void TestGetDocFoldersMany()
        {
            //  public string GetBridgeFolders(string bridgeIdentifier, OracleConnection conn)
            var utils = DocMgrUtils.Instance;

            var cmd = new OracleCommand()
            {
                CommandText = @"SELECT DISTINCT Br.Brkey
				               /*,Br.Bridge_Gd
				               ,P2.Shortdesc AS District
				               ,P1.Shortdesc AS County
				               ,Nvl(Br.Bridgegroup, 'UNASSIGNED') AS Bridgegroup
				               ,P1.Parmvalue AS Kdot_County
				               ,Br.County AS Nbi_County */
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
				 WHERE Kd.Doc_Status = '1' AND ROWNUM <=300
				 GROUP BY Br.Brkey
				         ,Br.Bridge_Gd
				         ,P2.Shortdesc
				         ,P1.Shortdesc
				         ,Nvl(Br.Bridgegroup, 'UNASSIGNED')
				         ,P1.Parmvalue
				         ,Br.County
				",
                Connection = new OracleConnection()
                {
                    ConnectionString = "Data Source=10.181.74.44:1521/ESOADEV.WORLD; User ID=KDOT_BLP; Password=eis3nh0wer;"
                },
                BindByName=true
            };


            cmd.Connection.Open();

            var a = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            var rows = a.Fill(dt);
            cmd.Connection.Close();
            cmd.Dispose();

            foreach (DataRow r in dt.Rows)
            {
                var bridgeIdentifier = r["BRKEY"].ToString();
                    
                var folderName = utils.GetDocFolder(bridgeIdentifier, "1100");
                Console.WriteLine(folderName);
            }

        }

        [TestMethod]
        public void TestGetDocFoldersJSONForBridge()
        {
            var utils = DocMgrUtils.Instance;

            var brkey = @"413700012101002";

            var conn = new OracleConnection()
            {
                ConnectionString = "Data Source=10.181.74.44:1521/ESOADEV.WORLD; User ID=KDOT_BLP; Password=eis3nh0wer;"
            };

            var cmd = new OracleCommand()
            {
                CommandText = @"SELECT DISTINCT Br.Brkey
                       --,Br.Bridge_Gd
                       ,P2.Shortdesc AS District
                       ,P1.Shortdesc AS County
                       ,Nvl(Br.Bridgegroup, 'UNASSIGNED') AS Bridgegroup
                       ,P1.Parmvalue AS Kdot_County
                       ,Br.County AS Nbi_County
         FROM Bridge Br        
         INNER JOIN Paramtrs P1
            ON P1.Longdesc = Br.County
           AND P1.Table_Name = 'bridge'
           AND P1.Field_Name = 'county'
         INNER JOIN Paramtrs P2
            ON P2.Parmvalue = Br.District
           AND P2.Table_Name = 'bridge'
           AND P2.Field_Name = 'district'
         WHERE br.BRKEY =:the_brkey  
          
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
                Value = brkey
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

            const string DocDrive = "X:\\";
            const string DocRootFolder = "Docs";
            const string FormsRootFolder = @"Forms";

            var row = dt.Rows[0];

           // var bridgeGd =row["BRIDGE_GD"].ToString();
            var district = row["DISTRICT"].ToString();
            var county =   row["COUNTY"].ToString();
            var bridgeGroup =  row["BRIDGEGROUP"].ToString();
            var docRoot = System.IO.Path.Combine(DocDrive, DocRootFolder);
            var formsRoot  = System.IO.Path.Combine(DocDrive, "Docs", FormsRootFolder);
            var jsonRootId = @"folders";

            var jSon = utils.GenerateBridgeFolderDefsJson(jsonRootId, brkey, district, county,
                bridgeGroup, docRoot);

            for (var i = 0; i < 200; i++)
            {
                var path = utils.GetBridgeFolderBySubTypeKey(jSon, "1410");
                Console.WriteLine(path);
            }
        }


        [TestMethod]
        public void TestGetAllBridgeFoldersFromJSON()
        {
            var utils = DocMgrUtils.Instance;

          

            var brkey = @"413700012101002";

            var conn = new OracleConnection()
            {
                ConnectionString = "Data Source=10.181.74.44:1521/ESOADEV.WORLD; User ID=KDOT_BLP; Password=eis3nh0wer;"
            };

            var cmd = new OracleCommand()
            {
                CommandText = @"SELECT DISTINCT Br.Brkey
                       --,Br.Bridge_Gd
                       ,P2.Shortdesc AS District
                       ,P1.Shortdesc AS County
                       ,Nvl(Br.Bridgegroup, 'UNASSIGNED') AS Bridgegroup
                       ,P1.Parmvalue AS Kdot_County
                       ,Br.County AS Nbi_County
         FROM Bridge Br        
         INNER JOIN Paramtrs P1
            ON P1.Longdesc = Br.County
           AND P1.Table_Name = 'bridge'
           AND P1.Field_Name = 'county'
         INNER JOIN Paramtrs P2
            ON P2.Parmvalue = Br.District
           AND P2.Table_Name = 'bridge'
           AND P2.Field_Name = 'district'
         WHERE br.BRKEY =:the_brkey  
          
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
                Value = brkey
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

            const string DocDrive = "X:\\";
            const string DocRootFolder = "Docs";
            const string FormsRootFolder = @"Forms";

            var row = dt.Rows[0];

            // var bridgeGd =row["BRIDGE_GD"].ToString();
            var district = row["DISTRICT"].ToString();
            var county = row["COUNTY"].ToString();
            var bridgeGroup = row["BRIDGEGROUP"].ToString();
            var docRoot = System.IO.Path.Combine(DocDrive, DocRootFolder);
            var formsRoot = System.IO.Path.Combine(DocDrive, "Docs", FormsRootFolder);
            var jsonRootId = @"folders";

            string jSon = utils.GenerateBridgeFolderDefsJson(jsonRootId, brkey, district, county, bridgeGroup, docRoot);
            var paths = utils.GetBridgeFoldersByTypeKey(jSon, "10");
            foreach (JToken token in paths)
            {
                Console.WriteLine(token.Value<string>());
            }

            var allPaths = utils.GetAllDocumentFolders(jSon);//utils.GetBridgeFolderBySubTypeKey(jSon, "1400");
            foreach (JToken token in allPaths)
            {
                Console.WriteLine(token.Value<string>());
            }
            
        }

        [TestMethod]
        public void TestGetFormFoldersFromJSON()
        {
            var utils = DocMgrUtils.Instance;
            string formsRootFolder = string.Concat("X:/", "Docs","/", "Forms");
            var jsonRootId = @"folders";
            string formsJson = utils.GenerateFormFolderDefs(jsonRootId, formsRootFolder);
            var formPaths = utils.GetAllDocumentFolders(formsJson);
            foreach (var frmPath in formPaths)
            {
                Console.WriteLine(frmPath.Value<string>());
            }
        }



        [TestMethod]
        public void TestGetDocFolderAppSettingsFromJSON()
        {
            var utils = DocMgrUtils.Instance;

            //var bridgeIdentifier = @"000301061704820";

            //var jSon = utils.GetAllBridgeFoldersJson(bridgeIdentifier);

            var settings = utils.GetDocFolderAppSettings().ToArray();

            foreach (var setting in settings)
            {
                Console.WriteLine(setting);
            }

        }


    }
}
