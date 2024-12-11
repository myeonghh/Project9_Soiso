using System;
using System.Windows;
using System.Windows.Input;

namespace ProjectSoiso.ViewModel
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // 새 창 전환을 위한 필드
        private readonly Type _windowType;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // 새로운 생성자: 창 전환에 사용
        public RelayCommand(Type windowType)
        {
            _windowType = windowType ?? throw new ArgumentNullException(nameof(windowType));
            _execute = OpenWindow;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // 새로운 창 열기 및 현재 창 닫기 메서드
        private void OpenWindow(object parameter)
        {
            if (_windowType == null) return;

            // 새로운 창 인스턴스 생성 및 열기
            var newWindow = Activator.CreateInstance(_windowType) as Window;
            newWindow?.Show();

            // 현재 창 닫기
            Application.Current.Windows[0]?.Close();
        }
    }
}
