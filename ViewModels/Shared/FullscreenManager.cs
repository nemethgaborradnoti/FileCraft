using FileCraft.Shared.Commands;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class FullscreenManager<T> : BaseViewModel where T : struct, Enum
    {
        private T _currentState;
        private readonly T _defaultState;

        public T CurrentState
        {
            get => _currentState;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_currentState, value))
                {
                    _currentState = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleCommand { get; }

        public FullscreenManager(T defaultState = default)
        {
            _defaultState = defaultState;
            _currentState = defaultState;
            ToggleCommand = new RelayCommand(Toggle);
        }

        private void Toggle(object? parameter)
        {
            if (parameter is T targetState)
            {
                if (EqualityComparer<T>.Default.Equals(CurrentState, targetState))
                {
                    // If clicking the currently active state, revert to default (None)
                    CurrentState = _defaultState;
                }
                else
                {
                    // Switch to the new state
                    CurrentState = targetState;
                }
            }
            else if (parameter is string stateStr && Enum.TryParse(stateStr, out T parsedState))
            {
                if (EqualityComparer<T>.Default.Equals(CurrentState, parsedState))
                {
                    CurrentState = _defaultState;
                }
                else
                {
                    CurrentState = parsedState;
                }
            }
        }
    }
}