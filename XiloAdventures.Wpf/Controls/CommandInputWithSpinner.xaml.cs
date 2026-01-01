using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XiloAdventures.Wpf.Controls
{
    /// <summary>
    /// Cuadro de entrada de comandos del jugador con un spinner para indicar que el LLM está procesando.
    /// </summary>
    public partial class CommandInputWithSpinner : UserControl
    {
        public CommandInputWithSpinner()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Texto actual del comando que escribe el jugador.
        /// </summary>
        public string CommandText
        {
            get => (string)GetValue(CommandTextProperty);
            set => SetValue(CommandTextProperty, value);
        }

        public static readonly DependencyProperty CommandTextProperty =
            DependencyProperty.Register(
                nameof(CommandText),
                typeof(string),
                typeof(CommandInputWithSpinner),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Indica si el LLM está procesando una petición. Cuando es true, se muestra el spinner.
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                nameof(IsBusy),
                typeof(bool),
                typeof(CommandInputWithSpinner),
                new PropertyMetadata(false, OnIsBusyChanged));

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CommandInputWithSpinner control)
                return;

            if (control.Spinner is null)
                return;

            var busy = (bool)e.NewValue;
            control.Spinner.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Evento que se lanza cuando el jugador pulsa ENTER para enviar el comando.
        /// </summary>
        public event EventHandler<string>? CommandSubmitted;

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var text = CommandText ?? string.Empty;

                // Notificamos que el usuario ha enviado el comando
                CommandSubmitted?.Invoke(this, text);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Da el foco al textbox de entrada.
        /// </summary>
        public void FocusInput()
        {
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text?.Length ?? 0;
        }
    }
}
