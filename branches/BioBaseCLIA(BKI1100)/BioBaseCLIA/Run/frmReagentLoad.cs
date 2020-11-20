using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using System.IO;
using Maticsoft.DBUtility;
using Dialogs;

namespace BioBaseCLIA.Run
{
    public partial class frmReagentLoad : frmParent
    {
        private BLL.tbReagent bllRg = new BLL.tbReagent();
        private BLL.tbProject bllP = new BLL.tbProject();
        private Model.tbReagent ModelRg = new Model.tbReagent();
        BLL.tbSampleInfo bllsp = new BLL.tbSampleInfo();
        private BLL.tbDilute bllDt = new BLL.tbDilute();//2019-02-21 zlx add
        frmMessageShow frmMsgShow = new frmMessageShow();
        private int RgSelectedNo = -1;
        /// <summary>
        /// 试剂盘配置文件地址
        /// </summary>
        string iniPathReagentTrayInfo = Directory.GetCurrentDirectory() + "\\ReagentTrayInfo.ini";
        DataTable dtItemInfo = new DataTable();//项目信息列表

        /// <summary
        /// 试剂警告最小值 2018-07-27 zlx add
        /// </summary>
        public static int WarnReagent = int.Parse(OperateIniFile.ReadInIPara("Limit", "WarnReagent"));
        public frmReagentLoad()
        {
            InitializeComponent();
        }
        //2018-08-01
        public void LoadData()
        {
            GetSelectedNo = -1;//2018-12-08 zlx mod
            ShowRgInfo(1);
        }
        /// <summary>
        /// 装载试剂样本盘信息
        /// </summary>
        private void srdReagentLoad()
        {
            srdReagent.BeadsGroupNum = RegentNum;
            srdReagent.RgGroupNum = float.Parse(RegentNum.ToString());
            srdReagent.SPGroupNum = SampleNum;
        }
        private void frmLoadReagent_Load(object sender, EventArgs e)
        {
            dgvRgInfoList.Columns[0].Width = 65;
            dgvRgInfoList.Columns[1].Width = 75;
            dgvRgInfoList.Columns[2].Width = 120;
            dgvRgInfoList.Columns[3].Width = 70;
            dgvRgInfoList.Columns[4].Width = 70;
            dgvRgInfoList.Columns[5].Width = 65;
            ShowRgInfo(1);
            cmbRgName.DataSource = GetItemShortName();
            cmbRgName.DisplayMember = "ItemShortName";//设置显示列
            SetDiskProperty();
            //2018-08-31 zlx mod
            int OverDateC = 0;//2018-07-17 zlx add
            foreach (DataRow  odr in dtRgInfo.Rows)
            {
                if (DateTime.Now.Date > Convert.ToDateTime(odr["ValidDate"]).Date)
                {
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "条码编号："
                                + odr["BarCode"] + "项目名称:" + odr["RgName"] + "试剂已经过期，请及时更换");
                    OverDateC++;
                }
            }
            frmAddSample.dtodgvEvent += ChangeDgv;
            frmWorkList.SpDiskUpdate += new Action(ChangeDgv);
            //frmScanSpCode.dtodgvEvent += ChangeDgv;
            int width = this.Width;//自动排版
            int height = this.Height;
            if (panel1.Location.X + panel1.Width >= width || panel1.Location.Y + panel1.Height >= height || groupBox2.Location.Y + groupBox2.Height >= height)
            {
                label1.Location = new Point((int)(width / 2), 0);
                label1.Height = 30;
                srdReagent.Location = new Point(0, 30);
                srdReagent.Width = srdReagent.Height = Math.Min((int)(width / 15 * 7), height - 30);
                groupBox3.Location = new Point(3, 3);
                groupBox4.Location = new Point(3, height - groupBox4.Height - 3);
                width -= srdReagent.Width;
                height -= 30;
                groupBox1.Location = new Point(srdReagent.Width, 30);
                groupBox1.Width = (int)(width / 5 * 3);
                groupBox1.Height = (int)(height / 2);
                groupBox2.Location = new Point(srdReagent.Width, 30 + groupBox1.Height);
                groupBox2.Width = (int)(width / 5 * 3);
                groupBox2.Height = (int)(height / 2);
                panel1.Location = new Point(groupBox1.Location.X + groupBox1.Width, 0);
                panel1.Width = (int)(width / 5 * 2);
                panel1.Height = height + 30;
            }

        }
        /// <summary>
        /// 获取实验项目的缩写名
        /// </summary>
        /// <returns>返回名称列表</returns>
        public DataTable GetItemShortName()
        {
            DbHelperOleDb db = new DbHelperOleDb(0);
            DataTable dtAllList = bllP.GetAllList().Tables[0];
            DataTable dt = new DataTable();
            dt.Columns.Add("ItemShortName", typeof(string));
            for (int i = 0; i < dtAllList.Rows.Count; i++)
            {
                dt.Rows.Add(dtAllList.Rows[i]["ShortName"]);
            }
            dt.Rows.Add("稀释液");                        
            return dt;
        }
        private void frmReagentLoad_SizeChanged(object sender, EventArgs e)
        {
            formSizeChange(this);
        }

        private void fbtnReturn_Click(object sender, EventArgs e)
        {
            fbtnReturn.Enabled = false;
            btnAddR.Enabled = false;
            btnDelR.Enabled = false;
            LeavePageSetReagentToMix();
            fbtnReturn.Enabled = true;
            btnAddR.Enabled = true;
            btnDelR.Enabled = true;
            this.Close();
        }

        messageDialog msd = new messageDialog();
        private void btnAddR_Click(object sender, EventArgs e)
        {
            if (txtRgPosition.Text.Trim() == "")
            {
                frmMsgShow.MessageShow("试剂装载", "未选择试剂位置，请重新输入！");
                txtRgPosition.Focus();
                return;
            }
            if (cmbRgName.Text.Trim() == "")//this add y 20180518
            {
                frmMsgShow.MessageShow("试剂装载", "未选择试剂名称，请重新选择！");
                cmbRgName.Focus();
                return;
            }//this end

            if (cmbReagentType.SelectedIndex == 0)
            {
                #region 试剂信息判断
                if (txtRgCode.Text.Trim() == "")
                {
                    frmMsgShow.MessageShow("试剂装载", "未输入试剂条码，请重新输入！");
                    txtRgCode.Focus();
                    return;
                }
                else//查重功能 this block add y 20180518
                {
                    for (int i = 1; i <= RegentNum; i++)
                    {
                        string BarCode = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "BarCode", "", iniPathReagentTrayInfo);
                        string ItemName = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "ItemName", "", iniPathReagentTrayInfo);
                        if (txtRgCode.Text.Trim() == BarCode && cmbRgName.Text.Trim() == ItemName)
                        {
                            MessageBox.Show("试剂条码与现有的重复（" + i + "号位置），请检查输入的试剂条码和试剂名称。本次加载操作已取消。", "试剂加载");
                            return;
                        }
                    }
                }//this block add y 20180518
                #endregion
            }
            DbHelperOleDb db = new DbHelperOleDb(3);
            DataTable dt = bllRg.GetList(" BarCode='" + txtRgCode.Text.Trim() + "' and Status = '正常'").Tables[0];
            DataTable dt1 = bllRg.GetList(" Postion='" + txtRgPosition.Text.Trim() + "' and Status = '正常'").Tables[0];
            string[] rg = new string[10];
            if (dt1.Rows.Count > 0)
            {
                frmMsgShow.MessageShow("试剂装载", "请先卸载该位置试剂！");
                dgvRgInfoList_SelectionChanged(null, null);
                return;
            }
            if (dt.Rows.Count > 0 && cmbReagentType.SelectedIndex == 0)
            {
                frmMsgShow.MessageShow("试剂装载", "该条码试剂已装载！");
                dgvRgInfoList_SelectionChanged(null, null);
                return;
                #region 屏蔽
                //if (dt.Rows[0]["Status"].ToString() == "卸载")
                //{
                //    ModelRg.ReagentID = int.Parse(dt.Rows[0]["ReagentID"].ToString());
                //    ModelRg.BarCode = dt.Rows[0]["BarCode"].ToString();
                //    ModelRg.Batch = dt.Rows[0]["Batch"].ToString();
                //    ModelRg.leftoverTestR1 = int.Parse(dt.Rows[0]["leftoverTestR1"].ToString());
                //    ModelRg.leftoverTestR2 = int.Parse(dt.Rows[0]["leftoverTestR1"].ToString());
                //    ModelRg.leftoverTestR3 = int.Parse(dt.Rows[0]["leftoverTestR1"].ToString());
                //    ModelRg.leftoverTestR4 = int.Parse(dt.Rows[0]["leftoverTestR1"].ToString());
                //    ModelRg.AllTestNumber = int.Parse(dt.Rows[0]["AllTestNumber"].ToString());
                //    ModelRg.Postion = txtRgPosition.Text.Trim();
                //    ModelRg.ReagentName = dt.Rows[0]["ReagentName"].ToString();
                //    ModelRg.Status = "正常";
                //}
                //else
                //{
                //    #region 检测输入项
                //    if (txtRgBatch.Text.Trim() == "")
                //    {
                //        frmMsgShow.MessageShow("试剂装载", "未输入批号，请重新输入！");
                //        txtRgBatch.Focus();
                //        return;
                //    }
                //    if (cmbRgName.Text.Trim() == "")
                //    {
                //        frmMsgShow.MessageShow("试剂装载", "未选择试剂名称，请重新输入！");
                //        cmbRgName.Focus();
                //        return;
                //    }
                //    if (txtRgAllTest.Text.Trim() == "")
                //    {
                //        frmMsgShow.MessageShow("试剂装载", "未输入总测数，请重新输入！");
                //        txtRgAllTest.Focus();
                //        return;
                //    }
                //    if (txtRgLastTest.Text.Trim() == "")
                //    {
                //        frmMsgShow.MessageShow("试剂装载", "未输入剩余测数，请重新输入！");
                //        txtRgLastTest.Focus();
                //        return;
                //    }
                //    if (int.Parse(txtRgAllTest.Text.Trim()) < int.Parse(txtRgLastTest.Text.Trim()))
                //    {
                //        frmMsgShow.MessageShow("试剂装载", "剩余测数大于总测数，请重新输入！");
                //        txtRgLastTest.Focus();
                //        return;
                //    }
                //    #endregion
                //    ModelRg.ReagentID = int.Parse(dt.Rows[0]["ReagentID"].ToString());
                //    ModelRg.BarCode = dt.Rows[0]["BarCode"].ToString();
                //    ModelRg.Batch = txtRgBatch.Text.Trim();
                //    ModelRg.leftoverTestR1 = int.Parse(txtRgLastTest.Text.Trim());
                //    ModelRg.leftoverTestR2 = int.Parse(txtRgLastTest.Text.Trim());
                //    ModelRg.leftoverTestR3 = int.Parse(txtRgLastTest.Text.Trim());
                //    ModelRg.leftoverTestR4 = int.Parse(txtRgLastTest.Text.Trim());
                //    ModelRg.AllTestNumber = int.Parse(txtRgAllTest.Text.Trim());
                //    ModelRg.Postion = txtRgPosition.Text.Trim();
                //    ModelRg.ReagentName = cmbRgName.Text.Trim();
                //    ModelRg.Status = dt.Rows[0]["Status"].ToString();
                //}
                //if (bllRg.UpdatePart(ModelRg))
                //{
                //    ShowRgInfo();

                //    frmMsgShow.MessageShow("试剂装载", "装载成功！");
                //}
                //rg[0] = ModelRg.BarCode;
                //rg[1] = ModelRg.ReagentName;
                //rg[2] = dt.Rows[0]["AllTestNumber"].ToString();
                //rg[3] = ModelRg.leftoverTestR1.ToString();
                //rg[4] = ModelRg.leftoverTestR1.ToString();
                //rg[5] = ModelRg.leftoverTestR1.ToString();
                //rg[6] = ModelRg.leftoverTestR1.ToString();
                //rg[7] = dt.Rows[0]["AddDate"].ToString();
                //ModifyRgIni(int.Parse(ModelRg.Postion.ToString()), rg);
                #endregion
            }
            else
            {
                fbtnReturn.Enabled = false;
                btnAddR.Enabled = false;
                btnDelR.Enabled = false;
                #region 检测输入项
               
                if (cmbRgName.Text.Trim() == "")
                {
                    frmMsgShow.MessageShow("试剂装载", "未选择试剂名称，请重新输入！");
                    cmbRgName.Focus();
                    return;
                }
                if (cmbReagentType.SelectedIndex == 0)
                {
                    if (txtRgBatch.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow("试剂装载", "未输入批号，请重新输入！");
                        txtRgBatch.Focus();
                        return;
                    }
                    if (txtRgAllTest.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow("试剂装载", "未输入总测数，请重新输入！");
                        txtRgAllTest.Focus();
                        return;
                    }
                    if (txtRgLastTest.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow("试剂装载", "未输入剩余测数，请重新输入！");
                        txtRgLastTest.Focus();
                        return;
                    }
                    if (int.Parse(txtRgAllTest.Text.Trim()) < int.Parse(txtRgLastTest.Text.Trim()))
                    {
                        frmMsgShow.MessageShow("试剂装载", "剩余测数大于总测数，请重新输入！");
                        txtRgLastTest.Focus();
                        return;
                    }
                }
                else
                {
                    txtRgLastTest.Text =txtRgAllTest.Text= "0";
                }
                //if (msd.Confirm("请装载试剂盒") == DialogResult.Cancel)
                //{
                //    return;
                //}
                #endregion
                ModelRg.BarCode = txtRgCode.Text.Trim();
                ModelRg.Batch = txtRgBatch.Text.Trim();
                ModelRg.leftoverTestR1 = int.Parse(txtRgLastTest.Text.Trim());
                ModelRg.leftoverTestR2 = int.Parse(txtRgLastTest.Text.Trim());
                ModelRg.leftoverTestR3 = int.Parse(txtRgLastTest.Text.Trim());
                ModelRg.leftoverTestR4 = int.Parse(txtRgLastTest.Text.Trim());
                ModelRg.AllTestNumber = int.Parse(txtRgAllTest.Text.Trim());
                ModelRg.AddDate = DateTime.Now.Date.ToShortDateString();
                ModelRg.Postion = txtRgPosition.Text.Trim();
                ModelRg.ReagentName = cmbRgName.Text.Trim();
                ModelRg.ValidDate = DateTime.Now.Date.AddDays(90).ToShortDateString();
                ModelRg.Status = "正常";
                ModelRg.ReagentNumber = txtRgPosition.Text.Trim();
                if (bllRg.Add(ModelRg))
                {
                    //2018-11-30 zlx mod
                    rg[0] = ModelRg.BarCode;
                    rg[1] = ModelRg.ReagentName;
                    if (cmbReagentType.SelectedIndex == 0)
                    {
                        rg[2] = ModelRg.Batch;
                        rg[3] = ModelRg.AllTestNumber.ToString();
                        rg[4] = ModelRg.leftoverTestR1.ToString();
                        rg[5] = ModelRg.leftoverTestR1.ToString();
                        rg[6] = ModelRg.leftoverTestR1.ToString();
                        rg[7] = ModelRg.leftoverTestR1.ToString();
                        rg[8] = ModelRg.AddDate.ToString();
                        rg[9] = "0";
                    }
                    else
                    {
                        rg[2] = rg[3] =  rg[4] =rg[5] =rg[6]= rg[7] = rg[8] = "";
                        rg[9] = "1";
                    }
                    ModifyRgIni(int.Parse(ModelRg.Postion.ToString()), rg);
                    ShowRgInfo(0);
                    if (NetCom3.isConnect)
                    {
                    SendAgain:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 0B B0 00 00"), 0);//y add 20180814 通知下位机加载完成
                        if (!NetCom3.Instance.SPQuery())
                        {
                            if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                goto SendAgain;
                            else
                                frmMsgShow.MessageShow("试剂装载", "通讯异常，请核对连接情况并对仪器系统进行重启！");
                        }
                        else
                        {
                            frmMsgShow.MessageShow("试剂装载", "装载成功！");
                            btnAddR.Enabled = false;
                            SetDiskProperty();//2018-07-27 zlx add
                        }
                    }
                    else
                    {
                        frmMsgShow.MessageShow("试剂装载", "网络未连接请先进行网络连接！");
                        return;
                    }
                    //if(NetCom3.Instance.SPQuery())
                    //{
                    //    frmMsgShow.MessageShow("试剂装载", "装载成功！");
                    //    btnAddR.Enabled = false;
                    //    SetDiskProperty();//2018-07-27 zlx add
                    //}
                }
                fbtnReturn.Enabled = true;
                btnAddR.Enabled = true;
                btnDelR.Enabled = true;
            }
            //2018-11-14 zlx add
            DataTable dtRgInfoCopy = dtRgInfo.Copy();
            for (int i = 0; i < dtRgInfoCopy.Rows.Count; i++)
            {
                if ((int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) == 9 || int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) == 10) && dtRgInfoCopy.Rows[i]["ReagentType"].ToString() == "1")
                {
                    //dtRgInfo.Rows[i]["leftoverTestR1"] = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfo.Rows[i]["Postion"], "leftDiuVol", "0", iniPathReagentTrayInfo);
                    dtRgInfoCopy.Rows[i]["leftoverTestR1"] = srdReagent.RgTestNum[int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) - 1] = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfoCopy.Rows[i]["Postion"], "leftDiuVol", "0", iniPathReagentTrayInfo);
                }
            }
            DataView dv = dtRgInfoCopy.DefaultView;
            dv.Sort = "Postion";
            dtRgInfoCopy = dv.ToTable();
            dgvRgInfoList.DataSource = dv;
        }
        private void ShowRgInfo(int start)
        {
            DbHelperOleDb db = new DbHelperOleDb(3);
            DataTable dtRI = bllRg.GetAllList().Tables[0];
            var dr = dtRI.Select("Status <> '卸载'");
            if (dtItemInfo.Rows.Count == 0)
            {
                db = new DbHelperOleDb(0);//2018-10-13 zlx add
                dtItemInfo = bllP.GetList("ActiveStatus=1").Tables[0];
            }
            if (frmWorkList.RunFlag != (int)RunFlagStart.IsRuning)
            {
                dtRgInfo.Clear();
                for (int i = 0; i < dr.Length; i++)
                {
                    dtRgInfo.Rows.Add();
                    dtRgInfo.Rows[i]["Postion"] = dr[i]["Postion"];
                    dtRgInfo.Rows[i]["RgName"] = dr[i]["ReagentName"];
                    dtRgInfo.Rows[i]["AllTestNumber"] = dr[i]["AllTestNumber"];
                    dtRgInfo.Rows[i]["leftoverTestR1"] = dr[i]["leftoverTestR1"];
                    dtRgInfo.Rows[i]["leftoverTestR2"] = dr[i]["leftoverTestR2"];
                    dtRgInfo.Rows[i]["leftoverTestR3"] = dr[i]["leftoverTestR3"];
                    dtRgInfo.Rows[i]["leftoverTestR4"] = dr[i]["leftoverTestR4"];
                    dtRgInfo.Rows[i]["BarCode"] = dr[i]["BarCode"];
                    //2018-11-14
                    int leftover = Convert.ToInt32(dtRgInfo.Rows[i]["leftoverTestR1"]);
                    //dtRgInfo.Rows[i]["Status"] = dr[i]["Status"];
                    dtRgInfo.Rows[i]["Batch"] = dr[i]["Batch"];
                    dtRgInfo.Rows[i]["ValidDate"] = dr[i]["ValidDate"];//2018-08-18 zlx add
                    //2018-11-14  zlx mod
                    if (Convert.ToDateTime(dtRgInfo.Rows[i]["ValidDate"]) < DateTime.Now.Date)
                        dtRgInfo.Rows[i]["Status"] = "过期";
                    else
                        dtRgInfo.Rows[i]["Status"] = "正常";
                    dtRgInfo.Rows[i]["NoUsePro"] = dtItemInfo.Select("ShortName='" + dr[i]["ReagentName"] + "'")[0]["NoUsePro"];//2018-10-13 zlx add
                    dtRgInfo.Rows[i]["ReagentType"] = OperateIniFile.ReadIniData("ReagentPos" + dr[i]["Postion"], "ReagentType", "", iniPathReagentTrayInfo); 

                }
            }
            DataTable dtRgInfoCopy = dtRgInfo.Copy();
            for (int i = 0; i < dtRgInfoCopy.Rows.Count; i++)
            {
                string ReagentType = dtRgInfoCopy.Rows[i]["ReagentType"].ToString();// OperateIniFile.ReadIniData("ReagentPos" + dtRgInfoCopy.Rows[i]["Postion"].ToString(), "ReagentType", "0", iniPathReagentTrayInfo);
                srdReagent.RgName[int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) - 1] = dr[i]["ReagentName"].ToString();
                //srdReagent.RgTestNum[int.Parse(dtRgInfo.Rows[i]["Postion"].ToString()) - 1] = dr[i]["leftoverTestR1"].ToString();
                if ((int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) == 9 || int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) == 10) && ReagentType == "1")
                {
                    //dtRgInfo.Rows[i]["leftoverTestR1"] = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfo.Rows[i]["Postion"], "leftDiuVol", "0", iniPathReagentTrayInfo);
                    dtRgInfoCopy.Rows[i]["leftoverTestR1"] = srdReagent.RgTestNum[int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) - 1] = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfoCopy.Rows[i]["Postion"], "leftDiuVol", "0", iniPathReagentTrayInfo);
                }
                else
                {
                    srdReagent.RgTestNum[int.Parse(dtRgInfoCopy.Rows[i]["Postion"].ToString()) - 1] = dr[i]["leftoverTestR1"].ToString();
                }

            }
            int OverDateC = 0;//2018-07-17 zlx add
            foreach (var odr in dr)
            {
                if (DateTime.Now.Date > Convert.ToDateTime(odr["ValidDate"]).Date)
                {
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "条码编号："
                                + odr["BarCode"] + "项目名称:" + odr["ReagentName"] + "试剂已经过期，请及时更换");
                    OverDateC++;
                }

            }
            DataView dv = dtRgInfoCopy.DefaultView;
            dv.Sort = "Postion";
            dtRgInfoCopy = dv.ToTable();
            dgvRgInfoList.DataSource = dtRgInfoCopy;
            //MessageBox.Show(dtRgInfo.Rows[0].ToString());
            SetDiskProperty();//2018-07-26 zlx add
            srdReagent.Invalidate();
            if (start > 0 && OverDateC > 0)//2018-11-16 添加试剂过期报警功能
            {
                MessageBox.Show("有过期试剂，请及时卸载！详情请查日志信息！", "试剂过期警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// 显示样本盘上的样本信息
        /// </summary>
        private void srdReagent_MouseDown(object sender, MouseEventArgs e)
        {
            int fg = -1; 
            if (srdReagent.rgSelectedNo >= -1)
            {
                if (srdReagent.rgSelectedNo == -1)
                    RgSelectedNo = 9;
                else
                    RgSelectedNo = srdReagent.rgSelectedNo;
                RgSelectedNo = srdReagent.rgSelectedNo;
                if (srdReagent.RgColor[GetSelectedNo].Name == "Yellow")
                {
                    srdReagent.RgColor[GetSelectedNo] = Color.White;
                    srdReagent.BdColor[GetSelectedNo] = Color.White;
                }
                string ss = srdReagent.RgName[RgSelectedNo].ToString().Trim();
                if (srdReagent.RgName[RgSelectedNo].ToString().Trim() == "")
                {
                    srdReagent.RgColor[RgSelectedNo] = Color.Yellow;
                    srdReagent.BdColor[RgSelectedNo] = Color.Yellow;
                    GetSelectedNo = RgSelectedNo;
                }
                else
                {
                    //int ii = Convert.ToInt32(srdReagent.RgTestNum[RgSelectedNo]);
                    string ReagentType = "";
                    if (RgSelectedNo == 8 || RgSelectedNo == 9)
                        ReagentType = OperateIniFile.ReadIniData("ReagentPos" + (RgSelectedNo+1).ToString(), "ReagentType", "0", iniPathReagentTrayInfo);
                    if (ReagentType != "1")
                    {
                        if (int.Parse(srdReagent.RgTestNum[RgSelectedNo]) == 0)
                        {
                            srdReagent.RgColor[RgSelectedNo] = srdReagent.CRgAlarm;
                            srdReagent.BdColor[RgSelectedNo] = srdReagent.CBeedsAlarm;
                        }
                        else if (Convert.ToInt32(srdReagent.RgTestNum[RgSelectedNo]) > 0 && Convert.ToInt32(srdReagent.RgTestNum[RgSelectedNo]) < WarnReagent)
                        {
                            srdReagent.RgColor[RgSelectedNo] = Color.Orange;
                            srdReagent.BdColor[RgSelectedNo] = Color.Orange;
                        }
                        else
                        {
                            srdReagent.RgColor[RgSelectedNo] = srdReagent.CRgLoaded;
                            srdReagent.BdColor[RgSelectedNo] = srdReagent.CBeedsLoaded;
                        }
                    }
                    
                }
                for (int i = 0; i < dgvRgInfoList.Rows.Count; i++)
                {
                    if (dgvRgInfoList.Rows[i].Cells[0].Value.ToString() == (srdReagent.rgSelectedNo + 1).ToString())
                    {
                        dgvRgInfoList.Rows[i].Selected = true;
                        fg = i;
                        break;
                    }
                }
                if (fg == -1)
                {
                    txtRgPosition.Text = (srdReagent.rgSelectedNo + 1).ToString();
                    cmbRgName.Text = "";
                    txtRgCode.Text = "";
                    txtRgBatch.Text = "";
                    txtRgAllTest.Text = "100";
                    txtRgLastTest.Text = "100";
                    txtDiluteVol.Text = "";
                    for (int d = 0; d < dgvRgInfoList.Rows.Count; d++)
                    {
                        dgvRgInfoList.Rows[d].Selected = false;
                    }
                }
                //2019-04-01 zlx add
                if (RgSelectedNo + 1 == 9 || RgSelectedNo + 1 == 10)
                    label17.Enabled = cmbReagentType.Enabled  = btnAddD.Enabled = true;
                else
                    label17.Enabled = cmbReagentType.Enabled = btnAddD.Enabled = false;
               
              
            }
        }
        public int GetSelectedNo { get; set; }
        private void dgvRgInfoList_SelectionChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < dtRgInfo.Rows.Count; i++)
            {
                if (dgvRgInfoList.SelectedRows.Count < 1) break;
                if (dtRgInfo.Rows[i]["Postion"].ToString() == (dgvRgInfoList.SelectedRows[0].Cells[0].Value).ToString())
                {
                    cmbReagentType.SelectedIndex = 0;
                    txtRgPosition.Text = dtRgInfo.Rows[i]["Postion"].ToString();
                    cmbRgName.Text = dtRgInfo.Rows[i]["RgName"].ToString();
                    txtRgCode.Text = dtRgInfo.Rows[i]["BarCode"].ToString();
                    txtRgBatch.Text = dtRgInfo.Rows[i]["Batch"].ToString();
                    txtRgAllTest.Text = dtRgInfo.Rows[i]["AllTestNumber"].ToString();
                    if (dtRgInfo.Rows[i]["ReagentType"].ToString() == "1")
                        txtRgLastTest.Text = "0";
                    else
                        txtRgLastTest.Text = dtRgInfo.Rows[i]["leftoverTestR1"].ToString();
                    txtDiluteVol.Text = OperateIniFile.ReadIniData("ReagentPos" + int.Parse(txtRgPosition.Text).ToString(), "leftDiuVol", "", iniPathReagentTrayInfo);//2019-02-19 zlx add
                    if (txtRgPosition.Text != "9" && txtRgPosition.Text != "10")
                        label17.Enabled = cmbReagentType.Enabled = btnAddD.Enabled = false;

                    else
                    {
                        label17.Enabled = cmbReagentType.Enabled = btnAddD.Enabled = true;
                        string R1 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent1", "0", iniPathReagentTrayInfo);
                        string R2 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent2", "0", iniPathReagentTrayInfo);
                        string R3 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent3", "0", iniPathReagentTrayInfo);
                        if (R1 == "") R1 = "0";
                        if (R2 == "") R2 = "0";
                        if (R3 == "") R3 = "0";
                        txtDiluteVol.Text = (int.Parse(R1) + int.Parse(R2) + int.Parse(R3)).ToString();
                    }
                }
               
            }
        }

        private void btnDelR_Click(object sender, EventArgs e)
        {
            string[] rg = new string[10];
            string RegentType = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "ReagentType", "", iniPathReagentTrayInfo);
            if (RegentType =="0"&& txtRgCode.Text == "")
            {
                frmMsgShow.MessageShow("试剂装载", "未输入试剂条码，请重新输入！");
                txtRgCode.Focus();
                return;
            }
            //if (msd.Confirm("请卸载试剂盒") == DialogResult.Cancel)
            //{
            //    return;
            //}
            fbtnReturn.Enabled = false;
            btnAddR.Enabled = false;
            btnDelR.Enabled = false;
            DbHelperOleDb db = new DbHelperOleDb(3);

            //2019-04-01 添加卸载稀释液
            BLL.tbDilute bllsb = new BLL.tbDilute();
            DataTable dtAllSb = bllsb.GetAllList().Tables[0];
            DataRow[] dr2 = dtAllSb.Select("DilutePos like '" + txtRgPosition.Text.Trim() + "*'");
            for (int i = 0; i < dr2.Length;i++ )
            {
                bllsb.Delete(dr2[i]["DilutePos"].ToString());
            }
            //new BLL.tbDilute().Delete("DilutePos like '" + txtRgPosition.Text.Trim() + "%'");

            DataTable dt = bllRg.GetList(" BarCode='" + txtRgCode.Text.Trim() + "' AND Status='正常'").Tables[0];
            ModelRg.ReagentID = int.Parse(dt.Rows[0]["ReagentID"].ToString());
            ModelRg.BarCode = txtRgCode.Text.Trim();
            ModelRg.Batch = txtRgBatch.Text.Trim();
            ModelRg.leftoverTestR1 = int.Parse(txtRgLastTest.Text.Trim());
            ModelRg.AllTestNumber = int.Parse(txtRgAllTest.Text.Trim());
            ModelRg.Postion = txtRgPosition.Text.Trim();
            ModelRg.ReagentName = cmbRgName.Text.Trim();
            ModelRg.Status = "卸载";
            for (int i = 0; i < 10; i++)
            {
                rg[i] = "";
            }
            if (bllRg.UpdatePart(ModelRg))
            {
                srdReagent.RgName[int.Parse(txtRgPosition.Text.Trim()) - 1] = "";
                srdReagent.RgTestNum[int.Parse(txtRgPosition.Text.Trim()) - 1] = "";
                ModifyRgIni(int.Parse(dt.Rows[0]["Postion"].ToString()), rg);
//仪器卸载时稀释液体积恢复默认值 lyn add 20180611
                OperateIniFile.WriteIniData("ReagentPos" + int.Parse(dt.Rows[0]["Postion"].ToString()).ToString(),
                    "leftDiuVol", "0", iniPathReagentTrayInfo);
                ShowRgInfo(0);//2018-11-16 zlx mod
                SetDiskProperty();//2018-07-27
                if (NetCom3.isConnect)
                {
                SendAgain:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 0B B0 00 00"), 0);//y add 20180814 通知下位机加载完成
                    if (!NetCom3.Instance.SPQuery())
                    {
                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                            goto SendAgain;
                        else
                            frmMsgShow.MessageShow("试剂装载", "通讯异常，请核对连接情况并对仪器系统进行重启！");
                    }
                    else
                    {
                        frmMsgShow.MessageShow("试剂装载", "卸载成功！");
                        btnDelR.Enabled = false;
                    }
                }
                else
                {
                    frmMsgShow.MessageShow("试剂装载", "网络未连接请先进行网络连接！");
                }
                //if (NetCom3.Instance.SPQuery())
                //{
                //    frmMsgShow.MessageShow("试剂装载", "卸载成功！");
                //    btnDelR.Enabled = false;
                //}
            }
            fbtnReturn.Enabled = true;
            btnAddR.Enabled = true;
            btnDelR.Enabled = true;
            
        }
        private void ModifyRgIni(int pos, string[] strRg)
        {
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "BarCode", strRg[0], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "ItemName", strRg[1], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "BachNum", strRg[2], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "TestCount", strRg[3], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "LeftReagent1", strRg[4], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "LeftReagent2", strRg[5], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "LeftReagent3", strRg[6], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "LeftReagent4", strRg[7], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "LoadDate", strRg[8], iniPathReagentTrayInfo);
            OperateIniFile.WriteIniData("ReagentPos" + pos.ToString(), "ReagentType", strRg[9], iniPathReagentTrayInfo);
        }
        private void btnAddCurve_Click(object sender, EventArgs e)
        {
            string activedate = DateTime.Now.ToString();
            string validDate = DateTime.Now.AddDays(30).ToString();
            frmAddScaling frmAS = new frmAddScaling(cmbRgName.Text.Trim(), txtRgBatch.Text.Trim(),activedate,validDate);
            frmAS.Show();
        }

        private void btnWorkList_Click(object sender, EventArgs e)
        {
            LeavePageSetReagentToMix();
            if (!CheckFormIsOpen("frmWorkList"))
            {
                frmWorkList frmWL = new frmWorkList();
                frmWL.TopLevel = false;
                frmWL.Parent = this.Parent;
                frmWL.Show();
            }
            else
            {
                frmWorkList frmWL = (frmWorkList)Application.OpenForms["frmWorkList"];
                frmWL.Show();
                frmWL.BringToFront();
            }

        }

        private void fbtnTestResult_Click(object sender, EventArgs e)
        {
            LeavePageSetReagentToMix();
            if (!CheckFormIsOpen("frmTestResult"))
            {
                frmTestResult frmTR = new frmTestResult();
                frmTR.TopLevel = false;
                frmTR.Parent = this.Parent;
                frmTR.Show();
            }
            else
            {
                frmTestResult frmTR = (frmTestResult)Application.OpenForms["frmTestResult"];
                frmTR.BringToFront(); ;

            }
        }
        private void LeavePageSetReagentToMix()//离开页面时，通知下位机加载完成，同时让装卸栽按钮变灰
        {
            btnAddR.Enabled = false;
            btnDelR.Enabled = false;
            if (NetCom3.isConnect)
            {
            SendAgain:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 0B B0 00 00"), 0);//y add 20180814 通知下位机加载完成
                if (!NetCom3.Instance.SPQuery())
                {
                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                        goto SendAgain;
                    else
                        frmMsgShow.MessageShow("试剂装载", "通讯异常，请核对连接情况并对仪器系统进行重启！");
                }
            }
            else
            {
                frmMsgShow.MessageShow("试剂装载", "网络未连接请先进行网络连接！");
                return;
            }
            NetCom3.Delay(1000);
            //NetCom3.Instance.SPQuery();
        }
        private void btnLoadSample_Click(object sender, EventArgs e)
        {
            LeavePageSetReagentToMix();
            if (!CheckFormIsOpen("frmSampleLoad"))
            {
                frmSampleLoad frmSL = new frmSampleLoad();
                frmSL.TopLevel = false;
                frmSL.Parent = this.Parent;
                frmSL.Show();
            }
            else
            {
                frmSampleLoad frmSL = (frmSampleLoad)Application.OpenForms["frmSampleLoad"];
                frmSL.BringToFront(); ;

            }
            this.Close();//2018-11-14 zlx add
        }

       
        private void SetDiskProperty()
        {
            //2018-07-27 zlx mod
            #region 试剂位号颜色设置
            for (int j = 0; j < srdReagent.RgGroupNum; j++)
            {
                srdReagent.RgColor[j] = Color.White;
                srdReagent.BdColor[j] = srdReagent.CBeedsNull;
            }
            for (int j = 0; j < dtRgInfo.Rows.Count; j++)
            {   string ReagentType="";
                if(int.Parse(dtRgInfo.Rows[j]["Postion"].ToString())==9||int.Parse(dtRgInfo.Rows[j]["Postion"].ToString())==10)
                    ReagentType = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfo.Rows[j]["Postion"].ToString(), "ReagentType", "0", iniPathReagentTrayInfo);
                if (ReagentType == "1")
                {
                    string leftDiuVol = OperateIniFile.ReadIniData("ReagentPos" + dtRgInfo.Rows[j]["Postion"].ToString(), "leftDiuVol", "0", iniPathReagentTrayInfo);
                    if (leftDiuVol != "" && int.Parse(leftDiuVol) > 0)
                    {
                        srdReagent.RgColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = Color.Purple ;
                    }
                    else
                        srdReagent.RgColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = Color.SkyBlue;

                }
                else
                {
                    if (Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR1"]) < WarnReagent || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR2"]) < WarnReagent || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR3"]) < WarnReagent || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR4"]) < WarnReagent)
                    {
                        if (Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR1"]) == 0 || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR2"]) == 0 || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR3"]) == 0 || Convert.ToInt32(dtRgInfo.Rows[j]["leftoverTestR4"]) == 0)
                        {
                            srdReagent.RgColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = srdReagent.CRgAlarm;
                            srdReagent.BdColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = srdReagent.CBeedsAlarm;
                        }
                        else
                        {
                            srdReagent.RgColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = Color.Orange;
                            srdReagent.BdColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = Color.Orange;
                        }
                    }
                    else
                    {
                        srdReagent.RgColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = srdReagent.CRgLoaded;
                        srdReagent.BdColor[int.Parse(dtRgInfo.Rows[j]["Postion"].ToString()) - 1] = srdReagent.CBeedsLoaded;
                    }
                }
            }
            #endregion
            #region 样本位号颜色设置
            for (int j = 0; j < srdReagent.SPGroupNum; j++)
            {
                srdReagent.SpColor[j] = Color.White;
            }
            for (int i = 0; i < dtSpInfo.Rows.Count; i++)
            {
                if (dtSpInfo.Rows[i]["Status"].ToString() == "0")
                {
                    srdReagent.SpColor[int.Parse(dtSpInfo.Rows[i]["Position"].ToString()) - 1] = srdReagent.CSampleLoaded;
                }
                else if (dtSpInfo.Rows[i]["Status"].ToString() == "1")
                {
                    srdReagent.SpColor[int.Parse(dtSpInfo.Rows[i]["Position"].ToString()) - 1] = srdReagent.CSampleCompleted;
                }
                else
                {
                    srdReagent.SpColor[int.Parse(dtSpInfo.Rows[i]["Position"].ToString()) - 1] = srdReagent.CSampleAlarm;
                }
            }
            #endregion
        }

        public void ChangeDgv()
        {
            ShowRgInfo(0);//2018-08-31 zlx add
            SetDiskProperty();
        }

        private void SetDtSampleInfo()
        {
            DataTable dtSI = dtSpInfo.Clone();
            DataTable dt = bllsp.GetList(" SendDateTime='" + DateTime.Now.ToShortDateString() + "'").Tables[0];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dtSI.Select("SampleNo='" + dt.Rows[i]["SampleNo"].ToString() + "'").Length < 1)
                {
                    dtSI.Rows.Add(dt.Rows[i]["Position"], dt.Rows[i]["SampleNo"], dt.Rows[i]["SampleType"],
                        dt.Rows[i]["SampleContainer"], GetItemName(dt.Rows[i]["ItemID"].ToString()), dt.Rows[i]["RepeatCount"], dt.Rows[i]["Emergency"], dt.Rows[i]["Status"]);
                }
                else
                {
                    var dr = dtSI.Select("SampleNo='" + dt.Rows[i]["SampleNo"].ToString() + "'");
                    dr[0]["ItemName"] += " " + GetItemName(dt.Rows[i]["ItemID"].ToString());
                }
            }
            dtSpInfo.Rows.Clear();
            dtSpInfo = dtSI;

        }
        private string GetItemName(string id)
        {
            dtItemInfo = bllP.GetList("ActiveStatus=1").Tables[0];
            var dr = dtItemInfo.Select("ProjectID=" + id);
            return dr[0]["ShortName"].ToString();
        }

        private void frmReagentLoad_FormClosed(object sender, FormClosedEventArgs e)
        {
            frmAddSample.dtodgvEvent -= ChangeDgv;
            frmWorkList.SpDiskUpdate -= new Action(ChangeDgv);
            //frmScanSpCode.dtodgvEvent -= ChangeDgv;
        }

        private void srdReagent_MouseUp(object sender, MouseEventArgs e)
        {
            if (RgSelectedNo > -1)
            {
                srdReagent.Enabled = false;
                string HoleNum = (RgSelectedNo + 1).ToString("x2");
                ////移动到试剂x装载位置
                if (NetCom3.isConnect)
                {
                    SendAgain:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 09 " + HoleNum), 0);
                    if (!NetCom3.Instance.SPQuery())
                    {
                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                            goto SendAgain;
                        else
                            frmMsgShow.MessageShow("试剂装载", "通讯异常，请核对连接情况并对仪器系统进行重启！");
                    }
                    //while (!NetCom3.SpReciveFlag)
                    //{
                    //    NetCom3.Delay(1);
                    //}
                }
                else
                {
                    frmMsgShow.MessageShow("试剂装载", "网络未连接请先进行网络连接！");
                    return;
                }
               //zlx modify点击10-15报错，及启用卸载按钮
                if (dtRgInfo.Select("Postion='" + (RgSelectedNo + 1)+"'").Length > 0)
                {
                    btnDelR.Enabled = true;
                    btnAddR.Enabled = false;
                    
                }
                else
                {
                    btnDelR.Enabled = false;
                    btnAddR.Enabled = true;
                }
             
                RgSelectedNo = -1;
                srdReagent.Enabled = true;
            }
        }

        private void chkManualInput_CheckedChanged(object sender, EventArgs e)
        {
            if (chkManualInput.Checked)
            {
                txtRgCode.Enabled = true;
                txtRgCode.Focus();
            }
            else
            {
                txtRgCode.Enabled = false;
            }
        }
        //2018-11-16 zlx add
        private void dgvRgInfoList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string HoleNum = Convert.ToInt32(dgvRgInfoList.CurrentRow.Cells[0].Value).ToString("x2");
            if (NetCom3.isConnect)
            {
            SendAgain:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 09 " + HoleNum), 0);
                srdReagent.Enabled = false;
                if (!NetCom3.Instance.SPQuery())
                {
                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                        goto SendAgain;
                    else
                        frmMsgShow.MessageShow("试剂装载", "通讯异常，请核对连接情况并对仪器系统进行重启！");
                }
                //while (!NetCom3.SpReciveFlag)
                //{
                //    NetCom3.Delay(1);
                //}
                srdReagent.Enabled = true;
            }
            else
            {
                frmMsgShow.MessageShow("试剂装载", "网络未连接请先进行网络连接！");
                return;
            }

            //zlx modify点击10-15报错，及启用卸载按钮
            if (dtRgInfo == null || dgvRgInfoList.CurrentRow == null) return;//2019-01-07 zlx add
            if (dtRgInfo.Select("Postion=" + Convert.ToInt32(dgvRgInfoList.CurrentRow.Cells[0].Value) + "").Length > 0)
            {
                btnDelR.Enabled = true;
                btnAddR.Enabled = false;
            }
            RgSelectedNo = -1;
        }

        private void btnAddD_Click(object sender, EventArgs e)
        {
            DataRow[] drRgInfo= dtRgInfo.Select("Postion=" + Convert.ToInt32(txtRgPosition.Text) + "");
            if (drRgInfo.Length == 0)
            {
                frmMsgShow.MessageShow("试剂装载", "请先装载试剂盒！");
                return;
            }
            frmLoadSu f = new frmLoadSu();
            f.RegentPos =int.Parse(txtRgPosition.Text);
            f.ShowDialog();
            string R1 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent1", "0", iniPathReagentTrayInfo);
            string R2 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent2", "0", iniPathReagentTrayInfo);
            string R3 = OperateIniFile.ReadIniData("ReagentPos" + txtRgPosition.Text, "LeftReagent3", "0", iniPathReagentTrayInfo);
            if (R1 == "") R1 = "0";
            if (R2 == "") R2 = "0";
            if (R3 == "") R3 = "0";
            txtDiluteVol.Text = (int.Parse(R1) + int.Parse(R2) + int.Parse(R3)).ToString();
            OperateIniFile.WriteIniData("ReagentPos" + txtRgPosition.Text, "leftDiuVol", txtDiluteVol.Text, iniPathReagentTrayInfo);
            drRgInfo[0]["leftoverTestR1"] = int.Parse(R1);
            drRgInfo[0]["leftoverTestR2"] = int.Parse(R2);
            drRgInfo[0]["leftoverTestR3"] = int.Parse(R3);
            DbHelperOleDb db = new DbHelperOleDb(3);
            DbHelperOleDb.ExecuteSql(@"update tbReagent set leftoverTestR1 =" + drRgInfo[0]["leftoverTestR1"] + ",leftoverTestR2 = " + drRgInfo[0]["leftoverTestR2"] +
                                      ",leftoverTestR3 = " + drRgInfo[0]["leftoverTestR3"] + "  where Postion='" + Convert.ToInt32(txtRgPosition.Text) + "' and ReagentName = '" + drRgInfo[0]["RgName"] + "' and Status='正常'");
            //OperateIniFile.ReadIniData("ReagentPos" + int.Parse(txtRgPosition.Text).ToString(), "leftDiuVol", "", iniPathReagentTrayInfo);
            ShowRgInfo(0);//2018-11-16 zlx mod
            SetDiskProperty();//2018-07-27
           
        }
        

    }
}
