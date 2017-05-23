using Chiats.nTinyPass.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Chiats.nTinyPass
{
    /// <summary>
    /// TinyPass DTO 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class nTinyPass<T>
    {
        abstract class Pass
        {
            protected Type PassType;
            protected IEnumerable<PropertyInfo> PropertyInfos;
            protected IEnumerable<FieldInfo> FieldInfos;

            // for check 
            protected string ColumnName { get; private set; }
            protected IColumnValueConvert ColumnValueConvert { get; private set; }
            public int RowIndex { get; set; }
            public Pass()
            {
                this.PassType = typeof(T);
                this.PropertyInfos = PassType.GetProperties();
                this.FieldInfos = PassType.GetFields(); 
            }

            protected T CreateNew()
            {
                return Activator.CreateInstance<T>();
            }

            private bool ColumnNameCheck(MemberInfo info, ref string FiledName)
            {
                IgnoreColumnPassAttribute[] irs = (IgnoreColumnPassAttribute[])info.GetCustomAttributes(typeof(IgnoreColumnPassAttribute), true);
                if (irs.Length == 0)
                {
                    ColumnPassAttribute[] rs = (ColumnPassAttribute[])info.GetCustomAttributes(typeof(ColumnPassAttribute), true);
                    if (rs.Length > 0)
                    {
                        ColumnName = rs[0].ColumnName;
                        if (string.IsNullOrEmpty(ColumnName)) ColumnName = info.Name;
                        ColumnValueConvert = rs[0].ValueConvert;
                        FiledName = ColumnName;
                        return true;
                    }
                    ColumnValueConvert = null;
                    ColumnName = info.Name;
                    FiledName = ColumnName;
                    return true;
                }
                ColumnValueConvert = null;
                ColumnName = null;
                return false;
            }

            private bool Update(PropertyInfo p, object NewObject, object ObjectValue)
            {
                if (p.CanWrite)
                {
                    p.SetValue(NewObject, ObjectValue.ChangeTypeEx(p.PropertyType), null);
                    return true;
                }
                else
                {
                    // 因為 Anonymous Type PropertyInfo 是無法寫入變數內容.要改用 FieldInfo (<ID>i__Field)

                    string FieldName = string.Format("<{0}>i__Field", p.Name);
                    var f = PassType.GetField(FieldName , BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null)
                    {
                        f.SetValue(NewObject, ObjectValue.ChangeTypeEx(f.FieldType));
                        return true;
                    }
                }
                return false;
            }

            private bool Update(PropertyInfo p, object NewObject)
            {
                if (GetColumnValue(out object ObjectValue))
                    return Update(p, NewObject, ObjectValue);
                return false;
            }

            private bool Update(FieldInfo f, object NewObject)
            {
                if (GetColumnValue(out object ObjectValue))
                {
                    f.SetValue(NewObject, ObjectValue.ChangeTypeEx(f.FieldType));
                    return true;
                }
                return false;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="NewObject"></param>
            /// <returns></returns>
            public int QueryFill(T NewObject, nTinyPassMode TinyPassMode)
            {

                List<string> InvalidColumns = new List<string>();

                //TODO: check IsFillALlNewObject Or Not

                int field_count = 0;
                PropertyInfo p_count = null;
                string FiledName = null;
                foreach (PropertyInfo prop in PropertyInfos)
                {
                    FiledName = null;
                    if (ColumnNameCheck(prop, ref FiledName))
                    {
                        if (Update(prop, NewObject))
                            field_count++;
                        else
                        {
                            if (FiledName == null || prop.Name == FiledName)
                                InvalidColumns.Add(prop.Name);
                            else
                                InvalidColumns.Add(prop.Name + "->" + FiledName);
                        }
                    }
                }

                foreach (FieldInfo field in FieldInfos)
                {
                    FiledName = null;
                    if (ColumnNameCheck(field, ref FiledName))
                    {
                        if (Update(field, NewObject))
                            field_count++;
                        else
                        {
                            if (FiledName == null || field.Name == FiledName)
                                InvalidColumns.Add(field.Name);
                            else
                                InvalidColumns.Add(field.Name + "->" + FiledName);
                        }
                    }
                }

                if (p_count != null)
                    Update(p_count, NewObject, field_count);

                if (InvalidColumns.Count > 0 && TinyPassMode == nTinyPassMode.CheckAndException)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var InvalidColumn in InvalidColumns) sb.AppendFormat("{0},", InvalidColumn);
                    throw new InvalidColumnNameException(sb.ToString());
                }

                return field_count;
            }

            public T Query(nTinyPassMode TinyPassMode = nTinyPassMode.NoCheck)
            {
                T NewObject = CreateNew();
                QueryFill(NewObject, TinyPassMode);
                return NewObject;
            }

            private bool GetColumnValue(out object _val)
            {
                if (GetDataValue(out _val))
                {
                    if (ColumnValueConvert != null)
                        _val = ColumnValueConvert.GetValue(_val);
                    else if (_val is DateTime && typeof(T) == typeof(string))
                    {
                        _val = ((DateTime)_val).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    return true;
                }
                return false;
            }

            protected abstract bool GetDataValue(out object Val);
            public abstract bool Next();
            public abstract int GetColumnIndex(string ColumnName);
            public abstract string GetColumnValue(int ColumnIndex);
        }

        class PassForDataReader : Pass
        {
            private IDataReader reader;
            private Dictionary<string, int> FieldIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public PassForDataReader(IDataReader reader)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                    FieldIndex.Add(reader.GetName(i), i);
                this.reader = reader;

            }
            public override int GetColumnIndex(string ColumnName)
            {
                if (FieldIndex.ContainsKey(ColumnName))
                {
                    return FieldIndex[ColumnName];
                }
                return -1;
            }

            public override string GetColumnValue(int ColumnIndex)
            {
                return reader.GetValueEx<string>(ColumnIndex);
            }

            protected override bool GetDataValue(out object _val)
            {
                _val = null;
                int index = GetColumnIndex(ColumnName);
                if (index != -1)
                    _val = reader.GetValue(index);
                return _val != null;
            }

            public override bool Next()
            {
                RowIndex++;
                return reader.Read();
            }


        }
        class PassForDataTable : Pass
        {
            private DataTable table;

            public PassForDataTable(DataTable table, int StartIndex)
            {
                this.table = table;
                this.RowIndex = StartIndex;
            }

            public override int GetColumnIndex(string ColumnName)
            {
                if (table.Columns.Contains(ColumnName))
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                        if (string.Compare(ColumnName, table.Columns[i].ColumnName) == 0) return i;
                }
                return -1;
            }

            public override string GetColumnValue(int ColumnIndex)
            {
                return table.Rows[RowIndex].GetValueEx<string>(ColumnIndex);
            }

            protected override bool GetDataValue(out object _val)
            {
                if (RowIndex >= 0 && RowIndex < table.Rows.Count)
                {
                    if (table.Columns.Contains(ColumnName))
                    {
                        _val = table.Rows[RowIndex].GetValueEx(ColumnName);
                        return true;
                    }
                }
                _val = null;
                return false;
            }

            public override bool Next()
            {
                RowIndex++;
                return RowIndex < table.Rows.Count;
            }
        }

        public static IEnumerable<T> QueryAll(DataTable table, int StartIndex = 0, int MaxRows = -1)
        {
            return QueryAll(new PassForDataTable(table, StartIndex), MaxRows);
        }
        public static T Query(DataTable table, int StartIndex)
        {
            return new PassForDataTable(table, StartIndex).Query();
        }
        public static Dictionary<string, T> QueryAllEx(IDataReader reader, string primaryKey, StringComparer Comparer = null, int MaxRows = -1)
        {

            return QueryAllEx(new PassForDataReader(reader), primaryKey, Comparer, MaxRows);
        }
        public static IEnumerable<T> QueryAll(IDataReader reader, int MaxRows = -1)
        {
            return QueryAll(new PassForDataReader(reader), MaxRows);
        }

        private static IEnumerable<T> QueryAll(Pass Pass, int MaxRows = -1)
        {
            List<T> list = new List<T>();
            while (Pass.Next())
            {
                list.Add(Pass.Query());
                if (MaxRows != -1 && Pass.RowIndex > MaxRows) break;
            }
            return list;
        }

        private static Dictionary<string, T> QueryAllEx(Pass Pass, string primaryKey, StringComparer Comparer = null, int MaxRows = -1)
        {
            Dictionary<string, T> Dictionary = null;

            if (Comparer != null)
                Dictionary = new Dictionary<string, T>(Comparer);
            else
                Dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

            int primaryKeyIndex = Pass.GetColumnIndex(primaryKey);

            if (primaryKeyIndex != -1)
            {
                while (Pass.Next())
                {
                    string key = Pass.GetColumnValue(primaryKeyIndex);

                    if (!Dictionary.ContainsKey(key))
                        Dictionary.Add(key, Pass.Query());
                    if (MaxRows != -1 && Pass.RowIndex > MaxRows) break;
                }
            }
            else
            {
                throw new CommonException("Invalid primaryKey in QueryAllEx");
            }
            return Dictionary;
        }
        public static int QueryFill(T NewObject, IDataReader reader, nTinyPassMode TinyPassMode)
        {
            return new PassForDataReader(reader).QueryFill(NewObject, TinyPassMode);
        }
        public static T Query(IDataReader reader)
        {
            if (reader.Read())
                return new PassForDataReader(reader).Query();
            return default(T);
        }

    }
}
