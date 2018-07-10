using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace UtilTool
{
    public static class DataTableExtensions
    {
        #region 将 list 转化为 DataTable （支持 JObject 类型、匿名类型、自定义Model类型）
        /// <summary>
        /// 将 list 转化为 DataTable （支持 JObject 类型、匿名类型、自定义Model类型）
        /// </summary>
        /// <typeparam name="T">JObject/dynamic/class</typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IEnumerable<T> list, params string[] propertyName)
        {
            DataTable dt = new DataTable();
            if (list == null && !list.Any()) { return dt; }

            if (list.FirstOrDefault() is JObject)
            {
                return JObjectToDataTable(list, propertyName);
            }
            else
            {
                return ModelToDataTable(list, propertyName);
            }
        }
        #endregion

        #region JObjectToDataTable
        /// <summary>
        /// JObjectToDataTable
        /// </summary>
        /// <typeparam name="T">JObject</typeparam>
        /// <param name="list">DataTable 的 Columns (默认null，即list中所有属性都进行转换)</param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static DataTable JObjectToDataTable<T>(IEnumerable<T> list, params string[] propertyName)
        {
            //创建属性的集合
            List<JProperty> jpList = new List<JProperty>();
            DataTable dt = new DataTable();
            if (list == null && !list.Any()) { return dt; }
            // 获取第一个对象，解析其数据结构
            JToken firstItem = list.FirstOrDefault() as JObject;
            if (firstItem == null) { return dt; }

            if (propertyName.IsNotNullAndEmpty())
            {
                // 与指定列（columns）匹配，只获取相匹配的列
                foreach (var column in propertyName)
                {
                    foreach (JProperty p in firstItem)
                    {
                        if (p.Name == column)
                        {
                            jpList.Add(p);
                            dt.Columns.Add(p.Name/*, p.Value.Type.GetType()*/);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (JProperty p in firstItem)
                {
                    jpList.Add(p);
                    dt.Columns.Add(p.Name/*, p.Value.Type.GetType()*/);
                }
            }

            foreach (var item in list)
            {
                //创建一个DataRow实例
                DataRow row = dt.NewRow();
                jpList.ForEach(p =>
                    row[p.Name] = ((object)((item as JObject).SelectToken(p.Name).ToString())) == null
                    ? DBNull.Value
                    : ((object)((item as JObject).SelectToken(p.Name).ToString()))
                    );
                //加入到DataTable
                dt.Rows.Add(row);
            }
            return dt;
        }
        #endregion

        #region ModelToDataTable
        /// <summary>
        /// ModelToDataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static DataTable ModelToDataTable<T>(IEnumerable<T> list, params string[] propertyName)
        {
            //创建属性的集合
            List<PropertyInfo> pList = new List<PropertyInfo>();
            DataTable dt = new DataTable();
            if (list == null && !list.Any()) { return dt; }
            //Type type = typeof(T);
            Type type = list.First().GetType();

            if (propertyName.IsNotNullAndEmpty())
            {
                foreach (var column in propertyName)
                {
                    //把所有的public属性加入到集合 并添加DataTable的列  处理了对 Nullable 的转换
                    Array.ForEach<PropertyInfo>(type.GetProperties(), p =>
                    {
                        if (p.Name == column)
                        {
                            pList.Add(p);
                            dt.Columns.Add(p.Name, (p.PropertyType.IsGenericType) && (p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                ? p.PropertyType.GetGenericArguments()[0] :
                                p.PropertyType
                                );
                        }
                    });
                }
            }
            else
            {
                //把所有的public属性加入到集合 并添加DataTable的列  处理了对 Nullable 的转换
                Array.ForEach<PropertyInfo>(type.GetProperties(), p =>
                {
                    pList.Add(p);
                    dt.Columns.Add(p.Name, (p.PropertyType.IsGenericType) && (p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        ? p.PropertyType.GetGenericArguments()[0] :
                        p.PropertyType
                        );
                });
            }
            DataRow row;
            foreach (var item in list)
            {
                //创建一个 DataRow 实例
                row = dt.NewRow();
                //给 row 赋值
                pList.ForEach(p => row[p.Name] = p.GetValue(item, null) == null ? DBNull.Value : p.GetValue(item, null));
                //加入到DataTable
                dt.Rows.Add(row);
            }
            return dt;
        }
        #endregion

        #region DataTable 转换为List 集合
        /// <summary>
        /// DataTable 转换为List 集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="dt">DataTable</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable dt) where T : class, new()
        {
            //创建一个属性的列表
            List<PropertyInfo> prlist = new List<PropertyInfo>();
            //获取TResult的类型实例  反射的入口
            Type t = typeof(T);
            //获得TResult 的所有的Public 属性 并找出TResult属性和DataTable的列名称相同的属性(PropertyInfo) 并加入到属性列表 
            Array.ForEach<PropertyInfo>(t.GetProperties(), p => { if (dt.Columns.IndexOf(p.Name) != -1) prlist.Add(p); });
            //创建返回的集合
            List<T> oblist = new List<T>();

            foreach (DataRow row in dt.Rows)
            {
                //创建TResult的实例
                T ob = new T();
                //找到对应的数据  并赋值
                prlist.ForEach(p => { if (row[p.Name] != DBNull.Value) p.SetValue(ob, row[p.Name], null); });
                //放入到返回的集合中.
                oblist.Add(ob);
            }
            return oblist;
        }
        #endregion

        #region 集合不为空 且存在子项
        /// <summary>
        /// 集合不为空 且存在子项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNullAndEmpty(this DataTable s)
        {
            return s != null && s.Rows != null && s.Rows.Count > 0;
        }
        #endregion

        #region 查找行
        /// <summary>
        /// 查找行
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="jParams">new { key1=value1,key2=value2 }</param>
        /// <returns></returns>
        public static DataRow FindRow(this DataTable dt, dynamic jParams)
        {
            Dictionary<string, object> where = Extensions.DynamicToDictionary(jParams);
            foreach (DataRow row in dt.Rows)
            {
                var macthingCount = 0;
                foreach (var item in where)
                {
                    if (row[item.Key].ToString() == item.Value.ToString())
                    {
                        macthingCount++;
                    }
                }
                if (macthingCount == where.Count)
                {
                    return row;
                }
            }
            return null;
        }
        #endregion

        #region DataRowToObject
        /// <summary>
        /// DataRowToObject (一般用户返回一个前端时的 json 序列化，DataRow 序列化)
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ToDictionary(this DataRow row)
        {
            if (row?.Table?.Columns != null)
            {
                Dictionary<string, object> dataList = new Dictionary<string, object>();
                foreach (DataColumn column in row.Table.Columns)
                {
                    dataList.Add(column.ColumnName, row[column]);
                }
                return dataList;
            }
            return null;
        }
        #endregion

    }
}