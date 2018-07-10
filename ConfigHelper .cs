using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilTool
{
    /// <summary>
    /// 配置文件帮助类 
    /// </summary>
    public class ConfigHelper
    {
        /// <summary>  
        /// 根据Key取Value值  
        /// </summary>  
        /// <param name="key"></param>  
        public static string GetValue(string key)
        {
            var cache = CacheHelper.GetCache(key)?.ToString();
            if (cache.IsNotNullAndWhiteSpace())
            {
                return cache;
            }
            else
            {
                cache = ConfigurationManager.AppSettings[key].ToString().Trim();
                if (cache.IsNullOrWhiteSpace()) { throw new ConfigurationErrorsException($@"无法在配置文件中找到key为：{key}的数据值！"); }
                CacheHelper.SetCache(key, cache);
                return cache;
            }
        }

        /// <summary>  
        /// 根据Key修改Value  
        /// </summary>  
        /// <param name="key">要修改的Key</param>  
        /// <param name="value">要修改为的值</param>  
        public static void SetValue(string key, string value)
        {
            ConfigurationManager.AppSettings.Set(key, value);
            CacheHelper.SetCache(key, value);
        }

        /// <summary>  
        /// 添加新的Key ，Value键值对  
        /// </summary>  
        /// <param name="key">Key</param>  
        /// <param name="value">Value</param>  
        public static void Add(string key, string value)
        {
            ConfigurationManager.AppSettings.Add(key, value);
        }

        /// <summary>  
        /// 根据Key删除项  
        /// </summary>  
        /// <param name="key">Key</param>  
        public static void Remove(string key)
        {
            ConfigurationManager.AppSettings.Remove(key);
        }
    }
}
