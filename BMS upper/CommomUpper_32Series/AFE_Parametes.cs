using System;
using System.Windows.Forms;

namespace CommomUpper_32Series
{
    public partial class Form1
    {

        class AFE_Parameters_RS485
        {

            public UInt16 u16VcellOvp = 0;    //单节过压 mv
            public UInt16 u16VcellOvp_Rcv = 0;          //过压恢复 mv
            public UInt16 u16VcellOvp_Filter = 0;       //过压延时 10ms

            public UInt16 u16VcellUvp = 0;       //单节低压
            public UInt16 u16VcellUvp_Rcv = 0;
            public UInt16 u16VcellUvp_Filter = 0;

            public UInt16 u16IchgOcp_First = 0;        //一级充电过流 A*10
            public UInt16 u16IchgOcp_Filter_First = 0;

            public UInt16 u16IchgOcp_Second = 0;         //二级充电过流
            public UInt16 u16IchgOcp_Filter_Second = 0;

            public UInt16 u16IdsgOcp_First = 0;        //一级放电过流
            public UInt16 u16IdsgOcp_Filter_First = 0;

            public UInt16 u16IdsgOcp_Second = 0;         //二级放电过流
            public UInt16 u16IdsgOcp_Filter_Second = 0;

            public UInt16 u16TChgOTp = 0;         //充电高温 (℃*10+400)
            public UInt16 u16TChgOTp_Rcv = 0;
            public UInt16 u16TchgUTp = 0;         //充电低温	
            public UInt16 u16TchgUTp_Rcv = 0;
            public UInt16 u16TdischgOTp = 0;  //放电高温
            public UInt16 u16TdischgOTp_Rcv = 0;
            public UInt16 u16TdischgUTp = 0; //放电低温
            public UInt16 u16TdischgUTp_Rcv = 0;
            public UInt16 u16CBC_Cur_DSG = 0;
            public UInt16 u16CBC_DelayT = 0;
            public UInt16 Sys_CSRes = 0;           //采样电阻
            public UInt16 Sys_CSRes_Num = 0;       //采样电阻数量



        };

        /* AFE 参数结构体 */
        AFE_Parameters_RS485 AFE_Parameters_RS485_Struction = new AFE_Parameters_RS485();
        UInt16[] AFE_OCD1V_OCCV = { 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 160, 180, 200 };//一级放电过流和充电过流，单位mv
        UInt16[] AFE_SCV = { 50, 80, 110, 140, 170, 200, 230, 260, 290, 320, 350, 400, 500, 600, 800, 1000 };//短路保护电压，单位mv
        UInt16[] AFE_OVT_UVT = { 100, 200, 300, 400, 600, 800, 1000, 2000, 3000, 4000, 6000, 8000, 10000, 20000, 30000, 40000 };//过压低压延时时间。单位ms
        UInt16[] AFE_SCT = { 0, 64, 128, 192, 256, 320, 384, 448, 512, 576, 640, 704, 768, 832, 896, 960 }; //短路延时,单位us。
        UInt16[] AFE_OCD1T = { 50, 100, 200, 400, 600, 800, 1000, 2000, 4000, 6000, 8000, 10000, 15000, 20000, 30000, 40000 }; //放电过流1延时。单位ms
        UInt16[] AFE_OCCT_OCD2T = { 10, 20, 40, 60, 80, 100, 200, 400, 600, 800, 1000, 2000, 4000, 8000, 10000, 20000 };//放电过流2和充电过流延时。单位ms

        /* 找出要写入AFE寄存器的值：参数1：当前要写的值，参数2：AFE参数列表的地址 */
        int choose_Right_Value(UInt16 cur_Value, UInt16[] AFE_list)
        {
            int i = 0;
            for (i = 0; i < 15; i++)
            {
                if (cur_Value <= AFE_list[i])
                {
                    break;
                }
            }
            return i;
        }

        void RxRdRegAck_AFE_Parameter(byte[] bydata)
        {
            UInt16 temp = 0;
            int i = 3;
            AFE_Parameters_RS485_Struction.u16VcellOvp = (UInt16)(bydata[i++] * 256 + bydata[i++]);   //单节过压 mv
            AFE_Parameters_RS485_Struction.u16VcellOvp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16VcellOvp_Filter = (UInt16)(bydata[i++] * 256 + bydata[i++]);

            AFE_Parameters_RS485_Struction.u16VcellUvp = (UInt16)(bydata[i++] * 256 + bydata[i++]);   //单节低压
            AFE_Parameters_RS485_Struction.u16VcellUvp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16VcellUvp_Filter = (UInt16)(bydata[i++] * 256 + bydata[i++]);

            AFE_Parameters_RS485_Struction.u16IchgOcp_First = (UInt16)(bydata[i++] * 256 + bydata[i++]);   //一级充电过流 A*10
            AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_First = (UInt16)(bydata[i++] * 256 + bydata[i++]);

            AFE_Parameters_RS485_Struction.u16IchgOcp_Second = (UInt16)(bydata[i++] * 256 + bydata[i++]);    //二级充电过流
            AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_Second = (UInt16)(bydata[i++] * 256 + bydata[i++]);

            AFE_Parameters_RS485_Struction.u16IdsgOcp_First = (UInt16)(bydata[i++] * 256 + bydata[i++]);    //一级放电过流
            AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_First = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16IdsgOcp_Second = (UInt16)(bydata[i++] * 256 + bydata[i++]);   //二级放电过流
            AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_Second = (UInt16)(bydata[i++] * 256 + bydata[i++]);

            AFE_Parameters_RS485_Struction.u16TChgOTp = (UInt16)(bydata[i++] * 256 + bydata[i++]);//充电高温 (℃*10+400)
            AFE_Parameters_RS485_Struction.u16TChgOTp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16TchgUTp = (UInt16)(bydata[i++] * 256 + bydata[i++]);//充电低温	
            AFE_Parameters_RS485_Struction.u16TchgUTp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16TdischgOTp = (UInt16)(bydata[i++] * 256 + bydata[i++]);//放电高温
            AFE_Parameters_RS485_Struction.u16TdischgOTp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16TdischgUTp = (UInt16)(bydata[i++] * 256 + bydata[i++]);//放电低温
            AFE_Parameters_RS485_Struction.u16TdischgUTp_Rcv = (UInt16)(bydata[i++] * 256 + bydata[i++]);
            AFE_Parameters_RS485_Struction.u16CBC_Cur_DSG = (UInt16)(bydata[i++] * 256 + bydata[i++]);//短路电流
            AFE_Parameters_RS485_Struction.u16CBC_DelayT = (UInt16)(bydata[i++] * 256 + bydata[i++]);//短路延时


            //单节过压 mv
            this.BeginInvoke(new Action(() => { comboBox_AFE_VcellOvp.Text = AFE_Parameters_RS485_Struction.u16VcellOvp + " mv"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_VcellOvp_Rcv.Text = AFE_Parameters_RS485_Struction.u16VcellOvp_Rcv + " mv"; }));
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16VcellOvp_Filter * 10);
                comboBox_AFE_VcellOvp_Filter.SelectedIndex =choose_Right_Value(temp, AFE_OVT_UVT);
            }));

            //单节低压
            this.BeginInvoke(new Action(() => { comboBox_AFE_VcellUvp.Text = AFE_Parameters_RS485_Struction.u16VcellUvp + " mv"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_VcellUvp_Rcv.Text = AFE_Parameters_RS485_Struction.u16VcellUvp_Rcv + " mv"; }));
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16VcellUvp_Filter * 10);
                comboBox_AFE_VcellUvp_Filter.SelectedIndex =choose_Right_Value(temp, AFE_OVT_UVT);
            }));

            //一级充电过流 A*10
            this.BeginInvoke(new Action(() => { textBox_AFE_IchgOcp_First.Text = AFE_Parameters_RS485_Struction.u16IchgOcp_First / 10 + ""; }));
            this.BeginInvoke(new Action(() => { textBox_AFE_IchgOcp_Filter_First.Text = AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_First * 10 + ""; }));

            //一级放电过流 A*10
            this.BeginInvoke(new Action(() => { textBox_AFE_IdsgOcp_First.Text = AFE_Parameters_RS485_Struction.u16IdsgOcp_First / 10 + ""; }));
            this.BeginInvoke(new Action(() => { textBox_AFE_IdsgOcp_Filter_First.Text = AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_First * 10 + ""; }));

            //充电高温 (℃*10+400)
            this.BeginInvoke(new Action(() => { comboBox_AFE_TChgOTp.Text = (AFE_Parameters_RS485_Struction.u16TChgOTp / 10 - 40).ToString() + " ℃"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_TChgOTp_Rcv.Text = (AFE_Parameters_RS485_Struction.u16TChgOTp_Rcv / 10 - 40).ToString() + " ℃"; }));

            //充电低温	
            this.BeginInvoke(new Action(() => { comboBox_AFE_TchgUTp.Text = (AFE_Parameters_RS485_Struction.u16TchgUTp / 10 - 40).ToString() + " ℃"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_TchgUTp_Rcv.Text = (AFE_Parameters_RS485_Struction.u16TchgUTp_Rcv / 10 - 40).ToString() + " ℃"; }));

            //放电高温
            this.BeginInvoke(new Action(() => { comboBox_AFE_TdischgOTp.Text = (AFE_Parameters_RS485_Struction.u16TdischgOTp / 10 - 40).ToString() + " ℃"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_TdischgOTp_Rcv.Text = (AFE_Parameters_RS485_Struction.u16TdischgOTp_Rcv / 10 - 40).ToString() + " ℃"; }));

            //放电低温
            this.BeginInvoke(new Action(() => { comboBox_AFE_TdischgUTp.Text = (AFE_Parameters_RS485_Struction.u16TdischgUTp / 10 - 40).ToString() + " ℃"; }));
            this.BeginInvoke(new Action(() => { comboBox_AFE_TdischgUTp_Rcv.Text = (AFE_Parameters_RS485_Struction.u16TdischgUTp_Rcv / 10 - 40).ToString() + " ℃"; }));


            /*二级充电过流和放电过流,短路电流*/
            comboBox_AFE_IchgOcp_Second.Items.Clear();
            comboBox_AFE_IdsgOcp_Second.Items.Clear();
            comboBox_AFE_CBC_Cur_DSG.Items.Clear();
            float g_u32CS_Res_AFE = (float)AFE_Parameters_RS485_Struction.Sys_CSRes_Num*1000 / AFE_Parameters_RS485_Struction.Sys_CSRes;
            for (int j = 0; j < 16; j++)
            {
                UInt16 tempCur = (UInt16)(AFE_OCD1V_OCCV[j] * g_u32CS_Res_AFE/1000);
                comboBox_AFE_IchgOcp_Second.Items.Add(tempCur + " A");
                comboBox_AFE_IdsgOcp_Second.Items.Add(tempCur + " A");
                tempCur = (UInt16)(AFE_SCV[j] * g_u32CS_Res_AFE/1000);
                comboBox_AFE_CBC_Cur_DSG.Items.Add(tempCur + " A");
            }


            ////二级充电过流 A*10
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16IchgOcp_Second * 100 / g_u32CS_Res_AFE);
                comboBox_AFE_IchgOcp_Second.SelectedIndex =choose_Right_Value(temp, AFE_OCD1V_OCCV);
            }));

            //二级充电过流延时
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_Second * 10); //当前对应多少ms
                comboBox_AFE_IchgOcp_Filter_Second.SelectedIndex = choose_Right_Value(temp, AFE_OCCT_OCD2T);
            }));

            //二级放电过流
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16IdsgOcp_Second * 100 / g_u32CS_Res_AFE); //当前对应多少mv
                comboBox_AFE_IdsgOcp_Second.SelectedIndex = choose_Right_Value(temp, AFE_OCD1V_OCCV);
            }));

            //二级放电过流延时
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_Second * 10); //当前对应多少ms 
                comboBox_AFE_IdsgOcp_Filter_Second.SelectedIndex = choose_Right_Value(temp, AFE_OCD1T);
            }));

            //短路电流
            this.BeginInvoke(new Action(() => {
                temp = (UInt16)(AFE_Parameters_RS485_Struction.u16CBC_Cur_DSG * 1000 / g_u32CS_Res_AFE); //当前对应多少mv
                comboBox_AFE_CBC_Cur_DSG.SelectedIndex = choose_Right_Value(temp, AFE_SCV);
            }));

            //短路延时
            this.BeginInvoke(new Action(() => {
                temp = AFE_Parameters_RS485_Struction.u16CBC_DelayT;
                comboBox_AFE_CBC_DelayT.SelectedIndex = choose_Right_Value(temp, AFE_SCT);
            }));
        }

        void init_current_combox(ushort Sys_CSRes_Num, ushort Sys_CSRes)
        {
            AFE_Parameters_RS485_Struction.Sys_CSRes_Num =Sys_CSRes_Num;
            AFE_Parameters_RS485_Struction.Sys_CSRes = Sys_CSRes;

            float g_u32CS_Res_AFE = (float)AFE_Parameters_RS485_Struction.Sys_CSRes_Num * 1000 / AFE_Parameters_RS485_Struction.Sys_CSRes;
            for (int j = 0; j < 16; j++)
            {
                UInt16 tempCur = (UInt16)(AFE_OCD1V_OCCV[j] * g_u32CS_Res_AFE / 1000);
                comboBox_AFE_IchgOcp_Second.Items.Add(tempCur + " A");
                comboBox_AFE_IdsgOcp_Second.Items.Add(tempCur + " A");
                tempCur = (UInt16)(AFE_SCV[j] * g_u32CS_Res_AFE / 1000);
                comboBox_AFE_CBC_Cur_DSG.Items.Add(tempCur + " A");
            }
        }


        /* AFE参数界面初始化 */
        void AFE_Parameters_Interface_Init()
        {
            //string[] AFE_OVT_UVT ={ "100ms", "200ms", "300ms", "400ms", "600ms", "800ms", "1s", "2s", "3s", "4s", "6", "8", "10", "20", "30", "40" };

            /*单节过压*//*单节过压恢复*/
            for (int i = 1000; i <= 5000; i += 5)
            {
                comboBox_AFE_VcellOvp.Items.Add(i + " mv");
                comboBox_AFE_VcellOvp_Rcv.Items.Add(i + " mv");
            }
            /*单节低压*//*单节低压恢复*/
            for (int i = 1000; i <= 5000; i += 20)
            {
                comboBox_AFE_VcellUvp.Items.Add(i + " mv");
                comboBox_AFE_VcellUvp_Rcv.Items.Add(i + " mv");
            }

            //充电高温//充电高温恢复//放电高温//放电高温恢复
            for (int i = 40; i <= 80; i++)
            {
                comboBox_AFE_TChgOTp.Items.Add(i + " ℃");
            }
            for (int i = 40; i <= 80; i++)
            {
                comboBox_AFE_TChgOTp_Rcv.Items.Add(i + " ℃");
            }
            for (int i = 40; i <= 80; i++)
            {
                comboBox_AFE_TdischgOTp.Items.Add(i + " ℃");
            }
            for (int i = 40; i <= 80; i++)
            {
                comboBox_AFE_TdischgOTp_Rcv.Items.Add(i + " ℃");
            }

            //充电低温//充电低温恢复//放电低温//放电低温恢复

            for (int i = -20; i <= 10; i++)
            {
                comboBox_AFE_TchgUTp.Items.Add(i + " ℃"); 
            }
            for (int i = -20; i <= 15; i++)
            {
                comboBox_AFE_TchgUTp_Rcv.Items.Add(i + " ℃");
            }
            for (int i = -40; i <= 10; i++)
            {
                comboBox_AFE_TdischgUTp.Items.Add(i + " ℃");
            }
            for (int i = -40; i <= 15; i++)
            {
                comboBox_AFE_TdischgUTp_Rcv.Items.Add(i + " ℃");
            }

            //短路延时
            for (int i = 0; i < 16; i++)
            {
                comboBox_AFE_CBC_DelayT.Items.Add(AFE_SCT[i] + " us");
            }
        }
       
        private void button_AFE_ParametersWrite_Click(object sender, EventArgs e)
        {
            UInt16[] u_Params = new UInt16[24];
            bRs485FunCmd = 0x10;
            u16Rs485RegAddr = 0x2400;
            u16Rs485RegNum = 24;
            bRs485ByteNum = 48;

            AFE_Parameters_RS485_Struction.u16VcellOvp = Convert.ToUInt16(comboBox_AFE_VcellOvp.Text.Replace(" mv", ""));   //单节过压 mv
            AFE_Parameters_RS485_Struction.u16VcellOvp_Rcv = Convert.ToUInt16(comboBox_AFE_VcellOvp_Rcv.Text.Replace(" mv", ""));
            AFE_Parameters_RS485_Struction.u16VcellOvp_Filter = (UInt16)(AFE_OVT_UVT[comboBox_AFE_VcellOvp_Filter.SelectedIndex] / 10);

            AFE_Parameters_RS485_Struction.u16VcellUvp = Convert.ToUInt16(comboBox_AFE_VcellUvp.Text.Replace(" mv", ""));    //单节低压
            AFE_Parameters_RS485_Struction.u16VcellUvp_Rcv = Convert.ToUInt16(comboBox_AFE_VcellUvp_Rcv.Text.Replace(" mv", ""));
            AFE_Parameters_RS485_Struction.u16VcellUvp_Filter = (UInt16)(AFE_OVT_UVT[comboBox_AFE_VcellUvp_Filter.SelectedIndex] / 10);

            try
            {
                AFE_Parameters_RS485_Struction.u16IchgOcp_First = (UInt16)(Convert.ToUInt32(textBox_AFE_IchgOcp_First.Text) * 10);   //一级充电过流 A*10
                AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_First = (UInt16)(Convert.ToUInt32(textBox_AFE_IchgOcp_Filter_First.Text) / 10);
                AFE_Parameters_RS485_Struction.u16IchgOcp_Second = (UInt16)(Convert.ToUInt16(comboBox_AFE_IchgOcp_Second.Text.Replace(" A", "")) * 10);      //二级充电过流
                AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_Second = (UInt16)(AFE_OCCT_OCD2T[comboBox_AFE_IchgOcp_Filter_Second.SelectedIndex] / 10);

                AFE_Parameters_RS485_Struction.u16IdsgOcp_First = (UInt16)(Convert.ToUInt32(textBox_AFE_IdsgOcp_First.Text) * 10);    //一级放电过流
                AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_First = (UInt16)(Convert.ToUInt32(textBox_AFE_IdsgOcp_Filter_First.Text) / 10);
                AFE_Parameters_RS485_Struction.u16IdsgOcp_Second = (UInt16)(Convert.ToUInt16(comboBox_AFE_IdsgOcp_Second.Text.Replace(" A", "")) * 10);   //二级放电过流
                AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_Second = (UInt16)(AFE_OCD1T[comboBox_AFE_IdsgOcp_Filter_Second.SelectedIndex] / 10);
            }
            catch
            {
                MessageBox.Show("一级过流参数异常", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            AFE_Parameters_RS485_Struction.u16TChgOTp = (UInt16)((Convert.ToInt16(comboBox_AFE_TChgOTp.Text.Replace(" ℃", "")) + 40) * 10); //充电高温 (℃*10+400)
            AFE_Parameters_RS485_Struction.u16TChgOTp_Rcv = (UInt16)((Convert.ToInt16(comboBox_AFE_TChgOTp_Rcv.Text.Replace(" ℃", "")) + 40) * 10);
            AFE_Parameters_RS485_Struction.u16TchgUTp = (UInt16)((Convert.ToInt16(comboBox_AFE_TchgUTp.Text.Replace(" ℃", "")) + 40) * 10);//充电低温	
            AFE_Parameters_RS485_Struction.u16TchgUTp_Rcv = (UInt16)((Convert.ToInt16(comboBox_AFE_TchgUTp_Rcv.Text.Replace(" ℃", "")) + 40) * 10);
            AFE_Parameters_RS485_Struction.u16TdischgOTp = (UInt16)((Convert.ToInt16(comboBox_AFE_TdischgOTp.Text.Replace(" ℃", "")) + 40) * 10);//放电高温
            AFE_Parameters_RS485_Struction.u16TdischgOTp_Rcv = (UInt16)((Convert.ToInt16(comboBox_AFE_TdischgOTp_Rcv.Text.Replace(" ℃", "")) + 40) * 10);
            AFE_Parameters_RS485_Struction.u16TdischgUTp = (UInt16)((Convert.ToInt16(comboBox_AFE_TdischgUTp.Text.Replace(" ℃", "")) + 40) * 10);//放电低温
            AFE_Parameters_RS485_Struction.u16TdischgUTp_Rcv = (UInt16)((Convert.ToInt16(comboBox_AFE_TdischgUTp_Rcv.Text.Replace(" ℃", "")) + 40) * 10);
            AFE_Parameters_RS485_Struction.u16CBC_Cur_DSG = (UInt16)Convert.ToUInt16(comboBox_AFE_CBC_Cur_DSG.Text.Replace(" A", "")); //短路电流
            AFE_Parameters_RS485_Struction.u16CBC_DelayT = (UInt16)(AFE_SCT[comboBox_AFE_CBC_DelayT.SelectedIndex]);

            #region 参数范围判断
            if (Convert.ToUInt32(textBox_AFE_IchgOcp_First.Text) * 10 > 50000)
            {
                MessageBox.Show("一级充电过流超过5000A", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Convert.ToUInt32(textBox_AFE_IdsgOcp_First.Text) * 10 > 50000)
            {
                MessageBox.Show("一级放电过流超过5000A", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Convert.ToUInt32(textBox_AFE_IchgOcp_Filter_First.Text) / 10 > 50000)
            {
                MessageBox.Show("一级充电过流延时超过500000ms", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Convert.ToUInt32(textBox_AFE_IdsgOcp_Filter_First.Text) / 10 > 50000)
            {
                MessageBox.Show("一级放电过流延时超过500000ms", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16VcellOvp < AFE_Parameters_RS485_Struction.u16VcellOvp_Rcv)
            {
                MessageBox.Show("单节过压小于过压恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16VcellUvp > AFE_Parameters_RS485_Struction.u16VcellUvp_Rcv)
            {
                MessageBox.Show("单节低压大于低压恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16TChgOTp < AFE_Parameters_RS485_Struction.u16TChgOTp_Rcv)
            {
                MessageBox.Show("充电过温小于过温恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16TchgUTp > AFE_Parameters_RS485_Struction.u16TchgUTp_Rcv)
            {
                MessageBox.Show("充电低温大于低温恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16TdischgOTp < AFE_Parameters_RS485_Struction.u16TdischgOTp_Rcv)
            {
                MessageBox.Show("放电过温小于过温恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (AFE_Parameters_RS485_Struction.u16TdischgUTp > AFE_Parameters_RS485_Struction.u16TdischgUTp_Rcv)
            {
                MessageBox.Show("放电低温大于低温恢复", "错误信息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            #endregion

            int i = 0;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellOvp;   //单节过压 mv
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellOvp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellOvp_Filter;

            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellUvp;   //单节低压
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellUvp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16VcellUvp_Filter;

            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IchgOcp_First;   //一级充电过流 A*10
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_First;

            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IchgOcp_Second;    //二级充电过流
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IchgOcp_Filter_Second;

            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IdsgOcp_First;    //一级放电过流
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_First;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IdsgOcp_Second;   //二级放电过流
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16IdsgOcp_Filter_Second;

            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TChgOTp;//充电高温 (℃*10+400)
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TChgOTp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TchgUTp;//充电低温	
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TchgUTp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TdischgOTp;//放电高温
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TdischgOTp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TdischgUTp;//放电低温
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16TdischgUTp_Rcv;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16CBC_Cur_DSG;
            u_Params[i++] = AFE_Parameters_RS485_Struction.u16CBC_DelayT;

            SentData(u_Params, bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum, bRs485ByteNum);
        }

        private void button_AFE_ParametersRead_Click(object sender, EventArgs e)
        {
            byte[] senddataTemp = new byte[8];
            textBox_AFE_IchgOcp_First.Text = "";
            textBox_AFE_IchgOcp_Filter_First.Text = "";
            textBox_AFE_IdsgOcp_First.Text = "";
            textBox_AFE_IdsgOcp_Filter_First.Text = "";

            textBox_Sys_read_Click(sender, e);//读采样电阻和数量

            System.Threading.Thread.Sleep(400);
            if (AFE_Parameters_RS485_Struction.Sys_CSRes == 0 || AFE_Parameters_RS485_Struction.Sys_CSRes_Num == 0)
            {
                return;
            }


            //RS485_CMD_READ_REGS
            bRs485FunCmd = 0x03;                //这个单片机的精髓，就是不断修改这个值，开始去掉进入0x06死循环，后面加回来就正常了
            u16Rs485RegAddr = 0x2400;   //我以前的感悟怎么这么傻
            u16Rs485RegNum = 0x18;

            senddataTemp[0] = RS485_SLAVE_ADDR;
            senddataTemp[1] = bRs485FunCmd;
            senddataTemp[2] = (byte)(u16Rs485RegAddr / 256);
            senddataTemp[3] = (byte)(u16Rs485RegAddr % 256);
            senddataTemp[4] = (byte)(u16Rs485RegNum / 256);
            senddataTemp[5] = (byte)(u16Rs485RegNum % 256);
            Calculate_Sum_Tx(ref senddataTemp, 6);

            //bRxByteCnt = 0;
            //bTotleBytes = 0;
            //bRxFrameFinishFlag = false;
            serialPort1.Write(senddataTemp, 0, 8);
        }

        private void button_AFE_ParametesReset_Click(object sender, EventArgs e)
        {
            bRs485FunCmd = 0x06;
            u16Rs485RegAddr = (UInt16)RS485_CMD_RW_E.RS485_CMD_ADDR_RESET_AFE_PARAMETERS;
            u16Rs485RegNum = 0x0001;
            SentData(bRs485FunCmd, u16Rs485RegAddr, u16Rs485RegNum);
        }
    }
}

