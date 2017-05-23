using System;

namespace Chiats.nTinyPass
{
    /// <summary>
    /// 指示該欄位為為填入相對資料的 Field or Property
    /// </summary>
    public class ColumnPassAttribute : Attribute
    {
        public string ColumnName = null;
        public IColumnValueConvert ValueConvert = null;
        public ColumnPassAttribute() { }
        /// <summary>
        /// 指示該欄位為為填入相對資料的 Field or Property
        /// </summary>
        /// <param name="ColumnName">ColumnName 為 null 則為  Field or Property 名稱</param>
        /// <param name="ValueConvert">是否有欄位值轉換器</param>
        public ColumnPassAttribute(string ColumnName = null, IColumnValueConvert ValueConvert = null)
        {
            this.ColumnName = ColumnName;
            this.ValueConvert = ValueConvert;
        }
    }
}
