using System;
using System.Security.Cryptography;
using System.Text;

namespace UtilTool
{
    /// <summary>
    /// 加密
    /// </summary>
    public class Encrypt
    {
        #region MD5加密
        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="strText">要加密字符串</param>
        /// <returns></returns>
        public static string MD5Encrypt(string strText)
        {
            string ret = "";
            if (!string.IsNullOrEmpty(strText))
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strText);
                bytes = md5.ComputeHash(bytes);
                md5.Clear();
                for (int i = 0; i < bytes.Length; i++)
                {
                    ret += bytes[i].ToString("X2");
                }
            }

            return ret;
        }
        #endregion

        #region SHA1 加密，返回大写字符串
        /// <summary>  
        /// SHA1 加密，返回大写字符串  
        /// </summary>  
        /// <param name="content">需要加密字符串</param>  
        /// <returns>返回40位UTF8 大写</returns>  
        public static string SHA1(string content)
        {
            return SHA1(content, Encoding.UTF8);
        }

        /// <summary>  
        /// SHA1 加密，返回大写字符串  
        /// </summary>  
        /// <param name="content">需要加密字符串</param>  
        /// <param name="encode">指定加密编码</param>  
        /// <returns>返回40位大写字符串</returns>  
        public static string SHA1(string content, Encoding encode)
        {
            try
            {
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                byte[] bytes_in = encode.GetBytes(content);
                byte[] bytes_out = sha1.ComputeHash(bytes_in);
                sha1.Dispose();
                string result = BitConverter.ToString(bytes_out);
                result = result.Replace("-", "");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("SHA1加密出错：" + ex.Message);
            }
        }
        #endregion

        #region SHA1加密
        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="mk"></param>
        /// <param name="secretkey"></param>
        /// <returns></returns>
        public static string HmacSha1AndBase64(string mk, string secretkey)
        {
            HMACSHA1 hmacsha1 = new HMACSHA1();
            hmacsha1.Key = Encoding.UTF8.GetBytes(secretkey);
            byte[] dataBuffer = Encoding.UTF8.GetBytes(mk);
            byte[] hashBytes = hmacsha1.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
        #endregion

        #region SHA256加密
        /// <summary>
        /// SHA256加密
        /// </summary>
        /// <param name="mk"></param>
        /// <param name="secretkey"></param>
        /// <returns></returns>
        public static string HmacSha256AndBase64(string mk, string secretkey)
        {
            HMACSHA256 hmacsha256 = new HMACSHA256();
            hmacsha256.Key = Encoding.UTF8.GetBytes(secretkey);
            byte[] dataBuffer = Encoding.UTF8.GetBytes(mk);
            byte[] hashBytes = hmacsha256.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
        #endregion
    }
}
