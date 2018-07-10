using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.IO;

namespace UtilTool
{
    /// <summary>
    /// 压缩、解压
    /// SharpCompress 0.22.0 版本
    /// 用法示例：https://github.com/adamhathcock/sharpcompress/blob/master/USAGE.md
    /// </summary>
    public class CompressHelper
    {
        #region 压缩（RAR、ZIP、GZIP、TAR、7Z）
        /// <summary>
        /// 压缩（RAR、ZIP、GZIP、TAR、7Z）
        /// </summary>
        /// <param name="filePath">需要被压缩的文件目录</param>
        /// <param name="rarFilePath">压缩文件的路径</param>
        public static void Compression(string filePath, string rarFilePath = "")
        {
            using (var archive = ZipArchive.Create())
            {
                archive.AddAllFromDirectory(Path.GetDirectoryName(filePath));
                archive.SaveTo(string.IsNullOrWhiteSpace(rarFilePath) ? filePath : rarFilePath, CompressionType.Deflate);
            }
        }
        #endregion

        #region 解压（RAR、ZIP、GZIP、TAR、7Z）
        /// <summary>
        /// 解压（RAR、ZIP、GZIP、TAR、7Z）
        /// </summary>
        /// <param name="filePath">压缩文件路径</param>
        /// <param name="strDirectory">解压到哪个目录</param>
        /// <param name="password">密码</param>
        /// <param name="overWrite">是否覆盖</param>
        public static void Decompression(string filePath, string strDirectory="", string password = null, bool overWrite = true)
        {
            if (!File.Exists(filePath)) { throw new FileNotFoundException(); }
            strDirectory = (string.IsNullOrWhiteSpace(strDirectory) ? Path.GetDirectoryName(filePath) : strDirectory).TrimEnd('\\') + "\\";

            using (Stream stream = File.OpenRead(filePath))
            {
                var reader = ReaderFactory.Open(stream, new ReaderOptions() { Password = password });
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(strDirectory, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = overWrite
                        });
                    }
                }
            }
        }
        #endregion

    }
}
