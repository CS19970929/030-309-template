using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text;

namespace CommomUpper_32Series
{
    class SerialPort
    {

    }

    partial class Form1
    {
        #region 上位机指令及变量
        static public byte[] sendCatch = new byte[100];     //发送缓存
        public byte[] bRxDataBuff = new byte[250];

        public byte[,] EventRecord = new byte[100, 2];
        //串口通讯相关
        public static byte RS485_SLAVE_ADDR = 0x01;
        public byte RS485_BROADCAST_ADDR = 0x00;

        public byte RS485_CMD_READ_REGS = 0x03;
        public byte RS485_CMD_WRITE_REG = 0x06;
        public byte RS485_CMD_WRITE_REGS = 0x10;

        public UInt32 u16RdRunInfoRxCnt = 0;
        public UInt32 u16RdRunInfoTxCnt = 0;

        public byte bRs485FunCmd = 3;
        public byte bRdCmdErrCnt = 0;
        public UInt16 g_u16RdSel = 0;
        public UInt16 u16Rs485RegAddr = 0;
        public UInt16 u16Rs485RegNum = 0;
        public byte bRs485ByteNum = 0;
        public UInt16 u16Rs485RegData = 0;
        public string bLanguageSelect;

        public bool bl_RxFinishedFlag = true;

        public enum RS485_CMD_RW_E
        {
            RS485_CMD_ADDR_RESET_CALIB_COEF = 0x1000,
            RS485_CMD_ADDR_RESET_PROTECT_RECORD,
            RS485_CMD_ADDR_RESET_PROTECT_ELEMENT,
            RS485_CMD_ADDR_RESET_OTHER_CANADD,
            RS485_CMD_ADDR_RESET_HEAT_COOL,
            RS485_CMD_ADDR_SET_ONCE_SOC,
            RS485_CMD_ADDR_RESET_AFE_PARAMETERS,
            RS485_CMD_ADDR_RESET_EVENT_RECORD,
            /*
            RS485_CMD_ADDR_SYSFUNC_ONOFF_BALANCE = 0x1100,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_BMS_SOURCE,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_MOS,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_RELAY,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_SOC_FIXED,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_HEAT,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_COOL,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_AFE1,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_AFE2,
            RS485_CMD_ADDR_SYSFUNC_ONOFF_SLEEP,
            */
            RS485_CMD_ADDR_SWITCH_ON = 0x1100,
            RS485_CMD_ADDR_SWITCH_OFF,
            RS485_CMD_ADDR_SYSTEM_FUNCTION_ON,
            RS485_CMD_ADDR_SYSTEM_FUNCTION_OFF,

            RS485_CMD_ADDR_VC1CALIB_K = 0x2000,
            RS485_CMD_ADDR_VC1CALIB_B,
            RS485_CMD_ADDR_VC2CALIB_K,
            RS485_CMD_ADDR_VC2CALIB_B,
            RS485_CMD_ADDR_VC3CALIB_K,
            RS485_CMD_ADDR_VC3CALIB_B,
            RS485_CMD_ADDR_VC4CALIB_K,
            RS485_CMD_ADDR_VC4CALIB_B,
            RS485_CMD_ADDR_VC5CALIB_K,
            RS485_CMD_ADDR_VC5CALIB_B,
            RS485_CMD_ADDR_VC6CALIB_K,
            RS485_CMD_ADDR_VC6CALIB_B,
            RS485_CMD_ADDR_VC7CALIB_K,
            RS485_CMD_ADDR_VC7CALIB_B,
            RS485_CMD_ADDR_VC8CALIB_K,
            RS485_CMD_ADDR_VC8CALIB_B,
            RS485_CMD_ADDR_VC9CALIB_K,
            RS485_CMD_ADDR_VC9CALIB_B,
            RS485_CMD_ADDR_VC10CALIB_K,
            RS485_CMD_ADDR_VC10CALIB_B,
            RS485_CMD_ADDR_VC11CALIB_K,
            RS485_CMD_ADDR_VC11CALIB_B,
            RS485_CMD_ADDR_VC12CALIB_K,
            RS485_CMD_ADDR_VC12CALIB_B,
            RS485_CMD_ADDR_VC13CALIB_K,
            RS485_CMD_ADDR_VC13CALIB_B,
            RS485_CMD_ADDR_VC14CALIB_K,
            RS485_CMD_ADDR_VC14CALIB_B,
            RS485_CMD_ADDR_VC15CALIB_K,
            RS485_CMD_ADDR_VC15CALIB_B,
            RS485_CMD_ADDR_VC16CALIB_K,
            RS485_CMD_ADDR_VC16CALIB_B,
            RS485_CMD_ADDR_VC17CALIB_K,
            RS485_CMD_ADDR_VC17CALIB_B,
            RS485_CMD_ADDR_VC18CALIB_K,
            RS485_CMD_ADDR_VC18CALIB_B,
            RS485_CMD_ADDR_VC19CALIB_K,
            RS485_CMD_ADDR_VC19CALIB_B,
            RS485_CMD_ADDR_VC20CALIB_K,
            RS485_CMD_ADDR_VC20CALIB_B,
            RS485_CMD_ADDR_VC21CALIB_K,
            RS485_CMD_ADDR_VC21CALIB_B,
            RS485_CMD_ADDR_VC22CALIB_K,
            RS485_CMD_ADDR_VC22CALIB_B,
            RS485_CMD_ADDR_VC23CALIB_K,
            RS485_CMD_ADDR_VC23CALIB_B,
            RS485_CMD_ADDR_VC24CALIB_K,
            RS485_CMD_ADDR_VC24CALIB_B,
            RS485_CMD_ADDR_VC25CALIB_K,
            RS485_CMD_ADDR_VC25CALIB_B,
            RS485_CMD_ADDR_VC26CALIB_K,
            RS485_CMD_ADDR_VC26CALIB_B,
            RS485_CMD_ADDR_VC27CALIB_K,
            RS485_CMD_ADDR_VC27CALIB_B,
            RS485_CMD_ADDR_VC28CALIB_K,
            RS485_CMD_ADDR_VC28CALIB_B,
            RS485_CMD_ADDR_VC29CALIB_K,
            RS485_CMD_ADDR_VC29CALIB_B,
            RS485_CMD_ADDR_VC30CALIB_K,
            RS485_CMD_ADDR_VC30CALIB_B,
            RS485_CMD_ADDR_VC31CALIB_K,
            RS485_CMD_ADDR_VC31CALIB_B,
            RS485_CMD_ADDR_VC32CALIB_K,
            RS485_CMD_ADDR_VC32CALIB_B,
            RS485_CMD_ADDR_AFE1CALIB_K,
            RS485_CMD_ADDR_AFE1CALIB_B,
            RS485_CMD_ADDR_AFE2CALIB_K,
            RS485_CMD_ADDR_AFE2CALIB_B,
            RS485_CMD_ADDR_VBUSCALIB_K,
            RS485_CMD_ADDR_VBUSCALIB_B,

            //RS485_CMD_ADDR_ICHGCALIB_K = 0x2100,
            RS485_CMD_ADDR_ICHGCALIB_K,
            RS485_CMD_ADDR_ICHGCALIB_B,
            RS485_CMD_ADDR_IDISCHGCALIB_K,
            RS485_CMD_ADDR_IDISCHGCALIB_B,
            RS485_CMD_ADDR_TEMP1_CALIB_K,
            RS485_CMD_ADDR_TEMP1_CALIB_B,
            RS485_CMD_ADDR_TEMP2_CALIB_K,
            RS485_CMD_ADDR_TEMP2_CALIB_B,
            RS485_CMD_ADDR_TEMP3_CALIB_K,
            RS485_CMD_ADDR_TEMP3_CALIB_B,
            RS485_CMD_ADDR_TEMP4_CALIB_K,
            RS485_CMD_ADDR_TEMP4_CALIB_B,
            RS485_CMD_ADDR_TEMP5_CALIB_K,
            RS485_CMD_ADDR_TEMP5_CALIB_B,
            RS485_CMD_ADDR_TEMP6_CALIB_K,
            RS485_CMD_ADDR_TEMP6_CALIB_B,
            RS485_CMD_ADDR_TEMP_ENV1_CALIB_K,
            RS485_CMD_ADDR_TEMP_ENV1_CALIB_B,
            RS485_CMD_ADDR_TEMP_ENV2_CALIB_K,
            RS485_CMD_ADDR_TEMP_ENV2_CALIB_B,
            RS485_CMD_ADDR_TEMP_ENV3_CALIB_K,
            RS485_CMD_ADDR_TEMP_ENV3_CALIB_B,
            RS485_CMD_ADDR_TEMP_MOS_CALIB_K,
            RS485_CMD_ADDR_TEMP_MOS_CALIB_B,

            RS485_CMD_ADDR_VCELL_OVP_FIRST = 0x2100, //8448
            RS485_CMD_ADDR_VCELL_OVP_SECOND,
            RS485_CMD_ADDR_VCELL_OVP_THIRD,
            RS485_CMD_ADDR_VCELL_OVP_RCV,
            RS485_CMD_ADDR_VCELL_OVP_FILTER,

            RS485_CMD_ADDR_VCELL_UVP_FIRST,
            RS485_CMD_ADDR_VCELL_UVP_SECOND,
            RS485_CMD_ADDR_VCELL_UVP_THIRD,
            RS485_CMD_ADDR_VCELL_UVP_RCV,
            RS485_CMD_ADDR_VCELL_UVP_FILTER,

            RS485_CMD_ADDR_VBUS_OVP_FIRST,
            RS485_CMD_ADDR_VBUS_OVP_SECOND,
            RS485_CMD_ADDR_VBUS_OVP_THIRD,
            RS485_CMD_ADDR_VBUS_OVP_RCV,
            RS485_CMD_ADDR_VBUS_OVP_FILTER,

            RS485_CMD_ADDR_VBUS_UVP_FIRST,
            RS485_CMD_ADDR_VBUS_UVP_SECOND,
            RS485_CMD_ADDR_VBUS_UVP_THIRD,
            RS485_CMD_ADDR_VBUS_UVP_RCV,
            RS485_CMD_ADDR_VBUS_UVP_FILTER,

            RS485_CMD_ADDR_ICHG_OCP_FIRST,
            RS485_CMD_ADDR_ICHG_OCP_SECOND,
            RS485_CMD_ADDR_ICHG_OCP_THIRD,
            RS485_CMD_ADDR_ICHG_OCP_RCV,
            RS485_CMD_ADDR_ICHG_OCP_FILTER,

            RS485_CMD_ADDR_IDSG_OCP_FIRST,
            RS485_CMD_ADDR_IDSG_OCP_SECOND,
            RS485_CMD_ADDR_IDSG_OCP_THIRD,
            RS485_CMD_ADDR_IDSG_OCP_RCV,
            RS485_CMD_ADDR_IDSG_OCP_FILTER,

            RS485_CMD_ADDR_TCHG_OTP_FIRST,
            RS485_CMD_ADDR_TCHG_OTP_SECOND,
            RS485_CMD_ADDR_TCHG_OTP_THIRD,
            RS485_CMD_ADDR_TCHG_OTP_RCV,
            RS485_CMD_ADDR_TCHG_OTP_FILTER,

            RS485_CMD_ADDR_TCHG_UTP_FIRST,
            RS485_CMD_ADDR_TCHG_UTP_SECOND,
            RS485_CMD_ADDR_TCHG_UTP_THIRD,
            RS485_CMD_ADDR_TCHG_UTP_RCV,
            RS485_CMD_ADDR_TCHG_UTP_FILTER,

            RS485_CMD_ADDR_TDSG_OTP_FIRST,
            RS485_CMD_ADDR_TDSG_OTP_SECOND,
            RS485_CMD_ADDR_TDSG_OTP_THIRD,
            RS485_CMD_ADDR_TDSG_OTP_RCV,
            RS485_CMD_ADDR_TDSG_OTP_FILTER,

            RS485_CMD_ADDR_TDSG_UTP_FIRST,
            RS485_CMD_ADDR_TDSG_UTP_SECOND,
            RS485_CMD_ADDR_TDSG_UTP_THIRD,
            RS485_CMD_ADDR_TDSG_UTP_RCV,
            RS485_CMD_ADDR_TDSG_UTP_FILTER,

            RS485_CMD_ADDR_TMOS_OTP_FIRST,
            RS485_CMD_ADDR_TMOS_OTP_SECOND,
            RS485_CMD_ADDR_TMOS_OTP_THIRD,
            RS485_CMD_ADDR_TMOS_OTP_RCV,
            RS485_CMD_ADDR_TMOS_OTP_FILTER,

            RS485_CMD_ADDR_VDELTA_OP_FIRST,
            RS485_CMD_ADDR_VDELTA_OP_SECOND,
            RS485_CMD_ADDR_VDELTA_OP_THIRD,
            RS485_CMD_ADDR_VDELTA_OP_RCV,
            RS485_CMD_ADDR_VDELTA_OP_FILTER,

            RS485_CMD_ADDR_SOC_UP_FIRST,
            RS485_CMD_ADDR_SOC_UP_SECOND,
            RS485_CMD_ADDR_SOC_UP_THIRD,
            RS485_CMD_ADDR_SOC_UP_RCV,
            RS485_CMD_ADDR_SOC_UP_FILTER,

            RS485_CMD_ADDR_VPACK_OVP_FIRST,
            RS485_CMD_ADDR_VPACK_OVP_SECOND,
            RS485_CMD_ADDR_VPACK_OVP_THIRD,
            RS485_CMD_ADDR_VPACK_OVP_RCV,
            RS485_CMD_ADDR_VPACK_OVP_FILTER,

            RS485_CMD_ADDR_VPACK_UVP_FIRST,
            RS485_CMD_ADDR_VPACK_UVP_SECOND,
            RS485_CMD_ADDR_VPACK_UVP_THIRD,
            RS485_CMD_ADDR_VPACK_UVP_RCV,
            RS485_CMD_ADDR_VPACK_UVP_FILTER,

            RS485_CMD_ADDR_PARALLEL_SERIAL_NUM,
            RS485_CMD_ADDR_PARALLEL_PACK_NUM,
            RS485_CMD_ADDR_PARALLEL_RES1,
            RS485_CMD_ADDR_PARALLEL_RES2,
            RS485_CMD_ADDR_PARALLEL_RES3,

            RS485_CMD_ADDR_SOC_VOLTAGE1 = 0x2200,
            RS485_CMD_ADDR_SOC_VALUE1,
            RS485_CMD_ADDR_SOC_VOLTAGE2,
            RS485_CMD_ADDR_SOC_VALUE2,
            RS485_CMD_ADDR_SOC_VOLTAGE3,
            RS485_CMD_ADDR_SOC_VALUE3,
            RS485_CMD_ADDR_SOC_VOLTAGE4,
            RS485_CMD_ADDR_SOC_VALUE4,
            RS485_CMD_ADDR_SOC_VOLTAGE5,
            RS485_CMD_ADDR_SOC_VALUE5,
            RS485_CMD_ADDR_SOC_VOLTAGE6,
            RS485_CMD_ADDR_SOC_VALUE6,
            RS485_CMD_ADDR_SOC_VOLTAGE7,
            RS485_CMD_ADDR_SOC_VALUE7,
            RS485_CMD_ADDR_SOC_VOLTAGE8,
            RS485_CMD_ADDR_SOC_VALUE8,
            RS485_CMD_ADDR_SOC_VOLTAGE9,
            RS485_CMD_ADDR_SOC_VALUE9,
            RS485_CMD_ADDR_SOC_VOLTAGE10,
            RS485_CMD_ADDR_SOC_VALUE10,
            RS485_CMD_ADDR_SOC_VOLTAGE11,
            RS485_CMD_ADDR_SOC_VALUE11,
            RS485_CMD_ADDR_SOC_VOLTAGE12,
            RS485_CMD_ADDR_SOC_VALUE12,
            RS485_CMD_ADDR_SOC_VOLTAGE13,
            RS485_CMD_ADDR_SOC_VALUE13,
            RS485_CMD_ADDR_SOC_VOLTAGE14,
            RS485_CMD_ADDR_SOC_VALUE14,
            RS485_CMD_ADDR_SOC_VOLTAGE15,
            RS485_CMD_ADDR_SOC_VALUE15,
            RS485_CMD_ADDR_SOC_VOLTAGE16,
            RS485_CMD_ADDR_SOC_VALUE16,
            RS485_CMD_ADDR_SOC_VOLTAGE17,
            RS485_CMD_ADDR_SOC_VALUE17,
            RS485_CMD_ADDR_SOC_VOLTAGE18,
            RS485_CMD_ADDR_SOC_VALUE18,
            RS485_CMD_ADDR_SOC_VOLTAGE19,
            RS485_CMD_ADDR_SOC_VALUE19,
            RS485_CMD_ADDR_SOC_VOLTAGE20,
            RS485_CMD_ADDR_SOC_VALUE20,
            RS485_CMD_ADDR_SOC_VOLTAGE21,
            RS485_CMD_ADDR_SOC_VALUE21,

            RS485_CMD_ADDR_COPPERLOSS1,
            RS485_CMD_ADDR_COPPERLOSS2,
            RS485_CMD_ADDR_COPPERLOSS3,
            RS485_CMD_ADDR_COPPERLOSS4,
            RS485_CMD_ADDR_COPPERLOSS5,
            RS485_CMD_ADDR_COPPERLOSS6,
            RS485_CMD_ADDR_COPPERLOSS7,
            RS485_CMD_ADDR_COPPERLOSS8,
            RS485_CMD_ADDR_COPPERLOSS9,
            RS485_CMD_ADDR_COPPERLOSS10,
            RS485_CMD_ADDR_COPPERLOSS11,
            RS485_CMD_ADDR_COPPERLOSS12,
            RS485_CMD_ADDR_COPPERLOSS13,
            RS485_CMD_ADDR_COPPERLOSS14,
            RS485_CMD_ADDR_COPPERLOSS15,
            RS485_CMD_ADDR_COPPERLOSS16,
            RS485_CMD_ADDR_CELLNUM1,
            RS485_CMD_ADDR_CELLNUM2,
            RS485_CMD_ADDR_CELLNUM3,
            RS485_CMD_ADDR_CELLNUM4,
            RS485_CMD_ADDR_CELLNUM5,
            RS485_CMD_ADDR_CELLNUM6,
            RS485_CMD_ADDR_CELLNUM7,
            RS485_CMD_ADDR_CELLNUM8,
            RS485_CMD_ADDR_CELLNUM9,
            RS485_CMD_ADDR_CELLNUM10,
            RS485_CMD_ADDR_CELLNUM11,
            RS485_CMD_ADDR_CELLNUM12,
            RS485_CMD_ADDR_CELLNUM13,
            RS485_CMD_ADDR_CELLNUM14,
            RS485_CMD_ADDR_CELLNUM15,
            RS485_CMD_ADDR_CELLNUM16,

            RS485_CMD_ADDR_RTC_TIME_YEAR,
            RS485_CMD_ADDR_RTC_TIME_MONTH,
            RS485_CMD_ADDR_RTC_TIME_DAY,
            RS485_CMD_ADDR_RTC_TIME_HOUR,
            RS485_CMD_ADDR_RTC_TIME_MINUTE,
            RS485_CMD_ADDR_RTC_TIME_SECOND,
            RS485_CMD_ADDR_RTC_ALARM_YEAR,
            RS485_CMD_ADDR_RTC_ALARM_MONTH,
            RS485_CMD_ADDR_RTC_ALARM_DAY,
            RS485_CMD_ADDR_RTC_ALARM_HOUR,
            RS485_CMD_ADDR_RTC_ALARM_MINUTE,
            RS485_CMD_ADDR_RTC_ALARM_SECOND,

            RS485_CMD_ADDR_BALANCE_OV = 0x2300,
            RS485_CMD_ADDR_BALANCE_OW,
            RS485_CMD_ADDR_BALANCE_CW1,
            RS485_CMD_ADDR_BALANCE_CW2,
            RS485_CMD_ADDR_OPENTIME_ODD,
            RS485_CMD_ADDR_OPENTIME_EVEN,
            RS485_CMD_ADDR_OPENTIME_MOS,
            RS485_CMD_ADDR_OPENTIME_RES,

            RS485_CMD_ADDR_CS_CUR_CHGMAX,
            RS485_CMD_ADDR_CS_CUR_DSGMAX,
            RS485_CMD_ADDR_CBC_CUR_CHG,
            RS485_CMD_ADDR_CBC_CUR_DSG,
            RS485_CMD_ADDR_COOL_DSG_H,
            RS485_CMD_ADDR_COOL_DSG_L,
            RS485_CMD_ADDR_COOL_CHG_H,
            RS485_CMD_ADDR_COOL_CHG_L,

            RS485_CMD_ADDR_SLEEP_V_NORMAL,
            RS485_CMD_ADDR_SLEEP_TIME_NORMAL,
            RS485_CMD_ADDR_SLEEP_V_LOW,
            RS485_CMD_ADDR_SLEEP_TIME_LOW,
            RS485_CMD_ADDR_SLEEP_I_CHG,
            RS485_CMD_ADDR_SLEEP_I_DSG,
            RS485_CMD_ADDR_SLEEP_RES1,
            RS485_CMD_ADDR_SLEEP_RES2,

            RS485_CMD_ADDR_SOC_AH,
            RS485_CMD_ADDR_SOC_CYCLE_TIME,
            RS485_CMD_ADDR_SOC_RES1,
            RS485_CMD_ADDR_SOC_RES2,

            RS485_CMD_ADDR_SYS_SERIES_NUM,
            RS485_CMD_ADDR_SYS_CS_RESIS,
            RS485_CMD_ADDR_SYS_CS_NUM,
            RS485_CMD_ADDR_SYS_RES1,


            RS485_CMD_ADDR_HEAT_DSG_HIGH,
            RS485_CMD_ADDR_HEAT_DSG_LOW,
            RS485_CMD_ADDR_HEAT_CHG_HIGH,
            RS485_CMD_ADDR_HEAT_CHG_LOW,
            RS485_CMD_ADDR_HEAT_CUR_MAX,
            RS485_CMD_ADDR_HEAT_CUR_MIN,
            RS485_CMD_ADDR_HEAT_TIME_MAX,
            RS485_CMD_ADDR_HEAT_RES1,
            RS485_CMD_ADDR_HEAT_RES2,
            RS485_CMD_ADDR_HEAT_RES3,
            RS485_CMD_ADDR_HEAT_RES4,
            RS485_CMD_ADDR_HEAT_RES5,
            RS485_CMD_ADDR_HEAT_RES6,

            RS485_CMD_ADDR_COOL_DSG_HIGH,
            RS485_CMD_ADDR_COOL_DSG_LOW,
            RS485_CMD_ADDR_COOL_CHG_HIGH,
            RS485_CMD_ADDR_COOL_CHG_LOW,
            RS485_CMD_ADDR_COOL_CUR_MAX,
            RS485_CMD_ADDR_COOL_CUR_MIN,
            RS485_CMD_ADDR_COOL_TIME_MAX,
            RS485_CMD_ADDR_COOL_RES1,
            RS485_CMD_ADDR_COOL_RES2,
            RS485_CMD_ADDR_COOL_RES3,
            RS485_CMD_ADDR_COOL_RES4,   //0x2337

            RS485_CMD_ADDR_CANID,       //0x2338
            RS485_CMD_ADDR_CANID_LOW,   //0x2339

            RS485_CMD_ADDR_DEVADDR = 0x2340,
        };
        #endregion

        #region CRC校验
        static public void Calculate_CRC16_TX(ref byte[] Data, byte Lenth)
        {
            // Data指向要计算的CRC数组，Lenth为数据的有效长度
            UInt16 CRC = 0xFFFF;	//CRC的初始值
            UInt16 i, j;

            for (i = 0; i < Lenth; i++)
            {
                CRC ^= Data[i];				//和当前字节异或一次		//CRC ^= Data[i];
                for (j = 0; j < 8; j++)	//每个字节循环8次		
                {
                    CRC >>= 1;			    //右移1位			
                    if ((CRC & 0x01) != 0)
                    {
                        CRC ^= 0xA001;		//和多项式异或
                    }
                }
            }
            Data[Lenth] = (byte)(CRC / 256);
            Data[Lenth + 1] = (byte)(CRC % 256);
        }
        static public void Calculate_Sum_Tx(ref byte[] Data, UInt32 Lenth)
        {

            // Data指向要计算的CRC数组，Lenth为数据的有效长度
            UInt16 CRC = 0xFFFF;	//CRC的初始值
            UInt16 i, j;

            for (i = 0; i < Lenth; i++)
            {
                CRC ^= Data[i];				//和当前字节异或一次		//CRC ^= Data[i];
                for (j = 0; j < 8; j++)	//每个字节循环8次		
                {
                    if ((CRC & 0x01) != 0)
                    {
                        CRC >>= 1;			    //右移1位	
                        CRC ^= 0xA001;		//和多项式异或
                    }
                    else
                    {
                        CRC >>= 1;			    //右移1位	
                    }
                }
            }
            Data[Lenth] = (byte)(CRC % 256);
            Data[Lenth + 1] = (byte)(CRC / 256);
        }
        static public bool Calculate_Sum_Rx(ref byte[] Data, byte Lenth)
        {
            // Data指向要计算的CRC数组，Lenth为数据的有效长度
            UInt16 CRC = 0xFFFF;	//CRC的初始值
            UInt16 i, j;
            bool bCRCResult;

            for (i = 0; i < Lenth; i++)
            {
                CRC ^= Data[i];				//和当前字节异或一次		//CRC ^= Data[i];
                for (j = 0; j < 8; j++)	//每个字节循环8次		
                {
                    //CRC >>= 1;			    //右移1位			
                    if ((CRC & 0x01) != 0)
                    {
                        CRC >>= 1;			    //右移1位	
                        CRC ^= 0xA001;		//和多项式异或
                    }
                    else
                    {
                        CRC >>= 1;			    //右移1位	
                    }
                }
            }

            if (CRC == (Data[Lenth + 1] * 256 + Data[Lenth]))
            {
                bCRCResult = true;
            }
            else
            {
                bCRCResult = false;
            }
            return bCRCResult;
        }
        #endregion CRC校验

        private void RxRdRunInfoAck_tianhanlianxing(byte[] bydata)
        {
            int high;
            int low;
            if((bRxDataBuff[0] == 0xaa) && ((bRxDataBuff[1] == 0xeb)) && bRxDataBuff[2] == 0x0f && bRxByteCnt == 20)
            {
                // 把两个字节当作 ASCII 字符直接转成字符串
                string s = Encoding.ASCII.GetString(bydata, 5, 2); // 等同于 new string(new char[]{(char)bydata[5], (char)bydata[6]})

                if (label_ver.InvokeRequired)
                {
                    label_ver.Invoke(new Action(() => label_ver.Text = s));
                }
                else
                {
                    label_ver.Text = s;
                }

                 u16RdRunInfoRxCnt++;
                if (Lang.b_LangFlag == 0)
                {
                    toolStripLabel_RxCnt1.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                }
                else
                {
                    toolStripLabel_RxCnt1.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                }

            }
            else if((bRxDataBuff[0] == 0x5a) && ((bRxDataBuff[1] == 0xa5)) && bRxDataBuff[2] == 0x03 && bRxByteCnt == 22)
            {
                //little endian
                ushort value = (ushort)((bydata[7] << 8) | bydata[6]);

                if (label_chg_time.InvokeRequired)
                {
                    label_chg_time.Invoke(new Action(() => label_chg_time.Text = value.ToString()));
                }
                else
                {
                    label_chg_time.Text = value.ToString();
                }

                value = (ushort)((bydata[9] << 8) | bydata[8]);

                if (label_dsg_time.InvokeRequired)
                {
                    label_dsg_time.Invoke(new Action(() => label_dsg_time.Text = value.ToString()));
                }
                else
                {
                    label_dsg_time.Text = value.ToString();
                }

                 u16RdRunInfoRxCnt++;
                if (Lang.b_LangFlag == 0)
                {
                    toolStripLabel_RxCnt1.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                }
                else
                {
                    toolStripLabel_RxCnt1.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                }

            }

        }

        #region 串口接收解码
        //0xD000的数据循环读取100ms  0x03功能
        private void RxRdRunInfoAck(byte[] bydata)
        {
            try
            {
                int i = 3;
                int temp;
                switch (u16Rs485RegAddr)
                {
                    case 0xD000:
                        {
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC1.Invoke(new EventHandler(delegate
                                {
                                    label_VC1.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC1.Invoke(new EventHandler(delegate
                                {
                                    label_VC1.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC2.Invoke(new EventHandler(delegate
                                {
                                    label_VC2.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC2.Invoke(new EventHandler(delegate
                                {
                                    label_VC2.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC3.Invoke(new EventHandler(delegate
                                {
                                    label_VC3.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC3.Invoke(new EventHandler(delegate
                                {
                                    label_VC3.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC4.Invoke(new EventHandler(delegate
                                {
                                    label_VC4.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC4.Invoke(new EventHandler(delegate
                                {
                                    label_VC4.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC5.Invoke(new EventHandler(delegate
                                {
                                    label_VC5.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC5.Invoke(new EventHandler(delegate
                                {
                                    label_VC5.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC6.Invoke(new EventHandler(delegate
                                {
                                    label_VC6.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC6.Invoke(new EventHandler(delegate
                                {
                                    label_VC6.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC7.Invoke(new EventHandler(delegate
                                {
                                    label_VC7.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC7.Invoke(new EventHandler(delegate
                                {
                                    label_VC7.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC8.Invoke(new EventHandler(delegate
                                {
                                    label_VC8.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC8.Invoke(new EventHandler(delegate
                                {
                                    label_VC8.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC9.Invoke(new EventHandler(delegate
                                {
                                    label_VC9.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC9.Invoke(new EventHandler(delegate
                                {
                                    label_VC9.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC10.Invoke(new EventHandler(delegate
                                {
                                    label_VC10.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC10.Invoke(new EventHandler(delegate
                                {
                                    label_VC10.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC11.Invoke(new EventHandler(delegate
                                {
                                    label_VC11.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC11.Invoke(new EventHandler(delegate
                                {
                                    label_VC11.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC12.Invoke(new EventHandler(delegate
                                {
                                    label_VC12.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC12.Invoke(new EventHandler(delegate
                                {
                                    label_VC12.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC13.Invoke(new EventHandler(delegate
                                {
                                    label_VC13.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC13.Invoke(new EventHandler(delegate
                                {
                                    label_VC13.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC14.Invoke(new EventHandler(delegate
                                {
                                    label_VC14.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC14.Invoke(new EventHandler(delegate
                                {
                                    label_VC14.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC15.Invoke(new EventHandler(delegate
                                {
                                    label_VC15.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC15.Invoke(new EventHandler(delegate
                                {
                                    label_VC15.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC16.Invoke(new EventHandler(delegate
                                {
                                    label_VC16.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC16.Invoke(new EventHandler(delegate
                                {
                                    label_VC16.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC17.Invoke(new EventHandler(delegate
                                {
                                    label_VC17.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC17.Invoke(new EventHandler(delegate
                                {
                                    label_VC17.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC18.Invoke(new EventHandler(delegate
                                {
                                    label_VC18.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC18.Invoke(new EventHandler(delegate
                                {
                                    label_VC18.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC19.Invoke(new EventHandler(delegate
                                {
                                    label_VC19.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC19.Invoke(new EventHandler(delegate
                                {
                                    label_VC19.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC20.Invoke(new EventHandler(delegate
                                {
                                    label_VC20.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC20.Invoke(new EventHandler(delegate
                                {
                                    label_VC20.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC21.Invoke(new EventHandler(delegate
                                {
                                    label_VC21.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC21.Invoke(new EventHandler(delegate
                                {
                                    label_VC21.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC22.Invoke(new EventHandler(delegate
                                {
                                    label_VC22.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC22.Invoke(new EventHandler(delegate
                                {
                                    label_VC22.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC23.Invoke(new EventHandler(delegate
                                {
                                    label_VC23.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC23.Invoke(new EventHandler(delegate
                                {
                                    label_VC23.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC24.Invoke(new EventHandler(delegate
                                {
                                    label_VC24.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC24.Invoke(new EventHandler(delegate
                                {
                                    label_VC24.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC25.Invoke(new EventHandler(delegate
                                {
                                    label_VC25.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC25.Invoke(new EventHandler(delegate
                                {
                                    label_VC25.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC26.Invoke(new EventHandler(delegate
                                {
                                    label_VC26.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC26.Invoke(new EventHandler(delegate
                                {
                                    label_VC26.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC27.Invoke(new EventHandler(delegate
                                {
                                    label_VC27.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC27.Invoke(new EventHandler(delegate
                                {
                                    label_VC27.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC28.Invoke(new EventHandler(delegate
                                {
                                    label_VC28.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC28.Invoke(new EventHandler(delegate
                                {
                                    label_VC28.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC29.Invoke(new EventHandler(delegate
                                {
                                    label_VC29.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC29.Invoke(new EventHandler(delegate
                                {
                                    label_VC29.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC30.Invoke(new EventHandler(delegate
                                {
                                    label_VC30.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC30.Invoke(new EventHandler(delegate
                                {
                                    label_VC30.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC31.Invoke(new EventHandler(delegate
                                {
                                    label_VC31.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC31.Invoke(new EventHandler(delegate
                                {
                                    label_VC31.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }
                            temp = bydata[i++] * 256 + bydata[i++];
                            if (temp >= 60000)
                            {
                                label_VC32.Invoke(new EventHandler(delegate
                                {
                                    label_VC32.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_VC32.Invoke(new EventHandler(delegate
                                {
                                    label_VC32.Text = Convert.ToDouble((float)(temp)).ToString("0");
                                }));
                            }

                            label_Vcell_max.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_max.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));

                            label_Vcell_min.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_min.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));

                            label_Max_pos.Invoke(new EventHandler(delegate
                            {
                                label_Max_pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));

                            label_Min_pos.Invoke(new EventHandler(delegate
                            {
                                label_Min_pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));

                            label_V_delta.Invoke(new EventHandler(delegate
                            {
                                label_V_delta.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));

                            label_Vbat.Invoke(new EventHandler(delegate
                            {
                                label_Vbat.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.0");
                            }));
                            break;
                        }
                    case 0xD026:
                        {
                            i = 3;
                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp1.Invoke(new EventHandler(delegate
                                {
                                    label_Temp1.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp1.Invoke(new EventHandler(delegate
                                {
                                    label_Temp1.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp2.Invoke(new EventHandler(delegate
                                {
                                    label_Temp2.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp2.Invoke(new EventHandler(delegate
                                {
                                    label_Temp2.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp3.Invoke(new EventHandler(delegate
                                {
                                    label_Temp3.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp3.Invoke(new EventHandler(delegate
                                {
                                    label_Temp3.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp4.Invoke(new EventHandler(delegate
                                {
                                    label_Temp4.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp4.Invoke(new EventHandler(delegate
                                {
                                    label_Temp4.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp5.Invoke(new EventHandler(delegate
                                {
                                    label_Temp5.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp5.Invoke(new EventHandler(delegate
                                {
                                    label_Temp5.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_Temp6.Invoke(new EventHandler(delegate
                                {
                                    label_Temp6.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_Temp6.Invoke(new EventHandler(delegate
                                {
                                    label_Temp6.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempEnv1.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv1.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempEnv1.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv1.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }
                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempEnv2.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv2.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempEnv2.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv2.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempEnv3.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv3.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempEnv3.Invoke(new EventHandler(delegate
                                {
                                    label_TempEnv3.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempMos.Invoke(new EventHandler(delegate
                                {
                                    label_TempMos.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempMos.Invoke(new EventHandler(delegate
                                {
                                    label_TempMos.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempMax.Invoke(new EventHandler(delegate
                                {
                                    label_TempMax.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempMax.Invoke(new EventHandler(delegate
                                {
                                    label_TempMax.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            temp = (bydata[i++] * 256 + bydata[i++]) / 10 - 40;
                            if (temp <= TempOffset)
                            {
                                label_TempMin.Invoke(new EventHandler(delegate
                                {
                                    label_TempMin.Text = "NA";
                                }));
                            }
                            else
                            {
                                label_TempMin.Invoke(new EventHandler(delegate
                                {
                                    label_TempMin.Text = Convert.ToDouble((float)temp).ToString("0.0");
                                }));
                            }

                            label_Ichg.Invoke(new EventHandler(delegate
                            {
                                label_Ichg.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                            }));

                            label_Idsg.Invoke(new EventHandler(delegate
                            {
                                label_Idsg.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                            }));

                            label_SOC.Invoke(new EventHandler(delegate
                            {
                                label_SOC.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0.0");
                            }));
                            label_SOH.Invoke(new EventHandler(delegate
                            {
                                label_SOH.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0.0");
                            }));
                            label_Present_mAh.Invoke(new EventHandler(delegate
                            {
                                label_Present_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) * 10).ToString("0");
                            }));
                            label_Full_mAh.Invoke(new EventHandler(delegate
                            {
                                label_Full_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) * 10).ToString("0");
                            }));
                            label_Factory_mAh.Invoke(new EventHandler(delegate
                            {
                                label_Factory_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) * 10).ToString("0");
                            }));
                            label_CycleTimes.Invoke(new EventHandler(delegate
                            {
                                label_CycleTimes.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            }));


                            if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                            {
                                label_Fault_First.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_First.Text = Convert.ToDouble((float)(1)).ToString("0");
                                }));
                            }
                            else
                            {
                                label_Fault_First.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_First.Text = Convert.ToDouble((float)(0)).ToString("0");
                                }));
                            }
                            label_Vcell_OV_First.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_OV_First.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                            }));
                            label_Vcell_UV_First.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_UV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Vbat_OV_First.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_OV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Vbat_UV_First.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_UV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_CHG_OC_First.Invoke(new EventHandler(delegate
                            {
                                label_CHG_OC_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_DSG_OC_First.Invoke(new EventHandler(delegate
                            {
                                label_DSG_OC_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Cellchg_OT_First.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_OT_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Celldsg_OT_First.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_OT_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));

                            label_Cellchg_UT_First.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_UT_First.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                            }));
                            label_Celldsg_UT_First.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_UT_First.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                            }));
                            label_Vdelta_Op_First.Invoke(new EventHandler(delegate
                            {
                                label_Vdelta_Op_First.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Res_First.Invoke(new EventHandler(delegate
                            {
                                label_Res_First.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Soc_Up_First.Invoke(new EventHandler(delegate
                            {
                                label_Soc_Up_First.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Tmos_OTP_First.Invoke(new EventHandler(delegate
                            {
                                label_Tmos_OTP_First.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));

                            ++i; ++i;
                            if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                            {
                                label_Fault_Second.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_Second.Text = Convert.ToDouble((float)(1)).ToString("0");
                                }));
                            }
                            else
                            {
                                label_Fault_Second.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_Second.Text = Convert.ToDouble((float)(0)).ToString("0");
                                }));
                            }
                            label_Vcell_OV_Second.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_OV_Second.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                            }));
                            label_Vcell_UV_Second.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_UV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Vbat_OV_Second.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_OV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Vbat_UV_Second.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_UV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_CHG_OC_Second.Invoke(new EventHandler(delegate
                            {
                                label_CHG_OC_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_DSG_OC_Second.Invoke(new EventHandler(delegate
                            {
                                label_DSG_OC_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Cellchg_OT_Second.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_OT_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Celldsg_OT_Second.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_OT_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));

                            label_Cellchg_UT_Second.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_UT_Second.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                            }));
                            label_Celldsg_UT_Second.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_UT_Second.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                            }));
                            label_Vdelta_Op_Second.Invoke(new EventHandler(delegate
                            {
                                label_Vdelta_Op_Second.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Res_Second.Invoke(new EventHandler(delegate
                            {
                                label_Res_Second.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Soc_Up_Second.Invoke(new EventHandler(delegate
                            {
                                label_Soc_Up_Second.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Tmos_OTP_Second.Invoke(new EventHandler(delegate
                            {
                                label_Tmos_OTP_Second.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));


                            ++i; ++i;
                            if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                            {
                                label_Fault_Third.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_Third.Text = Convert.ToDouble((float)(1)).ToString("0");
                                }));
                            }
                            else
                            {
                                label_Fault_Third.Invoke(new EventHandler(delegate
                                {
                                    label_Fault_Third.Text = Convert.ToDouble((float)(0)).ToString("0");
                                }));
                            }
                            label_Vcell_OV_Third.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_OV_Third.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                            }));
                            label_Vcell_UV_Third.Invoke(new EventHandler(delegate
                            {
                                label_Vcell_UV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Vbat_OV_Third.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_OV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Vbat_UV_Third.Invoke(new EventHandler(delegate
                            {
                                label_Vbat_UV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_CHG_OC_Third.Invoke(new EventHandler(delegate
                            {
                                label_CHG_OC_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_DSG_OC_Third.Invoke(new EventHandler(delegate
                            {
                                label_DSG_OC_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Cellchg_OT_Third.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_OT_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Celldsg_OT_Third.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_OT_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));

                            label_Cellchg_UT_Third.Invoke(new EventHandler(delegate
                            {
                                label_Cellchg_UT_Third.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                            }));
                            label_Celldsg_UT_Third.Invoke(new EventHandler(delegate
                            {
                                label_Celldsg_UT_Third.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                            }));
                            label_Vdelta_Op_Third.Invoke(new EventHandler(delegate
                            {
                                label_Vdelta_Op_Third.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Res_Third.Invoke(new EventHandler(delegate
                            {
                                label_Res_Third.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Soc_Up_Third.Invoke(new EventHandler(delegate
                            {
                                label_Soc_Up_Third.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Tmos_OTP_Third.Invoke(new EventHandler(delegate
                            {
                                label_Tmos_OTP_Third.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));


                            ++i; ++i;
                            Balanced_VC1.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC1.Text = Convert.ToDouble((float)((bydata[i + 1]) & 0x01)).ToString("0");
                            }));
                            Balanced_VC2.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC2.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            Balanced_VC3.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC3.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            Balanced_VC4.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC4.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            Balanced_VC5.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC5.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            Balanced_VC6.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC6.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            Balanced_VC7.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC7.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            Balanced_VC8.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC8.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));
                            Balanced_VC9.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC9.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                            }));
                            Balanced_VC10.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC10.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            Balanced_VC11.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC11.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            Balanced_VC12.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC12.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            Balanced_VC13.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC13.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            Balanced_VC14.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC14.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));
                            Balanced_VC15.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC15.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                            }));
                            Balanced_VC16.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC16.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                            }));

                            ++i; ++i;
                            Balanced_VC17.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC17.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            Balanced_VC18.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC18.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            Balanced_VC19.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC19.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            Balanced_VC20.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC20.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            Balanced_VC21.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC21.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            Balanced_VC22.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC22.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            Balanced_VC23.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC23.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            Balanced_VC24.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC24.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));
                            Balanced_VC25.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC25.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            Balanced_VC26.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC26.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            Balanced_VC27.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC27.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            Balanced_VC28.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC28.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            Balanced_VC29.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC29.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            Balanced_VC30.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC30.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));
                            Balanced_VC31.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC31.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                            }));
                            Balanced_VC32.Invoke(new EventHandler(delegate
                            {
                                Balanced_VC32.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                            }));
                            break;
                        }
                    case 0xD100:
                        {
                            i = 3;
                            // toolStripLabel_RTC_year.Text = Convert.ToString(bydata[i++]);
                            // toolStripLabel_RTC_month.Text = Convert.ToString(bydata[i++]);
                            // toolStripLabel_RTC_day.Text = Convert.ToString(bydata[i++]);
                            // toolStripLabel_RTC_hour.Text = Convert.ToString(bydata[i++]);
                            // toolStripLabel_RTC_minute.Text = Convert.ToString(bydata[i++]);
                            // toolStripLabel_RTC_second.Text = Convert.ToString(bydata[i++]);
                            // label_ver.Text = Convert.ToString(bydata[i++] | bydata[i++] << 8);
                            // label_chg_time.Text = Convert.ToString(bydata[i++] | bydata[i++] << 8);
                            // label_dsg_time.Text = Convert.ToString(bydata[i++] | bydata[i++] << 8);

                            label_ver.Text = Convert.ToString(bydata[i++] << 8 | bydata[i++] );
                            label_chg_time.Text = Convert.ToString(bydata[i++] <<8 | bydata[i++]);
                            label_dsg_time.Text = Convert.ToString(bydata[i++] <<8 | bydata[i++]);
                            // string s;
                            // // label_ver.Text = Convert.ToString(bydata[i++] << 8 | bydata[i++]);
                            // s = Convert.ToString(bydata[i++] << 8 | bydata[i++]);
                            // label_chg_time.Text = Convert.ToString(bydata[i++] <<8 | bydata[i++]);
                            // label_dsg_time.Text = Convert.ToString(bydata[i++] <<8 | bydata[i++]);

                            textBox_Fault_First1.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_First1.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[0] = bydata[i - 1];

                            textBox_Fault_First2.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_First2.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[1] = bydata[i - 1];

                            textBox_Fault_First3.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_First3.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[2] = bydata[i - 1];

                            textBox_Fault_First4.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_First4.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[3] = bydata[i - 1];


                            textBox_Fault_Second1.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Second1.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[4] = bydata[i - 1];

                            textBox_Fault_Second2.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Second2.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[5] = bydata[i - 1];

                            textBox_Fault_Second3.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Second3.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[6] = bydata[i - 1];

                            textBox_Fault_Second4.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Second4.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[7] = bydata[i - 1];


                            textBox_Fault_Third1.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Third1.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[8] = bydata[i - 1];

                            textBox_Fault_Third2.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Third2.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[9] = bydata[i - 1];

                            textBox_Fault_Third3.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Third3.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[10] = bydata[i - 1];

                            textBox_Fault_Third4.Invoke(new EventHandler(delegate
                            {
                                if (bydata[i] < 40)
                                {
                                    textBox_Fault_Third4.Text = FaultWarnName[bydata[i++]];
                                }
                            }));
                            Form2_FaultDataBuff[11] = bydata[i - 1];


                            label_Err_AFE1.Invoke(new EventHandler(delegate
                            {
                                label_Err_AFE1.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_AFE2.Invoke(new EventHandler(delegate
                            {
                                label_Err_AFE2.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Can.Invoke(new EventHandler(delegate
                            {
                                label_Err_Can.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_E2P_COM.Invoke(new EventHandler(delegate
                            {
                                label_Err_E2P_COM.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));

                            label_Err_SPI.Invoke(new EventHandler(delegate
                            {
                                label_Err_SPI.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Upper.Invoke(new EventHandler(delegate
                            {
                                label_Err_Upper.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Client1.Invoke(new EventHandler(delegate
                            {
                                label_Err_Client1.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Screen.Invoke(new EventHandler(delegate
                            {
                                label_Err_Screen.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));

                            label_Err_Wifi.Invoke(new EventHandler(delegate
                            {
                                label_Err_Wifi.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_BlueTooth.Invoke(new EventHandler(delegate
                            {
                                label_Err_BlueTooth.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_App.Invoke(new EventHandler(delegate
                            {
                                label_Err_App.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_CBC_CHG.Invoke(new EventHandler(delegate
                            {
                                label_Err_CBC_CHG.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));

                            label_Err_E2P_Store.Invoke(new EventHandler(delegate
                            {
                                label_Err_E2P_Store.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_HSE.Invoke(new EventHandler(delegate
                            {
                                label_Err_HSE.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_LSE.Invoke(new EventHandler(delegate
                            {
                                label_Err_LSE.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Flash.Invoke(new EventHandler(delegate
                            {
                                label_Err_Flash.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));

                            label_Err_Balanced.Invoke(new EventHandler(delegate
                            {
                                label_Err_Balanced.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_ADC.Invoke(new EventHandler(delegate
                            {
                                label_Err_ADC.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Res1.Invoke(new EventHandler(delegate
                            {
                                label_Err_Res1.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Res2.Invoke(new EventHandler(delegate
                            {
                                label_Err_Res2.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));

                            label_Err_CBC_DSG.Invoke(new EventHandler(delegate
                            {
                                label_Err_CBC_DSG.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Res4.Invoke(new EventHandler(delegate
                            {
                                label_Err_Res4.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Res5.Invoke(new EventHandler(delegate
                            {
                                label_Err_Res5.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            label_Err_Res6.Invoke(new EventHandler(delegate
                            {
                                label_Err_Res6.Text = Convert.ToDouble((float)bydata[i++]).ToString("0");
                            }));
                            break;
                        }
                    case 0xD115:
                        {
                            i = 3;
                            label_SysStatus_Heat.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Heat.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Cool.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Cool.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_AFE1.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_AFE1.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_AFE2.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_AFE2.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Balance.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Balance.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_ToSleep.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_ToSleep.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Res1.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res1.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Res2.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res2.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                            }));

                            label_SysStatus_BMS_StartUp.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_BMS_StartUp.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Pre_MOS.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Pre_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_CHG_MOS.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_CHG_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_DSG_MOS.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_DSG_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Pre_Relay.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Pre_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_CHG_Relay.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_CHG_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_DSG_Relay.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_DSG_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Main_Relay.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Main_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));

                            ++i; ++i;
                            label_SysStatus_SysLimits.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_SysLimits.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Res4.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res4.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Res5.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res5.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_SysStatus_Res6.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res6.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));

                            label_SysStatus_Res7.Invoke(new EventHandler(delegate
                            {
                                label_SysStatus_Res7.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");

                                if (label_SysStatus_Res7.Text == "1")
                                {
                                    groupBox_other.Visible = false;
                                    tabPage2_defaultPara.Parent = null;
                                    tabPage3_AfePara.Parent = tabControl_protectPara;

                                }
                                else
                                {
                                    tabPage2_defaultPara.Parent = tabControl_protectPara;
                                    tabPage3_AfePara.Parent = null;
                                    groupBox_other.Visible = true;
                                }
                            }));

                            ++i; ++i;
                            label_AFE2_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_AFE2_Func_Status.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Sleep_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_Sleep_Func_Status.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            label_SocZero_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_SocZero_Func_Status.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));

                            label_Balance_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_Balance_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            /*
                            label_BMS_Source_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_BMS_Source_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            */
                            label_MosRelay_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_MosRelay_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Relay_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_Relay_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_SocFixed_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_SocFixed_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Heated_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_Heated_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Cool_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_Cool_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_AFE1_Func_Status.Invoke(new EventHandler(delegate
                            {
                                label_AFE1_Func_Status.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));
                            ++i; ++i;
                            //无

                            ++i; ++i;
#if false
                            label_Switch9.Invoke(new EventHandler(delegate
                            {
                                label_Switch9.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Switch10.Invoke(new EventHandler(delegate
                            {
                                label_Switch10.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Switch11.Invoke(new EventHandler(delegate
                            {
                                label_Switch11.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Switch12.Invoke(new EventHandler(delegate
                            {
                                label_Switch12.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Switch13.Invoke(new EventHandler(delegate
                            {
                                label_Switch13.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Switch14.Invoke(new EventHandler(delegate
                            {
                                label_Switch14.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Switch15.Invoke(new EventHandler(delegate
                            {
                                label_Switch15.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Switch16.Invoke(new EventHandler(delegate
                            {
                                label_Switch16.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                            }));

                            label_Switch1.Invoke(new EventHandler(delegate
                            {
                                label_Switch1.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Switch2.Invoke(new EventHandler(delegate
                            {
                                label_Switch2.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Switch3.Invoke(new EventHandler(delegate
                            {
                                label_Switch3.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Switch4.Invoke(new EventHandler(delegate
                            {
                                label_Switch4.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Switch5.Invoke(new EventHandler(delegate
                            {
                                label_Switch5.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Switch6.Invoke(new EventHandler(delegate
                            {
                                label_Switch6.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Switch7.Invoke(new EventHandler(delegate
                            {
                                label_Switch7.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Switch8.Invoke(new EventHandler(delegate
                            {
                                label_Switch8.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));
                                                    
#endif

                            ++i; ++i;
#if false
                            label_Switch25.Invoke(new EventHandler(delegate
                            {
                                label_Switch25.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Switch26.Invoke(new EventHandler(delegate
                            {
                                label_Switch26.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Switch27.Invoke(new EventHandler(delegate
                            {
                                label_Switch27.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Switch28.Invoke(new EventHandler(delegate
                            {
                                label_Switch28.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Switch29.Invoke(new EventHandler(delegate
                            {
                                label_Switch29.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Switch30.Invoke(new EventHandler(delegate
                            {
                                label_Switch30.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Switch31.Invoke(new EventHandler(delegate
                            {
                                label_Switch31.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Switch32.Invoke(new EventHandler(delegate
                            {
                                label_Switch32.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                            }));

                            label_Switch17.Invoke(new EventHandler(delegate
                            {
                                label_Switch17.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Switch18.Invoke(new EventHandler(delegate
                            {
                                label_Switch18.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Switch19.Invoke(new EventHandler(delegate
                            {
                                label_Switch19.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Switch20.Invoke(new EventHandler(delegate
                            {
                                label_Switch20.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Switch21.Invoke(new EventHandler(delegate
                            {
                                label_Switch21.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Switch22.Invoke(new EventHandler(delegate
                            {
                                label_Switch22.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_Switch23.Invoke(new EventHandler(delegate
                            {
                                label_Switch23.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Switch24.Invoke(new EventHandler(delegate
                            {
                                label_Switch24.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));
#endif
                            //加热冷凝错误
                            ++i; ++i;
                            label_Heat_Res1.Invoke(new EventHandler(delegate
                            {
                                label_Heat_Res1.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Heat_Res2.Invoke(new EventHandler(delegate
                            {
                                label_Heat_Res2.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Heat_Res3.Invoke(new EventHandler(delegate
                            {
                                label_Heat_Res3.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Cool_Res1.Invoke(new EventHandler(delegate
                            {
                                label_Cool_Res1.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                            }));
                            label_Cool_Res2.Invoke(new EventHandler(delegate
                            {
                                label_Cool_Res2.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                            }));
                            label_Cool_Res3.Invoke(new EventHandler(delegate
                            {
                                label_Cool_Res3.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                            }));

                            label_Heat_OnOFF.Invoke(new EventHandler(delegate
                            {
                                label_Heat_OnOFF.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                            }));
                            label_Heat_Err.Invoke(new EventHandler(delegate
                            {
                                label_Heat_Err.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                            }));
                            label_Cool_OnOFF.Invoke(new EventHandler(delegate
                            {
                                label_Cool_OnOFF.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                            }));
                            label_Cool_Err.Invoke(new EventHandler(delegate
                            {
                                label_Cool_Err.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                            }));

                            label_CurLit.Invoke(new EventHandler(delegate
                            {
                                label_CurLit.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                            }));
                            label_CurLit_Err.Invoke(new EventHandler(delegate
                            {
                                label_CurLit_Err.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                            }));
                            label_StunGun.Invoke(new EventHandler(delegate
                            {
                                label_StunGun.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                            }));
                            label_Load.Invoke(new EventHandler(delegate
                            {
                                label_Load.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                            }));

                            //还有5个字
                            break;
                        }
                    case 0xD200:
                        {
                            //1个字
                            temp = bydata[i++] * 256 + bydata[i++];
                            this.Invoke(new EventHandler(delegate
                            {
                                if (temp == 0)
                                {
                                    label_SlaverNow.Text = "SlaverSingle";
                                }
                                else if (temp == 17)
                                {
                                    label_SlaverNow.Text = "MasterMode";
                                }
                                else
                                {
                                    label_SlaverNow.Text = "Slaver" + Convert.ToDouble((float)(temp)).ToString("0");

                                }
                            }));
                            break;
                        }
                    default:
                        break;
                }
                u16RdRunInfoRxCnt++;
                if (Lang.b_LangFlag == 0)
                {
                    toolStripLabel_RxCnt1.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                }
                else
                {
                    toolStripLabel_RxCnt1.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt2.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt3.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt4.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    toolStripLabel_RxCnt5.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                    //toolStripLabel_RxCnt6.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString(), "0xD000显示错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //0x10功能，写多个寄存器返回ACK处理
        private void RxWrRegsAck(byte[] bydata)
        {
            if (bydata[1] == 0x90)
            {
                switch (u16Rs485RegAddr)
                {
                    case 0xFFFD:
                        if (Lang.b_LangFlag == 0)
                            textBox_upgrate_window.Invoke(new EventHandler(delegate
                            {
                                textBox_upgrate_window.AppendText("设备连接失败！\n");
                            }));
                        else
                            textBox_upgrate_window.Invoke(new EventHandler(delegate
                            {
                                textBox_upgrate_window.AppendText("MCU connect failed！\n");
                            }));
                        break;

                    case 0xFFFE:
                        textBox_upgrate_window.Invoke(new EventHandler(delegate
                        {
                            textBox_upgrate_window.AppendText("升级过程出错...\n");
                        }));
                        CountCnt = 0;
                        length = 0;
                        break;

                    case 0xFFFF:
                        textBox_upgrate_window.Invoke(new EventHandler(delegate
                        {
                            textBox_upgrate_window.AppendText("升级过程出错...\n");
                        }));
                        CountCnt = 0;
                        length = 0;
                        break;
                    default:
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("参数写入有误！错误码： " + Convert.ToString(bydata[2]), "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else
                            MessageBox.Show("Parameters written errror！Error Code： " + Convert.ToString(bydata[2]), "Error！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
                return;
            }

            switch (u16Rs485RegAddr)
            {
                case 0x2200:
                case 0x2209:
                case 0x220D:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("保护参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Protective parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2218:
                case 0x2221:
                case 0x2225:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("预警参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Warning parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x222C:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("均衡参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Balancing parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2230:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("加热参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Heating parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2234:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("SOC保护参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("SOC protective parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2238:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("压差过大保护参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Delta over protective parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x223C:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("休眠保护参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Hibernate protective parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2304:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("SOC参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("SOC parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2300:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
                case 0x2340:
                    RS485_SLAVE_ADDR = FormDAM_Info.newDevAddr;
                    MessageBox.Show("设备地址修改成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                #region 在线升级参数回复
                case 0xFFFD:
                    {
                        RS485_SLAVE_ADDR = 0x01;
                        if (Lang.b_LangFlag == 0)
                            textBox_upgrate_window.Invoke(new EventHandler(delegate
                            {
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("5......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("4......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("3......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("2......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("1......\n");
                                // Delay_ms(1000);
                                textBox_upgrate_window.AppendText("设备连接成功！\n");
                                button_upgrate_begin.Enabled = true;
                            }));
                        else
                            textBox_upgrate_window.Invoke(new EventHandler(delegate
                            {
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("5......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("4......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("3......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("2......\n");
                                // Delay_ms(1000);
                                // textBox_upgrate_window.AppendText("1......\n");
                                // Delay_ms(1000);
                                textBox_upgrate_window.AppendText("MCU connect OK！\n");
                                button_upgrate_begin.Enabled = true;
                            }));
                        break;
                    }
                case 0xFFFE:
                    {
                        if (progressBar_upgrate.Value == 100)
                        {
                            FlashUpgrateComplete();
                        }
                        else
                        {
                            if (CountCnt == 0)
                            {
                                textBox_upgrate_window.Invoke(new EventHandler(delegate
                                {
                                    textBox_upgrate_window.AppendText("正在升级程序...\n");
                                    button_upgrate_begin.Enabled = false;
                                    button_upgrate_connect.Enabled = false;
                                }));
                            }
                            ++CountCnt;
                            FlashUpgrate();
                        }
                        break;
                    }
                case 0xFFFF:
                    {
                        textBox_upgrate_window.Invoke(new EventHandler(delegate
                        {
                            textBox_upgrate_window.AppendText("程序升级完成\n");
                            button_upgrate_begin.Enabled = true;
                            button_upgrate_connect.Enabled = true;
                        }));
                        CountCnt = 0;
                        length = 0;
                        MessageBox.Show("升级成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                #endregion 在线升级参数回复
                default:
                    if (by_BatchProcessFlag == 0)
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("参数写入成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Parameters written successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else by_BatchProcessFlag = 0;
                    break;
            }
        }
        //0x06功能，写一个寄存器
        private void RxWrRegAck(byte[] bydata)
        {
            if (bydata[1] == 0x86)
            {
                if (Lang.b_LangFlag == 0)
                    MessageBox.Show("写入有误！错误码： " + Convert.ToString(bydata[2]), "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("Written Error！Error Code： " + Convert.ToString(bydata[2]), "Error！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                timer1.Enabled = true;
                return;
            }

            switch (u16Rs485RegAddr)
            {
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_CALIB_COEF:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("校准参数复位成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Calibrating parameters reset successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_PROTECT_RECORD:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("当前保护记录清零成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Protect records cleared successfully！", "SuccessMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_PROTECT_ELEMENT://复位校正参数 
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("保护参数复位成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Protective parameters reset successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_OTHER_CANADD://复位校正参数 
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("其它参数复位成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Other parameters reset successfully！", "SuccessfullyMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SWITCH_ON:
                    {
                        //MessageBox.Show("该开关打开成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //timer1.Enabled = true;        //无效，打不开，所以还是 不弹窗口了
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SWITCH_OFF:
                    {
                        //MessageBox.Show("该开关关闭成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYSTEM_FUNCTION_ON:
                    {
                        //MessageBox.Show("该功能打开成功！", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYSTEM_FUNCTION_OFF:
                    {
                        //MessageBox.Show("该功能关闭成功!", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_HEAT_COOL:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("加热冷凝点复位成功!", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("HeatCool parameters reset successfully！", "SuccessMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SET_ONCE_SOC:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("SOC设置成功!", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("SOC set successfully！", "SuccessMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_AFE_PARAMETERS:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("AFE参数复位成功!", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("AFE Parameters Reset successfully！", "SuccessMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_EVENT_RECORD:
                    {
                        if (Lang.b_LangFlag == 0)
                            MessageBox.Show("事件记录清空完毕!", "提示信息！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Event Record Reset successfully！", "SuccessMessage！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                default:
                    {
                        if (Lang.b_LangFlag == 0)
                        {
                            MessageBox.Show("操作错误！", "错误信息！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("Mission Failed！", "ErrorMessage！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    }
            }
        }
        //0x03功能，读一次的操作
        private void RxRdRegAck(byte[] bydata)
        {
            float f32B;
            UInt16 i;

            if (bydata[1] == 0x83)
            {
                if (Lang.b_LangFlag == 0)
                {
                    MessageBox.Show("参数读取有误！错误码： " + Convert.ToString(bydata[2]), "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Parameters Reading Error！Error Code： " + Convert.ToString(bydata[2]), "Error！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            switch (u16Rs485RegAddr)
            {
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VC1CALIB_K:
                    {
                        i = 3;
                        textBox_cail_cell1_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell1_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        //不这样写，负数会溢出
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell1_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell1_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell2_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell2_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell2_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell2_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell3_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell3_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell3_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell3_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell4_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell4_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell4_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell4_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell5_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell5_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell5_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell5_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell6_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell6_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell6_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell6_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell7_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell7_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell7_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell7_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell8_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell8_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell8_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell8_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell9_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell9_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell9_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell9_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell10_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell10_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell10_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell10_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell11_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell11_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell11_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell11_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell12_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell12_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell12_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell12_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell13_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell13_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell13_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell13_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell14_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell14_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell14_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell14_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell15_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell15_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell15_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell15_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell16_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell16_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell16_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell16_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));


                        textBox_cail_cell17_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell17_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell17_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell17_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell18_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell18_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell18_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell18_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell19_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell19_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell19_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell19_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell20_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell20_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell20_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell20_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell21_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell21_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell21_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell21_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell22_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell22_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell22_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell22_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell23_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell23_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell23_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell23_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell24_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell24_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell24_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell24_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell25_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell25_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell25_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell25_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell26_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell26_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell26_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell26_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell27_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell27_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell27_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell27_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell28_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell28_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell28_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell28_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell29_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell29_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell29_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell29_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell30_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell30_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell30_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell30_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell31_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell31_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell31_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell31_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        textBox_cail_cell32_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell32_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_cell32_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_cell32_b.Text = Convert.ToDouble(f32B).ToString("0");
                        }));

                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_AFE1CALIB_K:
                    {
                        i = 3;
                        textBox_cail_AFE1_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_AFE1_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        textBox_cail_AFE1_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_AFE1_b.Text = Convert.ToDouble((float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])))).ToString("0");
                        }));
                        textBox_cail_AFE2_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_AFE2_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        textBox_cail_AFE2_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_AFE2_b.Text = Convert.ToDouble((float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])))).ToString("0");
                        }));
                        textBox_cail_Vbus_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Vbus_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        textBox_cail_Vbus_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Vbus_b.Text = Convert.ToDouble((float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])))).ToString("0");
                        }));
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_ICHGCALIB_K:
                    {
                        i = 3;
                        textBox_cail_Ichg_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Ichg_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        //不这样写，负数会溢出
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_Ichg_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Ichg_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_cail_Idsg_k.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Idsg_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_cail_Idsg_b.Invoke(new EventHandler(delegate
                        {
                            textBox_cail_Idsg_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_TEMP1_CALIB_K:
                    {
                        i = 3;
                        textBox_temp1_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp1_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        //不这样写，负数会溢出
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp1_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp1_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp2_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp2_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp2_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp2_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp3_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp3_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp3_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp3_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp4_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp4_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp4_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp4_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp5_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp5_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp5_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp5_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp6_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp6_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp6_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp6_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_tempEnv1_k.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv1_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_tempEnv1_b.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv1_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_tempEnv2_k.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv2_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_tempEnv2_b.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv2_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_tempEnv3_k.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv3_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_tempEnv3_b.Invoke(new EventHandler(delegate
                        {
                            textBox_tempEnv3_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));

                        textBox_temp_mos_k.Invoke(new EventHandler(delegate
                        {
                            textBox_temp_mos_k.Text = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++]) / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        f32B = (float)(Convert.ToDouble((Int16)((bydata[i++] << 8) + bydata[i++])));
                        textBox_temp_mos_b.Invoke(new EventHandler(delegate
                        {
                            textBox_temp_mos_b.Text = Convert.ToDouble(f32B / Math.Pow(2, 10)).ToString("0.000");
                        }));
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VCELL_OVP_FIRST:
                    {
                        i = 3;
                        textBox_VcellOVP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellOVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellOVP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellOVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellOVP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellOVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellOVP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellOVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellOVP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellOVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_VcellUVP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellUVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellUVP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellUVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellUVP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellUVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellUVP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellUVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VcellUVP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_VcellUVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_VbusOVP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusOVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusOVP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusOVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusOVP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusOVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusOVP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusOVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusOVP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusOVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_VbusUVP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusUVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusUVP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusUVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusUVP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusUVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusUVP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusUVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        textBox_VbusUVP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_VbusUVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_IchgOCP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_IchgOCP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IchgOCP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_IchgOCP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IchgOCP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_IchgOCP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IchgOCP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_IchgOCP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IchgOCP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_IchgOCP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_IdsgOCP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_IdsgOCP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IdsgOCP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_IdsgOCP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IdsgOCP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_IdsgOCP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IdsgOCP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_IdsgOCP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));
                        textBox_IdsgOCP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_IdsgOCP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_TchgOTP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgOTP_First.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgOTP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgOTP_Second.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgOTP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgOTP_Third.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgOTP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgOTP_Rec.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgOTP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgOTP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_TchgUTP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgUTP_First.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgUTP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgUTP_Second.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgUTP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgUTP_Third.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgUTP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgUTP_Rec.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TchgUTP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_TchgUTP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_TdsgOTP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgOTP_First.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgOTP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgOTP_Second.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgOTP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgOTP_Third.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgOTP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgOTP_Rec.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgOTP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgOTP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_TdsgUTP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgUTP_First.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgUTP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgUTP_Second.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgUTP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgUTP_Third.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgUTP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgUTP_Rec.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TdsgUTP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_TdsgUTP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_TmosOTP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_TmosOTP_First.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TmosOTP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_TmosOTP_Second.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TmosOTP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_TmosOTP_Third.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TmosOTP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_TmosOTP_Rec.Text = Convert.ToDouble(((float)(bydata[i++] * 256 + bydata[i++]) - 400) / 10).ToString("0.0");
                        }));
                        textBox_TmosOTP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_TmosOTP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_VdeltaOVP_First.Invoke(new EventHandler(delegate
                        {
                            textBox_VdeltaOVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VdeltaOVP_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_VdeltaOVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VdeltaOVP_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_VdeltaOVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VdeltaOVP_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_VdeltaOVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_VdeltaOVP_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_VdeltaOVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_SocUp_First.Invoke(new EventHandler(delegate
                        {
                            textBox_SocUp_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocUp_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_SocUp_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocUp_Third.Invoke(new EventHandler(delegate
                        {
                            textBox_SocUp_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocUp_Rec.Invoke(new EventHandler(delegate
                        {
                            textBox_SocUp_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocUp_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_SocUp_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_VOLTAGE1:
                    {
                        i = 3;
                        textBox_SocTable_Volt1.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value1.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt2.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value2.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt3.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value3.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt4.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt4.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value4.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value4.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt1.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt5.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value5.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value5.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt6.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt6.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value6.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value6.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt7.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt7.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value7.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value7.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_SocTable_Volt8.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt8.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value8.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value8.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt9.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt9.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value9.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value9.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt10.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt10.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value10.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value10.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt11.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt11.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value11.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value11.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt12.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt12.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value12.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value12.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt13.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt13.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value13.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value13.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_SocTable_Volt14.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt14.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value14.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value14.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt15.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt15.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value15.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value15.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt16.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt16.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value16.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value16.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt17.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt17.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value17.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value17.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt18.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt18.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value18.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value18.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt19.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt19.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value19.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value19.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt20.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt20.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value20.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value20.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Volt21.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Volt21.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_SocTable_Value21.Invoke(new EventHandler(delegate
                        {
                            textBox_SocTable_Value21.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_COPPERLOSS1:
                    {
                        i = 3;
                        textBox_CopperLoss1.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss2.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss3.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss4.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss4.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss5.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss5.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss6.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss6.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss7.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss7.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss8.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss8.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss9.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss9.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss10.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss10.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss11.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss11.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss12.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss12.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss13.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss13.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss14.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss14.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss15.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss15.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CopperLoss16.Invoke(new EventHandler(delegate
                        {
                            textBox_CopperLoss16.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));


                        textBox_CellNum1.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum2.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum3.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum4.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum4.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum5.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum5.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum6.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum6.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum7.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum7.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum8.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum8.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum9.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum9.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum10.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum10.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum11.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum11.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum12.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum12.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum13.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum13.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum14.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum14.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum15.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum15.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_CellNum16.Invoke(new EventHandler(delegate
                        {
                            textBox_CellNum16.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RTC_TIME_YEAR:
                    {
                        i = 3;
                        textBox_RTC_Time_Year.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Year.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Time_Month.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Month.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Time_Day.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Day.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Time_Hour.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Hour.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Time_Minute.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Minute.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Time_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Time_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));

                        textBox_RTC_Alarm_Year.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Year.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Alarm_Month.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Month.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Alarm_Day.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Day.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Alarm_Hour.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Hour.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Alarm_Minute.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Minute.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        textBox_RTC_Alarm_Second.Invoke(new EventHandler(delegate
                        {
                            textBox_RTC_Alarm_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString();
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_BALANCE_OV:
                    {
                        textBox_openV.Invoke(new EventHandler(delegate
                        {
                            textBox_openV.Text = Convert.ToDouble((float)(bydata[3] * 256 + bydata[4])).ToString();
                        }));

                        textBox_openW.Invoke(new EventHandler(delegate
                        {
                            textBox_openW.Text = Convert.ToDouble((float)(bydata[5] * 256 + bydata[6])).ToString();
                        }));

                        textBox_CloseWin.Invoke(new EventHandler(delegate
                        {
                            textBox_CloseWin.Text = Convert.ToDouble((float)(bydata[7] * 256 + bydata[8])).ToString();
                        }));

                        textBox_Balance_Res1.Invoke(new EventHandler(delegate
                        {
                            textBox_Balance_Res1.Text = Convert.ToDouble((float)(bydata[9] * 256 + bydata[10])).ToString();
                        }));

                        textBox_Balance_Res2.Invoke(new EventHandler(delegate
                        {
                            textBox_Balance_Res2.Text = Convert.ToDouble((float)(bydata[11] * 256 + bydata[12])).ToString();
                        }));

                        textBox_Balance_Res3.Invoke(new EventHandler(delegate
                        {
                            textBox_Balance_Res3.Text = Convert.ToDouble((float)(bydata[13] * 256 + bydata[14])).ToString();
                        }));

                        textBox_Balance_Res4.Invoke(new EventHandler(delegate
                        {
                            textBox_Balance_Res4.Text = Convert.ToDouble((float)(bydata[15] * 256 + bydata[16])).ToString();
                        }));

                        textBox_Balance_Res5.Invoke(new EventHandler(delegate
                        {
                            textBox_Balance_Res5.Text = Convert.ToDouble((float)(bydata[17] * 256 + bydata[18])).ToString();
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_CS_CUR_CHGMAX:
                    {
                        textBox_CS_CurCHG.Invoke(new EventHandler(delegate
                        {
                            textBox_CS_CurCHG.Text = Convert.ToDouble((float)(bydata[3] * 256 + bydata[4]) / 10).ToString("0");
                        }));
                        textBox_CS_CurDSG.Invoke(new EventHandler(delegate
                        {
                            textBox_CS_CurDSG.Text = Convert.ToDouble((float)(bydata[5] * 256 + bydata[6]) / 10).ToString("0");
                        }));
                        textBox_CBC_DelayT.Invoke(new EventHandler(delegate
                        {
                            textBox_CBC_DelayT.Text = Convert.ToDouble((float)(bydata[7] * 256 + bydata[8]) / 10).ToString("0");
                        }));
                        textBox_CBC_CurDSG.Invoke(new EventHandler(delegate
                        {
                            textBox_CBC_CurDSG.Text = Convert.ToDouble((float)(bydata[9] * 256 + bydata[10]) / 10).ToString("0");
                        }));

                        textBox_Soc_TableSelect.Invoke(new EventHandler(delegate
                        {
                            textBox_Soc_TableSelect.Text = Convert.ToDouble((float)(bydata[11] * 256 + bydata[12])).ToString("0");
                        }));
                        textBox_Password_Forever.Invoke(new EventHandler(delegate
                        {
                            textBox_Password_Forever.Text = Convert.ToDouble((float)(bydata[13] * 256 + bydata[14])).ToString("0");
                        }));
                        textBox_CurLimit_Vdel.Invoke(new EventHandler(delegate
                        {
                            textBox_CurLimit_Vdel.Text = Convert.ToDouble((float)(bydata[15] * 256 + bydata[16])).ToString("0");
                        }));
                        textBox_CurLimit_Cur.Invoke(new EventHandler(delegate
                        {
                            textBox_CurLimit_Cur.Text = Convert.ToDouble((float)(bydata[17] * 256 + bydata[18]) / 10).ToString("0.0");
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SLEEP_V_NORMAL:
                    {
                        textBox_SleepNormalV.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepNormalV.Text = Convert.ToDouble((float)(bydata[3] * 256 + bydata[4])).ToString("0");
                        }));
                        textBox_SleepNormalT.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepNormalT.Text = Convert.ToDouble((float)(bydata[5] * 256 + bydata[6])).ToString("0");
                        }));
                        textBox_SleepOverDsgV.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepOverDsgV.Text = Convert.ToDouble((float)(bydata[7] * 256 + bydata[8])).ToString("0");
                        }));
                        textBox_SleepOverDsgT.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepOverDsgT.Text = Convert.ToDouble((float)(bydata[9] * 256 + bydata[10])).ToString("0");
                        }));

                        textBox_SleepVirCur_Chg.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepVirCur_Chg.Text = Convert.ToDouble((float)(bydata[11] * 256 + bydata[12]) / 10).ToString("0.0");
                        }));
                        textBox_SleepVirCur_Dsg.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepVirCur_Dsg.Text = Convert.ToDouble((float)(bydata[13] * 256 + bydata[14]) / 10).ToString("0.0");
                        }));
                        textBox_SleepRTC_WakeUpT.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepRTC_WakeUpT.Text = Convert.ToDouble((float)(bydata[15] * 256 + bydata[16])).ToString("0");
                        }));
                        textBox_SleepRes.Invoke(new EventHandler(delegate
                        {
                            textBox_SleepRes.Text = Convert.ToDouble((float)(bydata[17] * 256 + bydata[18])).ToString("0");
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SOC_AH:
                    {
                        textBox_Soc_Ah.Invoke(new EventHandler(delegate
                        {
                            textBox_Soc_Ah.Text = Convert.ToDouble((float)(bydata[3] * 256 + bydata[4]) / 10).ToString("0.0");
                        }));
                        textBox_Soc_CycleTime.Invoke(new EventHandler(delegate
                        {
                            textBox_Soc_CycleTime.Text = Convert.ToDouble((float)(bydata[5] * 256 + bydata[6])).ToString("0");
                        }));
                        textBox_Soc_V_100.Invoke(new EventHandler(delegate
                        {
                            textBox_Soc_V_100.Text = Convert.ToDouble((float)(bydata[7] * 256 + bydata[8])).ToString("0");
                        }));
                        textBox_Soc_V_0.Invoke(new EventHandler(delegate
                        {
                            textBox_Soc_V_0.Text = Convert.ToDouble((float)(bydata[9] * 256 + bydata[10])).ToString("0");
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_SYS_SERIES_NUM:
                    {
                        textBox_Sys_SeriesNum.Invoke(new EventHandler(delegate
                        {
                            textBox_Sys_SeriesNum.Text = Convert.ToDouble((float)(bydata[3] * 256 + bydata[4])).ToString("0");
                        }));
                        textBox_Sys_CSRes.Invoke(new EventHandler(delegate
                        {
                            AFE_Parameters_RS485_Struction.Sys_CSRes = (UInt16)(bydata[5] * 256 + bydata[6]);
                            textBox_Sys_CSRes.Text = Convert.ToDouble((float)(bydata[5] * 256 + bydata[6])).ToString("0");
                        }));
                        textBox_Sys_CSRes_Num.Invoke(new EventHandler(delegate
                        {
                            AFE_Parameters_RS485_Struction.Sys_CSRes_Num = (UInt16)(bydata[7] * 256 + bydata[8]);
                            textBox_Sys_CSRes_Num.Text = Convert.ToDouble((float)(bydata[7] * 256 + bydata[8])).ToString("0");
                        }));
                        textBox_Sys_PreChg_Time.Invoke(new EventHandler(delegate
                        {
                            textBox_Sys_PreChg_Time.Text = Convert.ToDouble((float)(bydata[9] * 256 + bydata[10])).ToString("0");
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_HEAT_DSG_HIGH:
                    {
                        i = 3;
                        textBox_Heat_OpenT.Invoke(new EventHandler(delegate
                        {
                            textBox_Heat_OpenT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));
                        textBox_Heat_CloseT.Invoke(new EventHandler(delegate
                        {
                            textBox_Heat_CloseT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));
                        textBox_Heat_OpenCur.Invoke(new EventHandler(delegate
                        {
                            textBox_Heat_OpenCur.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0");
                        }));

                        textBox_Cool_OpenT.Invoke(new EventHandler(delegate
                        {
                            textBox_Cool_OpenT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));
                        textBox_Cool_CloseT.Invoke(new EventHandler(delegate
                        {
                            textBox_Cool_CloseT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));
                        textBox_Res1.Invoke(new EventHandler(delegate
                        {
                            textBox_Res1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        textBox_Res2.Invoke(new EventHandler(delegate
                        {
                            textBox_Res2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res3.Invoke(new EventHandler(delegate
                        {
                            textBox_Res3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res4.Invoke(new EventHandler(delegate
                        {
                            textBox_Res4.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        textBox_Res5.Invoke(new EventHandler(delegate
                        {
                            textBox_Res5.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res6.Invoke(new EventHandler(delegate
                        {
                            textBox_Res6.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res7.Invoke(new EventHandler(delegate
                        {
                            textBox_Res7.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res8.Invoke(new EventHandler(delegate
                        {
                            textBox_Res8.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));


                        textBox_Res9.Invoke(new EventHandler(delegate
                        {
                            textBox_Res9.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res10.Invoke(new EventHandler(delegate
                        {
                            textBox_Res10.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res11.Invoke(new EventHandler(delegate
                        {
                            textBox_Res11.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        textBox_Res12.Invoke(new EventHandler(delegate
                        {
                            textBox_Res12.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res13.Invoke(new EventHandler(delegate
                        {
                            textBox_Res13.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res14.Invoke(new EventHandler(delegate
                        {
                            textBox_Res14.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res15.Invoke(new EventHandler(delegate
                        {
                            textBox_Res15.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        textBox_Res16.Invoke(new EventHandler(delegate
                        {
                            textBox_Res16.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res17.Invoke(new EventHandler(delegate
                        {
                            textBox_Res17.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res18.Invoke(new EventHandler(delegate
                        {
                            textBox_Res18.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        textBox_Res19.Invoke(new EventHandler(delegate
                        {
                            textBox_Res19.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_VPACK_OVP_FIRST:
                    {
                        i = 3;
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackOVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackOVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackOVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackOVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackOVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackUVP_First.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackUVP_Second.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackUVP_Third.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackUVP_Rec.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_VpackUVP_DelayT.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        break;
                    }
                case (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_PARALLEL_SERIAL_NUM:
                    {
                        i = 3;
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_Parallel_SerialNum.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_Parallel_PackNum.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_Parallel_Res1.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_Parallel_Res2.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            textBox_Parallel_Res3.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));
                        break;
                    }
                case 0xC001:
                    {
                        i = 3;

                        textBox_Fault_Record1.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record1.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record1.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record1.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record2.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record2.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record2.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record2.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record3.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record3.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record3.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record3.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record4.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record4.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record4.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record4.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record5.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record5.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record5.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record5.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record6.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record6.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record6.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record6.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record7.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record7.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record7.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record7.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record8.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record8.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record8.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record8.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record9.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record9.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record9.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record9.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }

                        textBox_Fault_Record10.Invoke(new EventHandler(delegate
                        {
                            textBox_Fault_Record10.Text = FaultWarnName[(bydata[i++] << 8) + bydata[i++]];
                        }));
                        Year = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Month = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Day = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Hour = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Minute = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        Second = Convert.ToDouble((float)((bydata[i++] << 8) + bydata[i++])).ToString("0");
                        if (Month == "0" && Day == "0")
                        {
                            label_Time_Fault_Record10.Text = "NA";
                        }
                        else
                        {
                            label_Time_Fault_Record10.Text = Year + "/" + Month + "/" + Day + "/" + Hour + "/" + Minute + "/" + Second;
                        }
                        by_BatchProcessFlag = 0;
                        break;
                    }
                case 0xC002:
                    {
                        string str = null;
                        byte[] data = new byte[32];

                        for (i = 0; i < 32; i++)
                        {
                            data[i] = bydata[i + 3];
                        }
                        str = Encoding.GetEncoding("GBK").GetString(data);
                        this.Invoke(new Action(() => { textBox_BMS_SerialNum.Text = str; }));

                        for (i = 0; i < 32; i++)
                        {
                            data[i] = bydata[i + 3 + 32];
                        }
                        str = Encoding.GetEncoding("GBK").GetString(data);
                        this.Invoke(new Action(() => { textBox_BMS_HardWareVer.Text = str; }));

                        for (i = 0; i < 32; i++)
                        {
                            data[i] = bydata[i + 3 + 32 + 32];
                        }
                        str = Encoding.GetEncoding("GBK").GetString(data);
                        this.Invoke(new Action(() => { textBox_BMS_SoftWareVer.Text = str; }));
                        break;
                    }
                case 0xC003:
                    {
                        i = 3;
                        int BMS_Num = bydata[3] * 256 + bydata[4];
                        //int BMS_NumTemp = 0;
                        Control Controler;
                        label_BMS_Num.Invoke(new EventHandler(delegate
                        {
                            label_BMS_Num.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));


                        for (int j = 0; j < 16; ++j)
                        {
                            Controler = this.Controls.Find("label_VbatBMS" + (j + 1).ToString(), true)[0];
                            if (j < BMS_Num)
                            {
                                this.Invoke(new EventHandler(delegate
                                {
                                    Controler.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                                }));
                            }
                            else
                            {
                                this.Invoke(new EventHandler(delegate
                                {
                                    Controler.Text = "NA";
                                    i++; i++;
                                }));
                            }
                        }

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VbatSum.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VpackSumDelta.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_IchgSum.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_IdsgSum.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10).ToString("0.0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VpackMax.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VpackMin.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 100).ToString("0.00");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VpackMax_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VpackMin_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellMax.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellMin.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellDeltaAll.Text =
                            (Convert.ToDouble(label_Parallel_VcellMax.Text) - Convert.ToDouble(label_Parallel_VcellMin.Text)).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            //label_Parallel_VcellMax_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            label_Parallel_VcellMax_Pos.Text = Convert.ToDouble((float)bydata[i++]).ToString("0") + "__";
                            label_Parallel_VcellMax_Pos.Text += Convert.ToDouble((float)bydata[i++]).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            //label_Parallel_VcellMin_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                            label_Parallel_VcellMin_Pos.Text = Convert.ToDouble((float)bydata[i++]).ToString("0") + "__";
                            label_Parallel_VcellMin_Pos.Text += Convert.ToDouble((float)bydata[i++]).ToString("0");
                        }));


                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellDeltaMax.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellDeltaMin.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellDeltaMax_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_VcellDeltaMin_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMax.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMin.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMax_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMin_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMosMax.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMosMin.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++]) / 10 - 40).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMosMax_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_TempMosMin_Pos.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_SOC.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_SOH.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Present_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Full_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Factory_mAh.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CycleTimes.Text = Convert.ToDouble((float)(bydata[i++] * 256 + bydata[i++])).ToString("0");
                        }));


                        if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_First.Text = Convert.ToDouble((float)(1)).ToString("0");
                            }));
                        }
                        else
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_First.Text = Convert.ToDouble((float)(0)).ToString("0");
                            }));
                        }
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_OV_First.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_UV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_OV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_UV_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CHG_OC_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_DSG_OC_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_OT_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_OT_First.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_UT_First.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_UT_First.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vdelta_Op_First.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Res_First.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Soc_Up_First.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Tmos_OTP_First.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_OV_First.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_UV_First.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                        }));


                        ++i; ++i;
                        if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_Second.Text = Convert.ToDouble((float)(1)).ToString("0");
                            }));
                        }
                        else
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_Second.Text = Convert.ToDouble((float)(0)).ToString("0");
                            }));
                        }
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_OV_Second.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_UV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_OV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_UV_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CHG_OC_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_DSG_OC_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_OT_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_OT_Second.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_UT_Second.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_UT_Second.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vdelta_Op_Second.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Res_Second.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Soc_Up_Second.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Tmos_OTP_Second.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_OV_Second.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_UV_Second.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                        }));

                        ++i; ++i;
                        if ((bydata[i] * 256 + bydata[i + 1]) != 0)
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_Third.Text = Convert.ToDouble((float)(1)).ToString("0");
                            }));
                        }
                        else
                        {
                            this.Invoke(new EventHandler(delegate
                            {
                                label_Parallel_Fault_Third.Text = Convert.ToDouble((float)(0)).ToString("0");
                            }));
                        }
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_OV_Third.Text = Convert.ToDouble((float)(bydata[i + 1] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vcell_UV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_OV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vbat_UV_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CHG_OC_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_DSG_OC_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_OT_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_OT_Third.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                        }));

                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cellchg_UT_Third.Text = Convert.ToDouble((float)(bydata[i] & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Celldsg_UT_Third.Text = Convert.ToDouble((float)(bydata[i] >> 1 & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vdelta_Op_Third.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Res_Third.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Soc_Up_Third.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Tmos_OTP_Third.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_OV_Third.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                        }));
                        this.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Vpack_UV_Third.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                        }));

                        ++i; ++i;
                        int ErrBMS_Num = 0;
                        int ErrBMS_Cnt = 0;

                        for (int j = 0; j < 16; ++j)
                        {
                            Controler = this.Controls.Find("label_ComErrBMS" + (j + 1).ToString(), true)[0];
                            ErrBMS_Cnt = (bydata[i++] * 256 + bydata[i++]);
                            if (ErrBMS_Cnt != 0)
                            {
                                ++ErrBMS_Num;
                            }
                            this.Invoke(new EventHandler(delegate
                            {
                                Controler.Text = Convert.ToDouble((float)ErrBMS_Cnt).ToString("0");
                            }));
                        }

                        label_Parallel_Heat.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Heat.Text = Convert.ToDouble((float)((bydata[i] >> 0) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Cool.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Cool.Text = Convert.ToDouble((float)((bydata[i] >> 1) & 0x01)).ToString("0");
                        }));
                        label_Parallel_AFE1.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_AFE1.Text = Convert.ToDouble((float)((bydata[i] >> 2) & 0x01)).ToString("0");
                        }));
                        label_Parallel_AFE2.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_AFE2.Text = Convert.ToDouble((float)((bydata[i] >> 3) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Balance.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Balance.Text = Convert.ToDouble((float)((bydata[i] >> 4) & 0x01)).ToString("0");
                        }));
                        label_Parallel_ToSleep.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_ToSleep.Text = Convert.ToDouble((float)((bydata[i] >> 5) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Res1.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Res1.Text = Convert.ToDouble((float)((bydata[i] >> 6) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Res2.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Res2.Text = Convert.ToDouble((float)((bydata[i] >> 7) & 0x01)).ToString("0");
                        }));

                        label_Parallel_BMS_StartUp.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_BMS_StartUp.Text = Convert.ToDouble((float)((bydata[i + 1] >> 0) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Pre_MOS.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Pre_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 1) & 0x01)).ToString("0");
                        }));
                        label_Parallel_CHG_MOS.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CHG_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 2) & 0x01)).ToString("0");
                        }));
                        label_Parallel_DSG_MOS.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_DSG_MOS.Text = Convert.ToDouble((float)((bydata[i + 1] >> 3) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Pre_Relay.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Pre_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 4) & 0x01)).ToString("0");
                        }));
                        label_Parallel_CHG_Relay.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_CHG_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 5) & 0x01)).ToString("0");
                        }));
                        label_Parallel_DSG_Relay.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_DSG_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 6) & 0x01)).ToString("0");
                        }));
                        label_Parallel_Main_Relay.Invoke(new EventHandler(delegate
                        {
                            label_Parallel_Main_Relay.Text = Convert.ToDouble((float)((bydata[i + 1] >> 7) & 0x01)).ToString("0");
                        }));

                        ++i; ++i;
                        break;
                    }

                case 0xC004:
                    {
                        {
                            string str = null;
                            byte[] data = new byte[11];

                            for (i = 0; i < 11; i++)
                            {
                                data[i] = bydata[i + 3];
                            }
                            str = Encoding.GetEncoding("GBK").GetString(data);
                            this.Invoke(new Action(() => { textBox_BatNum.Text = str; }));


                            break;
                        }
                    }
                case 0xC005:
                case 0xC006:
                case 0xC007:
                    {
                        int j;
                        i = 3;
                        for (j = 0; j < 64; j++)
                        {
                            ParallelVCell[j + (u16Rs485RegAddr - 0xC004) * 64] = (bydata[i++] << 8) + bydata[i++];
                        }
                        break;
                    }
                case 0xC008:
                    {
                        int j;
                        i = 3;
                        for (j = 0; j < 100; j++)
                        {
                            EventRecord[j, 0] = bydata[i++];
                            EventRecord[j, 1] = bydata[i++];
                        }
                        EVENT_RECORD_OK = true;
                        break;
                    }
                //用于处理切换页面的时候，D000的数据因为计时器关闭到这里而出现错误
                case 0xD000:
                case 0xD026:
                case 0xD100:
                case 0xD115:
                case 0xD200:
                    break;
                case 0x2400:
                    RxRdRegAck_AFE_Parameter(bydata);
                    break;
                default:
                    {
                        if (Lang.b_LangFlag == 0)
                        {
                            MessageBox.Show("操作错误！", "错误信息！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("Mission Failed！", "ErrorMessage！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    }
            }

            u16RdRunInfoRxCnt++;
            if (Lang.b_LangFlag == 0)
            {
                toolStripLabel_RxCnt1.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt2.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt3.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt4.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt5.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                //toolStripLabel_RxCnt6.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt7.Text = "接收次数：" + Convert.ToString(u16RdRunInfoRxCnt);
            }
            else
            {
                toolStripLabel_RxCnt1.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt2.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt3.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt4.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt5.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                //toolStripLabel_RxCnt6.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
                toolStripLabel_RxCnt7.Text = "Received Counts：" + Convert.ToString(u16RdRunInfoRxCnt);
            }
        }
        #endregion

        /// <summary>
        /// 串口发送数据
        /// </summary>
        /// <param name="u_Params">待发送数据缓存</param>
        /// <param name="bRs485FunCmd">MODBus命令码</param>
        /// <param name="u16Rs485RegAddr">设备地址</param>
        /// <param name="u16Rs485RegNum">操作的寄存器数量</param>
        /// <param name="bRs485ByteNum">发送缓存的字节数</param>
        //03，06指令
        public void SentData(byte bRs485FunCmd, UInt16 u16Rs485RegAddr, UInt16 u16Rs485RegNum)
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

                sendCatch[i++] = RS485_SLAVE_ADDR;
                sendCatch[i++] = bRs485FunCmd;
                sendCatch[i++] = (byte)(u16Rs485RegAddr / 256);
                sendCatch[i++] = (byte)(u16Rs485RegAddr % 256);
                sendCatch[i++] = (byte)(u16Rs485RegNum / 256);
                sendCatch[i++] = (byte)(u16Rs485RegNum % 256);

                Calculate_Sum_Tx(ref sendCatch, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(sendCatch, 0, i + 2);
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
        //0x10指令
        public void SentData(UInt16[] u_Params, byte bRs485FunCmd,
            UInt16 u16Rs485RegAddr, UInt16 u16Rs485RegNum, byte bRs485ByteNum)
        {
            try
            {
                byte i = 0, j = 0;

                sendCatch[i++] = RS485_SLAVE_ADDR;
                sendCatch[i++] = bRs485FunCmd;
                sendCatch[i++] = (byte)(u16Rs485RegAddr / 256);
                sendCatch[i++] = (byte)(u16Rs485RegAddr % 256);
                sendCatch[i++] = (byte)(u16Rs485RegNum / 256);
                sendCatch[i++] = (byte)(u16Rs485RegNum % 256);
                sendCatch[i++] = bRs485ByteNum;

                for (; j < u16Rs485RegNum; j++)
                {
                    sendCatch[i++] = (byte)((u_Params[j] >> 8) & 0x00ff);
                    sendCatch[i++] = (byte)(u_Params[j] & 0x00ff);
                }

                Calculate_Sum_Tx(ref sendCatch, i);
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                bRxByteCnt = 0;
                bTotleBytes = 0;
                bRxFrameFinishFlag = false;
                serialPort1.Write(sendCatch, 0, i + 2);
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
