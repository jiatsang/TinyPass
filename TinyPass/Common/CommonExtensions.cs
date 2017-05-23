using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Chiats.TinyPass.Common
{
    /// <summary>
    /// 系統相關的輔助程式庫.
    /// </summary>
    public static class CommonExtensions
    {

        /// <summary>
        /// 取得指定欄位之值
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="row">DataRow</param>
        /// <param name="ColumnName">指定欄位名稱</param>
        /// <returns></returns>
        public static T GetValueEx<T>(this DataRow row, string ColumnName)
        {
            return GetValueEx<T>(row, ColumnName, default(T));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="ColumnIndex"></param>
        /// <returns></returns>
        public static T GetValueEx<T>(this DataRow row, int ColumnIndex)
        {
            return GetValueEx<T>(row, ColumnIndex, default(T));
        }

        /// <summary>
        /// 取得指定欄位之值
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <param name="ColumnName">指定欄位名稱</param>
        /// <returns></returns>
        public static object GetValueEx(this DataRow row, string ColumnName)
        {
            object val = row[ColumnName];
            if (val == DBNull.Value) return null;
            return val;
        }

        /// <summary>
        /// 取得指定欄位之值
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <param name="ColumnIndex"></param>
        /// <returns></returns>
        public static object GetValueEx(this DataRow row, int ColumnIndex)
        {
            object val = row[ColumnIndex];
            if (val == DBNull.Value) return null;
            return val;
        }
        /// <summary>
        /// 取值支援指定多種型別 
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="row">DataRow</param>
        /// <param name="ColumnName">指定欄位名稱</param>
        /// <param name="default_value">預設值</param>
        /// <returns></returns>
        public static T GetValueEx<T>(this DataRow row, string ColumnName, T default_value)
        {
            object val = row[ColumnName];
            if (val == null || val == DBNull.Value)
            {
                return default_value;
            }
            return val.ChangeType<T>();
        }
        /// <summary>
        /// 取值支援指定多種型別 
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="row">DataRow</param>
        /// <param name="ColumnIndex"></param>
        /// <param name="default_value">預設值</param>
        /// <returns></returns>
        public static T GetValueEx<T>(this DataRow row, int ColumnIndex, T default_value)
        {
            object val = row[ColumnIndex];
            if (val == null || val == DBNull.Value)
            {
                return default_value;
            }
            return val.ChangeType<T>();
        }
        /// <summary>
        ///  取值 支援指定多種型別 
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="reader">IDataReader</param>
        /// <param name="ColumnName">指定欄位名稱</param>
        /// <returns></returns>
        public static T GetValueEx<T>(this IDataReader reader, string ColumnName)
        {
            int index = reader.GetOrdinal(ColumnName);
            return GetValueEx<T>(reader, index);
        }
        /// <summary>
        ///  取值 支援指定多種型別 
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="reader">IDataReader</param>
        /// <param name="index">指定欄位索引值</param>
        /// <returns></returns>
        public static T GetValueEx<T>(this IDataReader reader, int index)
        {
            object val = reader.GetValue(index);
            if (val == null || val == DBNull.Value)
                return default(T);
            return val.ChangeType<T>();
        }

        /// <summary>
        /// 型別自動型別函數, 支援 DBNull.Value 的自轉型為 NULL
        /// </summary>
        /// <typeparam name="T">傳回值型別</typeparam>
        /// <param name="val">原始值</param>
        /// <param name="_default"></param>
        /// <returns></returns>
        // [System.Diagnostics.DebuggerNonUserCode]
        public static T ChangeType<T>(this object val, T _default = default(T))
        {
            if (val == null || val == DBNull.Value)
                return _default;
            else if (val is string && (string)val == "")
            {
                // 空字串的處理方法
                if (typeof(T) == typeof(string))
                    return (T)val;
                return _default;
            }
            else
            {
                Type converionType = typeof(T);

                // 檢查是否為 Nullable 的型別
                if (converionType.GetTypeInfo().IsGenericType && converionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    converionType = Nullable.GetUnderlyingType(converionType);

                if (val.GetType() == converionType) return (T)val;
                if (converionType == typeof(Type)) return (T)val;

                try
                {
                    return (T)(Convert.ChangeType(val, converionType) ?? _default);
                }
                catch (Exception /*ex*/)
                {
                    // 對轉型失敗時 回傳  _default
                    // Debug.Print("Exception Found in ChangeType({1} to {2}) '{0}' return default value {3} ", val, val.GetType().Name, converionType.Name, _default);
                    return _default;
                }
            }
        }

        /// <summary>
        /// 強迫自動轉型, 在可能的範圍內執行自動轉型的工作. (會自動轉換 DBNull 為 null)
        /// </summary>
        /// <param name="converionType"></param>
        /// <param name="val"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        public static object ChangeTypeEx(this object val, Type converionType, object _default = null)
        {
            // a thread-safe way to hold default instances created at run-time
            if (val == null || val == DBNull.Value)
                return converionType.GetTypeInfo().IsValueType ? Activator.CreateInstance(converionType) : _default;
            if (val is string && (string)val == "")
            {
                // 空字串的處理方法
                if (converionType == typeof(string))
                    return val;
                return converionType.GetTypeInfo().IsValueType ? Activator.CreateInstance(converionType) : _default;
            }
            else
            {
                if (converionType.GetTypeInfo().IsGenericType && converionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    converionType = Nullable.GetUnderlyingType(converionType);
                }

                if (val.GetType() == converionType) return val;
                if (converionType == typeof(Type)) return val;
                try
                {
                    return Convert.ChangeType(val, converionType) ?? _default;
                }
                catch
                {
                    return _default; // Convert Failed.
                }
            }
        }

    }

}
