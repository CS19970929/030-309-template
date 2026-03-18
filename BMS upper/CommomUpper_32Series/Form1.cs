using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Collections.Generic;

namespace CommomUpper_32Series
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //下边这句代码可以防止线程间调试报异常
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;//设置该属性 为false
        }

        #region 变量定义
        //串口在线升级相关变量
        string filename = null;
        public int CountCnt = 0;
        public int cout = 0;
        public int m = 0;
        public int len = 0;
        public int length = 0;
        public byte[] sendbuf;
        public float ProgressBar_n = 0;

        byte bRxByteCnt = 0;
        byte bTotleBytes = 0;
        bool bRxFrameFinishFlag = false;

        public byte byComAvailableNum;
        public string[] stComName = { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10", "COM11", "COM12", "COM13", "COM14", "COM15", "COM16", "COM17", "COM18", "COM19", "COM20" };
        public string[] stComAvailable = new string[20];
        public UInt16 u16ComCnt = 0;
        public string[] FaultWarnName = null;
        public byte[] Form2_FaultDataBuff = new byte[12];
        public byte[,] Form2_FaultSysTime = new byte[12, 6];

        public string[] FaultWarnName_Eng = { "NA", "CellOVPro", "CellUVPro", "BatOVPro", "BatUVPro", "ChgOCPro", "DsgOCPro", "ChgOTPro", "DsgOTPro", "MosOTPro",
                                            "ChgUTPro", "DsgUTPro", "LowSOCPro", "HighDeltaPro", "NA","NA","NA","NA","NA","NA","CellOVWarn", "CellUVWarn", "BatOVWarn",
                                            "BatUVWarn", "ChgOCWarn", "DsgOCWarn", "ChgOTWarn", "DsgOTWarn", "MosOTWarn", "ChgUTWarn", "DsgUTWarn", "LowSOCWarn", "HighDeltaWarn"};

        public string[] stSlaverName = { "SlaverSingle", "Slaver1", "Slaver2", "Slaver3", "Slaver4", "Slaver5", "Slaver6", "Slaver7", "Slaver8", "Slaver9", "Slaver10", "Slaver11", "Slaver12", "Slaver13", "Slaver14", "Slaver15", "Slaver16", "Master" };

        String Year = "";
        String Month = "";
        String Day = "";
        String Hour = "";
        String Minute = "";
        String Second = "";

        public int TempOffset = -40;

        public int[] ParallelVCell = new int[256];


        #endregion

        #region 串口在线升级
        public static byte[] ConvertToBinary(string Path)
        {
            FileStream stream = new FileInfo(Path).OpenRead();
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
            return buffer;
        }

        public void FlashHEXcout()
        {
            try
            {
                sendbuf = ConvertToBinary(filename);
                len = sendbuf.Length;
                cout = len / 1024;
                //m = len - 1024 * cout;
                m = len % 1024;
                float len2 = len / 1024;
                textBox_upgrate_window.AppendText("大小为:" + len2.ToString() + " K\n");
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("找不到烧写文件！", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Can not find the pack!", "ErrorMessage！");
                }
                return;
            }
        }

        public void FlashUpgrateConnect()
        {
            byte[] senddataTemp = new byte[50];
            textBox_upgrate_window.AppendText("正在连接设备.....\n");
            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0xFFFD;
                u16Rs485RegNum = 1;
                bRs485ByteNum = 2;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;
                senddataTemp[i++] = 0;
                senddataTemp[i++] = 0;
                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Errot，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        public void FlashUpgrateComplete()
        {
            byte[] senddataTemp = new byte[50];
            CountCnt = 0;
            length = 0;
            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0xFFFF;
                u16Rs485RegNum = 1;
                bRs485ByteNum = 2;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;
                senddataTemp[i++] = 0;
                senddataTemp[i++] = 0;
                Calculate_Sum_Tx(ref senddataTemp, (UInt32)i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Errot，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        public void FlashUpgrate()
        {
            byte[] senddataTemp = new byte[1100];
            int j = 0;

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = 0xFFFE;
            u16Rs485RegNum = 0;
            bRs485ByteNum = 0;

            senddataTemp[j++] = RS485_SLAVE_ADDR;
            senddataTemp[j++] = bRs485FunCmd;
            senddataTemp[j++] = (byte)(u16Rs485RegAddr / 256);
            senddataTemp[j++] = (byte)(u16Rs485RegAddr % 256);
            try
            {
                if (CountCnt == cout && m != 0)
                {
                    u16Rs485RegNum = (UInt16)m;
                    senddataTemp[j++] = (byte)(u16Rs485RegNum / 256);
                    senddataTemp[j++] = (byte)(u16Rs485RegNum % 256);
                    senddataTemp[j++] = bRs485ByteNum;
                    Array.ConstrainedCopy(sendbuf, length, senddataTemp, j, m);
                    //j = j + m + 1;
                    j = j + m;
                    Calculate_Sum_Tx(ref senddataTemp, (UInt32)j);
                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();
                    bRxByteCnt = 0;
                    bTotleBytes = 0;
                    bRxFrameFinishFlag = false;
                    serialPort1.Write(senddataTemp, 0, j + 2);
                    serialPort1.DiscardInBuffer();
                    length += m;
                    progressBar_upgrate.Value = 100;
                }
                else
                {
                    u16Rs485RegNum = 1024;
                    senddataTemp[j++] = (byte)(u16Rs485RegNum / 256);
                    senddataTemp[j++] = (byte)(u16Rs485RegNum % 256);
                    senddataTemp[j++] = bRs485ByteNum;
                    Array.ConstrainedCopy(sendbuf, length, senddataTemp, j, 1024);
                    //j = j + 1024 + 1; //边界问题想清楚
                    j = j + 1024;
                    Calculate_Sum_Tx(ref senddataTemp, (UInt32)j);
                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();
                    bRxByteCnt = 0;
                    bTotleBytes = 0;
                    bRxFrameFinishFlag = false;
                    serialPort1.Write(senddataTemp, 0, j + 2);
                    serialPort1.DiscardInBuffer();

                    length += 1024;
                    ProgressBar_n = (float)length / (float)len;
                    ProgressBar_n *= 100;
                    progressBar_upgrate.Invoke(new EventHandler(delegate
                    {
                        progressBar_upgrate.Value = (int)ProgressBar_n;
                    }));
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("传输bin文件出错！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Transferring bin file error！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

        }

        private void Button_upgrate_find_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = @"升级包|*.bin";
            file.ShowDialog();
            if (file.FileName.Length > 0)
            {
                this.textBox_upgrate_pack.Text = file.FileName;
                filename = file.FileName;
                textBox_upgrate_window.AppendText("已加载升级包！");
                button_upgrate_connect.Enabled = true;
            }
            FlashHEXcout();     //计算HEX各种大小
        }

        private void button_upgrate_connect_Click(object sender, EventArgs e)
        {
            FlashUpgrateConnect();
            CountCnt = 0;
            length = 0;
            /*
            CountCnt = 0;
            length = 0;
            button_upgrate_begin.Enabled = true;
            FlashUpgrateComplete();
            */
        }

        private void button_upgrate_begin_Click(object sender, EventArgs e)
        {
            /*
            Task task = new Task(FlashUpgrate);
            task.Start();
             * */
            FlashUpgrate();
        }

        private void button_upgrate_clear_Click(object sender, EventArgs e)
        {
            textBox_upgrate_window.Text = "";
            textBox_upgrate_pack.Text = "";
            button_upgrate_connect.Enabled = false;
            button_upgrate_begin.Enabled = false;
            progressBar_upgrate.Value = 0;
        }
        #endregion

        #region 读写.ini类(未使用)
        public class NiceIniWriteAndRead
        {
            [DllImport("kernel32")]
            private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
            [DllImport("kernel32")]
            private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

            //------------【函数：将字符串写入ini】------------    
            //section: 要写入的段落名
            //key: 要写入的键，如果该key存在则覆盖写入
            //val: key所对应的值
            //filePath: INI文件的完整路径和文件名
            //------------------------------------------------------------------------
            public static bool WriteToIni(string section, string key, string val, string filePath)
            {
                /*  以下报错，问题不清楚
                bool result = true;
                try
                {
                    string mystr1 = NiceFileProduce.CheckAndCreatPath(NiceFileProduce.DecomposePathAndName(filePath, NiceFileProduce.DecomposePathEnum.PathOnly));
                    if (mystr1 != "error")
                    {
                        WritePrivateProfileString(section,key,val,filePath);
                    }
                    else
                    {
                        result = false;
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
                */

                bool result = true;
                //判断文件路径是否存在，不存在则创建文件夹
                try
                {
                    if (!System.IO.Directory.Exists(filePath))
                    {
                        System.IO.Directory.CreateDirectory(filePath);//不存在就创建目录
                    }
                    //判断文件是否存在 
                    if (File.Exists(filePath))
                    {
                        //存在 
                        result = true;
                        WritePrivateProfileString(section, key, val, filePath);
                    }
                    else
                    {
                        //不存在
                        result = false;
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
            }

            //------------【函数：从ini读取字符串】------------    
            //section：要读取的段落名
            //key: 要读取的键
            //defVal: 读取异常的情况下的缺省值

            //filePath: INI文件的完整路径和文件名
            //------------------------------------------------------------------------
            public static string ReadFromIni(string section, string key, string def, string filePath)
            {
                StringBuilder retVal = new StringBuilder();
                GetPrivateProfileString(section, key, def, retVal, 500, filePath);
                return retVal.ToString();
            }
        }
        #endregion

        #region 基础设置及相关按键
        //相当于main函数，主程序入口
        private void Form1_Load(object sender, EventArgs e)
        {
            //ComSelect();
            Calibration_init();
            //button_English.PerformClick();      //默认英文
            button_Chinese.PerformClick();
            tabPage_BlueTWifi.Parent = null;    //蓝牙WIFI页面默认隐藏，这个作用是台式电脑通过同样的模块和BMS连接，进行上位机通讯。
                                                //笔记本电脑直接用自己的蓝牙连接，是否能进行上位机通讯不清楚，不行就直接接一个蓝牙模块，通过串口通讯
                                                //HideTabPages();//隐藏需要管理员权限的页面
            button_test.Visible = false;//单元测试已删除，所以这里隐藏单元测试按钮 

            //把SlaverAll排除，以免出现歧义，不需要set ALL了，内部自动切换
            //还是用按钮去处理
            for (int i = 0; i < 18; i++)
            {
                comboBox_SalverSelect.Items.Add(stSlaverName[i]);
            }
            //timer_Parallel.Enabled = false;

            AFE_Parameters_Interface_Init();

            ComSelect2();

            //timer1.Enabled = false;

            //parameterExportAndImportInit();
        }

        //语言选择，可以实现保存上次的语言，未使用
        private void LanguageSelect()
        {
            string section = "LanguageSelect";
            string key = "keyword";
            string filePath = @".\Language.ini";
            string def = "";
            bLanguageSelect = NiceIniWriteAndRead.ReadFromIni(section, key, def, filePath);
            switch (bLanguageSelect)
            {
                case "English":
                    break;
                case "Chinese":
                    break;
                default:
                    break;
            }
        }

        //串口初始化，打开串口，打开时钟，这个函数没写好，后人去完善吧
        private void ComSelect()
        {
            byte i, j;

            j = 0;
            byComAvailableNum = 0;
            for (i = 0; i < 20; i++)
            {
                try
                {
                    serialPort1.PortName = stComName[i];
                    serialPort1.Open();
                    stComAvailable[j++] = stComName[i];

                    serialPort1.Close();
                    //comboBox_ComNum.DataSource = SerialPort.GetPortNames();
                }
                catch (Exception)
                {
                    u16ComCnt++;
                }

            }
            byComAvailableNum = j;
            if (byComAvailableNum > 0)
            {
                serialPort1.PortName = stComAvailable[0];
                try
                {
                    comboBox_ComNum.Text = serialPort1.PortName;
                    comboBox_BandRate.Text = Convert.ToString(serialPort1.BaudRate);
                    for (i = 0; i < byComAvailableNum; i++)     //感觉没啥用
                    {
                        comboBox_ComNum.Items.Add(stComAvailable[i]);
                    }

                    serialPort1.Open();
                    button_TurnOnOffCom.Enabled = true;

                    if (Lang.b_LangFlag == 0)
                        button_TurnOnOffCom.Text = "关闭串口";
                    else
                        button_TurnOnOffCom.Text = "ShutDown";

                    groupBox_set.Enabled = true;
                    groupBox_read.Enabled = true;
                    timer1.Enabled = true;
                    bRdCmdErrCnt = 0;
                }
                catch (Exception)
                {
                    groupBox_set.Enabled = false;
                    groupBox_read.Enabled = false;

                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口打开失败！", "错误信息！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show("Start serial port failed！", "ErrorMessage！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (Lang.b_LangFlag == 0)
                    MessageBox.Show("请插入USB线缆！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Please connect the USB cable！", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void ComSelect2()
        {
            string[] ComNames = System.IO.Ports.SerialPort.GetPortNames();
            if (ComNames.Length > 0)
            {
                try
                {
                    comboBox_ComNum.DataSource = ComNames;
                    serialPort1.PortName = ComNames[0];
                    comboBox_ComNum.Text = ComNames[0];
                    comboBox_BandRate.Text = Convert.ToString(serialPort1.BaudRate);
                    serialPort1.Open();
                    button_TurnOnOffCom.Enabled = true;

                    if (Lang.b_LangFlag == 0)
                        button_TurnOnOffCom.Text = "关闭串口";
                    else
                        button_TurnOnOffCom.Text = "ShutDown";

                    groupBox_set.Enabled = true;
                    groupBox_read.Enabled = true;
                    timer1.Enabled = true;
                }
                catch (Exception)
                {
                    groupBox_set.Enabled = false;
                    groupBox_read.Enabled = false;

                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口打开失败！", "错误信息！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show("Start serial port failed！", "ErrorMessage！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (Lang.b_LangFlag == 0)
                    MessageBox.Show("请插入USB线缆！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Please connect the USB cable！", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //打开串口按钮
        private void button_TurnOnOffCom_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                    //bComOnOffFlag = false;
                    timer1.Enabled = false;
                    groupBox_set.Enabled = false;
                    groupBox_read.Enabled = false;
                    //button_TurnOnOffCom.Text = "打开串口";


                    if (Lang.b_LangFlag == 0)
                        button_TurnOnOffCom.Text = "打开串口";
                    else
                        button_TurnOnOffCom.Text = "StartUp";
                    //add end

                    //toolStripStatusLabel_ComStatus.Text = "串口" + serialPort1.PortName + "已关闭";
                    //toolStripStatusLabel_ComConfig.Text = serialPort1.PortName + "  " + Convert.ToString(serialPort1.BaudRate) + ",n,8,1";
                }
                else
                {
                    serialPort1.PortName = comboBox_ComNum.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox_BandRate.Text, 10);
                    //bComOnOffFlag = true;
                    bRdCmdErrCnt = 0;
                    timer1.Enabled = true;
                    groupBox_set.Enabled = true;
                    groupBox_read.Enabled = true;
                    //bMdlAddr = 1;
                    serialPort1.Open();
                    //button_TurnOnOffCom.Text = "关闭串口";


                    if (Lang.b_LangFlag == 0)
                        button_TurnOnOffCom.Text = "关闭串口";
                    else
                        button_TurnOnOffCom.Text = "ShutDown";
                    //add end

                    //toolStripStatusLabel_ComStatus.Text = "串口" + serialPort1.PortName + "已打开";
                    //toolStripStatusLabel_ComConfig.Text = serialPort1.PortName + "  " + Convert.ToString(serialPort1.BaudRate) + ",n,8,1";
                }
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                    MessageBox.Show("串口错误，请检查串口号是否正确", "错误提示！");
                else
                    MessageBox.Show("Serial port error, Please recheck the Serial Port!", "ErrorMessage！");
                return;
            }

        }
        //页面选择更改时钟状态(停止或开始)
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                if (comboBox_SalverSelect.Text != "Master")
                {
                    //还有ComSelect()这里的自动打开timer1也去掉
                    //还是不需要去掉，默认打开便可
                    timer1.Enabled = true;
                }
                else
                {
                    timer1.Enabled = false;
                }
            }
            else
            {
                if (false == FormDAM_Info.siglMnitSchema)
                {
                    tabControl1.SelectedIndex = 0;
                    MessageBox.Show("请切换至单机模式！", "参数管理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                timer1.Enabled = false;
            }

            if (tabControl1.SelectedIndex == 6)
            {
                timer_Parallel.Enabled = true;
            }
            else
            {
                timer_Parallel.Enabled = false;
            }
            //还是用按钮来处理
            //Parallel_tabControl1_SelectedIndexChanged();
        }
        #endregion

        #region BMS信息轮循时钟及接收(0xD000系列)
        private void timer1_Tick(object sender, EventArgs e)
        {
            UInt16 u16RegAddrTemp;
            UInt16 u16RegNumTemp;
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            label_DevAddrShow.Text = "" + RS485_SLAVE_ADDR;
            bl_RxFinishedFlag = false;

            if (!serialPort1.IsOpen)
            {
                bRdCmdErrCnt++;
                if (bRdCmdErrCnt > 3)
                {
                    timer1.Enabled = false;
                    bRdCmdErrCnt = 0;
                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口错误，串口未打开", "错误提示！");
                    else
                        MessageBox.Show("Serial port error, Serial Port is not Start Up", "ErrorMessage！");
                }
                return;
            }

            switch (g_u16RdSel++)
            {
                case 0:
                    u16RegAddrTemp = 0xD000;
                    u16RegNumTemp = 38;
                    break;

                case 1:
                    u16RegAddrTemp = 0xD026;
                    u16RegNumTemp = 25;
                    break;
                case 2:
                    u16RegAddrTemp = 0xD100;
                    u16RegNumTemp = 21;
                    break;
                case 3:
                    u16RegAddrTemp = 0xD115;
                    u16RegNumTemp = 12;
                    break;
                case 4:
                    u16RegAddrTemp = 0xD200;
                    u16RegNumTemp = 1;
                    g_u16RdSel = 0;
                    break;
                // case 5:
                //     //u16RegAddrTemp = 0xD200;
                //     //u16RegNumTemp = 1;
                //     try
                //     {
                //         byte i = 0;

                //         sendCatch[i++] = 0x5a;
                //         sendCatch[i++] = 0xa5;
                //         sendCatch[i++] = 0x05;
                //         sendCatch[i++] = 0x01;
                //         sendCatch[i++] = 0x01;
                //         sendCatch[i++] = 0x05;
                //         sendCatch[i++] = 0xf0;

                //         // Calculate_Sum_Tx(ref sendCatch, i);
                //         serialPort1.DiscardInBuffer();
                //         serialPort1.DiscardOutBuffer();
                //         bRxByteCnt = 0;
                //         bTotleBytes = 0;
                //         bRxFrameFinishFlag = false;
                //         // serialPort1.Write(sendCatch, 0, i + 2);
                //         serialPort1.Write(sendCatch, 0, i);
                //         serialPort1.DiscardInBuffer();
                //     }
                //     catch (Exception)
                //     {
                //         MessageBox.Show(s_MsgInfo[(int)Lang.MsgInfo.PortError],
                //             s_MsgInfo[(int)Lang.MsgInfo.OperationFailed],
                //             MessageBoxButtons.OK, MessageBoxIcon.Error);
                //         return;
                //     }
                    
                //     // g_u16RdSel = 0;

                //      u16RdRunInfoTxCnt++;
                // if (Lang.b_LangFlag == 0)
                // {
                //     toolStripLabel_TxCnt1.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt2.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt3.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt4.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt5.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     //toolStripLabel_TxCnt6.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                // }
                // else
                // {
                //     toolStripLabel_TxCnt1.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt2.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt3.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt4.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt5.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     //toolStripLabel_TxCnt6.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                // }
                //     return;
                // // //break;
                // case 6:
                //     read_chg_dsg_time();
                //     g_u16RdSel = 0;
                //      u16RdRunInfoTxCnt++;
                // if (Lang.b_LangFlag == 0)
                // {
                //     toolStripLabel_TxCnt1.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt2.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt3.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt4.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt5.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     //toolStripLabel_TxCnt6.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                // }
                // else
                // {
                //     toolStripLabel_TxCnt1.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt2.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt3.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt4.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     toolStripLabel_TxCnt5.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                //     //toolStripLabel_TxCnt6.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                // }
                //     return;

                default:
                    u16RegAddrTemp = 0xD000;
                    u16RegNumTemp = 36;
                    break;
            }

            try
            {
                //RS485_CMD_READ_REGS
                bRs485FunCmd = 0x03;                //这个单片机的精髓，就是不断修改这个值，开始去掉进入0x06死循环，后面加回来就正常了
                u16Rs485RegAddr = u16RegAddrTemp;   //我以前的感悟怎么这么傻
                u16Rs485RegNum = u16RegNumTemp;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
                u16RdRunInfoTxCnt++;
                if (Lang.b_LangFlag == 0)
                {
                    toolStripLabel_TxCnt1.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt2.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt3.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt4.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt5.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    //toolStripLabel_TxCnt6.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                }
                else
                {
                    toolStripLabel_TxCnt1.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt2.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt3.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt4.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt5.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    //toolStripLabel_TxCnt6.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                }
            }
            catch (Exception)
            {
                bRdCmdErrCnt++;
                if (bRdCmdErrCnt > 3)
                {
                    timer1.Enabled = false;
                    bRdCmdErrCnt = 0;
                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                    else
                        MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
                }
                return;
            }
        }
        public void DataReceiveDeal_tianhan_lianxing()
        {
           
            try
            {
                RxRdRunInfoAck_tianhanlianxing(bRxDataBuff);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "接收数据错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                bl_RxFinishedFlag = true;
            }

        }

        public void DataReceiveDeal_Modbus()
        {
            try
            {
                byte bTemp;
                if ((bRxDataBuff[0] != RS485_SLAVE_ADDR) && ((bRxDataBuff[0] != 0)))
                {
                    bRxByteCnt = 0;
                }
                else
                {
                    switch (bRs485FunCmd)
                    {
                        case 0x03:
                            {
                                if (bRxByteCnt >= 2)
                                {
                                    if (bRxDataBuff[1] == bRs485FunCmd)
                                    {
                                        if (bRxByteCnt >= 3)
                                        {
                                            bTotleBytes = (byte)(bRxDataBuff[2] + 5);
                                            if (bRxByteCnt >= bTotleBytes)
                                            {
                                                bRxFrameFinishFlag = true;
                                                bRxByteCnt = 0;
                                            }
                                        }
                                    }
                                    else if (bRxDataBuff[1] == (bRs485FunCmd | 0x80))
                                    {
                                        if (bRxByteCnt >= 5)
                                        {
                                            bRxFrameFinishFlag = true;
                                            bTotleBytes = bRxByteCnt;
                                            bRxByteCnt = 0;
                                        }
                                    }
                                    else
                                    {
                                        bRxByteCnt = 0;
                                    }
                                }
                                break;
                            }
                        case 0x06:
                        case 0x10:
                            {
                                if (bRxByteCnt >= 2)
                                {
                                    if (bRxDataBuff[1] == bRs485FunCmd)
                                    {
                                        if (bRxByteCnt >= 8)
                                        {
                                            bRxFrameFinishFlag = true;
                                            bTotleBytes = bRxByteCnt;
                                            bRxByteCnt = 0;
                                        }
                                    }
                                    else if (bRxDataBuff[1] == (bRs485FunCmd | 0x80))
                                    {
                                        if (bRxByteCnt >= 5)
                                        {
                                            bRxFrameFinishFlag = true;
                                            bTotleBytes = bRxByteCnt;
                                            bRxByteCnt = 0;
                                        }
                                    }
                                    else
                                    {
                                        bRxByteCnt = 0;
                                    }
                                }
                                break;
                            }
                        default:
                            bRxByteCnt = 0;
                            break;
                    }
                }

                if (bRxFrameFinishFlag)
                {
                    //加入接收CRC校验
                    bTemp = (byte)(bTotleBytes - 2);
                    if (Calculate_Sum_Rx(ref bRxDataBuff, bTemp))//CRC校验正确
                    {
                        bTemp = (byte)(bRxDataBuff[1] & 0xFF);
                        switch (bTemp)//功能码
                        {
                            //写多个寄存器
                            case 0x10:
                            case 0x90:
                                RxWrRegsAck(bRxDataBuff);
                                break;
                            //读寄存器
                            case 0x03:
                            case 0x83:
                                if (timer1.Enabled == true)
                                {
                                    RxRdRunInfoAck(bRxDataBuff);    //h很关键
                                }
                                else
                                {
                                    RxRdRegAck(bRxDataBuff);
                                    //MessageBox.Show("fuck", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                break;
                            //写单个寄存器
                            case 0x06:
                            case 0x86:
                                RxWrRegAck(bRxDataBuff);
                                break;
                            default:
                                break;
                        }
                    }
                    else//CRC校验错误
                    {
                        //if (Lang.b_LangFlag == 0)
                        //    MessageBox.Show("操作不成功，接收数据校验错误！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //else
                        //    MessageBox.Show("Mission Failed，Received data validation error！", "Error！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (10000 < Int32.Parse(toolStripLabel_CRCErrCnt.Text.ToString()))
                            toolStripLabel_CRCErrCnt.Text = "0";
                        else
                            toolStripLabel_CRCErrCnt.Text = (1 + Int32.Parse(toolStripLabel_CRCErrCnt.Text.ToString())).ToString();
                    }
                    bRxFrameFinishFlag = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "接收数据错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                bl_RxFinishedFlag = true;
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = this.serialPort1.BytesToRead;    //比较好奇，这串口能存多少个字符啊
                byte[] buff = new byte[bytesToRead];
                int bytesRead = 0;
                bytesRead = this.serialPort1.Read(buff, 0, bytesToRead);

                switch (BlueTooth_ComFlag)
                {
                    case 0:
                        for (int i = 0; i < bytesRead; i++)
                        {

                            if (bRxByteCnt >= 240)
                            {
                                bRxByteCnt = 0;
                                break;
                            }

                            bRxDataBuff[bRxByteCnt++] = buff[i];

                        }
                        // DataReceiveDeal_tianhan_lianxing();
                        DataReceiveDeal_Modbus();
                        break;

                    case 1:
                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (bRxByteCnt_BT >= 240)
                            {
                                break;
                            }
                            bRxDataBuff_BT[bRxByteCnt_BT++] = buff[i];
                        }
                        DataReceiveDeal_BlueTooth();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "串口出错，请检查串口连接是否正常！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //serialPort1.DiscardInBuffer();        //这句话谁TM留在这里的？？？？害我WIFI蓝牙通讯这么难搞
        }

        private void timer_Parallel_Tick(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                bRdCmdErrCnt++;
                if (bRdCmdErrCnt > 3)
                {
                    timer_Parallel.Enabled = false;
                    bRdCmdErrCnt = 0;
                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口错误，串口未打开", "错误提示！");
                    else
                        MessageBox.Show("Serial port error, Serial Port is not Start Up", "ErrorMessage！");
                }
                return;
            }

            switch (g_u16RdSel++)
            {
                case 0:
                    u16Rs485RegAddr = 0xC003;
                    u16Rs485RegNum = 80;
                    break;

                case 1:
                    u16Rs485RegAddr = 0xC004;
                    u16Rs485RegNum = 80;
                    break;
                case 2:
                    u16Rs485RegAddr = 0xC005;
                    u16Rs485RegNum = 80;
                    break;
                case 3:
                    u16Rs485RegAddr = 0xC006;
                    u16Rs485RegNum = 80;
                    break;
                case 4:
                    u16Rs485RegAddr = 0xC007;
                    u16Rs485RegNum = 80;
                    g_u16RdSel = 0;
                    break;

                default:
                    u16Rs485RegAddr = 0xC003;
                    u16Rs485RegNum = 80;
                    break;
            }

            try
            {
                bRs485FunCmd = 0x03;
                //u16Rs485RegAddr = 0xC003;
                //u16Rs485RegNum = 80;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();

                u16RdRunInfoTxCnt++;
                if (Lang.b_LangFlag == 0)
                {
                    toolStripLabel_TxCnt1.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt2.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt3.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt4.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt5.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    //toolStripLabel_TxCnt6.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt7.Text = "发送次数：" + Convert.ToString(u16RdRunInfoTxCnt);
                }
                else
                {
                    toolStripLabel_TxCnt1.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt2.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt3.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt4.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt5.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    //toolStripLabel_TxCnt6.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                    toolStripLabel_TxCnt7.Text = "Transmittion Counts：" + Convert.ToString(u16RdRunInfoTxCnt);
                }
            }
            catch (Exception)
            {
                bRdCmdErrCnt++;
                if (bRdCmdErrCnt > 3)
                {
                    timer1.Enabled = false;
                    bRdCmdErrCnt = 0;
                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                    else
                        MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
                }
                return;
            }
        }
        #endregion

        //第一二页按钮采用竞争通讯方式，不行按多几次。而不采用暂时性关闭时钟，返回再打开时钟方式
        #region 第一页按钮(0x1000系列)(功能开关控制为主)
        private void button_ProtectPresentClear_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_PROTECT_RECORD;
                u16Rs485RegData = 0x0001;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();

            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        public void System_Function_ON_Call()
        {
            //timer1.Enabled = false;
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYSTEM_FUNCTION_ON;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        public void System_Function_OFF_Call()
        {
            //timer1.Enabled = false;
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYSTEM_FUNCTION_OFF;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_Balance_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0001;
            System_Function_ON_Call();
        }

        private void button_SetSocOnce_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_SetSocOnce.Text.Trim();

            //验证数据是否合法
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;

            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 1; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 0; vfyObj.max = 100; vfyObj.unit = "*";
            if (!VerifyParams_ProtValidate(vfyObj)) return;

            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 1; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC;
            u16Rs485RegNum = u_Params[0];
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
            /*
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC;
            u16Rs485RegNum = (UInt16)(Single.Parse(textBox_SetSocOnce.Text));
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
            */
        }

        private void button_MosRelay_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0003;
            System_Function_ON_Call();
        }

        private void button_Relay_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0004;
            System_Function_ON_Call();
        }

        private void button_SocFixed_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0005;
            System_Function_ON_Call();
        }

        private void button_Heated_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0006;
            System_Function_ON_Call();
        }

        private void button_Cool_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0007;
            System_Function_ON_Call();
        }

        private void button_AFE1_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0008;
            System_Function_ON_Call();
        }

        private void button_AFE2_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0009;
            System_Function_ON_Call();
        }

        private void button_Sleep_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000A;
            System_Function_ON_Call();
        }

        private void button_SocZero_Func_Open_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000B;
            System_Function_ON_Call();
        }

        private void button_Balance_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0001;
            System_Function_OFF_Call();
        }

        private void button_BMS_Source_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0002;
            System_Function_OFF_Call();
        }

        private void button_MosRelay_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0003;
            System_Function_OFF_Call();
        }

        private void button_Relay_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0004;
            System_Function_OFF_Call();
        }

        private void button_SocFixed_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0005;
            System_Function_OFF_Call();
        }

        private void button_Heated_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0006;
            System_Function_OFF_Call();
        }

        private void button_Cool_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0007;
            System_Function_OFF_Call();
        }

        private void button_AFE1_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0008;
            System_Function_OFF_Call();
        }

        private void button_AFE2_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0009;
            System_Function_OFF_Call();
        }

        private void button_Sleep_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000A;
            System_Function_OFF_Call();
        }

        private void button_SocZero_Func_Close_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000B;
            System_Function_OFF_Call();
        }
        #endregion

        #region 第二页按钮(0x1000系列)(Switch按钮系列)
#if false
        public void Switch_High_Call()
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SWITCH_ON;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        public void Switch_Low_Call()
        {
            //timer1.Enabled = false;
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SWITCH_OFF;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_High_Switch1_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0001;
            Switch_High_Call();
        }

        private void button_High_Switch2_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0002;
            Switch_High_Call();
        }

        private void button_High_Switch3_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0003;
            Switch_High_Call();
        }

        private void button_High_Switch4_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0004;
            Switch_High_Call();
        }

        private void button_High_Switch5_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0005;
            Switch_High_Call();
        }

        private void button_High_Switch6_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0006;
            Switch_High_Call();
        }

        private void button_High_Switch7_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0007;
            Switch_High_Call();
        }

        private void button_High_Switch8_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0008;
            Switch_High_Call();
        }

        private void button_High_Switch9_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0009;
            Switch_High_Call();
        }

        private void button_High_Switch10_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000A;
            Switch_High_Call();
        }

        private void button_High_Switch11_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000B;
            Switch_High_Call();
        }

        private void button_High_Switch12_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000C;
            Switch_High_Call();
        }

        private void button_High_Switch13_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000D;
            Switch_High_Call();
        }

        private void button_High_Switch14_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000E;
            Switch_High_Call();
        }

        private void button_High_Switch15_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000F;
            Switch_High_Call();
        }

        private void button_High_Switch16_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0010;
            Switch_High_Call();
        }

        private void button_High_Switch17_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0011;
            Switch_High_Call();
        }

        private void button_High_Switch18_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0012;
            Switch_High_Call();
        }

        private void button_High_Switch19_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0013;
            Switch_High_Call();
        }

        private void button_High_Switch20_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0014;
            Switch_High_Call();
        }

        private void button_High_Switch21_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0015;
            Switch_High_Call();
        }

        private void button_High_Switch22_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0016;
            Switch_High_Call();
        }

        private void button_High_Switch23_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0017;
            Switch_High_Call();
        }

        private void button_High_Switch24_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0018;
            Switch_High_Call();
        }

        private void button_High_Switch25_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0019;
            Switch_High_Call();
        }

        private void button_High_Switch26_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001A;
            Switch_High_Call();
        }

        private void button_High_Switch27_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001B;
            Switch_High_Call();
        }

        private void button_High_Switch28_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001C;
            Switch_High_Call();
        }

        private void button_High_Switch29_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001D;
            Switch_High_Call();
        }

        private void button_High_Switch30_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001E;
            Switch_High_Call();
        }

        private void button_High_Switch31_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001F;
            Switch_High_Call();
        }

        private void button_High_Switch32_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0020;
            Switch_High_Call();
        }

        private void button_Low_Switch1_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0001;
            Switch_Low_Call();
        }

        private void button_Low_Switch2_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0002;
            Switch_Low_Call();
        }

        private void button_Low_Switch3_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0003;
            Switch_Low_Call();
        }

        private void button_Low_Switch4_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0004;
            Switch_Low_Call();
        }

        private void button_Low_Switch5_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0005;
            Switch_Low_Call();
        }

        private void button_Low_Switch6_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0006;
            Switch_Low_Call();
        }

        private void button_Low_Switch7_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0007;
            Switch_Low_Call();
        }

        private void button_Low_Switch8_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0008;
            Switch_Low_Call();
        }

        private void button_Low_Switch9_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0009;
            Switch_Low_Call();
        }

        private void button_Low_Switch10_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000A;
            Switch_Low_Call();
        }

        private void button_Low_Switch11_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000B;
            Switch_Low_Call();
        }

        private void button_Low_Switch12_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000C;
            Switch_Low_Call();
        }

        private void button_Low_Switch13_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000D;
            Switch_Low_Call();
        }

        private void button_Low_Switch14_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000E;
            Switch_Low_Call();
        }

        private void button_Low_Switch15_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x000F;
            Switch_Low_Call();
        }

        private void button_Low_Switch16_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0010;
            Switch_Low_Call();
        }

        private void button_Low_Switch17_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0011;
            Switch_Low_Call();
        }

        private void button_Low_Switch18_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0012;
            Switch_Low_Call();
        }

        private void button_Low_Switch19_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0013;
            Switch_Low_Call();
        }

        private void button_Low_Switch20_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0014;
            Switch_Low_Call();
        }

        private void button_Low_Switch21_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0015;
            Switch_Low_Call();
        }

        private void button_Low_Switch22_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0016;
            Switch_Low_Call();
        }

        private void button_Low_Switch23_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0017;
            Switch_Low_Call();
        }

        private void button_Low_Switch24_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0018;
            Switch_Low_Call();
        }

        private void button_Low_Switch25_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0019;
            Switch_Low_Call();
        }

        private void button_Low_Switch26_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001A;
            Switch_Low_Call();
        }

        private void button_Low_Switch27_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001B;
            Switch_Low_Call();
        }

        private void button_Low_Switch28_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001C;
            Switch_Low_Call();
        }

        private void button_Low_Switch29_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001D;
            Switch_Low_Call();
        }

        private void button_Low_Switch30_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001E;
            Switch_Low_Call();
        }

        private void button_Low_Switch31_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x001F;
            Switch_Low_Call();
        }

        private void button_Low_Switch32_Click(object sender, EventArgs e)
        {
            u16Rs485RegData = 0x0020;
            Switch_Low_Call();
        }
#endif
        #endregion

        #region 校准操作(校准页1)(0x2000)
        private void button_calc_cell1_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_1.Text == "") || (textBox_Vx2_1.Text == "") || (textBox_Vy1_1.Text == "") || (textBox_Vy2_1.Text == ""))
            {
                textBox_cail_cell1_k.Text = "";
                textBox_cail_cell1_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_1.Text);
            y1 = Single.Parse(textBox_Vy1_1.Text);
            x2 = Single.Parse(textBox_Vx2_1.Text);
            y2 = Single.Parse(textBox_Vy2_1.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell1_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell1_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell2_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_2.Text == "") || (textBox_Vx2_2.Text == "") || (textBox_Vy1_2.Text == "") || (textBox_Vy2_2.Text == ""))
            {
                textBox_cail_cell2_k.Text = "";
                textBox_cail_cell2_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end
;
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_2.Text);
            y1 = Single.Parse(textBox_Vy1_2.Text);
            x2 = Single.Parse(textBox_Vx2_2.Text);
            y2 = Single.Parse(textBox_Vy2_2.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell2_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell2_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell3_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_3.Text == "") || (textBox_Vx2_3.Text == "") || (textBox_Vy1_3.Text == "") || (textBox_Vy2_3.Text == ""))
            {
                textBox_cail_cell3_k.Text = "";
                textBox_cail_cell3_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end
;
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_3.Text);
            y1 = Single.Parse(textBox_Vy1_3.Text);
            x2 = Single.Parse(textBox_Vx2_3.Text);
            y2 = Single.Parse(textBox_Vy2_3.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell3_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell3_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell4_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_4.Text == "") || (textBox_Vx2_4.Text == "") || (textBox_Vy1_4.Text == "") || (textBox_Vy2_4.Text == ""))
            {
                textBox_cail_cell4_k.Text = "";
                textBox_cail_cell4_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_4.Text);
            y1 = Single.Parse(textBox_Vy1_4.Text);
            x2 = Single.Parse(textBox_Vx2_4.Text);
            y2 = Single.Parse(textBox_Vy2_4.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell4_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell4_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell5_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_5.Text == "") || (textBox_Vx2_5.Text == "") || (textBox_Vy1_5.Text == "") || (textBox_Vy2_5.Text == ""))
            {
                textBox_cail_cell5_k.Text = "";
                textBox_cail_cell5_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_5.Text);
            y1 = Single.Parse(textBox_Vy1_5.Text);
            x2 = Single.Parse(textBox_Vx2_5.Text);
            y2 = Single.Parse(textBox_Vy2_5.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell5_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell5_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell6_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_6.Text == "") || (textBox_Vx2_6.Text == "") || (textBox_Vy1_6.Text == "") || (textBox_Vy2_6.Text == ""))
            {
                textBox_cail_cell6_k.Text = "";
                textBox_cail_cell6_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_6.Text);
            y1 = Single.Parse(textBox_Vy1_6.Text);
            x2 = Single.Parse(textBox_Vx2_6.Text);
            y2 = Single.Parse(textBox_Vy2_6.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell6_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell6_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell7_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_7.Text == "") || (textBox_Vx2_7.Text == "") || (textBox_Vy1_7.Text == "") || (textBox_Vy2_7.Text == ""))
            {
                textBox_cail_cell7_k.Text = "";
                textBox_cail_cell7_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_7.Text);
            y1 = Single.Parse(textBox_Vy1_7.Text);
            x2 = Single.Parse(textBox_Vx2_7.Text);
            y2 = Single.Parse(textBox_Vy2_7.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell7_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell7_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell8_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_8.Text == "") || (textBox_Vx2_8.Text == "") || (textBox_Vy1_8.Text == "") || (textBox_Vy2_8.Text == ""))
            {
                textBox_cail_cell8_k.Text = "";
                textBox_cail_cell8_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_8.Text);
            y1 = Single.Parse(textBox_Vy1_8.Text);
            x2 = Single.Parse(textBox_Vx2_8.Text);
            y2 = Single.Parse(textBox_Vy2_8.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell8_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell8_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell9_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_9.Text == "") || (textBox_Vx2_9.Text == "") || (textBox_Vy1_9.Text == "") || (textBox_Vy2_9.Text == ""))
            {
                textBox_cail_cell9_k.Text = "";
                textBox_cail_cell9_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_9.Text);
            y1 = Single.Parse(textBox_Vy1_9.Text);
            x2 = Single.Parse(textBox_Vx2_9.Text);
            y2 = Single.Parse(textBox_Vy2_9.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell9_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell9_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell10_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_10.Text == "") || (textBox_Vx2_10.Text == "") || (textBox_Vy1_10.Text == "") || (textBox_Vy2_10.Text == ""))
            {
                textBox_cail_cell10_k.Text = "";
                textBox_cail_cell10_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_10.Text);
            y1 = Single.Parse(textBox_Vy1_10.Text);
            x2 = Single.Parse(textBox_Vx2_10.Text);
            y2 = Single.Parse(textBox_Vy2_10.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell10_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell10_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell11_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_11.Text == "") || (textBox_Vx2_11.Text == "") || (textBox_Vy1_11.Text == "") || (textBox_Vy2_11.Text == ""))
            {
                textBox_cail_cell11_k.Text = "";
                textBox_cail_cell11_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_11.Text);
            y1 = Single.Parse(textBox_Vy1_11.Text);
            x2 = Single.Parse(textBox_Vx2_11.Text);
            y2 = Single.Parse(textBox_Vy2_11.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell11_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell11_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell12_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_12.Text == "") || (textBox_Vx2_12.Text == "") || (textBox_Vy1_12.Text == "") || (textBox_Vy2_12.Text == ""))
            {
                textBox_cail_cell12_k.Text = "";
                textBox_cail_cell12_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_12.Text);
            y1 = Single.Parse(textBox_Vy1_12.Text);
            x2 = Single.Parse(textBox_Vx2_12.Text);
            y2 = Single.Parse(textBox_Vy2_12.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell12_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell12_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell13_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_13.Text == "") || (textBox_Vx2_13.Text == "") || (textBox_Vy1_13.Text == "") || (textBox_Vy2_13.Text == ""))
            {
                textBox_cail_cell13_k.Text = "";
                textBox_cail_cell13_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_13.Text);
            y1 = Single.Parse(textBox_Vy1_13.Text);
            x2 = Single.Parse(textBox_Vx2_13.Text);
            y2 = Single.Parse(textBox_Vy2_13.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell13_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell13_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell14_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_14.Text == "") || (textBox_Vx2_14.Text == "") || (textBox_Vy1_14.Text == "") || (textBox_Vy2_14.Text == ""))
            {
                textBox_cail_cell14_k.Text = "";
                textBox_cail_cell14_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_14.Text);
            y1 = Single.Parse(textBox_Vy1_14.Text);
            x2 = Single.Parse(textBox_Vx2_14.Text);
            y2 = Single.Parse(textBox_Vy2_14.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell14_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell14_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell15_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_15.Text == "") || (textBox_Vx2_15.Text == "") || (textBox_Vy1_15.Text == "") || (textBox_Vy2_15.Text == ""))
            {
                textBox_cail_cell15_k.Text = "";
                textBox_cail_cell15_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_15.Text);
            y1 = Single.Parse(textBox_Vy1_15.Text);
            x2 = Single.Parse(textBox_Vx2_15.Text);
            y2 = Single.Parse(textBox_Vy2_15.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell15_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell15_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell16_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_16.Text == "") || (textBox_Vx2_16.Text == "") || (textBox_Vy1_16.Text == "") || (textBox_Vy2_16.Text == ""))
            {
                textBox_cail_cell16_k.Text = "";
                textBox_cail_cell16_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_16.Text);
            y1 = Single.Parse(textBox_Vy1_16.Text);
            x2 = Single.Parse(textBox_Vx2_16.Text);
            y2 = Single.Parse(textBox_Vy2_16.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell16_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell16_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell17_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_17.Text == "") || (textBox_Vx2_17.Text == "") || (textBox_Vy1_17.Text == "") || (textBox_Vy2_17.Text == ""))
            {
                textBox_cail_cell17_k.Text = "";
                textBox_cail_cell17_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_17.Text);
            y1 = Single.Parse(textBox_Vy1_17.Text);
            x2 = Single.Parse(textBox_Vx2_17.Text);
            y2 = Single.Parse(textBox_Vy2_17.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell17_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell17_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell18_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_18.Text == "") || (textBox_Vx2_18.Text == "") || (textBox_Vy1_18.Text == "") || (textBox_Vy2_18.Text == ""))
            {
                textBox_cail_cell18_k.Text = "";
                textBox_cail_cell18_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_18.Text);
            y1 = Single.Parse(textBox_Vy1_18.Text);
            x2 = Single.Parse(textBox_Vx2_18.Text);
            y2 = Single.Parse(textBox_Vy2_18.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell18_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell18_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell19_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_19.Text == "") || (textBox_Vx2_19.Text == "") || (textBox_Vy1_19.Text == "") || (textBox_Vy2_19.Text == ""))
            {
                textBox_cail_cell19_k.Text = "";
                textBox_cail_cell19_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_19.Text);
            y1 = Single.Parse(textBox_Vy1_19.Text);
            x2 = Single.Parse(textBox_Vx2_19.Text);
            y2 = Single.Parse(textBox_Vy2_19.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell19_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell19_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell20_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_20.Text == "") || (textBox_Vx2_20.Text == "") || (textBox_Vy1_20.Text == "") || (textBox_Vy2_20.Text == ""))
            {
                textBox_cail_cell20_k.Text = "";
                textBox_cail_cell20_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_20.Text);
            y1 = Single.Parse(textBox_Vy1_20.Text);
            x2 = Single.Parse(textBox_Vx2_20.Text);
            y2 = Single.Parse(textBox_Vy2_20.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell20_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell20_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell21_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_21.Text == "") || (textBox_Vx2_21.Text == "") || (textBox_Vy1_21.Text == "") || (textBox_Vy2_21.Text == ""))
            {
                textBox_cail_cell21_k.Text = "";
                textBox_cail_cell21_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_21.Text);
            y1 = Single.Parse(textBox_Vy1_21.Text);
            x2 = Single.Parse(textBox_Vx2_21.Text);
            y2 = Single.Parse(textBox_Vy2_21.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell21_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell21_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell22_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_22.Text == "") || (textBox_Vx2_22.Text == "") || (textBox_Vy1_22.Text == "") || (textBox_Vy2_22.Text == ""))
            {
                textBox_cail_cell22_k.Text = "";
                textBox_cail_cell22_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_22.Text);
            y1 = Single.Parse(textBox_Vy1_22.Text);
            x2 = Single.Parse(textBox_Vx2_22.Text);
            y2 = Single.Parse(textBox_Vy2_22.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell22_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell22_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell23_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_23.Text == "") || (textBox_Vx2_23.Text == "") || (textBox_Vy1_23.Text == "") || (textBox_Vy2_23.Text == ""))
            {
                textBox_cail_cell23_k.Text = "";
                textBox_cail_cell23_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_23.Text);
            y1 = Single.Parse(textBox_Vy1_23.Text);
            x2 = Single.Parse(textBox_Vx2_23.Text);
            y2 = Single.Parse(textBox_Vy2_23.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell23_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell23_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell24_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_24.Text == "") || (textBox_Vx2_24.Text == "") || (textBox_Vy1_24.Text == "") || (textBox_Vy2_24.Text == ""))
            {
                textBox_cail_cell24_k.Text = "";
                textBox_cail_cell24_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_24.Text);
            y1 = Single.Parse(textBox_Vy1_24.Text);
            x2 = Single.Parse(textBox_Vx2_24.Text);
            y2 = Single.Parse(textBox_Vy2_24.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell24_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell24_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell25_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_25.Text == "") || (textBox_Vx2_25.Text == "") || (textBox_Vy1_25.Text == "") || (textBox_Vy2_25.Text == ""))
            {
                textBox_cail_cell25_k.Text = "";
                textBox_cail_cell25_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_25.Text);
            y1 = Single.Parse(textBox_Vy1_25.Text);
            x2 = Single.Parse(textBox_Vx2_25.Text);
            y2 = Single.Parse(textBox_Vy2_25.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell25_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell25_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell26_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_26.Text == "") || (textBox_Vx2_26.Text == "") || (textBox_Vy1_26.Text == "") || (textBox_Vy2_26.Text == ""))
            {
                textBox_cail_cell26_k.Text = "";
                textBox_cail_cell26_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_26.Text);
            y1 = Single.Parse(textBox_Vy1_26.Text);
            x2 = Single.Parse(textBox_Vx2_26.Text);
            y2 = Single.Parse(textBox_Vy2_26.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell26_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell26_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell27_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_27.Text == "") || (textBox_Vx2_27.Text == "") || (textBox_Vy1_27.Text == "") || (textBox_Vy2_27.Text == ""))
            {
                textBox_cail_cell27_k.Text = "";
                textBox_cail_cell27_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_27.Text);
            y1 = Single.Parse(textBox_Vy1_27.Text);
            x2 = Single.Parse(textBox_Vx2_27.Text);
            y2 = Single.Parse(textBox_Vy2_27.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell27_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell27_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell28_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_28.Text == "") || (textBox_Vx2_28.Text == "") || (textBox_Vy1_28.Text == "") || (textBox_Vy2_28.Text == ""))
            {
                textBox_cail_cell28_k.Text = "";
                textBox_cail_cell28_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_28.Text);
            y1 = Single.Parse(textBox_Vy1_28.Text);
            x2 = Single.Parse(textBox_Vx2_28.Text);
            y2 = Single.Parse(textBox_Vy2_28.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell28_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell28_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell29_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_29.Text == "") || (textBox_Vx2_29.Text == "") || (textBox_Vy1_29.Text == "") || (textBox_Vy2_29.Text == ""))
            {
                textBox_cail_cell29_k.Text = "";
                textBox_cail_cell29_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_29.Text);
            y1 = Single.Parse(textBox_Vy1_29.Text);
            x2 = Single.Parse(textBox_Vx2_29.Text);
            y2 = Single.Parse(textBox_Vy2_29.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell29_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell29_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell30_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_30.Text == "") || (textBox_Vx2_30.Text == "") || (textBox_Vy1_30.Text == "") || (textBox_Vy2_30.Text == ""))
            {
                textBox_cail_cell30_k.Text = "";
                textBox_cail_cell30_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_30.Text);
            y1 = Single.Parse(textBox_Vy1_30.Text);
            x2 = Single.Parse(textBox_Vx2_30.Text);
            y2 = Single.Parse(textBox_Vy2_30.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell30_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell30_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell31_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_31.Text == "") || (textBox_Vx2_31.Text == "") || (textBox_Vy1_31.Text == "") || (textBox_Vy2_31.Text == ""))
            {
                textBox_cail_cell31_k.Text = "";
                textBox_cail_cell31_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_31.Text);
            y1 = Single.Parse(textBox_Vy1_31.Text);
            x2 = Single.Parse(textBox_Vx2_31.Text);
            y2 = Single.Parse(textBox_Vy2_31.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell31_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell31_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_calc_cell32_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_32.Text == "") || (textBox_Vx2_32.Text == "") || (textBox_Vy1_32.Text == "") || (textBox_Vy2_32.Text == ""))
            {
                textBox_cail_cell32_k.Text = "";
                textBox_cail_cell32_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_32.Text);
            y1 = Single.Parse(textBox_Vy1_32.Text);
            x2 = Single.Parse(textBox_Vx2_32.Text);
            y2 = Single.Parse(textBox_Vy2_32.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_cell32_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_cell32_b.Text = Convert.ToDouble(b).ToString("0");
        }


        private void button_cali_cell1_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell1_k.Text == "") || (textBox_cail_cell1_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell1_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell1_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2000;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell2_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell2_k.Text == "") || (textBox_cail_cell2_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell2_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell2_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2002;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell3_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell3_k.Text == "") || (textBox_cail_cell3_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell3_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell3_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2004;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell4_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell4_k.Text == "") || (textBox_cail_cell4_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell4_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell4_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2006;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell5_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell5_k.Text == "") || (textBox_cail_cell5_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell5_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell5_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2008;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell6_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell6_k.Text == "") || (textBox_cail_cell6_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell6_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell6_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x200A;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell7_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell7_k.Text == "") || (textBox_cail_cell7_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell7_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell7_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x200C;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell8_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell8_k.Text == "") || (textBox_cail_cell8_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell8_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell8_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x200E;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell9_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell9_k.Text == "") || (textBox_cail_cell9_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell9_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell9_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2010;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell10_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell10_k.Text == "") || (textBox_cail_cell10_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell10_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell10_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2012;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell11_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell11_k.Text == "") || (textBox_cail_cell11_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell11_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell11_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2014;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell12_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell12_k.Text == "") || (textBox_cail_cell12_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell12_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell12_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2016;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell13_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell13_k.Text == "") || (textBox_cail_cell13_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell13_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell13_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x2018;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell14_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell14_k.Text == "") || (textBox_cail_cell14_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell14_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell14_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x201A;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell15_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell15_k.Text == "") || (textBox_cail_cell15_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell15_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell15_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x201C;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell16_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell16_k.Text == "") || (textBox_cail_cell16_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell16_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell16_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = 0x201E;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell17_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            if ((textBox_cail_cell17_k.Text == "") || (textBox_cail_cell17_b.Text == ""))
            {
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            UInt16 k, b;
            float f32B;


            k = (UInt16)((Single.Parse(textBox_cail_cell17_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell17_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC17CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_cali_cell18_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell18_k.Text == "") || (textBox_cail_cell18_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell18_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell18_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC18CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell19_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell19_k.Text == "") || (textBox_cail_cell19_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell19_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell19_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC19CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell20_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell20_k.Text == "") || (textBox_cail_cell20_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell20_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell20_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC20CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell21_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell21_k.Text == "") || (textBox_cail_cell21_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell21_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell21_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC21CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell22_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell22_k.Text == "") || (textBox_cail_cell22_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell22_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell22_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC22CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell23_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell23_k.Text == "") || (textBox_cail_cell23_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell23_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell23_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC23CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell24_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell24_k.Text == "") || (textBox_cail_cell24_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell24_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell24_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC24CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell25_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell25_k.Text == "") || (textBox_cail_cell25_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell25_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell25_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC25CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell26_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell26_k.Text == "") || (textBox_cail_cell26_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell26_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell26_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC26CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell27_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell27_k.Text == "") || (textBox_cail_cell27_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell27_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell27_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC27CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell28_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell28_k.Text == "") || (textBox_cail_cell28_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell28_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell28_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC28CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell29_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell29_k.Text == "") || (textBox_cail_cell29_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell29_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell29_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC29CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell30_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell30_k.Text == "") || (textBox_cail_cell30_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell30_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell30_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC30CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell31_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell31_k.Text == "") || (textBox_cail_cell31_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell31_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell31_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC31CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_cell32_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_cell32_k.Text == "") || (textBox_cail_cell32_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_cell32_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_cell32_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC32CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_read_all_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_cail_cell1_k.Text = "";
            textBox_cail_cell1_b.Text = "";

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC1CALIB_K;
                u16Rs485RegNum = 64;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AA;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }


        private void button_calc_AFE1_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_AFE1.Text == "") || (textBox_Vx2_AFE1.Text == "") || (textBox_Vy1_AFE1.Text == "") || (textBox_Vy2_AFE1.Text == ""))
            {
                textBox_cail_AFE1_k.Text = "";
                textBox_cail_AFE1_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_AFE1.Text);
            y1 = Single.Parse(textBox_Vy1_AFE1.Text);
            x2 = Single.Parse(textBox_Vx2_AFE1.Text);
            y2 = Single.Parse(textBox_Vy2_AFE1.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_AFE1_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_AFE1_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_cali_AFE1_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_AFE1_k.Text == "") || (textBox_cail_AFE1_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_AFE1_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_AFE1_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_AFE1CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_calc_AFE2_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_AFE2.Text == "") || (textBox_Vx2_AFE2.Text == "") || (textBox_Vy1_AFE2.Text == "") || (textBox_Vy2_AFE2.Text == ""))
            {
                textBox_cail_AFE2_k.Text = "";
                textBox_cail_AFE2_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_AFE2.Text);
            y1 = Single.Parse(textBox_Vy1_AFE2.Text);
            x2 = Single.Parse(textBox_Vx2_AFE2.Text);
            y2 = Single.Parse(textBox_Vy2_AFE2.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_AFE2_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_AFE2_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_cali_AFE2_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_AFE2_k.Text == "") || (textBox_cail_AFE2_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_AFE2_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_AFE2_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_AFE2CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_calc_Vbus_Click_1(object sender, EventArgs e)
        {
            if ((textBox_Vx1_Vbus.Text == "") || (textBox_Vx2_Vbus.Text == "") || (textBox_Vy1_Vbus.Text == "") || (textBox_Vy2_Vbus.Text == ""))
            {
                textBox_cail_Vbus_k.Text = "";
                textBox_cail_Vbus_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_Vbus.Text);
            y1 = Single.Parse(textBox_Vy1_Vbus.Text);
            x2 = Single.Parse(textBox_Vx2_Vbus.Text);
            y2 = Single.Parse(textBox_Vy2_Vbus.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_Vbus_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_Vbus_b.Text = Convert.ToDouble(b).ToString("0");
        }

        private void button_cali_Vbus_Click_1(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_Vbus_k.Text == "") || (textBox_cail_Vbus_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;
            k = (UInt16)((Single.Parse(textBox_cail_Vbus_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_Vbus_b.Text)) * 1);
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VBUSCALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_Read_Vafe_bus_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_cail_AFE1_k.Text = "";
            textBox_cail_AFE1_b.Text = "";

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_AFE1CALIB_K;
                u16Rs485RegNum = 6;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_Reset_AFE1_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AB;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_Reset_AFE2_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AC;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_Reset_Vbus_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AD;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }
        #endregion

        #region 校准操作(校准页2)(0x2000)
        private void button_calc_temp1_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp1.Text == "") || (textBox_Vx2_temp1.Text == "") || (textBox_Vy1_temp1.Text == "") || (textBox_Vy2_temp1.Text == ""))
            {
                textBox_temp1_k.Text = "";
                textBox_temp1_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp1.Text);
            y1 = Single.Parse(textBox_Vy1_temp1.Text);
            x2 = Single.Parse(textBox_Vx2_temp1.Text);
            y2 = Single.Parse(textBox_Vy2_temp1.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp1_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp1_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_temp2_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp2.Text == "") || (textBox_Vx2_temp2.Text == "") || (textBox_Vy1_temp2.Text == "") || (textBox_Vy2_temp2.Text == ""))
            {
                textBox_temp2_k.Text = "";
                textBox_temp2_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp2.Text);
            y1 = Single.Parse(textBox_Vy1_temp2.Text);
            x2 = Single.Parse(textBox_Vx2_temp2.Text);
            y2 = Single.Parse(textBox_Vy2_temp2.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp2_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp2_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_temp3_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp3.Text == "") || (textBox_Vx2_temp3.Text == "") || (textBox_Vy1_temp3.Text == "") || (textBox_Vy2_temp3.Text == ""))
            {
                textBox_temp3_k.Text = "";
                textBox_temp3_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp3.Text);
            y1 = Single.Parse(textBox_Vy1_temp3.Text);
            x2 = Single.Parse(textBox_Vx2_temp3.Text);
            y2 = Single.Parse(textBox_Vy2_temp3.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp3_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp3_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_temp4_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp4.Text == "") || (textBox_Vx2_temp4.Text == "") || (textBox_Vy1_temp4.Text == "") || (textBox_Vy2_temp4.Text == ""))
            {
                textBox_temp4_k.Text = "";
                textBox_temp4_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp4.Text);
            y1 = Single.Parse(textBox_Vy1_temp4.Text);
            x2 = Single.Parse(textBox_Vx2_temp4.Text);
            y2 = Single.Parse(textBox_Vy2_temp4.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp4_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp4_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_temp5_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp5.Text == "") || (textBox_Vx2_temp5.Text == "") || (textBox_Vy1_temp5.Text == "") || (textBox_Vy2_temp5.Text == ""))
            {
                textBox_temp5_k.Text = "";
                textBox_temp5_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp5.Text);
            y1 = Single.Parse(textBox_Vy1_temp5.Text);
            x2 = Single.Parse(textBox_Vx2_temp5.Text);
            y2 = Single.Parse(textBox_Vy2_temp5.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp5_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp5_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_temp6_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_temp6.Text == "") || (textBox_Vx2_temp6.Text == "") || (textBox_Vy1_temp6.Text == "") || (textBox_Vy2_temp6.Text == ""))
            {
                textBox_temp6_k.Text = "";
                textBox_temp6_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_temp6.Text);
            y1 = Single.Parse(textBox_Vy1_temp6.Text);
            x2 = Single.Parse(textBox_Vx2_temp6.Text);
            y2 = Single.Parse(textBox_Vy2_temp6.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp6_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp6_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_tempEnv1_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_tempEnv1.Text == "") || (textBox_Vx2_tempEnv1.Text == "") || (textBox_Vy1_tempEnv1.Text == "") || (textBox_Vy2_tempEnv1.Text == ""))
            {
                textBox_tempEnv1_k.Text = "";
                textBox_tempEnv1_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_tempEnv1.Text);
            y1 = Single.Parse(textBox_Vy1_tempEnv1.Text);
            x2 = Single.Parse(textBox_Vx2_tempEnv1.Text);
            y2 = Single.Parse(textBox_Vy2_tempEnv1.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_tempEnv1_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_tempEnv1_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_tempEnv2_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_tempEnv2.Text == "") || (textBox_Vx2_tempEnv2.Text == "") || (textBox_Vy1_tempEnv2.Text == "") || (textBox_Vy2_tempEnv2.Text == ""))
            {
                textBox_tempEnv2_k.Text = "";
                textBox_tempEnv2_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_tempEnv2.Text);
            y1 = Single.Parse(textBox_Vy1_tempEnv2.Text);
            x2 = Single.Parse(textBox_Vx2_tempEnv2.Text);
            y2 = Single.Parse(textBox_Vy2_tempEnv2.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_tempEnv2_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_tempEnv2_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_tempEnv3_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_tempEnv3.Text == "") || (textBox_Vx2_tempEnv3.Text == "") || (textBox_Vy1_tempEnv3.Text == "") || (textBox_Vy2_tempEnv3.Text == ""))
            {
                textBox_tempEnv3_k.Text = "";
                textBox_tempEnv3_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_tempEnv3.Text);
            y1 = Single.Parse(textBox_Vy1_tempEnv3.Text);
            x2 = Single.Parse(textBox_Vx2_tempEnv3.Text);
            y2 = Single.Parse(textBox_Vy2_tempEnv3.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_tempEnv3_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_tempEnv3_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_tempmos_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_tempMos.Text == "") || (textBox_Vx2_tempMos.Text == "") || (textBox_Vy1_tempMos.Text == "") || (textBox_Vy2_tempMos.Text == ""))
            {
                textBox_temp_mos_k.Text = "";
                textBox_temp_mos_b.Text = "";
                //MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //add end

                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_tempMos.Text);
            y1 = Single.Parse(textBox_Vy1_tempMos.Text);
            x2 = Single.Parse(textBox_Vx2_tempMos.Text);
            y2 = Single.Parse(textBox_Vy2_tempMos.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_temp_mos_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_temp_mos_b.Text = Convert.ToDouble(b).ToString("0.000");
        }


        private void button_cali_temp1_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp1_k.Text == "") || (textBox_temp1_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp1_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp1_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP1_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp2_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp2_k.Text == "") || (textBox_temp2_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp2_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp2_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP2_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp3_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp3_k.Text == "") || (textBox_temp3_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp3_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp3_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP3_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp4_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp4_k.Text == "") || (textBox_temp4_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp4_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp4_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP4_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp5_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp5_k.Text == "") || (textBox_temp5_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp5_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp5_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP5_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp6_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp6_k.Text == "") || (textBox_temp6_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp6_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp6_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP6_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_tempEnv1_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_tempEnv1_k.Text == "") || (textBox_tempEnv1_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_tempEnv1_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_tempEnv1_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP_ENV1_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_tempEnv2_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_tempEnv2_k.Text == "") || (textBox_tempEnv2_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_tempEnv2_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_tempEnv2_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP_ENV2_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_tempEnv3_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_tempEnv3_k.Text == "") || (textBox_tempEnv3_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_tempEnv3_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_tempEnv3_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP_ENV3_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_temp_mos_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_temp_mos_k.Text == "") || (textBox_temp_mos_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_temp_mos_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_temp_mos_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP_MOS_CALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_temp_read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_temp1_k.Text = "";
            textBox_temp1_b.Text = "";

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP1_CALIB_K;
                u16Rs485RegNum = 20;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_temp_reset_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AE;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_calc_Idsg_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_Idsg.Text == "") || (textBox_Vx2_Idsg.Text == "") || (textBox_Vy1_Idsg.Text == "") || (textBox_Vy2_Idsg.Text == ""))
            {
                textBox_cail_Idsg_k.Text = "";
                textBox_cail_Idsg_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_Idsg.Text);
            y1 = Single.Parse(textBox_Vy1_Idsg.Text);
            x2 = Single.Parse(textBox_Vx2_Idsg.Text);
            y2 = Single.Parse(textBox_Vy2_Idsg.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_Idsg_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_Idsg_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_calc_Ichg_Click(object sender, EventArgs e)
        {
            if ((textBox_Vx1_Ichg.Text == "") || (textBox_Vx2_Ichg.Text == "") || (textBox_Vy1_Ichg.Text == "") || (textBox_Vy2_Ichg.Text == ""))
            {
                textBox_cail_Ichg_k.Text = "";
                textBox_cail_Ichg_b.Text = "";
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            float x1, x2, y1, y2, k, b;
            x1 = Single.Parse(textBox_Vx1_Ichg.Text);
            y1 = Single.Parse(textBox_Vy1_Ichg.Text);
            x2 = Single.Parse(textBox_Vx2_Ichg.Text);
            y2 = Single.Parse(textBox_Vy2_Ichg.Text);

            //计算k和b
            k = (y2 - y1) / (x2 - x1);
            b = y1 - k * x1;

            textBox_cail_Ichg_k.Text = Convert.ToDouble(k).ToString("0.000");
            textBox_cail_Ichg_b.Text = Convert.ToDouble(b).ToString("0.000");
        }

        private void button_cali_Idsg_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_Idsg_k.Text == "") || (textBox_cail_Idsg_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_cail_Idsg_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_Idsg_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_IDISCHGCALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_cali_Ichg_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_cail_Ichg_k.Text == "") || (textBox_cail_Ichg_b.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("数据不能为空！", "计算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Data is empty！", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            UInt16 k, b;
            float f32B;

            k = (UInt16)((Single.Parse(textBox_cail_Ichg_k.Text)) * Math.Pow(2, 10));
            f32B = (float)((Single.Parse(textBox_cail_Ichg_b.Text)) * Math.Pow(2, 10));
            if (f32B >= 0)
            {
                b = (UInt16)f32B;
            }
            else
            {
                b = (UInt16)(((UInt16)(-f32B)) | 0x8000);
            }

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_ICHGCALIB_K;
                u16Rs485RegNum = 2;
                bRs485ByteNum = 4;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((k >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(k & 0x00ff);

                senddataTemp[i++] = (byte)((b >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(b & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_Current_read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_cail_Idsg_k.Text = "";
            textBox_cail_Idsg_b.Text = "";

            if (!serialPort1.IsOpen)
            {
                //MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_ICHGCALIB_K;
                u16Rs485RegNum = 4;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_Idsg_reset_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55AF;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_Ichg_reset_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF;
                u16Rs485RegData = 0x55B0;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }
        #endregion

        #region 保护点操作(0x2100)
        private void button_Protect_read_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VCELL_OVP_FIRST;
            u16Rs485RegNum = 65;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Protect_Reset_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_PROTECT_ELEMENT;
            u16Rs485RegNum = 0x0001;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_VcellOVP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            //初步验证数据是否合法，1：串口是否打开，2：数据是否为空，3：数据长度，不能超过5位
            Verify vfyObj = new Verify { len = 5 }; //decNum:默认0位小数位，即只能是整数
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VcellOVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VcellOVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VcellOVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VcellOVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VcellOVP_DelayT.Text.Trim();
            if (!VerifyParams_ProtUsual(vfyObj)) return;

            //判断数据范围是否合适
            for (vfyObj.idx = 0; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 1000; vfyObj.max = 5000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;

            //判断数据合理性，大小顺序
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;


            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)vfyObj.f_Params[vfyObj.idx];
            }
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VCELL_OVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_VcellUVP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VcellUVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VcellUVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VcellUVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VcellUVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VcellUVP_DelayT.Text.Trim();
            //验证数据是否合法
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 1000; vfyObj.max = 5000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)vfyObj.f_Params[vfyObj.idx];
            }
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VCELL_UVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_VbusOVP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 2 }; //两位小数
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VbusOVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VbusOVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VbusOVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VbusOVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VbusOVP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 3; vfyObj.max = 655; vfyObj.unit = "V";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 100);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VBUS_OVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_VbusUVP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 2 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VbusUVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VbusUVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VbusUVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VbusUVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VbusUVP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 3; vfyObj.max = 655; vfyObj.unit = "V";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 100);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VBUS_UVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_IchgOCP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_IchgOCP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_IchgOCP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_IchgOCP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_IchgOCP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_IchgOCP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 1; vfyObj.max = 5000; vfyObj.unit = "A";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_ICHG_OCP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_IdsgOCP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_IdsgOCP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_IdsgOCP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_IdsgOCP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_IdsgOCP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_IdsgOCP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 1; vfyObj.max = 5000; vfyObj.unit = "A";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_IDSG_OCP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_TchgOTP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_TchgOTP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_TchgOTP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_TchgOTP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_TchgOTP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_TchgOTP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 160; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx] + 40) * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TCHG_OTP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_TchgUTP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_TchgUTP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_TchgUTP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_TchgUTP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_TchgUTP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_TchgUTP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = -40; vfyObj.max = 40; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx] + 40) * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TCHG_UTP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_TdsgOTP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_TdsgOTP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_TdsgOTP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_TdsgOTP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_TdsgOTP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_TdsgOTP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 160; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx] + 40) * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TDSG_OTP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_TdsgUTP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_TdsgUTP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_TdsgUTP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_TdsgUTP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_TdsgUTP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_TdsgUTP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = -40; vfyObj.max = 40; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx] + 40) * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TDSG_UTP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_TmosOTP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 1 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_TmosOTP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_TmosOTP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_TmosOTP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_TmosOTP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_TmosOTP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波的表单时间验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 160; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx] + 40) * 10);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TMOS_OTP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_VdeltaOVP_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VdeltaOVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VdeltaOVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VdeltaOVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VdeltaOVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VdeltaOVP_DelayT.Text.Trim();
            //验证数据是否合法
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 10; vfyObj.max = 2000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VDELTA_OP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_SocUp_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_SocUp_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_SocUp_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_SocUp_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_SocUp_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_SocUp_DelayT.Text.Trim();
            //验证数据是否合法
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 50; vfyObj.unit = "%";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_UP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }
        #endregion

        #region 其它可设参数(0x2200，0x2300)
        //RS485_ADDR_RW_OTHER,0x2200
        private void button_SocTable_Read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };
            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_VOLTAGE1;
                u16Rs485RegNum = 42;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_SocTable_Set_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[100];
            float f32Volt1, f32Volt2, f32Volt3, f32Volt4, f32Volt5, f32Volt6, f32Volt7, f32Volt8, f32Volt9, f32Volt10, f32Volt11;
            float f32Volt12, f32Volt13, f32Volt14, f32Volt15, f32Volt16, f32Volt17, f32Volt18, f32Volt19, f32Volt20, f32Volt21;
            float f32Value1, f32Value2, f32Value3, f32Value4, f32Value5, f32Value6, f32Value7, f32Value8, f32Value9, f32Value10, f32Value11;
            float f32Value12, f32Value13, f32Value14, f32Value15, f32Value16, f32Value17, f32Value18, f32Value19, f32Value20, f32Value21;
            UInt16 Volt1, Volt2, Volt3, Volt4, Volt5, Volt6, Volt7, Volt8, Volt9, Volt10, Volt11;
            UInt16 Volt12, Volt13, Volt14, Volt15, Volt16, Volt17, Volt18, Volt19, Volt20, Volt21;
            UInt16 Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8, Value9, Value10, Value11;
            UInt16 Value12, Value13, Value14, Value15, Value16, Value17, Value18, Value19, Value20, Value21;

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_SocTable_Volt1.Text == "") || (textBox_SocTable_Volt2.Text == "") || (textBox_SocTable_Volt3.Text == "") ||
                (textBox_SocTable_Volt4.Text == "") || (textBox_SocTable_Volt5.Text == "") || (textBox_SocTable_Volt6.Text == "") ||
                (textBox_SocTable_Volt7.Text == "") || (textBox_SocTable_Volt8.Text == "") || (textBox_SocTable_Volt9.Text == "") ||
                (textBox_SocTable_Volt10.Text == "") || (textBox_SocTable_Volt11.Text == "") || (textBox_SocTable_Volt12.Text == "") ||
                (textBox_SocTable_Volt13.Text == "") || (textBox_SocTable_Volt14.Text == "") || (textBox_SocTable_Volt15.Text == "") ||
                (textBox_SocTable_Volt16.Text == "") || (textBox_SocTable_Volt17.Text == "") || (textBox_SocTable_Volt18.Text == "") ||
                (textBox_SocTable_Volt19.Text == "") || (textBox_SocTable_Volt20.Text == "") || (textBox_SocTable_Volt21.Text == "") ||

                (textBox_SocTable_Value1.Text == "") || (textBox_SocTable_Value2.Text == "") || (textBox_SocTable_Value3.Text == "") ||
                (textBox_SocTable_Value4.Text == "") || (textBox_SocTable_Value5.Text == "") || (textBox_SocTable_Value6.Text == "") ||
                (textBox_SocTable_Value7.Text == "") || (textBox_SocTable_Value8.Text == "") || (textBox_SocTable_Value9.Text == "") ||
                (textBox_SocTable_Value10.Text == "") || (textBox_SocTable_Value11.Text == "") || (textBox_SocTable_Value12.Text == "") ||
                (textBox_SocTable_Value13.Text == "") || (textBox_SocTable_Value14.Text == "") || (textBox_SocTable_Value15.Text == "") ||
                (textBox_SocTable_Value16.Text == "") || (textBox_SocTable_Value17.Text == "") || (textBox_SocTable_Value18.Text == "") ||
                (textBox_SocTable_Value19.Text == "") || (textBox_SocTable_Value20.Text == "") || (textBox_SocTable_Value21.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("请输入完整的信息", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Please enter all information", "ErrorMessage！");
                }
                return;
            }

            f32Volt1 = (Single.Parse(textBox_SocTable_Volt1.Text));
            f32Volt2 = (Single.Parse(textBox_SocTable_Volt2.Text));
            f32Volt3 = (Single.Parse(textBox_SocTable_Volt3.Text));
            f32Volt4 = (Single.Parse(textBox_SocTable_Volt4.Text));
            f32Volt5 = (Single.Parse(textBox_SocTable_Volt5.Text));
            f32Volt6 = (Single.Parse(textBox_SocTable_Volt6.Text));
            f32Volt7 = (Single.Parse(textBox_SocTable_Volt7.Text));
            f32Volt8 = (Single.Parse(textBox_SocTable_Volt8.Text));
            f32Volt9 = (Single.Parse(textBox_SocTable_Volt9.Text));
            f32Volt10 = (Single.Parse(textBox_SocTable_Volt10.Text));
            f32Volt11 = (Single.Parse(textBox_SocTable_Volt11.Text));
            f32Volt12 = (Single.Parse(textBox_SocTable_Volt12.Text));
            f32Volt13 = (Single.Parse(textBox_SocTable_Volt13.Text));
            f32Volt14 = (Single.Parse(textBox_SocTable_Volt14.Text));
            f32Volt15 = (Single.Parse(textBox_SocTable_Volt15.Text));
            f32Volt16 = (Single.Parse(textBox_SocTable_Volt16.Text));
            f32Volt17 = (Single.Parse(textBox_SocTable_Volt17.Text));
            f32Volt18 = (Single.Parse(textBox_SocTable_Volt18.Text));
            f32Volt19 = (Single.Parse(textBox_SocTable_Volt19.Text));
            f32Volt20 = (Single.Parse(textBox_SocTable_Volt20.Text));
            f32Volt21 = (Single.Parse(textBox_SocTable_Volt21.Text));

            f32Value1 = (Single.Parse(textBox_SocTable_Value1.Text));
            f32Value2 = (Single.Parse(textBox_SocTable_Value2.Text));
            f32Value3 = (Single.Parse(textBox_SocTable_Value3.Text));
            f32Value4 = (Single.Parse(textBox_SocTable_Value4.Text));
            f32Value5 = (Single.Parse(textBox_SocTable_Value5.Text));
            f32Value6 = (Single.Parse(textBox_SocTable_Value6.Text));
            f32Value7 = (Single.Parse(textBox_SocTable_Value7.Text));
            f32Value8 = (Single.Parse(textBox_SocTable_Value8.Text));
            f32Value9 = (Single.Parse(textBox_SocTable_Value9.Text));
            f32Value10 = (Single.Parse(textBox_SocTable_Value10.Text));
            f32Value11 = (Single.Parse(textBox_SocTable_Value11.Text));
            f32Value12 = (Single.Parse(textBox_SocTable_Value12.Text));
            f32Value13 = (Single.Parse(textBox_SocTable_Value13.Text));
            f32Value14 = (Single.Parse(textBox_SocTable_Value14.Text));
            f32Value15 = (Single.Parse(textBox_SocTable_Value15.Text));
            f32Value16 = (Single.Parse(textBox_SocTable_Value16.Text));
            f32Value17 = (Single.Parse(textBox_SocTable_Value17.Text));
            f32Value18 = (Single.Parse(textBox_SocTable_Value18.Text));
            f32Value19 = (Single.Parse(textBox_SocTable_Value19.Text));
            f32Value20 = (Single.Parse(textBox_SocTable_Value20.Text));
            f32Value21 = (Single.Parse(textBox_SocTable_Value21.Text));

            if ((f32Volt1 > 5000) || (f32Volt1 < 0) || (f32Volt2 > 5000) || (f32Volt2 < 0) || (f32Volt3 > 5000) || (f32Volt3 < 0) ||
                (f32Volt4 > 5000) || (f32Volt4 < 0) || (f32Volt5 > 5000) || (f32Volt5 < 0) || (f32Volt6 > 5000) || (f32Volt6 < 0) ||
                (f32Volt7 > 5000) || (f32Volt7 < 0) || (f32Volt8 > 5000) || (f32Volt8 < 0) || (f32Volt9 > 5000) || (f32Volt9 < 0) ||
                (f32Volt10 > 5000) || (f32Volt10 < 0) || (f32Volt11 > 5000) || (f32Volt11 < 0) || (f32Volt12 > 5000) || (f32Volt12 < 0) ||
                (f32Volt13 > 5000) || (f32Volt13 < 0) || (f32Volt14 > 5000) || (f32Volt14 < 0) || (f32Volt15 > 5000) || (f32Volt15 < 0) ||
                (f32Volt16 > 5000) || (f32Volt16 < 0) || (f32Volt17 > 5000) || (f32Volt17 < 0) || (f32Volt18 > 5000) || (f32Volt18 < 0) ||
                (f32Volt19 > 5000) || (f32Volt19 < 0) || (f32Volt20 > 5000) || (f32Volt20 < 0) || (f32Volt21 > 5000) || (f32Volt21 < 0))
            {
                MessageBox.Show("Soc表格电压点取值范围为0 - 5000mV，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((f32Value1 > 100) || (f32Value1 < 0) || (f32Value2 > 100) || (f32Value2 < 0) || (f32Value3 > 100) || (f32Value3 < 0) ||
                (f32Value4 > 100) || (f32Value4 < 0) || (f32Value5 > 100) || (f32Value5 < 0) || (f32Value6 > 100) || (f32Value6 < 0) ||
                (f32Value7 > 100) || (f32Value7 < 0) || (f32Value8 > 100) || (f32Value8 < 0) || (f32Value9 > 100) || (f32Value9 < 0) ||
                (f32Value10 > 100) || (f32Value10 < 0) || (f32Value11 > 100) || (f32Value11 < 0) || (f32Value12 > 100) || (f32Value12 < 0) ||
                (f32Value13 > 100) || (f32Value13 < 0) || (f32Value14 > 100) || (f32Value14 < 0) || (f32Value15 > 100) || (f32Value15 < 0) ||
                (f32Value16 > 100) || (f32Value16 < 0) || (f32Value17 > 100) || (f32Value17 < 0) || (f32Value18 > 100) || (f32Value18 < 0) ||
                (f32Value19 > 100) || (f32Value19 < 0) || (f32Value20 > 100) || (f32Value20 < 0) || (f32Value21 > 100) || (f32Value21 < 0))
            {
                MessageBox.Show("Soc表格Soc百分比取值范围为为0 - 100(%)，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Volt1 = (UInt16)(Single.Parse(textBox_SocTable_Volt1.Text));
            Volt2 = (UInt16)(Single.Parse(textBox_SocTable_Volt2.Text));
            Volt3 = (UInt16)(Single.Parse(textBox_SocTable_Volt3.Text));
            Volt4 = (UInt16)(Single.Parse(textBox_SocTable_Volt4.Text));
            Volt5 = (UInt16)(Single.Parse(textBox_SocTable_Volt5.Text));
            Volt6 = (UInt16)(Single.Parse(textBox_SocTable_Volt6.Text));
            Volt7 = (UInt16)(Single.Parse(textBox_SocTable_Volt7.Text));
            Volt8 = (UInt16)(Single.Parse(textBox_SocTable_Volt8.Text));
            Volt9 = (UInt16)(Single.Parse(textBox_SocTable_Volt9.Text));
            Volt10 = (UInt16)(Single.Parse(textBox_SocTable_Volt10.Text));
            Volt11 = (UInt16)(Single.Parse(textBox_SocTable_Volt11.Text));
            Volt12 = (UInt16)(Single.Parse(textBox_SocTable_Volt12.Text));
            Volt13 = (UInt16)(Single.Parse(textBox_SocTable_Volt13.Text));
            Volt14 = (UInt16)(Single.Parse(textBox_SocTable_Volt14.Text));
            Volt15 = (UInt16)(Single.Parse(textBox_SocTable_Volt15.Text));
            Volt16 = (UInt16)(Single.Parse(textBox_SocTable_Volt16.Text));
            Volt17 = (UInt16)(Single.Parse(textBox_SocTable_Volt17.Text));
            Volt18 = (UInt16)(Single.Parse(textBox_SocTable_Volt18.Text));
            Volt19 = (UInt16)(Single.Parse(textBox_SocTable_Volt19.Text));
            Volt20 = (UInt16)(Single.Parse(textBox_SocTable_Volt20.Text));
            Volt21 = (UInt16)(Single.Parse(textBox_SocTable_Volt21.Text));

            Value1 = (UInt16)(Single.Parse(textBox_SocTable_Value1.Text));
            Value2 = (UInt16)(Single.Parse(textBox_SocTable_Value2.Text));
            Value3 = (UInt16)(Single.Parse(textBox_SocTable_Value3.Text));
            Value4 = (UInt16)(Single.Parse(textBox_SocTable_Value4.Text));
            Value5 = (UInt16)(Single.Parse(textBox_SocTable_Value5.Text));
            Value6 = (UInt16)(Single.Parse(textBox_SocTable_Value6.Text));
            Value7 = (UInt16)(Single.Parse(textBox_SocTable_Value7.Text));
            Value8 = (UInt16)(Single.Parse(textBox_SocTable_Value8.Text));
            Value9 = (UInt16)(Single.Parse(textBox_SocTable_Value9.Text));
            Value10 = (UInt16)(Single.Parse(textBox_SocTable_Value10.Text));
            Value11 = (UInt16)(Single.Parse(textBox_SocTable_Value11.Text));
            Value12 = (UInt16)(Single.Parse(textBox_SocTable_Value12.Text));
            Value13 = (UInt16)(Single.Parse(textBox_SocTable_Value13.Text));
            Value14 = (UInt16)(Single.Parse(textBox_SocTable_Value14.Text));
            Value15 = (UInt16)(Single.Parse(textBox_SocTable_Value15.Text));
            Value16 = (UInt16)(Single.Parse(textBox_SocTable_Value16.Text));
            Value17 = (UInt16)(Single.Parse(textBox_SocTable_Value17.Text));
            Value18 = (UInt16)(Single.Parse(textBox_SocTable_Value18.Text));
            Value19 = (UInt16)(Single.Parse(textBox_SocTable_Value19.Text));
            Value20 = (UInt16)(Single.Parse(textBox_SocTable_Value20.Text));
            Value21 = (UInt16)(Single.Parse(textBox_SocTable_Value21.Text));

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_VOLTAGE1;
                u16Rs485RegNum = 42;
                bRs485ByteNum = 84;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((Volt1 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt1 & 0x00ff);
                senddataTemp[i++] = (byte)((Value1 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value1 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt2 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt2 & 0x00ff);
                senddataTemp[i++] = (byte)((Value2 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value2 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt3 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt3 & 0x00ff);
                senddataTemp[i++] = (byte)((Value3 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value3 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt4 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt4 & 0x00ff);
                senddataTemp[i++] = (byte)((Value4 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value4 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt5 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt5 & 0x00ff);
                senddataTemp[i++] = (byte)((Value5 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value5 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt6 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt6 & 0x00ff);
                senddataTemp[i++] = (byte)((Value6 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value6 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt7 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt7 & 0x00ff);
                senddataTemp[i++] = (byte)((Value7 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value7 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt8 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt8 & 0x00ff);
                senddataTemp[i++] = (byte)((Value8 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value8 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt9 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt9 & 0x00ff);
                senddataTemp[i++] = (byte)((Value9 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value9 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt10 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt10 & 0x00ff);
                senddataTemp[i++] = (byte)((Value10 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value10 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt11 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt11 & 0x00ff);
                senddataTemp[i++] = (byte)((Value11 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value11 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt12 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt12 & 0x00ff);
                senddataTemp[i++] = (byte)((Value12 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value12 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt13 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt13 & 0x00ff);
                senddataTemp[i++] = (byte)((Value13 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value13 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt14 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt14 & 0x00ff);
                senddataTemp[i++] = (byte)((Value14 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value14 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt15 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt15 & 0x00ff);
                senddataTemp[i++] = (byte)((Value15 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value15 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt16 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt16 & 0x00ff);
                senddataTemp[i++] = (byte)((Value16 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value16 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt17 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt17 & 0x00ff);
                senddataTemp[i++] = (byte)((Value17 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value17 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt18 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt18 & 0x00ff);
                senddataTemp[i++] = (byte)((Value18 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value18 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt19 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt19 & 0x00ff);
                senddataTemp[i++] = (byte)((Value19 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value19 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt20 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt20 & 0x00ff);
                senddataTemp[i++] = (byte)((Value20 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value20 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt21 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt21 & 0x00ff);
                senddataTemp[i++] = (byte)((Value21 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value21 & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_CopperLoss_read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_CopperLoss1.Text = "";
            textBox_CopperLoss2.Text = "";
            textBox_CopperLoss3.Text = "";
            textBox_CopperLoss4.Text = "";
            textBox_CopperLoss5.Text = "";
            textBox_CopperLoss6.Text = "";
            textBox_CopperLoss7.Text = "";
            textBox_CopperLoss8.Text = "";

            textBox_CellNum1.Text = "";
            textBox_CellNum2.Text = "";
            textBox_CellNum3.Text = "";
            textBox_CellNum4.Text = "";
            textBox_CellNum5.Text = "";
            textBox_CellNum6.Text = "";
            textBox_CellNum7.Text = "";
            textBox_CellNum8.Text = "";

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_COPPERLOSS1;
                u16Rs485RegNum = 32;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_CopperLoss_set_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[100];
            float f32Volt1, f32Volt2, f32Volt3, f32Volt4, f32Volt5, f32Volt6, f32Volt7, f32Volt8, f32Volt9, f32Volt10, f32Volt11;
            float f32Volt12, f32Volt13, f32Volt14, f32Volt15, f32Volt16;
            float f32Value1, f32Value2, f32Value3, f32Value4, f32Value5, f32Value6, f32Value7, f32Value8, f32Value9, f32Value10, f32Value11;
            float f32Value12, f32Value13, f32Value14, f32Value15, f32Value16;
            UInt16 Volt1, Volt2, Volt3, Volt4, Volt5, Volt6, Volt7, Volt8, Volt9, Volt10, Volt11;
            UInt16 Volt12, Volt13, Volt14, Volt15, Volt16;
            UInt16 Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8, Value9, Value10, Value11;
            UInt16 Value12, Value13, Value14, Value15, Value16;

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_CopperLoss1.Text == "") || (textBox_CopperLoss2.Text == "") || (textBox_CopperLoss3.Text == "") ||
                (textBox_CopperLoss4.Text == "") || (textBox_CopperLoss5.Text == "") || (textBox_CopperLoss6.Text == "") ||
                (textBox_CopperLoss7.Text == "") || (textBox_CopperLoss8.Text == "") || (textBox_CopperLoss9.Text == "") ||
                (textBox_CopperLoss10.Text == "") || (textBox_CopperLoss11.Text == "") || (textBox_CopperLoss12.Text == "") ||
                (textBox_CopperLoss13.Text == "") || (textBox_CopperLoss14.Text == "") || (textBox_CopperLoss15.Text == "") ||
                (textBox_CopperLoss16.Text == "") ||

                (textBox_CellNum1.Text == "") || (textBox_CellNum2.Text == "") || (textBox_CellNum3.Text == "") ||
                (textBox_CellNum4.Text == "") || (textBox_CellNum5.Text == "") || (textBox_CellNum6.Text == "") ||
                (textBox_CellNum7.Text == "") || (textBox_CellNum8.Text == "") || (textBox_CellNum9.Text == "") ||
                (textBox_CellNum10.Text == "") || (textBox_CellNum11.Text == "") || (textBox_CellNum12.Text == "") ||
                (textBox_CellNum13.Text == "") || (textBox_CellNum14.Text == "") || (textBox_CellNum15.Text == "") ||
                (textBox_CellNum16.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("请输入完整的信息", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Please enter all information", "ErrorMessage！");
                }
                return;
            }

            f32Volt1 = Single.Parse(textBox_CopperLoss1.Text);
            f32Volt2 = (Single.Parse(textBox_CopperLoss2.Text));
            f32Volt3 = (Single.Parse(textBox_CopperLoss3.Text));
            f32Volt4 = (Single.Parse(textBox_CopperLoss4.Text));
            f32Volt5 = (Single.Parse(textBox_CopperLoss5.Text));
            f32Volt6 = (Single.Parse(textBox_CopperLoss6.Text));
            f32Volt7 = (Single.Parse(textBox_CopperLoss7.Text));
            f32Volt8 = (Single.Parse(textBox_CopperLoss8.Text));
            f32Volt9 = (Single.Parse(textBox_CopperLoss9.Text));
            f32Volt10 = (Single.Parse(textBox_CopperLoss10.Text));
            f32Volt11 = (Single.Parse(textBox_CopperLoss11.Text));
            f32Volt12 = (Single.Parse(textBox_CopperLoss12.Text));
            f32Volt13 = (Single.Parse(textBox_CopperLoss13.Text));
            f32Volt14 = (Single.Parse(textBox_CopperLoss14.Text));
            f32Volt15 = (Single.Parse(textBox_CopperLoss15.Text));
            f32Volt16 = (Single.Parse(textBox_CopperLoss16.Text));

            f32Value1 = (Single.Parse(textBox_CellNum1.Text));
            f32Value2 = (Single.Parse(textBox_CellNum2.Text));
            f32Value3 = (Single.Parse(textBox_CellNum3.Text));
            f32Value4 = (Single.Parse(textBox_CellNum4.Text));
            f32Value5 = (Single.Parse(textBox_CellNum5.Text));
            f32Value6 = (Single.Parse(textBox_CellNum6.Text));
            f32Value7 = (Single.Parse(textBox_CellNum7.Text));
            f32Value8 = (Single.Parse(textBox_CellNum8.Text));
            f32Value9 = (Single.Parse(textBox_CellNum9.Text));
            f32Value10 = (Single.Parse(textBox_CellNum10.Text));
            f32Value11 = (Single.Parse(textBox_CellNum11.Text));
            f32Value12 = (Single.Parse(textBox_CellNum12.Text));
            f32Value13 = (Single.Parse(textBox_CellNum13.Text));
            f32Value14 = (Single.Parse(textBox_CellNum14.Text));
            f32Value15 = (Single.Parse(textBox_CellNum15.Text));
            f32Value16 = (Single.Parse(textBox_CellNum16.Text));

            if ((f32Volt1 > 10000) || (f32Volt1 < 0) || (f32Volt2 > 10000) || (f32Volt2 < 0) || (f32Volt3 > 10000) || (f32Volt3 < 0) ||
                (f32Volt4 > 10000) || (f32Volt4 < 0) || (f32Volt5 > 10000) || (f32Volt5 < 0) || (f32Volt6 > 10000) || (f32Volt6 < 0) ||
                (f32Volt7 > 10000) || (f32Volt7 < 0) || (f32Volt8 > 10000) || (f32Volt8 < 0) || (f32Volt9 > 10000) || (f32Volt9 < 0) ||
                (f32Volt10 > 10000) || (f32Volt10 < 0) || (f32Volt11 > 10000) || (f32Volt11 < 0) || (f32Volt12 > 10000) || (f32Volt12 < 0) ||
                (f32Volt13 > 10000) || (f32Volt13 < 0) || (f32Volt14 > 10000) || (f32Volt14 < 0) || (f32Volt15 > 10000) || (f32Volt15 < 0) ||
                (f32Volt16 > 10000) || (f32Volt16 < 0))
            {
                MessageBox.Show("铜损补偿电阻取值范围为0 - 10000uΩ，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((f32Value1 > 32) || (f32Value1 < 0) || (f32Value2 > 32) || (f32Value2 < 0) || (f32Value3 > 32) || (f32Value3 < 0) ||
                (f32Value4 > 32) || (f32Value4 < 0) || (f32Value5 > 32) || (f32Value5 < 0) || (f32Value6 > 32) || (f32Value6 < 0) ||
                (f32Value7 > 32) || (f32Value7 < 0) || (f32Value8 > 32) || (f32Value8 < 0) || (f32Value9 > 32) || (f32Value9 < 0) ||
                (f32Value10 > 32) || (f32Value10 < 0) || (f32Value11 > 32) || (f32Value11 < 0) || (f32Value12 > 32) || (f32Value12 < 0) ||
                (f32Value13 > 32) || (f32Value13 < 0) || (f32Value14 > 32) || (f32Value14 < 0) || (f32Value15 > 32) || (f32Value15 < 0) ||
                (f32Value16 > 32) || (f32Value16 < 0))
            {
                MessageBox.Show("铜损补偿串数取值范围为为0 - 32，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            Volt1 = (UInt16)(Single.Parse(textBox_CopperLoss1.Text));
            Volt2 = (UInt16)(Single.Parse(textBox_CopperLoss2.Text));
            Volt3 = (UInt16)(Single.Parse(textBox_CopperLoss3.Text));
            Volt4 = (UInt16)(Single.Parse(textBox_CopperLoss4.Text));
            Volt5 = (UInt16)(Single.Parse(textBox_CopperLoss5.Text));
            Volt6 = (UInt16)(Single.Parse(textBox_CopperLoss6.Text));
            Volt7 = (UInt16)(Single.Parse(textBox_CopperLoss7.Text));
            Volt8 = (UInt16)(Single.Parse(textBox_CopperLoss8.Text));
            Volt9 = (UInt16)(Single.Parse(textBox_CopperLoss9.Text));
            Volt10 = (UInt16)(Single.Parse(textBox_CopperLoss10.Text));
            Volt11 = (UInt16)(Single.Parse(textBox_CopperLoss11.Text));
            Volt12 = (UInt16)(Single.Parse(textBox_CopperLoss12.Text));
            Volt13 = (UInt16)(Single.Parse(textBox_CopperLoss13.Text));
            Volt14 = (UInt16)(Single.Parse(textBox_CopperLoss14.Text));
            Volt15 = (UInt16)(Single.Parse(textBox_CopperLoss15.Text));
            Volt16 = (UInt16)(Single.Parse(textBox_CopperLoss16.Text));

            Value1 = (UInt16)(Single.Parse(textBox_CellNum1.Text));
            Value2 = (UInt16)(Single.Parse(textBox_CellNum2.Text));
            Value3 = (UInt16)(Single.Parse(textBox_CellNum3.Text));
            Value4 = (UInt16)(Single.Parse(textBox_CellNum4.Text));
            Value5 = (UInt16)(Single.Parse(textBox_CellNum5.Text));
            Value6 = (UInt16)(Single.Parse(textBox_CellNum6.Text));
            Value7 = (UInt16)(Single.Parse(textBox_CellNum7.Text));
            Value8 = (UInt16)(Single.Parse(textBox_CellNum8.Text));
            Value9 = (UInt16)(Single.Parse(textBox_CellNum9.Text));
            Value10 = (UInt16)(Single.Parse(textBox_CellNum10.Text));
            Value11 = (UInt16)(Single.Parse(textBox_CellNum11.Text));
            Value12 = (UInt16)(Single.Parse(textBox_CellNum12.Text));
            Value13 = (UInt16)(Single.Parse(textBox_CellNum13.Text));
            Value14 = (UInt16)(Single.Parse(textBox_CellNum14.Text));
            Value15 = (UInt16)(Single.Parse(textBox_CellNum15.Text));
            Value16 = (UInt16)(Single.Parse(textBox_CellNum16.Text));

            try
            {
                byte i;

                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_COPPERLOSS1;
                u16Rs485RegNum = 32;
                bRs485ByteNum = 64;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((Volt1 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt1 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt2 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt2 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt3 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt3 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt4 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt4 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt5 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt5 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt6 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt6 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt7 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt7 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt8 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt8 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt9 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt9 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt10 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt10 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt11 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt11 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt12 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt12 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt13 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt13 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt14 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt14 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt15 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt15 & 0x00ff);
                senddataTemp[i++] = (byte)((Volt16 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt16 & 0x00ff);


                senddataTemp[i++] = (byte)((Value1 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value1 & 0x00ff);
                senddataTemp[i++] = (byte)((Value2 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value2 & 0x00ff);
                senddataTemp[i++] = (byte)((Value3 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value3 & 0x00ff);
                senddataTemp[i++] = (byte)((Value4 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value4 & 0x00ff);
                senddataTemp[i++] = (byte)((Value5 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value5 & 0x00ff);
                senddataTemp[i++] = (byte)((Value6 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value6 & 0x00ff);
                senddataTemp[i++] = (byte)((Value7 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value7 & 0x00ff);
                senddataTemp[i++] = (byte)((Value8 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value8 & 0x00ff);
                senddataTemp[i++] = (byte)((Value9 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value9 & 0x00ff);
                senddataTemp[i++] = (byte)((Value10 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value10 & 0x00ff);
                senddataTemp[i++] = (byte)((Value11 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value11 & 0x00ff);
                senddataTemp[i++] = (byte)((Value12 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value12 & 0x00ff);
                senddataTemp[i++] = (byte)((Value13 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value13 & 0x00ff);
                senddataTemp[i++] = (byte)((Value14 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value14 & 0x00ff);
                senddataTemp[i++] = (byte)((Value15 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value15 & 0x00ff);
                senddataTemp[i++] = (byte)((Value16 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Value16 & 0x00ff);


                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_RTC_Read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_RTC_Time_Year.Text = "";
            textBox_RTC_Time_Month.Text = "";
            textBox_RTC_Time_Day.Text = "";
            textBox_RTC_Time_Hour.Text = "";
            textBox_RTC_Time_Minute.Text = "";
            textBox_RTC_Time_Second.Text = "";

            textBox_RTC_Alarm_Year.Text = "";
            textBox_RTC_Alarm_Month.Text = "";
            textBox_RTC_Alarm_Day.Text = "";
            textBox_RTC_Alarm_Hour.Text = "";
            textBox_RTC_Alarm_Minute.Text = "";
            textBox_RTC_Alarm_Second.Text = "";

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RTC_TIME_YEAR;
                u16Rs485RegNum = 12;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void button_RTC_Set_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[50];
            float f32Volt1, f32Volt2, f32Volt3, f32Volt4, f32Volt5, f32Volt6, f32Volt7, f32Volt8, f32Volt9, f32Volt10, f32Volt11, f32Volt12;
            UInt16 Volt1, Volt2, Volt3, Volt4, Volt5, Volt6, Volt7, Volt8, Volt9, Volt10, Volt11, Volt12;


            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }

            if ((textBox_RTC_Time_Year.Text == "") || (textBox_RTC_Time_Month.Text == "")
                || (textBox_RTC_Time_Day.Text == "") || (textBox_RTC_Time_Hour.Text == "")
                || (textBox_RTC_Time_Minute.Text == "") || (textBox_RTC_Time_Second.Text == "")
                || (textBox_RTC_Alarm_Year.Text == "") || (textBox_RTC_Alarm_Month.Text == "")
                || (textBox_RTC_Alarm_Day.Text == "") || (textBox_RTC_Alarm_Hour.Text == "")
                || (textBox_RTC_Alarm_Minute.Text == "") || (textBox_RTC_Alarm_Second.Text == ""))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("请输入完整的信息", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Please enter all information", "ErrorMessage！");
                }
                return;
            }

            f32Volt1 = Single.Parse(textBox_RTC_Time_Year.Text);
            f32Volt2 = (Single.Parse(textBox_RTC_Time_Month.Text));
            f32Volt3 = (Single.Parse(textBox_RTC_Time_Day.Text));
            f32Volt4 = (Single.Parse(textBox_RTC_Time_Hour.Text));
            f32Volt5 = (Single.Parse(textBox_RTC_Time_Minute.Text));
            f32Volt6 = (Single.Parse(textBox_RTC_Time_Second.Text));

            f32Volt7 = (Single.Parse(textBox_RTC_Alarm_Year.Text));
            f32Volt8 = (Single.Parse(textBox_RTC_Alarm_Month.Text));
            f32Volt9 = (Single.Parse(textBox_RTC_Alarm_Day.Text));
            f32Volt10 = (Single.Parse(textBox_RTC_Alarm_Hour.Text));
            f32Volt11 = (Single.Parse(textBox_RTC_Alarm_Minute.Text));
            f32Volt12 = (Single.Parse(textBox_RTC_Alarm_Second.Text));

            if ((f32Volt1 > 99) || (f32Volt1 < 0) || (f32Volt7 > 99) || (f32Volt7 < 0))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC年(包括闹钟)设置范围为0 - 99，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC year range is 0 - 99, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if ((f32Volt2 > 12) || (f32Volt2 < 1) || (f32Volt8 > 12) || (f32Volt8 < 1))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC月(包括闹钟)设置范围为1 - 12，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC month range is 1 - 12, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if ((f32Volt3 > 31) || (f32Volt3 < 1) || (f32Volt9 > 31) || (f32Volt9 < 1))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC日(包括闹钟)设置范围为1 - 31，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC day range is 1 - 31, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if ((f32Volt4 > 23) || (f32Volt4 < 0) || (f32Volt10 > 23) || (f32Volt10 < 0))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC时(包括闹钟)设置范围为0 - 23，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC hour range is 0 - 23, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if ((f32Volt5 > 59) || (f32Volt5 < 0) || (f32Volt11 > 59) || (f32Volt11 < 0))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC分(包括闹钟)设置范围为0 - 59，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC minute range is 0 - 59, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if ((f32Volt6 > 59) || (f32Volt6 < 0) || (f32Volt12 > 59) || (f32Volt12 < 0))
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("RTC秒(包括闹钟)设置范围为0 - 59，数据越界！", "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("The RTC second range is 0 - 59, and the data is out of bounds！",
                        "Setting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            Volt1 = (UInt16)(Single.Parse(textBox_RTC_Time_Year.Text));
            Volt2 = (UInt16)(Single.Parse(textBox_RTC_Time_Month.Text));
            Volt3 = (UInt16)(Single.Parse(textBox_RTC_Time_Day.Text));
            Volt4 = (UInt16)(Single.Parse(textBox_RTC_Time_Hour.Text));
            Volt5 = (UInt16)(Single.Parse(textBox_RTC_Time_Minute.Text));
            Volt6 = (UInt16)(Single.Parse(textBox_RTC_Time_Second.Text));

            Volt7 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Year.Text));
            Volt8 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Month.Text));
            Volt9 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Day.Text));
            Volt10 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Hour.Text));
            Volt11 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Minute.Text));
            Volt12 = (UInt16)(Single.Parse(textBox_RTC_Alarm_Second.Text));

            try
            {
                byte i;
                bRs485FunCmd = 0x10;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RTC_TIME_YEAR;
                u16Rs485RegNum = 12;
                bRs485ByteNum = 24;

                i = 0;
                senddataTemp[i++] = RS485_SLAVE_ADDR;
                senddataTemp[i++] = bRs485FunCmd;
                senddataTemp[i++] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[i++] = (byte)(u16Rs485RegNum % 256);
                senddataTemp[i++] = bRs485ByteNum;

                senddataTemp[i++] = (byte)((Volt1 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt1 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt2 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt2 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt3 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt3 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt4 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt4 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt5 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt5 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt6 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt6 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt7 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt7 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt8 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt8 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt9 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt9 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt10 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt10 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt11 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt11 & 0x00ff);

                senddataTemp[i++] = (byte)((Volt12 >> 8) & 0x00ff);
                senddataTemp[i++] = (byte)(Volt12 & 0x00ff);

                Calculate_Sum_Tx(ref senddataTemp, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, i + 2);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        //RS485_ADDR_RW_OTHER_CANADD,0x2300
        private void button_CanAdd_Reset_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_OTHER_CANADD;
            u16Rs485RegNum = 0x0001;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Balance_read_Click(object sender, EventArgs e)
        {
            textBox_openV.Text = "";
            textBox_openW.Text = "";
            textBox_CloseWin.Text = "";
            textBox_Balance_Res1.Text = "";
            textBox_Balance_Res2.Text = "";
            textBox_Balance_Res3.Text = "";
            textBox_Balance_Res4.Text = "";
            textBox_Balance_Res5.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_BALANCE_OV;
            u16Rs485RegNum = 8;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Balance_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 8 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_openV.Text.Trim();
            vfyObj.s_Params[1] = textBox_openW.Text.Trim();
            vfyObj.s_Params[2] = textBox_CloseWin.Text.Trim();
            vfyObj.s_Params[3] = textBox_Balance_Res1.Text.Trim();
            vfyObj.s_Params[4] = textBox_Balance_Res2.Text.Trim();
            vfyObj.s_Params[5] = textBox_Balance_Res3.Text.Trim();
            vfyObj.s_Params[6] = textBox_Balance_Res4.Text.Trim();
            vfyObj.s_Params[7] = textBox_Balance_Res5.Text.Trim();
            //验证数据是否合法
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 8; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 1000; vfyObj.max = 5000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 1; vfyObj.len = 2; vfyObj.min = 0; vfyObj.max = 2000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 3; vfyObj.len = 5; vfyObj.min = 0; vfyObj.max = 65000; vfyObj.unit = "*";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 8; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_BALANCE_OV;
            u16Rs485RegNum = 8;
            bRs485ByteNum = 16;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_Other_Read_Click(object sender, EventArgs e)
        {
            textBox_CS_CurCHG.Text = "";
            textBox_CS_CurDSG.Text = "";
            textBox_CBC_DelayT.Text = "";
            textBox_CBC_CurDSG.Text = "";
            textBox_Soc_TableSelect.Text = "";
            textBox_Password_Forever.Text = "";
            textBox_CurLimit_Vdel.Text = "";
            textBox_CurLimit_Cur.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_CS_CUR_CHGMAX;
            u16Rs485RegNum = 8;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Other_Set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 8 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_CS_CurCHG.Text.Trim();
            vfyObj.s_Params[1] = textBox_CS_CurDSG.Text.Trim();
            vfyObj.s_Params[2] = textBox_CBC_DelayT.Text.Trim();
            vfyObj.s_Params[3] = textBox_CBC_CurDSG.Text.Trim();
            vfyObj.s_Params[4] = textBox_Soc_TableSelect.Text.Trim();
            vfyObj.s_Params[5] = textBox_Password_Forever.Text.Trim();
            vfyObj.s_Params[6] = textBox_CurLimit_Vdel.Text.Trim();
            vfyObj.s_Params[7] = textBox_CurLimit_Cur.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4; vfyObj.idx = 0; vfyObj.decNum = 1;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 2; vfyObj.idx = 4; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;

            vfyObj.len = 1; vfyObj.idx = 6; vfyObj.decNum = 0;      //限流模块
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 7; vfyObj.decNum = 1;      //限流模块
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 8; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 6500; vfyObj.unit = "A";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 4; vfyObj.min = 0; vfyObj.max = 65535; vfyObj.unit = "*";
            if (!VerifyParams_ProtValidate(vfyObj)) return;

            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx]) * 10);
            }
            for (vfyObj.idx = 4, vfyObj.len = 6; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }
            u_Params[6] = (UInt16)((vfyObj.f_Params[6]));
            u_Params[7] = (UInt16)((vfyObj.f_Params[7]) * 10);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_CS_CUR_CHGMAX;
            u16Rs485RegNum = 8;
            bRs485ByteNum = 16;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_Sleep_Read_Click(object sender, EventArgs e)
        {
            textBox_SleepNormalV.Text = "";
            textBox_SleepNormalT.Text = "";
            textBox_SleepOverDsgV.Text = "";
            textBox_SleepOverDsgT.Text = "";
            textBox_SleepVirCur_Chg.Text = "";
            textBox_SleepVirCur_Dsg.Text = "";
            textBox_SleepRTC_WakeUpT.Text = "";
            textBox_SleepRes.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SLEEP_V_NORMAL;
            u16Rs485RegNum = 8;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Sleep_Set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 8 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_SleepNormalV.Text.Trim();
            vfyObj.s_Params[1] = textBox_SleepNormalT.Text.Trim();
            vfyObj.s_Params[2] = textBox_SleepOverDsgV.Text.Trim();
            vfyObj.s_Params[3] = textBox_SleepOverDsgT.Text.Trim();
            vfyObj.s_Params[4] = textBox_SleepVirCur_Chg.Text.Trim();
            vfyObj.s_Params[5] = textBox_SleepVirCur_Dsg.Text.Trim();
            vfyObj.s_Params[6] = textBox_SleepRTC_WakeUpT.Text.Trim();
            vfyObj.s_Params[7] = textBox_SleepRes.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 2; vfyObj.idx = 4; vfyObj.decNum = 1;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 2; vfyObj.idx = 6; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 8; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 1000; vfyObj.max = 5000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 1; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 65000; vfyObj.unit = "s";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 2; vfyObj.len = 1; vfyObj.min = 1000; vfyObj.max = 5000; vfyObj.unit = "mV";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 3; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 65000; vfyObj.unit = "s";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 2; vfyObj.min = 0; vfyObj.max = 5000; vfyObj.unit = "A";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 6; vfyObj.len = 2; vfyObj.min = 0; vfyObj.max = 50000; vfyObj.unit = "min";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }
            for (vfyObj.idx = 4, vfyObj.len = 6; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx]) * 10);
            }
            for (vfyObj.idx = 6, vfyObj.len = 8; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SLEEP_V_NORMAL;
            u16Rs485RegNum = 8;
            bRs485ByteNum = 16;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_Soc_read_Click(object sender, EventArgs e)
        {
            textBox_Soc_Ah.Text = "";
            textBox_Soc_CycleTime.Text = "";
            textBox_Soc_V_100.Text = "";
            textBox_Soc_V_0.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_AH;
            u16Rs485RegNum = 4;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Soc_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 4 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_Soc_Ah.Text.Trim();
            vfyObj.s_Params[1] = textBox_Soc_CycleTime.Text.Trim();
            vfyObj.s_Params[2] = textBox_Soc_V_100.Text.Trim();
            vfyObj.s_Params[3] = textBox_Soc_V_0.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 1; vfyObj.decNum = 1;  //容量的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 3; vfyObj.idx = 1; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 0.1F; vfyObj.max = 650; vfyObj.unit = "Ah";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 1; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "Times";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 2; vfyObj.len = 2; vfyObj.min = 0; vfyObj.max = 50000; vfyObj.unit = "Item";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            //填充要发送的数据
            u_Params[0] = (UInt16)(vfyObj.f_Params[0] * 10);
            for (vfyObj.idx = 1, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_AH;
            u16Rs485RegNum = 4;
            bRs485ByteNum = 8;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void textBox_Sys_read_Click(object sender, EventArgs e)
        {
            textBox_Sys_CSRes.Text = "";
            textBox_Sys_CSRes_Num.Text = "";
            textBox_Sys_SeriesNum.Text = "";
            textBox_Sys_PreChg_Time.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYS_SERIES_NUM;
            u16Rs485RegNum = 4;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void textBox_Sys_set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 4 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_Sys_SeriesNum.Text.Trim();
            vfyObj.s_Params[1] = textBox_Sys_CSRes.Text.Trim();
            vfyObj.s_Params[2] = textBox_Sys_CSRes_Num.Text.Trim();
            vfyObj.s_Params[3] = textBox_Sys_PreChg_Time.Text.Trim();
            //验证数据是否合法
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 3; vfyObj.max = 32; vfyObj.unit = "Series";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 1; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 65000; vfyObj.unit = "mΩ";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 2; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 10000; vfyObj.unit = "Nums";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 3; vfyObj.len = 1; vfyObj.min = 0; vfyObj.max = 50000; vfyObj.unit = "s";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYS_SERIES_NUM;
            u16Rs485RegNum = 4;
            bRs485ByteNum = 8;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        //Heat_Cool_Element
        private void button_HeatCool_Read_Click(object sender, EventArgs e)
        {
            textBox_Heat_OpenT.Text = "";
            textBox_Heat_OpenCur.Text = "";
            textBox_Cool_OpenT.Text = "";
            textBox_Res1.Text = "";
            textBox_Res2.Text = "";
            textBox_Res3.Text = "";
            textBox_Res4.Text = "";
            textBox_Heat_CloseT.Text = "";
            textBox_Cool_CloseT.Text = "";
            textBox_Res5.Text = "";
            textBox_Res6.Text = "";
            textBox_Res7.Text = "";
            textBox_Res8.Text = "";

            textBox_Res9.Text = "";
            textBox_Res10.Text = "";
            textBox_Res11.Text = "";
            textBox_Res12.Text = "";
            textBox_Res13.Text = "";
            textBox_Res14.Text = "";
            textBox_Res15.Text = "";
            textBox_Res16.Text = "";
            textBox_Res17.Text = "";
            textBox_Res18.Text = "";
            textBox_Res19.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_HEAT_DSG_HIGH;
            u16Rs485RegNum = 24;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_HeatCool_Set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 24 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_Heat_OpenT.Text.Trim();
            vfyObj.s_Params[1] = textBox_Heat_CloseT.Text.Trim();
            vfyObj.s_Params[2] = textBox_Heat_OpenCur.Text.Trim();
            vfyObj.s_Params[3] = textBox_Cool_OpenT.Text.Trim();
            vfyObj.s_Params[4] = textBox_Cool_CloseT.Text.Trim();
            vfyObj.s_Params[5] = textBox_Res1.Text.Trim();
            vfyObj.s_Params[6] = textBox_Res2.Text.Trim();
            vfyObj.s_Params[7] = textBox_Res3.Text.Trim();
            vfyObj.s_Params[8] = textBox_Res4.Text.Trim();
            vfyObj.s_Params[9] = textBox_Res5.Text.Trim();
            vfyObj.s_Params[10] = textBox_Res6.Text.Trim();
            vfyObj.s_Params[11] = textBox_Res7.Text.Trim();
            vfyObj.s_Params[12] = textBox_Res8.Text.Trim();
            vfyObj.s_Params[13] = textBox_Res9.Text.Trim();
            vfyObj.s_Params[14] = textBox_Res10.Text.Trim();
            vfyObj.s_Params[15] = textBox_Res11.Text.Trim();
            vfyObj.s_Params[16] = textBox_Res12.Text.Trim();
            vfyObj.s_Params[17] = textBox_Res13.Text.Trim();
            vfyObj.s_Params[18] = textBox_Res14.Text.Trim();
            vfyObj.s_Params[19] = textBox_Res15.Text.Trim();
            vfyObj.s_Params[20] = textBox_Res16.Text.Trim();
            vfyObj.s_Params[21] = textBox_Res17.Text.Trim();
            vfyObj.s_Params[22] = textBox_Res18.Text.Trim();
            vfyObj.s_Params[23] = textBox_Res19.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 2; vfyObj.idx = 0; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 2; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 2; vfyObj.idx = 3; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 19; vfyObj.idx = 5; vfyObj.decNum = 0;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 24; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 2; vfyObj.min = -40; vfyObj.max = 160; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 2; vfyObj.len = 1; vfyObj.min = 0; vfyObj.max = 50; vfyObj.unit = "A";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 3; vfyObj.len = 2; vfyObj.min = -40; vfyObj.max = 160; vfyObj.unit = "℃";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 5; vfyObj.len = 19; vfyObj.min = 0; vfyObj.max = 65000; vfyObj.unit = "-";
            if (!VerifyParams_ProtValidate(vfyObj)) return;

            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 2; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(((vfyObj.f_Params[vfyObj.idx]) + 40) * 10);
            }
            for (vfyObj.idx = 2, vfyObj.len = 3; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)((vfyObj.f_Params[vfyObj.idx]) * 10);
            }
            for (vfyObj.idx = 3, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(((vfyObj.f_Params[vfyObj.idx]) + 40) * 10);
            }
            for (vfyObj.idx = 5, vfyObj.len = 24; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_HEAT_DSG_HIGH;
            u16Rs485RegNum = 24;
            bRs485ByteNum = 48;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_HeatCool_Reset_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_HEAT_COOL;
            u16Rs485RegNum = 0x0001;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        #endregion

        #region 保护点记录(0xC001)
        private void Button_Fault_Record_Read_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8] { 0x01, 0x02, 0x01, 0x01, 0, 0x08, 0, 0 };

            textBox_Fault_Record1.Text = "";
            textBox_Fault_Record2.Text = "";
            textBox_Fault_Record3.Text = "";
            textBox_Fault_Record4.Text = "";
            textBox_Fault_Record5.Text = "";
            textBox_Fault_Record6.Text = "";
            textBox_Fault_Record7.Text = "";
            textBox_Fault_Record8.Text = "";
            textBox_Fault_Record9.Text = "";
            textBox_Fault_Record10.Text = "";

            if (!serialPort1.IsOpen)
            {
                // MessageBox.Show("串口错误，串口未打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                //add end

                return;
            }

            try
            {
                bRs485FunCmd = 0x03;
                u16Rs485RegAddr = 0xC001;
                u16Rs485RegNum = 70;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
                senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                //MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");


                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                //add end

                return;
            }
        }

        private void Button_Fault_Record_Clear_Click(object sender, EventArgs e)
        {
            textBox_Fault_Record1.Text = "NA";
            textBox_Fault_Record2.Text = "NA";
            textBox_Fault_Record3.Text = "NA";
            textBox_Fault_Record4.Text = "NA";
            textBox_Fault_Record5.Text = "NA";
            textBox_Fault_Record6.Text = "NA";
            textBox_Fault_Record7.Text = "NA";
            textBox_Fault_Record8.Text = "NA";
            textBox_Fault_Record9.Text = "NA";
            textBox_Fault_Record10.Text = "NA";

            label_Time_Fault_Record1.Text = "NA";
            label_Time_Fault_Record2.Text = "NA";
            label_Time_Fault_Record3.Text = "NA";
            label_Time_Fault_Record4.Text = "NA";
            label_Time_Fault_Record5.Text = "NA";
            label_Time_Fault_Record6.Text = "NA";
            label_Time_Fault_Record7.Text = "NA";
            label_Time_Fault_Record8.Text = "NA";
            label_Time_Fault_Record9.Text = "NA";
            label_Time_Fault_Record10.Text = "NA";
        }
        #endregion


        public void CleanSectionOne()
        {
            Control Controler;
            for (int j = 0; j < 32; ++j)
            {
                Controler = this.Controls.Find("label_VC" + (j + 1).ToString(), true)[0];
                this.Invoke(new EventHandler(delegate
                {
                    Controler.Text = "NA";
                }));
            }

            for (int j = 0; j < 32; ++j)
            {
                Controler = this.Controls.Find("Balanced_VC" + (j + 1).ToString(), true)[0];
                this.Invoke(new EventHandler(delegate
                {
                    Controler.Text = "NA";
                }));
            }
        }

        private void button_SalverSelect_Click(object sender, EventArgs e)
        {
            byte[] SlaverNum = System.Text.Encoding.ASCII.GetBytes(comboBox_SalverSelect.Text);
            //SlaverNum[SlaverNum.Length - 1];
            //u16Rs485RegData = (byte)(SlaverNum[SlaverNum.Length - 1] - 0x30);       //阿斯克码转数字
            //仔细想想，不这么干先，毕竟复用了switch的功能
            if (SlaverNum.Length == 7)      //Slaver1-9
            {
                u16Rs485RegData = SlaverNum[SlaverNum.Length - 1];
                timer1.Enabled = true;
                timer_Parallel.Enabled = false;
            }
            else if (SlaverNum.Length == 8) //Slaver10-16
            {
                u16Rs485RegData = (byte)(SlaverNum[SlaverNum.Length - 1] + 10);
                timer1.Enabled = true;
                timer_Parallel.Enabled = false;
            }
            //else if (SlaverNum.Length == 9) //SlaverAll
            else if (SlaverNum.Length == 6) //改为Master
            {
                u16Rs485RegData = 0x30 + 17;
                timer1.Enabled = false;
                timer_Parallel.Enabled = true;
                CleanSectionOne();
                this.Invoke(new EventHandler(delegate
                {
                    label_SlaverNow.Text = "MasterMode";
                }));
            }
            else     //SlaverSingle
            {
                timer1.Enabled = true;
                timer_Parallel.Enabled = false;
                return;
            }

            byte[] senddataTemp = new byte[8] { 0x01, 0x01, 0x08, 0x01, 0, 0, 0, 0 };

            if (!serialPort1.IsOpen)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，串口未打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Serial Port is not open", "ErrorMessage！");
                }
                return;
            }
            try
            {
                bRs485FunCmd = 0x06;
                u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SWITCH_ON;

                senddataTemp[0] = RS485_SLAVE_ADDR;
                senddataTemp[1] = bRs485FunCmd;
                senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
                senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
                senddataTemp[4] = (byte)(u16Rs485RegData / 256);
                senddataTemp[5] = (byte)(u16Rs485RegData % 256);
                Calculate_Sum_Tx(ref senddataTemp, 6);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(senddataTemp, 0, 8);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                }
                else
                {
                    MessageBox.Show("Serial Port Error，Please recheck Serial Port", "ErrorMessage！");
                }
                return;
            }
        }

        private void button_Parallel_Vpack_ProRead_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VPACK_OVP_FIRST;
            u16Rs485RegNum = 10;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        private void button_Parallel_VpackOVP_ProSet_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 2 }; //两位小数
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VpackOVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VpackOVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VpackOVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VpackOVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VpackOVP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 3; vfyObj.max = 200; vfyObj.unit = "V";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.over)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 100);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VPACK_OVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_Parallel_VpackUVP_ProSet_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5, decNum = 2 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_VpackUVP_First.Text.Trim();
            vfyObj.s_Params[1] = textBox_VpackUVP_Second.Text.Trim();
            vfyObj.s_Params[2] = textBox_VpackUVP_Third.Text.Trim();
            vfyObj.s_Params[3] = textBox_VpackUVP_Rec.Text.Trim();
            vfyObj.s_Params[4] = textBox_VpackUVP_DelayT.Text.Trim();
            //验证数据是否合法
            vfyObj.len = 4;
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            vfyObj.len = 1; vfyObj.idx = 4; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 4; vfyObj.min = 3; vfyObj.max = 200; vfyObj.unit = "V";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 4; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 50000; vfyObj.unit = "10ms";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            if (!VerifyParams_ProtReasonable(vfyObj.f_Params, Verify.under)) return;
            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 4; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx] * 100);
            }
            u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VPACK_UVP_FIRST;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_Parallel_SysPar_Read_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_PARALLEL_SERIAL_NUM;
            u16Rs485RegNum = 5;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }

        //这个函数作为了解杨岩这个代码怎么写怎么用的范本
        private void button_Parallel_SysPar_Set_Click(object sender, EventArgs e)
        {
            //填充待验证的数据
            Verify vfyObj = new Verify { len = 5 };
            vfyObj.s_Params = new string[vfyObj.len];
            vfyObj.f_Params = new float[vfyObj.len];
            UInt16[] u_Params = new UInt16[vfyObj.len];
            vfyObj.s_Params[0] = textBox_Parallel_SerialNum.Text.Trim();
            vfyObj.s_Params[1] = textBox_Parallel_PackNum.Text.Trim();
            vfyObj.s_Params[2] = textBox_Parallel_Res1.Text.Trim();
            vfyObj.s_Params[3] = textBox_Parallel_Res2.Text.Trim();
            vfyObj.s_Params[4] = textBox_Parallel_Res3.Text.Trim();

            //验证数据是否合法
            vfyObj.idx = 0; vfyObj.len = 5; vfyObj.decNum = 0;  //滤波时间的表单验证
            if (!VerifyParams_ProtUsual(vfyObj)) return;

            //验证数据范围
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                vfyObj.f_Params[vfyObj.idx] = Convert.ToSingle(vfyObj.s_Params[vfyObj.idx]);
            }
            vfyObj.idx = 0; vfyObj.len = 1; vfyObj.min = 3; vfyObj.max = 32; vfyObj.unit = "Serial";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 1; vfyObj.len = 1; vfyObj.min = 1; vfyObj.max = 16; vfyObj.unit = "Pack";
            if (!VerifyParams_ProtValidate(vfyObj)) return;
            vfyObj.idx = 2; vfyObj.len = 3; vfyObj.min = 0; vfyObj.max = 65000; vfyObj.unit = "*";
            if (!VerifyParams_ProtValidate(vfyObj)) return;

            //填充要发送的数据
            for (vfyObj.idx = 0, vfyObj.len = 5; vfyObj.idx < vfyObj.len; vfyObj.idx++)
            {
                u_Params[vfyObj.idx] = (UInt16)(vfyObj.f_Params[vfyObj.idx]);
            }

            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_PARALLEL_SERIAL_NUM;
            u16Rs485RegNum = 5;
            bRs485ByteNum = 10;
            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        // private void textBox_password_TextChanged(object sender, EventArgs e)
        // {
        //     if (textBox_password.Text == "hs456")
        //     {
        //         MessageBox.Show( "","密码正确", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //         tabPage_Cali1.Parent = tabControl1;
        //         //tabPage_Cali2.Parent = tabControl1;
        //         tabPage_Protect.Parent = tabControl1;
        //         //tabPage_OtherElement.Parent = tabControl1;
        //     }
        // }

        private void but_change_baud_Click(object sender, EventArgs e)
        {
            // try
            // {
            //     if (serialPort1.IsOpen)
            //     {
            //         serialPort1.Close();
            //         //bComOnOffFlag = false;
            //         timer1.Enabled = false;
            //         groupBox_set.Enabled = false;
            //         groupBox_read.Enabled = false;
            //         //button_TurnOnOffCom.Text = "打开串口";


            //         if (Lang.b_LangFlag == 0)
            //             button_TurnOnOffCom.Text = "打开串口";
            //         else
            //             button_TurnOnOffCom.Text = "StartUp";
            //         //add end

            //         //toolStripStatusLabel_ComStatus.Text = "串口" + serialPort1.PortName + "已关闭";
            //         //toolStripStatusLabel_ComConfig.Text = serialPort1.PortName + "  " + Convert.ToString(serialPort1.BaudRate) + ",n,8,1";
            //     }
            //     else
            //     {
            //         serialPort1.PortName = comboBox_ComNum.Text;
            //         serialPort1.BaudRate = Convert.ToInt32(comboBox_BandRate.Text, 10);
            //         //bComOnOffFlag = true;
            //         bRdCmdErrCnt = 0;
            //         timer1.Enabled = true;
            //         groupBox_set.Enabled = true;
            //         groupBox_read.Enabled = true;
            //         //bMdlAddr = 1;
            //         serialPort1.Open();
            //         //button_TurnOnOffCom.Text = "关闭串口";


            //         if (Lang.b_LangFlag == 0)
            //             button_TurnOnOffCom.Text = "关闭串口";
            //         else
            //             button_TurnOnOffCom.Text = "ShutDown";
            //         //add end

            //         //toolStripStatusLabel_ComStatus.Text = "串口" + serialPort1.PortName + "已打开";
            //         //toolStripStatusLabel_ComConfig.Text = serialPort1.PortName + "  " + Convert.ToString(serialPort1.BaudRate) + ",n,8,1";
            //     }
            // }
            // catch (Exception)
            // {
            //     if (Lang.b_LangFlag == 0)
            //         MessageBox.Show("串口错误，请检查串口号是否正确", "错误提示！");
            //     else
            //         MessageBox.Show("Serial port error, Please recheck the Serial Port!", "ErrorMessage！");
            //     return;
            // }

            //comboBox_BandRate.Text
            UInt32 baud = Convert.ToUInt32(comboBox_BandRate.Text, 10);
            switch (baud)
            {
                case 9600:

                    bRs485FunCmd = 0x06;
                    //u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC;
                    u16Rs485RegAddr = (UInt16)4104;
                    u16Rs485RegNum = 1;
                    SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);

                    break;

                case 19200:
                    bRs485FunCmd = 0x06;
                    //u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC;
                    u16Rs485RegAddr = (UInt16)4104;
                    u16Rs485RegNum = 2;
                    SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);

                    break;

                case 115200:
                    bRs485FunCmd = 0x06;
                    //u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC;
                    u16Rs485RegAddr = (UInt16)4104;
                    u16Rs485RegNum = 3;
                    SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);

                    break;
                default:
                    if (Lang.b_LangFlag == 0)
                        MessageBox.Show("串口错误，请检查串口号是否正确", "错误提示！");
                    else
                        MessageBox.Show("Serial port error, Please recheck the Serial Port!", "ErrorMessage！");
                    return;
            }
        }

        private void button_readBatNum_Click(object sender, EventArgs e)
        {
            textBox_BatNum.Text = "";

            bRs485FunCmd = 0x03;
            u16Rs485RegAddr = RS485_ADDR_ONLYBATNUM_READ;
            u16Rs485RegNum = 11;
            // SendData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);

            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
            
            //  {
            //     string str_BatNum = textBox_BatNum.Text;

            //     if (str_BatNum.Length != 11 || str_BatNum.Length == 0)
            //     {
            //         MessageBox.Show("输入不合法，电池编号长度应为11");
            //         // MessageBox.Show("输入字符串为空或超过11个字符");
            //         return;
            //     }

            //     try
            //     {
            //         byte[] b_str_BatNum = System.Text.Encoding.ASCII.GetBytes(str_BatNum);
            //         byte[] sendbuf = new byte[11 + 9];

            //         int index = 0;

            //         bRs485FunCmd = 0x10;
            //         u16Rs485RegAddr = RS485_ADDR_ONLY_BATNUM;
            //         u16Rs485RegNum = 11;
            //         bRs485ByteNum = 0;

            //         sendbuf[index++] = RS485_SLAVE_ADDR;
            //         sendbuf[index++] = bRs485FunCmd;
            //         sendbuf[index++] = (byte)(u16Rs485RegAddr / 256);
            //         sendbuf[index++] = (byte)(u16Rs485RegAddr % 256);
            //         sendbuf[index++] = (byte)(u16Rs485RegNum / 256);
            //         sendbuf[index++] = (byte)(u16Rs485RegNum % 256);
            //         sendbuf[index++] = bRs485ByteNum;

            //         Array.ConstrainedCopy(b_str_BatNum, 0, sendbuf, index, 11);

            //         index = index + 11;
            //         Calculate_Sum_Tx(ref sendbuf, (UInt32)index);

            //         serialPort1.DiscardInBuffer();
            //         serialPort1.DiscardOutBuffer();
            //         bRxByteCnt = 0;
            //         bTotleBytes = 0;
            //         bRxFrameFinishFlag = false;
            //         serialPort1.Write(sendbuf, 0, index + 2);
            //         serialPort1.DiscardInBuffer();


            //         // serialPort1.Write(sendbuf, 0, sendbuf.Length);


            //     }
            //     catch (Exception)
            //     {
            //         // bRdCmdErrCnt++;
            //         // if (bRdCmdErrCnt > 3)
            //         // {
            //         //     timer1.Enabled = false;
            //         //     bRdCmdErrCnt = 0;
            //         //     if (Lang.b_LangFlag == 0)
            //         //         MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
            //         //     else
            //         //         MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
            //         // }
            //         // return;

            //         {
            //             // timer1.Enabled = false;

            //             if (Lang.b_LangFlag == 0)
            //                 MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
            //             else
            //                 MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
            //         }
            //         return;
            //     }


            // }

        }

        private void button_setBatNum_Click(object sender, EventArgs e)
        {

            {
                string str_BatNum = textBox_BatNum.Text;

                if (str_BatNum.Length != 11 || str_BatNum.Length == 0)
                {
                    MessageBox.Show("输入不合法，电池编号长度应为11");
                    // MessageBox.Show("输入字符串为空或超过11个字符");
                    return;
                }

                try
                {
                    byte[] b_str_BatNum = System.Text.Encoding.ASCII.GetBytes(str_BatNum);
                    byte[] sendbuf = new byte[12 + 9];

                    int index = 0;

                    bRs485FunCmd = 0x10;
                    u16Rs485RegAddr = RS485_ADDR_ONLY_BATNUM;
                    u16Rs485RegNum = 6;
                    bRs485ByteNum = 12;

                    sendbuf[index++] = RS485_SLAVE_ADDR;
                    sendbuf[index++] = bRs485FunCmd;
                    sendbuf[index++] = (byte)(u16Rs485RegAddr / 256);
                    sendbuf[index++] = (byte)(u16Rs485RegAddr % 256);
                    sendbuf[index++] = (byte)(u16Rs485RegNum / 256);
                    sendbuf[index++] = (byte)(u16Rs485RegNum % 256);
                    sendbuf[index++] = bRs485ByteNum;

                    Array.ConstrainedCopy(b_str_BatNum, 0, sendbuf, index, 11);

                    index = index + 11;
                    sendbuf[index++] = 0xff;

                    Calculate_Sum_Tx(ref sendbuf, (UInt32)index);

                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();
                    bRxByteCnt = 0;
                    bTotleBytes = 0;
                    bRxFrameFinishFlag = false;
                    serialPort1.Write(sendbuf, 0, index + 2);
                    serialPort1.DiscardInBuffer();


                    // serialPort1.Write(sendbuf, 0, sendbuf.Length);


                }
                catch (Exception)
                {
                    // bRdCmdErrCnt++;
                    // if (bRdCmdErrCnt > 3)
                    // {
                    //     timer1.Enabled = false;
                    //     bRdCmdErrCnt = 0;
                    //     if (Lang.b_LangFlag == 0)
                    //         MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                    //     else
                    //         MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
                    // }
                    // return;

                    {
                        // timer1.Enabled = false;

                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("串口错误，请检查串口是否打开", "错误提示！");
                        else
                            MessageBox.Show("Serial port error, Please recheck the Serial Port！", "ErrorMessage！");
                    }
                    return;
                }


            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
           if (!serialPort1.IsOpen)
            {
                MessageBox.Show(s_MsgInfo[(int)Lang.MsgInfo.PortClosed],
                    s_MsgInfo[(int)Lang.MsgInfo.ReadFailed],
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                byte i = 0;

                sendCatch[i++] = 0x5a;
                sendCatch[i++] = 0xa5;
                sendCatch[i++] = 0x05;
                sendCatch[i++] = 0x01;
                sendCatch[i++] = 0x01;
                sendCatch[i++] = 0x05;
                sendCatch[i++] = 0xf0;

                // Calculate_Sum_Tx(ref sendCatch, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                // serialPort1.Write(sendCatch, 0, i + 2);
                serialPort1.Write(sendCatch, 0, i);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                MessageBox.Show(s_MsgInfo[(int)Lang.MsgInfo.PortError],
                    s_MsgInfo[(int)Lang.MsgInfo.OperationFailed],
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

         public void read_chg_dsg_time()
        {
            try
            {
                byte i = 0;

                sendCatch[i++] = 0x5a;
                sendCatch[i++] = 0xa5;
                sendCatch[i++] = 0x03;
                sendCatch[i++] = 0x01;
                sendCatch[i++] = 0x01;
                sendCatch[i++] = 0x03;
                sendCatch[i++] = 0xf0;

                // Calculate_Sum_Tx(ref sendCatch, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                // serialPort1.Write(sendCatch, 0, i + 2);
                serialPort1.Write(sendCatch, 0, i);
                serialPort1.DiscardInBuffer();
            }
            catch (Exception)
            {
                MessageBox.Show(s_MsgInfo[(int)Lang.MsgInfo.PortError],
                    s_MsgInfo[(int)Lang.MsgInfo.OperationFailed],
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

    }
}
