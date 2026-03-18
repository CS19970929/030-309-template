using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CommomUpper_32Series
{
    partial class Form1
    {
        //端口、空值、数据类型验证
        private bool VerifyParams_ProtUsual(Verify vfyObj) {
            if (!serialPort1.IsOpen)
            {
                MessageBox.Show(s_MsgInfo[(int)Lang.MsgInfo.PortClosed], s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Regex ObjReg;
            int Len = vfyObj.idx +  vfyObj.len;
            switch(vfyObj.decNum) {
                case 0:
                    ObjReg = new Regex(@"^(\-)?\d{1,5}$");            //有符号，1-5位数字
                    break;
                case 1:
                    ObjReg = new Regex(@"^(\-)?\d{1,5}(\.\d)?$");     //有符号，整数位最多5个数字，最多1位小数
                    break;
                case 2:
                    ObjReg = new Regex(@"^(\-)?\d{1,5}(\.\d{1,2})?$");//有符号，整数位最多5个数字，最多2位小数
                    break;
                default:
                    ObjReg = new Regex(@"^(\-)?\d{1,5}(\.\d{1,3})?$");//有符号，整数位最多5个数字，最多3位小数
                    break;
            }
            for(; vfyObj.idx < Len; vfyObj.idx++)
            {
                if (vfyObj.s_Params[vfyObj.idx] == "")        //非空验证
                {
                    MessageBox.Show(s_Verification[(int)Verify.LangIdx.LackOfInfo_Idx] + (vfyObj.idx + 1) + s_Verification[(int)Verify.LangIdx.LackOfInfo_Msg],
                         s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else
                {
                    if (!ObjReg.IsMatch(vfyObj.s_Params[vfyObj.idx]))   //正则表达式不匹配
                    {
                        MessageBox.Show(s_Verification[(int)Verify.LangIdx.Int_Idx] + (vfyObj.idx + 1)
                            + s_Verification[(int)Verify.LangIdx.Int_Msg] 
                            + vfyObj.decNum + s_Verification[(int)Verify.LangIdx.Int_NumOfDecimal],
                            s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            return true;
        }
        //最大、最小值范围验证
        private bool VerifyParams_ProtValidate(Verify vfyObj)
        {
            int Len = vfyObj.idx + vfyObj.len;
            for (; vfyObj.idx < Len; vfyObj.idx++) {
                if (vfyObj.f_Params[vfyObj.idx] < vfyObj.min  || vfyObj.f_Params[vfyObj.idx] > vfyObj.max)
                {
                    MessageBox.Show(s_Verification[(int)Verify.LangIdx.Range_Idx] + (vfyObj.idx + 1) 
                        + s_Verification[(int)Verify.LangIdx.RangeIs] 
                        + String.Format("{0:F0}", vfyObj.min) + " - " + String.Format("{0:F0}", vfyObj.max)
                        + "(" + vfyObj.unit + ")"
                        + s_Verification[(int)Verify.LangIdx.OutOfRange], 
                        s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }
        
        //参数间相互合理性验证
        private bool VerifyParams_ProtReasonable(float[] Params, bool LogicalFlag) {
            if (Verify.over == LogicalFlag)
            {
                for (byte i = 0; i < 2; i++)
                {
                    if (Params[i] > Params[i + 1])
                    {
                        MessageBox.Show(s_Verification[(int)Verify.LangIdx.Reasonable_Idx_1] + (i + 1)
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Data_1]
                               + s_Verification[(int)Verify.LangIdx.Reasonable_CantGreater]
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Idx_2] + (i + 2)
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Data_2],
                               s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                if (Params[3] > Params[2])
                {
                    MessageBox.Show(s_Verification[(int)Verify.LangIdx.Reasonable_Idx_1] + 4
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Data_1]
                           + s_Verification[(int)Verify.LangIdx.Reasonable_CantGreater]
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Idx_2] + 3
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Data_2],
                           s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                for (byte i = 0; i < 2; i++)
                {
                    if (Params[i] < Params[i + 1])
                    {
                        MessageBox.Show(s_Verification[(int)Verify.LangIdx.Reasonable_Idx_1] + (i + 1)
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Data_1]
                               + s_Verification[(int)Verify.LangIdx.Reasonable_CantLess]
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Idx_2] + (i + 2)
                               + s_Verification[(int)Verify.LangIdx.Reasonable_Data_2],
                               s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                if (Params[3] < Params[2])
                {
                    MessageBox.Show(s_Verification[(int)Verify.LangIdx.Reasonable_Idx_1] + 4
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Data_1]
                           + s_Verification[(int)Verify.LangIdx.Reasonable_CantLess]
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Idx_2] + 3
                           + s_Verification[(int)Verify.LangIdx.Reasonable_Data_2],
                           s_MsgInfo[(int)Lang.MsgInfo.SetFailed],
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }
    }

    class Verify {
        public string[] s_Params;
        public float[] f_Params;
        public byte len;        //有以下两种意义，注意区分：1、需验证数据的个数；2、需验证数据的结束下标
        public byte idx = 0;    //需验证数据的起始下标
        public byte decNum = 0;     //小数位
        public float min, max;
        public string unit;

        public const bool over = true;
        public const bool under = false;

        public enum LangIdx
        {
            LackOfInfo_Idx = 0,
            LackOfInfo_Msg,
            Int_Idx,
            Int_Msg,
            Int_NumOfDecimal,
            Range_Idx,
            RangeIs,
            OutOfRange,
            Reasonable_Idx_1,
            Reasonable_Data_1,
            Reasonable_CantGreater,
            Reasonable_CantLess,
            Reasonable_Idx_2,
            Reasonable_Data_2,
        }
    }
}                                    
