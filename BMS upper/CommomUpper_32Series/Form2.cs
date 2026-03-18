using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace CommomUpper_32Series
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public string[] FaultWarnName = { "NA",
            "一级单节过压", "一级单节低压", "一级总电压过压", "一级总电压低压", "一级充电过流", "一级放电过流",
            "一级充电过温", "一级充电低温", "一级放电过温", "一级放电低温", "一级Mos过温", "一级压差过大", "一级电量过低",
            "二级单节过压", "二级单节低压", "二级总电压过压", "二级总电压低压", "二级充电过流", "二级放电过流",
            "二级充电过温", "二级充电低温", "二级放电过温", "二级放电低温", "二级Mos过温", "二级压差过大", "二级电量过低",
            "三级单节过压", "三级单节低压", "三级总电压过压", "三级总电压低压", "三级充电过流", "三级放电过流",
            "三级充电过温", "三级充电低温", "三级放电过温", "三级放电低温", "三级Mos过温", "三级压差过大", "三级电量过低"};

        public void Form2LabelLoad(byte[] FaultArg)
        {
            //label_aa.Text = str;
            textBox_Fault_First1.Text = FaultWarnName[FaultArg[0]];
            textBox_Fault_First2.Text = FaultWarnName[FaultArg[1]];
            textBox_Fault_First3.Text = FaultWarnName[FaultArg[2]];
            textBox_Fault_First4.Text = FaultWarnName[FaultArg[3]];

            textBox_Fault_Second1.Text = FaultWarnName[FaultArg[4]];
            textBox_Fault_Second2.Text = FaultWarnName[FaultArg[5]];
            textBox_Fault_Second3.Text = FaultWarnName[FaultArg[6]];
            textBox_Fault_Second4.Text = FaultWarnName[FaultArg[7]];

            textBox_Fault_Third1.Text = FaultWarnName[FaultArg[8]];
            textBox_Fault_Third2.Text = FaultWarnName[FaultArg[9]];
            textBox_Fault_Third3.Text = FaultWarnName[FaultArg[10]];
            textBox_Fault_Third4.Text = FaultWarnName[FaultArg[11]];
        }


        public string[] EventRecordName = { "NA",
            "BMS开机", "BMS休眠", "均衡开启", "加热开启", "冷凝开启",
            "单节过压保护", "总压过压保护", "充电过流保护",
            "单节低压保护", "总压低压保护", "放电过流保护",
            "充电低温保护", "放电低温保护", "充电高温保护", "放电高温保护", "压差过大保护",
            "短路保护", "AFE1报错", "AFE2报错", "EEPROM报错"};



        public void Form2_EventRecord(byte[,] Event_Recordd)
        {
            listView1.View = View.Details;     //可视化，加了这个才能看到
            listView1.Items.Clear();           //清空原来的

            listView1.Columns.Add(@"序号", 50);
            listView1.Columns.Add(@"事件内容", 150);
            listView1.Columns.Add(@"与上次间隔", 100);

            for (int i = 0; i < 100; ++i)
            {
                ListViewItem item = new ListViewItem();          //这两句话得放进来
                item.SubItems.Clear();
                //item.SubItems.Add(Convert.ToString(i + 1));    //这句话没用？得下面这么写
                item.Text = Convert.ToString(i + 1);


                if (Event_Recordd[i, 0] >= 21)           //添加的话，这里的范围要改
                {
                    item.SubItems.Add("溢出ERROR");
                    //item.SubItems.Add(Convert.ToString(Event_Recordd[i, 0]) + "溢出ERROR");
                }
                else
                {
                    item.SubItems.Add(EventRecordName[Event_Recordd[i, 0]]);
                }

                if (Event_Recordd[i, 1] == 0)
                {
                    item.SubItems.Add("NA");
                }
                else if (Event_Recordd[i, 1] == 171)
                {
                    item.SubItems.Add("1min以内");
                }
                else if (Event_Recordd[i, 1] <= 24)
                {
                    item.SubItems.Add(Convert.ToString(Event_Recordd[i, 1]) + "h");
                }
                else if (Event_Recordd[i, 1] <= 168)
                {
                    string temp = Convert.ToString(Event_Recordd[i, 1] / 24) + "d_" + Convert.ToString(Event_Recordd[i, 1] % 24) + "h";
                    item.SubItems.Add(temp);
                }
                else
                {
                    item.SubItems.Add("溢出ERROR");
                    //item.SubItems.Add(Convert.ToString(Event_Recordd[i, 1]) + "溢出ERROR");
                }

                //字体，颜色配置
                item.Font = new Font("宋体", 11, FontStyle.Bold);    //FontStyle.Italic斜体，FontStyle.Underline下划线
                if (i % 2 == 0)
                {
                    item.BackColor = Color.AliceBlue;
                }
                else
                {
                    item.BackColor = Color.AntiqueWhite;
                }

                listView1.Items.Add(item);
            }

            this.Controls.Add(listView1);
        }
    }
}

