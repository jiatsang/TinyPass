using System;

namespace Chiats.nTinyPass
{
    public class InvalidColumnNameException : CommonException
    {
        /// <summary>
        /// CommonException 建構子
        /// </summary>
        /// <param name="message">字串訊息內容</param>
        public InvalidColumnNameException(string ColumnNames) : base($"Invalid Column Name {ColumnNames}") { }
    }
}