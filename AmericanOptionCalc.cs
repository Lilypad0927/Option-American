using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace OptionAmerican
{
    /// <summary>
    /// 美式期权计算器
    /// </summary>
    public class AmericanOptionCalc
    {
        /// <summary>
        /// 计算器初始化，计算中间变量
        /// </summary>
        /// <param name="info"></param>
        public AmericanOptionCalc(AmericanOptionInfo info) 
        {
            try
            {
                #region 期权信息检查
                if (info.OptionType != 1 && info.OptionType != -1) throw new Exception("期权类型非法");
                if (info.N < 2) throw new Exception("期数(执行步数)非法");
                if (info.AssetPrice < 0) throw new Exception("标的当前价格非法");
                if (info.ExercisePrice < 0) throw new Exception("执行价格非法");
                if (info.EndDate <= info.StartDate) throw new Exception("起始到期日期非法");
                if (info.r < 0) throw new Exception("无风险利率r非法");
                if (info.q < 0) throw new Exception("股息率q非法");
                if (info.sigma == 0) throw new Exception("波动率非法");
                #endregion
                ExpiryDays = (info.EndDate - info.StartDate).Days;
                AnnualTimePerStep = (double)ExpiryDays / 365 / info.N;
                b = info.r - info.q;
                a = Math.Exp(b * AnnualTimePerStep);
                DiscountRate = Math.Exp(-info.r * AnnualTimePerStep);
                OptionType = info.OptionType;
                Info = info;
                Calc(info, out u, out d, out p, out pNegative, out TreeAssetPrice, out TreeOptionPrice);
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }

        #region 计算结果

        /// <summary>
        /// 获取理论价格
        /// </summary>
        public double GetTheoreticalPrice()
        {
            if (TreeOptionPrice == null || TreeOptionPrice.Length < 1) throw new Exception("期权计算器尚未初始化");
            return OptionType * TreeOptionPrice[0];
        }

        /// <summary>
        /// 获取理论价格%
        /// </summary>
        public double GetTheoreticalPricePercent()
        {
            return GetTheoreticalPrice() / TreeAssetPrice[0];
        }

        /// <summary>
        /// 获取Delta
        /// </summary>
        public double GetDelta()
        {
            if (TreeOptionPrice == null || TreeOptionPrice.Length < 3) throw new Exception("期权计算器尚未初始化");
            return (TreeOptionPrice[1] - TreeOptionPrice[2]) / (TreeAssetPrice[1] - TreeAssetPrice[2]) * OptionType;
        }

        /// <summary>
        /// 获取Gamma
        /// </summary>
        public double GetGamma()
        {
            if (TreeOptionPrice == null || TreeOptionPrice.Length < 6) throw new Exception("期权计算器尚未初始化");
            double r = (TreeOptionPrice[3] - TreeOptionPrice[4]) / (TreeAssetPrice[3] - TreeAssetPrice[0]) -
                (TreeOptionPrice[4] - TreeOptionPrice[5]) / (TreeAssetPrice[0] - TreeAssetPrice[5]);
            double h = 0.5 * (TreeAssetPrice[3] - TreeAssetPrice[5]);
            return r / h * OptionType;
        }

        /// <summary>
        /// 获取Vega
        /// </summary>
        public double GetVega()
        {
            double epsilon = 0.00001;
            AmericanOptionInfo info2 = AmericanOptionInfo.Clone(Info);
            info2.sigma += epsilon;
            double tu = 0, td = 0, tp = 0, tpNegative = 0;
            double[] tTreeAssetPrice = null;
            double[] tTreeOptionPrice = null;
            Calc(info2, out tu, out td, out tp, out tpNegative, out tTreeAssetPrice, out tTreeOptionPrice);
            if (tTreeOptionPrice == null || tTreeOptionPrice.Length < 1) throw new Exception("获取Vega失败");
            return (tTreeOptionPrice[0] - TreeOptionPrice[0]) / epsilon * OptionType * 0.01;
        }

        /// <summary>
        /// 获取Theta
        /// </summary>
        public double GetTheta()
        {
            if (TreeOptionPrice == null || TreeOptionPrice.Length < 6) throw new Exception("期权计算器尚未初始化");
            return (TreeOptionPrice[4] - TreeOptionPrice[0]) / (2 * AnnualTimePerStep * 365) * OptionType;
        }

        /// <summary>
        /// 获取Rho
        /// </summary>
        public double GetRho()
        {
            double epsilon = 0.00001;
            AmericanOptionInfo info2 = AmericanOptionInfo.Clone(Info);
            info2.r += epsilon;
            double tu = 0, td = 0, tp = 0, tpNegative = 0;
            double[] tTreeAssetPrice = null;
            double[] tTreeOptionPrice = null;
            Calc(info2, out tu, out td, out tp, out tpNegative, out tTreeAssetPrice, out tTreeOptionPrice);
            if (tTreeOptionPrice == null || tTreeOptionPrice.Length < 1) throw new Exception("获取Rho失败");
            return (tTreeOptionPrice[0] - TreeOptionPrice[0]) / epsilon * OptionType * 0.01;
        }

        /// <summary>
        /// 获取内在价值
        /// </summary>
        public double GetIntriValue()
        {
            if (Info == null) throw new Exception("期权计算器尚未初始化");
            return Math.Max((Info.AssetPrice - Info.ExercisePrice) * OptionType, 0) * OptionType;
        }

        /// <summary>
        /// 获取时间价值
        /// </summary>
        public double GetTimeValue()
        {
            return GetTheoreticalPrice() - GetIntriValue();
        }

        /// <summary>
        /// 获取到期损益
        /// </summary>
        public double GetDueProfit()
        {
            if (TreeOptionPrice == null || TreeOptionPrice.Length < 1 || Info == null) throw new Exception("期权计算器尚未初始化");
            double tmp1 = Math.Max(Info.AssetPrice - Info.ExercisePrice, 0);
            double tmp2 = Info.MarketPrice == 0 ? TreeOptionPrice[0] : Info.MarketPrice;
            return (tmp1 - tmp2 * OptionType) * OptionType;
        }

        /// <summary>
        /// 获取当日损益
        /// </summary>
        public double GetCurrentProfit()
        {
            return GetTimeValue() + GetDueProfit();
        }

        #endregion

        /// <summary>
        /// 1.认购期权(Call Option) -1.认沽期权(Put Option)
        /// </summary>
        private readonly int OptionType;

        private AmericanOptionInfo Info;

        #region 中间变量

        /// <summary>
        /// 到期天数T-t
        /// </summary>
        private readonly int ExpiryDays;

        /// <summary>
        /// 获取到期天数T-t
        /// </summary>
        public int GetExpiryDays()
        {
            return ExpiryDays;
        }

        /// <summary>
        /// 每步年化时间Δt
        /// </summary>
        private readonly double AnnualTimePerStep;

        /// <summary>
        /// 获取每步年化时间Δt
        /// </summary>
        public double GetAnnualTimePerStep()
        {
            return AnnualTimePerStep;
        }

        /// <summary>
        /// 权益b=r-q
        /// </summary>
        private readonly double b;

        /// <summary>
        /// 获取权益b=r-q
        /// </summary>
        public double Getb()
        {
            return b;
        }

        /// <summary>
        /// 中间变量a
        /// </summary>
        private readonly double a;

        /// <summary>
        /// 获取中间变量a
        /// </summary>
        public double Geta()
        {
            return a;
        }

        /// <summary>
        /// 上涨幅度u
        /// </summary>
        private readonly double u;

        /// <summary>
        /// 获取上涨幅度u
        /// </summary>
        public double Getu()
        {
            return u;
        }

        /// <summary>
        /// 下跌幅度d
        /// </summary>
        private readonly double d;

        /// <summary>
        /// 获取下跌幅度d
        /// </summary>
        public double Getd()
        {
            return d;
        }

        /// <summary>
        /// 上涨概率p
        /// </summary>
        private readonly double p;

        /// <summary>
        /// 获取上涨概率p
        /// </summary>
        public double Getp()
        {
            return p;
        }

        /// <summary>
        /// 下跌概率1-p
        /// </summary>
        private readonly double pNegative;

        /// <summary>
        /// 获取下跌概率1-p
        /// </summary>
        public double GetpNegative()
        {
            return pNegative;
        }

        /// <summary>
        /// 折现率
        /// </summary>
        private readonly double DiscountRate;

        /// <summary>
        /// 获取折现率
        /// </summary>
        public double GetDiscountRate()
        {
            return DiscountRate;
        }

        /// <summary>
        /// 标的价格二叉树
        /// </summary>
        private readonly double[] TreeAssetPrice = null;

        /// <summary>
        /// 期权价格二叉树
        /// </summary>
        private readonly double[] TreeOptionPrice = null;

        /// <summary>
        /// 获取标的价格二叉树
        /// </summary>
        public double[] GetTreeAssetPrice()
        {
            return TreeAssetPrice;
        }

        /// <summary>
        /// 获取期权价格二叉树
        /// </summary>
        public double[] GetTreeOptionPrice()
        {
            return TreeOptionPrice;
        }

        #endregion

        private void Calc(AmericanOptionInfo info, out double tu, out double td, out double tp, out double tpNegative, out double[] tTreeAssetPrice, out double[] tTreeOptionPrice)
        {
            tu = Math.Exp(info.sigma * Math.Sqrt(AnnualTimePerStep));
            td = Math.Exp(-info.sigma * Math.Sqrt(AnnualTimePerStep));
            tp = (a - td) / (tu - td);
            tpNegative = 1 - tp;
            #region 生成二叉树
            int numberOfNodes = (info.N + 2) * (info.N + 1) / 2;
            //计算标的价格
            tTreeAssetPrice = new double[numberOfNodes];
            //根节点
            tTreeAssetPrice[0] = info.AssetPrice;
            for (int i = 1; i <= info.N; i++) //i为层数，第i层有i+1个点，根节点在0层
            {
                int index = (i * (i + 1) / 2); //第i层第1个点的索引
                int father = index - i; //第i层第1个点父节点的索引
                tTreeAssetPrice[index] = tTreeAssetPrice[father] * tu; //第i层第1个点由父节点上涨一次所得，乘上涨幅度u
                for (int j = 2; j <= i + 1; j++) //j为点数，首个点编号为1
                {
                    index = (i * (i + 1) / 2) + j - 1; //第i层第j个点的索引
                    father = index - i - 1; //父节点的索引
                    tTreeAssetPrice[index] = tTreeAssetPrice[father] * td; //由父节点下跌一次所得，乘下跌幅度d
                }
            }
            //计算期权价格
            tTreeOptionPrice = new double[numberOfNodes];
            //首先计算最底层N，共N+1个点
            for (int i = numberOfNodes - info.N - 1; i < numberOfNodes; i++)
            {
                tTreeOptionPrice[i] = Math.Max((tTreeAssetPrice[i] - info.ExercisePrice) * OptionType, 0);
            }
            //倒推
            int level = info.N - 1; //当前层数，初始在N-1层
            int cnt = 0;
            for (int i = numberOfNodes - info.N - 2; i >= 0; i--) //i为索引
            {
                cnt++; //当前为本层倒数第cnt个点
                double tmp1 = tp * tTreeOptionPrice[i + level + 1] + tpNegative * tTreeOptionPrice[i + level + 2];
                double tmp2 = Math.Exp(-info.r * AnnualTimePerStep) * tmp1;
                tTreeOptionPrice[i] = Math.Max((tTreeAssetPrice[i] - info.ExercisePrice) * OptionType, tmp2);
                if (cnt == level + 1) //本层遍历结束
                {
                    cnt = 0;
                    level--;
                }
            }
            #endregion
        }

    }
}
