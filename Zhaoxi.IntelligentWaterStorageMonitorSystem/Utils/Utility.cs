using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zhaoxi.CustControls;

namespace Zhaoxi.IntelligentWaterStorageMonitorSystem.Utils
{
    /// <summary>
    /// 项目相关的通用类
    /// </summary>
    public static class Utility
    {
        public static Dictionary<string,UButton> dicBtnSets= new Dictionary<string,UButton>();

        /// <summary>
        /// 禁用UButton
        /// </summary>
        /// <param name="btn"></param>
        public static void UnableBtn(this UButton btn)
        {
            btn.Enabled= false;
            btn.BgColor = Color.Gray;
            btn.BgColor2 = Color.Transparent;
            btn.ForeColor = Color.LightGray;
            btn.FocusForeColor = Color.LightGray;
            btn.FocusBgColor = Color.Gray;
        }

        /// <summary>
        /// 启用UButton
        /// </summary>
        /// <param name="btn"></param>
        public static void EnableBtn(this UButton btn)
        {
            btn.Enabled = true;
            UButton btnCopy=dicBtnSets[btn.Name];
            btn.BgColor = btnCopy.BgColor;
            btn.BgColor2 = btnCopy.BgColor2;
            btn.ForeColor = btnCopy.ForeColor;
            btn.FocusForeColor = btnCopy.FocusForeColor;
            btn.FocusBgColor = btnCopy.FocusBgColor;
        }

        //将ushort数组转换为float
        public static float GetUshortToFloat(ushort[] ushorts)
        {
            List<byte> bytes = new List<byte>();
            byte[] bytes1 = BitConverter.GetBytes(ushorts[1]);
            bytes.AddRange(bytes1);
            byte[] bytes2 = BitConverter.GetBytes(ushorts[0]);
            bytes.AddRange(bytes2);
            return BitConverter.ToSingle(bytes.ToArray(), 0);
        }

        //将ushort数组转换为uint
        public static uint GetUshortToUInt(ushort[] ushorts)
        {
            List<byte> bytes = new List<byte>();
            byte[] bytes1 = BitConverter.GetBytes(ushorts[1]);
            bytes.AddRange(bytes1);
            byte[] bytes2 = BitConverter.GetBytes(ushorts[0]);
            bytes.AddRange(bytes2);
            return BitConverter.ToUInt32(bytes.ToArray(), 0);
        }
    }
}
