using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace UtilTool
{
    /**
     * WEB API 统一返回值
     * 
     */

    #region 返回成功结果 JSON
    /// <summary>
    /// 返回成功结果 JSON
    /// </summary>
    public class ApiResultToSuccess : ApiResult
    {
        public ApiResultToSuccess() { }

        public ApiResultToSuccess(object data)
        {
            this.data = data;
        }

        /// <summary>
        /// 状态码
        /// </summary>
        public new ApiStatusCode errorCode { get; set; } = ApiStatusCode.OK;
        /// <summary>
        /// 返回成功与否
        /// </summary>
        public new bool success { get; set; } = true;
    }
    #endregion

    #region 返回失败结果 JSON 默认是 ApiStatusCode.Error 错误。
    /// <summary>
    /// 返回失败结果 JSON 默认是 ApiStatusCode.Error 错误。
    /// </summary>
    public class ApiResultToFail : ApiResult
    {

        public ApiResultToFail() { }

        public ApiResultToFail(string message)
        {
            this.message = message;
        }
        public ApiResultToFail(string message, ApiStatusCode errorCode)
        {
            this.message = message;
            this.errorCode = errorCode;
        }
        /// <summary>
        /// 状态码
        /// </summary>
        public new ApiStatusCode errorCode { get; set; } = ApiStatusCode.Error;

        /// <summary>
        /// 返回成功与否
        /// </summary>
        public new bool success { get; set; } = false;

        /// <summary>
        /// 返回信息，如果失败则是错误提示
        /// </summary>
        public new string message { get; set; } = ApiStatusCode.Error.GetEnumDesc();
    }
    #endregion

    #region 核心对象
    /// <summary>
    /// 返回JSON数据
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public ApiStatusCode errorCode { get; set; }
        /// <summary>
        /// 返回成功与否
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// 返回信息，如果失败则是错误提示
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 返回信息
        /// </summary>
        public object data { get; set; }

        public static ApiResult GetResult(bool flag)
        {
            if (flag)
            {
                return new ApiResultToSuccess();
            }
            else
            {
                return new ApiResultToFail();
            }
        }
    }
    #endregion

    #region 分页对象
    /// <summary>
    /// 返回分页JSON数据
    /// </summary>
    public class ApiPageResult : ApiResult
    {
        public ApiPageResult() { }
        public ApiPageResult(object data, int pageIndex, int pageSize, int totalCount)
        {
            this.data = data;
            this.pageIndex = pageIndex;
            this.pageSize = pageSize;
            this.totalCount = totalCount;
            this.success = true;
            this.errorCode = ApiStatusCode.OK;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int pageIndex { get; set; }
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int pageSize { get; set; }
        /// <summary>
        /// 总数据数
        /// </summary>
        public int totalCount { get; set; }
        /// <summary>
        /// 总页数
        /// </summary>
        public int totalPage
        {
            get
            {
                return pageSize != 0 ? totalCount % pageSize == 0 ? (totalCount / pageSize) : (totalCount / pageSize + 1) : 0;
            }
        }
    }
    #endregion

    #region 分页对象2
    /// <summary>
    /// 返回分页JSON数据
    /// </summary>
    public class ApiPageResult2 : ApiResult
    {
        /// <summary>
        /// 下标偏移量
        /// </summary>
        public int offsetIndex { get; set; }
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int pageSize { get; set; }
        /// <summary>
        /// 总数据数
        /// </summary>
        public int totalCount { get; set; }
        /// <summary>
        /// 当前 Data 的 Count
        /// </summary>
        public int currentCount
        {
            get
            {
                if (data is DataTable)
                {
                    return ((DataTable)data)?.Rows?.Count ?? 0;
                }
                if (data is IEnumerable<object>)
                {
                    var data1 = data as IEnumerable<object>;
                    return (data1 != null && data1.Any()) ? data1.Count() : 0;
                }
                return 0;
            }
        }
    }
    #endregion

    #region 泛型返回值
    /// <summary>
    /// 返回JSON数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T> : ApiResult
    {
        /// <summary>
        /// 返回信息
        /// </summary>
        public new T data { get; set; }
    }
    #endregion

    #region 泛型分页返回值
    /// <summary>
    /// 返回分页JSON数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiPageResult<T> : ApiPageResult
    {
        /// <summary>
        /// 返回信息
        /// </summary>
        public new T data { get; set; }
    }
    #endregion

    #region 自定义返回状态值
    /// <summary>
    /// 自定义返回状态值（可自行扩展自己需要的返回码）
    /// </summary>
    public enum ApiStatusCode
    {
        /// <summary>
        /// 请求成功
        /// </summary>
        [Description("请求成功")]
        OK = 0,
        /// <summary>
        /// 系统内部错误(代码错误)
        /// </summary>
        [Description("系统内部错误")]
        Error = 400,

        // 授权相关
        /// <summary>
        /// 没有权限
        /// </summary>
        [Description("没有权限")]
        Unauthorized = 1001,
        /// <summary>
        /// 授权用户不存在！
        /// </summary>
        [Description("授权用户不存在！")]
        InvalidClient = 1002,
        /// <summary>
        /// Token过期
        /// </summary>
        [Description("Token过期")]
        InvalidToken = 1003,
        /// <summary>
        /// 签名错误
        /// </summary>
        [Description("签名错误")]
        InvalidSign = 1004,
        /// <summary>
        /// 无效的时间戳
        /// </summary>
        [Description("无效的时间戳")]
        InvalidTimeStamp = 1005,

        /// <summary>
        /// 访问次数超过限制
        /// </summary>
        [Description("访问次数超过限制")]
        LimitRequest = 1006,

        // 业务逻辑错误状态码 3000-3999
        /// <summary>
        /// 数据已存在
        /// </summary>
        [Description("数据已存在")]
        DataExisted = 3001,
        /// <summary>
        /// 数据不存在
        /// </summary>
        [Description("数据不存在")]
        NotFound = 3002,
        /// <summary>
        /// 参数错误(invalid parameter)
        /// </summary>
        [Description("参数错误")]
        InvalidParam = 3003,

        // 系统异常 5000-5999
        /// <summary>
        /// 内部系统异常
        /// </summary>
        [Description("内部系统异常")]
        InternalServerError = 5001,

        [Description("付款失败")]
        PayError = 6010

    }
    #endregion
}