using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionAmerican
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //double test3 = 40927.52 * 100;
                //double test6 = (double)((decimal)40927.52 * 100);
                //double test9 = (double)((decimal)40927.525 * 100);
                //double test10 = Math.Floor(test9) / 100;
                //double test11 = (double)Math.Floor((decimal)40927.525 * 100) / 100;
                //double test7 = (float)40927.52 * 100;
                //double test2 = -40927.52 * 100;
                //double test1 = -Math.Abs(40927.52) * 100;
                //double test = Math.Ceiling(-Math.Abs(40927.52) * 100) / 100;
                //double test4 = -Math.Ceiling(Math.Abs(40927.52) * 100) / 100;
                //double test8 = -Math.Ceiling(Math.Abs(40927.5199) * 100) / 100;
                double test5 = Math.Floor(40927.52 * 100) / 100;
                double test12 = (double)Math.Floor((decimal)40927.52 * 100) / 100;

                #region 美式期权

                //某美式期权信息
                AmericanOptionInfo americanOptionInfo = new AmericanOptionInfo()
                {
                    OptionType = -1,
                    N = 2,
                    AssetPrice = 3.061,
                    ExercisePrice = 3.12,
                    MarketPrice = -0.0833,
                    StartDate = new DateTime(2019, 12, 1),
                    EndDate = new DateTime(2020, 1, 22),
                    sigma = 0.1105,
                    r = 0.0303,
                    q = 0
                };

                Stopwatch sw1 = Stopwatch.StartNew();

                //美式期权计算器初始化
                AmericanOptionCalc americanOptionCalc = new AmericanOptionCalc(americanOptionInfo);

                sw1.Stop();
                Console.WriteLine(string.Format("初始化用时：{0}秒", sw1.Elapsed.TotalSeconds));

                Stopwatch sw2 = Stopwatch.StartNew();

                //获取理论价格
                double TheoreticalPrice = americanOptionCalc.GetTheoreticalPrice();
                //获取理论价格%
                double TheoreticalPricePer = americanOptionCalc.GetTheoreticalPricePercent();

                double Delta = americanOptionCalc.GetDelta();
                double Gamma = americanOptionCalc.GetGamma();
                double Vega = americanOptionCalc.GetVega();
                double Theta = americanOptionCalc.GetTheta();
                double Rho = americanOptionCalc.GetRho();

                double IntriValue = americanOptionCalc.GetIntriValue();
                double TimeValue = americanOptionCalc.GetTimeValue();
                double DueProfit = americanOptionCalc.GetDueProfit();
                double CurrentProfit = americanOptionCalc.GetCurrentProfit();

                sw2.Stop();
                Console.WriteLine(string.Format("获取结果用时：{0}秒\r\n共{1}秒", sw2.Elapsed.TotalSeconds, sw1.Elapsed.TotalSeconds + sw2.Elapsed.TotalSeconds));

                #endregion

                #region 概率图

                Stopwatch sw3 = Stopwatch.StartNew();

                //样本价格序列S0
                List<double> S0 = new List<double>() { 3, 2, 3, 4, 5, 6, 7, 8, 7 };
                //当前标的价格
                double AssetPrice = 3;
                //盈亏平衡点
                List<double> BalancePoints = new List<double>() { 4, 6 };

                ProbabilityCalc probabilityCalc = new ProbabilityCalc(S0);
                //概率图点集（步骤八）
                List<Node> ProbabilityMap = probabilityCalc.GetProbabilityMap(AssetPrice, BalancePoints);
                //各区间概率大小（步骤七）
                List<double> IntervalProbability = probabilityCalc.GetIntervalProbability(BalancePoints);

                sw3.Stop();
                //各区间概率和越接近1，区间概率结果越精确
                Console.WriteLine(string.Format("概率图用时：{0}秒，各区间概率和：{1}", sw3.Elapsed.TotalSeconds, IntervalProbability.Sum()));

                #endregion

                Console.WriteLine("Hello World!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
