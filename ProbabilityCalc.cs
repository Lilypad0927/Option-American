using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionAmerican
{
    /// <summary>
    /// 概率图计算器
    /// </summary>
    public class ProbabilityCalc
    {
        /// <summary>
        /// 初始化计算器
        /// </summary>
        /// <param name="S0">样本价格序列S0</param>
        public ProbabilityCalc(List<double> S0)
        {
            if (S0 == null || S0.Count() == 0) throw new Exception("样本价格序列为空");
            //步骤二：对样本序列取对数，计算对数价格Ln（S）的平均值μ和标准差σ
            List<double> lnS0 = new List<double>();
            foreach (var item in S0)
            {
                if (item > 0) lnS0.Add(Math.Log(item));
            }
            AveragelnS0 = lnS0.Average();
            StandardlnS0 = CalcStandardDeviation(lnS0);
        }

        /// <summary>
        /// 获取概率图点集（步骤八：绘制标的价格序列与概率密度函数的曲线图）
        /// </summary>
        /// <param name="price">计算时的标的价格</param>
        /// <param name="balancePoints">盈亏平衡点（可以为null）</param>
        /// <param name="size">默认取前后各2000组数据</param>
        /// <param name="step">默认取步长为0.001</param>
        public List<Node> GetProbabilityMap(double price, List<double> balancePoints, int size = 2000, double step = 0.001)
        {
            if (price < 0) throw new Exception("计算时的标的价格非法");
            //步骤三：以计算时的标的价格为中心取前后各2000组数据，步长为0.001生成新的价格序列
            List<double> S = CalcNewList(price, step, size);
            if (balancePoints != null && balancePoints.Count() > 0)
            {
                balancePoints.Sort();
                foreach (var item in balancePoints)
                {
                    if (S.Contains(item) == false) S.Add(item);
                }
                S.Sort();
            }
            S = S.FindAll(t => t > 0);
            //步骤四：对新的标的价格序列S对数化，生成对数价格序列Ln（S）
            List<double> lnS = new List<double>();
            foreach (var item in S)
            {
                lnS.Add(Math.Log(item));
            }
            //步骤五：以样本计算的均值μ和标准差σ为参数，计算新价格序列Ln（S）的概率密度f（lnS）
            List<double> flnS = new List<double>();
            foreach (var item in lnS)
            {
                if (item < 0) flnS.Add(0);
                else flnS.Add(CalcNormalDistribution(AveragelnS0, StandardlnS0, item));
            }
            List<Node> result = new List<Node>();
            for (int i = 0; i < flnS.Count(); i++)
            {
                Node node = new Node() { x = S[i], y = flnS[i] };
                result.Add(node);
            }
            return result;
        }

        /// <summary>
        /// 计算盈亏平衡点两侧区间的概率大小（步骤七），从首个区间起，依次返回各区间的概率大小（如果返回的List大小小于区间数量，说明剩下的区间概率大小是0）
        /// </summary>
        /// <param name="balancePoints">盈亏平衡点</param>
        /// <param name="size">计算积分时的精度</param>
        /// <param name="span">计算积分时的跨度，代表计算多少个标准差的长度，越大越精确</param>
        /// <returns>从首个区间起，依次返回各区间的概率大小（如果返回的List大小小于区间数量，说明剩下的区间概率大小是0）</returns>
        public List<double> GetIntervalProbability(List<double> balancePoints, int size = 100000, double span = 5)
        {
            if (balancePoints == null || balancePoints.Count() == 0) throw new Exception("盈亏平衡点为空");
            if (size < 2) throw new Exception("非法计算精度");
            if (span < 1) throw new Exception("非法计算跨度");
            //计算盈亏平衡点ln值
            List<double> lnBalancePoints = new List<double>();
            foreach (var item in balancePoints)
            {
                if (item <= 0) throw new Exception("盈亏平衡点非法");
                lnBalancePoints.Add(Math.Log(item));
            }
            lnBalancePoints.Sort();
            //计算正态分布的点集
            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            CalcNormalDistributionNodes(AveragelnS0, StandardlnS0, out X, out Y, size, span);
            //计算区间概率
            List<double> result = new List<double>();
            double curProbability = 0;
            int j = 0; //lnBalancePoints的索引
            for (int i = 0; i < X.Count() - 1; i++)
            {
                //计算到平衡点时，存一次区间概率大小，cur重置
                if (j < lnBalancePoints.Count() && X[i] >= lnBalancePoints[j])
                {
                    result.Add(curProbability);
                    curProbability = 0;
                    j++;
                }
                curProbability += (X[i + 1] - X[i]) * (Y[i] + Y[i + 1]) / 2;
            }
            result.Add(curProbability);
            return result;
        }

        #region 其他实用函数

        /// <summary>
        /// 标准差
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public double CalcStandardDeviation(List<double> numbers)
        {
            int count = numbers.Count();
            double average = numbers.Average();
            double sum = numbers.Sum(d => Math.Pow(d - average, 2));
            double deviation = Math.Sqrt(sum / count);
            return deviation;
        }

        /// <summary>
        /// 生成新序列
        /// </summary>
        /// <param name="center">中心</param>
        /// <param name="step">步长</param>
        /// <param name="size">前后各size组数据</param>
        /// <returns></returns>
        public List<double> CalcNewList(double center, double step, int size)
        {
            List<double> result = new List<double>();
            for (int i = -size; i <= size; i++)
            {
                result.Add(i * step + center);
            }
            return result;
        }

        /// <summary>
        /// 求正态分布f(x)
        /// </summary>
        /// <param name="average"></param>
        /// <param name="standardDeviation"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public double CalcNormalDistribution(double average, double standardDeviation, double x)
        {
            double tmp = -Math.Pow(x - average, 2) / (2 * Math.Pow(standardDeviation, 2));
            double y = Math.Pow(Math.E, tmp) / (Math.Sqrt(2 * Math.PI) * standardDeviation);
            return y;
        }

        /// <summary>
        /// 计算正态分布的点集
        /// </summary>
        /// <param name="average"></param>
        /// <param name="standardDeviation"></param>
        /// <param name="X">点集的横坐标</param>
        /// <param name="Y">点集的纵坐标</param>
        /// <param name="size">计算积分时的精度</param>
        /// <param name="span">计算积分时的跨度</param>
        /// <returns></returns>
        public void CalcNormalDistributionNodes(double average, double standardDeviation, out List<double> X, out List<double> Y, int size = 100000, double span = 4)
        {
            //区间（μ-σ,μ+σ）面积为68.268949%，（μ-1.96σ,μ+1.96σ）面积为95.449974%，（μ-2.58σ,μ+2.58σ）内的面积为99.730020%
            //正态分布的3σ原则：X落在（μ-3σ,μ+3σ）以外的概率小于千分之三，可以把区间（μ-3σ,μ+3σ）看作是随机变量X实际可能的取值区间
            double step = 2 * span * standardDeviation / size;
            X = CalcNewList(average, step, size / 2);
            Y = new List<double>();
            foreach (var item in X)
            {
                Y.Add(CalcNormalDistribution(average, standardDeviation, item));
            }
        }

        #endregion

        /// <summary>
        /// 均数
        /// </summary>
        private readonly double AveragelnS0;

        /// <summary>
        /// 标准差
        /// </summary>
        private readonly double StandardlnS0;
    }
}
