using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace OptionAmerican
{
    /// <summary>
    /// 美式期权信息
    /// </summary>
    [Serializable]
    public class AmericanOptionInfo
    {
        /// <summary>
        /// 1.认购期权(Call Option) -1.认沽期权(Put Option)
        /// </summary>
        public int OptionType { get; set; }

        /// <summary>
        /// 期数(执行步数)
        /// </summary>
        public int N { get; set; }

        /// <summary>
        /// 标的当前价格
        /// </summary>
        public double AssetPrice { get; set; }

        /// <summary>
        /// 执行价格
        /// </summary>
        public double ExercisePrice { get; set; }

        /// <summary>
        /// 市价
        /// </summary>
        public double MarketPrice { get; set; }

        /// <summary>
        /// 起始日期t
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 到期日期T
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 波动率σ
        /// </summary>
        public double sigma { get; set; }

        /// <summary>
        /// 无风险利率r
        /// </summary>
        public double r { get; set; }

        /// <summary>
        /// 股息率q
        /// </summary>
        public double q { get; set; }

        /// <summary>
        /// 深拷贝
        /// </summary>
        public static T Clone<T>(T obj)
        {
            T ret = default(T);
            if (obj != null)
            {
                XmlSerializer cloner = new XmlSerializer(typeof(T));
                MemoryStream stream = new MemoryStream();
                cloner.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                ret = (T)cloner.Deserialize(stream);
            }
            return ret;
        }

    }
}
