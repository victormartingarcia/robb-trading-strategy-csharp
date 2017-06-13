using System;
using System.Collections.Generic;
using TradingMotion.SDKv2.Algorithms;
using TradingMotion.SDKv2.Algorithms.InputParameters;
using TradingMotion.SDKv2.Markets.Charts;
using TradingMotion.SDKv2.Markets.Orders;
using TradingMotion.SDKv2.Markets.Indicators.StatisticFunctions;
using TradingMotion.SDKv2.Markets.Indicators.OverlapStudies;

/// <summary>
/// Robb Strategy rules:
///     * Entry: Sell when price breaks the lower band of Bollinger Bands indicator
///     * Exit: Price hits the Profit target set 3 standard deviations below the entry price
///     * Filters: None
/// </summary>
namespace RobbStrategy
{
    public class RobbStrategy : Strategy
    {
        BBAndsIndicator bollingerBandsIndicator;
        StdDevIndicator standardDeviationIndicator;

        public RobbStrategy(Chart mainChart, List<Chart> secondaryCharts)
            : base(mainChart, secondaryCharts)
        {

        }

        /// <summary>
        /// Strategy Name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Robb Strategy";
            }
        }

        /// <summary>
        /// Security filter that ensures the Position will be closed at the end of the trading session.
        /// </summary>
        public override bool ForceCloseIntradayPosition
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Security filter that sets a maximum open position size of 1 contract (either side)
        /// </summary>
        public override uint MaxOpenPosition
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// This strategy doesn't use the Advanced Order Management mode
        /// </summary>
        public override bool UsesAdvancedOrderManagement
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Strategy Parameter definition
        /// </summary>
        public override InputParameterList SetInputParameters()
        {
            InputParameterList parameters = new InputParameterList();

            // The previous N bars period Standard Deviation indicator will use
            parameters.Add(new InputParameter("Standard deviation period", 20));

            // The distance between the entry price and the profit target order
            parameters.Add(new InputParameter("Profit target standard deviations", 3));

            // The previous N bars period Bollinger Bands indicator will use
            parameters.Add(new InputParameter("Bollinger Bands period", 58));
            // The distance between the price and the upper Bollinger band
            parameters.Add(new InputParameter("Upper standard deviations", 3.0));
            // The distance between the price and the lower Bollinger band
            parameters.Add(new InputParameter("Lower standard deviations", 3.0));

            return parameters;
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public override void OnInitialize()
        {
            log.Debug("RobbStrategy onInitialize()");

            // Adding the Standard Deviation indicador to the strategy
            // (see http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:standard_deviation)
            standardDeviationIndicator = new StdDevIndicator(this.Bars.Close, (int)this.GetInputParameter("Standard deviation period"), (int)this.GetInputParameter("Profit target standard deviations"));
            this.AddIndicator("Standard Deviation indicator", standardDeviationIndicator);

            // Adding the Bollinger Bands indicator to the strategy
            // (see http://www.investopedia.com/terms/b/bollingerbands.asp)
            bollingerBandsIndicator = new BBAndsIndicator(this.Bars.Close, (int)this.GetInputParameter("Bollinger Bands period"), (double)this.GetInputParameter("Upper standard deviations"), (double)this.GetInputParameter("Lower standard deviations"));
            this.AddIndicator("Bollinger Bands indicator", bollingerBandsIndicator);
        }

        /// <summary>
        /// Strategy enter/exit/filtering rules
        /// </summary>
        public override void OnNewBar()
        {
            // Sell signal: the price has crossed above the lower Bollinger band
            if (this.Bars.Close[1] >= bollingerBandsIndicator.GetLowerBand()[1] && this.Bars.Close[0] < bollingerBandsIndicator.GetLowerBand()[0] && this.GetOpenPosition() == 0)
            {
                // Entering short and placing a profit target 3 standard deviations below the current market price
                this.Sell(OrderType.Market, 1, 0.0, "Enter short position");
                this.ExitShort(OrderType.Limit, this.Bars.Close[0] - standardDeviationIndicator.GetStdDev()[0], "Exit short position (profit target)");
            }
            else if (this.GetOpenPosition() == -1)
            {
                // In Simple Order Management mode, Stop and Limit orders must be placed at every bar
                this.ExitShort(OrderType.Limit, this.GetFilledOrders()[0].FillPrice - standardDeviationIndicator.GetStdDev()[0], "Exit short position (profit target)");
            }
        }
    }
}
