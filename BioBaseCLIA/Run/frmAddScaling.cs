using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Maticsoft.DBUtility;
using BioBaseCLIA.ScalingQC;
using System.Text.RegularExpressions;

namespace BioBaseCLIA.Run
{
    public partial class frmAddScaling : frmSmallParent
    {
        private DataTable dtConcValue = new DataTable();
        BLL.tbMainScalCurve bllmsc = new BLL.tbMainScalCurve();
        Model.tbMainScalCurve modelMainScalcurve = new Model.tbMainScalCurve();
        public frmMessageShow frmMsg = new frmMessageShow(); 
        /// <summary>
        /// 项目名称
        /// </summary>
        private string itemName = "";
        /// <summary>
        /// 试剂批号
        /// </summary>
        private string regentBatch = "";
        /// <summary>
        /// 激活日期
        /// </summary>
        private string activeDate = "";
        /// <summary>
        /// 有效期
        /// </summary>
        private string validDate = "";
        /// <summary>
        /// 添加主曲线事件
        /// </summary>
        public static event Action AddMainCurve;
        public frmAddScaling()
        {
            InitializeComponent();
            dtConcValue.Columns.Add("No",typeof(string));
            dtConcValue.Columns.Add("Conc",typeof(string));
            dtConcValue.Columns.Add("Value",typeof(string));
        }
        public frmAddScaling(string name, string batch,string activedate,string validdate)
        {
            InitializeComponent();
            dtConcValue.Columns.Add("No", typeof(string));
            dtConcValue.Columns.Add("Conc", typeof(string));
            dtConcValue.Columns.Add("Value", typeof(string));
            itemName = name;
            regentBatch = batch;
            activeDate = activedate;
            validDate = validdate;
            

           
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            DbHelperOleDb db = new DbHelperOleDb(1);
            //查询是否存在相应项目名称和试剂批号的主曲线
            DataTable dt = bllmsc.GetList(" ItemName = '" + itemName + "' and RegentBatch = '" + regentBatch + "'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                frmMsg.MessageShow("添加曲线", "该批号试剂主曲线已输入过！");
                Close();
                return;
            }
            
            string pt="";
            //吸光度值
            string values = "";
            for(int i=0;i<dtConcValue.Rows.Count;i++)
            {
                pt += "(" + dtConcValue.Rows[i]["Conc"].ToString() + "," + dtConcValue.Rows[i]["Value"].ToString() + ");";
                values += dtConcValue.Rows[i]["Value"].ToString() + ",";
            }
            string[] value = values.Split(',');
            for (int i = 0; i < value.Length - 1; i++)
            {
                if (value[i] == "")
                {
                    frmMsg.MessageShow("添加曲线", "存在发光值为空的点，请重新输入！");
                    return;
                }
            }
            modelMainScalcurve.ItemName = itemName;
            modelMainScalcurve.RegentBatch = regentBatch;
            modelMainScalcurve.Points = pt;
            modelMainScalcurve.ValidPeriod =validDate==""?DateTime.Now.AddDays(30): DateTime.Parse(validDate);
            modelMainScalcurve.ActiveDate = activeDate == "" ? DateTime.Now : DateTime.Parse(activeDate);
            db = new DbHelperOleDb(1);
            if (bllmsc.Add(modelMainScalcurve))
            {
                frmMsg.MessageShow("添加曲线", "添加定标曲线成功！");
                if (AddMainCurve != null)
                {
                    AddMainCurve();
                }
                Close();
            }
            else
            {
                frmMsg.MessageShow("添加曲线", "添加定标曲线失败！");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmAddScaling_Load(object sender, EventArgs e)
        {
            txtRegentBatch.Text = regentBatch;
            BLL.tbProject bllProject = new BLL.tbProject();
            DbHelperOleDb db = new DbHelperOleDb(0);
            DataTable dt = bllProject.GetList(" ShortName='" + itemName + "'").Tables[0];
            for (int i = 0; i < 6; i++)
            {
                dtConcValue.Rows.Add(i + 1, dt.Rows[0]["CalPointConc"].ToString().Split(',')[i], "");
            }
            //2018-11-14 zlx mod
            db = new DbHelperOleDb(1);
            dt = bllmsc.GetList(" ItemName = '" + itemName + "' and RegentBatch = '" + regentBatch + "'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                string[] splitPoint = dt.Rows[0]["Points"].ToString().Replace("(", "").Replace(")", "").Split(';');
                foreach (string pointinfo in splitPoint)
                {
                    string[] point = pointinfo.Split(',');
                    for (int i = 0; i < dtConcValue.Rows.Count; i++)
                    {
                        if (dtConcValue.Rows[i][1].ToString() == point[0].ToString())
                        {
                            dtConcValue.Rows[i][2] = point[1];
                            //continue;
                        }
                    }
                }
            }
            dgvScaling.DataSource = dtConcValue;
        }

        /// <summary>
        /// 添加主曲线时控制发光值的输入 
        /// 2018-4-19 zlx add
        /// </summary>
        TextBox control;
        bool _bevent=false;
        private void dgvScaling_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            control = (TextBox)e.Control;
            if (control.Text == "" && !_bevent)
            {
                control.KeyPress += new KeyPressEventHandler(control_KeyPress);
                _bevent = true;
            }
        }
        void control_KeyPress(object sender, KeyPressEventArgs e)
        {
            //限制只能输入1-9的数字和退格键
            if (dgvScaling.CurrentCell.ColumnIndex == 2 )
            {
                if (((int)e.KeyChar >= 48 && (int)e.KeyChar <= 57) || e.KeyChar == 8)
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                    frmMsg.MessageShow("添加曲线", "只能输入数字！");
                }
            }
            
        }

       
    }
}
