﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Maticsoft.DBUtility;
using Common;

namespace BioBaseCLIA.InfoSetting
{
    public partial class frmInfo : frmParent
    {
        #region 变量
        #region 项目信息
        private DataTable dtItemInfo;//项目信息存储表
        private DataTable dtStep = new DataTable();//项目步骤表
        private int num = 0;
        private DataTable dtStdConc = new DataTable();//定标浓度表
        DataTable dtGetShortName = new DataTable();//项目缩写名表
        private string filePath = "";
        private Common.ProjectXml proInfoXml = new Common.ProjectXml();

        private BLL.tbProject bllProject = new BLL.tbProject();
        private Model.tbProject mProject = new Model.tbProject();

        frmMessageShow frmMsgShow = new frmMessageShow();
        #endregion

        #region 基础信息
        BLL.tbProject bllPj = new BLL.tbProject();
        BLL.tbReagent bllRg = new BLL.tbReagent();
        BLL.tbProjectGroup bllPG = new BLL.tbProjectGroup();
        BLL.tbDepartment bllDep = new BLL.tbDepartment();
        BLL.tbDoctor bllDoctor = new BLL.tbDoctor();
        DataTable dtGroupItem = new DataTable();//组合项目信息
        DataTable dtDepInfo = new DataTable();//科室信息
        DataTable dtDoctorInfo = new DataTable();//医生信息
        DataTable dtItemInfo1 = new DataTable();//项目信息列表
        Model.tbProjectGroup modelPG = new Model.tbProjectGroup();
        Model.tbDepartment modelDep = new Model.tbDepartment();
        Model.tbDoctor modelDoc = new Model.tbDoctor();
        #endregion
        #endregion
        public frmInfo()
        {
            InitializeComponent();
            #region 项目信息
            dtStep.Columns.Add("NO");
            dtStep.Columns.Add("StepName");
            dtStep.Columns.Add("Para");
            dtStep.Columns.Add("Unit");

            dtStdConc.Columns.Add("NO");
            dtStdConc.Columns.Add("StdName");
            dtStdConc.Columns.Add("StdConc");
            #endregion

        }

        /// <summary>
        /// 根据配置信息，决定是否隐藏部分内容
        /// </summary>
        private void LimitOfLook()
        {
            string token = OperateIniFile.ReadInIPara("LimitOfLook", "token");
            if (token == "1415926")
            {
                return;
            }
            else
            {
                groupBox2.Visible = false;
                groupBox2.Enabled = false;
                groupBox3.Visible = false;
                groupBox3.Enabled = false;

                int widAdd = (int)(tabPPro.Size.Width - dgvItemList.Size.Width - 34 - groupBox1.Size.Width) / 7;
                int heightAdd = (int)(tabPPro.Size.Height - 34 - groupBox1.Size.Height) / 5;
                groupBox1.Size = new Size(tabPPro.Size.Width - dgvItemList.Size.Width - 34, tabPPro.Size.Height - 34);

                label2.Location = new Point(label2.Location.X + widAdd, label2.Location.Y + heightAdd);
                label3.Location = new Point(label3.Location.X + widAdd * 2, label3.Location.Y + heightAdd);
                label4.Location = new Point(label4.Location.X + widAdd, label4.Location.Y + heightAdd * 2);
                label5.Location = new Point(label5.Location.X + widAdd, label5.Location.Y + heightAdd * 3);
                //label6.Location = new Point(label6.Location.X + widAdd * 2, label6.Location.Y + heightAdd * 2);
                label7.Location = new Point(label7.Location.X + widAdd * 3, label7.Location.Y + heightAdd * 2);
                label8.Location = new Point(label8.Location.X + widAdd * 2, label8.Location.Y + heightAdd * 3);
                label9.Location = new Point(label9.Location.X + widAdd * 3, label9.Location.Y + heightAdd);

                txtItemShortName.Location = new Point(txtItemShortName.Location.X + widAdd, txtItemShortName.Location.Y + heightAdd);
                txtItemFullName.Location = new Point(txtItemFullName.Location.X + widAdd * 2, txtItemFullName.Location.Y + heightAdd);
                cmbItemType.Location = new Point(cmbItemType.Location.X + widAdd * 3, cmbItemType.Location.Y + heightAdd);
                txtDilutionFactor.Location = new Point(txtDilutionFactor.Location.X + widAdd, txtDilutionFactor.Location.Y + heightAdd * 2);
                //txtDilutionName.Location = new Point(txtDilutionName.Location.X + widAdd * 2, txtDilutionName.Location.Y + heightAdd * 2);2018-07-28 zlx mod
                txtValueUnit.Location = new Point(txtValueUnit.Location.X + widAdd * 3, txtValueUnit.Location.Y + heightAdd * 2);
                txtValueRange1.Location = new Point(txtValueRange1.Location.X + widAdd, txtValueRange1.Location.Y + heightAdd * 3);
                txtValueRange2.Location = new Point(txtValueRange2.Location.X + widAdd * 2, txtValueRange2.Location.Y + heightAdd * 3);
                chkItemUseOrNot.Location = new Point(chkItemUseOrNot.Location.X + widAdd * 2, chkItemUseOrNot.Location.Y + heightAdd * 4);
            }
        }

        private void frmInfo_Load(object sender, EventArgs e)
        {
            LimitOfLook();
            #region 项目信息
            DbHelperOleDb db = new DbHelperOleDb(0);
            dtItemInfo = bllProject.GetAllList().Tables[0];
            dtGetShortName = GetItemShortName(dtItemInfo);
            dgvItemList.RowHeadersVisible = false;//去掉列表左侧的黑三角显示

            //与下面的+=组合使用，原因：当datagridview赋值时内部会触发选中事件，故此处先注销掉，再在后面注册上。
            dgvItemList.SelectionChanged -= dgvItemList_SelectionChanged;
            dgvItemList.DataSource = dtGetShortName;
            dgvItemList.Columns[0].Width = 40;
            dgvItemList.SelectionChanged += dgvItemList_SelectionChanged;
            #endregion

            #region 基础信息

            GetItemInfo();
            if (dtItemInfo1 != null)
            {
                for (int i = 0; i < dtItemInfo1.Rows.Count; i++)
                {
                    CheckBox box = new CheckBox();
                    box.AutoSize = true;
                    box.Text = dtItemInfo1.Rows[i]["ShortName"].ToString();
                    flpItemName.Controls.Add(box);
                }
            }
            SetGroupItem();
            txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = false;
            SetDoctorInfo();
            SetDepInfo();

            #endregion

            #region 打印设置
            ShowReportSet();
            ShowReportSort();
            #endregion

            if (dgvDoctor.Rows.Count == 0)
            {
                btnModifyDoc.Enabled = false;
                btnDelDoc.Enabled = false;
            }
            if (lbDep.Items.Count == 0)//y add 20180420
            {
                btnModifyDep.Enabled = false;
                btnDelDep.Enabled = false;
            }
            if (lbGroupItem.Items.Count == 0)//y add 20180420
            {
                btnModify.Enabled = false;
                btnDelete.Enabled = false;
            }
        }
        #region 项目信息

        private void btnModifyStep_Click(object sender, EventArgs e)
        {
            //2018-07-28 zlx mod
            //dgvItemStep.SelectedRows[0].Cells[2].ReadOnly = true;//将当前单元格设为可读
            //dgvItemStep.CurrentCell = dgvItemStep.SelectedRows[0].Cells[2];//获取当前单元格
            //dgvItemStep.BeginEdit(true);//将单元格设为编辑状态
        }


        private void btnSaveItem_Click(object sender, EventArgs e)
        {
            DbHelperOleDb db = new DbHelperOleDb(0);
            int selectId = dgvItemList.SelectedRows[0].Index; ;
            GetModelProject();
            if (bllProject.Update(mProject))
            {
                dtItemInfo = bllProject.GetAllList().Tables[0];
                dtGetShortName = GetItemShortName(dtItemInfo);
                dgvItemList.SelectionChanged -= dgvItemList_SelectionChanged;
                dgvItemList.ClearSelection();  //去除默认选中第一行
                dgvItemList.DataSource = dtGetShortName;
                dgvItemList.Rows[0].Selected = false;
                dgvItemList.Rows[selectId].Selected = true;
                dgvItemList.SelectionChanged += dgvItemList_SelectionChanged;
                frmMsgShow.MessageShow("项目管理", "项目信息保存成功！");
            }
        }

        private void btnLoadItem_Click(object sender, EventArgs e)
        {
            NetCom3.Instance.stopsendFlag = true;
            DbHelperOleDb db = new DbHelperOleDb(0);
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            dialog.Filter = "xml文件|*.xml";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                filePath = dialog.FileName;
                proInfoXml = Common.ProjectXml.GetProjectInfo(filePath);
                #region 是否使用实验预设功能 2019-02-26 zlx add
                string PredictSet=OperateIniFile.ReadInIPara("TestResultSet", "PredictSet");
                if (PredictSet == "1")
                {
                    Run.XmlOperation.GetXmlFile();
                    Run.XmlOperation.InsertExperiment(proInfoXml.ShortName);
                    Run.XmlOperation.EndXmlFile();
                }
                #endregion
                XmlToModel(proInfoXml, mProject);
                if (bllProject.Add(mProject))
                {
                    dtItemInfo = bllProject.GetAllList().Tables[0];
                    dtGetShortName = GetItemShortName(dtItemInfo);
                    dgvItemList.RowHeadersVisible = false;//去掉列表左侧的黑三角显示

                    //与下面的+=组合使用，原因：当datagridview赋值时内部会触发选中事件，故此处先注销掉，再在后面注册上。
                    dgvItemList.SelectionChanged -= dgvItemList_SelectionChanged;
                    dgvItemList.DataSource = dtGetShortName;
                    dgvItemList.Columns[0].Width = 40;
                    dgvItemList.SelectionChanged += dgvItemList_SelectionChanged;
                    frmMsgShow.MessageShow("项目管理", "导入成功！");
                    ShowReportSort();
                }
                else
                {
                    frmMsgShow.MessageShow("项目管理", "导入格式不正确！");
                }
                if (dgvItemList.RowCount > 0)
                {
                    dgvItemList_SelectionChanged(sender,e);
                }
            }
            NetCom3.Instance.stopsendFlag = false;
        }
        /// <summary>
        /// 获取项目列表名称
        /// </summary>
        /// <param name="dtAllList">项目全列表</param>
        /// <returns></returns>
        public DataTable GetItemShortName(DataTable dtAllList)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemShortName", typeof(string));
            for (int i = 0; i < dtAllList.Rows.Count; i++)
            {
                dt.Rows.Add(dtAllList.Rows[i]["ProjectID"], dtAllList.Rows[i]["ShortName"]);
            }
            return dt;

        }
        private void XmlToModel(Common.ProjectXml xp, Model.tbProject mp)
        {
            mp.ProjectNumber = xp.ProjectNumber;
            mp.ProjectProcedure = xp.ProjectProcedure;
            mp.ProjectType = xp.ProjectType;
            mp.QCPointNumber = xp.QCPointNumber;
            mp.QCPoints = xp.QCPoints;
            mp.RangeType = xp.RangeType;
            mp.ShortName = xp.ShortName;
            mp.ValueRange1 = xp.ValueRange1;
            mp.ValueRange2 = xp.ValueRange2;
            mp.ValueUnit = xp.ValueUnit;
            mp.MinValue = xp.MinValue;
            mp.MaxValue = xp.MaxValue;
            mp.LoadType = xp.LoadType;
            mp.FullName = xp.FullName;
            mp.DiluteCount = xp.DiluteCount;
            mp.CalPointNumber = xp.CalPointNumber;
            mp.CalPointConc = xp.CalPointConc;
            mp.CalMode = xp.CalMode;
            mp.CalMethod = xp.CalMethod;
            mp.CalculateMethod = xp.CalculateMethod;
            mp.ActiveStatus = xp.ActiveStatus;
            // 稀释液名称，LYN add 20171114
            mp.DiluteName = xp.DiluteName;
            mp.ExpiryDate = xp.ExpiryDate;//2018-08-07 zlx add
            mp.NoUsePro = xp.NoUsePro;//2018-10-13 zlx add
            mp.VRangeType = xp.VRangeType;
        }
        private void dgvItemList_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvItemList.SelectedRows.Count > 0)
            {
                ShowItemAllValue(int.Parse(dgvItemList.SelectedRows[0].Cells[0].Value.ToString()));
            }
        }
        /// <summary>
        /// 将选中项目的所有信息显示在界面上
        /// </summary>
        /// <param name="selectedID">被选中项目的ID</param>
        private void ShowItemAllValue(int selectedID)
        {
            if (dtItemInfo.Rows.Count < 1) return;
            DataRow[] dr = dtItemInfo.Select("ProjectID=" + selectedID);
            if (dr.Length > 0)
            {
                txtItemShortName.Text = dr[0]["ShortName"].ToString();
                txtItemFullName.Text = dr[0]["FullName"].ToString();
                txtDilutionFactor.Text = dr[0]["DiluteCount"].ToString();
                cmbItemType.SelectedIndex = int.Parse(dr[0]["ProjectType"].ToString());
                txtValueRange1.Text = dr[0]["ValueRange1"].ToString();
                txtValueRange2.Text = dr[0]["ValueRange2"].ToString();
                txtValueUnit.Text = dr[0]["ValueUnit"].ToString();
                //2018-07-30 zlx add
                try
                {
                    numExpiryDate.Value = Convert.ToDecimal(dr[0]["ExpiryDate"]);
                }
                catch (Exception e)
                {
                    numExpiryDate.Value = 0;
                }
                //2018-11-05 zlx add
                try
                {
                    NumResult.Value = Convert.ToDecimal(dr[0]["RangeType"]);
                }
                catch (Exception e)
                {
                    NumResult.Value = 0;
                }
                //显示稀释液名称，界面和数据库已添加该字段。LYN add 20171114
                //txtDilutionName.Text = dr[0]["DiluteName"].ToString(); //2018-07-28 zlx mod
                if (dr[0]["ActiveStatus"].ToString().Trim() == "1")
                {
                    chkItemUseOrNot.Checked = true;
                }
                dtStep.Rows.Clear();
                //2018-11-05 zlx mod
                string[] allStep = (dr[0]["ProjectProcedure"].ToString()).Split(';');
                for (int i = 0; i < allStep.Length; i++)
                {
                    if (allStep[i] == "")
                        continue;
                    string[] singStep = allStep[i].Split('-');
                    dtStep.Rows.Add((i + 1).ToString(), GetStepName(singStep[0]), singStep[1], singStep[2]);
                }
                //string[] allStep = (dr[0]["ProjectProcedure"].ToString()).Split('-', ';');
                //for (int i = 0; i < allStep.Length; i += 3)
                //{
                //    num++;
                //    dtStep.Rows.Add(num.ToString(), GetStepName(allStep[i]), allStep[i + 1], allStep[i + 2]);
                //}
                dgvItemStep.DataSource = dtStep;
                num = 0;
                Array.Clear(allStep, 0, allStep.Length);

                dtStdConc.Rows.Clear();
                string[] allConc = (dr[0]["CalPointConc"].ToString()).Split(',');
                for (int j = 0; j < allConc.Length; j++)
                {
                    dtStdConc.Rows.Add(j + 1, "S" + (j + 1), allConc[j]);
                }
                dgvItemStd.DataSource = dtStdConc;
                Array.Clear(allConc, 0, allConc.Length);

            }

        }
        /// <summary>
        /// 获取各个步骤缩写对应的实际步骤名称
        /// </summary>
        /// <param name="flagName">步骤缩写</param>
        /// <returns></returns>
        private string GetStepName(string flagName)
        {
            //S-30-ml;R1-30-ml;R2-30-ml;H-15-min;B-30-ml;H-5-min;W-300-ml;T-20-ml;D-10-s
            if (flagName == "S")
            {
                return "加样";
            }
            if (flagName == "R1")
            {
                return "试剂1";
            }
            if (flagName == "R2")
            {
                return "试剂2";
            }
            if (flagName == "R3")
            {
                return "试剂3";
            }
            if (flagName == "H")
            {
                return "孵育";
            }
            if (flagName == "B")
            {
                return "加磁珠";
            }
            if (flagName.Contains("W"))
            {
                return "清洗";
            }
            if (flagName == "T")
            {
                return "加底物";
            }
            if (flagName == "D")
            {
                return "读数";
            }
            return "自定义";

        }
        /// <summary>
        /// 将界面上所有的参数转换成Model
        /// </summary>
        private void GetModelProject()
        {
            string strPara = "";
            mProject.ProjectID = int.Parse(dgvItemList.SelectedRows[0].Cells[0].Value.ToString());
            DataRow[] dr = dtItemInfo.Select("ProjectID=" + mProject.ProjectID);

            mProject.ProjectNumber = dr[0]["ProjectNumber"].ToString();
            try
            {
                mProject.QCPointNumber = int.Parse(dr[0]["QCPointNumber"].ToString());
            }
            catch { mProject.QCPointNumber = 0; }
            mProject.QCPoints = dr[0]["QCPoints"].ToString();
            try
            {
                mProject.LoadType = int.Parse(dr[0]["LoadType"].ToString());
            }
            catch { mProject.LoadType = 0; }
            try
            {
                mProject.CalMode = int.Parse(dr[0]["CalMode"].ToString());
            }
            catch { mProject.LoadType = 0; }
            try
            {
                mProject.CalMethod = int.Parse(dr[0]["CalMethod"].ToString());
            }
            catch { mProject.LoadType = 0; }
            mProject.CalculateMethod = dr[0]["CalculateMethod"].ToString();
            mProject.RangeType = dr[0]["RangeType"].ToString();
            try
            {
                mProject.MinValue = double.Parse(dr[0]["MinValue"].ToString());
            }
            catch { mProject.LoadType = 0; }
            try
            {
                mProject.MaxValue = double.Parse(dr[0]["MaxValue"].ToString());
            }
            catch { mProject.LoadType = 0; }
            mProject.ShortName = txtItemShortName.Text.Trim();
            #region 是否使用实验预设功能 2019-02-26 zlx add
            string PredictSet = OperateIniFile.ReadInIPara("TestResultSet", "PredictSet");
            if (PredictSet == "1")
            {
                Run.XmlOperation.GetXmlFile();
                Run.XmlOperation.InsertExperiment(proInfoXml.ShortName);
                Run.XmlOperation.EndXmlFile();
            }
            #endregion
            mProject.FullName = txtItemFullName.Text.Trim();
            mProject.ProjectType = cmbItemType.SelectedIndex.ToString();
            try
            {
                mProject.DiluteCount = int.Parse(txtDilutionFactor.Text.Trim());
            } 
            catch { mProject.LoadType = 0; }
            mProject.ValueRange1 = txtValueRange1.Text.Trim();
            mProject.ValueRange2 = txtValueRange2.Text.Trim();
            mProject.ValueUnit = txtValueUnit.Text.Trim();
            mProject.CalPointNumber = dgvItemStd.RowCount;
            mProject.ExpiryDate = Convert.ToInt32(numExpiryDate.Value); //2018-07-30 zlx mod
            mProject.RangeType = NumResult.Value.ToString();//2018-11-05 zlx mod
            // 稀释液名称，LYN add 20171114
            //mProject.DiluteName = txtDilutionName.Text.Trim(); 2018-07-28 zlx mod
            for (int i = 0; i < dgvItemStd.RowCount; i++)
            {
                if (i != dgvItemStd.RowCount - 1)
                {
                    strPara += dgvItemStd.Rows[i].Cells[2].Value.ToString() + ",";
                }
                else
                {
                    strPara += dgvItemStd.Rows[i].Cells[2].Value.ToString();
                }
            }
            mProject.CalPointConc = strPara;
            mProject.ActiveStatus = chkItemUseOrNot.Checked ? 1 : 0;
            strPara = "";
            for (int j = 0; j < dgvItemStep.RowCount; j++)
            {
                strPara += SetStepName(dgvItemStep.Rows[j].Cells[1].Value.ToString())
                    + "-" + dgvItemStep.Rows[j].Cells[2].Value.ToString()
                    + "-" + dgvItemStep.Rows[j].Cells[3].Value.ToString();
                if (j != dgvItemStep.RowCount - 1)
                {
                    strPara += ";";
                }
            }
            mProject.ProjectProcedure = strPara;

        }
        /// <summary>
        /// 将各个步骤名转换为步骤缩写
        /// </summary>
        /// <param name="SName">步骤实际名称</param>
        /// <returns></returns>
        private string SetStepName(string SName)
        {
            if (SName == "加样")
            {
                return "S";
            }
            if (SName == "试剂1")
            {
                return "R1";
            }
            if (SName == "试剂2")
            {
                return "R2";
            }
            if (SName == "孵育")
            {
                return "H";
            }
            if (SName == "加磁珠")
            {
                return "B";
            }
            if (SName == "清洗")
            {
                return "W";
            }
            if (SName == "加底物")
            {
                return "T";
            }
            if (SName == "读数")
            {
                return "D";
            }
            return "E";

        }
        private void dgvItemStep_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //2018-07-30 
            //dgvItemStep.Rows[e.RowIndex].Cells[2].ReadOnly = false;//将当前单元格设为可读
            dgvItemStep.CurrentCell = dgvItemStep.Rows[e.RowIndex].Cells[2];//获取当前单元格
            dgvItemStep.BeginEdit(true);//将单元格设为编辑状态
        }
        private void btnInstrumentPara_Click(object sender, EventArgs e)
        {
            if (!CheckFormIsOpen("frmInstrumentPara"))
            {
                frmInstrumentPara frmIP = new frmInstrumentPara();
                //this.TopLevel = false;
                frmIP.TopLevel = false;
                frmIP.Parent = this.Parent;
                frmIP.Show();
            }
            else
            {
                frmInstrumentPara frmIP = (frmInstrumentPara)Application.OpenForms["frmInstrumentPara"];
                //frmIM.Activate();
                frmIP.BringToFront();

            }
        }
        #endregion

        #region 基础信息
        private void GetItemInfo()
        {

            dtItemInfo1 = null;
            DbHelperOleDb db = new DbHelperOleDb(0);
            DataTable dtProject = bllPj.GetList("ActiveStatus=1").Tables[0];
            db = new DbHelperOleDb(3);
            DataTable dtRgItem = bllRg.GetList("Status='正常'").Tables[0];
            dtRgItem = Distinct(dtRgItem, "ReagentName");
            dtItemInfo1 = dtProject.Clone();
            for (int i = 0; i < dtRgItem.Rows.Count; i++)
            {
                var dr = dtProject.Select("ShortName='" + dtRgItem.Rows[i]["ReagentName"].ToString() + "'");
                if (dr.Length > 0)
                {
                    dtItemInfo1.Rows.Add(dr[0].ItemArray);
                }
            }
            db = new DbHelperOleDb(0);
            dtGroupItem = bllPG.GetAllList().Tables[0];
        }
        /// <summary>
        /// 去除表中的重复元素
        /// </summary>
        /// <param name="dt">需要进行设置的表</param>
        /// <param name="filedNames">保留的列名</param>
        /// <returns></returns>
        public static DataTable Distinct(DataTable dt, string filedNames)
        {
            DataView dv = dt.DefaultView;
            DataTable DistTable = dv.ToTable("Dist", true, filedNames);
            return DistTable;
        }
        private void SetGroupItem()
        {
            DbHelperOleDb db = new DbHelperOleDb(0);
            dtGroupItem = bllPG.GetAllList().Tables[0];
            if (dtGroupItem.Rows.Count >= 0)//y this line modify 20180420
            {
                lbGroupItem.Items.Clear();
                for (int i = 0; i < dtGroupItem.Rows.Count; i++)
                {
                    lbGroupItem.Items.Add(dtGroupItem.Rows[i]["ProjectGroupNumber"]);
                }
            }
        }
        private void SetDoctorInfo()
        {
            DbHelperOleDb db = new DbHelperOleDb(2);
            dtDoctorInfo = DbHelperOleDb.Query(@"Select tbDoctor.DoctorID,tbDoctor.DoctorName,tbDepartment.DepartmentName from tbDoctor,tbDepartment where tbDoctor.DepartmentID=tbDepartment.DepartmentID").Tables[0];
            dgvDoctor.DataSource = dtDoctorInfo;
        }
        private void SetDepInfo()
        {
            lbDep.Items.Clear();
            cmbDep.Items.Clear();
            DbHelperOleDb db = new DbHelperOleDb(2);
            dtDepInfo = bllDep.GetAllList().Tables[0];
            if (dtDepInfo.Rows.Count > 0)
            {
                lbDep.Items.Clear();
                for (int i = 0; i < dtDepInfo.Rows.Count; i++)
                {
                    lbDep.Items.Add(dtDepInfo.Rows[i]["DepartmentName"]);
                    cmbDep.Items.Add(dtDepInfo.Rows[i]["DepartmentName"]);
                }
            }
        }
        private void btnAddDep_Click(object sender, EventArgs e)
        {
            if (btnAddDep.Text == "添加")
            {
                btnAddDep.Text = "取消";
                txtDepName.Text = "";
                btnModifyDep.Enabled = btnDelDep.Enabled = false;
                txtDepName.Enabled = btnSaveDep.Enabled = true;
            }
            else
            {
                btnAddDep.Text = "添加";
                if (lbDep.Items.Count != 0)//y add 20180510
                    btnModifyDep.Enabled = btnDelDep.Enabled = true;
                txtDepName.Enabled = btnSaveDep.Enabled = false;
                txtDepName.Text = "";
            }
        }

        private void btnModifyDep_Click(object sender, EventArgs e)
        {
            if (lbDep.SelectedItem == null) txtDepName.Text = "";
            if (btnModifyDep.Text == "修改")
            {
                btnModifyDep.Text = "取消";
                btnAddDep.Enabled = btnDelDep.Enabled = false;
                txtDepName.Enabled = btnSaveDep.Enabled = true;
            }
            else
            {
                btnModifyDep.Text = "修改";
                btnAddDep.Enabled = btnDelDep.Enabled = true;
                txtDepName.Enabled = btnSaveDep.Enabled = false;
                if (lbDep.SelectedItems.Count > 0)//y add 20180510
                    txtDepName.Text = Convert.ToString(lbDep.SelectedItems[0]);//y add 20180510
                else//y add 20180510
                    txtDepName.Text = "";//y add 20180510
            }
        }

        private void btnSaveDep_Click(object sender, EventArgs e)
        {
            if (!Inspect.NameOnlycharacter2(txtDepName.Text.Trim()))//y modify 20180419
            {
                frmMsgShow.MessageShow("基础信息设置", "科室名称只能由二到十个汉字、字母、数字、星号、\".\"、下划线组成！");
                txtDepName.Focus();
                return;
            }

            DbHelperOleDb db = new DbHelperOleDb(2);
            if (btnAddDep.Text == "取消")
            {
                foreach (var a in lbDep.Items)//查重
                {
                    if (a.ToString() == txtDepName.Text)
                    {
                        frmMsgShow.MessageShow("基础信息设置", "科室信息不能重名");
                        return;
                    }
                }
                modelDep.DepartmentName = txtDepName.Text.Trim();
                modelDep.Remark = "001";
                if (bllDep.Add(modelDep))
                {
                    SetDepInfo();
                    txtDepName.Text = "";
                    btnAddDep.Text = "添加";
                    btnAddDep.Enabled = btnModifyDep.Enabled = btnDelDep.Enabled = true;
                    txtDepName.Enabled = btnSaveDep.Enabled = false;
                    frmMsgShow.MessageShow("基础信息设置", "科室信息添加成功！");
                }
                if (lbDep.Items.Count != 0)//y add 20180420
                {
                    btnModifyDep.Enabled = true;
                    btnDelDep.Enabled = true;
                }
            }
            else
            {
                if (lbDep.SelectedItem == null)
                {
                    frmMsgShow.MessageShow("基础信息设置", "请选中修改项");
                    return;
                }
                for (int i = 0; i < lbDep.Items.Count;i++ )//y ths.block add 20180420
                {
                    if (lbDep.Items.IndexOf(lbDep.SelectedItem) == i) continue;
                    if (lbDep.Items[i].ToString() == txtDepName.Text)
                    {
                        frmMsgShow.MessageShow("基础信息设置", "科室信息不能重名");
                        return;
                    }
                }
                modelDep.DepartmentID = int.Parse(dtDepInfo.Rows[lbDep.SelectedIndex]["DepartmentID"].ToString());
                modelDep.DepartmentName = txtDepName.Text.Trim();
                modelDep.Remark = dtDepInfo.Rows[lbDep.SelectedIndex]["Remark"].ToString();
                if (bllDep.Update(modelDep))
                {
                    SetDepInfo();
                    btnModifyDep.Text = "修改";
                    btnAddDep.Enabled = btnModifyDep.Enabled = btnDelDep.Enabled = true;
                    txtDepName.Enabled = btnSaveDep.Enabled = false;
                    frmMsgShow.MessageShow("基础信息设置", "科室信息修改成功！");
                }
            }
        }

        private void btnDelDep_Click(object sender, EventArgs e)
        {
            if (lbDep.SelectedItem == null)
            {
                frmMsgShow.MessageShow("基础信息设置", "请选中要删除的项");
                return;
            }
            var dr = dtDoctorInfo.Select("DepartmentName='" + dtDepInfo.Rows[lbDep.SelectedIndex]["DepartmentName"].ToString() + "'");
            if (dr.Length > 0)
            {
                frmMsgShow.MessageShow("基础信息设置", "该科室下存有医生信息，需删除该科室下所有医生信息后才可删除该科室信息！");
                return;
            }
            DbHelperOleDb db = new DbHelperOleDb(2);
            if (bllDep.Delete(int.Parse(dtDepInfo.Rows[lbDep.SelectedIndex]["DepartmentID"].ToString())))
            {
                SetDepInfo();
                frmMsgShow.MessageShow("基础信息设置", "科室信息删除成功！");
            }
            txtDepName.Text = "";
            if (lbDep.Items.Count == 0)//y add 20180420
            {
                btnModifyDep.Enabled = false;
                btnDelDep.Enabled = false;
            }
        }

        private void btnAddDoc_Click(object sender, EventArgs e)
        {
            if (btnAddDoc.Text == "添加")
            {
                btnAddDoc.Text = "取消";
                btnModifyDoc.Enabled = btnDelDoc.Enabled = false;
                txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = true;
                txtDocName.Text = "";
            }
            else
            {
                btnAddDoc.Text = "添加";
                if (dgvDoctor.Rows.Count != 0)//y add 20180510
                    btnModifyDoc.Enabled = btnDelDoc.Enabled = true;
                txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = false;
                if (dgvDoctor.SelectedRows.Count == 0)
                {
                    txtDocName.Text = "";
                }
                else
                {
                    int temp = dgvDoctor.SelectedRows[0].Index;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = false;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = true;//add y 20180511
                    //txtDocName.Text = dgvDoctor.SelectedRows[0].Cells["Doctor"].Value.ToString();
                }
            }
        }

        private void btnModifyDoc_Click(object sender, EventArgs e)
        {
            if (btnModifyDoc.Text == "修改")
            {
                btnModifyDoc.Text = "取消";
                btnAddDoc.Enabled = btnDelDoc.Enabled = false;
                txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = true;
                if (dgvDoctor.SelectedRows.Count != 0)//add y 20180511
                {
                    int temp = dgvDoctor.SelectedRows[0].Index;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = false;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = true;//add y 20180511
                    //txtDocName.Text = dgvDoctor.SelectedRows[0].Cells["Doctor"].Value.ToString();
                }
            }
            else
            {
                btnModifyDoc.Text = "修改";
                btnAddDoc.Enabled = btnDelDoc.Enabled = true;
                txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = false;
                if (dgvDoctor.SelectedRows.Count == 0)
                {
                    txtDocName.Text = "";
                }
                else
                {
                    txtDocName.Text = dgvDoctor.SelectedRows[0].Cells["Doctor"].Value.ToString();
                    cmbDep.Text = dgvDoctor.SelectedRows[0].Cells["Department"].Value.ToString();
                }
            }
        }

        private void btnSaveDoc_Click(object sender, EventArgs e)
        {
            if (!Inspect.NameOnlycharacter2(txtDocName.Text.Trim()))
            {
                frmMsgShow.MessageShow("基础信息设置", "医生姓名只能由二到十个汉字、字母、数字、星号、\".\"或下划线组成！");
                txtDepName.Focus();
                return;
            }
            if (dtDepInfo.Rows.Count == 0)//this move y 20180511
            {
                frmMsgShow.MessageShow("基础信息设置", "请先添加科室信息！");
                return;
            }
            if (cmbDep.SelectedItem == null)
            {
                frmMsgShow.MessageShow("基础信息设置", "请选择科室信息！");
                return;
            }//this end
            DbHelperOleDb db = new DbHelperOleDb(2);
            if (btnAddDoc.Text == "取消")
            {
                foreach (DataGridViewRow a in dgvDoctor.Rows)//重名判断。y add 20180419
                {
                    if (txtDocName.Text.Trim() == a.Cells["Doctor"].Value.ToString() && cmbDep.Text.Trim() == a.Cells["Department"].Value.ToString())
                    {
                        frmMsgShow.MessageShow("基础信息设置", "相同的科室下已有同名的医生");
                        return;
                    }
                }
                modelDoc.DoctorName = txtDocName.Text.Trim();
                var dr = dtDepInfo.Select("DepartmentName='" + cmbDep.Text.Trim() + "'");
                modelDoc.DepartmentID = int.Parse(dr[0]["DepartmentID"].ToString());
                modelDoc.DoctorType = 1;
                if (bllDoctor.Add(modelDoc))
                {
                    SetDoctorInfo();
                    btnAddDoc.Text = "添加";
                    btnModifyDoc.Enabled = btnDelDoc.Enabled = true;
                    txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = false;
                    if (dgvDoctor.Rows.Count != 0)
                    {
                        btnModifyDoc.Enabled = true;
                        btnDelDoc.Enabled = true;
                    }
                    frmMsgShow.MessageShow("基础信息设置", "医生信息添加成功！");
                }
            }
            else if (btnModifyDoc.Text == "取消")
            {
                if (dgvDoctor.SelectedRows.Count == 0)
                {
                    frmMsgShow.MessageShow("基础信息设置", "请选择需要修改的医生信息");
                    return;
                }
                for (int i = 0; i < dgvDoctor.Rows.Count; i++)//重名判断。y add 20180419
                {
                    if (dgvDoctor.Rows.IndexOf(dgvDoctor.SelectedRows[0]) == i)
                    {
                        continue;
                    }
                    if (txtDocName.Text.Trim() == dgvDoctor.Rows[i].Cells["Doctor"].Value.ToString() && cmbDep.Text.Trim() == dgvDoctor.Rows[i].Cells["Department"].Value.ToString())
                    {
                        frmMsgShow.MessageShow("基础信息设置", "相同的科室下已有其他同名的医生");
                        return;
                    }
                }
                modelDoc.DoctorID = int.Parse(dtDoctorInfo.Rows[dgvDoctor.SelectedRows[0].Index]["DoctorID"].ToString());
                modelDoc.DoctorName = txtDocName.Text.Trim();
                var dr = dtDepInfo.Select("DepartmentName='" + cmbDep.Text.Trim() + "'");
                modelDoc.DepartmentID = int.Parse(dr[0]["DepartmentID"].ToString());
                modelDoc.DoctorType = 1;
                if (bllDoctor.Update(modelDoc))
                {
                    SetDoctorInfo();
                    btnModifyDoc.Text = "修改";
                    btnAddDoc.Enabled = btnDelDoc.Enabled = true;
                    txtDocName.Enabled = cmbDep.Enabled = btnSaveDoc.Enabled = false;
                    frmMsgShow.MessageShow("基础信息设置", "医生信息修改成功！");
                }
            }
        }

        private void btnDelDoc_Click(object sender, EventArgs e)
        {
            if (dgvDoctor.SelectedRows.Count == 0)
            {
                frmMsgShow.MessageShow("基础信息设置", "请选择需要删除的信息");
                return;
            }
            if (dgvDoctor.SelectedRows == null) return;
            DbHelperOleDb db = new DbHelperOleDb(2);
            if (bllDoctor.Delete(int.Parse(dtDoctorInfo.Rows[dgvDoctor.SelectedRows[0].Index]["DoctorID"].ToString())))
            {
                SetDoctorInfo();
                if (dgvDoctor.SelectedRows.Count == 0)
                {
                    txtDocName.Text = "";
                    if (dgvDoctor.Rows.Count == 0)
                    {
                        btnModifyDoc.Enabled = false;
                        btnDelDoc.Enabled = false;
                    }
                }
                else
                {
                    int temp = dgvDoctor.SelectedRows[0].Index;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = false;//add y 20180511
                    dgvDoctor.Rows[temp].Selected = true;//add y 20180511
                    //txtDocName.Text = dgvDoctor.SelectedRows[0].Cells["Doctor"].Value.ToString();
                }
                frmMsgShow.MessageShow("基础信息设置", "医生信息删除成功！");
            }
        }
        private void lbDep_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbDep.SelectedItem != null)
                txtDepName.Text = lbDep.SelectedItem.ToString();
        }

        private void dgvDoctor_SelectionChanged(object sender, EventArgs e)
        {

            if (dgvDoctor.SelectedRows.Count > 0)
            {
                txtDocName.Text = dgvDoctor.SelectedRows[0].Cells[1].Value.ToString();
                cmbDep.Text = dgvDoctor.SelectedRows[0].Cells[2].Value.ToString();
            }
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (btnAdd.Text == "添加")
            {
                txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = true;
                btnModify.Enabled = btnDelete.Enabled = false;
                txtGroupItemName.Text = "";
                btnAdd.Text = "取消";
            }
            else
            {
                txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = false;
                if (lbGroupItem.Items.Count != 0)//y add 20180510
                    btnModify.Enabled = btnDelete.Enabled = true;
                btnAdd.Text = "添加";
                try   //y this block add 20180420
                {
                    if (lbGroupItem.Items.Count == 0)
                    {
                        txtGroupItemName.Text = "";
                        foreach (CheckBox ch in flpItemName.Controls)
                        {
                            ch.Checked = false;
                        }
                    }
                    else
                    {
                        int i = lbGroupItem.SelectedIndex > -1 ? lbGroupItem.SelectedIndex : 0;
                        lbGroupItem.SelectedItems.Clear();
                        lbGroupItem.SelectedItem = lbGroupItem.Items[i];
                    }
                }
                catch (Exception ex)
                {
                    frmMsgShow.MessageShow("", ex.ToString());
                }
            }
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (btnModify.Text == "修改")
            {
                txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = true;
                btnAdd.Enabled = btnDelete.Enabled = false;
                btnModify.Text = "取消";
            }
            else
            {
                txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = false;
                btnAdd.Enabled = btnDelete.Enabled = true;
                btnModify.Text = "修改";
                try   //y this block add 20180420
                {
                    if (lbGroupItem.Items.Count == 0)
                    {
                        txtGroupItemName.Text = "";
                        foreach (CheckBox ch in flpItemName.Controls)
                        {
                            ch.Checked = false;
                        }
                    }
                    else
                    {
                        int i = lbGroupItem.SelectedIndex > -1 ? lbGroupItem.SelectedIndex : 0;
                        lbGroupItem.SelectedItems.Clear();
                        lbGroupItem.SelectedItem = lbGroupItem.Items[i];
                    }
                }
                catch (Exception ex)
                {
                    frmMsgShow.MessageShow("", ex.Message);
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lbGroupItem.SelectedItem == null)
            {
                frmMsgShow.MessageShow("基础信息设置", "请选择要删除的组合项目");
                return;
            }
            var dr = dtGroupItem.Select("ProjectGroupNumber='" + lbGroupItem.SelectedItem.ToString() + "'");
            if (dr.Length > 0)
            {
                DbHelperOleDb db = new DbHelperOleDb(0);
                if (bllPG.Delete(int.Parse(dr[0]["ProjectGroupID"].ToString())))
                {
                    frmMsgShow.MessageShow("基础信息设置", "组合项目删除成功！");
                    SetGroupItem();
                    if (lbGroupItem.Items.Count == 0)   //y this block add 20180420
                    {
                        txtGroupItemName.Text = "";
                        foreach (CheckBox ch in flpItemName.Controls)
                        {
                            ch.Checked = false;
                        }
                        btnModify.Enabled = false;
                        btnDelete.Enabled = false;
                    }
                    else
                    {
                        lbGroupItem.SelectedItems.Clear();
                        lbGroupItem.SelectedItem = lbGroupItem.Items[0];
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            int num = 0;
            string gpItem = "";
            if (!Inspect.NameOnlycharacter2(txtGroupItemName.Text.Trim()))
            {
                frmMsgShow.MessageShow("基础信息设置", "组合项目名称只能由二到十个汉字、字母、星号、数字、\".\"、下划线组成");
                txtGroupItemName.Focus();
                return;
            }
            foreach (CheckBox ch in flpItemName.Controls)
            {
                if (ch.Checked)
                {
                    if (num != 0)
                    {
                        gpItem += "-";
                    }
                    gpItem += ch.Text;
                    num++;
                }
            }
            if (num == 0)
            {
                frmMsgShow.MessageShow("基础信息设置", "未选择实验项目，请选择");
                return;
            }
            if (btnAdd.Text == "取消")
            {
                DbHelperOleDb db = new DbHelperOleDb(0);
                modelPG.ProjectGroupNumber = txtGroupItemName.Text.Trim();
                modelPG.GroupContent = gpItem;
                modelPG.ProjectNumber = num;
                foreach (var a in lbGroupItem.Items)//y this block add 20180420
                {
                    if (modelPG.ProjectGroupNumber == a.ToString())
                    {
                        frmMsgShow.MessageShow("基础信息设置", "组合项目不能重名！");
                        return;
                    }
                }
                if (bllPG.Add(modelPG))
                {
                    SetGroupItem();
                    btnAdd.Text = "添加";
                    txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = false;
                    btnAdd.Enabled = btnModify.Enabled = btnDelete.Enabled = true;
                    frmMsgShow.MessageShow("基础信息设置", "组合项目添加成功！");
                }
                if (lbGroupItem.Items.Count != 0)//y add 20180420
                {
                    btnModify.Enabled = true;
                    btnDelete.Enabled = true;
                }
            }
            else if (btnModify.Text == "取消")
            {
                DbHelperOleDb db = new DbHelperOleDb(0);
                int gpNo = lbGroupItem.SelectedIndex;
                if (gpNo == -1)// add y 20180510
                {
                    frmMsgShow.MessageShow("基础信息设置", "修改组合项目前请先选中修改项。");// add y 20180510
                    return;// add y 20180510
                }
                modelPG.ProjectGroupID = int.Parse(dtGroupItem.Rows[gpNo]["ProjectGroupID"].ToString());
                modelPG.ProjectGroupNumber = txtGroupItemName.Text.Trim();
                modelPG.GroupContent = gpItem;
                modelPG.ProjectNumber = num;

                for (int i=0;i<lbGroupItem.Items.Count;i++)  //y this block add 20180420
                {
                    if (lbGroupItem.Items.IndexOf(lbGroupItem.SelectedItem) == i) continue;
                    if (modelPG.ProjectGroupNumber.ToString() == lbGroupItem.Items[i].ToString())
                    {
                        frmMsgShow.MessageShow("基础信息设置", "组合项目不能重名！");
                        return;
                    }
                }
                if (bllPG.Update(modelPG))
                {
                    SetGroupItem();
                    btnModify.Text = "修改";
                    txtGroupItemName.Enabled = flpItemName.Enabled = btnSave.Enabled = false;
                    btnAdd.Enabled = btnModify.Enabled = btnDelete.Enabled = true;
                    frmMsgShow.MessageShow("基础信息设置", "组合项目修改成功！");
                }
            }
        }


        private void ShowReportSort()
        {
            string itemName = "";
            string sort = "";
            //for (int i = 0; i < dtItemInfo1.Rows.Count; i++)
            //{
            //    itemName = dtItemInfo1.Rows[i]["ShortName"].ToString();
            //    sort = OperateIniFile.ReadIniData("RpSort", itemName, "100", Application.StartupPath + "//ReportSort.ini");
            //    dgvPrint.Rows.Add(sort, itemName);
            //}
            //2018-11-02 zlx add
            DbHelperOleDb db = new DbHelperOleDb(0);
            DataTable dtProject = bllPj.GetList("ActiveStatus=1").Tables[0];
            for (int i = 0; i < dtProject.Rows.Count; i++)
            {
                itemName = dtProject.Rows[i]["ShortName"].ToString();
                sort = OperateIniFile.ReadIniData("RpSort", itemName, (dgvPrint.Rows.Count + 1).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
                OperateIniFile.WriteIniData("RpSort", itemName, (dgvPrint.Rows.Count + 1).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
                dgvPrint.Rows.Add(sort, itemName);
            }
            dgvPrint.Sort(dgvPrint.Columns[0], ListSortDirection.Ascending);
        }
        private void lbGroupItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            int gpNo = lbGroupItem.SelectedIndex;
            if (lbGroupItem.SelectedItems.Count != 0)
            {
                txtGroupItemName.Text = lbGroupItem.SelectedItem.ToString();
                string[] pName = (dtGroupItem.Rows[gpNo]["GroupContent"].ToString()).Split('-');
                foreach (CheckBox ch in flpItemName.Controls)
                {
                    ch.Checked = false;
                    if (pName.Contains(ch.Text.Trim()))
                    {
                        ch.Checked = true;
                    }
                }
            }
            else
            {
                txtGroupItemName.Text = "";
                foreach (CheckBox ch in flpItemName.Controls)
                {
                    ch.Checked = false;
                }
            }
        }
        #endregion
        private void frmInfo_SizeChanged(object sender, EventArgs e)
        {
            formSizeChange(this);
        }

        private void fbtnReturn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUserInfo_Click(object sender, EventArgs e)
        {
            if (!CheckFormIsOpen("frmUserManage"))
            {
                frmUserManage frmUM = new frmUserManage();
                //this.TopLevel = false;
                frmUM.TopLevel = false;
                frmUM.Parent = this.Parent;
                frmUM.Show();
            }
            else
            {
                frmUserManage frmUM = (frmUserManage)Application.OpenForms["frmUserManage"];
                //frmIM.Activate();
                frmUM.BringToFront();

            }
        }

        #region 打印设置

        /// <summary>
        /// 显示报告设置过的参数
        /// </summary>
        private void ShowReportSet()
        {
            txtHospitalName.Text = OperateIniFile.ReadInIPara("PrintSet", "HospitalName");
            InitprinterComboBox();
            cmbPrinter.SelectedItem = OperateIniFile.ReadInIPara("PrintSet", "defaultPrinter");
            cmbFormat.SelectedItem = OperateIniFile.ReadInIPara("PrintSet", "PageSize");
            string Margin = OperateIniFile.ReadInIPara("PrintSet", "Margin");
            string[] udlr = Margin.Split('|');
            nudUP.Value = decimal.Parse(udlr[0]);
            nudDown.Value = decimal.Parse(udlr[1]);
            nudLeft.Value = decimal.Parse(udlr[2]);
            nudRight.Value = decimal.Parse(udlr[3]);
            if (bool.Parse(OperateIniFile.ReadInIPara("PrintSet", "AutoPrint")))
            {
                rdbOpen.Checked = true;
            }
            else
            {
                rdbClose.Checked = true;
            }

        }

        /// <summary>
        /// 查询本地打印机并添加到combox控件的Item中
        /// </summary>
        private void InitprinterComboBox()
        {
            List<String> list = LocalPrinter.GetLocalPrinters(); //获得系统中的打印机列表
            foreach (String s in list)
            {
                cmbPrinter.Items.Add(s); //将打印机名称添加到下拉框中
            }
        }

        private void btnSetHospitalName_Click(object sender, EventArgs e)
        {
            //2018-11-02 zlx add
            if (cmbPrinter.SelectedItem==null)
            {
                frmMsgShow.MessageShow("基础信息设置", "请选择默认的打印机！");
                txtDepName.Focus();
                return;
            }

            if (!Inspect.NameOnlycharacter3(txtHospitalName.Text.Trim()))
            {
                frmMsgShow.MessageShow("基础信息设置", "报告名称只能由汉字、字母、数字、星号、\".\"或下划线组成，且不能为空！");
                txtDepName.Focus();
                return;
            }
            OperateIniFile.WriteIniPara("PrintSet", "HospitalName", txtHospitalName.Text.Trim());
            OperateIniFile.WriteIniPara("PrintSet", "defaultPrinter", cmbPrinter.SelectedItem.ToString());


            if (cmbPrinter.SelectedItem != null) //判断是否有选中值
            {
                if (!Externs.SetDefaultPrinter(cmbPrinter.SelectedItem.ToString())) //设置默认打印机
                {
                    frmMsgShow.MessageShow("基础信息", cmbPrinter.SelectedItem.ToString() + "设置为默认打印机失败！");
                }
            }
            if (cmbFormat.SelectedItem != null)
            {
                OperateIniFile.WriteIniPara("PrintSet", "PageSize", cmbFormat.SelectedItem.ToString());
            }
            OperateIniFile.WriteIniPara("PrintSet", "AutoPrint", rdbOpen.Checked.ToString());
            OperateIniFile.WriteIniPara("PrintSet", "Margin", nudUP.Value.ToString() + "|" + nudDown.Value.ToString() + "|"
                + nudLeft.Value.ToString() + "|" + nudRight.Value.ToString());
            frmMsgShow.MessageShow("打印设置", "打印参数设置成功！");
        }


        private void dgvPrint_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                OperateIniFile.WriteIniData("RpSort", dgvPrint.Rows[e.RowIndex].Cells[1].Value.ToString(),
                int.Parse((dgvPrint.Rows[e.RowIndex].Cells[0].Value).ToString()).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
            }
        }
        #endregion

        private void fbtnConnetSet_Click(object sender, EventArgs e)
        {
            if (!CheckFormIsOpen("frmNetSet"))
            {
                frmNetSet frmNS = new frmNetSet();
                //this.TopLevel = false;
                frmNS.TopLevel = false;
                frmNS.Parent = this.Parent;
                frmNS.Show();
            }
            else
            {
                frmNetSet frmNS = (frmNetSet)Application.OpenForms["frmNetSet"];
                //frmIM.Activate();
                frmNS.BringToFront();

            }
        }

        private void btnMoveUP_Click(object sender, EventArgs e)
        {
            //2018-11-02 zlx add
            int tmp = int.Parse(dgvPrint.CurrentRow.Cells[0].Value.ToString());
            if (tmp == 1)
                return;
            foreach (DataGridViewRow dr in dgvPrint.Rows)
            {
                if (int.Parse(dr.Cells[0].Value.ToString()) == (tmp - 1))
                {
                    dr.Cells[0].Value = tmp.ToString("D3");
                    OperateIniFile.WriteIniData("RpSort", dr.Cells[1].Value.ToString(), int.Parse((dr.Cells[0].Value).ToString()).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
                }
            }
            dgvPrint.CurrentRow.Cells[0].Value = (tmp - 1).ToString("D3");
            OperateIniFile.WriteIniData("RpSort", dgvPrint.CurrentRow.Cells[1].Value.ToString(), int.Parse((dgvPrint.CurrentRow.Cells[0].Value).ToString()).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            //2018-11-02 zlx add
            int tmp = int.Parse(dgvPrint.CurrentRow.Cells[0].Value.ToString());
            if (tmp == dgvPrint.RowCount)
                return;
            foreach (DataGridViewRow dr in dgvPrint.Rows)
            {
                if (int.Parse(dr.Cells[0].Value.ToString()) == (tmp + 1))
                {
                    dr.Cells[0].Value = tmp.ToString("D3");
                    OperateIniFile.WriteIniData("RpSort", dr.Cells[1].Value.ToString(), int.Parse((dr.Cells[0].Value).ToString()).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
                }
            }
            dgvPrint.CurrentRow.Cells[0].Value = (tmp + 1).ToString("D3");
            OperateIniFile.WriteIniData("RpSort", dgvPrint.CurrentRow.Cells[1].Value.ToString(), int.Parse((dgvPrint.CurrentRow.Cells[0].Value).ToString()).ToString("D3"), Application.StartupPath + "//ReportSort.ini");
        }

        private void dgvPrint_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            dgvPrint.Sort(dgvPrint.Columns[0], ListSortDirection.Ascending);
        }

      
    }
}