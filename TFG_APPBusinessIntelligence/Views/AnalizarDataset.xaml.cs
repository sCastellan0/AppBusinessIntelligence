using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class AnalizarDataset : ContentPage
    {
        private readonly DatasetAnalyzerService _analyzerService;
        private readonly GeneradorPdfService    _pdfService;
        private readonly IFileSaverService      _fileSaverService;

        private string? _rutaArchivoSeleccionado;
        private string? _rutaPdfGenerado;

        private static readonly string[] TiposPermitidos = { ".csv", ".tsv", ".xlsx", ".xls", ".json" };

        public AnalizarDataset(DatasetAnalyzerService analyzerService, GeneradorPdfService pdfService, IFileSaverService fileSaverService)
        {
            InitializeComponent();
            _analyzerService  = analyzerService;
            _pdfService       = pdfService;
            _fileSaverService = fileSaverService;
        }

        // ── Seleccionar archivo ───────────────────────────────────────────────
        private async void OnSeleccionarArchivoClicked(object sender, EventArgs e)
        {
            try
            {
                var opciones = new PickOptions
                {
                    PickerTitle = "Selecciona un dataset",
                    FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS,     new[] { "public.data", "public.content" } },
                        { DevicePlatform.WinUI,   new[] { ".csv", ".tsv", ".xlsx", ".xls", ".json" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.data" } },
                    })
                };

                var archivo = await FilePicker.Default.PickAsync(opciones);
                if (archivo is null) return;

                string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!TiposPermitidos.Contains(ext))
                {
                    await DisplayAlert("Formato no soportado",
                        $"El archivo '{archivo.FileName}' no es compatible.\nFormatos aceptados: CSV, TSV, Excel, JSON.",
                        "Aceptar");
                    return;
                }

                _rutaArchivoSeleccionado = archivo.FullPath;

                // Actualizar UI
                NombreArchivoLabel.Text      = archivo.FileName;
                NombreArchivoLabel.TextColor  = Colors.White;
                var tamaño = new FileInfo(archivo.FullPath).Length / 1024.0;
                InfoArchivoLabel.Text        = $"{tamaño:F1} KB  ·  {ext.ToUpperInvariant()}";
                InfoArchivoLabel.IsVisible   = true;

                GenerarBtn.IsEnabled         = true;
                ResultadoFrame.IsVisible     = false;
                ErrorFrame.IsVisible         = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo seleccionar el archivo:\n{ex.Message}", "Aceptar");
            }
        }

        // ── Generar informe ───────────────────────────────────────────────────
        private async void OnGenerarInformeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_rutaArchivoSeleccionado)) return;

            // Estado inicial
            GenerarBtn.IsEnabled     = false;
            ProgresoFrame.IsVisible  = true;
            ResultadoFrame.IsVisible = false;
            ErrorFrame.IsVisible     = false;
            Spinner.IsRunning        = true;
            BarraProgreso.Progress   = 0;

            try
            {
                // Paso 1 – Cargar y analizar
                EstadoLabel.Text = "Cargando dataset…";
                await BarraProgreso.ProgressTo(0.25, 200, Easing.Linear);

                var resultado = await Task.Run(() => _analyzerService.Analizar(_rutaArchivoSeleccionado));

                EstadoLabel.Text = "Calculando estadísticas…";
                await BarraProgreso.ProgressTo(0.6, 300, Easing.Linear);

                // Paso 2 – Generar PDF en caché temporal
                EstadoLabel.Text = "Generando PDF…";
                string nombrePdf = $"informe_{Path.GetFileNameWithoutExtension(resultado.NombreArchivo)}.pdf";
                string tempPdf   = await Task.Run(() => _pdfService.GenerarPdf(resultado, FileSystem.CacheDirectory));

                await BarraProgreso.ProgressTo(0.85, 200, Easing.Linear);

                // Paso 3 – Mover al destino elegido por el usuario
                EstadoLabel.Text = "Guardando informe…";
                string rutaPdf = await _fileSaverService.SavePdfAsync(tempPdf, nombrePdf);

                // Limpiar temporal si es distinto del destino
                if (tempPdf != rutaPdf && File.Exists(tempPdf))
                    File.Delete(tempPdf);

                await BarraProgreso.ProgressTo(1.0, 150, Easing.Linear);

                // Éxito
                _rutaPdfGenerado         = rutaPdf;
                ProgresoFrame.IsVisible  = false;
                ResultadoFrame.IsVisible = true;
                RutaPdfLabel.Text        = rutaPdf;

                if (AbrirPdfSwitch.IsToggled)
                    AbrirPdf(rutaPdf);
            }
            catch (Exception ex)
            {
                ProgresoFrame.IsVisible = false;
                ErrorFrame.IsVisible    = true;
                ErrorLabel.Text         = ex.Message;
            }
            finally
            {
                Spinner.IsRunning    = false;
                GenerarBtn.IsEnabled = true;
            }
        }

        // ── Abrir PDF ─────────────────────────────────────────────────────────
        private void OnAbrirPdfClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_rutaPdfGenerado))
                AbrirPdf(_rutaPdfGenerado);
        }

        private static void AbrirPdf(string ruta)
        {
            try
            {
                Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(ruta)
                });
            }
            catch
            {
                // Si no hay visor de PDF instalado, silenciosamente ignora
            }
        }

        // ── Volver ────────────────────────────────────────────────────────────
        private async void OnVolverClicked(object sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}
