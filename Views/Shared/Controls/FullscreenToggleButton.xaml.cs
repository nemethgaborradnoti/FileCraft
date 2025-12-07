using System.Windows;
using System.Windows.Input;

namespace FileCraft.Views.Shared.Controls
{
    public partial class FullscreenToggleButton : UserControl
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(FullscreenToggleButton), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentStateProperty =
            DependencyProperty.Register(nameof(CurrentState), typeof(object), typeof(FullscreenToggleButton), new PropertyMetadata(null, OnStateChanged));

        public static readonly DependencyProperty TargetStateProperty =
            DependencyProperty.Register(nameof(TargetState), typeof(object), typeof(FullscreenToggleButton), new PropertyMetadata(null, OnStateChanged));

        private static readonly DependencyPropertyKey IsActivePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsActive), typeof(bool), typeof(FullscreenToggleButton), new PropertyMetadata(false));

        public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CurrentState
        {
            get { return GetValue(CurrentStateProperty); }
            set { SetValue(CurrentStateProperty, value); }
        }

        public object TargetState
        {
            get { return GetValue(TargetStateProperty); }
            set { SetValue(TargetStateProperty, value); }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            private set { SetValue(IsActivePropertyKey, value); }
        }

        public FullscreenToggleButton()
        {
            InitializeComponent();
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FullscreenToggleButton)d;
            control.UpdateActiveState();
        }

        private void UpdateActiveState()
        {
            IsActive = object.Equals(CurrentState, TargetState);
        }
    }
}