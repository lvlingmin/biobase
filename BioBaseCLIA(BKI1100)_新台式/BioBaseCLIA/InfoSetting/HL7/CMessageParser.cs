using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using System.Data.OleDb;
using BioBaseCLIA.Model;
using BioBaseCLIA.Run;
using Maticsoft.DBUtility;
using System.Data;
namespace BioBaseCLIA.InfoSetting
{
   
    public class CMessageParser:HLObject 
    {

        public static  Patient p { get; set; }
        public static  ServeyReport rp { get; set; }
        int _dalaytime=6000;
        /// <summary>
        /// 发送延迟
        /// </summary>
        public int Dalaytime
        {
            get { return _dalaytime; }
            set { _dalaytime = value; }
        }

        //public static  LabResult rs { get; set; }
        //LisCommunication liscon = new LisCommunication();
        /// <summary>
        /// 发送ORU^01信息，用来传输实验结果
        /// </summary>
        public void SendORU(List<TestResult> list)
        {
            //TestResult Result,
            if (list.Count == 0) return;
            p = new Patient();
            rp = new ServeyReport();
            Mtype = "ORU^R01";
            
            string Message = "";
            string _samplet = list[0].SampleType.Trim();
            if (list[0].SampleType.Contains("质控品"))
            {
                ConstrolID = ConstrolID + 1;
                DbHelperOleDb db = new DbHelperOleDb(3);
                DataTable dtQCInfo = DbHelperOleDb.Query(3, @"select QCID,Batch,SD,QCLevel from tbQC where status = '1' and ProjectName = '"
                                                            + list[0].ItemName + "' and Status = '1'").Tables[0];
                Sendtype = 2;
                obrID = ConstrolID;
                rp.PorderNum = list[0].SampleID.ToString();
                rp.ForderNum = list[0].SampleNo;
                rp.ODate = DateTime.Now.ToShortDateString();
                rp.OEDate = DateTime.Now.ToString();
                rp.CollectIdent = "^" + list[0].SamplePos;
                rp.SpcCode = "1";
                rp.RelevantInfo = list[0].ItemName;
                rp.ReseltDate = DateTime.Now.ToShortDateString();
                rp.Source = "0";
                rp.OProvider = dtQCInfo.Rows[0][1].ToString();
                rp.OCallbackNum = list[0].PMT.ToString();
                rp.SimpleState = dtQCInfo.Rows[0][2].ToString();
                rp.XdCode = list[0].concentration;
                rp.FilletF1 = "";
                //rp.FilletF2=;
                rp.ResuleState = list[0].Result;
                Message = startMessage + MSH() + OBR(rp) + endMessage;
            }
            else
            {
                ConstrolID = ConstrolID + 1;
                Sendtype = 0;
                obrID = ConstrolID;
                rp.PorderNum = list[0].SampleNo;
                rp.ForderNum = list[0].SampleNo;
                DbHelperOleDb db = new DbHelperOleDb(1);
                OleDbDataReader read = DbHelperOleDb.ExecuteReader(1, @"SELECT Emergency,SendDateTime,SendDoctor,Department,InspectDoctor,Diagnosis,Source,PatientName,Sex,Age,
                        ClinicNo,InpatientArea,Ward,BedNo,MedicaRecordNo  FROM tbSampleInfo WHERE SampleID=" + list[0].SampleID + " AND SampleNo='" + list[0].SampleNo + "'");

                if (read.Read())
                {
                    if (read.GetInt32(0) == 3)
                        rp.Priority = true;
                    else
                        rp.Priority = false;
                    rp.ReceiveTime = read.GetDateTime(1).ToString();
                    //rp.SpcCode;
                    rp.Source = read.GetString(6);
                    try
                    {
                        rp.OProvider = read.GetString(2);
                    }
                    catch
                    {
                        rp.OProvider = "";
                    }
                    try
                    {
                        rp.OCallbackNum = read.GetString(3);
                    }
                    catch
                    {
                        rp.OCallbackNum = "";
                    }
                    //rp.SimpleState;
                    //rp.XdCode;
                    try
                    {
                        rp.FilletF1 = read.GetString(4);
                    }
                    catch
                    {
                        rp.FilletF1 = "";
                    }
                    //rp.FilletF2;
                    try
                    {
                        rp.RelevantInfo = read.GetString(5);
                    }
                    catch
                    {
                        rp.RelevantInfo = "";
                    }
                    try
                    {
                        p.Pname = read.GetString(7);
                    }
                    catch
                    {
                        p.Pname = "";
                    }
                    try
                    {
                        if (read.GetString(8) == "")
                            p.Sex = 'M';
                        else if (read.GetString(8) == "女")
                            p.Sex = 'F';
                        else
                            p.Sex = 'O';
                    }
                    catch
                    {
                        p.Sex = 'O';
                    }
                    if (read.GetDouble(9).ToString() != null)
                    {
                        p.Age = read.GetDouble(9).ToString();
                    }
                    //double  d = read.GetDouble(9);
                    try
                    {
                        p.Patientid = read.GetString(10);
                    }
                    catch
                    {
                        p.Patientid = "";
                    }
                    try
                    {
                        p.SickArea = read.GetString(11);
                    }
                    catch
                    {
                        p.SickArea = "";
                    }
                    try
                    {
                        p.SickRoom = read.GetString(12);
                    }
                    catch
                    {
                        p.SickRoom = "";
                    }
                    try
                    {
                        p.Bedid = Convert.ToInt32(read.GetString(13));
                    }
                    catch
                    {
                        p.Bedid = 0;
                    }
                    try
                    {
                        p.PIdent = read.GetString(14);
                    }
                    catch
                    {
                        p.PIdent = "";
                    }
                }
                read = null;

                rp.ODate = DateTime.Now.ToShortDateString();
                rp.OEDate = DateTime.Now.ToString();
                rp.CollectV = "1";
                rp.CollectIdent = "^" + list[0].SamplePos;
                rp.ReseltDate = DateTime.Now.ToShortDateString();
                rp.ResuleState = list[0].Result;
                Message = startMessage + MSH() + PID(p) + OBR(rp);
                SetID = 1;
                foreach (TestResult li in list)
                {
                    LabResult rs = new LabResult();
                    db = new DbHelperOleDb(0);
                    read = DbHelperOleDb.ExecuteReader(0, @"SELECT ProjectNumber, FullName, DiluteCount,ProjectType FROM tbProject WHERE FullName='" + li.ItemName + "'");
                    if (read.Read())
                    {
                        if (read.GetInt32(2) > 1)
                            rp.Bdilute = true;
                        else
                            rp.Bdilute = false;
                        rs.ProjectId = read.GetString(0);
                        if (read.GetString(3) == "0")
                            rs.Obxtype = "ST";
                        else
                            rs.Obxtype = "NM";
                    }
                    rs.ProjectName = li.ItemName;
                    rs.Resulttype = li.concentration;
                    //rs.Unit=Result.
                    rs.Rrs = li.Range1;
                    rs.Abnormalflag = li.Result;
                    rs.ResultFlag = "F";
                    rs.Resulttype = li.concentration;
                    rs.Observetime = DateTime.Now;
                    rs.Method = "";
                    rs.Doctor = rp.FilletF1;
                    Message = Message + OBX(rs);
                    SetID = SetID + 1;

                }
                Message = Message + endMessage;
                read = null;
            }
            if (Message!="")
                LisCommunication.Instance.write(Message);
        }
        /// <summary>
        /// 根据选择发送样本结果
        /// </summary>
        /// <param name="Result">实验结果</param>
        /// <param name="tranInfo">选择结果发送请求</param>
        public void SendSample(TestResult Result, string tranInfo)
        {
            LabResult rs = new LabResult();
              Mtype = "ORU^R01";
              ConstrolID++;
              DbHelperOleDb db = new DbHelperOleDb(3);
              OleDbDataReader read = DbHelperOleDb.ExecuteReader(3,@"SELECT Emergency,SendDateTime,SendDoctor,Department,InspectDoctor,Diagnosis,Source,PatientName,Sex,Age,
                        ClinicNo,InpatientArea,Ward,BedNo,MedicaRecordNo  FROM tbSampleInfo WHERE SampleID=" + Result.SampleID + " AND SampleNo=" + Result.SampleNo + "");
            if(read.Read())
            {
                string[] spstr=tranInfo.Split(',');
                for (int i = 0; i < spstr.Length; i++)
                {
                    switch (spstr[0])
                    {
                        case "01":
                            rp.ForderNum = Result.SampleNo;
                            break ;
                        case "02":
                            p.Pname = read.GetString(7) ;
                            break;
                        case"03":
                            if (read.GetString(8) == "男")
                                p.Sex = 'M';
                            else if (read.GetString(8) == "女")
                                p.Sex = 'F';
                            else
                                p.Sex = 'O';
                            break;
                        case "04":
                            if (read.GetDouble(9).ToString() != null)
                            {
                                p.Age = read.GetDouble(9).ToString();
                            }
                            break;
                        case "05":
                            rp.OProvider = read.GetString(2);
                            break;
                        case "07":
                            p.Bedid = Convert.ToInt32(read.GetString(13));
                            break;
                        case "08":
                            rp.OCallbackNum = read.GetString(3);
                            break;
                        case"09":
                            rp.RelevantInfo = read.GetString(5);
                            break;
                        case"11":
                            rs.ProjectName = Result.ItemName;
                            break ;
                        case "13":
                            rs.Resulttype = Result.concentration;
                            break;
                        case "14":
                            rs.Rrs = Result.Range1;
                            break;
                        case "15":
                            rp.ReceiveTime = read.GetString(1);
                            break;
                        case "16":
                            rp.FilletF1 = read.GetString(4);
                            break;

                    }
                }
            }
        }
        /// <summary>
        /// 发送查询消息
        /// </summary>
        /// <returns></returns>
        public void SendQry()
        {
            string Message = startMessage + MSH() + QRD() + QRF() + endMessage;
            LisCommunication.Instance.write(Message);
            bool delay = LisCommunication.Instance.comWait.WaitOne(Dalaytime);
            if (!delay)
            {
                LisCommunication.Instance.comWait.Set();
            }
        }
        /// <summary>
        /// 对DSR消息进行响应
        /// </summary>
        /// <returns></returns>
        public string SendACK()
        {
            return startMessage + MSH() + MSA()+ endMessage;
        }
        private static tbSampleInfo _sampleInfo=new tbSampleInfo();
        public tbSampleInfo GetSampleInfo(tbSampleInfo sampleInfo)
        {
           if (p != null)
                {
                    _sampleInfo = sampleInfo;
                    try
                    {
                        _sampleInfo.Age = Convert.ToDouble(p.Age);
                    }
                    catch
                    {
                        _sampleInfo.Age = 0;
                    }
                    _sampleInfo.BedNo = p.Bedid.ToString();
                    _sampleInfo.ClinicNo = p.Patientid;
                    _sampleInfo.InpatientArea = p.SickArea;
                    _sampleInfo.MedicaRecordNo = p.PIdent;
                    _sampleInfo.PatientName = p.Pname;
                    if (p.Sex == 'M')
                        _sampleInfo.Sex = "男";
                    else if (p.Sex == 'F')
                        _sampleInfo.Sex = "女";
                    else
                        _sampleInfo.Sex = "其他";
                    _sampleInfo.Ward = p.SickRoom;
                }
                if (rp != null)
                {
                    _sampleInfo.InspectDoctor = rp.FilletF1;
                    _sampleInfo.SendDoctor = rp.OProvider;
                    _sampleInfo.Department = rp.OCallbackNum;
                    /*暂时屏蔽更新项目信息
                    if (rp.ProjectInfo!=null&& rp.ProjectInfo.Count > 0)
                    {
                        for (int i = 0; i < rp.ProjectInfo.Count; i++)
                        {
                           string []info= rp.ProjectInfo[i].Split('^');
                           _sampleInfo.ProjectName = info[1] + " ";
                        }
                    }
                    */
                }
                
                return _sampleInfo; 
        }
        public tbSampleInfo SampleInfo
        {
            set
            {
                _sampleInfo = value;
            }
            get
            {
                return _sampleInfo;
            }
        }
        /// <summary>
        /// 根据样品编号查询病人信息
        /// </summary>
        /// <param name="SampleNo"></param>
        public void SelectBySampleNo(string sampleNo)
        {
            SendApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
            SendFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
            Mtype = "QRY^Q02";
            Sendtype = 0;
            SampleNo = sampleNo;
            SendQry();
        }
        /// <summary>
        /// 处理从Lis接收的信息
        /// </summary>
        /// <param name="splidate"></param>
        public void ReciveInfo(string[] splidate)
        {
            p = new Patient();
            rp = new ServeyReport();
            rp.ProjectInfo = new List<string>();
            for (int i = 0; i < splidate.Length; i++)
            {
                string[] message = splidate[i].Split('|');
                if (message[0] == "MSH" && message[16] != "")
                {
                    rp.TestResult = Convert.ToInt32(message[16]);
                }
                if (message[0] == "DSP")
                {
                    switch (Convert.ToInt32(message[1]))
                    {
                        case 1:
                            p.Patientid = message[3];
                            break;
                        case 2:
                            p.Bedid = Convert.ToInt32(message[3]);
                            break;
                        case 3:
                            p.Pname = message[3];
                            break;
                        case 4:
                            p.Birth = message[3];
                            break;
                        case 5:
                            if (message[3] == "M")
                                p.Sex = 'M';
                            else if (message[3] == "F")
                                p.Sex = 'F';
                            else
                                p.Sex = 'O';
                            break;
                        case 6:
                            p.Blood = message[3];
                            break;
                        case 7:
                            p.Race = message[3];
                            break;
                        case 8:
                            p.Address = message[3];
                            break;
                        case 9:
                            p.Post = message[3];
                            break;
                        case 10:
                            p.PhoneNum = message[3];
                            break;
                        case 11:
                            p.Workphone = message[3];
                            break;
                        case 13:
                            p.Marriage = message[3];
                            break;
                        case 14:
                            p.Region = message[3];
                            break;
                        case 15:
                            p.PatientType = message[3];
                            break;
                        case 16:
                            p.Ybnum = message[3];
                            break;
                        case 17:
                            p.FeeType = message[3];
                            break;
                        case 18:
                            p.National = message[3];
                            break;
                        case 19:
                            p.Origo = message[3];
                            break;
                        case 20:
                            Country = message[3];
                            break;
                        case 21:
                            rp.PorderNum = message[3];
                            break;
                        case 22:
                            rp.ForderNum = message[3];
                            break;
                        case 23:
                            rp.ReceiveTime = message[3];
                            break;
                        case 24:
                            rp.Priority  =Convert.ToBoolean(message[3]);
                            break;
                        case 25:
                            //样本采集量
                            rp.CollectV = message[3];
                            break;
                        case 26:
                           //样本类型
                            rp.Source = message[3];
                            break;
                        case 27:
                            rp.OProvider = message[3];
                            break;
                        case 28:
                            rp.OCallbackNum = message[3];
                            break;
                        case 29:
                            rp.ProjectInfo.Add(message[3]);
                            break;
                        default :
                            break;
                    }

                }
            }
            //GetSampleInfo();
            LisCommunication.Instance.comWait.Set();
        }

       
    }
}
