using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;  /* Microsoft.AspNet.WebApi.Core */
using System.Threading.Tasks;
using System.Web;

namespace UtilTool
{
    /// <summary>
    /// WEB API 接口接收上传文件及Form数据
    /// </summary>
    public class ReceiveData
    {
        #region WEB API 接口接收上传文件及Form数据
        /// <summary>
        /// 获得上传文件的 Bytes
        /// </summary>
        /// <returns>
        ///     byte[] bytes
        ///     string filename
        /// </returns>
        public static async Task<dynamic> GetUploadBytes(HttpRequestMessage request)
        {
            IsMimeMultipartContent(request);
            var provider = await GetProvider(request);
            byte[] bytes = new byte[0];
            string filename = "";
            foreach (var fileContent in provider.FileContents)
            {
                var stream = await fileContent.ReadAsStreamAsync();
                bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                // 初始化流的位置
                stream.Seek(0, SeekOrigin.Begin);
                filename = fileContent.Headers.ContentDisposition.FileName.Trim('"');
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            foreach (var key in provider.FormData.AllKeys)
            {//接收FormData  
                dic.Add(key, provider.FormData[key]);
            }

            return new
            {
                Bytes = bytes,
                FormData = dic,
                Filename = filename
            };
        }


        /// <summary>
        /// 获得上传文件流 Stream
        /// </summary>
        /// <returns></returns>
        public static async Task<Stream> GetUploadStream(HttpRequestMessage request)
        {
            IsMimeMultipartContent(request);
            var provider = await GetProvider(request);
            foreach (var fileContent in provider.FileContents)
            {
                return await fileContent.ReadAsStreamAsync();
            }
            return null;
        }

        /// <summary>
        /// <para>保存文件（保存方式是使用【流式存储】(边读边存)）</para>
        /// <para>Item1:文件本地路径</para>
        /// <para>Item2:form参数</para>
        /// <para>Item3:文件外网路径</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="savePath">保存的目录路径</param>
        /// <param name="fileNameWithoutExtension"> 新文件名（无后缀）【null:随机一个名字，如果同名则会覆盖同名文件（暂不支持分片上传）】 </param>
        /// <returns></returns>
        public async Task<Tuple<string, IDictionary<string, object>, string>> SaveFile(HttpRequestMessage request, string savePath = "/Uploads/", string fileNameWithoutExtension = null)
        {
            IsMimeMultipartContent(request);
            string root = CreateDirectory(savePath);
            var filePath = "";
            var provider = new ReNameMultipartFormDataStreamProvider(root, fileNameWithoutExtension);
            await request.Content.ReadAsMultipartAsync(provider);
            foreach (MultipartFileData file in provider.FileData)
            {
                filePath = file.LocalFileName;
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            foreach (var key in provider.FormData.AllKeys)
            {//接收FormData  
                dic.Add(key, provider.FormData[key]);
            }

            //返回上传后的文件全路径 和 其它参数
            return new Tuple<string, IDictionary<string, object>, string>(filePath, dic, Util.GetDomain() + "/" + filePath.Remove(0, HttpContext.Current.Server.MapPath("~").Length).Replace("\\", "/"));
            //return new
            //{
            //    FilePath = filePath,
            //    FormData = dic,
            //    HttpFilePath = Util.GetDomain() + "/" + filePath.Remove(0, HttpContext.Current.Server.MapPath("~").Length).Replace("\\", "/")
            //};
        }

        private static string CreateDirectory(string savePath)
        {
            string root = HttpContext.Current.Server.MapPath(savePath);
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            return root;
        }

        private static async System.Threading.Tasks.Task<MultipartFormDataMemoryStreamProvider> GetProvider(HttpRequestMessage request)
        {
            var provider = new MultipartFormDataMemoryStreamProvider();
            await request.Content.ReadAsMultipartAsync(provider);
            return provider;
        }

        private static void IsMimeMultipartContent(HttpRequestMessage request)
        {
            if (!request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(System.Net.HttpStatusCode.UnsupportedMediaType);
            }
        }
        #endregion

        #region 提供小文件下载
        /// <summary>
        /// 将二进制文件写到输出流中，提供给客户端下载
        /// </summary>
        /// <param name="fileBytes">文件的二进制数组</param>
        /// <param name="fileName">文件名称（包括后缀）</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        public HttpResponseMessage ResponseOutputStream(HttpRequestMessage request, byte[] fileBytes, string fileName = null)
        {
            return ResponseOutputStream(request.CreateResponse(), fileBytes, fileName);
        }

        /// <summary>
        /// 将二进制文件写到输出流中，提供给客户端下载
        /// </summary>
        /// <param name="fileBytes">文件的二进制数组</param>
        /// <param name="fileName">文件名称（包括后缀）</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        public HttpResponseMessage ResponseOutputStream(HttpResponseMessage response, byte[] fileBytes, string fileName = null)
        {
            response.Content = new PushStreamContent(async (stream, content, context) =>
            {
                await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                stream.Close();
            });
            // response.Content = new StreamContent(new System.IO.MemoryStream(bytes));

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-steam");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            if (fileName != null)
            {
                response.Content.Headers.ContentDisposition.FileName = fileName;
            }
            return response;
        }
        #endregion

    }

    #region ReNameMultipartFormDataStreamProvider
    public class ReNameMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        string _fileNameWithoutExtension = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileNameWithoutExtension">新文件名(无后缀)  默认：随机一个名字</param>
        public ReNameMultipartFormDataStreamProvider(string root, string fileNameWithoutExtension = null)
            : base(root, 4096)
        {
            this._fileNameWithoutExtension = fileNameWithoutExtension;
        }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            //截取文件扩展名
            string exp = Path.GetExtension(headers.ContentDisposition.FileName.TrimStart('\"').TrimEnd('\"'));
            if (string.IsNullOrWhiteSpace(_fileNameWithoutExtension))
            {
                _fileNameWithoutExtension = base.GetLocalFileName(headers);
            }
            return _fileNameWithoutExtension + exp;
        }
    }
    #endregion

    #region MultipartFormDataMemoryStreamProvider
    public class MultipartFormDataMemoryStreamProvider : MultipartStreamProvider
    {
        private NameValueCollection _formData = new NameValueCollection();
        private Collection<bool> _isFormData = new Collection<bool>();
        /// <summary>
        /// 获取文件对应的HttpContent集合,文件如何读取由实际使用方确定，可以ReadAsByteArrayAsync，也可以ReadAsStreamAsync
        /// </summary>
        public Collection<HttpContent> FileContents
        {
            get
            {
                //两者总数不一致，认为未执行过必须的Request.Content.ReadAsMultipartAsync(provider)方法
                if (this._isFormData.Count != this.Contents.Count)
                {
                    throw new InvalidOperationException("System.Net.Http.HttpContentMultipartExtensions.ReadAsMultipartAsync must be called first!");
                }
                return new Collection<HttpContent>(this.Contents.Where((ct, idx) => !this._isFormData[idx]).ToList());
            }
        }
        /// <summary>Gets a <see cref="T:System.Collections.Specialized.NameValueCollection" /> of form data passed as part of the multipart form data.</summary>
        /// <returns>The <see cref="T:System.Collections.Specialized.NameValueCollection" /> of form data.</returns>
        public NameValueCollection FormData
        {
            get
            {
                return this._formData;
            }
        }

        public override async Task ExecutePostProcessingAsync()
        {
            for (var i = 0; i < this.Contents.Count; i++)
            {
                if (!this._isFormData[i])//非文件
                {
                    continue;
                }
                var formContent = this.Contents[i];
                ContentDispositionHeaderValue contentDisposition = formContent.Headers.ContentDisposition;
                string formFieldName = UnquoteToken(contentDisposition.Name) ?? string.Empty;
                string formFieldValue = await formContent.ReadAsStringAsync();
                this.FormData.Add(formFieldName, formFieldValue);
            }
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            ContentDispositionHeaderValue contentDisposition = headers.ContentDisposition;
            if (contentDisposition == null)
            {
                throw new InvalidOperationException("Content-Disposition is null");
            }
            this._isFormData.Add(string.IsNullOrEmpty(contentDisposition.FileName));
            return new MemoryStream();
        }

        /// <summary>
        /// 复制自 System.Net.Http.FormattingUtilities 下同名方法，因为该类为internal，不能在其它命名空间下被调用
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string UnquoteToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }
            return token;
        }
    }
    #endregion

}
