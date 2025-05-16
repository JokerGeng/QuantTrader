using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuantTrader.ViewModels;

namespace QuantTrader
{
    /// <summary>
    /// ChartView.xaml 的交互逻辑
    /// </summary>
    public partial class ChartView : UserControl
    {
        public ChartView()
        {
            InitializeComponent();
            // 在设计时，不需要实际的视图模型
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new ChartViewModel(null);
            }
            else
            {
                // 在运行时，通过IoC容器获取视图模型
                DataContext = ((App)App.Current).ServiceProvider.GetService(typeof(ChartViewModel));
            }
        }
    }
}
