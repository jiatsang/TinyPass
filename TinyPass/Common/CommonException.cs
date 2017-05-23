using System;

namespace Chiats.TinyPass
{
    public class CommonException : Exception
    {
        private string moreMessage;
        /// <summary>
        /// CommonException 建構子
        /// </summary>
        /// <param name="message">字串訊息內容</param>
        public CommonException(string message) : base(message) { }
        /// <summary>
        /// CommonException 建構子
        /// </summary>
        /// <param name="fmt">含格式化字串訊息內容, 格式化字串相關資料，見 <see cref="String.Format(string, object[])"/> Method.</param>
        /// <param name="args">格式化字串之引數</param>
        public CommonException(string fmt, params object[] args) : base(string.Format(fmt, args)) { }
        /// <summary>
        /// </summary>
        /// <param name="innerException">傳入原始引發例外物件.</param>
        /// <param name="fmt">含格式化字串訊息內容, 格式化字串相關資料，見 <see cref="String.Format(string, object[])"/>.</param>
        /// <param name="args">格式化字串之引數</param>
        public CommonException(Exception innerException, string fmt, params object[] args) : base(string.Format(fmt, args), innerException) { }

        /// <summary>
        /// 傳回更多詳細資料.
        /// </summary>
        public string MoreMessage
        {
            get
            {
                return moreMessage;
            }
            protected set { moreMessage = value; }
        }
    }
}