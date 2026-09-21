using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zhaoxi.Utils
{
    /// <summary>
    /// 消息框封装类
    /// </summary>
    public class MessageHelper
    {
        /// <summary>
        /// 成功消息提示框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public static void Success(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 错误消息提示框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public static void Error(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 询问消息提示框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public static DialogResult Question(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        }
    }
}
