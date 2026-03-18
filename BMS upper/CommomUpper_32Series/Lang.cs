using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CommomUpper_32Series
{
    partial class Form1
    {
        public static string[] s_Verification;
        public static string[] s_MsgInfo;
        private TabPage[] tp = new TabPage[5];      //用于隐藏/显示页面

        /* 翻译 ：校准页面 */
        private List<Label> Calibration1_CELL_当前电压 = new List<Label>();
        private List<Label> Calibration1_CELL_目标电压 = new List<Label>();
        private List<Label> Calibration2_temp_当前温度 = new List<Label>();
        private List<Label> Calibration2_temp_目标温度 = new List<Label>();
        private List<Button> Calibration2_temp_计算 = new List<Button>();
        private List<Button> Calibration2_temp_写入 = new List<Button>();
        private Control[] Calibration1_CELL_计算 = new Control[35];
        private Control[] Calibration1_CELL_写入 = new Control[35];
        /* 校准翻译初始化 */
        public void Calibration_init()
        {
            #region 校准1页面的当前电压和目标电压
            Calibration1_CELL_当前电压.AddRange(new Label[] { label175, label185, label195 ,label177, label187 ,label197, label179,
                                            label189 ,label199 ,label181, label191, label201,
                                            label183 ,label193,
                                            label224 ,label218, label345, label341, label337,
                                            label333 ,label329,
                                            label365 ,label353, label373, label369, label361,
                                            label357 ,label349,
                                            label381 ,label377, label108, label59, label389,
                                            label385 , label21});


            Calibration1_CELL_目标电压.AddRange(new Label[] { label174 ,label184, label194 ,label176, label186,
                        label196, label178,
                        label192, label182 ,label200, label190 ,label180,
                        label198, label188,
                        label223 ,label172, label344, label340 ,label336,
                        label332 ,label328,
                        label348 ,label4,
                        label356 ,label384, label360,
                        label388, label48, label368, label372, label107,
                        label352, label376,  label380, label364});
            #endregion

            #region 校准1页面的计算和写入 
            for (int i = 0; i < 32; i++)
            {
                Calibration1_CELL_计算[i] = this.Controls.Find("button_calc_cell" + (i + 1).ToString(), true)[0];
                Calibration1_CELL_写入[i] = this.Controls.Find("button_cali_cell" + (i + 1).ToString(), true)[0];
            }
            Calibration1_CELL_计算[32] = button_calc_AFE1;
            Calibration1_CELL_计算[33] = button_calc_AFE2;
            Calibration1_CELL_计算[34] = button_calc_Vbus;

            Calibration1_CELL_写入[32] = button_cali_AFE1;
            Calibration1_CELL_写入[33] = button_cali_AFE2;
            Calibration1_CELL_写入[34] = button_cali_Vbus;
            #endregion

            #region 校准2页面的计算、写入、当前温度、目标温度
            Calibration2_temp_当前温度.AddRange(new Label[] { label209, label215, label396, label400, label202, label202, label408, label404, label213, label211 });
            Calibration2_temp_目标温度.AddRange(new Label[] { label208, label214, label210, label392, label398, label63, label216, label406, label402, label212 });
            Calibration2_temp_计算.AddRange(new Button[] { button_calc_temp1, button_calc_temp2 ,button_calc_temp3 ,button_calc_temp4, button_calc_temp5,
                button_calc_temp6, button_calc_tempEnv1, button_calc_tempEnv2, button_calc_tempEnv3, button_calc_tempmos,button_calc_Idsg,button_calc_Ichg
                });
            Calibration2_temp_写入.AddRange(new Button[] { button_cali_temp1, button_cali_temp2 ,button_cali_temp3 ,button_cali_temp4, button_cali_temp5,
                button_cali_temp6, button_cali_tempEnv1, button_cali_tempEnv2, button_cali_tempEnv3,
                button_cali_temp_mos,button_cali_Idsg , button_cali_Ichg });
            #endregion
        }

        private void HideTabPages()
        {
            /*
            for (int x = 0; x < 5; x++)
            {
                tp[x] = this.tabControl1.TabPages[2];
                tabControl1.TabPages.Remove(tp[x]);
            }
            foreach (Control ctrl in groupBox_set.Controls)
            {
                if (ctrl is Button)
                {
                    ctrl.Enabled = false;
                }
            }
            */
            //tabPage_Switch.Parent = null;
            tabPage_Cali1.Parent = null;
            //tabPage_Cali2.Parent = null;
            tabPage_Protect.Parent = null;
            //tabPage_OtherElement.Parent = null;

            tabPage_BlueTWifi.Parent = null;
            tabPage_SeriesParallel.Parent = null;
            tabPage_SeriesParallel_Element.Parent = null;
        }

        private void ShowTabPages()
        {
            /*
            for (int x = 0; x < 5; x++)
            {
                tabControl1.TabPages.Insert(1, tp[4 - x]);
            }
            tp = null;
            */
            //tabPage_Switch.Parent = tabControl1;
            tabPage_Cali1.Parent = tabControl1;
            tabPage_Cali2.Parent = tabControl1;
            //tabPage_Protect.Parent = tabControl1;
            tabPage_OtherElement.Parent = tabControl1;

            tabPage_BlueTWifi.Parent = tabControl1;
            tabPage_SeriesParallel.Parent = tabControl1;
            tabPage_SeriesParallel_Element.Parent = tabControl1;
        }

        private void textBox_admin_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //选中管理员密码文本框时，按回车键
            if (e.KeyValue == 13)
            {
                button_Admin_Click(sender, e);
            }
        }

        private void button_Admin_Click(object sender, EventArgs e)
        {
            if (string.Equals(textBox_admin.Text, "hs201803"))
            {
                if (tp == null) return;
                ShowTabPages();
                foreach (Control ctrl in groupBox_set.Controls)
                {
                    if (ctrl is Button)
                    {
                        ctrl.Enabled = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("密码错误", "权限认证", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Chinese_Click(object sender, EventArgs e)
        {
            Lang.b_LangFlag = 0;
            this.LangSelection_InformationPage();
            this.LangSelection_ProtectionPage();
            this.LangSelection_OtherElementPage();
            this.LangSelection_MsgInfo();
            this.LangSelection_SeriesParallelPage();
            s_Verification = Lang.s_Verification_Ch;

            #region 校准页面1
            foreach (var x in Calibration1_CELL_当前电压)
                x.Text = "当前电压";
            foreach (var x in Calibration1_CELL_目标电压)
                x.Text = "目标电压";
            foreach (var x in Calibration1_CELL_计算)
                x.Text = "计算";
            foreach (var x in Calibration1_CELL_写入)
                x.Text = "写入";
            button_Read_Vcell.Text = "Vell读取";
            button_Reset_Vcell.Text = "Vell复位";
            button_Read_Vafe_bus.Text = "AFE_Bus读取";
            button_Reset_AFE1.Text = "AFE1复位";
            button_Reset_AFE2.Text = "AFE2复位";
            button_Reset_Vbus.Text = "Vbus复位";
#endregion

            #region 校准页面2
            foreach (var x in Calibration2_temp_当前温度)
                x.Text = "当前温度";
            foreach (var x in Calibration2_temp_目标温度)
                x.Text = "目标温度";
            foreach (var x in Calibration2_temp_计算)
                x.Text = "计算";
            foreach (var x in Calibration2_temp_写入)
                x.Text = "写入";

            label207.Text = label205.Text = "当前电流";
            label206.Text = label204.Text = "目标电流";
            button_temp_read.Text = "Temp读取";
            button_temp_reset.Text = "Temp参数复位";
            button_Idsg_reset.Text = "Idsg参数复位";
            button_Current_read.Text = " Current读取";
            button_Ichg_reset.Text = " Ichg参数复位";
#endregion

            #region 蓝牙模块
            label322.Text = "蓝牙设备选择:";
            button_BT_Factory.Text = "恢复出厂设置";
            button_BT_BandRate_Get.Text = "获取波特率";
            label412.Text = "当前波特率:";
            button_BT_BandRate_Change.Text = "修改波特率";
            label423.Text = "目标波特率:";
            button_BT_DevName_Get.Text = "获取设备名";
            label436.Text = "当前设备名:";
            button_BT_DevName_Change.Text = "修改设备名";
            label444.Text = "目标设备名:";
            button_BT_Check_Status.Text = "查看连接状况";
            label475.Text = "状态:";
            label_Connection_Status_BT.Text = "未连接";
            label474.Text = "连接设备:";
            label477.Text = "设备搜索结果:";
            label478.Text = "通讯交互窗口:";
            button_Scan_BlueTooth.Text = "搜索蓝牙";
            button_Connect_BlueTooth.Text = "连接蓝牙";
            button_DisConnect_BlueTooth.Text = "断开连接";

#endregion

            #region WIFI模块

            label487.Text = "WiFi设备选择:";
            button_Wifi_Factory.Text = "恢复出厂设置";
            button_Wifi_BandRate_Get.Text = "获取波特率";
            label489.Text = "当前波特率:";
            button_Wifi_BandRate_Change.Text = "修改波特率";
            label488.Text = "目标波特率:";
            button_AP_Get.Text = "获取设备AP";
            label486.Text = "当前AP名:";
            label491.Text = "当前AP密码:";
            button_AP_Change.Text = "修改设备AP";
            label485.Text = "目标AP名:";
            label490.Text = "目标AP密码:";
            label492.Text = "备注：作为站点模式可以不管这两项 ";
            button_Wifi_Check_Status.Text = "查看连接状况";
            label484.Text = "状态:";
            label_Connection_Status_Wifi.Text = "未连接";
            label483.Text = "连接设备:";
            button_Connect_Wifi.Text = "连接AP";
            label479.Text = "目标AP热点名:";
            label482.Text = "目标AP热点密码:";
            button_DisConnect_Wifi.Text = "断开AP";
            label481.Text = "设备搜索结果:";
            label480.Text = "通讯交互窗口:";
            button_Scan_AP.Text = "搜索AP";
            button_Select_AP.Text = "选择AP";

#endregion

            #region BMS产品编号模块
            label_BMS_SerialVer.Text = "BMS序列号:";
            label_BMS_HardWareVer.Text = "BMS硬件版本号:";
            label_BMS_SoftWareVer.Text = "BMS软件版本号:";

            button_BMS_SerialNum_Set.Text = "设置";
            button_BMS_HardWareVer_Set.Text = "设置";
            button_BMS_SoftWareVer_Set.Text = "设置";
            button_SN_Version_Read.Text = "读取";
#endregion

            #region 其他
            label_DevAddr.Text = "当前设备地址:";
            button_DevAddrMng.Text = "地址管理";
            label_LogStatus.Text = "已停止";
#endregion
        }

        private void button_English_Click(object sender, EventArgs e)
        {
            Lang.b_LangFlag = 1;
            this.LangSelection_InformationPage();
            this.LangSelection_ProtectionPage();
            this.LangSelection_OtherElementPage();
            this.LangSelection_MsgInfo();
            this.LangSelection_SeriesParallelPage();
            s_Verification = Lang.s_Verification_En;

            #region 校准页面1
            foreach (var x in Calibration1_CELL_当前电压)
                x.Text = "Cur_vol";
            foreach (var x in Calibration1_CELL_目标电压)
                x.Text = "Tar_vol";
            foreach (var x in Calibration1_CELL_计算)
                x.Text = "Calculate";
            foreach (var x in Calibration1_CELL_写入)
                x.Text = "Write";
            button_Read_Vcell.Text = "Vell_Read";
            button_Reset_Vcell.Text = "Vell_Reset";
            button_Read_Vafe_bus.Text = "AFE_Bus_Read";
            button_Reset_AFE1.Text = "AFE1Reset";
            button_Reset_AFE2.Text = "AFE2Reset";
            button_Reset_Vbus.Text = "VbusReset";

            groupBox_Vcell_cal.Text = "VCell_Calibration(B unit mV)";
    #endregion

            #region 校准页面2
            foreach (var x in Calibration2_temp_当前温度)
                x.Text = "Cur_temp";
            foreach (var x in Calibration2_temp_目标温度)
                x.Text = "Tar_temp";
            foreach (var x in Calibration2_temp_计算)
                x.Text = "Calculate";
            foreach (var x in Calibration2_temp_写入)
                x.Text = "Write";

            label207.Text = label205.Text = "Cur_cur";
            label206.Text = label204.Text = "Tar_cur";
            button_temp_read.Text = "Temp_Read";
            button_temp_reset.Text = "Temp_Reset";
            button_Idsg_reset.Text = "Idsg_Reset";
            button_Current_read.Text = "Cur_Read";
            button_Ichg_reset.Text = "Ichg_Reset";

            groupBox_temp_cal.Text = "Temp_Calibration(B unit ℃)";
    #endregion

            #region 蓝牙模块
            label322.Text = "DevSelect:";
            button_BT_Factory.Text = "FactoryReset";
            button_BT_BandRate_Get.Text = "BandGet";
            label412.Text = "CurrentBaud:";
            button_BT_BandRate_Change.Text = "BandChange";
            label423.Text = "TargetBaud:";
            button_BT_DevName_Get.Text = "DevNameGet";
            label436.Text = "CurrentDevName:";
            button_BT_DevName_Change.Text = "DevNameChange";
            label444.Text = "TargetDevName: ";
            button_BT_Check_Status.Text = "CheckStatus";
            label475.Text = "Status:";
            label_Connection_Status_BT.Text = "Ununited";
            label474.Text = "ConnectedDev:";
            label477.Text = "DeviceSearch Result:";
            label478.Text = "Communicate Interaction:";
            button_Scan_BlueTooth.Text = "SearchBT";
            button_Connect_BlueTooth.Text = "ConnectBT";
            button_DisConnect_BlueTooth.Text = "Disconnect";
    #endregion

            #region WIFI模块
            label487.Text = "DeviceSelect:";
            button_Wifi_Factory.Text = "FactoryReset";
            button_Wifi_BandRate_Get.Text = "BandGet";
            label489.Text = "CurrentBaud:";
            button_Wifi_BandRate_Change.Text = "BandChange";
            label488.Text = "TargetBaud:";
            button_AP_Get.Text = "AP Get";
            label486.Text = "CurrentAP Name:";
            label491.Text = "CurrentAP Password:";
            button_AP_Change.Text = "Modify AP";
            label485.Text = "TargetAP Name:";
            label490.Text = "TargetAP Password";
            label492.Text = "Note:As a site model you can ignore these two items";
            button_Wifi_Check_Status.Text = "CheckStatus";
            label484.Text = "Status:";
            label_Connection_Status_Wifi.Text = "Ununited";
            label483.Text = "ConnectedAP:";
            button_Connect_Wifi.Text = "ConnectAP";
            label479.Text = "Target AP HotName:";
            label482.Text = "Destination AP Hotspot Password:";
            button_DisConnect_Wifi.Text = "Break AP";
            label481.Text = "Device Search Results:";
            label480.Text = "Communicate Interaction:";
            button_Scan_AP.Text = "SearchAP";
            button_Select_AP.Text = "SelectAP";
    #endregion

            #region BMS产品编号模块
            label_BMS_SerialVer.Text = "SerialVer:";
            label_BMS_HardWareVer.Text = "HardWareVer:";
            label_BMS_SoftWareVer.Text = "SoftWareVer:";

            button_BMS_SerialNum_Set.Text = "Set";
            button_BMS_HardWareVer_Set.Text = "Set";
            button_BMS_SoftWareVer_Set.Text = "Set";
            button_SN_Version_Read.Text = "Read";
            #endregion

            #region 其他
            label_DevAddr.Text = "DevAddr_Now:";
            button_DevAddrMng.Text = "DevAddrMng";
            label_LogStatus.Text = "Stop";
            #endregion
        }

        #region Information
        private void LangSelection_InformationPage()
        {
            string[] s_InformationPage;
            string[] s_Admin;
            string[] s_FunSelect;
            string[] s_SystemError;
            string[] s_PresentProtectionRecords;
            string[] s_HeatCoolStatus;
            string[] s_CellVoltage;
            string[] s_Protection;
            string[] s_VcellStatistics;
            string[] s_Temperature;
            string[] s_ComprehensiveInfo;
            string[] s_SystemStatus;

            if (0 == Lang.b_LangFlag)
            { //中文 
                s_InformationPage = Lang.s_InformationPage_Ch;
                s_Admin = Lang.s_Admin_Ch;
                s_FunSelect = Lang.s_FunSelect_Ch;
                s_SystemError = Lang.s_SystemError_Ch;
                s_PresentProtectionRecords = Lang.s_PresentProtectionRecords_Ch;
                s_HeatCoolStatus = Lang.s_HeatCoolStatus_Ch;
                s_CellVoltage = Lang.s_CellVoltage_Ch;
                s_Protection = Lang.s_Protection_Ch;
                s_VcellStatistics = Lang.s_VcellStatistics_Ch;
                s_Temperature = Lang.s_Temperature_Ch;

                s_ComprehensiveInfo = Lang.s_ComprehensiveInfo_Ch;
                label_Present_mAh_ref.Location = new Point(104, 73);
                label_Full_mAh_ref.Location = new Point(104, 99);
                label_Factory_mAh_ref.Location = new Point(104, 122);

                s_SystemStatus = Lang.s_SystemStatus_Ch;
                FaultWarnName = Lang.s_FaultWarnName_Ch;
            }
            else
            {
                s_InformationPage = Lang.s_InformationPage_En;
                s_Admin = Lang.s_Admin_En;
                s_FunSelect = Lang.s_FunSelect_En;
                s_SystemError = Lang.s_SystemError_En;
                s_PresentProtectionRecords = Lang.s_PresentProtectionRecords_En;
                s_HeatCoolStatus = Lang.s_HeatCoolStatus_En;
                s_CellVoltage = Lang.s_CellVoltage_En;
                s_Protection = Lang.s_Protection_En;
                s_VcellStatistics = Lang.s_VcellStatistics_En;
                s_Temperature = Lang.s_Temperature_En;

                s_ComprehensiveInfo = Lang.s_ComprehensiveInfo_En;
                label_Present_mAh_ref.Location = new Point(126, 73);
                label_Full_mAh_ref.Location = new Point(126, 99);
                label_Factory_mAh_ref.Location = new Point(126, 122);

                s_SystemStatus = Lang.s_SystemStatus_En;
                FaultWarnName = Lang.s_FaultWarnName_En;
            }
            InformationPage_LangInit(s_InformationPage);
            Admin_LangInit(s_Admin);
            FunSelect_LangInit(s_FunSelect);
            SystemError_LangInit(s_SystemError);
            PresentProtectionRecords_LangInit(s_PresentProtectionRecords);
            HeatCoolStatus_LangInit(s_HeatCoolStatus);
            CellVoltage_LangInit(s_CellVoltage);
            Protection_LangInit(s_Protection);
            VcellStatistics_LangInit(s_VcellStatistics);
            Temperature_LangInit(s_Temperature);
            ComprehensiveInfo_LangInit(s_ComprehensiveInfo);
            SystemStatus_LangInit(s_SystemStatus);
        }

        private void InformationPage_LangInit(string[] s_Lang)
        {
            this.Text = s_Lang[0];
            tabPage_CellInfo.Text = s_Lang[1];
            label_UART.Text = s_Lang[2];
            label_BaudRate.Text = s_Lang[3];
            label_Strategy.Text = s_Lang[4];
            //comboBox_SystemDriver.Items[0] = s_Lang[5];
            //comboBox_SystemDriver.Items[1] = s_Lang[6];
            //comboBox_SystemDriver.Items[2] = s_Lang[7];
            //comboBox_SystemDriver.Items[3] = s_Lang[8];
            //button_SystemDriver.Text = s_Lang[9];
            groupBox_SysInfo.Text = s_Lang[10];
            label_PreStrategy.Text = s_Lang[11];
            label_PreLogStatus.Text = s_Lang[12];

            //tabPage_Switch.Text = s_Lang[13];
            tabPage_Cali1.Text = s_Lang[14];
            tabPage_Cali2.Text = s_Lang[15];
            tabPage_Protect.Text = s_Lang[16];
            tabPage_OtherElement.Text = s_Lang[17];
            tabPage_BlueTWifi.Text = s_Lang[18];
            tabPage_SeriesParallel.Text = s_Lang[19];
            tabPage_SeriesParallel_Element.Text = s_Lang[20];
        }

        private void Admin_LangInit(string[] s_Lang)
        {
            label_admin.Text = s_Lang[0];
            button_Admin.Text = s_Lang[1];
        }

        private void FunSelect_LangInit(string[] s_Lang)
        {
            groupBox_set.Text = s_Lang[4];
            label_Function.Text = s_Lang[5];
            label_Ctrl.Text = s_Lang[6];
            label_Status.Text = s_Lang[7];
            label_CtrlMOS_Relay.Text = s_Lang[8];
            label_CtrlRelay.Text = s_Lang[9];
            label_SOCFixed.Text = s_Lang[10];
            label_SOCClear.Text = s_Lang[11];
            label_CtrlHeat.Text = s_Lang[12];
            label_CtrlCool.Text = s_Lang[13];
            label_CtrlBalance.Text = s_Lang[14];
            label_CtrlAFE1.Text = s_Lang[15];
            label_CtrlAFE2.Text = s_Lang[16];
            label_CtrlSleep.Text = s_Lang[17];
            label_CtrlAPS.Text = s_Lang[18];
            button_SetSocOnce.Text = s_Lang[19];
            foreach (Control ctrl_inside in groupBox_set.Controls)
            {
                if (ctrl_inside is Button)
                {
                    switch (ctrl_inside.Name)
                    {
                        case "button_MosRelay_Func_Open":
                        case "button_Relay_Func_Open":
                        case "button_SocFixed_Func_Open":
                        case "button_SocZero_Func_Open":
                        case "button_Heated_Func_Open":
                        case "button_Cool_Func_Open":
                        case "button_Balance_Func_Open":
                        case "button_Sleep_Func_Open":
                        case "button_AFE2_Func_Open":
                            //case "button_SetSocOnce":
                            {
                                ctrl_inside.Text = s_Lang[0];
                                break;
                            }
                        case "button_AFE1_Func_Open":
                            {
                                ctrl_inside.Text = s_Lang[1];
                                break;
                            }
                        case "button_MosRelay_Func_Close":
                        case "button_Relay_Func_Close":
                        case "button_SocFixed_Func_Close":
                        case "button_SocZero_Func_Close":
                        case "button_Heated_Func_Close":
                        case "button_Cool_Func_Close":
                        case "button_Balance_Func_Close":
                        case "button_Sleep_Func_Close":
                        case "button_BMS_Source_Func_Close":
                        case "button_AFE2_Func_Close":
                            {
                                ctrl_inside.Text = s_Lang[2];
                                break;
                            }
                        case "button_AFE1_Func_Close":
                            {
                                ctrl_inside.Text = s_Lang[3];
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
            }
        }

        private void SystemError_LangInit(string[] s_Lang)
        {
            groupBox_read.Text = s_Lang[0];
            groupBox_ERROR.Text = s_Lang[1];
            label_AFE1_ComError.Text = s_Lang[2];
            label_AFE2_ComError.Text = s_Lang[3];
            label134.Text = s_Lang[4];
            label_Reserved2_ref.Text = s_Lang[5];
            label325.Text = s_Lang[6];
            label253.Text = s_Lang[7];
            label257.Text = s_Lang[8];
            label251.Text = s_Lang[9];
            label249.Text = s_Lang[10];
            label161.Text = s_Lang[11];
            label158.Text = s_Lang[12];
            label121.Text = s_Lang[13];
            label123.Text = s_Lang[14];
            label_Reserved3_ref.Text = s_Lang[15];
            label_Reserved4_ref.Text = s_Lang[16];
            label311.Text = s_Lang[17];
            label327.Text = s_Lang[18];
            label248.Text = s_Lang[19];
            label247.Text = s_Lang[20];
            label170.Text = s_Lang[21];
            label166.Text = s_Lang[22];
            label154.Text = s_Lang[23];
            label_Reserved1_ref.Text = s_Lang[24];
            label228.Text = s_Lang[25];
        }

        private void PresentProtectionRecords_LangInit(string[] s_Lang)
        {
            groupBox_Present.Text = s_Lang[0];
            label_Fault_Reacent.Text = s_Lang[1];
            label_Warn_Reacent.Text = s_Lang[2];
            label410.Text = s_Lang[3];
            button_ProtectPresentClear.Text = s_Lang[4];
            button_ProtectPresent_Detail.Text = s_Lang[5];
        }

        private void HeatCoolStatus_LangInit(string[] s_Lang)
        {
            groupBox36.Text = s_Lang[0];
            label304.Text = s_Lang[1];
            label_Heat_O.Text = s_Lang[2];
            label_Heat_.Text = s_Lang[3];
            label299.Text = s_Lang[4];
            label282.Text = s_Lang[5];
            label279.Text = s_Lang[6];
            label278.Text = s_Lang[7];

            label425.Text = s_Lang[8];
            label421.Text = s_Lang[9];
            label413.Text = s_Lang[10];
            label424.Text = s_Lang[11];
            label420.Text = s_Lang[12];
            label411.Text = s_Lang[13];
            label326.Text = s_Lang[14];
        }

        private void CellVoltage_LangInit(string[] s_Lang)
        {
            groupBox_VcellAFE1.Text = s_Lang[0];
            groupBox_VcellAFE2.Text = s_Lang[1];
            groupBox_Balance.Text = s_Lang[2];
            groupBox_Balance2.Text = s_Lang[3];
        }

        private void Protection_LangInit(string[] s_Lang)
        {
            #region 一级保护
            label237.Text = s_Lang[0];
            label234.Text = s_Lang[1];
            label231.Text = s_Lang[2];
            label230.Text = s_Lang[3];
            label243.Text = s_Lang[4];
            label245.Text = s_Lang[5];
            label244.Text = s_Lang[6];
            label246.Text = s_Lang[7];
            label235.Text = s_Lang[8];
            label233.Text = s_Lang[9];
            label221.Text = s_Lang[10];
            label173.Text = s_Lang[11];
            label168.Text = s_Lang[12];
            label169.Text = s_Lang[13];
            #endregion 一级保护
#region 二级保护
            label268.Text = s_Lang[0];
            label265.Text = s_Lang[1];
            label263.Text = s_Lang[2];
            label262.Text = s_Lang[3];
            label272.Text = s_Lang[4];
            label274.Text = s_Lang[5];
            label273.Text = s_Lang[6];
            label275.Text = s_Lang[7];
            label266.Text = s_Lang[8];
            label264.Text = s_Lang[9];
            label259.Text = s_Lang[10];
            label258.Text = s_Lang[11];
            label255.Text = s_Lang[12];
            label256.Text = s_Lang[13];
#endregion 二级保护
#region 三级保护
            label296.Text = s_Lang[0];
            label293.Text = s_Lang[1];
            label291.Text = s_Lang[2];
            label290.Text = s_Lang[3];
            label300.Text = s_Lang[4];
            label302.Text = s_Lang[5];
            label301.Text = s_Lang[6];
            label303.Text = s_Lang[7];
            label294.Text = s_Lang[8];
            label292.Text = s_Lang[9];
            label287.Text = s_Lang[10];
            label286.Text = s_Lang[11];
            label283.Text = s_Lang[12];
            label284.Text = s_Lang[13];
#endregion 三级保护
            groupBox_FirstPro.Text = s_Lang[14];
            groupBox_SecondPro.Text = s_Lang[15];
            groupBox_ThirdPro.Text = s_Lang[16];
        }

        private void VcellStatistics_LangInit(string[] s_Lang)
        {
            groupBox_Vcell_Statistics.Text = s_Lang[0];
            label_Vcell_max_ref.Text = s_Lang[1];
            label_Vcell_min_ref.Text = s_Lang[2];
            label_Max_pos_ref.Text = s_Lang[3];
            label_Min_pos_ref.Text = s_Lang[4];
            label_V_delta_ref.Text = s_Lang[5];
            label_Vbat_ref.Text = s_Lang[6];
        }

        private void Temperature_LangInit(string[] s_Lang)
        {
            groupBox_Temperature.Text = s_Lang[0];
            label_Temp1_ref.Text = s_Lang[1];
            label_Temp3_ref.Text = s_Lang[2];
            label_Temp_mos_ref.Text = s_Lang[3];
            label_Temp2_ref.Text = s_Lang[4];
            label_Temp_env_ref.Text = s_Lang[5];
            label155.Text = s_Lang[6];
            label164.Text = s_Lang[7];
            label160.Text = s_Lang[8];
            label163.Text = s_Lang[9];
            label157.Text = s_Lang[10];
            label_TempMax_ref.Text = s_Lang[11];
            label_TempMin_ref.Text = s_Lang[12];
        }

        private void ComprehensiveInfo_LangInit(string[] s_Lang)
        {
            groupBox35.Text = s_Lang[0];
            label_Ichg_ref.Text = s_Lang[1];
            label_Idsg_ref.Text = s_Lang[2];
            label_WarnStu_ref.Text = s_Lang[3];
            label_FaultStu_ref.Text = s_Lang[4];
            label309.Text = s_Lang[5];
            label435.Text = s_Lang[6];
            label242.Text = s_Lang[7];
            label250.Text = s_Lang[8];
            label_Present_mAh_ref.Text = s_Lang[9];
            label_Full_mAh_ref.Text = s_Lang[10];
            label_Factory_mAh_ref.Text = s_Lang[11];
            label_CycleTimes_ref.Text = s_Lang[12];
        }

        private void SystemStatus_LangInit(string[] s_Lang)
        {
            groupBox_SysStatus.Text = s_Lang[0];

            label_CHG_MOS_ref.Text = s_Lang[1];
            label_DSG_MOS_ref.Text = s_Lang[2];
            label94.Text = s_Lang[3];
            label323.Text = s_Lang[4];
            label455.Text = s_Lang[5];
            label_heat_ref.Text = s_Lang[6];
            label58.Text = s_Lang[7];

            label470.Text = s_Lang[8];
            label260.Text = s_Lang[9];
            label458.Text = s_Lang[10];
            label462.Text = s_Lang[11];
            label319.Text = s_Lang[12];
            label315.Text = s_Lang[13];
            label317.Text = s_Lang[14];

            label277.Text = s_Lang[15];
            label267.Text = s_Lang[16];
            label270.Text = s_Lang[17];
            label459.Text = s_Lang[18];
            label456.Text = s_Lang[19];
            label472.Text = s_Lang[20];
            label476.Text = s_Lang[21];
        }
        #endregion Information

        #region Protection
        private void LangSelection_ProtectionPage()
        {
            string[] s_ProtectionPage;
            string[] s_ProtectElement;
            string[] s_OtherParameters;
            string[] s_HeatCool;
            string[] s_BatchProcess;

            if (0 == Lang.b_LangFlag)
            { //中文 
                s_ProtectionPage = Lang.s_ProtectionPage_Ch;
                s_ProtectElement = Lang.s_ProtectElement_Ch;
                s_OtherParameters = Lang.s_OtherParameters_Ch;
                s_HeatCool = Lang.s_HeatCool_Ch;
                s_BatchProcess = Lang.s_BatchProcess_Ch;
            }
            else
            {
                s_ProtectionPage = Lang.s_ProtectionPage_En;
                s_ProtectElement = Lang.s_ProtectElement_En;
                s_OtherParameters = Lang.s_OtherParameters_En;
                s_HeatCool = Lang.s_HeatCool_En;
                s_BatchProcess = Lang.s_BatchProcess_En;
            }
            ProtectionPage_LangInit(s_ProtectionPage);
            ProtectElement_LangInit(s_ProtectElement);
            OtherParameters_LangInit(s_OtherParameters);
            HeatCool_LangInit(s_HeatCool);
            BatchProcess_LangInit(s_BatchProcess);
        }

        private void ProtectionPage_LangInit(string[] s_Lang)
        {
            tabPage_Protect.Text = s_Lang[0];
            foreach (Control ctrl in tabPage_Protect.Controls)
            {
                if (ctrl is GroupBox)
                {
                    foreach (Control ctrl_inside in ctrl.Controls)
                    {
                        if (ctrl_inside is Button)
                        {
                            switch (ctrl_inside.Name)
                            {
                                case "button_Protect_read":
                                case "textBox_Soc_read":
                                case "textBox_Sys_read":
                                case "button_Sleep_Read":
                                case "button_Balance_read":
                                case "button_Other_Read":
                                case "button_HeatCool_Read":
                                    {
                                        ctrl_inside.Text = s_Lang[1];
                                        break;
                                    }
                                case "button_VcellOVP_set":
                                case "button_VcellUVP_set":
                                case "button_VbusOVP_set":
                                case "button_VbusUVP_set":
                                case "button_IchgOCP_set":
                                case "button_IdsgOCP_set":
                                case "button_TchgOTP_set":
                                case "button_TchgUTP_set":
                                case "button_TdsgOTP_set":
                                case "button_TdsgUTP_set":
                                case "button_TmosOTP_set":
                                case "button_VdeltaOVP_set":
                                case "button_SocUp_set":
                                case "textBox_Soc_set":
                                case "textBox_Sys_set":
                                case "button_Sleep_Set":
                                case "button_Balance_set":
                                case "button_Other_Set":
                                case "button_HeatCool_Set":
                                    {
                                        ctrl_inside.Text = s_Lang[2];
                                        break;
                                    }
                                default: break;
                            }
                        }
                    }
                }
            }
        }

        private void ProtectElement_LangInit(string[] s_Lang)
        {
            groupBox_ProtectElement.Text = s_Lang[0];
            label24.Text = s_Lang[1];
            label39.Text = s_Lang[2];
            label44.Text = s_Lang[3];
            label40.Text = s_Lang[4];
            label_Vcell_DT.Text = s_Lang[5];
            lable_VcellOVP.Text = s_Lang[6];
            label_Vcell_UVP.Text = s_Lang[7];
            label_Vbus_OVP.Text = s_Lang[8];
            label_Vbus_UVP.Text = s_Lang[9];
            label_Ichg_OCP.Text = s_Lang[10];
            label_Idsg_OCP.Text = s_Lang[11];
            label_Tcell_ChgOTP.Text = s_Lang[12];
            label_Tcell_ChgUTP.Text = s_Lang[13];
            label_Tcell_DsgOTP.Text = s_Lang[14];
            label_Tcell_DsgUTP.Text = s_Lang[15];
            label_Tmos_OTP.Text = s_Lang[16];
            label_DeltaOP.Text = s_Lang[17];
            label_SocUP.Text = s_Lang[18];
            button_Protect_Reset.Text = s_Lang[19];
        }

        private void OtherParameters_LangInit(string[] s_Lang)
        {
            groupBox_OtherParam.Text = s_Lang[0];
            button_CanAdd_Reset.Text = s_Lang[1];
            label151.Text = s_Lang[2];
            label150.Text = s_Lang[3];
            label147.Text = s_Lang[4];
            label_SOC_V_100.Text = s_Lang[5];
            label_SOC_V_0.Text = s_Lang[6];
            label152.Text = s_Lang[7];
            label238.Text = s_Lang[8];
            label_CS_Res.Text = s_Lang[9];
            label46.Text = s_Lang[10];
            label232.Text = s_Lang[11];
            label_SleepElement.Text = s_Lang[12];
            label_NormalV.Text = s_Lang[13];
            label_NormalT.Text = s_Lang[14];
            label_OverDsgV.Text = s_Lang[15];
            label_OverDsgT.Text = s_Lang[16];
            label146.Text = s_Lang[17];
            label132.Text = s_Lang[18];
            label122.Text = s_Lang[19];
            label117.Text = s_Lang[20];
            labe_balance.Text = s_Lang[21];
            label_Open_V.Text = s_Lang[22];
            label_Open_W.Text = s_Lang[23];
            label_Close_W1.Text = s_Lang[24];
            label_Close_W2.Text = s_Lang[25];
            label254.Text = s_Lang[26];
            label240.Text = s_Lang[27];
            label241.Text = s_Lang[28];
            label60.Text = s_Lang[29];
            label_Other_set.Text = s_Lang[30];
            label_DSG_high.Text = s_Lang[31];
            label_DSG_low.Text = s_Lang[32];
            label_CHG_low.Text = s_Lang[33];
            label_CBC_Delay.Text = s_Lang[34];
            label269.Text = s_Lang[35];
            label252.Text = s_Lang[36];
            label261.Text = s_Lang[37];
            label165.Text = s_Lang[38];
        }

        private void HeatCool_LangInit(string[] s_Lang)
        {
            groupBox_HeatCool.Text = s_Lang[0];
            label434.Text = s_Lang[1];
            label433.Text = s_Lang[2];
            label281.Text = s_Lang[3];
            label422.Text = s_Lang[4];
            label432.Text = s_Lang[5];
            label468.Text = s_Lang[6];
            label321.Text = s_Lang[7];
            label313.Text = s_Lang[8];
            label285.Text = s_Lang[9];
            label289.Text = s_Lang[10];
            label467.Text = s_Lang[11];
            label469.Text = s_Lang[12];
            label466.Text = s_Lang[13];
            label464.Text = s_Lang[14];
            label437.Text = s_Lang[15];
            label4441.Text = s_Lang[16];
            label460.Text = s_Lang[17];
            label457.Text = s_Lang[18];
            label461.Text = s_Lang[19];
            label443.Text = s_Lang[20];
            label439.Text = s_Lang[21];
            label441.Text = s_Lang[22];
            label473.Text = s_Lang[23];
            label465.Text = s_Lang[24];
            label463.Text = s_Lang[25];
            label471.Text = s_Lang[26];
            button_HeatCool_Reset.Text = s_Lang[27];
        }

        private void BatchProcess_LangInit(string[] s_Lang)
        {
            groupBox_BatchProcess.Text = s_Lang[0];
            button_FileLoad.Text = s_Lang[1];
            button_Export.Text = s_Lang[2];
            //button_FileBrowse.Text = s_Lang[3];
            button_ReadAll.Text = s_Lang[4];
            button_SetAll.Text = s_Lang[5];
        }
#endregion Protection        

        #region OtherElement
        private void LangSelection_OtherElementPage()
        {
            string[] s_OtherElementPage;
            string[] s_Upgrade;
            string[] s_LogRecord;

            if (0 == Lang.b_LangFlag)
            { //中文 
                s_OtherElementPage = Lang.s_OtherElementPage_Ch;
                s_Upgrade = Lang.s_Upgrade_Ch;
                s_LogRecord = Lang.s_LogRecord_Ch;
            }
            else
            {
                s_OtherElementPage = Lang.s_OtherElementPage_En;
                s_Upgrade = Lang.s_Upgrade_En;
                s_LogRecord = Lang.s_LogRecord_En;
            }
            OtherElementPage_LangInit(s_OtherElementPage);
            Upgrade_LangInit(s_Upgrade);
            LogRecord_LangInit(s_LogRecord);
        }

        private void OtherElementPage_LangInit(string[] s_Lang)
        {
            tabPage_OtherElement.Text = s_Lang[0];
            foreach (Control ctrl in tabPage_OtherElement.Controls)
            {
                if (ctrl is GroupBox)
                {
                    foreach (Control ctrl_Inside in ctrl.Controls)
                    {
                        switch (ctrl_Inside.Name)
                        {
                            case "button_SocTable_Read":
                            case "button_CopperLoss_read":
                            case "Button_Fault_Record_Read":
                            case "button_RTC_Read":
                                {
                                    ctrl_Inside.Text = s_Lang[1];
                                    break;
                                }
                            case "button_SocTable_Set":
                            case "button_CopperLoss_set":
                            case "button_RTC_Set":
                                {
                                    ctrl_Inside.Text = s_Lang[2];
                                    break;
                                }
                            default: break;
                        }
                    }
                }
            }
            groupBox_SocTable.Text = s_Lang[3];
            label395.Text = s_Lang[4];
            label394.Text = s_Lang[5];
            groupBox_CopperLoss.Text = s_Lang[6];
            label_CopperLoss.Text = s_Lang[7];
            label_CellNum.Text = s_Lang[8];
            label_CellNumNotice.Text = s_Lang[9];
            groupBox_History.Text = s_Lang[10];
            Button_Fault_Record_Clear.Text = s_Lang[11];
        }

        private void Upgrade_LangInit(string[] s_Lang)
        {
            groupBox_upgrade.Text = s_Lang[0];
            label2.Text = s_Lang[1];
            Button_upgrate_find.Text = s_Lang[2];
            button_upgrate_connect.Text = s_Lang[3];
            button_upgrate_begin.Text = s_Lang[4];
            button_upgrate_clear.Text = s_Lang[5];
            label1.Text = s_Lang[6];
        }

        private void LogRecord_LangInit(string[] s_Lang)
        {
            groupBox_Daily.Text = s_Lang[0];
            label_LogTimer.Text = s_Lang[1];
            button_LogTimerStart.Text = s_Lang[2];
            button_LogTimerStop.Text = s_Lang[3];
            label_LogFileName.Text = s_Lang[4];
        }
        #endregion OtherElement

        #region 串机页面
        private void LangSelection_SeriesParallelPage()
        {
            string[] s_Vcell_Statistics;
            string[] s_Current_SOC_HeatCool_FaultFlag;
            string[] s_SafetyStatus_Flag;
            string[] s_ParallelSystem_Status;
            string[] s_OtherElement_Status;

            if (0 == Lang.b_LangFlag)
            { //中文 
                s_Vcell_Statistics = Lang.s_Vcell_Statistics_Ch;
                s_Current_SOC_HeatCool_FaultFlag = Lang.s_Current_SOC_HeatCool_FaultFlag_Ch;
                s_SafetyStatus_Flag = Lang.s_SafetyStatus_Ch;
                s_ParallelSystem_Status = Lang.s_ParallelSystem_Ch;
                s_OtherElement_Status = Lang.s_OtherElement_Ch;
            }
            else
            {
                s_Vcell_Statistics = Lang.s_Vcell_Statistics_En;
                s_Current_SOC_HeatCool_FaultFlag = Lang.s_Current_SOC_HeatCool_FaultFlag_En;
                s_SafetyStatus_Flag = Lang.s_SafetyStatus_En;
                s_ParallelSystem_Status = Lang.s_ParallelSystem_En;
                s_OtherElement_Status = Lang.s_OtherElement_En;
            }

            SeriesParallelPage_Vcell_Statistics_Init(s_Vcell_Statistics);
            SeriesParallelPage_Current_SOC_HeatCool_FaultFlag_Init(s_Current_SOC_HeatCool_FaultFlag);
            SeriesParallelPage_SafetyStatus_Flag_Init(s_SafetyStatus_Flag);
            SeriesParallelPage_System_Status_Init(s_ParallelSystem_Status);
            SeriesParallelPage_OtherElement_Init(s_OtherElement_Status);
        }

        private void SeriesParallelPage_OtherElement_Init(string[] s_Lang)
        {
            Control Controler1;
            Control Controler2;
            try
            {
                for (int j = 0; j < 16; ++j)
                {
                    Controler1 = this.Controls.Find("label_Par_VbatBMS" + (j + 1).ToString(), true)[0];
                    this.Invoke(new EventHandler(delegate
                    {
                        Controler1.Text = "BMS" + (j + 1).ToString() + "_" + s_Lang[0];
                    }

                    ));

                    Controler2 = this.Controls.Find("label_Err_BMS" + (j + 1).ToString(), true)[0];
                    this.Invoke(new EventHandler(delegate
                    {
                        Controler2.Text = "BMS" + (j + 1).ToString() + "_" + s_Lang[1];
                    }));
                }
            }
            catch (Exception)
            {
            }
        }

        private void SeriesParallelPage_Vcell_Statistics_Init(string[] s_Lang)
        {
            label_Par_VbatSum.Text = s_Lang[0];
            label_Par_VpackSumDelta.Text = s_Lang[1];
            label_Par_VpackMax.Text = s_Lang[2];
            label_Par_VpackMin.Text = s_Lang[3];
            label_Par_VpackMax_Pos.Text = s_Lang[4];
            label_Par_VpackMin_Pos.Text = s_Lang[5];
            label_Par_VcellMax.Text = s_Lang[6];
            label_Par_VcellMin.Text = s_Lang[7];
            label_Par_VcellMax_Pos.Text = s_Lang[8];
            label_Par_VcellMin_Pos.Text = s_Lang[9];
            label_Par_VcellDeltaMax.Text = s_Lang[10];
            label_Par_VcellDeltaMin.Text = s_Lang[11];
            label_Par_VcellDeltaMax_Pos.Text = s_Lang[12];
            label_Par_VcellDeltaMin_Pos.Text = s_Lang[13];
            label_Par_TempMax.Text = s_Lang[14];
            label_Par_TempMin.Text = s_Lang[15];
            label_Par_TempMax_Pos.Text = s_Lang[16];
            label_Par_TempMin_Pos.Text = s_Lang[17];
            label_Par_TempMosMax.Text = s_Lang[18];
            label_Par_TempMosMin.Text = s_Lang[19];
            label_Par_TempMosMax_Pos.Text = s_Lang[20];
            label_Par_TempMosMin_Pos.Text = s_Lang[21];

            label_Par_VcellDeltaAll.Text = s_Lang[22];       //添加
        }

        private void SeriesParallelPage_Current_SOC_HeatCool_FaultFlag_Init(string[] s_Lang)
        {
            label_Par_IchgSum.Text = s_Lang[0];
            label_Par_IdsgSum.Text = s_Lang[1];
            label_Par_Fault_First.Text = s_Lang[2];
            label_Par_Fault_Second.Text = s_Lang[3];
            label_Par_Fault_Third.Text = s_Lang[4];
            label_Par_ErrHeatCool.Text = s_Lang[5];

            label_Par_SOC.Text = s_Lang[6];
            label_Par_SOH.Text = s_Lang[7];
            label_Par_Present_mAh.Text = s_Lang[8];
            label_Par_Full_mAh.Text = s_Lang[9];
            label_Par_Factory_mAh.Text = s_Lang[10];
            label_Par_CycleTimes.Text = s_Lang[11];
        }

        private void SeriesParallelPage_SafetyStatus_Flag_Init(string[] s_Lang)
        {
            label_Par_Vcell_OV_First.Text = s_Lang[0];
            label_Par_Vcell_UV_First.Text = s_Lang[1];
            label_Par_Vpack_OV_First.Text = s_Lang[2];
            label_Par_Vpack_OV_First.Text = s_Lang[3];
            label_Par_Vbat_OV_First.Text = s_Lang[4];
            label_Par_Vbat_UV_First.Text = s_Lang[5];
            label_Par_CHG_OC_First.Text = s_Lang[6];
            label_Par_DSG_OC_First.Text = s_Lang[7];
            label_Par_Cellchg_OT_First.Text = s_Lang[8];
            label_Par_Cellchg_UT_First.Text = s_Lang[9];
            label_Par_Celldsg_OT_First.Text = s_Lang[10];
            label_Par_Celldsg_UT_First.Text = s_Lang[11];
            label_Par_Vdelta_Op_First.Text = s_Lang[12];
            label574.Text = s_Lang[13];
            label_Par_Tmos_OTP_First.Text = s_Lang[14];
            label_Par_Soc_Up_First.Text = s_Lang[15];

            label_Par_Vcell_OV_Second.Text = s_Lang[0];
            label_Par_Vcell_UV_Second.Text = s_Lang[1];
            label_Par_Vpack_OV_Second.Text = s_Lang[2];
            label_Par_Vpack_OV_Second.Text = s_Lang[3];
            label_Par_Vbat_OV_Second.Text = s_Lang[4];
            label_Par_Vbat_UV_Second.Text = s_Lang[5];
            label_Par_CHG_OC_Second.Text = s_Lang[6];
            label_Par_DSG_OC_Second.Text = s_Lang[7];
            label_Par_Cellchg_OT_Second.Text = s_Lang[8];
            label_Par_Cellchg_UT_Second.Text = s_Lang[9];
            label_Par_Celldsg_OT_Second.Text = s_Lang[10];
            label_Par_Celldsg_UT_Second.Text = s_Lang[11];
            label_Par_Vdelta_Op_Second.Text = s_Lang[12];
            label574.Text = s_Lang[13];
            label_Par_Tmos_OTP_Second.Text = s_Lang[14];
            label_Par_Soc_Up_Second.Text = s_Lang[15];

            label_Par_Vcell_OV_Third.Text = s_Lang[0];
            label_Par_Vcell_UV_Third.Text = s_Lang[1];
            label_Par_Vpack_OV_Third.Text = s_Lang[2];
            label_Par_Vpack_OV_Third.Text = s_Lang[3];
            label_Par_Vbat_OV_Third.Text = s_Lang[4];
            label_Par_Vbat_UV_Third.Text = s_Lang[5];
            label_Par_CHG_OC_Third.Text = s_Lang[6];
            label_Par_DSG_OC_Third.Text = s_Lang[7];
            label_Par_Cellchg_OT_Third.Text = s_Lang[8];
            label_Par_Cellchg_UT_Third.Text = s_Lang[9];
            label_Par_Celldsg_OT_Third.Text = s_Lang[10];
            label_Par_Celldsg_UT_Third.Text = s_Lang[11];
            label_Par_Vdelta_Op_Third.Text = s_Lang[12];
            label574.Text = s_Lang[13];
            label_Par_Tmos_OTP_Third.Text = s_Lang[14];
            label_Par_Soc_Up_Third.Text = s_Lang[15];

            groupBox_Par_FirstPro.Text = s_Lang[16];
            groupBox_Par_SecondPro.Text = s_Lang[17];
            groupBox_Par_ThirdPro.Text = s_Lang[18];
        }

        private void SeriesParallelPage_System_Status_Init(string[] s_Lang)
        {
            label_Par_Pre_MOS.Text = s_Lang[0];
            label_Par_CHG_MOS.Text = s_Lang[1];
            label_Par_DSG_MOS.Text = s_Lang[2];
            label_Par_AFE1.Text = s_Lang[3];
            label_Par_AFE2.Text = s_Lang[4];
            label_Par_Heat.Text = s_Lang[5];
            label_Par_Cool.Text = s_Lang[6];
            label_Par_Pre_Relay.Text = s_Lang[7];
            label_Par_Main_Relay.Text = s_Lang[8];
            label_Par_CHG_Relay.Text = s_Lang[9];
            label_Par_DSG_Relay.Text = s_Lang[10];
            label_Par_BMS_StartUp.Text = s_Lang[11];
            label_Par_Balance.Text = s_Lang[12];
            label_Par_ToSleep.Text = s_Lang[13];
        }
        #endregion

        #region MsgInfo
        private void LangSelection_MsgInfo()
        {
            if (0 == Lang.b_LangFlag)
            { //中文 
                s_MsgInfo = Lang.s_MsgInfo_Ch;
            }
            else
            {
                s_MsgInfo = Lang.s_MsgInfo_En;
            }
        }
        #endregion MsgInfo
    }

    public partial class Lang
    {
        static public byte b_LangFlag = 0;  //0：中文

        #region 语言成员变量
        #region 电池信息页
        //        static public string[] s_InformationPage_Ch = { "恒创兴电子PC软件工具", "电池信息",
        static public string[] s_InformationPage_Ch = { "32串通用上位机", "电池信息",
        "端口", "波特率", "选择驱动:",
        "MOS分口方案", "MOS同口方案", "Relay分口方案", "Relay同口方案",
        "写入", "系统关键信息", "当前驱动策略:", "当前日志状态:",
        "IO口控制","校准页面1","校准页面2","保护参数","其他参数","蓝牙Wifi","串并机信息","串并机参数"};
        //        static public string[] s_InformationPage_En = { "BesTech Power Software PC Utility (Windows)", "Information",
        static public string[] s_InformationPage_En = { "32SeriesComUpper", "Information",
        "Port", "BaudRate", "Strategy:",
        "MOS_Diff_Gate", "MOS_Same_Gate", "Relay_Diff_Gate", "Relay_Same_Gate",
        "Set", "System_Key_Information", "Present Strategy:", "Present Log Status:",
        "SwitchCtrl","Calibration1","Calibration2","Protection","OtherElement","BlueT_Wifi","Parallel","ParallelElement"};

        static public string[] s_Admin_Ch = { "管理员密码:", "确定" };
        static public string[] s_Admin_En = { "AdminPassword:", "OK" };

        static public string[] s_FunSelect_Ch = { "开启", "激活", "关闭", "休眠",
        "BMS功能选项", "功能:", "控制:", "状态:", "MOS_Relay:", "继电器:", "SOC固定:",
        "SOC清零:", "加热功能:", "冷凝功能:", "均衡功能:", "AFE1功能:", "限流模块:",
        "休眠功能:", "SOC设置:", "设置"};
        static public string[] s_FunSelect_En = { "Open", "Act", "Close", "Sleep",
        "BMS_Function_Select", "Function:", "Ctrl:", "Status:", "MOS_Relay:", "Relay:",
        "SOCFixed:", "SOCClear:", "Heat:", "Cool:", "Balance:", "AFE1:", "CurLimit:",
        "Sleep:", "SetSoc:", "Set"};

        static public string[] s_SystemError_Ch = { "实时信息", "系统提示",
         "AFE1通讯错误:", "AFE2通讯错误:", "CAN通讯错误:", "E2P通讯错误:", "SPI通讯错误:",
         "上位机通讯错误:", "客户机通讯错误:", "显示屏通讯错误:", "Wifi通讯错误:", "蓝牙通讯错误:",
         "APP通讯错误:", "E2P存储错误:", "HSE错误:", "LSE错误:", "压差过大错误:", "均衡次数统计:",
         "ADC错误:", "加热错误:", "冷凝错误:", "SOC校准:", "热偶探头断线:", "保留6:", "充电短路:", "放电短路:"};
        static public string[] s_SystemError_En = { "Read_imformation", "SYSTEM_ERROR",
         "ComError_AFE1:", "ComError_AFE2:", "ComError_Can:", "ComError_E2P:", "ComError_SPI:",
         "ComError_Upper:", "ComErr_Client1:", "ComError_Screen:", "ComError_Wifi:", "ComError_BlueT:",
         "ComError_App:", "Error_StoreE2P:", "Error_HSE:", "Error_LSE:", "Error_VdeltaOv:", "Cnt_Balanced:",
         "Error_ADC:", "Error_Heat:", "Error_Cool:", "Soc_Cail:", "TempBreak:", "Res6:", "Error_CBC_CHG:", "Error_CBC_DSG:"};

        static public string[] s_PresentProtectionRecords_Ch = {"当前保护记录",
        "一级保护记录:", "二级保护记录:", "三级保护记录:", "清除", "详情"};
        static public string[] s_PresentProtectionRecords_En = {"PresentProtectionRecords",
        "PrimaryRecord:", "SecondaryRecord", "TertiaryRecord:", "Clear", "Detail"};

        static public string[] s_HeatCoolStatus_Ch = {"BMS配件状态",
        "加热状态:", "加热错误:", "限流状态:", "限流错误:",
        "保留1:", "保留2:", "保留3:",
        "冷凝状态:", "冷凝错误:", "电枪在线:", "负载在线:",
        "保留1:", "保留2:", "保留3:",};
        static public string[] s_HeatCoolStatus_En = { "BMS_Parts_Status",
        "Heat_OnOFF:", "Heat_Err:", "CurLimit:", "CurLimitErr:",
        "Res1:", "Res2:", "Res3:",
        "Cool_OnOFF:", "Cool_Err:", "StunGun:", "Load:",
        "Res1:", "Res2:", "Res3:",};

        static public string[] s_CellVoltage_Ch = { "电芯电压_AFE1(mV)", "电芯电压_AFE2(mV)",
        "均衡状态_AFE1(Y/N)", "均衡状态_AFE2(Y/N)"};
        static public string[] s_CellVoltage_En = { "Vcell_AFE1(mV)", "Vcell_AFE2(mV)",
        "Bn_AFE1(Y/N)", "Bn_AFE2(Y/N)"};

        static public string[] s_Protection_Ch = { "单节过压:", "单节低压:", "总压过压:", "总压低压:",
        "充电过流:", "放电过流:", "充电过温:", "充电低温:", "放电过温:", "放电低温:", "压差过大:",
        "MOS过温:", "SOC过低:", "预留:", "一级安全状态", "二级安全状态", "三级安全状态" };
        static public string[] s_Protection_En = { "Vcell_OV:", "Vcell_UV:", "Vbat_OV:", "Vbat_UV:",
        "Chg_OC:", "Dsg_OC:", "Chg_OT:", "Chg_UT:", "Dsg_OT:", "Dsg_UT:", "Vdelta_OP:",
        "Tmos_OT:", "SOC_Low_P:", "Res:", "PrimarySafetyStatus", "SecondarySafetyStatus", "TertiarySafetyStatus" };

        static public string[] s_VcellStatistics_Ch = { "电压统计",
        "电芯最高压:", "电芯最低压:", "最高压电芯位号:", "最低压电芯位号:", "压差(mV):", "总压(V):"};
        static public string[] s_VcellStatistics_En = { "Vcell_Statistics",
        "Vcell_max:", "Vcell_min:", "Max_position:", "Min_position:", "Vdelta(mV):", "Vbat(V):"};

        static public string[] s_Temperature_Ch = { "温度(℃)",
        "温度1:", "温度2:", "温度3:", "温度4:", "温度5:", "温度6:",
        "环境温度1:", "环境温度2:", "环境温度3:",
        "MOS温度:", "最高温度:", "最低温度:",};
        static public string[] s_Temperature_En = { "Temperature(℃)",
        "Temp1:", "Temp2:", "Temp3:", "Temp4:", "Temp5:", "Temp6:",
        "TempEnv1:", "TempEnv2:", "TempEnv3:",
        "TempMOS:", "TempMax:", "TempMin:",};

        static public string[] s_ComprehensiveInfo_Ch = { "电流_容量_保护标志",
        "充电电流(A):", "放电电流(A):", "一级保护:", "二级保护:", "三级保护:", "加热_冷凝错误:",
        "SOC(%):", "SOH(%):", "剩余容量(mAh):", "满电容量(mAh):", "出厂容量(mAh):", "循环次数:" };
        static public string[] s_ComprehensiveInfo_En = { "Current_SOC_HeatCool_FaultFlag",
        "Ichg(A):", "Idsg(A):", "FaultFirst:", "FaultSecond:", "FaultThird:", "HeatCoolErr:",
        "SOC(%):", "SOH(%):", "Res_mAh:", "Full_mAh:", "Fac_mAh:", "CycTime:" };

        static public string[] s_SystemStatus_Ch = { "系统状态",
        "预充MOS:", "充电MOS:", "放电MOS:", "AFE1:", "AFE2:", "加热:", "冷凝:",
        "预充继电器:", "主继电器:", "充电继电器:", "放电继电器:", "预留1:", "预留2:", "系统锁定:",
        "BMS启动:", "均衡:", "休眠功能:", "预留4:", "预留5:", "预留6:", "AFE_C:" };
        static public string[] s_SystemStatus_En = { "System_Status",
        "Pre_MOS:", "CHG_MOS:", "DSG_MOS:", "AFE1:", "AFE2:", "Heat:", "Cool:",
        "Pre_Relay:", "Main_Relay:", "CHG_Relay:", "DSG_Relay:", "Res1:", "Res2:", "Sys_Limits:",
        "BMS_StartUp:", "Balance:", "SleepFunc:", "Res4:", "Res5:", "Res6:", "AFE_C:" };

        public static string[] s_FaultWarnName_Ch = { "NA",
            "一级单节过压", "一级单节低压", "一级总电压过压", "一级总电压低压", "一级充电过流", "一级放电过流",
            "一级充电过温", "一级充电低温", "一级放电过温", "一级放电低温", "一级Mos过温", "一级压差过大", "一级电量过低",
            "二级单节过压", "二级单节低压", "二级总电压过压", "二级总电压低压", "二级充电过流", "二级放电过流",
            "二级充电过温", "二级充电低温", "二级放电过温", "二级放电低温", "二级Mos过温", "二级压差过大", "二级电量过低",
            "三级单节过压", "三级单节低压", "三级总电压过压", "三级总电压低压", "三级充电过流", "三级放电过流",
            "三级充电过温", "三级充电低温", "三级放电过温", "三级放电低温", "三级Mos过温", "三级压差过大", "三级电量过低"
        };
        public static string[] s_FaultWarnName_En = { "NA",
            "PrimaryCellOV", "PrimaryCellUV", "PrimaryBatOV", "PrimaryBatUV", "PrimaryChgOC", "PrimaryDsgOC",
            "PrimaryChgOT", "PrimaryChgUT", "PrimaryDsgOT", "PrimaryDsgUT", "PrimaryMosOT", "PrimaryVDeltaOV", "PrimarySocUP",
            "SecondaryCellOV", "SecondaryCellUV", "SecondaryBatOV", "SecondaryBatUV", "SecondaryChgOC", "SecondaryDsgOC",
            "SecondaryChgOT", "SecondaryChgUT", "SecondaryDsgOT", "SecondaryDsgUT", "SecondaryMosOT", "SecondaryVDeltaOV", "SecondarySocUP",
            "TertiaryCellOV", "TertiaryCellUV", "TertiaryBatOV", "TertiaryBatUV", "TertiaryChgOC", "TertiaryDsgOC",
            "TertiaryChgOT", "TertiaryChgUT", "TertiaryDsgOT", "TertiaryDsgUT", "TertiaryMosOT", "TertiaryVDeltaOV", "TertiarySocUP",
        };
#endregion 电池信息页

        #region 保护参数页
        static public string[] s_ProtectionPage_Ch = { "保护参数", "读取", "设置" };
        static public string[] s_ProtectionPage_En = { "Protection", "Read", "Set" };

        static public string[] s_ProtectElement_Ch = { "保护点",
        "一级报警:", "二级报警:", "三级保护:", "保护恢复:", "滤波(10ms):",
        "单节过压(mV):", "单节低压(mV):", "总压过压(V):", "总压低压(V):", "充电过流(A):", "放电过流(A):",
        "充电过温:", "充电低温:", "放电过温:", "放电低温:",
        "MOS过温:", "压差过大(mV):", "电量过低(%):", "保护点重置"};
        static public string[] s_ProtectElement_En = { "Protect_Element",
        "Primary:", "Secondary:", "Tertiary:", "Rec(Third):", "DelayT(10ms):",
        "Vcell_OVP(mV):", "Vcell_UVP(mV):", "Vbus_OVP(V):", "Vbus_UVP(V):", "Ichg_OCP(A):", "Idsg_OCP(A):",
        "Tcell_ChgOTP:", "Tcell_ChgUTP:", "Tcell_DsgOTP:", "Tcell_DsgUTP:",
        "Tmos_OTP:", "VdeltaOvp(mV):", "CellSocUp(%):", "Reset_Pro"};

        static public string[] s_OtherParameters_Ch = { "其它参数", "其他参数重置",
        "SOC参数", "容量(Ah):", "循环次数:", "SOC_100电压:", "SOC_0电压:",
        "系统参数", "串数:", "采样电阻(mΩ):", "采样电阻数量:", "预充时间(s):",
        "休眠参数", "正常休眠电压(mV):", "正常休眠时间(min):", "过放休眠电压(mV):", "过放休眠时间(min):",
        "充电电流过滤(A):", "放电电流过滤(A):", "RTC唤醒时间(min):", "RTC休眠时间(min):",
        "均衡参数(mV)", "开启电压:", "开启窗口:", "关闭窗口:", "保留位1:",
        "保留位2:", "保留位3:", "保留位4:", "保留位5:",
        "短路参数", "充电短路范围(A):", "放电短路范围(A):", "实际短路电流(A):", "短路延时(us):",
        "SOC曲线选择:", "永久密码:", "限流压差(mV):", "限流电流(A):"};
        static public string[] s_OtherParameters_En = { "OtherCanAdd_Element", "Reset_CanAdd",
        "Soc_Element", "Cap(Ah):", "CycleTime:", "SOC_100_mV:", "SOC_0_mV:",
        "System_Element", "SeriesNum:", "CS_Res(mΩ):", "CSResNum:", "PreChg_T(s):",
        "Sleep_Element", "V_Nor(mV):", "T_Nor(min):", "V_low(mV):", "T_Low(min):",
        "VirCurChg(A):", "VirCurDsg(A):", "RTC_WT(min):", "RTC_ST(min):",
        "Balance(mV)", "Open_Vol:", "Open_Win:", "Close_Win:", "Res1:",
        "Res2:", "Res3:", "Res4:", "Res5:",
        "ShortCur_Par", "ShortCHG_MAX(A):", "ShortDSG_MAX(A):", "CBC_CurNow(A):", "CBC_DelayT(us):",
        "SOC_TabelSel:", "PassW_Forever:", "CurMod_Vdel(mV):", "CurMod_Cur(A):"};

        static public string[] s_HeatCool_Ch = { "温控参数", "温控参数(℃)",
        "加热开启温度:", "加热关闭温度:", "加热开启电流:",
        "冷凝开启温度:", "冷凝关闭温度:", "保留1:",
        "保留2:", "保留3:", "保留4:",
        "保留5:", "保留6:", "保留7:", "保留8:",
        "保留参数", "保留9:", "保留10:", "保留11:", "保留12:",
        "保留13:", "保留14:", "保留15:",
        "保留16:", "保留17:", "保留18:", "保留19:",
        "温控参数重置"};
        static public string[] s_HeatCool_En = { "HeatCool_Element", "Element(℃)",
        "Heat_OpenT:", "Heat_CloseT:", "Heat_OpenCur:",
        "Cool_OpenT:", "Cool_CloseT:", "Res1:",
        "Res2:", "Res3:", "Res4:",
        "Res5:", "Res6:", "Res7:", "Res8:",
        "Element", "Res9:", "Res10:", "Res11:", "Res12:",
        "Res13:", "Res14:", "Res15:",
        "Res16:", "Res17:", "Res18:", "Res19:",
        "Reset_HeatCool"};

        static public string[] s_BatchProcess_Ch = { "批量处理", "导入", "导出", "浏览..", "一键读", "一键写" };
        static public string[] s_BatchProcess_En = { "BatchProcess", "Load", "Export", "Browse..", "ReadAll", "SetAll" };
#endregion 保护参数页

        #region 其他参数页
        static public string[] s_OtherElementPage_Ch = { "其他功能", "读取", "设置",
        "SOC表格设置", "电芯电压(mV)", "SOC值(%)",
        "铜损补偿", "铜损(uΩ)", "电芯位号",
        "如果'电芯位号'为0，则这一行数据会被忽略",
        "历史保护记录", "清除" };
        static public string[] s_OtherElementPage_En = { "OtherElement", "Read", "Set",
        "SOC_Table", "CellVoltage(mV)", "SOC(%)",
        "CopperLossCompensation", "CopperLoss(uΩ)", "CellNum",
        "if 'CellNum' equals 0, the row data will be ignored.",
        "HistoricalProtectionRecord", "Clear" };

        static public string[] s_Upgrade_Ch = { "固件升级",
        "升级包:", "浏览..", "连接设备", "开始升级", "清除", "升级进度：" };
        static public string[] s_Upgrade_En = { "FirmwareUpdate",
        "Upgrate:", "Browse..", "Connect", "Update", "Clear", "Schedule:" };

        static public string[] s_LogRecord_Ch = { "日志记录",
        "记录间隔(s):", "开始", "停止", "日志文件名:" };
        static public string[] s_LogRecord_En = { "LogRecord",
        "RecordGaps(s):", "Start", "Stop", "LogFileName:" };
        #endregion 其他参数页

        #region 串机页面
        static public string[] s_Vcell_Statistics_Ch = { "总电压(V):", "包间压差(V):", "单包最大电压(V):", "单包最小电压(V):", "单包最大位置:", "单包最小位置:" ,
                                                         "单节最大电压(mV):", "单节最小电压(mV):", "单节最大位置:", "单节最小位置:", "包内压差最大(mV):", "包内压差最小(mV):" ,
                                                         "包内压差最大位置:", "包内压差最小位置:" , "温度最高:", "温度最低:", "温度最高位置:", "温度最低位置:", 
                                                         "MOS温度最高:", "MOS温度最低:", "MOS温度最高位置:", "MOS温度最低位置:", "总包单节压差(mV):"};
        static public string[] s_Vcell_Statistics_En = { "VbatSum(V):", "VpackDelta(V):", "VpackMax(V):", "VpackMin(V):", "Max_position:", "Min_position:",
                                                         "Vcell_Max:", "Vcell_Min:", "Max_position:", "Min_position:", "VcellDeltaMax(mV):", "VcellDeltaMin:",
                                                         "Max_position:", "Min_position", "TempMax(℃):", "TempMin(℃):", "Max_position:", "Min_position",
                                                         "TempMosMax(℃):", "TempMosMin(℃):", "Max_position:", "Min_position:", "VcellDeltaAll(mV):"};

        static public string[] s_Current_SOC_HeatCool_FaultFlag_Ch = { "充电电流(A):", "放电电流(A):", "一级告警:", "二级告警:", "三级保护:", "加热冷凝错误:",
                                                                       "SOC(%):", "SOH(%):", "剩余容量(mAh):", "满电容量(mAh):", "出厂容量(mAh):", "循环次数:"};
        static public string[] s_Current_SOC_HeatCool_FaultFlag_En = { "IchgSum(A):", "IdsgSum(A):", "WarnFirst:", "WarnSecond:", "FaultThird:", "HeatCoolErr:",
                                                                       "SOC(%):", "SOH(%):", "Res_mAh:", "Full_mAh:", "Fac_mAh:", "CycTimes:"};

        static public string[] s_SafetyStatus_Ch = { "单节过压:", "单节低压:", "单包过压:", "单包低压:","总压过压:", "总压低压:",
                                                     "充电过流:", "放电过流:", "充电过温:", "充电低温:", "放电过温:", "放电低温:",
                                                     "压差过大:","保留位:", "MOS过温:", "SOC过低:", "一级告警状态", "二级告警状态", "三级保护状态" };
        static public string[] s_SafetyStatus_En = { "Vcell_OV:", "Vcell_UV:", "Vpack_OV:", "Vpack_UV:", "Vbat_OV:", "Vbat_UV:",
                                                     "Chg_OC:", "Dsg_OC:", "Chg_OT:", "Chg_UT:", "Dsg_OT:", "Dsg_UT:",
                                                     "Vdelta_OP:", "Res:", "Tmos_OT:", "SOC_Low_P:", "PrimaryWarnStatus", "SecondaryWarnStatus", "TertiarySafetyStatus" };

        static public string[] s_ParallelSystem_Ch = { "预充MOS:", "充电MOS:", "放电MOS:", "前端1状态:", "前端2状态:", "加热状态:", "冷凝状态:",
                                                       "预充接触器:", "主接触器:", "充电接触器:", "放电接触器:", "BMS开机状态:", "均衡状态:", "休眠功能:"};
        static public string[] s_ParallelSystem_En = { "Pre_MOS:", "CHG_MOS:", "DSG_MOS:", "AFE1:", "AFE2:", "Heat:", "Cool:",
                                                       "Pre_Relay:", "Main_Relay:", "CHG_Relay:", "DSG_Relay:", "BMS_StartUp:", "Balance:", "SleepFunc:"};

        static public string[] s_OtherElement_Ch = { "电压:", "通讯错误:"};
        static public string[] s_OtherElement_En = { "Volt:", "ComErr:" };

        #endregion 串机页面

        #region 表单验证提示
        static public string[] s_Verification_Ch = {
            "第 "," 个数据为空，请输入完整信息！",
            "第 ", " 个数据超出处理范围，最多5位整数，", "位小数！",
            "第 ", " 个数据的范围为 ", "，数据越界！",
            "第 ", " 个数据不能", "大于", "小于", "第 ", " 个数据",
        };
        static public string[] s_Verification_En = {
            "The ", "(st,nd,rd,th) Data Is Empty. Please Enter All Information!",
            "The ", "(st,nd,rd,th) Data Can't Be Processed. 5 Integers, ", " decimal places at most!",
            "The Range Of The ", "(st,nd,rd,th) Data Is ", ", Data Is Out Of Range!",
            "The ", "(st,nd,rd,th) Data Can't ", "Greater Than ", "Less Than ", "The ", "(st,nd,rd,th) Data!",
        };
#endregion 表单验证提示

        #region MsgInfo
        public static string[] s_MsgInfo_Ch = { "读取失败！", "重置失败！", "设置失败！", "操作失败！",
            "串口未打开!", "串口错误，请检查串口是否打开！",
        };
        public static string[] s_MsgInfo_En = { "ReadFailed!", "ResetFailed!", "Set Failed!", "Operation failed!",
            "Series Port Is Closed!", "Serial Port Error, Please Recheck Serial Port!",
        };
        #endregion MsgInfo

        #endregion 语言成员变量

        public enum MsgInfo
        {
            ReadFailed = 0,
            ResetFailed,
            SetFailed,
            OperationFailed,
            PortClosed,
            PortError,
        }
    }
}
