using System.Collections.ObjectModel;
using System.Windows.Input;
using QuantTrader.Commands;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.TradingEngines;

namespace QuantTrader.ViewModels
{
    public class StockManagerViewModel : ViewModelBase
    {
        private readonly IMarketDataService _marketDataService;
        private readonly ITradingEngine _tradingEngine;

        private string _searchText;
        private bool _isSearching;
        private StockInfo _selectedStock;
        private string _statusMessage;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PerformSearch();
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        public StockInfo SelectedStock
        {
            get => _selectedStock;
            set => SetProperty(ref _selectedStock, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<StockInfo> SearchResults { get; } = new ObservableCollection<StockInfo>();
        public ObservableCollection<StockInfo> SelectedStocks { get; } = new ObservableCollection<StockInfo>();
        public ObservableCollection<StrategyTemplateViewModel> StrategyTemplates { get; } = new ObservableCollection<StrategyTemplateViewModel>();

        public ICommand AddStockCommand { get; }
        public ICommand RemoveStockCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ApplyStrategyCommand { get; }
        public ICommand StartAllStrategiesCommand { get; }
        public ICommand StopAllStrategiesCommand { get; }
        public ICommand RefreshPricesCommand { get; }

        public StockManagerViewModel(IMarketDataService marketDataService, ITradingEngine tradingEngine)
        {
            _marketDataService = marketDataService;
            _tradingEngine = tradingEngine;

            // 初始化命令
            AddStockCommand = new RelayCommand<StockInfo>(ExecuteAddStock, stock => stock != null);
            RemoveStockCommand = new RelayCommand<StockInfo>(ExecuteRemoveStock, stock => stock != null);
            ClearAllCommand = new RelayCommand(ExecuteClearAll, () => SelectedStocks.Count > 0);
            SelectAllCommand = new RelayCommand(ExecuteSelectAll);
            ApplyStrategyCommand = new AsyncRelayCommand<StrategyTemplateViewModel>(ExecuteApplyStrategyAsync);
            StartAllStrategiesCommand = new AsyncRelayCommand(ExecuteStartAllStrategiesAsync);
            StopAllStrategiesCommand = new AsyncRelayCommand(ExecuteStopAllStrategiesAsync);
            RefreshPricesCommand = new AsyncRelayCommand(ExecuteRefreshPricesAsync);

            // 初始化策略模板
            InitializeStrategyTemplates();

            // 初始化股票数据库
            InitializeStockDatabase();

            // 启动价格更新
            StartPriceUpdates();
        }

        private void InitializeStrategyTemplates()
        {
            StrategyTemplates.Add(new StrategyTemplateViewModel
            {
                Name = "移动平均线交叉",
                Type = "MovingAverageCross",
                Description = "快线上穿慢线买入，下穿卖出",
                DefaultParameters = new Dictionary<string, object>
                {
                    { "FastPeriod", 5 },
                    { "SlowPeriod", 20 },
                    { "Quantity", 100 }
                }
            });

            StrategyTemplates.Add(new StrategyTemplateViewModel
            {
                Name = "RSI策略",
                Type = "RSI",
                Description = "RSI超买超卖策略",
                DefaultParameters = new Dictionary<string, object>
                {
                    { "RSIPeriod", 14 },
                    { "OversoldLevel", 30 },
                    { "OverboughtLevel", 70 },
                    { "Quantity", 100 }
                }
            });

            StrategyTemplates.Add(new StrategyTemplateViewModel
            {
                Name = "布林带策略",
                Type = "BollingerBands",
                Description = "价格触及布林带边界时交易",
                DefaultParameters = new Dictionary<string, object>
                {
                    { "Period", 20 },
                    { "Multiplier", 2.0m },
                    { "Quantity", 100 }
                }
            });

            StrategyTemplates.Add(new StrategyTemplateViewModel
            {
                Name = "MACD策略",
                Type = "MACD",
                Description = "MACD指标交叉策略",
                DefaultParameters = new Dictionary<string, object>
                {
                    { "FastPeriod", 12 },
                    { "SlowPeriod", 26 },
                    { "SignalPeriod", 9 },
                    { "Quantity", 100 }
                }
            });
        }

        private void InitializeStockDatabase()
        {
            // 这里可以从文件或数据库加载股票信息
            // 为了演示，我们创建一些常见的股票信息
            var stockDatabase = new List<StockInfo>
            {
                // A股主要股票
                new StockInfo { Symbol = "000001", Name = "平安银行", Market = "深圳", Industry = "银行", Pinyin = "PAYH" },
                new StockInfo { Symbol = "000002", Name = "万科A", Market = "深圳", Industry = "房地产", Pinyin = "WKA" },
                new StockInfo { Symbol = "000858", Name = "五粮液", Market = "深圳", Industry = "白酒", Pinyin = "WLY" },
                new StockInfo { Symbol = "002415", Name = "海康威视", Market = "深圳", Industry = "安防", Pinyin = "HKWS" },
                new StockInfo { Symbol = "002594", Name = "比亚迪", Market = "深圳", Industry = "汽车", Pinyin = "BYD" },
                new StockInfo { Symbol = "300059", Name = "东方财富", Market = "深圳", Industry = "金融服务", Pinyin = "DFCF" },
                new StockInfo { Symbol = "600000", Name = "浦发银行", Market = "上海", Industry = "银行", Pinyin = "PFYH" },
                new StockInfo { Symbol = "600036", Name = "招商银行", Market = "上海", Industry = "银行", Pinyin = "ZSYH" },
                new StockInfo { Symbol = "600519", Name = "贵州茅台", Market = "上海", Industry = "白酒", Pinyin = "GZMT" },
                new StockInfo { Symbol = "600887", Name = "伊利股份", Market = "上海", Industry = "食品饮料", Pinyin = "YLGF" },
                new StockInfo { Symbol = "601318", Name = "中国平安", Market = "上海", Industry = "保险", Pinyin = "ZGPA" },
                new StockInfo { Symbol = "601398", Name = "工商银行", Market = "上海", Industry = "银行", Pinyin = "GSYH" },
                new StockInfo { Symbol = "601857", Name = "中国石油", Market = "上海", Industry = "石油石化", Pinyin = "ZGSY" },
                new StockInfo { Symbol = "601988", Name = "中国银行", Market = "上海", Industry = "银行", Pinyin = "ZGYH" },
                
                // 港股主要股票
                new StockInfo { Symbol = "00700", Name = "腾讯控股", Market = "香港", Industry = "互联网", Pinyin = "TXKG" },
                new StockInfo { Symbol = "09988", Name = "阿里巴巴", Market = "香港", Industry = "电商", Pinyin = "ALBB" },
                new StockInfo { Symbol = "03690", Name = "美团", Market = "香港", Industry = "本地服务", Pinyin = "MT" },
                new StockInfo { Symbol = "01024", Name = "快手", Market = "香港", Industry = "短视频", Pinyin = "KS" },
                new StockInfo { Symbol = "02318", Name = "中国平安", Market = "香港", Industry = "保险", Pinyin = "ZGPA" },
                
                // 美股主要股票
                new StockInfo { Symbol = "AAPL", Name = "苹果公司", Market = "纳斯达克", Industry = "科技", Pinyin = "PGGS" },
                new StockInfo { Symbol = "MSFT", Name = "微软公司", Market = "纳斯达克", Industry = "软件", Pinyin = "WRGS" },
                new StockInfo { Symbol = "GOOGL", Name = "谷歌", Market = "纳斯达克", Industry = "互联网", Pinyin = "GG" },
                new StockInfo { Symbol = "AMZN", Name = "亚马逊", Market = "纳斯达克", Industry = "电商", Pinyin = "YMX" },
                new StockInfo { Symbol = "TSLA", Name = "特斯拉", Market = "纳斯达克", Industry = "电动车", Pinyin = "TSL" },
                new StockInfo { Symbol = "META", Name = "Meta", Market = "纳斯达克", Industry = "社交媒体", Pinyin = "META" },
                new StockInfo { Symbol = "NVDA", Name = "英伟达", Market = "纳斯达克", Industry = "芯片", Pinyin = "YWD" }
            };

            // 存储到成员变量中供搜索使用
            _stockDatabase = stockDatabase;
        }

        private List<StockInfo> _stockDatabase = new List<StockInfo>();

        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchResults.Clear();
                return;
            }

            IsSearching = true;

            Task.Run(() =>
            {
                var searchTerm = SearchText.ToUpper();
                var results = _stockDatabase.Where(stock =>
                    stock.Symbol.ToUpper().Contains(searchTerm) ||
                    stock.Name.Contains(SearchText) ||
                    stock.Pinyin.ToUpper().Contains(searchTerm)
                ).Take(20).ToList();

                ExecuteOnUI(() =>
                {
                    SearchResults.Clear();
                    foreach (var stock in results)
                    {
                        SearchResults.Add(new StockInfo
                        {
                            Symbol = stock.Symbol,
                            Name = stock.Name,
                            Market = stock.Market,
                            Industry = stock.Industry,
                            Pinyin = stock.Pinyin
                        });
                    }
                    IsSearching = false;
                });
            });
        }

        private void ExecuteAddStock(StockInfo stock)
        {
            if (stock == null || SelectedStocks.Any(s => s.Symbol == stock.Symbol))
                return;

            var newStock = new StockInfo
            {
                Symbol = stock.Symbol,
                Name = stock.Name,
                Market = stock.Market,
                Industry = stock.Industry,
                Pinyin = stock.Pinyin,
                StrategyStatus = "无策略"
            };

            SelectedStocks.Add(newStock);

            // 订阅实时数据
            _marketDataService.SubscribeLevel1Data(stock.Symbol, data =>
            {
                ExecuteOnUI(() =>
                {
                    newStock.CurrentPrice = data.LastPrice;
                    newStock.ChangePercent = data.ChangePercent;
                    newStock.Volume = data.Volume;
                });
            });

            StatusMessage = $"已添加股票：{stock.Name}({stock.Symbol})";

            // 清除搜索
            SearchText = "";
        }

        private void ExecuteRemoveStock(StockInfo stock)
        {
            if (stock == null)
                return;

            // 取消订阅
            _marketDataService.UnsubscribeLevel1Data(stock.Symbol, null);

            // 停止相关策略
            var strategies = _tradingEngine.Strategies
                .Where(s => s.Symbol == stock.Symbol).ToList();

            foreach (var strategy in strategies)
            {
                Task.Run(() => _tradingEngine.StopStrategyAsync(strategy.Id));
            }

            SelectedStocks.Remove(stock);
            StatusMessage = $"已移除股票：{stock.Name}({stock.Symbol})";
        }

        private void ExecuteClearAll()
        {
            var stocksToRemove = SelectedStocks.ToList();
            foreach (var stock in stocksToRemove)
            {
                ExecuteRemoveStock(stock);
            }
        }

        private void ExecuteSelectAll()
        {
            foreach (var stock in SelectedStocks)
            {
                stock.IsSelected = true;
            }
        }

        private async Task ExecuteApplyStrategyAsync(StrategyTemplateViewModel template)
        {
            if (template == null)
                return;

            var selectedStocks = SelectedStocks.Where(s => s.IsSelected).ToList();
            if (selectedStocks.Count == 0)
            {
                StatusMessage = "请先选择要应用策略的股票";
                return;
            }

            StatusMessage = $"正在为 {selectedStocks.Count} 只股票应用{template.Name}策略...";

            foreach (var stock in selectedStocks)
            {
                try
                {
                    // 准备策略参数
                    var parameters = new Dictionary<string, object>(template.DefaultParameters)
                    {
                        ["Symbol"] = stock.Symbol
                    };

                    // 创建策略
                    var strategy = await _tradingEngine.AddStrategyAsync(template.Type, parameters);

                    // 启动策略
                    await _tradingEngine.StartStrategyAsync(strategy.Id);

                    // 更新股票状态
                    ExecuteOnUI(() =>
                    {
                        stock.HasStrategy = true;
                        stock.StrategyStatus = $"{template.Name} - 运行中";
                        stock.IsSelected = false;
                    });
                }
                catch (Exception ex)
                {
                    StatusMessage = $"为股票 {stock.Symbol} 应用策略失败: {ex.Message}";
                    return;
                }
            }

            StatusMessage = $"已为 {selectedStocks.Count} 只股票成功应用{template.Name}策略";
        }

        private async Task ExecuteStartAllStrategiesAsync()
        {
            var strategiesToStart = _tradingEngine.Strategies
                .Where(s => s.Status != StrategyStatus.Running)
                .ToList();

            foreach (var strategy in strategiesToStart)
            {
                try
                {
                    await _tradingEngine.StartStrategyAsync(strategy.Id);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"启动策略失败: {ex.Message}";
                }
            }

            StatusMessage = $"已启动 {strategiesToStart.Count} 个策略";
        }

        private async Task ExecuteStopAllStrategiesAsync()
        {
            var strategiesToStop = _tradingEngine.Strategies
                .Where(s => s.Status == StrategyStatus.Running)
                .ToList();

            foreach (var strategy in strategiesToStop)
            {
                try
                {
                    await _tradingEngine.StopStrategyAsync(strategy.Id);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"停止策略失败: {ex.Message}";
                }
            }

            // 更新股票状态
            ExecuteOnUI(() =>
            {
                foreach (var stock in SelectedStocks)
                {
                    stock.StrategyStatus = "已停止";
                }
            });

            StatusMessage = $"已停止 {strategiesToStop.Count} 个策略";
        }

        private async Task ExecuteRefreshPricesAsync()
        {
            StatusMessage = "正在刷新价格...";

            foreach (var stock in SelectedStocks)
            {
                try
                {
                    var data = await _marketDataService.GetLevel1DataAsync(stock.Symbol);
                    if (data != null)
                    {
                        ExecuteOnUI(() =>
                        {
                            stock.CurrentPrice = data.LastPrice;
                            stock.ChangePercent = data.ChangePercent;
                            stock.Volume = data.Volume;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"刷新 {stock.Symbol} 价格失败: {ex.Message}");
                }
            }

            StatusMessage = "价格刷新完成";
        }

        private void StartPriceUpdates()
        {
            // 定期更新策略状态
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(5000); // 每5秒更新一次

                        ExecuteOnUI(() =>
                        {
                            foreach (var stock in SelectedStocks)
                            {
                                // 查找该股票的策略
                                var strategies = _tradingEngine.Strategies
                                    .Where(s => s.Symbol == stock.Symbol).ToList();

                                if (strategies.Count > 0)
                                {
                                    var runningCount = strategies.Count(s => s.Status == StrategyStatus.Running);
                                    var totalCount = strategies.Count;

                                    stock.HasStrategy = totalCount > 0;
                                    stock.StrategyStatus = $"{runningCount}/{totalCount} 运行中";
                                }
                                else
                                {
                                    stock.HasStrategy = false;
                                    stock.StrategyStatus = "无策略";
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"更新策略状态失败: {ex.Message}");
                    }
                }
            });
        }
    }
}
