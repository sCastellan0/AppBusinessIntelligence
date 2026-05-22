using TFG_APPBusinessIntelligence.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class AnalizarDataset : ContentPage
    {
        private readonly DatasetAnalyzerService _analyzerService;
        private readonly GeneradorPdfService    _pdfService;
        private readonly IFileSaverService      _fileSaverService;

        private string? _rutaArchivoSeleccionado;
        private string? _rutaPdfGenerado;
        private bool _procesoEnCurso = false;
        private CancellationTokenSource? _cts;

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
                    var errorDialog = NotificacionDialog.Error(
                        "Formato No Soportado",
                        $"El archivo '{archivo.FileName}' no es compatible.",
                        "Formatos aceptados: CSV, TSV, Excel (.xlsx, .xls), JSON");
                    await Navigation.PushModalAsync(errorDialog, animated: true);
                    await errorDialog.MostrarAsync();
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
                VerDatasetBtn.IsVisible      = true;  // Mostrar botón Ver Dataset
                ResultadoFrame.IsVisible     = false;
                ErrorFrame.IsVisible         = false;
            }
            catch (Exception ex)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "No se pudo seleccionar el archivo",
                    ex.Message);
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
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
            _procesoEnCurso          = true;
            _cts                     = new CancellationTokenSource();

            // Iniciar generación en segundo plano
            _ = GenerarInformeEnSegundoPlanoAsync(_rutaArchivoSeleccionado, _cts.Token);
        }

        private async Task GenerarInformeEnSegundoPlanoAsync(string rutaArchivo, CancellationToken token)
        {
            try
            {
                // Paso 1 – Cargar y analizar
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EstadoLabel.Text = "Cargando dataset…";
                    BarraProgreso.ProgressTo(0.25, 200, Easing.Linear);
                });

                var resultado = await Task.Run(() => _analyzerService.Analizar(rutaArchivo), token);

                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EstadoLabel.Text = "Calculando estadísticas…";
                    BarraProgreso.ProgressTo(0.6, 300, Easing.Linear);
                });

                // Paso 2 – Generar PDF en caché temporal
                MainThread.BeginInvokeOnMainThread(() => EstadoLabel.Text = "Generando PDF…");

                string nombrePdf = $"informe_{Path.GetFileNameWithoutExtension(resultado.NombreArchivo)}.pdf";
                string tempPdf   = await Task.Run(() => _pdfService.GenerarPdf(resultado, FileSystem.CacheDirectory), token);

                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() => BarraProgreso.ProgressTo(0.85, 200, Easing.Linear));

                // Paso 3 – Mover al destino elegido por el usuario
                MainThread.BeginInvokeOnMainThread(() => EstadoLabel.Text = "Guardando informe…");

                string rutaPdf = await _fileSaverService.SavePdfAsync(tempPdf, nombrePdf);

                // Limpiar temporal si es distinto del destino
                if (tempPdf != rutaPdf && File.Exists(tempPdf))
                {
                    try
                    {
                        File.Delete(tempPdf);
                    }
                    catch (Exception deleteEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"No se pudo eliminar archivo temporal: {deleteEx.Message}");
                        // No es crítico si falla, continuar
                    }
                }

                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() => BarraProgreso.ProgressTo(1.0, 150, Easing.Linear));

                // Éxito
                _rutaPdfGenerado = rutaPdf;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProgresoFrame.IsVisible  = false;
                    ResultadoFrame.IsVisible = true;
                    RutaPdfLabel.Text        = rutaPdf;
                });

                // Enviar notificación si el usuario salió de la vista
                await EnviarNotificacionInformeCompletadoAsync(nombrePdf);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ProgresoFrame.IsVisible = false;
                        ErrorFrame.IsVisible    = true;
                        ErrorLabel.Text         = ex.Message;
                    });
                }
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Spinner.IsRunning    = false;
                    GenerarBtn.IsEnabled = true;
                    _procesoEnCurso      = false;
                });
            }
        }

        private async Task EnviarNotificacionInformeCompletadoAsync(string nombrePdf)
        {
            // Solo enviar notificación si el usuario ha salido de la vista
            await Task.Delay(500); // Pequeña espera para asegurar que la UI se actualice

            var request = new NotificationRequest
            {
                NotificationId = 1001,
                Title = "📊 Informe completado",
                Description = $"El informe '{nombrePdf}' se ha generado correctamente",
                CategoryType = NotificationCategoryType.Status,
                Android = new AndroidOptions
                {
                    IconSmallName = new AndroidIcon("appicon"),
                    AutoCancel = true,
                    Priority = AndroidPriority.High
                }
            };

            await LocalNotificationCenter.Current.Show(request);
        }

        // ── Volver ────────────────────────────────────────────────────────────
        private async void OnVolverClicked(object sender, EventArgs e)
        {
            // Si hay un proceso en curso, mostrar diálogo personalizado
            if (_procesoEnCurso)
            {
                var dialogo = new DialogoConfirmacion(
                    "Proceso en curso",
                    "El informe se está generando. Si sales ahora, recibirás una notificación cuando termine.\n\n¿Deseas salir?",
                    "Salir",
                    "Quedarme");

                await Navigation.PushModalAsync(dialogo);
                bool continuar = await dialogo.MostrarAsync();

                if (!continuar)
                    return;
            }

            await Navigation.PopAsync();
        }

        // ── Ver Dataset ───────────────────────────────────────────────────────
        private async void OnVerDatasetClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_rutaArchivoSeleccionado))
                return;

            try
            {
                // Abrir el archivo con el visor predeterminado del sistema
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_rutaArchivoSeleccionado)
                });
            }
            catch (Exception ex)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error al Abrir Archivo",
                    "No se pudo abrir el archivo",
                    ex.Message);
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // No cancelamos el proceso, dejamos que continúe en segundo plano
            // Pero limpiamos el CancellationTokenSource si existe y no hay proceso activo
            if (_cts != null && !_procesoEnCurso)
            {
                try
                {
                    _cts?.Dispose();
                }
                catch { /* Ignorar errores al limpiar */ }
                finally
                {
                    _cts = null;
                }
            }
        }
    }
}
