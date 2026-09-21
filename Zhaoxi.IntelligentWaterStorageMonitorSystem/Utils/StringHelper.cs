using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zhaoxi.Utils
{
    /// <summary>
    /// 字符串辅助类
    /// </summary>
    public static class StringHelper
    {
        public static decimal GetDecimal(this string str)
        {
            decimal result = 0;
            decimal.TryParse(str, out result);
            return result;
        }

        public static float GetFloat(this string str)
        {
            float result = 0;
            float.TryParse(str, out result);
            return result;
        }

        public static uint GetUInt(this string str)
        {
            uint result = 0;
            uint.TryParse(str, out result);
            return result;
        }

        public static int GetInt(this string str)
        {
            int result = 0;
            int.TryParse(str, out result);
            return result;
        }

        public static ushort GetUShort(this string str)
        {
            ushort result = 0;
            ushort.TryParse(str, out result);
            return result;
        }

        public static byte GetByte(this string str)
        {
            byte result = 0;
            byte.TryParse(str, out result);
            return result;
        }
    }
}
