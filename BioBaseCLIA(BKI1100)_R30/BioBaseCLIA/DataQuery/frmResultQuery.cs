using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Maticsoft.DBUtility;
using FastReport;
using FastReport.Utils;
using Common;
using BioBaseCLIA.InfoSetting;
using BioBaseCLIA.Run;
using System.IO;
using System.Data.OleDb;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using System.Resources;
using System.Text.RegularExpressions;
using BioBaseCLIA.CalculateCurve;

namespace BioBaseCLIA.DataQuery
{
    public partial class frmResultQuery : frmParent
    {
        /// <summary>
        /// 功能简介：全自动化学发光结果查询界面，设置查询病人信息和样本结果。
        /// 完成日期：20170726
        /// 编写人：刘亚男
        /// 版本：1.0
        /// </summary>
        /// 
        frmMessageShow frmMsgShow = new frmMessageShow();
        CalculateCurve.Calculater er = null;
        public frmMessageShow frmMsg = new frmMessageShow();
        BLL.tbAssayResult bllAssayResult = new BLL.tbAssayResult();//2018-12-10 zlx add
        /// <summary>
        /// 样本信息表表的bll实例 2019-01-10 zlx add
        /// </summary>
        BLL.tbSampleInfo bllsp = new BLL.tbSampleInfo();
        List<string> DataAnalysisProList = new List<string>();//释放图项目
        public frmResultQuery()
        {
            InitializeComponent();

        }
        private void frmResultQuery_Load(object sender, EventArgs e)
        {
            bool IsLisConnect = bool.Parse(OperateIniFile.ReadInIPara("LisSet", "IsLisConnect"));
            if (!IsLisConnect)
            {
                tbnSendResult.Enabled = false;
            }
            FileStream fs = new FileStream(Environment.CurrentDirectory + "\\DataAnalysis.txt", FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string[] tempName = sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None);
            foreach (string temp in tempName)
            {
                DataAnalysisProList.Add(temp);
            }
            sr.Close();
            fs.Close();
            cmbSelect.SelectedIndex = 0;//2018-11-20 zlx add
            fbtnAddSampleResult.Visible = false;
            fbtnQuery_Click(sender, e);
           
        }
        private void frmResultQuery_SizeChanged(object sender, EventArgs e)
        {
            formSizeChange(this);
        }

        #region 右侧功能按钮

        private void fbtnReturn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region 界面功能按钮
        private void fbtnQuery_Click(object sender, EventArgs e)
        {
            //2018-12-15 zlx mod
            //dgvSampleData.DataSource = null;
            DbHelperOleDb DB = new DbHelperOleDb(1);
            string str = "select distinct SampleNo,format(SendDateTime,'yyyy-mm-dd')AS SendDateTimeS,tbSampleInfo.SampleID,PatientName,Sex,Age,ClinicNo,InpatientArea," +
                  "Ward,BedNo,MedicaRecordNo,Diagnosis,InspectionItems,AcquisitionTime from tbSampleInfo INNER JOIN tbAssayResult on tbAssayResult.SampleID = tbSampleInfo.SampleID where " +
                  "tbSampleInfo.SendDateTime>=#" + dtpStartDate.Value.ToString("yyyy-MM-dd") + "# and tbSampleInfo.SendDateTime < #" + dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd") + "# ";//AND tbSampleInfo.Status>0 AND (tbSampleInfo.SampleType not like '标准品%' OR tbSampleInfo.SampleType not like '质控品%'
            if (cmbSelect.SelectedIndex == 0)
                str = str + "AND tbSampleInfo.SampleNo like'%" + txtSampleNo.Text.ToString() + "%'";
            else if (cmbSelect.SelectedIndex == 1)
                str = str + "AND tbSampleInfo.PatientName like'%" + txtSampleNo.Text.ToString() + "%'";
            DataTable dtPatientInfo = DbHelperOleDb.Query(1, @str).Tables[0];
            DataView dv = dtPatientInfo.DefaultView;
            dv.Sort = "SendDateTimeS desc,SampleNo";
            dgvPatientInfo.DataSource = dv;
            if (dgvPatientInfo.Rows.Count == 0)
            {
                while (dgvSampleData.Rows.Count > 0)
                    dgvSampleData.Rows.Remove(dgvSampleData.Rows[dgvSampleData.Rows.Count - 1]);
                return;
            }
            //2018-08-09 zlx mod
            //string str = "select tbAssayResult.AssayResultID,tbAssayResult.SampleID,tbSampleInfo.SampleNo,"
            //          + "tbAssayResult.ItemName,tbAssayResult.TestDate,tbAssayResult.PMTCounter,"
            //          + "tbAssayResult.Concentration,tbAssayResult.Unit,tbAssayResult.Result,tbAssayResult.Range,tbSampleInfo.SampleType,tbAssayResult.Status from " //2018-08-17 添加tbAssayResult.Status
            //          + "tbAssayResult INNER JOIN tbSampleInfo on tbAssayResult.SampleID = tbSampleInfo.SampleID "
            //          + "where tbAssayResult.TestDate >=#" + dtpStartDate.Value.ToString("yyyy-MM-dd") + "# and tbAssayResult.TestDate < #" + dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd") + "#";
            //if (txtSampleNo.Text != "")
            //    str = str + " AND tbSampleInfo.SampleNo='" + txtSampleNo.Text.ToString() + "'";
            //DbHelperOleDb DB = new DbHelperOleDb(1);//2018-5-9 zlx add tbSampleInfo.SampleType
            //DataTable dtTestData = DbHelperOleDb.Query(@str).Tables[0];
            //            DataTable dtTestData = DbHelperOleDb.Query(@"select tbAssayResult.AssayResultID,tbAssayResult.SampleID,tbSampleInfo.SampleNo,
            //                                                         tbAssayResult.ItemName,tbAssayResult.TestDate,tbAssayResult.PMTCounter,
            //                                                         tbAssayResult.Concentration,tbAssayResult.Unit,tbAssayResult.Result,tbAssayResult.Range,tbSampleInfo.SampleType from 
            //                                                         tbAssayResult INNER JOIN tbSampleInfo on tbAssayResult.SampleID = tbSampleInfo.SampleID 
            //                                                         where tbAssayResult.TestDate >=#"
            //                                                         + dtpStartDate.Value.ToString("yyyy-MM-dd") + "#and tbAssayResult.TestDate <#"
            //                                                         + dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd") + "#").Tables[0];
            //dgvSampleData.DataSource = dtTestData;          
        }
        private BLL.tbReagent bllRg = new BLL.tbReagent();
        /// <summary>
        /// 获取试剂信息
        /// </summary>
        public void GetReagentInfo()
        {
            DbHelperOleDb db = new DbHelperOleDb(3);
            DataTable dtRI = bllRg.GetAllList().Tables[0];
            //var dr = dtRI.Select("Status <> '" + Getstring("uninstall") + "'");
            var dr = dtRI.Select("Postion <> ''");
            for (int i = 0; i < dr.Length; i++)
            {
                dtRgInfo.Rows.Add();
                dtRgInfo.Rows[i]["Postion"] = dr[i]["Postion"];
                dtRgInfo.Rows[i]["RgName"] = dr[i]["ReagentName"];
                dtRgInfo.Rows[i]["AllTestNumber"] = dr[+i]["AllTestNumber"];
                dtRgInfo.Rows[i]["leftoverTestR1"] = dr[i]["leftoverTestR1"];
                dtRgInfo.Rows[i]["leftoverTestR2"] = dr[i]["leftoverTestR2"];
                dtRgInfo.Rows[i]["leftoverTestR3"] = dr[i]["leftoverTestR3"];
                dtRgInfo.Rows[i]["leftoverTestR4"] = dr[i]["leftoverTestR4"];
                dtRgInfo.Rows[i]["BarCode"] = dr[i]["BarCode"];
                dtRgInfo.Rows[i]["Status"] = dr[i]["Status"];
                dtRgInfo.Rows[i]["Batch"] = dr[i]["Batch"];//2018-08-08 zlx add
                dtRgInfo.Rows[i]["ValidDate"] = dr[i]["ValidDate"];//2018-08-18 zlx add
            }
        }
        //2018-08-17 zlx add
        public void GetDataGridColor()
        {
            for (int i = 0; i < dgvSampleData.Rows.Count; i++)
            {
                if (Convert.ToInt32(dgvSampleData.Rows[i].Cells["Status"].Value) == 1)
                {
                    object id = dgvSampleData.Rows[i].Cells["SampleID"].Value;
                    dgvSampleData.Rows[i].DefaultCellStyle.BackColor = Color.Yellow;
                }
                else if (Convert.ToInt32(dgvSampleData.Rows[i].Cells["Status"].Value) == 9)
                {
                    object id = dgvSampleData.Rows[i].Cells["SampleID"].Value;
                    dgvSampleData.Rows[i].DefaultCellStyle.BackColor = Color.SkyBlue;
                }
                //2018-08-18 zlx add
                if (dtRgInfo.Rows.Count == 0)
                    GetReagentInfo();
                DataRow[] dr = dtRgInfo.Select("Batch='" + dgvSampleData.Rows[i].Cells["ReagentBeach"].Value + "'");
                if (dr.Length > 0)
                {
                    if (Convert.ToDateTime(dr[0]["ValidDate"]) < Convert.ToDateTime(dgvSampleData.Rows[i].Cells["TestDate"].Value))
                    {
                        dgvSampleData.Rows[i].Cells["ItemName"].Style.BackColor = Color.Pink;
                        //dgvSampleData.Rows[i].DefaultCellStyle.BackColor = Color.Pink;
                        //dgvSampleData.Rows[i].Cells["ItemName"].Value = "*-" + dgvSampleData.Rows[i].Cells["ItemName"].Value;
                    }
                }
            }
        }
        private void fbtnChoiceCurve_Click(object sender, EventArgs e)
        {
            frmHistoryScaling frmHS = new frmHistoryScaling();
            if (dgvSampleData.CurrentRow == null)
            { return; }
            frmHS.tempItemName = dgvSampleData.CurrentRow.Cells["ItemName"].Value.ToString();
            frmHS.RegentBatch = dgvSampleData.CurrentRow.Cells["ReagentBeach"].Value.ToString();//2018-11-02 zlx add
            if (frmHS.ShowDialog() == DialogResult.OK)
                er = frmHS.Caler;
            else
                return;
        }
        private void fbtnRecalculate_Click(object sender, EventArgs e)
        {
            if (dgvSampleData.SelectedRows.Count == 0)
            {
                frmMessageShow frmMsgShow = new frmMessageShow();
                frmMsgShow.MessageShow(Getstring("Recalculate"), Getstring("RecalculateMessage"));
                return;
            }
            //2018-12-07 zlx mod
            frmHistoryScaling frmHS = new frmHistoryScaling();
            if (dgvSampleData.CurrentRow == null)
            { return; }
            frmHS.tempItemName = dgvSampleData.SelectedRows[0].Cells["ItemName"].Value.ToString();
            frmHS.RegentBatch = dgvSampleData.SelectedRows[0].Cells["ReagentBeach"].Value.ToString();//2018-10-30 zlx add
            if (frmHS.ShowDialog() == DialogResult.OK)
                er = frmHS.Caler;
            if (dgvSampleData.CurrentRow == null)
            { return; };
            if (er == null)
            {
                return;
            }
            else
            {
                for (int i = 0; i < dgvSampleData.SelectedRows.Count; i++)
                {
                    dgvSampleData.SelectedRows[i].Cells["Result"].ReadOnly = false;
                    DbHelperOleDb db = new DbHelperOleDb(0);
                    DataTable tbtbProject = DbHelperOleDb.Query(0, @"select RangeType,ValueRange1,MinValue,MaxValue from tbProject where ShortName = '" + dgvSampleData.SelectedRows[i].Cells["ItemName"].Value + "'").Tables[0];
                    //db = new DbHelperOleDb(1);
                    //DataTable tResult = DbHelperOleDb.Query(1, @"select Batch,DiluteCount from tbAssayResult where AssayResultID = '" + dgvSampleData.SelectedRows[i].Cells["AssayResultID"].Value + "'").Tables[0];
                    string concentration = CalculationConcentration(dgvSampleData.SelectedRows[i].Cells["ItemName"].Value.ToString(), dgvSampleData.SelectedRows[i].Cells["ReagentBeach"].Value.ToString(), int.Parse(dgvSampleData.SelectedRows[i].Cells["PMTCounter"].Value.ToString())).ToString("#0.000");
                    #region 稀释
                    //项目中的稀释倍数
                    int DiuTimes = int.Parse(DbHelperOleDb.GetSingle(0, @"select DiluteCount from tbProject where ShortName = '" + dgvSampleData.SelectedRows[i].Cells["ItemName"].Value + "'").ToString());
                    int newDiuTimes = int.Parse(DbHelperOleDb.GetSingle(1, @"select DiluteCount from tbAssayResult where Batch = '" + dgvSampleData.SelectedRows[i].Cells["ReagentBeach"].Value.ToString() + "'").ToString());
                    //int newDiuTimes = int.Parse(tResult.Rows[0][1].ToString());
                    if (DiuTimes < newDiuTimes)
                    {
                        double DiuProportion = (double)newDiuTimes / DiuTimes;
                        concentration = (double.Parse(concentration) * DiuProportion).ToString("#0.000");
                    }
                    #endregion
                    string itemName = dgvSampleData.SelectedRows[i].Cells["ItemName"].Value.ToString();
                    string batch = dgvSampleData.SelectedRows[i].Cells["ReagentBeach"].Value.ToString();
                    int pmt = int.Parse(dgvSampleData.SelectedRows[i].Cells["PMTCounter"].Value.ToString());
                    double minValue = double.Parse(tbtbProject.Rows[0][2].ToString());
                    double maxValue = double.Parse(tbtbProject.Rows[0][3].ToString());
                    string Range1 = tbtbProject.Rows[0][1].ToString();
                    //正则表达式 表示汉字范围;
                    Regex cn = new Regex("[\u4e00-\u9fa5]+");
                    string sconcentration = concentration;
                    string result = dgvSampleData.SelectedRows[i].Cells["Result"].Value.ToString();
                    if (double.IsNaN(double.Parse(concentration)))
                    {
                        concentration = GetNanPmtConcentration(itemName, batch, pmt, minValue, maxValue);
                        sconcentration = concentration.Substring(1);
                    }
                    else if (double.Parse(concentration) < minValue)
                    {
                        concentration = "<" + minValue.ToString("#0.000");
                        sconcentration = concentration.Substring(1);
                    }
                    else if (double.Parse(concentration) > (maxValue))
                    {
                        concentration = ">" + (maxValue).ToString("#0.000");
                        sconcentration = concentration.Substring(1);
                    }
                    if (dgvSampleData.SelectedRows[i].Cells["Result"].Value.ToString() == "")
                        result = "";
                    else
                    {
                        if (cn.IsMatch(Range1))//range1字符串中有中文
                        {
                            result = "";
                        }
                        else if (Regex.Matches(Range1, "[a-zA-Z]").Count > 0)//range1字符串中有英文
                        {
                            result = "";
                        }
                        else
                        {
                            if (Range1.Contains("-"))
                            {
                                string[] ranges = Range1.Split('-');
                                if (double.Parse(sconcentration) < double.Parse(ranges[0]))
                                {
                                    result = "↓";
                                }
                                else if (double.Parse(sconcentration) > double.Parse(ranges[1]))
                                {
                                    result = "↑";
                                }
                                else
                                    result = getString("keywordText.normal");
                            }
                            else if (Range1.Contains("<"))
                            {
                                if (double.Parse(sconcentration) >= double.Parse(Range1.Substring(1)))
                                {
                                    result = "↑";
                                }
                                else
                                {
                                    result = getString("keywordText.normal");
                                }
                            }
                            else if (Range1.Contains("<="))
                            {
                                if (double.Parse(sconcentration) > double.Parse(Range1.Substring(2)))
                                {
                                    result = "↑";
                                }
                                else
                                {
                                    result = getString("keywordText.normal");
                                }
                            }
                            else if (Range1.Contains(">"))
                            {
                                if (double.Parse(sconcentration) <= double.Parse(Range1.Substring(1)))
                                {
                                    result = "↓";
                                }
                                else
                                {
                                    result = getString("keywordText.normal");
                                }

                            }
                            else if (Range1.Contains(">="))
                            {
                                if (double.Parse(sconcentration) < double.Parse(Range1.Substring(2)))
                                {
                                    result = "↓";
                                }
                                else
                                {
                                    result = getString("keywordText.normal");
                                }
                            }
                        }
                    }

                    dgvSampleData.SelectedRows[i].Cells[7].Value = concentration;
                    //if (!double.IsNaN(double.Parse(concentration)) && dgvSampleData.SelectedRows[i].Cells["ItemName"].Value.ToString() != "")
                    dgvSampleData.SelectedRows[i].Cells["Result"].Value = result;
                    //更新重新计算后的结果到数据库
                    DbHelperOleDb DB = new DbHelperOleDb(1);
                    DbHelperOleDb.ExecuteSql(1, @"update tbAssayResult set Concentration = '" +
                    dgvSampleData.SelectedRows[i].Cells["Concentration"].Value.ToString() + "' where AssayResultID = " + int.Parse
                    (dgvSampleData.SelectedRows[i].Cells[1].Value.ToString()));
                    DbHelperOleDb.ExecuteSql(1, @"update tbAssayResult set Result = '" +
                    dgvSampleData.SelectedRows[i].Cells["Result"].Value.ToString() + "' where AssayResultID = " + int.Parse
                    (dgvSampleData.SelectedRows[i].Cells[1].Value.ToString()));

                    dgvSampleData.SelectedRows[i].Cells["Result"].ReadOnly = true;
                }
            }
            //fbtnSaveData.Visible = true;
        }
        /// <summary>
        /// 浓度为无法计算的NaN值计算浓度
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <param name="batch">批号</param>
        /// <param name="pmt">发光值</param>
        /// <returns>显示.浓度</returns>
        private string GetNanPmtConcentration(string name, string batch, int pmt, double minValue, double maxValue)
        {
            string concentration = string.Empty;
            DbHelperOleDb dbflag = new DbHelperOleDb(0);
            int calMode = int.Parse(DbHelperOleDb.GetSingle(0,
                @"select CalMode from tbProject where ShortName = '" + name + "'").ToString());
            List<Data_Value> scaling = GetScalingResult(name, batch);
            if (calMode == 0)
            {
                if (pmt > scaling[0].DataValue)
                {
                    concentration = "<" + scaling[0].Data.ToString("#0.000");
                }
                else
                {
                    concentration = ">" + maxValue.ToString("#0.000");
                }
            }
            if (calMode == 2)
            {
                if (pmt < scaling[0].DataValue)
                {
                    concentration = "<" + scaling[0].Data.ToString("#0.000");
                }
                else
                {
                    concentration = ">" + maxValue.ToString("#0.000");
                }
            }

            return concentration;
        }
        #region 计算浓度
        /// <summary>
        /// 根据发光值计算浓度
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <param name="reagentBatch">试剂批号</param>
        /// <param name="yValue">发光值</param>
        /// <returns></returns>
        private double CalculationConcentration(string name, string reagentBatch, double yValue)
        {
            if (yValue < 0)
                yValue = 0;

            Calculater er = GetCalculater(name);
            List<Data_Value> scalingResult = GetScalingResult(name, reagentBatch);

            if (scalingResult == null)
                return 0;

            if (scalingResult[0].Data == 0)
                scalingResult[0].Data = 0.0001;

            #region 超出范围
            if (scalingResult[0].DataValue < scalingResult[1].DataValue)
            {
                if (yValue < scalingResult[0].DataValue)
                    return scalingResult[0].Data - 0.1;

                //if (yValue > scalingResult[scalingResult.Count() - 1].DataValue)
                //    return scalingResult[scalingResult.Count() - 1].Data;
            }

            if (scalingResult[0].DataValue > scalingResult[1].DataValue)
            {
                if (yValue > scalingResult[0].DataValue)
                    return scalingResult[0].Data - 0.1;

                //if (yValue < scalingResult[scalingResult.Count() - 1].DataValue)
                //    return scalingResult[scalingResult.Count() - 1].Data;
            }
            #endregion

            er.AddData(scalingResult);
            er.Fit();

            return er.GetResultInverse(yValue);
        }

        /// <summary>
        /// 获取拟合算法实例
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <returns></returns>
        private Calculater GetCalculater(string name)
        {
            DbHelperOleDb db = new DbHelperOleDb(0);
            int calMode = int.Parse(DbHelperOleDb.GetSingle(0,
                @"select CalMode from tbProject where ShortName = '" + name + "'").ToString());

            CalculateFactory calculate = new CalculateFactory();
            Calculater er = calculate.getCaler(calMode);

            return er;
        }

        /// <summary>
        /// 获取定标结果
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <param name="reagentBatch">试剂批号</param>
        /// <returns></returns>
        private List<Data_Value> GetScalingResult(string name, string reagentBatch)
        {
            BLL.tbScalingResult bllscalResult = new BLL.tbScalingResult();
            DbHelperOleDb db = new DbHelperOleDb(1);
            DataTable scalingResultTemp = bllscalResult.GetList("ItemName='" + name + "' AND RegentBatch='" + reagentBatch + "' AND Status= 1").Tables[0];

            List<Data_Value> scalingResult = new List<Data_Value>();

            if (scalingResultTemp.Rows.Count > 0)
            {
                string[] mainPoint = scalingResultTemp.Rows[scalingResultTemp.Rows.Count - 1]["Points"].ToString().Replace("(", "").Replace(")", "").Split(';');
                for (int i = 0; i < mainPoint.Length; i++)
                {
                    string[] pointinfo = mainPoint[i].Split(',');
                    if (pointinfo.Length == 2)
                        scalingResult.Add(new Data_Value() { Data = double.Parse(pointinfo[0]), DataValue = double.Parse(pointinfo[1]) });
                }

                return scalingResult;
            }

            return null;
        }
        #endregion
        /// <summary>
        /// 获取结果标识  2018-12-07 zlx add
        /// </summary>
        ///<param name="ResultId">实验结果编号</param>
        ///<param name="concentration">浓度值</param>
        /// <returns></returns>
        private string GetResult(string ResultId, string concentration)
        {
            string Result = "";
            DbHelperOleDb db = new DbHelperOleDb(1);
            DataTable tbtbProject = DbHelperOleDb.Query(1, @"select Range from tbAssayResult where AssayResultID = " + long.Parse(ResultId) + "").Tables[0];
            string Range = tbtbProject.Rows[0]["Range"].ToString();
            string[] SpRange = Range.Split(' ');
            Regex cn = new Regex("[\u4e00-\u9fa5]+");
            db = new DbHelperOleDb(1);
            DataTable tbProject = DbHelperOleDb.Query(1, @"select ItemName from tbAssayResult where AssayResultID = " + long.Parse(ResultId) + "").Tables[0];
            string itemname = tbProject.Rows[0]["ItemName"].ToString();
            DbHelperOleDb ds = new DbHelperOleDb(0);
            double MinValue = double.Parse(DbHelperOleDb.GetSingle(0, @"select MinValue from tbProject where ShortName = '" + itemname + "'").ToString());
            double MaxValue = double.Parse(DbHelperOleDb.GetSingle(0, @"select MaxValue from tbProject where ShortName = '" + itemname + "'").ToString());
            string sconcentration = concentration;
            if (concentration.Contains("<") || concentration.Contains(">"))
            {
                sconcentration = concentration.Substring(1);
                //Result = Getstring("NotRangeMessage");
            }
            if (cn.IsMatch(Range))//range1字符串中有中文
            {
                Result = "";
            }
            else if (Regex.Matches(Range, "[a-zA-Z]").Count > 0)//range1字符串中有英文
            {
                Result = "";
            }
            else
            {
                double dconcentration = double.Parse(sconcentration);
                if (Range.Contains("-"))
                {
                    string[] ranges = Range.Split('-');
                    if (dconcentration < double.Parse(ranges[0]))
                    {
                        Result = "↓";
                    }
                    else if (dconcentration > double.Parse(ranges[1]))
                    {
                        Result = "↑";
                    }
                    else
                        Result = Getstring("Normal");
                }
                else if (Range.Contains("<"))
                {
                    if (dconcentration >= double.Parse(Range.Substring(1)))
                    {
                        Result = "↑";
                    }
                    else
                    {
                        Result = Getstring("Normal");
                    }
                }
                else if (Range.Contains("<="))
                {
                    if (dconcentration > double.Parse(Range.Substring(2)))
                    {
                        Result = "↑";
                    }
                    else
                    {
                        Result = Getstring("Normal");
                    }
                }
                else if (Range.Contains(">"))
                {
                    if (dconcentration <= double.Parse(Range.Substring(1)))
                    {
                        Result = "↓";
                    }
                    else
                    {
                        Result = Getstring("Normal");
                    }
                }
                else if (Range.Contains(">="))
                {
                    if (dconcentration < double.Parse(Range.Substring(2)))
                    {
                        Result = "↓";
                    }
                    else
                    {
                        Result = Getstring("Normal");
                    }
                }
            }
            return Result;
        }
        private void fbtnSaveData_Click(object sender, EventArgs e)
        {
            if (dgvSampleData.CurrentRow == null)
            {
                return;
            }
            DbHelperOleDb DB = new DbHelperOleDb(1);
            for (int i = 0; i < dgvSampleData.SelectedRows.Count; i++)
            {
                DbHelperOleDb.ExecuteSql(1, @"update tbAssayResult set Concentration = " +
               dgvSampleData.SelectedRows[i].Cells[7].Value.ToString() + " where AssayResultID = " + int.Parse
               (dgvSampleData.SelectedRows[i].Cells[1].Value.ToString()));
            }
            frmMsgShow.MessageShow(Getstring("SaveHead"), Getstring("SaveMessage"));
        }

        private void fbtnDeleteResult_Click(object sender, EventArgs e)
        {
            if (dgvSampleData.CurrentRow == null) return;
            BLL.tbAssayResult bllAssayResult = new BLL.tbAssayResult();
            for (int i = dgvSampleData.Rows.Count - 1; i >= 0; i--)
            {
                if (dgvSampleData.Rows[i].Selected)
                {
                    //2018-08-11  zlx add 
                    if (dgvSampleData.CurrentRow.Cells["SampleType"].Value.ToString() == Getstring("OutInfo"))
                        dgvSampleData.Rows.RemoveAt(i);
                    else
                    {
                        //删除样本信息及对应的项目
                        bllAssayResult.Delete(int.Parse(dgvSampleData.Rows[i].Cells["AssayResultID"].Value.ToString()));
                        dgvSampleData.Rows.RemoveAt(i);
                    }
                }
            }
            frmMsgShow.MessageShow(Getstring("SaveHead"), Getstring("DeleteMessage"));
        }

        private void fbtnModifyResult_Click(object sender, EventArgs e)
        {
            if (dgvSampleData.CurrentRow == null)
            {
                return;
            }
            frmModifyTestResult frmMTR = new frmModifyTestResult();
            string sampleID = dgvSampleData.CurrentRow.Cells["SampleID"].Value.ToString();
            frmMTR.sampleId = int.Parse(sampleID);
            frmMTR.sampleNo = dgvSampleData.CurrentRow.Cells["SampleNo"].Value.ToString();
            string assayresultid = dgvSampleData.CurrentRow.Cells["AssayResultID"].Value.ToString();
            frmMTR.assayresultId = int.Parse(assayresultid);
            if (frmMTR.ShowDialog() != DialogResult.OK) return;
            dgvPatientInfo_SelectionChanged(null, null);//2018-11-10 zlx mod
            //fbtnQuery_Click(null, null);
        }

        private void fbtnPrint_MouseDown(object sender, MouseEventArgs e)
        {
            //if (dgvPatientInfo.CurrentRow == null)
            //{
            //    return;
            //}
            if (dgvPatientInfo.SelectedRows.Count == 0 || dgvSampleData.RowCount == 0)
                return;
            if (fbtnPrint.Text == Getstring("Print"))
            {
                fbtnQuery.Enabled = false;//2018-4-20  zlx add
                fbtnModifyResult.Enabled = false;
                fbtnDeleteResult.Enabled = false;
                printMode(false);
            }
            else if (fbtnPrint.Text == Getstring("Determine"))
            {
                #region chart
                //string[] releaseChartInfo1 =null;
                //string[] releassChartInfo2 = null;
                string psaRatio = null;
                string fshLhRatio = null;
                string pgiRatio = null;

                string sampleNum = dgvPatientInfo.SelectedRows[0].Cells["SampleNo1"].Value.ToString();//样本编号
                string name = dgvPatientInfo.SelectedRows[0].Cells["PatientName"].Value.ToString();
                string sex = dgvPatientInfo.SelectedRows[0].Cells["Sex"].Value.ToString();
                string age = dgvPatientInfo.SelectedRows[0].Cells["Age"].Value.ToString();
                string clinicNo = dgvPatientInfo.SelectedRows[0].Cells["ClinicNo"].Value.ToString();
                string medicaRecordNo = dgvPatientInfo.SelectedRows[0].Cells["MedicaRecordNo"].Value.ToString();
                DataTable table = ((DataTable)dgvSampleData.DataSource).Copy();
                int countProList = 0;
                foreach (string Pro in DataAnalysisProList)
                {
                    if (Pro != "")
                    {
                        countProList = countProList + table.Select("ItemName = '" + Pro + "'").Length ;
                    }
                }
                if (countProList > 0)
                {
                    DataQuery.frmDataAnalysis f = new DataQuery.frmDataAnalysis(new List<string>() { sampleNum }, name, sex, age, medicaRecordNo, clinicNo);
                    f.ShowDialog();
                    if (frmDataAnalysis.confirmClosed == true)
                    {
                        //releaseChartInfo1 = frmDataAnalysis.releaseChartInfo1;
                        //releassChartInfo2 = frmDataAnalysis.releassChartInfo2;
                        psaRatio = frmDataAnalysis.psaRatio;
                        fshLhRatio = frmDataAnalysis.fshLhRatio;
                        pgiRatio = frmDataAnalysis.pgiRatio;
                    }
                }
               
                #endregion
                #region 声明打印变量及属性
                Report report = new Report();
                Config.ReportSettings.ShowPerformance = false;
                Config.ReportSettings.ShowProgress = true;
                //report.Load(Application.StartupPath + @"\Report\A5ZH.frx");
                DataSet dsReport = new DataSet();
                #endregion
                #region 获取选中的样本结果
                DataTable dtTestResult = new DataTable();
                string printModel = OperateIniFile.ReadIniData("PrintSet", "PrintMode", "", Application.StartupPath + "//InstrumentPara.ini");
                string modelPath = Application.StartupPath + @"\Report\";
                //if (printModel.Contains("模版一") || printModel.Contains("1"))
                //{
                //    modelPath += "A5ZH-模版一.frx";
                //}
                //else if (printModel.Contains("模版二") || printModel.Contains("2"))
                //{
                //    modelPath += "A5ZH-模版二.frx";
                //}
                //else if (printModel.Contains("模版三") || printModel.Contains("3"))
                //{
                //    modelPath += "A5ZH-模版三.frx";
                //}
                //else if (printModel.Contains("模版六") || printModel.Contains("6"))
                //{
                //    modelPath += "A5ZH-模版六.frx";
                //}
                if (printModel == " ")
                {
                    if (System.Globalization.CultureInfo.CurrentCulture.ToString() == "en")
                    {
                        printModel = "Mode 2";
                    }
                    else
                        printModel = "模版二";
                }
                modelPath += "A5ZH-" + printModel + ".frx";
                report.Load(modelPath);
                dtTestResult.Columns.Add(new DataColumn("ShortName", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Concentration", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Result", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Range1", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Range2", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("printIndex", typeof(int)));
                #region lyq
                dtTestResult.Columns.Add(new DataColumn("ShortName-2", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Concentration-2", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Result-2", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Range1-2", typeof(string)));
                dtTestResult.Columns.Add(new DataColumn("Range2-2", typeof(string)));

                BLL.tbProject bllPro = new BLL.tbProject();//lyq
                DataTable dtPro = bllPro.GetList("").Tables[0];
                #endregion

                DataRow dr;
                int count = dgvSampleData.Rows.Count;
                for (int i = 0; i < count; i++)
                {
                    string _selectValue = dgvSampleData.Rows[i].Cells["colChioce"].EditedFormattedValue.ToString();
                    if (_selectValue == "True")
                    {
                        dgvSampleData.Rows[i].Selected = true;
                        dr = dtTestResult.NewRow();
                        DataRow[] drPro = dtPro.Select("ShortName = '" + dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString() + "'");
                        if (drPro.Length > 0)
                            dr["ShortName"] = drPro[0]["FullName"].ToString();
                        else
                            dr["ShortName"] = dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString();//lyq/*dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString();*/
                        //dr["ShortName"] = dtPro.Select("ShortName = '" + dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString() + "'")[0]["FullName"].ToString();//lyq/*dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString();*/
                        dr["Concentration"] = dgvSampleData.Rows[i].Cells["Concentration"].Value.ToString();
                        dr["Result"] = dgvSampleData.Rows[i].Cells["Result"].Value.ToString();
                        dr["Range1"] = dgvSampleData.Rows[i].Cells["Range"].Value.ToString();
                        dr["Range2"] = dgvSampleData.Rows[i].Cells["Unit"].Value.ToString();//2018-11-02 zlx mod
                        //string printIndex = OperateIniFile.ReadIniData("RpSort", dr["ShortName"].ToString(), "",
                        //    Application.StartupPath + "//ReportSort.ini");                                                                    

                        string printIndex = OperateIniFile.ReadIniData("RpSort", dgvSampleData.Rows[i].Cells["ItemName"].Value.ToString(), "",
                             Application.StartupPath + "//ReportSort.ini");
                        if (printIndex == "")
                            printIndex = "999";
                        dr["printIndex"] = printIndex;
                        dtTestResult.Rows.Add(dr);
                    }
                }
                //排序
                dtTestResult.DefaultView.Sort = "printIndex asc";
                dtTestResult = dtTestResult.DefaultView.ToTable();
                dtTestResult.TableName = "Records";
                count = dtTestResult.Rows.Count;
                if (printModel.Contains("模版一")|| printModel.Contains("Mode 1"))
                {
                    if (count <= 10)
                        ;
                    else if (count <= 20)
                    {
                        //移动到右列
                        for (int i = 10; i < count; i++)
                        {
                            dtTestResult.Rows[i - 10]["ShortName-2"] = dtTestResult.Rows[i]["ShortName"];  //dtPro.Select("ShortName = '" + dgvSampleData.Rows[i + 24].Cells["ItemName"].Value.ToString() + "'")[0]["FullName"];//lyq
                            dtTestResult.Rows[i - 10]["Concentration-2"] = dtTestResult.Rows[i]["Concentration"]; //dgvSampleData.Rows[i + 24].Cells["Concentration"].Value.ToString();
                            dtTestResult.Rows[i - 10]["Result-2"] = dtTestResult.Rows[i]["Result"]; //dgvSampleData.Rows[i + 24].Cells["Result"].Value.ToString();
                            dtTestResult.Rows[i - 10]["Range1-2"] = dtTestResult.Rows[i]["Range1"]; // dgvSampleData.Rows[i + 24].Cells["Range"].Value.ToString();
                            dtTestResult.Rows[i - 10]["Range2-2"] = dtTestResult.Rows[i]["Range2"]; // dgvSampleData.Rows[i + 24].Cells["Unit"].Value.ToString();
                        }
                        //移除多余列
                        for (int i = count; i > 10; i--)
                        {
                            dtTestResult.Rows.RemoveAt(i - 1);
                        }
                    }
                    else
                    {
                        //移动到右列
                        for (int i = (count + 1) / 2; i < count; i++)
                        {
                            dtTestResult.Rows[i - (count + 1) / 2]["ShortName-2"] = dtTestResult.Rows[i]["ShortName"];  //dtPro.Select("ShortName = '" + dgvSampleData.Rows[i + 24].Cells["ItemName"].Value.ToString() + "'")[0]["FullName"];//lyq
                            dtTestResult.Rows[i - (count + 1) / 2]["Concentration-2"] = dtTestResult.Rows[i]["Concentration"]; //dgvSampleData.Rows[i + 24].Cells["Concentration"].Value.ToString();
                            dtTestResult.Rows[i - (count + 1) / 2]["Result-2"] = dtTestResult.Rows[i]["Result"]; //dgvSampleData.Rows[i + 24].Cells["Result"].Value.ToString();
                            dtTestResult.Rows[i - (count + 1) / 2]["Range1-2"] = dtTestResult.Rows[i]["Range1"]; // dgvSampleData.Rows[i + 24].Cells["Range"].Value.ToString();
                            dtTestResult.Rows[i - (count + 1) / 2]["Range2-2"] = dtTestResult.Rows[i]["Range2"]; // dgvSampleData.Rows[i + 24].Cells["Unit"].Value.ToString();
                        }
                        //移除多余列
                        for (int i = count; i > (count + 1) / 2; i--)
                        {
                            dtTestResult.Rows.RemoveAt(i - 1);
                        }
                    }
                }
                #endregion
                //dtTestResult.DefaultView.Sort = "printIndex asc";
                //dtTestResult = dtTestResult.DefaultView.ToTable();
                //dtTestResult.TableName = "Records";
                dsReport.Tables.Clear();
                dsReport.Tables.Add(dtTestResult.Copy());
                #region 打印模板参数赋值


                report.SetParameterValue("ClinicNo", dgvPatientInfo.SelectedRows[0].Cells["ClinicNo"].Value);
                report.SetParameterValue("Diagnosis", dgvPatientInfo.SelectedRows[0].Cells["Diagnosis"].Value);
                report.SetParameterValue("SampleNo", dgvPatientInfo.SelectedRows[0].Cells["SampleNo1"].Value);

                report.SetParameterValue("PatientName", dgvPatientInfo.SelectedRows[0].Cells["PatientName"].Value);
                report.SetParameterValue("Sex", dgvPatientInfo.SelectedRows[0].Cells["Sex"].Value);
                report.SetParameterValue("Age", dgvPatientInfo.SelectedRows[0].Cells["Age"].Value);
                report.SetParameterValue("BedNo", dgvPatientInfo.SelectedRows[0].Cells["BedNo"].Value);
                #region release Chart

                if (psaRatio != null)
                {
                    report.SetParameterValue("RatioPSA", psaRatio);
                }
                else
                {
                    report.SetParameterValue("RatioPSA", "abc");
                }
                if (fshLhRatio != null)
                {
                    report.SetParameterValue("RatioFSH", fshLhRatio);
                }
                else
                {
                    report.SetParameterValue("RatioFSH", "abc");
                }
                if (pgiRatio != null)
                {
                    report.SetParameterValue("RatioPGI", pgiRatio);
                }
                else
                {
                    report.SetParameterValue("RatioPGI", "abc");
                }
                #endregion

                #region lyq report
                report.SetParameterValue("MedicaRecordNo", dgvPatientInfo.SelectedRows[0].Cells["MedicaRecordNo"].Value);//病历号
                report.SetParameterValue("InspectionItems", dgvPatientInfo.SelectedRows[0].Cells["InspectionItems"].Value);//检验项目
                report.SetParameterValue("AcquisitionTime", dgvPatientInfo.SelectedRows[0].Cells["AcquisitionTime"].Value);//采集时间                
                report.SetParameterValue("Date", DateTime.Now.ToString());//报告时间


                string str = @"select Source,[Position],SampleContainer,[RepeatCount],RegentBatch,ProjectName,[Emergency],InpatientArea,Ward,[Status] " +//
                    "from tbSampleInfo where SampleID=" + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString();
                DataTable dtTemp = DbHelperOleDb.Query(1, str).Tables[0];

                report.SetParameterValue("SampleID", dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                report.SetParameterValue("Source", dtTemp.Rows[0]["Source"].ToString());
                report.SetParameterValue("Position", dtTemp.Rows[0]["Position"].ToString());
                report.SetParameterValue("SampleContainer", dtTemp.Rows[0]["SampleContainer"].ToString());
                report.SetParameterValue("RepeatCount", dtTemp.Rows[0]["RepeatCount"].ToString());
                report.SetParameterValue("RegentBatch", dtTemp.Rows[0]["RegentBatch"].ToString());
                report.SetParameterValue("ProjectName", dtTemp.Rows[0]["ProjectName"].ToString());
                report.SetParameterValue("Emergency", dtTemp.Rows[0]["Emergency"].ToString());
                report.SetParameterValue("InpatientArea", dtTemp.Rows[0]["InpatientArea"].ToString());
                report.SetParameterValue("Ward", dtTemp.Rows[0]["Ward"].ToString());
                report.SetParameterValue("Status", dtTemp.Rows[0]["Status"].ToString());
                #endregion

                object SampleType = DbHelperOleDb.GetSingle(1, @"select SampleType from tbSampleInfo where SampleID = "
                                                         + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (SampleType == null)
                {
                    SampleType = " ";
                }
                report.SetParameterValue("SampleType", SampleType);

                object Department = DbHelperOleDb.GetSingle(1, @"select Department from tbSampleInfo where SampleID = "
                                                         + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (Department == null)
                {
                    Department = " ";
                }
                report.SetParameterValue("Department", Department);
                object SendDoctor = DbHelperOleDb.GetSingle(1, @"select SendDoctor from tbSampleInfo where SampleID = "
                                                         + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (SendDoctor == null)
                {
                    SendDoctor = " ";
                }
                report.SetParameterValue("SendDoctor", SendDoctor);
                object SendDateTime = DbHelperOleDb.GetSingle(1, @"select SendDateTime from tbSampleInfo where SampleID = "
                                                       + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (SendDateTime == null)
                {
                    SendDateTime = " ";
                }
                report.SetParameterValue("SendDateTime", SendDateTime);
                object InspectDoctor = DbHelperOleDb.GetSingle(1, @"select InspectDoctor from tbSampleInfo where SampleID = "
                                                       + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (InspectDoctor == null)
                {
                    InspectDoctor = LoginGName;//2018-11-20 zlx mod
                }
                report.SetParameterValue("InspectDoctor", InspectDoctor);
                //2018-11-12 zlx add
                object CheckDoctor = DbHelperOleDb.GetSingle(1, @"select CheckDoctor from tbSampleInfo where SampleID = "
                                              + dgvSampleData.Rows[0].Cells["SampleID"].Value.ToString());
                if (CheckDoctor == null)
                {
                    CheckDoctor = "";
                }
                report.SetParameterValue("CheckDoctor", CheckDoctor);
                report.SetParameterValue("RecordCount", dtTestResult.Rows.Count);
                report.SetParameterValue("title", OperateIniFile.ReadInIPara("PrintSet", "HospitalName"));
                report.RegisterData(dsReport);
                #endregion
                //是否直接打印
                report.PrintSettings.ShowDialog = !bool.Parse(OperateIniFile.ReadInIPara("PrintSet", "AutoPrint"));//2018-11-02 zlx mod
                ReportPage rp = report.Pages[0] as ReportPage;
                //A4,A5选择
                if (OperateIniFile.ReadInIPara("PrintSet", "PageSize") == "A4")
                {
                    rp.PaperHeight = 297f;
                }
                else
                {
                    rp.PaperHeight = 148.5f;
                }
                //页边距设置
                string Margin = OperateIniFile.ReadInIPara("PrintSet", "Margin");
                string[] udlr = Margin.Split('|');
                rp.TopMargin = float.Parse(udlr[0]) * 10;
                rp.BottomMargin = float.Parse(udlr[1]) * 10;
                rp.LeftMargin = float.Parse(udlr[2]) * 10;
                rp.RightMargin = float.Parse(udlr[3]) * 10;
                try
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        report.Print();
                        //frmMsgShow.MessageShow("结果查询", "打印成功！");
                    }
                    else
                    {
                        report.Show();
                    }
                }
                catch (Exception ex)
                {
                    frmMessageShow frmMS = new frmMessageShow();
                    frmMS.MessageShow(Getstring("SaveHead"), ex.Message);
                    frmMS.Dispose();
                }
                report.Dispose();
                printMode(true);
                fbtnQuery.Enabled = true;//2018-4-20  zlx add
                fbtnModifyResult.Enabled = true;
                fbtnDeleteResult.Enabled = true;
            }
        }
        /// <summary>
        /// 点击打印按钮对控件进行编辑
        /// </summary>
        /// <param name="printFlag"></param>
        void printMode(bool printFlag)
        {
            //dgvSampleData.ReadOnly = printFlag;
            string SampleID = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value.ToString();//2018-11-13 zlx mod
            for (int i = 0; i < dgvSampleData.RowCount; i++)
            {
                //if (dgvSampleData.Rows[i].Cells["SampleID"].Value.ToString() != SampleID)
                //    dgvSampleData.Rows[i].Visible = printFlag;
                dgvSampleData.Rows[i].Cells["colChioce"].Value = true;//2018-11-02 zlx mod
            }
            if (printFlag == false)
            {
                fbtnPrint.Text = Getstring("Determine");
                dgvSampleData.Columns[0].Visible = true;
                dgvSampleData.SelectionMode = DataGridViewSelectionMode.CellSelect;
            }
            else
            {
                fbtnPrint.Text = Getstring("Print");
                dgvSampleData.Columns[0].Visible = false;
                dgvSampleData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvSampleData.Rows[0].Selected = true;
            }
        }

        private void fbtnMakeupInfo_Click(object sender, EventArgs e)
        {
            if (dgvPatientInfo.SelectedRows.Count == 0)
            {
                frmMsg.MessageShow(Getstring("SampleInputHead"), Getstring("SampleInputMessage"));
                return;
            }
            frmPatientInfo frmPI = new frmPatientInfo();
            //string SampleID = dgvSampleData.CurrentRow.Cells["SampleID"].Value.ToString();
            string SampleID = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value.ToString();//2018-11-13 zlx mod
            frmPI.SampleID = int.Parse(SampleID);
            if (frmPI.ShowDialog() != DialogResult.OK) return;
            fbtnQuery_Click(null, null);//2018-11-10 zlx mod
            foreach (DataGridViewRow dgr in dgvPatientInfo.Rows)
            {
                if (dgr.Cells["SampleID1"].Value.ToString().Contains(SampleID))
                {
                    dgr.Selected = true;
                    return;
                }
                dgr.Selected = false;
            }
            //dgvSampleData_SelectionChanged(null, null);
        }
        #endregion

        private void dgvSampleData_SelectionChanged(object sender, EventArgs e)
        {
            //            #region 获取选中样本的病人信息
            //            if (dgvSampleData.CurrentRow == null || dgvSampleData.CurrentRow.Cells["SampleID"].Value == null)
            //            {
            //                return;
            //            }
            //            //2018-08-10  zlx add
            //            if (dgvSampleData.CurrentRow.Cells["SampleType"].Value != null && dgvSampleData.CurrentRow.Cells["SampleType"].Value.ToString() == "外部信息")
            //            {
            //                fbtnSaveData.Enabled=fbtnModifyResult.Enabled = false;
            //            }
            //            else
            //            {
            //                fbtnSaveData.Enabled = fbtnModifyResult.Enabled = true;
            //            }
            //            string SampleID = dgvSampleData.CurrentRow.Cells["SampleID"].Value.ToString();
            //            if (SampleID == "")
            //            {
            //                return;
            //            }
            //            DbHelperOleDb DB = new DbHelperOleDb(1);
            //            DataTable dtPatientInfo = DbHelperOleDb.Query(@"select SampleID,PatientName,Sex,Age,ClinicNo,InpatientArea,
            //                                                            Ward,BedNo,MedicaRecordNo,Diagnosis from tbSampleInfo where
            //                                                            SampleID =  " + int.Parse(SampleID)).Tables[0];
            //            dgvPatientInfo.DataSource = dtPatientInfo;
            //            #endregion
        }

        private void tbnSendResult_Click(object sender, EventArgs e)//2018-4-28 zlx add
        {

            if (dgvPatientInfo.SelectedRows.Count < 1) return;
            if (dgvPatientInfo.SelectedRows.Count > 20)
            {
                frmMessageShow f = new frmMessageShow();
                f.MessageShow(Getstring("Tips"), Getstring("NumberWarning"));
                return;
            }

            for (int index = 0; index < dgvPatientInfo.SelectedRows.Count; index++)
            {
                SetTestData(dgvPatientInfo.SelectedRows[index].Cells["SampleID1"].Value.ToString());

                #region 若LIS实时发送数据为选中转态，发送实验报告
                string CommunicationType = OperateIniFile.ReadInIPara("LisSet", "CommunicationType");

                string tranInfo = OperateIniFile.ReadInIPara("LisSet", "TransInfo");
                if (CommunicationType.Contains("NetConn") || CommunicationType.Contains("网口通讯"))
                {
                    #region 网口通讯批量发送实验结果
                    if (LisCommunication.Instance.IsConnect())
                    {
                        DataTable dtshow = new DataTable();
                        DataColumn dc = new DataColumn("SampleNo", typeof(string));
                        dtshow.Columns.Add(dc);
                        DataTable _dtTest = (DataTable)dgvSampleData.DataSource;
                        for (int i = 0; i < dgvSampleData.RowCount; i++)
                        {
                            DataRow[] dr = dtshow.Select("SampleNo='" + dgvSampleData.Rows[i].Cells["SampleNo"].Value + "'");
                            if (dr.Length == 0)
                            {
                                DataRow row = dtshow.NewRow();
                                row["SampleNo"] = dgvSampleData.Rows[i].Cells["SampleNo"].Value;
                                dtshow.Rows.Add(row);
                            }
                        }
                        foreach (DataRow dr in dtshow.Rows)
                        {
                            List<TestResult> resultlist = new List<TestResult>();
                            DataRow[] drtest = _dtTest.Select("SampleNo='" + dr["SampleNo"] + "'");
                            for (int i = 0; i < drtest.Length; i++)
                            {
                                TestResult result = new TestResult();
                                result.SampleID = Convert.ToInt32(drtest[i]["SampleID"]);
                                result.SampleNo = drtest[i]["SampleNo"].ToString();
                                result.ItemName = drtest[i]["ItemName"].ToString();
                                result.PMT = Convert.ToInt32(drtest[i]["PMTCounter"]);
                                result.concentration = drtest[i]["Concentration"].ToString();
                                result.Result = drtest[i]["Result"].ToString();
                                result.Range1 = drtest[i]["Range"].ToString();
                                result.Range2 = drtest[i]["Range"].ToString();
                                result.SampleType = drtest[i]["SampleType"].ToString();
                                resultlist.Add(result);
                            }

                            CMessageParser Cmp = new CMessageParser();
                            Cmp.SendApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
                            Cmp.SendFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
                            Cmp.SendORU(resultlist);
                        }

                    }
                    else
                    {
                        frmMessageShow messageShow = new frmMessageShow();
                        messageShow.MessageShow(Getstring("MessageHead"), Getstring("NoConnMessage"));
                    }
                    #endregion
                }
                else if (CommunicationType.Contains("SerialConn") || CommunicationType.Contains("串口通讯"))
                {
                    #region 串口通讯批量发送实验结果
                    if (LisConnection.Instance.IsOpen())
                    {
                        if (LisConnection.Instance.BWork)
                        {
                            MessageBox.Show(Getstring("SerialConnWorkMessage"), Getstring("MessageHead"));
                            //MessageBox.Show("串口正在进行数据通讯！请稍后再进行发送！", "信息提示！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        DataTable dtshow = new DataTable();
                        DataColumn dc = new DataColumn("SampleNo", typeof(string));
                        dtshow.Columns.Add(dc);
                        DataTable _dtTest = (DataTable)dgvSampleData.DataSource;
                        for (int i = 0; i < dgvSampleData.RowCount; i++)
                        {
                            DataRow[] dr = dtshow.Select("SampleNo='" + dgvSampleData.Rows[i].Cells["SampleNo"].Value + "'");
                            if (dr.Length == 0)
                            {
                                DataRow row = dtshow.NewRow();
                                row["SampleNo"] = dgvSampleData.Rows[i].Cells["SampleNo"].Value;
                                dtshow.Rows.Add(row);
                            }
                        }
                        foreach (DataRow dr in dtshow.Rows)
                        {
                            List<TestResult> resultlist = new List<TestResult>();
                            DataRow[] drtest = _dtTest.Select("SampleNo='" + dr["SampleNo"] + "'");
                            for (int i = 0; i < drtest.Length; i++)
                            {
                                TestResult result = new TestResult();
                                result.SampleID = Convert.ToInt32(drtest[i]["SampleID"]);
                                result.SampleNo = drtest[i]["SampleNo"].ToString();
                                result.ItemName = drtest[i]["ItemName"].ToString();
                                result.PMT = Convert.ToInt32(drtest[i]["PMTCounter"]);
                                result.concentration = drtest[i]["Concentration"].ToString();
                                result.Result = drtest[i]["Result"].ToString();
                                result.Range1 = drtest[i]["Range"].ToString();
                                result.Range2 = drtest[i]["Range"].ToString();
                                result.SampleType = drtest[i]["SampleType"].ToString();
                                resultlist.Add(result);
                            }

                            CAMessageParser Cmp = new CAMessageParser();
                            Cmp.SendApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
                            Cmp.SendFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
                            Cmp.SendORU(resultlist);
                        }
                    }
                    else
                        MessageBox.Show(Getstring("SeriesNoOpenMessage"), Getstring("MessageHead"));
                    //MessageBox.Show("串口未打开！", "信息提示！");
                    #endregion
                }
                //switch (CommunicationType)
                //{
                //    case "网口通讯":
                //        #region 网口通讯批量发送实验结果
                //        if (LisCommunication.Instance.IsConnect())
                //        {
                //            DataTable dtshow = new DataTable();
                //            DataColumn dc = new DataColumn("SampleNo", typeof(string));
                //            dtshow.Columns.Add(dc);
                //            DataTable _dtTest = (DataTable)dgvSampleData.DataSource;
                //            for (int i = 0; i < dgvSampleData.RowCount; i++)
                //            {
                //                DataRow[] dr = dtshow.Select("SampleNo='" + dgvSampleData.Rows[i].Cells["SampleNo"].Value + "'");
                //                if (dr.Length == 0)
                //                {
                //                    DataRow row = dtshow.NewRow();
                //                    row["SampleNo"] = dgvSampleData.Rows[i].Cells["SampleNo"].Value;
                //                    dtshow.Rows.Add(row);
                //                }
                //            }
                //            foreach (DataRow dr in dtshow.Rows)
                //            {
                //                List<TestResult> resultlist = new List<TestResult>();
                //                DataRow[] drtest = _dtTest.Select("SampleNo='" + dr["SampleNo"] + "'");
                //                for (int i = 0; i < drtest.Length; i++)
                //                {
                //                    TestResult result = new TestResult();
                //                    result.SampleID = Convert.ToInt32(drtest[i]["SampleID"]);
                //                    result.SampleNo = drtest[i]["SampleNo"].ToString();
                //                    result.ItemName = drtest[i]["ItemName"].ToString();
                //                    result.PMT = Convert.ToInt32(drtest[i]["PMTCounter"]);
                //                    result.concentration = drtest[i]["Concentration"].ToString();
                //                    result.Result = drtest[i]["Result"].ToString();
                //                    result.Range1 = drtest[i]["Range"].ToString();
                //                    result.Range2 = drtest[i]["Range"].ToString();
                //                    result.SampleType = drtest[i]["SampleType"].ToString();
                //                    resultlist.Add(result);
                //                }

                //                CMessageParser Cmp = new CMessageParser();
                //                Cmp.SendApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
                //                Cmp.SendFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
                //                Cmp.SendORU(resultlist);
                //            }

                //        }
                //        else
                //            MessageBox.Show("系统未连接LIS服务器！", "信息提示！");
                //        #endregion
                //        break;
                //    case "串口通讯":
                //        #region 串口通讯批量发送实验结果
                //        if (LisConnection.Instance.IsOpen())
                //        {
                //            if (LisConnection.Instance.BWork)
                //            {
                //                MessageBox.Show("串口正在进行数据通讯！请稍后再进行发送！", "信息提示！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //                return;
                //            }
                //            DataTable dtshow = new DataTable();
                //            DataColumn dc = new DataColumn("SampleNo", typeof(string));
                //            dtshow.Columns.Add(dc);
                //            DataTable _dtTest = (DataTable)dgvSampleData.DataSource;
                //            for (int i = 0; i < dgvSampleData.RowCount; i++)
                //            {
                //                DataRow[] dr = dtshow.Select("SampleNo='" + dgvSampleData.Rows[i].Cells["SampleNo"].Value + "'");
                //                if (dr.Length == 0)
                //                {
                //                    DataRow row = dtshow.NewRow();
                //                    row["SampleNo"] = dgvSampleData.Rows[i].Cells["SampleNo"].Value;
                //                    dtshow.Rows.Add(row);
                //                }
                //            }
                //            foreach (DataRow dr in dtshow.Rows)
                //            {
                //                List<TestResult> resultlist = new List<TestResult>();
                //                DataRow[] drtest = _dtTest.Select("SampleNo='" + dr["SampleNo"] + "'");
                //                for (int i = 0; i < drtest.Length; i++)
                //                {
                //                    TestResult result = new TestResult();
                //                    result.SampleID = Convert.ToInt32(drtest[i]["SampleID"]);
                //                    result.SampleNo = drtest[i]["SampleNo"].ToString();
                //                    result.ItemName = drtest[i]["ItemName"].ToString();
                //                    result.PMT = Convert.ToInt32(drtest[i]["PMTCounter"]);
                //                    result.concentration = drtest[i]["Concentration"].ToString();
                //                    result.Result = drtest[i]["Result"].ToString();
                //                    result.Range1 = drtest[i]["Range"].ToString();
                //                    result.Range2 = drtest[i]["Range"].ToString();
                //                    result.SampleType = drtest[i]["SampleType"].ToString();
                //                    resultlist.Add(result);
                //                }

                //                CAMessageParser Cmp = new CAMessageParser();
                //                Cmp.SendApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
                //                Cmp.SendFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
                //                Cmp.SendORU(resultlist);
                //            }

                //        }
                //        else
                //            MessageBox.Show("串口未打开！", "信息提示！");
                //        #endregion
                //        break;
                //    default:
                //        break;
                //}

                #endregion
            }
        }
        private void SetTestData(string sampleID)
        {
            //string SampleID = dgvPatientInfo.CurrentRow.Cells[1].Value.ToString();
            string str = "select tbAssayResult.AssayResultID,tbAssayResult.SampleID,tbSampleInfo.SampleNo,"
                      + "tbAssayResult.ItemName,tbAssayResult.TestDate,tbAssayResult.PMTCounter,"
                      + "tbAssayResult.Concentration,tbAssayResult.Unit,tbAssayResult.Result,tbAssayResult.Range,tbSampleInfo.SampleType,tbAssayResult.Status,tbAssayResult.Batch from " //2018-08-17  zlx 添加tbAssayResult.Status
                      + "tbAssayResult INNER JOIN tbSampleInfo on tbAssayResult.SampleID = tbSampleInfo.SampleID "
                      + "where tbAssayResult.SampleID=" + sampleID + " AND  tbAssayResult.TestDate >=#" + dtpStartDate.Value.ToString("yyyy-MM-dd") + "# and tbAssayResult.TestDate < #" + dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd") + "# AND tbAssayResult.Status=0";

            DbHelperOleDb DB = new DbHelperOleDb(1);//2018-5-9 zlx add tbSampleInfo.SampleType
            DataTable dtTestData = DbHelperOleDb.Query(1, @str).Tables[0];
            DataTable dtResult = new DataTable();
            dtResult = dtTestData.Clone();
            foreach (DataColumn col in dtResult.Columns)
            {
                if (col.ColumnName == "Concentration")
                {
                    col.DataType = typeof(String);
                }
            }
            foreach (DataRow dr in dtTestData.Rows)
            {
                DataRow nrow = dtResult.NewRow();
                nrow["AssayResultID"] = dr["AssayResultID"];
                nrow["SampleID"] = dr["SampleID"];
                nrow["SampleNo"] = dr["SampleNo"];
                nrow["ItemName"] = dr["ItemName"];
                nrow["TestDate"] = dr["TestDate"];
                nrow["PMTCounter"] = dr["PMTCounter"];
                nrow["Unit"] = dr["Unit"];
                nrow["Result"] = dr["Result"];
                nrow["Concentration"] = dr["Concentration"].ToString();
                nrow["Range"] = dr["Range"];
                nrow["SampleType"] = dr["SampleType"];
                nrow["Status"] = dr["Status"];
                nrow["Batch"] = dr["Batch"];
                dtResult.Rows.Add(nrow);
            }
            dgvSampleData.DataSource = dtResult;
            GetDataGridColor();
            dgvSampleData.ClearSelection();
        }
        //2018-08-10 zlx add
        private void btnImPort_Click(object sender, EventArgs e)
        {
            if (dgvPatientInfo.SelectedRows.Count == 0)
            {
                frmMessageShow f = new frmMessageShow();
                f.MessageShow(Getstring("MessageHead"), Getstring("keywordText.SelectSampleMessage"));
                return;
            }
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            dialog.Filter = "xls" + Getstring("File") + "|*.xls";
            frmMessageShow fmessage = new frmMessageShow();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = dialog.FileName;
                DataTable dt = OperateExcel.ImPortExcel(filePath);
                if (dt.Rows.Count == 0)
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(Getstring("MessageHead"), Getstring("ImportNullMessage"));
                    return;
                }
                DataTable _dtData = (DataTable)dgvSampleData.DataSource;
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr[0].ToString() == "" && dr[1].ToString() == "")
                        continue;
                    try
                    {
                        double con = double.Parse(dr[4].ToString());
                    }
                    catch (Exception ee)
                    {
                        fmessage.MessageShow(Getstring("MessageHead"), Getstring("ImportErrorMessage"));
                        return;
                    }
                    Model.tbAssayResult modelAssayResult = new Model.tbAssayResult();
                    if (_dtData == null)
                    {
                        //样本编号 ,项目名称,实验时间 ,发光值,浓度 ,单位 ,结果 ,参值考
                        dgvSampleData.Rows.Add(dgvSampleData.Rows.Count, 1);
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[1].Value = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value;
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[3].Value = dr[0].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[4].Value = dr[1].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[5].Value = dr[2].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[6].Value = dr[3].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[7].Value = dr[4].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[8].Value = dr[5].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[9].Value = dr[6].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells[10].Value = dr[7].ToString();
                        dgvSampleData.Rows[dgvSampleData.Rows.Count - 1].Cells["SampleType"].Value = Getstring("OutInfo");
                    }
                    else
                    {
                        DataRow newrow = _dtData.NewRow();
                        //dgvSampleData.Rows.Add(1);
                        newrow[1] = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value;
                        newrow[2] = dr[0].ToString();
                        newrow[3] = dr[1].ToString();
                        newrow[4] = dr[2].ToString();
                        newrow[5] = dr[3].ToString();
                        newrow[6] = dr[4].ToString();
                        newrow[7] = dr[5].ToString();
                        newrow[8] = dr[6].ToString();
                        newrow[9] = dr[7].ToString();
                        newrow["SampleType"] = Getstring("OutInfo");
                        _dtData.Rows.Add(newrow);
                    }
                    modelAssayResult.SampleID = int.Parse(dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value.ToString());
                    modelAssayResult.ItemName = dr[1].ToString();
                    modelAssayResult.TestDate = Convert.ToDateTime(dr[2]);
                    modelAssayResult.PMTCounter = int.Parse(dr[3].ToString());
                    modelAssayResult.Concentration = dr[4].ToString();
                    modelAssayResult.Unit = dr[5].ToString();
                    modelAssayResult.Result = dr[6].ToString();
                    modelAssayResult.Range = dr[7].ToString();
                    modelAssayResult.Batch = "";

                    modelAssayResult.ConcSpec = "";
                    modelAssayResult.DiluteCount = 0;
                    modelAssayResult.Specification = "";
                    modelAssayResult.Upload = "";
                    modelAssayResult.Status = 9;
                    DbHelperOleDb db = new DbHelperOleDb(1);
                    bllAssayResult.Add(modelAssayResult);
                }
                if (_dtData == null)
                    dgvSampleData.DataSource = _dtData;
                fmessage.MessageShow(Getstring("MessageHead"), Getstring("ImportSucessMessage"));
            }

        }

        //Jun add 20190319 修复原本代码在一些情况下出现乱码
        private void btnExPort_Click(object sender, EventArgs e)
        {

            //Jun mod 使用NPOI解决Office版本不一致问题（或未安装Office）
            if (dgvSampleData.Rows.Count == 0)
            {
                frmMessageShow fmessage = new frmMessageShow();
                fmessage.MessageShow(Getstring("MessageHead"), Getstring("NoDataMessage"));
                return;
            }
            if (dgvSampleData.SelectedRows.Count == 0)
            {
                dgvSampleData.SelectAll();
            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "xls" + Getstring("File") + "|*.xls";
            string FileName = dialog.FileName = DateTime.Now.ToString("yyyyMMdd") + Getstring("FileName");
            IWorkbook excel = new HSSFWorkbook();//创建.xls文件
            ISheet sheet = excel.CreateSheet(FileName); //创建sheet
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                IRow row = sheet.CreateRow(0);//创建行对象，填充表头
                for (int i = 2; i < dgvSampleData.Columns.Count - 3; i++)
                {
                    row.CreateCell(i - 2).SetCellValue(dgvSampleData.Columns[i + 1].HeaderText);
                    //sheet.AutoSizeColumn(i);
                }
                //填充内容，j从1开始，屏蔽掉第一列，循环读取
                for (int i = 0; i < dgvSampleData.Rows.Count; i++)
                {
                    if (!dgvSampleData.Rows[i].Selected) continue;
                    row = sheet.CreateRow(i + 1);
                    for (int j = 3; j < dgvSampleData.Columns.Count - 2; j++)
                    {
                        //杨洁明提出不想要Excel的三角号，解决的办法就是把数字写进去喽，可是有的列不是数字，捕获一下转换异常吧 jun add 20190418
                        #region 注释掉，try、cathch导出
                        //try
                        //{
                        //    row.CreateCell(j - 3).SetCellValue(double.Parse(dgvSampleData.Rows[i].Cells[j].Value.ToString()));
                        //}
                        //catch (Exception ex)
                        //{
                        //    row.CreateCell(j - 3).SetCellValue(dgvSampleData.Rows[i].Cells[j].Value.ToString());
                        //}
                        #endregion
                        #region 导出 lyq 20190807 add 
                        if (dgvSampleData.Columns[j].HeaderText.ToString().Equals(Getstring("PMTCounter.HeaderText")))
                        {
                            row.CreateCell(j - 3).SetCellValue(double.Parse(dgvSampleData.Rows[i].Cells[j].Value.ToString()));
                        }
                        else
                        {
                            row.CreateCell(j - 3).SetCellValue(dgvSampleData.Rows[i].Cells[j].Value.ToString());
                        }
                        #endregion
                        //row.CreateCell(j - 1).SetCellValue(dgvSampleData.Rows[i].Cells[j].Value.ToString());
                        //sheet.AutoSizeColumn(j - 1);//列宽自适应
                    }
                }
            }
            //点击取消直接退出 jun add 20190328
            else
            {
                return;
            }
            try
            {
                FileStream file = new FileStream(dialog.FileName.ToString(), FileMode.Create);
                excel.Write(file);
                file.Close();
                frmMessageShow frmessage = new frmMessageShow();
                frmessage.MessageShow(Getstring("MessageHead"), Getstring("ExportSucessMessage"));
            }
            catch (Exception exc)
            {
                frmMessageShow fmessage = new frmMessageShow();
                fmessage.MessageShow(Getstring("MessageHead"), Getstring("ExportErrorMessage"));
                return;
            }
            #region 注释
            //frmMessageShow frmessage = new frmMessageShow();
            //frmessage.MessageShow("导出提示", "文件导出成功！");
            //if (dgvSampleData.SelectedRows.Count == 0)
            //    return;
            //SaveFileDialog dialog = new SaveFileDialog();
            //dialog.Filter = "xls文件|*.xls";
            //string FileName = dialog.FileName =DateTime.Now.ToString("yyyyMMdd")+"_实验结果";
            //if (dialog.ShowDialog() == DialogResult.OK)
            //{
            //    DataTable dt = new DataTable();
            //    dt = (DataTable)dgvSampleData.DataSource;
            //    if (dgvSampleData.Rows.Count == 0)
            //    {
            //        frmMessageShow f = new frmMessageShow();
            //        f.MessageShow("导出提示", "没有数据可以导出！");
            //        return;
            //    }
            //    dt.TableName = FileName;
            //    OperateExcel.ExcelTable = dt;
            //    OperateExcel.ExcelTable.TableName = FileName;
            //    string filePath = dialog.FileName;
            //    if (File.Exists(filePath))
            //        File.Delete(filePath);
            //    OperateExcel.CreatExcel(filePath);
            //    OperateExcel.ExPortToExcel(filePath);
            //    frmMessageShow fmessage = new frmMessageShow();
            //    fmessage.MessageShow("导出提示", "数据导出完成！");
            //}
            #endregion
        }

        private void dgvSampleData_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {

        }
        //2018-08-18 zlx add
        private void dgvPatientInfo_SelectionChanged(object sender, EventArgs e)
        {
            //2018-11-13 zlx mod
            string SampleID = "";
            if (dgvPatientInfo.SelectedRows.Count > 0)
                SampleID = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value.ToString();
            else
            {
                if (dgvPatientInfo.CurrentRow != null)
                    SampleID = dgvPatientInfo.CurrentRow.Cells["SampleID1"].Value.ToString();
                else
                    SampleID = "";
            }
            if (SampleID == "")
                return;
            //string SampleID = dgvPatientInfo.CurrentRow.Cells[1].Value.ToString();
            string str = "select tbAssayResult.AssayResultID,tbAssayResult.SampleID,tbSampleInfo.SampleNo,"
                      + "tbAssayResult.ItemName,tbAssayResult.TestDate,tbAssayResult.PMTCounter,"
                      + "tbAssayResult.Concentration,tbAssayResult.Unit,tbAssayResult.Result,tbAssayResult.Range,tbSampleInfo.SampleType,tbAssayResult.Status,tbAssayResult.Batch from " //2018-08-17  zlx 添加tbAssayResult.Status
                      + "tbAssayResult INNER JOIN tbSampleInfo on tbAssayResult.SampleID = tbSampleInfo.SampleID "
                      + "where tbAssayResult.SampleID=" + SampleID + " AND  tbAssayResult.TestDate >=#" + dtpStartDate.Value.ToString("yyyy-MM-dd") + "# and tbAssayResult.TestDate < #" + dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd") + "#";// AND tbAssayResult.Status=0

            DbHelperOleDb DB = new DbHelperOleDb(1);//2018-5-9 zlx add tbSampleInfo.SampleType
            DataTable dtTestData = DbHelperOleDb.Query(1, @str).Tables[0];
            DataTable dtResult = new DataTable();
            dtResult = dtTestData.Clone();
            foreach (DataColumn col in dtResult.Columns)
            {
                if (col.ColumnName == "Concentration")
                {
                    col.DataType = typeof(String);
                }
            }
            foreach (DataRow dr in dtTestData.Rows)
            {
                DataRow nrow = dtResult.NewRow();
                nrow["AssayResultID"] = dr["AssayResultID"];
                nrow["SampleID"] = dr["SampleID"];
                nrow["SampleNo"] = dr["SampleNo"];
                nrow["ItemName"] = dr["ItemName"];
                nrow["TestDate"] = dr["TestDate"];
                nrow["PMTCounter"] = dr["PMTCounter"];
                nrow["Unit"] = dr["Unit"];
                nrow["Result"] = dr["Result"];
                nrow["Concentration"] = dr["Concentration"].ToString();
                nrow["Range"] = dr["Range"];
                nrow["SampleType"] = dr["SampleType"];
                nrow["Status"] = dr["Status"];
                nrow["Batch"] = dr["Batch"];
                dtResult.Rows.Add(nrow);
            }
            dgvSampleData.DataSource = dtResult;
            //2018-11-13 zlx mod
            GetDataGridColor();
            dgvSampleData.ClearSelection();
            //2018-11-08 zlx add
            //foreach (DataGridViewRow dgv in dgvSampleData.Rows)
            //{
            //    double concentration = double.Parse(dgv.Cells["Concentration"].Value.ToString());
            //    DbHelperOleDb db = new DbHelperOleDb(0);
            //    DataTable tbtbProject = DbHelperOleDb.Query(0, @"select RangeType,ValueRange1,MinValue,MaxValue from tbProject where ShortName = '" + dgv.Cells["ItemName"].Value + "'").Tables[0];
            //    //2018-11-26 zlx mod
            //    if (tbtbProject.Rows[0][0].ToString() != "")
            //    {
            //        if (concentration == double.Parse(tbtbProject.Rows[0][2].ToString()) && dgv.Cells["Result"].Value.ToString().Contains(Getstring("NotRangeMessage")))
            //            dgv.Cells["Concentration"].Value = "<" + concentration;
            //        else if (concentration == double.Parse(tbtbProject.Rows[0][3].ToString()) && dgv.Cells["Result"].Value.ToString().Contains(Getstring("NotRangeMessage")))
            //            dgv.Cells["Concentration"].Value = ">" + concentration;
            //        else
            //            dgv.Cells["Concentration"].Value = concentration.ToString("F" + int.Parse(tbtbProject.Rows[0][0].ToString()) + "");
            //    }
            //    else
            //        dgv.Cells["Concentration"].Value = concentration.ToString("F0");

            //}

        }
        //2018-12-07 zlx mod
        private void dgvSampleData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvSampleData.CurrentRow == null || dgvSampleData.CurrentRow.Cells["Status"].Value.ToString() == "" || Convert.ToInt32(dgvSampleData.CurrentRow.Cells["Status"].Value) == 9)
                    return;//Result
            }
            catch (Exception ex) { return; }
            if (!Regex.IsMatch(dgvSampleData.CurrentRow.Cells["Concentration"].Value.ToString(), @"^\d+\.?\d*$"))
            {
                return;
            }

            if (dgvSampleData.CurrentCell.Value == null) return;

            if (dgvSampleData.CurrentCell.Value.ToString() == "")
                dgvSampleData.CurrentCell.Value = 0;
            dgvSampleData.CurrentRow.Cells["Result"].ReadOnly = false;
            if (double.Parse(dgvSampleData.CurrentRow.Cells["Concentration"].Value.ToString()) > 0)
                dgvSampleData.CurrentRow.Cells["Result"].Value = GetResult(dgvSampleData.CurrentRow.Cells["AssayResultID"].Value.ToString(), dgvSampleData.CurrentRow.Cells["Concentration"].Value.ToString());
            dgvSampleData.CurrentRow.Cells["Result"].ReadOnly = true;
            DbHelperOleDb db = new DbHelperOleDb(1);
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update tbAssayResult set ");
            strSql.Append("PMTCounter=@PMTCounter,");
            strSql.Append("Concentration=@Concentration,");
            strSql.Append("Result=@Result");
            strSql.Append(" where AssayResultID=@AssayResultID");
            OleDbParameter[] parameters = {
                    new OleDbParameter("@PMTCounter", OleDbType.Integer,4),
                    new OleDbParameter("@Concentration", OleDbType.VarChar,20),
                    new OleDbParameter("@Result", OleDbType.VarChar,20),
                    new OleDbParameter("@AssayResultID", OleDbType.Integer,4)};
            parameters[0].Value = dgvSampleData.CurrentRow.Cells["PMTCounter"].Value;
            parameters[1].Value = dgvSampleData.CurrentRow.Cells["Concentration"].Value;
            parameters[2].Value = dgvSampleData.CurrentRow.Cells["Result"].Value;
            parameters[3].Value = dgvSampleData.CurrentRow.Cells["AssayResultID"].Value;
            DbHelperOleDb.ExecuteSql(1, strSql.ToString(), parameters);
        }

        private void dgvSampleData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvSampleData.SelectedRows.Count == 0)
                return;
            if (!Regex.IsMatch(dgvSampleData.CurrentRow.Cells["Concentration"].Value.ToString(), @"^\d+\.?\d*$"))
            {
                return;
            }
            DbHelperOleDb db = new DbHelperOleDb(1);
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update tbAssayResult set ");
            strSql.Append("PMTCounter=@PMTCounter,");
            strSql.Append("Concentration=@Concentration,");
            strSql.Append("Result=@Result");
            strSql.Append(" where AssayResultID=@AssayResultID");
            OleDbParameter[] parameters = {
                    new OleDbParameter("@PMTCounter", OleDbType.Integer,4),
                    new OleDbParameter("@Concentration", OleDbType.VarChar,20),
                     new OleDbParameter("@Result", OleDbType.VarChar,20),
                    new OleDbParameter("@AssayResultID", OleDbType.Integer,4)};
            parameters[0].Value = dgvSampleData.CurrentRow.Cells["PMTCounter"].Value;
            parameters[1].Value = dgvSampleData.CurrentRow.Cells["Concentration"].Value;
            parameters[2].Value = dgvSampleData.CurrentRow.Cells["Result"].Value;
            parameters[3].Value = dgvSampleData.CurrentRow.Cells["AssayResultID"].Value;
            DbHelperOleDb.ExecuteSql(1, strSql.ToString(), parameters);
        }
        //2018-10-07 zlx add
        void control_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (dgvSampleData.CurrentCell.ColumnIndex == 6 || dgvSampleData.CurrentCell.ColumnIndex == 7)
            {
                //限制只能输入1-9的数字和退格键
                if (((int)e.KeyChar >= 48 && (int)e.KeyChar <= 57) || e.KeyChar == 8)
                {
                    if (control.Text.ToString().Length == 9 && e.KeyChar != 8)
                    {
                        e.Handled = true;
                        return;
                    }
                    e.Handled = false;
                }
                else
                {
                    if (e.KeyChar == 46)
                    {
                        if (dgvSampleData.CurrentCell.ColumnIndex == 7 && !control.Text.ToString().Contains("."))
                        {
                            e.Handled = false;
                        }
                        else
                        {
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        TextBox control;
        bool _bevent = false;
        private void dgvSampleData_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            control = (TextBox)e.Control;
            control.KeyPress += new KeyPressEventHandler(control_KeyPress);

        }

        private void fbtnTestAgain_Click(object sender, EventArgs e)
        {
            bool _boolAgain = false;
            if (dgvSampleData.SelectedRows.Count == 0)
            {
                frmMsgShow.MessageShow(Getstring("MessageHead"), Getstring("ReTestMessage"));
                return;
            }
            if (dgvSampleData.SelectedRows.Count == 0) return;
            DbHelperOleDb db = new DbHelperOleDb(1);
            DataTable _dtSP = bllsp.GetList("SampleID=" + dgvSampleData.SelectedRows[0].Cells["SampleID"].Value + "").Tables[0];
            if (_dtSP.Rows.Count > 0)
            {
                if (Convert.ToInt32(_dtSP.Rows[0]["Status"]) == 1)
                {
                    for (int i = 0; i < dtSpInfo.Rows.Count; i++)
                    {
                        if (dtSpInfo.Rows[i]["SampleNo"].ToString().Equals(_dtSP.Rows[0]["SampleNo"].ToString()))
                        {
                            if (DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Status = 0  where SampleID=" + dgvSampleData.SelectedRows[0].Cells["SampleID"].Value + "") > 0)
                            {
                                dtSpInfo.Rows[i]["Status"] = 0;
                                _dtSP.Rows[0]["Status"] = 0;
                            }
                        }
                    }
                }
                else if (Convert.ToInt32(_dtSP.Rows[0]["Status"]) == 2)
                {
                    frmMsgShow.MessageShow(Getstring("MessageHead"), Getstring("SampleUnStall"));
                }
                if (Convert.ToInt32(_dtSP.Rows[0]["Status"]) == 0)
                    _boolAgain = true;
            }
            int bUpdate = 0;
            if (_boolAgain)
                bUpdate = DbHelperOleDb.ExecuteSql(1, @"update tbAssayResult set Status = -1  where SampleID=" + dgvSampleData.SelectedRows[0].Cells["SampleID"].Value + " AND ItemName='" + dgvSampleData.SelectedRows[0].Cells["ItemName"].Value + "'AND PMTCounter=" + dgvSampleData.SelectedRows[0].Cells["PMTCounter"].Value + "");
            if (bUpdate > 0)
            {
                frmMsgShow.MessageShow(Getstring("MessageHead"), Getstring("ReTestSetSucess"));
            }
        }
        private string Getstring(string key)
        {
            ResourceManager resManagerA =
                    new ResourceManager("BioBaseCLIA.DataQuery.frmResultQuery", typeof(frmResultQuery).Assembly);
            return resManagerA.GetString(key);
        }

        private void fbtnAddSampleResult_Click(object sender, EventArgs e)
        {
            if (dgvPatientInfo.SelectedRows.Count < 1)
            {
                frmMessageShow msg = new frmMessageShow();
                msg.MessageShow(Getstring("Tip"), Getstring("AddDataAfterSelect"));
                return;
            }
            string sampleNum = dgvPatientInfo.SelectedRows[0].Cells["SampleNo1"].Value.ToString();//样本编号
            string sampleId = dgvPatientInfo.SelectedRows[0].Cells["SampleID1"].Value.ToString();//SampleID
            frmAddSampleResult f = new frmAddSampleResult(sampleNum, sampleId);
            f.ShowDialog();
            fbtnQuery_Click(null, null);
            foreach (DataGridViewRow dgr in dgvPatientInfo.Rows)
            {
                if (dgr.Cells["SampleID1"].Value.ToString().Contains(sampleId))
                {
                    dgr.Selected = true;
                    return;
                }
                dgr.Selected = false;
            }
        }
        private string getString(string key)
        {
            ResourceManager resManager = new ResourceManager(typeof(frmWorkList));
            return resManager.GetString(key);
        }
    }
}
