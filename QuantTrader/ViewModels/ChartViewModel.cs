using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot;
using QuantTrader.Commands;
using QuantTrader.MarketDatas;
using System.Windows.Input;

namespace QuantTrader.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private readonly IMarketDataService _marketDataService;

        private string _selectedSymbol;
        private string _selectedPeriod;
        private PlotModel _plotModel;
        private PlotController _plotController;

        public ObservableCollection<string> Symbols { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Periods { get; } = new ObservableCollection<string>();

        public string SelectedSymbol
        {
            get => _selectedSymbol;
            set => SetProperty(ref _selectedSymbol, value);
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set => SetProperty(ref _selectedPeriod, value);
        }

        public PlotModel PlotModel
        {
            get => _plotModel;
            set => SetProperty(ref _plotModel, value);
        }

        public PlotController PlotController
        {
            get => _plotController;
            set => SetProperty(ref _plotController, value);
        }

        public ICommand RefreshChartCommand { get; }

        public ChartViewModel(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService;

            // 初始化命令
            RefreshChartCommand = new AsyncRelayCommand(_ => RefreshChartAsync());

            // 初始化图表控制器
            PlotController = new PlotController();

            // 创建默认图表
            CreateDefaultPlotModel();

            // 初始化符号列表
            Symbols.Add("AAPL");
            Symbols.Add("MSFT");
            Symbols.Add("GOOGL");
            Symbols.Add("AMZN");
            Symbols.Add("FB");
            SelectedSymbol = "AAPL";

            // 初始化周期列表
            Periods.Add("1 Minute");
            Periods.Add("5 Minutes");
            Periods.Add("15 Minutes");
            Periods.Add("1 Hour");
            Periods.Add("1 Day");
            SelectedPeriod = "1 Day";

            // 加载初始数据
            Task.Run(() => RefreshChartAsync());
        }

        private void CreateDefaultPlotModel()
        {
            var plotModel = new PlotModel
            {
                Title = "Price Chart",
                Subtitle = "Loading data...",
                PlotAreaBorderColor = OxyColors.LightGray,
                //LegendPosition = LegendPosition.TopRight,
                //LegendOrientation = LegendOrientation.Horizontal
            };

            // 添加X轴（时间轴）
            plotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Date",
                StringFormat = "yyyy-MM-dd",
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Black),
                TicklineColor = OxyColors.LightGray
            });

            // 添加Y轴（价格轴）
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Price",
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Black),
                TicklineColor = OxyColors.LightGray
            });

            PlotModel = plotModel;
        }

        private async Task RefreshChartAsync()
        {
            if (string.IsNullOrEmpty(SelectedSymbol) || string.IsNullOrEmpty(SelectedPeriod))
                return;

            try
            {
                // 解析周期
                TimeSpan period = SelectedPeriod switch
                {
                    "1 Minute" => TimeSpan.FromMinutes(1),
                    "5 Minutes" => TimeSpan.FromMinutes(5),
                    "15 Minutes" => TimeSpan.FromMinutes(15),
                    "1 Hour" => TimeSpan.FromHours(1),
                    "1 Day" => TimeSpan.FromDays(1),
                    _ => TimeSpan.FromDays(1)
                };

                // 获取K线数据
                var candles = await _marketDataService.GetLatestCandlesticksAsync(SelectedSymbol, 100, period);

                // 创建新的图表模型
                var plotModel = new PlotModel
                {
                    Title = $"{SelectedSymbol} Price Chart",
                    Subtitle = $"Period: {SelectedPeriod}",
                    PlotAreaBorderColor = OxyColors.LightGray,
                    //LegendPosition = LegendPosition.TopRight,
                    //LegendOrientation = LegendOrientation.Horizontal
                };

                // 添加X轴（时间轴）
                plotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Date",
                    StringFormat = period.TotalDays >= 1 ? "yyyy-MM-dd" : "HH:mm",
                    MajorGridlineStyle = LineStyle.Dot,
                    MinorGridlineStyle = LineStyle.None,
                    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Black),
                    TicklineColor = OxyColors.LightGray
                });

                // 添加Y轴（价格轴）
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Price",
                    MajorGridlineStyle = LineStyle.Dot,
                    MinorGridlineStyle = LineStyle.None,
                    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Black),
                    TicklineColor = OxyColors.LightGray
                });

                // 添加蜡烛图系列
                var candlestickSeries = new CandleStickSeries
                {
                    Title = SelectedSymbol,
                    Color = OxyColors.Black,
                    IncreasingColor = OxyColors.Green,
                    DecreasingColor = OxyColors.Red,
                    TrackerFormatString = "Time: {0}\nOpen: {4:0.00}\nHigh: {2:0.00}\nLow: {3:0.00}\nClose: {5:0.00}"
                };

                // 添加数据点
                foreach (var candle in candles)
                {
                    candlestickSeries.Items.Add(new HighLowItem(
                        DateTimeAxis.ToDouble(candle.Timestamp),
                        (double)candle.High,
                        (double)candle.Low,
                        (double)candle.Open,
                        (double)candle.Close));
                }

                // 添加移动平均线
                if (candles.Count > 0)
                {
                    // 计算20日移动平均线
                    var ma20Series = new LineSeries
                    {
                        Title = "MA20",
                        Color = OxyColors.Blue,
                        StrokeThickness = 1,
                        MarkerType = MarkerType.None
                    };

                    // 简单移动平均计算
                    for (int i = 19; i < candles.Count; i++)
                    {
                        decimal sum = 0;
                        for (int j = 0; j < 20; j++)
                        {
                            sum += candles[i - j].Close;
                        }

                        decimal ma = sum / 20;
                        ma20Series.Points.Add(new DataPoint(
                            DateTimeAxis.ToDouble(candles[i].Timestamp),
                            (double)ma));
                    }

                    // 计算60日移动平均线
                    var ma60Series = new LineSeries
                    {
                        Title = "MA60",
                        Color = OxyColors.Red,
                        StrokeThickness = 1,
                        MarkerType = MarkerType.None
                    };

                    if (candles.Count >= 60)
                    {
                        for (int i = 59; i < candles.Count; i++)
                        {
                            decimal sum = 0;
                            for (int j = 0; j < 60; j++)
                            {
                                sum += candles[i - j].Close;
                            }

                            decimal ma = sum / 60;
                            ma60Series.Points.Add(new DataPoint(
                                DateTimeAxis.ToDouble(candles[i].Timestamp),
                                (double)ma));
                        }
                    }

                    // 添加系列到图表
                    plotModel.Series.Add(candlestickSeries);
                    plotModel.Series.Add(ma20Series);

                    if (candles.Count >= 60)
                    {
                        plotModel.Series.Add(ma60Series);
                    }
                }
                else
                {
                    // 没有数据时
                    plotModel.Subtitle = "No data available";
                }

                // 更新图表模型
                ExecuteOnUI(() => PlotModel = plotModel);
            }
            catch (Exception ex)
            {
                // 处理错误
                var plotModel = new PlotModel
                {
                    Title = "Error",
                    Subtitle = $"Error loading data: {ex.Message}"
                };

                ExecuteOnUI(() => PlotModel = plotModel);
            }
        }
    }
}
