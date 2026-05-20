using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class ProbarDemo : ContentPage
    {
        // Rutas de archivos de demostración
        private const string NombreDatasetVentas = "Ventas_Mensuales_2024.csv";
        private const string NombreInformeVentas = "informe_Ventas_Mensuales_2024.pdf";

        private const string NombreDatasetMarketing = "Campañas_Marketing_2024.csv";
        private const string NombreInformeMarketing = "informe_Campañas_Marketing_2024.pdf";

        private const string NombreDatasetFinanzas = "Finanzas_Trimestrales_2024.csv";
        private const string NombreInformeFinanzas = "informe_Finanzas_Trimestrales_2024.pdf";

        private readonly DatasetAnalyzerService _analyzerService;
        private readonly GeneradorPdfService _pdfService;

        public ProbarDemo()
        {
            InitializeComponent();

            // Obtener servicios del contenedor de DI
            _analyzerService = App.Current!.Handler!.MauiContext!.Services.GetService<DatasetAnalyzerService>()!;
            _pdfService = App.Current!.Handler!.MauiContext!.Services.GetService<GeneradorPdfService>()!;
        }

        // ══════════════════════════════════════════════════════════════════════
        // DATASET 1: VENTAS
        // ══════════════════════════════════════════════════════════════════════

        private async void OnVerDatasetVentasClicked(object sender, EventArgs e)
        {
            await MostrarDatasetAsync(NombreDatasetVentas, CrearDatasetVentasAsync);
        }

        private async void OnVerInformeVentasClicked(object sender, EventArgs e)
        {
            await MostrarInformeAsync(sender, NombreDatasetVentas, NombreInformeVentas, CrearDatasetVentasAsync);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DATASET 2: MARKETING
        // ══════════════════════════════════════════════════════════════════════

        private async void OnVerDatasetMarketingClicked(object sender, EventArgs e)
        {
            await MostrarDatasetAsync(NombreDatasetMarketing, CrearDatasetMarketingAsync);
        }

        private async void OnVerInformeMarketingClicked(object sender, EventArgs e)
        {
            await MostrarInformeAsync(sender, NombreDatasetMarketing, NombreInformeMarketing, CrearDatasetMarketingAsync);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DATASET 3: FINANZAS
        // ══════════════════════════════════════════════════════════════════════

        private async void OnVerDatasetFinanzasClicked(object sender, EventArgs e)
        {
            await MostrarDatasetAsync(NombreDatasetFinanzas, CrearDatasetFinanzasAsync);
        }

        private async void OnVerInformeFinanzasClicked(object sender, EventArgs e)
        {
            await MostrarInformeAsync(sender, NombreDatasetFinanzas, NombreInformeFinanzas, CrearDatasetFinanzasAsync);
        }

        private async void OnRegistrarseClicked(object sender, EventArgs e)
        {
            // Navegar directamente a la vista de Registro
            var registro = App.Current!.Handler!.MauiContext!.Services.GetService<Registro>();
            await Navigation.PushAsync(registro!);
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Métodos auxiliares genéricos
        // ══════════════════════════════════════════════════════════════════════

        private async Task MostrarDatasetAsync(string nombreDataset, Func<Task<string>> creadorDataset)
        {
            try
            {
                string rutaDataset = await creadorDataset();

                if (File.Exists(rutaDataset))
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaDataset)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el dataset: {ex.Message}", "Aceptar");
            }
        }

        private async Task MostrarInformeAsync(object sender, string nombreDataset, string nombreInforme, Func<Task<string>> creadorDataset)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Text = "⏳ Generando...";
                }

                string rutaInforme = await CrearInformeGenericoAsync(nombreDataset, nombreInforme, creadorDataset);

                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Text = "📄 Informe";
                }

                if (File.Exists(rutaInforme))
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaInforme)
                    });
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo generar el informe", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Text = "📄 Informe";
                }
                await DisplayAlert("Error", $"No se pudo abrir el informe: {ex.Message}", "Aceptar");
            }
        }

        private async Task<string> CrearInformeGenericoAsync(string nombreDataset, string nombreInforme, Func<Task<string>> creadorDataset)
        {
            string rutaPdf = Path.Combine(FileSystem.CacheDirectory, nombreInforme);

            if (File.Exists(rutaPdf))
                return rutaPdf;

            try
            {
                string rutaDataset = await creadorDataset();
                var resultado = await Task.Run(() => _analyzerService.Analizar(rutaDataset));
                rutaPdf = await Task.Run(() => _pdfService.GenerarPdf(resultado, FileSystem.CacheDirectory));
                return rutaPdf;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando informe: {ex.Message}");
                return string.Empty;
            }
        }
        // ══════════════════════════════════════════════════════════════════════
        // Generadores de datasets
        // ══════════════════════════════════════════════════════════════════════

        private async Task<string> CrearDatasetVentasAsync()
        {
            string rutaDemo = Path.Combine(FileSystem.CacheDirectory, NombreDatasetVentas);

            if (File.Exists(rutaDemo))
                return rutaDemo;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha,Producto,Categoría,Cantidad,Precio,Total,Región,Vendedor");

            var random = new Random(42);
            var productos = new[] { "Laptop Pro", "Mouse Inalámbrico", "Teclado Mecánico", "Monitor 27\"", "Webcam HD", "Auriculares", "SSD 1TB", "Hub USB-C" };
            var categorias = new[] { "Computadoras", "Periféricos", "Periféricos", "Monitores", "Accesorios", "Audio", "Almacenamiento", "Accesorios" };
            var regiones = new[] { "Norte", "Sur", "Este", "Oeste", "Centro" };
            var vendedores = new[] { "Ana García", "Luis Martínez", "Carmen López", "Pedro Sánchez", "María Fernández" };

            var fechaInicio = new DateTime(2024, 1, 1);

            for (int i = 0; i < 500; i++)
            {
                var fecha = fechaInicio.AddDays(random.Next(0, 365));
                var productoIdx = random.Next(productos.Length);
                var producto = productos[productoIdx];
                var categoria = categorias[productoIdx];
                var cantidad = random.Next(1, 20);
                var precio = Math.Round(50 + random.NextDouble() * 1500, 2);
                var total = Math.Round(cantidad * precio, 2);
                var region = regiones[random.Next(regiones.Length)];
                var vendedor = vendedores[random.Next(vendedores.Length)];

                sb.AppendLine($"{fecha:yyyy-MM-dd},{producto},{categoria},{cantidad},{precio},{total},{region},{vendedor}");
            }

            await File.WriteAllTextAsync(rutaDemo, sb.ToString());
            return rutaDemo;
        }

        private async Task<string> CrearDatasetMarketingAsync()
        {
            string rutaDemo = Path.Combine(FileSystem.CacheDirectory, NombreDatasetMarketing);

            if (File.Exists(rutaDemo))
                return rutaDemo;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha,Campaña,Canal,Impresiones,Clics,Conversiones,Costo,Ingresos,ROI");

            var random = new Random(123);
            var campañas = new[] { "Black Friday", "Navidad 2024", "Verano Digital", "Back to School", "Cyber Monday", "San Valentín", "Primavera Sale" };
            var canales = new[] { "Google Ads", "Facebook", "Instagram", "Email", "LinkedIn", "TikTok", "YouTube" };

            var fechaInicio = new DateTime(2024, 1, 1);

            for (int i = 0; i < 400; i++)
            {
                var fecha = fechaInicio.AddDays(random.Next(0, 365));
                var campaña = campañas[random.Next(campañas.Length)];
                var canal = canales[random.Next(canales.Length)];
                var impresiones = random.Next(10000, 500000);
                var clics = random.Next(100, impresiones / 10);
                var conversiones = random.Next(10, clics / 5);
                var costo = Math.Round(500 + random.NextDouble() * 5000, 2);
                var ingresos = Math.Round(conversiones * (100 + random.NextDouble() * 300), 2);
                var roi = Math.Round((ingresos - costo) / costo * 100, 2);

                sb.AppendLine($"{fecha:yyyy-MM-dd},{campaña},{canal},{impresiones},{clics},{conversiones},{costo},{ingresos},{roi}");
            }

            await File.WriteAllTextAsync(rutaDemo, sb.ToString());
            return rutaDemo;
        }

        private async Task<string> CrearDatasetFinanzasAsync()
        {
            string rutaDemo = Path.Combine(FileSystem.CacheDirectory, NombreDatasetFinanzas);

            if (File.Exists(rutaDemo))
                return rutaDemo;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha,Transacción,Categoría,Departamento,Monto,Tipo,Referencia");

            var random = new Random(789);
            var categorias = new[] { "Salarios", "Servicios", "Marketing", "Ventas", "Compras", "Infraestructura", "Impuestos" };
            var departamentos = new[] { "RRHH", "IT", "Marketing", "Ventas", "Compras", "Finanzas", "Legal" };
            var tipos = new[] { "Ingreso", "Gasto", "Gasto", "Gasto", "Ingreso" }; // Más gastos que ingresos

            var fechaInicio = new DateTime(2024, 1, 1);

            for (int i = 0; i < 600; i++)
            {
                var fecha = fechaInicio.AddDays(random.Next(0, 365));
                var transaccion = $"TRX{i + 1:D4}";
                var categoria = categorias[random.Next(categorias.Length)];
                var departamento = departamentos[random.Next(departamentos.Length)];
                var tipo = tipos[random.Next(tipos.Length)];
                var monto = tipo == "Ingreso" 
                    ? Math.Round(5000 + random.NextDouble() * 50000, 2)
                    : Math.Round(500 + random.NextDouble() * 15000, 2);
                var referencia = $"REF-2024-{random.Next(1000, 9999)}";

                sb.AppendLine($"{fecha:yyyy-MM-dd},{transaccion},{categoria},{departamento},{monto},{tipo},{referencia}");
            }

            await File.WriteAllTextAsync(rutaDemo, sb.ToString());
            return rutaDemo;
        }
    }
}
