# Util
整合好用的第三方常用工具
## 使用方式
```
	using UtilTool;
	// 常用数据转换
	var number = "123".To<int>();
	var datetime = "2018-07-10".To<DateTime>();

	// 序列化
	var obj = "{\"id\":1,\"name\":\"小李子\"}".ToObject<User>();
	var obj = new { id = 1, name="小李子" }.ToJson();

	// 日志使用
	LogHelper.Info("日志测试");
	LogHelper.Error("日志测试");

	// 文件压缩、解压
	CompressHelper.Compression("C:\\Temp\\","D:\\Rar\\temp.rar");
	CompressHelper.Decompression("D:\\Rar\\temp.rar");

	// 接收上传文件
	ReceiveData.Save(Request);

	// WEB API 统一返回值
	return new ApiResultSuccess(new { id=1, name="小李子" });
	return new ApiResultToFail("执行操作发生错误");
```