using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace UtilTool
{
    /// <summary>
    /// 
    /// </summary>
    public class DBHelper
    {
        string connectionString = string.Empty;
        const string _strConn = "MSSQL";

        public DBHelper(string strConn = _strConn)
        {
            this.StrConn = strConn;
            var cache_key = $@"DBHelper_{strConn}";
            connectionString = CacheHelper.GetCache(cache_key)?.ToString();
            if (connectionString.IsNullOrWhiteSpace())
            {
                connectionString = ConfigurationManager.ConnectionStrings[strConn].ConnectionString;
                CacheHelper.SetCache(cache_key, connectionString);
            }
        }

        object lockobj = new object();

        public string StrConn { get; set; }

        private IDbConnection conn;
        private IDbConnection Conn
        {
            get
            {
                if (conn == null || conn.ConnectionString.IsNullOrWhiteSpace())
                {
                    lock (lockobj)
                    {
                        if (conn == null || conn.ConnectionString.IsNullOrWhiteSpace())
                        {
                            this.conn = new SqlConnection(connectionString);
                        }
                    }
                }
                return conn;
            }
            set
            {
                this.conn = value;
            }
        }

        private IDbTransaction Trans
        {
            get;
            set;
        }

        IDbTransaction CreateTransaction()
        {
            this.ConnOpen();
            return this.Conn.BeginTransaction();
        }

        void ConnOpen()
        {
            if (this.Conn.State != ConnectionState.Connecting && this.Conn.State != ConnectionState.Open)
            {
                lock (lockobj)
                {
                    if (this.Conn.State != ConnectionState.Connecting && this.Conn.State != ConnectionState.Open)
                    {
                        this.Conn.Open();
                    }
                }
            }
        }

        #region UseDBHelper
        /// <summary>
        /// DBHelper 实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="strConn"></param>
        /// <returns></returns>
        public static T UseDBHelper<T>(Func<DBHelper, T> action, string strConn = _strConn)
        {
            return action(new DBHelper(_strConn));
        }
        #endregion

        #region UseDBHelper
        /// <summary>
        /// DBHelper 实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="strConn"></param>
        /// <returns></returns>
        public static void UseDBHelper(Action<DBHelper> action, string strConn = _strConn)
        {
            action(new DBHelper(_strConn));
        }
        #endregion

        #region try..catch 释放资源
        /// <summary>
        /// try..catch 释放资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        T UseConnDispose<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            finally
            {
                if (this.Trans == null || this.Trans.Connection == null) this.Conn.Dispose();
            }
        }
        #endregion

        #region CreateCommand
        /// <summary>
        /// CreateCommand
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        IDbCommand CreateCommand(string sql, object sqlParams, CommandType commandType = CommandType.Text)
        {
            IDbCommand cmd = this.Conn.CreateCommand();
            cmd.CommandType = commandType;
            cmd.Transaction = this.Trans;
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            var pas = sqlParams is SqlParameter[] ? (sqlParams as SqlParameter[]) : GetSqlParameter(sqlParams);
            if (pas != null && pas.Any())
            {
                foreach (var item in pas)
                {
                    cmd.Parameters.Add(item);
                }
            }
            this.ConnOpen();
            return cmd;
        }
        #endregion

        #region UseDataAdapter
        /// <summary>
        /// UseDataAdapter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        T UseDataAdapter<T>(Func<SqlDataAdapter, SqlCommand, T> action, string sql, object sqlParams = null, CommandType commandType = CommandType.Text)
        {
            return UseConnDispose<T>(() =>
            {
                using (var cmd = CreateCommand(sql, sqlParams, commandType))
                {
                    using (SqlDataAdapter adp = new SqlDataAdapter())
                    {
                        return action(adp, cmd as SqlCommand);
                    }
                }
            });
        }
        #endregion

        #region UseTrans
        /// <summary>
        /// 使用事务
        /// 语法：
        /// 示例一：
        ///     DBHelper.UseDBHelper((_db) =>
        ///     {
        ///         return _db.UseTrans(() =>
        ///         {
        ///             _db.Insert("tb_test", new { user_name = "3333", age = 14 });
        ///             _db.Update("tb_test", new { user_name = "1122" }, new { id = 1 });
        ///             _db.Delete("tb_test", new { id = 8 });
        ///             _db.GetPagingTable("select * from tb_test", pageIndex, pageSize, " id desc");
        ///             _db.Delete("tb_test", new { id = 9 });
        ///             return _db.ExecuteScalar<DateTime>("select create_time from tb_test", new { id = 1 });
        ///         });
        ///     });
        /// 示例二：
        ///     DBHelper _db = new DBHelper();
        ///     _db.UseTrans(() =>
        ///     {
        ///         _db.Insert("tb_test", new { user_name = "3333", age = 14 });
        ///         _db.Update("tb_test", new { user_name = "1122" }, new { id = 1 });
        ///         _db.Delete("tb_test", new { id = 8 });
        ///         _db.GetPagingTable("select * from tb_test", pageIndex, pageSize, " id desc");
        ///         _db.Delete("tb_test", new { id = 9 });
        ///         return _db.ExecuteScalar<DateTime>("select create_time from tb_test", new { id = 1 });
        ///     });
        /// </summary>
        /// <param name="action"></param>
        public void UseTrans(Action action)
        {
            try
            {
                using (this.Trans = CreateTransaction())
                {
                    action();
                    this.Trans.Commit();
                }
            }
            catch
            {
                this.Trans.Rollback();
                if (this.Trans != null) { this.Trans.Dispose(); }
                throw;
            }
            finally
            {
                if (this.Trans != null) { this.Trans.Dispose(); }
                this.Conn.Dispose();
            }
        }

        /// <summary>
        /// 使用事务
        /// </summary>
        /// <param name="action"></param>
        public T UseTrans<T>(Func<T> action)
        {
            try
            {
                using (this.Trans = CreateTransaction())
                {
                    T t = action();
                    this.Trans.Commit();
                    return t;
                }
            }
            catch
            {
                this.Trans.Rollback();
                if (this.Trans != null) { this.Trans.Dispose(); }
                throw;
            }
            finally
            {
                if (this.Trans != null) { this.Trans.Dispose(); }
                this.Conn.Dispose();
            }
        }
        #endregion


        #region GetPagingTable

        #region GetPagingTable
        /// <summary>
        /// GetPagingTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="order"></param>
        /// <param name="total"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        //public DataTable GetPagingTable(string sql, int pageIndex, int pageSize, string order, out int total, object sqlParams = null)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append($@"select * from( select ROW_NUMBER() over(order by {order}) as rowNumber,* from (");
        //    sb.Append($@"select * from ({sql}) as alawliet");
        //    sb.Append($@" ) as t ) as t2");
        //    sb.Append($@" where rowNumber between {(((pageIndex - 1) * pageSize) + 1)} and { pageIndex * pageSize} ");
        //    sb.Append($@" order by {order}");

        //    var dt = GetDataTable(sb.ToString(), sqlParams);
        //    dt.Columns.Remove("rowNumber");
        //    total = ExecuteScalar<int>($"select count(1) from ({sql}) t1", sqlParams);

        //    return dt;
        //}
        #endregion

        #region GetPagingTable
        /// <summary>
        /// GetPagingTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="order"></param>
        /// <param name="total"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        //public Tuple<DataTable, int> GetPagingTable(string sql, int pageIndex, int pageSize, string order, object sqlParams = null)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append($@"select * from( select ROW_NUMBER() over(order by {order}) as rowNumber,* from (");
        //    sb.Append($@"select * from ({sql}) as alawliet");
        //    sb.Append($@" ) as t ) as t2");
        //    sb.Append($@" where rowNumber between {(((pageIndex - 1) * pageSize) + 1)} and { pageIndex * pageSize} ");
        //    sb.Append($@" order by {order}");

        //    var dt = GetDataTable(sb.ToString(), sqlParams);
        //    dt.Columns.Remove("rowNumber");
        //    var totalCount = ExecuteScalar<int>($"select count(1) from ({sql}) t1", sqlParams);

        //    return new Tuple<DataTable, int>(dt, totalCount);
        //}
        #endregion

        #region GetPagingTable  MSSQL 2012 以上版本
        /// <summary>
        /// GetPagingTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="order"></param>
        /// <param name="total"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        public DataTable GetPagingTable(string sql, int pageIndex, int pageSize, string order, out int total, object sqlParams = null)
        {
            total = ExecuteScalar<int>($"select count(1) from ({sql}) t1", sqlParams);
            return GetDataTable($@"select * from ({sql}) __t ORDER BY {order} OFFSET {(pageIndex - 1) * pageSize} ROWS FETCH NEXT { pageSize } ROWS ONLY", sqlParams);
        }
        #endregion

        #region GetPagingTable  MSSQL 2012 以上版本
        /// <summary>
        /// GetPagingTable  SQL2012 以上数据库版本使用
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="order"></param>
        /// <param name="total"></param>
        /// <param name="sqlParams"></param>
        /// <returns>Item1:数据  Item2:totalCount</returns>
        public Tuple<DataTable, int> GetPagingTable(string sql, int pageIndex, int pageSize, string order, object sqlParams = null)
        {
            var dt = GetDataTable($@"select * from ({sql}) __t ORDER BY {order} OFFSET {(pageIndex - 1) * pageSize} ROWS FETCH NEXT { pageSize } ROWS ONLY", sqlParams);
            var totalCount = ExecuteScalar<int>($"select count(1) from ({sql}) t1", sqlParams);
            return new Tuple<DataTable, int>(dt, totalCount);
        }
        #endregion

        #endregion

        #region First
        /// <summary>
        /// First
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public T First<T>(string sql, object sqlParams = null, CommandType commandType = CommandType.Text) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            return UseConnDispose<T>(() =>
            {
                using (var cmd = CreateCommand(sql, sqlParams, commandType))
                {
                    T model = Activator.CreateInstance<T>();
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var newEntity = Activator.CreateInstance(typeof(T));
                        foreach (var item in props)
                        {
                            item.SetValue(model, reader[item.Name] == DBNull.Value ? null : reader[item.Name]);
                        }
                    }
                    return model;
                }
            });
        }
        #endregion

        #region First
        /// <summary>
        /// First
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public T First<T>(object where = null) where T : class, new()
        {
            var props = typeof(T).GetProperties();
            var strSql = new StringBuilder($@"select top 1 * from {typeof(T).Name} where 1=1 {(AppendSqlParameters(GetSqlParameter(where), ParamAppendType.WHERE))}");
            return UseConnDispose<T>(() =>
            {
                using (var cmd = CreateCommand(strSql.ToString(), where))
                {
                    T model = Activator.CreateInstance<T>();
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var newEntity = Activator.CreateInstance(typeof(T));
                        foreach (var item in props)
                        {
                            item.SetValue(model, reader[item.Name] == DBNull.Value ? null : reader[item.Name]);
                        }
                        return model;
                    }
                    return null;
                }
            });
        }
        #endregion

        #region First
        /// <summary>
        /// First
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <returns>DataRow</returns>
        public DataRow First(string sql, object sqlParams = null)
        {
            var dt = GetDataTable(sql, sqlParams);
            if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }
        #endregion

        #region FirstDynamic
        /// <summary>
        /// System.Dynamic.ExpandoObject
        /// 获取对象 （一般用于查询后对数据继续进行处理，如添加额外的字段等等，如果不需要处理，用 GetDataTable 即可）
        /// 示例：db.GetModelBySql("select top 1 * from weixin_user where id=@id", new { id = 1 })?.nickname
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>System.Dynamic.ExpandoObject</returns>
        public dynamic FirstDynamic(string sql, object sqlParams = null)
        {
            IDictionary<string, object> dic = new System.Dynamic.ExpandoObject();
            var row = First(sql, sqlParams);
            if (row != null)
            {
                foreach (DataColumn column in row.Table.Columns)
                {
                    dic[column.ColumnName] = row[column.ColumnName];
                }
            }
            return dic;
        }
        #endregion

        #region GetDataTable
        /// <summary>
        /// GetDataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string sql, object sqlParams = null, CommandType commandType = CommandType.Text)
        {
            return UseDataAdapter((adp, cmd) =>
            {
                DataTable dt = new DataTable();
                adp.SelectCommand = cmd;
                adp.Fill(dt);
                return dt;
            }, sql, sqlParams);
        }
        #endregion

        #region GetDataSet
        /// <summary>
        /// GetDataSet
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public DataSet GetDataSet(string sql, object sqlParams, CommandType commandType = CommandType.Text)
        {
            return UseDataAdapter((adp, cmd) =>
            {
                DataSet ds = new DataSet();
                adp.SelectCommand = cmd;
                adp.Fill(ds);
                return ds;
            }, sql, sqlParams);
        }
        #endregion

        #region ExecuteScalar
        /// <summary>
        /// 返回 第一行，第一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string sql, object sqlParams = null, CommandType commandType = CommandType.Text)
        {
            return UseConnDispose<T>(() =>
            {
                using (var cmd = CreateCommand(sql, sqlParams, commandType))
                {
                    return cmd.ExecuteScalar().To<T>();
                }
            });
        }
        #endregion

        #region ExecuteNonQuery
        /// <summary>
        /// 执行增删改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="sqlParams"></param>
        /// <param name="commandType"></param>
        /// <returns>影响行数</returns>
        public int ExecuteNonQuery(string sql, object sqlParams = null, CommandType commandType = CommandType.Text)
        {
            return UseConnDispose<int>(() =>
            {
                using (var cmd = CreateCommand(sql, sqlParams, commandType))
                {
                    return cmd.ExecuteNonQuery();
                }
            });
        }
        #endregion

        /************ Insert/Update 参考于 http://blog.csdn.net/xpoer/article/details/26287739 ************/

        #region Insert
        /// <summary>
        /// 默认忽略插入主键id值
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <returns>插入数据的id</returns>
        public int Insert(string tableName, object obj)
        {
            return Insert(tableName, obj, new string[] { "id" });
        }
        #endregion

        #region Insert
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <param name="ignoreProperties"></param>
        /// <param name="where"></param>
        /// <param name="strExists"> exists:存在(满足where)条件则 INSERT ; not exists:不存在(不满足where)条件则 INSERT （场景：重复报名检测等等）</param>
        /// <returns></returns>
        public int Insert(string tableName, object obj, string[] ignoreProperties = null, object where = null, string strExists = " not exists ")
        {
            // 解析参数
            var sqlParams = GetSqlParameter(obj, ignoreProperties);
            var parameterNames = sqlParams.Select(x => x.ParameterName);
            var sql = "";
            if (where != null)
            {
                sql = $@"INSERT INTO {tableName}({string.Join(",", parameterNames)}) OUTPUT INSERTED.ID 
SELECT @{string.Join(",@", parameterNames)} WHERE {strExists} (select top 1 1 from {tableName} where 1=1 {AppendSqlParameters(GetSqlParameter(where), ParamAppendType.WHERE)})";
            }
            else
            {
                sql = $@"INSERT INTO {tableName}({string.Join(",", parameterNames)}) OUTPUT INSERTED.ID VALUES (@{string.Join(",@", parameterNames)})";
            }
            return ExecuteScalar<int>(sql, sqlParams);
        }
        #endregion

        #region Update
        /// <summary>
        /// 更新表（默认通过id来更新数据）
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values">格式：new { key=value }  [key:列名，value：值]</param>
        /// <returns></returns>
        public int Update(string tableName, object values)
        {
            var id = "";
            if (values.GetType().IsAnonymousType())
                id = values.GetType().GetProperty("id").GetValue(values).ToString();
            else
                id = ((dynamic)values).id.ToString();
            if (id.IsNullOrWhiteSpace()) { throw new ArgumentNullException("id 不能为空"); }
            return Update(tableName, values, $" and id={id}", new string[] { "id" });
        }
        #endregion

        #region Update
        /// <summary>
        /// 更新表（主键id无法更新，默认忽略）
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values">格式：new { key=value }  [key:列名，value：值]</param>
        /// <param name="where">格式：new { key=value }    [key:列名，value：值]</param>
        /// <returns></returns>
        public int Update(string tableName, object values, object where)
        {
            return Update(tableName, values, where, new string[] { "id" });
        }
        #endregion

        #region Update
        /// <summary>
        /// 更新表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values">格式：new { key=value }  [key:列名，value：值]</param>
        /// <param name="where">格式：new { key=value }    [key:列名，value：值]</param>
        /// <param name="ignoreProperties">需要忽略更新的字段</param>
        /// <returns></returns>
        public int Update(string tableName, object values, object where, params string[] ignoreProperties)
        {
            return UseConnDispose<int>(() =>
            {
                var builderObj = BuilderUpdateCmd(tableName, values, where, ignoreProperties);
                using (IDbCommand cmd = this.CreateCommand(builderObj.Item1, builderObj.Item2.Concat(builderObj.Item3).ToArray()))
                {
                    return cmd.ExecuteNonQuery();
                }
            });
        }
        #endregion

        #region 生成 Update 语句
        /// <summary>
        /// 生成 Update 语句
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        /// <param name="ignoreProperties"></param>
        /// <returns></returns>
        Tuple<string, SqlParameter[], SqlParameter[]> BuilderUpdateCmd(string tableName, object values, object where, params string[] ignoreProperties)
        {
            if (values == null) { throw new Exception("无更新列"); }
            if (where == null) { throw new Exception("不支持无条件更新"); }

            // 处理更新列
            var sqlParams = GetSqlParameter(values, ignoreProperties);

            var sb = new StringBuilder($@"UPDATE {tableName} SET {AppendSqlParameters(sqlParams, ParamAppendType.UPDATE)} WHERE 1=1 ");
            // 处理条件列
            SqlParameter[] sqlParamsWhere = new List<SqlParameter>().ToArray();
            if (where is String)
            {
                sb.Append(where.ToString().TrimStart(' ').IndexOf("and ") == 0 ? where.ToString() : (" and " + where.ToString()));
            }
            else
            {
                sqlParamsWhere = GetSqlParameter(where);
                sb.Append(AppendSqlParameters(sqlParamsWhere, ParamAppendType.WHERE));
            }

            return new Tuple<string, SqlParameter[], SqlParameter[]>(sb.ToString(), sqlParams, sqlParamsWhere);
            //return new
            //{
            //    Sql = sb.ToString(),  // update set field1=@field1,field2=@field2 where field1=@_field1 and field2=@_field2
            //    sqlParams,            // @field1=value1,@field2=value2
            //    sqlParamsWhere        // @_field1=value1,@_field2=value2
            //};
        }
        #endregion

        #region Delete
        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlParams">匿名参数 eg: new { id=1, name="xxx"}</param>
        /// <returns></returns>
        public int Delete(string tableName, object values)
        {
            var sqlParams = GetSqlParameter(values, null);
            if (sqlParams.IsNullOrEmpty()) throw new ArgumentNullException("必须设定删除条件");
            StringBuilder sql = new StringBuilder($@"delete {tableName} where 1=1 ");
            return ExecuteNonQuery(sql.Append(AppendSqlParameters(sqlParams, ParamAppendType.WHERE)).ToString(), sqlParams);
        }
        #endregion


        #region 批量插入数据
        /// <summary>  
        /// 往数据库中批量插入数据  
        /// </summary>  
        /// <param name="sourceDt">数据源表</param>  
        /// <param name="targetTableName">服务器上目标表</param>  
        public bool BulkToDB(DataTable sourceDt, string targetTableName, string[] ignoreProperties = null)
        {
            return UseConnDispose<bool>(() =>
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(this.Conn as SqlConnection, SqlBulkCopyOptions.Default, this.Trans as SqlTransaction))
                {
                    bulkCopy.DestinationTableName = targetTableName;
                    bulkCopy.BatchSize = sourceDt.Rows.Count;   //每一批次中的行数  

                    if (sourceDt != null && sourceDt.Rows != null && sourceDt.Rows.Count != 0)
                    {
                        foreach (DataColumn column in sourceDt.Columns)
                        {
                            if (ignoreProperties.IsNotNullAndEmpty() && ignoreProperties.Contains(column.ColumnName))
                            {
                                continue;
                            }
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }
                        bulkCopy.WriteToServer(sourceDt);
                        return true;
                    }
                    return false;
                }
            });
        }
        #endregion

        #region 批量更新
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="values">数据</param>
        /// <param name="updateFileds">需要更新的键 new { Name = "base_value", Type = SqlDbType.NVarChar, Size = 500 } </param>
        /// <param name="where">更新的条件 new { Name = "id",Value="@id", Type = SqlDbType.Int, Size = 4 }</param>
        /// <returns></returns>
        public int Updates(string tableName, JArray values, List<object> updateFileds, List<object> where)
        {
            if (values.IsNullOrEmpty())
            {
                throw new ArgumentException("values 的值为空或者长度为0");
            }

            DataTable dt = JArrayToDataTable(values, null);
            dt.AcceptChanges();
            foreach (DataRow row in dt.Rows)
            {
                row.SetModified();
            }
            List<SqlParameter> list = new List<SqlParameter>();
            List<string> updateFiledList = new List<string>();
            List<string> whereFiledList = new List<string>();

            foreach (dynamic item in updateFileds)
            {
                list.Add(new SqlParameter($@"@{item.Name}", item.Type, item.Size, item.Name));
                updateFiledList.Add($@"{item.Name}=@{item.Name}");
            }
            foreach (dynamic item in where)
            {
                // 参数化处理
                if ($@"@{item.Name}" == item.Value.ToString() && item?.Type != null && item.Size != null)
                {
                    list.Add(new SqlParameter($@"@{item.Name}", item.Type, item.Size, item.Name));
                }
                // 常量值处理
                whereFiledList.Add($@"{item.Name}={item.Value}");
            }
            return UseDataAdapter<int>((adp, cmd) =>
            {
                adp.UpdateCommand = cmd;
                var change_count = adp.Update(dt);
                dt.AcceptChanges();
                return change_count;
            }, $@"update {tableName} set {string.Join(",", updateFiledList)} where {string.Join(" and ", whereFiledList)}", list.ToArray());
        }
        #endregion


        #region JArrayToDataTable
        /// <summary>
        /// JArrayToDataTable
        /// </summary>
        /// <param name="values"></param>
        /// <param name="ignoreProperties"></param>
        /// <returns></returns>
        public static DataTable JArrayToDataTable(JArray values, string[] ignoreProperties)
        {
            DataTable dt = new DataTable();
            var columns = GetSqlParameter(values.First(), ignoreProperties);
            // 表头
            foreach (var item in columns)
            {
                dt.Columns.Add(item.ParameterName);
            }
            // 填充数据
            DataRow row;
            foreach (JToken item in values)
            {
                row = dt.NewRow();
                foreach (JProperty property in item)
                {
                    if (columns.Any(x => x.ParameterName == property.Name))
                    {
                        row[property.Name] = property.Value.ToString();
                    }
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
        #endregion

        #region 解析参数
        /// <summary>
        /// 解析参数
        /// </summary>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        private static SqlParameter[] GetSqlParameter(object sqlParams, string[] ignoreProperties = null)
        {
            if (sqlParams == null) { return null; }
            List<SqlParameter> list = new List<SqlParameter>();
            if (sqlParams is System.Dynamic.ExpandoObject)
            {
                if (ignoreProperties != null && ignoreProperties.Any())
                {
                    ((IDictionary<String, Object>)sqlParams).Where(x => !ignoreProperties.Contains(x.Key)).ToList().ForEach(x => list.Add(new SqlParameter(x.Key, x.Value ?? DBNull.Value)));
                }
                else
                {
                    ((IDictionary<String, Object>)sqlParams).ToList().ForEach(x => list.Add(new SqlParameter(x.Key, x.Value ?? DBNull.Value)));
                }
            }
            else if (sqlParams is JObject)
            {
                if (ignoreProperties != null && ignoreProperties.Any())
                {
                    ((JObject)sqlParams).Properties().Where(x => !ignoreProperties.Contains(x.Name)).ToList().ForEach(x => list.Add(new SqlParameter(x.Name, JValue.CreateNull().Equals(x.Value) ? DBNull.Value : x.Value.ToString() as dynamic)));
                }
                else
                {
                    ((JObject)sqlParams).Properties().ToList().ForEach(x => list.Add(new SqlParameter(x.Name, JValue.CreateNull().Equals(x.Value) ? DBNull.Value : x.Value.ToString() as dynamic)));
                }
            }
            else
            {
                if (ignoreProperties != null && ignoreProperties.Any())
                {
                    sqlParams.GetType().GetProperties().Where(x => !ignoreProperties.Contains(x.Name)).ToList().ForEach(x => list.Add(new SqlParameter(x.Name, x.GetValue(sqlParams, null) == null ? DBNull.Value : x.GetValue(sqlParams, null).ToString() as dynamic)));
                }
                else
                {
                    sqlParams.GetType().GetProperties().ToList().ForEach(x => list.Add(new SqlParameter(x.Name, x.GetValue(sqlParams, null) == null ? DBNull.Value : x.GetValue(sqlParams, null).ToString() as dynamic)));
                }
            }
            return list.ToArray();
        }
        #endregion

        #region Sql 条件拼接
        /// <summary>
        /// Sql 条件拼接
        /// </summary>
        /// <param name="sqlParams"></param>
        /// <param name="sqlParamaterType"></param>
        /// <returns></returns>
        private string AppendSqlParameters(SqlParameter[] sqlParams, ParamAppendType sqlParamaterType)
        {
            switch (sqlParamaterType)
            {
                case ParamAppendType.INSERT:
                    return AppendSqlParameters(sqlParams, ",");
                case ParamAppendType.UPDATE:
                    return AppendSqlParameters(sqlParams, ",", "=");
                case ParamAppendType.WHERE:
                    return AppendSqlParameters(sqlParams, " and ", "=");
            }
            return "";
        }
        #endregion

        #region Sql 条件拼接
        /// <summary>
        /// Sql 条件拼接
        /// <para> (separator="," , connector="" )   --->   @id,@id,@id 【insert】</para>
        /// <para> (separator="," , connector="=")   --->   id=@id,id=@id,id=@id 【update】</para>
        /// <para> (separator=" and " , connector="=")   --->  and id=@id and id=@id and id=@id 【where条件】</para>
        /// </summary>
        /// <param name="sqlParams"></param>
        /// <param name="separator">分割符  一般为 (,)、(and) </param>
        /// <param name="connector">连接符 = </param>
        /// <returns></returns>
        private string AppendSqlParameters(SqlParameter[] sqlParams, string separator = ",", string connector = "", string prefix = "@")
        {
            if (sqlParams.IsNotNullAndEmpty())
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in sqlParams)
                {
                    sb.Append($"{(connector.IsNotNullAndWhiteSpace() ? (item.ParameterName + connector) : "")}{prefix}{item.ParameterName}{separator}");
                }
                return (separator == " and " ? separator : "") + sb.Remove(sb.Length - separator.Length, separator.Length).ToString();
            }
            return "";
        }
        #endregion

    }

    /// <summary>
    /// Sql 条件拼接类型
    /// </summary>
    public enum ParamAppendType
    {
        /// <summary>
        /// 结果：@id,@id
        /// </summary>
        INSERT,
        /// <summary>
        /// 结果：id=@id,id=@id
        /// </summary>
        UPDATE,
        /// <summary>
        /// 结果：and id=@id and id=@id
        /// </summary>
        WHERE
    }
}
