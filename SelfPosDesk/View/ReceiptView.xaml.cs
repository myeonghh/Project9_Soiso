using Microsoft.VisualBasic;
using SelfPosDesk.ViewModel;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SelfPosDesk.View
{
    /// <summary>
    /// ReceiptView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReceiptView : UserControl
    {
        private DispatcherTimer _timer;
        public ReceiptView()
        {
            InitializeComponent();
            InitializeInactivityTimer();
        }

        private void InitializeInactivityTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += OnInactivityTimeout;
            _timer.Start();
        }

        private void OnInactivityTimeout(object sender, EventArgs e)
        {
            _timer.Stop();

            if (DataContext is SelfPosVM vm)
            {
                vm.GoToIntroView();
            }
        }

    }
}
