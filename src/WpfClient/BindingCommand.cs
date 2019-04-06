using System;
using System.Windows.Input;

namespace WpfClient
{
    public class BindingCommand : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public BindingCommand(Action<object> execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public BindingCommand(Action execute) : this(o => execute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
        }

        public BindingCommand(Action<object> execute, Func<object, bool> canExecute) : this(execute) => _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));

        public BindingCommand(Action execute, Func<bool> canExecute) : this(o => execute(), o => canExecute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute(parameter);

            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) => _execute(parameter);

        public void ChangeCanExecute()
        {
            EventHandler changed = CanExecuteChanged;
            changed?.Invoke(this, EventArgs.Empty);
        }

        public static ICommand Create(ref ICommand backingField, Action execute)
            => backingField ?? (backingField = new BindingCommand(execute));
        public static ICommand Create(ref ICommand backingField, Action<object> execute)
            => backingField ?? (backingField = new BindingCommand(execute));
    }
}