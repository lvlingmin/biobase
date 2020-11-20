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
using System.IO;

namespace BioBaseCLIA.Run
{
    public partial class frmLoadSu : frmSmallParent
    {
        /// <summary>
        /// 稀释液装载位置
        /// </summary>
        /// <summary>
        /// 试剂盘配置文件地址
        /// </summary>
        string iniPathReagentTrayInfo = Directory.GetCurrentDirectory() + "\\ReagentTrayInfo.ini";
        public int RegentPos { get; set; }
        BLL.tbDilute bllsb = new BLL.tbDilute();
        DataTable dtSb = new DataTable();
  
        frmMessageShow frmMsgShow = new frmMessageShow();
        public frmLoadSu()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 规格体积
        /// </summary>
        private int AllDiuVol { get; set; }
        /// <summary>
        /// 剩余体积
        /// </summary>
        private int LeftDiuVol { get; set; }
        private void frmLoadSu_Load(object sender, EventArgs e)
        {
            cmdPos.SelectedIndex =cmbUnit1.SelectedIndex = cmbUnit2.SelectedIndex = 0;
            cmdPos_SelectedIndexChanged(sender, e);
        }
       
      private void btnChangeSubstrate_Click(object sender, EventArgs e)
      {
         
         if (btnChangeSubstrate.Text == "更换")//add by y 20180509
         {
             txtDiluteNumber.Text = "";//2018-10-18 zlx add
             txtSubstrateAllTest.Text = "0";//2018-10-18 zlx add
             txtSubstrateLastTest.Text = "0";//2018-10-18 zlx add
             ValidDate.Value = DateTime.Now.Date.AddMonths(1);//2018-10-17 zlx add
             txtSubstrateAllTest.Enabled = txtDiluteNumber.Enabled = txtSubstrateLastTest.Enabled = true;
             btnLoadSubstrate.Enabled = true;
             btnChangeSubstrate.Text = "取消";//add by y 20180509
         }
         else//add by y 20180509
         {
             txtSubstrateAllTest.Enabled = txtDiluteNumber.Enabled = txtSubstrateLastTest.Enabled = false;//add by y 20180509
             btnLoadSubstrate.Enabled = false;//add by y 20180509
             btnChangeSubstrate.Enabled = true;//add by y 20180509
             btnChangeSubstrate.Text = "更换";//add by y 20180509
             frmLoadSu_Load(null, null);//add by y 20180509
         }
      }

      private void btnLoadSubstrate_Click(object sender, EventArgs e)
      {
          
          if (int.Parse(txtSubstrateLastTest.Text) > int.Parse(txtSubstrateAllTest.Text))//add by y 20180509
          {
              frmMsgShow.MessageShow("稀释液装载", "剩余体积不应该大于规格体积！");//add by y 20180509
              txtSubstrateLastTest.Focus();//add by y 20180510
              frmLoadSu_Load(null, null);//add by y 20180509
              return;//add by y 20180509
          }
          if (cmdPos.SelectedItem.ToString() == "")//2019-03-29 zlx add
          {
              cmdPos.Focus();//add by y 20180510
              frmMsgShow.MessageShow("稀释液装载", "请输入稀释液的装载位置！");//add by y 20180510
              return;//y add 20180510
          }
          if (txtDiluteNumber.Text.Trim() == "")//add by y 20180510
          {
              txtDiluteNumber.Focus();//add by y 20180510
              frmMsgShow.MessageShow("稀释液装载", "请输入稀释液编码！");//add by y 20180510
              return;//y add 20180510
          }
          Model.tbDilute modelSb = new Model.tbDilute();
          DbHelperOleDb db = new DbHelperOleDb(3);
          DataTable dtAllSb = bllsb.GetAllList().Tables[0];
          var dr1 = dtAllSb.Select("DiluteNumber='" + txtDiluteNumber.Text.Trim() + "'");
          var dr2 = dtAllSb.Select("DilutePos = '" + RegentPos+'-'+cmdPos.SelectedItem.ToString() + "'");//2019-04-01 zlx add
          string[] SuInfo = new string[4];
          if (dr1.Length > 0)//原来数据库是否存在该条码，length大于0，则存在
          {
              frmMsgShow.MessageShow("稀释液装载", "该稀释液编号正在使用，请录入正确的编号！");
              txtSubstrateAllTest.Enabled = txtDiluteNumber.Enabled = txtSubstrateLastTest.Enabled = false;
              btnLoadSubstrate.Enabled = false;
              btnChangeSubstrate.Enabled = true;
              btnChangeSubstrate.Text = "更换";
              frmLoadSu_Load(null, null);//add by y 20180509
              return;
          }
          if (dr2.Length > 0)
          {
              DataTable dt1 = bllsb.GetList("State=1 and DilutePos ='" + RegentPos + "-" + cmdPos.SelectedItem.ToString() + "'").Tables[0];
              if (dt1.Rows.Count > 0)
                  bllsb.Delete(dt1.Rows[0]["DilutePos"].ToString());
          }
          if (cmbUnit1.SelectedItem.ToString() == "ml")
              AllDiuVol = int.Parse(txtSubstrateAllTest.Text) * 1000;
          else
              AllDiuVol = int.Parse(txtSubstrateAllTest.Text);
          if (cmbUnit2.SelectedItem.ToString() == "ml")
              LeftDiuVol = int.Parse(txtSubstrateLastTest.Text) * 1000;
          else
              LeftDiuVol = int.Parse(txtSubstrateLastTest.Text);
          modelSb.DiluteNumber = txtDiluteNumber.Text;
          modelSb.DilutePos = RegentPos +"-"+ cmdPos.SelectedItem.ToString();
          modelSb.AllDiuVol = AllDiuVol;
          modelSb.LeftDiuVol = LeftDiuVol;
          modelSb.Unit = "ul";
          modelSb.AddData = DateTime.Now.ToShortDateString();
          modelSb.ValiData = ValidDate.Value.ToShortDateString();
          modelSb.State = 1;
          if (bllsb.Add(modelSb))
          {
              if (cmdPos.SelectedIndex==0)//LeftReagent1
                  OperateIniFile.WriteIniData("ReagentPos" + RegentPos,"LeftReagent1", LeftDiuVol.ToString(), iniPathReagentTrayInfo);
              else if (cmdPos.SelectedIndex == 1)//LeftReagent1
                  OperateIniFile.WriteIniData("ReagentPos" + RegentPos, "LeftReagent2", LeftDiuVol.ToString(), iniPathReagentTrayInfo);
              else if(cmdPos.SelectedIndex==2)//LeftReagent1
                  OperateIniFile.WriteIniData("ReagentPos" + RegentPos,"LeftReagent3", LeftDiuVol.ToString(), iniPathReagentTrayInfo);
              frmMsgShow.MessageShow("供应品状态", "稀释液装载成功！");
          }
          txtSubstrateAllTest.Enabled = txtDiluteNumber.Enabled = txtSubstrateLastTest.Enabled = false;
          btnLoadSubstrate.Enabled = false;
          btnChangeSubstrate.Enabled = true;
          btnChangeSubstrate.Text = "更换";//add by y 20180509
      }

      private void functionButton1_Click(object sender, EventArgs e)
      {
          DbHelperOleDb db = new DbHelperOleDb(3);
          DataTable dtAllSb = bllsb.GetAllList().Tables[0];
          
          var dr2 = dtAllSb.Select("DilutePos = '" + RegentPos+'-'+cmdPos.SelectedItem.ToString() + "'");
          if (dr2.Length > 0)
          {
              DataTable dt1 = bllsb.GetList("State=1 and DilutePos ='"+RegentPos+"-"+cmdPos.SelectedItem.ToString()+"'").Tables[0];
              if (dt1.Rows.Count > 0)
              {
                  bllsb.Delete(dt1.Rows[0]["DilutePos"].ToString());
                  if (cmdPos.SelectedIndex == 0)//LeftReagent1
                      OperateIniFile.WriteIniData("ReagentPos" + RegentPos, "LeftReagent1","0", iniPathReagentTrayInfo);
                  else if (cmdPos.SelectedIndex == 1)//LeftReagent1
                      OperateIniFile.WriteIniData("ReagentPos" + RegentPos, "LeftReagent2","0", iniPathReagentTrayInfo);
                  else if (cmdPos.SelectedIndex == 2)//LeftReagent1
                      OperateIniFile.WriteIniData("ReagentPos" + RegentPos, "LeftReagent3","0", iniPathReagentTrayInfo);
                  //OperateIniFile.WriteIniData("ReagentPos" + RegentPos, "leftDiuVol", "0", iniPathReagentTrayInfo);
                  frmLoadSu_Load(null, null);
              }
          }

         
      }

      private void cmdPos_SelectedIndexChanged(object sender, EventArgs e)
      {
          DbHelperOleDb db = new DbHelperOleDb(3);
          string SRegentPos = txtRegentPos.Text +"-"+cmdPos.SelectedItem.ToString();
          dtSb = bllsb.GetList("State=1 and DilutePos = '" + SRegentPos + "'").Tables[0];
          if (dtSb.Rows.Count > 0)
          {
              txtDiluteNumber.Text = dtSb.Rows[0]["DiluteNumber"].ToString();
              AllDiuVol = int.Parse(dtSb.Rows[0]["AllDiuVol"].ToString());
              LeftDiuVol = int.Parse(dtSb.Rows[0]["LeftDiuVol"].ToString());
              if (cmbUnit1.SelectedItem.ToString() == "ml")
                  txtSubstrateAllTest.Text = (AllDiuVol / 1000).ToString();
              else
                  txtSubstrateAllTest.Text = AllDiuVol.ToString();
              if (cmbUnit2.SelectedItem.ToString() == "ml")
                  txtSubstrateLastTest.Text = (LeftDiuVol / 1000).ToString();
              else
                  txtSubstrateLastTest.Text = LeftDiuVol.ToString();
              ValidDate.Value = Convert.ToDateTime(dtSb.Rows[0]["ValiData"]);
          }
          else//add by y 20180509
          {
              txtRegentPos.Text = RegentPos.ToString();
              txtDiluteNumber.Text = "";
              txtSubstrateAllTest.Text = "0";
              txtSubstrateLastTest.Text = "0";
              ValidDate.Value = DateTime.Now.Date.AddMonths(1);
          }
            
      }

      private void cmbUnit2_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (cmbUnit1.SelectedItem!=null&&cmbUnit1.SelectedItem.ToString() == "ml")
            txtSubstrateAllTest.Text = (AllDiuVol / 1000).ToString();
        else
            txtSubstrateAllTest.Text = AllDiuVol.ToString();
         if (cmbUnit2.SelectedItem != null && cmbUnit2.SelectedItem.ToString() == "ml")
            txtSubstrateLastTest.Text = (LeftDiuVol / 1000).ToString();
        else
            txtSubstrateLastTest.Text = LeftDiuVol.ToString();
              
      }

    }
}
