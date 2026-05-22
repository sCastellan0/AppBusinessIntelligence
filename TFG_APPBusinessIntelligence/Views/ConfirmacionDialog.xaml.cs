namespace TFG_APPBusinessIntelligence.Views
{
    public partial class ConfirmacionDialog : ContentPage
    {
        private TaskCompletionSource<bool> _taskCompletionSource = new();

        public ConfirmacionDialog()
        {
            InitializeComponent();
        }

        public static ConfirmacionDialog Crear(string titulo, string mensaje, string textoConfirmar = "Confirmar", string textoCancelar = "Cancelar", bool esPeligroso = false)
        {
            var dialog = new ConfirmacionDialog();
            dialog.TituloLabel.Text = titulo;
            dialog.MensajeLabel.Text = mensaje;
            dialog.ConfirmarBtn.Text = textoConfirmar;
            dialog.CancelarBtn.Text = textoCancelar;

            if (esPeligroso)
            {
                // Icono de advertencia y botón rojo
                dialog.IconoLabel.Text = "⚠";
                dialog.IconoLabel.TextColor = Color.FromArgb("#EF4444");
                dialog.ConfirmarBtn.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#DC2626"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#EF4444"), Offset = 1.0f }
                    }
                };
            }
            else
            {
                // Icono de pregunta y botón azul
                dialog.IconoLabel.Text = "❓";
                dialog.IconoLabel.TextColor = Color.FromArgb("#3B82F6");
                dialog.ConfirmarBtn.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#3B82F6"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#60A5FA"), Offset = 1.0f }
                    }
                };
            }

            return dialog;
        }

        public Task<bool> MostrarAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            return _taskCompletionSource.Task;
        }

        private async void OnConfirmarClicked(object sender, EventArgs e)
        {
            await CerrarDialogo(true);
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await CerrarDialogo(false);
        }

        private async Task CerrarDialogo(bool resultado)
        {
            await Navigation.PopModalAsync(animated: true);
            _taskCompletionSource?.SetResult(resultado);
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CerrarDialogo(false);
            });
            return true;
        }
    }
}
