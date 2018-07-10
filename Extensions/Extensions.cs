using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UtilTool
{
    /// <summary>
    /// 通用方法扩展类
    /// </summary>
    public static class Extensions
    {
        #region 通用类型转换 对 Nullable<T> 进行了处理
        //public static T To<T>(this object value)
        //{
        //    if (null == value)
        //    {
        //        return default(T);
        //    }

        //    if (!typeof(T).IsGenericType)
        //    {
        //        return (T)Convert.ChangeType(value, typeof(T));
        //    }
        //    else
        //    {
        //        Type genericTypeDefinition = typeof(T).GetGenericTypeDefinition();
        //        if (genericTypeDefinition == typeof(Nullable<>))
        //        {
        //            return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)));
        //        }
        //    }
        //    throw new InvalidCastException(string.Format("Invalid cast from type \"{0}\" to type \"{1}\".", value.GetType().FullName, typeof(T).FullName));
        //}

        //public static object ChangeType(object value, Type conversionType)
        //{
        //    // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
        //    // checking properties on conversionType below.
        //    if (conversionType == null)
        //    {
        //        throw new ArgumentNullException("conversionType");
        //    } // end if

        //    // If it's not a nullable type, just pass through the parameters to Convert.ChangeType

        //    if (conversionType.IsGenericType &&
        //      conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        //    {
        //        // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
        //        // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
        //        // determine what the underlying type is
        //        // If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
        //        // have a type--so just return null
        //        // Note: We only do this check if we're converting to a nullable type, since doing it outside
        //        // would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
        //        // value is null and conversionType is a value type.
        //        if (value == null)
        //        {
        //            return null;
        //        } // end if

        //        // It's a nullable type, and not null, so that means it can be converted to its underlying type,
        //        // so overwrite the passed-in conversion type with this underlying type
        //        System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(conversionType);

        //        conversionType = nullableConverter.UnderlyingType;
        //    } // end if

        //    // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
        //    // nullable type), pass the call on to Convert.ChangeType
        //    return Convert.ChangeType(value, conversionType);
        //}
        #endregion

        #region 通用类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <typeparam name="T">转换目标类型</typeparam>
        /// <param name="value">需要转换的值</param>
        /// <param name="isReturnDefault">true:如果异常则返回默认的（T）的值，false:抛出异常</param>
        /// <returns>T</returns>
        public static T To<T>(this object value, bool isReturnDefault = true)
        {
            var result = default(T);
            try
            {
                if (value != null && !typeof(T).IsGenericType)
                {// 不为空 并且不是泛型
                    result = (T)Convert.ChangeType(value, typeof(T));
                }
                else if (value != null && typeof(T).IsGenericType)
                {// 不为空 但是泛型
                    Type genericTypeDefinition = typeof(T).GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(Nullable<>))
                    {
                        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)));
                    }
                }
                else if (!isReturnDefault)
                {// 无法转换时，抛出异常
                    throw new ArgumentNullException();
                }
            }
            catch (Exception) { if (!isReturnDefault) { throw; } }
            return result;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <typeparam name="T">转换目标类型</typeparam>
        /// <param name="value">需要转换的值</param>
        /// <param name="defaultValue">转换失败时的默认值 当此值的类型为bool类型时，isReturnDefault必须赋值</param>
        /// <param name="isReturnDefault">true:如果异常则返回默认的（T）的值，false:抛出异常</param>
        /// <returns>T</returns>
        public static T To<T>(this object value, T defaultValue, bool isReturnDefault = true)
        {
            try
            {
                if (value != null && !typeof(T).IsGenericType)
                {// 不为空 并且不是泛型
                    defaultValue = (T)Convert.ChangeType(value, typeof(T));
                }
                else if (value != null && typeof(T).IsGenericType)
                {// 不为空 但是泛型
                    Type genericTypeDefinition = typeof(T).GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(Nullable<>))
                    {
                        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)));
                    }
                }
                else if (!isReturnDefault)
                {// 无法转换时，抛出异常
                    throw new ArgumentNullException();
                }
            }
            catch (Exception) { if (!isReturnDefault) { throw; } }
            return defaultValue;
        }

        #endregion

        #region 时间戳转为C#格式时间
        /// <summary>
        /// 时间戳转为C#格式时间
        /// </summary>
        /// <param name="timestamp">时间戳的值</param>
        /// <returns></returns>
        public static DateTime ToDateTimeByTimestamp(this int timestamp)
        {
            DateTime dateTimeBegin = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timestamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);

            return dateTimeBegin.Add(toNow);
        }
        #endregion

        #region DateTime时间格式转换为Unix时间戳格式
        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int ToTimestamp(this DateTime dateTime)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(dateTime - startTime).TotalSeconds;
        }
        #endregion

        #region 序列 反序列
        /* Newtonsoft 高级用法：http://www.cnblogs.com/yanweidie/p/4605212.html */
        #endregion

        #region 序列化
        /// <summary>
        /// 将 obj 序列化为 JSON 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson(this object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// 将 json 序列化为 JObject
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static JObject ToJObject(this string json)
        {
            return JObject.Parse(json);
        }
        #endregion

        #region 反序列化
        /// <summary>
        /// 将 Json 反序列为 T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T ToObject<T>(this string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
        #endregion

        #region 转换为可动态添加属性的对象
        /// <summary>
        /// 转换为可动态添加属性的对象
        /// </summary>
        /// <typeparam name="T">一般用 dynamic</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T ToExpandoObject<T>(this object obj)
        {
            return JsonConvert.DeserializeObject<T>(ToJson(obj), new ExpandoObjectConverter());
        }
        #endregion

        #region List扩展

        #region 检查是否为空  不检查集合数量
        /// <summary>
        /// 检查是否为空  不检查集合数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNull<T>(this IEnumerable<T> s)
        {
            return s == null;
        }
        #endregion

        #region 如果为空则返回一个数量为0的List<T>  一般用于 foreach  例如 foreach(var item in list.WhenNullThenDefault())
        /// <summary>
        /// 如果为空则返回一个数量为0的List[T]   一般用于 foreach  例如 foreach(var item in list.WhenNullThenDefault())
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IEnumerable<T> WhenNullThenDefault<T>(this IEnumerable<T> s)
        {
            return s == null ? new List<T>(0) : s;
        }
        #endregion

        #region 检查集合不为空 不检查子项个数
        /// <summary>
        /// 不为空 不检查子项个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNull<T>(this IEnumerable<T> s)
        {
            return s != null;
        }
        #endregion

        #region 集合为空或者子项个数为0
        /// <summary>
        /// 集合为空或者子项个数为0
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> s)
        {
            return s == null || !s.Any();
        }
        #endregion

        #region 集合不为空 且存在子项
        /// <summary>
        /// 集合不为空 且存在子项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNullAndEmpty<T>(this IEnumerable<T> s)
        {
            return s != null && s.Any();
        }
        #endregion

        #endregion

        #region IsNullOrEmpty IsNullOrWhiteSpace IsNotNullAndEmpty IsNotNullAndWhiteSpace
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s.ToString());
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static bool IsNotNullAndEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static bool IsNotNullAndWhiteSpace(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }
        #endregion

        #region 解析参数
        /// <summary>
        /// 解析参数
        /// </summary>
        /// <param name="dynamicParams"></param>
        /// <returns></returns>
        public static Dictionary<string, object> DynamicToDictionary(this object dynamicParams)
        {
            if (dynamicParams == null) { return null; }
            Dictionary<string, object> dic = new Dictionary<string, object>();
            if (dynamicParams is System.Dynamic.ExpandoObject)
            {
                ((IDictionary<String, Object>)dynamicParams).ToList().ForEach(x => dic.Add(x.Key, x.Value));
            }
            else
            {
                dynamicParams.GetType().GetProperties().ToList().ForEach(x =>
                dic.Add(x.Name, x.GetValue(dynamicParams, null))
                );
            }
            return dic;
        }
        #endregion

        #region 枚举扩展
        /// <summary>
        /// 获取枚举的描述
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetEnumDesc(this ApiStatusCode enumValue)
        {
            string str = enumValue.ToString();
            System.Reflection.FieldInfo field = enumValue.GetType().GetField(str);
            object[] objs = field.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
            if (objs == null || objs.Length == 0) return string.Empty;
            System.ComponentModel.DescriptionAttribute da = (System.ComponentModel.DescriptionAttribute)objs[0];
            return da.Description;
        }
        #endregion

        #region IsPropertyExist
        /// <summary>
        /// 检测 dynamic 、 ExpandoObject 的某个属性是否存在
        /// </summary>
        /// <param name="data"></param>
        /// <param name="propertyname"></param>
        /// <returns></returns>
        public static bool IsPropertyExist(this object data, string propertyname)
        {
            if (data is System.Dynamic.ExpandoObject)
                return ((IDictionary<string, object>)data).ContainsKey(propertyname);
            return data.GetType().GetProperty(propertyname) != null;
        }
        #endregion

        #region IsAnonymousType
        /// <summary>
        /// 判断是否是匿名类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAnonymousType(this Type type)
        {
            if (!type.IsGenericType)
                return false;

            if ((type.Attributes & TypeAttributes.NotPublic) != TypeAttributes.NotPublic)
                return false;

            if (!Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                return false;

            return type.Name.Contains("AnonymousType");
        }
        #endregion


    }
}