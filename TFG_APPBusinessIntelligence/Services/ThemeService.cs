namespace TFG_APPBusinessIntelligence.Services
{
    public enum AppTema { Oscuro, AzulClaro }

    public class ThemeService
    {
        private const string ClavePreferencia = "app_tema";

        // Tema Oscuro (original)
        private static readonly Dictionary<string, Color> TemaOscuro = new()
        {
            { "TemaGradienteInicio", Color.FromArgb("#1E3A8A") },
            { "TemaGradienteFin",    Color.FromArgb("#0F172A") },
            { "TemaTarjeta",         Color.FromArgb("#1E293B") },
            { "TemaBorde",           Color.FromArgb("#334155") },
            { "TemaSubtexto",        Color.FromArgb("#94A3B8") },
            { "TemaAccent",          Color.FromArgb("#60A5FA") },
            { "TemaAccentOscuro",    Color.FromArgb("#64748B") },
        };

        // Tema Azul Claro
        private static readonly Dictionary<string, Color> TemaAzulClaro = new()
        {
            { "TemaGradienteInicio", Color.FromArgb("#3B82F6") },
            { "TemaGradienteFin",    Color.FromArgb("#1D4ED8") },
            { "TemaTarjeta",         Color.FromArgb("#1E40AF") },
            { "TemaBorde",           Color.FromArgb("#60A5FA") },
            { "TemaSubtexto",        Color.FromArgb("#BFDBFE") },
            { "TemaAccent",          Color.FromArgb("#93C5FD") },
            { "TemaAccentOscuro",    Color.FromArgb("#3B82F6") },
        };

        public AppTema TemaActual { get; private set; }

        public ThemeService()
        {
            var guardado = Preferences.Get(ClavePreferencia, nameof(AppTema.Oscuro));
            TemaActual = Enum.TryParse<AppTema>(guardado, out var t) ? t : AppTema.Oscuro;
        }

        public void AplicarTema(AppTema tema)
        {
            TemaActual = tema;
            Preferences.Set(ClavePreferencia, tema.ToString());

            var colores = tema == AppTema.AzulClaro ? TemaAzulClaro : TemaOscuro;
            var recursos = Application.Current!.Resources;

            foreach (var par in colores)
                recursos[par.Key] = par.Value;
        }

        public void AplicarTemaGuardado() => AplicarTema(TemaActual);
    }
}
