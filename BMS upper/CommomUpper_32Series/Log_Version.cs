using System;
using System.Text;
using System.Windows.Forms;

namespace CommomUpper_32Series
{
    public partial class Form1
    {
        private void timer_Log_Tick(object sender, EventArgs e)
        {
            #region 日志变量
            string str =
                DateTime.Now.ToString() + "," +
                label_Err_CBC_CHG.Text + "," +
                label_Err_CBC_DSG.Text + "," +
                label_VC1 .Text + "," +
                label_VC2 .Text + "," +
                label_VC3 .Text + "," +
                label_VC4 .Text + "," +
                label_VC5 .Text + "," +
                label_VC6 .Text + "," +
                label_VC7 .Text + "," +
                label_VC8 .Text + "," +
                label_VC9 .Text + "," +
                label_VC10.Text + "," +
                label_VC11.Text + "," +
                label_VC12.Text + "," +
                label_VC13.Text + "," +
                label_VC14.Text + "," +
                label_VC15.Text + "," +
                label_VC16.Text + "," +
                label_VC17.Text + "," +
                label_VC18.Text + "," +
                label_VC19.Text + "," +
                label_VC20.Text + "," +
                label_VC21.Text + "," +
                label_VC22.Text + "," +
                label_VC23.Text + "," +
                label_VC24.Text + "," +
                label_VC25.Text + "," +
                label_VC26.Text + "," +
                label_VC27.Text + "," +
                label_VC28.Text + "," +
                label_VC29.Text + "," +
                label_VC30.Text + "," +
                label_VC31.Text + "," +
                label_VC32.Text + "," +
                label_Vbat.Text + "," +
                label_Ichg.Text + "," +
                label_Idsg.Text + "," +
                label_SOC.Text + "," +
                label_Present_mAh.Text + "," +
                label_chg_time.Text + "," +
                label_dsg_time.Text + "," +
                label_Fault_Third.Text + "," +
                label_SysStatus_Pre_MOS.Text + "," +
                label_SysStatus_CHG_MOS.Text + "," +
                label_SysStatus_DSG_MOS.Text + "," +
                label_Temp1.Text + "," +
                label_Temp2.Text + "," +
                label_Temp3.Text + "," +
                label_Temp4.Text + "," +
                label_Temp5.Text + "," +
                label_Temp6.Text + "," +
                label_TempEnv1.Text + "," +
                label_TempEnv2.Text + "," +
                label_TempEnv3.Text + "," +
                label_TempMos.Text + "," +
                label_Vcell_OV_Third.Text + "," +
                label_Vcell_UV_Third.Text + "," +
                label_Vbat_OV_Third.Text + "," +
                label_Vbat_UV_Third.Text + "," +
                label_CHG_OC_Third.Text + "," +
                label_DSG_OC_Third.Text + "," +
                label_Cellchg_OT_Third.Text + "," +
                label_Celldsg_OT_Third.Text + "," +
                label_Cellchg_UT_Third.Text + "," +
                label_Celldsg_UT_Third.Text + "," +
                label_Vdelta_Op_Third.Text + "," +
                label_Soc_Up_Third.Text + "," +
                label_Tmos_OTP_Third.Text;
            #endregion

            Log_Version.LogRecord(str, "./" + textBox_LogFileName.Text);
        }

        private void button_LogTimerStart_Click(object sender, EventArgs e)
        {
            if (timer_Log.Enabled == true)
            {
                MessageBox.Show("LogRecording Already Started", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int interval = 0;
                interval = Convert.ToInt32(textBox_LogTimer.Text);
                if (interval < 2 || interval > 3600)
                {
                    if (Lang.b_LangFlag == 0)
                    {
                        MessageBox.Show("日志记录时间间隔范围为 2 - 3600s，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("Log record interval range is 2 - 3600s, and the data is out of bounds！",
                            "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                string FilePath = "./" + DateTime.Now.Year +"-"+ DateTime.Now.Month +"-"+ DateTime.Now.Day + "_" +
                        DateTime.Now.Hour + "-" + DateTime.Now.Minute + "_" + "log.csv";

                Log_Version log = new Log_Version();
                log.LogFileBuild(FilePath);

                timer_Log.Interval = interval * 1000;
                timer_Log.Enabled = true;

                if (Lang.b_LangFlag == 0)
                {
                    label_LogStatus.Text = "正在运行";
                    MessageBox.Show("日志记录时间间隔设置成功，开始记录", "消息提示！");
                }
                else
                {
                    label_LogStatus.Text = "Recoding";
                    MessageBox.Show("Log record interval set succeed，recording!", "Message！");
                }
                textBox_LogFileName.Text = FilePath.Substring(2);
            }
            catch (Exception ex)
            {
                if (string.Equals(ex.GetType().ToString(), "System.FormatException"))
                {
                    MessageBox.Show("Please Input Right Interval", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    if (Lang.b_LangFlag == 0)
                    {
                        MessageBox.Show("日志文件异常", "错误提示！");
                    }
                    else
                    {
                        MessageBox.Show("LogFile Error!", "ErrorMessage！");
                    }
                }
                timer_Log.Enabled = false;
            }
        }

        private void button_LogTimerStop_Click(object sender, EventArgs e)
        {
            timer_Log.Enabled = false;
            if (Lang.b_LangFlag == 0)
            {
                label_LogStatus.Text = "已停止";
                MessageBox.Show("停止记录日志", "消息提示！");
            }
            else
            {
                label_LogStatus.Text = "Stopped";
                MessageBox.Show("Log Recording Stopped!", "Message！");
            }
        }

        #region 产品序列号等信息
        public UInt16 RS485_ADDR_SN_SERIAL_NUM = 0xFFF0;
        public UInt16 RS485_ADDR_SN_HAEDWARE_VER = 0xFFF1;
        public UInt16 RS485_ADDR_SN_SOFTWARE_VER = 0xFFF2;
        public UInt16 RS485_ADDR_ONLY_BATNUM = 0xFFF3;
        public UInt16 RS485_ADDR_SN_READ = 0xC002;
        public UInt16 RS485_ADDR_ONLYBATNUM_READ = 0xC004;

        private void button_BMS_SerialNum_Set_Click(object sender, EventArgs e)
        {
            UInt16[] u_Params = new UInt16[16];
            string str = textBox_BMS_SerialNum.Text;

            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("串口没打开");
                return;
            }

            //这里提示多了多少个字节。TODO
            if (str.Length > 32 || str.Length == 0)
            {
                MessageBox.Show("输入字符串为空或超过32个字符串");
                return;
            }

            byte[] sendByte = System.Text.Encoding.GetEncoding("GBK").GetBytes(str);

            //就写16个，固定
            for (int i = 0; i < 16; ++i)
            {
                if (2 * i < sendByte.Length)
                {
                    //这个数据转换太难了吧？？？？？
                    //Cannot implicitly convert type 'int' to 'ushort'
                    //原因在于：
                    //一，8为隐式int类型，
                    //二，其执行顺序是右往左？强制转换类型的优先级不如 << ？
                    //三，运算符会悄悄把其转换为int类型，但是+=符号不会
                    //u_Params[i] = (UInt16)sendByte[2 * i] << 8;      //不行，报错 
                    //u_Params[i] = (UInt16)(sendByte[2 * i]<<8);      //这个可以，但是有问题，数据溢出
                    u_Params[i] = (UInt16)(((UInt16)sendByte[2 * i])<<8);
                }
                else
                {
                    u_Params[i] = 0;
                }

                if (2 * i + 1 < sendByte.Length)
                {
                    //以下两种方式二选一
                    //u_Params[i] = (UInt16)(u_Params[i] + ((UInt16)sendByte[2 * i + 1]));
                    u_Params[i] += (UInt16)sendByte[2 * i + 1];
                }
                else
                {
                    u_Params[i] += 0;
                }
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = RS485_ADDR_SN_SERIAL_NUM;
            u16Rs485RegNum = 16;
            bRs485ByteNum = 32;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_BMS_HardWareVer_Set_Click(object sender, EventArgs e)
        {
            UInt16[] u_Params = new UInt16[16];
            string str = textBox_BMS_HardWareVer.Text;

            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("串口没打开");
                return;
            }

            //这里提示多了多少个字节。TODO
            if (str.Length > 32 || str.Length == 0)
            {
                MessageBox.Show("输入字符串为空或超过32个字符串");
                return;
            }

            byte[] sendByte = System.Text.Encoding.GetEncoding("GBK").GetBytes(str);

            //就写16个，固定
            for (int i = 0; i < 16; ++i)
            {
                if (2 * i < sendByte.Length)
                {
                    u_Params[i] = (UInt16)(((UInt16)sendByte[2 * i]) << 8);
                }
                else
                {
                    u_Params[i] = 0;
                }

                if (2 * i + 1 < sendByte.Length)
                {
                    //以下两种方式二选一
                    //u_Params[i] = (UInt16)(u_Params[i] + ((UInt16)sendByte[2 * i + 1]));
                    u_Params[i] += (UInt16)sendByte[2 * i + 1];
                }
                else
                {
                    u_Params[i] += 0;
                }
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = RS485_ADDR_SN_HAEDWARE_VER;
            u16Rs485RegNum = 16;
            bRs485ByteNum = 32;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_BMS_SoftWareVer_Set_Click(object sender, EventArgs e)
        {
            UInt16[] u_Params = new UInt16[16];
            string str = textBox_BMS_SoftWareVer.Text;

            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("串口没打开");
                return;
            }

            //这里提示多了多少个字节。TODO
            if (str.Length > 32 || str.Length == 0)
            {
                MessageBox.Show("输入字符串为空或超过32个字符串");
                return;
            }

            byte[] sendByte = System.Text.Encoding.GetEncoding("GBK").GetBytes(str);

            //就写16个，固定
            for (int i = 0; i < 16; ++i)
            {
                if (2 * i < sendByte.Length)
                {
                    u_Params[i] = (UInt16)(((UInt16)sendByte[2 * i]) << 8);
                }
                else
                {
                    u_Params[i] = 0;
                }

                if (2 * i + 1 < sendByte.Length)
                {
                    //以下两种方式二选一
                    //u_Params[i] = (UInt16)(u_Params[i] + ((UInt16)sendByte[2 * i + 1]));
                    u_Params[i] += (UInt16)sendByte[2 * i + 1];
                }
                else
                {
                    u_Params[i] += 0;
                }
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = RS485_ADDR_SN_SOFTWARE_VER;
            u16Rs485RegNum = 16;
            bRs485ByteNum = 32;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_SN_Version_Read_Click(object sender, EventArgs e)
        {
            textBox_BMS_SerialNum.Text = "";
            textBox_BMS_HardWareVer.Text = "";
            textBox_BMS_SoftWareVer.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = RS485_ADDR_SN_READ;
            u16Rs485RegNum = 48;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }
        #endregion

        #region 串机日志

        private void timer_serialLog_Tick(object sender, EventArgs e)
        {
            #region 日志变量
            string str =
                DateTime.Now.ToString() + "," +
                label_Parallel_VbatSum.Text + "," +
                label_Parallel_VpackSumDelta.Text + "," +
                label_Parallel_VpackMax.Text + "," +
                label_Parallel_VpackMin.Text + "," +
                label_Parallel_VpackMax_Pos.Text + "," +
                label_Parallel_VpackMin_Pos.Text + "," +
                label_Parallel_VcellMax.Text + "," +
                label_Parallel_VcellMin.Text + "," +
                label_Parallel_VcellMax_Pos.Text + "," +
                label_Parallel_VcellMin_Pos.Text + "," +
                label_Parallel_VcellDeltaMax.Text + "," +
                label_Parallel_VcellDeltaMin.Text + "," +
                label_Parallel_VcellDeltaMax_Pos.Text + "," +
                label_Parallel_VcellDeltaMin_Pos.Text + "," +

                label_Parallel_IchgSum.Text + "," +
                label_Parallel_IdsgSum.Text + "," +

                label_Parallel_CHG_Relay.Text + "," +
                label_Parallel_DSG_Relay.Text + "," +

                label_Parallel_Vcell_OV_Third.Text + "," +
                label_Parallel_Vcell_UV_Third.Text + "," +
                label_Parallel_Vpack_OV_Third.Text + "," +
                label_Parallel_Vpack_UV_Third.Text + "," +
                label_Parallel_Vbat_OV_Third.Text + "," +
                label_Parallel_Vbat_UV_Third.Text + "," +
                label_Parallel_CHG_OC_Third.Text + "," +
                label_Parallel_DSG_OC_Third.Text + "," +
                label_Parallel_Cellchg_OT_Third.Text + "," +
                label_Parallel_Cellchg_UT_Third.Text + "," +
                label_Parallel_Celldsg_OT_Third.Text + "," +
                label_Parallel_Celldsg_UT_Third.Text + "," +
                label_Parallel_Vdelta_Op_Third.Text + "," +
                label_Parallel_Res_Third.Text + "," +
                label_Parallel_Tmos_OTP_Third.Text + "," +
                label_Parallel_Soc_Up_Third.Text + "," +
                label_Parallel_SOC.Text + "," +
                label_Parallel_Present_mAh.Text;


            for (int i = 0; i < 256; ++i)
            {
                str += ("," + ParallelVCell[i]);
            }
            #endregion

            Log_Version.LogRecord(str, "./" + textBox_serialLogFileName.Text);

        }

        private void button_serialLogTimerStart_Click(object sender, EventArgs e)
        {
            if (timer_serialLog.Enabled == true)
            {
                MessageBox.Show("LogRecording Already Started", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int interval = 0;
                interval = Convert.ToInt32(textBox_serialLogTimer.Text);
                if (interval < 2 || interval > 3600)
                {
                    if (Lang.b_LangFlag == 0)
                    {
                        MessageBox.Show("日志记录时间间隔范围为 2 - 3600s，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("Log record interval range is 2 - 3600s, and the data is out of bounds！",
                            "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                string FilePath = "./" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "_" +
                        DateTime.Now.Hour + "-" + DateTime.Now.Minute + "_" + "log.csv";

                Log_Version log = new Log_Version(true);
                log.LogFileBuild(FilePath);

                timer_serialLog.Interval = interval * 1000;
                timer_serialLog.Enabled = true;

                if (Lang.b_LangFlag == 0)
                {
                    label_LogStatus_Parallel.Text = "正在运行";
                    MessageBox.Show("日志记录时间间隔设置成功，开始记录", "消息提示！");
                }
                else
                {
                    label_LogStatus_Parallel.Text = "Recoding";
                    MessageBox.Show("Log record interval set succeed，recording!", "Message！");
                }
                textBox_serialLogFileName.Text = FilePath.Substring(2);
            }
            catch (Exception ex)
            {
                if (string.Equals(ex.GetType().ToString(), "System.FormatException"))
                {
                    MessageBox.Show("Please Input Right Interval", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    if (Lang.b_LangFlag == 0)
                    {
                        MessageBox.Show("日志文件异常", "错误提示！");
                    }
                    else
                    {
                        MessageBox.Show("LogFile Error!", "ErrorMessage！");
                    }
                }
                timer_serialLog.Enabled = false;
            }
        }



        private void button_serialLogTimerStop_Click(object sender, EventArgs e)
        {
            timer_serialLog.Enabled = false;
            if (Lang.b_LangFlag == 0)
            {
                label_LogStatus_Parallel.Text = "已停止";
                MessageBox.Show("停止记录日志", "消息提示！");

            }
            else
            {
                label_LogStatus_Parallel.Text = "Stopped";
                MessageBox.Show("Log Recording Stopped!", "Message！");
            }
        }


        #endregion

        #region Form2的内容__改为100条Event日志记录
        //按钮在Form1，显示结果在Form2的代码里面
        public UInt16 RS485_ADDR_EVENT_RECORD = 0xC008;
        public UInt16 RS485_CMD_ADDR_RESET_EVENT_RECORD = 0x1007;
        bool EVENT_RECORD_OK = false;

        public void EventRecord_Read()
        {
            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = RS485_ADDR_EVENT_RECORD;
            u16Rs485RegNum = 100;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
            EVENT_RECORD_OK = false;
        }

        //非独占性延时函数
        public static void Delay_Non_Exclusive(int milliSecond)
        {
            int start = Environment.TickCount;
            while (Math.Abs(Environment.TickCount - start) < milliSecond)
            {
                Application.DoEvents();
            }
        }

        private void button_ProtectPresent_Detail_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;       //停止轮询
            for(int i = 0;i<100;++i)
            {
                EventRecord[i, 0] = 0;
                EventRecord[i, 1] = 0;
            }

            EventRecord_Read();
            Delay_Non_Exclusive(1000);    //非独占性延时1s，等待回收数据
            if (EVENT_RECORD_OK)
            {
                Form2 myForm = new Form2();
                myForm.Form2LabelLoad(Form2_FaultDataBuff);
                myForm.Form2_EventRecord(EventRecord);
                myForm.ShowDialog();
            }
            timer1.Enabled = true;
        }

        private void button_EventClear_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;       //停止轮询
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = RS485_CMD_ADDR_RESET_EVENT_RECORD;
            u16Rs485RegNum = 0x0001;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
            Delay_Non_Exclusive(1000);    //非独占性延时1s，等待回收数据
            timer1.Enabled = true;
        }

        #endregion

    }

    class Log_Version
    {
        private string sLogNameCh = "时间,充电CBC,放电CBC,第一串电压,第二串电压,第三串电压,第四串电压,第五串电压,"+
                    "第六串电压,第七串电压,第八串电压,第九串电压,第十串电压,第十一串电压,第十二串电压,"         +
                    "第十三串电压,第十四串电压,第十五串电压,第十六串电压,"                                      +
                    "第十七串电压,第十八串电压,第十九串电压,第二十串电压,第二十一串电压,第二十二串电压,第二十三串电压," +
                    "第二十四串电压,第二十五串电压,第二十六串电压,第二十七串电压,第二十八串电压,第二十九串电压,第三十串电压," + 
                    "第三十一串电压,第三十二串电压," + 
                    "总压,充电电流,放电电流,SOC,当前剩余电量,充电剩余时间,放电剩余时间,第三级保护状态,预充MOS,充电管,放电管," +
                    "温度1,温度2,温度3,温度4,温度5,温度6,环境温度1,环境温度2,环境温度3,MOS温度," +
                    "单节过压保护,单节低压保护,总电压过压保护,总电压低压保护,"                                  +
                    "充电过流保护,放电过流保护,充电过温保护,放电过温保护,充电低温保护,放电低温保护,"            +
                    "压差过大保护,SOC过低,MOS过温保护";

        private string sLogNameEn = "Time,Chg_CBC,Dsg_CBC,VC1,VC2,VC3,VC4,VC5,"+
                    "VC6,VC7,VC8,VC9,VC10,VC11,VC12,VC13,VC14,VC15,VC16,"       +
                    "VC17,VC18,VC19,VC20,VC21,VC22,VC23,VC24,VC25,VC26,VC27,VC28,VC29,VC30,VC31,VC32," +
                    "Vbat,Ichg,Idsg,SOC,Pre_mAh,FaultThird,PreFET,ChgFET,DsgFET," +
                    "Temp1,Temp2,Temp3,Temp4,Temp5,Temp6,TempEV1,TempEV2,TempEV3,TempMOS," +
                    "CellOVP,CellUVP,BatOVP,BatUVP,"                             +
                    "IchgOCP,IdsgOCP,ChgOTP,DsgOTP,ChgUTP,DsgUTP," +
                    "VcellDeltaOP,SOCLowP,MosOTP";
        /*
        private string sSerialLogNameEn = "Time,VbatSum,VpackDelta,VpackMax,VpackMin,Max_position,Min_position,Vcell_Max," +
                   "Vcell_Min,Max_position,Min_position,VcellDeltaMax,VcellDeltaMin,Max_position,Min_position,  IchgSum,IdsgSum,  CHG_Relay,DSG_Relay,"   +
                   "Vcell_OV,Vcell_UV,Vpack_OV,Vpack_UV,Vbat_OV,Vbat_UV,CHG_OC,DSG_OC,Cellchg_OT," +
                    "Cellchg_UT,Celldsg_OT,Celldsg_UT,Vdelta_Op,Res,Tmos_OTP,SOC_low_P";
        */
        private string sSerialLogNameEn = "时间,输出总压,单包压差,单包最大电压,单包最小电压,单包最大位置,单包最小位置,单节最大电压," +
           "单节最小电压,单节最大位置,单节最小位置,单节压差最大包,单节压差最小包,单节压差最大包位置,单节压差最小包位置,充电电流,放电电流,充电接触器,放电接触器," +
           "单节过压保护,单节低压保护,单包过压保护,单包低压保护,总压过压保护,总压低压保护,充电过流保护,放电过流保护,充电高温保护," +
            "充电低温保护,放电高温保护,放电低温保护,压差过大保护,保留位,MOS过温保护,SOC过低,SOC,剩余容量," +
            "第1串电压,第2串电压,第3串电压,第4串电压,第5串电压,第6串电压,第7串电压,第8串电压,第9串电压,第10串电压,第11串电压,第12串电压,第13串电压,第14串电压,第15串电压,第16串电压," +
            "第17串电压,第18串电压,第19串电压,第20串电压,第21串电压,第22串电压,第23串电压,第24串电压,第25串电压,第26串电压,第27串电压,第28串电压,第29串电压,第30串电压,第31串电压,第32串电压," +
            "第33串电压,第34串电压,第35串电压,第36串电压,第37串电压,第38串电压,第39串电压,第40串电压,第41串电压,第42串电压,第43串电压,第44串电压,第45串电压,第46串电压,第47串电压,第48串电压," +
            "第49串电压,第50串电压,第51串电压,第52串电压,第53串电压,第54串电压,第55串电压,第56串电压,第57串电压,第58串电压,第59串电压,第60串电压,第61串电压,第62串电压,第63串电压,第64串电压," +
            "第65串电压,第66串电压,第67串电压,第68串电压,第69串电压,第70串电压,第71串电压,第72串电压,第73串电压,第74串电压,第75串电压,第76串电压,第77串电压,第78串电压,第79串电压,第80串电压," +
            "第81串电压,第82串电压,第83串电压,第84串电压,第85串电压,第86串电压,第87串电压,第88串电压,第89串电压,第90串电压,第91串电压,第92串电压,第93串电压,第94串电压,第95串电压,第96串电压," +
            "第97串电压,第98串电压,第99串电压,第100串电压,第101串电压,第102串电压,第103串电压,第104串电压,第105串电压,第106串电压,第107串电压,第108串电压,第109串电压,第110串电压,第111串电压,第112串电压," +
            "第113串电压,第114串电压,第115串电压,第116串电压,第117串电压,第118串电压,第119串电压,第120串电压,第121串电压,第122串电压,第123串电压,第124串电压,第125串电压,第126串电压,第127串电压,第128串电压," +
            "第129串电压,第130串电压,第131串电压,第132串电压,第133串电压,第134串电压,第135串电压,第136串电压,第137串电压,第138串电压,第139串电压,第140串电压,第141串电压,第142串电压,第143串电压,第144串电压," +
            "第145串电压,第146串电压,第147串电压,第148串电压,第149串电压,第150串电压,第151串电压,第152串电压,第153串电压,第154串电压,第155串电压,第156串电压,第157串电压,第158串电压,第159串电压,第160串电压," +
            "第161串电压,第162串电压,第163串电压,第164串电压,第165串电压,第166串电压,第167串电压,第168串电压,第179串电压,第170串电压,第171串电压,第172串电压,第173串电压,第174串电压,第175串电压,第176串电压," +
            "第177串电压,第178串电压,第179串电压,第180串电压,第181串电压,第182串电压,第183串电压,第184串电压,第185串电压,第186串电压,第187串电压,第188串电压,第189串电压,第190串电压,第191串电压,第192串电压," +
            "第193串电压,第194串电压,第195串电压,第196串电压,第197串电压,第198串电压,第199串电压,第200串电压,第201串电压,第202串电压,第203串电压,第204串电压,第205串电压,第206串电压,第207串电压,第208串电压," +
            "第209串电压,第210串电压,第211串电压,第212串电压,第213串电压,第214串电压,第215串电压,第216串电压,第217串电压,第218串电压,第219串电压,第220串电压,第221串电压,第222串电压,第223串电压,第224串电压," +
            "第225串电压,第226串电压,第227串电压,第228串电压,第229串电压,第230串电压,第231串电压,第232串电压,第233串电压,第234串电压,第235串电压,第236串电压,第237串电压,第238串电压,第239串电压,第240串电压," +
            "第241串电压,第242串电压,第243串电压,第244串电压,第245串电压,第246串电压,第247串电压,第248串电压,第249串电压,第250串电压,第251串电压,第252串电压,第253串电压,第254串电压,第255串电压,第256串电压"
            ;
        private string sLogName;

        public Log_Version() {
            if (Lang.b_LangFlag == 0)
                sLogName = sLogNameCh;
            else
                sLogName = sLogNameEn;
        }
        public Log_Version(bool flag)
        {
                sLogName = sSerialLogNameEn;
        }


        public void LogFileBuild(string FilePath)
        {
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(FilePath, true, Encoding.UTF8, 2);
                sw.WriteLine(this.sLogName);
                sw.Flush();
                sw.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        static public void LogRecord(string str, string FilePath) {
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(FilePath, true, Encoding.UTF8, 2);

                sw.WriteLine(str);
                sw.Flush();
                sw.Close();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("日志保存异常", "错误提示！");
                }
                else
                {
                    MessageBox.Show("LogFile Saving Error!", "ErrorMessage！");
                }
            }
        }
    }
}















