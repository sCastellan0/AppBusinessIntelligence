namespace TFG_APPBusinessIntelligence.Services
{
    /// <summary>
    /// Servicio que detecta inactividad del usuario y dispara un evento para cerrar sesión.
    /// </summary>
    public class InactivityService
    {
        private static readonly TimeSpan TiempoInactividad = TimeSpan.FromMinutes(5);

        private CancellationTokenSource? _cts;

        /// <summary>
        /// Se dispara cuando el usuario ha estado inactivo durante el tiempo establecido.
        /// </summary>
        public event EventHandler? SesionExpirada;

        /// <summary>
        /// Reinicia el temporizador de inactividad. Debe llamarse en cada interacción del usuario.
        /// </summary>
        public void ReiniciarTemporizador()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            Task.Delay(TiempoInactividad, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    SesionExpirada?.Invoke(this, EventArgs.Empty);
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Detiene el temporizador (por ejemplo, al cerrar sesión manualmente).
        /// </summary>
        public void Detener()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
