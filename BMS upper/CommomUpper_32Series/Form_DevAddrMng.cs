using System;
using System.Windows.Forms;

namespace CommomUpper_32Series
{
    public partial class Form_DevAddrMng : Form
    {
        public Form_DevAddrMng()
        {
            InitializeComponent();
        }

        public void InfoFill()
        {
            textBox_PrztDevAddr.Text = "" + Form1.RS485_SLAVE_ADDR;
            textBox_NewDevAddr.Text = "";
            textBox_MnitDura.Text = "" + FormDAM_Info.mnitDura;
            textBox_MnitAddrStrt.Text = "" + FormDAM_Info.mnitAddrStrt;
            textBox_MnitAddrEnd.Text = "" + FormDAM_Info.mnitAddrEnd;

            if (FormDAM_Info.siglMnitSchema)
            {
                radioButton_SiglMnit.Checked = true;
                groupBox_MultMnitSetting.Enabled = false;
                textBox_NewDevAddr.Enabled = true;
            }
            else
            {
                radioButton_MultMnit.Checked = true;
                groupBox_MultMnitSetting.Enabled = true;
                textBox_NewDevAddr.Enabled = false;
            }
        }

        private void radioButton_SiglMnit_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_SiglMnit.Checked)
            {
                groupBox_MultMnitSetting.Enabled = false;
                textBox_NewDevAddr.Enabled = true;
            }
        }

        private void RadioButton_MultMnit_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_MultMnit.Checked)
            {
                groupBox_MultMnitSetting.Enabled = true;
                textBox_NewDevAddr.Enabled = false;
            }
        }

        private void button_AddrCfrm_Click(object sender, EventArgs e)
        {
            FormDAM_Info.prztDevAddr = Convert.ToByte(textBox_PrztDevAddr.Text);
            if (String.IsNullOrEmpty(textBox_NewDevAddr.Text))
                FormDAM_Info.newDevAddr = 0;
            else
                FormDAM_Info.newDevAddr = Convert.ToByte(textBox_NewDevAddr.Text);
            FormDAM_Info.mnitDura = Convert.ToUInt16(textBox_MnitDura.Text);
            FormDAM_Info.mnitAddrStrt = Convert.ToByte(textBox_MnitAddrStrt.Text);
            FormDAM_Info.mnitAddrEnd = Convert.ToByte(textBox_MnitAddrEnd.Text);

            if (radioButton_SiglMnit.Checked)
                FormDAM_Info.siglMnitSchema = true;
            else
                FormDAM_Info.siglMnitSchema = false;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button_AddrCacl_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    public static class FormDAM_Info
    {
        public static bool siglMnitSchema = true;    //true单机
        public static byte prztDevAddr = 0x01;
        public static byte newDevAddr = 0x01;
        public static ushort mnitDura = 10;
        public static byte mnitAddrStrt = 0x01;
        public static byte mnitAddrEnd = 0x05;
    }


    //问题解决，如果把这个放在前面，软件会自行把Form_DevAddrMng.Designer.cs的配置文件的
    //开头那个个partial class Form_DevAddrMng修改为partial class Form1，导致一系列上下文不存在该参数的错误
    partial class Form1
    {
        private void Button_DevAddrMng_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer_MultDev.Enabled = false;

            Form_DevAddrMng form_DevAddrMng = new Form_DevAddrMng();
            form_DevAddrMng.InfoFill();

            if (DialogResult.OK == form_DevAddrMng.ShowDialog())
            {
                if (FormDAM_Info.siglMnitSchema)
                {
                    if (FormDAM_Info.prztDevAddr != FormDAM_Info.newDevAddr
                        && FormDAM_Info.newDevAddr != 0)
                    {
                        settingDevAddr();
                        FormDAM_Info.prztDevAddr = FormDAM_Info.newDevAddr;
                        RS485_SLAVE_ADDR = FormDAM_Info.prztDevAddr;
                    }
                    else
                    {
                        RS485_SLAVE_ADDR = FormDAM_Info.prztDevAddr;
                    }
                }
                else
                {
                    RS485_SLAVE_ADDR = FormDAM_Info.prztDevAddr;

                    timer_MultDev.Interval = FormDAM_Info.mnitDura * 1000;
                    timer_MultDev.Tick += Timer_MultDev_Tick;
                    timer_MultDev.Enabled = true;
                }
            }
            else if (FormDAM_Info.siglMnitSchema) { timer_MultDev.Enabled = true; }

            timer1.Enabled = true;
        }

        private void settingDevAddr()
        {
            UInt16[] u_Params = { FormDAM_Info.newDevAddr };

            //填充要发送的数据
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_DEVADDR;
            u16Rs485RegNum = 1;
            bRs485ByteNum = 2;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void Timer_MultDev_Tick(object sender, EventArgs e)
        {
            byte by_Cnt = 0;
            timer1.Enabled = false;
            if ((++RS485_SLAVE_ADDR) > FormDAM_Info.mnitAddrEnd)
            {
                RS485_SLAVE_ADDR = FormDAM_Info.mnitAddrStrt;
            }
            while (!bl_RxFinishedFlag) { Delay_ms(10); if ((++by_Cnt) > 10) bl_RxFinishedFlag = true; }    //等待接收完成
            timer1.Enabled = true;
        }
    }
}
