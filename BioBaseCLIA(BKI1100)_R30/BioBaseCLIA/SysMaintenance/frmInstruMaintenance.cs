using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using System.Threading;
using System.IO;
using BioBaseCLIA.Run;
using Maticsoft.DBUtility;
using Localization;

namespace BioBaseCLIA.SysMaintenance
{
    public partial class frmInstruMaintenance : frmParent
    {
        #region 变量
        /// <summary>
        /// 底物灌注次数
        /// </summary>
        public int subPerfusionNum = int.Parse(OperateIniFile.ReadInIPara("MaintenancePara", "subPerfusionNum"));
        /// <summary>
        /// 加样管路灌注次数
        /// </summary>
        public int samPerfusionNum = int.Parse(OperateIniFile.ReadInIPara("MaintenancePara", "samPerfusionNum"));
        /// <summary>
        /// 清洗管路灌注次数
        /// </summary>
        public int washPerfusionNum = int.Parse(OperateIniFile.ReadInIPara("MaintenancePara", "washPerfusionNum"));
        /// <summary>
        /// PMT背景值检测对比值
        /// </summary>
        public int PmtCompareValue = int.Parse(OperateIniFile.ReadInIPara("MaintenancePara", "PmtCompareValue"));
        /// <summary>
        /// 底物有效性检测对比值
        /// </summary>
        public int subCompareValue = int.Parse(OperateIniFile.ReadInIPara("MaintenancePara", "subCompareValue"));
        /// <summary>
        /// 清洗盘取放管位置当前孔号
        /// </summary>
        int currentHoleNum = 1;

        /// <summary>
        /// 维护开始线程
        /// </summary>
        private Thread StartThread;

        /// <summary>
        /// 底物与暂存盘配置文件地址
        /// </summary>
        string iniPathSubstrateTube = Directory.GetCurrentDirectory() + "\\SubstrateTube.ini";
        /// <summary>
        /// 试剂盘配置文件地址
        /// </summary>
        string iniPathReagentTrayInfo = Directory.GetCurrentDirectory() + "\\ReagentTrayInfo.ini";
        /// <summary>
        /// 反应盘配置文件地址
        /// </summary>
        string iniPathReactTrayInfo = Directory.GetCurrentDirectory() + "\\ReactTrayInfo.ini";
        /// <summary>
        /// 清洗盘配置文件地址
        /// </summary>
        string iniPathWashTrayInfo = Directory.GetCurrentDirectory() + "\\WashTrayInfo.ini";

        /// <summary>
        /// 初始洗针时间
        /// </summary>
        int FirstNeedleWashTime = int.Parse(OperateIniFile.ReadInIPara("Time", "FirstCleanNeedleTime"));

        /// <summary>
        /// 报警时更改主界面日志按钮的颜色
        /// </summary>
        //public static event Action<int> btnLogColor;
        frmMessageShow frmMsgShow = new frmMessageShow();
        /// <summary>
        /// 指令返回
        /// </summary>
        string BackObj = "";
        /// <summary>
        /// 下位机返回数据
        /// </summary>
        string[] dataRecive = new string[16];
        int substrateNum1;
        int substrateNum2;
        string subBar;
        ComponentResourceManager resources = new ComponentResourceManager(typeof(frmInstruMaintenance));
        #endregion

        public frmInstruMaintenance()
        {
            InitializeComponent();
        }

        private void frmInstruMaintenance_Load(object sender, EventArgs e)
        {
            NetCom3.Instance.ReceiveHandel += new Action<string>(Instance_ReceiveHandel);
            rdbtnGeneral.Checked = true;
            fbtnStart.Enabled = true;
            fbtnStop.Enabled = false;
            if (LoginUserType == "0")//admin 用户
            {
                //fbtnInstruMaintenance.Enabled = false;
                fbtnInstruDiagnost.Enabled = false;
                fbtnGroupTest.Enabled = false;
            }
            else if (LoginUserType == "2") // normal 用户
            {
                fbtnInstruDiagnost.Enabled = false;
                fbtnGroupTest.Enabled = false;
            }
            cmbSelectAct.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            cbClearTray.Checked = false;
        }

        void Instance_ReceiveHandel(string obj)
        {
            if (obj.IsNullOrEmpty())
                return;

            else
            {
                BackObj = obj;
            }
        }
        void ControlEnable(bool Flag)
        {
            chbClearWashTube.Enabled = chbPmt.Enabled = chbSamplePipeline.Enabled = chbSubstrate.Enabled = chbSubstrateTest.Enabled
                = chbWashPipeline.Enabled = txtSamplePipeline.Enabled = txtSubPipeline.Enabled = txtWashPipeline.Enabled = chbClearReactTube.Enabled
            = cmbSubPipeCH.Enabled = lblSubPipe2.Enabled = Flag;
        }
        private void rdbtnGeneral_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbtnGeneral.Checked)
            {
                ControlEnable(false);
                txtSamplePipeline.Text = samPerfusionNum.ToString();
                txtSubPipeline.Text = subPerfusionNum.ToString();
                txtWashPipeline.Text = washPerfusionNum.ToString();
                chbClearWashTube.Checked = true;
                chbInit.Checked = true;
                chbPmt.Checked = true;
                chbSamplePipeline.Checked = true;
                chbSubstrate.Checked = true;
                chbSubstrateTest.Checked = true;
                chbWashPipeline.Checked = true;


            }
            else
            {
                ControlEnable(true);
                chbClearWashTube.Checked = false;
                //chbInit.Checked = true;
                chbPmt.Checked = false;
                chbSamplePipeline.Checked = false;
                chbSubstrate.Checked = false;
                chbSubstrateTest.Checked = false;
                chbWashPipeline.Checked = false;
            }
        }
        private void fbtnStart_Click(object sender, EventArgs e)
        {
            if (!ControlInit())
                return;
            fbtnStart.Enabled = false;
            fbtnStop.Enabled = true;
            groupBox1.Enabled = false;//add y 20180510
            rdbtnGeneral.Enabled = rdbtnCustom.Enabled = false;//add y 20180510

            StartThread = new Thread(new ParameterizedThreadStart(MaintenanceStart));// GaTestRun  TestRun
            StartThread.IsBackground = true;
            StartThread.CurrentCulture = Language.AppCultureInfo;
            StartThread.CurrentUICulture = Language.AppCultureInfo;
            StartThread.Start();

        }
        /// <summary>
        /// 维护开始之前各个控件初始化
        /// </summary>
        bool ControlInit()//this function modify y 20180510
        {
            if (!NetCom3.isConnect)
            {
                frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Selectperfusiontimes"));
                return false;
            }
            substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            substrateNum2 = int.Parse(OperateIniFile.ReadIniData("Substrate2", "LeftCount", "0", iniPathSubstrateTube));
            if (rdbtnGeneral.Checked)
            {
                if (substrateNum1 > 0)
                {
                    //cmbSubstrate.SelectedIndex = 0;
                    cmbSubPipeCH.SelectedIndex = 0;
                }
                else
                {
                    //cmbSubstrate.SelectedIndex = 1;
                    cmbSubPipeCH.SelectedIndex = 1;
                }
            }
            else
            {
                if (chbSamplePipeline.Checked)//this block add y 20180510
                {
                    if (txtSamplePipeline.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Selectperfusiontimes"));
                        txtSamplePipeline.Focus();
                        return false;
                    }
                }//this block end
                if (chbWashPipeline.Checked)//this block add y 20180510
                {
                    if (txtWashPipeline.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.SelectCleanperfusiontimes"));
                        txtWashPipeline.Focus();
                        return false;
                    }
                    else
                    {
                        chbClearWashTube.Checked = true;
                        chbClearWashTube.Enabled = false;
                    }
                }//this block end
                if (chbPmt.Checked)//this block add y 20180510
                {
                    if (txtPMT.Text.Trim() == "")
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.PMTParameter"));
                        txtPMT.Focus();
                        return false;
                    }
                }//this block end

                if (chbSubstrate.Checked)
                {
                    if (txtSubPipeline.Text.Trim() == "")//this block add y 20180510
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Selectsubstrateperfusiontimes"));
                        txtSubPipeline.Focus();
                        return false;
                    }//this block end
                    else
                    {
                        chbClearWashTube.Checked = true;
                        chbClearWashTube.Enabled = false;
                    }
                    //if (cmbSubstrate.SelectedItem == null)
                    //{
                    //    frmMsgShow.MessageShow("仪器维护", "请选择底物管路灌注功能的底物管路！");
                    //    cmbSubstrate.Focus();
                    //    return false;
                    //}
                    if (substrateNum1 == 0)
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Substrateempty"));
                        return false;
                    }
                    //if (cmbSubstrate.SelectedIndex == 1 && substrateNum2 == 0)
                    //{
                    //    frmMsgShow.MessageShow("仪器维护", "管路2无底物，请装载！");
                    //    return false;
                    //}
                }
                if (chbSubstrateTest.Checked)
                {
                    if (substrateNum1 == 0)
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Tue1substrate"));
                        return false;
                    }
                    if (substrateNum2 == 0)
                    {
                        frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Tue2substrate"));
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 仪器开始维护具体方法
        /// </summary>
        /// <param name="obj"></param>
        void MaintenanceStart(object obj)
        {
            if (rdbtnGeneral.Checked)
            {
                if (InstruInit())
                {
                    this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Initfinish") + Environment.NewLine); }));
                }
                else
                {
                    this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.InitFalure") + Environment.NewLine); }));
                    goto complate;
                }
                Thread.Sleep(1000);
                //ClearUsedTube();
                washTrayTubeClear();
                Thread.Sleep(1000);
                ClearReactTube();
                Thread.Sleep(1000);
                SamplePipeline();
                Thread.Sleep(1000);
                WashPipeline();
                Thread.Sleep(1000);
                washTrayTubeClear();
                SubstratePipeline(int.Parse(txtSubPipeline.Text.Trim()));
                Thread.Sleep(1000);
                PMTTest();
                Thread.Sleep(1000);
                SubstrateTest();
            }
            else if (rdbtnCustom.Checked)
            {
                //仪器是否初始化
                if (chbInit.Checked)
                {
                    if (InstruInit())
                    {
                        this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Initfinish") + Environment.NewLine); }));
                    }
                    else
                    {
                        this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.InitFalure") + Environment.NewLine); }));
                        goto complate;
                    }
                }
                //清空清洗盘反应管
                if (chbClearWashTube.Checked)
                {
                    //ClearUsedTube();
                    washTrayTubeClear();
                }
                //清空反应盘反应管
                if (chbClearReactTube.Checked)
                {
                    ClearReactTube();
                }
                //是否进行加样管路灌注
                if (chbSamplePipeline.Checked)
                {
                    SamplePipeline();
                }
                //是否进行清洗管路灌注
                if (chbWashPipeline.Checked)
                {
                    WashPipeline();
                }
                //是否进行底物管路灌注
                if (chbSubstrate.Checked)
                {
                    washTrayTubeClear();
                    SubstratePipeline(int.Parse(txtSubPipeline.Text.Trim()));
                }
                //是否进行PMT背景检测
                if (chbPmt.Checked)
                {
                    PMTTest();
                }
                //是否进行底物有效性检测
                if (chbSubstrateTest.Checked)
                {
                    SubstrateTest();
                }
            }
            else if (rdbDaily.Checked)
            {
                //仪器初始化
                if (chbInit.Checked)
                {
                    if (InstruInit())
                    {
                        this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Initfinish") + Environment.NewLine); }));
                    }
                    else
                    {
                        this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.InitFalure") + Environment.NewLine); }));
                    }
                }
                //清空清洗盘反应管
                if (chbClearWashTube.Checked)
                {
                    washTrayTubeClear();
                }
                //进行底物管路灌注
                if (chbSubstrate.Checked)
                {
                    washTrayTubeClear();
                    SubstratePipeline(int.Parse(txtSubPipeline.Text.Trim()));
                }
                //进行底物有效性检测
                if (chbSubstrateTest.Checked)
                {
                    SubstrateTest();
                }
            }
            complate:
            BeginInvoke(new Action(() =>
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                txtInfo.AppendText(GetString("keywordText.Maintenancecomplete") + Environment.NewLine);//add y 20180510
                groupBox1.Enabled = true;//add y 20180510
                rdbtnGeneral.Enabled = rdbtnCustom.Enabled = true;//add y 20180510
            }));
        }
        /// <summary>
        /// 仪器初始化
        /// </summary>
        bool InstruInit()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Initializing") + Environment.NewLine); }));
            //仪器初始化
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
            if (!NetCom3.Instance.SingleQuery())
            {
                return false;
            }
            #region 判断各个模组是否初始化成功
            if (NetCom3.Instance.ErrorMessage != null)
            {
                //2018-09-06 zlx mod
                MessageBox.Show(NetCom3.Instance.ErrorMessage, GetString("keywordText.Tip"));
                return false;
            }
            #endregion
            currentHoleNum = 1;
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            return true;

        }

        /// <summary>
        /// 清空清洗盘的反应管
        /// </summary>
        bool washTrayTubeClear()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Emptycleaning") + Environment.NewLine); }));
            DataTable dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            for (int i = 0; i < dtWashTrayIni.Rows.Count; i++)
            {
                if (i != 0)
                {
                    //清洗盘顺时针旋转一位
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                    if (!NetCom3.Instance.WashQuery())
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return false;
                    }
                    currentHoleNum = currentHoleNum - 1;
                    if (currentHoleNum <= 0)
                    {
                        currentHoleNum = currentHoleNum + WashTrayNum;
                    }
                    OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                    dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
                    DataTable dtTemp = new DataTable();
                    dtTemp = dtWashTrayIni.Copy();
                    //清洗盘状态列表中添加反应盘位置字段，赋值需多赋值一个字段。 
                    for (int j = 1; j < 2; j++)
                        dtWashTrayIni.Rows[dtWashTrayIni.Rows.Count - 1][j] = dtTemp.Rows[0][j];
                    for (int k = 0; k < dtWashTrayIni.Rows.Count - 1; k++)
                    {
                        for (int j = 1; j < 2; j++)
                        {
                            dtWashTrayIni.Rows[k][j] = dtTemp.Rows[k + 1][j];
                        }
                    }

                    OperateIniFile.WriteConfigToFile("[TubePosition]", iniPathWashTrayInfo, dtWashTrayIni);
                }
                #region 移管手取放管位置取管扔废管
                int iNeedCool = 0;
                AgainNewMove:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                LogFile.Instance.Write("==============  " + currentHoleNum + "  扔管");
                if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag != (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                        {
                            goto AgainNewMove;
                        }
                        else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                        {
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                            if (!NetCom3.Instance.MoveQuery())
                            {
                                fbtnStart.Enabled = true;
                                fbtnStop.Enabled = false;
                                return false;
                            }
                            goto AgainNewMove;
                        }
                    }
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return false;
                }
                OperateIniFile.WriteIniData("TubePosition", "No1", "0", iniPathWashTrayInfo);
                #endregion
            }
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Emptycleaningfinish") + Environment.NewLine); }));
            return true;
        }

        /// <summary>
        /// 清空反应盘中的反应管
        /// </summary>
        void ClearReactTube()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Emptycleaningfinishuse") + Environment.NewLine); }));
            DataTable dtReactTrayIni = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            #region 反应盘清除使用过的反应管
            for (int i = 0; i < dtReactTrayIni.Rows.Count; i++)
            {
                if (int.Parse(dtReactTrayIni.Rows[i][1].ToString()) > 1)
                {
                    int pos = int.Parse(dtReactTrayIni.Rows[i][0].ToString().Substring(2));
                    int iNeedCool = 0;
                    AgainNewMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + pos.ToString("x2")), 1);
                    if (!NetCom3.Instance.MoveQuery())
                    {
                        iNeedCool++;
                        if (iNeedCool < 2)
                        {
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                                NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                if (!NetCom3.Instance.SingleQuery())
                                {
                                    fbtnStart.Enabled = true;
                                    fbtnStop.Enabled = false;
                                    return;
                                }
                                goto AgainNewMove;
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                if (!NetCom3.Instance.SingleQuery())
                                {
                                    fbtnStart.Enabled = true;
                                    fbtnStop.Enabled = false;
                                    return;
                                }
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + pos.ToString("x2")), 1);
                                if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    goto nextStep;
                                }
                                else
                                {
                                    fbtnStart.Enabled = true;
                                    fbtnStop.Enabled = false;
                                    return;
                                }
                            }
                        }
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    nextStep:
                    //配置文件修改
                    OperateIniFile.WriteIniData("ReactTrayInfo", "no" + pos, "0", iniPathReactTrayInfo);
                }
            }
            #endregion
        }
        /// <summary>
        /// 加样管路灌注
        /// </summary>
        void SamplePipeline()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Sampleperfusion") + Environment.NewLine); }));
            int Num = int.Parse(txtSamplePipeline.Text.Trim());
            while (Num > 0)
            {

                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 08"), 0);
                if (!NetCom3.Instance.SPQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                Num--;
            }

        }
        /// <summary>
        /// 清洗管路灌注
        /// </summary>
        //2019.5.15  hly add
        void WashPipeline()
        {
            washTrayTubeClear();
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Cleaningpipeline") + Environment.NewLine); }));
            int Num = int.Parse(txtWashPipeline.Text.Trim());
            //注液位置
            int pos1 = 6;
            int pos2 = 10;
            int pos3 = 14;

            #region 注液1位置放管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-6).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum - (1 - pos1);

            if (currentHoleNum <= 0)
            {
                currentHoleNum = WashTrayNum + currentHoleNum;
            }

            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            int iNeedCool = 0;
            AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"),GetString("keywordText.TemporaryDiskStuckTube"));
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #region 取放管成功，相关配置文件修改
            List<int> lisTubeNum = new List<int>();
            lisTubeNum = QueryTubeNum();
            OperateIniFile.WriteIniData("TubePosition", "No6", "1", iniPathWashTrayInfo);
            #endregion
            #endregion

            #region 注液2位置放管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos1 - pos2).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum - (pos1 - pos2);
            if (currentHoleNum <= 0)
            {
                currentHoleNum = WashTrayNum + currentHoleNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());

            iNeedCool = 0;
            AgainNewMove2:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #region 取放管成功 相关配置文件修改
            lisTubeNum = new List<int>();
            lisTubeNum = QueryTubeNum();
            //清洗盘配置文件修改
            OperateIniFile.WriteIniData("TubePosition", "No10", "1", iniPathWashTrayInfo);
            #endregion
            #endregion

            #region 注液3位置放管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos2 - pos3).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum - (pos2 - pos3);
            if (currentHoleNum <= 0)
            {
                currentHoleNum = WashTrayNum + currentHoleNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove3:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove3;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove3;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #region 取放管成功 相关配置文件修改
            lisTubeNum = new List<int>();
            lisTubeNum = QueryTubeNum();
            //清洗盘配置文件修改
            OperateIniFile.WriteIniData("TubePosition", "No14", "1", iniPathWashTrayInfo);
            #endregion
            #endregion

            #region 清洗盘回到原来的位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos3).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum - pos3 + 1;
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            #endregion

            while (Num > 0)
            {
                #region 注液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 11 10"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                #endregion

                #region 清洗盘顺时针旋转1位
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }

                currentHoleNum = currentHoleNum + 1;
                if (currentHoleNum > WashTrayNum)
                {
                    currentHoleNum = currentHoleNum - WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());

                #region 吸液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 01"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                #endregion
                #endregion

                #region 清洗盘逆时针转回注液位置
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (1).ToString("X2")), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }

                currentHoleNum = currentHoleNum - 1;
                if (currentHoleNum <= 0)
                {
                    currentHoleNum = currentHoleNum + WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                #endregion
                Num--;
            }

            #region 扔废管
            #region 清洗盘逆时针旋转16位扔注液3位置反应管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (16).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum + 13;
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove4:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove4;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove4;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep;
                        }
                        else
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            nextStep:
            OperateIniFile.WriteIniData("TubePosition", "No14", "0", iniPathWashTrayInfo);
            #endregion
            #region 清洗盘逆时针旋转4位扔注液2位置反应管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (4).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum - 4;
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove5:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove5;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove5;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep2;
                        }
                        else
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            nextStep2:
            OperateIniFile.WriteIniData("TubePosition", "No10", "0", iniPathWashTrayInfo);
            #endregion
            #region 清洗盘逆时针旋转4位扔注液1位置反应管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (4).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum - 4;
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove6:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove6;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove6;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep3;
                        }
                        else
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            nextStep3:
            OperateIniFile.WriteIniData("TubePosition", "No6", "0", iniPathWashTrayInfo);
            #endregion
            #endregion
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Cleaningpipelinefinish") + Environment.NewLine); }));
        }
        /// <summary>
        /// 查询四个暂存盘中管的个数
        /// </summary>
        /// <returns></returns>
        List<int> QueryTubeNum()
        {
            List<int> lisTubeNum = new List<int>();
            lisTubeNum.Add(int.Parse(OperateIniFile.ReadIniData("Tube", "Pos1", "", iniPathSubstrateTube)));
            lisTubeNum.Add(int.Parse(OperateIniFile.ReadIniData("Tube", "Pos2", "", iniPathSubstrateTube)));
            lisTubeNum.Add(int.Parse(OperateIniFile.ReadIniData("Tube", "Pos3", "", iniPathSubstrateTube)));
            lisTubeNum.Add(int.Parse(OperateIniFile.ReadIniData("Tube", "Pos4", "", iniPathSubstrateTube)));
            return lisTubeNum;
        }
        /// <summary>
        /// 底物管路灌注
        /// </summary>
        //2019.5.15  hly add
        void SubstratePipeline(int num)
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Substrateperfusion") + Environment.NewLine); }));
            int pos1 = 19;
            int Num = num;// int.Parse(txtSubPipeline.Text.Trim());
            string subPipe = "1";
            substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            #region 清洗盘顺时针18位，然后放管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (1 - pos1).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum + (pos1 - 1);
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //夹新管到清洗盘
            int iNeedCool = 0;
            AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #region 取放管成功，相关配置文件修改
            List<int> lisTubeNum = new List<int>();
            lisTubeNum = QueryTubeNum();
            OperateIniFile.WriteIniData("TubePosition", "No19", "1", iniPathWashTrayInfo);
            #endregion
            #endregion
            #region 清洗盘逆时针旋转19位，回到19号孔，加底物位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos1).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum - pos1 + 1;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            #endregion
            while (Num > 0)
            {
                #region 底物注液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 01 " + subPipe + "0"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                if (subPipe == "1")
                {
                    substrateNum1 = substrateNum1 - 1;
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum1.ToString(), iniPathSubstrateTube);
                    string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                    string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                    DbHelperOleDb dbase = new DbHelperOleDb(3);
                    DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");
                    substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                }
                else
                {
                    substrateNum2 = substrateNum2 - 1;
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum2.ToString(), iniPathSubstrateTube);
                }
                #endregion
                #region 清洗盘顺时针旋转2位，底物吸液位置
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-2).ToString("X2").Substring(6, 2)), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                currentHoleNum = currentHoleNum + 2;
                //若当前管号等于0，说明转过来的孔号为30
                if (currentHoleNum > WashTrayNum)
                {
                    currentHoleNum = currentHoleNum - WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                #region 吸液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 01"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }
                #endregion
                #endregion
                #region 清洗盘转回底物注液位置，19号位
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (2).ToString("X2")), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    fbtnStart.Enabled = true;
                    fbtnStop.Enabled = false;
                    return;
                }

                currentHoleNum = currentHoleNum - 2;
                //若当前管号等于0，说明转过来的孔号为30
                if (currentHoleNum <= 0)
                {
                    currentHoleNum = currentHoleNum + WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                #endregion
                Num--;
            }
            #region 加底物位置扔废管
            //清洗盘逆时针旋转12位转到取放管位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (11).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum + 18;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove2:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep;
                        }
                        else
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            nextStep:
            OperateIniFile.WriteIniData("TubePosition", "No19", "0", iniPathWashTrayInfo);
            #endregion
        }
        /// <summary>
        /// PMT背景值检测
        /// </summary>
        //2019.5.15  hly add
        void PMTTest()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.PMTdetection") + Environment.NewLine); }));
            //int PMT = 0;
            BackObj = "";
            //发送单独的读数指令
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 00 01"), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            else
            {
                int delay = 1000;
                while (!BackObj.Contains("EB 90 31 A3") && delay > 0)
                {
                    NetCom3.Delay(10);
                    delay = delay - 10;
                }
                if (BackObj.Contains("EB 90 31 A3"))
                {
                    string temp = BackObj.Substring(BackObj.Length - 16).Replace(" ", "");
                    temp = Convert.ToInt64(temp, 16).ToString();
                    if (double.Parse(temp) > Math.Pow(10, 5))
                        temp = ((int)GetPMT(double.Parse(temp))).ToString();
                    //textReadShow.AppendText(DateTime.Now.ToString("HH-mm-ss") + ": " + "PMT背景值：" + temp + Environment.NewLine);
                    this.BeginInvoke(new Action(() => { txtPMT.Text = temp; }));
                }
            }
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.PMTdetectionfinish") + Environment.NewLine); }));
        }
        /// <summary>
        /// 底物有效性检测
        /// </summary>
        void SubstrateTest()
        {
            this.BeginInvoke(new Action(() => { txtInfo.AppendText(GetString("keywordText.Substrateavailability") + Environment.NewLine); }));
            int subPMT = 0;
            int pos1 = 19;
            string subPipe = "0";
            Invoke(new Action(() =>
            {
                subPipe = "1";
                //if (cmbSubPipeCH.SelectedItem.ToString() == "1")
                //{
                //    subPipe = "1";
                //}
                //else if (cmbSubPipeCH.SelectedItem.ToString() == "2")
                //{
                //    subPipe = "2";
                //}
            }));
            #region 暂存盘取管到清洗盘
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (1 - pos1).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum + (pos1 - 1);
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //夹新管到清洗盘
            int iNeedCool = 0;
            AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #region 取放管成功，相关配置文件修改
            List<int> lisTubeNum = new List<int>();
            lisTubeNum = QueryTubeNum();
            OperateIniFile.WriteIniData("TubePosition", "No19", "1", iniPathWashTrayInfo);
            #endregion
            #endregion
            #region 清洗盘逆时针旋转19位，回到19号孔，加底物位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos1).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum - pos1 + 1;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            #endregion
            #region 底物灌注
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 01 " + subPipe + "0"), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            if (subPipe == "1")
            {
                substrateNum1 = substrateNum1 - 1;
                OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum1.ToString(), iniPathSubstrateTube);
                string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                DbHelperOleDb dbase = new DbHelperOleDb(3);
                DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");
                substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            }
            else
            {
                substrateNum2 = substrateNum2 - 1;
                OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum2.ToString(), iniPathSubstrateTube);
                string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                DbHelperOleDb dbase = new DbHelperOleDb(3);
                DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");
                substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            }
            #endregion
            #region 清洗盘旋转到读数位置读数
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (6).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            currentHoleNum = currentHoleNum + 6;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //发送单独的读数指令
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 00 01"), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            #endregion
            string temp = BackObj.Substring(BackObj.Length - 16).Replace(" ", "");
            temp = Convert.ToInt64(temp, 16).ToString();
            subPMT = int.Parse(temp);
            if (subPMT > Math.Pow(10, 5))
                subPMT = (int)GetPMT(double.Parse(subPMT.ToString()));
            this.BeginInvoke(new Action(() => { txtSubTest.Text = subPMT.ToString(); }));
            #region 读数完成25号位置扔废管
            //清洗盘逆时针旋转5位转到取放管位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (5).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }

            currentHoleNum = currentHoleNum + 6;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove2:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep;
                        }
                        else
                        {
                            fbtnStart.Enabled = true;
                            fbtnStop.Enabled = false;
                            return;
                        }
                    }
                    else
                    {
                        fbtnStart.Enabled = true;
                        fbtnStop.Enabled = false;
                        return;
                    }
                }
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                return;
            }
            nextStep:
            OperateIniFile.WriteIniData("TubePosition", "No19", "0", iniPathWashTrayInfo);
            #endregion
            //this.BeginInvoke(new Action(() => { txtSubTest.Text = subPMT.ToString(); }));
        }

        private void fbtnStop_Click(object sender, EventArgs e)
        {
            StartThread.Abort();
            fbtnStart.Enabled = true;//add y 20180510
            fbtnStop.Enabled = false;//add y 20180510
            txtInfo.AppendText(GetString("keywordText.Maintenancepause") + Environment.NewLine);//add y 20180510
            groupBox1.Enabled = true;//add y 20180510
            rdbtnGeneral.Enabled = rdbtnCustom.Enabled = true;//add y 20180510
        }
        private void fbtnReturn_Click(object sender, EventArgs e)
        {
            if (isNewWashRun)
            {
                frmMessageShow frmMessage = new frmMessageShow();
                frmMessage.MessageShow(GetString("fbtnInstruMaintenance.Text"), GetString("keywordText.Idleoperations"));
                return;
            }
            this.Hide();
        }

        private void fbtnInstruDiagnost_Click(object sender, EventArgs e)
        {
            if (isNewWashRun)
            {
                frmMessageShow frmMessage = new frmMessageShow();
                frmMessage.MessageShow(GetString("fbtnInstruMaintenance.Text"), GetString("keywordText.Idleoperations"));
                return;
            }
            if (!CheckFormIsOpen("frmDiagnost"))
            {
                frmDiagnost frnID = new frmDiagnost();
                frnID.TopLevel = false;
                frnID.Parent = this.Parent;
                frnID.Show();
            }
            else
            {
                frmDiagnost frnID = (frmDiagnost)Application.OpenForms["frmDiagnost"];
                frnID.BringToFront();
            }
        }
        private void fbtnGroupTest_Click(object sender, EventArgs e)
        {
            if (isNewWashRun)
            {
                frmMessageShow frmMessage = new frmMessageShow();
                frmMessage.MessageShow(GetString("fbtnInstruMaintenance.Text"), GetString("keywordText.Idleoperations"));
                return;
            }
            if (!CheckFormIsOpen("frmInstruGroupTest"))
            {
                frmInstruGroupTest frnIGT = new frmInstruGroupTest();
                frnIGT.TopLevel = false;
                frnIGT.Parent = this.Parent;
                frnIGT.Show();
            }
            else
            {
                frmInstruGroupTest frnIGT = (frmInstruGroupTest)Application.OpenForms["frmInstruGroupTest"];
                frnIGT.BringToFront();

            }
        }
        private void frmInstruMaintenance_SizeChanged(object sender, EventArgs e)
        {
            formSizeChange(this);
        }

        private void frmInstruMaintenance_FormClosed(object sender, FormClosedEventArgs e)
        {
            NetCom3.Instance.ReceiveHandel -= new Action<string>(Instance_ReceiveHandel);
        }

        private void functionButton1_Click(object sender, EventArgs e)//20180516 y 仪器初始化点击事件
        {
            if (isNewWashRun)
            {
                frmMessageShow frmMessage = new frmMessageShow();
                frmMessage.MessageShow(GetString("fbtnInstruMaintenance.Text"), GetString("keywordText.Idleoperations"));
                return;
            }
            //int X = Convert.ToInt32((this.Width - dfInitializers.Width) / 2);
            //int Y = Convert.ToInt32((this.Height - dfInitializers.Height)/2);
            //dfInitializers.Location = new Point(X, Y);
            functionButton1.Enabled = false;
            dfInitializers.Visible = true;
            //上下位机连接
            BeginInvoke(new Action(() =>
            {
                pbinitializers.Value = 20;
                lainitializers.Text = GetString("keywordText.connect") + " " + pbinitializers.Value.ToString() + "%";
            }));
            if (!NetCom3.isConnect)
            {
                if (NetCom3.Instance.CheckMyIp_Port_Link())
                {
                    NetCom3.Instance.ConnectServer();

                    if (!NetCom3.isConnect)
                        goto complete;

                }
            }

            //仪器初始化
            BeginInvoke(new Action(() =>
            {
                pbinitializers.Value = 50;
                lainitializers.Text = GetString("keywordText.Init") + " " + pbinitializers.Value.ToString() + "%";
            }));
            Array.Clear(dataRecive, 0, 15);//2018-09-17
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
            if (!NetCom3.Instance.SingleQuery())
            {
                goto complete;
            }
            currentHoleNum = int.Parse(OperateIniFile.ReadInIPara("OtherPara", "washCurrentHoleNum"));
            #region 判断各个模组是否初始化成功

            if (NetCom3.Instance.ErrorMessage != null)
            {
                //2018-09-06 zlx mod
                frmMsgShow.MessageShow(GetString("keywordText.Init"), NetCom3.Instance.ErrorMessage);
                goto complete;
            }
            #endregion

            BeginInvoke(new Action(() =>
            {
                pbinitializers.Value = 100;
                lainitializers.Text = GetString("keywordText.Init") + " " + pbinitializers.Value.ToString() + "%";
            }));
            NetCom3.Delay(2000);
            complete:
            dfInitializers.Visible = false;
            functionButton1.Enabled = true;
        }

        private void RdbDaily_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbDaily.Checked)
            {
                chbClearWashTube.Checked = true;
                chbClearWashTube.Enabled = false;
                chbClearReactTube.Enabled = false;
                chbSamplePipeline.Enabled = false;
                txtSamplePipeline.Enabled = false;
                chbWashPipeline.Enabled = false;
                txtWashPipeline.Enabled = false;
                chbPmt.Enabled = false;
                txtPMT.Enabled = false;
                chbSubstrate.Checked = true;
                chbSubstrate.Enabled = false;
                txtSubPipeline.Text = "15";
                txtSubPipeline.Enabled = false;
                cmbSubstrate.SelectedIndex = 0;
                cmbSubstrate.Enabled = false;
                chbSubstrateTest.Checked = true;
                chbSubstrateTest.Enabled = false;
                //txtSubTest.Enabled = false;
                cmbSubPipeCH.SelectedIndex = 0;
                cmbSubPipeCH.Enabled = false;
                fbtnStart.Enabled = true;
                fbtnStop.Enabled = false;
                rdbtnGeneral.Enabled = rdbtnCustom.Enabled = true;
            }
            else
            {
                ControlEnable(true);
                txtSubPipeline.Text = "5";
            }
        }


        #region 新添加的日检测代码
        bool isAddBase = false;
        private void functionButton2_Click(object sender, EventArgs e)
        {
            if (isNewWashRun)
            {
                MessageBox.Show(GetString("keywordText.Idleoperations"));
                return;
            }
            int basenum = (int)numericUpDown4.Value;
            string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
            CancellationToken = false;
            int leftcount1 = int.Parse(LeftCount1);
            if (leftcount1 < basenum)
            {
                MessageBox.Show(GetString("keywordText.Substrateinsufficient"));
                return;
            }
            functionButton1.Enabled = false;
            isAddBase = true;

            for (int i = 1; i <= basenum; i++)
            {
                if (isNewWashEnd()) return;  //lyq add 20190822
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 07 07"), 5);
                if (!NetCom3.Instance.SingleQuery())
                {
                    functionButton1.Enabled = true;
                    return;
                }
                leftcount1 = leftcount1 - 1;
                OperateIniFile.WriteIniData("Substrate1", "LeftCount", leftcount1.ToString(), iniPathSubstrateTube);
                string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                DbHelperOleDb dbase = new DbHelperOleDb(3);
                DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");
                substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                numericUpDown4.Value = basenum - i;
                Thread.Sleep(500);
            }
            functionButton1.Enabled = true;
            isAddBase = false;
        }

        bool isNewWashRun = false;
        bool CancellationToken = false;
        bool bLoopRun = false;
        int LoopPourinto = 0;
        int isNewCleanTray = 1;
        void TestLoopRun()
        {
            //BeginInvoke(new Action(() => { textBox1.Text = ""; }));
            #region 清空清洗盘
            TExtAppend(GetString("keywordText.Clearclean"));
            for (int i = 0; i < WashTrayNum; i++)
            {
                if (isNewWashEnd()) return;  //lyq add 20190822
                if (!bLoopRun) return;
                //打印当前取放管孔位到Log。  jun 2019/1/22
                LogFile.Instance.Write(string.Format("***{0}->:{1}***", "取放管时间: " + DateTime.Now.ToString("HH:mm:ss:fff"), "管孔位置: " + tubeHoleNum));
                int iNeedCool = 0;
                AgainNewMove:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                if (!NetCom3.Instance.MoveQuery())
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        goto nextStep;
                    }
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                        {
                            goto AgainNewMove;
                        }
                        else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                        {
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                            if (!NetCom3.Instance.SingleQuery())
                            {
                                NewWashEnd(1);
                                return;
                            }
                            goto AgainNewMove;
                        }
                        else
                        {
                            NewWashEnd(1);
                            return;
                        }
                    }
                    NewWashEnd(1);
                    return;
                }
                nextStep:
                if (isNewWashEnd()) return;  //lyq add 20190822
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                //逆时针计数。  Jun 2019/1/22
                if (tubeHoleNum <= 0)
                {
                    tubeHoleNum = tubeHoleNum + WashTrayNum;
                }
                tubeHoleNum = tubeHoleNum - 1;
                if (!NetCom3.Instance.WashQuery())
                {
                    NewWashEnd(3);
                    return;
                }
            }
            #endregion
            TExtAppend(GetString("keywordText.Clearcleanfinish"));
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!AddTubeInCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Add1"));
            CleanTrayMovePace(5);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!AddTubeInCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Add2"));
            CleanTrayMovePace(4);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!AddTubeInCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Add3"));
            CleanTrayMovePace(4);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!AddTubeInCleanTray())
            {
                NewWashEnd();
                return;
            }
            TExtAppend(GetString("keywordText.Add4"));
            CleanTrayMovePace(5 + isNewCleanTray);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            TExtAppend(GetString("keywordText.Perfusionbegins"));
            for (int i = 0; i < LoopPourinto; i++)
            {
                if (bLoopClick)
                    BeginInvoke(new Action(() => { numRepeat.Value = LoopPourinto - i; }));
                if (isNewWashEnd()) return;  //lyq add 20190822
                //底物灌注改为1次
                for (int index = 0; index < 1; index++)
                {
                    if (!bLoopRun)
                    {
                        NewWashEnd();
                        return;
                    }
                    CleanTrayWash(1);
                    CleanTrayMovePace(-1);
                    CleanTrayWash(2);
                    CleanTrayMovePace(-1);
                    CleanTrayWash(2);
                    CleanTrayMovePace(2);
                }
            }
            TExtAppend(GetString("keywordText.Perfusionfinish"));
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            CleanTrayMovePace(-5 - isNewCleanTray);
            if (!RemoveTubeOutCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Throw1"));
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            CleanTrayMovePace(-4);
            if (!RemoveTubeOutCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Throw2"));
            CleanTrayMovePace(-4);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!RemoveTubeOutCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Throw3"));
            CleanTrayMovePace(-5);
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (!RemoveTubeOutCleanTray())
            {
                NewWashEnd(1);
                return;
            }
            TExtAppend(GetString("keywordText.Throw4"));
            NewWashEnd();
            TExtAppend(GetString("keywordText.Circulationcompleted"));
        }
        private void functionButton4_Click(object sender, EventArgs e)
        {
            if (checkBox7.Checked)
            {
                if (!string.IsNullOrEmpty(numDwPourin.Value.ToString()))
                {
                    if (string.IsNullOrEmpty(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube)))
                    {
                        MessageBox.Show(GetString("keywordText.NoSubstrate"));
                        return;
                    }
                    int left = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                    if (left < int.Parse(numDwPourin.Value.ToString()))
                    {
                        MessageBox.Show(GetString("keywordText.NoSubstrate"));
                        return;
                    }
                }
            }
            textBox1.Text = string.Empty;
            //清空，新管，清洗，底物，读数，扔管
            if (isNewWashRun)
            {
                MessageBox.Show(GetString("keywordText.Runingnow"));
                return;
            }
            bool[] flow = new bool[6] { checkBox6.Checked, checkBox1.Checked, checkBox2.Checked, checkBox3.Checked, checkBox4.Checked, checkBox5.Checked };
            int starhole;
            int whichPipe = 1;
            subBar = OperateIniFile.ReadIniData("Substrate1", "BarCode", "", iniPathSubstrateTube);
            if (!(int.TryParse(comboBox2.Text, out starhole)))// && (flow[3] && int.TryParse(comboBox1.Text, out whichPipe) || !flow[3])
            {
                MessageBox.Show(GetString("keywordText.Parameterincorrect"));
                return;
            }
            if (flow[2] && !flow[3])
            {
                MessageBox.Show(GetString("keywordText.Cleansubstrate"));
                return;
            }
            if (checkBox3.Checked)
            {
                if (string.IsNullOrEmpty(OperateIniFile.ReadIniData("Substrate" + whichPipe + "", "LeftCount", "", iniPathSubstrateTube)))
                {
                    MessageBox.Show(GetString("keywordText.Insufficientsubstrate"));
                    return;
                }
                int LeftCount1 = int.Parse(OperateIniFile.ReadIniData("Substrate" + whichPipe + "", "LeftCount", "", iniPathSubstrateTube));
                int CleanTray = 0;
                if (cbCleanTray.Checked)
                {
                    CleanTray = 1;
                }
                if (LeftCount1 < numericUpDown3.Value + CleanTray)
                {
                    MessageBox.Show(GetString("keywordText.Insufficientsubstrate"));
                    return;
                }
            }
            int tubeNum = (int)numericUpDown3.Value;
            int repeatRead = (int)numericUpDown2.Value;

            isNewWashRun = true;
            CancellationToken = false;
            functionButton4.Enabled = false;
            functionButton5.Enabled = true;
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            textBox1.Invoke(new Action(() => { textBox1.Text = ""; }));
            frmMain.BQLiquaid = false;
            TExtAppend(GetString("keywordText.Initializing"));
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
            if (!NetCom3.Instance.SingleQuery())
            {
                NewWashEnd();
                return;
            }
            #region 判断各个模组是否初始化成功
            if (NetCom3.Instance.ErrorMessage != null)
            {
                //2018-09-06 zlx mod
                TExtAppend(GetString("keywordText.Init") + ":" + NetCom3.Instance.ErrorMessage);
                //frmMsgShow.MessageShow(GetString("keywordText.Init"), NetCom3.Instance.ErrorMessage);
                NewWashEnd();
                return;
            }
            TExtAppend(GetString("keywordText.Initfinish"));
            frmMain.BQLiquaid = true;
            #endregion
            if (cbClearTray.Checked)
            {
                TExtAppend(GetString("keywordText.CleanIncubate"));
                if (!reactTrayTubeClear())
                {
                    if(!isNewWashEnd())
                        NewWashEnd(1);
                    return;
                }
                TExtAppend(GetString("keywordText.CleanIncubatefinish"));
            }
            if (isNewWashEnd()) return;  //lyq add 20190822
            if (flow[0])//清空
            {
                TExtAppend(GetString("keywordText.Startclean"));
                tubeHoleNum = starhole;
                for (int i = 0; i < WashTrayNum; i++)
                {
                    if (isNewWashEnd()) return;  //lyq add 20190822
                                                 //打印当前取放管孔位到Log。  jun 2019/1/22
                    LogFile.Instance.Write(string.Format("***{0}->:{1}***", "取放管时间: " + DateTime.Now.ToString("HH:mm:ss:fff"), "管孔位置: " + tubeHoleNum));
                    int iNeedCool = 0;
                    AgainNewMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                    if (!NetCom3.Instance.MoveQuery())
                    {
                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep;
                        }
                        iNeedCool++;
                        if (iNeedCool < 2)
                        {
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                            {
                                goto AgainNewMove;
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                if (!NetCom3.Instance.SingleQuery())
                                {
                                    NewWashEnd(1);
                                    return;
                                }
                                goto AgainNewMove;
                            }
                            else
                            {
                                NewWashEnd(1);
                                return;
                            }
                        }
                        NewWashEnd(1);
                        return;
                    }
                    nextStep:
                    if (isNewWashEnd()) return;  //lyq add 20190822
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                    //逆时针计数。  Jun 2019/1/22
                    if (tubeHoleNum <= 0)
                    {
                        tubeHoleNum = tubeHoleNum + WashTrayNum;
                    }
                    tubeHoleNum = tubeHoleNum - 1;
                    if (!NetCom3.Instance.WashQuery())
                    {
                        NewWashEnd(3);
                        return;
                    }
                    if (isNewWashEnd()) return;  //lyq add 20190822
                }
                TExtAppend(GetString("keywordText.Startcleanfinish"));
            }
            if (checkBox7.Checked)
            {
                SubstratePipeline();
            }
            if (isNewWashEnd()) return;  //lyq add 20190822
            TExtAppend(GetString("keywordText.Gotohole") + starhole);
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 02 " + (starhole).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                NewWashEnd(3);
                return;
            }
            CleanTray tray = new CleanTray(starhole);
            int temptubenum = tubeNum;
            int tempread = 1;
            int tubecount = 1;
            for (int i = 0; i < WashTrayNum + tubeNum; i++)
            {
                if (isNewWashEnd()) return;
                if (temptubenum > 0)//夹新管
                {
                    if (flow[1])
                    {
                        TExtAppend(string.Format(GetString("keywordText.Adding"),(tubeNum - temptubenum + 1)));
                        int iNeedCool = 0;
                        AgainNewMove:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            iNeedCool++;
                            if (iNeedCool < 2)
                            {
                                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                                    NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                    if (!NetCom3.Instance.SingleQuery())
                                    {
                                        NewWashEnd(1);
                                        return;
                                    }
                                    goto AgainNewMove;
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                                {
                                    goto AgainNewMove;
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                                {
                                    frmMessageShow frmMessage = new frmMessageShow();
                                    frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                                    NewWashEnd(1);
                                    return;
                                }
                                else
                                {
                                    NewWashEnd(1);
                                    return;
                                }
                            }
                            NewWashEnd(1);
                            return;
                        }
                    }
                    tray.pointer[0].Value[1] = 1;
                    temptubenum--;
                }
                if (isNewWashEnd()) return;

                if (tray.needStay(flow[2], flow[3], flow[4]))//清洗盘工作
                {
                    //dw2018.12.25 
                    if (flow[4] && tray.pointer[9].Value[1] == 1 && tempread <= tubeNum)
                    //if (flow[4] && tray.pointer[9].Value[1] == 1)
                    {
                        NetCom3.Instance.ReceiveHandel += GetReadNum2;
                        TExtAppend(string.Format(GetString("keywordText.Holereading"), tempread));
                        //TExtAppend(GetString("keywordText.The") + tempread + GetString("keywordText.Holereading"));
                        tempread++;
                    }
                    NetCom3.Instance.Send(NetCom3.Cover(tray.GetWashOrder(flow[2], flow[3], flow[4], whichPipe)), 2);
                    if (!NetCom3.Instance.WashQuery())
                    {
                        NewWashEnd(3);
                        return;
                    }
                    else
                    {
                        if (flow[3] && tray.pointer[8].Value[1] == 1)
                        {
                            string LeftCount1 = OperateIniFile.ReadIniData("Substrate" + whichPipe + "", "LeftCount", "", iniPathSubstrateTube);
                            OperateIniFile.WriteIniData("Substrate" + whichPipe + "", "LeftCount", (int.Parse(LeftCount1) - 1).ToString(), iniPathSubstrateTube);
                            DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + (int.Parse(LeftCount1) - 1).ToString() + " where BarCode = '"
                                              + subBar + "'");
                        }
                    }
                    if (flow[4] && tray.pointer[9].Value[1] == 1)//重复读数
                    {
                        for (int j = 1; j < repeatRead; j++)
                        {
                            if (isNewWashEnd()) return;  //lyq add 20190822
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 00 11"), 2);
                            if (!NetCom3.Instance.WashQuery())
                            {
                                NewWashEnd(3);
                                return;
                            }
                        }
                        Thread.Sleep(500);
                        NetCom3.Instance.ReceiveHandel -= GetReadNum2;
                    }
                }
                if (isNewWashEnd()) return;  //lyq add 20190822
                if (flow[5] && tray.pointer[10].Value[1] == 1)
                {
                    TExtAppend(string.Format(GetString("keywordText.Throwthe"), tubecount));
                    //TExtAppend(GetString("keywordText.Throwthe") + tubecount + GetString("keywordText.gxg"));
                    int iNeedCool = 0;
                    AgainNewMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 01"), 1);
                    if (!NetCom3.Instance.MoveQuery())
                    {
                        iNeedCool++;
                        if (iNeedCool < 2)
                        {
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                                if (!NetCom3.Instance.MoveQuery())
                                {
                                    NewWashEnd(1);
                                    return;
                                }
                                goto AgainNewMove;
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                            {
                                goto AgainNewMove;
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                if (!NetCom3.Instance.SingleQuery())
                                {
                                    NewWashEnd(1);
                                    return;
                                }
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 01"), 1);
                                if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    goto nextStep;
                                }
                                else
                                {
                                    NewWashEnd(1);
                                    return;
                                }
                            }
                            else
                            {
                                NewWashEnd(1);
                                return;
                            }
                        }
                        NewWashEnd(1);
                        return;
                    }
                    nextStep:
                    tray.pointer[10].Value[1] = 0;
                    tubecount++;
                }

                if (isNewWashEnd()) return;
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (1).ToString("X2")), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    NewWashEnd(3);
                    return;
                }
                tray.PoninterMoveOnePace();
                if (chkDelay.Checked)
                    NetCom3.Delay(20000);
                else
                    Thread.Sleep(500);
            }
            if (cbCleanTray.Checked)
            {
                LoopPourinto = 1;
                bLoopRun = true;
                TExtAppend(GetString("keywordText.Pipelinefillstart"));
                TestLoopRun();
                //functionButton6_Click(sender, e);
            }
            NewWashEnd();
        }
        /// <summary>
        /// 底物管路灌注  /2019.8.5  zlx  add
        /// </summary>
        void SubstratePipeline()
        {
            if (isNewWashEnd()) return;  //lyq add 20190822
            substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            TExtAppend(GetString("keywordText.Substrateperfusion"));
            int pos1 = 19;
            //暂存盘取管位置
            //int Num = 20;
            int Num = int.Parse(numDwPourin.Value.ToString());
            if (Num == 0)
                Num = 20;
            string subPipe = "";
            subPipe = "1";
            subBar = OperateIniFile.ReadIniData("Substrate" + subPipe, "BarCode", "", iniPathSubstrateTube);
            if (isNewWashEnd()) return;  //lyq add 20190822
            #region 清洗盘顺时针18位，然后放管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (1 - pos1).ToString("X2").Substring(6, 2)), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                return;
            }
            currentHoleNum = currentHoleNum + (pos1 - 1);
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //OperateIniFile.WriteIniData("TubePosition", "No19", "1", iniPathWashTrayInfo);
            int iNeedCool = 0;
            //夹新管到清洗盘
            AgainNewMove:
            if (isNewWashEnd()) return;
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 06 "), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                        NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            NewWashEnd(1);
                            return;
                        }
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        goto AgainNewMove;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                    {
                        frmMessageShow frmMessage = new frmMessageShow();
                        frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                        NewWashEnd(1);
                        return;
                    }
                    else
                    {
                        NewWashEnd(1);
                        return;
                    }
                }
                NewWashEnd(1);
                return;
            }
            #region 取放管成功，相关配置文件修改
            OperateIniFile.WriteIniData("TubePosition", "No19", "1", iniPathWashTrayInfo);
            #endregion
            #endregion
            if (isNewWashEnd()) return;  //lyq add 20190822
            #region 清洗盘逆时针旋转19位，回到19号孔，加底物位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (pos1).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                return;
            }
            currentHoleNum = currentHoleNum - pos1 + 1;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            #endregion
            while (Num > 0)
            {
                if (isNewWashEnd()) return;  //lyq add 20190822
                #region 底物注液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 00 01 " + subPipe + "0"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    //fbtnStart.Enabled = true;
                    //fbtnStop.Enabled = false;
                    return;
                }
                if (subPipe == "1")
                {
                    substrateNum1 = substrateNum1 - 1;
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum1.ToString(), iniPathSubstrateTube);
                    DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + substrateNum1.ToString() + " where BarCode = '"
                                         + subBar + "'");
                    LogFile.Instance.Write("当前剩余底物" + substrateNum1 + "\n");
                }
                else
                {
                    substrateNum2 = substrateNum2 - 1;
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum2.ToString(), iniPathSubstrateTube);
                    DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + substrateNum2.ToString() + " where BarCode = '"
                                          + subBar + "'");
                }
                #endregion
                if (isNewWashEnd()) return;  //lyq add 20190822
                #region 清洗盘顺时针旋转2位，底物吸液位置
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-2).ToString("X2").Substring(6, 2)), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    return;
                }
                currentHoleNum = currentHoleNum + 2;
                //若当前管号等于0，说明转过来的孔号为30
                if (currentHoleNum > WashTrayNum)
                {
                    currentHoleNum = currentHoleNum - WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                #region 吸液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 01"), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    return;
                }
                #endregion
                #endregion
                if (isNewWashEnd()) return;  //lyq add 20190822
                #region 清洗盘转回底物注液位置，19号位
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (2).ToString("X2")), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    return;
                }

                currentHoleNum = currentHoleNum - 2;
                //若当前管号等于0，说明转过来的孔号为30
                if (currentHoleNum <= 0)
                {
                    currentHoleNum = currentHoleNum + WashTrayNum;
                }
                OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                #endregion
                Num--;
                numDwPourin.Value = Num;
                Thread.Sleep(500);
            }
            if (isNewWashEnd()) return;  //lyq add 20190822
            #region 加底物位置扔废管
            //清洗盘逆时针旋转12位转到取放管位置
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (11).ToString("X2")), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                return;
            }

            currentHoleNum = currentHoleNum + 18;
            //若当前管号等于0，说明转过来的孔号为30
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            iNeedCool = 0;
            AgainNewMove2:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                iNeedCool++;
                if (iNeedCool < 2)
                {
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            NewWashEnd(1);
                            return;
                        }
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        goto AgainNewMove2;
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            NewWashEnd(1);
                            return;
                        }
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                        if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                        {
                            goto nextStep;
                        }
                        else
                        {
                            NewWashEnd(1);
                            return;
                        }
                    }
                    else
                    {
                        NewWashEnd(1);
                        return;
                    }
                }
                NewWashEnd(1);
                return;
            }
            nextStep:
            OperateIniFile.WriteIniData("TubePosition", "No19", "0", iniPathWashTrayInfo);
            #endregion
            TExtAppend(GetString("keywordText.Substrateperfusionfinish"));
        }
        void TExtAppend(string text)
        {

            while (!this.IsHandleCreated)
            {
                Thread.Sleep(15);
            }
            textBox1.Invoke(new Action(() => { textBox1.AppendText(Environment.NewLine + text); }));
        }
        private void SetCultureInfo()
        {
            Language.AppCultureInfo = new System.Globalization.CultureInfo(GetCultureInfo());
            //Language.AppCultureInfo = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = Language.AppCultureInfo;
            System.Threading.Thread.CurrentThread.CurrentUICulture = Language.AppCultureInfo;
        }
        private static string GetCultureInfo()
        {
            if (OperateIniFile.ReadInIPara("CultureInfo", "Culture") == "en")
            {
                return "en";
            }

            return "zh-CN";
        }
        /// <summary>
        /// 温育反应盘清管
        /// </summary>
        bool reactTrayTubeClear()
        {
            for (int i = 1; i <= frmParent.ReactTrayNum; i++)
            {
                int iNeedCool = 0;
                AgainNewMove:
                if (isNewWashEnd()) return false;  //lyq add 20190822
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + i.ToString("x2")), 1);
                if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag != (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked ||
                           NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                        {
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                            if (!NetCom3.Instance.SingleQuery())
                            {
                                return false;
                            }
                            goto AgainNewMove;
                        }
                    }
                    MessageBox.Show(GetString("keywordText.Handexception"));
                    return false;
                }
                //修改反应盘信息
                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + i.ToString(), "0", iniPathReactTrayInfo);
            }
            return true;
        }
        bool isNewWashEnd()
        {
            if (CancellationToken)
            {
                //NewWashEnd();
                return true;
            }
            else return false;
        }
        void NewWashEnd(int errorflag = 0)
        {
            if (errorflag == 1)
            {
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    TExtAppend(GetString("keywordText.Handtakehit"));
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                    TExtAppend(GetString("keywordText.Handputhit"));
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.LackTube)
                    TExtAppend(GetString("keywordText.EMptytube"));
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                    TExtAppend(GetString("keywordText.Handovertime"));
            }
            else if (errorflag == 3)
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                    TExtAppend(GetString("keywordText.Cleanovertime"));
            }
            else if (errorflag == 2)
            {
                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                    TExtAppend(GetString("keywordText.Sampleovertime"));
                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked)
                    TExtAppend(GetString("keywordText.Samplehit"));
            }
            isNewWashRun = false;
            CancellationToken = true; //lyq add 20190822
            frmMain.BQLiquaid = true;
            NetCom3.Instance.ReceiveHandel -= GetReadNum2;
            if (bLoopRun)
            {
                btnPourInto.Enabled = false;
                BeginInvoke(new Action(() => { btnPourInto.Text = GetString("keywordText.implement"); }));
                bLoopRun = false;
                btnPourInto.Enabled = true;
            }
            else
            {
                functionButton4.Enabled = true;
                functionButton5.Enabled = false;
            }
            TExtAppend(GetString("keywordText.Over"));
        }

        private void GetReadNum2(string order)
        {
            if (order.Contains("EB 90 31 A3"))
            {
                SetCultureInfo();
                string temp = order.Replace(" ", "");
                int pos = temp.IndexOf("EB9031A3");
                temp = temp.Substring(pos, 32);
                temp = temp.Substring(temp.Length - 8);
                temp = Convert.ToInt64(temp, 16).ToString();
                if (int.Parse(temp) > Math.Pow(10, 5))
                    temp = ((int)GetPMT(double.Parse(temp))).ToString();
                TExtAppend(DateTime.Now.ToString("HH-mm-ss") + ": " + GetString("keywordText.PMTValue") + temp);
            }
        }
        class CleanTray
        {
            LinkedList<int[]> tray = new LinkedList<int[]>();
            public List<LinkedListNode<int[]>> pointer { get; set; }

            public CleanTray()
            {
                pointer = new List<LinkedListNode<int[]>>(11);
                for (int i = 0; i < WashTrayNum; i++)
                {
                    int[] temp = new int[2] { i + 1, 0 };
                    LinkedListNode<int[]> tempnode = new LinkedListNode<int[]>(temp);
                    tray.AddLast(tempnode);
                    if (i == 0) pointer.Add(tempnode);
                    if (i == 5) pointer.Add(tempnode);
                    if (i == 6) pointer.Add(tempnode);
                    if (i == 9) pointer.Add(tempnode);
                    if (i == 10) pointer.Add(tempnode);
                    if (i == 13) pointer.Add(tempnode);
                    if (i == 14) pointer.Add(tempnode);
                    if (i == 17) pointer.Add(tempnode);
                    if (i == 19) pointer.Add(tempnode);
                    if (i == 24) pointer.Add(tempnode);
                    if (i == 28) pointer.Add(tempnode);
                }
            }
            public CleanTray(int starhole)
                : this()
            {
                while (starhole != pointer[0].Value[0])
                {
                    PoninterMoveOnePace();
                }
            }
            public void PoninterMoveOnePace()
            {
                for (int i = 0; i < 11; i++)
                {
                    pointer[i] = pointer[i].Previous;
                    if (pointer[i] == null) pointer[i] = tray.Last;
                }
            }

            public bool needStay(bool wash, bool addbase, bool read)
            {
                bool stay = false;
                if (wash)
                {
                    for (int i = 1; i < 8; i++)
                    {
                        if (pointer[i].Value[1] == 1)
                        {
                            stay = true;
                            return stay;
                        }
                    }
                }
                if (addbase && pointer[8].Value[1] == 1 || (read && pointer[9].Value[1] == 1))
                {
                    stay = true;
                    return stay;
                }
                return stay;
            }
            public string GetWashOrder(bool wash, bool addbase, bool read, int washPipe)
            {
                StringBuilder order = new StringBuilder(64);
                order.Append("EB 90 31 03 03");
                if (wash && (pointer[1].Value[1] == 1 || pointer[3].Value[1] == 1 || pointer[5].Value[1] == 1 || pointer[7].Value[1] == 1))
                {
                    order.Append(" 01 ");
                }
                else order.Append(" 00 ");
                if (wash && pointer[2].Value[1] == 1)
                {
                    order.Append("1");
                }
                else order.Append("0");
                if (wash && pointer[4].Value[1] == 1)
                {
                    order.Append("1 ");
                }
                else order.Append("0 ");
                if (wash && pointer[6].Value[1] == 1)
                {
                    order.Append("1");
                }
                else order.Append("0");
                if (addbase && pointer[8].Value[1] == 1)
                {
                    order.Append("1 ");
                }
                else order.Append("0 ");
                order.Append(washPipe);
                if (read && pointer[9].Value[1] == 1)
                {
                    order.Append("1");
                }
                else order.Append("0");
                return order.ToString();
            }
        }
        void CleanTrayMovePace(int pace)
        {
            DataTable dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            string order;
            if (pace > 0)
            {
                order = "EB 90 31 03 01 " + (pace).ToString("X2");
            }
            else if (pace < 0)
            {
                order = "EB 90 31 03 01 " + (pace).ToString("X2").Substring(6, 2);
            }
            else return;
            AgainNewMove:
            if (isNewWashEnd()) return;  //lyq add 20190822
            NetCom3.Instance.Send(NetCom3.Cover(order), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                    goto AgainNewMove;
                else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " +GetString("keywordText.Cleanovertime"));
                    frmMsgShow.MessageShow(GetString("keywordText.WashErr"), GetString("keywordText.Overtimepause"));
                }
                else
                    return;

            }
            countWashHole(pace);
            currentHoleNum = currentHoleNum - 1;
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            if (currentHoleNum <= 0)
            {
                currentHoleNum = currentHoleNum + WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            DataTable dtTemp = new DataTable();
            dtTemp = dtWashTrayIni.Copy();
            for (int j = 1; j < 2; j++)
                dtWashTrayIni.Rows[dtWashTrayIni.Rows.Count - 1][j] = dtTemp.Rows[0][j];
            for (int k = 0; k < dtWashTrayIni.Rows.Count - 1; k++)
            {
                for (int j = 1; j < 2; j++)
                {
                    dtWashTrayIni.Rows[k][j] = dtTemp.Rows[k + 1][j];
                }
            }
            OperateIniFile.WriteConfigToFile("[TubePosition]", iniPathWashTrayInfo, dtWashTrayIni);
        }
        void CleanTrayWash(int oneOrTwo)
        {
            string order;
            if (oneOrTwo == 1)
            {
                substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube));
                subBar = OperateIniFile.ReadIniData("Substrate1", "BarCode", "", iniPathSubstrateTube);
                order = "EB 90 31 03 03 00 11 11 10";
            }
            else if (oneOrTwo == 2)
            {
                order = "EB 90 31 03 03 01 00 00 00";
            }
            else
            {
                throw new Exception();
            }
            AgainNewMove:
            if (isNewWashEnd()) return;  //lyq add 20190822
            NetCom3.Instance.Send(NetCom3.Cover(order), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                    goto AgainNewMove;
                else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.Cleanovertime"));
                    frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Overtimepause"));
                }
                else
                    return;
            }
            else
            {
                if (oneOrTwo == 1)
                {
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", (substrateNum1 - 1).ToString(), iniPathSubstrateTube);
                    DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + (substrateNum1 - 1).ToString() + " where BarCode = '"
                                           + subBar + "'");
                    substrateNum1--;
                }
            }
        }
        private bool AddTubeInCleanTray(int pos = 0)
        {
            int iNeedCool = 0;
            int IsKnockedCool = 0;
            AgainNewMove:
            if (isNewWashEnd()) return false;  //lyq add 20190822
            string order = "EB 90 31 01 06";
            NetCom3.Instance.Send(NetCom3.Cover(order), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            return false;
                        }
                        goto AgainNewMove;
                    }
                    else
                    {
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MAddNewTReactNulls"));
                        DialogResult tempresult = MessageBox.Show(GetString("keywordText.Handemptystop"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.LackTube)
                {
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.EMptytube"));
                    DialogResult tempresult = MessageBox.Show(GetString("keywordText.Tubeemptystop"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return false;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                {
                    if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                    {
                        NetCom3.Instance.waitAndAgainSend.Abort();
                    }
                    goto AgainNewMove;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                {
                    IsKnockedCool++;
                    if (IsKnockedCool < 2)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                        if (!NetCom3.Instance.SingleQuery())
                        {
                            return false;
                        }
                        goto AgainNewMove;
                    }
                    else
                    {
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MAddNewTReactIsKnock"));
                        LogFile.Instance.Write("==============  移管手在暂存盘向清洗盘取放管处抓管发生撞管  " + currentHoleNum);
                        DialogResult tempresult = MessageBox.Show(GetString("keywordText.Hitstop"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        return false;
                    }

                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                {
                    IsKnockedCool++;
                    if (IsKnockedCool < 2)
                        goto AgainNewMove;
                    else
                    {
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MWashLossIsKnock"));
                        LogFile.Instance.Write("==============  移管手在暂存盘向清洗盘抓管时发生撞管  " + currentHoleNum);
                        DialogResult tempresult = MessageBox.Show(GetString("keywordText.Emptystop"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                {
                    frmMessageShow frmMessage = new frmMessageShow();
                    frmMessage.MessageShow(GetString("label6.Text"), GetString("keywordText.TemporaryDiskStuckTube"));
                    NewWashEnd(1);
                    return false;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                   LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MAddNewToWashOvertime"));
                    DialogResult tempresult = MessageBox.Show(GetString("keywordText.Overtimestop"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return false;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                {
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.TemporaryDiskStuckTube"));
                    DialogResult tempresult = MessageBox.Show(GetString("keywordText.TemporaryDiskStuckTube"), GetString("keywordText.Tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return false;
                }
                #endregion
            }
            return true;
        }
        //Jun Add 20190318  从清洗盘取放管处扔管
        private bool RemoveTubeOutCleanTray()
        {
            int iNeedCool = 0;
            int IsKnockedCool = 0;
            AgainNewMove:
            if (isNewWashEnd()) return false;  //lyq add 20190822
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " +GetString("keywordText.NoRead") + " *** " + GetString("keywordText.Throwovertimestop"));
                            LogFileAlarm.Instance.Write(" *** " + GetString("keywordText.time") + DateTime.Now.ToString("HH-mm-ss") + GetString("keywordText.WashHole") + tubeHoleNum + " *** ");
                            DialogResult tempresult = frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Emptytimesstop"));
                            return false;
                        }
                        goto AgainNewMove;
                    }
                    else
                    {
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " +GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MWashLossNullS"));
                        LogFileAlarm.Instance.Write(" *** " + GetString("keywordText.time") + DateTime.Now.ToString("HH-mm-ss") + GetString("keywordText.WashHole") + tubeHoleNum + " *** ");
                        DialogResult tempresult = frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Emptytimesstop"));
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                {
                    if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                    {
                        NetCom3.Instance.waitAndAgainSend.Abort();
                    }
                    goto AgainNewMove;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                {
                    IsKnockedCool++;
                    if (IsKnockedCool < 2)
                    {
                        goto AgainNewMove;
                    }
                    else
                    {
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " +GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead") + " *** " + GetString("keywordText.MWashLossIsKnock"));
                        LogFileAlarm.Instance.Write(" *** " + GetString("keywordText.time") + DateTime.Now.ToString("HH-mm-ss") + GetString("keywordText.WashHole") + tubeHoleNum + " *** ");
                        DialogResult tempresult = frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Throwstop"));
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + GetString("keywordText.Err") + " *** " + GetString("keywordText.NoRead")  + " *** " + GetString("keywordText.MWashLossOverTime"));
                    DialogResult tempresult = frmMsgShow.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Throwovertimestop"));
                    return false;
                }
                #endregion
            }
            return true;
        }
        /// <summary>
        ///循环灌注线程
        /// </summary>
        Thread Loopthread = null;
        /// <summary>
        /// 循环灌注标志
        /// </summary>
        bool bLoopClick = false;
        private void functionButton6_Click(object sender, EventArgs e)
        {
            if (functionButton6.Text == GetString("keywordText.Circulatoryperfusion"))
            {
                functionButton6.Enabled = false;
                functionButton6.Text = GetString("keywordText.Stopperfusion1");
                bLoopRun = true;
                CancellationToken = false;
                substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                if (!bLoopRun) return;
                if (numericUpDown5.Value.ToString().Trim() == "0")
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Perfusiontimes"));
                    functionButton6.Enabled = true;
                    functionButton6.Text = GetString("keywordText.Circulatoryperfusion");
                    bLoopRun = true;
                    return;
                }
                if (numericUpDown5.Value.ToString().Trim() != "" && int.Parse(numericUpDown5.Value.ToString()) > substrateNum1)
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Insufficientsubstrate"));
                    functionButton6.Enabled = true;
                    functionButton6.Text = GetString("keywordText.Circulatoryperfusion");
                    bLoopRun = true;
                    return;
                }
                if (Loopthread == null || Loopthread.ThreadState != ThreadState.Running)
                {
                    LoopPourinto = int.Parse(numericUpDown5.Value.ToString());
                    TExtAppend(GetString("keywordText.Circulatoryperfusionbegins"));
                    bLoopClick = true;
                    Loopthread = new Thread(new ThreadStart(TestLoopRun));
                    Loopthread.IsBackground = true;
                    Loopthread.CurrentCulture = Language.AppCultureInfo;
                    Loopthread.CurrentUICulture = Language.AppCultureInfo;
                    Loopthread.Start();
                    functionButton6.Enabled = true;
                }
            }
            else
            {
                if (!bLoopClick)
                    return;
                bLoopClick = false;
                for (int i = 0; i < 5; i++)
                {
                    if (Loopthread != null && Loopthread.ThreadState != ThreadState.Stopped)
                    {
                        Loopthread.Abort();
                        Thread.Sleep(4000);
                    }
                }
                functionButton6.Text = GetString("keywordText.Circulatoryperfusion");
                bLoopRun = false;
                functionButton6.Enabled = true;
            }
        }
        private void functionButton5_Click(object sender, EventArgs e)
        {
            if (CancellationToken || !isNewWashRun)
            {
                MessageBox.Show(GetString("keywordText.Noruning"));
                return;
            }
            CancellationToken = true;
            NewWashEnd();
        }
        /// <summary>
        /// 清洗灌注夹管标志
        /// </summary>
        bool IsClearNewTube = false;
        private void btnPourInto_Click(object sender, EventArgs e)
        {
            if (btnPourInto.Text == GetString("keywordText.Stop1"))
            {
                #region 停止灌注
                NewWashEnd();
                #endregion
            }
            else
            {
                textBox1.Text = "";
                if (isNewWashRun)
                {
                    MessageBox.Show(GetString("keywordText.Running"));
                    return;
                }
                btnPourInto.Enabled = false;
                textBox1.Text = "";
                btnPourInto.Text = GetString("keywordText.Stop1");
                #region 判断运行条件
                if (numRepeat.Value.ToString().Trim() == "0")
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Perfusiontimeset"));
                    btnPourInto.Enabled = true;
                    btnPourInto.Text = GetString("keywordText.implement");
                    return;
                }
                else if (cmbSelectAct.SelectedIndex == 0 && numRepeat.Value > 20)
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.Beadmax20"));
                    btnPourInto.Enabled = true;
                    btnPourInto.Text = GetString("keywordText.implement");
                    return;
                }
                else if (cmbSelectAct.SelectedIndex == 1 && numRepeat.Value > 10)
                {
                    frmMessageShow f = new frmMessageShow();
                    f.MessageShow(GetString("keywordText.Tip"), GetString("keywordText.probemax10"));
                    btnPourInto.Enabled = true;
                    btnPourInto.Text = GetString("keywordText.implement");
                    return;
                }
                #endregion
                #region 启用灌注线程

                isNewWashRun = true;
                switch (cmbSelectAct.SelectedIndex)
                {
                    case 3:
                        bLoopRun = true;
                        CancellationToken = false;
                        if (Loopthread == null || Loopthread.ThreadState != ThreadState.Running)
                        {
                            LoopPourinto = int.Parse(numRepeat.Value.ToString());
                            TExtAppend(GetString("keywordText.Circulatoryperfusionbegins"));
                            bLoopClick = true;
                            Loopthread = new Thread(new ThreadStart(TestLoopRun));
                            Loopthread.IsBackground = true;
                            Loopthread.CurrentCulture = Language.AppCultureInfo;
                            Loopthread.CurrentUICulture = Language.AppCultureInfo;
                            Loopthread.Start();
                            btnPourInto.Enabled = true;
                        }
                        break;
                    case 0:
                        bLoopRun = true;
                        CancellationToken = false;
                        IsClearNewTube = true;
                        if (Loopthread == null || Loopthread.ThreadState != ThreadState.Running)
                        {
                            LoopPourinto = int.Parse(numRepeat.Value.ToString());
                            TExtAppend(GetString("keywordText.beadperfusion"));
                            bLoopClick = true;
                            Loopthread = new Thread(new ThreadStart(ClearPourIntoRun));
                            Loopthread.IsBackground = true;
                            Loopthread.CurrentCulture = Language.AppCultureInfo;
                            Loopthread.CurrentUICulture = Language.AppCultureInfo;
                            Loopthread.Start();
                            btnPourInto.Enabled = true;
                        }
                        break;
                    case 1:
                        bLoopRun = true;
                        CancellationToken = false;
                        if (Loopthread == null || Loopthread.ThreadState != ThreadState.Running)
                        {
                            LoopPourinto = int.Parse(numRepeat.Value.ToString());
                            TExtAppend(GetString("probeperfusionstart"));
                            bLoopClick = true;
                            Loopthread = new Thread(new ThreadStart(SubstratePourIntoRun));
                            Loopthread.IsBackground = true;
                            Loopthread.CurrentCulture = Language.AppCultureInfo;
                            Loopthread.CurrentUICulture = Language.AppCultureInfo;
                            Loopthread.Start();
                            btnPourInto.Enabled = true;
                        }
                        break;
                }
                #endregion
            }
        }
        void ClearPourIntoRun()
        {
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (IsClearNewTube)
            {
                #region 清空清洗盘
                SetCultureInfo();
                string s = GetString("keywordText.Emptycleaning");
                TExtAppend(GetString("keywordText.Emptycleaning"));
                for (int i = 0; i < WashTrayNum; i++)
                {
                    if (isNewWashEnd()) return;  //lyq add 20190822
                    if (!bLoopRun) return;
                    //打印当前取放管孔位到Log。  jun 2019/1/22
                    int iNeedCool = 0;
                    LogFile.Instance.Write(string.Format("***{0}->:{1}***", "取放管时间: " + DateTime.Now.ToString("HH:mm:ss:fff"), "管孔位置: " + tubeHoleNum));
                    AgainNewMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                    if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag != (int)ErrorState.IsNull)
                    {
                        iNeedCool++;
                        if (iNeedCool < 2)
                        {
                            if (NetCom3.Instance.MoverrorFlag != (int)ErrorState.IsKnocked)
                            {
                                goto AgainNewMove;
                            }
                            else if (NetCom3.Instance.MoverrorFlag != (int)ErrorState.putKnocked)
                            {
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 01 03 00"), 5);
                                if (!NetCom3.Instance.SingleQuery())
                                {
                                    return;
                                }
                                goto AgainNewMove;
                            }
                        }
                        NewWashEnd(1);
                        return;
                    }
                    if (isNewWashEnd()) return;  //lyq add 20190822
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                    //逆时针计数。  Jun 2019/1/22
                    if (tubeHoleNum <= 0)
                    {
                        tubeHoleNum = tubeHoleNum + WashTrayNum;
                    }
                    tubeHoleNum = tubeHoleNum - 1;
                    if (!NetCom3.Instance.WashQuery())
                    {
                        NewWashEnd(3);
                        return;
                    }
                }
                TExtAppend(GetString("keywordText.Emptycleaningfinish"));
                #endregion
                TExtAppend(GetString("keywordText.Cliptube"));
                if (isNewWashEnd()) return;
                if (!AddTubeInCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
                CleanTrayMovePace(4);
                if (isNewWashEnd()) return;
                if (!AddTubeInCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
                if (isNewWashEnd()) return;
                CleanTrayMovePace(4);
                if (!AddTubeInCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
                CleanTrayMovePace(5 + isNewCleanTray);
            }
            TExtAppend(GetString("keywordText.startperfusion"));
            for (int i = 0; i < LoopPourinto; i++)
            {
                if (isNewWashEnd()) return;
                if (bLoopClick)
                    BeginInvoke(new Action(() => { numRepeat.Value = LoopPourinto - i; }));
                if (!bLoopRun)
                {
                    NewWashEnd();
                    return;
                }
                //清洗管路灌注
                if (!CleanPowerInto(IsClearNewTube))
                {
                    NewWashEnd(3);
                    return;
                }
                if (IsClearNewTube)
                {
                    CleanTrayMovePace(-1);
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 0B 01 00"), 5);
                    if (!NetCom3.Instance.SingleQuery() && NetCom3.Instance.errorFlag != (int)ErrorState.ReadySend)
                    {
                        TExtAppend(GetString("keywordText.Perfusionfailure") + Enum.GetName(typeof(ErrorState), NetCom3.Instance.errorFlag) + "\n");
                        NewWashEnd();
                        return;
                    }
                    CleanTrayMovePace(1);
                }
            }
            for (int i = 0; i < 3; i++)//lyq add20201223
            {
                //注液
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 00 11 10"), 2);
                if (!NetCom3.Instance.SingleQuery() && NetCom3.Instance.errorFlag != (int)ErrorState.ReadySend)
                {
                    TExtAppend(GetString("keywordText.Perfusionfailure") + Enum.GetName(typeof(ErrorState), NetCom3.Instance.errorFlag) + "\n");
                    NewWashEnd();
                    return;
                }
                if (IsClearNewTube)
                {
                    //顺时针转1格
                    CleanTrayMovePace(-1);
                    //抽液
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 01"), 2);
                    if (!NetCom3.Instance.SingleQuery() && NetCom3.Instance.errorFlag != (int)ErrorState.ReadySend)
                    {
                        TExtAppend(GetString("keywordText.Perfusionfailure") + Enum.GetName(typeof(ErrorState), NetCom3.Instance.errorFlag) + "\n");
                        NewWashEnd();
                        return;
                    }
                    //逆时针转1格
                    CleanTrayMovePace(1);
                }
            }
            TExtAppend(GetString("keywordText.Perfusionfinish2"));
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (IsClearNewTube)
            {
                CleanTrayMovePace(-5 - isNewCleanTray);
                if (!RemoveTubeOutCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
                if (!bLoopRun)
                {
                    NewWashEnd();
                    return;
                }
                CleanTrayMovePace(-4);
                if (!RemoveTubeOutCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
                CleanTrayMovePace(-4);
                if (!bLoopRun)
                {
                    NewWashEnd();
                    return;
                }
                if (!RemoveTubeOutCleanTray())
                {
                    NewWashEnd(1);
                    return;
                }
            }
            NewWashEnd();
            TExtAppend(GetString("keywordText.beadPerfusionfinish"));
        }
        void SubstratePourIntoRun()
        {
            if (!bLoopRun)
            {
                NewWashEnd();
                return;
            }
            if (isNewWashEnd()) return;
            TExtAppend(GetString("keywordText.startperfusion"));
            for (int i = 0; i < LoopPourinto; i++)
            {
                if (isNewWashEnd()) return;  //lyq add 20190822
                if (bLoopClick)
                    BeginInvoke(new Action(() => { numRepeat.Value = LoopPourinto - i; }));

                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 08"), 0);
                //if (!NetCom3.Instance.SPQuery())
                //{
                //    NewWashEnd();
                //    return;
                //}
                if (!NetCom3.Instance.SPQuery() && NetCom3.Instance.AdderrorFlag != (int)ErrorState.ReadySend)
                {
                    TExtAppend(GetString("keywordText.Perfusionfailure") + Enum.GetName(typeof(ErrorState), NetCom3.Instance.AdderrorFlag) + "\n");
                    NewWashEnd();
                    return;
                }
                Thread.Sleep(2000);
            }
            if (isNewWashEnd()) return;
            BeginInvoke(new Action(() => { btnPourInto.Text = GetString("keywordText.implement"); }));
            bLoopRun = false;
            btnPourInto.Enabled = true;
            NewWashEnd();
            TExtAppend(GetString("keywordText.probeperfusionfinish"));
        }
        /// <summary>
        /// 清洗管路灌注
        /// </summary>
        /// <param name="IsTube">是否有管</param>
        bool CleanPowerInto(bool IsTube)
        {
            string order = "";
            if (IsTube)
                order = "EB 90 11 0B 01 03";
            else
                order = "EB 90 11 0B 00 03";
            NetCom3.Instance.Send(NetCom3.Cover(order), 5);
            //if (!NetCom3.Instance.SingleQuery())
            //    return false;
            if (!NetCom3.Instance.SingleQuery() && NetCom3.Instance.errorFlag != (int)ErrorState.ReadySend)
            {
                TExtAppend(GetString("keywordText.Perfusionfailure") + Enum.GetName(typeof(ErrorState), NetCom3.Instance.errorFlag) + "\n");
                return false;
            }
            else
                return true;
        }
        /// <summary>
        /// 底物管路灌注
        /// </summary>
        /// <param name="IsTube"></param>
        /// <returns></returns>
        bool SubstratePourIn(bool IsTube)
        {
            string order = "";
            if (IsTube)
                order = "EB 90 11 07 07 01";
            else
                order = "EB 90 11 07 07 00";
            NetCom3.Instance.Send(NetCom3.Cover(order), 5);
            if (!NetCom3.Instance.SingleQuery())
                return false;
            else
                return true;
        }
        #endregion

        private string GetString(string key)
        {
            return resources.GetString(key);
        }
    }
}
