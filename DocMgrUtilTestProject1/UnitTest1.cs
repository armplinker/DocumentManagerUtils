using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using DocumentManagerUtil;
using Oracle.ManagedDataAccess.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;



namespace DocMgrUtilTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void TestGetBridgeFoldersJson()
        {
             
            var utils =new DocMgrUtils();
            var brkey = @"413700012101002";
            var json = utils.GetAllBridgeFoldersJson(brkey);

            Console.WriteLine(json);
            
        }

        [TestMethod]
        public void TestGetDocFolder()
        {
            //  public string GetBridgeFolders(string bridgeIdentifier, OracleConnection conn)
            var utils = new DocMgrUtils();
            var brkey = @"413700012101002";
            var folderName = utils.GetDocFolder(brkey, "1100");

            Console.WriteLine(folderName);


        }

        [TestMethod]
        public void TestGetDocFoldersMany()
        {
            //  public string GetBridgeFolders(string bridgeIdentifier, OracleConnection conn)
            var utils = new DocMgrUtils();

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
                var brkey = r["BRKEY"].ToString();
                    
                var folderName = utils.GetDocFolder( brkey, "1100");
                Console.WriteLine(folderName);
            }

        }

        [TestMethod]
        public void TestGetDocFoldersJSONForBridge()
        {
            var utils = new DocMgrUtils();

            var brkey = @"413700012101002";

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
            var row = dt.Rows[0];

            string bridgeGd =row["BRIDGE_GD"].ToString();
            string district = row["DISTRICT"].ToString();
            string county =   row["COUNTY"].ToString();
            string bridgeGroup =  row["BRIDGEGROUP"].ToString();
            string docRoot = System.IO.Path.Combine(DocDrive, DocRootFolder);
            string jsonRootId = @"folders";

            var jSon = utils.GenerateBridgeFolderDefsJson(jsonRootId, brkey, bridgeGd, district, county,
                bridgeGroup, docRoot);

            for (var i = 0; i < 200; i++)
            {
                var path = utils.GetBridgeFolderBySubTypeKey(jSon, "1400");
                Console.WriteLine(path);
            }
        }


        [TestMethod]
        private void GetBridgeFolderFromJSON()
        {
            var utils = new DocMgrUtils();

            var bridgeIdentifier = @"000301061704820";

            var jSon = utils.GetAllBridgeFoldersJson(bridgeIdentifier);

            var path = utils.GetBridgeFolderBySubTypeKey(jSon, "1400");

        }

    }
}
