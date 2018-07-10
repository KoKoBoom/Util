using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Management;
using System.Configuration;

namespace UtilTool
{
    /// <summary>
    /// 基础工具类
    /// </summary>
    public class Util
    {
        #region 获得用户IP
        /// <summary>
        /// 获得用户IP
        /// </summary>
        public static string GetUserIp()
        {
            string ip;
            string[] temp;
            bool isErr = false;
            if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_ForWARDED_For"] == null)
                ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
            else
                ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_ForWARDED_For"].ToString();
            if (ip.Length > 15)
                isErr = true;
            else
            {
                temp = ip.Split('.');
                if (temp.Length == 4)
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (temp[i].Length > 3) isErr = true;
                    }
                }
                else
                    isErr = true;
            }

            if (isErr)
                return "1.1.1.1";
            else
                return ip;
        }
        /// <summary>  
        /// 获取本机MAC地址  
        /// </summary>  
        /// <returns>本机MAC地址</returns>  
        public static string GetMacAddress()
        {
            try
            {
                string strMac = string.Empty;
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        strMac = mo["MacAddress"].ToString();
                    }
                }
                moc = null;
                mc = null;
                return strMac;
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// 获得服务器的域名路径，包含端口
        /// </summary>
        /// <returns></returns>
        public static string getServerPath()
        {
            HttpRequest req = System.Web.HttpContext.Current.Request;
            string serverpath = req.ServerVariables["SERVER_NAME"];
            bool ishascurrentport = false;
            for (int i = 0; i < ConfigurationManager.ConnectionStrings.Count; i++)
            {
                if (ConfigurationManager.ConnectionStrings[i].Name == "CURRENTPORT")
                {
                    ishascurrentport = true;
                }
            }
            if (ishascurrentport)
            {
                string currentport = ConfigurationManager.ConnectionStrings["CURRENTPORT"].ConnectionString;
                if (req.ServerVariables["SERVER_PORT"] != null && !"".Equals(req.ServerVariables["SERVER_PORT"]) && !currentport.Equals(req.ServerVariables["SERVER_PORT"])) { serverpath += ":" + req.ServerVariables["SERVER_PORT"]; }
            }
            else
            {
                if ("443".Equals(req.ServerVariables["SERVER_PORT"]))
                {
                    return "https://" + serverpath;
                }
                else if (req.ServerVariables["SERVER_PORT"] != null && !"".Equals(req.ServerVariables["SERVER_PORT"]) && !"80".Equals(req.ServerVariables["SERVER_PORT"])) { serverpath += ":" + req.ServerVariables["SERVER_PORT"]; }
            }
            return "http://" + serverpath;
        }
        #endregion

        #region 自动生成日期编号  
        /// <summary>  
        /// 自动生成编号  201008251145409865  
        /// </summary>  
        /// <returns></returns>  
        public static string CreateNo()
        {
            Random random = new Random();
            string strRandom = random.Next(1000, 10000).ToString(); //生成编号   
            string code = DateTime.Now.ToString("yyyyMMddHHmmss") + strRandom;//形如  
            return code;
        }
        #endregion

        #region 删除最后一个字符之后的字符  
        /// <summary>  
        /// 删除最后结尾的一个逗号  
        /// </summary>  
        public static string DelLastComma(string str)
        {
            return str.Substring(0, str.LastIndexOf(","));
        }
        /// <summary>  
        /// 删除最后结尾的指定字符后的字符  
        /// </summary>  
        public static string DelLastChar(string str, string strchar)
        {
            return str.Substring(0, str.LastIndexOf(strchar));
        }
        /// <summary>  
        /// 删除最后结尾的长度  
        /// </summary>  
        /// <param name="str"></param>  
        /// <param name="Length"></param>  
        /// <returns></returns>  
        public static string DelLastLength(string str, int Length)
        {
            if (string.IsNullOrEmpty(str))
                return "";
            str = str.Substring(0, str.Length - Length);
            return str;
        }
        #endregion

        #region 验证参数
        /// <summary>
        /// 验证参数
        /// <para>返回值：{ OK : false , Msg : "错误消息" }</para>
        /// </summary>
        /// <param name="parameters">需要进行验证的参数集合</param>
        /// <returns>{OK:false,Msg:"错误消息"}</returns>
        public static dynamic CheckParameters(params Parameter[] parameters)
        {
            //枚举 正则表达式 验证方式
            Regex regex;
            foreach (Parameter temp in parameters)
            {
                regex = new Regex(temp?.Regex ?? "");
                if (temp.IsCheck)
                {
                    if (string.IsNullOrWhiteSpace(temp?.Value) || (!string.IsNullOrEmpty(temp?.Regex) && !regex.IsMatch(temp?.Value)))
                    {
                        return new { OK = false, Msg = temp?.Msg };
                    }
                }
            }
            return new { OK = true };
        }

        /// <summary>
        /// 检测参数 如果验证不通过则直接抛出异常（异常会被程序自动捕获，返回给前端友好提示）
        /// </summary>
        /// <param name="parameters">需要进行验证的参数集合</param>
        public static void CheckParams(params Parameter[] parameters)
        {
            //枚举 正则表达式 验证方式
            Regex regex;
            foreach (Parameter temp in parameters)
            {
                regex = new Regex(temp?.Regex ?? "", temp.regexOptions);
                if (temp.IsCheck)
                {
                    if (string.IsNullOrWhiteSpace(temp?.Value) || (!string.IsNullOrEmpty(temp?.Regex) && !regex.IsMatch(temp?.Value)))
                    {
                        throw new ExceptionExtensions(temp.Msg);
                    }
                }
            }
        }

        #endregion

        #region 获取请求的Json参数
        /// <summary>
        /// 获取Post请求的Json参数
        /// </summary>
        /// <returns></returns>
        public static JObject GetJsonParamToJObject()
        {
            var stream = System.Web.HttpContext.Current.Request.InputStream;
            stream.Position = 0;
            using (StreamReader sw = new StreamReader(stream))
            {
                return JObject.Parse(sw.ReadToEnd());
            }
        }
        #endregion

        #region 检测 sql 注入
        /// <summary>
        /// 检测 sql 注入
        /// </summary>
        /// <param name="str"></param>
        public static void CheckSqlInjection(String str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                string sql_keyword = $@"'|and|exec|insert|select|delete|update|count|*|%|chr|mid|master|truncate|char|declare|;|or|-|+|,";

                string[] sql_keywords = sql_keyword.Split('|');
                if (sql_keywords.Any(x => str.ToLower().Split(' ').Any(p => p == x)))
                {
                    throw new ExceptionExtensions("参数包含敏感字符");
                }
            }
        }
        #endregion

        #region 获取当前网站URL
        /// <summary>
        /// 获取当前网站URL
        /// </summary>
        /// <returns></returns>
        public static string GetDomain()
        {
            HttpRequest request = HttpContext.Current.Request;
            string urlAuthority = request.Url.GetLeftPart(UriPartial.Authority);
            if (request.ApplicationPath == null || request.ApplicationPath == "/")
            {
                //当前部署在Web站点下
                return urlAuthority;
            }
            else
            {
                //当前部署在虚拟目录下
                return urlAuthority + request.ApplicationPath;
            }
        }
        #endregion
    }

    #region Regexs
    public class Regexs
    {
        /// <summary>
        /// Sql注入正则表达式
        /// </summary>
        public static readonly string SqlInjection = $@"\s*('|and|exec|insert|select|delete|update|count|\\*|\\%|chr|mid|master|truncate|char|declare|;|or|\\-|\\+|,){1}\s*";

    }
    #endregion

    #region 参数验证
    /// <summary>
    /// 参数验证
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// 待验证的值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 错误提示消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 正则表达式 （可空）
        /// </summary>
        public string Regex { get; set; }

        /// <summary>
        /// 是否对 Value 值进行验证 默认为 true
        /// <para>当IsCheck达成某个条件时，才对Value进行验证</para>
        /// <para>例如：IsCheck = string.IsNullOrEmpty(Temp) 即Temp不为空时，才对Value进行验证</para>
        /// </summary>
        public bool IsCheck { get; set; } = true;

        /// <summary>
        /// 正则表达式配置，如忽略大小写
        /// </summary>
        public RegexOptions regexOptions { get; set; }
    }
    #endregion

}