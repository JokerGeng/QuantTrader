using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using QuantTrader.Strategies;
using QuantTrader.ViewModels;
using System.Xml;

namespace QuantTrader.Views
{
    /// <summary>
    /// ScriptEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScriptEditorWindow : Window
    {
        private readonly ScriptEditorViewModel _viewModel;

        public ScriptStrategy ScriptStrategy { get; private set; }

        public ScriptEditorWindow(ScriptEditorViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            // 设置语法高亮（需要引用AvalonEdit）
            try
            {
                // 尝试加载C#语法高亮定义
                using (var stream = GetType().Assembly.GetManifestResourceStream("QuantTrader.Resources.CSharp.xshd"))
                {
                    if (stream != null)
                    {
                        using (var reader = new XmlTextReader(stream))
                        {
                            ScriptEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                // 使用默认高亮
            }

            // 设置初始文本
            ScriptEditor.Text = _viewModel.ScriptCode;

            // 双向绑定文本编辑器内容
            ScriptEditor.TextChanged += (s, e) => _viewModel.ScriptCode = ScriptEditor.Text;

            // 订阅视图模型事件
            _viewModel.ScriptSaved += OnScriptSaved;
            _viewModel.EditorCancelled += OnEditorCancelled;
            _viewModel.TemplateChanged += () => ScriptEditor.Text = _viewModel.ScriptCode;

            // 窗口关闭事件
            Closing += (s, e) =>
            {
                _viewModel.ScriptSaved -= OnScriptSaved;
                _viewModel.EditorCancelled -= OnEditorCancelled;
            };
        }

        private void OnScriptSaved(ScriptStrategy strategy)
        {
            ScriptStrategy = strategy;
            DialogResult = true;
            Close();
        }

        private void OnEditorCancelled()
        {
            DialogResult = false;
            Close();
        }

        public void SetStrategy(ScriptStrategy strategy)
        {
            //_viewModel.InitializeFromStrategy(strategy);
            ScriptEditor.Text = strategy.ScriptCode;
        }
    }
}
