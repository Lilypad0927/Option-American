using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionAmerican
{
    /// <summary>
    /// 坐标信息
    /// </summary>
    [Serializable]
    public class Node
    {
        /// <summary>
        /// 横坐标
        /// </summary>
        public double x { get; set; }

        /// <summary>
        /// 纵坐标
        /// </summary>
        public double y { get; set; }
    }
}
