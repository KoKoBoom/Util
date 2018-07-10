using Newtonsoft.Json;
using System;
using System.Linq;

namespace UtilTool
{
    /// <summary>
    /// JSON Web Tokens
    /// 参考于：https://segmentfault.com/a/1190000005047525
    /// </summary>
    public class JWT
    {
        public static readonly string Secretkey;
        /// <summary>
        /// 过期时间 （默认3600秒）
        /// </summary>
        public static readonly int? Timeout;

        #region 初始化 加密密钥 和 超时时间
        /// <summary>
        /// 读取 加密密钥 和 超时时间
        /// 先从 web.config 里面读取，如果未获取到
        /// 再从数据库读取，如果还未获取到
        /// 则使用默认值
        /// </summary>
        static JWT()
        {
            var db = new DBHelper();
            Secretkey = ConfigHelper.GetValue("jwt_secretkey");
            var _timeout = ConfigHelper.GetValue("jwt_timeout");

            if (string.IsNullOrWhiteSpace(Secretkey) || string.IsNullOrWhiteSpace(_timeout))
            {
                var jwtObj = db.First<api_jwt>($@"select * from [codebook].[dbo].[api_jwt] where is_delete=0 order by id desc");
                Secretkey = jwtObj.jwt_secretkey;
                Timeout = jwtObj.timeout;
            }

            if (string.IsNullOrWhiteSpace(Secretkey) || Timeout == null)
            {
                Secretkey = "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAKK5u3MLBkMcwqFn2oNzRs5vJSu/nCU7DTsSI+AYxfQkTmYlsavJ5G6+sGcWbfzhHkTkVHYLpGds0NNbvc069NUCAwEAAQ==";
                Timeout = 3600;
            }
        }
        #endregion

        #region 构造函数
        public JWT()
        {
            Header = new HeaderModel();
            Payload = new PayloadModel();
        }

        public JWT(HeaderModel header, PayloadModel payload)
        {
            Header = header;
            Payload = payload;
        }

        public JWT(HeaderModel header, PayloadModel payload, string signatureLocal)
        {
            Header = header;
            Payload = payload;
            SignatureLocal = signatureLocal;
        }
        #endregion

        #region 主要参数成员

        /// <summary>
        /// 头信息
        /// </summary>
        public HeaderModel Header { get; set; }
        /// <summary>
        /// 负载
        /// </summary>
        public PayloadModel Payload { get; set; }
        /// <summary>
        /// 服务器生成的签名
        /// </summary>
        public virtual string Signature()
        {
            switch (Header.alg.ToUpper())
            {
                case "HS256":
                default:
                    return Util.HmacSha256AndBase64($@"{Header.Base64String()}.{Payload.Base64String()}", Secretkey);
            }
        }

        /// <summary>
        /// 客户端传的签名
        /// </summary>
        private string SignatureLocal { get; set; }

        #endregion

        #region 生成 Token
        /// <summary>
        /// 生成 Token
        /// </summary>
        /// <returns></returns>
        public string Token()
        {
            return string.Format("{0}.{1}.{2}", Header.Base64String(), Payload.Base64String(), this.Signature());
        }
        #endregion

        #region 检测签名
        /// <summary>
        /// 检测签名
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckSignature()
        {
            //if (DateTime.Now.ToTimestamp() > Payload.exp)
            //{
            //    throw new CommonException(ApiStatusCode.InvalidToken);
            //}
            return SignatureLocal.Equals(this.Signature());
        }
        #endregion

        #region 将 token 转换为 JWT 对象
        /// <summary>
        /// 将 token 转换为 JWT 对象
        /// </summary>
        /// <param name="token">类似 xxxxx.yyyyy.zzzzz </param>
        /// <returns></returns>
        public static JWT ToJWT(string token)
        {
            var str_jwt = token?.Split('.');
            if (token.IsNotNullAndWhiteSpace() && str_jwt?.Count() == 3)
            {
                return new JWT(Base64ToUTF8(str_jwt[0]).ToObject<HeaderModel>(), Base64ToUTF8(str_jwt[1]).ToObject<PayloadModel>(), str_jwt[2]);
            }
            else
            {
                throw new ExceptionExtensions(ApiStatusCode.InvalidSign.GetEnumDesc(), ApiStatusCode.InvalidSign);
            }
        }
        #endregion

        #region Base64 解码
        /// <summary>
        /// Base64 解码
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static string Base64ToUTF8(string base64String)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
        }
        #endregion
    }

    public class HeaderModel
    {
        public string typ { get; set; } = "JWT";
        /// <summary>
        /// 默认 HS256 加密，暂不支持其它
        /// </summary>
        public string alg { get; set; } = "HS256";

        public string Base64String()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(this.ToJson()));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PayloadModel
    {
        /// <summary>
        /// Issuer，发行者
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string iss { get; set; } = "Taki";
        /// <summary>
        /// Subject，主题
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string sub { get; set; }
        /// <summary>
        /// Audience，观众
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string aud { get; set; }
        /// <summary>
        /// Expiration time，过期时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int exp { get; set; } = DateTime.Now.AddSeconds(JWT.Timeout.To<int>(3600)).ToTimestamp();
        /// <summary>
        /// Not before
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string nbf { get; set; }
        /// <summary>
        /// Issued at，发行时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int iat { get; set; } = DateTime.Now.ToTimestamp();
        /// <summary>
        /// JWT ID
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string jti { get; set; }
        public string Base64String()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(this.ToJson()));
        }
    }

    public class api_jwt
    {
        public int id { get; set; }
        public string jwt_secretkey { get; set; }
        public int timeout { get; set; }
        public int is_delete { get; set; }
        public DateTime create_time { get; set; }
    }

}