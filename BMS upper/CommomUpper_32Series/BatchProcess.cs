using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using System.Windows.Forms;
using myExcel = Microsoft.Office.Interop.Excel;

namespace CommomUpper_32Series
{
    public partial class Form1
    {

        byte by_BatchProcessFlag = 0;

        List<Control> parameterExportContentList = new List<Control>();
        List<String> parameterExportNameList = new List<String>();

        enum ParaType
        {
            AFE_PARAMETER,
            DEFAULT_PARAMETER,
        }

        void addTextBoxToList(ParaType type)
        {
            List<Control> AFE_parameter_element_list = new List<Control>();
            List<Control> protect_element_list = new List<Control>();
            List<Control> otherCanAdd_element_list = new List<Control>();
            List<Control> short_cur_element_list = new List<Control>();
            List<Control> heatCool_element_list = new List<Control>();

            if (parameterExportContentList.Count > 0)
            {
                parameterExportContentList.Clear();
            }

            #region  AFE_Protect_Element
            //过压
            AFE_parameter_element_list.Add(comboBox_AFE_VcellOvp);
            AFE_parameter_element_list.Add(comboBox_AFE_VcellOvp_Rcv);
            AFE_parameter_element_list.Add(comboBox_AFE_VcellOvp_Filter);

            //低压
            AFE_parameter_element_list.Add(comboBox_AFE_VcellUvp);
            AFE_parameter_element_list.Add(comboBox_AFE_VcellUvp_Rcv);
            AFE_parameter_element_list.Add(comboBox_AFE_VcellUvp_Filter);

            //充电过流
            AFE_parameter_element_list.Add(textBox_AFE_IchgOcp_First);
            AFE_parameter_element_list.Add(textBox_AFE_IchgOcp_Filter_First);
            AFE_parameter_element_list.Add(comboBox_AFE_IchgOcp_Second);
            AFE_parameter_element_list.Add(comboBox_AFE_IchgOcp_Filter_Second);

            //放电过流
            AFE_parameter_element_list.Add(textBox_AFE_IdsgOcp_First);
            AFE_parameter_element_list.Add(textBox_AFE_IdsgOcp_Filter_First);
            AFE_parameter_element_list.Add(comboBox_AFE_IdsgOcp_Second);
            AFE_parameter_element_list.Add(comboBox_AFE_IdsgOcp_Filter_Second);

            //充电高温低温
            AFE_parameter_element_list.Add(comboBox_AFE_TChgOTp);
            AFE_parameter_element_list.Add(comboBox_AFE_TChgOTp_Rcv);
            AFE_parameter_element_list.Add(comboBox_AFE_TchgUTp);
            AFE_parameter_element_list.Add(comboBox_AFE_TchgUTp_Rcv);

            //放电高温低温
            AFE_parameter_element_list.Add(comboBox_AFE_TdischgOTp);
            AFE_parameter_element_list.Add(comboBox_AFE_TdischgOTp_Rcv);
            AFE_parameter_element_list.Add(comboBox_AFE_TdischgUTp);
            AFE_parameter_element_list.Add(comboBox_AFE_TdischgUTp_Rcv);

            //短路电流延时
            AFE_parameter_element_list.Add(comboBox_AFE_CBC_Cur_DSG);
            AFE_parameter_element_list.Add(comboBox_AFE_CBC_DelayT);
            #endregion

            #region Protect_element

            //单节过压
            protect_element_list.Add(textBox_VcellOVP_First);
            protect_element_list.Add(textBox_VcellOVP_Second);
            protect_element_list.Add(textBox_VcellOVP_Third);
            protect_element_list.Add(textBox_VcellOVP_Rec);
            protect_element_list.Add(textBox_VcellOVP_DelayT);

            //单节抵压
            protect_element_list.Add(textBox_VcellUVP_First);
            protect_element_list.Add(textBox_VcellUVP_Second);
            protect_element_list.Add(textBox_VcellUVP_Third);
            protect_element_list.Add(textBox_VcellUVP_Rec);
            protect_element_list.Add(textBox_VcellUVP_DelayT);

            //总压过压
            protect_element_list.Add(textBox_VbusOVP_First);
            protect_element_list.Add(textBox_VbusOVP_Second);
            protect_element_list.Add(textBox_VbusOVP_Third);
            protect_element_list.Add(textBox_VbusOVP_Rec);
            protect_element_list.Add(textBox_VbusOVP_DelayT);

            //总压低压
            protect_element_list.Add(textBox_VbusUVP_First);
            protect_element_list.Add(textBox_VbusUVP_Second);
            protect_element_list.Add(textBox_VbusUVP_Third);
            protect_element_list.Add(textBox_VbusUVP_Rec);
            protect_element_list.Add(textBox_VbusUVP_DelayT);

            //充电过流
            protect_element_list.Add(textBox_IchgOCP_First);
            protect_element_list.Add(textBox_IchgOCP_Second);
            protect_element_list.Add(textBox_IchgOCP_Third);
            protect_element_list.Add(textBox_IchgOCP_Rec);
            protect_element_list.Add(textBox_IchgOCP_DelayT);

            //放电过流
            protect_element_list.Add(textBox_IdsgOCP_First);
            protect_element_list.Add(textBox_IdsgOCP_Second);
            protect_element_list.Add(textBox_IdsgOCP_Third);
            protect_element_list.Add(textBox_IdsgOCP_Rec);
            protect_element_list.Add(textBox_IdsgOCP_DelayT);

            //充电过温
            protect_element_list.Add(textBox_TchgOTP_First);
            protect_element_list.Add(textBox_TchgOTP_Second);
            protect_element_list.Add(textBox_TchgOTP_Third);
            protect_element_list.Add(textBox_TchgOTP_Rec);
            protect_element_list.Add(textBox_TchgOTP_DelayT);

            //充电低温
            protect_element_list.Add(textBox_TchgUTP_First);
            protect_element_list.Add(textBox_TchgUTP_Second);
            protect_element_list.Add(textBox_TchgUTP_Third);
            protect_element_list.Add(textBox_TchgUTP_Rec);
            protect_element_list.Add(textBox_TchgUTP_DelayT);

            //放电过温
            protect_element_list.Add(textBox_TdsgOTP_First);
            protect_element_list.Add(textBox_TdsgOTP_Second);
            protect_element_list.Add(textBox_TdsgOTP_Third);
            protect_element_list.Add(textBox_TdsgOTP_Rec);
            protect_element_list.Add(textBox_TdsgOTP_DelayT);

            //放电低温
            protect_element_list.Add(textBox_TdsgUTP_First);
            protect_element_list.Add(textBox_TdsgUTP_Second);
            protect_element_list.Add(textBox_TdsgUTP_Third);
            protect_element_list.Add(textBox_TdsgUTP_Rec);
            protect_element_list.Add(textBox_TdsgUTP_DelayT);

            //mos过温
            protect_element_list.Add(textBox_TmosOTP_First);
            protect_element_list.Add(textBox_TmosOTP_Second);
            protect_element_list.Add(textBox_TmosOTP_Third);
            protect_element_list.Add(textBox_TmosOTP_Rec);
            protect_element_list.Add(textBox_TmosOTP_DelayT);

            //压差过大
            protect_element_list.Add(textBox_VdeltaOVP_First);
            protect_element_list.Add(textBox_VdeltaOVP_Second);
            protect_element_list.Add(textBox_VdeltaOVP_Third);
            protect_element_list.Add(textBox_VdeltaOVP_Rec);
            protect_element_list.Add(textBox_VdeltaOVP_DelayT);

            //soc保护
            protect_element_list.Add(textBox_SocUp_First);
            protect_element_list.Add(textBox_SocUp_Second);
            protect_element_list.Add(textBox_SocUp_Third);
            protect_element_list.Add(textBox_SocUp_Rec);
            protect_element_list.Add(textBox_SocUp_DelayT);


            #endregion Protect_element

            #region OtherCanAdd_Element
            //soc element
            otherCanAdd_element_list.Add(textBox_Soc_Ah);
            otherCanAdd_element_list.Add(textBox_Soc_CycleTime);
            otherCanAdd_element_list.Add(textBox_Soc_V_100);
            otherCanAdd_element_list.Add(textBox_Soc_V_0);

            //system element
            otherCanAdd_element_list.Add(textBox_Sys_SeriesNum);
            otherCanAdd_element_list.Add(textBox_Sys_CSRes);
            otherCanAdd_element_list.Add(textBox_Sys_CSRes_Num);
            otherCanAdd_element_list.Add(textBox_Sys_PreChg_Time);

            //sleep element
            otherCanAdd_element_list.Add(textBox_SleepNormalV);
            otherCanAdd_element_list.Add(textBox_SleepNormalT);
            otherCanAdd_element_list.Add(textBox_SleepOverDsgV);
            otherCanAdd_element_list.Add(textBox_SleepOverDsgT);
            otherCanAdd_element_list.Add(textBox_SleepVirCur_Chg);
            otherCanAdd_element_list.Add(textBox_SleepVirCur_Dsg);
            otherCanAdd_element_list.Add(textBox_SleepRTC_WakeUpT);
            otherCanAdd_element_list.Add(textBox_SleepRes);

            //balance element
            otherCanAdd_element_list.Add(textBox_openV);
            otherCanAdd_element_list.Add(textBox_openW);
            otherCanAdd_element_list.Add(textBox_CloseWin);
            otherCanAdd_element_list.Add(textBox_Balance_Res1);
            otherCanAdd_element_list.Add(textBox_Balance_Res2);
            otherCanAdd_element_list.Add(textBox_Balance_Res3);
            otherCanAdd_element_list.Add(textBox_Balance_Res4);
            otherCanAdd_element_list.Add(textBox_Balance_Res5);


            #endregion OtherCanAdd_Element

            #region  short_cur_element
            //short_cur_element
            short_cur_element_list.Add(textBox_CS_CurCHG);
            short_cur_element_list.Add(textBox_CS_CurDSG);
            short_cur_element_list.Add(textBox_CBC_CurDSG);
            short_cur_element_list.Add(textBox_CBC_DelayT);
            short_cur_element_list.Add(textBox_Soc_TableSelect);
            short_cur_element_list.Add(textBox_Password_Forever);
            short_cur_element_list.Add(textBox_CurLimit_Vdel);
            short_cur_element_list.Add(textBox_CurLimit_Cur);
            #endregion

            #region HeatCool


            heatCool_element_list.Add(textBox_Heat_OpenT);
            heatCool_element_list.Add(textBox_Heat_CloseT);
            heatCool_element_list.Add(textBox_Heat_OpenCur);
            heatCool_element_list.Add(textBox_Cool_OpenT);
            heatCool_element_list.Add(textBox_Cool_CloseT);
            heatCool_element_list.Add(textBox_Res1);
            heatCool_element_list.Add(textBox_Res2);
            heatCool_element_list.Add(textBox_Res3);
            heatCool_element_list.Add(textBox_Res4);
            heatCool_element_list.Add(textBox_Res5);
            heatCool_element_list.Add(textBox_Res6);
            heatCool_element_list.Add(textBox_Res7);
            heatCool_element_list.Add(textBox_Res8);

            heatCool_element_list.Add(textBox_Res9);
            heatCool_element_list.Add(textBox_Res10);
            heatCool_element_list.Add(textBox_Res11);
            heatCool_element_list.Add(textBox_Res12);
            heatCool_element_list.Add(textBox_Res13);
            heatCool_element_list.Add(textBox_Res14);
            heatCool_element_list.Add(textBox_Res15);
            heatCool_element_list.Add(textBox_Res16);
            heatCool_element_list.Add(textBox_Res17);
            heatCool_element_list.Add(textBox_Res18);
            heatCool_element_list.Add(textBox_Res19);

            #endregion HeatCool

            if (type == ParaType.AFE_PARAMETER)
            {
                parameterExportContentList.AddRange(AFE_parameter_element_list);
                parameterExportContentList.AddRange(otherCanAdd_element_list);
                parameterExportContentList.AddRange(heatCool_element_list);
            }
            else
            {
                parameterExportContentList.AddRange(protect_element_list);
                parameterExportContentList.AddRange(otherCanAdd_element_list);
                parameterExportContentList.AddRange(heatCool_element_list);
                parameterExportContentList.AddRange(short_cur_element_list);
            }



            //protect_element_list.Clear();
            //otherCanAdd_element_list.Clear();
            //heatCool_element_list.Clear();

        }
        void addTextToList(ParaType type)
        {
            List<String> AFE_parameter_element_text_list = new List<String>();
            List<String> protect_element_text_list = new List<String>();
            List<String> otherCanAdd_element_text_list = new List<String>();
            List<String> heatCool_element_text_list = new List<String>();
            List<String> short_cur_element_text_list = new List<String>();

            if (parameterExportNameList.Count > 0)
            {
                parameterExportNameList.Clear();
            }

            #region  AFE_Protect_Element
            Label[] label_AFE_Protect_Element = new Label[24];
            //过压
            label_AFE_Protect_Element[0] = labal_VellOvp;           //label_comboBox_AFE_VcellOvp
            label_AFE_Protect_Element[1] = label22;           // label_comboBox_AFE_VcellOvp_Rcv
            label_AFE_Protect_Element[2] = label148;           //label_comboBox_AFE_VcellOvp_Filter

            //低压
            label_AFE_Protect_Element[3] = label159;           //label_comboBox_AFE_VcellUvp
            label_AFE_Protect_Element[4] = label153;           //label_comboBox_AFE_VcellUvp_Rcv
            label_AFE_Protect_Element[5] = label149;           //label_comboBox_AFE_VcellUvp_Filter

            //充电过流
            label_AFE_Protect_Element[6] = label167;           //label_textBox_AFE_IchgOcp_First
            label_AFE_Protect_Element[7] = label227;           //label_textBox_AFE_IchgOcp_Filter_First
            label_AFE_Protect_Element[8] = label162;           //label_comboBox_AFE_IchgOcp_Second
            label_AFE_Protect_Element[9] = label236;           //label_comboBox_AFE_IchgOcp_Filter_Second

            //放电过流
            label_AFE_Protect_Element[10] = label307;           //label_textBox_AFE_IdsgOcp_First
            label_AFE_Protect_Element[11] = label312;           //label_textBox_AFE_IdsgOcp_Filter_First
            label_AFE_Protect_Element[12] = label308;           //label_comboBox_AFE_IdsgOcp_Second
            label_AFE_Protect_Element[13] = label310;           //label_comboBox_AFE_IdsgOcp_Filter_Second

            //充电高温低温
            label_AFE_Protect_Element[14] = label271;           //label_comboBox_AFE_TChgOTp
            label_AFE_Protect_Element[15] = label288;           //label_comboBox_AFE_TChgOTp_Rcv
            label_AFE_Protect_Element[16] = label276;           //label_comboBox_AFE_TchgUTp
            label_AFE_Protect_Element[17] = label295;           //label_comboBox_AFE_TchgUTp_Rcv

            //放电高温低温
            label_AFE_Protect_Element[18] = label306;           //label_comboBox_AFE_TdischgOTp
            label_AFE_Protect_Element[19] = label298;           //label_comboBox_AFE_TdischgOTp_Rcv
            label_AFE_Protect_Element[20] = label297;           //label_comboBox_AFE_TdischgUTp
            label_AFE_Protect_Element[21] = label305;           //label_comboBox_AFE_TdischgUTp_Rcv

            //短路电流延时
            label_AFE_Protect_Element[22] = label314;           //label_comboBox_AFE_CBC_Cur_DSG
            label_AFE_Protect_Element[23] = label316;           //label_comboBox_AFE_CBC_DelayT

            for (int i = 0; i < label_AFE_Protect_Element.Length; i++)
            {
                AFE_parameter_element_text_list.Add(label_AFE_Protect_Element[i].Text);
            }

            #endregion

            #region Protect_element
            Label[] label = new Label[5];
            label[0] = label24; // label_first
            label[1] = label39; // label_second
            label[2] = label44; // label_third
            label[3] = label40; // label_rec
            label[4] = label_Vcell_DT; // label_delayT

            for (int i = 0; i < 5; i++)
                //单节过压
                protect_element_text_list.Add(lable_VcellOVP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //单节抵压
                protect_element_text_list.Add(label_Vcell_UVP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //总压过压
                protect_element_text_list.Add(label_Vbus_OVP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //总压低压
                protect_element_text_list.Add(label_Vbus_UVP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //充电过流
                protect_element_text_list.Add(label_Ichg_OCP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //放电过流
                protect_element_text_list.Add(label_Idsg_OCP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //充电过温
                protect_element_text_list.Add(label_Tcell_ChgOTP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //充电低温
                protect_element_text_list.Add(label_Tcell_ChgUTP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //放电过温
                protect_element_text_list.Add(label_Tcell_DsgOTP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //放电低温
                protect_element_text_list.Add(label_Tcell_DsgUTP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //mos过温
                protect_element_text_list.Add(label_Tmos_OTP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //压差过大
                protect_element_text_list.Add(label_DeltaOP.Text + label[i].Text);
            for (int i = 0; i < 5; i++)
                //soc保护
                protect_element_text_list.Add(label_SocUP.Text + label[i].Text);



            #endregion Protect_element

            #region OtherCanAdd_Element
            //soc element
            Label label_Soc_Ah = label150;
            Label label_Soc_CycleTime = label147;
            Label label_Soc_V_100 = label_SOC_V_100;
            Label label_Soc_V_0 = label_SOC_V_0;

            otherCanAdd_element_text_list.Add(label_Soc_Ah.Text);
            otherCanAdd_element_text_list.Add(label_Soc_CycleTime.Text);
            otherCanAdd_element_text_list.Add(label_Soc_V_100.Text);
            otherCanAdd_element_text_list.Add(label_Soc_V_0.Text);

            //system element
            Label label_Sys_SeriesNum = label238;
            Label label_Sys_CSRes = label_CS_Res;
            Label label_Sys_CSRes_Num = label46;
            Label label_Sys_PreChg_Time = label232;

            otherCanAdd_element_text_list.Add(label_Sys_SeriesNum.Text);
            otherCanAdd_element_text_list.Add(label_Sys_CSRes.Text);
            otherCanAdd_element_text_list.Add(label_Sys_CSRes_Num.Text);
            otherCanAdd_element_text_list.Add(label_Sys_PreChg_Time.Text);

            //sleep element
            Label label_SleepNormalV = label_NormalV;
            Label label_SleepNormalT = label_NormalT;
            Label label_SleepOverDsgV = label_OverDsgV;
            Label label_SleepOverDsgT = label_OverDsgT;
            Label label_SleepVirCur_Chg = label146;
            Label label_SleepVirCur_Dsg = label132;
            Label label_SleepRTC_WakeUpT = label122;
            Label label_SleepRes = label117;

            otherCanAdd_element_text_list.Add(label_SleepNormalV.Text);
            otherCanAdd_element_text_list.Add(label_SleepNormalT.Text);
            otherCanAdd_element_text_list.Add(label_SleepOverDsgV.Text);
            otherCanAdd_element_text_list.Add(label_SleepOverDsgT.Text);
            otherCanAdd_element_text_list.Add(label_SleepVirCur_Chg.Text);
            otherCanAdd_element_text_list.Add(label_SleepVirCur_Dsg.Text);
            otherCanAdd_element_text_list.Add(label_SleepRTC_WakeUpT.Text);
            otherCanAdd_element_text_list.Add(label_SleepRes.Text);

            //balance element
            Label label_openV = label_Open_V;
            Label label_openW = label_Open_W;
            Label label_CloseWin = label_Close_W1;
            Label label_Balance_Res1 = label_Close_W2;
            Label label_Balance_Res2 = label254;
            Label label_Balance_Res3 = label240;
            Label label_Balance_Res4 = label241;
            Label label_Balance_Res5 = label60;

            otherCanAdd_element_text_list.Add(label_openV.Text);
            otherCanAdd_element_text_list.Add(label_openW.Text);
            otherCanAdd_element_text_list.Add(label_CloseWin.Text);
            otherCanAdd_element_text_list.Add(label_Balance_Res1.Text);
            otherCanAdd_element_text_list.Add(label_Balance_Res2.Text);
            otherCanAdd_element_text_list.Add(label_Balance_Res3.Text);
            otherCanAdd_element_text_list.Add(label_Balance_Res4.Text);
            otherCanAdd_element_text_list.Add(label_Balance_Res5.Text);


            #endregion OtherCanAdd_Element

            #region short_cur_element
            //short_cur_element
            Label label_CS_CurCHG = label_DSG_high;
            Label label_CS_CurDSG = label_DSG_low;
            Label label_CBC_DelayT = label_CHG_low;
            Label label_CBC_CurDSG = label_CBC_Delay;
            Label label_Soc_TableSelect = label269;
            Label label_Password_Forever = label252;
            Label label_CurLimit_Vdel = label261;
            Label label_CurLimit_Cur = label165;

            short_cur_element_text_list.Add(label_CS_CurCHG.Text);
            short_cur_element_text_list.Add(label_CS_CurDSG.Text);
            short_cur_element_text_list.Add(label_CBC_DelayT.Text);
            short_cur_element_text_list.Add(label_CBC_CurDSG.Text);
            short_cur_element_text_list.Add(label_Soc_TableSelect.Text);
            short_cur_element_text_list.Add(label_Password_Forever.Text);
            short_cur_element_text_list.Add(label_CurLimit_Vdel.Text);
            short_cur_element_text_list.Add(label_CurLimit_Cur.Text);
            #endregion


            #region HeatCool
            Label label_Heat_OpenT = label433;
            Label label_Heat_CloseT = label281;
            Label label_Heat_OpenCur = label422;
            Label label_Cool_OpenT = label432;
            Label label_Cool_CloseT = label468;
            Label label_Res1 = label321;
            Label label_Res2 = label313;
            Label label_Res3 = label285;
            otherCanAdd_element_text_list.Add(label_Heat_OpenT.Text);
            otherCanAdd_element_text_list.Add(label_Heat_CloseT.Text);
            otherCanAdd_element_text_list.Add(label_Heat_OpenCur.Text);
            otherCanAdd_element_text_list.Add(label_Cool_OpenT.Text);
            otherCanAdd_element_text_list.Add(label_Cool_CloseT.Text);
            otherCanAdd_element_text_list.Add(label_Res1.Text);
            otherCanAdd_element_text_list.Add(label_Res2.Text);
            otherCanAdd_element_text_list.Add(label_Res3.Text);


            Label label_Res4 = label289;
            Label label_Res5 = label467;
            Label label_Res6 = label469;
            Label label_Res7 = label466;
            Label label_Res8 = label464;
            Label label_Res9 = label4441;
            Label label_Res10 = label460;
            Label label_Res11 = label457;
            otherCanAdd_element_text_list.Add(label_Res4.Text);
            otherCanAdd_element_text_list.Add(label_Res5.Text);
            otherCanAdd_element_text_list.Add(label_Res6.Text);
            otherCanAdd_element_text_list.Add(label_Res7.Text);
            otherCanAdd_element_text_list.Add(label_Res8.Text);
            otherCanAdd_element_text_list.Add(label_Res9.Text);
            otherCanAdd_element_text_list.Add(label_Res10.Text);
            otherCanAdd_element_text_list.Add(label_Res11.Text);

            Label label_Res12 = label461;
            Label label_Res13 = label443;
            Label label_Res14 = label439;
            Label label_Res15 = label441;
            Label label_Res16 = label473;
            Label label_Res17 = label465;
            Label label_Res18 = label463;
            Label label_Res19 = label471;
            otherCanAdd_element_text_list.Add(label_Res12.Text);
            otherCanAdd_element_text_list.Add(label_Res13.Text);
            otherCanAdd_element_text_list.Add(label_Res14.Text);
            otherCanAdd_element_text_list.Add(label_Res15.Text);
            otherCanAdd_element_text_list.Add(label_Res16.Text);
            otherCanAdd_element_text_list.Add(label_Res17.Text);
            otherCanAdd_element_text_list.Add(label_Res18.Text);
            otherCanAdd_element_text_list.Add(label_Res19.Text);

            #endregion HeatCool

            if (type == ParaType.AFE_PARAMETER)
            {
                parameterExportNameList.AddRange(AFE_parameter_element_text_list);
                parameterExportNameList.AddRange(otherCanAdd_element_text_list);
                parameterExportNameList.AddRange(heatCool_element_text_list);
            }
            else
            {
                parameterExportNameList.AddRange(protect_element_text_list);
                parameterExportNameList.AddRange(otherCanAdd_element_text_list);
                parameterExportNameList.AddRange(heatCool_element_text_list);
                parameterExportNameList.AddRange(short_cur_element_text_list);
            }

            //protect_element_text_list.Clear();
            //otherCanAdd_element_text_list.Clear();
            //heatCool_element_text_list.Clear();

        }
        void parameterExportAndImportInit(ParaType type)
        {
            addTextBoxToList(type);
            addTextToList(type);
        }


        //延时小函数
        public static void Delay_ms(int milliSecond)
        {
            int start = Environment.TickCount;
            while (Math.Abs(Environment.TickCount - start) < milliSecond) ;
        }

        #region 一键读取/写入
        //多线程操作函数
        public void AllProtectSetClickCall() {
            byte by_Cnt = 0;
            string s_temp = button_SetAll.Text;
            try
            {
                button_SetAll.Enabled = false;
                button_SetAll.Text = "4";   //倒计时

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_VcellOVP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_VcellUVP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_VbusOVP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_VbusUVP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_IchgOCP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_IdsgOCP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }
                button_SetAll.Text = "3";   //倒计时

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_TchgOTP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_TchgUTP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_TdsgOTP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_TdsgUTP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_TmosOTP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_VdeltaOVP_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }
                button_SetAll.Text = "2";   //倒计时

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_SocUp_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Soc_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Sys_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Sleep_Set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Balance_set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Other_Set.PerformClick();
                button_SetAll.Text = "1";   //倒计时
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_HeatCool_Set.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                if (0 == Lang.b_LangFlag)
                    MessageBox.Show("全部参数写入成功", "参数写入", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("All parameters setting succeed", "Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("全部参数写入异常", "参数写入", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                button_SetAll.Text = s_temp;
                button_SetAll.Enabled = true;
                by_BatchProcessFlag = 0;
            }
        }

        public void AllProtectReadClickCall()
        {
            byte by_Cnt = 0;
            try
            {
                button_ReadAll.Enabled = false;

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Protect_read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Soc_read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Sys_read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Sleep_Read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Balance_read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_Other_Read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                by_BatchProcessFlag = 1; by_Cnt = 0;
                button_HeatCool_Read.PerformClick();
                while (by_BatchProcessFlag != 0) { Delay_ms(10); if ((++by_Cnt) > 100) throw new Exception(); }

                //会出问题
                //by_BatchProcessFlag = 1; by_Cnt = 0;
                //button_SocTable_Read.PerformClick();
                //while (by_BatchProcessFlag != 0) {Delay_ms(10); if((++by_Cnt) > 100) throw new Exception(); }

                //by_BatchProcessFlag = 1; by_Cnt = 0;
                //button_CopperLoss_read.PerformClick();
                //while (by_BatchProcessFlag != 0) {Delay_ms(10); if((++by_Cnt) > 100) throw new Exception(); }

                //by_BatchProcessFlag = 1; by_Cnt = 0;
                //Button_Fault_Record_Read.PerformClick();
                //while (by_BatchProcessFlag != 0) {Delay_ms(10); if((++by_Cnt) > 100) throw new Exception(); }

                //by_BatchProcessFlag = 1; by_Cnt = 0;
                //button_RTC_Read.PerformClick();
                //while (by_BatchProcessFlag != 0) {Delay_ms(10); if((++by_Cnt) > 100) throw new Exception(); }

                if (0 == Lang.b_LangFlag)
                    MessageBox.Show("全部参数读取成功", "参数读取", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("All parameters reading succeed", "Reading", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("全部参数读取异常", "参数读取", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_ReadAll.Enabled = true;
                by_BatchProcessFlag = 0;
            }
        }

        private void button_ReadAll_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(AllProtectReadClickCall);
            th.Start();
        }

        private void button_SetAll_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(AllProtectSetClickCall);
            th.Start();
        }
        #endregion 一键读取/写入

        #region 文件导入
        string getParaImportPath()
        {
            OpenFileDialog file = new OpenFileDialog();
            //file.Filter = "Excel(*.xlsx, *.xls)|*.xlsx;*.xls";
            file.Filter = "excel(*.xls)|*.xls";
            file.ShowDialog();
            return file.FileName;
        }
        void parameterImport()
        {
        
            string filePath = getParaImportPath();
            if (filePath == "")
            {
                //MessageBox.Show("文件为空", "文件导入提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            button_FileLoad.Enabled = false;
            try
            {
                //方式二：用这种方式试一下
                myExcel.Application excelApp = new myExcel.Application();
                myExcel.Workbook excelBook; //Excel文档变量
                myExcel.Worksheet excelsheet;
                excelBook = excelApp.Workbooks.Open(filePath, 0, true);//工作簿打开时不更新外部引用（链接）。
                excelsheet = (myExcel.Worksheet)excelBook.Sheets[1]; //Sheets下标从1开始而不是0
                bool paraMatchFlag = true;

                if (tabPage2_defaultPara.Parent == null)
                {
                    parameterExportAndImportInit(ParaType.AFE_PARAMETER);
                    if (excelsheet.Name != "AFE_PARAMETER")
                        paraMatchFlag = false;
                }
                else
                {
                    parameterExportAndImportInit(ParaType.DEFAULT_PARAMETER);
                    if (excelsheet.Name != "DEFAULT_PARAMETER")
                        paraMatchFlag = false;
                }

                if (paraMatchFlag)
                {
                    for (int i = 0; i < parameterExportContentList.Count; i++)
                    {
                        parameterExportContentList[i].Text = excelsheet.Cells[i + 1, 2].Text.ToString();
                    }

                    /*************** AFE参数特殊处理 **************/
                    ushort Sys_CSRes_Num = ushort.Parse(textBox_Sys_CSRes_Num.Text);
                    ushort Sys_CSRes = ushort.Parse(textBox_Sys_CSRes.Text);
                    init_current_combox(Sys_CSRes_Num, Sys_CSRes);

                    for (int i = 0; i < parameterExportContentList.Count; i++)
                    {
                        parameterExportContentList[i].Text = excelsheet.Cells[i + 1, 2].Text.ToString();
                    }
                    /*************** AFE参数特殊处理 **************/

                    MessageBox.Show("文件导入成功", "文件导入提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else {
                    MessageBox.Show("数据导入异常", "参数不匹配", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                excelApp.Quit();
            }
            catch
            {
                MessageBox.Show("数据导入异常", "文件导入提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_FileLoad.Enabled = true;
            }
        }
        private void button_FileLoad_Click(object sender, EventArgs e)
        {
            parameterImport();
        }

        #endregion 文件导入

        #region 文件导出
        string getParaExportPath()
        {
            string filePath = null;
            SaveFileDialog s = new SaveFileDialog();
            //对话框初始路径
            //s.InitialDirectory = @"C:\Users\";

            s.FileName = "export_para_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss");//
            //默认保存的文件名

            //s.Filter = "文件(*.xls)|*.cs|文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
            s.Filter = "excel(*.xls)|*.xls";
            s.FilterIndex = 0;//默认选择文本文件

            //默认保存类型，如果过滤条件选所有文件且没写后缀名，则默认补上该默认值
            s.DefaultExt = ".xls";

            //返回快捷方式的路径而不是快捷方式映射的文件的路径
            s.DereferenceLinks = false;
            
            s.Title = "参数导出对话框";
            //s.RestoreDirectory = true;//没感觉每次都打开都回到了初始路径，你可以试一下

            if (s.ShowDialog() == DialogResult.OK)
            {
                 filePath = s.FileName;
            }
            return filePath;
        }
        void parameterExport()
        {
            string filePath = null;
            filePath = getParaExportPath();
            if (filePath == null)
                return;

            myExcel.Application xlApp = new myExcel.Application();
            myExcel.Workbooks workbooks = xlApp.Workbooks;
            myExcel.Workbook workbook = workbooks.Add(myExcel.XlWBATemplate.xlWBATWorksheet);
            myExcel.Worksheet worksheet = (myExcel.Worksheet)workbook.Worksheets[1];

            
            if (tabPage2_defaultPara.Parent == null)
            {
                worksheet.Name = "AFE_PARAMETER";
                parameterExportAndImportInit(ParaType.AFE_PARAMETER);
            }
            else {
                worksheet.Name = "DEFAULT_PARAMETER";
                parameterExportAndImportInit(ParaType.DEFAULT_PARAMETER);
            }
            
            for (int i = 0; i < parameterExportNameList.Count; i++)
            {
                worksheet.Cells[i+1, 2] = parameterExportContentList[i].Text;
                worksheet.Cells[i+1, 1] = parameterExportNameList[i];
            }

            try
            {
                workbook.SaveCopyAs(@filePath);
                workbook.Saved = true;
                MessageBox.Show("文件导出成功");
            }
            catch (Exception)
            {
                MessageBox.Show("文件保存异常，若已打开保存文件，请关闭");
            }
            finally
            {
                xlApp.Quit();
            }
        }
        private void button_Export_Click(object sender, EventArgs e)
        {
            parameterExport();
        }

        #endregion 文件导出
    }
}
