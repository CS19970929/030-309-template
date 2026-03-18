using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Ports;

namespace CommomUpper_32Series
{
    /*
    class BlueTooth_Wifi
    {
        private void button_BT_Factory_Click(object sender, EventArgs e)
        {

        }
    }
    */
    
    public partial class Form1
    {
        #region 蓝牙模块
        //蓝牙模块相关
        public byte BlueTooth_ComFlag = 0;
        public static bool BlueTooth_ReceiveOK_Flag = false;
        public static bool BlueTooth_TimeOut_Flag = false;
        public byte BlueTooth_ReceiveDataNeed = 0;
        public string BlueTooth_Command;

        //定义5个字符串数据存储
        public string[] BlueToothBaudR = { "9600", "19200", "115200", "4800", "14400", "28800", "38400", "57600", "76800", "230400" };
        //public string BaudR_TestCompare = "[AT]ER\r\n";
        public byte BaudR_Temp = 0;
        public string[] DevName = new string[10];
        public string[] MacAdress = new string[10];
        public string[] SignalStrength = new string[10];
        public byte[] bRxDataBuff_BT = new byte[200];
        byte bRxByteCnt_BT = 0;

        public enum BLUETOOTH_COMMAND_TYPE
        {
            BLUETOOTH_GET_BAUD = 0,
            BLUETOOTH_CHANGE_BAUD,
            BLUETOOTH_GET_DEV_NAME,
            BLUETOOTH_CHANGE_DEV_NAME,
            BLUETOOTH_SAVE_TO_FLASH,
            BLUETOOTH_CHECK_STATUS,
            BLUETOOTH_SCAN_DEV_NEARBY,
            BLUETOOTH_CONNECT_HOST_DEV,
            BLUETOOTH_DISCONNECT_HOST_DEV,
            BLUETOOTH_RECOVER_FACTORY
        };

        BLUETOOTH_COMMAND_TYPE BlueTooth_Task;


        public void TestCode()
        {
            /*
            int temp1;
            int temp2;
            int temp3;
            int temp_Begin;
            byte i = 0;
            string abcd1 = "[0th]mobbike,EE:56:8C:50:32:38,-97\n[1th] fast,EE:56:8C:50:32:38,-88\n[2th] aaaat,EE:56:8C:50:32:38,-88";
            byte[] abcd2 = System.Text.Encoding.ASCII.GetBytes(abcd1);
            string aaa = System.Text.Encoding.ASCII.GetString(abcd2);
            i = 0;
            temp_Begin = 0;
            temp1 = aaa.IndexOf("th]", temp_Begin);          //不应该转字符串吗，看看这样写是否有问题
            while (temp1 != -1)
            {
                temp2 = aaa.IndexOf(",", temp_Begin);
                temp3 = aaa.IndexOf(",", temp2 + 1); //第二个,号
                DevName[i] = aaa.Substring(temp1 + 3, temp2 - temp1 - 3);
                MacAdress[i] = aaa.Substring(temp2 + 1, 17);
                SignalStrength[i] = aaa.Substring(temp3 + 1, 3);
                listBox_BlueTooth_Dev.Items.Add(DevName[i]);
                ++i;
                temp_Begin = temp3+3;
                temp1 = aaa.IndexOf("th]", temp_Begin);

                if (i >= 10)     //最多10个？TODO
                {
                    break;
                }
            }
            listBox_BlueTooth_Interface.Items.Add("aaa");
            listBox_BlueTooth_Interface.Items.Add("bbb");
            */
            try
            {

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;
                BlueTooth_ReceiveDataNeed = 8;          //4800波特率好像返回的字符没这么多，实验结果是波特率越高，能读到的字符越多，先3个试试水
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_BAUD;
                BlueTooth_Command = "AT\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(BlueToothBaudR[3], 10);

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);

                if (BaudR_Temp == 0)     //第一次执行
                {
                    listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);
                }

                //BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    //DataReceiveJudge_BlueTooth();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "蓝牙波特率获取错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void BlueTooth_TimeOutHander(int milliSecond)
        {
            int start = Environment.TickCount;
            int Second = milliSecond * 1000;
            while (Math.Abs(Environment.TickCount - start) < Second)    //改为秒
            {
                //Application.DoEvents();//可执行某无聊的操作
                if (BlueTooth_ReceiveOK_Flag)
                {
                    break;
                }
            }

            if (!BlueTooth_ReceiveOK_Flag)
            {
                BlueTooth_TimeOut_Flag = true;
            }
        }

        //不要纠结这个是否加多一个按钮，结论不要加，修改波特率，修改设备名字，必须保存成功才算成功，别的都算失败，这样的思路就对了
        public void BlueTooth_Flash_Save()
        {
            try
            {
                bRxByteCnt_BT = 0;
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 8;
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SAVE_TO_FLASH;
                BlueTooth_Command = "AT+SAVE\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);

                listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                {
                    listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);
                }));

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)    //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("蓝牙保存出问题！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //MessageBox.Show(ex.GetType().ToString());
                MessageBox.Show(ex.GetType().ToString(), "蓝牙保存错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //要加一个时间溢出限制
        //已添加
        public void DataReceiveDeal_BlueTooth()
        {
            int temp;
            string OutByteToString;

            switch (BlueTooth_Task)
            {
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_BAUD:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_BAUD:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_DEV_NAME:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SAVE_TO_FLASH:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CONNECT_HOST_DEV:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_DISCONNECT_HOST_DEV:
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_RECOVER_FACTORY:
                    if (bRxByteCnt_BT >= BlueTooth_ReceiveDataNeed || BlueTooth_TimeOut_Flag)
                    {
                        //bRxByteCnt = 0;
                        BlueTooth_ReceiveOK_Flag = true;
                    }
                    break;

                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_DEV_NAME:     //因为设备名字长度不明确所以接收数据长度不明确
                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHECK_STATUS:
                    OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                    temp = OutByteToString.IndexOf("\r");
                    if (OutByteToString.IndexOf("\r", temp + 1) != -1) //只要有两个\r说明接收完毕
                    {
                        BlueTooth_ReceiveOK_Flag = true;
                    }
                    break;

                case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SCAN_DEV_NEARBY:
                    //回去看看怎么处理，接收10个还是咋样就不要了，然后数据格式是咋样的
                    //这个函数没有接受长度限制，直接timeout处理
                    //在这里写意义不大，因为timeout后不会执行串口接收函数，从而这个函数就不会被执行，除非有数据过来
                    /*
                    if (BlueTooth_TimeOut_Flag)
                    {
                        //bRxByteCnt = 0;
                        BlueTooth_ReceiveOK_Flag = true;
                    }
                    */
                    break;

                default:
                    break;
            }
            /*
            if (BlueTooth_ReceiveOK_Flag)
            {
                DataReceiveJudge_BlueTooth();
            }
            */
        }

        public void DataReceiveJudge_BlueTooth()
        {
            try
            {
                int temp1;
                int temp2;
                int temp3;
                int temp_Begin;
                byte i = 0;
                string OutByteToString;

                switch (BlueTooth_Task)
                {
                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_BAUD:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            //判断正确的做法是，如果接受的字符>=8个(实质就是准确的8个)，准入门槛，然后发现[AT]ER便可
                            if (OutByteToString.Length >= 8)
                            {
                                if (String.Compare(OutByteToString.Substring(0, 6), "[AT]ER") == 0)
                                {
                                    //该波特率为正确波特率
                                    listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                                    {
                                        listBox_BlueTooth_Interface.Items.Add("Baud: " + BlueToothBaudR[BaudR_Temp]);
                                    }));

                                    comboBox_BT_BandRate_Now.Invoke(new EventHandler(delegate
                                    {
                                        comboBox_BT_BandRate_Now.Text = BlueToothBaudR[BaudR_Temp];
                                    }));
                                    BaudR_Temp = 0;
                                    MessageBox.Show("蓝牙波特率查询成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    break;
                                }
                            }
                            //如果以上判断条件不成立，则全部归咎错误，继续循环
                            if (BaudR_Temp >= 9)
                            {
                                BaudR_Temp = 0;
                                MessageBox.Show("硬件错误，无法连接蓝牙模块", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                //使用一行好像好点，但是算了，不如加个类似烧代码的柱子？
                                listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                                {
                                    listBox_BlueTooth_Interface.Items.Add("...");
                                }));
                                ++BaudR_Temp;
                                //button_BT_BandRate_Get.PerformClick();
                                //默认情况下，C#不允许在一个线程中直接操作另一个线程中的控件，这是因为访问Windows窗体控件本质上不是线程安全的。
                                //如果有两个或多个线程同时操作某一控件的状态，则可能会迫使该控件进入一种不一致的状态，还可能会出现其他与线程相关的Bug，
                                //以及不同线程争用控件引起的死锁问题。因此确保以线程安全方式访问控件非常重要。
                                //ThreadStart threadStart = new ThreadStart(button_BT_BandRate_Get.PerformClick);//通过ThreadStart委托告诉子线程执行什么方法　　
                                //Thread thread = new Thread(threadStart);
                                //thread.Start();//启动新线程
                                //以下方法也不行，一定要执行完整个线程才会操作控件显示
                                /*
                                this.Invoke((MethodInvoker)delegate
                                {
                                    button_BT_BandRate_Get.PerformClick();
                                });
                                */
                                //也没用
                                button_BT_BandRate_Get.Invoke(new EventHandler(delegate
                                {
                                    button_BT_BandRate_Get.PerformClick();
                                }));
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_BAUD:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)      //出问题都是这里
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));
                            //以下这句话，会出现System.InvalidOperationException问题，主要是跨线程操作导致的错误，换成以下解决
                            //listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)     //没有完结果字符，正确应该是[AT]OK\r\n
                            {
                                comboBox_BandRate.Invoke(new EventHandler(delegate
                                {
                                    comboBox_BandRate.Text = comboBox_BT_BandRate_Need.Text;             //上位机波特率随之修改
                                    serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Need.Text, 10); //十进制，放这里可以吗，好像真的可以
                                }));

                                comboBox_BT_BandRate_Now.Invoke(new EventHandler(delegate
                                {
                                    comboBox_BT_BandRate_Now.Text = comboBox_BT_BandRate_Need.Text;             //上位机波特率随之修改
                                }));
                                //如果用这个会导致这个函数执行很慢，出现就算接收完也是超时机制使接收函数继续下去？
                                //BlueTooth_Flash_Save();
                                ThreadStart threadStart = new ThreadStart(BlueTooth_Flash_Save);//通过ThreadStart委托告诉子线程执行什么方法　　
                                Thread thread = new Thread(threadStart);
                                thread.Start();//启动新线程
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("蓝牙波特率修改失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_DEV_NAME:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1 && OutByteToString.IndexOf("[DA]") == -1)      //出问题都是这里
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r", OutByteToString.IndexOf("\r") + 1));

                            temp1 = OutByteToString.IndexOf(",", 0);
                            temp2 = OutByteToString.IndexOf(",", temp1 + 1); //第二个,号

                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("DevName: " + OutByteToString.Substring(temp1 + 1, temp2 - temp1 - 1));
                            }));
                            textBox_BT_DevName_Now.Invoke(new EventHandler(delegate
                            {
                                textBox_BT_DevName_Now.Text = OutByteToString.Substring(temp1 + 1, temp2 - temp1 - 1);  //上位机波特率随之修改
                            }));
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_DEV_NAME:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)      //出问题都是这里
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));
                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)
                            {
                                textBox_BT_DevName_Now.Invoke(new EventHandler(delegate
                                {
                                    textBox_BT_DevName_Now.Text = textBox_BT_DevName_Need.Text;             //上位机波特率随之修改
                                }));
                                ThreadStart threadStart = new ThreadStart(BlueTooth_Flash_Save);//通过ThreadStart委托告诉子线程执行什么方法　　
                                Thread thread = new Thread(threadStart);
                                thread.Start();//启动新线程
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("设备名字修改失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SAVE_TO_FLASH:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));

                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("参数修改并存储成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("参数修改成功但存储失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SCAN_DEV_NEARBY:
                        {
                            i = 0;
                            temp_Begin = 0;
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("OK", 0) == -1)
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }

                            //temp1 = Array.IndexOf(bRxDataBuff, "th]", temp_Begin);     //不应该转字符串吗，看看这样写是否有问题，有问题，已修改为转字符串
                            temp1 = OutByteToString.IndexOf("th]", temp_Begin);
                            while (temp1 != -1)
                            {
                                temp2 = OutByteToString.IndexOf(",", temp_Begin);
                                temp3 = OutByteToString.IndexOf(",", temp2 + 1); //第二个,号

                                switch (comboBox_DevSel_BlueTooth.Text)
                                {
                                    case "天工360":
                                        DevName[i] = OutByteToString.Substring(temp1 + 3, temp2 - temp1 - 3);
                                        MacAdress[i] = OutByteToString.Substring(temp2 + 1, 17);
                                        break;

                                    case "天工369":
                                        MacAdress[i] = OutByteToString.Substring(temp1 + 3, 17);
                                        DevName[i] = OutByteToString.Substring(temp2 + 1, temp3 - temp2 - 1);
                                        break;
                                    default:
                                        break;
                                }

                                SignalStrength[i] = OutByteToString.Substring(temp3 + 1, 3);

                                listBox_BlueTooth_Dev.Invoke(new EventHandler(delegate
                                {
                                    listBox_BlueTooth_Dev.Items.Add(DevName[i]);
                                }));
                                //listBox_BlueTooth_Dev.Items.Add(DevName[i]);
                                ++i;
                                temp_Begin = temp3 + 3;
                                temp1 = OutByteToString.IndexOf("th]", temp_Begin);
                                if (temp1 >= 150 || i >= 10)     //最多10个？TODO
                                {
                                    break;
                                }
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CONNECT_HOST_DEV:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));

                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)     //没有完结果字符
                            {
                                label_Connection_Status_BT.Invoke(new EventHandler(delegate
                                {
                                    label_Connection_Status_BT.Text = "已连接";
                                }));
                                MessageBox.Show("蓝牙连接成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("蓝牙连接失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_DISCONNECT_HOST_DEV:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));

                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("断开连接成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("断开连接失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHECK_STATUS:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1 && OutByteToString.IndexOf("[DA]") == -1)      //出问题都是这里
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r", OutByteToString.IndexOf("\r") + 1));

                            temp1 = OutByteToString.IndexOf("[DA]", 0);
                            temp2 = OutByteToString.IndexOf(",", 0);
                            OutByteToString = OutByteToString.Substring(temp1 + 4, temp2 - temp1 - 4);

                            //先显示，没有就是null
                            textBox_BT_Connect_Dev.Invoke(new EventHandler(delegate
                            {
                                textBox_BT_Connect_Dev.Text = OutByteToString;
                            }));

                            if (OutByteToString.IndexOf("addr=", 0) == -1)
                            {
                                listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                                {
                                    listBox_BlueTooth_Interface.Items.Add("Not Connected: " + OutByteToString);
                                }));
                                label_Connection_Status_BT.Invoke(new EventHandler(delegate
                                {
                                    label_Connection_Status_BT.Text = "未连接";
                                }));
                            }
                            else
                            {
                                listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                                {
                                    listBox_BlueTooth_Interface.Items.Add("Connected: " + OutByteToString);
                                }));
                                label_Connection_Status_BT.Invoke(new EventHandler(delegate
                                {
                                    label_Connection_Status_BT.Text = "已连接";
                                }));
                            }
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_RECOVER_FACTORY:
                        {
                            OutByteToString = System.Text.Encoding.ASCII.GetString(bRxDataBuff_BT);
                            if (OutByteToString.IndexOf("\r") == -1)
                            {
                                MessageBox.Show("当前波特率错误", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            OutByteToString = OutByteToString.Substring(0, OutByteToString.IndexOf("\r"));

                            listBox_BlueTooth_Interface.Invoke(new EventHandler(delegate
                            {
                                listBox_BlueTooth_Interface.Items.Add("Rece: " + OutByteToString);
                            }));

                            if (String.Compare(OutByteToString, "[AT]OK") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("设备恢复出厂设置成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else if (String.Compare(OutByteToString, "[AT]ER") == 0)     //没有完结果字符
                            {
                                MessageBox.Show("设备恢复出厂设置失败", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("未知错误(可能为当前波特率错误)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        }

                    default:
                        break;
                }

                bRxByteCnt_BT = 0;                  //必须复原，不然原来的数据造成假象
                for (i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ComFlag = 0;      //复原，停止蓝牙整改通讯
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "蓝牙数据判断错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_BandRate_Get_Click(object sender, EventArgs e)
        {
            try
            {
                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;
                BlueTooth_ReceiveDataNeed = 8;          //4800波特率好像返回的字符没这么多，实验结果是波特率越高，能读到的字符越多，先3个试试水
                                                        //结论出来了，如果是115200，再用4800或者9600去尝试，一个字符都不会返回，所以超时机制只能设置为2s
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_BAUD;
                BlueTooth_Command = "AT+BAUD\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(BlueToothBaudR[BaudR_Temp], 10);
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = BlueToothBaudR[BaudR_Temp];             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);

                if (BaudR_Temp == 0)     //第一次执行
                {
                    listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);
                }

                BlueTooth_TimeOutHander(1);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)
                {
                    DataReceiveJudge_BlueTooth();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "蓝牙波特率获取错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_BandRate_Change_Click(object sender, EventArgs e)
        {
            try
            {
                if ((comboBox_BT_BandRate_Now.Text == "") || (comboBox_BT_BandRate_Need.Text == ""))
                {
                    comboBox_BT_BandRate_Now.Text = "";
                    comboBox_BT_BandRate_Need.Text = "";
                    MessageBox.Show("波特率数据不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;       //妈的，漏了这个初始化，导致老是溢出错误，而且各个函数也忘了try去catch导致真正的错误位置没定位到
                                                        //由于没有复原，导致接收1个就进去了数据判断函数，然后OutByteToString.IndexOf("\r")返回-1，然后错误
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;
                BlueTooth_ReceiveDataNeed = 8;          //[AT]OK\r\n，如果不是包括\r\n，则会出现接收错误的问题(字符串搜索的是\r字符)
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_BAUD;
                BlueTooth_Command = "AT+BAUD=" + comboBox_BT_BandRate_Need.Text + "\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("蓝牙波特率修改出问题！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //MessageBox.Show(ex.GetType().ToString());
                MessageBox.Show(ex.GetType().ToString(), "蓝牙波特率修改错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_DevName_Get_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox_BT_BandRate_Now.Text == "")
                {
                    comboBox_BT_BandRate_Now.Text = "";
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 20;          //
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_DEV_NAME;
                BlueTooth_Command = "AT+STATUS\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "设备名字获取错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_DevName_Change_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox_BT_BandRate_Now.Text == "" || textBox_BT_DevName_Need.Text == "")
                {
                    MessageBox.Show("当前波特率和目标设备名字不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 8;          //会回[AT]OK\r\n
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_DEV_NAME;
                BlueTooth_Command = "AT+DEV_NAME=" + textBox_BT_DevName_Need.Text + "\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "修改设备名字错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Scan_BlueTooth_Click(object sender, EventArgs e)
        {
            try
            {
                ///*
                if (comboBox_BT_BandRate_Now.Text == "" || comboBox_DevSel_BlueTooth.Text == "")
                {
                    MessageBox.Show("当前波特率和设备类型选择不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;
                BlueTooth_ReceiveDataNeed = 190;
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SCAN_DEV_NEARBY;
                BlueTooth_Command = "AT+SCAN_BLE=5\r\n";

                BlueTooth_ReceiveDataNeed = 8;              //忘记屏蔽，但是用起来很顺手，什么情况
                //BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SAVE_TO_FLASH;
                //BlueTooth_Command = "AT+BAUD=" + comboBox_BT_BandRate_Need.Text + "\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);

                //ThreadStart threadStart = new ThreadStart(BlueTooth_TimeOutHander);//通过ThreadStart委托告诉子线程执行什么方法　　
                //Thread thread = new Thread(threadStart);
                //thread.Start();//启动新线程

                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);   //问题找到了，首先串口发出命令后，会卡在这里6s，由于非占用延时问题，接收函数同步2s以内会接收完全部数据
                BlueTooth_TimeOutHander(6);                //这个时间长一些             //但是由于必须超时，才能Judge，这个时候不能执行Judge函数。超时完毕，该函数执行完毕，窗口显示发送命令内容
                                                           //由于接收函数不再执行，所以结束。后续思考为啥前面的线程没问题

                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果6s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
                //*/
                //TestCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "蓝牙搜索错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Connect_BlueTooth_Click(object sender, EventArgs e)
        {
            int i;
            try
            {
                if (listBox_BlueTooth_Dev.SelectedItem.ToString() == "")
                {
                    MessageBox.Show("请选择要连接的设备！", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox_BT_BandRate_Now.Text == "")
                {
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;       //妈的，漏了这个初始化，导致老是溢出错误，而且各个函数也忘了try去catch导致真正的错误位置没定位到
                                                        //由于没有复原，导致接收1个就进去了数据判断函数，然后OutByteToString.IndexOf("\r")返回-1，然后错误
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;
                BlueTooth_ReceiveDataNeed = 8;          //[AT]OK\r\n，如果不是包括\r\n，则会出现接收错误的问题(字符串搜索的是\r字符)
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CONNECT_HOST_DEV;

                for (i = 0; i < 10; ++i)
                {
                    if (DevName[i] == listBox_BlueTooth_Dev.SelectedItem.ToString())
                    {
                        break;
                    }
                }
                BlueTooth_Command = "AT+CON_MAC=" + MacAdress[i] + "\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "连接蓝牙错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_DisConnect_BlueTooth_Click(object sender, EventArgs e)
        {
            int i;
            try
            {
                if (comboBox_BT_BandRate_Now.Text == "")
                {
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 8;          //[AT]OK\r\n，如果不是包括\r\n，则会出现接收错误的问题(字符串搜索的是\r字符)
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_DISCONNECT_HOST_DEV;
                BlueTooth_Command = "AT+DISCON\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_BlueTooth();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "断开已连接蓝牙错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_Factory_Click(object sender, EventArgs e)
        {
            try
            {
                bRxByteCnt_BT = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 8;
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_RECOVER_FACTORY;
                BlueTooth_Command = "AT+FACTORY\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)
                {
                    DataReceiveJudge_BlueTooth();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "恢复出厂设置错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_BT_Check_Status_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox_BT_BandRate_Now.Text == "")
                {
                    comboBox_BT_BandRate_Now.Text = "";
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_BT = 0;
                for (int i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ReceiveOK_Flag = false;
                BlueTooth_TimeOut_Flag = false;
                BlueTooth_ComFlag = 1;

                BlueTooth_ReceiveDataNeed = 20;
                BlueTooth_Task = BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHECK_STATUS;
                BlueTooth_Command = "AT+STATUS\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(BlueTooth_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_BT_BandRate_Now.Text, 10);  //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;                 //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, BlueTooth_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + BlueTooth_Command);

                BlueTooth_TimeOutHander(5);
                if (BlueTooth_TimeOut_Flag || BlueTooth_ReceiveOK_Flag)
                {
                    DataReceiveJudge_BlueTooth();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "查看蓝牙状态错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Clear_InterFace_BT_Click(object sender, EventArgs e)
        {
            //listBox_BlueTooth_Interface.ClearSelected();
            listBox_BlueTooth_Interface.Items.Clear();
            listBox_BlueTooth_Dev.Items.Clear();
        }
        #endregion

        #region WIFI模块
        //WIFI模块相关
        public byte Wifi_ComFlag = 0;
        public static bool Wifi_ReceiveOK_Flag = false;
        public static bool Wifi_TimeOut_Flag = false;
        public byte Wifi_ReceiveDataNeed = 0;
        public string Wifi_Command;

        //定义5个字符串数据存储
        public string[] WifiBaudR = { "9600", "19200", "115200", "4800", "14400", "28800", "38400", "57600", "76800", "230400" };
        //public string BaudR_TestCompare = "[AT]ER\r\n";
        public byte Wifi_BaudR_Temp = 0;
        public string[] Wifi_DevName = new string[10];
        public string[] Wifi_DMacAdress = new string[10];
        public string[] Wifi_DSignalStrength = new string[10];
        public byte[] bRxDataBuff_Wifi = new byte[200];
        byte bRxByteCnt_Wifi = 0;

        public enum WIFI_COMMAND_TYPE
        {
            WIFI_GET_BAUD = 0,
            WIFI_CHANGE_BAUD,
            WIFI_GET_AP,
            WIFI_CHANGE_AP,

            WIFI_CHECK_STATUS,
            WIFI_DISCONNECT_HOST_DEV,
            WIFI_RECOVER_FACTORY,


            WIFI_SCAN_AP_NEARBY,
            WIFI_SET_STATION_MODE,
            WIFI_CONNECT_AP,
            WIFI_ESTABLISH_TCP_CONNECTION,
            WIFI_SET_TRANSPARENT,
            WIFI_BEGIN_SEND_DATA
        };

        WIFI_COMMAND_TYPE Wifi_Task;


        public static void Wifi_TimeOutHander(int milliSecond)
        {
            int start = Environment.TickCount;
            int Second = milliSecond * 1000;
            while (Math.Abs(Environment.TickCount - start) < Second)    //改为秒
            {
                //Application.DoEvents();//可执行某无聊的操作
                if (Wifi_ReceiveOK_Flag)
                {
                    break;
                }
            }

            if (!Wifi_ReceiveOK_Flag)
            {
                Wifi_TimeOut_Flag = true;
            }
        }

        public void DataReceiveDeal_Wifi()
        {
            //int temp;
            //string OutByteToString;

            switch (Wifi_Task)
            {
                case WIFI_COMMAND_TYPE.WIFI_SET_STATION_MODE:
                case WIFI_COMMAND_TYPE.WIFI_CONNECT_AP:
                case WIFI_COMMAND_TYPE.WIFI_ESTABLISH_TCP_CONNECTION:
                case WIFI_COMMAND_TYPE.WIFI_SET_TRANSPARENT:
                case WIFI_COMMAND_TYPE.WIFI_BEGIN_SEND_DATA:
                    if (bRxByteCnt_Wifi >= Wifi_ReceiveDataNeed || Wifi_TimeOut_Flag)
                    {
                        //bRxByteCnt = 0;
                        Wifi_ReceiveOK_Flag = true;
                    }
                    break;

                //case WIFI_COMMAND_TYPE.BLUETOOTH_GET_DEV_NAME:     //因为设备名字长度不明确所以接收数据长度不明确
                //case WIFI_COMMAND_TYPE.BLUETOOTH_CHECK_STATUS:

                //break;

                case WIFI_COMMAND_TYPE.WIFI_SCAN_AP_NEARBY:

                    break;

                default:
                    break;
            }

        }

        public void DataReceiveJudge_Wifi()
        {
            try
            {
                //int temp1;
                //int temp2;
                //int temp3;
                //int temp_Begin;
                byte i = 0;
                //string OutByteToString;

                switch (BlueTooth_Task)
                {
                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_BAUD:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_BAUD:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_GET_DEV_NAME:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHANGE_DEV_NAME:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SAVE_TO_FLASH:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_SCAN_DEV_NEARBY:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CONNECT_HOST_DEV:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_DISCONNECT_HOST_DEV:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_CHECK_STATUS:
                        {
                            break;
                        }

                    case BLUETOOTH_COMMAND_TYPE.BLUETOOTH_RECOVER_FACTORY:
                        {
                            break;
                        }

                    default:
                        break;
                }

                bRxByteCnt_BT = 0;                  //必须复原，不然原来的数据造成假象
                for (i = 0; i < bRxDataBuff_BT.Length; i++)
                {
                    bRxDataBuff_BT[i] = 0;
                }
                BlueTooth_ComFlag = 0;      //复原，停止蓝牙整改通讯
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "蓝牙数据判断错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Wifi_SetStationMode()
        {
            try
            {
                bRxByteCnt_Wifi = 0;
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;

                Wifi_ReceiveDataNeed = 8;
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_SET_STATION_MODE;
                Wifi_Command = "AT+CWMODE=1\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;             //上位机波特率随之修改
                }));
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                listBox_Wifi_Interface.Invoke(new EventHandler(delegate
                {
                    listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);
                }));

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)    //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi设置站点模式错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Wifi_ConnectAP()
        {
            int i;
            try
            {
                if (textBox_Wifi_AP_Name.Text == "" || textBox_Wifi_AP_Password.Text == "")
                {
                    MessageBox.Show("请填写AP热点名字及其密码！", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox_Wifi_BandRate_Now.Text == "")
                {
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_Wifi = 0;
                for (i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;       //妈的，漏了这个初始化，导致老是溢出错误，而且各个函数也忘了try去catch导致真正的错误位置没定位到
                                                   //由于没有复原，导致接收1个就进去了数据判断函数，然后OutByteToString.IndexOf("\r")返回-1，然后错误
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;
                Wifi_ReceiveDataNeed = 8;          //[AT]OK\r\n，如果不是包括\r\n，则会出现接收错误的问题(字符串搜索的是\r字符)
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_CONNECT_AP;

                Wifi_Command = "AT+CWJAP=" + "\"" + textBox_Wifi_AP_Name + "\"" + "," + "\"" + textBox_Wifi_AP_Password + "\"" + "\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);
                listBox_BlueTooth_Interface.Items.Add("Tran: " + Wifi_Command);

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "连接AP热点错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Wifi_Establish_TCP_Connect()
        {
            try
            {
                bRxByteCnt_Wifi = 0;
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;

                Wifi_ReceiveDataNeed = 8;
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_ESTABLISH_TCP_CONNECTION;
                Wifi_Command = "AT+CIPSTART=\"TCP\",\"192.168.4.1\",5555\r\n";             //目标IP地址和端口都是固定的     
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;
                }));
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                listBox_Wifi_Interface.Invoke(new EventHandler(delegate
                {
                    listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);
                }));

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi建立TCP连接错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Wifi_Set_Transparent_Transmission()
        {
            try
            {
                bRxByteCnt_Wifi = 0;
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;

                Wifi_ReceiveDataNeed = 8;
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_SET_TRANSPARENT;
                Wifi_Command = "AT+CIPMODE=1\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;             //上位机波特率随之修改
                }));
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                listBox_Wifi_Interface.Invoke(new EventHandler(delegate
                {
                    listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);
                }));

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)    //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi设置透传模式错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Wifi_Begin_SendData()
        {
            try
            {
                bRxByteCnt_Wifi = 0;
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;

                Wifi_ReceiveDataNeed = 1;
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_BEGIN_SEND_DATA;
                Wifi_Command = "AT+CIPSEND\r\n";
                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;             //上位机波特率随之修改
                }));
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                listBox_Wifi_Interface.Invoke(new EventHandler(delegate
                {
                    listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);
                }));

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)    //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi开始发送数据错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Wifi_Factory_Click(object sender, EventArgs e)
        {

        }

        private void button_Wifi_BandRate_Get_Click(object sender, EventArgs e)
        {
            try
            {
                bRxByteCnt_Wifi = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;
                Wifi_ReceiveDataNeed = 2;           //4800波特率好像返回的字符没这么多，实验结果是波特率越高，能读到的字符越多，先3个试试水
                                                    //结论出来了，如果是115200，再用4800或者9600去尝试，一个字符都不会返回，所以超时机制只能设置为2s
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_GET_BAUD;
                Wifi_Command = "AT\r\n";           //是否是+++试探一下？

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(WifiBaudR[BaudR_Temp], 10);
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = WifiBaudR[BaudR_Temp];             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                if (BaudR_Temp == 0)     //第一次执行
                {
                    listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);
                }

                Wifi_TimeOutHander(1);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)
                {
                    DataReceiveJudge_Wifi();
                }
                //comboBox_BandRate.Text = comboBox_BT_BandRate_Now.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi模块波特率获取错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Wifi_BandRate_Change_Click(object sender, EventArgs e)
        {
            try
            {
                if ((comboBox_Wifi_BandRate_Now.Text == "") || (comboBox_Wifi_BandRate_Need.Text == ""))
                {
                    comboBox_Wifi_BandRate_Now.Text = "";
                    comboBox_Wifi_BandRate_Need.Text = "";
                    MessageBox.Show("波特率数据不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_Wifi = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;        //妈的，漏了这个初始化，导致老是溢出错误，而且各个函数也忘了try去catch导致真正的错误位置没定位到
                                                    //由于没有复原，导致接收1个就进去了数据判断函数，然后OutByteToString.IndexOf("\r")返回-1，然后错误
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;
                Wifi_ReceiveDataNeed = 8;          //[AT]OK\r\n，如果不是包括\r\n，则会出现接收错误的问题(字符串搜索的是\r字符)
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_CHANGE_BAUD;
                Wifi_Command = "AT+UART=" + comboBox_Wifi_BandRate_Need.Text + "," + "8" + "," + "1" + "," + "0" + "," + "0" + "\r\n";

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);     //这个不是依照ASCII表转化了，1234转化为"1234"
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;             //上位机波特率随之修改
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);
                listBox_Wifi_Interface.Items.Add("Tran: " + Wifi_Command);

                Wifi_TimeOutHander(5);
                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)          //如果8s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "Wifi模块波特率修改错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_AP_Get_Click(object sender, EventArgs e)
        {

        }

        private void button_AP_Change_Click(object sender, EventArgs e)
        {

        }

        private void button_Wifi_Check_Status_Click(object sender, EventArgs e)
        {

        }

        private void button_Scan_AP_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox_Wifi_BandRate_Now.Text == "")
                {
                    MessageBox.Show("当前波特率不能为空！", "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bRxByteCnt_Wifi = 0;                         //担忧别的地方调用了
                for (int i = 0; i < bRxDataBuff_Wifi.Length; i++)
                {
                    bRxDataBuff_Wifi[i] = 0;
                }
                Wifi_ReceiveOK_Flag = false;
                Wifi_TimeOut_Flag = false;
                Wifi_ComFlag = 1;
                Wifi_ReceiveDataNeed = 190;
                Wifi_Task = WIFI_COMMAND_TYPE.WIFI_SCAN_AP_NEARBY;
                Wifi_Command = "AT+CWSAP?\r\n";

                BlueTooth_ReceiveDataNeed = 8;

                byte[] CommandBuff = System.Text.Encoding.ASCII.GetBytes(Wifi_Command);

                serialPort1.BaudRate = Convert.ToInt32(comboBox_Wifi_BandRate_Now.Text, 10);
                comboBox_BandRate.Invoke(new EventHandler(delegate
                {
                    comboBox_BandRate.Text = comboBox_Wifi_BandRate_Now.Text;
                }));

                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                serialPort1.Write(CommandBuff, 0, Wifi_Command.Length);

                listBox_BlueTooth_Interface.Items.Add("Tran: " + Wifi_Command);   //问题找到了，首先串口发出命令后，会卡在这里6s，由于非占用延时问题，接收函数同步2s以内会接收完全部数据
                Wifi_TimeOutHander(6);                //这个时间长一些             //但是由于必须超时，才能Judge，这个时候不能执行Judge函数。超时完毕，该函数执行完毕，窗口显示发送命令内容
                                                      //由于接收函数不再执行，所以结束。后续思考为啥前面的线程没问题

                if (Wifi_TimeOut_Flag || Wifi_ReceiveOK_Flag)          //如果6s还没接收完，意味着没数据来了，接收函数不会再触发，这里要执行一下
                {
                    DataReceiveJudge_Wifi();
                }
                //*/
                //TestCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "AP热点搜索错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Connect_Wifi_Click(object sender, EventArgs e)
        {
            Wifi_SetStationMode();
        }

        private void button_DisConnect_Wifi_Click(object sender, EventArgs e)
        {

        }

        private void button_Select_AP_Click(object sender, EventArgs e)
        {
            if (listBox_Wifi_Dev.SelectedItem.ToString() == "")
            {
                MessageBox.Show("请选择要连接的AP热点！", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            textBox_Wifi_AP_Name.Text = listBox_Wifi_Dev.SelectedItem.ToString();
        }

        private void button_Clear_InterFace_Wifi_Click(object sender, EventArgs e)
        {
            listBox_Wifi_Interface.Items.Clear();
            listBox_Wifi_Dev.Items.Clear();
        }
        #endregion
    }

}
