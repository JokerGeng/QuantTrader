using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public abstract class StrategyInfoBase : IStrategyInfo
    {
        IDataRepository _dataRepository;
        public string Id { get; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public List<StrategyParameter> Parameters { get; protected set; }

        public StrategyInfoBase(string id, IDataRepository dataRepository)
        {
            this.Id = id;
            this._dataRepository = dataRepository;
        }

        public virtual async Task UpdateParametersAsync(string name, object value)
        {
            var parameter = Parameters?.Find(t => t.Name == name);
            if (parameter != null)
            {
                parameter.Value = value;
                await Log($"Parameters updated: {parameter.Name}={parameter.Value}");
            }
            await Task.CompletedTask;
        }

        protected async Task Log(string message)
        {
            var timestamp = DateTime.Now;

            // 记录到数据库
            await _dataRepository.LogStrategyExecutionAsync(Id, message, timestamp).ConfigureAwait(false);

            //// 触发日志事件
            //LogGenerated?.Invoke(Id, message);
        }
    }
}
