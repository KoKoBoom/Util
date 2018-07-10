using System;
using System.Security.Cryptography;
using System.Text;

namespace UtilTool
{
    /// <summary>
    /// BaseRandom
    /// 产生随机数
    /// 
    /// 随机数管理，最大值、最小值可以自己进行设定。
    /// 
    /// 随机数只保证同一时间下不会产生相同的值，但不保证在范围内不产生重复值，如1-10范围，产生的第11个值必定是重复值。区别于唯一键，不要混淆概念。
    /// 
    /// </summary>
    public class BaseRandom
    {

        /**
         *  GetRandomByIncrement 重复小
         *  GetRandomByGUID 15W 记录  1W 左右重复 重复大
         *  GetRandom 15W 记录  1W 左右重复 重复大
         *  GenerateRandomInteger 15W 记录  1W 左右重复
         * 
         **/

        private readonly static string RandomString = "0123456789ABCDEFGHIJKMLNOPQRSTUVWXYZ";
        private readonly static Random Random = new Random(~unchecked((int)DateTime.Now.Ticks));
        private readonly static RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private readonly static Random random_guid = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)); // 经并发测试依然有重复 （初步估计是因为 Next 方法不是安全的）


        #region Random + GUID 因子随机数
        /// <summary>
        /// <para>随机数</para>
        /// <para>压测：JMeter 10次循环  每次100个请求   测试两轮  生成2000个随机数  【3个重复】</para>
        /// <para>这个是GUID（UUID）生成器，出来的是128-bit的字节数组，通常被表示为8-4-4-4-12的32个hex字符。</para>
        /// <para>  缺点：生成长度一定，而且生成出来的结果可能与环境相关，在高安全需求的环境不适用；</para>
        /// <para>  优点：有强大的数学理论支持，在每秒产生10亿笔UUID的情况下，100年后只产生一次重复的机率是50%；</para>
        /// <para>  效率：中，产生1,000,000个结果需要255ms（包含Guid对象创建时间）</para>
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static int GetRandomByGUID(int minValue, int maxValue)
        {
            return RandomInstanceByGuid().Next(minValue, maxValue);
        }

        /// <summary>
        /// <para>6 位随机数</para>
        /// <para>这个是GUID（UUID）生成器，出来的是128-bit的字节数组，通常被表示为8-4-4-4-12的32个hex字符。</para>
        /// <para>  缺点：生成长度一定，而且生成出来的结果可能与环境相关，在高安全需求的环境不适用；</para>
        /// <para>  优点：有强大的数学理论支持，在每秒产生10亿笔UUID的情况下，100年后只产生一次重复的机率是50%；</para>
        /// <para>  效率：中，产生1,000,000个结果需要255ms（包含Guid对象创建时间）</para>
        /// </summary>
        /// <param name="length">默认6</param>
        /// <returns></returns>
        public static int GetRandomByGUID(int length = 6)
        {
            if (length < 0 || length > 9) { throw new ArgumentOutOfRangeException(); }
            return RandomInstanceByGuid().Next(int.Parse("1".PadRight(length, '0')), int.Parse("".PadLeft(length, '9')));
        }

        static Random RandomInstanceByGuid()
        {
            return new Random(GuidToInt());
        }
        static int GuidToInt()
        {
            return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        }
        #endregion

        #region 使用 RandomNumberGenerator 生成的随机数
        /// <summary>
        /// <para>使用 RandomNumberGenerator 生成的随机数</para>
        /// <para>这个是用于产生密码的安全随机数生成器，产生出来的随机数离散度高，产生1,000,000个32位（8-byte）的随机数无重复</para>
        /// <para>  缺点：速度很慢，对比System.Random是两个数量级的效率差距；</para>
        /// <para>  优点：安全度高，产生的结果可看作环境无关，而且可以填充任意长度的字节数组；</para>
        /// <para>  效率：低，同一个对象产生1,000,000个结果需要4221ms（不含对象创建时间）</para>
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetCryptGenRandom()
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            return Math.Abs(BitConverter.ToInt32(randomBytes, 0));
        }

        public static long GetStrCryptGenRandom()
        {
            byte[] randomBytes = new byte[8];
            rng.GetBytes(randomBytes);
            return BitConverter.ToInt64(randomBytes, 0);
        }
        #endregion

        #region public static int GetRandom(int minimum, int maximal)
        /// <summary>
        /// 产生随机数
        /// 压测：JMeter 10次循环  每次100个请求   测试两轮  生成2000个随机数  【4个重复】
        /// </summary>
        /// <param name="minimum">最小值</param>
        /// <param name="maximal">最大值(不包括)</param>
        /// <returns>随机数</returns>
        public static int GetRandom(int minimum = 100000, int maximal = 999999)
        {
            return Random.Next(minimum, maximal);
        }
        #endregion

        #region 产生一个随机数
        /// <summary>
        /// 产生一个随机数
        /// 压测：JMeter 每次100个请求  10次循环   测试两轮  生成2000个随机数  【2个重复】
        /// </summary>
        /// <param name="min">Minimum number</param>
        /// <param name="max">Maximum number</param>
        /// <returns>Result</returns>
        public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
        {
            var randomNumberBuffer = new byte[10];
            new RNGCryptoServiceProvider().GetBytes(randomNumberBuffer);
            return new Random(BitConverter.ToInt32(randomNumberBuffer, 0)).Next(min, max);
        }
        #endregion


        #region public static string GetRandomString() 产生随机字符
        /// <summary>
        /// 产生随机字符 （数字+字符）
        /// </summary>
        /// <returns>字符串</returns>
        public static string GetRandomString(int randomLength = 6)
        {
            StringBuilder returnValue = new StringBuilder();
            for (int i = 0; i < randomLength; i++)
            {
                returnValue.Append(RandomString[Random.Next(0, RandomString.Length - 1)]);
            }
            return returnValue.ToString();
        }
        #endregion


        #region Random + 增量
        /// <summary>
        /// 保证一次性产生的随机数不重复（6位随机数测试，15W 并发不重复），但不保证本次产生的和下次的不重复（如：本次的15W和下次的15W肯定有重合部分）
        /// 压测：JMeter 10次循环  每次100个请求    测试两轮  生成2000个随机数  【无重复】
        /// BUG: 每秒生成的随机数不要大于1000个，不要修改本机时间，不然重新发布后可能会出现很多重复随机数
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static int GetRandomByIncrement(int minValue, int maxValue)
        {
            return RandomInstanceByPrimaryKey().Next(minValue, maxValue);
        }

        /// <summary>
        /// 保证一次性产生的随机数不重复（6位随机数测试，15W 并发不重复），但不保证本次产生的和下次的不重复（如：本次的15W和下次的15W肯定有重合部分）
        /// 压测：JMeter 10次循环  每次100个请求    测试两轮  生成2000个随机数  【无重复】
        /// BUG: 每秒生成的随机数不要大于1000个，不要修改本机时间，不然重新发布后可能会出现很多重复随机数
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int GetRandomByIncrement(int length = 6)
        {
            if (length < 0 || length > 9) { throw new ArgumentOutOfRangeException(); }
            return RandomInstanceByPrimaryKey().Next(int.Parse("1".PadRight(length, '0')), int.Parse("".PadLeft(length, '9')));
        }

        static Random RandomInstanceByPrimaryKey()
        {
            return new Random((int)DataBaseGenerator.GetPrimaryKey());
        }
        #endregion


        #region 随机密码
        /// <summary>
        /// 随机密码
        /// </summary>
        /// <param name="length">密码长度</param>
        /// <param name="specialCharactersNumber">特殊字符数量</param>
        /// <returns></returns>
        public static string GetRandomPwd(int length = 10, int specialCharactersNumber = 2)
        {
            return System.Web.Security.Membership.GeneratePassword(length, specialCharactersNumber);
        }
        #endregion
    }

    /// <summary>
    /// 唯一键
    /// 每秒增长数不要大于1000个，不要修改本机时间，不然重新发布后可能会出现重复
    /// </summary>
    public static class DataBaseGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        private static long seed = long.Parse(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString("0"));

        /// <summary>
        /// 原子操作递增生成唯一键
        /// <para>500W 并发量 生成时间为 300ms 左右</para>
        /// </summary>
        /// <returns></returns>
        public static long GetPrimaryKey()
        {
            return System.Threading.Interlocked.Increment(ref seed);
        }
    }
}