using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using BioBaseCLIA.CalculateCurve;
using BioBaseCLIA.DataQuery;
using BioBaseCLIA.Extentions;
using BioBaseCLIA.InfoSetting;
using Common;
using Localization;
using Maticsoft.DBUtility;
using static BioBaseCLIA.Run.TestSchedule;

namespace BioBaseCLIA.Run
{
    public partial class frmWorkList : frmParent
    {
        #region 界面显示与进度计算相关变量
        /// <summary>
        /// 根据不同情况更改主界面运行按钮状态
        /// </summary>
        public static event Action btnRunStatus;
        /// <summary>
        /// 根据不同情况更改暂存盘按钮颜色显示 2019-03-12 zlx add
        /// </summary>
        public static event Action dbtnRackStatus;
        /// <summary>
        /// 样本试剂盘自定义控件更新
        /// </summary>
        public static event Action SpDiskUpdate;
        /// <summary>
        /// 报警时更改主界面日志按钮的颜色
        /// </summary>
        //public static event Action<int> btnLogColor;
        /// <summary>
        /// 实验每个步骤的信息
        /// </summary>
        List<TestMethod> lisTestMethod = new List<TestMethod>();
        /// <summary>
        /// TestMethod 新建一个实例
        /// </summary>
        TestMethod testmethod = new TestMethod();
        /// <summary>
        /// 各个步骤实验进度,最终结果按照实验运行顺序排序
        /// </summary>
        List<TestSchedule> lisTestSchedule = new List<TestSchedule>();

        /// <summary>
        /// TestSchedule 新建一个实例
        /// </summary>
        TestSchedule testschedule = new TestSchedule();
        /// <summary>
        /// 用于datagridview控件绑定的集合
        /// </summary>
        static IList<TestItem> lisTestItem = new List<TestItem>();

        /// <summary>
        /// 绑定类型的列表，便于作为datagridview的数据源时进行增删改查
        /// </summary>
        public static BindingList<TestItem> BTestItem = new BindingList<TestItem>(lisTestItem);
        /// <summary>
        /// 新建一个所做实验项目的实例
        /// </summary>
        TestItem testItem = new TestItem();
        /// <summary>
        /// 进度条控件序列
        /// </summary>
        List<Bar.ProgressBar> lisProBar = new List<Bar.ProgressBar>();

        Bar.ProgressBar proBar = new Bar.ProgressBar();
        /// <summary>
        /// 添加控件事件
        /// </summary>
        public event Action<bool> EventControlAdd;

        Action<string, int> TestStatusInfo;
        /// <summary>
        /// 正在做的实验步骤
        /// </summary>
        TestSchedule DoingTestStep = new TestSchedule();
        /// <summary>
        /// 实验完成数量
        /// </summary>
        private int completeTestNums = 0;
        /// <summary>
        /// 记录上一次加试剂时间
        /// </summary>
        int lastAddLiquidTime = 0;
        /// <summary>
        /// 实验总时间
        /// </summary>
        int sumTime = 0;
        /// <summary>
        /// 当前步骤的时间
        /// </summary>
        int stepTime = 0;
        /// <summary>
        /// 实际步骤运行时间是否有延迟
        /// </summary>
        bool DalayFlag = false;
        /// <summary>
        /// 模拟运行时间间隔，1000代表1s
        /// </summary>
        int timeInterval = 1000;
        /// <summary>
        /// 实验剩余时间(以分钟计算)
        /// </summary>
        public static int LeftTestTime = 0;
        /// <summary>
        /// 当前可做样本的数量
        /// </summary>
        int SampleNumCurrent = 0;
        /// <summary>
        ///定义实验结果的List列表
        /// </summary>
        public static List<TestResult> ITestResult = new List<TestResult>();
        /// <summary>
        /// 存储显示在实验结果界面的数据
        /// </summary>
        public static BindingCollection<TestResult> BTestResult = new BindingCollection<TestResult>();
        /// <summary>
        /// 存储实验结果数据
        /// </summary>
        public static BindingCollection<TestResult> TemporaryTestResult = new BindingCollection<TestResult>();
        /// <summary>
        /// 实验结果
        /// </summary>
        public static TestResult testResult = new TestResult();
        /// <summary>
        /// 已经结束的实验项目
        /// </summary>
        List<TestItem> lisTiEnd = new List<TestItem>();
        /// <summary>
        /// 加样时间
        /// </summary>
        int SampleTime = int.Parse(OperateIniFile.ReadInIPara("Time", "sampleTime"));
        /// <summary>
        /// 加试剂时间
        /// </summary>
        int RegentTime = int.Parse(OperateIniFile.ReadInIPara("Time", "RegentTime"));
        /// <summary>
        /// 加磁珠时间
        /// </summary>
        int beadTime = int.Parse(OperateIniFile.ReadInIPara("Time", "beadTime"));
        /// <summary>
        /// 清洗时间
        /// </summary>
        int washTime = int.Parse(OperateIniFile.ReadInIPara("Time", "washTime"));
        /// <summary>
        /// 加底物时间
        /// </summary>
        int substrateTime = int.Parse(OperateIniFile.ReadInIPara("Time", "substrateTime"));
        /// <summary>
        /// 读数时间
        /// </summary>
        int readTime = int.Parse(OperateIniFile.ReadInIPara("Time", "readTime"));
        /// <summary>
        /// 稀释时间。LYN add 20171114
        /// </summary>
        int dilutionTime = 0;
        /// <summary>
        /// 底物剩余测数
        /// </summary>
        int substrateNum1 = 0;
        int substrateNum2 = 0;
        /// <summary>
        /// 弹窗提示实例
        /// </summary>
        frmMessageShow frmMsgShow = new frmMessageShow();
        /// <summary>
        /// 额外稀释体积，LYN add 20171114
        /// </summary>
        int extraDilutionVol = 100;
        /// <summary>
        /// 后弃体积
        /// </summary>
        int AbandSampleVol = 10;
        /// <summary>
        /// 温育盘起始位置。LYN add 20171114
        /// </summary>
        int ReactStartPos = 0;
        /// <summary>
        /// 实验始末清洗盘清洗次数
        /// </summary>
        int WashTrayCleanTimes = 3;//lyq 为判断底物数量不足
        #endregion

        #region 实验运行相关变量
        /// <summary>
        /// 运行线程
        /// </summary>
        private Thread RunThread;
        /// <summary>
        /// 加液线程
        /// </summary>
        private Thread AddLiquidThread;
        /// <summary>
        /// 是否是新的清洗盘，新清洗盘的抽液和注液位置均往后一个位置，是为1，否为0
        /// </summary>
        int isNewCleanTray = 1;
        /// <summary>
        /// 已停止的实验编号
        /// </summary>
        public static List<string> StopList = new List<string>();
        /// <summary>
        /// 实验项目名字
        /// </summary>
        List<string> ProjectName = new List<string>();
        #region 公共
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
        //lyn add 20180611
        /// <summary>
        /// R1后弃比例
        /// </summary>
        int abanR1Pro = 10;
        /// <summary>
        /// R2后弃比例
        /// </summary>
        int abanR2Pro = 10;
        /// <summary>
        /// R3后弃比例
        /// </summary>
        int abanR3Pro = 10;
        /// <summary>
        /// 稀释液后弃比例
        /// </summary>
        int abanDiuPro = 10;
        /// <summary>
        /// 磁珠后弃比例
        /// </summary>
        int abanBeadPro = 10;
        /// <summary>
        /// 20个位置的稀释液体积
        /// </summary>
        string[] diuleftVol = new string[RegentNum];
        /// <summary>
        /// 移管手标志位，是否正在使用，TRUE为正在使用，false未在使用
        /// </summary>
        bool MoveTubeUseFlag = false;
        /// <summary>
        /// 清洗盘标志位，是否正在使用，TRUE为正在使用，false未在使用
        /// </summary>
        bool WashTrayUseFlag = false;
        /// <summary>
        /// 清洗盘是否正在旋转
        /// </summary>
        bool WashTurnFlag = false;
        /// <summary>
        /// 正在加样标志位
        /// </summary>
        public static bool AddingSampleFlag = false;
        /// <summary>
        /// 实验是否正在运行
        /// </summary>
        public static int RunFlag = (int)RunFlagStart.NoStart;

        /// <summary>
        /// 反应盘孔数
        /// </summary>
        int ReactTrayHoleNum = int.Parse(OperateIniFile.ReadInIPara("OtherPara", "ReactTrayHoleNum"));
        /// <summary>
        /// 最小吸液量
        /// </summary>
        int ImbibitionMin = 7;
        /// <summary>
        /// 稀释液获取不到的体积/ul 2019-04-12 zlx add
        /// </summary>
        int DiuNoUsePro = 0;
        /// <summary>
        /// 稀释样本后弃体积
        /// </summary>
        int DiuLeftVol = 60;
        /// <summary>
        /// 试剂获取不到的体积/ul 2019-04-12 zlx add
        /// </summary>
        int RegentNoUsePro = 200;
        /// <summary>
        /// 反应盘待使用空白反应管个数
        /// </summary>
        int toUsedTube = 10;//modify y 20180522 10=>15
        /// <summary>
        /// 未开始进行实验的反应管的id
        /// </summary>
        int NoStartTestId = 1;
        /// <summary>
        /// 是否添加急诊
        /// </summary>
        public static bool EmergencyFlag = false;
        /// <summary>
        /// 是否加普通样本
        /// </summary>
        public static bool addOrdinaryFlag = false;
        /// <summary>
        /// 添加重测样本
        /// </summary>
        public static bool RetestFlag = false;
        /// <summary>
        /// 实验运行的下一步骤
        /// </summary>
        TestSchedule TestStep;
        #endregion
        #region 清洗相关
        /// <summary>
        /// 清洗线程timer
        /// </summary>
        System.Timers.Timer timer = new System.Timers.Timer();
        /// <summary>
        /// 清洗盘是否放入第一个反应管
        /// </summary>
        bool FirstTubeWash = false;
        /// <summary>
        /// 存储清洗盘反应管状态
        /// </summary>
        DataTable dtWashTrayTubeStatus = new DataTable();
        /// <summary>
        /// 存储需进行取放管的反应管信息
        /// </summary>
        List<MoveTubeStatus> lisMoveTube = new List<MoveTubeStatus>();
        MoveTubeStatus moveTube = new MoveTubeStatus();
        /// <summary>
        /// 夹管线程
        /// </summary>
        private Thread MoveTubeThread;
        /// <summary>
        /// 读数线程
        /// </summary>
        private Thread ReadThread;
        /// <summary>
        /// 清洗线程
        /// </summary>
        private Thread washThread;
        /// <summary>
        /// 第一次清洗的结束时间
        /// </summary>
        List<int> W1EndTime = new List<int>();
        /// <summary>
        /// 清洗盘取放管位置当前孔号
        /// </summary>
        int currentHoleNum = 1;
        /// 稀释使用反应管数。LYN add 20171114
        int DiuTubeNum = 0;
        /// <summary>
        /// 下位机返回数据
        /// </summary>
        string[] dataRecive = new string[16];
        /// <summary>
        /// 暂时存储未处理的读数数据
        /// </summary>
        Queue<string> DataReciveNumberRead = new Queue<string>();
        #endregion
        #endregion

        #region 结果计算
        /// <summary>
        /// 吸光度
        /// </summary>
        int PMT = 0;
        /// <summary>
        /// 浓度
        /// </summary>
        string concentration = "";
        /// <summary>
        /// 实验结果
        /// </summary>
        string result;

        string sco = "";
        /// <summary>
        /// 实验计算线程
        /// </summary>
        private Thread CaculateThread;
        /// <summary>
        /// 标准品定标液的初始信息
        /// </summary>
        public static List<ScalingInfo> lisScalingInfo;//= new List<ScalingInfo>();
        /// <summary>
        /// 标准品信息实例
        /// </summary>
        ScalingInfo scalingInfo = new ScalingInfo();
        /// <summary>
        /// 存储标准品或定标液的吸光度值
        /// </summary>
        DataTable dtScalingPMT;
        /// <summary>
        /// 标准品或定标液计算结果
        /// </summary>
        DataTable dtScalCacResult;
        #endregion


        #region 数据处理相关变量
        /// <summary>
        /// 读数下位机返回数据
        /// </summary>
        string ReadbackObj = "";
        #endregion

        #region 标志位
        /// <summary>
        /// 仪器运行指示灯标志位
        /// </summary>
        bool RunLightFlag = false;
        /// <summary>
        /// 缺管标志  
        /// </summary>
        public static bool TubeStop = false;
        /// <summary>
        /// 堵针标志  
        /// </summary>
        public static bool PlugneedleFalg = false;
        /// <summary>
        /// 向温育盘夹管失败的位置
        /// </summary>
        List<int> AddTubeStop = new List<int>();
        /// <summary>
        /// 底物缺少标志 
        /// </summary>
        public static bool SubstrateStop = false;
        /// <summary>
        /// 温育盘满盘标志
        /// </summary>
        private bool BFullReactTray = false;
        /// <summary>
        /// 抓管出现问题标志
        /// </summary>
        private bool TubeProblemFlag = false;
        /// <summary>
        /// 是否允许清洗盘转动
        /// </summary>
        bool NoTateFlag = false;
        /// <summary>
        /// 温育盘往清洗盘抓空标志
        /// </summary>
        bool moveNullTubeProblemFlag = false;
        /// <summary>
        /// 是否进入实验流程
        /// </summary>
        public static bool EntertRun = false;
        /// 试剂/稀释液缺少标致
        /// </summary>
        public static bool bReagentDiuLess = false;
        #endregion
        /// <summary>
        /// 液位检测报警事件
        /// </summary>
        public static event Action<string, int> LiquidLevelDetectionEvent;
        /// <summary>
        /// 发光值校准系数
        /// </summary>
        public static string Coefficient = OperateIniFile.ReadInIPara("ResultCoefficient", "Coefficient");

        public frmWorkList()
        {
            InitializeComponent();

            dtWashTrayTubeStatus.Columns.Add("Pos", typeof(string));
            dtWashTrayTubeStatus.Columns.Add("Value", typeof(string));
            dtWashTrayTubeStatus.Columns.Add("TestId", typeof(int));
            dtWashTrayTubeStatus.Columns.Add("StepNum", typeof(int));
            //清洗盘状态表中新添加一个字段，存放反应管在反应盘的位置。LYN add 20171114
            dtWashTrayTubeStatus.Columns.Add("reactTrayPos", typeof(int));
            //加载数据
            foreach (TestResult item in ITestResult)
            {
                BTestResult.Add(item);
            }
            if (dgvWorkListData.RowCount > 0)//2019-01-08 zlx mod
                dgvWorkListData.DataSource = null;
        }

        private void frmWorkList_Load(object sender, EventArgs e)
        {
            LogFile.Instance.Write("进入工作列表load" + DateTime.Now.ToString("mm:ss:ms"));

            //2018-08-13 zlx add
            if (!frmMain.LiquidQueryFlag)
                frmMain.LiquidQueryFlag = true;
            if (!NetCom3.isConnect)
            {
                if (NetCom3.Instance.CheckMyIp_Port_Link())
                {
                    NetCom3.Instance.ConnectServer();
                }
            }
            if (NetCom3.Instance.stopsendFlag)
                NetCom3.Instance.stopsendFlag = false;
            if (!NetCom3.isConnect)
            {
                frmMsgShow.MessageShow(getString("keywordText.Scnn"), getString("keywordText.ConnectErr"));
                return;
            }

            #region 数据初始化及变量赋值
            lisTestMethod = new List<TestMethod>();
            lisTestSchedule = new List<TestSchedule>();
            //NetCom3.Instance.ReceiveHandel += new Action<string>(Instance_ReceiveHandel);
            #endregion
            if (BTestResult != null)
                if (BTestResult.Count > 0)
                {
                    for (int i = BTestResult.Count - 1; i >= 0; i--)//modify y 20180512
                    {
                        //BTestResult.RemoveAt(i);
                    }
                }
            if (TemporaryTestResult != null)
            {
                TemporaryTestResult.Clear();
            }
            if (BTestItem != null)
                if (BTestItem.Count > 0)
                {
                    for (int i = BTestItem.Count - 1; i >= 0; i--)//modify y 20180512
                    {
                        BTestItem.RemoveAt(i);
                    }
                }
            DiuInfo.ReadDiuTime();
            frmMain.btnRunClick -= new Action<object, EventArgs>(TestRun);
            frmMain.btnRunClick += new Action<object, EventArgs>(TestRun);
            frmMain.btnPauseClick -= new Action(AllPause);
            frmMain.btnPauseClick += new Action(AllPause);
            frmMain.btnStopClick -= new Action(AllStop);
            frmMain.btnStopClick += new Action(AllStop);
            frmMain.btnGoonClick -= new Action(Goon);
            frmMain.btnGoonClick += new Action(Goon);
            frmSampleLoad.EmergencySample -= new Action(EmergencySampleSch);
            frmSampleLoad.EmergencySample += new Action(EmergencySampleSch);
            frmWarn.btnPauseClick += new Action(AllPause);
            frmWarn.btnGoonClick += new Action(Goon);
            NetCom3.Instance.EventStop += new Action(AllStop);

            frmTestResult.RetestSample -= new Action(EmergencySampleSch);
            frmTestResult.RetestSample += new Action(EmergencySampleSch);

            //NetCom3.MoveTubeError += this.DisposeMoveAndAddError;//y add 20180723
            AddResetEvent.Set();//add y 20180727

        }
        /// <summary>
        /// 数据接收
        /// </summary>
        /// <param name="obj"></param>
        void Instance_ReceiveHandel(string obj)
        {
            if (obj.IsNullOrEmpty())
                return;
            else
            {
                lock (dataLocker)
                {
                    dataRecive = obj.Split(' ');
                    if (dataRecive[0] != null && dataRecive[3] == "A3" && (dataRecive[15] != "00" || dataRecive[14] != "00" || dataRecive[13] != "00"))
                    {
                        string readData = dataRecive[12] + dataRecive[13] + dataRecive[14] + dataRecive[15];
                        lock (DataReciveNumberRead)
                        {
                            DataReciveNumberRead.Enqueue(readData);
                        }
                        dataRecive[0] = null;
                    }
                    if (obj.Contains("EB 90 11 AF 01 06 FF"))
                    {
                        TubeStop = true;
                        LogFile.Instance.Write(DateTime.Now.ToString("HH:mm:ss") + "查询理杯机指令返回数据！");
                    }
                }
            }
        }

        #region 进度生成

        /// <summary>
        /// 急诊样本进度生成
        /// </summary>
        /// <param name="dtEmergencySample"></param>
        void EmergencySampleSch()
        {
            BLL.tbSampleInfo bllsampleinfo = new BLL.tbSampleInfo();
            BLL.tbProject bllproject = new BLL.tbProject();
            LoadingHelper.ShowLoadingScreen();
            DataTable dtSampleInfo = new DataTable();
            dtSampleInfo.Columns.Add("SampleID", typeof(int));
            dtSampleInfo.Columns.Add("SampleType", typeof(string));
            dtSampleInfo.Columns.Add("SampleNo", typeof(string));
            dtSampleInfo.Columns.Add("RepeatCount", typeof(int));
            dtSampleInfo.Columns.Add("Position", typeof(int));
            dtSampleInfo.Columns.Add("SampleContainer", typeof(string));
            dtSampleInfo.Columns.Add("Emergency", typeof(string));
            dtSampleInfo.Columns.Add("ProjectProcedure", typeof(string));
            dtSampleInfo.Columns.Add("ShortName", typeof(string));
            #region 生成实验进度
            if (RunFlag == (int)RunFlagStart.IsRuning)
            {
                //运行中加急诊样本
                if (EmergencyFlag)
                {
                    #region 急诊样本
                    #region 获取样本信息
                    //查询添加的急诊样本
                    DbHelperOleDb db = new DbHelperOleDb(1);
                    //DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                    //    + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                    //    + "# and Status = 0  and Emergency = 2 order by CInt([Position])").Tables[0];
                    DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                        + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                        + "# and Status = 0  and Emergency = 2 order by  SampleNo").Tables[0];
                    db = new DbHelperOleDb(1);
                    DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Emergency = 3 where Status = 0 and Emergency = 2 and SendDateTime >=#"
                                      + DateTime.Now.ToString("yyyy-MM-dd") + "#and SendDateTime <#"
                                      + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + "#");
                    db = new DbHelperOleDb(0);
                    DataTable dtproinfo = bllproject.GetList("").Tables[0];
                    for (int i = 0; i < dtprosample.Rows.Count; i++)
                    {
                        //  int count = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).FindAll(ty =>
                        //ty.SamplePos == Convert.ToInt32(dtprosample.Rows[i]["Position"])).Count;
                        int count = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).FindAll(ty =>
                            ty.SampleNo == dtprosample.Rows[i]["SampleNo"].ToString()).Count;
                        if (count != 0)
                            continue;
                        string[] projectName = dtprosample.Rows[i]["ProjectName"].ToString().TrimEnd().Split(' ');
                        for (int j = 0; j < projectName.Length; j++)
                        {
                            DataRow[] dr = dtproinfo.Select("ShortName ='" + projectName[j] + "'");
                            string projectProcedure = (string)dr[0]["ProjectProcedure"];
                            dtSampleInfo.Rows.Add(dtprosample.Rows[i]["SampleID"], dtprosample.Rows[i]["SampleType"],
                                dtprosample.Rows[i]["SampleNo"], dtprosample.Rows[i]["RepeatCount"], dtprosample.Rows[i]["Position"],
                                dtprosample.Rows[i]["SampleContainer"], 4, projectProcedure, projectName[j]);
                        }
                    }
                    #endregion
                    if (dtSampleInfo.Rows.Count == 0 || dtSampleInfo == null)
                    {
                        LoadingHelper.CloseForm();
                        EmergencyFlag = false;
                        frmAddSample.newSample = false;
                        Goon();//2018-10-18 zlx mod
                        return;
                    }
                    #region 将dataTable转换为list<TestMethod>类型
                    Dictionary<string, Type> dEnum = new Dictionary<string, Type>();
                    dEnum.Add("StepName", typeof(ExperimentStep));
                    List<TestMethod> templisTestMethod = (List<TestMethod>)ConvertHelper.DataTableConvertToListGenuric<TestMethod>(MethodInit(dtSampleInfo), dEnum);
                    #endregion
                    //合并加液步骤，对进度进行赋值
                    List<TestSchedule> templisTestSchedule = MethodMergeSchedule(templisTestMethod);
                    //对进度列表进行排序，并按照所排的顺序对TestID按照顺序赋值
                    templisTestSchedule = orderModifyTestID(templisTestSchedule);
                    int tempSamNum = templisTestSchedule.FindAll(tx => tx.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube).Count;
                    //查询还未开始运行的标准品和定标液数量或急诊样本数量
                    int NoStartScalNum = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).FindAll(ty =>
                        ty.TestID >= NoStartTestId && (ty.SampleType.Contains(getString("keywordText.Standard")) || ty.SampleType.Contains(getString("keywordText.CalibrationSolution")))).Count;
                    int NoStartTestID = NoStartTestId + NoStartScalNum;
                    //被延后的反应管的所在位置
                    int OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestID - 1 &&
                    ((ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube) ||
                        (ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis))).AddSamplePos;
                    //急诊样本testID赋值
                    for (int j = 0; j < templisTestSchedule.Count; j++)
                    {
                        TestSchedule TempTestSchedule = templisTestSchedule[j];
                        TempTestSchedule.TestID = TempTestSchedule.TestID + NoStartTestID - 1;
                        if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                        {
                            //稀释实验赋值
                            if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                            {
                                string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                                if (diupos.Length > 1)
                                {
                                    string newpos = "";
                                    for (int i = 0; i < diupos.Length; i++)
                                    {
                                        if (newpos == "")
                                        {
                                            newpos = "1";
                                        }
                                        else
                                        {
                                            newpos += "-" + (i + 1).ToString();
                                        }
                                    }
                                    TempTestSchedule.dilutionPos = newpos;
                                }
                                else
                                {
                                    TempTestSchedule.dilutionPos = "1";
                                }
                                string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                                TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                            }
                            else
                            {
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                                TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                            }
                        }
                    }
                    //已生成进度条testid修改值
                    for (int i = 0; i < lisTestSchedule.Count; i++)
                    {
                        if (lisTestSchedule[i].TestID >= NoStartTestID)
                        {
                            TestSchedule TempTestSchedule = lisTestSchedule[i];
                            //还存在问题
                            TempTestSchedule.TestID = templisTestSchedule[templisTestSchedule.Count - 1].TestID - NoStartTestID + 1 + TempTestSchedule.TestID;
                            if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                            {
                                if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                                {
                                    string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                                    if (diupos.Length > 1)
                                    {
                                        //2018-06-09 zlx  mod
                                        string newpos = "";
                                        for (int j = 0; j < diupos.Length; j++)
                                        {
                                            if (newpos == "")
                                            {
                                                newpos = "1";
                                            }
                                            else
                                            {
                                                newpos += "-" + (j + 1).ToString();
                                            }
                                        }
                                        TempTestSchedule.dilutionPos = newpos;
                                    }
                                    else
                                    {
                                        TempTestSchedule.dilutionPos = "1";
                                    }
                                    string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                                    TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                                    OldAddSamPos = OldAddSamPos + 1;
                                    if (OldAddSamPos > ReactTrayHoleNum)
                                    {
                                        OldAddSamPos = 4;
                                    }
                                    TempTestSchedule.AddSamplePos = OldAddSamPos;
                                }
                                else
                                {
                                    OldAddSamPos = OldAddSamPos + 1;
                                    if (OldAddSamPos > ReactTrayHoleNum)
                                    {
                                        OldAddSamPos = 4;
                                    }
                                    TempTestSchedule.AddSamplePos = OldAddSamPos;
                                    TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                                }
                            }
                        }
                    }
                    //急诊样本之前的标准品还有未运行的
                    NoStartTestID = NoStartTestID - NoStartScalNum;
                    //获取添加新的急诊样本后的当前样本数量
                    SampleNumCurrent = SampleNumCurrent + tempSamNum;
                    //templisTestSchedule.Sort(new SortRun());//按照步骤开始顺序排序
                    //将实验运行时的样本合并到已经生成进度的列表中
                    lisTestSchedule.AddRange(templisTestSchedule);
                    //按照testid进行排序
                    lisTestSchedule.Sort(new SortEmergency());
                    //进度计算
                    #region 控件清空还未开始运行的进度条控件
                    while (dgvWorkListData.Controls.Count > 2)
                    {
                        this.dgvWorkListData.Controls.Clear();//清除已有的控件
                    }
                    for (int i = BTestItem.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        BTestItem.Remove(BTestItem[i]);
                    }
                    //清空之前获取已经开始实验的进度条控件
                    for (int i = lisProBar.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        lisProBar.Remove(lisProBar[i]);
                    }

                    //JUN Add 2019-3-6 确保列表中没有已经完成的实验
                    if (RunFlag != (int)RunFlagStart.IsRuning)
                    {
                        while (dgvWorkListData.Controls.Count > 2)
                        {
                            this.dgvWorkListData.Controls.Clear();//清除已有的控件
                        }
                        for (int i = BTestItem.Count - 1; i >= 0; i--)
                        {
                            BTestItem.Remove(BTestItem[i]);
                        }
                        //清空之前获取已经开始实验的进度条控件
                        for (int i = lisProBar.Count - 1; i >= 0; i--)
                        {
                            lisProBar.Remove(lisProBar[i]);
                        }
                    }
                    #endregion
                    //将新添加的急诊进度和未开始运行的样本进度附加到datagridview中
                    BindData(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), NoStartTestID);
                    #region 获取已经开始实验的样本的空闲时间
                    //获取已经开始运行的样本的进度表
                    List<TestSchedule> RunSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    List<string> runFreeTime = GetFreeTime(RunSchedule);
                    #endregion
                    //未运行的样本进度计算
                    List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), runFreeTime);
                    //清空之前获取已经开始实验的进度条控件
                    lisTestSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    lisTestSchedule.AddRange(lisTestNoRun);
                    lisTestSchedule.Sort(new SortRun());
                    #endregion
                }
                else if (addOrdinaryFlag)
                {
                    #region 追加普通样本
                    #region 获取样本信息
                    //查询添加的急诊样本
                    DbHelperOleDb db = new DbHelperOleDb(1);
                    //DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                    //    + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                    //    + "# and Status = 0  and Emergency = 0 order by CInt([Position])").Tables[0];
                    DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                        + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                        + "# and Status = 0  and Emergency = 0 order by SampleNo").Tables[0];
                    db = new DbHelperOleDb(1);
                    DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Emergency = 1 where Status = 0 and Emergency = 0 and SendDateTime >=#"
                        + DateTime.Now.ToString("yyyy-MM-dd") + "#and SendDateTime <#"
                        + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + "#");
                    db = new DbHelperOleDb(0);
                    DataTable dtproinfo = bllproject.GetList("").Tables[0];
                    for (int i = 0; i < dtprosample.Rows.Count; i++)
                    {
                        //20180528 zlx add
                        //int count = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).FindAll(ty =>
                        //  ty.SamplePos == Convert.ToInt32(dtprosample.Rows[i]["Position"])).Count;
                        //Jun mod
                        int count = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).FindAll(ty =>
                            ty.SampleNo == dtprosample.Rows[i]["SampleNo"].ToString()).Count;
                        if (count != 0)
                            continue;
                        string[] projectName = dtprosample.Rows[i]["ProjectName"].ToString().TrimEnd().Split(' ');
                        for (int j = 0; j < projectName.Length; j++)
                        {
                            DataRow[] dr = dtproinfo.Select("ShortName ='" + projectName[j] + "'");
                            string projectProcedure = (string)dr[0]["ProjectProcedure"];
                            dtSampleInfo.Rows.Add(dtprosample.Rows[i]["SampleID"], dtprosample.Rows[i]["SampleType"],
                                dtprosample.Rows[i]["SampleNo"], dtprosample.Rows[i]["RepeatCount"], dtprosample.Rows[i]["Position"],
                                dtprosample.Rows[i]["SampleContainer"], 4, projectProcedure, projectName[j]);
                        }
                    }
                    #endregion
                    if (dtSampleInfo.Rows.Count == 0 || dtSampleInfo == null)
                    {
                        LoadingHelper.CloseForm();
                        addOrdinaryFlag = false;
                        frmAddSample.newSample = false;
                        Goon();
                        return;
                    }
                    #region 将dataTable转换为list<TestMethod>类型
                    Dictionary<string, Type> dEnum = new Dictionary<string, Type>();
                    dEnum.Add("StepName", typeof(ExperimentStep));
                    List<TestMethod> templisTestMethod = (List<TestMethod>)ConvertHelper.DataTableConvertToListGenuric<TestMethod>(MethodInit(dtSampleInfo), dEnum);
                    #endregion
                    //合并加液步骤，对进度进行赋值
                    List<TestSchedule> templisTestSchedule = MethodMergeSchedule(templisTestMethod);
                    //对进度列表进行排序，并按照所排的顺序对TestID按照顺序赋值
                    templisTestSchedule = orderModifyTestID(templisTestSchedule);
                    int tempSamNum = templisTestSchedule.FindAll(tx => tx.TestScheduleStep ==
                        TestSchedule.ExperimentScheduleStep.AddLiquidTube).Count;
                    int NoStartTestID = dgvWorkListData.Rows.Count + 1;
                    //被延后的反应管的所在位置
                    int OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestID - 1 && ((
                        ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube) ||
                        (ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis))).AddSamplePos;
                    //追加样本testID赋值
                    for (int j = 0; j < templisTestSchedule.Count; j++)
                    {
                        TestSchedule TempTestSchedule = templisTestSchedule[j];
                        TempTestSchedule.TestID = TempTestSchedule.TestID + NoStartTestID - 1;
                        if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                        {
                            //稀释实验赋值
                            if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                            {
                                string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                                if (diupos.Length > 1)
                                {
                                    string newpos = "";
                                    for (int i = 0; i < diupos.Length; i++)
                                    {
                                        if (newpos == "")
                                        {
                                            newpos = "1";
                                        }
                                        else
                                        {
                                            newpos += "-" + (i + 1).ToString();
                                        }
                                    }
                                    TempTestSchedule.dilutionPos = newpos;
                                }
                                else
                                {
                                    TempTestSchedule.dilutionPos = "1";
                                }
                                string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                                TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                            }
                            else
                            {
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                                TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                            }
                        }
                    }
                    //追加样本之前还有未运行的样本
                    NoStartTestID = NoStartTestId;
                    //获取添加新的普通样本后的当前样本数量
                    SampleNumCurrent = SampleNumCurrent + tempSamNum;//2018-07-11 lyn mod
                    //将实验运行时的样本合并到已经生成进度的列表中
                    lisTestSchedule.AddRange(templisTestSchedule);
                    //按照testid进行排序
                    lisTestSchedule.Sort(new SortEmergency());
                    //进度计算
                    #region 控件清空还未开始运行的进度条控件
                    while (dgvWorkListData.Controls.Count > 2)
                    {
                        this.dgvWorkListData.Controls.Clear();//清除已有的控件
                    }
                    for (int i = BTestItem.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        BTestItem.Remove(BTestItem[i]);
                    }
                    //清空之前获取已经开始实验的进度条控件
                    for (int i = lisProBar.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        lisProBar.Remove(lisProBar[i]);
                    }
                    #endregion
                    //将新添加的急诊进度和未开始运行的样本进度附加到datagridview中
                    BindData(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), NoStartTestID);
                    #region 获取已经开始实验的样本的空闲时间
                    //获取已经开始运行的样本的进度表
                    List<TestSchedule> RunSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    List<string> runFreeTime = GetFreeTime(RunSchedule);
                    #endregion
                    //未运行的样本进度计算
                    List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), runFreeTime);
                    //清空之前获取已经开始实验的进度条控件
                    lisTestSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    lisTestSchedule.AddRange(lisTestNoRun);
                    lisTestSchedule.Sort(new SortRun());
                    #endregion
                }
                else
                {
                    #region 重检
                    #region 获取样本信息
                    //查询添加的急诊样本
                    DbHelperOleDb db = new DbHelperOleDb(1);
                    DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                        + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                        + "# and Status = 0  and Emergency = 6 order by SampleNo").Tables[0];
                    db = new DbHelperOleDb(1);
                    DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Emergency = 1 where Status = 0 and Emergency = 6 and SendDateTime >=#"
                        + DateTime.Now.ToString("yyyy-MM-dd") + "#and SendDateTime <#"
                        + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + "#");
                    db = new DbHelperOleDb(0);
                    DataTable dtproinfo = bllproject.GetList("").Tables[0];
                    for (int i = 0; i < dtprosample.Rows.Count; i++)
                    {
                        string[] projectName = dtprosample.Rows[i]["ProjectName"].ToString().TrimEnd().Split(' ');
                        for (int j = 0; j < projectName.Length; j++)
                        {
                            DataRow[] dr = dtproinfo.Select("ShortName ='" + projectName[j] + "'");
                            string projectProcedure = (string)dr[0]["ProjectProcedure"];
                            dtSampleInfo.Rows.Add(dtprosample.Rows[i]["SampleID"], dtprosample.Rows[i]["SampleType"],
                                dtprosample.Rows[i]["SampleNo"], dtprosample.Rows[i]["RepeatCount"], dtprosample.Rows[i]["Position"],
                                dtprosample.Rows[i]["SampleContainer"], 4, projectProcedure, projectName[j]);
                        }
                    }
                    #endregion
                    if (dtSampleInfo.Rows.Count == 0 || dtSampleInfo == null)
                    {
                        LoadingHelper.CloseForm();
                        RetestFlag = false;
                        frmAddSample.newSample = false;
                        Goon();
                        return;
                    }
                    #region 将dataTable转换为list<TestMethod>类型
                    Dictionary<string, Type> dEnum = new Dictionary<string, Type>();
                    dEnum.Add("StepName", typeof(ExperimentStep));
                    List<TestMethod> templisTestMethod = (List<TestMethod>)ConvertHelper.DataTableConvertToListGenuric<TestMethod>(MethodInit(dtSampleInfo), dEnum);
                    #endregion
                    //合并加液步骤，对进度进行赋值
                    List<TestSchedule> templisTestSchedule = MethodMergeSchedule(templisTestMethod);
                    //对进度列表进行排序，并按照所排的顺序对TestID按照顺序赋值
                    templisTestSchedule = orderModifyTestID(templisTestSchedule);
                    int tempSamNum = templisTestSchedule.FindAll(tx => tx.TestScheduleStep ==
                        TestSchedule.ExperimentScheduleStep.AddLiquidTube).Count;
                    int NoStartTestID = dgvWorkListData.Rows.Count + 1;
                    //被延后的反应管的所在位置
                    int OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestID - 1 && ((
                        ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube) ||
                        (ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis))).AddSamplePos;
                    //追加样本testID赋值
                    for (int j = 0; j < templisTestSchedule.Count; j++)
                    {
                        TestSchedule TempTestSchedule = templisTestSchedule[j];
                        TempTestSchedule.TestID = TempTestSchedule.TestID + NoStartTestID - 1;
                        if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                        {
                            //稀释实验赋值
                            if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                            {
                                string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                                if (diupos.Length > 1)
                                {
                                    string newpos = "";
                                    for (int i = 0; i < diupos.Length; i++)
                                    {
                                        if (newpos == "")
                                        {
                                            newpos = "1";
                                        }
                                        else
                                        {
                                            newpos += "-" + (i + 1).ToString();
                                        }
                                    }
                                    TempTestSchedule.dilutionPos = newpos;
                                }
                                else
                                {
                                    TempTestSchedule.dilutionPos = "1";
                                }
                                string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                                TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                            }
                            else
                            {
                                OldAddSamPos = OldAddSamPos + 1;
                                if (OldAddSamPos > ReactTrayHoleNum)
                                {
                                    OldAddSamPos = 4;
                                }
                                TempTestSchedule.AddSamplePos = OldAddSamPos;
                                TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                            }
                        }
                    }
                    //追加样本之前还有未运行的样本
                    NoStartTestID = NoStartTestId;
                    //获取添加新的普通样本后的当前样本数量
                    SampleNumCurrent = SampleNumCurrent + tempSamNum;//2018-07-11 lyn mod
                    //将实验运行时的样本合并到已经生成进度的列表中
                    lisTestSchedule.AddRange(templisTestSchedule);
                    //按照testid进行排序
                    lisTestSchedule.Sort(new SortEmergency());
                    //进度计算
                    #region 控件清空还未开始运行的进度条控件
                    while (dgvWorkListData.Controls.Count > 2)
                    {
                        this.dgvWorkListData.Controls.Clear();//清除已有的控件
                    }
                    for (int i = BTestItem.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        BTestItem.Remove(BTestItem[i]);
                    }
                    //清空之前获取已经开始实验的进度条控件
                    for (int i = lisProBar.Count - 1; i >= NoStartTestID - 1; i--)
                    {
                        lisProBar.Remove(lisProBar[i]);
                    }
                    #endregion
                    //将新添加的急诊进度和未开始运行的样本进度附加到datagridview中
                    BindData(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), NoStartTestID);
                    #region 获取已经开始实验的样本的空闲时间
                    //获取已经开始运行的样本的进度表
                    List<TestSchedule> RunSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    List<string> runFreeTime = GetFreeTime(RunSchedule);
                    #endregion
                    //未运行的样本进度计算
                    List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestID), runFreeTime);
                    //清空之前获取已经开始实验的进度条控件
                    lisTestSchedule = lisTestSchedule.FindAll(tx => tx.TestID < NoStartTestID);
                    lisTestSchedule.AddRange(lisTestNoRun);
                    lisTestSchedule.Sort(new SortRun());
                    #endregion
                }
            }
            else
            {
                LogFile.Instance.Write("开始进行计时操作" + DateTime.Now.ToString("mm:ss:ms"));
                #region 获取需要实验的样本
                DbHelperOleDb db = new DbHelperOleDb(1);
                DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Emergency = 3 where Status = 0 and Emergency = 2 and SendDateTime >=#"
                    + DateTime.Now.ToString("yyyy-MM-dd") + "#and SendDateTime <#"
                    + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + "#");
                db = new DbHelperOleDb(1);
                DataTable dtprosample = bllsampleinfo.GetList(" SendDateTime >=#" + DateTime.Now.ToString("yyyy-MM-dd")
                    + "#and SendDateTime <#" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
                    + "# and Status = 0 order by Emergency DESC,SampleNo ASC").Tables[0];
                db = new DbHelperOleDb(0);
                DataTable dtproinfo = bllproject.GetList("").Tables[0];
                for (int i = 0; i < dtprosample.Rows.Count; i++)
                {
                    string[] projectName = dtprosample.Rows[i]["ProjectName"].ToString().TrimEnd().Split(' ');
                    for (int j = 0; j < projectName.Length; j++)
                    {
                        DataRow[] dr = dtproinfo.Select("ShortName ='" + projectName[j] + "'");
                        string projectProcedure = (string)dr[0]["ProjectProcedure"];
                        dtSampleInfo.Rows.Add(dtprosample.Rows[i]["SampleID"], dtprosample.Rows[i]["SampleType"],
                            dtprosample.Rows[i]["SampleNo"], dtprosample.Rows[i]["RepeatCount"], dtprosample.Rows[i]["Position"],
                            dtprosample.Rows[i]["SampleContainer"], dtprosample.Rows[i]["Emergency"],
                            projectProcedure, projectName[j]);
                    }
                }

                #endregion
                if (dtSampleInfo.Rows.Count == 0 || dtSampleInfo == null)
                {
                    while (LoadingHelper.loadingForm == null)
                    {
                        Thread.Sleep(30);
                    }
                    LoadingHelper.CloseForm();
                    return;
                }
                #region 将dataTable转换为list<TestMethod>类型
                Dictionary<string, Type> dEnum = new Dictionary<string, Type>();
                dEnum.Add("StepName", typeof(ExperimentStep));
                lisTestMethod = (List<TestMethod>)ConvertHelper.DataTableConvertToListGenuric<TestMethod>(MethodInit(dtSampleInfo), dEnum);
                #endregion
                //合并加液步骤，对进度进行赋值
                lisTestSchedule = MethodMergeSchedule(lisTestMethod);
                //对进度列表进行排序，并按照所排的顺序对TestID按照顺序赋值
                lisTestSchedule = orderModifyTestID(lisTestSchedule);
                //dgvWorkListData控件上绑定进度排好的数据
                cleanDgvShowData(1);
                BindData(lisTestSchedule, 1);
                //进度计算
                lisTestSchedule = ExperimentalScheduleAlgorithm(lisTestSchedule, new List<string>() { "0-1000000" });
                lisTestSchedule.Sort(new SortRun());
            }
            #endregion
            if ((EmergencyFlag || addOrdinaryFlag || RetestFlag) && (RunFlag == (int)RunFlagStart.IsRuning))
            {
                List<TestSchedule> tss = lisTestSchedule.FindAll(tx => tx.StartTime <= sumTime);
                _GaDoingOne = tss[tss.Count - 1];
                TestStep = GaNextOne();
            }
            frmAddSample.newSample = false;
            LoadingHelper.CloseForm();
            NetCom3.ComWait.Set();
            frmSampleLoad.CaculatingFlag = false;
            EmergencyFlag = false;
            addOrdinaryFlag = false;
            RetestFlag = false;
            if (frmMain.pauseFlag == true)
            {
                GetNoStartList(); //lyq 20200824
            }

            StopWatchAddOrSubtractEmergency();//增加倒计时的值
            LogFile.Instance.Write("结束进行计时操作" + DateTime.Now.ToString("mm:ss:ms"));
        }
        #region 清除工作界面展示数据
        /// <summary>
        /// 清除工作界面展示数据，JUN add 2019-03-06
        /// </summary>
        /// <returns></returns>
        /// <summary>
        void cleanDgvShowData(int NoStartTestID)
        {
            while (dgvWorkListData.Controls.Count > 2)
            {
                this.dgvWorkListData.Controls.Clear();//清除已有的控件
            }
            for (int i = BTestItem.Count - 1; i >= NoStartTestID - 1; i--)
            {
                BTestItem.Remove(BTestItem[i]);
            }
            for (int i = lisProBar.Count - 1; i >= NoStartTestID - 1; i--)
            {
                lisProBar.Remove(lisProBar[i]);
            }
        }
        #endregion
        System.Timers.Timer stopTimer = new System.Timers.Timer();//倒计时timer
        int LastMaxTime = 0;//记录加急诊前的试验时间最大值
        double MaxTime = 0;//记录实验剩余时间
        int LastSumTime = 0;//记录上一次sunmtime的值
        const float PiusTimes = 1f;//增长的beilv
        void SetStopWatch()//实验倒计时,初始化并开始
        {
            MaxTime = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            LastMaxTime = (int)MaxTime;
            MaxTime = MaxTime - sumTime + 180 * 2;
            MaxTime *= PiusTimes;
            LastSumTime = 0;
            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            LogFile.Instance.Write("**********时间更新1：" + DateTime.Now.ToString("HH-mm-ss") +
                " H :" + ((int)span.TotalHours).ToString("00") +
                "M :" + span.Minutes.ToString("00") + "**********");
            TimeLabel2.Invoke(new Action(() =>
            {
                TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                TimeLabel3.Text = span.Minutes.ToString("00");
                TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = true;
            }));

            LeftTestTime = Convert.ToInt32(TimeLabel2.Text) * 60 + Convert.ToInt32(TimeLabel3.Text);

            stopTimer.Elapsed += StopWatchDecrease;
            stopTimer.Interval = 60000;
            stopTimer.Start();
        }
        void StopStopWatch()//结束并清理倒计时
        {
            stopTimer.Stop();
            stopTimer.Elapsed -= StopWatchDecrease;

            LogFile.Instance.Write("**********时间更新4：" + DateTime.Now.ToString("HH-mm-ss") +
                " StopStopWatch");

            while (!TimeLabel2.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            LogFile.Instance.Write("**********时间更新5：" + DateTime.Now.ToString("HH-mm-ss"));
            lock (TimeLabel2)
            {
                TimeLabel2.Invoke(new Action(() =>
                {
                    TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = false;
                    return;
                }));
            }

            LeftTestTime = Convert.ToInt32(TimeLabel2.Text) * 60 + Convert.ToInt32(TimeLabel3.Text);
        }
        void StopWatchAddOrSubtractEmergency()//加急诊之后增加倒计时,或者减去试验后减少倒计时
        {
            int temp = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            int margin = temp - LastMaxTime;
            LastMaxTime = temp;
            MaxTime = MaxTime + (margin * PiusTimes);
        }
        void StopWatchDecrease(object sender, ElapsedEventArgs e)//倒计时计时器每分钟引发的事件
        {
            LogFile.Instance.Write("**********时间更新2：" + DateTime.Now.ToString("HH-mm-ss") +
               " RunFlag :" + (int)RunFlagStart.IsRuning + " MaxTime :" + MaxTime + "**********");
            if (RunFlag == (int)RunFlagStart.IsRuning)
            {
                if (sumTime == 0)
                {
                    MaxTime -= 60;
                }
                else
                {
                    if (_GaDoingOne == null)
                    {
                        MaxTime -= 60;
                    }
                    else
                    {
                        double temp = (sumTime - LastSumTime) * PiusTimes;
                        MaxTime -= temp;
                        LastSumTime = sumTime;
                    }
                }
                if (MaxTime <= 0)
                {
                    MaxTime = 0;
                }
                TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
                while (!this.IsHandleCreated)
                {
                    Thread.Sleep(30);
                }
                LogFile.Instance.Write("**********时间更新3：" + DateTime.Now.ToString("HH-mm-ss") +
                " H :" + ((int)span.TotalHours).ToString("00") +
                "M :" + span.Minutes.ToString("00") + "**********");
                TimeLabel2.Invoke(new Action(() =>
                {
                    TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                    TimeLabel3.Text = span.Minutes.ToString("00");
                }));
            }
            else
            {
                StopStopWatch();
            }

            LeftTestTime = Convert.ToInt32(TimeLabel2.Text) * 60 + Convert.ToInt32(TimeLabel3.Text);
        }
        /// <summary>
        /// 实验步骤初始化
        /// </summary>
        /// <param name="dtSampleInfo">读取的符合条件的样本信息，可以实验运行之前生成的样本信息也可以时实验运行中添加的急诊样本信息</param>
        /// <returns></returns>

        public DataTable MethodInit(DataTable dtSampleInfo)
        {
            #region 从数据库中读取样本信息，初始化实验步骤存储到DataTable

            //新建datatable保存实验的每个步骤
            DataTable dtStepInfo = new DataTable();
            dtStepInfo.Columns.Add("SampleID", typeof(int));
            dtStepInfo.Columns.Add("TestID", typeof(int));
            dtStepInfo.Columns.Add("StepID", typeof(int));
            dtStepInfo.Columns.Add("SampleNo", typeof(string));
            dtStepInfo.Columns.Add("SampleContainer", typeof(string));
            dtStepInfo.Columns.Add("SamplePos", typeof(int));
            dtStepInfo.Columns.Add("ItemName", typeof(string));
            dtStepInfo.Columns.Add("AddLiqud", typeof(string));
            dtStepInfo.Columns.Add("Emergency", typeof(int));
            dtStepInfo.Columns.Add("StepName", typeof(string));
            dtStepInfo.Columns.Add("ProMethod", typeof(int));
            dtStepInfo.Columns.Add("timeLength", typeof(int));
            //清洗盘状态表中新添加一个字段，存放稀释倍数。
            dtStepInfo.Columns.Add("dilutionTimes", typeof(string));

            int testID = 1;
            int stepID = 1;
            DbHelperOleDb db = new DbHelperOleDb(1);
            DataTable dtTestData = new BLL.tbAssayResult().GetList("").Tables[0];
            for (int i = 0; i < dtSampleInfo.Rows.Count; i++)
            {
                DataRow[] drTestData = null;
                string Emergency = dtSampleInfo.Rows[i]["Emergency"].ToString();
                if (Emergency != "4" || Emergency != "5")
                {
                    drTestData = dtTestData.Select("SampleID="
                          + dtSampleInfo.Rows[i]["SampleID"] + " AND ItemName='"
                          + dtSampleInfo.Rows[i]["ShortName"].ToString() + "'AND Status >= 0");
                }
                if (drTestData != null && drTestData.Length > 0)
                {
                    if (drTestData.Length >= int.Parse(dtSampleInfo.Rows[i]["RepeatCount"].ToString()))
                        continue;
                    else
                        dtSampleInfo.Rows[i]["RepeatCount"] = int.Parse(dtSampleInfo.Rows[i]["RepeatCount"].
                            ToString()) - drTestData.Length;
                }
                string ProjectProcedure = dtSampleInfo.Rows[i]["ProjectProcedure"].ToString();
                string[] step = ProjectProcedure.Split(';');
                int tempProMethod = 1;
                for (int j = 0; j < int.Parse(dtSampleInfo.Rows[i]["RepeatCount"].ToString()); j++)
                {
                    #region 稀释步骤添加 
                    string name = dtSampleInfo.Rows[i]["ShortName"].ToString();
                    //从已有的样本信息表中获取当前样本的稀释信息
                    DataRow[] rows = dtSampleRunInfo.Select("ItemName='" + name + "' and SampleNo='"
                        + dtSampleInfo.Rows[i]["SampleNo"].ToString() + "'");
                    int DiuTimes = 1;
                    if (rows.Length == 0)
                    {
                        DiuTimes = 1;
                    }
                    else
                    {
                        if (rows[0]["SampleType"].ToString().Contains(getString("keywordText.Standard")) ||
                            rows[0]["SampleType"].ToString().Contains(getString("keywordText.CalibrationSolution")) ||
                            rows[0]["SampleType"].ToString().Contains(getString("keywordText.Calibrator")))
                        {
                            Emergency = "5";
                        }
                        else if (rows[0]["SampleType"].ToString().Contains(getString("keywordText.Control")))
                        {
                            Emergency = "4";
                        }
                        else if (rows[0]["SampleType"].ToString().Contains(getString("keywordText.CrossContamination")))//lyq add 20190830
                        {
                            Emergency = (int.Parse(dtSampleInfo.Rows[i]["RepeatCount"].ToString()) - j - 6).ToString();
                        }
                        else
                        {
                            //获取急诊信息
                            Emergency = rows[0]["Emergency"].ToString() == getString("keywordText.Is") ?
                                ((frmWorkList.RunFlag == (int)RunFlagStart.IsRuning) ? "2" : "3") :
                                (frmWorkList.RunFlag == (int)RunFlagStart.IsRuning ? "0" : "1");
                        }
                        if (rows.Length > 0 &&
                            !(rows[0]["SampleType"].ToString().Contains(getString("keywordText.Standard"))
                            || rows[0]["SampleType"].ToString().Contains(getString("keywordText.Control"))
                            || rows[0]["SampleType"].ToString().Contains(getString("keywordText.Calibrator"))))
                        {
                            //获取稀释倍数
                            DiuTimes = int.Parse(rows[0]["DilutionTimes"].ToString());
                        }
                    }
                    //加液量
                    string[] Addliqud = step[0].Split('-');
                    if (DiuTimes > 1)//稀释倍数大于1
                    {
                        dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                            dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                            dtSampleInfo.Rows[i]["Position"],
                                dtSampleInfo.Rows[i]["ShortName"], Addliqud[1], Emergency,
                                ExperimentStep.Dilution, 1, dilutionTime, DiuTimes);
                        stepID++;
                    }
                    #endregion
                    for (int k = 0; k < step.Length; k++)
                    {
                        string[] AddLiqud = step[k].Split('-');
                        if (step[k].Contains("S"))
                        {
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"], AddLiqud[1],
                                Emergency, ExperimentStep.AddSample, 1, SampleTime, DiuTimes);
                            stepID++;
                        }
                        else if (step[k].Contains("R1"))
                        {//加试剂1步骤
                            if (k < 3)
                            {
                                tempProMethod = 1;
                            }
                            else
                            {
                                tempProMethod = 2;
                            }
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddRegent1, tempProMethod, RegentTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("R2"))
                        {//加试剂2步骤
                            if (k < 3)
                            {
                                tempProMethod = 1;
                            }
                            else
                            {
                                tempProMethod = 2;
                            }
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddRegent2, tempProMethod, RegentTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("R3"))
                        {//加试剂3步骤
                            if (k < 3)
                            {
                                tempProMethod = 1;
                            }
                            else
                            {
                                tempProMethod = 2;
                            }
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddRegent3, tempProMethod, RegentTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("RD"))
                        {//加试剂D步骤
                            if (k < 3)
                            {
                                tempProMethod = 1;
                            }
                            else
                            {
                                tempProMethod = 2;
                            }
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddRegentD, tempProMethod, RegentTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("H"))
                        {//孵育

                            string[] incubationTime = step[k].Split('-');
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"], 0,
                                Emergency, ExperimentStep.Incubation, 0, int.Parse(incubationTime[1]) * 60, 1);
                            stepID++;

                        }
                        else if (step[k].Contains("B"))
                        {//加磁珠
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddBeads, 0, beadTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("W"))
                        {//清洗
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.Clean, 0, washTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("T"))
                        {//加底物
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"],
                                AddLiqud[1], Emergency, ExperimentStep.AddSubstrate, 0, substrateTime, 1);
                            stepID++;
                        }
                        else if (step[k].Contains("D"))
                        {//读数
                            dtStepInfo.Rows.Add(dtSampleInfo.Rows[i]["SampleID"], testID, stepID,
                                dtSampleInfo.Rows[i]["SampleNo"], dtSampleInfo.Rows[i]["SampleContainer"],
                                dtSampleInfo.Rows[i]["Position"], dtSampleInfo.Rows[i]["ShortName"], 0,
                                Emergency, ExperimentStep.Read, 0, readTime, 1);
                            stepID++;
                        }
                    }
                    stepID = 1;
                    testID++;
                }
            }
            #endregion
            return dtStepInfo;
        }
        /// <summary>
        /// 加液步骤按顺序进行合并加液的步骤，对进度进行赋值
        /// </summary>
        /// <param name="lisTM">各个样本的步骤方法</param>
        /// <returns></returns>
        public List<TestSchedule> MethodMergeSchedule(List<TestMethod> lisTM)
        {
            List<TestSchedule> TEMPlisTestSchedule = new List<TestSchedule>();
            int temp = 0;
            if (lisTM.Count == 0 || lisTM == null)
            {
                return TEMPlisTestSchedule;
            }
            DbHelperOleDb db = new DbHelperOleDb(0);
            DataTable dtProject = DbHelperOleDb.Query(0, @"select * from tbProject").Tables[0];
            for (int j = 1; j <= lisTM[lisTM.Count - 1].TestID; j++)
            {
                int testTimeLength = 0;
                //一个实验中该步骤在第几个运行
                int stepTime = 1;
                //获取相同项目和相同样本步骤数量
                int numItem = lisTM.Count(ty => ty.TestID == j);
                #region 加液步骤合并
                //判断合并的步骤已有几个
                int flagstep = 0;
                //获取当前试剂的索引
                int tempRegent = 0;

                for (int i = temp; i < temp + numItem; i++)
                {
                    switch (lisTM[i].StepName)
                    {
                        #region 稀释步骤合并到加液步骤中。LYN add 20171114
                        case ExperimentStep.Dilution:
                            //20180601 zlx add
                            testschedule.singleStep += "D-";
                            #region 新修改的稀释过程
                            //object DiluteCount = DbHelperOleDb.GetSingle(@"select DiluteCount from tbProject where ShortName 
                            //                                                 = '" + lisTM[i].ItemName + "'");
                            DataRow[] drProject = dtProject.Select("ShortName = '" + lisTM[i].ItemName + "'");
                            object DiluteCount = drProject[0]["DiluteCount"];
                            int ExtraDiluteCount = int.Parse(lisTM[i].dilutionTimes) / int.Parse(DiluteCount.ToString());
                            int diucount = 0;
                            if (ExtraDiluteCount > 1)
                            {
                                string diuInfo = DiuInfo.GetDiuInfo(ExtraDiluteCount);
                                foreach (string diu in diuInfo.Split(';'))
                                {
                                    if (diu != "")
                                    {
                                        DiuTubeNum = DiuTubeNum + 1;
                                        testTimeLength += dilutionTime;
                                        if (testschedule.dilutionPos == null)
                                        {
                                            ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                                            testschedule.dilutionPos = (ReactStartPos).ToString();
                                        }
                                        else
                                        {
                                            ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                                            testschedule.dilutionPos += "-" + ((ReactStartPos).ToString());
                                        }
                                    }
                                    diucount = diucount + 1;
                                }
                            }
                            if (int.Parse(DiluteCount.ToString()) > 1)
                            {
                                //db = new DbHelperOleDb(0);
                                object DiluteName = drProject[0]["DiluteName"];
                                //object DiluteName = DbHelperOleDb.GetSingle(@"select DiluteName from tbProject where ShortName 
                                //                                             = '" + lisTM[i].ItemName + "'");
                                foreach (string diu in DiluteName.ToString().Split(';'))
                                {
                                    if (diu != "")
                                    {
                                        DiuTubeNum = DiuTubeNum + 1;
                                        //testTimeLength += dilutionTime;
                                        if (testschedule.dilutionPos == null)
                                        {
                                            ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                                            testschedule.dilutionPos = (ReactStartPos).ToString();
                                        }
                                        else
                                        {
                                            ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                                            testschedule.dilutionPos += "-" + ((ReactStartPos).ToString());
                                        }
                                    }
                                    diucount = diucount + 1;
                                }
                            }
                            testschedule.dilutionTimes = lisTM[i].dilutionTimes;
                            testschedule.getSamplePos = "R" + (ReactStartPos).ToString();
                            #endregion
                            //testTimeLength = int.Parse(OperateIniFile.ReadInIPara("Time", "dilutionTime" + diucount + ""));
                            testTimeLength = DiuInfo.GetDiuTime(diucount);
                            testschedule.AddLiqud += lisTM[i].AddLiqud + "-";
                            ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                            testschedule.AddSamplePos = ReactStartPos;
                            break;

                        #endregion
                        case ExperimentStep.AddSample:
                            testTimeLength += SampleTime;
                            flagstep = ++flagstep;
                            testschedule.singleStep += "S";
                            testschedule.AddLiqud += lisTM[i].AddLiqud;
                            break;
                        case ExperimentStep.AddRegent1:
                            if (lisTM[i].ProMethod == 1)
                            {
                                testschedule.singleStep += "-R1";
                                testTimeLength += RegentTime;
                                testschedule.AddLiqud += "-" + lisTM[i].AddLiqud;
                                tempRegent = i;
                            }
                            break;
                        case ExperimentStep.AddRegent2:
                            if (lisTM[i].ProMethod == 1)
                            {
                                testschedule.singleStep += "-R2";
                                testTimeLength += RegentTime;
                                testschedule.AddLiqud += "-" + lisTM[i].AddLiqud;
                                tempRegent = i;
                            }
                            break;
                        case ExperimentStep.AddRegent3:

                            if (lisTM[i].ProMethod == 1)
                            {
                                testschedule.singleStep += "-R3";
                                testTimeLength += RegentTime;
                                testschedule.AddLiqud += "-" + lisTM[i].AddLiqud;
                                tempRegent = i;
                            }
                            break;
                        default:
                            continue;
                    }
                    testschedule.SampleNo = lisTM[i].SampleNo;
                    testschedule.SampleID = lisTM[i].SampleID;
                    testschedule.ItemName = lisTM[i].ItemName;
                    testschedule.TestID = lisTM[i].TestID;
                    testschedule.samplePos = lisTM[i].SamplePos;
                    testschedule.SampleContainer = lisTM[i].SampleContainer;
                    testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                    testschedule.ProMethod = lisTM[i].ProMethod;
                    testschedule.StartTime = 0;
                    testschedule.TimeLengh = testTimeLength;
                    testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                    testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddLiquidTube;
                    testschedule.Emergency = lisTM[i].Emergency;
                }
                //如果未稀释，如下赋值 LYN add 20171114
                if (testschedule.dilutionPos == null)
                {
                    ReactStartPos = ReactStartPos + 1 > ReactTrayHoleNum ? (ReactStartPos + 1) % ReactTrayHoleNum : ReactStartPos + 1;
                    testschedule.AddSamplePos = ReactStartPos;
                    testschedule.dilutionTimes = "1";
                    testschedule.getSamplePos = "S" + testschedule.samplePos.ToString();
                }
                testschedule.stepNum = stepTime++;
                TEMPlisTestSchedule.Add(testschedule);
                testschedule = new TestSchedule();
                #endregion

                #region 余下步骤依次赋值
                for (int i = temp; i < temp + numItem; i++)
                {
                    switch (lisTM[i].StepName)
                    {
                        case ExperimentStep.AddBeads:
                            testschedule.SampleNo = lisTM[i].SampleNo;
                            testschedule.SampleID = lisTM[i].SampleID;
                            testschedule.ItemName = lisTM[i].ItemName;
                            testschedule.TestID = lisTM[i].TestID;
                            testschedule.samplePos = lisTM[i].SamplePos;
                            testschedule.SampleContainer = lisTM[i].SampleContainer;
                            testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                            testschedule.StartTime = 0;
                            testschedule.TimeLengh = beadTime;
                            testschedule.stepNum = 3;
                            testschedule.stepNum = stepTime++;
                            testschedule.AddLiqud = lisTM[i].AddLiqud;
                            testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                            testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddBeads;
                            testschedule.ProMethod = lisTM[i].ProMethod;
                            testschedule.Emergency = lisTM[i].Emergency;
                            //稀释相关字段赋值。 LYN add 20171114
                            testschedule.dilutionPos = "";
                            testschedule.AddSamplePos = 0;
                            testschedule.dilutionTimes = "1";
                            testschedule.getSamplePos = "";
                            TEMPlisTestSchedule.Add(testschedule);
                            testschedule = new TestSchedule();
                            break;
                        case ExperimentStep.Incubation:
                            testschedule.SampleNo = lisTM[i].SampleNo;
                            testschedule.SampleID = lisTM[i].SampleID;
                            testschedule.ItemName = lisTM[i].ItemName;
                            testschedule.TestID = lisTM[i].TestID;
                            testschedule.samplePos = lisTM[i].SamplePos;
                            testschedule.SampleContainer = lisTM[i].SampleContainer;
                            testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                            testschedule.StartTime = 0;
                            testschedule.stepNum = stepTime++;
                            testschedule.TimeLengh = lisTM[i].timeLength;
                            testschedule.AddLiqud = lisTM[i].AddLiqud;
                            testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                            testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.Incubation;
                            testschedule.Emergency = lisTM[i].Emergency;
                            testschedule.ProMethod = lisTM[i].ProMethod;
                            //稀释相关字段赋值。 LYN add 20171114
                            testschedule.dilutionPos = "";
                            testschedule.AddSamplePos = 0;
                            testschedule.dilutionTimes = "1";
                            testschedule.getSamplePos = "";
                            TEMPlisTestSchedule.Add(testschedule);
                            testschedule = new TestSchedule();
                            break;
                        case ExperimentStep.AddRegent1:
                            if (lisTM[i].ProMethod == 2)
                            {
                                testschedule.SampleNo = lisTM[i].SampleNo;
                                testschedule.SampleID = lisTM[i].SampleID;
                                testschedule.ItemName = lisTM[i].ItemName;
                                testschedule.TestID = lisTM[i].TestID;
                                testschedule.samplePos = lisTM[i].SamplePos;
                                testschedule.SampleContainer = lisTM[i].SampleContainer;
                                testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                                testschedule.AddLiqud = lisTM[i].AddLiqud;
                                testschedule.StartTime = 0;
                                testschedule.TimeLengh = RegentTime;
                                testschedule.stepNum = stepTime++;
                                testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                                testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddSingleR;
                                testschedule.Emergency = lisTM[i].Emergency;
                                testschedule.ProMethod = lisTM[i].ProMethod;
                                //稀释相关字段赋值。 LYN add 20171114
                                testschedule.dilutionPos = "";
                                testschedule.AddSamplePos = 0;
                                testschedule.dilutionTimes = "1";
                                testschedule.getSamplePos = "";
                                testschedule.singleStep = "R1";
                                TEMPlisTestSchedule.Add(testschedule);
                                testschedule = new TestSchedule();
                            }
                            break;
                        case ExperimentStep.AddRegent2:
                            if (lisTM[i].ProMethod == 2)
                            {
                                testschedule.SampleNo = lisTM[i].SampleNo;
                                testschedule.SampleID = lisTM[i].SampleID;
                                testschedule.ItemName = lisTM[i].ItemName;
                                testschedule.TestID = lisTM[i].TestID;
                                testschedule.samplePos = lisTM[i].SamplePos;
                                testschedule.SampleContainer = lisTM[i].SampleContainer;
                                testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                                testschedule.AddLiqud = lisTM[i].AddLiqud;
                                testschedule.StartTime = 0;
                                testschedule.TimeLengh = RegentTime;
                                testschedule.stepNum = stepTime++;
                                testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                                testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddSingleR;
                                testschedule.Emergency = lisTM[i].Emergency;
                                testschedule.ProMethod = lisTM[i].ProMethod;
                                //稀释相关字段赋值。 LYN add 20171114
                                testschedule.dilutionPos = "";
                                testschedule.AddSamplePos = 0;
                                testschedule.dilutionTimes = "1";
                                testschedule.getSamplePos = "";
                                testschedule.singleStep = "R2";
                                TEMPlisTestSchedule.Add(testschedule);
                                testschedule = new TestSchedule();
                            }
                            break;
                        case ExperimentStep.AddRegent3:
                            if (lisTM[i].ProMethod == 2)
                            {
                                testschedule.SampleNo = lisTM[i].SampleNo;
                                testschedule.SampleID = lisTM[i].SampleID;
                                testschedule.ItemName = lisTM[i].ItemName;
                                testschedule.TestID = lisTM[i].TestID;
                                testschedule.samplePos = lisTM[i].SamplePos;
                                testschedule.SampleContainer = lisTM[i].SampleContainer;
                                testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                                testschedule.AddLiqud = lisTM[i].AddLiqud;
                                testschedule.StartTime = 0;
                                testschedule.TimeLengh = RegentTime;
                                testschedule.stepNum = stepTime++;
                                testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                                testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddSingleR;
                                testschedule.Emergency = lisTM[i].Emergency;
                                testschedule.ProMethod = lisTM[i].ProMethod;
                                //稀释相关字段赋值。 LYN add 20171114
                                testschedule.dilutionPos = "";
                                testschedule.AddSamplePos = 0;
                                testschedule.dilutionTimes = "1";
                                testschedule.getSamplePos = "";
                                testschedule.singleStep = "R3";
                                TEMPlisTestSchedule.Add(testschedule);
                                testschedule = new TestSchedule();
                            }
                            break;
                        case ExperimentStep.AddRegentD:
                            if (lisTM[i].ProMethod == 2)
                            {
                                testschedule.SampleNo = lisTM[i].SampleNo;
                                testschedule.SampleID = lisTM[i].SampleID;
                                testschedule.ItemName = lisTM[i].ItemName;
                                testschedule.TestID = lisTM[i].TestID;
                                testschedule.samplePos = lisTM[i].SamplePos;
                                testschedule.SampleContainer = lisTM[i].SampleContainer;
                                testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                                testschedule.AddLiqud = lisTM[i].AddLiqud;
                                testschedule.StartTime = 0;
                                testschedule.TimeLengh = RegentTime;
                                testschedule.stepNum = stepTime++;
                                testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                                testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.AddSingleR;
                                testschedule.Emergency = lisTM[i].Emergency;
                                testschedule.ProMethod = lisTM[i].ProMethod;
                                //稀释相关字段赋值。 LYN add 20171114
                                testschedule.dilutionPos = "";
                                testschedule.AddSamplePos = 0;
                                testschedule.dilutionTimes = "1";
                                testschedule.getSamplePos = "";
                                testschedule.singleStep = "RD";
                                TEMPlisTestSchedule.Add(testschedule);
                                testschedule = new TestSchedule();
                            }
                            break;
                        case ExperimentStep.Clean:
                            if (i < temp + numItem - 3)
                            {
                                testschedule.SampleNo = lisTM[i].SampleNo;
                                testschedule.SampleID = lisTM[i].SampleID;
                                testschedule.ItemName = lisTM[i].ItemName;
                                testschedule.TestID = lisTM[i].TestID;
                                testschedule.samplePos = lisTM[i].SamplePos;
                                testschedule.SampleContainer = lisTM[i].SampleContainer;
                                testschedule.AddLiqud = lisTM[i].AddLiqud;
                                testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                                testschedule.StartTime = 0;
                                testschedule.stepNum = stepTime++;
                                testschedule.TimeLengh = lisTM[i].timeLength + 100;
                                testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                                testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.Wash1;
                                testschedule.ProMethod = lisTM[i].ProMethod;
                                testschedule.Emergency = lisTM[i].Emergency;
                                //稀释相关字段赋值。 LYN add 20171114
                                testschedule.dilutionPos = "";
                                testschedule.AddSamplePos = 0;
                                testschedule.dilutionTimes = "1";
                                testschedule.getSamplePos = "";
                                TEMPlisTestSchedule.Add(testschedule);
                                testschedule = new TestSchedule();
                            }
                            break;
                        default:
                            continue;
                    }
                }
                #region 清洗步骤合并
                for (int i = temp + numItem - 3; i < temp + numItem; i++)
                {

                    switch (lisTM[i].StepName)
                    {
                        case ExperimentStep.Clean:
                            testTimeLength += washTime;
                            testschedule.singleStep += "W2";
                            testschedule.AddLiqud += lisTM[i].AddLiqud;
                            break;
                        case ExperimentStep.AddSubstrate:
                            testTimeLength += substrateTime;
                            testschedule.singleStep += "-Su";
                            testschedule.AddLiqud += "-" + lisTM[i].AddLiqud;
                            break;
                        case ExperimentStep.Read:
                            testTimeLength += readTime;
                            testschedule.singleStep += "-R";
                            testschedule.AddLiqud += "-" + lisTM[i].AddLiqud;
                            break;
                        default:
                            continue;
                    }
                    testschedule.SampleNo = lisTM[i].SampleNo;
                    testschedule.SampleID = lisTM[i].SampleID;
                    testschedule.ItemName = lisTM[i].ItemName;
                    testschedule.TestID = lisTM[i].TestID;
                    testschedule.samplePos = lisTM[i].SamplePos;
                    testschedule.SampleContainer = lisTM[i].SampleContainer;
                    testschedule.TimePro = ItemProportion(lisTM[i].ItemName, lisTM[i].SamplePos, lisTM);
                    testschedule.ProMethod = lisTM[i].ProMethod;
                    testschedule.StartTime = 0;
                    testschedule.TimeLengh = testTimeLength;
                    testschedule.EndTime = testschedule.StartTime + testschedule.TimeLengh;
                    testschedule.TestScheduleStep = TestSchedule.ExperimentScheduleStep.WashTray;
                    testschedule.Emergency = lisTM[i].Emergency;
                }
                //稀释相关字段赋值。 LYN add 20171114
                testschedule.dilutionPos = "";
                testschedule.AddSamplePos = 0;
                testschedule.dilutionTimes = "1";
                testschedule.getSamplePos = "";
                testschedule.stepNum = stepTime++;
                TEMPlisTestSchedule.Add(testschedule);
                testschedule = new TestSchedule();
                #endregion
                temp += numItem;
                #endregion
            }
            return TEMPlisTestSchedule.SetActionTime().SetIncubateTime(); ;
        }

        /// <summary>
        /// 计算相同项目所占比重
        /// </summary>
        /// <param name="ItemName">项目名称</param>
        /// <param name="sampos">样本位号</param>
        /// <param name="lisTM">各个样本的步骤方法</param>
        /// <returns></returns>
        double ItemProportion(string ItemName, int sampos, List<TestMethod> lisTM)
        {
            double sumTime = 0;
            //取出相同样本号的项目
            List<TestMethod> tempSameTestMethod = lisTM.FindAll(ty => ty.SamplePos == sampos);
            List<ItemNameTime> lisItemNameTime = new List<ItemNameTime>();
            ItemNameTime itemNameTime = new ItemNameTime();
            for (int j = tempSameTestMethod[0].TestID; j <= tempSameTestMethod[tempSameTestMethod.Count - 1].TestID; j++)
            {
                //是否循环到第一次温育
                bool loopFlag = false;
                //取出相同样本号的步骤
                List<TestMethod> tempIDTestMethod = tempSameTestMethod.FindAll(ty => ty.TestID == j);
                double testTime = 0;
                for (int k = 0; k < tempIDTestMethod.Count; k++)
                {
                    switch (tempIDTestMethod[k].StepName)
                    {
                        //稀释时间添加. LYN add 20171114
                        case ExperimentStep.Dilution:
                        case ExperimentStep.AddSample:
                        case ExperimentStep.AddRegent1:
                        case ExperimentStep.AddRegent2:
                        case ExperimentStep.AddRegent3:
                            testTime += tempIDTestMethod[k].timeLength;
                            break;
                        case ExperimentStep.Incubation:
                            loopFlag = true;
                            break;
                    }
                    if (loopFlag)
                    {
                        break;
                    }
                }
                itemNameTime.ItemName = tempIDTestMethod[0].ItemName;
                itemNameTime.TestTime = testTime;
                lisItemNameTime.Add(itemNameTime);
                itemNameTime = new ItemNameTime();
            }
            for (int i = 0; i < tempSameTestMethod.Count; i++)
            {
                switch (tempSameTestMethod[i].StepName)
                {
                    //稀释时间添加. LYN add 20171114
                    case ExperimentStep.Dilution:
                    case ExperimentStep.AddSample:
                    case ExperimentStep.AddRegent1:
                    case ExperimentStep.AddRegent2:
                    case ExperimentStep.AddRegent3:
                    case ExperimentStep.Incubation:
                    case ExperimentStep.AddBeads:
                        sumTime += tempSameTestMethod[i].timeLength;
                        break;
                }
            }
            foreach (ItemNameTime inTime in lisItemNameTime)
            {
                if (inTime.ItemName == ItemName)
                {
                    return inTime.TestTime / sumTime;
                }
            }//求出每个项目的实验时间占所有项目实验时间的比重
            return 0;
        }

        /// <summary>
        /// 对进度列表进行排序，并按照所排的顺序对TestID按照顺序赋值
        /// </summary>
        /// <param name="lisTS">合并步骤后的实验进度</param>
        /// <returns></returns>
        public List<TestSchedule> orderModifyTestID(List<TestSchedule> lisTS)
        {
            #region 可选调度方式
            string dispatchType = OperateIniFile.ReadIniData("DispatchType", "DispatchType", "", Application.StartupPath + "//InstrumentPara.ini");
            dispatchType = string.IsNullOrEmpty(dispatchType) ? "0" : dispatchType;

            IComparer<TestSchedule> comparer;
            switch (dispatchType)
            {
                case "0":
                    comparer = new SortSchedule();
                    break;
                case "1":
                    comparer = new SortScheduleBySammeProject();
                    break;
                default:
                    comparer = new SortScheduleBySpeed();
                    break;
            }

            lisTS.Sort(comparer);
            #endregion

            #region 反应盘开始放管位置赋值
            DataTable dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            //第一个有管的位置
            string FirstPos = "";
            //2018-08-29 zlx mod
            DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum.ToString() + "'");
            if (RunFlag == (int)RunFlagStart.IsRuning && NoStartTestId > 1) //lyq 20200328
            {
                FirstPos = GetRuningFirstPos(dtReactTrayInfo);
            }
            else
            {
                _GaDoingOne = null; //lyq 20200328
                FirstPos = GetFisrtPos(dtReactTrayInfo);
            }
            if (FirstPos == "")
                ReactStartPos = 3;
            else
                ReactStartPos = int.Parse(FirstPos.Substring(2)) - 1;
            #endregion

            #region 对稀释位置、加样位置、取样位置进行赋值. LYN add 20171114
            List<TestSchedule> tempTestSchedule = new List<TestSchedule>();
            //获取加液步骤的列表
            tempTestSchedule = lisTS.FindAll(tx => tx.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube);
            //DbHelperOleDb db = new DbHelperOleDb(0);
            //DataTable dtDilute = DbHelperOleDb.Query(@"select DiluteCount from tbProject").Tables[0];
            foreach (TestSchedule Ts in tempTestSchedule)
            {
                //稀释倍数大于1
                //DbHelperOleDb db = new DbHelperOleDb(0);
                //object DiluteCount = DbHelperOleDb.GetSingle(@"select DiluteCount from tbProject where ShortName= '" + Ts.ItemName + "'");
                //int ExtraDiluteCount = int.Parse(Ts.dilutionTimes) / int.Parse(DiluteCount.ToString());
                if (int.Parse(Ts.dilutionTimes.Trim()) > 1)
                {
                    //20180601 zlx add
                    string[] AddLiqud = Ts.AddLiqud.Split('-');
                    //稀释使用多个管
                    string[] spos = Ts.dilutionPos.Split('-');
                    Ts.dilutionPos = "";
                    for (int i = 0; i < spos.Length; i++)
                    {
                        if (Ts.dilutionPos == "")
                        {
                            Ts.dilutionPos = "1";
                        }
                        else
                        {
                            Ts.dilutionPos += "-" + ((i + 1) % 4 != 0 ? (i + 1) % 4 : 1).ToString();
                        }
                    }
                    string[] diupos = Ts.dilutionPos.Split('-');
                    Ts.getSamplePos = "R" + diupos[diupos.Length - 1];
                    ReactStartPos = ReactStartPos + 1;
                    if (ReactStartPos > ReactTrayHoleNum)
                    {
                        ReactStartPos = 4;
                    }
                    Ts.AddSamplePos = ReactStartPos;
                }
                else
                {
                    ReactStartPos = ReactStartPos + 1;
                    if (ReactStartPos > ReactTrayHoleNum)
                    {
                        ReactStartPos = 4;
                    }
                    Ts.AddSamplePos = ReactStartPos;
                    Ts.getSamplePos = "S" + Ts.samplePos.ToString();
                }
            }
            #endregion
            //处理过的列表数量
            int tempScheduleNum = 0;
            int testIDNum = 1;
            while (tempScheduleNum < lisTS.Count)
            {
                List<TestSchedule> tempLis = new List<TestSchedule>();
                for (int i = tempScheduleNum; i < lisTS.Count; i++)
                {
                    tempLis.Add(lisTS[i]);
                }
                int SameTestNum = tempLis.Count(ty => ty.samplePos == lisTS[tempScheduleNum].samplePos
                    && ty.ItemName == lisTS[tempScheduleNum].ItemName && ty.TestID == lisTS[tempScheduleNum].TestID);
                for (int i = tempScheduleNum; i < tempScheduleNum + SameTestNum; i++)
                {
                    TestSchedule testCurrentSchedule = lisTS[i];
                    testCurrentSchedule.TestID = testIDNum;
                }
                testIDNum++;
                tempScheduleNum = tempScheduleNum + SameTestNum;
            }
            return lisTS;
        }

        /// <summary>
        /// 运行中温育第一个加管位置
        /// </summary>
        /// <param name="dtReactTrayInfo"></param>
        /// <returns></returns>
        string GetRuningFirstPos(DataTable dtReactTrayInfo)
        {
            string FirstPos = "";
            int OldAddSamPos =
                lisTestSchedule.Find(ty => ty.TestID == (NoStartTestId - 1) && ((
                ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube) ||
                (ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis))).AddSamplePos;
            if (OldAddSamPos == dtReactTrayInfo.Rows.Count)
            {
                FirstPos = "";
            }
            else
            {
                FirstPos = "no" + (OldAddSamPos + 1);
            }
            return FirstPos;
        }

        /// <summary>
        /// 温育第一个加管位置
        /// </summary>
        /// <param name="dtReactTrayInfo"></param>
        /// <returns></returns>
        string GetFisrtPos(DataTable dtReactTrayInfo)
        {
            string FirstPos = "";
            DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum.ToString() + "'");
            if (Convert.ToInt32(dr[0][1]) == 1)
            {
                for (int i = dtReactTrayInfo.Rows.Count - 1; i >= 0; i--)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() != "1")
                    {
                        FirstPos = dtReactTrayInfo.Rows[i + 1][0].ToString();
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                    {
                        //后一个值一直覆盖前一个最终的赋值为最后一个位置
                        FirstPos = dtReactTrayInfo.Rows[i][0].ToString();
                        if (FirstPos == "no1" || FirstPos == "no2" || FirstPos == "no3")
                            continue;
                        if (FirstPos == "no4")
                        {
                            for (int num = dtReactTrayInfo.Rows.Count - 1; num > 0; num--)
                            {
                                if (dtReactTrayInfo.Rows[num][1].ToString() != "0")
                                {
                                    if (num == ReactTrayHoleNum - 1)
                                        FirstPos = dtReactTrayInfo.Rows[3][0].ToString();
                                    else
                                    {
                                        if (num + 1 - toUsedTube > 4)
                                            FirstPos = dtReactTrayInfo.Rows[num + 1][0].ToString();
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return FirstPos;
        }

        /// <summary>
        /// 实验运行调度
        /// </summary>
        /// <param name="lisTS">赋值testid后的实验进度</param>
        /// <returns></returns>
        public List<TestSchedule> ExperimentalScheduleAlgorithm(List<TestSchedule> lisTS, List<string> freeTime)
        {
            LogFile.Instance.Write("**********" + "计算调度" + "**********");
            lastAddLiquidTime = 0;
            int beforeTime = 0;
            for (int i = 0; i < lisTS.Count; i++)
            {
                TestSchedule testSchedule = lisTS[i];
                switch (lisTS[i].TestScheduleStep)
                {
                    case TestSchedule.ExperimentScheduleStep.AddLiquidTube:
                        List<TestSchedule> lisTestid = lisTS.FindAll(tx => tx.TestID == testSchedule.TestID);
                        lastAddLiquidTime = AllowTime(freeTime, lisTestid);
                        testSchedule.StartTime = lastAddLiquidTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        ChangFreeTime(freeTime, testSchedule.StartTime, testSchedule.TimeLengh);
                        LogFile.Instance.Write("TESTID " + testSchedule.TestID + " 开始时间  ： " + lastAddLiquidTime);
                        break;
                    case TestSchedule.ExperimentScheduleStep.AddBeads:
                        testSchedule.StartTime = beforeTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        ChangFreeTime(freeTime, testSchedule.StartTime, testSchedule.TimeLengh);
                        break;
                    case TestSchedule.ExperimentScheduleStep.AddSingleR:
                        testSchedule.StartTime = beforeTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        ChangFreeTime(freeTime, testSchedule.StartTime, testSchedule.TimeLengh);
                        break;
                    case TestSchedule.ExperimentScheduleStep.Incubation:
                        testSchedule.StartTime = beforeTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        break;
                    case TestSchedule.ExperimentScheduleStep.Wash1:
                        testSchedule.StartTime = beforeTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        //ChangFreeTime(freeTime, testSchedule.StartTime, 5);
                        //ChangFreeTime(freeTime, testSchedule.EndTime, 5);
                        break;
                    case TestSchedule.ExperimentScheduleStep.WashTray:
                        testSchedule.StartTime = beforeTime;
                        testSchedule.EndTime = testSchedule.StartTime + testSchedule.TimeLengh;
                        beforeTime = testSchedule.EndTime;
                        //ChangFreeTime(freeTime, testSchedule.StartTime, 5);
                        break;
                }
            }
            return lisTS;
        }

        /// <summary>
        /// 获取空闲时间
        /// </summary>
        /// <returns></returns>
        private List<string> GetFreeTime(List<TestSchedule> RunSchedule)
        {
            List<string> runFreeTime = new List<string>() { (sumTime + 2).ToString() + "-1000000" };
            for (int i = 0; i < RunSchedule.Count; i++)
            {
                TestSchedule ts = RunSchedule[i];
                for (int j = 0; j < runFreeTime.Count; j++)
                {
                    string[] minMaxTime = runFreeTime[j].Split('-');
                    int minTime = int.Parse(minMaxTime[0]);
                    int maxTime = int.Parse(minMaxTime[1]);
                    if (minTime == maxTime)
                    {
                        runFreeTime.Remove(runFreeTime[j]);
                        j = j - 1;
                        continue;
                    }
                    if (ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddBeads ||
                        ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube ||
                        ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddSingleR)
                    {
                        #region 计算空闲 加磁珠/加液/单独加试剂
                        if (ts.StartTime <= minTime)
                        {
                            if (ts.EndTime < minTime)
                            {
                                continue;
                            }
                            else if (ts.EndTime == minTime || (ts.EndTime < maxTime && ts.EndTime > minTime))
                            {
                                runFreeTime[j] = (ts.EndTime).ToString() + "-" + maxTime;
                                break;
                            }
                            else if (ts.EndTime >= maxTime)
                            {
                                runFreeTime.RemoveAt(j);
                                j = j - 1;
                                break;
                            }
                        }
                        else if (ts.StartTime > minTime && ts.StartTime < maxTime)
                        {
                            if (ts.EndTime > minTime && ts.EndTime < maxTime)
                            {
                                int index = 0;
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                                index = j + 1;
                                runFreeTime.Insert(index, (ts.EndTime).ToString() + "-" + maxTime);
                                break;
                            }
                            else if (ts.EndTime >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                                break;
                            }

                        }
                        else if (ts.StartTime == maxTime)
                        {
                            if (ts.EndTime >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (maxTime);
                                break;
                            }
                        }
                        else if (ts.StartTime > maxTime)
                        {
                            continue;
                        }
                        #endregion
                        //ChangFreeTime(runFreeTime, ts.StartTime, ts.TimeLengh);
                    }
                    if (ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.Wash1)
                    {
                        //#region 计算空闲 到清洗盘
                        //if (ts.StartTime <= minTime)
                        //{
                        //    if (ts.StartTime + 5 < minTime)
                        //    {
                        //        continue;
                        //    }
                        //    else if (ts.StartTime + 5 == minTime || (ts.StartTime + 5 < maxTime && ts.StartTime + 5 > minTime))
                        //    {
                        //        runFreeTime[j] = (ts.StartTime + 5).ToString() + "-" + maxTime;
                        //        break;
                        //    }
                        //    else if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime.RemoveAt(j);
                        //        j = j - 1;
                        //        break;
                        //    }
                        //}
                        //else if (ts.StartTime > minTime && ts.StartTime < maxTime)
                        //{
                        //    if (ts.StartTime + 5 > minTime && ts.StartTime + 5 < maxTime)
                        //    {
                        //        int index = 0;
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                        //        index = j + 1;
                        //        runFreeTime.Insert(index, (ts.StartTime + 5).ToString() + "-" + maxTime);
                        //        break;
                        //    }
                        //    else if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                        //        break;
                        //    }

                        //}
                        //else if (ts.StartTime == maxTime)
                        //{
                        //    if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (maxTime);
                        //        break;
                        //    }
                        //}
                        //else if (ts.StartTime > maxTime)
                        //{
                        //    continue;
                        //}
                        //#endregion

                        #region 计算空闲 到温育盘
                        if (ts.EndTime <= minTime)
                        {
                            if (ts.EndTime + 5 < minTime)
                            {
                                continue;
                            }
                            else if (ts.EndTime + 5 == minTime || (ts.EndTime + 5 < maxTime && ts.EndTime + 5 > minTime))
                            {
                                runFreeTime[j] = (ts.EndTime + 5).ToString() + "-" + maxTime;
                                break;
                            }
                            else if (ts.EndTime + 5 >= maxTime)
                            {
                                runFreeTime.RemoveAt(j);
                                j = j - 1;
                                break;
                            }
                        }
                        else if (ts.EndTime > minTime && ts.EndTime < maxTime)
                        {
                            if (ts.EndTime + 5 > minTime && ts.EndTime + 5 < maxTime)
                            {
                                int index = 0;
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.EndTime);
                                index = j + 1;
                                runFreeTime.Insert(index, (ts.EndTime + 5).ToString() + "-" + maxTime);
                                break;
                            }
                            else if (ts.EndTime + 5 >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.EndTime);
                                break;
                            }

                        }
                        else if (ts.EndTime == maxTime)
                        {
                            if (ts.EndTime + 5 >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (maxTime);
                                break;
                            }
                        }
                        else if (ts.EndTime > maxTime)
                        {
                            continue;
                        }
                        #endregion
                    }
                    if (ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.WashTray)
                    {
                        //#region 计算空闲 到清洗盘
                        //if (ts.StartTime <= minTime)
                        //{
                        //    if (ts.StartTime + 5 < minTime)
                        //    {
                        //        continue;
                        //    }
                        //    else if (ts.StartTime + 5 == minTime || (ts.StartTime + 5 < maxTime && ts.StartTime + 5 > minTime))
                        //    {
                        //        runFreeTime[j] = (ts.StartTime + 5).ToString() + "-" + maxTime;
                        //        break;
                        //    }
                        //    else if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime.RemoveAt(j);
                        //        j = j - 1;
                        //        break;
                        //    }
                        //}
                        //else if (ts.StartTime > minTime && ts.StartTime < maxTime)
                        //{
                        //    if (ts.StartTime + 5 > minTime && ts.StartTime + 5 < maxTime)
                        //    {
                        //        int index = 0;
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                        //        index = j + 1;
                        //        runFreeTime.Insert(index, (ts.StartTime + 5).ToString() + "-" + maxTime);
                        //        break;
                        //    }
                        //    else if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime);
                        //        break;
                        //    }

                        //}
                        //else if (ts.StartTime == maxTime)
                        //{
                        //    if (ts.StartTime + 5 >= maxTime)
                        //    {
                        //        runFreeTime[j] = (minTime).ToString() + "-" + (maxTime);
                        //        break;
                        //    }
                        //}
                        //else if (ts.StartTime > maxTime)
                        //{
                        //    continue;
                        //}
                        //#endregion
                        //ChangFreeTime(runFreeTime, ts.StartTime, 5);
                    }
                }
            }
            return runFreeTime;
        }
        /// <summary>
        /// 改变空闲时间
        /// </summary>
        /// <param name="freeTime"></param>
        /// <param name="startTime"></param>
        /// <param name="continueTime"></param>
        private void ChangFreeTime(List<string> freeTime, int startTime, int continueTime)
        {
            for (int i = 0; i < freeTime.Count; i++)
            {
                string[] minMaxTime = freeTime[i].Split('-');
                int minTime = int.Parse(minMaxTime[0]);
                int maxTime = int.Parse(minMaxTime[1]);
                if (minTime == maxTime)
                {
                    freeTime.Remove(freeTime[i]);
                    i = i - 1;
                    continue;
                }
                if (startTime >= minTime && startTime + continueTime <= maxTime)
                {
                    if ((startTime + continueTime) == maxTime && (startTime == minTime))
                    {
                        freeTime.Remove(freeTime[i]);
                    }
                    else
                    {
                        freeTime[i] = (startTime + continueTime).ToString() + "-" + maxTime;
                        freeTime.Insert(i, minTime.ToString() + "-" + startTime);
                    }
                    return;
                }
            }
        }
        /// <summary>
        /// 产生开始加液的位置
        /// </summary>
        /// <param name="freeTime">空闲时间</param>
        /// <param name="lisTS">当前实验步骤</param>
        /// <returns>开始加样时间</returns>
        public int AllowTime(List<string> freeTime, List<TestSchedule> lisTS)
        {
            int addLiquidStartTime = lastAddLiquidTime;
            bool isAllowFlag = false;
            int beforeTime = 0;
            int firstTime = 0;
        sequel:
            if (firstTime != 0)
            {
                addLiquidStartTime += 1;
            }
            for (int index = 0; index < lisTS.Count; index++)
            {
                firstTime++;
                switch (lisTS[index].TestScheduleStep)
                {
                    case TestSchedule.ExperimentScheduleStep.AddLiquidTube:
                        for (int i = 0; i < freeTime.Count; i++)
                        {
                            string[] minMaxTime = freeTime[i].Split('-');
                            int minTime = int.Parse(minMaxTime[0]);
                            if (minTime < addLiquidStartTime)
                            {
                                continue;
                            }
                            int maxTime = int.Parse(minMaxTime[1]);
                            if (addLiquidStartTime >= minTime && addLiquidStartTime + lisTS[index].TimeLengh <= maxTime
                                && addLiquidStartTime >= lastAddLiquidTime)
                            {
                                isAllowFlag = true;
                                break;
                            }
                            else
                            {
                                isAllowFlag = false;
                                continue;
                            }
                        }
                        if (isAllowFlag == false)
                        {
                            goto sequel;
                        }
                        //LogFile.Instance.Write("TESTID " + lisTS[index].TestID + " 上次时间  ： " + addLiquidStartTime);
                        beforeTime = addLiquidStartTime + lisTS[index].TimeLengh;
                        break;
                    case TestSchedule.ExperimentScheduleStep.AddBeads:
                        for (int i = 0; i < freeTime.Count; i++)
                        {
                            string[] minMaxTime = freeTime[i].Split('-');
                            int minTime = int.Parse(minMaxTime[0]);
                            if (minTime < addLiquidStartTime)
                            {
                                continue;
                            }
                            int maxTime = int.Parse(minMaxTime[1]);
                            if (beforeTime >= minTime && beforeTime + lisTS[index].TimeLengh <= maxTime)
                            {
                                isAllowFlag = true;
                                break;
                            }
                            else
                            {
                                isAllowFlag = false;
                                continue;
                            }
                        }
                        if (isAllowFlag == false)
                        {
                            goto sequel;
                        }
                        beforeTime = beforeTime + lisTS[index].TimeLengh;
                        break;
                    case TestSchedule.ExperimentScheduleStep.Incubation:
                        beforeTime = beforeTime + lisTS[index].TimeLengh;
                        break;
                    case TestSchedule.ExperimentScheduleStep.Wash1:
                        //for (int i = 0; i < freeTime.Count; i++)
                        //{
                        //    string[] minMaxTime = freeTime[i].Split('-');
                        //    int minTime = int.Parse(minMaxTime[0]);
                        //    int maxTime = int.Parse(minMaxTime[1]);
                        //    if (beforeTime >= minTime && beforeTime + 5 <= maxTime)
                        //    {
                        //        isAllowFlag = true;
                        //        break;
                        //    }
                        //    else
                        //    {
                        //        isAllowFlag = false;
                        //        continue;
                        //    }
                        //}
                        //if (isAllowFlag == false)
                        //{
                        //    goto sequel;
                        //}
                        beforeTime = beforeTime + lisTS[index].TimeLengh;
                        break;
                    case TestSchedule.ExperimentScheduleStep.AddSingleR:
                        for (int i = 0; i < freeTime.Count; i++)
                        {
                            string[] minMaxTime = freeTime[i].Split('-');
                            int minTime = int.Parse(minMaxTime[0]);
                            if (minTime < addLiquidStartTime)
                            {
                                continue;
                            }
                            int maxTime = int.Parse(minMaxTime[1]);
                            if (beforeTime >= minTime && beforeTime + lisTS[index].TimeLengh <= maxTime)
                            {
                                beforeTime = beforeTime + lisTS[index].TimeLengh;
                                isAllowFlag = true;
                                break;
                            }
                            else
                            {
                                isAllowFlag = false;
                                continue;
                            }
                        }
                        if (isAllowFlag == false)
                        {
                            goto sequel;
                        }
                        beforeTime = beforeTime + lisTS[index].TimeLengh;
                        break;
                    case TestSchedule.ExperimentScheduleStep.WashTray:
                        //for (int i = 0; i < freeTime.Count; i++)
                        //{
                        //    string[] minMaxTime = freeTime[i].Split('-');
                        //    int minTime = int.Parse(minMaxTime[0]);
                        //    int maxTime = int.Parse(minMaxTime[1]);
                        //    if (beforeTime >= minTime && beforeTime + 5 <= maxTime)
                        //    {
                        //        isAllowFlag = true;
                        //        break;
                        //    }
                        //    else
                        //    {
                        //        isAllowFlag = false;
                        //        continue;
                        //    }
                        //}
                        //if (isAllowFlag == false)
                        //{
                        //    goto sequel;
                        //}
                        lastAddLiquidTime = addLiquidStartTime;
                        beforeTime = beforeTime + lisTS[index].TimeLengh;
                        break;
                }
            }
            return addLiquidStartTime;
        }
        /// <summary>
        /// 将数据绑定到datagridview控件
        /// </summary>
        void BindData(List<TestSchedule> lisTS, int nostartid)
        {
            EventControlAdd += new Action<bool>(AddSchedule);
            EventControlAdd(false);
            if (lisTS.Count == 0 || lisTS == null)
            {
                return;
            }
            int tempTestBegain = 0;
            DbHelperOleDb db = new DbHelperOleDb(1);
            BLL.tbSampleInfo bllsp = new BLL.tbSampleInfo();
            DataTable dtSample = bllsp.GetList(" SendDateTime  >=#"
                + frmParent.StartRuntime.ToString("yyyy-MM-dd") + "#and SendDateTime <#"
                + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + "# and Status = 0 order by SampleNo").Tables[0];
            for (int i = nostartid; i <= lisTS[lisTS.Count - 1].TestID; i++)
            {
                //实验项目步骤数量
                int ItemStepNum = lisTS.Count(ty => ty.TestID == i);
                int testTime = 0;
                string mergeSteps = "";
                for (int j = tempTestBegain; j < ItemStepNum + tempTestBegain; j++)
                {
                    switch (lisTS[j].TestScheduleStep)
                    {
                        case TestSchedule.ExperimentScheduleStep.AddLiquidTube:
                            mergeSteps += lisTS[j].singleStep;
                            testTime += lisTS[j].TimeLengh;
                            break;
                        case TestSchedule.ExperimentScheduleStep.AddBeads:
                            mergeSteps += "-B";
                            testTime += lisTS[j].TimeLengh;
                            break;
                        case TestSchedule.ExperimentScheduleStep.AddSingleR:
                            if (lisTS[j].singleStep == "R1")
                            {
                                mergeSteps += "-R1";
                            }
                            else if (lisTS[j].singleStep == "R2")
                            {
                                mergeSteps += "-R2";
                            }
                            else if (lisTS[j].singleStep == "R3")
                            {
                                mergeSteps += "-R3";
                            }
                            else if (lisTS[j].singleStep == "RD")
                            {
                                mergeSteps += "-RD";
                            }
                            testTime += lisTS[j].TimeLengh; ;
                            break;
                        case TestSchedule.ExperimentScheduleStep.Wash1:
                            mergeSteps += "-W1";
                            testTime += lisTS[j].TimeLengh;
                            break;
                        case TestSchedule.ExperimentScheduleStep.Incubation:
                            mergeSteps += "-I";
                            testTime += lisTS[j].TimeLengh;
                            break;
                        case TestSchedule.ExperimentScheduleStep.WashTray:
                            mergeSteps += "-" + lisTS[j].singleStep;
                            testTime += lisTS[j].TimeLengh;
                            break;
                    }
                }
                TimeSpan ts = new TimeSpan(0, 0, testTime);
                testItem.SampleNo = lisTS[tempTestBegain].SampleNo;
                testItem.SampleID = lisTS[tempTestBegain].SampleID;
                testItem.ItemName = lisTS[tempTestBegain].ItemName;
                testItem.SamplePos = lisTS[tempTestBegain].samplePos;
                //频繁操作数据库浪费了大量的时间，改成一次查出来，然后从DataTable中查数据
                DataRow[] dr = dtSample.Select("Position ='" + lisTS[tempTestBegain].samplePos + "'");
                testItem.SampleType = dr[0]["SampleType"].ToString();
                testItem.RegentBatch = dr[0]["RegentBatch"].ToString();
                testItem.Schedule = mergeSteps;
                testItem.TestID = lisTS[tempTestBegain].TestID;
                testItem.TestStatus = "";
                testItem.TestTime = ts.ToString();
                testItem.SubstratePipe = "";//2018-10-17
                testItem.RegentPos = "";//2018-10-17
                BTestItem.Add(testItem);
                testItem = new TestItem();
                tempTestBegain = tempTestBegain + ItemStepNum;
            }
            this.dgvWorkListData.DataSource = BTestItem;//绑定数据源
            proBarInit(nostartid);
            EventControlAdd += new Action<bool>(AddSchedule);
            EventControlAdd(false);
        }
        /// <summary>
        /// 对该界面datagridview控件使用的自定义进度条控件赋值到list列表中
        /// </summary>
        void proBarInit(int nostartId)
        {
            for (int i = nostartId - 1; i < dgvWorkListData.Rows.Count; i++)
            {
                proBar = new Bar.ProgressBar();
                proBar.Visible = false;
                string steps = dgvWorkListData.Rows[i].Cells[6].Value.ToString();
                string[] step = steps.Split('-');
                proBar.BarNumber = step.Length;
                proBar.ValueChange();
                //EventControlAdd += new Action<bool>(AddSchedule);
                TestSchedule tempTs = new TestSchedule();
                for (int k = 0; k < step.Length; k++)
                {
                    switch (step[k])
                    {
                        //稀释步骤控件属性赋值。 LYN add 20171114
                        case "D":
                            proBar.BarColor[k] = Color.BlueViolet;
                            proBar.BarWidth[k] = 20;
                            break;
                        case "S":
                            proBar.BarColor[k] = Color.Red;
                            proBar.BarWidth[k] = 20;
                            break;
                        case "R2":
                            proBar.BarColor[k] = Color.Red;
                            proBar.BarWidth[k] = 13;
                            break;
                        case "R1":
                            proBar.BarColor[k] = Color.Red;
                            proBar.BarWidth[k] = 13;
                            break;
                        case "R3":
                            proBar.BarColor[k] = Color.Red;
                            proBar.BarWidth[k] = 13;
                            break;
                        case "I":
                            proBar.BarColor[k] = Color.Green;
                            proBar.BarWidth[k] = 80;
                            break;
                        case "B":
                            proBar.BarColor[k] = Color.Black;
                            proBar.BarWidth[k] = 15;
                            break;
                        case "Su":
                            proBar.BarColor[k] = Color.Pink;
                            proBar.BarWidth[k] = 10;
                            break;
                        case "W1":
                        case "W2":
                            proBar.BarColor[k] = Color.Blue;
                            proBar.BarWidth[k] = 30;
                            break;
                        case "R":
                            proBar.BarColor[k] = Color.Orange;
                            proBar.BarWidth[k] = 10;
                            break;
                        case "RD":
                            proBar.BarColor[k] = Color.Red;
                            proBar.BarWidth[k] = 13;
                            break;
                    }
                }
                lisProBar.Add(proBar);
                //proBar = new Bar.ProgressBar();
            }
        }
        #endregion
        /// <summary>
        /// 运行状态改变影响相关按钮
        /// </summary>
        /// <param name="Flag"></param>
        void buttonEnableRun(bool Flag)
        {
            fbtnAddE.Enabled = fbtnAddS.Enabled = fbtnDelTest.Enabled = Flag;//2018-11-29 zlx mod
            btnLoadReagent.Enabled = btnLoadSample.Enabled = fbtnReturn.Enabled = !Flag;//2018-11-29 zlx mod
        }
        #region 实验方法
        /// <summary>
        /// 实验运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TestRun(object sender, EventArgs e)
        {
            LogFile.Instance.Write("进入TestRun");
            if (NetCom3.Instance.stopsendFlag)
                NetCom3.Instance.stopsendFlag = false;
            //2018-09-25 zlx add
            frmMain.StartFlag = true;
            PlugneedleFalg = false;
            bReagentDiuLess = false;
            if (dgvWorkListData.RowCount == 0)
            {
                btnRunStatus();
                return;
            }
            int testNumber = frmWorkList.BTestItem
                    .Where(item => (!item.TestStatus.Contains(getString("keywordText.Testcomplete"))) &&
                    (!item.TestStatus.Contains(getString("keywordText.TestStatusAbondoned")))).Count();
            if (testNumber == 0)
            {
                btnRunStatus();
                return;
            }
            //if (NetCom3.Instance.FReciveCallBack >= 3)//2018-10-24
            //    NetCom3.Instance.FReciveCallBack = 0;
            SetStopWatch();//倒计时功能 y add 0809
            TrayRemoveAllTube = false;//y add 抓空标志位，确定是否触发抓空异常
            isCleanPipeLineNow = false;//add y 20180727 是否在清洗清洗盘管路标志位
            //2018-07-23
            //if (frmMain.BQLiquaid)
            //    frmMain.BQLiquaid = false;
            if (dtSpInfo.Select("SampleType='" + getString("keywordText.CalibrationSolution") + "' and Status='0'").Length > 0 ||
                   dtSpInfo.Select("SampleType like '" + getString("keywordText.Calibrator") + "%' and Status='0'").Length > 0 ||
                   //dtSpInfo.Select("SampleType like '" + getString("keywordText.Control") + "%' and Status='0'").Length > 0 ||
                   dtSpInfo.Select("SampleType like '" + getString("keywordText.Standard") + "%' and Status='0'").Length > 0)
            {
                if (dtSpInfo.Select("SampleType ='" + getString("keywordText.Serum") + "' and Status='0'").Length > 0 ||
                    dtSpInfo.Select("SampleType ='" + getString("keywordText.Urine") + "' and Status='0'").Length > 0 ||
                    dtSpInfo.Select("SampleType ='" + getString("keywordText.BodyFluid") + "' and Status='0'").Length > 0)
                {
                    MessageBox.Show(getString("keywordText.SerumSampleCannotTestWithOther") + getString("keywordText.testNoStart"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (btnRunStatus != null)
                    {
                        btnRunStatus();
                    }
                    buttonEnableRun(false);
                    return;
                }
            }
            //2018-07-18
            #region 缺液提示
            if (frmMain.LackLq[0] > 0 || frmMain.LackLq[1] > 0 || frmMain.LackLq[2] > 0 || frmMain.LackLq[3] > 0)
            {
                //bool BStopFlag = false;
                StringBuilder st = new StringBuilder();
                if (frmMain.LackLq[0] > 0)
                {
                    st.Append(getString("keywordText.liquidB"));
                }
                if (frmMain.LackLq[1] > 0)
                {
                    if (st.Length == 0)
                        st.Append(getString("keywordText.liquidWash"));
                    else
                        st.Append("\n" + getString("keywordText.liquidWash"));
                }
                if (frmMain.LackLq[2] > 0)
                {
                    if (st.Length == 0)
                        st.Append(getString("keywordText.liquidWaste"));
                    else
                        st.Append("\n" + getString("keywordText.liquidWaste"));
                }
                if (frmMain.LackLq[3] > 0)
                {
                    if (st.Length == 0)
                        st.Append(getString("keywordText.tubeWaste"));
                    else
                        st.Append("\n" + getString("keywordText.tubeWaste"));
                }
                if ((frmMain.StopFlag[0] || frmMain.StopFlag[1] || frmMain.StopFlag[2] || frmMain.StopFlag[3]))
                {
                    st.Append("\n" + getString("keywordText.testNoStart"));
                    MessageBox.Show(st.ToString(), getString("keywordText.LiquidWarn"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (btnRunStatus != null)
                    {
                        btnRunStatus();
                    }
                    buttonEnableRun(false);
                    return;
                }
                else
                {
                    st.Append("\n" + getString("keywordText.ifcontinue"));
                    DialogResult dr = MessageBox.Show(st.ToString(), getString("keywordText.LiquidWarn"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (dr != DialogResult.OK)
                    {
                        if (btnRunStatus != null)
                        {
                            btnRunStatus();
                        }
                        buttonEnableRun(false);
                        return;
                    }
                }

            }
            #endregion
            //开启仪器运行指示灯 2018-07-07
            //NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 08 00"), 5);
            //NetCom3.Instance.SingleQuery();
            RunFlag = (int)RunFlagStart.IsRuning;
            frmTestResult.BRun = true;//2018-08-15 zlx add
            btnLoadReagent.Enabled = btnLoadSample.Enabled = fbtnReturn.Enabled = fbtnReturn.Enabled = false;//2018-07-26 zlx add
            lisSavedId = new List<int>();
            MoveTubeThread = new Thread(new ParameterizedThreadStart(MoveTube));
            MoveTubeThread.CurrentCulture = Language.AppCultureInfo;
            MoveTubeThread.CurrentUICulture = Language.AppCultureInfo;
            MoveTubeThread.IsBackground = true;
            MoveTubeThread.Start();
            EntertRun = false;
            if (!MachineInit())
            {
                //释放已经开启的移管线程
                if (MoveTubeThread != null)
                {
                    MoveTubeThread.Abort();
                }
                if (btnRunStatus != null)
                {
                    btnRunStatus();
                }
                RunFlag = (int)RunFlagStart.Stoped;
                buttonEnableRun(false);
                fbtnReturn.Enabled = true;//2018-07-31 zlx add
                return;
            }
            dtScalingPMT = new DataTable();
            dtScalingPMT.Columns.Add("PMT", typeof(int));
            dtScalingPMT.Columns.Add("Conc", typeof(double));
            dtScalingPMT.Columns.Add("ItemName", typeof(string));
            dtScalingPMT.Columns.Add("ItemType", typeof(int));
            dtScalingPMT.Columns.Add("RegentBatch", typeof(string));

            dtScalCacResult = new DataTable();
            dtScalCacResult.Columns.Add("ItemName", typeof(string));
            dtScalCacResult.Columns.Add("ItemType", typeof(int));
            dtScalCacResult.Columns.Add("Result", typeof(string));
            dtScalCacResult.Columns.Add("RegentBatch", typeof(string));
            while (washTrayTube() || lisMoveTube.Count > 0)
            {
                NetCom3.Delay(10);
            }
            //frmMain.BQLiquaid = true;//2018-07-21

            #region 清洗盘反应管状态表
            DataTable dtIniWashTray = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            for (int i = 0; i < dtIniWashTray.Rows.Count; i++)
            {
                //清洗盘添加反应盘位置字段，初始赋值该字段也需赋值。 LYN add 20171114
                dtWashTrayTubeStatus.Rows.Add(dtIniWashTray.Rows[i][0], dtIniWashTray.Rows[i][1], 0, 0, 0);
            }
            #endregion
            FirstTubeWash = true;//实验开始第一次清洗反应管放入
            lisTestSchedule.Sort(new SortRun());//按照步骤开始顺序排序
            TestStatusInfo = new Action<string, int>((str, index) =>
            {
                dgvWorkListData.Rows[index - 1].Cells["TestStatus"].Value = str;
            });//实验的实时运行实验状态
            lisSavedId = new List<int>();//2018-08-21 zlx add
            buttonEnableRun(true);//2018-11-29 zlx mod
            EntertRun = true;
            frmParent.StartRuntime = DateTime.Now;
            RunThread = new Thread(new ParameterizedThreadStart(GaTestRun));// GaTestRun  TestRun
            RunThread.CurrentCulture = Language.AppCultureInfo;
            RunThread.CurrentUICulture = Language.AppCultureInfo;
            RunThread.IsBackground = true;
            RunThread.Start();

            timeReckon.Enabled = true;
            timeReckon.Interval = timeInterval;
            timeReckon.Start();

        }
        /// <summary>
        /// 设备各部件复位
        /// </summary>
        /// <returns>复位成功，返回true;否则返回false</returns>
        bool MachineInit()
        {
            //2018-08-16 zlx mod
            if (!RunConditionJudge())
            {
                return false;
            }
            NetCom3.Instance.ReceiveHandel += new Action<string>(Instance_ReceiveHandel);
            NetCom3.Instance.ReceiveHandelForQueryTemperatureAndLiquidLevel += new Action<string>(Instance_ReceiveHandel);
            //仪器初始化
            lock (dataLocker)
            {
                Array.Clear(dataRecive, 0, 15);//2018-07-05 zlx add
            }
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
            if (!NetCom3.Instance.SingleQuery())
            {
                return false;
            }
            #region 判断各个模组是否初始化成功
            if (NetCom3.Instance.ErrorMessage != null)
            {
                //2018-09-06 zlx mod
                MessageBox.Show(NetCom3.Instance.ErrorMessage, "仪器初始化");
                return false;
            }
            #endregion
            currentHoleNum = 1;
            washCountNum = 1;
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            LogFile.Instance.Write("==============复位成功，当前位置  " + washCountNum);
            #region 查询仪器中是否有使用过的反应管，若有夹出
            if (!washTrayTubeClear(null))
            {
                MessageBox.Show(getString("keywordText.EmptyWashErr"));
                return false;
            }
            //washTrayTubeClear();
            //if (RunFlag != (int)RunFlagStart.IsRuning) return false;
            if (!CleanTrayWashPipeline())
            {
                MessageBox.Show(getString("keywordText.cleanWashEr"));
                return false;
            }
            //if (RunFlag != (int)RunFlagStart.IsRuning) return false;
            //reactTrayTubeClear();
            if (!reactTrayTubeClear(null))
            {
                MessageBox.Show(getString("keywordText.Emptytray"));
                return false;
            }
            if (RunFlag != (int)RunFlagStart.IsRuning) return false;
            #endregion
            #region 检查温育盘信息
            DataTable dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            //反应盘上空反应管个数
            int sumReactTubeNum = 0;
            //反应管的位置
            string TrayPos = "";
            //2018-08-23
            sumReactTubeNum = dtReactTrayInfo.Select("Value=1").Length;
            LogFile.Instance.Write("1温育盘有管数量" + sumReactTubeNum);
            if (sumReactTubeNum == 0)
            {
                OperateIniFile.WriteIniData("Tube", "ReacTrayTub", "", iniPathSubstrateTube);
            }
            else
            {
                string ReacTrayTub = OperateIniFile.ReadIniData("Tube", "ReacTrayTub", "", iniPathSubstrateTube);
                if (ReacTrayTub != "")
                {
                    string[] ReacTrayTubSplit = ReacTrayTub.Split(';');
                    foreach (string ReacTub in ReacTrayTubSplit)
                    {
                        if (TubeStop) return false;
                        if (ReacTub != "" && (OperateIniFile.ReadIniData("ReactTrayInfo", "No" + ReacTub, "", iniPathReactTrayInfo)) == "0")
                        {
                            rackToReact(int.Parse(ReacTub));
                        }
                    }
                }
            }
            dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            sumReactTubeNum = dtReactTrayInfo.Select("Value=1").Length;
            LogFile.Instance.Write("2温育盘有管数量" + sumReactTubeNum);
            if (sumReactTubeNum < toUsedTube)
            {
              
                DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum + "'");
                if (Convert.ToInt32(dr[0][1]) == 1)
                {
                    for (int i = 3; i < dtReactTrayInfo.Rows.Count; i++)
                    {
                        if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                        {
                            TrayPos = dtReactTrayInfo.Rows[i][0].ToString();
                        }
                        else
                            break;
                    }
                }
                else
                {
                    for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                    {
                        if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                        {
                            TrayPos = dtReactTrayInfo.Rows[i][0].ToString();
                        }
                    }
                }
            }
            if (sumReactTubeNum < toUsedTube)
            {
                if (TrayPos == "")
                {
                    TrayPos = "NO3";
                }
                int LackTubeNum = toUsedTube - sumReactTubeNum;
                int OnePos = int.Parse(OperateIniFile.ReadIniData("Tube", "TubePos", "1", iniPathSubstrateTube)) - 1; //2018-09-28
                for (int i = 0; i < LackTubeNum; i++)
                {
                    if (TubeStop)
                    {
                        MessageBox.Show(getString("keywordText.MAddNewTReactStop"));
                        return false;
                    }
                    //MoveTubeStatus moveTube1 = new MoveTubeStatus();
                    //moveTube1.StepNum = 0;
                    int putPos = int.Parse(TrayPos.Substring(2)) + i + 1;
                    if (putPos > ReactTrayHoleNum)
                        putPos = putPos - ReactTrayHoleNum + 3;
                    rackToReact(putPos);
                    //moveTube1.putTubePos = "1-" + putPos.ToString();
                    //moveTube1.TestId = 0;
                    //bool b;
                    //moveTube1.TakeTubePos = "0-" + 1;
                    //lock (locker2)
                    //{
                    //    lisMoveTube.Add(moveTube1);
                    //}
                }
            }
            NetCom3.Instance.ReceiveHandelForQueryTemperatureAndLiquidLevel -= new Action<string>(Instance_ReceiveHandel);
            #endregion
            return true;
        }

        /// <summary>
        /// 实验开始前，检查并清空
        /// </summary>
        /// <returns></returns>
        private bool CleanTube()
        {
            if (!washTrayTubeClear(null))
            {
                MessageBox.Show(getString("keywordText.EmptyWashErr"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (RunFlag != (int)RunFlagStart.IsRuning)
            {
                MessageBox.Show(getString("keywordText.RunStatusErr"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!CleanTrayWashPipeline())
            {
                MessageBox.Show(getString("keywordText.cleanWashEr"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!reactTrayTubeClear(null))
            {
                MessageBox.Show(getString("keywordText.Emptytray"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (RunFlag != (int)RunFlagStart.IsRuning)
            {
                MessageBox.Show(getString("keywordText.RunStatusErr"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 实验运行之间进行是否能运行的判断
        /// </summary>
        /// <returns>可以继续运行返回true，反之返回false</returns>
        bool RunConditionJudge()
        {
            lisScalingInfo = new List<ScalingInfo>();
            BLL.tbMainScalCurve bllMainCurve = new BLL.tbMainScalCurve();
            #region 从数据库中读取样本信息

            if (dtSampleRunInfo.Rows.Count == 0 || dtSampleRunInfo == null)
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.ReLoadS"));
                return false;
            }
            #endregion

            //将所做的实验项目转化为List类型
            List<TestItem> lisItem = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource).ToList();
            //对项目名称进行分组
            var ItemNames = lisItem.GroupBy(x => x.ItemName).Where(x => x.Count() >= 1).ToList();
            //查询试剂信息
            List<ReagentIniInfo> lisRIinfo = QueryReagentIniInfo();
            //所有的标准品或定标液
            //List<TestItem> lisAllScaling;
            //相同项目的样本
            List<TestItem> lisSameItem = new List<TestItem>();
            List<string> lisRegentBatch = new List<string>();
            #region 检查稀释液和试剂是否够用
            DataTable DtRgInfoNoStat = frmSampleLoad.DtItemInfoNoStat.Copy();
            bool SCflag = false;//由于定标原因禁止开始实验标识
            string SCStopItems = null;
            if (DtRgInfoNoStat.Rows.Count == 0)
            {
                foreach (var item in ItemNames)
                {
                    List<TestSchedule> list = lisTestSchedule.FindAll(ty => (ty.ItemName == item.Key && ty.TestScheduleStep == ExperimentScheduleStep.AddLiquidTube));
                    DataRow newrow = DtRgInfoNoStat.NewRow();
                    newrow["RgName"] = item.Key;
                    newrow["TestRg"] = list.Count;
                    newrow["TestDiu"] = 0;
                    DtRgInfoNoStat.Rows.Add(newrow);
                    List<TestSchedule> listDiu = list.FindAll(ty => (int.Parse(ty.dilutionTimes) > 1));
                    #region 计算稀释液需要使用量
                    if (listDiu.Count > 0)
                    {
                        string DiuName = "";
                        List<ReagentIniInfo> lisRTtem = lisRIinfo.FindAll(ty => (ty.ItemName == item.Key));
                        foreach (ReagentIniInfo RTtem in lisRTtem)
                        {
                            string DiuPos = OperateIniFile.ReadIniData("ReagentPos" + RTtem.Postion, "DiuPos", "", iniPathReagentTrayInfo);
                            if (DiuPos != "")
                            {
                                DiuName = OperateIniFile.ReadIniData("ReagentPos" + DiuPos, "ItemName", "", iniPathReagentTrayInfo);
                                break;
                            }
                        }
                        int SumDiluteVol = 0;
                        foreach (TestSchedule diutest in listDiu)
                        {
                            int diuTimes = int.Parse(diutest.dilutionTimes);
                            int GetSampleV = int.Parse(diutest.AddLiqud.Split('-')[0]);
                            DataTable DiluteInfo = DbHelperOleDb.Query(0, @"select DiluteName , DiluteCount from tbProject where ShortName ='" + item.Key + "'").Tables[0];
                            string DiluteName = "1";
                            int DiluteCount = 1;
                            foreach (DataRow drDiu in DiluteInfo.Rows)
                            {
                                DiluteName = drDiu["DiluteName"].ToString();
                                DiluteCount = int.Parse(drDiu["DiluteCount"].ToString());
                            }
                            //DbHelperOleDb db = new DbHelperOleDb(0);
                            //string DiluteName = DbHelperOleDb.GetSingle(0, @"select DiluteName from tbProject where ShortName ='" + item.Key + "'").ToString();
                            //db = new DbHelperOleDb(0);
                            //int DiluteCount = int.Parse(DbHelperOleDb.GetSingle(0, @"select DiluteCount from tbProject where ShortName ='" + item.Key + "'").ToString());
                            if (DiluteCount > 1)
                            {
                                List<string> diuList = GetDiuVol(GetSampleV, DiluteName);
                                foreach (string duitec in diuList)
                                {
                                    SumDiluteVol = SumDiluteVol + int.Parse(duitec.Split(';')[1]) + abanDiuPro;
                                }
                                GetSampleV = int.Parse(diuList[0].Split(';')[0]);
                            }
                            int ExtraDiuCount = diuTimes / DiluteCount;
                            if (ExtraDiuCount > 1)
                            {
                                string diuInfo = DiuInfo.GetDiuInfo(ExtraDiuCount);
                                List<string> diuList = GetDiuVol(GetSampleV, diuInfo);
                                foreach (string duitec in diuList)
                                {
                                    SumDiluteVol = SumDiluteVol + int.Parse(duitec.Split(';')[1]) + abanDiuPro;
                                }
                            }
                        }
                        DataRow[] dr = DtRgInfoNoStat.Select("RgName='" + DiuName + "'");
                        if (dr.Length > 0)
                        {
                            dr[0]["TestRg"] = int.Parse(dr[0]["TestRg"].ToString()) + SumDiluteVol;
                        }
                        else
                        {
                            newrow = DtRgInfoNoStat.NewRow();
                            newrow["RgName"] = DiuName;
                            newrow["TestRg"] = SumDiluteVol;
                            newrow["TestDiu"] = 0;
                            DtRgInfoNoStat.Rows.Add(newrow);
                        }
                    }
                    #endregion
                }
            }
            foreach (DataRow dr in DtRgInfoNoStat.Rows)
            {
                if (dr["RgName"].ToString() == "") continue;
                List<ReagentIniInfo> lisRTtem = lisRIinfo.FindAll(ty => (ty.ItemName == dr["RgName"].ToString()));
                int left = 0;
                foreach (ReagentIniInfo R in lisRTtem)
                {
                    string DiuFlag = OperateIniFile.ReadIniData("ReagentPos" + R.Postion, "DiuFlag", "", iniPathReagentTrayInfo);
                    if (DiuFlag == "1")
                        left = left + R.LeftReagent1 - DiuNoUsePro;
                    else
                        left = left + R.LeftReagent1;
                }
                if (left < int.Parse(dr["TestRg"].ToString()))
                {
                    frmMsgShow.MessageShow(getString("btnWorkList.Text"), dr["RgName"].ToString() + getString("keywordText.ProjectLess"));
                    return false;
                }
            }
            frmSampleLoad.DtItemInfoNoStat.Rows.Clear();
            #endregion
            bool flag = false;
            string ExpiredItems = null;
            foreach (var item in ItemNames)
            {
                List<ReagentIniInfo> itemRiInfo = lisRIinfo.FindAll(ty => (ty.ItemName == item.Key));
                lisSameItem = lisItem.FindAll(ty => ty.ItemName == item.Key);
                #region 查询是否有已有定标，并且对标准品浓度和吸光度进行保存
                DbHelperOleDb db = new DbHelperOleDb(0);
                //DataTable dtItemInfo = DbHelperOleDb.Query(@"select ProjectType,CalPointConc,CalPointNumber from tbProject where ShortName ='" + item.Key + "'").Tables[0];
                //2018-07-31  zlx add ExpiryDate
                DataTable dtItemInfo = DbHelperOleDb.Query(0, @"select ProjectType,CalPointConc,CalPointNumber,ExpiryDate from tbProject where ShortName ='" + item.Key + "'").Tables[0];
                DataTable dtScal = new DataTable();
                dtScal.Columns.Add("SampleType", typeof(string));
                dtScal.Columns.Add("Num", typeof(int));
                dtScal.Columns.Add("Conc", typeof(string));
                //对装载试剂的试剂批号进行分组
                var regenBatchNums = lisRIinfo.FindAll(tx => tx.ItemName == item.Key)
                    .GroupBy(x => x.BatchNum).Where(x => x.Count() >= 1).ToList();
                if (regenBatchNums.Count > 0)
                {
                    foreach (var reBNum in regenBatchNums)
                    {
                        if (reBNum.Key.ToString() == "") continue;
                        //2018-08-25 zlx add
                        dtScal.Clear();

                        //查询该试剂批号的项目是否有历史定标
                        //object obPoints = DbHelperOleDb.GetSingle(@"select Points from tbScalingResult where ItemName ='"
                        //    + item.Key + "'and RegentBatch = '" + reBNum.Key + "' and status = 1");
                        //string points = obPoints == null ? "" : obPoints.ToString();
                        //2018-07-31 zlx mod
                        db = new DbHelperOleDb(1);
                        DataTable tbScalingResult = DbHelperOleDb.Query(1, @"select Points,ActiveDate from tbScalingResult where ItemName ='"
                        + item.Key + "'and RegentBatch = '" + reBNum.Key + "' and status = 1").Tables[0];
                        string points = null;
                        string ActiveDate = null;
                        foreach (DataRow dr in tbScalingResult.Rows)
                        {
                            if (dr["Points"].ToString() != "")
                                points = dr["Points"].ToString();
                            if (dr["ActiveDate"].ToString() != "")
                                ActiveDate = dr["ActiveDate"].ToString();
                        }
                        if (int.Parse(dtItemInfo.Rows[0][0].ToString()) == 1)
                        {
                            #region 定量实验
                            List<TestItem> listSccal = lisSameItem.FindAll(ty => ty.SampleType.Contains(getString("keywordText.Standard")) && ty.RegentBatch == reBNum.Key);
                            for (int i = 0; i < int.Parse(dtItemInfo.Rows[0][2].ToString()); i++)
                            {
                                switch (i)
                                {
                                    case 0:
                                        dtScal.Rows.Add(getString("keywordText.StandardA"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardA")
                                            && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 1:
                                        dtScal.Rows.Add(getString("keywordText.StandardB"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardB")
                                            && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 2:
                                        dtScal.Rows.Add(getString("keywordText.StandardC"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardC")
                                        && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 3:
                                        dtScal.Rows.Add(getString("keywordText.StandardD"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardD")
                                        && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 4:
                                        dtScal.Rows.Add(getString("keywordText.StandardE"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardE")
                                        && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 5:
                                        dtScal.Rows.Add(getString("keywordText.StandardF"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardF")
                                        && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                    case 6:
                                        dtScal.Rows.Add(getString("keywordText.StandardG"), listSccal.FindAll(ty => ty.SampleType == getString("keywordText.StandardG")
                                        && ty.RegentBatch == reBNum.Key).Count);
                                        break;
                                }
                            }
                            int sNum = 0;
                            for (int i = 0; i < dtScal.Rows.Count; i++)
                            {
                                if (int.Parse(dtScal.Rows[i][1].ToString()) == 0)
                                {
                                    sNum++;
                                }
                            }
                            ////lyq add 20190831
                            //if (lisItem.FindAll(ty => ty.SampleType.Contains("交叉污染")).Count > 0)
                            //    sNum = 0;

                            if (sNum > 0)//六个标准品中存在1个个数为0
                            {
                                if (int.Parse(dtScal.Rows[2][1].ToString()) > 0 && int.Parse(dtScal.Rows[4][1].ToString()) > 0)//C.E两点数量不为0
                                {
                                    db = new DbHelperOleDb(1);
                                    bool ExitsMainCurve = bllMainCurve.ExistsCurve(item.Key, reBNum.Key);
                                    if (ExitsMainCurve) //有主曲线
                                    {

                                        //if (lisSameItem.FindAll(ty => (ty.SampleType.Contains(getString("keywordText.Standard")))).Count == 0)
                                        if (listSccal.Count == 0)
                                        {
                                            #region 判断定标曲线是否可以使用
                                            string[] scpoint = points.Split(';');
                                            DataTable tempdt = new DataTable();
                                            tempdt.Columns.Add("Conc", typeof(float));
                                            tempdt.Columns.Add("PMT", typeof(float));
                                            DataTable DataMain = new DataTable();
                                            DataMain.Columns.Add("Conc", typeof(float));
                                            DataMain.Columns.Add("PMT", typeof(float));
                                            for (int i = 0; i < scpoint.Length; i++)
                                            {
                                                if (scpoint[i] != "")
                                                {
                                                    string[] pointinfo = scpoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                    tempdt.Rows.Add(double.Parse(pointinfo[0]), double.Parse(pointinfo[1]));
                                                }
                                            }
                                            if (tempdt.Rows.Count == 2)
                                            {
                                                DataTable _DataMain = bllMainCurve.GetList("ItemName='" + item.Key + "' AND RegentBatch='" + reBNum.Key + "'").Tables[0];
                                                if (_DataMain.Rows.Count > 0)
                                                {
                                                    string[] mainPoint = _DataMain.Rows[0]["Points"].ToString().Split(';');
                                                    for (int i = 0; i < mainPoint.Length; i++)
                                                    {
                                                        string[] pointinfo = mainPoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                        if (pointinfo.Length == 2)
                                                            DataMain.Rows.Add(float.Parse(pointinfo[0]), float.Parse(pointinfo[1]));
                                                    }
                                                }
                                            }
                                            Calculater er = GetCalculater(DataMain, tempdt, item.Key);
                                            if (er.R2 < 0.99)
                                            {
                                                if (!SCflag)
                                                {
                                                    SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                        + getString("keywordText.Recalibrate") + "\n";
                                                    SCflag = true;
                                                }
                                                else
                                                    SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                        + getString("keywordText.Recalibrate") + "\n";
                                                //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Reagentbatch") + reBNum.Key + getString("keywordText.ProjectName") + item.Key + getString("keywordText.Recalibrate"));
                                                //return false;
                                            }
                                            #endregion
                                        }
                                        scalingInfo.ItemName = item.Key;
                                        scalingInfo.RegenBatch = reBNum.Key;
                                        scalingInfo.Num = dtScal.Rows[2][1].ToString() + "," + dtScal.Rows[4][1].ToString();
                                        scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString().Split(',')[2] + ","
                                            + dtItemInfo.Rows[0][1].ToString().Split(',')[4];
                                        scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                        lisScalingInfo.Add(scalingInfo);
                                        scalingInfo = new ScalingInfo();
                                    }
                                    else//无主曲线
                                    {
                                        //判断是否有历史定标
                                        if (points == null || points == "")
                                        {
                                            if (!SCflag)
                                            {
                                                SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                    + getString("keywordText.noMainCurve") + "\n";
                                                SCflag = true;
                                            }
                                            else
                                                SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                    + getString("keywordText.noMainCurve") + "\n";
                                            //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.noMainCurve"));
                                            //return false;
                                        }
                                        else if (DateTime.Now.Date.AddDays(-Convert.ToInt32(dtItemInfo.Rows[0][3])).Date > Convert.ToDateTime(ActiveDate))
                                        {
                                            //2018-07-31 zlx add
                                            scalingInfo.ItemName = item.Key;
                                            scalingInfo.RegenBatch = reBNum.Key;
                                            scalingInfo.Num = "0";
                                            scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                            scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                            lisScalingInfo.Add(scalingInfo);
                                            scalingInfo = new ScalingInfo();
                                            if (!flag)
                                            {
                                                ExpiredItems += getString("keywordText.ProjectName") + item.Key + "  " + getString("keywordText.Reagentbatch") + reBNum.Key + "\n";
                                                flag = true;
                                            }
                                            else
                                                ExpiredItems += getString("keywordText.ProjectName") + item.Key + "  " + getString("keywordText.Reagentbatch") + reBNum.Key + "\n";
                                        }
                                        else
                                        {
                                            //if (lisSameItem.FindAll(ty => (ty.SampleType.Contains(getString("keywordText.Standard")))).Count == 0)
                                            if (listSccal.Count == 0)
                                            {
                                                #region 判断定标曲线是否可以使用
                                                string[] scpoint = points.Split(';');
                                                DataTable tempdt = new DataTable();
                                                tempdt.Columns.Add("Conc", typeof(float));
                                                tempdt.Columns.Add("PMT", typeof(float));
                                                DataTable DataMain = new DataTable();
                                                DataMain.Columns.Add("Conc", typeof(float));
                                                DataMain.Columns.Add("PMT", typeof(float));
                                                for (int i = 0; i < scpoint.Length; i++)
                                                {
                                                    if (scpoint[i] != "")
                                                    {
                                                        string[] pointinfo = scpoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                        tempdt.Rows.Add(double.Parse(pointinfo[0]), double.Parse(pointinfo[1]));
                                                    }
                                                }
                                                if (tempdt.Rows.Count == 2)
                                                {
                                                    db = new DbHelperOleDb(1);
                                                    DataTable _DataMain = bllMainCurve.GetList("ItemName='" + item.Key + "' AND RegentBatch='" + reBNum.Key + "'").Tables[0];
                                                    if (_DataMain.Rows.Count > 0)
                                                    {
                                                        string[] mainPoint = _DataMain.Rows[0]["Points"].ToString().Split(';');
                                                        for (int i = 0; i < mainPoint.Length; i++)
                                                        {
                                                            string[] pointinfo = mainPoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                            if (pointinfo.Length == 2)
                                                                DataMain.Rows.Add(float.Parse(pointinfo[0]), float.Parse(pointinfo[1]));
                                                        }
                                                    }
                                                }
                                                Calculater er = GetCalculater(DataMain, tempdt, item.Key);
                                                if (er.R2 < 0.99)
                                                {
                                                    if (!SCflag)
                                                    {
                                                        SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                            + getString("keywordText.Recalibrate") + "\n";
                                                        SCflag = true;
                                                    }
                                                    else
                                                        SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                            + getString("keywordText.Recalibrate") + "\n";
                                                    //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Reagentbatch") + reBNum.Key + getString("keywordText.ProjectName") + item.Key + getString("keywordText.Recalibrate"));
                                                    //return false;
                                                }
                                                #endregion
                                            }
                                            scalingInfo.ItemName = item.Key;
                                            scalingInfo.RegenBatch = reBNum.Key;
                                            scalingInfo.Num = "0";
                                            scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                            scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                            lisScalingInfo.Add(scalingInfo);
                                            scalingInfo = new ScalingInfo();
                                        }
                                    }
                                }
                                else
                                {
                                    //判断是否有历史定标
                                    if (points == null || points == "")
                                    {
                                        int count = lisSameItem.FindAll(ty => (ty.RegentBatch == reBNum.Key || ty.RegentBatch == "")).Count;
                                        if (count > 0)
                                        {
                                            if (!SCflag)
                                            {
                                                SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                    + getString("keywordText.NoScling") + "\n";
                                                SCflag = true;
                                            }
                                            else
                                                SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                    + getString("keywordText.NoScling") + "\n";
                                            //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Reagentbatch") + reBNum.Key + getString("keywordText.ProjectName") + item.Key + getString("keywordText.NoScling"));
                                            //return false;
                                        }
                                    }
                                    else if (DateTime.Now.Date.AddDays(-Convert.ToInt32(dtItemInfo.Rows[0][3])).Date > Convert.ToDateTime(ActiveDate))
                                    {
                                        scalingInfo.ItemName = item.Key;
                                        scalingInfo.RegenBatch = reBNum.Key;
                                        scalingInfo.Num = "0";
                                        scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                        scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                        lisScalingInfo.Add(scalingInfo);
                                        scalingInfo = new ScalingInfo();
                                        //2018-07-31 zlx add
                                        if (!flag)
                                        {
                                            ExpiredItems += getString("keywordText.ProjectName") + item.Key + "  " + getString("keywordText.Reagentbatch") + reBNum.Key + "\n";
                                            flag = true;
                                        }
                                        else
                                            ExpiredItems += getString("keywordText.ProjectName") + item.Key + "  " + getString("keywordText.Reagentbatch") + reBNum.Key + "\n";
                                    }
                                    else
                                    {
                                        if (lisSameItem.FindAll(ty => (ty.SampleType.Contains(getString("keywordText.Standard")))).Count == 0)
                                        {
                                            #region 判断定标曲线是否可以使用
                                            string[] scpoint = points.Split(';');
                                            DataTable tempdt = new DataTable();
                                            tempdt.Columns.Add("Conc", typeof(float));
                                            tempdt.Columns.Add("PMT", typeof(float));
                                            DataTable DataMain = new DataTable();
                                            DataMain.Columns.Add("Conc", typeof(float));
                                            DataMain.Columns.Add("PMT", typeof(float));
                                            for (int i = 0; i < scpoint.Length; i++)
                                            {
                                                if (scpoint[i] != "")
                                                {
                                                    string[] pointinfo = scpoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                    tempdt.Rows.Add(double.Parse(pointinfo[0]), double.Parse(pointinfo[1]));
                                                }
                                            }
                                            if (tempdt.Rows.Count == 2)
                                            {
                                                db = new DbHelperOleDb(1);
                                                DataTable _DataMain = bllMainCurve.GetList("ItemName='" + item.Key + "' AND RegentBatch='" + reBNum.Key + "'").Tables[0];
                                                if (_DataMain.Rows.Count > 0)
                                                {
                                                    string[] mainPoint = _DataMain.Rows[0]["Points"].ToString().Split(';');
                                                    for (int i = 0; i < mainPoint.Length; i++)
                                                    {
                                                        string[] pointinfo = mainPoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                        if (pointinfo.Length == 2)
                                                            DataMain.Rows.Add(float.Parse(pointinfo[0]), float.Parse(pointinfo[1]));
                                                    }
                                                }
                                            }
                                            Calculater er = GetCalculater(DataMain, tempdt, item.Key);
                                            if (er.R2 < 0.99)
                                            {
                                                if (!SCflag)
                                                {
                                                    SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                        + getString("keywordText.Recalibrate") + "\n";
                                                    SCflag = true;
                                                }
                                                else
                                                    SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                        + getString("keywordText.Recalibrate") + "\n";
                                                //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Reagentbatch") + reBNum.Key + getString("keywordText.ProjectName") + item.Key + getString("keywordText.Recalibrate"));
                                                //return false;
                                            }
                                            #endregion
                                        }
                                        scalingInfo.ItemName = item.Key;
                                        scalingInfo.RegenBatch = reBNum.Key;
                                        scalingInfo.Num = "0";
                                        scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                        scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                        lisScalingInfo.Add(scalingInfo);
                                        scalingInfo = new ScalingInfo();
                                    }
                                }
                            }
                            else//六个标准品都不为0
                            {
                                if (lisSameItem.FindAll(ty => (ty.SampleType.Contains(getString("keywordText.Standard")))).Count == 0)
                                {
                                    #region 判断定标曲线是否可以使用
                                    string[] scpoint = points.Split(';');
                                    DataTable tempdt = new DataTable();
                                    tempdt.Columns.Add("Conc", typeof(float));
                                    tempdt.Columns.Add("PMT", typeof(float));
                                    DataTable DataMain = new DataTable();
                                    DataMain.Columns.Add("Conc", typeof(float));
                                    DataMain.Columns.Add("PMT", typeof(float));
                                    for (int i = 0; i < scpoint.Length; i++)
                                    {
                                        if (scpoint[i] != "")
                                        {
                                            string[] pointinfo = scpoint[i].Replace("(", "").Replace(")", "").Split(',');
                                            tempdt.Rows.Add(double.Parse(pointinfo[0]), double.Parse(pointinfo[1]));
                                        }
                                    }
                                    if (tempdt.Rows.Count == 2)
                                    {
                                        db = new DbHelperOleDb(1);
                                        DataTable _DataMain = bllMainCurve.GetList("ItemName='" + item.Key + "' AND RegentBatch='" + reBNum.Key + "'").Tables[0];
                                        if (_DataMain.Rows.Count > 0)
                                        {
                                            string[] mainPoint = _DataMain.Rows[0]["Points"].ToString().Split(';');
                                            for (int i = 0; i < mainPoint.Length; i++)
                                            {
                                                string[] pointinfo = mainPoint[i].Replace("(", "").Replace(")", "").Split(',');
                                                if (pointinfo.Length == 2)
                                                    DataMain.Rows.Add(float.Parse(pointinfo[0]), float.Parse(pointinfo[1]));
                                            }
                                        }
                                    }
                                    Calculater er = GetCalculater(DataMain, tempdt, item.Key);
                                    if (er.R2 < 0.99)
                                    {
                                        if (!SCflag)
                                        {
                                            SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                + getString("keywordText.Recalibrate") + "\n";
                                            SCflag = true;
                                        }
                                        else
                                            SCStopItems += getString("keywordText.ProjectName") + item.Key + "," + getString("keywordText.Reagentbatch") + reBNum.Key + " "
                                                + getString("keywordText.Recalibrate") + "\n";
                                        //frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Reagentbatch") + reBNum.Key + getString("keywordText.ProjectName") + item.Key + getString("keywordText.Recalibrate"));
                                        //return false;
                                    }
                                    #endregion
                                }
                                scalingInfo.ItemName = item.Key;
                                scalingInfo.RegenBatch = reBNum.Key;
                                if (dtScal.Rows.Count == 7)
                                {
                                    scalingInfo.Num = dtScal.Rows[0][1].ToString() + "," + dtScal.Rows[1][1].ToString() + "," +
                                    dtScal.Rows[2][1].ToString() + "," + dtScal.Rows[3][1].ToString() + "," +
                                    dtScal.Rows[4][1].ToString() + "," + dtScal.Rows[5][1].ToString() + "," + dtScal.Rows[6][1].ToString();
                                }
                                else
                                {
                                    scalingInfo.Num = dtScal.Rows[0][1].ToString() + "," + dtScal.Rows[1][1].ToString() + "," +
                                        dtScal.Rows[2][1].ToString() + "," + dtScal.Rows[3][1].ToString() + "," +
                                        dtScal.Rows[4][1].ToString() + "," + dtScal.Rows[5][1].ToString();
                                }
                                scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                lisScalingInfo.Add(scalingInfo);
                                scalingInfo = new ScalingInfo();
                            }
                            #endregion
                        }
                        else
                        {
                            #region 定性实验
                            dtScal.Rows.Add(getString("keywordText.CalibrationSolution"), lisItem.FindAll(ty => ty.SampleType == getString("keywordText.CalibrationSolution") && ty.RegentBatch == reBNum.Key).Count);
                            if (int.Parse(dtScal.Rows[0][1].ToString()) == 0)//实验运行中未装载定标液
                            {
                                if (points == "")
                                {
                                    frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.NoCUTOFF"));
                                    return false;
                                }
                                else
                                {
                                    scalingInfo.ItemName = item.Key;
                                    scalingInfo.RegenBatch = reBNum.Key;
                                    scalingInfo.Num = "0";
                                    scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                    scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                    lisScalingInfo.Add(scalingInfo);
                                    scalingInfo = new ScalingInfo();
                                }
                            }
                            else
                            {
                                scalingInfo.ItemName = item.Key;
                                scalingInfo.RegenBatch = reBNum.Key;
                                scalingInfo.Num = dtScal.Rows[0][1].ToString();
                                scalingInfo.TestConc = dtItemInfo.Rows[0][1].ToString();
                                scalingInfo.testType = int.Parse(dtItemInfo.Rows[0][0].ToString());
                                lisScalingInfo.Add(scalingInfo);
                                scalingInfo = new ScalingInfo();
                            }
                            #endregion
                        }
                    }
                }
                #endregion
            }
            //if (flag)
            //{
            //    DialogResult result = MessageBox.Show(ExpiredItems + getString("keywordText.SclingOver"), getString("btnWorkList.Text"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //    if (result == DialogResult.No)
            //        return false;
            //}
            #region 检测底物测数是否够本次实验使用
            string BarCode = OperateIniFile.ReadIniData("Substrate1", "BarCode", "", iniPathSubstrateTube);
            if (BarCode == "")
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.Nosubstrate"));
                return false;
            }
            string ValidDate1 = OperateIniFile.ReadIniData("Substrate1", "ValidDate", "", iniPathSubstrateTube);//2018-10-17 zlx add
            //string ValidDate2 = OperateIniFile.ReadIniData("Substrate2", "ValidDate", "", iniPathSubstrateTube);//2018-10-17 zlx add
            substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
            substrateNum2 = 0;// int.Parse(OperateIniFile.ReadIniData("Substrate2", "LeftCount", "0", iniPathSubstrateTube));
            if (Convert.ToDateTime(ValidDate1) < DateTime.Now.Date)
            {
                string OverInfo = "";
                if (Convert.ToDateTime(ValidDate1) < DateTime.Now.Date)
                    OverInfo = getString("keywordText.substrate");
                LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "警告" + " *** " + "未读" + " *** " + "底物1已经过期");
                DialogResult r = MessageBox.Show("" + OverInfo + getString("keywordText.OvertimeInfo"), getString("keywordText.tip"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (DialogResult.OK != r)
                    return false;
            }

            if (substrateNum1 + substrateNum2 < BTestItem.Count + WashTrayCleanTimes)//6次是实验前和实验后维护需要
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.SubstrateLess"));
                return false;
            }
            #endregion
            List<TestItem> QCList = lisItem.FindAll(x => x.SampleType.Contains(getString("keywordText.Control")));
            List<TestItem> QCListcopy = new List<TestItem>();
            foreach (TestItem item in QCList)
            {
                string QCLevel;
                string QcBatch = item.RegentBatch;
                var itemc = QCListcopy.FindAll(x => (x.SampleType == item.SampleType && x.RegentBatch == item.RegentBatch));
                if (itemc.Count > 0)
                    continue;
                else
                    QCListcopy.Add(item);
                if (item.SampleType == getString("keywordText.ControlHigh"))
                {
                    QCLevel = "0";
                    QcBatch = QcBatch + "-H";
                }
                else if (item.SampleType == getString("keywordText.ControlMiddle")) 
                {
                    QCLevel = "1";
                    QcBatch = QcBatch + "-M";
                }
                else
                {
                    QCLevel = "2";
                    QcBatch = QcBatch + "-L";
                }

                DbHelperOleDb db = new DbHelperOleDb(3);
                DataTable dtQCInfo = DbHelperOleDb.Query(3, @"select QCID,Batch,QCLevel from tbQC where status = '1' and ProjectName = '"
                                                            + item.ItemName + "'and QCLevel = '" + QCLevel + "' and Batch='" + QcBatch + "' and Status = '1'").Tables[0];
                if (dtQCInfo == null || dtQCInfo.Rows.Count == 0)
                {
                    if (!SCflag)
                    {
                        SCStopItems += string.Format(getString("keywordText.NocontrolInfo"), item.ItemName, QcBatch, item.SampleType) + "\n";
                        SCflag = true;
                    }
                    else
                        SCStopItems += string.Format(getString("keywordText.NocontrolInfo"), item.ItemName, QcBatch, item.SampleType) + "\n";
                    ////frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.ProjectName") + item.ItemName + "," + getString("keywordText.controltype") + item.SampleType + getString("keywordText.controlInfo"));
                    //frmMsgShow.MessageShow(getString("btnWorkList.Text"), string.Format(getString("keywordText.NocontrolInfo"), item.ItemName, QcBatch, item.SampleType));
                    //return false;
                }
            }
            if (SCflag)
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), SCStopItems);
                return false;
            }
            if (flag)
            {
                DialogResult result = MessageBox.Show(ExpiredItems + getString("keywordText.SclingOver"), getString("btnWorkList.Text"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取定标公式信息
        /// </summary>
        private Calculater GetCalculater(DataTable dtMain, DataTable dtCaculate, string projectName)
        {
            //新建定标方程类的实例
            CalculateFactory ft = new CalculateFactory();
            List<Data_Value> CurveData = new List<Data_Value>();
            //取到定标计算公式
            DbHelperOleDb db = new DbHelperOleDb(0);
            //数据库项目表中查询定标方程选择字段
            int calMode = int.Parse(DbHelperOleDb.GetSingle(0, @"select CalMode from tbProject where ShortName = '"
                                                            + projectName + "'").ToString());
            Calculater er = ft.getCaler(calMode);
            if (dtCaculate.Rows.Count == 2)
            {
                //两点定标数据存储
                DataTable dt = GetCorrectedPoints(dtCaculate, dtMain);
                if (dt == null) return null;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    CurveData.Add(new Data_Value()
                    {
                        Data = double.Parse(dt.Rows[i]["Conc"].ToString()),
                        DataValue = double.Parse(dt.Rows[i]["PMT"].ToString())
                    });
                }
            }
            else
            {
                for (int j = 0; j < dtCaculate.Rows.Count; j++)
                {
                    CurveData.Add(new Data_Value()
                    {
                        Data = double.Parse(dtCaculate.Rows[j]["Conc"].ToString()),
                        DataValue = double.Parse(dtCaculate.Rows[j]["PMT"].ToString())
                    });
                }
            }
            if (CurveData.Count > 0)//ltData标准品
            {
                for (int i = 0; i < CurveData.Count; i++)
                {
                    if (CurveData[i].Data == 0)
                    {
                        CurveData[i].Data = 0.0001;
                    }
                    if (CurveData[i].Data == 1)
                    {
                        CurveData[i].Data = 0.999999;
                    }
                    if (CurveData[i].DataValue == 0)
                    {
                        CurveData[i].DataValue = 0.0001;
                    }
                }
                for (int i = 0; i < CurveData.Count; i++)
                {
                    //对处理过的数据进行纠错
                    if (double.IsNaN(CurveData[i].DataValue) || double.IsNaN(CurveData[i].Data))
                    {
                        MessageBox.Show(getString("keywordText.CalculationInfo"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
                //计算定标曲线的系数
                //for (int i = 0; i < CurveData.Count; i++)
                //{
                er.AddData(CurveData);
                er.Fit();
                //}
            }
            foreach (double par in er._pars)
            {
                if (double.IsNaN(par) || double.IsInfinity(par))
                {
                    //dtScalCacResult.Rows.Add(dtCaculate.Rows[0]["ItemName"].ToString(), 1, "");
                    MessageBox.Show(getString("keywordText.ProjectName") + dtCaculate.Rows[0]["ItemName"].ToString() + getString("keywordText.CalculationInfo"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }

            return er;
        }

        /// <summary>
        /// 清洗盘清管
        /// </summary>
        bool washTrayTubeClear(object obj)
        {
            LogFile.Instance.Write("======>" + "清洗盘开始清管");
            DataTable dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            //TrayRemoveAllTube = true;//y add 抓空标志位，保证不触发抓空异常
            for (int i = 0; i < WashTrayNum; i++)
            {
                if (RunFlag != (int)RunFlagStart.IsRuning || NetCom3.Instance.stopsendFlag)
                {
                    return false;
                }
                if (i != 0)
                {
                //顺时针旋转1个孔位
                AgainSend:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                    //NetCom3.Instance.WashQuery();
                    if (!NetCom3.Instance.WashQuery())
                    {
                        if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                            goto AgainSend;
                        else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                        {
                            NetCom3.Instance.stopsendFlag = true;
                            ShowWarnInfo(getString("keywordText.WashTurnOver"), getString("keywordText.Wash"), 1);
                            //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                            //MessageBox.Show("指令接收超时，实验已终止", "清洗指令错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //addLiquiding = false;
                            //AllStop();
                            return false;
                        }
                    }
                    currentHoleNum = currentHoleNum - 1;
                    //如果孔号小于等于0
                    if (currentHoleNum <= 0)
                    {
                        currentHoleNum = currentHoleNum + WashTrayNum;
                    }
                    countWashHole(-1);
                    //LogFile.Instance.Write("==================  当前位置  " + currentHoleNum);
                    OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());

                    //顺时针应该是加的 jun add 
                    tubeHoleNum = tubeHoleNum + 1;
                    if (tubeHoleNum >= 31)
                    {
                        tubeHoleNum = tubeHoleNum - WashTrayNum;
                    }
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
                MoveTubeUseFlag = true;
                WashTrayUseFlag = true;
                int IsKnockedCool = 0;
            AgainNewMove:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                LogFile.Instance.Write("==================  位置  " + washCountNum + "  扔管");
                //NetCom3.Instance.MoveQuery();
                if (!NetCom3.Instance.MoveQuery())
                {
                    #region 异常处理
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                        goto AgainNewMove;
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        IsKnockedCool++;
                        if (IsKnockedCool < 2)
                        {
                            goto AgainNewMove;
                        }
                        else
                        {
                            setmainformbutten();
                            LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时取管撞管,撞管位置为：" + tubeHoleNum);
                            //LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "洗盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                            return false;
                        }
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                    {
                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MWashLossOver"), getString("keywordText.Move"), 1);
                        AllStop();
                        //setmainformbutten();
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时接收数据超时！");
                        return false;
                    }
                    #endregion
                }
                LogFile.Instance.Write("==================  位置  " + washCountNum + "  扔管");
                OperateIniFile.WriteIniData("TubePosition", "No1", "0", iniPathWashTrayInfo);
                MoveTubeUseFlag = false;
                WashTrayUseFlag = false;
                #endregion
            }
            //TrayRemoveAllTube = false;//y add 抓空标志位
            LogFile.Instance.Write("======>" + "清洗盘清管结束");
            isHavedCount = true;
            return true;
        }
        void washTrayTubeClear()
        {
            LogFile.Instance.Write("======>" + "清洗盘开始清管");
            DataTable dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            //TrayRemoveAllTube = true;//y add 抓空标志位，保证不触发抓空异常
            for (int i = 0; i < WashTrayNum; i++)
            {
                if (RunFlag != (int)RunFlagStart.IsRuning || NetCom3.Instance.stopsendFlag)
                {
                    return;
                }
                if (i != 0)
                {
                //顺时针旋转1个孔位
                AgainSend:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 " + (-1).ToString("X2").Substring(6, 2)), 2);
                    //NetCom3.Instance.WashQuery();
                    if (!NetCom3.Instance.WashQuery())
                    {
                        if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                            goto AgainSend;
                        else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                        {
                            NetCom3.Instance.stopsendFlag = true;
                            ShowWarnInfo(getString("keywordText.WashTurnOver"), getString("keywordText.Wash"), 1);
                            //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                            //MessageBox.Show("指令接收超时，实验已终止", "清洗指令错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //addLiquiding = false;
                            AllStop();
                        }
                    }
                    currentHoleNum = currentHoleNum - 1;
                    //如果孔号小于等于0
                    if (currentHoleNum <= 0)
                    {
                        currentHoleNum = currentHoleNum + WashTrayNum;
                    }
                    countWashHole(-1);
                    //LogFile.Instance.Write("==================  当前位置  " + currentHoleNum);
                    OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());

                    //顺时针应该是加的 jun add 
                    tubeHoleNum = tubeHoleNum + 1;
                    if (tubeHoleNum >= WashTrayNum + 1)
                    {
                        tubeHoleNum = tubeHoleNum - WashTrayNum;
                    }
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
                MoveTubeUseFlag = true;
                WashTrayUseFlag = true;
                int IsKnockedCool = 0;
            AgainNewMove:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                LogFile.Instance.Write("==================  位置  " + washCountNum + "  扔管");
                //NetCom3.Instance.MoveQuery();
                if (!NetCom3.Instance.MoveQuery())
                {
                    #region 异常处理
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                        goto AgainNewMove;
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        IsKnockedCool++;
                        if (IsKnockedCool < 2)
                        {
                            goto AgainNewMove;
                        }
                        else
                        {
                            NetCom3.Instance.stopsendFlag = true;
                            ShowWarnInfo(getString("keywordText.MWashLossIsKnocked") + "," + getString("keywordText.Pos") + tubeHoleNum, getString("keywordText.Move"), 1);
                            AllStop();
                            //setmainformbutten();
                            //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时取管撞管,撞管位置为：" + tubeHoleNum);
                            //LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "洗盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                            return;
                        }
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                    {

                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MWashLossOver"), getString("keywordText.Move"), 1);
                        AllStop();
                        //setmainformbutten();
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时接收数据超时！");
                        return;
                    }
                    #endregion
                }
                LogFile.Instance.Write("==================  位置  " + washCountNum + "  扔管");
                OperateIniFile.WriteIniData("TubePosition", "No1", "0", iniPathWashTrayInfo);
                MoveTubeUseFlag = false;
                WashTrayUseFlag = false;
                #endregion
            }
            //TrayRemoveAllTube = false;//y add 抓空标志位
            LogFile.Instance.Write("======>" + "清洗盘清管结束");
            isHavedCount = true;
        }
        public static bool isCleanPipeLineNow = false;//是否正在进行清洗管路
        /// <summary>
        /// 实验清洗注液管路
        /// </summary>
        /// <returns></returns>
        private bool CleanTrayWashPipeline()
        {
            isCleanPipeLineNow = true;
            if (!AddTubeInCleanTray())
                return false;
            if (!CleanTrayMovePace(5))
                return false;
            if (!AddTubeInCleanTray())
                return false;
            if (!CleanTrayMovePace(4))
                return false;
            if (RunFlag == (int)RunFlagStart.IsRuning ||
                RunFlag == (int)RunFlagStart.IsStoping)
            {
                if (!AddTubeInCleanTray())
                    return false;
                if (!CleanTrayMovePace(4))
                    return false;
                if (!AddTubeInCleanTray())
                    return false;
                if (!CleanTrayMovePace(5 + isNewCleanTray))
                    return false;
                for (int i = 0; i < WashTrayCleanTimes; i++)
                {
                    if (RunFlag != (int)RunFlagStart.IsRuning &&
                        RunFlag != (int)RunFlagStart.IsStoping || NetCom3.Instance.stopsendFlag) return false;
                    if (!CleanTrayWash(1))
                        return false;
                    if (!CleanTrayMovePace(-1))
                        return false;
                    if (!CleanTrayWash(2))
                        return false;
                    if (!CleanTrayMovePace(-1))
                        return false;
                    if (!CleanTrayWash(2))
                        return false;
                    if (!CleanTrayMovePace(2))
                        return false;
                }
                if (!CleanTrayMovePace(-5 - isNewCleanTray))
                    return false;
                if (!RemoveTubeOutCleanTray())
                    return false;
                if (!CleanTrayMovePace(-4))
                    return false;
                if (!RemoveTubeOutCleanTray())
                    return false;
            }
            if (!CleanTrayMovePace(-4))
                return false;
            if (!RemoveTubeOutCleanTray())
                return false;
            if (!CleanTrayMovePace(-5))
                return false;
            if (!RemoveTubeOutCleanTray())
                return false;
            isCleanPipeLineNow = false;
            return true;
        }

        bool CleanTrayWash(int oneOrTwo)//清洗盘清洗1:全部注液，2：全部吸液
        {
            string order;
            string substratePipe = "";
            if (oneOrTwo == 1)
            {
                //if (substrateNum1 != 0)
                //{
                //    substratePipe = "1";
                //}
                //else
                //{
                //    substratePipe = "2";
                //}
                //2018-10-17 zlx mod
                string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
                /*
                string LeftCount2 = OperateIniFile.ReadIniData("Substrate2", "LeftCount", "", iniPathSubstrateTube);
                if (int.Parse(LeftCount1) > 0)//substrateNum1
                {
                    if (int.Parse(LeftCount1) > int.Parse(LeftCount2) && int.Parse(LeftCount2)>0)
                        substratePipe = "2";
                    else
                        substratePipe = "1";
                }
                else
                {
                    substratePipe = "2";
                }
                 */
                substratePipe = "1";
                order = "EB 90 31 03 03 00 11 11 " + substratePipe + "0";//全部注液
            }
            else if (oneOrTwo == 2)
            {
                order = "EB 90 31 03 03 01 00 00 10";//全部吸液
            }
            else
            {
                throw new Exception();
            }
        AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover(order), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                    goto AgainNewMove;
                else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                {
                    //NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.WashPourOver"), getString("keywordText.Wash"), 1);
                    //AllStop();
                    //NetCom3.Instance.stopsendFlag = true;
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "清洗盘在清洗时接收数据超时！");
                    //MessageBox.Show("指令接收超时，实验已终止", "清洗指令错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ////addLiquiding = false;
                    //AllStop();
                    return false;
                }
                else
                    return false;
            }
            else
            {
                if (oneOrTwo == 1)
                {
                    #region 底物减少逻辑
                    substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                    substrateNum1 = (substrateNum1 - 1) > 0 ? (substrateNum1 - 1) : 0;
                    OperateIniFile.WriteIniData("Substrate1", "LeftCount", substrateNum1.ToString(), iniPathSubstrateTube);
                    string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                    string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                    DbHelperOleDb dbase = new DbHelperOleDb(3);
                    DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");
                    substrateNum1 = int.Parse(OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube));
                    #endregion
                }
                /*
                if (oneOrTwo == 1)
                {
                    if (substratePipe == "1")
                    {
                        OperateIniFile.WriteIniData("Substrate1", "LeftCount", (substrateNum1 - 1).ToString(), iniPathSubstrateTube);
                        substrateNum1--;
                    }
                    else
                    {
                        OperateIniFile.WriteIniData("Substrate2", "LeftCount", (substrateNum2 - 1).ToString(), iniPathSubstrateTube);
                        substrateNum2--;
                    }
                */
            }
            return true;
        }
        private bool AddTubeInCleanTray(int pos = 0)//加空管到清洗盘取放管处
        {
            /*
            bool noUse;
            int boardPos;
            if (pos == 0)
            {
                boardPos = BoardNextPos(pos, false, out noUse);
            }
            else
            {
                boardPos = pos;
            }
            //MoveTubeUseFlag = true;
            //WashTrayUseFlag = true;
            //WashTurnFlag = true;
            int plate = boardPos % 88 == 0 ? boardPos / 88 - 1 : boardPos / 88;//几号板
            int column = boardPos % 11 == 0 ? boardPos / 11 - (plate * 8) : boardPos / 11 + 1 - (plate * 8);
            int hole = boardPos % 11 == 0 ? 11 : boardPos % 11;
             */
            int iNeedCool = 0;
            int IsKnockedCool = 0;
        AgainNewMove:
            //string order = "EB 90 31 01 06 " + plate.ToString("x2") + " " + column.ToString("x2") + " " + hole.ToString("x2");
            //NetCom3.Instance.Send(NetCom3.Cover(order), 1);
            string order = "EB 90 31 01 06";
            NetCom3.Instance.Send(NetCom3.Cover(order), 1);
            LogFile.Instance.Write("==============  加空管到清洗盘  " + washCountNum);
            if (!NetCom3.Instance.MoveQuery())
            {
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                        goto AgainNewMove;
                    else
                    {
                        ShowWarnInfo(getString("keywordText.MAddNewTWashnull"), getString("keywordText.Move"), 1);
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.LackTube)
                {
                    TubeStop = false;
                    iNeedCool++;
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 01 06"), 5);
                    NetCom3.Instance.SingleQuery();
                    Thread.Sleep(3000);
                    if (!TubeStop || iNeedCool > 2)
                    {
                        LogFile.Instance.Write(DateTime.Now.ToString("HH:mm:ss") + "由于缺管停止实验！");
                        ShowWarnInfo(getString("keywordText.LackTube"), getString("keywordText.Move"), 1);
                        return false;
                    }
                    goto AgainNewMove;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                {
                    ShowWarnInfo(getString("keywordText.TemporaryDiskStuckTube"), getString("keywordText.Move"), 1);
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
                        goto AgainNewMove;
                    else
                    {
                        ShowWarnInfo(getString("keywordText.MAddNewTWashIsKnocked"), getString("keywordText.Move"), 1);
                        //setmainformbutten();
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在暂存盘向清洗盘取放管处抓管时取管撞管！");
                        //LogFile.Instance.Write("==============  移管手在暂存盘向清洗盘取放管处抓管发生撞管  " + currentHoleNum);
                        //DialogResult tempresult = MessageBox.Show("移管手在暂存盘向清洗盘取放管处抓管发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        return false;
                    }

                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                {
                    IsKnockedCool++;
                GAgainMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                    if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                    {
                        IsKnockedCool++;
                        if (IsKnockedCool < 2)
                            goto GAgainMove;
                        else
                        {
                            ShowWarnInfo(getString("keywordText.MWashLossIsKnocked"), getString("keywordText.Move"), 1);
                            //setmainformbutten();
                            //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔管时取管撞管！");
                            //LogFile.Instance.Write("==============  移管手在清洗盘扔管时发生撞管  " + currentHoleNum);
                            //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    goto AgainNewMove;
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                    //NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.MAddNewTWashOver"), getString("keywordText.Move"), 1);
                    //AllStop();
                    //setmainformbutten();
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在暂存盘向清洗盘抓管时接收数据超时！");
                    //DialogResult tempresult = MessageBox.Show("移管手在暂存盘向清洗盘取放管处抓管接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return false;
                }
                #endregion
            }
            return true;
            //if (pos == 0)
            //{
            //    NetCom3.Instance.Send(NetCom3.Cover(order), 1);
            //    NetCom3.Instance.MoveQuery();
            //}
            //else
            //{
            //    NetCom3.Instance.Send(NetCom3.Cover(order), 4);
            //    //NetCom3.Instance.tempMoveOrder = NetCom3.Cover(order);
            //}
            //WashTurnFlag = false;
            //WashTrayUseFlag = false;
            //MoveTubeUseFlag = false;
        }


        private bool RemoveTubeOutCleanTray()//从清洗盘取放管处扔管
        {
            MoveTubeUseFlag = true;
            WashTrayUseFlag = true;
            //WashTurnFlag = true;
            int iNeedCool = 0;
            int IsKnockedCool = 0;
        AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
            LogFile.Instance.Write("==============  从清洗盘取放管处扔管  " + washCountNum);
            if (!NetCom3.Instance.MoveQuery())
            {
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                    {
                    AgainReSetSend:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                            {
                                LogFile.Instance.Write(DateTime.Now + "清洗盘复位返回指令重新发送！");
                                goto AgainReSetSend;
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.WashResetOver"), getString("keywordText.Wash"), 1);
                                AllStop();
                            }
                        }
                        goto AgainNewMove;
                    }
                    else
                    {
                        setmainformbutten();
                        ShowWarnInfo(getString("keywordText.MWashLossNullS"), getString("keywordText.Move"), 1);
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时多次抓空！抓空位置为" + tubeHoleNum);

                        ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "请洗盘抓空孔位置" + tubeHoleNum + " *** ");
                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时多次抓空，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
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
                        setmainformbutten();
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时取管撞管！撞管位置为：" + tubeHoleNum);
                        ShowWarnInfo(getString("keywordText.MWashLossIsKnocked"), getString("keywordText.Move"), 1);
                        //LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "清洗盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                        //LogFile.Instance.Write("==============  移管手在清洗盘扔废管时发生撞管  " + washCountNum + "取扔管");
                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.MWashLossOver"), getString("keywordText.Move"), 1);
                    AllStop();
                    //setmainformbutten();
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时接收数据超时！");
                    //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return false;
                }
                #endregion
            }
            //WashTurnFlag = false;
            MoveTubeUseFlag = false;
            WashTrayUseFlag = false;
            return true;
        }
        bool CleanTrayMovePace(int pace)//旋转清洗盘相应空位，正数为逆时针旋转
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
            else return true;
            AgainNewMove:
            NetCom3.Instance.Send(NetCom3.Cover(order), 2);
            if (!NetCom3.Instance.WashQuery())
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                    goto AgainNewMove;
                else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                {
                    //NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.WashTurnOver"), getString("keywordText.Wash"), 1);
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                    //MessageBox.Show("指令接收超时，实验已终止", "清洗指令错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //addLiquiding = false;
                    //AllStop();
                    return false;
                }
                else
                    return false;
            }
            countWashHole(pace);
            currentHoleNum = currentHoleNum - 1;
            //如果孔号小于等于0
            if (currentHoleNum > WashTrayNum)
            {
                currentHoleNum = currentHoleNum - WashTrayNum;
            }
            if (currentHoleNum <= 0)
            {
                currentHoleNum = currentHoleNum + WashTrayNum;
            }
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //LogFile.Instance.Write("==================  当前位置  " + currentHoleNum);
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
            return true;
        }
        /// <summary>
        /// 温育反应盘清管
        /// </summary>
        bool reactTrayTubeClear(object obj)
        {
            DataTable dtInTrayIni = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            TrayRemoveAllTube = true;//y add 抓空标志位，保证不触发抓空异常
            for (int i = 0; i < dtInTrayIni.Rows.Count; i++)
            {
                if (RunFlag != (int)RunFlagStart.IsRuning || NetCom3.Instance.stopsendFlag)
                {
                    return false;
                }
                //if (NetCom3.Instance.stopsendFlag) return false;
                if (i == 0 || i == 1 || i == 2)
                {
                    if (int.Parse(dtInTrayIni.Rows[i][1].ToString()) >= 1)
                    {
                        int IsKnockedCool = 0;
                    AgainNewMove:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                                //{
                                //    NetCom3.Instance.waitAndAgainSend.Abort();
                                //}
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
                                    //setmainformbutten();
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                    //NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MReactLossIsKnocked"), getString("keywordText.Move"), 1);
                                    return false;
                                }
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                //NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                                //AllStop();
                                //setmainformbutten();
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                                return false;
                            }
                            #endregion
                        }
                        //修改反应盘信息
                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString(), "0", iniPathReactTrayInfo);
                    }
                }
                else
                {
                    if (int.Parse(dtInTrayIni.Rows[i][1].ToString()) > 1)
                    {
                        int IsKnockedCool = 0;
                    AgainNewMove:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                                //{
                                //    NetCom3.Instance.waitAndAgainSend.Abort();
                                //}
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
                                    //NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MReactLossIsKnocked"), getString("keywordText.Move"), 1);
                                    //setmainformbutten();
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                    return false;
                                }
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                //NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                                return false;
                            }
                            #endregion
                            //return;
                        }
                        //修改反应盘信息
                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString(), "0", iniPathReactTrayInfo);
                    }
                }
            }
            TrayRemoveAllTube = false;//y add 抓空标志位
            return true;
        }
        void reactTrayTubeClear()
        {
            DataTable dtInTrayIni = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            TrayRemoveAllTube = true;//y add 抓空标志位，保证不触发抓空异常
            for (int i = 0; i < dtInTrayIni.Rows.Count; i++)
            {
                if (NetCom3.Instance.stopsendFlag) break;
                if (i == 0 || i == 1 || i == 2)
                {
                    if (int.Parse(dtInTrayIni.Rows[i][1].ToString()) >= 1)
                    {
                        int IsKnockedCool = 0;
                    AgainNewMove:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                                //{
                                //    NetCom3.Instance.waitAndAgainSend.Abort();
                                //}
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
                                    setmainformbutten();
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                    return;
                                }
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                                return;
                            }
                            #endregion
                        }
                        //修改反应盘信息
                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString(), "0", iniPathReactTrayInfo);
                    }
                }
                else
                {
                    if (int.Parse(dtInTrayIni.Rows[i][1].ToString()) > 1)
                    {
                        int IsKnockedCool = 0;
                    AgainNewMove:
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //if (NetCom3.Instance.waitAndAgainSend != null && NetCom3.Instance.waitAndAgainSend is Thread)
                                //{
                                //    NetCom3.Instance.waitAndAgainSend.Abort();
                                //}
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
                                    setmainformbutten();
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                    return;
                                }
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                                return;
                            }
                            #endregion
                            //return;
                        }
                        //修改反应盘信息
                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + int.Parse(dtInTrayIni.Rows[i][0].ToString().Substring(2)).ToString(), "0", iniPathReactTrayInfo);
                    }
                }
            }
            TrayRemoveAllTube = false;//y add 抓空标志位
        }
        /// <summary>
        /// 查询试剂盘中试剂的信息
        /// </summary>
        /// <returns></returns>
        List<ReagentIniInfo> QueryReagentIniInfo()
        {
            List<ReagentIniInfo> lisReagentIniInfo = new List<ReagentIniInfo>();
            ReagentIniInfo reagentIniInfo = new ReagentIniInfo();
            for (int i = 1; i <= RegentNum; i++)
            {
                reagentIniInfo.Postion = i.ToString();
                reagentIniInfo.BarCode = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "BarCode", "", iniPathReagentTrayInfo);
                reagentIniInfo.ItemName = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "ItemName", "", iniPathReagentTrayInfo);
                reagentIniInfo.TestCount = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "TestCount", "", iniPathReagentTrayInfo);
                reagentIniInfo.BatchNum = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "BachNum", "", iniPathReagentTrayInfo);
                string leftR1 = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "LeftReagent1", "", iniPathReagentTrayInfo);
                string leftR2 = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo);
                string leftR3 = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "LeftReagent3", "", iniPathReagentTrayInfo);
                string leftR4 = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "LeftReagent4", "", iniPathReagentTrayInfo);
                string leftDiuVol = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "leftDiuVol", "", iniPathReagentTrayInfo);
                if (leftR1 == "")
                {
                    reagentIniInfo.LeftReagent1 = 0;
                }
                else
                {
                    reagentIniInfo.LeftReagent1 = int.Parse(leftR1);
                }
                if (leftR2 == "")
                {
                    reagentIniInfo.LeftReagent2 = 0;
                }
                else
                {
                    reagentIniInfo.LeftReagent2 = int.Parse(leftR2);
                }
                if (leftR3 == "")
                {
                    reagentIniInfo.LeftReagent3 = 0;
                }
                else
                {
                    reagentIniInfo.LeftReagent3 = int.Parse(leftR3);
                }
                if (leftR4 == "")
                {
                    reagentIniInfo.LeftReagent4 = 0;
                }
                else
                {
                    reagentIniInfo.LeftReagent4 = int.Parse(leftR4);
                }
                if (leftDiuVol == "")
                {
                    reagentIniInfo.leftDiuVol = 0;
                }
                else
                {
                    reagentIniInfo.leftDiuVol = int.Parse(leftDiuVol);
                }
                reagentIniInfo.LoadDate = OperateIniFile.ReadIniData("ReagentPos" + i.ToString(), "LoadDate", "", iniPathReagentTrayInfo);
                lisReagentIniInfo.Add(reagentIniInfo);
                reagentIniInfo = new ReagentIniInfo();
            }

            if (lisReagentIniInfo.Count > 0)
            {
                return lisReagentIniInfo;
            }
            else
            {
                return null;
            }

        }
        /// <summary>
        /// 反应盘夹到清洗盘标志位1
        /// </summary>
        bool ValueFlag3 = false;
        /// <summary>
        /// 反应盘夹到清洗盘标志位2
        /// </summary>
        bool ValueFlag4 = false;
        /// <summary>
        /// 实验运行
        /// </summary>
        /// <param name="obj"></param>
        void GaTestRun(object obj)
        {
            LogFile.Instance.Write("进入GaTestRun");
            sumTime = 0;
            completeTestNums = 0;
            try
            {
                if (lisTestSchedule.Count > 0)
                {
                    RunLightFlag = true;
                    SampleNumCurrent = dgvWorkListData.Rows.Count;
                    while (SampleNumCurrent >= StopList.Count
                        && SampleNumCurrent != 0
                        && RunFlag == (int)RunFlagStart.IsRuning)
                    {
                        Thread.Sleep(20);
                        TestStep = GaNextOne();
                    StartRun:
                        DalayFlag = false;
                    waitTime:
                        if (RunFlag != (int)RunFlagStart.IsRuning)//增加一个停止按钮的过程中防止标志位被改写！！！！
                        {
                            continue;
                        }
                        #region 供应品状态变化
                        if (!frmMain.pauseFlag)
                        {
                            if ((frmMain.StopFlag[0] || frmMain.StopFlag[1] || frmMain.StopFlag[2] || frmMain.StopFlag[3]) || BFullReactTray)
                            {
                                AllPause();
                                GetNoStartList();
                            }
                            if (TubeStop)
                            {
                                if (!frmMain.pauseFlag)
                                {
                                    AllPause();
                                    GetNoStartList();
                                }
                                OperateIniFile.WriteIniData("Tube", "TubePos", "1", Application.StartupPath + "//SubstrateTube.ini");
                            }
                            //if (SubstrateStop)
                            //{
                            //    if (!frmMain.pauseFlag)
                            //    {
                            //        AllPause();
                            //        GetNoStartList();
                            //    }
                            //}
                            if (PlugneedleFalg)
                            {
                                if (!frmMain.pauseFlag)
                                {
                                    AllPause();
                                    GetNoStartList();
                                }
                            }
                        }
                        else
                        {
                            //string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
                            if ((!frmMain.StopFlag[0] && !frmMain.StopFlag[1] && !frmMain.StopFlag[2] && !frmMain.StopFlag[3]) && StopList.Count > 0  && !BFullReactTray && !PlugneedleFalg)//&& !TubeStop && (int.Parse(LeftCount1) > SampleNumCurrent)
                            {
                                //if (SubstrateStop)
                                //    SubstrateStop = false;
                                StopList.Clear();
                                Goon();
                                frmMain.pauseFlag = false;

                            }
                        }
                        #endregion
                        #region 更换管完成，去除缺管状态
                        if (AddTubeStop.Count > 0 && !TubeStop)
                        {
                            List<int> AddTubeStopCopy = AddTubeStop.GetRange(0, AddTubeStop.Count);
                            for (int i = 0; i < AddTubeStopCopy.Count; i++)
                            {
                                //int OnePos = int.Parse(OperateIniFile.ReadIniData("Tube", "TubePos", "1", iniPathSubstrateTube));
                                string ss = OperateIniFile.ReadIniData("ReactTrayInfo", "no" + AddTubeStopCopy[i], "", iniPathReactTrayInfo);
                                if (ss == "0" && !TubeStop)
                                {
                                    rackToReact(AddTubeStopCopy[i]);
                                    if (int.Parse(OperateIniFile.ReadIniData("ReactTrayInfo", "no" + AddTubeStopCopy[i], "", iniPathReactTrayInfo)) == 1)
                                    {
                                        lock (AddTubeStop)
                                        {
                                            AddTubeStop.Remove(AddTubeStopCopy[i]);
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        if (NetCom3.Instance.stopsendFlag)
                            AllStop();
                        Thread.Sleep(30);
                        if (TestStep == null)
                        {
                            Thread.Sleep(100);
                            if (((SampleNumCurrent >= StopList.Count)
                                && SampleNumCurrent != 0)//&& !SubstrateStop
                                || (EmergencyFlag || addOrdinaryFlag))
                            {
                                BackToCurrentUI(SampleNumCurrent, ref EmergencyFlag, ref addOrdinaryFlag);

                                goto StartRun;
                            }
                            else if ((TestStep != null) && !EmergencyFlag && !addOrdinaryFlag)
                            {
                                goto StartRun;
                            }
                            else
                            {
                                break;
                            }
                        }
                        #region 实验过程
                        switch (TestStep.TestScheduleStep)
                        {
                            case TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis://this part add y 20180727
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        sumTime = TestStep.StartTime + 10;
                                        stepTime = TestStep.EndTime;
                                        continue;
                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                break;
                            case TestSchedule.ExperimentScheduleStep.AddLiquidTube:
                                while (addLiquiding)
                                    goto waitTime;
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        LogFile.Instance.Write("sumTime的值：" + sumTime + ",TestStep.StartTime的值" + TestStep.StartTime);
                                        sumTime = TestStep.StartTime;
                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                #region 判断是否暂停加样
                                //如果正在加急诊，未开始运行的不允许开始加样
                                if (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag)
                                {
                                    break;
                                }
                                #region 稀释液试剂是否够用判断
                                //是否进行稀释
                                bool IsDiu = false;
                                int DiuNum = 0;
                                if (int.Parse(TestStep.dilutionTimes) > 1)
                                {
                                    DbHelperOleDb db = new DbHelperOleDb(0);
                                    string DiluteCount = DbHelperOleDb.GetSingle(0, @"select DiluteCount from tbProject where ShortName ='" + TestStep.ItemName + "'").ToString();
                                    db = new DbHelperOleDb(0);
                                    string DiluteName = DbHelperOleDb.GetSingle(0, @"select DiluteName from tbProject where ShortName ='" + TestStep.ItemName + "'").ToString();
                                    int GetSampleV = int.Parse(TestStep.AddLiqud.Split('-')[0]);
                                    if (DiluteName != "1")
                                    {
                                        List<string> diuList = GetDiuVol(GetSampleV, DiluteName);
                                        foreach (string duitec in diuList)
                                        {
                                            DiuNum = DiuNum + int.Parse(duitec.Split(';')[1]) + abanDiuPro;
                                        }
                                        GetSampleV = int.Parse(diuList[0].Split(';')[0]);
                                    }
                                    int ExDilute = int.Parse(TestStep.dilutionTimes) / int.Parse(DiluteCount);
                                    if (ExDilute > 1)
                                    {
                                        string diuInfo = DiuInfo.GetDiuInfo(ExDilute);
                                        List<string> diuList = GetDiuVol(int.Parse(TestStep.AddLiqud.Split('-')[0]), diuInfo);
                                        foreach (string duitec in diuList)
                                        {
                                            DiuNum = DiuNum + int.Parse(duitec.Split(';')[1]) + abanDiuPro;
                                        }
                                    }
                                }
                                DataRow[] drRg = dtRgInfo.Select("RgName='" + TestStep.ItemName.ToString() + "'");
                                int Rgcounum = 0;
                                List<int> DiuNumList = new List<int>();
                                foreach (var rowRg in drRg)
                                {
                                    string leftoverTestR1 = OperateIniFile.ReadIniData("ReagentPos" + rowRg["Postion"].ToString(), "LeftReagent1", "", iniPathReagentTrayInfo);
                                    Rgcounum = Rgcounum + Convert.ToInt32(leftoverTestR1);//Convert.ToInt32(rowRg["leftoverTestR1"].ToString());
                                    string diulest = OperateIniFile.ReadIniData("ReagentPos" + rowRg["Postion"].ToString(), "leftDiuVol", "", iniPathReagentTrayInfo);
                                    if (IsDiu && diulest != "" && int.Parse(diulest) - DiuNoUsePro > DiuNum)
                                        IsDiu = false;
                                }
                                LogFile.Instance.Write(TestStep.ItemName + "项目试剂剩余测试为" + Rgcounum);
                                if ((int.Parse(TestStep.dilutionTimes) > 1 && IsDiu) || Rgcounum < 1)
                                {
                                    if (int.Parse(TestStep.dilutionTimes) > 1 && IsDiu)
                                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "" + TestStep.ItemName + "项目稀释液已用完，将停止后续的" + TestStep.ItemName + "实验！");
                                    if (Rgcounum < 1)
                                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "" + TestStep.ItemName + "项目试剂已用完，将停止后续的" + TestStep.ItemName + "实验！");
                                    GetNoStartList(TestStep.ItemName, IsDiu, TestStep.TestID);
                                    setmainformbutten();
                                    break;
                                }
                                #endregion
                                #region 底物是否够用判断
                                /*
                                string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
                                List<TestSchedule> list = lisTestSchedule.FindAll(tx => (tx.TestID < NoStartTestId && tx.StartTime > 0));
                                int Sampleing = 0;
                                List<string> stoplist = new List<string>();
                                foreach (TestSchedule li in list)
                                {
                                    if (!stoplist.Contains(li.TestID.ToString()))
                                    {
                                        Sampleing++;
                                        stoplist.Add(li.TestID.ToString());
                                    }
                                }
                                if (int.Parse(LeftCount1) <= Sampleing - completeTestNums)
                                {
                                    SubstrateStop = true;
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "底物缺少，将暂停加样！");
                                    setmainformbutten();
                                    break;
                                }
                                */
                                #endregion
                                int movepos = TestStep.AddSamplePos + toUsedTube;
                                if (movepos > ReactTrayNum)
                                    movepos = movepos - ReactTrayNum + 3;
                                string ss = OperateIniFile.ReadIniData("ReactTrayInfo", "no" + TestStep.AddSamplePos, "", iniPathReactTrayInfo);
                                if (int.Parse(ss) > 1)
                                {
                                    BFullReactTray = true;
                                    break;
                                }
                                else if (int.Parse(ss) == 0)
                                {
                                    if (!AddTubeStop.Contains(TestStep.AddSamplePos))
                                    {
                                        AddTubeStop.Add(TestStep.AddSamplePos);
                                    }
                                    goto waitTime;
                                }
                                #endregion
                                stepTime = TestStep.EndTime;
                                NoStartTestId++;
                                AddLiquidThread = new Thread(new ParameterizedThreadStart(addLiquid));
                                AddLiquidThread.CurrentCulture = Language.AppCultureInfo;
                                AddLiquidThread.CurrentUICulture = Language.AppCultureInfo;
                                AddLiquidThread.IsBackground = true;
                                AddLiquidThread.Start(TestStep);
                                break;
                            case TestSchedule.ExperimentScheduleStep.AddSingleR:
                                while (addLiquiding)
                                    goto waitTime;
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        sumTime = TestStep.StartTime;

                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                if (NoStartTestId <= TestStep.TestID && (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag))
                                {
                                    break;
                                }
                                stepTime = TestStep.EndTime;
                                int addSampos = lisTestSchedule.Find(ty => ty.ItemName == TestStep.ItemName && ty.SampleNo == TestStep.SampleNo
                                    && ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube && ty.TestID == TestStep.TestID).AddSamplePos;
                                TestStep.AddSamplePos = addSampos;
                                AddLiquidThread = new Thread(new ParameterizedThreadStart(addLiquid));
                                AddLiquidThread.CurrentCulture = Language.AppCultureInfo;
                                AddLiquidThread.CurrentUICulture = Language.AppCultureInfo;
                                AddLiquidThread.IsBackground = true;
                                AddLiquidThread.Start(TestStep);
                                break;
                            case TestSchedule.ExperimentScheduleStep.AddBeads:
                                while (addLiquiding)
                                    goto waitTime;
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        sumTime = TestStep.StartTime;
                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                if (NoStartTestId <= TestStep.TestID && (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag))
                                {
                                    break;
                                }
                                stepTime = TestStep.EndTime;
                                addSampos = lisTestSchedule.Find(ty => ty.ItemName == TestStep.ItemName && ty.SampleNo == TestStep.SampleNo
                                     && ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube && ty.TestID == TestStep.TestID).AddSamplePos;
                                TestStep.AddSamplePos = addSampos;
                                AddLiquidThread = new Thread(new ParameterizedThreadStart(addLiquid));
                                AddLiquidThread.CurrentCulture = Language.AppCultureInfo;
                                AddLiquidThread.CurrentUICulture = Language.AppCultureInfo;
                                AddLiquidThread.IsBackground = true;
                                AddLiquidThread.Start(TestStep);
                                break;
                            case TestSchedule.ExperimentScheduleStep.Incubation:
                                if (sumTime < TestStep.StartTime)
                                {
                                    goto waitTime;
                                }
                                if (NoStartTestId <= TestStep.TestID && (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag))
                                {
                                    break;
                                }
                                if (this.IsHandleCreated)
                                {
                                    BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Incubation"), TestStep.TestID });
                                }
                                break;
                            case TestSchedule.ExperimentScheduleStep.Wash1:
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        sumTime = TestStep.StartTime;

                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                if (NoStartTestId <= TestStep.TestID && (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag))
                                {
                                    break;
                                }
                                W1EndTime.Add(TestStep.EndTime);
                                while (ValueFlag3)
                                {
                                    Thread.Sleep(30);
                                }
                                ValueFlag3 = true;
                                MoveTubeStatus moveTube4 = new MoveTubeStatus();
                                moveTube4.putTubePos = "2-1";
                                moveTube4.StepNum = TestStep.stepNum;
                                //移管列表加样位置赋值给列表中的反应盘取样位置。 LYN add 20171114
                                addSampos = lisTestSchedule.Find(ty => ty.ItemName == TestStep.ItemName && ty.SampleNo == TestStep.SampleNo
                                    && ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube && ty.TestID == TestStep.TestID).AddSamplePos;
                                moveTube4.TakeTubePos = "1-" + addSampos.ToString();
                                moveTube4.TestId = TestStep.TestID;
                                lock (locker2)
                                {
                                    lisMoveTube.Add(moveTube4);
                                }
                                ValueFlag3 = false;
                                if (FirstTubeWash)
                                {
                                    FirstTubeWash = false;
                                    washThread = new Thread(new ParameterizedThreadStart(washTray));
                                    washThread.CurrentCulture = Language.AppCultureInfo;
                                    washThread.CurrentUICulture = Language.AppCultureInfo;
                                    washThread.IsBackground = true;
                                    washThread.Start();
                                }
                                break;
                            case TestSchedule.ExperimentScheduleStep.WashTray:
                                if (sumTime != TestStep.StartTime)
                                {
                                    if (sumTime > TestStep.StartTime)
                                    {
                                        sumTime = TestStep.StartTime;

                                    }
                                    else
                                    {
                                        goto waitTime;
                                    }
                                }
                                if (NoStartTestId <= TestStep.TestID && (EmergencyFlag || addOrdinaryFlag || frmMain.pauseFlag))
                                {
                                    break;
                                }
                                while (ValueFlag4)
                                {
                                    Thread.Sleep(30);
                                }
                                ValueFlag4 = true;
                                MoveTubeStatus moveTube5 = new MoveTubeStatus();
                                moveTube5.putTubePos = "2-1";
                                moveTube5.StepNum = TestStep.stepNum;
                                //移管列表加样位置赋值给列表中的反应盘取样位置。 LYN add 20171114
                                addSampos = lisTestSchedule.Find(ty => ty.ItemName == TestStep.ItemName && ty.SampleNo == TestStep.SampleNo
                                    && ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube && ty.TestID == TestStep.TestID).AddSamplePos;
                                moveTube5.TakeTubePos = "1-" + addSampos.ToString();
                                moveTube5.TestId = TestStep.TestID;
                                lock (locker2)
                                {
                                    lisMoveTube.Add(moveTube5);
                                }
                                ValueFlag4 = false;
                                if (FirstTubeWash)
                                {
                                    FirstTubeWash = false;
                                    washThread = new Thread(new ParameterizedThreadStart(washTray));
                                    washThread.CurrentCulture = Language.AppCultureInfo;
                                    washThread.CurrentUICulture = Language.AppCultureInfo;
                                    washThread.IsBackground = true;
                                    washThread.Start();
                                }
                                break;
                        }
                        #endregion
                    }
                    #region 停止实验流程及相关的线程
                    while (RunFlag == (int)RunFlagStart.IsRuning)
                    {
                        if (((SampleNumCurrent <= 0 && BTestResult.Count != 0) || (SampleNumCurrent == StopList.Count && StopList.Count > 0)) && MoveTubeUseFlag == false && !washTrayTube() && !ReactTrayTube())//2018-10-09 zlx mod
                        {
                            RunFlag = (int)RunFlagStart.IsStoping;
                            if (SpDiskUpdate != null)
                            {
                                this.BeginInvoke(new Action(() => { SpDiskUpdate(); }));
                            }
                            //稀释管数初始化。 LYN add 20171114
                            DiuTubeNum = 1;
                            ////将清洗盘当前取放管位置的孔号保存到配置文件中
                            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                            IniUpdateAccess();//由配置文件同步试剂与底物信息到数据库
                            if (MoveTubeThread != null)
                            {
                                MoveTubeThread.Abort();
                            }
                            if (washThread != null)
                            {
                                washThread.Abort();
                            }
                            if (AddLiquidThread != null)
                            {
                                AddLiquidThread.Abort();
                            }
                            if (CaculateThread != null)
                            {
                                CaculateThread.Abort();
                            }
                            if (ReadThread != null)
                            {
                                ReadThread.Abort();
                            }
                            timeReckon.Stop();
                            timer.Enabled = false;
                            #region 实验完成进行仪器初始化 2018-07-04 zlx add
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
                            NetCom3.Instance.SingleQuery();
                            //washCountNum = 1;
                            //if (!CleanTrayWashPipeline())
                            //{
                            //    MessageBox.Show(getString("keywordText.Testcomplete") + "," + getString("keywordText.cleanWashEr"));
                            //}
                            //NetCom3.Instance.Send(NetCom3.Cover("EB 90 F1 02"), 5);
                            //NetCom3.Instance.SingleQuery();
                            //关闭仪器运行指示灯 2018-07-07
                            //NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 08 01"), 5);
                            //NetCom3.Instance.SingleQuery();
                            #endregion

                            StopWatchWithUpdateStatus();

                           
                            if (frmMain.pauseFlag)
                                frmMain.pauseFlag = false;
                            RunLightFlag = false;
                            //buttonEnableRun(false);
                           

                            if (StopList.Count > 0)
                            {
                                if (frmMain.StopFlag[0] || frmMain.StopFlag[1] || frmMain.StopFlag[2] || frmMain.StopFlag[3])
                                {
                                    string Message = "";
                                    if (frmMain.StopFlag[0] || frmMain.StopFlag[1])
                                        Message = getString("keywordText.LiquidWarn");
                                    if (frmMain.StopFlag[2])
                                    {
                                        if (Message == "")
                                            Message = getString("keywordText.liquidWaste");
                                        else
                                            Message = Message + "," + getString("keywordText.liquidWaste");
                                    }
                                    if (frmMain.StopFlag[3])
                                    {
                                        if (Message == "")
                                            Message = getString("keywordText.tubeWaste");
                                        else
                                            Message = Message + "," + getString("keywordText.tubeWaste");
                                    }
                                    //this.Invoke(new Action(() =>
                                    //{
                                    MessageBox.Show(Message + "，" + getString("keywordText.finshApartTest") + getString("keywordText.ReadWarn"), getString("keywordText.Detectionstatus"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                    //}));
                                }
                                else
                                {
                                    //this.Invoke(new Action(() =>
                                    //{
                                    MessageBox.Show(getString("keywordText.LackSupplies") + "," + getString("keywordText.finshApartTest") + getString("keywordText.ReadWarn"), getString("keywordText.Detectionstatus"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                    //}));
                                }
                            }
                            else
                            {
                                //this.Invoke(new Action(() =>
                                //{
                                MessageBox.Show(getString("keywordText.Testcomplete"), getString("keywordText.Detectionstatus"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);//2018-07-13 zlx mod
                                //}));
                            }
                            buttonEnableRun(false);
                            RunFlag = (int)RunFlagStart.Stoped;
                            fbtnReturn.Enabled = true;//完成全部实验才允许返回按钮可用 
                            break;
                        }
                        Thread.Sleep(50);
                    }
                    #endregion
                }
            }
            catch (ThreadAbortException ex)
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), ex.Message);
                IniUpdateAccess();
            }
            finally { }
        }
        /// <summary>
        /// 不在当前界返回当前界面
        /// </summary>
        /// <param name="sampleNumCurrent">剩余样本数量</param>
        /// <param name="emergencyFlag">加急诊标志</param>
        /// <param name="addOrdinaryFlag">加普通标志</param>
        private void BackToCurrentUI(int sampleNumCurrent, ref bool emergencyFlag, ref bool addOrdinaryFlag)
        {
            if (sampleNumCurrent != 0) return;

            this.BeginInvoke(new Action(() =>
            {
                if (CheckFormIsOpen("frmWorkList") && frmWorkList.RunFlag == (int)RunFlagStart.IsRuning)
                {
                    frmWorkList frmWL = (frmWorkList)Application.OpenForms["frmWorkList"];
                    frmWL.Show();
                    frmWL.BringToFront();
                }

                if (CheckFormIsOpen("frmAddSample") && frmWorkList.RunFlag == (int)RunFlagStart.IsRuning)
                {
                    frmAddSample frmAS = (frmAddSample)Application.OpenForms["frmAddSample"];
                    frmAS.Close();
                }
            }));

            emergencyFlag = false;
            addOrdinaryFlag = false;
        }

        private void StopWatchWithUpdateStatus()
        {
            Task.Factory.StartNew(() =>
            {
                //启动一个任务防止阻塞更新按钮操作
                StopStopWatch();
            });
            if (btnRunStatus != null)
            {
                this.BeginInvoke(new Action(() =>
                {
                    btnRunStatus();
                }));
            }
        }
        /// <summary>
        /// 判断清洗盘是否有管，LYN add 20171114
        /// </summary>
        /// <returns></returns>
        bool washTrayTube()
        {
            DataTable dtWashTrayIni = OperateIniFile.ReadConfig(iniPathWashTrayInfo);
            for (int i = 0; i < dtWashTrayIni.Rows.Count; i++)
            {
                if (dtWashTrayIni.Rows[i][1].ToString() == "1")
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断温育盘是否有正在进行的实验 2010-10-09 zlx add
        /// </summary>
        /// <returns></returns>
        bool ReactTrayTube()
        {
            DataTable dtReactTrayIni = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            for (int i = 0; i < dtReactTrayIni.Rows.Count; i++)
            {
                if (dtReactTrayIni.Rows[i][1].ToString() == "2")
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取未开始的实验编号
        /// </summary>
        public void GetNoStartList()
        {
            List<TestSchedule> list = lisTestSchedule.FindAll(tx => (tx.TestID >= NoStartTestId && tx.StartTime > 0));
            foreach (TestSchedule li in list)
            {
                if (!StopList.Contains(li.TestID.ToString()))
                {
                    StopList.Add(li.TestID.ToString());
                }
            }
        }
        public void GetNoStartList(string projectName, bool IsDiu, int TestID)
        {
            NetCom3.ComWait.Reset();
            List<string> stopList = new List<string>();
            List<TestSchedule> Temp = lisTestSchedule.FindAll(tx => (tx.TestID >= TestID && tx.ItemName == projectName));
            foreach (var item in Temp)
            {
                item.TestScheduleStep = TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis;
                if (!stopList.Contains(item.TestID.ToString()))
                {
                    stopList.Add(item.TestID.ToString());
                    foreach (DataGridViewRow dr in dgvWorkListData.Rows)
                    {
                        if (int.Parse(dr.Cells[1].Value.ToString()) == item.TestID)
                        {
                            if (IsDiu)
                                dr.Cells["TestStatus"].Value = getString("keywordText.NoDiu");
                            else
                                dr.Cells["TestStatus"].Value = getString("keywordText.NoRTeagent");
                            dr.DefaultCellStyle.BackColor = Color.Gray;
                        }
                    }
                }
            }
            SampleNumCurrent = SampleNumCurrent - stopList.Count;
            if (!bReagentDiuLess)
                bReagentDiuLess = true;
            LogFile.Instance.Write("调用了GetNoStartList(string projectName, bool IsDiu)方法");
        }
        /// <summary>
        /// 正在执行的实验步骤
        /// </summary>
        TestSchedule _GaDoingOne;
        /// <summary>
        /// 获取下一个实验步骤
        /// </summary>
        /// <returns></returns>
        TestSchedule GaNextOne()
        {
            int temp = 0;
            if (_GaDoingOne == null)
            {
                _GaDoingOne = lisTestSchedule[0];
                return _GaDoingOne;
            }
            foreach (TestSchedule wp in lisTestSchedule)
            {
                temp++;
                if (_GaDoingOne == wp)
                    break;
            }
            if (temp <= lisTestSchedule.Count - 1)
            {
                TestSchedule thisone = null;
                thisone = lisTestSchedule[temp];
                _GaDoingOne = thisone;
                return _GaDoingOne;
            }
            else
            {
                _GaDoingOne = null;
                return null;
            }
        }

        /// <summary>
        /// 获取稀释吸液量 2018-06-01
        /// 样本量/稀释液量
        /// </summary>
        /// <param name="Times"></param>
        /// <returns></returns>
        int[] getDiuVol(int SumVol, int Times)
        {
            int[] DiuVoInfo = new int[2];//稀释信息 样本量/稀释液量
            if (Times != 0)
            {
                DiuVoInfo[0] = int.Parse((Math.Round(Convert.ToDouble(SumVol) / Times)).ToString());
                DiuVoInfo[1] = SumVol - DiuVoInfo[0];
            }
            return DiuVoInfo;
        }
        /// <summary>
        /// 获取稀释加样信息
        /// </summary>
        /// <param name="AddLiquidVol">样本使用量</param>
        /// <param name="DiutimeInfo">稀释倍数信息</param>
        /// <returns>稀释加样信息</returns>
        List<string> GetDiuVol(int AddLiquidVol, string DiutimeInfo)
        {
            List<string> DiuVoInfo = new List<string>();
            string[] Diutimes = DiutimeInfo.Split(';');
            for (int i = Diutimes.Length; i > 0; i--)
            {
                if (Diutimes[i - 1] != "" && int.Parse(Diutimes[i - 1]) != 1)
                {
                    //获取稀释完成最少体积、
                    int MinSunDiuV = AddLiquidVol + DiuLeftVol;
                    float AddSample = float.Parse(MinSunDiuV.ToString()) / float.Parse(Diutimes[i - 1]);
                    int AddSampleV = 0;
                    int AddDiuV = 0;
                    if (AddSample < ImbibitionMin)
                        AddSampleV = ImbibitionMin;
                    else
                    {
                        if ((AddSample - (int)AddSample) != 0)
                        {
                            AddSampleV = (int)AddSample + 1;
                        }
                        else
                            AddSampleV = (int)AddSample;
                    }
                    AddDiuV = AddSampleV * int.Parse(Diutimes[i - 1]) - AddSampleV;
                    DiuVoInfo.Insert(0, AddSampleV.ToString() + ";" + AddDiuV);
                    AddLiquidVol = AddSampleV;
                }
            }
            return DiuVoInfo;
        }
        private bool addLiquiding = false;
        /// <summary>
        /// 实验运行中加液步骤
        /// </summary>
        /// <param name="testS">当前步骤对象</param>
        private void addLiquid(object testS)
        {
            addLiquiding = true;
            //lock (addLiquiding)
            //{
            //NetCom3.ComWait.WaitOne(-1);
            //frmMain.barrelFlag = true;
            //将object形转换为TestSchedule形
            TestSchedule testTempS = testS as TestSchedule;
            //获取试剂盘上对应实验名称的试剂信息
            List<ReagentIniInfo> lisRIinfo = QueryReagentIniInfo();
            if (lisRIinfo.Count > 0)
            {
                foreach (ReagentIniInfo reagentIniInfo in lisRIinfo)
                {
                    //2018-09-04 zlx  mod
                    DbHelperOleDb db = new DbHelperOleDb(3);
                    DbHelperOleDb.ExecuteSql(3, @"update tbReagent set leftoverTestR1 =" + reagentIniInfo.LeftReagent1 + ",leftoverTestR2 = " + reagentIniInfo.LeftReagent2 +
                                              ",leftoverTestR3 = " + reagentIniInfo.LeftReagent3 + ",leftoverTestR4 = " + reagentIniInfo.LeftReagent4 + " where BarCode = '"
                                                  + reagentIniInfo.BarCode + "' and ReagentName = '" + reagentIniInfo.ItemName + "'");
                }
            }
            DbHelperOleDb dba = new DbHelperOleDb(3);
            //DataTable dtRegent = new BLL.tbReagent().GetList("ReagentName='" + testTempS.ItemName.ToString() + "' AND Status='正常' AND leftoverTestR1>=0 ").Tables[0];
            DataTable dtRegent = new BLL.tbReagent().GetList("ReagentName='" + testTempS.ItemName.ToString() + "' AND Status='正常' AND Postion<>''  AND leftoverTestR1>=0 ").Tables[0]; //lyq mod 20201021
            DataRow[] drRg;
            foreach (DataRow dr in dtRegent.Rows)
            {
                drRg = dtRgInfo.Select("Postion='" + dr["Postion"] + "'");
                if (drRg.Length > 0)
                {
                    drRg[0]["leftoverTestR1"] = dr["leftoverTestR1"].ToString();
                    drRg[0]["leftoverTestR2"] = dr["leftoverTestR2"].ToString();
                    drRg[0]["leftoverTestR3"] = dr["leftoverTestR3"].ToString();
                    drRg[0]["leftoverTestR4"] = dr["leftoverTestR4"].ToString();
                }
            }
            //drRg = dtRgInfo.Select("RgName='" + testTempS.ItemName.ToString() + "' AND leftoverTestR1>0").OrderBy(x => int.Parse(x["Postion"].ToString())).ToArray();
            //DataRow[] 
            drRg = dtRgInfo.Select("RgName='" + testTempS.ItemName.ToString() + "' AND leftoverTestR1>0").OrderBy(x => int.Parse(x["leftoverTestR1"].ToString())).ToArray();
            int rgPos = 0;
            int rgindex = -1;
            //int NoUsePro = 0;//2018-10-13 zlx add
            int AddErrorCount;
            switch (testTempS.TestScheduleStep)
            {
                case TestSchedule.ExperimentScheduleStep.AddLiquidTube:
                    string[] TestStepSingle = testTempS.singleStep.Split('-');
                    string[] LiquidVol = testTempS.AddLiqud.Split('-');
                    int PlugneedleCount = 0;
                    for (int j = 0; j < TestStepSingle.Length; j++)
                    {
                        //稀释步骤添加。 LYN add 20171114
                        if (TestStepSingle[j] == "D")
                        {
                            AddingSampleFlag = true;
                            while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                            {
                                Thread.Sleep(30);
                            }
                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Diluting"), testTempS.TestID });

                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "D")]
                                = Color.Yellow;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            string[] diupos = testTempS.dilutionPos.Split('-');
                            DbHelperOleDb db = new DbHelperOleDb(0);//2019-02-28 zlx add
                            string DiluteCount = DbHelperOleDb.GetSingle(0, @"select DiluteCount from tbProject where ShortName 
                                                                             = '" + testTempS.ItemName + "'").ToString();
                            db = new DbHelperOleDb(0);
                            string DiluteName = DbHelperOleDb.GetSingle(0, @"select DiluteName from tbProject where ShortName ='" + testTempS.ItemName + "'").ToString();
                            int ExtraDiluteC = int.Parse(testTempS.dilutionTimes) / int.Parse(DiluteCount);
                            if (ExtraDiluteC > 1)
                            {
                                if (DiluteName == "1")
                                    DiluteName = DiuInfo.GetDiuInfo(ExtraDiluteC);
                                else
                                    DiluteName = DiuInfo.GetDiuInfo(ExtraDiluteC) + ";" + DiluteName;
                            }
                            #region 稀释
                            if (DiluteName != "1")
                            {
                                List<string> diuList = GetDiuVol(int.Parse(LiquidVol[j]), DiluteName);
                                string Pos = "";
                                for (int i = 0; i < diuList.Count; i++)
                                {
                                    int SampleVol = int.Parse(diuList[i].Split(';')[0]);
                                    int DiuVol = int.Parse(diuList[i].Split(';')[1]);
                                    #region 获取稀释液位置
                                    List<ReagentIniInfo> currentReagent =
                                         QueryReagentIniInfo().Where(reagent => reagent.ItemName == testTempS.ItemName.ToString()).ToList();
                                    string DiuName = "";
                                    if (currentReagent.Count > 0)
                                    {
                                        foreach (var reagent in currentReagent)
                                        {
                                            string DiuPos = OperateIniFile.ReadIniData("ReagentPos" + reagent.Postion, "DiuPos", "", iniPathReagentTrayInfo);
                                            if (DiuPos != "")
                                            {
                                                DiuName = OperateIniFile.ReadIniData("ReagentPos" + DiuPos, "ItemName", "", iniPathReagentTrayInfo);
                                                break;
                                            }
                                            //string diulest = 
                                            //    OperateIniFile.ReadIniData("ReagentPos" + reagent.Postion, "leftDiuVol", "", iniPathReagentTrayInfo);
                                            //if ((!string.IsNullOrEmpty(diulest)) && (int.Parse(diulest) > DiuVol + abanDiuPro + DiuNoUsePro))
                                            //{
                                            //    rgPos = int.Parse(reagent.Postion);//获取该试剂位置编号
                                            //    break;
                                            //}
                                        }
                                    }
                                    if (DiuName != "")
                                    {
                                        currentReagent = QueryReagentIniInfo().Where(reagent => reagent.ItemName == DiuName && reagent.LeftReagent1 > 0)
                                        .ToList();
                                        foreach (var reagent in currentReagent)
                                        {
                                            string diulest =
                                                OperateIniFile.ReadIniData("ReagentPos" + reagent.Postion, "LeftReagent1", "", iniPathReagentTrayInfo);
                                            if ((!string.IsNullOrEmpty(diulest)) && (int.Parse(diulest) > DiuVol + abanDiuPro + DiuNoUsePro))
                                            {
                                                rgPos = int.Parse(reagent.Postion);//获取该试剂位置编号
                                                break;
                                            }
                                        }
                                    }
                                    LogFile.Instance.Write("rgPos:" + rgPos);
                                    #endregion
                                    if (rgPos > 0)
                                    {
                                        //新管位置
                                        int pos = int.Parse(diupos[i]);
                                        #region 加新管
                                        while (MoveTubeUseFlag)
                                            NetCom3.Delay(10);
                                        rackToReact(pos);
                                        if (TubeStop)//加新管时暂存盘为空 2019-02-22
                                        {
                                            RemoveTestList(testTempS, getString("keywordText.LackTube"));
                                            #region 稀释不成功扔管
                                            for (int index = 1; index < 4; index++)
                                            {
                                                string exitStatus = OperateIniFile.ReadIniData("ReactTrayInfo",
                                                "no" + index, "", iniPathReactTrayInfo);
                                                if (exitStatus == "1" || exitStatus == "2")
                                                {
                                                    ReactToAband(index);
                                                }
                                            }
                                            #endregion
                                            goto outAddLiquidTube;
                                        }
                                        #endregion
                                        #region 加需稀释的样本
                                        int samplePos;//获取样本位置。
                                        if (i == 0)
                                            samplePos = testTempS.samplePos;
                                        else
                                            samplePos = int.Parse(diupos[i - 1]);
                                        AddErrorCount = 0;
                                        PlugneedleCount = 0;
                                        if (i == 0)
                                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " " + SampleVol.ToString("x2")), 0);
                                        else
                                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 02 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " " + SampleVol.ToString("x2")), 0);
                                        if (!NetCom3.Instance.SPQuery())
                                        {
                                            #region 异常处理
                                            string againSend = "";
                                            if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                            {
                                                AddErrorCount++;
                                                //重新发送指令
                                                if (againSend == "")
                                                    againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                    + " 00";
                                            }
                                            else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                            {
                                                //重新发送指令
                                                if (againSend == "")
                                                    againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                    + " " + SampleVol.ToString("x2");
                                            }
                                            else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.AddSampleOver"), getString("keywordText.Samplingneedle"), 1);
                                                AllStop();
                                            }
                                            else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.putKnocked && PlugneedleCount < 2 && i == 0)
                                            {
                                                PlugneedleCount++;
                                                //加样管路灌注 
                                                againSend = "EB 90 31 02 08";
                                                int flag = SendAgain(againSend, 0);
                                                if (flag == (int)ErrorState.Success)
                                                {
                                                    ReactToAband(pos);
                                                    rackToReact(pos);
                                                    if (TubeStop)
                                                    {
                                                        RemoveTestList(testTempS, getString("keywordText.LackTube"));
                                                        goto outAddLiquidTube;
                                                    }
                                                    againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                        + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2");
                                                }
                                                else if (flag == (int)ErrorState.putKnocked)
                                                {
                                                    if (testTempS.TestID == 1)
                                                    {
                                                        NetCom3.Instance.stopsendFlag = true;
                                                        ShowWarnInfo(getString("keywordText.AddSPlugneedle"), getString("keywordText.Samplingneedle"), 1);
                                                        AllStop();
                                                    }
                                                    else
                                                    {
                                                        PlugneedleCount++;
                                                        PlugneedleFalg = true;
                                                        string Message = DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TestId") + testTempS.TestID + getString("keywordText.AddSPlugneedle") + ";" + getString("keywordText.TestStopedAddS");
                                                        MoveTubeListAddTubeDispose(pos);
                                                        RemoveTestList(testTempS, getString("keywordText.AddSPlugneedle"));
                                                        LogFileAlarm.Instance.Write(Message);
                                                        goto outAddLiquidTube;
                                                    }
                                                }
                                                else if (flag == (int)ErrorState.IsKnocked)
                                                {
                                                    AddErrorCount++;
                                                    againSend = "EB 90 31 02 08";
                                                }

                                            }
                                        Again:
                                            int sendFlag = SendAgain(againSend, 0);
                                            if (sendFlag == (int)ErrorState.Sendfailure)
                                                goto Again;
                                            else if (sendFlag == (int)ErrorState.IsKnocked)
                                                AddErrorCount++;
                                            else if (sendFlag == (int)ErrorState.putKnocked)
                                            {
                                                PlugneedleCount++;
                                                if (testTempS.TestID == 1)
                                                {
                                                    NetCom3.Instance.stopsendFlag = true;
                                                    ShowWarnInfo(getString("keywordText.AddSPlugneedle"), getString("keywordText.Samplingneedle"), 1);
                                                    AllStop();
                                                }
                                                else
                                                {
                                                    PlugneedleCount++;
                                                    PlugneedleFalg = true;
                                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TestId") + testTempS.TestID + getString("keywordText.AddSPlugneedle") + ";" + getString("keywordText.TestStopedAddS"));
                                                    MoveTubeListAddTubeDispose(pos);
                                                    RemoveTestList(testTempS, getString("keywordText.AddSPlugneedle"));
                                                    goto outAddLiquidTube;
                                                }

                                            }
                                            #endregion
                                        }
                                        if (NetCom3.Instance.LiquidLevelDetectionFlag == (int)LiquidLevelDetectionAlarm.Low &&
                                            LiquidLevelDetectionEvent != null &&
                                            (!(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))))
                                            LiquidLevelDetectionEvent(getString("keywordText.SamplePos") + samplePos + "：" + getString("keywordText.SampleClot"), 2);
                                        //配置文件进行修改
                                        //diupos[0] = pos.ToString();
                                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + diupos[0], "2", iniPathReactTrayInfo);
                                        if (AddErrorCount > 0)
                                        {
                                            if (AddErrorCount > 1)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.AddSampleIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                                AllStop();
                                            }
                                            else
                                            {
                                                MoveTubeListAddTubeDispose(pos);
                                                RemoveTestList(testTempS, getString("keywordText.AddSampleIsKnocked"));
                                            }
                                            break;
                                        }
                                        #endregion
                                        #region 加稀释液
                                        AddErrorCount = 0;
                                        AddErrorCount = AddLiquid(rgPos, pos, DiuVol);
                                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.AddDiuOver"), getString("keywordText.Samplingneedle"), 1);
                                            AllStop();
                                        }
                                        if (AddErrorCount > 0)
                                        {
                                            if (AddErrorCount > 1)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.AddDiuOverIsKnocked"), getString("keywordText.Samplingneedle"), 1);
                                                AllStop();
                                            }
                                            else
                                            {
                                                MoveTubeListAddTubeDispose(pos);
                                                RemoveTestList(testTempS, getString("keywordText.AddDiuOverIsKnocked"));
                                            }
                                            break;
                                        }
                                        DataRow[] drDiu = dtRgInfo.Select("Postion='" + rgPos + "'");
                                        drDiu[0]["leftoverTestR1"] = OperateIniFile.ReadIniData("ReagentPos" + rgPos, "LeftReagent1", "", iniPathReagentTrayInfo);
                                        string rgBar = OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "BarCode", "", iniPathReagentTrayInfo);
                                        DbHelperOleDb.ExecuteSql(3, @"update tbReagent set leftoverTestR1 =" + (drDiu[0]["leftoverTestR1"]).ToString() + " where BarCode = '"
                                                + rgBar + "' and Postion = '" + rgPos.ToString() + "'");
                                    #endregion
                                    //混匀
                                    AgainMix:
                                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                                        if (!NetCom3.Instance.SPQuery())//NetCom3.Instance.MoveQuery()
                                        {
                                            #region 异常处理

                                            if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                            {
                                                goto AgainMix;
                                            }
                                            else
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + pos, getString("keywordText.Mix"), 1);
                                                AllStop();
                                                break;
                                            }
                                            #endregion
                                        }
                                    }
                                    else
                                    {
                                        RemoveTestList(testTempS, getString("keywordText.NoDiu"));
                                        return;
                                    }
                                    #region 扔上一次稀释的管
                                    if (i > 0)
                                    {
                                        ReactToAband(int.Parse(diupos[i - 1]));
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "D")]
                                = Color.Gray;
                            toGray(testTempS.TestID - 1, StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "D"));
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            AddingSampleFlag = false;
                        }
                        else if (TestStepSingle[j] == "S")
                        {
                            AddingSampleFlag = true;
                            while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                            {
                                Thread.Sleep(30);
                            }
                            #region 记录
                            StringBuilder records = new StringBuilder();
                            records.Append("实验ID testTempS.TestID  " + testTempS.TestID + Environment.NewLine);
                            records.Append("dgvWorkListData.Rows  " + dgvWorkListData.Rows.Count + Environment.NewLine);
                            LogFile.Instance.Write(DateTime.Now + ":" + records);
                            #endregion 

                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingS"), testTempS.TestID });
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "S")]
                                = Color.Yellow;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            #region 加样操作
                            int samplePos = int.Parse(testTempS.getSamplePos.Substring(1));//获取样本位置。                                                                                           //获取当前反应盘可用位置
                            int pos = testTempS.AddSamplePos;
                            string cupType = testTempS.SampleContainer;//获取样本杯类型
                            if (testTempS.getSamplePos.Contains("R"))
                            {
                                while (lisMoveTube.Count != 0)//查询反应盘是否空闲，如不空闲，持续等待
                                {
                                    Thread.Sleep(100);
                                }
                                AddErrorCount = 0;
                                PlugneedleCount = 0;
                                ///反应盘转到取样位置SamplePos，加样针加样到反应盘pos位置。
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 02 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                    + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2")), 0);
                                if (!NetCom3.Instance.SPQuery())
                                {
                                #region 异常处理
                                Again:
                                    string againSend = "";
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                    {
                                        AddErrorCount++;
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 02 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " 00";
                                    }
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                    {
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 02 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2");
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddSampleOver"), getString("keywordText.Samplingneedle"), 1);
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                        //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }

                                    int sendFlag = SendAgain(againSend, 0);
                                    if (sendFlag == (int)ErrorState.Sendfailure)
                                        goto Again;
                                    else if (sendFlag == (int)ErrorState.IsKnocked)
                                        AddErrorCount++;

                                    #endregion
                                }
                                if (NetCom3.Instance.LiquidLevelDetectionFlag == (int)LiquidLevelDetectionAlarm.Low &&
                                   LiquidLevelDetectionEvent != null &&
                                   (!(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))))
                                    LiquidLevelDetectionEvent(getString("keywordText.SamplePos") + samplePos + getString("keywordText.SampleClot"), 2);
                                //该位置信息写入配置文件
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + pos.ToString(), "2", iniPathReactTrayInfo);
                                if (AddErrorCount > 0)
                                {
                                    if (AddErrorCount > 1)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddSampleIsKnocked"), getString("keywordText.Samplingneedle"), 1);
                                        AllStop();
                                    }
                                    else
                                    {
                                        MoveTubeListAddTubeDispose(pos);
                                        MoveTubeListAddTubeDispose(samplePos);
                                        RemoveTestList(testTempS, getString("keywordText.AddSampleIsKnocked"));
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                AddErrorCount = 0;
                                ///样本盘转到取样位置SamplePos，加样针加样到反应盘pos位置。
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                    + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2")), 0);
                                if (!NetCom3.Instance.SPQuery())
                                {
                                    #region 异常处理

                                    string againSend = "";
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                    {
                                        AddErrorCount++;
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " 00";
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                    {
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2");
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddSampleOver"), getString("keywordText.Samplingneedle"), 1);
                                        AllStop();
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.putKnocked && PlugneedleCount < 2)
                                    {
                                        PlugneedleCount++;
                                        //加样管路灌注 
                                        againSend = "EB 90 31 02 08";
                                        int flag = SendAgain(againSend, 0);
                                        if (flag == (int)ErrorState.Success)
                                        {
                                            ReactToAband(pos);
                                            rackToReact(pos);
                                            if (TubeStop)
                                            {
                                                RemoveTestList(testTempS, getString("keywordText.LackTube"));
                                                goto outAddLiquidTube;
                                            }
                                            againSend = "EB 90 31 02 01 " + samplePos.ToString("x2") + " " + pos.ToString("x2")
                                                + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2");
                                        }
                                        else if (flag == (int)ErrorState.putKnocked)
                                        {
                                            if (testTempS.TestID == 1)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.AddSPlugneedle"), getString("keywordText.Samplingneedle"), 1);
                                                AllStop();
                                            }
                                            else
                                            {
                                                PlugneedleCount++;
                                                PlugneedleFalg = true;
                                                string Message = DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TestId") + testTempS.TestID + getString("keywordText.AddSPlugneedle") + ";" + getString("keywordText.TestStopedAddS");
                                                MoveTubeListAddTubeDispose(pos);
                                                RemoveTestList(testTempS, getString("keywordText.AddSPlugneedle"));
                                                LogFileAlarm.Instance.Write(Message);
                                                goto outAddLiquidTube;
                                            }
                                        }
                                        else if (flag == (int)ErrorState.IsKnocked)
                                        {
                                            AddErrorCount++;
                                            againSend = "EB 90 31 02 08";
                                        }
                                    }
                                Again:
                                    int sendFlag = SendAgain(againSend, 0);
                                    if (sendFlag == (int)ErrorState.Sendfailure)
                                        goto Again;
                                    else if (sendFlag == (int)ErrorState.IsKnocked)
                                        AddErrorCount++;
                                    else if (sendFlag == (int)ErrorState.putKnocked)
                                    {
                                        PlugneedleCount++;
                                        if (testTempS.TestID == 1)
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.AddSPlugneedle"), getString("keywordText.Samplingneedle"), 1);
                                            AllStop();
                                        }
                                        else
                                        {
                                            PlugneedleCount++;
                                            PlugneedleFalg = true;
                                            LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TestId") + testTempS.TestID + getString("keywordText.AddSPlugneedle") + ";" + getString("keywordText.TestStopedAddS"));
                                            MoveTubeListAddTubeDispose(pos);
                                            RemoveTestList(testTempS, getString("keywordText.AddSPlugneedle"));
                                            goto outAddLiquidTube;
                                        }

                                    }
                                    #endregion
                                }
                                if (NetCom3.Instance.LiquidLevelDetectionFlag == (int)LiquidLevelDetectionAlarm.Low &&
                                    LiquidLevelDetectionEvent != null &&
                                    (!(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))))
                                    LiquidLevelDetectionEvent(getString("keywordText.SamplePos") + samplePos + "：" + getString("keywordText.SampleClot"), 2);
                                //该位置信息写入配置文件
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + pos.ToString(), "2", iniPathReactTrayInfo);
                                if (AddErrorCount > 0)
                                {
                                    if (AddErrorCount > 1)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddSampleIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                        //MessageBox.Show("加样针撞针未能修复，实验已终止", "错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    else
                                    {
                                        MoveTubeListAddTubeDispose(pos);
                                        RemoveTestList(testTempS, getString("keywordText.AddSampleIsKnocked"));
                                    }
                                    break;
                                }

                            }

                            #region 扔第二次稀释的管
                            if (testTempS.getSamplePos.Contains("R"))
                            {
                                ReactToAband(samplePos);
                            }
                            #endregion

                            #endregion
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "S")]
                                = Color.Gray;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            AddingSampleFlag = false;
                        }
                        else if (TestStepSingle[j] == "R1")
                        {
                            int index = TestStepSingle.ToList().IndexOf("R1");
                            while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                            {
                                Thread.Sleep(30);
                            }
                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR1"), testTempS.TestID });
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1")]
                                = Color.Yellow;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            #region 加R1操作
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //drRg = dtRgInfo.Select("RgName='" + testTempS.ItemName.ToString() + "'");
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 添加
                                    //if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")) || dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品"))
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")) || dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Control")))
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString() || int.Parse(drRg[g]["leftoverTestR1"].ToString()) <= 0)
                                            continue;
                                        else
                                        {
                                            rgPos = int.Parse(drRg[g]["Postion"].ToString());
                                            rgindex = g;
                                        }
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR1"].ToString()) > 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        string Batch = drRg[g]["Batch"].ToString();
                                        if (dgvWorkListData != null && dgvWorkListData.Rows.Count != 0)
                                        {
                                            this.BeginInvoke(new Action(() =>
                                            {
                                                dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value =
                                                    Batch;
                                                dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value =
                                                   rgPos;
                                            }));
                                        }
                                        rgindex = g;
                                        //NoUsePro = Convert.ToInt32(drRg[rgindex]["NoUsePro"].ToString().Split('-')[0]);//2018-10-13 zlx mod
                                        break;
                                    }
                                }
                            }
                            if (rgPos > 0)
                            {
                                //获取当前反应盘可用位置
                                int pos = testTempS.AddSamplePos;
                                //剩余R1体积
                                int leftR1 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", "", iniPathReagentTrayInfo));
                                //string leftR1Vol = (leftR1 * int.Parse(LiquidVol[j].Trim()) + (int)(int.Parse(LiquidVol[j].Trim()) * abanR1Pro)).ToString("x4");
                                string leftR1Vol = (RegentNoUsePro + leftR1 * int.Parse(LiquidVol[j].Trim()) + leftR1 * abanR1Pro).ToString("x4");//2018-10-13  zlx mod
                                AddErrorCount = 0;
                                ///取试剂盘rgPos位置试剂加到反应盘Pos位置///洗针
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                    + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2)), 0);
                                if (!NetCom3.Instance.SPQuery())
                                {
                                #region 异常处理
                                Again:
                                    string againSend = "";
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                    {
                                        AddErrorCount++;
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = againSend = "EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                    + " 00 " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                    {
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                    + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        //setmainformbutten();
                                        ShowWarnInfo("加样针加试剂时指令接收超时", "加样", 1);
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "加样针在加试剂1时指令接收超时！");
                                        //frmMsgShow.MessageShow("加样错误提示", "指令接收超时，实验已终止");
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    int sendFlag = SendAgain(againSend, 0);
                                    if (sendFlag == (int)ErrorState.Sendfailure)
                                        goto Again;
                                    else if (sendFlag == (int)ErrorState.IsKnocked)
                                        AddErrorCount++;
                                    #endregion
                                }
                                //以下设置moveTube，即反应盘对应需加新管位置（testTempS.TestID+DValue+10）到放管位置
                                drRg[rgindex]["leftoverTestR1"] = leftR1 - 1;
                                OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", (leftR1 - 1).ToString(), iniPathReagentTrayInfo);
                                //lyq
                                string rgBar = OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "BarCode", "", iniPathReagentTrayInfo);
                                DbHelperOleDb.ExecuteSql(3, @"update tbReagent set leftoverTestR1 =" + (leftR1 - 1).ToString() + " where BarCode = '"
                                                + rgBar + "' and Postion = '" + rgPos.ToString() + "'");
                                if (AddErrorCount > 0)
                                {
                                    if (AddErrorCount > 1)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        //setmainformbutten();
                                        ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "加样针加试剂1时发生撞针！");
                                        //frmMsgShow.MessageShow("加样错误提示", "加样针撞针未能修复，实验已终止");
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    else
                                    {
                                        MoveTubeListAddTubeDispose(pos);
                                        RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                    }
                                    break;
                                }
                                if (TestStepSingle[TestStepSingle.Length - 1] == "R1")
                                {
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                                    if (!NetCom3.Instance.SPQuery())
                                    {
                                        #region 异常处理
                                        string againSend = "";
                                    Again:
                                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                        {
                                            //重新发送指令
                                            if (againSend == "")
                                                againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                            if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                                goto Again;
                                        }
                                        else
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                            //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            AllStop();
                                            break;
                                        }
                                        #endregion
                                    }
                                }
                            }
                            #endregion
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1")]
                                = Color.Gray;
                            toGray(testTempS.TestID - 1, StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1"));
                            lisProBar[testTempS.TestID - 1].Invalidate();
                        }
                        else if (TestStepSingle[j] == "R2")
                        {
                            int index = TestStepSingle.ToList().IndexOf("R2");
                            while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                            {
                                Thread.Sleep(30);
                            }
                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR2"), testTempS.TestID });
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                                = Color.Yellow;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            #region 加R2过程
                            if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                                rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                            else
                            {
                                if (drRg.Length > 0)
                                {
                                    for (int g = 0; g < drRg.Length; g++)
                                    {
                                        //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                        //if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")) || dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品"))
                                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))
                                        //|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品"))
                                        {
                                            if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                                continue;
                                        }
                                        if (int.Parse(drRg[g]["leftoverTestR2"].ToString()) > 0)//判定试剂剩余量是否大于0
                                        {
                                            rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                            rgindex = g;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (rgPos > 0)
                            {
                                //获取当前反应盘可用位置
                                int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                                //剩余R2体积
                                //特殊项目处理
                                var isCurrentSpecialProject =
                                    File.ReadAllLines(System.Windows.Forms.Application.StartupPath + "//SpacialProjects.txt")
                                    .ToList()
                                    .Where(item => item == testTempS.ItemName).Count() > 0;
                                int reagentPosition;
                                //string SleftReagentTest = OperateIniFile.ReadIniData("ReagentPos" + dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo);
                                string SleftReagentTest = OperateIniFile.ReadIniData("ReagentPos" + rgPos, "LeftReagent2", "", iniPathReagentTrayInfo);
                                LogFile.Instance.Write("加R2的位置：" + rgPos + ";试剂剩余量为：" + SleftReagentTest);
                                int leftReagentTest = 0;
                                if (SleftReagentTest != "")
                                    leftReagentTest = int.Parse(SleftReagentTest);
                                //int leftReagentTest = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));

                                bool isSinglebottle = false;
                                if (rgPos.ToString() == "30")
                                {
                                    isSinglebottle = true;
                                }
                                else
                                {
                                    int position = int.Parse(rgPos.ToString());
                                    string name = OperateIniFile.ReadIniData("ReagentPos" + (position + 1), "ItemName", "", iniPathReagentTrayInfo);
                                    string barCode = OperateIniFile.ReadIniData("ReagentPos" + (position + 1), "BarCode", "", iniPathReagentTrayInfo);
                                    if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(barCode))
                                    {
                                        isSinglebottle = false;
                                    }
                                    else
                                    {
                                        isSinglebottle = true;
                                    }
                                }
                                int leftR2 = 0;
                                if (isCurrentSpecialProject && leftReagentTest > 50 && !isSinglebottle)//tpoab/b2-mg特殊项目
                                {
                                    leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo)) - 50;
                                    reagentPosition = rgPos;
                                }
                                else
                                {
                                    leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));

                                    if (isCurrentSpecialProject && !isSinglebottle)
                                    {
                                        reagentPosition = rgPos + 1;
                                    }
                                    else
                                    {
                                        reagentPosition = rgPos;
                                    }
                                }

                                string leftR2Vol = (RegentNoUsePro + leftR2 * int.Parse(LiquidVol[j].Trim()) + leftR2 * abanR2Pro).ToString("x4");

                                AddErrorCount = 0;
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 04 " + reagentPosition.ToString("x2") + " " + pos.ToString("x2")
                                        + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2)), 0);
                                if (!NetCom3.Instance.SPQuery())
                                {
                                #region 异常处理
                                Again:
                                    string againSend = "";
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                    {
                                        AddErrorCount++;
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 04 " + reagentPosition.ToString("x2") + " " + pos.ToString("x2")
                                       + " 00 " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                    {
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 04 " + reagentPosition.ToString("x2") + " " + pos.ToString("x2")
                                        + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                        //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    int sendFlag = SendAgain(againSend, 0);
                                    if (sendFlag == (int)ErrorState.Sendfailure)
                                        goto Again;
                                    else if (sendFlag == (int)ErrorState.IsKnocked)
                                        AddErrorCount++;
                                    #endregion
                                }
                                //修改试剂配制信息
                                //drRg[rgindex]["leftoverTestR2"] = leftR2 - 1;
                                for (int i = 0; i < drRg.Length; i++)
                                {
                                    if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                        drRg[i]["leftoverTestR2"] = leftR2 - 1;
                                }
                                leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));
                                OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", (leftR2 - 1).ToString(), iniPathReagentTrayInfo);
                                if (AddErrorCount > 0)
                                {
                                    if (AddErrorCount > 1)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                        //MessageBox.Show("加样针撞针未能修复，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    else
                                    {
                                        MoveTubeListAddTubeDispose(pos);
                                        RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                    }
                                    break;
                                }
                                if (TestStepSingle[TestStepSingle.Length - 1] == "R2")
                                {

                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);//add y 20180524混匀
                                    if (!NetCom3.Instance.SPQuery())//add y 20180524混匀
                                    {
                                        #region 异常处理
                                        string againSend = "";
                                    Again:
                                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                        {
                                            //重新发送指令
                                            if (againSend == "")
                                                againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                            if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                                goto Again;
                                        }
                                        else
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                            //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            //addLiquiding = false;
                                            AllStop();
                                            break;
                                        }
                                        #endregion
                                    }

                                }

                            }
                            #endregion
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                                = Color.Gray;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                        }
                        else if (TestStepSingle[j] == "R3")
                        {
                            int index = TestStepSingle.ToList().IndexOf("R3");
                            while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                            {
                                Thread.Sleep(30);
                            }
                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR3"), testTempS.TestID });
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R3")]
                                = Color.Yellow;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                            #region 加R3过程
                            if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                                rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                            else
                            {
                                if (drRg.Length > 0)
                                {
                                    for (int g = 0; g < drRg.Length; g++)
                                    {
                                        //2018-08-27 zlx add
                                        //if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")) || dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品"))
                                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))
                                        //|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品"))
                                        {
                                            if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                                continue;
                                        }
                                        if (int.Parse(drRg[g]["leftoverTestR3"].ToString()) > 0)//判定试剂剩余量是否大于0
                                        {
                                            rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                            rgindex = g;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (rgPos > 0)
                            {
                                //获取当前反应盘可用位置
                                int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                                //剩余R3体积
                                int leftR3 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent3", "", iniPathReagentTrayInfo));
                                string leftR3Vol = (RegentNoUsePro + leftR3 * int.Parse(LiquidVol[j].Trim()) + leftR3 * abanR3Pro).ToString("x4");//2018-10-13 zlx mod
                                                                                                                                                  //string leftR3Vol = (leftR3 * int.Parse(LiquidVol[j].Trim()) + (int)(int.Parse(LiquidVol[j].Trim()) * abanR3Pro)).ToString("x4");
                                AddErrorCount = 0;
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                        + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2)), 0);
                                if (!NetCom3.Instance.SPQuery())
                                {
                                #region 异常处理
                                Again:
                                    string againSend = "";
                                    if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                    {
                                        AddErrorCount++;
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                        + " 00 " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                    {
                                        //重新发送指令
                                        if (againSend == "")
                                            againSend = "EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                        + " " + int.Parse(LiquidVol[j].Trim()).ToString("x2") + " " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2);
                                    }
                                    else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                        //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    int sendFlag = SendAgain(againSend, 0);
                                    if (sendFlag == (int)ErrorState.Sendfailure)
                                        goto Again;
                                    else if (sendFlag == (int)ErrorState.IsKnocked)
                                        AddErrorCount++;
                                    #endregion
                                }
                                //drRg[rgindex]["leftoverTestR3"] = leftR3 - 1;
                                for (int i = 0; i < drRg.Length; i++)
                                {
                                    if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                        drRg[i]["leftoverTestR3"] = leftR3 - 1;
                                }
                                OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent3", (leftR3 - 1).ToString(), iniPathReagentTrayInfo);
                                if (AddErrorCount > 0)
                                {
                                    if (AddErrorCount > 1)
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                        //MessageBox.Show("加样针撞针未能修复，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        //addLiquiding = false;
                                        AllStop();
                                    }
                                    else
                                    {
                                        MoveTubeListAddTubeDispose(pos);
                                        RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                    }
                                    break;
                                }
                                if (TestStepSingle[TestStepSingle.Length - 1] == "R3")
                                {
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                                    if (!NetCom3.Instance.SPQuery())
                                    {
                                        #region 异常处理
                                        string againSend = "";
                                    Again:
                                        if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                        {
                                            //重新发送指令
                                            if (againSend == "")
                                                againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                            if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                                goto Again;
                                        }
                                        else
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                            //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            AllStop();
                                            break;
                                        }
                                        #endregion
                                    }
                                }

                            }
                            #endregion
                            lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R3")]
                                = Color.Gray;
                            lisProBar[testTempS.TestID - 1].Invalidate();
                        }
                    }
                    #region 夹新管
                    MoveTubeStatus moveTube6 = new MoveTubeStatus();
                    int pos1 = testTempS.AddSamplePos + toUsedTube;
                    if (pos1 > ReactTrayHoleNum)
                    {
                        pos1 = pos1 - ReactTrayHoleNum + 3;
                    }
                    string ss = OperateIniFile.ReadIniData("ReactTrayInfo", "no" + pos1, "", iniPathReactTrayInfo);
                    if ((ss == "2" || ss == "9") && !AddTubeStop.Contains(pos1))
                        AddTubeStop.Add(pos1);
                    else
                    {
                        moveTube6.StepNum = testTempS.stepNum;
                        moveTube6.putTubePos = "1-" + pos1.ToString();
                        moveTube6.TestId = testTempS.TestID;
                        //暂存盘夹新管未夹位置统计
                        int tubeNum1 = 0;
                        for (int i = 0; i < lisMoveTube.Count; i++)
                        {
                            if (lisMoveTube[i].TakeTubePos.Split('-')[0] == "0")
                            {
                                tubeNum1++;
                            }
                        }
                        moveTube6.TakeTubePos = "0-" + (int.Parse(OperateIniFile.ReadIniData("Tube", "TubePos", "1", iniPathSubstrateTube))
                            + tubeNum1).ToString();
                        lock (locker2)
                        {
                            lisMoveTube.Add(moveTube6);
                        }
                    }
                #endregion
                outAddLiquidTube:
                    DalayFlag = false;
                    stepTime = 0;
                    break;
                case TestSchedule.ExperimentScheduleStep.AddBeads:
                    try
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "B") - 1]
                        = Color.Gray;
                    }
                    catch (Exception e)
                    {
                        NetCom3.Instance.writeLog(e);
                    }
                    lisProBar[testTempS.TestID - 1].Invalidate();
                    while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                    {
                        Thread.Sleep(30);
                    }
                    BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingB"), testTempS.TestID });
                    lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "B")]
                        = Color.Yellow;
                    lisProBar[testTempS.TestID - 1].Invalidate();
                    #region 加磁珠过程
                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                        rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                    else
                    {
                        if (drRg.Length > 0)
                        {
                            for (int g = 0; g < drRg.Length; g++)
                            {
                                if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard")))
                                {
                                    if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString() || int.Parse(drRg[g]["leftoverTestR1"].ToString()) <= 0)
                                        continue;
                                }
                                if (int.Parse(drRg[g]["leftoverTestR1"].ToString()) >= 0)//判定试剂剩余量是否大于0
                                {
                                    rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                    if (dgvWorkListData != null && dgvWorkListData.Rows.Count != 0)
                                    {
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value =
                                                drRg[g]["Batch"].ToString();
                                            dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value =
                                               rgPos;
                                        }));
                                    }
                                    rgindex = g;
                                    break;
                                }
                            }
                        }
                    }
                    if (rgPos > 0)
                    {
                        //获取当前反应盘可用位置
                        int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                        //剩余R4体积
                        int leftR4 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent4", "", iniPathReagentTrayInfo));
                        string leftR4Vol = (RegentNoUsePro + leftR4 * int.Parse(testTempS.AddLiqud) + leftR4 * abanR1Pro).ToString("x4");//2018-10-13 zlx mod
                        ///取试剂盘rgPos位置试剂加到反应盘Pos位置///洗针
                        AddErrorCount = 0;
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 07 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                            + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR4Vol.Substring(0, 2) + " " + leftR4Vol.Substring(2, 2)), 0);
                        if (!NetCom3.Instance.SPQuery())
                        {
                        #region 异常处理
                        Again:
                            string againSend = "";
                            if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                            {
                                AddErrorCount++;
                                //重新发送指令
                                if (againSend == "")
                                    againSend = "EB 90 31 02 07 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                            + " 00 " + leftR4Vol.Substring(0, 2) + " " + leftR4Vol.Substring(2, 2);
                            }
                            else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //重新发送指令
                                if (againSend == "")
                                    againSend = "EB 90 31 02 07 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                            + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR4Vol.Substring(0, 2) + " " + leftR4Vol.Substring(2, 2);
                            }
                            else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.AddBOver"), getString("keywordText.Samplingneedle"), 1);
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //addLiquiding = false;
                                AllStop();
                            }
                            int sendFlag = SendAgain(againSend, 0);
                            if (sendFlag == (int)ErrorState.Sendfailure)
                                goto Again;
                            else if (sendFlag == (int)ErrorState.IsKnocked)
                                AddErrorCount++;
                            #endregion
                        }
                        for (int i = 0; i < drRg.Length; i++)
                        {
                            if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                drRg[i]["leftoverTestR4"] = leftR4 - 1;
                        }
                        OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent4", (leftR4 - 1).ToString(), iniPathReagentTrayInfo);
                        if (AddErrorCount > 0)
                        {
                            if (AddErrorCount > 1)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.AddBIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                AllStop();
                            }
                            else
                            {
                                MoveTubeListAddTubeDispose(pos);
                                RemoveTestList(testTempS, getString("keywordText.AddBIsKnocked"));
                            }
                            break;
                        }

                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);//add y 20180524混匀
                        if (!NetCom3.Instance.SPQuery())//add y 20180524混匀
                        {
                            #region 异常处理
                            string againSend = "";
                        Again:
                            if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                            {
                                //重新发送指令
                                if (againSend == "")
                                    againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                    goto Again;
                            }
                            else
                            {
                                NetCom3.Instance.stopsendFlag = true;

                                ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //addLiquiding = false;
                                AllStop();
                                break;
                            }
                            #endregion
                        }
                        //drRg = dtRgInfo.Select("RgName='" + testTempS.ItemName.ToString() + "'");

                    }
                    #endregion
                    lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "B")]
                        = Color.Gray;
                    lisProBar[testTempS.TestID - 1].Invalidate();
                    DalayFlag = false;
                    stepTime = 0;
                    break;
                case TestSchedule.ExperimentScheduleStep.AddSingleR:
                    #region 特殊项目装在两个试剂瓶,试剂2装载两个试剂船
                    bool isSinglebottle2 = false;
                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() == "30")
                    {
                        isSinglebottle2 = true;
                    }
                    else
                    {
                        int position = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        string name = OperateIniFile.ReadIniData("ReagentPos" + (position + 1), "ItemName", "", iniPathReagentTrayInfo);
                        string barCode = OperateIniFile.ReadIniData("ReagentPos" + (position + 1), "BarCode", "", iniPathReagentTrayInfo);
                        if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(barCode))
                        {
                            isSinglebottle2 = false;
                        }
                        else
                        {
                            isSinglebottle2 = true;
                        }
                    }
                    var isSpecialProject =
                        File.ReadAllLines(System.Windows.Forms.Application.StartupPath + "//SpacialProjects.txt")
                        .ToList()
                        .Where(item => item == testTempS.ItemName).Count() > 0;
                    if (testTempS.singleStep == "R2" && isSpecialProject &&
                        int.Parse(OperateIniFile.ReadIniData("ReagentPos" + dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString(),
                        "LeftReagent2", "", iniPathReagentTrayInfo)) <= 50 && !isSinglebottle2)
                        testTempS.singleStep = "NextLocationR2";
                    #endregion

                    if (testTempS.singleStep == "R1")
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1") - 1]
                        = Color.Gray;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                        {
                            Thread.Sleep(30);
                        }
                        BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR1"), testTempS.TestID });
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1")]
                            = Color.Yellow;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        #region 加R过程
                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                            rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        else
                        {
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                                        || dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Control")))
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                            continue;
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR1"].ToString()) > 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value = rgPos;
                                        rgindex = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (rgPos > 0)
                        {
                            //获取当前反应盘可用位置
                            int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                            //剩余R1体积
                            int leftR1 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", "", iniPathReagentTrayInfo));
                            string leftR1Vol = (RegentNoUsePro + leftR1 * int.Parse(testTempS.AddLiqud) + leftR1 * abanR1Pro).ToString("x4");//2018-10-13 zlx mod
                                                                                                                                             //string leftR1Vol = (leftR1 * int.Parse(testTempS.AddLiqud) + (int)(int.Parse(testTempS.AddLiqud) * abanR1Pro)).ToString("x4");
                            ///取试剂盘rgPos位置试剂加到反应盘Pos位置///洗针
                            AddErrorCount = 0;
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2)), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                            #region 异常处理
                            Again:
                                string againSend = "";
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                {
                                    AddErrorCount++;
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                              + " 00 " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 03 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR1Vol.Substring(0, 2) + " " + leftR1Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                    //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //addLiquiding = false;
                                    AllStop();
                                }
                                int sendFlag = SendAgain(againSend, 0);
                                if (sendFlag == (int)ErrorState.Sendfailure)
                                    goto Again;
                                else if (sendFlag == (int)ErrorState.IsKnocked)
                                    AddErrorCount++;
                                #endregion
                            }
                            for (int i = 0; i < drRg.Length; i++)
                            {
                                if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                    drRg[i]["leftoverTestR1"] = leftR1 - 1;
                            }
                            OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", (leftR1 - 1).ToString(), iniPathReagentTrayInfo);
                            //lyq
                            string rgBar = OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "BarCode", "", iniPathReagentTrayInfo);
                            DbHelperOleDb.ExecuteSql(3, @"update tbReagent set leftoverTestR1 =" + (leftR1 - 1).ToString() + " where BarCode = '"
                                            + rgBar + "' and Postion = '" + rgPos.ToString() + "'");
                            if (AddErrorCount > 0)
                            {
                                if (AddErrorCount > 1)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                    AllStop();
                                }
                                else
                                {
                                    RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                    MoveTubeListAddTubeDispose(pos);
                                }
                                break;
                            }

                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                                #region 异常处理
                                string againSend = "";
                            Again:
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                    if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                        goto Again;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                    //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //addLiquiding = false;
                                    AllStop();
                                    break;
                                }
                                #endregion
                            }


                        }
                        #endregion
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R1")]
                            = Color.Gray;
                    }
                    else if (testTempS.singleStep == "R2")
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2") - 1]
                        = Color.Gray;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                        {
                            Thread.Sleep(30);
                        }
                        BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR2"), testTempS.TestID });
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                            = Color.Yellow;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        #region 加R过程
                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                            rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        else
                        {
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                                        /*|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品")*/)
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                            continue;
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR2"].ToString()) > 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        rgindex = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (rgPos > 0)
                        {
                            //获取当前反应盘可用位置
                            int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                            //剩余R2体积
                            int leftR2 = 0;
                            if (isSpecialProject && !isSinglebottle2)//tpoab/b2-mg特殊项目
                            {
                                leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo)) - 50;
                            }
                            else
                            {
                                leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));
                            }

                            string leftR2Vol = (RegentNoUsePro + leftR2 * int.Parse(testTempS.AddLiqud) + leftR2 * abanR2Pro).ToString("x4");//2018-10-13 zlx mod
                            AddErrorCount = 0;
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 04 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2)), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                            #region 异常处理
                            Again:
                                string againSend = "";
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                {
                                    AddErrorCount++;
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 04 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                + " 00 " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 04 " + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                    //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //addLiquiding = false;
                                    AllStop();
                                }
                                int sendFlag = SendAgain(againSend, 0);
                                if (sendFlag == (int)ErrorState.Sendfailure)
                                    goto Again;
                                else if (sendFlag == (int)ErrorState.IsKnocked)
                                    AddErrorCount++;
                                #endregion
                            }
                            for (int i = 0; i < drRg.Length; i++)
                            {
                                if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                    drRg[i]["leftoverTestR2"] = leftR2 - 1;
                            }
                            leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));
                            OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", (leftR2 - 1).ToString(), iniPathReagentTrayInfo);
                            if (AddErrorCount > 0)
                            {
                                if (AddErrorCount > 1)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                    AllStop();
                                }
                                else
                                {
                                    MoveTubeListAddTubeDispose(pos);
                                    RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                }
                                break;
                            }

                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                                #region 异常处理
                                string againSend = "";
                            Again:
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                    if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                        goto Again;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                    //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    AllStop();
                                    break;
                                }
                                #endregion
                            }

                        }
                        #endregion
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                            = Color.Gray;
                    }
                    else if (testTempS.singleStep == "R3")
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R3") - 1]
                        = Color.Gray;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                        {
                            Thread.Sleep(30);
                        }
                        BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR3"), testTempS.TestID });
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R3")]
                            = Color.Yellow;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        #region 加R过程
                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                            rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        else
                        {
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                                        /*|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品")*/)
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                            continue;
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR3"].ToString()) > 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        rgindex = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (rgPos > 0)
                        {
                            //获取当前反应盘可用位置
                            int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                            //剩余R3体积
                            int leftR3 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent3", "", iniPathReagentTrayInfo));
                            string leftR3Vol = (RegentNoUsePro + leftR3 * int.Parse(testTempS.AddLiqud) + leftR3 * abanR3Pro).ToString("x4");//2018-10-13 zlx mod
                            AddErrorCount = 0;
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2)), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                            #region 异常处理
                            Again:
                                string againSend = "";
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                {
                                    AddErrorCount++;
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " 00 " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 03 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR3Vol.Substring(0, 2) + " " + leftR3Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                    //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //addLiquiding = false;
                                    AllStop();
                                }
                                int sendFlag = SendAgain(againSend, 0);
                                if (sendFlag == (int)ErrorState.Sendfailure)
                                    goto Again;
                                else if (sendFlag == (int)ErrorState.IsKnocked)
                                    AddErrorCount++;
                                #endregion
                            }
                            //drRg[rgindex]["leftoverTestR3"] = leftR3 - 1;
                            for (int i = 0; i < drRg.Length; i++)
                            {
                                if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                    drRg[i]["leftoverTestR3"] = leftR3 - 1;
                            }
                            OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent3", (leftR3 - 1).ToString(), iniPathReagentTrayInfo);
                            if (AddErrorCount > 0)
                            {
                                if (AddErrorCount > 1)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                    //MessageBox.Show("加样针撞针未能修复，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    AllStop();
                                }
                                else
                                {
                                    MoveTubeListAddTubeDispose(pos);
                                    RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                }
                                break;
                            }

                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                                #region 异常处理
                                string againSend = "";
                            Again:
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                    if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                        goto Again;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                    //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    AllStop();
                                    break;
                                }
                                #endregion
                            }

                        }
                        #endregion
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R3")]
                            = Color.Gray;
                    }
                    else if (testTempS.singleStep == "RD")
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "RD") - 1]
                        = Color.Gray;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                        {
                            Thread.Sleep(30);
                        }
                        BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingRD"), testTempS.TestID });
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "RD")]
                            = Color.Yellow;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        #region 加R过程
                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                            rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        else
                        {
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                                        /*|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品")*/)
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                            continue;
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR1"].ToString()) >= 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        rgindex = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (rgPos > 0)
                        {
                            int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;

                            AddReagentD(rgPos, pos, int.Parse(testTempS.AddLiqud));

                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                                #region 异常处理
                                string againSend = "";
                            Again:
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                    if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                        goto Again;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                    //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    AllStop();
                                    break;
                                }
                                #endregion
                            }
                        }
                        #endregion
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "RD")]
                            = Color.Gray;
                    }
                    else if (testTempS.singleStep == "NextLocationR2")//仅用于TPOAB/b2-mg
                    {
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2") - 1]
                        = Color.Gray;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        while (!this.IsHandleCreated)//为了防止出现“在创建窗口句柄之前，不能在控件上调用 Invoke 或 BeginInvoke”的错误
                        {
                            Thread.Sleep(30);
                        }
                        BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingR2"), testTempS.TestID });
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                            = Color.Yellow;
                        lisProBar[testTempS.TestID - 1].Invalidate();
                        #region 加R过程
                        if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString() != "")
                            rgPos = int.Parse(dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentPos"].Value.ToString());
                        else
                        {
                            if (drRg.Length > 0)
                            {
                                for (int g = 0; g < drRg.Length; g++)
                                {
                                    //标准品、质控品以及其他只能使用它们自身的试剂 2018-08-27 zlx add
                                    if (dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                                        /*|| dgvWorkListData.Rows[testTempS.TestID - 1].Cells["SampleType"].Value.ToString().Contains("质控品")*/)
                                    {
                                        if (drRg[g]["Batch"].ToString() != dgvWorkListData.Rows[testTempS.TestID - 1].Cells["RegentBatch"].Value.ToString())
                                            continue;
                                    }
                                    if (int.Parse(drRg[g]["leftoverTestR2"].ToString()) > 0)//判定试剂剩余量是否大于0
                                    {
                                        rgPos = int.Parse(drRg[g]["Postion"].ToString());//获取该试剂位置编号
                                        rgindex = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (rgPos > 0)
                        {
                            //获取当前反应盘可用位置
                            int pos = (testTempS.AddSamplePos) % ReactTrayHoleNum == 0 ? ReactTrayHoleNum : (testTempS.AddSamplePos) % ReactTrayHoleNum;
                            //剩余R2体积
                            int leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));
                            string leftR2Vol = (RegentNoUsePro + leftR2 * int.Parse(testTempS.AddLiqud) + leftR2 * abanR2Pro).ToString("x4");//2018-10-13 zlx mod
                            AddErrorCount = 0;
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 02 04 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2)), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                            #region 异常处理
                            Again:
                                string againSend = "";
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                                {
                                    AddErrorCount++;
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 04 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " 00 " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 02 04 " + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                + " " + int.Parse(testTempS.AddLiqud).ToString("x2") + " " + leftR2Vol.Substring(0, 2) + " " + leftR2Vol.Substring(2, 2);
                                }
                                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddROver"), getString("keywordText.Samplingneedle"), 1);
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                                    //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //addLiquiding = false;
                                    AllStop();
                                }
                                int sendFlag = SendAgain(againSend, 0);
                                if (sendFlag == (int)ErrorState.Sendfailure)
                                    goto Again;
                                else if (sendFlag == (int)ErrorState.IsKnocked)
                                    AddErrorCount++;
                                #endregion
                            }
                            for (int i = 0; i < drRg.Length; i++)
                            {
                                if (int.Parse(drRg[i]["Postion"].ToString()) == rgPos)
                                    drRg[i]["leftoverTestR2"] = leftR2 - 1;
                            }
                            leftR2 = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", "", iniPathReagentTrayInfo));
                            OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent2", (leftR2 - 1).ToString(), iniPathReagentTrayInfo);
                            if (AddErrorCount > 0)
                            {
                                if (AddErrorCount > 1)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.AddRIsKnockedS"), getString("keywordText.Samplingneedle"), 1);
                                    AllStop();
                                }
                                else
                                {
                                    MoveTubeListAddTubeDispose(pos);
                                    RemoveTestList(testTempS, getString("keywordText.AddRIsKnocked"));
                                }
                                break;
                            }

                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 04 01 " + pos.ToString("x2")), 0);
                            if (!NetCom3.Instance.SPQuery())
                            {
                                #region 异常处理
                                string againSend = "";
                            Again:
                                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    //重新发送指令
                                    if (againSend == "")
                                        againSend = "EB 90 31 04 01 " + pos.ToString("x2");
                                    if (SendAgain(againSend, 0) == (int)ErrorState.Sendfailure)
                                        goto Again;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MixOver") + "," + getString("keywordText.Pos") + ":" + pos, getString("keywordText.Mix"), 1);
                                    //MessageBox.Show("混匀异常！", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    AllStop();
                                    break;
                                }
                                #endregion
                            }

                        }
                        #endregion
                        lisProBar[testTempS.TestID - 1].BarColor[StepIndex(dgvWorkListData.Rows[testTempS.TestID - 1].Cells[6].Value.ToString(), "R2")]
                            = Color.Gray;
                    }
                    lisProBar[testTempS.TestID - 1].Invalidate();
                    DalayFlag = false;
                    stepTime = 0;
                    break;
            }
            drRg = null;
            addLiquiding = false;
            //}
        }
        /// <summary>
        /// 加在稀释液位置试剂
        /// </summary>
        /// <param name="rgPos">试剂位置</param>
        /// <param name="pos">加样位置</param>
        /// <param name="FirstDiu">加样体积</param>
        /// <returns></returns>
        private int AddReagentD(int rgPos, int pos, int FirstDiu)
        {
            int AddErrorCount = 0;
            int LeftdiuVol = (int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", "", iniPathReagentTrayInfo)) + 2) * (FirstDiu + 10);
            string strLeftdiuVol = LeftdiuVol.ToString("x2");
            int len = strLeftdiuVol.Trim().Length;
            if (len < 4)
            {
                for (int i = len; i < 4; i++)
                {
                    strLeftdiuVol = "0" + strLeftdiuVol;
                }
            }
            LogFile.Instance.Write(DateTime.Now + ":strLeftdiuVol的值为" + strLeftdiuVol);
            string Order = "EB 90 31 02 04 ";
            NetCom3.Instance.Send(NetCom3.Cover(Order + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                                          + " " + FirstDiu.ToString("x2") + " " + strLeftdiuVol.Substring(0, 2) + " " + strLeftdiuVol.Substring(2, 2)), 0);
            //指令未执行完成进行等待
            if (!NetCom3.Instance.SPQuery())
            {
            #region 异常处理
            Again:
                string againSend = "";
                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                {
                    AddErrorCount++;
                    //重新发送指令
                    if (againSend == "")
                        againSend = Order + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                        + " 00 " + strLeftdiuVol.Substring(0, 2) + " " + strLeftdiuVol.Substring(2, 2);
                }
                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                {
                    //重新发送指令
                    if (againSend == "")
                        againSend = Order + (rgPos + 1).ToString("x2") + " " + pos.ToString("x2")
                        + " " + FirstDiu.ToString("x2") + " " + strLeftdiuVol.Substring(0, 2) + " " + strLeftdiuVol.Substring(2, 2);
                }
                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                }
                int sendFlag = SendAgain(againSend, 0);
                if (sendFlag == (int)ErrorState.Sendfailure)
                    goto Again;
                else if (sendFlag == (int)ErrorState.IsKnocked)
                    AddErrorCount++;
                #endregion
            }
            return AddErrorCount;
        }

        /// <summary>
        /// 加稀释液方法
        /// </summary>
        /// <param name="rgPos">试剂位置</param>
        /// <param name="pos">加样位置</param>
        /// <param name="leftdiuVol">加样体积</param>
        private int AddLiquid(int rgPos, int pos, int FirstDiu)
        {
            int AddErrorCount = 0;
            int LeftdiuVol = int.Parse(OperateIniFile.ReadIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", "", iniPathReagentTrayInfo));
            string Order = "EB 90 31 02 06 ";
            string StrLeftdiuVol = LeftdiuVol.ToString("x2");
            if (StrLeftdiuVol.Length < 4)
            {
                int length = LeftdiuVol.ToString("x2").Length;
                for (int i = length; i < 4; i++)
                {
                    StrLeftdiuVol = "0" + StrLeftdiuVol;
                }
            }
            NetCom3.Instance.Send(NetCom3.Cover(Order + rgPos.ToString("x2") + " " + pos.ToString("x2")
                                          + " " + FirstDiu.ToString("x2") + " " + StrLeftdiuVol.Substring(0, 2) + " " + StrLeftdiuVol.Substring(2, 2)), 0);
            //指令未执行完成进行等待

            if (!NetCom3.Instance.SPQuery())
            {
            #region 异常处理
            Again:
                string againSend = "";
                if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.IsKnocked && AddErrorCount < 2)
                {
                    AddErrorCount++;
                    //重新发送指令
                    if (againSend == "")
                        againSend = Order + rgPos.ToString("x2") + " " + pos.ToString("x2")
                        + " 00 " + LeftdiuVol.ToString().Substring(0, 2) + " " + LeftdiuVol.ToString().Substring(2, 2);
                }
                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.Sendfailure)
                {
                    //重新发送指令
                    if (againSend == "")
                        againSend = Order + rgPos.ToString("x2") + " " + pos.ToString("x2")
                        + " " + FirstDiu.ToString("x2") + " " + LeftdiuVol.ToString().Substring(0, 2) + " " + LeftdiuVol.ToString().Substring(2, 2);
                }
                else if (NetCom3.Instance.AdderrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.AddDiuOver"), getString("keywordText.Samplingneedle"), 1);
                    AllStop();
                    //NetCom3.Instance.stopsendFlag = true;
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时发生撞管！");
                    //MessageBox.Show("指令接收超时，实验已终止", "加样错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //addLiquiding = false;
                    //AllStop();
                }
                int sendFlag = SendAgain(againSend, 0);
                if (sendFlag == (int)ErrorState.Sendfailure)
                    goto Again;
                else if (sendFlag == (int)ErrorState.IsKnocked)
                    AddErrorCount++;
                #endregion
            }
            #region 体积修改 lyn add 20180611
            DataRow[] drRg = dtRgInfo.Select("Postion=" + rgPos + "");
            OperateIniFile.WriteIniData("ReagentPos" + rgPos.ToString(), "LeftReagent1", (LeftdiuVol - (int)(FirstDiu + abanDiuPro)).ToString(), iniPathReagentTrayInfo);
            #endregion
            return AddErrorCount;
        }
        /// <summary>
        /// 异常消息显示
        /// </summary>
        /// <param name="threadid">动作执行者</param>
        /// <param name="state">异常状态</param>
        /// <param name="moving">执行动作</param>
        /// <param name="stopLog">实验是否停止</param>
        private void ShowWarnInfo(string message, string state, int stopLog)
        {
            setmainformbutten();
            string stopMessage = "";
            if (stopLog == 1) stopMessage = "," + getString("keywordText.TestStoped");
            LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Error") + " *** " + getString("keywordText.Notread") + " *** " + message);
            MessageBox.Show(message + stopMessage, state + getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        /// <summary>
        /// 将本步骤之前的步骤对应的进度条变灰
        /// </summary>
        /// <param name="testTempS">实验步骤</param>
        /// <param name="step">本步骤对应字符串</param>
        /// <returns></returns>
        private void toGray(int TestID, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                lisProBar[TestID].BarColor[i] = Color.Gray;
            }
        }

        /// <summary>
        /// 重复发送指令
        /// </summary>
        /// <param name="order">需要发送的指令</param>
        /// <param name="sengType">发送类型</param>
        /// <returns></returns>
        private int SendAgain(string order, int sendType)
        {
            NetCom3.Instance.Send(NetCom3.Cover(order), sendType);
            if (sendType == 0)//加样
            {
                NetCom3.Instance.SPQuery();
                return (int)NetCom3.Instance.AdderrorFlag;
            }
            if (sendType == 1)
            {
                NetCom3.Instance.MoveQuery();//移管
                return (int)NetCom3.Instance.MoverrorFlag;
            }
            if (sendType == 2)
            {
                NetCom3.Instance.WashQuery();//清洗
                return (int)NetCom3.Instance.WasherrorFlag;
            }
            return -1;
        }
        /// <summary>
        /// 获取各个步骤在当前实验的索引
        /// </summary>
        /// <param name="steps">所有步骤</param>
        /// <param name="currentStep">当前步骤</param>
        /// <returns></returns>
        int StepIndex(string steps, string currentStep)
        {
            string[] step = steps.Split('-');
            return step.ToList().IndexOf(currentStep);
        }
        /// <summary>
        /// 暂存盘取管到温育盘
        /// </summary>
        /// <param name="RackPos">暂存盘位置</param>
        /// <param name="ReactPos">温育盘位置</param>
        /// <returns></returns>
        void rackToReact(int ReactPos)
        {
            #region 发送指令及配置文件的实时更改（暂存盘夹新管到反应盘）
            while (MoveTubeUseFlag && !NetCom3.Instance.stopsendFlag)
            {
                Thread.Sleep(30);
            }
            if (NetCom3.Instance.stopsendFlag)
            {
                return;
            }
            int iNeedCool = 0;
            int IsKnockedCool = 0;
            MoveTubeUseFlag = true;
        AgainNewMove:
            //到暂存盘takepos[1]位置取管放到温育盘putpos位置
            //NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 01 " + plate.ToString("x2") + " " + column.ToString("x2")
            //    + " " + hole.ToString("x2") + " " + ReactPos.ToString("x2")), 1);

            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 01 " + ReactPos.ToString("x2")), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                TubeProblemFlag = true;
                //TubeProblemFlag = true;
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 3)
                    {
                        goto AgainNewMove;
                    }
                    else
                    {
                        TubeStop = true;
                        if ((ReactPos != 1 && ReactPos != 2 && ReactPos != 3) && !AddTubeStop.Contains(ReactPos))
                        {
                            OperateIniFile.ReadIniData("ReactTrayInfo", "no" + ReactPos, "", iniPathReactTrayInfo);
                            lock (AddTubeStop)
                            {
                                AddTubeStop.Add(ReactPos);
                            }
                        }
                        dbtnRackStatus();
                        setmainformbutten();
                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Error") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.MAddNewTReactNullS") + "!" + getString("keywordText.TestStopedAddS"));
                        TubeProblemFlag = false; //2019-08-24 ZLX add
                    }

                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.LackTube)
                {
                    TubeStop = true;
                    if ((ReactPos != 1 && ReactPos != 2 && ReactPos != 3) && !AddTubeStop.Contains(ReactPos))
                    {
                        OperateIniFile.ReadIniData("ReactTrayInfo", "no" + ReactPos, "", iniPathReactTrayInfo);
                        lock (AddTubeStop)
                        {
                            AddTubeStop.Add(ReactPos);
                        }
                    }
                    dbtnRackStatus();
                    setmainformbutten();
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.LackTube") + "!" + getString("keywordText.TestStopedAddS"));
                    TubeProblemFlag = false;//2019-08-24 ZLX add
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                {
                    TubeStop = true;
                    if ((ReactPos != 1 && ReactPos != 2 && ReactPos != 3) && !AddTubeStop.Contains(ReactPos))
                    {
                        OperateIniFile.ReadIniData("ReactTrayInfo", "no" + ReactPos, "", iniPathReactTrayInfo);
                        lock (AddTubeStop)
                        {
                            AddTubeStop.Add(ReactPos);
                        }
                    }
                    dbtnRackStatus();
                    setmainformbutten();
                    string ss = getString("keywordText.TemporaryDiskStuckTube");
                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TemporaryDiskStuckTube") + "!" + getString("keywordText.TestStopedAddS"));
                    TubeProblemFlag = false;//2019-08-24 ZLX add
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
                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MAddNewTReactIsKnocked"), getString("keywordText.Move"), 1);
                        AllStop();
                        //setmainformbutten();
                        //NetCom3.Instance.stopsendFlag = true;
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手向温育盘抓新管时取管撞管！");
                        //DialogResult tempresult = MessageBox.Show("移管手向温育盘抓新管时在取管位置发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        //AllStop();
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                {
                    IsKnockedCool++;
                GAgainNewMove:
                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + ReactPos.ToString("x2")), 1);
                    if (!NetCom3.Instance.MoveQuery())
                    {
                        #region 异常处理
                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                        {
                            IsKnockedCool++;
                            if (IsKnockedCool < 2)
                            {
                                goto GAgainNewMove;
                            }
                            else
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossIsKnocked"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //NetCom3.Instance.stopsendFlag = true;
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                //DialogResult tempresult = MessageBox.Show("移管手扔废管时发生撞管！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                //AllStop();
                            }
                        }
                        #endregion
                    }
                    if (IsKnockedCool < 2)
                    {
                        goto AgainNewMove;
                    }
                    else
                    {
                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MAddNewTReactIsKnocked"), getString("keywordText.Move"), 1);
                        AllStop();
                        //setmainformbutten();
                        //NetCom3.Instance.stopsendFlag = true;
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手向温育盘抓新管时放管撞管！");
                        //DialogResult tempresult = MessageBox.Show("移管手向温育盘抓新管时放管撞管", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        //AllStop();
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                    NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.MAddNewTReactPutKnocked"), getString("keywordText.Move"), 1);
                    AllStop();
                }
                #endregion
            }
            else
            {
                TubeProblemFlag = false;
                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + ReactPos, "1", iniPathReactTrayInfo);
            }
            MoveTubeUseFlag = false;
            #endregion
            #endregion
        }
        /// <summary>
        /// 温育盘扔废管
        /// </summary>
        /// <param name="takepos"></param>
        void ReactToAband(int takepos)
        {

            MoveTubeUseFlag = true;
            //NetCom3.IncubateTray.WaitOne(15000);//add y 20180524
            int iNeedCool = 0;
            int IsKnockedCool = 0;
        AgainNewMove:
            ///温育盘takepos[1]取管扔废管
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + takepos.ToString("x2")), 1);
            if (!NetCom3.Instance.MoveQuery())
            {
                #region 发生异常处理
                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                {
                    iNeedCool++;
                    if (iNeedCool < 2)
                        goto AgainNewMove;
                    else
                    {
                        //NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MReactLossNulls"), getString("keywordText.Move"), 1);
                        AllStop();
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
                        //NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo("移管手在温育盘扔废管时取管撞管", "移管", 1);
                        AllStop();
                        //setmainformbutten();
                        //NetCom3.Instance.stopsendFlag = true;
                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                        //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时发生撞管", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        //AllStop();
                    }
                }
                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                {
                    //NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                    AllStop();
                    //setmainformbutten();
                    //NetCom3.Instance.stopsendFlag = true;
                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                    //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    //AllStop();
                }
                #endregion
            }
            else
            {
                ///取管成功
                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos.ToString(), "0", iniPathReactTrayInfo);
            }
            MoveTubeUseFlag = false;
        }

        /// <summary>
        /// 运行时间计时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timeReckon_Tick(object sender, EventArgs e)
        {
            while (frmSampleLoad.CaculatingFlag ||
                (TubeProblemFlag && (RunFlag == (int)RunFlagStart.IsRuning)))
            {
                Thread.Sleep(30);
            }
            if (W1EndTime.Count == 0)
            {
                if ((sumTime == stepTime && sumTime != 0))
                    DalayFlag = true;
            }
            else
            {
                if ((sumTime == stepTime && sumTime != 0) || (sumTime != 0 && sumTime == W1EndTime[0]))
                {
                    DalayFlag = true;
                }
            }
            sumTime++;
        }
        #region 清洗相关方法
        /// <summary>
        /// 清洗盘中是否存在即将夹出的反应管
        /// </summary>
        /// <returns></returns>
        bool ClamptubeWash()
        {
            //2018-08-25 zlx mod
            if (lisMoveTube.Count > 0 && lisMoveTube.Exists(ty => ty.TakeTubePos != null && ty.TakeTubePos.Split('-')[0] == "2"))
                return true;
            else
                return false;
        }
        private void timerWash_Tick(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = Language.AppCultureInfo;
            System.Threading.Thread.CurrentThread.CurrentUICulture = Language.AppCultureInfo;
            if (RunFlag != (int)RunFlagStart.IsRuning || NetCom3.Instance.stopsendFlag)//2018-5-22 zlx add
            {
                return;
            }
            DalayFlag = false;//20180525 y zhushidiao
            if (WashTrayUseFlag)
            {
                DalayFlag = true;
            }
            timer.Enabled = false;//20180525 y add
            while (WashTrayUseFlag || WashTurnFlag || NoTateFlag || NetCom3.Instance.stopsendFlag || ClamptubeWash())
            {
                Thread.Sleep(100);
            }
            WashTrayUseFlag = true;//20180526 y move
            WashTurnFlag = true;
        #region 清洗盘旋转一位
        ///清洗盘逆时针旋转一位(发送指令)
        AgainSend:
            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 01 01"), 2);
            timer.Enabled = true;//20180525 y add
            if (!NetCom3.Instance.WashQuery())//2018-10-13 zlx mod
            {
                if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                {
                    LogFile.Instance.Write(DateTime.Now + "清洗盘旋转指令重新发送！");
                    goto AgainSend;
                }
                else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                {

                    NetCom3.Instance.stopsendFlag = true;
                    ShowWarnInfo(getString("keywordText.WashTurnOver"), getString("keywordText.Wash"), 1);
                    AllStop();
                    //NetCom3.Instance.stopsendFlag = true;
                    //ShowWarnInfo("清洗盘旋转指令接收超时", "清洗", 1);
                    ////MessageBox.Show("指令接收超时，实验已终止", "清洗指令错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ////addLiquiding = false;
                    //AllStop();
                }
            }
            countWashHole(1);
            currentHoleNum++;
            //如果孔号超过30，孔号设为1
            if (currentHoleNum == 31)
            {
                currentHoleNum = 1;
            }
            //LogFile.Instance.Write("==================  当前位置  " + currentHoleNum ); 
            OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
            //旋转一位当前取放管位置的孔号加1
            DataTable dtTemp = new DataTable();
            dtTemp = dtWashTrayTubeStatus.Copy();
            //清洗盘状态列表中添加反应盘位置字段，赋值需多赋值一个字段。 LYN add 20171114
            for (int j = 1; j < 5; j++)
                dtWashTrayTubeStatus.Rows[0][j] = dtTemp.Rows[dtWashTrayTubeStatus.Rows.Count - 1][j];
            for (int i = 1; i < dtWashTrayTubeStatus.Rows.Count; i++)
            {
                for (int j = 1; j < 5; j++)
                {
                    dtWashTrayTubeStatus.Rows[i][j] = dtTemp.Rows[i - 1][j];
                }
            }
            OperateIniFile.WriteConfigToFile("[TubePosition]", iniPathWashTrayInfo, dtWashTrayTubeStatus);
            WashTurnFlag = false;
            washStep();
            WashTrayUseFlag = false;
            #endregion
        }
        private void setmainformbutten()//更改主界面按钮颜色
        {
            if (this.Parent.Parent == null) return;

            if (this.Parent.Parent is frmMain)
            {
                ((frmMain)this.Parent.Parent).LogBtnColorChange(1);
            }
        }
        private Object locker2 = new object();//锁，用于移管手列表，lisMovetube的修改操作
        MoveTubeStatus[] MoveTubeError = new MoveTubeStatus[15];//当移管手指令出错时，暂时存储对应的移管手MoveTubeStatus，用于计算同一个指令出错的次数
        int ipostemp = 0;//数组MoveTubeError的当前可以存储的项的下标，相当于指针
        bool CatchVacancyThisTime = false;//是否抓空，根据此来决定如何修改清洗盘状态表
        ManualResetEvent MoveTubeManualReset = new ManualResetEvent(true);//当移管手出错时，阻止lisMoveTube列表继续被提取并被发送相关的移管手指令
        public static bool IsDoubleTime = false;//加液针是否是第二次出错，根据此来决定处理加液针出错的行为
        public static ManualResetEvent AddResetEvent = new ManualResetEvent(true);//当加液针出错时，阻止相关加液指令被发送
        public static bool TrayRemoveAllTube = false;//y add 抓空标志位，确定是否触发抓空异常
        MoveTubeStatus tempMoveTubeStatus;
        void SetItemInMoveTubeError(MoveTubeStatus moveTube)//将一个MoveTubeStatus存入MoveTubeError
        {
            MoveTubeError[ipostemp] = moveTube;
            ipostemp++;
            if (ipostemp == 15) ipostemp = 0;
        }
        private void MoveTubeListAddTubeDispose(int pos)//add y 20180727  丢掉不用的管,加液针撞针需要的处理
        {
            MoveTubeStatus moveTubeStatus = new MoveTubeStatus();
            moveTubeStatus.TakeTubePos = "1-" + pos;
            moveTubeStatus.putTubePos = "0";
            lock (locker2)
            {
                lisMoveTube.Add(moveTubeStatus);
            }
        }
        private void RemoveTestList(TestSchedule testSchedule, string endReason)//add y 20180727  整理testschould列表，将进度中撞针的实验设置为跳过，以实现可以继续剩下的实验
        {
            LogFile.Instance.Write("调用了RemoveTestList(TestSchedule testSchedule)方法！");
            string TestnameTemp = testSchedule.TestID.ToString();
            Thread thread;//2019-2-21 zlx mod
            if (TubeStop)
            {
                thread = new Thread(new ThreadStart(() => { MessageBox.Show(getString("keywordText.TestId") + TestnameTemp + getString("keywordText.abandonedtTest") + getString("keywordText.abandonedtReason") + getString("keywordText.LackTube"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Information); }));
                //thread = new Thread(new ThreadStart(() => { MessageBox.Show("编号为:" + TestnameTemp + " 的实验因为稀释缺管，无法继续，已经排除，请在稍后重新运行此实验", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
            }
            else
            {
                //thread = new Thread(new ThreadStart(() => { MessageBox.Show("编号为:" + TestnameTemp + " 的实验因为发生故障，无法继续，已经排除，请在稍后重新运行此实验", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
                thread = new Thread(new ThreadStart(() => { MessageBox.Show(getString("keywordText.TestId") + TestnameTemp + getString("keywordText.abandonedtTest"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Information); }));
            }
            LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Warning") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.TestId") + TestnameTemp + getString("keywordText.abandonedtTest") + getString("keywordText.abandonedtReason") + endReason + ";" + getString("keywordText.Pos") + testSchedule.AddSamplePos);
            if (thread != null)
            {
                thread.IsBackground = true;
                thread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
                thread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
                thread.Start();
            }
            foreach (DataGridViewRow dr in dgvWorkListData.Rows)
            {
                if (int.Parse(dr.Cells[1].Value.ToString()) == testSchedule.TestID)
                {
                    dr.Cells["TestStatus"].Value = getString("keywordText.TestStatusAbondoned");
                    //BeginInvoke(TestStatusInfo, new object[] { "实验废弃！", dr.Cells[1].Value.ToString() });
                    dr.DefaultCellStyle.BackColor = Color.Gray;
                    SampleNumCurrent = SampleNumCurrent - 1;
                    if ((StopList.Count > 0) &&
                              StopList.Contains(dr.Cells["No"].Value.ToString()))
                    {
                        StopList.Remove(dr.Cells["No"].Value.ToString());
                    }
                    break;
                }
            }
            List<TestSchedule> Temp = lisTestSchedule.FindAll(ty => ty.TestID == testSchedule.TestID);
            foreach (var item in Temp)
            {
                item.TestScheduleStep = TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis;
            }
            //SampleNumCurrent = SampleNumCurrent - 1;
            LoadingHelper.CloseForm();
        }
        private void RemoveTestList(int pos, int posTwo, bool isDuilte)//add y 20180727  整理testschould列表，将进度中撞针的实验设置为跳过，以实现可以继续剩下的实验
        {
            LogFile.Instance.Write("调用了RemoveTestList(int pos, int posTwo, bool isDuilte)方法！");
            LoadingHelper.ShowLoadingScreen();
            TestSchedule testSchedule = null;
            //int pointer = 0;
            //foreach (var item in lisTestSchedule)
            //{
            //    if (item == _GaDoingOne) break;
            //    pointer++;
            //}
            //if ((pointer - 1) <= lisTestSchedule.Count - 1)
            //{
            //    TestSchedule thisone = null;
            //    thisone = lisTestSchedule[pointer - 1];
            //    _GaDoingOne = thisone;
            //}
            //for (int i = pointer; i >= 0; i--)
            //{
            //    if (isDuilte && lisTestSchedule[i].AddSamplePos == posTwo || lisTestSchedule[i].AddSamplePos == pos || isDuilte && int.Parse(lisTestSchedule[i].dilutionPos) == posTwo)
            //    {
            //        testSchedule = lisTestSchedule[i];
            //        break;
            //    }
            //}

            //if (testSchedule == null) MessageBox.Show("未能找到该实验项目");

            string TestnameTemp = testSchedule.TestID.ToString();
            Thread thread = new Thread(new ThreadStart(() => { MessageBox.Show(getString("keywordText.TestId") + TestnameTemp + getString("keywordText.abandonedtTest") + getString("keywordText.abandonedtReason") + getString("keywordText.Samplingprobe"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Information); }));
            //2018-10-16 zlx mod
            foreach (DataGridViewRow dr in dgvWorkListData.Rows)
            {
                if (int.Parse(dr.Cells[1].Value.ToString()) == testSchedule.TestID)
                {
                    dr.Cells["TestStatus"].Value = getString("keywordText.TestStatusAbondoned");
                    //BeginInvoke(TestStatusInfo, new object[] { "实验废弃！", dr.Cells[1].Value.ToString() });
                    dr.DefaultCellStyle.BackColor = Color.Gray;
                    SampleNumCurrent = SampleNumCurrent - 1;
                    if ((StopList.Count > 0) &&
                              StopList.Contains(dr.Cells["No"].Value.ToString()))
                    {
                        StopList.Remove(dr.Cells["No"].Value.ToString());
                    }
                    break;
                }
            }
            thread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
            thread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
            thread.IsBackground = true;
            thread.Start();

            List<TestSchedule> Temp = lisTestSchedule.FindAll(ty => ty.TestID == testSchedule.TestID);
            foreach (var item in Temp)
            {
                item.TestScheduleStep = TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis;
            }
            SampleNumCurrent = SampleNumCurrent - 1;
            LoadingHelper.CloseForm();

        }
        private void DisposeMoveAndAddError(string errorname, int errorState)
        {

        }

        /// <summary>
        /// 清洗盘取放管方法
        /// </summary>
        /// <param name="TubeWashInfo">无用</param>
        void MoveTube(object TubeInfo)
        {

            while (true)
            {
            again:
                Thread.Sleep(30);
                if (lisMoveTube.Count > 0)
                {
                    MoveTubeManualReset.WaitOne();//y add 20170724
                    MoveTubeStatus TempMoveStatus = lisMoveTube[0];
                    tempMoveTubeStatus = TempMoveStatus;//y add 20180814
                    if (TempMoveStatus == null)
                    {
                        goto again;
                    }
                    string[] takepos = TempMoveStatus.TakeTubePos.Split('-');
                    string[] putpos = TempMoveStatus.putTubePos.Split('-');
                    TestSchedule ts = lisTestSchedule.Find(ty => ty.TestID == TempMoveStatus.TestId && ty.stepNum == TempMoveStatus.StepNum);
                    string step = "";
                    if (ts != null)
                        step = ts.TestScheduleStep.ToString();

                    if (takepos[0] == "0" && MoveTubeUseFlag == false)//暂存盘取管
                    {
                        if (putpos[0] == "1")
                        {
                            #region 发送指令及配置文件的实时更改（暂存盘夹新管到反应盘）
                            if (TubeStop)
                            {
                                if ((int.Parse(putpos[1]) != 1 || int.Parse(putpos[1]) != 2 || int.Parse(putpos[1]) != 3) && !AddTubeStop.Contains(int.Parse(putpos[1])))
                                    lock (AddTubeStop)
                                    {
                                        AddTubeStop.Add(int.Parse(putpos[1]));
                                    }
                                lisMoveTube.Remove(TempMoveStatus);
                                continue;
                            }
                            MoveTubeUseFlag = true;
                            int iNeedCool = 0;
                            int IsKnockedCool = 0;
                        #region 屏蔽
                        AgainNewMove:
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 01 " + int.Parse(putpos[1]).ToString("x2")), 1);
                            if (!NetCom3.Instance.MoveQuery())
                            {
                                TubeProblemFlag = true;
                                #region 发生异常处理
                                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    iNeedCool++;
                                    if (iNeedCool < 3)
                                        goto AgainNewMove;
                                    else
                                    {
                                        TubeStop = true;
                                        if ((int.Parse(putpos[1]) != 1 || int.Parse(putpos[1]) != 2 || int.Parse(putpos[1]) != 3) && !AddTubeStop.Contains(int.Parse(putpos[1])))
                                        {
                                            lock (AddTubeStop)
                                            {
                                                AddTubeStop.Add(int.Parse(putpos[1]));
                                            }
                                        }
                                        dbtnRackStatus();
                                        setmainformbutten();
                                        LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手向温育盘抓管时多次抓空!实验暂停加样！");
                                        TubeProblemFlag = false;//2019-08-24 ZLX add
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.LackTube)
                                {
                                    TubeStop = true;
                                    if ((int.Parse(putpos[1]) != 1 || int.Parse(putpos[1]) != 2 || int.Parse(putpos[1]) != 3) && !AddTubeStop.Contains(int.Parse(putpos[1])))
                                    {
                                        lock (AddTubeStop)
                                        {
                                            AddTubeStop.Add(int.Parse(putpos[1]));
                                        }
                                    }
                                    dbtnRackStatus();
                                    setmainformbutten();
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + getString("keywordText.Error") + " *** " + getString("keywordText.Notread") + " *** " + getString("keywordText.LackTube") + "!" + getString("keywordText.TestStopedAddS"));
                                    TubeProblemFlag = false;
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.StuckTube)
                                {
                                    TubeStop = true;
                                    if ((int.Parse(putpos[1]) != 1 || int.Parse(putpos[1]) != 2 || int.Parse(putpos[1]) != 3) && !AddTubeStop.Contains(int.Parse(putpos[1])))
                                    {
                                        lock (AddTubeStop)
                                        {
                                            AddTubeStop.Add(int.Parse(putpos[1]));
                                        }
                                    }
                                    dbtnRackStatus();
                                    setmainformbutten();
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "理杯机缺管!实验暂停加样！");
                                    TubeProblemFlag = false;
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
                                        goto AgainNewMove;
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo("移管手在暂存盘向温育盘移管时取管撞管", "移管", 1);
                                        AllStop();
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                                {
                                GAgainNewMove:
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(putpos[1]).ToString("x2")), 1);
                                    if (!NetCom3.Instance.MoveQuery())
                                    {
                                        #region 异常处理
                                        if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                                        {
                                            IsKnockedCool++;
                                            if (IsKnockedCool < 2)
                                            {
                                                goto GAgainNewMove;
                                            }
                                            else
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo("移管手在温育盘扔废管时取管发生撞管", "移管", 1);
                                                AllStop();
                                            }
                                        }
                                        #endregion
                                    }
                                    IsKnockedCool++;
                                    if (IsKnockedCool < 2)
                                    {
                                        goto AgainNewMove;
                                    }
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo("移管手向温育盘抓新管时发生撞管，实验将进行停止", "移管", 1);
                                        AllStop();
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo("移管手在暂存盘向温育盘抓管时接收数据超时", "移管", 1);
                                    AllStop();
                                }
                                #endregion
                            }
                            else
                            {
                                TubeProblemFlag = false;
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + putpos[1], "1", iniPathReactTrayInfo);
                            }
                            #endregion
                            #endregion
                            lock (locker2)
                            {
                                lisMoveTube.Remove(TempMoveStatus);
                            }
                            MoveTubeUseFlag = false;
                        }

                    }
                    //清洗盘夹管
                    else if (takepos[0] == "2" && WashTrayUseFlag == false)
                    {
                        if (putpos[0] == "1" && MoveTubeUseFlag == false)
                        {
                            #region 指令发送及相关文件修改（清洗盘夹管到反应盘）
                            MoveTubeUseFlag = true;
                            WashTrayUseFlag = true;
                            int iNeedCool = 0;
                            int IsKnockedCool = 0;
                        AgainNewMove:
                            //NetCom3.IncubateTray.WaitOne(15000);//add y 20180524
                            //LogFile.Instance.Write(string.Format("{0}<-:{1}", DateTime.Now.ToString("HH:mm:ss"), "清洗盘夹管到温育盘 movetube  WashTrayUseFlag = true"));
                            ///清洗盘取管放到温育盘putpos[1]位置
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 03 " + int.Parse(putpos[1]).ToString("x2") + " 01"), 1);
                            if (!NetCom3.Instance.MoveQuery())
                            {
                                #region 发生异常处理
                                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    iNeedCool++;
                                    if (iNeedCool < 2)
                                    {
                                    AgainReSetSend:
                                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                                        if (!NetCom3.Instance.MoveQuery())
                                        {
                                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                                            {
                                                LogFile.Instance.Write(DateTime.Now + "清洗盘复位返回指令重新发送！");
                                                goto AgainReSetSend;
                                            }
                                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.WashResetOver"), getString("keywordText.Wash"), 1);
                                                AllStop();
                                            }
                                        }
                                        goto AgainNewMove;
                                    }
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.MWashToReactNullS"), getString("keywordText.Move"), 1);
                                        AllStop();
                                        //setmainformbutten();
                                        //NetCom3.Instance.stopsendFlag = true;
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时多次抓空！");
                                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘夹管到温育盘时多次抓空，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                        //AllStop();
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
                                        goto AgainNewMove;
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.MWashToReactIsKnocked"), getString("keywordText.Move"), 1);
                                        AllStop();
                                        //setmainformbutten();
                                        //NetCom3.Instance.stopsendFlag = true;
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时取管撞管！");
                                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘夹管到温育盘时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                        //AllStop();
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                                {

                                    int CIsKnockedCool = 0;
                                    #region 清洗盘向温育盘放管时发生撞管
                                    OperateIniFile.WriteIniData("TubePosition", "no" + takepos[1], "0", iniPathWashTrayInfo);
                                    LogFile.Instance.Write("==============清洗盘夹管  " + washCountNum);
                                    if (dtWashTrayTubeStatus.Rows.Count > 0)
                                    {
                                        dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][1] = "0";
                                        dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2] = 0;
                                        dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][3] = 0;
                                        dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][4] = 0;
                                    }

                                #endregion

                                ClearMove:
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(putpos[1]).ToString("x2")), 1);
                                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                                    {
                                        CIsKnockedCool++;
                                        if (CIsKnockedCool < 2)
                                            goto ClearMove;
                                        else
                                        {
                                            NetCom3.Instance.stopsendFlag = true;
                                            ShowWarnInfo(getString("keywordText.MReactLossIsKnocked"), getString("keywordText.Move"), 1);
                                            AllStop();
                                            //setmainformbutten();
                                            //NetCom3.Instance.stopsendFlag = true;
                                            //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！撞管位置为：" + tubeHoleNum);
                                            ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "移管手在温育盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                                            //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                            //AllStop();
                                        }
                                    }
                                    else
                                    {
                                        List<TestSchedule> TestSchedule = lisTestSchedule.FindAll(ty => ty.TestID == TempMoveStatus.TestId);
                                        if (TestSchedule.Count > 0)
                                            RemoveTestList(TestSchedule[0], getString("keywordText.MWashToReactPutKnocked"));
                                        TempMoveStatus.isRetransmit = false;
                                        lock (locker2)
                                        {
                                            ///取放管失败
                                            lisMoveTube.Remove(TempMoveStatus);
                                        }
                                        OperateIniFile.WriteIniData("ReactTrayInfo", "no" + putpos[1], "0", iniPathReactTrayInfo);
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MWashToReactOver"), getString("keywordText.Move"), 1);
                                    AllStop();

                                    //setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘夹管到温育盘时接收数据超时！");
                                    //DialogResult tempresult = MessageBox.Show("移管手在清洗盘夹管到温育盘时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                }
                                #endregion
                            }
                            else
                            {
                                #region 取放管成功
                                OperateIniFile.WriteIniData("TubePosition", "no" + takepos[1], "0", iniPathWashTrayInfo);
                                LogFile.Instance.Write("==============清洗盘夹管  " + washCountNum);
                                //LogFile.Instance.Write("==============夹管到温育盘  " + takepos[1] + "  扔管");
                                //if (!CatchVacancyThisTime)
                                //{
                                if (dtWashTrayTubeStatus.Rows.Count > 0)
                                {
                                    dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][1] = "0";
                                    dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2] = 0;
                                    dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][3] = 0;
                                    dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][4] = 0;
                                }
                                //}
                                //CatchVacancyThisTime = false;
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + putpos[1], "2", iniPathReactTrayInfo);
                                #endregion
                            }
                            //WashTurnFlag = false;//2018-10-24 zlx mod
                            ///取放管失败
                            if (!TempMoveStatus.isRetransmit)
                            {
                                W1EndTime.RemoveAt(0);
                            }
                            DalayFlag = false;
                            #endregion
                            lock (locker2)
                            {
                                lisMoveTube.Remove(TempMoveStatus);
                            }
                            WashTrayUseFlag = false;
                            MoveTubeUseFlag = false;
                        }
                        //到废弃处
                        else if (putpos[0] == "0" && MoveTubeUseFlag == false && WashTrayUseFlag == false)
                        {
                            #region 指令发送及相关配置文件更新（清洗盘扔废管）
                            MoveTubeUseFlag = true;
                            WashTrayUseFlag = true;
                            //WashTurnFlag = true;
                            int iNeedCool = 0;
                            int IsKnockedCool = 0;
                            tubeHoleNum = washCountNum - 2;
                            if (tubeHoleNum <= 0)
                                tubeHoleNum = tubeHoleNum + WashTrayNum;
                            LogFile.Instance.Write("  ***  " + "当前时间" + DateTime.Now + "清洗盘takepos[1]取管扔废管取放管位置" + tubeHoleNum + ",washCountNum的值:" + washCountNum + "  ***  ");
                        AgainNewMove:
                            //LogFile.Instance.Write(string.Format("{0}<-:{1}", DateTime.Now.ToString("HH:mm:ss"), "到废弃处 movetube  WashTrayUseFlag = true"));
                            ///清洗盘takepos[1]取管扔废管
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 01"), 1);
                            if (!NetCom3.Instance.MoveQuery())// && !NetCom3.Instance.MoveQuery()
                            {
                                #region 发生异常处理
                                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                                {
                                    iNeedCool++;
                                    if (iNeedCool < 2)
                                    {
                                    AgainReSetSend:
                                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                                        if (!NetCom3.Instance.MoveQuery())
                                        {
                                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                                            {
                                                LogFile.Instance.Write(DateTime.Now + "清洗盘复位返回指令重新发送！");
                                                goto AgainReSetSend;
                                            }
                                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                            {
                                                NetCom3.Instance.stopsendFlag = true;
                                                ShowWarnInfo(getString("keywordText.WashResetOver"), getString("keywordText.Wash"), 1);
                                                AllStop();
                                            }
                                        }
                                        goto AgainNewMove;
                                    }
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.MWashLossNullS"), getString("keywordText.Move"), 1);
                                        AllStop();
                                        //setmainformbutten();
                                        //NetCom3.Instance.stopsendFlag = true;
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时多次抓空！抓空位置为：" + tubeHoleNum);
                                        ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "清洗盘扔废管时多次抓空孔位置" + tubeHoleNum + " *** ");
                                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时多次抓空，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                        //AllStop();
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
                                        goto AgainNewMove;
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.MWashLossIsKnocked"), getString("keywordText.Move"), 1);
                                        AllStop();
                                        //setmainformbutten();
                                        //NetCom3.Instance.stopsendFlag = true;
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时取管撞管！撞管位置为：" + tubeHoleNum);
                                        ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "移管手在清洗盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                        //AllStop();
                                    }
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MWashLossOver"), getString("keywordText.Move"), 1);
                                    AllStop();

                                    //setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时接收数据超时！");
                                    //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                }
                                #endregion
                            }
                            else
                            {
                                #region 取管成功
                                OperateIniFile.WriteIniData("TubePosition", "no" + takepos[1], "0", iniPathWashTrayInfo);
                                LogFile.Instance.Write("==============  " + washCountNum + "  扔管");
                                //LogFile.Instance.Write("==============  " + currentHoleNum + "  扔管");
                                //LogFile.Instance.Write("==============  " + takepos[1] + "  扔管");
                                //if (!CatchVacancyThisTime)
                                //{
                                if (dtWashTrayTubeStatus.Rows.Count > 0)
                                {
                                    dtWashTrayTubeStatus.Rows[28][1] = "0";
                                    dtWashTrayTubeStatus.Rows[28][2] = 0;
                                    dtWashTrayTubeStatus.Rows[28][3] = 0;
                                    dtWashTrayTubeStatus.Rows[28][4] = 0;
                                }
                                //}
                                //CatchVacancyThisTime = false;
                                #endregion
                                lock (locker2)
                                {
                                    lisMoveTube.Remove(TempMoveStatus);
                                }
                            }
                            #endregion
                            //WashTurnFlag = false;//2018-11-28 zlx mod
                            MoveTubeUseFlag = false;
                            WashTrayUseFlag = false;
                        }

                    }
                    //反应盘扔废管
                    else if (takepos[0] == "1" && putpos[0] == "0" && MoveTubeUseFlag == false)
                    {
                        #region 指令发送及文件修改（反应盘扔废管）
                        MoveTubeUseFlag = true;
                        int iNeedCool = 0;
                        int IsKnockedCool = 0;
                    AgainNewMove:
                        //NetCom3.IncubateTray.WaitOne(15000);//add y 20180524
                        ///温育盘takepos[1]取管扔废管
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 05 " + int.Parse(takepos[1]).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                            {
                                iNeedCool++;
                                if (iNeedCool < 2)
                                {
                                    goto AgainNewMove;
                                }
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MReactLossNulls"), getString("keywordText.Move"), 1);
                                    AllStop();
                                    //setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时多次抓空！");
                                    //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时多次抓空，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                    break;
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
                                    goto AgainNewMove;
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MReactLossIsKnocked"), getString("keywordText.Move"), 1);
                                    AllStop();
                                    //setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时取管撞管！");
                                    //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                }

                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactLossOver"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //NetCom3.Instance.stopsendFlag = true;
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘扔废管时接收数据超时！");
                                //DialogResult tempresult = MessageBox.Show("移管手在温育盘扔废管时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                //AllStop();
                                break;
                            }
                            #endregion
                        }
                        else
                        {
                            ///取管成功
                            OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos[1], "0", iniPathReactTrayInfo);
                            if (BFullReactTray)
                                BFullReactTray = false;
                        }
                        #endregion
                        lock (locker2)
                        {
                            lisMoveTube.Remove(TempMoveStatus);
                        }
                        MoveTubeUseFlag = false;

                    }
                    //反应盘夹管到清洗盘
                    //else if (takepos[0] == "1" && dtWashTrayTubeStatus.Rows[0][1].ToString() == "0" && putpos[0] == "2"
                    //    && WashTurnFlag == false && MoveTubeUseFlag == false && WashTrayUseFlag==false)///反应盘标志位是否不用判断
                    else if (takepos[0] == "1" && dtWashTrayTubeStatus.Rows[0][1].ToString() == "0" && putpos[0] == "2"
                        && MoveTubeUseFlag == false && WashTurnFlag == false)///反应盘标志位是否不用判断
                    {
                        NoTateFlag = true;
                        MoveTubeUseFlag = true;
                        //WashTrayUseFlag = true;
                        //WashTurnFlag = true;
                        int iNeedCool = 0;
                        int IsKnockedCool = 0;
                    #region 指令发送及文件修改（反应盘夹管到清洗盘）
                    //NetCom3.IncubateTray.WaitOne(15000);//add y 20180524
                    AgainNewMove:
                        ///从反应盘takepos[1]位取管放到清洗盘putpos[1]位置
                        LogFile.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + "取管位置:" + TempMoveStatus.TakeTubePos + ",放管位置:" + TempMoveStatus.putTubePos);
                        NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 02 " + int.Parse(takepos[1]).ToString("x2")), 1);
                        if (!NetCom3.Instance.MoveQuery())
                        {
                            LogFile.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + "EB 90 31 01 02重发，MoverrorFlag为:" + NetCom3.Instance.MoverrorFlag);
                            #region 发生异常处理
                            if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                            {
                                iNeedCool++;
                                if (iNeedCool < 2)
                                {
                                    goto AgainNewMove;
                                }
                                else
                                {
                                    setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘夹管到清洗盘时多次抓空！");
                                    //DialogResult tempresult = MessageBox.Show("移管手在反应盘夹管到清洗盘时多次抓空，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                    #region  当前实验废弃，复位后扔掉 
                                    lisMoveTube.Remove(TempMoveStatus);
                                    OperateIniFile.WriteIniData("TubePosition", "No1", "0", iniPathWashTrayInfo);
                                    dtWashTrayTubeStatus.Rows[0][1] = "0";
                                    dtWashTrayTubeStatus.Rows[0][2] = 0;
                                    dtWashTrayTubeStatus.Rows[0][3] = 0;
                                    dtWashTrayTubeStatus.Rows[0][4] = 0;
                                    NetCom3.Instance.Send(NetCom3.Cover("EB 90 11 01 10 00 00"), 5);
                                    NetCom3.Instance.SingleQuery();
                                    MoveTubeListAddTubeDispose(Convert.ToInt32(int.Parse(takepos[1])));
                                    List<TestSchedule> TestSchedule = lisTestSchedule.FindAll(ty => ty.TestID == TempMoveStatus.TestId);
                                    if (TestSchedule.Count > 0)
                                        RemoveTestList(TestSchedule[0], getString("keywordText.MReactToWashNulls"));
                                    #endregion
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
                                    goto AgainNewMove;
                                else
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.MReactToWashIsKnocked"), getString("keywordText.Move"), 1);
                                    AllStop();
                                    //setmainformbutten();
                                    //NetCom3.Instance.stopsendFlag = true;
                                    //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘夹管到清洗盘时取管撞管！撞管位置为：" + tubeHoleNum);
                                    ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "移管手在反应盘夹管到清洗盘时发生撞管孔位置" + tubeHoleNum + " *** ");
                                    //DialogResult tempresult = MessageBox.Show("移管手在反应盘夹管到清洗盘时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    //AllStop();
                                }
                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.putKnocked)
                            {
                                int CIsKnockedCool = 0;
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos[1], "0", iniPathReactTrayInfo);
                            ClearMove:
                                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 06"), 1);
                                if (!NetCom3.Instance.MoveQuery() && NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsKnocked)
                                {
                                    CIsKnockedCool++;
                                    if (CIsKnockedCool < 2)
                                        goto ClearMove;
                                    else
                                    {
                                        NetCom3.Instance.stopsendFlag = true;
                                        ShowWarnInfo(getString("keywordText.MWashLossIsKnocked"), getString("keywordText.Move"), 1);
                                        AllStop();
                                        //setmainformbutten();
                                        //NetCom3.Instance.stopsendFlag = true;
                                        //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在清洗盘扔废管时取管撞管！撞管位置为：" + tubeHoleNum);
                                        ////LogFileAlarm.Instance.Write(" *** " + "时间" + DateTime.Now.ToString("HH-mm-ss") + "移管手在清洗盘扔废管时发生撞管孔位置" + tubeHoleNum + " *** ");
                                        //DialogResult tempresult = MessageBox.Show("移管手在清洗盘扔废管时发生撞管，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                        //AllStop();
                                    }
                                }
                                else
                                {
                                    List<TestSchedule> TestSchedule = lisTestSchedule.FindAll(ty => ty.TestID == TempMoveStatus.TestId);
                                    if (TestSchedule.Count > 0)
                                        RemoveTestList(TestSchedule[0], getString("keywordText.MReactToWashPutKnocked"));
                                    step = "";
                                    lock (locker2)
                                    {
                                        ///取放管失败
                                        lisMoveTube.Remove(TempMoveStatus);
                                    }
                                }

                            }
                            else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                            {
                                NetCom3.Instance.stopsendFlag = true;
                                ShowWarnInfo(getString("keywordText.MReactToWashOver"), getString("keywordText.Move"), 1);
                                AllStop();
                                //setmainformbutten();
                                //NetCom3.Instance.stopsendFlag = true;
                                //LogFileAlarm.Instance.Write(DateTime.Now.ToString("HH-mm-ss") + " *** " + "错误" + " *** " + "未读" + " *** " + "移管手在温育盘夹管到清洗盘时接收数据超时！");
                                //DialogResult tempresult = MessageBox.Show("移管手在温育盘夹管到清洗盘时接收数据超时，实验将进行停止！", "移管手错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                //AllStop();
                                break;
                            }
                            #endregion
                        }
                        else
                        {
                            lock (locker2)
                            {
                                ///取放管失败
                                lisMoveTube.Remove(TempMoveStatus);
                            }
                            #region 取放管成功
                            //LogFile.Instance.Write("==============  温育向清洗盘放管  " + currentHoleNum);
                            LogFile.Instance.Write("==============  反应盘向清洗盘放管  " + washCountNum);
                            if (step == "Wash1")//2018-06-04 两步法移管留位置 zlx add
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos[1], "9", iniPathReactTrayInfo);
                            else
                            {
                                OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos[1], "0", iniPathReactTrayInfo);
                                if (BFullReactTray)
                                    BFullReactTray = false;
                            }
                            //OperateIniFile.WriteIniData("ReactTrayInfo", "no" + takepos[1], "0", iniPathReactTrayInfo);
                            OperateIniFile.WriteIniData("TubePosition", "No1", "1", iniPathWashTrayInfo);
                            //if (!CatchVacancyThisTime)
                            //{
                            dtWashTrayTubeStatus.Rows[0][1] = "1";
                            dtWashTrayTubeStatus.Rows[0][2] = TempMoveStatus.TestId;
                            dtWashTrayTubeStatus.Rows[0][3] = TempMoveStatus.StepNum;
                            dtWashTrayTubeStatus.Rows[0][4] = takepos[1];
                            //}
                            //CatchVacancyThisTime = false;

                            #endregion

                        }

                        //LogFile.Instance.Write(string.Format("{0}<-:{1}", DateTime.Now.ToString("HH:mm:ss"), "反应盘夹管到清洗盘 movetube  WashTrayUseFlag = false"));

                        if (step == "WashTray")
                        {

                            lisProBar[TempMoveStatus.TestId - 1].BarColor[StepIndex(dgvWorkListData.Rows[TempMoveStatus.TestId - 1].Cells[6].Value.ToString(), "W2") - 1]
                                   = Color.Gray;
                            lisProBar[TempMoveStatus.TestId - 1].Invalidate();

                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Washing"), TempMoveStatus.TestId });

                            lisProBar[TempMoveStatus.TestId - 1].BarColor[StepIndex(dgvWorkListData.Rows[TempMoveStatus.TestId - 1].Cells[6].Value.ToString(), "W2")]
                                = Color.Yellow;
                            lisProBar[TempMoveStatus.TestId - 1].Invalidate();

                        }
                        if (step == "Wash1")
                        {

                            lisProBar[TempMoveStatus.TestId - 1].BarColor[StepIndex(dgvWorkListData.Rows[TempMoveStatus.TestId - 1].Cells[6].Value.ToString(), "W1") - 1]
                                = Color.Gray;
                            lisProBar[TempMoveStatus.TestId - 1].Invalidate();

                            BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Washing"), TempMoveStatus.TestId });

                            lisProBar[TempMoveStatus.TestId - 1].BarColor[StepIndex(dgvWorkListData.Rows[TempMoveStatus.TestId - 1].Cells[6].Value.ToString(), "W1")]
                                = Color.Yellow;
                            lisProBar[TempMoveStatus.TestId - 1].Invalidate();
                        }
                        //WashTrayUseFlag = false;
                        MoveTubeUseFlag = false;
                        //WashTurnFlag = false;
                        #endregion
                        NoTateFlag = false;
                    }
                }
            }
        }

        void washTray(object TubeInfo)
        {
            timer.Interval = 20000 / (1000 / timeInterval);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timerWash_Tick);
            timer.Enabled = true;
        }
        /// <summary>
        /// 清洗盘各个实验步骤
        /// </summary>
        void washStep()
        {
            //是否进行读数
            string read = "0";
            //注液标志位
            List<string> LiquidInjectionFlag = new List<string>();
            //吸液标志位
            List<int> ImbibitionFlag = new List<int>();
            string Imbibition = "00";
            //是否加底物标志位
            string AddSubstrate = "0";
            //底物管路
            string substratePipe = "0";
            #region 读数
            if (dtWashTrayTubeStatus.Rows[24][1].ToString() == "1")
            {
                read = "1";
            }
            #endregion

            #region 清洗和加底物
            //判断第一次吸液位置是否有管
            if (dtWashTrayTubeStatus.Rows[4 + isNewCleanTray][1].ToString() == "1")
            {
                //if (frmMain.StopFlag[3])//2018-07-11 zlx add
                //{
                //    TestSchedule Schedule = lisTestSchedule.Find(ty => ty.TestID == int.Parse(dtWashTrayTubeStatus.Rows[4 + isNewCleanTray][2].ToString()));
                //    if (!StopList.Contains(Schedule.TestID.ToString()))
                //        StopList.Add(Schedule.TestID.ToString());
                //}
                ImbibitionFlag.Add(1);
            }
            else
            {
                ImbibitionFlag.Add(0);
            }
            //吸液步骤完成后是否将管放回反应盘
            bool takeTubeFlag = false;

            //判断第三次吸液位置是否有管
            if (dtWashTrayTubeStatus.Rows[8 + isNewCleanTray][1].ToString() == "1")
            {
                //if (frmMain.StopFlag[3])//2018-07-11 zlx add
                //{
                //    TestSchedule Schedule = lisTestSchedule.Find(ty => ty.TestID == int.Parse(dtWashTrayTubeStatus.Rows[8 + isNewCleanTray][2].ToString()));
                //    if (!StopList.Contains(Schedule.TestID.ToString()))
                //        StopList.Add(Schedule.TestID.ToString());
                //}
                ImbibitionFlag.Add(1);
            }
            else
            {
                ImbibitionFlag.Add(0);
            }

            //判断第二次吸液位置是否有管
            if (dtWashTrayTubeStatus.Rows[12 + isNewCleanTray][1].ToString() == "1")
            {
                //if (frmMain.StopFlag[3])//2018-07-11 zlx add
                //{
                //    TestSchedule Schedule = lisTestSchedule.Find(ty => ty.TestID == int.Parse(dtWashTrayTubeStatus.Rows[12 + isNewCleanTray][2].ToString()));
                //    if (!StopList.Contains(Schedule.TestID.ToString()))
                //        StopList.Add(Schedule.TestID.ToString());
                //}
                ImbibitionFlag.Add(1);
            }
            else
            {
                ImbibitionFlag.Add(0);
            }
            //判断第四次吸液位置是否有管
            if (dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][1].ToString() == "1")
            {
                string CurrentStep = lisTestSchedule.Find(ty => ty.stepNum == int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][3].ToString())
                                                         && ty.TestID == int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()))
                                                         .TestScheduleStep.ToString();
                ImbibitionFlag.Add(1);
                if (CurrentStep == BioBaseCLIA.Run.TestSchedule.ExperimentScheduleStep.Wash1.ToString())
                {
                    takeTubeFlag = true;
                }
            }
            else
            {
                ImbibitionFlag.Add(0);
            }
            //判断第一次注液位置是否有管
            if (dtWashTrayTubeStatus.Rows[5 + isNewCleanTray][1].ToString() == "1")
            {
                LiquidInjectionFlag.Add("1");
            }
            else
            {
                LiquidInjectionFlag.Add("0");
            }
            //判断第二次注液位置是否有管
            if (dtWashTrayTubeStatus.Rows[9 + isNewCleanTray][1].ToString() == "1")
            {
                LiquidInjectionFlag.Add("1");
            }
            else
            {
                LiquidInjectionFlag.Add("0");
            }
            //判断第三次注液位置是否有管
            if (dtWashTrayTubeStatus.Rows[13 + isNewCleanTray][1].ToString() == "1")
            {
                LiquidInjectionFlag.Add("1");
            }
            else
            {
                LiquidInjectionFlag.Add("0");
            }
            //加底物位置是否有管
            if (dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][1].ToString() == "1")
            {
                AddSubstrate = "1";
            }
            //吸液、注液或者加底物的位置下面有反应管
            if (ImbibitionFlag.Contains(1) || LiquidInjectionFlag.Contains("1") || AddSubstrate == "1" || read == "1")
            {
                if (ImbibitionFlag.Contains(1))
                {
                    Imbibition = "01";
                }
                if (AddSubstrate == "1")
                {
                    substratePipe = "1";
                }
                #region 指令发送
                if (read == "1")
                {
                    lock (listestid)
                    {
                        listestid.Enqueue(int.Parse(dtWashTrayTubeStatus.Rows[24][2].ToString()));
                    }
                    ReadThread = new Thread(new ParameterizedThreadStart(Read));
                    ReadThread.IsBackground = true;
                    ReadThread.CurrentCulture = Language.AppCultureInfo;
                    ReadThread.CurrentUICulture = Language.AppCultureInfo;
                    ReadThread.Start();
                }
            AgainSend:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 03 03 " + Imbibition + " " + LiquidInjectionFlag[0] +
                    LiquidInjectionFlag[1] + " " + LiquidInjectionFlag[2] + AddSubstrate + " " + substratePipe + read), 2);
                if (!NetCom3.Instance.WashQuery())
                {
                    if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.Sendfailure)
                        goto AgainSend;
                    else if (NetCom3.Instance.WasherrorFlag == (int)ErrorState.OverTime)
                    {

                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.WashPourOver"), getString("keywordText.Wash"), 1);
                        AllStop();
                    }
                }
                #endregion
            }
            #region 指令完成后界面信息改变
            if (read == "1")
            {
                //ReadThread = new Thread(new ParameterizedThreadStart(Read));
                //ReadThread.IsBackground = true;
                //ReadThread.Start();
                if (dgvWorkListData.RowCount == 0) return;
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[24][2].ToString()) - 1].
                BarColor[StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[24][2].ToString()) - 1].
                Cells[6].Value.ToString(), "R")]
               = Color.Gray;
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[24][2].ToString()) - 1].Invalidate();
                BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Testcomplete"), int.Parse(dtWashTrayTubeStatus.Rows[24][2].ToString()) });
                //completeTestNums++;
            }
            if (ImbibitionFlag.Contains(1))
            {
                if (ImbibitionFlag[3] == 1 && takeTubeFlag == false)
                {
                    if (dgvWorkListData.RowCount == 0) return;
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].BarColor
                            [StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1]
                            .Cells[6].Value.ToString(), "W2")] = Color.Gray;
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].Invalidate();
                    BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.AddingSubstrate"), int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) });
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].
                        BarColor[StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].
                        Cells[6].Value.ToString(), "Su")] = Color.Yellow;
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].Invalidate();

                }
                if (takeTubeFlag)
                {
                    if (dgvWorkListData.RowCount == 0) return;
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].BarColor
                        [StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1]
                        .Cells[6].Value.ToString(), "W1")] = Color.Gray;
                    lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString()) - 1].Invalidate();
                    int pos = (int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][4].ToString())) % ReactTrayHoleNum;
                    MoveTubeStatus moveTube7 = new MoveTubeStatus();
                    if (pos == 0)
                    {
                        moveTube7.putTubePos = "1-" + ReactTrayHoleNum.ToString();
                    }
                    else
                    {
                        moveTube7.putTubePos = "1-" + pos.ToString();
                    }
                    moveTube7.StepNum = int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][3].ToString());
                    moveTube7.TakeTubePos = "2-" + dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][0].ToString().Substring(2);
                    moveTube7.TestId = int.Parse(dtWashTrayTubeStatus.Rows[16 + isNewCleanTray][2].ToString());
                    lock (locker2)
                    {
                        if (lisMoveTube.Count > 0)
                        {
                            lisMoveTube.Insert(0, moveTube7);
                        }
                        else
                        {
                            lisMoveTube.Add(moveTube7);
                        }
                    }
                }
            }

            if (AddSubstrate == "1")
            {
                string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
                OperateIniFile.WriteIniData("Substrate1", "LeftCount", (int.Parse(LeftCount1) - 1).ToString(), iniPathSubstrateTube);
                string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
                string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
                DbHelperOleDb dbase = new DbHelperOleDb(3);
                DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '" + sbCode1 + "'");

                if (dgvWorkListData.RowCount == 0) return;
                dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].Cells["SubstratePipe"].Value = substratePipe;
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].
                    BarColor[StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].
                    Cells[6].Value.ToString(), "Su")] = Color.Gray;
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].Invalidate();
                BeginInvoke(TestStatusInfo, new object[] { getString("keywordText.Reading"), int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) });
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].
                    BarColor[StepIndex(dgvWorkListData.Rows[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].
                    Cells[6].Value.ToString(), "R")]
                    = Color.Yellow;
                lisProBar[int.Parse(dtWashTrayTubeStatus.Rows[18 + isNewCleanTray][2].ToString()) - 1].Invalidate();
                completeTestNums++;
            }
            #endregion
            #region 读数完成将管取出
            if (dtWashTrayTubeStatus.Rows[28][1].ToString() == "1")
            {
                int iNeedCool = 0;
                int IsKnockedCool = 0;
                tubeHoleNum = washCountNum - 2;
                if (tubeHoleNum <= 0)
                    tubeHoleNum = tubeHoleNum + frmParent.WashTrayNum;
                LogFile.Instance.Write("  ***  " + "当前时间" + DateTime.Now + "清洗盘takepos[1]取管扔废管取放管位置" + tubeHoleNum + ",washCountNum的值:" + washCountNum + "  ***  ");
                int time = 0;
                while (MoveTubeUseFlag)
                {
                    Thread.Sleep(50);
                    time = time + 50;
                }
                LogFile.Instance.Write("清洗盘扔管间隔时间为：" + time + ",WashTrayUseFlag的值：" + WashTrayUseFlag);
            AgainNewMove:
                NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 04 01"), 1);
                if (!NetCom3.Instance.MoveQuery())// && !NetCom3.Instance.MoveQuery()
                {
                    #region 发生异常处理
                    if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.IsNull)
                    {
                        iNeedCool++;
                        if (iNeedCool < 2)
                        {
                        AgainReSetSend:
                            NetCom3.Instance.Send(NetCom3.Cover("EB 90 31 01 08"), 1);
                            if (!NetCom3.Instance.MoveQuery())
                            {
                                if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.Sendfailure)
                                {
                                    LogFile.Instance.Write(DateTime.Now + "清洗盘复位返回指令重新发送！");
                                    goto AgainReSetSend;
                                }
                                else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                                {
                                    NetCom3.Instance.stopsendFlag = true;
                                    ShowWarnInfo(getString("keywordText.WashResetOver"), getString("keywordText.Wash"), 1);
                                    AllStop();
                                }
                            }
                            goto AgainNewMove;
                        }
                        else
                        {
                            NetCom3.Instance.stopsendFlag = true;
                            ShowWarnInfo(getString("keywordText.MWashLossNullS"), getString("keywordText.Move"), 1);
                            AllStop();
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
                            goto AgainNewMove;
                        else
                        {
                            NetCom3.Instance.stopsendFlag = true;
                            ShowWarnInfo(getString("keywordText.MWashLossIsKnocked"), getString("keywordText.Move"), 1);
                            AllStop();
                        }
                    }
                    else if (NetCom3.Instance.MoverrorFlag == (int)ErrorState.OverTime)
                    {
                        NetCom3.Instance.stopsendFlag = true;
                        ShowWarnInfo(getString("keywordText.MWashLossOver"), getString("keywordText.Move"), 1);
                        AllStop();
                    }
                    #endregion
                }
                else
                {
                    #region 取管成功
                    OperateIniFile.WriteIniData("TubePosition", "no" + dtWashTrayTubeStatus.Rows[28][0].ToString().Substring(2), "0", iniPathWashTrayInfo);
                    LogFile.Instance.Write("==============  " + washCountNum + "  扔管");
                    if (dtWashTrayTubeStatus.Rows.Count > 0)
                    {
                        dtWashTrayTubeStatus.Rows[28][1] = "0";
                        dtWashTrayTubeStatus.Rows[28][2] = 0;
                        dtWashTrayTubeStatus.Rows[28][3] = 0;
                        dtWashTrayTubeStatus.Rows[28][4] = 0;
                    }
                    #endregion
                }
                LogFile.Instance.Write(washCountNum + "扔管指令执行完成！抓管移管状态：" + NetCom3.Instance.MoverrorFlag);
                //moveTube.putTubePos = "0-0";
                //moveTube.StepNum = int.Parse(dtWashTrayTubeStatus.Rows[28][3].ToString());
                //moveTube.TakeTubePos = "2-" + dtWashTrayTubeStatus.Rows[28][0].ToString().Substring(2);
                //moveTube.TestId = int.Parse(dtWashTrayTubeStatus.Rows[28][2].ToString());
                //lock (locker2)
                //{
                //    if (lisMoveTube.Count > 0)
                //    {
                //        lisMoveTube.Insert(0, moveTube);
                //    }
                //    else
                //    {
                //        lisMoveTube.Add(moveTube);
                //    }
                //}
                //moveTube = new MoveTubeStatus();
            }
            #endregion
            #endregion
        }
        /// <summary>
        /// 正在计算标志位
        /// </summary>
        bool calNowFlag = false;
        bool ValueFlag = false;
        Queue<int> listestid = new Queue<int>();
        object dataLocker = new object();
        object readLocker = new object();
        void Read(object readInfo)
        {
            //while (dataRecive[0] == null || dataRecive[3] != "A3" || dataRecive[15] == "00" )
            //{
            //    Thread.Sleep(5);
            //}
            //string readData = dataRecive[12] + dataRecive[13] + dataRecive[14] + dataRecive[15];
            //lock (dataLocker)
            //{
            //    dataRecive[0] = null;
            //}
            string readData;
            lock (readLocker)
            {
                while (DataReciveNumberRead.Count < 1)
                {
                    Thread.Sleep(30);
                }
                lock (DataReciveNumberRead)
                {
                    readData = DataReciveNumberRead.Dequeue();
                }
            }
            //这部分会导致部分数据不显示
            //while (calNowFlag && RunFlag == (int)RunFlagStart.IsRuning)
            //{
            //    Thread.Sleep(30);
            //}
            calNowFlag = true;

            int testid;
            lock (readLocker)
            {
                while (listestid.Count < 1)
                {
                    Thread.Sleep(30);
                }
                lock (listestid)
                {
                    testid = listestid.Dequeue();
                }
            }
            double PMT = double.Parse(System.Convert.ToInt32("0x" + readData, 16).ToString());
            PMT = GetPMT(PMT);
            int pmt = (int)PMT;
            int NewPMT = (int)((double)pmt * double.Parse(Coefficient));
            LogFile.Instance.Write(string.Format("{0}<-:{1}", DateTime.Now.ToString("HH:mm:ss:fff"), "实验" + testid + "读数发光值为:" + pmt + ",通过校准系数校准后的读数发光值为：" + NewPMT));
            CalculatResult(testid, NewPMT);
        }
        /// <summary>
        /// 反应管完成结果计算
        /// </summary>
        /// <param name="testid">测试序号</param>
        /// <param name="pmt">光子值</param>
        void CalculatResult(int testid, int pmt)
        {
            #region 当前项目的所有样本完成时更新样本状态
            if (dgvWorkListData.Rows.Count == 0) return;
            TestItem ti = new TestItem();
            ti.ItemName = dgvWorkListData.Rows[testid - 1].Cells["ItemName"].Value.ToString();
            ti.RegentBatch = dgvWorkListData.Rows[testid - 1].Cells["RegentBatch"].Value.ToString();
            ti.SampleID = int.Parse(dgvWorkListData.Rows[testid - 1].Cells["SampleID"].Value.ToString());
            ti.SampleNo = dgvWorkListData.Rows[testid - 1].Cells["SampleNo"].Value.ToString();
            ti.SamplePos = int.Parse(dgvWorkListData.Rows[testid - 1].Cells["Position"].Value.ToString());
            ti.SampleType = dgvWorkListData.Rows[testid - 1].Cells["SampleType"].Value.ToString();
            ti.Schedule = dgvWorkListData.Rows[testid - 1].Cells["Schedule"].Value.ToString();
            ti.TestID = int.Parse(dgvWorkListData.Rows[testid - 1].Cells["No"].Value.ToString());
            ti.TestStatus = dgvWorkListData.Rows[testid - 1].Cells["TestStatus"].Value.ToString();
            ti.TestTime = dgvWorkListData.Rows[testid - 1].Cells["TestTime"].Value.ToString();
            lisTiEnd.Add(ti);
            //已经结束的反应管列表中查询当前testid的
            List<TestItem> lis1 = lisTiEnd.FindAll(tx => tx.SamplePos ==
                int.Parse(dgvWorkListData.Rows[testid - 1].Cells["Position"].Value.ToString()));
            List<TestItem> BToListTi = new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource);
            //总共的反应管查询当前testid的
            List<TestItem> lis2 = BToListTi.FindAll(tx => tx.SamplePos ==
                int.Parse(dgvWorkListData.Rows[testid - 1].Cells["Position"].Value.ToString()));
            DbHelperOleDb db = new DbHelperOleDb(1);
            if (lis1.Count == lis2.Count)
            {
                Thread updateStatus = new Thread(() =>
                {
                    DbHelperOleDb db2 = new DbHelperOleDb(1);
                    DbHelperOleDb.ExecuteSql(1, @"update tbSampleInfo set Status = 1 where SampleID = " + int.Parse
                               (dgvWorkListData.Rows[testid - 1].Cells["SampleID"].Value.ToString()));
                    for (int i = 0; i < dtSpInfo.Rows.Count; i++)
                    {
                        try
                        {
                            if (int.Parse(dtSpInfo.Rows[i]["Position"].ToString())
                            == int.Parse(dgvWorkListData.Rows[testid - 1].Cells["Position"].Value.ToString()))
                            {
                                dtSpInfo.Rows[i]["Status"] = "1";
                            }
                        }
                        catch (Exception ex)
                        {
                            LogFile.Instance.Write("记录添加样本操作相同数据时出现的异常信息，暂时不需要进行处理" + ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                });
                updateStatus.CurrentCulture = Language.AppCultureInfo;
                updateStatus.CurrentUICulture = Language.AppCultureInfo;
                updateStatus.Start();
                updateStatus.Join();
            }

            #endregion

            #region 初始化变量
            result = "";
            SampleNumCurrent--;
            int NumResult = 0;//实验结果精度 2018-11-05 zlx add
            string Unit = "";//实验结果单位 2018-11-10 zlx add
            //获取当前读数完成的反应管的项目名称
            string ItemName = dgvWorkListData.Rows[testid - 1].Cells["ItemName"].Value.ToString();
            //获取当前读数完成的反应管使用试剂的批号
            string Batch = dgvWorkListData.Rows[testid - 1].Cells["RegentBatch"].Value.ToString();
            //获取当前反应管样本的类型
            string sampleType = dgvWorkListData.Rows[testid - 1].Cells["SampleType"].Value.ToString();
            db = new DbHelperOleDb(0);
            //查询当前项目范围
            //2018-08-03 zlx mod
            DataTable tbtbProject = DbHelperOleDb.Query(0, @"select ValueUnit,RangeType,ExpiryDate,VRangeType,ValueRange1,ValueRange2 from tbProject where ShortName = '" + ItemName + "'").Tables[0];
            string Range1 = "";
            string Range2 = "";
            double MaxValue = 0;
            double MinValue = 0;
            int ExpiryDate = 0;
            string VRangeType = "";
            string newDiuTimes = "1"; //实验结果中的稀释倍数
            foreach (DataRow dr in tbtbProject.Rows)
            {
                if (dr != null)
                {
                    Range1 = dr["ValueRange1"].ToString();
                    Range2 = dr["ValueRange2"].ToString();
                    ExpiryDate = Convert.ToInt32(dr["ExpiryDate"]);
                    //2018-11-24 zlx mod
                    if (dr["RangeType"].ToString() != "")
                        NumResult = int.Parse(dr["RangeType"].ToString());
                    else
                        NumResult = 0;
                    Unit = dr["ValueUnit"].ToString();
                    VRangeType = dr["VRangeType"].ToString();
                }
            }
            //object ob = DbHelperOleDb.GetSingle(@"select ValueRange1 from tbProject where ShortName = '" + ItemName + "'");
            //string Range1 = ob == null ? "" : ob.ToString();
            //ob = DbHelperOleDb.GetSingle(@"select ValueRange2 from tbProject where ShortName = '" + ItemName + "'");
            //string Range2 = ob == null ? "" : ob.ToString();
            //当前反应管使用过的项目定标信息
            ScalingInfo CurrentScal = lisScalingInfo.Find(ty => ty.ItemName == ItemName && ty.RegenBatch == Batch);
            LogFile.Instance.Write("查询定标信息，项目名称为：" + ItemName + ",试剂批次为：" + Batch);
            //2018-08-17  zlx add
            int ScalingState = 0;
            if (CurrentScal.testType == 0)//定性实验
            {
                #region 定性实验
                if (CurrentScal.Num == "0")//实验中无正在做的标准品或定标液，使用历史定标
                {
                    db = new DbHelperOleDb(1);
                    //2018-08-03 zlx mod
                    DataTable tbScalingResult = DbHelperOleDb.Query(1, @"select Points,ActiveDate from tbScalingResult where ItemName = '" + ItemName
                        + "' and RegentBatch = '" + Batch + "' and Status = 1").Tables[0];
                    string points = "";
                    string ActiveDate = "";
                    foreach (DataRow dr in tbScalingResult.Rows)
                    {
                        if (dr != null)
                        {
                            points = dr["Points"].ToString();
                            ActiveDate = dr["ActiveDate"].ToString();
                        }
                    }

                    if (Convert.ToDateTime(ActiveDate).AddDays(ExpiryDate).Date < DateTime.Now.Date)
                    {
                        //2017-08-17 zlx mod
                        ScalingState = 1;
                    }
                    //string points = DbHelperOleDb.GetSingle(@"select Points from tbScalingResult where ItemName = '" + ItemName
                    //    + "' and RegentBatch = '" + Batch + "' and Status = 1").ToString();
                    dtScalCacResult.Rows.Add(ItemName, 0, points, Batch);
                }

                if (sampleType.Contains(getString("keywordText.CalibrationSolution")))
                {
                    dtScalingPMT.Rows.Add(PMT, 0, ItemName, 0, Batch);
                    //从dtScalingPMT中查询定性的当前项目的定标液信息
                    DataRow[] rows = dtScalingPMT.Select("ItemName = '" + ItemName + "' and RegentBatch = '" + Batch + "'");
                    if (rows.Length == int.Parse(CurrentScal.Num))
                    {
                        DataTable dtCacuData = new DataTable();
                        dtCacuData = dtScalingPMT.Clone(); // 克隆dtScalingPMT 的结构，包括所有 dtScalingPMT 架构和约束,并无数据；
                        foreach (DataRow row in rows)  // 将查询的结果添加到dtCacuData中；
                        {
                            dtCacuData.Rows.Add(row.ItemArray); //符合条件的所有数据
                        }
                        //  开启读数计算线程
                        CaculateThread = new Thread(new ParameterizedThreadStart(Calculate));
                        CaculateThread.IsBackground = true;
                        CaculateThread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
                        CaculateThread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
                        CaculateThread.Start(dtCacuData);
                    }
                }
                else//未知品质控品计算
                {
                    DataRow[] drCaluPara = dtScalCacResult.Select("ItemName = '" + ItemName + "' and RegentBatch = '" + Batch + "'");
                    while (drCaluPara.Length == 0)
                    {
                        drCaluPara = dtScalCacResult.Select("ItemName = '" + ItemName + "' and RegentBatch = '" + Batch + "'");
                        Thread.Sleep(30);
                    }
                    if (drCaluPara.Length > 0)
                    {
                        double scoValue = pmt / double.Parse(drCaluPara[0]["Result"].ToString());
                        concentration = "";
                        sco = scoValue.ToString();
                        if (scoValue <= 1)
                        {
                            result = getString("keywordText.positive");
                        }
                        else if (scoValue > 1)
                        {
                            result = getString("keywordText.negative");
                        }
                        else
                        {
                            result = getString("keywordText.NoComputation");
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region 定量实验
                if (CurrentScal.Num == "0")//无标准品使用历史定标
                {
                    db = new DbHelperOleDb(1);
                    //2018-08-03 zlx mod
                    DataTable tbScalingResult = DbHelperOleDb.Query(1, @"select Points,ActiveDate from tbScalingResult where ItemName = '" + ItemName
                        + "' and RegentBatch = '" + Batch + "' and Status = 1").Tables[0];
                    string points = "";
                    string ActiveDate = "";
                    foreach (DataRow dr in tbScalingResult.Rows)
                    {
                        if (dr != null)
                        {
                            points = dr["Points"].ToString();
                            ActiveDate = dr["ActiveDate"].ToString();
                        }
                    }
                    if (Convert.ToDateTime(ActiveDate).AddDays(ExpiryDate).Date < DateTime.Now.Date)
                    {
                        //2018-08-17 zlx mod
                        ScalingState = 1;
                    }
                    //string points = DbHelperOleDb.GetSingle(@"select Points from tbScalingResult where ItemName = '" + ItemName +
                    //    "' and RegentBatch= '" + Batch + "' and Status = 1").ToString();
                    if (points != "")
                    {
                        //获取定标点
                        string[] curvePoints = points.Split(';');
                        if (curvePoints.Length == 0)
                        {
                            return;
                        }
                        DataTable dtCacuData = new DataTable();
                        dtCacuData = dtScalingPMT.Clone();
                        for (int i = 0; i < curvePoints.Length; i++)
                        {
                            if (curvePoints[i] == "")
                            {
                                continue;
                            }
                            //将每个定标点的浓度和RLU分开放到数组中
                            string[] pointsData = curvePoints[i].Split(',');
                            dtCacuData.Rows.Add(pointsData[1].Substring(0, pointsData[1].IndexOf(")")), pointsData[0].Substring(1), ItemName, 1, Batch);
                        }
                        //  开启读数计算线程
                        CaculateThread = new Thread(new ParameterizedThreadStart(Calculate));
                        CaculateThread.IsBackground = true;
                        CaculateThread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
                        CaculateThread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
                        CaculateThread.Start(dtCacuData);
                    }
                }
                if (sampleType.Contains(getString("keywordText.Standard")))
                {
                    if (CurrentScal.Num.Split(',').Length >= 6)
                    {
                        #region 六点定标
                        result = "";
                        double conc = 0;
                        string[] concs = CurrentScal.TestConc.Split(',');
                        string[] Num = CurrentScal.Num.Split(',');
                        if (sampleType == getString("keywordText.StandardA"))
                        {
                            conc = double.Parse(concs[0]);
                        }
                        else if (sampleType == getString("keywordText.StandardB"))
                        {
                            conc = double.Parse(concs[1]);
                        }
                        else if (sampleType == getString("keywordText.StandardC"))
                        {
                            conc = double.Parse(concs[2]);
                        }
                        else if (sampleType == getString("keywordText.StandardD"))
                        {
                            conc = double.Parse(concs[3]);
                        }
                        else if (sampleType == getString("keywordText.StandardE"))
                        {
                            conc = double.Parse(concs[4]);
                        }
                        else if (sampleType == getString("keywordText.StandardF"))
                        {
                            conc = double.Parse(concs[5]);
                        }
                        else if (sampleType == getString("keywordText.StandardG"))
                        {
                            conc = double.Parse(concs[6]);
                        }
                        dtScalingPMT.Rows.Add(pmt, conc, ItemName, 1, Batch);
                        concentration = conc.ToString("#0.000");
                        result = "";

                        //从dtScalingPMT中查询定性的当前项目的定标液信息
                        DataRow[] rows = dtScalingPMT.Select("ItemName = '" + ItemName + "' and RegentBatch= '" + Batch + "'");
                        int ScalSumNum = 0;
                        //计算该项目该批号的所有标准品数量
                        foreach (string N in Num)
                        {
                            ScalSumNum += int.Parse(N);
                        }
                        if (rows.Length == ScalSumNum)
                        {
                            DataTable dtCacuData = new DataTable();
                            dtCacuData = dtScalingPMT.Clone(); // 克隆dtScalingPMT 的结构，包括所有 dtScalingPMT 架构和约束,并无数据；
                            //存放需要取均值的标准品
                            string[] PmtAvg = new string[Num.Length];
                            for (int i = 0; i < concs.Length; i++)
                            {
                                for (int j = 0; j < rows.Length; j++)
                                {
                                    if (double.Parse(rows[j]["Conc"].ToString()) == double.Parse(concs[i]))
                                    {
                                        PmtAvg[i] += rows[j]["PMT"].ToString() + ",";
                                    }
                                }
                            }
                            for (int k = 0; k < PmtAvg.Length; k++)
                            {
                                dtCacuData.Rows.Add(AVG(PmtAvg[k].Split(',')), concs[k], ItemName, 1, Batch);
                            }
                            CaculateThread = new Thread(new ParameterizedThreadStart(Calculate));
                            CaculateThread.IsBackground = true;
                            CaculateThread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
                            CaculateThread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
                            CaculateThread.Start(dtCacuData);
                        }

                        #endregion
                    }
                    else
                    {
                        #region 两点校准
                        DataTable dtCacuD = new DataTable();
                        DataTable dtCacuData = new DataTable();
                        #region 将校准品的浓度与发光值保存到dtScalingPMT
                        result = "";
                        double conc = 0;
                        string[] concs = CurrentScal.TestConc.Split(',');
                        string[] Num = CurrentScal.Num.Split(',');
                        if (sampleType == getString("keywordText.StandardC"))
                        {
                            conc = double.Parse(concs[0]);//默认为标准品C点浓度
                        }
                        else if (sampleType == getString("keywordText.StandardE"))
                        {
                            conc = double.Parse(concs[1]);//默认为标准品E点浓度
                        }
                        dtScalingPMT.Rows.Add(pmt, conc, ItemName, 1, Batch);
                        concentration = conc.ToString("#0.000");
                        result = "";
                        #endregion
                        //从dtScalingPMT中查询当前项目的校准品信息
                        DataRow[] rows = dtScalingPMT.Select("ItemName = '" + ItemName
                            + "' and RegentBatch= '" + Batch + "'");
                        int ScalSumNum = 0;
                        //计算该项目该批号的所有标准品数量
                        foreach (string N in Num)
                        {
                            ScalSumNum += int.Parse(N);
                        }
                        if (rows.Length == ScalSumNum)//两点定标只有两点
                        {
                            #region 获取原始定标的浓度与发光值
                            db = new DbHelperOleDb(1);
                            string points = DbHelperOleDb.GetSingle(1, @"select Points from tbMainScalCurve where ItemName = '"
                                + ItemName + "' and RegentBatch = '" + Batch + "'").ToString();
                            if (points != "")
                            {
                                //获取定标点
                                string[] curvePoints = points.Split(';');
                                if (curvePoints.Length == 0)
                                {
                                    return;
                                }

                                dtCacuD = dtScalingPMT.Clone();
                                for (int i = 0; i < curvePoints.Length; i++)
                                {
                                    if (curvePoints[i] == "")
                                    {
                                        continue;
                                    }
                                    //将每个定标点的浓度和RLU分开放到数组中
                                    string[] pointsData = curvePoints[i].Split(',');
                                    dtCacuD.Rows.Add(pointsData[1].Substring(0, pointsData[1].IndexOf(")")), pointsData[0].Substring(1), ItemName, 1, Batch);
                                }
                            }
                            #endregion
                            #region 取出校准品浓度与发光值存储在变量中

                            dtCacuData = dtScalingPMT.Clone(); // 克隆dtScalingPMT 的结构，包括所有 dtScalingPMT 架构和约束,并无数据；
                            //存放需要取均值的标准品
                            string[] PmtAvg = new string[Num.Length];
                            for (int i = 0; i < concs.Length; i++)
                            {
                                for (int j = 0; j < rows.Length; j++)
                                {
                                    double dtest = double.Parse(rows[j]["Conc"].ToString());
                                    if (double.Parse(rows[j]["Conc"].ToString()) == double.Parse(concs[i]))
                                    {
                                        PmtAvg[i] += rows[j]["PMT"].ToString() + ",";
                                    }
                                }
                            }
                            for (int k = 0; k < PmtAvg.Length; k++)
                            {
                                dtCacuData.Rows.Add(AVG(PmtAvg[k].Split(',')), concs[k], ItemName, 1, Batch);
                            }
                            #endregion
                            #region 根据校准品浓度与PMT，利用计算公式计算原始定标中各个浓度校正后的PMT，形成新的定标信息表
                            DataTable dtCorrectedCacuData = new DataTable();
                            dtCorrectedCacuData = dtScalingPMT.Clone();
                            dtCorrectedCacuData = GetCorrectedPoints(dtCacuData, dtCacuD);
                            #endregion
                            //  开启读数计算线程
                            CaculateThread = new Thread(new ParameterizedThreadStart(Calculate));
                            CaculateThread.IsBackground = true;
                            CaculateThread.CurrentCulture = Language.AppCultureInfo;//lyq 20210616
                            CaculateThread.CurrentUICulture = Language.AppCultureInfo;//lyq 20210616
                            CaculateThread.Start(dtCorrectedCacuData);
                        }
                        #endregion
                    }
                }
                else
                {
                    #region 非标准品、非校准品 结果计算
                    DataRow[] drCaluPara = dtScalCacResult.Select("ItemName = '" + ItemName + "' and RegentBatch = '" + Batch + "'");
                    while (drCaluPara.Length == 0)
                    {
                        drCaluPara = dtScalCacResult.Select("ItemName = '" + ItemName + "' and RegentBatch = '" + Batch + "'");
                        Thread.Sleep(10);
                    }
                    string strPars = drCaluPara[0]["Result"].ToString();
                    string[] pars = strPars.Split('|');
                    List<double> datas = new List<double>();
                    foreach (string strPar in pars)
                    {
                        datas.Add(double.Parse(strPar));
                    }
                    #region 浓度和结果显示
                    double[] dbpars = datas.ToArray();
                    db = new DbHelperOleDb(0);
                    //线性范围最小值 2018-10-20 zlx mod
                    MinValue = double.Parse(DbHelperOleDb.GetSingle(0, @"select MinValue from tbProject where ShortName = '" + ItemName + "'").ToString());
                    //线性范围最大值 2018-10-20 zlx mod
                    db = new DbHelperOleDb(0);//2018-09-04 zlx add
                    MaxValue = double.Parse(DbHelperOleDb.GetSingle(0, @"select MaxValue from tbProject where ShortName = '" + ItemName + "'").ToString());
                    //正则表达式 表示汉字范围;
                    Regex cn = new Regex("[\u4e00-\u9fa5]+");
                    //根据线性范围决定显示类型
                    concentration = CalculationConcentration(ItemName, Batch, pmt).ToString("#0.000");
                    //concentration = GetResultInverse(dbpars, pmt).ToString("#0.000000");//对得出的浓度进行小数点保留 lyn modify 20171118
                    LogFile.Instance.Write(DateTime.Now + "浓度：" + concentration + ";ItemName：" + ItemName + ";Batch:" + Batch + ";pmt:" + pmt);
                    #region 若用户对样本因结果不在线性范围内进行稀释
                    if (dtSampleRunInfo.Rows.Count > 0)
                    {
                        db = new DbHelperOleDb(0);
                        //项目中的稀释倍数
                        int DiuTimes = int.Parse(DbHelperOleDb.GetSingle(0, @"select DiluteCount from tbProject where ShortName = '" + ItemName + "'").ToString());
                        //该样本编号
                        string SampleNo = dgvWorkListData.Rows[testid - 1].Cells["SampleNo"].Value.ToString();
                        DataRow[] rows = dtSampleRunInfo.Select("ItemName='" + ItemName + "' and SampleNo='"
                        + SampleNo + "'");
                        newDiuTimes = rows[0]["DilutionTimes"].ToString();
                        if (DiuTimes < int.Parse(newDiuTimes))
                        {
                            double DiuProportion = double.Parse(newDiuTimes) / DiuTimes;
                            LogFile.Instance.Write(DateTime.Now + "不固定稀释倍数：" + DiuProportion + "");
                            concentration = (double.Parse(concentration) * DiuProportion).ToString("#0.000");
                        }
                    }
                    #endregion
                    string sconcentration = concentration;
                    if (double.IsNaN(double.Parse(concentration)))
                    {
                        concentration = GetNanPmtConcentration(ItemName, Batch, pmt, MinValue, MaxValue);
                        sconcentration = concentration.Substring(1);
                    }
                    else if (double.Parse(concentration) < MinValue)
                    {
                        concentration = "<" + MinValue.ToString("#0.000");
                        sconcentration = concentration.Substring(1);
                    }
                    else if (double.Parse(concentration) > (MaxValue))
                    {
                        concentration = ">" + (MaxValue).ToString("#0.000");
                        sconcentration = concentration.Substring(1);
                    }
                    if (VRangeType != "" && int.Parse(VRangeType) > 0)
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

                    if (sampleType.Contains(getString("keywordText.Control")))
                    {
                        result = "";
                    }
                    #endregion
                    #endregion
                }

                if (sampleType.Contains(getString("keywordText.Standard")) || sampleType.Contains(getString("keywordText.Control")))
                {
                    result = "";
                }
                #endregion
            }
            #endregion
            testResult.DiluteCount = int.Parse(newDiuTimes);
            testResult.SampleID = int.Parse
                (dgvWorkListData.Rows[testid - 1].Cells["SampleID"].Value.ToString());
            testResult.TestID = testid;
            testResult.SampleNo = (dgvWorkListData.Rows[testid - 1].Cells["SampleNo"].Value.ToString());
            testResult.SamplePos = int.Parse(dgvWorkListData.Rows[testid - 1].Cells["Position"].Value.ToString());
            testResult.SampleType = sampleType;
            testResult.ItemName = ItemName;
            testResult.PMT = pmt;
            //testResult.concentration = concentration;
            if (concentration != "" && !(concentration.Contains(">") || concentration.Contains("<")))
                testResult.concentration = concentration;//2018-11-07 zlx mod
            else
                testResult.concentration = concentration;
            testResult.Result = result;
            testResult.Range1 = Range1;
            testResult.Range2 = Range2;
            testResult.sco = sco;
            testResult.Status = ScalingState;
            testResult.ReagentBeach = dgvWorkListData.Rows[testid - 1].Cells["RegentBatch"].Value.ToString();
            testResult.SubstratePipe = dgvWorkListData.Rows[testid - 1].Cells["SubstratePipe"].Value.ToString();
            testResult.Unit = Unit;//2018-11-10 zlx add
            testResult.ResultDatetime = DateTime.Now;
            SaveTestResultData(BToListTi, lis1, lis2);

            calNowFlag = false;
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
        /// <summary>
        /// 保存实验结果
        /// </summary>
        /// <param name="BToListTi"></param>
        /// <param name="lis1"></param>
        /// <param name="lis2"></param>
        void SaveTestResultData(List<TestItem> BToListTi, List<TestItem> lis1, List<TestItem> lis2)
        {
            var testresult = new TestResult();
            testresult.DiluteCount = testResult.DiluteCount;
            testresult.PMT = testResult.PMT;
            testresult.SampleID = testResult.SampleID;
            testresult.TestID = testResult.TestID;
            testresult.SampleNo = testResult.SampleNo;
            testresult.SamplePos = testResult.SamplePos;
            testresult.SampleType = testResult.SampleType;
            testresult.ItemName = testResult.ItemName;
            testresult.concentration = testResult.concentration;
            testresult.Result = testResult.Result;
            testresult.sco = testResult.sco;
            testresult.Range1 = testResult.Range1;
            testresult.Range2 = testResult.Range2;
            testresult.Status = testResult.Status;
            testresult.ReagentBeach = testResult.ReagentBeach;
            testresult.SubstratePipe = testResult.SubstratePipe;
            testresult.Unit = testResult.Unit;
            testresult.ResultDatetime = testResult.ResultDatetime;
            if (lisTiEnd.Count == BToListTi.Count)
                frmTestResult.BRun = false;
            LogFile.Instance.Write("*********  发光值  ： " + testresult.PMT + "  **********");

            //调度到主线程添加的目的是为了保证结果列表添加刷新，但是有可能丢失数据
            this.Invoke(new Action(() =>
            {
                BTestResult.Add(testresult);
                TemporaryTestResult.Add(testresult);
            }));

            if (testresult.SampleType.Contains(getString("keywordText.Standard")))
            {
                GC.KeepAlive(testresult);//防止被回收               
                List<TestItem> BToList = BToListTi.FindAll(tx => (tx.ItemName == testresult.ItemName && tx.SampleType.Contains(getString("keywordText.Standard")) && tx.RegentBatch == testresult.ReagentBeach));
                List<TestItem> ENDList = lisTiEnd.FindAll(tx => (tx.ItemName == testresult.ItemName && tx.SampleType.Contains(getString("keywordText.Standard")) && tx.RegentBatch == testresult.ReagentBeach));
                if (BToList.Count == ENDList.Count)
                {
                    //List<TestResult> ScalingResult = new List<TestResult>(BTestResult).FindAll(tx => (tx.ItemName == testResult.ItemName && testResult.SampleType.Contains(getString("keywordText.Standard"))));
                    //frmTestResult f = new frmTestResult();
                    List<TestResult> ScalingResult = new List<TestResult>(TemporaryTestResult).FindAll(tx => (tx.ItemName == testresult.ItemName && testresult.SampleType.Contains(getString("keywordText.Standard")) && tx.ReagentBeach == testresult.ReagentBeach));

                    Invoke(new Action(() =>
                    {
                        frmTestResult f;
                        if (!CheckFormIsOpen("frmTestResult"))
                        {
                            f = new frmTestResult();
                        }
                        else
                        {
                            f = (frmTestResult)Application.OpenForms["frmTestResult"];
                        }
                        List<TestResult> SScalingResult = new List<TestResult>();
                        foreach (TestResult tr in ScalingResult)
                        {
                            SScalingResult.Add(tr);
                        }
                        f.SaveStandardResult(ScalingResult);
                    }));
                }
            }
            else
                SaveResultDate(testresult);
            SendLisData(lis1, lis2);
            testResult = new TestResult();
        }

        /// <summary>
        /// 向lis系统发送实验结果
        /// </summary>
        /// <param name="lis1"></param>
        /// <param name="lis2"></param>
        void SendLisData(List<TestItem> lis1, List<TestItem> lis2)
        {
            string CommunicationType = OperateIniFile.ReadInIPara("LisSet", "CommunicationType");
            bool IsTrueTimeTran = bool.Parse(OperateIniFile.ReadInIPara("LisSet", "IsTrueTimeTran"));
            string tranInfo = OperateIniFile.ReadInIPara("LisSet", "TransInfo");
            string SendingApplication = OperateIniFile.ReadInIPara("LisSet", "SendingApplication");
            string SendingFacility = OperateIniFile.ReadInIPara("LisSet", "SendingFacility");
            if (CommunicationType == getString("keywordText.NetworkConnection"))
            {
                if (LisCommunication.Instance.IsConnect() && IsTrueTimeTran && lis1.Count == lis2.Count)
                {
                    CMessageParser Cmp = new CMessageParser();
                    Cmp.SendApplication = SendingApplication;
                    Cmp.SendFacility = SendingFacility;
                    List<TestResult> list = new List<TestResult>();
                    for (int i = 0; i < BTestResult.Count; i++)
                    {
                        if (BTestResult[i].SampleNo == testResult.SampleNo)
                            list.Add(BTestResult[i]);
                    }
                    Cmp.SendORU(list);
                }
            }
            if (CommunicationType == getString("keywordText.SerialConnection"))
            {
                if (LisConnection.Instance.IsOpen() && IsTrueTimeTran && lis1.Count == lis2.Count)
                {
                    CAMessageParser Cmp = new CAMessageParser();
                    Cmp.SendApplication = SendingApplication;
                    Cmp.SendFacility = SendingFacility;
                    List<TestResult> list = new List<TestResult>();
                    for (int i = 0; i < BTestResult.Count; i++)
                    {
                        if (BTestResult[i].SampleNo == testResult.SampleNo)
                            list.Add(BTestResult[i]);
                    }
                    Cmp.SendORU(list);
                }
            }
            //switch (CommunicationType)
            //{
            //    case "网口通讯":
            //        if (LisCommunication.Instance.IsConnect() && IsTrueTimeTran && lis1.Count == lis2.Count)
            //        {
            //            CMessageParser Cmp = new CMessageParser();
            //            Cmp.SendApplication = SendingApplication;
            //            Cmp.SendFacility = SendingFacility;
            //            List<TestResult> list = new List<TestResult>();
            //            for (int i = 0; i < BTestResult.Count; i++)
            //            {
            //                if (BTestResult[i].SampleNo == testResult.SampleNo)
            //                    list.Add(BTestResult[i]);
            //            }
            //            Cmp.SendORU(list);
            //        }
            //        break;
            //    case "串口通讯":
            //        if (LisConnection.Instance.IsOpen() && IsTrueTimeTran && lis1.Count == lis2.Count)
            //        {
            //            CAMessageParser Cmp = new CAMessageParser();
            //            Cmp.SendApplication = SendingApplication;
            //            Cmp.SendFacility = SendingFacility;
            //            List<TestResult> list = new List<TestResult>();
            //            for (int i = 0; i < BTestResult.Count; i++)
            //            {
            //                if (BTestResult[i].SampleNo == testResult.SampleNo)
            //                    list.Add(BTestResult[i]);
            //            }
            //            Cmp.SendORU(list);
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }

        /// <summary>
        /// 保存单个实验结果 2018-07-19 zlx add
        /// </summary>
        /// <param name="result"></param>
        public void SaveResultDate(TestResult result)
        {
            Model.tbAssayResult modelAssayResult = new Model.tbAssayResult();
            Model.tbQCResult modelQCResult = new Model.tbQCResult();

            //存储质控实验结果到数据库
            if (result.SampleType.Contains(getString("keywordText.Control")))
            {
                string QCLevel;
                string QcBatch = result.ReagentBeach;
                if (result.SampleType == getString("keywordText.ControlHigh"))
                {
                    QCLevel = "0";
                    QcBatch = QcBatch + "-H";
                }
                else if (result.SampleType == getString("keywordText.ControlMiddle"))
                {
                    QCLevel = "1";
                    QcBatch = QcBatch + "-M";
                }
                else
                {
                    QCLevel = "2";
                    QcBatch = QcBatch + "-L";
                }
                DbHelperOleDb db = new DbHelperOleDb(3);
                DataTable dtQCInfo = DbHelperOleDb.Query(3, @"select QCID,Batch,QCLevel from tbQC where status = '1' and ProjectName = '"
                                                            + result.ItemName + "' and QCLevel = '" + QCLevel + "' and Batch = '"
                                                            + QcBatch + "'").Tables[0];
                if (dtQCInfo == null || dtQCInfo.Rows.Count == 0)
                {
                    //frmMsgShow.MessageShow("实验结果", "查找不到相关质控的信息！");
                    return;
                }
                modelQCResult.Batch = dtQCInfo.Rows[0][1].ToString();
                if (result.concentration == "")
                {
                    modelQCResult.Concentration = 0;
                }
                else if (result.concentration.Contains(">") || result.concentration.Contains("<"))
                    modelQCResult.Concentration = double.Parse(result.concentration.Substring(1, result.concentration.Length - 1));
                else
                {
                    modelQCResult.Concentration = double.Parse(result.concentration);
                }
                //modelQCResult.Concentration = double.Parse(result.concentration);
                modelQCResult.ConcLevel = int.Parse(dtQCInfo.Rows[0][2].ToString());
                modelQCResult.ConcSPEC = "";
                modelQCResult.ItemName = result.ItemName;
                modelQCResult.PMTCounter = result.PMT;
                //modelQCResult.PMTCounter = (int)((double)result.PMT * double.Parse(Coefficient));
                modelQCResult.QCID = int.Parse(dtQCInfo.Rows[0][0].ToString());
                modelQCResult.Source = 0;
                modelQCResult.TestDate = DateTime.Now;
                modelQCResult.Unit = "";
                db = new DbHelperOleDb(1);
                new BLL.tbQCResult().Add(modelQCResult);
            }
            //存储样本实验结果到数据库
            else
            {
                modelAssayResult.SampleID = result.SampleID;
                modelAssayResult.Batch = "";
                if (result.concentration == "")
                {
                    modelAssayResult.Concentration = "0";
                }
                modelAssayResult.Concentration = result.concentration;
                //else if (result.concentration.Contains(">") || result.concentration.Contains("<"))
                //    modelAssayResult.Concentration = double.Parse(result.concentration.Substring(1, result.concentration.Length - 1));
                //else
                //{
                //    modelAssayResult.Concentration = double.Parse(result.concentration);
                //}
                modelAssayResult.ConcSpec = "";
                modelAssayResult.DiluteCount = result.DiluteCount;
                modelAssayResult.ItemName = result.ItemName;
                modelAssayResult.PMTCounter = result.PMT;
                //modelAssayResult.PMTCounter = (int)((double)result.PMT * double.Parse(Coefficient));
                modelAssayResult.Range = result.Range1 + " " + result.Range2;
                modelAssayResult.Result = result.Result;
                modelAssayResult.Specification = "";
                //modelAssayResult.Status = 0;
                modelAssayResult.TestDate = DateTime.Now;
                modelAssayResult.Unit = "";
                modelAssayResult.Upload = "";
                modelAssayResult.Status = testResult.Status;//2018-07-17 zlx mod
                modelAssayResult.Batch = testResult.ReagentBeach;//2017-08-18 zlx add
                modelAssayResult.Unit = result.Unit;//2018-11-10 zlx mod
                DbHelperOleDb db = new DbHelperOleDb(1);
                new BLL.tbAssayResult().Add(modelAssayResult);
            }
            lisSavedId.Add(result.TestID);
        }
        /// <summary>
        /// 对一组可以转换成int类型的数组取均值
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        int AVG(string[] s)
        {
            int sum = 0;
            int emptyNum = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == "")
                {
                    emptyNum++;
                    continue;
                }
                sum += int.Parse(s[i]);
            }

            return sum / (s.Length - emptyNum);
        }
        /// <summary>
        /// 获取两点校正后的六个点坐标值（浓度与发光值）
        /// </summary>
        /// <param name="dtNew">两点校准的浓度与发光值信息表</param>
        /// <param name="dtOld">原始定标的浓度与发光值信息表</param>
        /// <returns>校正以后的定标信息表</returns>
        public DataTable GetCorrectedPoints(DataTable dtNew, DataTable dtOld)
        {
            double k, b, x, y, x1, y1;
            x = double.Parse(dtNew.Rows[0]["Conc"].ToString());
            x1 = double.Parse(dtNew.Rows[1]["Conc"].ToString());
            //注：当前默认校准点浓度为标准品C、E两点浓度，故不需要比对浓度，直接计算即可。
            //y为同浓度点的吸光度差值占原始浓度的百分比，即y等于新测试吸光度的值减去原始吸光度的值，而后再除以原始吸光度的值
            //y = (double.Parse(dtNew.Rows[0]["PMT"].ToString()) - double.Parse(dtOld.Rows[2]["PMT"].ToString())) / double.Parse(dtOld.Rows[2]["PMT"].ToString());
            //y1 = (double.Parse(dtNew.Rows[1]["PMT"].ToString()) - double.Parse(dtOld.Rows[4]["PMT"].ToString())) / double.Parse(dtOld.Rows[4]["PMT"].ToString());
            //modify 20181026 y
            y = double.Parse(dtNew.Rows[0]["PMT"].ToString()) / double.Parse(dtOld.Rows[2]["PMT"].ToString());
            y1 = double.Parse(dtNew.Rows[1]["PMT"].ToString()) / double.Parse(dtOld.Rows[4]["PMT"].ToString());

            k = (y1 - y) / (x1 - x);
            b = y - k * x;
            for (int i = 0; i < dtOld.Rows.Count; i++)
            {
                if (i == 2)
                {
                    dtOld.Rows[2]["PMT"] = dtNew.Rows[0]["PMT"].ToString();
                }
                else if (i == 4)
                {
                    dtOld.Rows[4]["PMT"] = dtNew.Rows[1]["PMT"].ToString();
                }
                else
                {
                    //除了C、E两点之外，其他点的PMT值为原始浓度乘以（1+吸光度变化百分比）注意（此百分比可正可负）
                    //dtOld.Rows[i]["PMT"] = double.Parse(dtOld.Rows[i]["PMT"].ToString()) * (1 + (k * double.Parse(dtOld.Rows[i]["Conc"].ToString()) + b));
                    //modify 20181026 y
                    dtOld.Rows[i]["PMT"] = double.Parse(dtOld.Rows[i]["PMT"].ToString()) * (k * double.Parse(dtOld.Rows[i]["Conc"].ToString()) + b);
                }
            }
            return dtOld;
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
        /// 计算定量实验中普通样本的浓度
        /// </summary>
        /// <param name="_pars">拟合公式系数</param>
        /// <param name="yValue">吸光度值</param>
        /// <returns></returns>
        [Obsolete("废弃")]
        public double GetResultInverse(double[] _pars, double yValue)//计算浓度
        {
            #region 浓度计算之前处理
            if (yValue < 0)
                yValue = Math.Abs(yValue);
            if (yValue < _pars[3])
                return -1;
            double d = 0.0001;
            double b = 1;
            if (_pars[1] != 0)
            {
                b = (1 / _pars[1]);
            }
            Random rd = new Random();
            foreach (double par in _pars)
            {
                if (double.IsInfinity(par))
                {
                    return 0;
                }
            }
            if ((yValue - _pars[3]) == 0)
            {
                d = ((double)rd.Next(10, 90)) / 10000;
            }
            else
            {
                d = (yValue - _pars[3]);
            }
            //if (m < d)
            //{
            //    if (_pars[1] > 0)
            //    {
            //        d = m;
            //    }
            //    else
            //    {
            //        d = m * 0.99;
            //    }
            //}
            #region 根据幂函数的性质以及不同情况下的定义域进行处理
            double[] fractions = decimalToFractions(b);
            double molecule = Math.Abs(fractions[0]);
            double denominator = Math.Abs(fractions[1]);
            double fourlPart = ((_pars[0] - _pars[3]) / d) - 1;
            if (denominator != 0)
            {
                if (fractions[1] != 0)
                {
                    if (fractions[0] / fractions[1] < 0)
                    {
                        if (molecule % 2 != 0 && denominator % 2 != 0 ||
                            molecule % 2 == 0 && denominator % 2 != 0)//分子和分母都为奇数或者分子为偶数，分母为奇数
                        {
                            if (fourlPart == 0)
                            {
                                fourlPart = ((double)rd.Next(10, 90)) / 10000;
                            }
                        }
                        else if (molecule % 2 != 0 && denominator % 2 == 0)//
                        {
                            if (fourlPart <= 0)
                            {
                                fourlPart = ((double)rd.Next(10, 90)) / 10000;
                            }
                        }
                    }
                    else
                    {
                        if (molecule % 2 != 0 && denominator % 2 == 0)
                        {
                            if (fourlPart < 0)
                            {
                                fourlPart = 0;
                            }
                        }
                    }
                }
            }
            #endregion
            #endregion
            double conc = _pars[2] * (Math.Pow(fourlPart, b));
            return conc;
        }
        /// <summary>
        /// 小数转分数
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        double[] decimalToFractions(double d)
        {
            double[] f = new double[2];
            try
            {
                bool fushuFlag = false;
                double inputnum = Convert.ToDouble(d);
                if (inputnum < 0)
                {
                    fushuFlag = true;
                    inputnum = Math.Abs(inputnum);
                }
                string[] array = inputnum.ToString().Split('.');
                int len = array[1].Length;
                if (len > 9)
                {
                    len = 9;
                    array[1] = double.Parse(array[1]).ToString("f9");
                }
                int num = Convert.ToInt32(Math.Pow(10, len));
                int value = Convert.ToInt32(inputnum * num);
                int a = value;
                int b = num;
                while (a != b)
                {
                    if (a > b)
                        a = a - b;
                    else
                        b = b - a;
                }
                value = value / a;
                num = num / a;
                if (fushuFlag)
                {
                    value = int.Parse("-" + value.ToString());
                }
                f[0] = value;
                f[1] = num;
            }
            catch (Exception)
            {
                f[0] = d;
                f[1] = 0;
            }
            return f;

        }
        object locker = new object();
        /// <summary>
        /// 结果计算
        /// </summary>
        /// <param name="obCaculate"></param>
        void Calculate(object obCaculate)
        {
            Thread.Sleep(10);
            lock (locker)
            {
                DataTable dtCaculate = new DataTable();
                dtCaculate = dtScalingPMT.Clone();
                dtCaculate = obCaculate as DataTable;
                if (dtCaculate.Rows.Count == 0 || dtCaculate == null)
                {
                    return;
                }
                DbHelperOleDb db = new DbHelperOleDb(0);
                #region 定性实验计算
                if (dtCaculate.Rows[0]["ItemType"].ToString() == "0")
                {
                    db = new DbHelperOleDb(0);
                    //计算参数
                    double calulatePara = double.Parse(DbHelperOleDb.GetSingle(0, @"select CalculateMethod from tbProject where ShortName = '"
                                                                                + dtCaculate.Rows[0]["ItemName"].ToString() + "'").ToString()) / 100;
                    //项目cutoff值
                    double cutoffValue = 0;
                    //吸光度和
                    double sumPMT = 0;
                    //吸光度均值
                    double avgPMT = 0;
                    for (int i = 0; i < dtCaculate.Rows.Count; i++)
                    {
                        sumPMT += double.Parse(dtCaculate.Rows[i][4].ToString());
                    }
                    avgPMT = sumPMT / dtCaculate.Rows.Count;
                    cutoffValue = avgPMT * calulatePara;
                    dtScalCacResult.Rows.Add(dtCaculate.Rows[0]["ItemName"].ToString(), 0, cutoffValue.ToString(), dtCaculate.Rows[0]["RegentBatch"].ToString());
                }
                #endregion
                #region 定量实验计算
                else
                {
                    //新建定标方程类的实例
                    CalculateFactory ft = new CalculateFactory();
                    List<Data_Value> CurveData = new List<Data_Value>();
                    db = new DbHelperOleDb(0);
                    //数据库项目表中查询定标方程选择字段
                    int CalMode = int.Parse(DbHelperOleDb.GetSingle(0, @"select CalMode from tbProject where ShortName = '"
                                                                    + dtCaculate.Rows[0]["ItemName"].ToString() + "'").ToString());
                    //取到定标计算公式
                    Calculater er = ft.getCaler(CalMode);
                    //2018-12-05 zlx 
                    for (int j = 0; j < dtCaculate.Rows.Count; j++)
                    {
                        CurveData.Add(new Data_Value()
                        {
                            Data = double.Parse(dtCaculate.Rows[j]["Conc"].ToString()),
                            DataValue = double.Parse(dtCaculate.Rows[j]["PMT"].ToString())
                        });
                    }

                    //排序
                    CurveData.Sort(new Data_ValueDataAsc());
                    ////////////////////y Mod This Block 20181114

                    //相同浓度的吸光度值两两求均值
                    //for (int i = CurveData.Count - 2; i >= 0; i--)
                    //{
                    //    Data_Value v2 = CurveData[i + 1];
                    //    Data_Value v1 = CurveData[i];
                    //    if (v2.Data == v1.Data)
                    //    {
                    //        v1.DataValue = (v1.DataValue + v2.DataValue) / 2;
                    //        CurveData.RemoveAt(i + 1);
                    //    }
                    //}

                    //2018-12-06 zlx 屏蔽
                    List<Data_Value> dataTableTemp = new List<Data_Value>();
                    for (int i = 0; i < CurveData.Count; i++)
                    {
                        double Data = CurveData[i].Data;
                        bool isHave = false;
                        foreach (var item in dataTableTemp)
                        {
                            if (item.Data == Data)
                            {
                                isHave = true;
                                break;
                            }
                        }
                        if (isHave) continue;
                        else
                        {
                            int num = 0;
                            double Sum = 0;
                            List<Data_Value> ListItem = CurveData.FindAll(tx => tx.Data == Data);
                            for (int j = 0; j < ListItem.Count; j++)
                            {
                                if (ListItem[j].Data == Data)
                                {
                                    Sum += ListItem[j].DataValue;
                                    num++;
                                }
                            }
                            Sum = Sum / num;
                            dataTableTemp.Add(new Data_Value() { Data = Data, DataValue = Sum });
                        }
                    }
                    CurveData = dataTableTemp;

                    ////////////////////////This Block End
                    if (CurveData.Count > 0)//ltData标准品
                    {
                        for (int i = 0; i < CurveData.Count; i++)
                        {
                            if (CurveData[i].Data == 0)
                            {
                                CurveData[i].Data = 0.0001;
                            }
                            if (CurveData[i].Data == 1)
                            {
                                CurveData[i].Data = 0.999999;
                            }
                            if (CurveData[i].DataValue == 0)
                            {
                                CurveData[i].DataValue = 0.0001;
                            }
                        }
                        for (int i = 0; i < CurveData.Count; i++)
                        {
                            //对处理过的数据进行纠错
                            if (double.IsNaN(CurveData[i].DataValue) || double.IsNaN(CurveData[i].Data))
                            {
                                MessageBox.Show(getString("keywordText.CalculationInfo"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }

                        //计算定标曲线的系数
                        //for (int i = 0; i < CurveData.Count; i++)
                        //{
                        er.AddData(CurveData);
                        er.Fit();
                        //}
                    }

                    foreach (double par in er._pars)
                    {
                        if (double.IsNaN(par) || double.IsInfinity(par))
                        {
                            //dtScalCacResult.Rows.Add(dtCaculate.Rows[0]["ItemName"].ToString(), 1, "");
                            MessageBox.Show(dtCaculate.Rows[0]["ItemName"].ToString() + getString("keywordText.CalculationInfo"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    dtScalCacResult.Rows.Add(dtCaculate.Rows[0]["ItemName"].ToString(), 1, er.StrPars, dtCaculate.Rows[0]["RegentBatch"]);

                }
                #endregion
            }
        }

        #endregion

        /// <summary>
        /// 实验暂停
        /// </summary>
        void AllPause()
        {
            frmMain.pauseFlag = true;

            //如果所有实验都已经开始就不再停止计时
            if (new List<TestItem>((BindingList<TestItem>)this.dgvWorkListData.DataSource)
                .Where(item => string.IsNullOrEmpty(item.TestStatus)).Count() > 1)
            {
                stopTimer.Stop();
            }
        }
        /// <summary>
        /// 实验停止
        /// </summary>
        void AllStop()
        {
            if (RunFlag == (int)RunFlagStart.IsRuning)
            {
                NetCom3.Instance.stopsendFlag = true;
                RunFlag = (int)RunFlagStart.IsStoping;
                if (AddingSampleFlag)
                {
                    AddingSampleFlag = false;
                    //NetCom3.Delay(10);//如果正在加样步骤，暂时先不会弹出样本装载界面
                }
                LogFile.Instance.Write(DateTime.Now + "实验调用了终止实验的程序！");
                StopStopWatch();//终止倒计时
                //frmMain.BQLiquaid = false;//2018-09-14
                buttonEnableRun(false);
                frmAddSample.newSample = true;
                if (btnRunStatus != null)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        btnRunStatus();
                    }));
                }
                if (SpDiskUpdate != null)
                {
                    this.BeginInvoke(new Action(() => { SpDiskUpdate(); }));
                }
                //将清洗盘当前取放管位置的孔号保存到配置文件中
                //OperateIniFile.WriteIniPara("OtherPara", "washCurrentHoleNum", currentHoleNum.ToString());
                IniUpdateAccess();//由配置文件同步试剂与底物信息到数据库
                try
                {
                    //if (NetCom3.Instance.MoverrorFlag != (int)ErrorState.Success)
                    //    MoveTubeUseFlag = false; 
                    //else
                    //{
                    //    while (MoveTubeUseFlag)
                    //        Thread.Sleep(10);
                    //}
                    while ((MoveTubeThread != null && MoveTubeThread.ThreadState != ThreadState.Stopped) && MoveTubeThread.IsAlive)//MoveTubeUseFlag||
                    {
                        MoveTubeThread.Abort();
                        MoveTubeThread.Join();
                        Thread.Sleep(100);
                    }
                    MoveTubeThread = null;
                    while ((washThread != null && washThread.ThreadState != ThreadState.Stopped) && washThread.IsAlive)//WashTrayUseFlag || 
                    {
                        washThread.Abort();
                        washThread.Join();
                        Thread.Sleep(100);
                    }
                    washThread = null;
                    while ((AddLiquidThread != null && AddLiquidThread.ThreadState != ThreadState.Stopped) && AddLiquidThread.IsAlive)//addLiquiding || 
                    {
                        AddLiquidThread.Abort();
                        AddLiquidThread.Join();
                        Thread.Sleep(100);
                    }
                    AddLiquidThread = null;
                    while (ReadThread != null && ReadThread.ThreadState != ThreadState.Stopped && ReadThread.IsAlive)
                    {
                        ReadThread.Abort();
                        ReadThread.Join();
                        Thread.Sleep(100);
                    }
                    for (int i = 3000; i > 0; i = i - 50)
                    {
                        if (calNowFlag)
                            Thread.Sleep(50);
                        else
                            break;
                    }
                    while ((CaculateThread != null && CaculateThread.ThreadState != ThreadState.Stopped) && CaculateThread.IsAlive)//calNowFlag||
                    {
                        CaculateThread.Abort();
                        CaculateThread.Join();
                        Thread.Sleep(100);
                    }
                    CaculateThread = null;
                    timeReckon.Stop();
                    timer.Enabled = false;
                }
                catch (ThreadAbortException e)
                {
                    frmMsgShow.MessageShow(getString("btnWorkList.Text"), e.Message);
                }
                finally
                {
                    if (frmMain.pauseFlag)
                        frmMain.pauseFlag = false;
                    RunFlag = (int)RunFlagStart.Stoped;
                    if (NetCom3.isConnect)
                        NetCom3.Instance.stopsendFlag = false;
                    //this.Close();
                    if (CheckFormIsOpen("frmWorkList"))
                    {
                        this.BeginInvoke(new Action(() => { Close(); }));
                    }
                }
            }
        }
        /// <summary>
        /// 实验继续进行
        /// </summary>
        void Goon()
        {
            if (EmergencyFlag || addOrdinaryFlag)
            {
                frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.AddSWarn"));
                return;
            }
            LoadingHelper.ShowLoadingScreen();
            frmSampleLoad.CaculatingFlag = true;
            NetCom3.ComWait.Reset();
            #region 进度重新计算
            //获取已经开始运行的样本的进度表
            //List<TestSchedule> RunSchedule = lisTestSchedule.FindAll(tx => (tx.TestID < NoStartTestId && tx.StartTime >= sumTime));
            List<TestSchedule> RunSchedule = lisTestSchedule.FindAll(tx => (tx.TestID < NoStartTestId));
            List<string> runFreeTime = new List<string>() { (sumTime + 2).ToString() + "-1000000" };
            for (int i = 0; i < RunSchedule.Count; i++)
            {
                TestSchedule ts = RunSchedule[i];
                for (int j = 0; j < runFreeTime.Count; j++)
                {
                    string[] minMaxTime = runFreeTime[j].Split('-');
                    //本次空闲时间段的最小值
                    int minTime = int.Parse(minMaxTime[0]);
                    //本次空闲时间段的最大值
                    int maxTime = int.Parse(minMaxTime[1]);
                    if (ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddBeads ||
                        ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube ||
                        ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddSingleR ||
                        (ts.TestScheduleStep == TestSchedule.ExperimentScheduleStep.DoNotTakeCareThis && ((ts.EndTime - ts.StartTime) < 120)))
                    {
                        if (ts.StartTime <= minTime)
                        {
                            if (ts.EndTime < minTime)
                            {
                                continue;
                            }
                            else if (ts.EndTime == minTime || (ts.EndTime < maxTime && ts.EndTime > minTime))
                            {
                                runFreeTime[j] = (ts.EndTime + 1).ToString() + "-" + maxTime;
                                break;
                            }
                            else if (ts.EndTime >= maxTime)
                            {
                                runFreeTime.RemoveAt(j);
                                j = j - 1;
                                break;
                            }
                        }
                        else if (ts.StartTime > minTime && ts.StartTime < maxTime)
                        {
                            if (ts.EndTime > minTime && ts.EndTime < maxTime)
                            {
                                int index = 0;
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime - 1);
                                index = j + 1;
                                runFreeTime.Insert(index, (ts.EndTime + 1).ToString() + "-" + maxTime);
                                break;
                            }
                            else if (ts.EndTime >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (ts.StartTime - 1);
                                break;
                            }
                        }
                        else if (ts.StartTime == maxTime)
                        {
                            if (ts.EndTime >= maxTime)
                            {
                                runFreeTime[j] = (minTime).ToString() + "-" + (maxTime - 1);
                                break;
                            }
                        }
                        else if (ts.StartTime > maxTime)
                        {
                            continue;
                        }
                    }
                }
            }
            //未运行的样本进度计算
            List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(orderModifyTestID(lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestId)), runFreeTime);
            foreach (TestSchedule TestS in lisTestNoRun)
            {
                lisTestSchedule.Remove(TestS);
                TestS.TestID = TestS.TestID + NoStartTestId - 1;
            }
            lisTestSchedule.AddRange(lisTestNoRun);
            lisTestSchedule.Sort(new SortRun());
            #endregion
            List<TestSchedule> tss = lisTestSchedule.FindAll(tx => tx.StartTime <= sumTime);//2019-04-08 删除等于号
            if (tss.Count > 1)
                _GaDoingOne = tss[tss.Count - 1];
            if (EntertRun)//判断在实验调度前点暂停，防止第一个实验不加样 2020/8/13
                TestStep = GaNextOne();
            LoadingHelper.CloseForm();
            NetCom3.ComWait.Set();
            frmSampleLoad.CaculatingFlag = false;
            frmSampleLoad.DtItemInfoNoStat.Rows.Clear();
            MaxTime = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            LastMaxTime = (int)MaxTime;
            MaxTime = MaxTime - sumTime;
            MaxTime *= PiusTimes;
            LastSumTime = sumTime;
            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            TimeLabel2.Invoke(new Action(() =>
            {
                TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                TimeLabel3.Text = span.Minutes.ToString("00");
                TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = true;
            }));
            stopTimer.Start();//倒计时继续

            LeftTestTime = Convert.ToInt32(TimeLabel2.Text) * 60 + Convert.ToInt32(TimeLabel3.Text);
        }

        #region 控件窗体
        /// <summary>
        /// 刷新列表中的进度条控件，重新显示
        /// </summary>
        /// <param name="runflag">实验是否正在运行</param>
        void dgvRefresh(bool runflag)
        {
            EventControlAdd += new Action<bool>(AddSchedule);
            EventControlAdd(runflag);
        }
        private void frmWorkList_SizeChanged(object sender, EventArgs e)
        {
            formSizeChange(this);
        }

        private void fbtnReturn_Click(object sender, EventArgs e)
        {
            if (RunFlag == (int)RunFlagStart.IsRuning)
            {
                this.Hide();
            }
            else
            {
                frmAddSample.newSample = true;
                this.Close();
            }
        }

        private void btnLoadSample_Click(object sender, EventArgs e)
        {
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
                frmSL.Show();
                frmSL.BringToFront(); ;
            }
            if (RunFlag == (int)RunFlagStart.Stoped || RunFlag == (int)RunFlagStart.NoStart)//move y 20180528 更改此代码的位置，不然窗体close后，后边的代码执行不到。
            {
                frmAddSample.newSample = true; //2018-08-02 zlx add
                this.Close();
            }
        }

        private void btnLoadReagent_Click(object sender, EventArgs e)
        {
            if (!CheckFormIsOpen("frmReagentLoad"))
            {
                frmReagentLoad frmRL = new frmReagentLoad();
                frmRL.TopLevel = false;
                frmRL.Parent = this.Parent;
                frmRL.Show();
            }
            else
            {
                frmReagentLoad frmRL = (frmReagentLoad)Application.OpenForms["frmReagentLoad"];
                frmRL.Show();
                frmRL.BringToFront(); ;
            }
            if (RunFlag == (int)RunFlagStart.Stoped || RunFlag == (int)RunFlagStart.NoStart)//move y 20180528 更改此代码的位置，不然窗体close后，后边的代码执行不到。
            {
                frmAddSample.newSample = true; //2018-08-02 zlx add
                this.Close();
            }
        }
        private void fbtnTestResult_Click(object sender, EventArgs e)
        {
            //20180506 zlx mod
            if (!CheckFormIsOpen("frmTestResult"))
            {
                frmTestResult frmTR = new frmTestResult();
                frmTR.TopLevel = false;
                frmTR.Parent = this.Parent;
                frmTR.Show();
                frmTR.BringToFront();
            }
            else
            {
                frmTestResult frmTR = (frmTestResult)Application.OpenForms["frmTestResult"];
                frmTR.Show();
                frmTR.BringToFront();
            }
            if (RunFlag == (int)RunFlagStart.Stoped || RunFlag == (int)RunFlagStart.NoStart)//move y 20180528 更改此代码的位置，不然窗体close后，后边的代码执行不到。
            {
                this.Close();
            }
        }
        /// <summary>
        /// 添加实验进度控件
        /// </summary>
        /// <param name="runFlag">是否正在运行</param>
        private void AddSchedule(bool runFlag)
        {
            //获取datagridview显示的第一行的索引
            int firstViewRowIndex = 0;
            firstViewRowIndex = this.dgvWorkListData.FirstDisplayedScrollingRowIndex;
            //获取向用户显示的datagridview控件的行数
            int viewRows = this.dgvWorkListData.DisplayedRowCount(true);
            for (int i = firstViewRowIndex; i < firstViewRowIndex + viewRows; i++)
            {
                try
                {
                    proBar = lisProBar[i];
                }
                catch (IndexOutOfRangeException exception)
                {
                    LogFile.Instance.Write(DateTime.Now.ToString("mm:ss:ms") + " 记录进度条数组越界异常");
                }
                #region 获取显示在界面上的进度列表单元格的大小并赋值给进度控件
                proBar.Tag = dgvWorkListData.Rows[i].Cells[6].Tag;
                Rectangle rec = this.dgvWorkListData.GetCellDisplayRectangle(6, i, false);
                proBar.Left = rec.X + 2;
                proBar.Top = rec.Y + 4;
                proBar.Width = rec.Width - 5;
                proBar.Height = rec.Height - 8;
                proBar.Visible = true;
                #endregion
                //添加进度控件
                this.dgvWorkListData.Controls.Add(lisProBar[i]);
            }
            EventControlAdd -= new Action<bool>(AddSchedule);
        }

        private void dgvWorkListData_Scroll(object sender, ScrollEventArgs e)
        {
            while (dgvWorkListData.Controls.Count > 2)
            {
                this.dgvWorkListData.Controls.Clear();//清除已有的控件
            }
            dgvRefresh(false);
        }
        private void frmWorkList_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogFile.Instance.Write("进入工作列表closed" + DateTime.Now.ToString("mm:ss:ms"));
            if (RunLightFlag)
            {
                //关闭仪器运行指示灯

                RunLightFlag = false;
            }
            frmMain.btnRunClick -= new Action<object, EventArgs>(TestRun);
            frmSampleLoad.EmergencySample -= new Action(EmergencySampleSch);
            frmMain.btnGoonClick -= new Action(Goon);
            frmMain.btnPauseClick -= new Action(AllPause);
            frmMain.btnStopClick -= new Action(AllStop);
            NetCom3.Instance.ReceiveHandel -= new Action<string>(Instance_ReceiveHandel);
            NetCom3.Instance.EventStop -= new Action(AllStop);
            frmTestResult.RetestSample -= new Action(EmergencySampleSch);
            //NetCom3.MoveTubeError -= this.DisposeMoveAndAddError;//add y 20180723
            AddResetEvent.Set();//add y 20180727
            timeReckon.Stop();
            timer.Enabled = false;
            try
            {
                if (RunFlag == (int)RunFlagStart.IsRuning)
                {
                    if (MoveTubeThread != null)
                    {
                        MoveTubeThread.Abort();

                    }
                    if (washThread != null)
                    {
                        washThread.Abort();
                    }
                    if (AddLiquidThread != null)
                    {
                        AddLiquidThread.Abort();
                    }

                    if (CaculateThread != null)
                    {
                        CaculateThread.Abort();
                    }
                    if (ReadThread != null)
                    {
                        ReadThread.Abort();
                    }
                }
            }
            catch
            {
            }
            finally { RunFlag = (int)RunFlagStart.Stoped; }
        }
        /// <summary>
        /// 同步试剂、底物配置文件信息到数据库
        /// </summary>
        private void IniUpdateAccess()
        {
            #region 将试剂盘配置文件信息更新到数据库
            List<ReagentIniInfo> lisRIinfo = QueryReagentIniInfo();
            if (lisRIinfo.Count > 0)
            {
                foreach (ReagentIniInfo reagentIniInfo in lisRIinfo)
                {
                    //2018-09-04 zlx  mod
                    DbHelperOleDb db = new DbHelperOleDb(3);
                    DbHelperOleDb.ExecuteSql(3, @"update tbReagent set leftoverTestR1 =" + reagentIniInfo.LeftReagent1 + ",leftoverTestR2 = " + reagentIniInfo.LeftReagent2 +
                                              ",leftoverTestR3 = " + reagentIniInfo.LeftReagent3 + ",leftoverTestR4 = " + reagentIniInfo.LeftReagent4 + " where BarCode = '"
                                                  + reagentIniInfo.BarCode + "' and ReagentName = '" + reagentIniInfo.ItemName + "'");
                }
            }
            #endregion
            #region 将底物配置文件信息更新到数据库
            string sbCode1 = OperateIniFile.ReadIniData("Substrate1", "BarCode", "0", iniPathSubstrateTube);
            string sbNum1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "0", iniPathSubstrateTube);
            DbHelperOleDb dbase = new DbHelperOleDb(3);
            DbHelperOleDb.ExecuteSql(3, @"update tbSubstrate set leftoverTest =" + sbNum1 + " where BarCode = '"
                                                  + sbCode1 + "'");
            //string sbCode2 = OperateIniFile.ReadIniData("Substrate2", "BarCode", "0", iniPathSubstrateTube);
            //string sbNum2 = OperateIniFile.ReadIniData("Substrate2", "LeftCount", "0", iniPathSubstrateTube);
            //DbHelperOleDb.ExecuteSql(@"update tbSubstrate set leftoverTest =" + sbNum2 + " where BarCode = '"
            //                                      + sbCode2 + "'");
            #endregion
            string AddTubeString = "";
            if (AddTubeStop.Count > 0)
            {
                foreach (int AddTube in AddTubeStop)
                {
                    AddTubeString = AddTubeString + AddTube + ";";
                }
            }
            OperateIniFile.WriteIniData("Tube", "ReacTrayTub", AddTubeString, iniPathSubstrateTube);
        }
        #endregion

        private void fbtnAddE_Click(object sender, EventArgs e)
        {
            #region 提示
            if (Convert.ToInt16(TimeLabel2.Text) * 60 + Convert.ToInt16(TimeLabel3.Text) < 12)
            {
                MessageBox.Show(getString("keywordText.StopAddS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //if (frmMain.pauseFlag)
            //{
            //    MessageBox.Show("当前供应品缺乏，请保证供应品充足后在进行实验操作！", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            if (frmMain.StopFlag[0] || frmMain.StopFlag[1] || frmMain.StopFlag[2] || frmMain.StopFlag[3] || TubeStop)//|| SubstrateStop
            {
                MessageBox.Show(getString("keywordText.LackSupplies"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //while (AddingSampleFlag)
            //{
            //    NetCom3.Delay(10);//如果正在加样步骤，暂时先不会弹出样本装载界面
            //}

            if (AddingSampleFlag || RetestFlag)
            {
                MessageBox.Show(getString("keywordText.StopAddingS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (bReagentDiuLess || StopList.Count > 0)
            {
                MessageBox.Show(getString("keywordText.bReagentDiuLess"), getString("keywordText.Tips"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            frmSampleLoad.SubstrateLeft = GetSunLeft();
            if (frmSampleLoad.SubstrateLeft <= 0)
            {
                MessageBox.Show(getString("keywordText.SubLessNoAddS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            #endregion
            fbtnAddE.Enabled = false;
            EmergencyFlag = true;//2018-10-15 
            frmSampleLoad.DtItemInfoNoStat = GetNoAddLiquid().Copy();
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
                frmSL.BringToFront();
            }
            fbtnAddE.Enabled = true;
        }
        /// <summary>
        /// 获取底物可检测实验数量
        /// </summary>
        /// <returns></returns>
        public int GetSunLeft()
        {
            string LeftCount1 = OperateIniFile.ReadIniData("Substrate1", "LeftCount", "", iniPathSubstrateTube);
            int TestItemcount = dgvWorkListData.RowCount;
            if (LeftCount1 == "") LeftCount1 = "0";
            return int.Parse(LeftCount1) - (TestItemcount - completeTestNums);
        }
        private void fbtnAddS_Click(object sender, EventArgs e)
        {
            #region 提示
            if (Convert.ToInt16(TimeLabel2.Text) * 60 + Convert.ToInt16(TimeLabel3.Text) < 12)
            {
                MessageBox.Show(getString("keywordText.StopAddS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //if (frmMain.pauseFlag)
            //{
            //    MessageBox.Show("当前供应品缺乏，请保证供应品充足后在进行实验操作！", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            if (frmMain.StopFlag[0] || frmMain.StopFlag[1] || frmMain.StopFlag[2] || frmMain.StopFlag[3] || TubeStop)//|| SubstrateStop
            {
                MessageBox.Show(getString("keywordText.LackSupplies"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (AddingSampleFlag)
            {
                MessageBox.Show(getString("keywordText.StopAddingS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (bReagentDiuLess || StopList.Count > 0)
            {
                MessageBox.Show(getString("keywordText.bReagentDiuLess"), getString("keywordText.Tips"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            frmSampleLoad.SubstrateLeft = GetSunLeft();
            if (frmSampleLoad.SubstrateLeft <= 0)
            {
                MessageBox.Show(getString("keywordText.SubLessNoAddS"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            #endregion
            fbtnAddS.Enabled = false;
            addOrdinaryFlag = true;//2018-10-18 zlx mod
            frmSampleLoad.DtItemInfoNoStat = GetNoAddLiquid().Copy();
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
                frmSL.BringToFront();
            }
            fbtnAddS.Enabled = true;
        }
        /// <summary>
        /// 获取未开始的实验需要的试剂和样本
        /// </summary>
        /// <returns></returns>
        public DataTable GetNoAddLiquid()
        {
            DataTable dtAddliquid = frmSampleLoad.DtItemInfoNoStat.Clone();
            List<TestSchedule> list = lisTestSchedule.FindAll(tx => (tx.TestID >= NoStartTestId && tx.StartTime > 0 && tx.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube));
            var ItemNames = list.GroupBy(x => x.ItemName).Where(x => x.Count() >= 1).ToList();
            for (int i = 0; i < ItemNames.Count; i++)
            {
                string ItemName = ItemNames[i].Key.ToString();
                DbHelperOleDb db = new DbHelperOleDb(0);
                DataTable dtDilute = DbHelperOleDb.Query(0, @"select DiluteCount,DiluteName from tbProject where ShortName ='" + ItemName + "'").Tables[0];
                List<TestSchedule> ItemNamelist = list.FindAll(tx => (tx.ItemName == ItemName));
                int LeftDiu = 0;
                int leftRg = 0;
                int DiluteCount = int.Parse(dtDilute.Rows[0]["DiluteCount"].ToString());
                string DiluteName = dtDilute.Rows[0]["DiluteName"].ToString();
                foreach (TestSchedule test in ItemNamelist)
                {
                    leftRg++;
                    DataRow[] drRunInfo = dtSampleRunInfo.Select("SampleNo='" + test.SampleNo + "' AND ItemName='" + ItemName + "'");
                    if (!drRunInfo[0]["SampleType"].ToString().Contains(getString("keywordText.Standard")) && !drRunInfo[0]["SampleType"].ToString().Contains(getString("keywordText.Control")))
                    {
                        if (int.Parse(test.dilutionTimes) > 1)
                        {
                            int ExtraDiluteC = int.Parse(test.dilutionTimes) / DiluteCount;
                            if (DiluteCount == 1)
                                DiluteName = DiuInfo.GetDiuInfo(ExtraDiluteC);
                            else
                                DiluteName = DiuInfo.GetDiuInfo(ExtraDiluteC) + ";" + DiluteName;
                            List<string> diuList = GetDiuVol(int.Parse(test.AddLiqud.Split('-')[0].ToString()), DiluteName);
                            for (int j = 0; j < diuList.Count; j++)
                            {
                                LeftDiu = LeftDiu + int.Parse(diuList[j].Split(';')[1]);
                            }
                        }
                    }
                }
                DataRow dr = dtAddliquid.NewRow();
                dr["RgName"] = ItemName;
                dr["TestRg"] = leftRg;
                dr["TestDiu"] = LeftDiu;
                dtAddliquid.Rows.Add(dr);
            }
            return dtAddliquid;
        }
        #region 删除生成的工作列表 lyn add 2018.06.16
        private void fbtnDelTest_Click(object sender, EventArgs e)
        {
            NetCom3.ComWait.Reset();
            if (AllowDel())
            {
                LoadingHelper.ShowLoadingScreen();
                //#region 原有数据删除
                //string[] dgvSelectedID = new string[dgvWorkListData.SelectedRows.Count];
                //for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
                //{
                //    dgvSelectedID[m] = dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString();
                //}
                #region 原有数据删除
                string[] dgvSelectedID = new string[dgvWorkListData.SelectedRows.Count];
                //Jun add
                DbHelperOleDb db = new DbHelperOleDb(1);
                for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
                {
                    string dgvSelectedid = dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString();
                    string SampleNo = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                    string ItemName = dgvWorkListData.SelectedRows[m].Cells["ItemName"].Value.ToString();
                    lisTestSchedule.RemoveAll(ty => ty.TestID == int.Parse(dgvSelectedid));
                    List<TestSchedule> LeftSchedule = lisTestSchedule.FindAll(ty => ty.SampleNo == SampleNo);
                    if (LeftSchedule.Count > 0)
                    {
                        if (LeftSchedule.FindAll(ty => ty.ItemName == ItemName).Count > 0)
                        {
                            SampleNumCurrent--; //lyq 191021
                            if ((StopList.Count > 0) &&
                                        StopList.Contains(dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString()))
                            {
                                StopList.Remove(dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString());
                            }
                            continue;
                        }
                        else
                        {
                            var drr = dtSampleRunInfo.Select("SampleNo='" + SampleNo + "'AND ItemName='" + ItemName + "'");
                            if (drr.Length > 0)
                            {
                                dtSampleRunInfo.Rows.Remove(drr[0]);
                            }
                        }
                    }
                    else
                    {
                        var drr = dtSampleRunInfo.Select("SampleNo='" + SampleNo + "'AND ItemName='" + ItemName + "'");
                        if (drr.Length > 0)
                        {
                            dtSampleRunInfo.Rows.Remove(drr[0]);
                        }
                        db = new DbHelperOleDb(1);
                        int bDelete = DbHelperOleDb.ExecuteSql(1, @"delete from tbSampleInfo where Status =0 AND SampleNo='" + SampleNo + "'");
                        if (bDelete > 0)
                        {
                            var ddr = dtSpInfo.Select("SampleNo='" + SampleNo + "'");
                            if (ddr.Length > 0)
                            {
                                dtSpInfo.Rows.Remove(ddr[0]);
                            }
                        }
                        //var drRun = frmParent.dtSampleRunInfo.Select("SampleNo='" + dgvWorkListData.SelectedRows[m].Cells["No"].Value + "'");
                    }
                    //var drRun = frmParent.dtSampleRunInfo.Select("SampleNo='" + dgvWorkListData.SelectedRows[m].Cells["No"].Value + "'AND ItemName=");
                    SampleNumCurrent--;
                    if ((StopList.Count > 0) &&
                                StopList.Contains(dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString()))
                    {
                        StopList.Remove(dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString());
                    }
                    #region 屏蔽原有代码
                    /*
                    int bDelete = 0;
                    dgvSelectedID[m] = dgvWorkListData.SelectedRows[m].Cells["No"].Value.ToString();

                    bDelete = DbHelperOleDb.ExecuteSql(@"delete from tbSampleInfo where Status =0 AND SampleNo='" + dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value + "'");

                    //bDelete = DbHelperOleDb.ExecuteSql(@"update tbSampleInfo set Status = 2  where SampleNo='" + dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value + "'");

                    if (bDelete > 0)
                    {

                        string temp = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                        var drr = dtSpInfo.Select("SampleNo='" + temp + "'");
                        if (drr.Length > 0)
                        {
                            dtSpInfo.Rows.Remove(drr[0]);
                        }
                        var drRun = frmParent.dtSampleRunInfo.Select("SampleNo='" + temp + "'");
                        foreach (DataRow d in drRun)
                        {
                            frmParent.dtSampleRunInfo.Rows.Remove(d);
                        }
                    }
                     */
                    #endregion
                }
                //for (int n = 0; n < dgvSelectedID.Length; n++)
                //{
                //    lisTestSchedule.RemoveAll(ty => ty.TestID == int.Parse(dgvSelectedID[n]));
                //    SampleNumCurrent--;
                //}

                #endregion
                #region testid和温育盘位置重新赋值
                //按照testid进行排序
                lisTestSchedule.Sort(new SortEmergency());
                int OldAddSamPos = 3;
                DataTable dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
                //第一个有管的位置
                string FirstPos = "";
                //2018-08-30 zlx mod
                DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum.ToString() + "'");
                if (Convert.ToInt32(dr[0][1]) == 1)
                {
                    for (int i = dtReactTrayInfo.Rows.Count - 1; i >= 0; i--)
                    {
                        if (dtReactTrayInfo.Rows[i][1].ToString() != "1")
                        {
                            FirstPos = dtReactTrayInfo.Rows[i + 1][0].ToString();
                            break;
                        }

                    }
                }
                else
                {
                    for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                    {
                        if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                        {
                            //后一个值一直覆盖前一个最终的赋值为最后一个位置
                            FirstPos = dtReactTrayInfo.Rows[i][0].ToString();
                            if (FirstPos == "no1" || FirstPos == "no2" || FirstPos == "no3")
                                continue;
                            //2018-06-12 zlx mod
                            if (FirstPos == "no4")
                            {
                                for (int num = dtReactTrayInfo.Rows.Count - 1; num > 0; num--)
                                {
                                    if (dtReactTrayInfo.Rows[num][1].ToString() != "0")
                                    {
                                        if (num == ReactTrayHoleNum - 1)
                                            FirstPos = dtReactTrayInfo.Rows[3][0].ToString();
                                        else
                                        {
                                            if (num + 1 - toUsedTube > 4)//dtReactTrayInfo.Rows[num + 1 - 15][0].ToString()!= FirstPos
                                                FirstPos = dtReactTrayInfo.Rows[num + 1][0].ToString();
                                        }
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }

                //for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                //{
                //    if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                //    {
                //        //后一个值一直覆盖前一个最终的赋值为最后一个位置
                //        FirstPos = dtReactTrayInfo.Rows[i][0].ToString();
                //        if (FirstPos == "no4")
                //        {
                //            for (int num = dtReactTrayInfo.Rows.Count - 1; num > 0; num--)
                //            {
                //                if (dtReactTrayInfo.Rows[num][1].ToString() == "0")
                //                {
                //                    if (num == ReactTrayHoleNum - 1)
                //                        FirstPos = dtReactTrayInfo.Rows[3][0].ToString();
                //                    else
                //                        FirstPos = dtReactTrayInfo.Rows[num + 1][0].ToString();
                //                    break;
                //                }
                //            }
                //        }
                //        break;
                //    }
                //}
                if (FirstPos == "")
                    OldAddSamPos = 3;
                else
                    OldAddSamPos = int.Parse(FirstPos.Substring(2)) - 1;
                if (NoStartTestId != 1)
                {
                    //被延后的反应管的所在位置
                    OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestId - 1 &&
                        ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube).AddSamplePos;

                }
                List<TestSchedule> lisNoStartSchedule = lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestId);
                lisTestSchedule.RemoveAll(tx => tx.TestID >= NoStartTestId);
                //testid赋值
                int initID = NoStartTestId;
                for (int i = 1; i <= dgvWorkListData.Rows.Count; i++)
                {
                    List<TestSchedule> TempTestSchedule = lisNoStartSchedule.FindAll(tx => tx.TestID == i);
                    if (TempTestSchedule.Count == 0)
                    {
                        continue;
                    }
                    for (int j = 0; j < TempTestSchedule.Count; j++)
                    {
                        TempTestSchedule[j].TestID = initID;
                    }
                    initID++;
                }
                //温育盘位置赋值
                for (int i = 0; i < lisNoStartSchedule.Count; i++)
                {
                    TestSchedule TempTestSchedule = lisNoStartSchedule[i];
                    if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                    {
                        //稀释实验赋值
                        if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                        {
                            string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                            if (diupos.Length > 1)
                            {
                                string newpos = "";
                                for (int j = 0; j < diupos.Length; j++)
                                {
                                    if (newpos == "")
                                    {
                                        newpos = "1";
                                    }
                                    else
                                    {
                                        newpos += "-" + (j + 1).ToString(); ;
                                    }
                                }
                                TempTestSchedule.dilutionPos = newpos;

                            }
                            else
                            {
                                TempTestSchedule.dilutionPos = "1";
                            }
                            string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                            TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                            OldAddSamPos = OldAddSamPos + 1;
                            if (OldAddSamPos > ReactTrayHoleNum)
                            {
                                OldAddSamPos = 4;
                            }
                            TempTestSchedule.AddSamplePos = OldAddSamPos;
                        }
                        else
                        {
                            OldAddSamPos = OldAddSamPos + 1;
                            if (OldAddSamPos > ReactTrayHoleNum)
                            {
                                OldAddSamPos = 4;
                            }
                            TempTestSchedule.AddSamplePos = OldAddSamPos;
                            TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                        }
                    }
                }
                #endregion
                #region 控件清空还未开始运行的进度条控件
                while (dgvWorkListData.Controls.Count > 2)
                {
                    this.dgvWorkListData.Controls.Clear();//清除已有的控件
                }
                for (int i = BTestItem.Count - 1; i >= NoStartTestId - 1; i--)
                {
                    BTestItem.Remove(BTestItem[i]);
                }
                //清空之前获取已经开始实验的进度条控件
                for (int i = lisProBar.Count - 1; i >= NoStartTestId - 1; i--)
                {
                    lisProBar.Remove(lisProBar[i]);
                }
                #endregion
                //将删除后未开始运行的样本进度附加到datagridview中
                BindData(lisNoStartSchedule, NoStartTestId);
                #region 进度重新计算
                //获取已经开始运行的样本的进度表
                List<TestSchedule> RunSchedule = lisTestSchedule;
                List<string> runFreeTime = GetFreeTime(RunSchedule);
                //未运行的样本进度计算
                List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisNoStartSchedule, runFreeTime);
                lisTestSchedule.AddRange(lisTestNoRun);
                lisTestSchedule.Sort(new SortRun());
                #endregion
            }
            #region 重新为倒计时进行赋值
            List<TestSchedule> tss = lisTestSchedule.FindAll(tx => tx.StartTime <= sumTime);
            if (tss.Count != 0)
            {
                _GaDoingOne = tss[tss.Count - 1];
                TestStep = GaNextOne();
            }
            LoadingHelper.CloseForm();
            NetCom3.ComWait.Set();
            MaxTime = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            LastMaxTime = (int)MaxTime;
            MaxTime = MaxTime - sumTime;
            MaxTime *= PiusTimes;
            LastSumTime = sumTime;
            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            TimeLabel2.Invoke(new Action(() =>
            {
                TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                TimeLabel3.Text = span.Minutes.ToString("00");
                TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = true;
            }));
            stopTimer.Start();//倒计时继续
            #endregion
            //StopWatchAddOrSubtractEmergency();//加急诊之后增加倒计时,或者减去试验后减少倒计时

            LeftTestTime = Convert.ToInt32(TimeLabel2.Text) * 60 + Convert.ToInt32(TimeLabel3.Text);
        }

        private void dgvWorkListData_SelectionChanged(object sender, EventArgs e)
        {
            //if (dgvWorkListData.SelectedRows.Count > 0)
            //{
            //    fbtnDelTest.Enabled = true;
            //}
            //else
            //{
            //    fbtnDelTest.Enabled = false;
            //}
        }
        /// <summary>
        /// 是否允许删除选中的行
        /// </summary>
        /// <returns>true为允许；false为不允许</returns>
        bool AllowDel()
        {
            for (int i = 0; i < dgvWorkListData.SelectedRows.Count; i++)
            {
                if (dgvWorkListData.SelectedRows[i].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.Standard"))
                    || dgvWorkListData.SelectedRows[i].Cells["SampleType"].Value.ToString().Contains(getString("keywordText.CalibrationSolution")))
                {
                    frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.SelectStandards"));
                    return false;
                }
                if (dgvWorkListData.SelectedRows[i].Cells["TestStatus"].Value.ToString() != "")
                {
                    frmMsgShow.MessageShow(getString("btnWorkList.Text"), getString("keywordText.SelectRun"));
                    return false;
                }
            }
            return true;
        }
        #endregion
        private string getString(string key)
        {
            ResourceManager resManager = new ResourceManager(typeof(frmWorkList));
            return resManager.GetString(key).Replace(@"\n", "\n");
        }
        private void btnWorkList_Click(object sender, EventArgs e)
        {

        }

        private void dgvWorkListData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
        private void btnPatientInfo_Click(object sender, EventArgs e)
        {
            #region 提示
            if (dgvWorkListData.SelectedRows.Count < 1)
            {
                MessageBox.Show(getString("keywordText.SelectSample"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (IsSelectedRowsExitStandard())
            {
                MessageBox.Show(getString("keywordText.Operationforbidden"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            #endregion

            #region 设置
            string sampleNo = dgvWorkListData.SelectedRows[0].Cells["SampleNo"].Value.ToString();
            string sampleID = dgvWorkListData.SelectedRows[0].Cells["SampleID"].Value.ToString();

            frmPatientInfo frmPI = new frmPatientInfo();
            frmPI.SampleID = int.Parse(sampleID);
            frmPI.LoginGName = LoginGName;
            if (frmPI.ShowDialog() != DialogResult.OK) return;
            #endregion
        }

        bool IsSelectedRowsExitStandard()
        {
            bool isExitStandard = false;
            for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
            {
                string sampleType = dgvWorkListData.SelectedRows[m].Cells["SampleType"].Value.ToString();

                if (sampleType.Contains(getString("keywordText.Standard")) || sampleType.Contains(getString("keywordText.Control")) || sampleType.Contains(getString("keywordText.Calibrator")) || sampleType.Contains(getString("keywordText.CalibrationSolution"))) return true;
            }

            return isExitStandard;
        }

        private void fbtnToEmergency_Click(object sender, EventArgs e)
        {
            #region 提示
            if (dgvWorkListData.SelectedRows.Count < 1)
            {
                MessageBox.Show(getString("keywordText.SelectSample"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (IsSelectedRowsExitStandard())
            {
                MessageBox.Show(getString("keywordText.Operationforbidden"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
            {
                string SampleNo = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                List<TestSchedule> LeftSchedule = lisTestSchedule.FindAll(ty => ty.SampleNo == SampleNo);

                if (LeftSchedule.Where(item => item.Emergency >= 4).Count() > 0)
                {
                    MessageBox.Show(getString("keywordText.Operationforbidden"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            #endregion

            NetCom3.ComWait.Reset();

            LoadingHelper.ShowLoadingScreen();

            #region 原有数据删除
            string[] dgvSelectedID = new string[dgvWorkListData.SelectedRows.Count];
            DbHelperOleDb db = new DbHelperOleDb(1);
            for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
            {
                string SampleNo = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                List<TestSchedule> LeftSchedule = lisTestSchedule.FindAll(ty => ty.SampleNo == SampleNo);

                foreach (var item in LeftSchedule)
                {
                    item.Emergency = 4;//变成急诊类型4
                }
            }
            #endregion

            //按照testid进行排序
            lisTestSchedule.Sort(new SortEmergencyByEmergency());
            lisTestSchedule.SetChangeEmergencyTestID(NoStartTestId);

            #region testid和温育盘位置重新赋值
            int OldAddSamPos = 3;
            DataTable dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            //第一个有管的位置
            string FirstPos = "";
            DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum.ToString() + "'");
            if (Convert.ToInt32(dr[0][1]) == 1)
            {
                for (int i = dtReactTrayInfo.Rows.Count - 1; i >= 0; i--)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() != "1")
                    {
                        FirstPos = dtReactTrayInfo.Rows[i + 1][0].ToString();
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                    {
                        //后一个值一直覆盖前一个最终的赋值为最后一个位置
                        FirstPos = dtReactTrayInfo.Rows[i][0].ToString();
                        if (FirstPos == "no1" || FirstPos == "no2" || FirstPos == "no3")
                            continue;
                        if (FirstPos == "no4")
                        {
                            for (int num = dtReactTrayInfo.Rows.Count - 1; num > 0; num--)
                            {
                                if (dtReactTrayInfo.Rows[num][1].ToString() != "0")
                                {
                                    if (num == ReactTrayHoleNum - 1)
                                        FirstPos = dtReactTrayInfo.Rows[3][0].ToString();
                                    else
                                    {
                                        if (num + 1 - toUsedTube > 4)//dtReactTrayInfo.Rows[num + 1 - 15][0].ToString()!= FirstPos
                                            FirstPos = dtReactTrayInfo.Rows[num + 1][0].ToString();
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            if (FirstPos == "")
                OldAddSamPos = 3;
            else
                OldAddSamPos = int.Parse(FirstPos.Substring(2)) - 1;
            if (NoStartTestId != 1)
            {
                //被延后的反应管的所在位置
                OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestId - 1 &&
                    ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube).AddSamplePos;
            }
            List<TestSchedule> lisNoStartSchedule = lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestId);
            lisTestSchedule.RemoveAll(tx => tx.TestID >= NoStartTestId);

            //温育盘位置赋值
            for (int i = 0; i < lisNoStartSchedule.Count; i++)
            {
                TestSchedule TempTestSchedule = lisNoStartSchedule[i];
                if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                {
                    //稀释实验赋值
                    if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                    {
                        string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                        if (diupos.Length > 1)
                        {
                            string newpos = "";
                            for (int j = 0; j < diupos.Length; j++)
                            {
                                if (newpos == "")
                                {
                                    newpos = "1";
                                }
                                else
                                {
                                    newpos += "-" + (j + 1).ToString(); ;
                                }
                            }
                            TempTestSchedule.dilutionPos = newpos;
                        }
                        else
                        {
                            TempTestSchedule.dilutionPos = "1";
                        }
                        string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                        TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                        OldAddSamPos = OldAddSamPos + 1;
                        if (OldAddSamPos > ReactTrayHoleNum)
                        {
                            OldAddSamPos = 4;
                        }
                        TempTestSchedule.AddSamplePos = OldAddSamPos;
                    }
                    else
                    {
                        OldAddSamPos = OldAddSamPos + 1;
                        if (OldAddSamPos > ReactTrayHoleNum)
                        {
                            OldAddSamPos = 4;
                        }
                        TempTestSchedule.AddSamplePos = OldAddSamPos;
                        TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                    }
                }
            }
            #endregion
            #region 控件清空还未开始运行的进度条控件
            while (dgvWorkListData.Controls.Count > 2)
            {
                this.dgvWorkListData.Controls.Clear();//清除已有的控件
            }
            for (int i = BTestItem.Count - 1; i >= NoStartTestId - 1; i--)
            {
                BTestItem.Remove(BTestItem[i]);
            }
            //清空之前获取已经开始实验的进度条控件
            for (int i = lisProBar.Count - 1; i >= NoStartTestId - 1; i--)
            {
                lisProBar.Remove(lisProBar[i]);
            }
            #endregion
            BindData(lisNoStartSchedule, NoStartTestId);
            #region 进度重新计算
            //获取已经开始运行的样本的进度表
            List<TestSchedule> RunSchedule = lisTestSchedule;
            List<string> runFreeTime = GetFreeTime(RunSchedule);
            //未运行的样本进度计算
            List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisNoStartSchedule, runFreeTime);
            lisTestSchedule.AddRange(lisTestNoRun);
            lisTestSchedule.Sort(new SortRun());
            #endregion

            #region 重新为倒计时进行赋值
            List<TestSchedule> tss = lisTestSchedule.FindAll(tx => tx.StartTime <= sumTime);
            if (tss.Count != 0)
            {
                _GaDoingOne = tss[tss.Count - 1];
                TestStep = GaNextOne();
            }
            LoadingHelper.CloseForm();
            NetCom3.ComWait.Set();
            MaxTime = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            LastMaxTime = (int)MaxTime;
            MaxTime = MaxTime - sumTime;
            MaxTime *= PiusTimes;
            LastSumTime = sumTime;
            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            TimeLabel2.Invoke(new Action(() =>
            {
                TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                TimeLabel3.Text = span.Minutes.ToString("00");
                TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = true;
            }));
            stopTimer.Start();//倒计时继续
            #endregion
        }

        private void fbtnToEmergency_Click_1(object sender, EventArgs e)
        {
            #region 提示
            if (dgvWorkListData.SelectedRows.Count < 1)
            {
                MessageBox.Show(getString("keywordText.SelectSample"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (IsSelectedRowsExitStandard())
            {
                MessageBox.Show(getString("keywordText.Operationforbidden"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
            {
                string SampleNo = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                List<TestSchedule> LeftSchedule = lisTestSchedule.FindAll(ty => ty.SampleNo == SampleNo);

                if (LeftSchedule.Where(item => item.Emergency >= 4).Count() > 0)
                {
                    MessageBox.Show(getString("keywordText.Operationforbidden"), getString("keywordText.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            #endregion

            NetCom3.ComWait.Reset();

            LoadingHelper.ShowLoadingScreen();

            #region 原有数据删除
            string[] dgvSelectedID = new string[dgvWorkListData.SelectedRows.Count];
            DbHelperOleDb db = new DbHelperOleDb(1);
            for (int m = 0; m < dgvWorkListData.SelectedRows.Count; m++)
            {
                string SampleNo = dgvWorkListData.SelectedRows[m].Cells["SampleNo"].Value.ToString();
                List<TestSchedule> LeftSchedule = lisTestSchedule.FindAll(ty => ty.SampleNo == SampleNo);

                foreach (var item in LeftSchedule)
                {
                    item.Emergency = 4;//变成急诊类型4
                }
            }
            #endregion

            //按照testid进行排序
            lisTestSchedule.Sort(new SortEmergencyByEmergency());
            lisTestSchedule.SetChangeEmergencyTestID(NoStartTestId);

            #region testid和温育盘位置重新赋值
            int OldAddSamPos = 3;
            DataTable dtReactTrayInfo = OperateIniFile.ReadConfig(iniPathReactTrayInfo);
            //第一个有管的位置
            string FirstPos = "";
            DataRow[] dr = dtReactTrayInfo.Select("Pos='no" + ReactTrayNum.ToString() + "'");
            if (Convert.ToInt32(dr[0][1]) == 1)
            {
                for (int i = dtReactTrayInfo.Rows.Count - 1; i >= 0; i--)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() != "1")
                    {
                        FirstPos = dtReactTrayInfo.Rows[i + 1][0].ToString();
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < dtReactTrayInfo.Rows.Count; i++)
                {
                    if (dtReactTrayInfo.Rows[i][1].ToString() == "1")
                    {
                        //后一个值一直覆盖前一个最终的赋值为最后一个位置
                        FirstPos = dtReactTrayInfo.Rows[i][0].ToString();
                        if (FirstPos == "no1" || FirstPos == "no2" || FirstPos == "no3")
                            continue;
                        if (FirstPos == "no4")
                        {
                            for (int num = dtReactTrayInfo.Rows.Count - 1; num > 0; num--)
                            {
                                if (dtReactTrayInfo.Rows[num][1].ToString() != "0")
                                {
                                    if (num == ReactTrayHoleNum - 1)
                                        FirstPos = dtReactTrayInfo.Rows[3][0].ToString();
                                    else
                                    {
                                        if (num + 1 - toUsedTube > 4)//dtReactTrayInfo.Rows[num + 1 - 15][0].ToString()!= FirstPos
                                            FirstPos = dtReactTrayInfo.Rows[num + 1][0].ToString();
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            if (FirstPos == "")
                OldAddSamPos = 3;
            else
                OldAddSamPos = int.Parse(FirstPos.Substring(2)) - 1;
            if (NoStartTestId != 1)
            {
                //被延后的反应管的所在位置
                OldAddSamPos = lisTestSchedule.Find(ty => ty.TestID == NoStartTestId - 1 &&
                    ty.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube).AddSamplePos;
            }
            List<TestSchedule> lisNoStartSchedule = lisTestSchedule.FindAll(tx => tx.TestID >= NoStartTestId);
            lisTestSchedule.RemoveAll(tx => tx.TestID >= NoStartTestId);

            //温育盘位置赋值
            for (int i = 0; i < lisNoStartSchedule.Count; i++)
            {
                TestSchedule TempTestSchedule = lisNoStartSchedule[i];
                if (TempTestSchedule.TestScheduleStep == TestSchedule.ExperimentScheduleStep.AddLiquidTube)
                {
                    //稀释实验赋值
                    if (int.Parse(TempTestSchedule.dilutionTimes) > 1)
                    {
                        string[] diupos = TempTestSchedule.dilutionPos.Split('-');
                        if (diupos.Length > 1)
                        {
                            string newpos = "";
                            for (int j = 0; j < diupos.Length; j++)
                            {
                                if (newpos == "")
                                {
                                    newpos = "1";
                                }
                                else
                                {
                                    newpos += "-" + (j + 1).ToString(); ;
                                }
                            }
                            TempTestSchedule.dilutionPos = newpos;
                        }
                        else
                        {
                            TempTestSchedule.dilutionPos = "1";
                        }
                        string[] temppos = TempTestSchedule.dilutionPos.Split('-');
                        TempTestSchedule.getSamplePos = "R" + temppos[temppos.Length - 1];
                        OldAddSamPos = OldAddSamPos + 1;
                        if (OldAddSamPos > ReactTrayHoleNum)
                        {
                            OldAddSamPos = 4;
                        }
                        TempTestSchedule.AddSamplePos = OldAddSamPos;
                    }
                    else
                    {
                        OldAddSamPos = OldAddSamPos + 1;
                        if (OldAddSamPos > ReactTrayHoleNum)
                        {
                            OldAddSamPos = 4;
                        }
                        TempTestSchedule.AddSamplePos = OldAddSamPos;
                        TempTestSchedule.getSamplePos = "S" + TempTestSchedule.samplePos;
                    }
                }
            }
            #endregion
            #region 控件清空还未开始运行的进度条控件
            while (dgvWorkListData.Controls.Count > 2)
            {
                this.dgvWorkListData.Controls.Clear();//清除已有的控件
            }
            for (int i = BTestItem.Count - 1; i >= NoStartTestId - 1; i--)
            {
                BTestItem.Remove(BTestItem[i]);
            }
            //清空之前获取已经开始实验的进度条控件
            for (int i = lisProBar.Count - 1; i >= NoStartTestId - 1; i--)
            {
                lisProBar.Remove(lisProBar[i]);
            }
            #endregion
            BindData(lisNoStartSchedule, NoStartTestId);
            #region 进度重新计算
            //获取已经开始运行的样本的进度表
            List<TestSchedule> RunSchedule = lisTestSchedule;
            List<string> runFreeTime = GetFreeTime(RunSchedule);
            //未运行的样本进度计算
            List<TestSchedule> lisTestNoRun = ExperimentalScheduleAlgorithm(lisNoStartSchedule, runFreeTime);
            lisTestSchedule.AddRange(lisTestNoRun);
            lisTestSchedule.Sort(new SortRun());
            #endregion

            #region 重新为倒计时进行赋值
            List<TestSchedule> tss = lisTestSchedule.FindAll(tx => tx.StartTime <= sumTime);
            if (tss.Count != 0)
            {
                _GaDoingOne = tss[tss.Count - 1];
                TestStep = GaNextOne();
            }
            LoadingHelper.CloseForm();
            NetCom3.ComWait.Set();
            MaxTime = lisTestSchedule.Select(it => it.EndTime).ToList<int>().Max();
            LastMaxTime = (int)MaxTime;
            MaxTime = MaxTime - sumTime;
            MaxTime *= PiusTimes;
            LastSumTime = sumTime;
            TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(MaxTime));
            while (!this.IsHandleCreated)
            {
                Thread.Sleep(30);
            }
            TimeLabel2.Invoke(new Action(() =>
            {
                TimeLabel2.Text = ((int)span.TotalHours).ToString("00");
                TimeLabel3.Text = span.Minutes.ToString("00");
                TimeLabel2.Visible = TimeLabel1.Visible = TimeLabel3.Visible = label2.Visible = label3.Visible = true;
            }));
            stopTimer.Start();//倒计时继续
            #endregion
        }

        private void fbtnfrmResultQuery_Click(object sender, EventArgs e)
        {
            frmResultQuery frmRQ = new frmResultQuery();
            frmRQ.TopLevel = false;
            frmRQ.Parent = this.Parent;
            frmRQ.Show();
            frmRQ.BringToFront();
        }

    }
    public class TestResultInfo
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int TestID { get; set; }
        /// <summary>
        /// 光子值
        /// </summary>
        public int PMT { get; set; }
    }
}
