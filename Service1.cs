using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using YLWService;

namespace YLWEDITransGCL
{
    public partial class Service1 : ServiceBase
    {
        internal class cLog
        {
            public string Msg { get; set; }
            public string File { get; set; }
        }

        private bool m_IsStop; // 중지시키기 위한 플래그

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            YLWService.LogWriter.WriteLog("Service Start!");

            Thread t1 = new Thread(new ThreadStart(CallMainService));
            t1.Start();
        }

        protected override void OnStop()
        {
            YLWService.LogWriter.WriteLog("Service Stop!");
            m_IsStop = true;
        }

        // --------------------------------------------------------------------------------------------------------------

        private void CallMainService()
        {
            YLWService.LogWriter.WriteLog("CallMainService Start!");

            m_IsStop = false;
            while (true)
            {
                if (m_IsStop) break;

                try
                {
                    //in folder 읽기 
                    ReadFiles();

                    //out folder 쓰기
                    int rtn = 1;
                    while (rtn > 0)
                    {
                        rtn = WriteFiles();
                    }

                    Thread.Sleep(1 * 60 * 1000); // 1분 대기
                }
                catch
                {
                    Thread.Sleep(1 * 60 * 1000); // 1분 대기
                }
            }

            YLWService.LogWriter.WriteLog("CallMainService Stop!");
        }


        public void ReadFiles()
        {
            string inpath = YLWServiceModule.GetInPath();
            YlwSecurityJson security = YLWService.MTRServiceModule.SecurityJson.Clone();  //깊은복사
            security.serviceId = "Metro.Package.AdjSL.BisAdjSLEDITransGCL";
            security.methodId = "in";

            DataSet ds = new DataSet("ROOT");
            DataTable dt = ds.Tables.Add("DataBlock1");

            dt.Columns.Add("WorkingTag");
            dt.Columns.Add("IDX_NO");
            dt.Columns.Add("DataSeq");
            dt.Columns.Add("Status");
            dt.Columns.Add("Selected");
            dt.Columns.Add("TABLE_NAME");

            dt.Columns.Add("companyseq");
            dt.Columns.Add("send_type");
            dt.Columns.Add("success_fg");
            dt.Columns.Add("cust_code");
            dt.Columns.Add("trans_dtm");
            dt.Columns.Add("file_name");
            dt.Columns.Add("edi_text");
            dt.Columns.Add("remain");

            try
            {
                if (!Directory.Exists(inpath)) Directory.CreateDirectory(inpath);
                var files = Directory.GetFiles(inpath, "*.inf");
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    string editext = "";
                    using (StreamReader str = new StreamReader(file, Encoding.Default))
                    {
                        editext = str.ReadToEnd();
                    }

                    dt.Clear();
                    DataRow dr = dt.Rows.Add();

                    dr["WorkingTag"] = "A";
                    dr["IDX_NO"] = 1;
                    dr["DataSeq"] = 1;
                    dr["Status"] = 0;
                    dr["Selected"] = 0;
                    dr["TABLE_NAME"] = dt.TableName;

                    dr["companyseq"] = security.companySeq;
                    dr["send_type"] = 0;
                    dr["success_fg"] = 0;
                    dr["cust_code"] = "GCL";
                    dr["file_name"] = filename;
                    dr["edi_text"] = editext;

                    DataSet yds = MTRServiceModule.CallMTRServiceCallPost(security, ds);
                    if (yds != null)
                    {
                        DataTable dataBlock1 = yds.Tables["DataBlock1"];
                        if (dataBlock1 != null && dataBlock1.Rows.Count > 0)
                        {
                            if (dataBlock1.Rows[0]["success_fg"] + "" == "1")
                            {
                                File.Delete(file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                YLWService.LogWriter.WriteLog(ex.Message);
            }
        }

        public int WriteFiles()
        {
            string outpath = YLWServiceModule.GetOutPath();
            YlwSecurityJson security = YLWService.MTRServiceModule.SecurityJson.Clone();  //깊은복사
            security.serviceId = "Metro.Package.AdjSL.BisAdjSLEDITransGCL";
            security.methodId = "out";

            DataSet ds = new DataSet("ROOT");
            DataTable dt = ds.Tables.Add("DataBlock1");

            dt.Columns.Add("companyseq");
            dt.Columns.Add("send_type");
            dt.Columns.Add("success_fg");
            dt.Columns.Add("cust_code");
            dt.Columns.Add("file_name");
            dt.Columns.Add("edi_id");

            dt.Clear();
            DataRow dr = dt.Rows.Add();

            dr["companyseq"] = security.companySeq;
            dr["send_type"] = 1;
            dr["success_fg"] = 0;
            dr["cust_code"] = "GCL";

            DataSet yds = MTRServiceModule.CallMTRServiceCall(security, ds);
            if (yds != null && yds.Tables.Count > 0)
            {
                DataTable dataBlock1 = yds.Tables["DataBlock1"];
                if (dataBlock1 != null && dataBlock1.Rows.Count > 0)
                {
                    try
                    {
                        if (!Directory.Exists(outpath)) Directory.CreateDirectory(outpath);
                        string fileName = dataBlock1.Rows[0]["file_name"] + "";
                        string file = outpath + "/" + fileName;
                        if (File.Exists(file)) File.Delete(file);
                        FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write);
                        using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("euc-kr")))
                        {
                            sw.Write(dataBlock1.Rows[0]["edi_text"]);
                            sw.Close();
                        }
                        fs.Close();
                    }
                    catch (Exception ex)
                    {
                        YLWService.LogWriter.WriteLog(ex.Message);
                        return 0;
                    }
                    dr["edi_id"] = dataBlock1.Rows[0]["edi_id"];

                    security.methodId = "commit";
                    yds = MTRServiceModule.CallMTRServiceCall(security, ds);
                    if (yds != null && yds.Tables.Count > 0)
                    {
                        DataTable inBlock1 = yds.Tables["DataBlock1"];
                        if (inBlock1 != null) return Convert.ToInt32(inBlock1.Rows[0]["remain"]);
                        return 0;
                    }
                }
            }
            return 0;
        }
    }
}
