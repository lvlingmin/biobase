using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BioBaseCLIA
{
    public partial class frmMessageShow : frmSmallParent
    {
        delegate void SetTextCallBack(string text);//修改控件属性块 20180524 zlx add
        public frmMessageShow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            
            this.Close();
        }
        public void MessageShow(string title, string content)
        {
            //20180524 zlx mod
            //this.Text = title;
            //this.lblMessage.Text = content;
            if (this.IsHandleCreated)
            {
                this.Invoke(new SetTextCallBack(SetText), content);
            }
            else
            {
                this.Text = title;
                this.lblMessage.Text = content;
            }
            this.Width = 12 * content.Length + 80;//根据提示信息长短确定弹窗大小。
            //this.Height = ;
            this.btnOK.Left = this.Width / 2 - 37;//设置弹窗中按钮的位置
            //2018-11-23 zlx add
            if (this.Text.Contains("警告"))
                StartKiller();
            this.ShowDialog();
        }
        public void MessageWrite(string title, string content)
        {
            Invoke(new SetTextCallBack(SetText), content);
        }
        /// <summary>
        /// 更改控件信息 20180524 zlx add
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        private void SetText(string content)
        {
            this.lblMessage.Text = content;
        }
        private void StartKiller()
        {
            Timer timer = new Timer();
            timer.Interval = 5000; //3秒启动 
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            btnOK_Click(null, null);
            //停止Timer 
            ((Timer)sender).Stop();
        } 
        //public DialogResult MessageConfirm(string title, string content)
        //{
        //    this.Text = title;
        //    this.lblMessage.Text = content;
        //    this.Width = 12 * content.Length + 80;//根据提示信息长短确定弹窗大小。
        //    //this.Height = ;
        //    this.btnOK.Left = this.Width / 2 - 37;//设置弹窗中按钮的位置
        //    return MessageBox.Show(MessageBoxButtons.OKCancel, MessageBoxIcon.Question); ;
        //}
    }
}
