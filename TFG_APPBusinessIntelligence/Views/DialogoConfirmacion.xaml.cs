namespace TFG_APPBusinessIntelligence.Views
{
    public partial class DialogoConfirmacion : ContentPage
    {
        private TaskCompletionSource<bool> _tcs;

        public DialogoConfirmacion(string titulo, string mensaje, string textoConfirmar = "Aceptar", string textoCancelar = "Cancelar")
        {
            InitializeComponent();

            TituloLabel.Text = titulo;
            MensajeLabel.Text = mensaje;
            BotonConfirmar.Text = textoConfirmar;
            BotonCancelar.Text = textoCancelar;

            _tcs = new TaskCompletionSource<bool>();
        }

        public Task<bool> MostrarAsync()
        {
            return _tcs.Task;
        }

        private async void OnConfirmarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(true);
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(false);
        }

        protected override bool OnBackButtonPressed()
        {
            // Evitar que el botón atrás cierre el diálogo sin respuesta
            Task.Run(async () =>
            {
                await Navigation.PopModalAsync();
                _tcs.TrySetResult(false);
            });
            return true; // Indicar que manejamos el evento
        }
    }
}
