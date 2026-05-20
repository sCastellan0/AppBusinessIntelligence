using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Services
{
    public class GeneradorPdfService
    {
        private static bool _fontResolverRegistrado = false;

        private static void AsegurarFontResolver()
        {
            if (_fontResolverRegistrado) return;
            GlobalFontSettings.FontResolver = AppFontResolver.Instance;
            _fontResolverRegistrado = true;
        }
        // ── Colores corporativos ──────────────────────────────────────────────
        private static readonly XColor AzulOscuro  = XColor.FromArgb(30,  58,  95);
        private static readonly XColor AzulPrinc   = XColor.FromArgb(59,  130, 246);
        private static readonly XColor AzulClaro   = XColor.FromArgb(219, 234, 254);
        private static readonly XColor GrisTexto   = XColor.FromArgb(50,  50,  50);
        private static readonly XColor GrisFondo   = XColor.FromArgb(248, 250, 252);
        private static readonly XColor Blanco       = XColors.White;
        private static readonly XColor Rojo         = XColor.FromArgb(220, 38,  38);
        private static readonly XColor Verde        = XColor.FromArgb(22,  163, 74);

        private const double MargenIzq = 40;
        private const double MargenDer = 40;
        private const double MargenSup = 50;
        private const double MargenInf = 40;
        private const double AnchoUtil = 595 - MargenIzq - MargenDer;  // A4 = 595pt

        // ── Fuentes ───────────────────────────────────────────────────────────
        private static XFont FontTit(double sz)  => new("OpenSans", sz, XFontStyle.Bold);
        private static XFont FontNeg(double sz)  => new("OpenSans", sz, XFontStyle.Bold);
        private static XFont FontReg(double sz)  => new("OpenSans", sz, XFontStyle.Regular);

        // ── Estado de pagina ──────────────────────────────────────────────────
        private PdfDocument  _doc   = null!;
        private PdfPage      _pag   = null!;
        private XGraphics    _gfx   = null!;
        private double       _y;
        private int          _nPag;

        // ─────────────────────────────────────────────────────────────────────
        // PUNTO DE ENTRADA
        // ─────────────────────────────────────────────────────────────────────
        public string GenerarPdf(ResultadoAnalisis resultado, string? carpetaDestino = null)
        {
            AsegurarFontResolver();

            _doc  = new PdfDocument();
            _nPag = 0;

            // Portada
            NuevaPagina(portada: true);
            DibujarPortada(resultado);

            // Contenido
            NuevaPagina();

            SeccionResumenGeneral(resultado);
            SeccionTiposYNulos(resultado);
            SeccionNumericas(resultado);
            SeccionCategoricas(resultado);
            SeccionCorrelaciones(resultado);
            SeccionConclusiones(resultado);

            // Guardar
            string carpeta = carpetaDestino
                ?? Path.GetDirectoryName(resultado.RutaArchivo)
                ?? FileSystem.CacheDirectory;

            string nombre   = Path.GetFileNameWithoutExtension(resultado.NombreArchivo);
            string rutaPdf  = Path.Combine(carpeta, $"informe_{nombre}.pdf");

            _doc.Save(rutaPdf);
            return rutaPdf;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GESTIoN DE PaGINAS
        // ─────────────────────────────────────────────────────────────────────
        private void NuevaPagina(bool portada = false)
        {
            _pag = _doc.AddPage();
            _pag.Width  = XUnit.FromPoint(595);
            _pag.Height = XUnit.FromPoint(842);
            _gfx = XGraphics.FromPdfPage(_pag);
            _nPag++;
            _y = MargenSup;

            if (!portada)
            {
                DibujarCabecera();
                DibujarPie();
                _y = MargenSup + 20;
            }
        }

        private void VerificarSalto(double alturaRequerida = 30)
        {
            if (_y + alturaRequerida > _pag.Height.Point - MargenInf)
                NuevaPagina();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CABECERA Y PIE
        // ─────────────────────────────────────────────────────────────────────
        private void DibujarCabecera()
        {
            _gfx.DrawRectangle(new XSolidBrush(AzulOscuro),
                new XRect(0, 0, _pag.Width.Point, 28));
            _gfx.DrawString("AppBusinessIntelligence - Informe de Analisis de Dataset",
                FontReg(8), new XSolidBrush(Blanco),
                new XRect(0, 8, _pag.Width.Point, 0), XStringFormats.TopLeft);
        }

        private void DibujarPie()
        {
            double y = _pag.Height.Point - 20;
            _gfx.DrawString(
                $"Pagina {_nPag}  |  Generado el {DateTime.Now:dd/MM/yyyy HH:mm}",
                FontReg(7), new XSolidBrush(XColor.FromArgb(150, 150, 150)),
                new XRect(0, y, _pag.Width.Point, 0), XStringFormats.TopLeft);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PORTADA
        // ─────────────────────────────────────────────────────────────────────
        private void DibujarPortada(ResultadoAnalisis r)
        {
            // Fondo degradado simulado
            int pasos = 30;
            for (int i = 0; i < pasos; i++)
            {
                double t   = i / (double)(pasos - 1);
                int    b   = (int)(30  + t * 10);
                int    g   = (int)(58  + t * 40);
                int    bl  = (int)(95  + t * 120);
                double alH = _pag.Height.Point / pasos;
                _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(b, g, bl)),
                    new XRect(0, i * alH, _pag.Width.Point, alH + 1));
            }

            // Titulo principal
            _gfx.DrawString("AppBusinessIntelligence",
                FontTit(26), new XSolidBrush(Blanco),
                new XRect(0, 148, _pag.Width.Point, 0), XStringFormats.TopLeft);

            _gfx.DrawString("Informe de Analisis de Dataset",
                FontReg(15), new XSolidBrush(XColor.FromArgb(200, 225, 255)),
                new XRect(0, 190, _pag.Width.Point, 0), XStringFormats.TopLeft);

            // Tarjeta central blanca
            double cx = 85, cy = 240, cw = 425, ch = 140;
            _gfx.DrawRoundedRectangle(new XSolidBrush(Blanco), new XRect(cx, cy, cw, ch), new XSize(10, 10));
            _gfx.DrawRoundedRectangle(new XPen(AzulPrinc, 1.5), new XRect(cx, cy, cw, ch), new XSize(10, 10));

            string nombreCorto = r.NombreArchivo.Length > 45
                ? r.NombreArchivo[..45] + "..."
                : r.NombreArchivo;

            _gfx.DrawString($"Archivo: {nombreCorto}", FontNeg(11), new XSolidBrush(AzulOscuro),
                new XRect(cx, cy + 24, cw, 0), XStringFormats.TopLeft);

            _gfx.DrawString($"Filas: {r.Filas:N0}   |   Columnas: {r.Columnas}", FontReg(10), new XSolidBrush(GrisTexto),
                new XRect(cx, cy + 50, cw, 0), XStringFormats.TopLeft);

            _gfx.DrawString($"Fecha: {r.FechaAnalisis:dd 'de' MMMM 'de' yyyy, HH:mm}", FontReg(9), new XSolidBrush(GrisTexto),
                new XRect(cx, cy + 72, cw, 0), XStringFormats.TopLeft);

            _gfx.DrawString("Generado automaticamente por AppBusinessIntelligence", FontReg(8),
                new XSolidBrush(XColor.FromArgb(130, 130, 130)),
                new XRect(cx, cy + 104, cw, 0), XStringFormats.TopLeft);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS DE DIBUJO
        // ─────────────────────────────────────────────────────────────────────
        private void DibujarSeccion(string titulo)
        {
            VerificarSalto(22);
            _gfx.DrawRectangle(new XSolidBrush(AzulPrinc), new XRect(MargenIzq, _y, AnchoUtil, 18));
            _gfx.DrawString($"  {titulo}", FontNeg(9.5), new XSolidBrush(Blanco),
                new XRect(MargenIzq + 4, _y + 3, AnchoUtil, 0), XStringFormats.TopLeft);
            _y += 22;
        }

        private void DibujarSubseccion(string titulo)
        {
            VerificarSalto(16);
            _gfx.DrawString(titulo, FontNeg(9), new XSolidBrush(AzulOscuro),
                new XRect(MargenIzq, _y, AnchoUtil, 0), XStringFormats.TopLeft);
            _y += 4;
            _gfx.DrawLine(new XPen(AzulPrinc, 0.5), MargenIzq, _y, MargenIzq + AnchoUtil, _y);
            _y += 7;
        }

        private void DibujarTexto(string texto, double sz = 9)
        {
            VerificarSalto(14);
            _gfx.DrawString(texto, FontReg(sz), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq, _y, AnchoUtil, 0), XStringFormats.TopLeft);
            _y += 13;
        }

        private void DibujarFilaTabla(string etiqueta, string valor, bool sombreado)
        {
            VerificarSalto(14);
            XColor fondo = sombreado ? AzulClaro : Blanco;
            double eW = 160, vW = AnchoUtil - 160;
            _gfx.DrawRectangle(new XSolidBrush(fondo), new XRect(MargenIzq, _y, eW, 13));
            _gfx.DrawRectangle(new XSolidBrush(fondo), new XRect(MargenIzq + eW, _y, vW, 13));
            _gfx.DrawRectangle(new XPen(XColor.FromArgb(200, 210, 230), 0.3), new XRect(MargenIzq, _y, AnchoUtil, 13));
            _gfx.DrawString(etiqueta, FontNeg(8.5), new XSolidBrush(AzulOscuro),
                new XRect(MargenIzq + 4, _y + 2, eW - 6, 0), XStringFormats.TopLeft);
            _gfx.DrawString(valor.Length > 75 ? valor[..75] + "..." : valor,
                FontReg(8.5), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq + eW + 4, _y + 2, vW - 6, 0), XStringFormats.TopLeft);
            _y += 14;
        }

        private void DibujarFilaTablaGrid(string[] celdas, double[] anchos, bool cabecera, bool sombreado)
        {
            VerificarSalto(13);
            double x = MargenIzq;
            XColor fondo = cabecera ? AzulOscuro : sombreado ? AzulClaro : Blanco;
            XColor colorTexto = cabecera ? Blanco : GrisTexto;
            XFont  fuente = cabecera ? FontNeg(8) : FontReg(8);

            for (int i = 0; i < celdas.Length && i < anchos.Length; i++)
            {
                _gfx.DrawRectangle(new XSolidBrush(fondo), new XRect(x, _y, anchos[i], 13));
                _gfx.DrawRectangle(new XPen(XColor.FromArgb(180, 200, 220), 0.3), new XRect(x, _y, anchos[i], 13));
                int maxChars = Math.Max(4, (int)(anchos[i] / 5.5));
                string txt = celdas[i].Length > maxChars ? celdas[i][..maxChars] + "..." : celdas[i];
                _gfx.DrawString(txt, fuente, new XSolidBrush(colorTexto),
                    new XRect(x + 3, _y + 2.5, anchos[i] - 5, 0), XStringFormats.TopLeft);
                x += anchos[i];
            }
            _y += 14;
        }

        private void DibujarBarra(string etiqueta, int valor, int maximo, double alturaFila = 14)
        {
            VerificarSalto(alturaFila + 2);
            double etiqW = 130;
            double barW  = AnchoUtil - etiqW - 45;
            double relleno = maximo > 0 ? (valor / (double)maximo) * barW : 0;

            _gfx.DrawString(etiqueta.Length > 25 ? etiqueta[..25] + "..." : etiqueta,
                FontReg(8), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq, _y + 2, etiqW, 0), XStringFormats.TopLeft);

            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(220, 230, 245)),
                new XRect(MargenIzq + etiqW, _y + 2, barW, alturaFila - 4));
            if (relleno > 0)
                _gfx.DrawRectangle(new XSolidBrush(AzulPrinc),
                    new XRect(MargenIzq + etiqW, _y + 2, relleno, alturaFila - 4));
            _gfx.DrawString(valor.ToString("N0"), FontReg(7.5),
                new XSolidBrush(GrisTexto),
                new XRect(MargenIzq + etiqW + barW + 4, _y + 2, 42, 0), XStringFormats.TopLeft);
            _y += alturaFila;
        }

        private void DibujarLeyendaRango()
        {
            VerificarSalto(14);
            double x = MargenIzq + 130;
            // Cuadrado azul claro = rango total
            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(190, 210, 240)), new XRect(x, _y + 2, 10, 8));
            _gfx.DrawString("Rango total", FontReg(7.5), new XSolidBrush(GrisTexto), new XRect(x + 12, _y + 1, 60, 0), XStringFormats.TopLeft);
            // Cuadrado azul oscuro = media
            _gfx.DrawRectangle(new XSolidBrush(AzulOscuro), new XRect(x + 80, _y + 2, 10, 8));
            _gfx.DrawString("Media", FontReg(7.5), new XSolidBrush(GrisTexto), new XRect(x + 92, _y + 1, 40, 0), XStringFormats.TopLeft);
            _y += 14;
        }

        /// <summary>Dibuja una barra que muestra min, media y max de forma visual.</summary>
        private void DibujarBarraRango(string etiqueta, double min, double media, double max, double alturaFila = 15)
        {
            VerificarSalto(alturaFila + 2);
            double etiqW = 130;
            double barW  = AnchoUtil - etiqW - 70;
            double rango = max - min;
            if (rango <= 0) rango = 1;

            double posMedia = (media - min) / rango * barW;

            _gfx.DrawString(etiqueta.Length > 22 ? etiqueta[..22] + "..." : etiqueta,
                FontReg(8), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq, _y + 2, etiqW, 0), XStringFormats.TopLeft);

            double bx = MargenIzq + etiqW;
            double by = _y + 4;
            double bh = alturaFila - 8;

            // Fondo = rango total
            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(190, 210, 240)), new XRect(bx, by, barW, bh));
            // Marcador media
            double mx = bx + posMedia - 1;
            _gfx.DrawRectangle(new XSolidBrush(AzulOscuro), new XRect(mx, by - 1, 3, bh + 2));

            // Etiquetas min / max
            _gfx.DrawString(Fmt(min), FontReg(6.5), new XSolidBrush(GrisTexto),
                new XRect(bx, by + bh + 1, 35, 0), XStringFormats.TopLeft);
            _gfx.DrawString(Fmt(max), FontReg(6.5), new XSolidBrush(GrisTexto),
                new XRect(bx + barW - 20, by + bh + 1, 35, 0), XStringFormats.TopLeft);

            _y += alturaFila + 5;
        }

        private void DibujarBarraColoreada(string etiqueta, int valor, int maximo, XColor color, double alturaFila = 16, string sufijo = " col.")
        {
            VerificarSalto(alturaFila + 2);
            double etiqW = 130;
            double barW  = AnchoUtil - etiqW - 55;
            double relleno = maximo > 0 ? (valor / (double)maximo) * barW : 0;

            _gfx.DrawString(etiqueta.Length > 25 ? etiqueta[..25] + "..." : etiqueta,
                FontReg(8.5), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq, _y + 2, etiqW, 0), XStringFormats.TopLeft);

            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(230, 234, 240)),
                new XRect(MargenIzq + etiqW, _y + 3, barW, alturaFila - 6));
            if (relleno > 0)
                _gfx.DrawRectangle(new XSolidBrush(color),
                    new XRect(MargenIzq + etiqW, _y + 3, relleno, alturaFila - 6));

            _gfx.DrawString($"{valor}{sufijo}", FontNeg(8), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq + etiqW + barW + 5, _y + 2, 55, 0), XStringFormats.TopLeft);
            _y += alturaFila;
        }

        /// <summary>Barra horizontal que muestra |r| de 0 a 1 con escala fija.</summary>
        private void DibujarBarraCorrelacion(string etiqueta, double absR, XColor color, double alturaFila = 14)
        {
            VerificarSalto(alturaFila + 2);
            double etiqW = 160;
            double barMaxW = AnchoUtil - etiqW - 55;
            double relleno = absR * barMaxW;

            string etiqCorta = etiqueta.Length > 28 ? etiqueta[..28] + "..." : etiqueta;
            _gfx.DrawString(etiqCorta, FontReg(8), new XSolidBrush(GrisTexto),
                new XRect(MargenIzq, _y + 2, etiqW, 0), XStringFormats.TopLeft);

            double bx = MargenIzq + etiqW;
            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(225, 228, 235)), new XRect(bx, _y + 3, barMaxW, alturaFila - 6));
            if (relleno > 0)
                _gfx.DrawRectangle(new XSolidBrush(color), new XRect(bx, _y + 3, relleno, alturaFila - 6));

            _gfx.DrawString(absR.ToString("F3"), FontNeg(8), new XSolidBrush(GrisTexto),
                new XRect(bx + barMaxW + 5, _y + 2, 45, 0), XStringFormats.TopLeft);
            _y += alturaFila;
        }
        // ─────────────────────────────────────────────────────────────────────
        private void SeccionResumenGeneral(ResultadoAnalisis r)
        {
            DibujarSeccion("1. Resumen General del Dataset");

            double tam = r.TamanoBytes / 1_048_576.0;
            var datos = new[]
            {
                ("Archivo",                r.NombreArchivo),
                ("Filas",                  $"{r.Filas:N0}"),
                ("Columnas",               $"{r.Columnas}"),
                ("Tamano en disco",        $"{tam:F3} MB"),
                ("Valores nulos totales",  $"{r.NulosTotales:N0} ({r.PctNulosTotales}%)"),
                ("Filas duplicadas",       $"{r.Duplicados:N0}"),
                ("Columnas numericas",     $"{r.Columnas_Info.Count(c => c.Tipo == "Numerica")}"),
                ("Columnas categoricas",   $"{r.Columnas_Info.Count(c => c.Tipo == "Categorica")}"),
                ("Columnas de fecha",      $"{r.Columnas_Info.Count(c => c.Tipo == "Fecha")}"),
                ("Memoria estimada",       $"{r.MemoriaEstimada / 1024.0:F1} KB"),
            };
            for (int i = 0; i < datos.Length; i++)
                DibujarFilaTabla(datos[i].Item1, datos[i].Item2, i % 2 == 0);

            // Grafica: distribucion de tipos de columna
            _y += 8;
            DibujarSubseccion("Distribucion de tipos de columna");
            var tiposData = new[]
            {
                ("Numericas",    r.Columnas_Info.Count(c => c.Tipo == "Numerica"),   AzulPrinc),
                ("Categoricas",  r.Columnas_Info.Count(c => c.Tipo == "Categorica"), XColor.FromArgb(99, 102, 241)),
                ("Fechas",       r.Columnas_Info.Count(c => c.Tipo == "Fecha"),      XColor.FromArgb(20, 184, 166)),
                ("Otras",        r.Columnas_Info.Count(c => c.Tipo != "Numerica" && c.Tipo != "Categorica" && c.Tipo != "Fecha"), XColor.FromArgb(156, 163, 175)),
            };
            int maxTipo = Math.Max(1, tiposData.Max(t => t.Item2));
            foreach (var (nombre, cnt, col) in tiposData)
                if (cnt > 0) DibujarBarraColoreada(nombre, cnt, maxTipo, col);

            _y += 6;
        }

        private void SeccionTiposYNulos(ResultadoAnalisis r)
        {
            DibujarSeccion("2. Tipos de Datos y Valores Nulos");

            // Tabla por columna
            DibujarSubseccion("Detalle por columna");
            double[] anchos = { 120, 60, 42, 48, 48, 197 };
            DibujarFilaTablaGrid(
                new[] { "Columna", "Tipo", "Nulos", "% Nulos", "unicos", "Ejemplo" },
                anchos, cabecera: true, sombreado: false);

            for (int i = 0; i < r.Columnas_Info.Count; i++)
            {
                var ci = r.Columnas_Info[i];
                DibujarFilaTablaGrid(
                    new[] {
                        ci.Nombre,
                        ci.Tipo,
                        ci.Nulos.ToString(),
                        $"{ci.PctNulos}%",
                        ci.Unicos.ToString(),
                        ci.Ejemplo
                    },
                    anchos, cabecera: false, sombreado: i % 2 == 0);
            }

            // Grafico de barras de nulos
            var colsConNulos = r.Columnas_Info.Where(c => c.Nulos > 0)
                                              .OrderByDescending(c => c.Nulos)
                                              .ToList();
            if (colsConNulos.Any())
            {
                _y += 6;
                DibujarSubseccion("Columnas con valores nulos");
                int maxNulos = colsConNulos.Max(c => c.Nulos);
                foreach (var ci in colsConNulos)
                    DibujarBarra(ci.Nombre, ci.Nulos, maxNulos);
            }
            _y += 6;
        }

        private void SeccionNumericas(ResultadoAnalisis r)
        {
            var nums = r.Columnas_Info.Where(c => c.Tipo == "Numerica").ToList();
            if (!nums.Any()) return;

            DibujarSeccion("3. Estadisticas Descriptivas - Variables Numericas");

            double[] anchos = { 110, 46, 42, 38, 38, 38, 38, 38, 38, 38 };
            DibujarFilaTablaGrid(
                new[] { "Columna", "Media", "Std", "Min", "P25", "P50", "P75", "Max", "Sesgo", "Kurt" },
                anchos, cabecera: true, sombreado: false);

            for (int i = 0; i < nums.Count; i++)
            {
                var ci = nums[i];
                DibujarFilaTablaGrid(
                    new[]
                    {
                        ci.Nombre,
                        Fmt(ci.Media), Fmt(ci.Desv), Fmt(ci.Min),
                        Fmt(ci.P25),   Fmt(ci.Mediana), Fmt(ci.P75),
                        Fmt(ci.Max),   Fmt(ci.Sesgo), Fmt(ci.Curtosis)
                    },
                    anchos, cabecera: false, sombreado: i % 2 == 0);
            }

            // Grafica: rango normalizado Min / Media / Max por columna
            _y += 8;
            DibujarSubseccion("Visualizacion de rango por variable numerica (Min - Media - Max)");
            DibujarLeyendaRango();
            foreach (var ci in nums)
            {
                if (ci.Min.HasValue && ci.Max.HasValue && ci.Media.HasValue)
                    DibujarBarraRango(ci.Nombre, ci.Min.Value, ci.Media.Value, ci.Max.Value);
            }

            // Grafica: numero de outliers por columna
            var conOutliers = nums.Where(c => c.Outliers.Any()).ToList();
            if (conOutliers.Any())
            {
                _y += 8;
                DibujarSubseccion("Numero de valores atipicos (outliers) por columna - metodo IQR");
                int maxOut = conOutliers.Max(c => c.Outliers.Count);
                foreach (var ci in conOutliers)
                    DibujarBarraColoreada(ci.Nombre, ci.Outliers.Count, maxOut, Rojo, alturaFila: 14, sufijo: " outliers");
            }
            _y += 6;
        }

        private void SeccionCategoricas(ResultadoAnalisis r)
        {
            var cats = r.Columnas_Info.Where(c => c.Tipo == "Categorica").ToList();
            if (!cats.Any()) return;

            DibujarSeccion("4. Variables Categoricas");

            foreach (var ci in cats)
            {
                DibujarSubseccion($"Columna: {ci.Nombre}");
                string moda = ci.TopValores.Any() ? ci.TopValores[0].Valor : "-";
                int modaCnt = ci.TopValores.Any() ? ci.TopValores[0].Conteo : 0;
                DibujarTexto(
                    $"unicos: {ci.Unicos}   |   Moda: \"{moda}\" ({modaCnt:N0} veces)   |   Nulos: {ci.Nulos}",
                    sz: 8.5);

                if (ci.TopValores.Any())
                {
                    int max = ci.TopValores.Max(v => v.Conteo);
                    foreach (var (val, cnt) in ci.TopValores.Take(10))
                        DibujarBarra(val, cnt, max, alturaFila: 11);
                }
                _y += 4;
            }
        }

        private void SeccionCorrelaciones(ResultadoAnalisis r)
        {
            var nums = r.Columnas_Info.Where(c => c.Tipo == "Numerica").ToList();
            if (nums.Count < 2) return;

            DibujarSeccion("5. Correlaciones entre Variables Numericas");

            if (r.Correlaciones.Any())
            {
                DibujarSubseccion("Pares con correlacion alta (|r| >= 0.7)");
                double[] anchos = { 180, 180, 155 };
                DibujarFilaTablaGrid(new[] { "Variable A", "Variable B", "Pearson r" },
                    anchos, cabecera: true, sombreado: false);

                for (int i = 0; i < r.Correlaciones.Count; i++)
                {
                    var p = r.Correlaciones[i];
                    DibujarFilaTablaGrid(
                        new[] { p.ColA, p.ColB, p.R.ToString("F3") },
                        anchos, cabecera: false, sombreado: i % 2 == 0);
                }

                // Grafica de barras de |r| por par
                _y += 8;
                DibujarSubseccion("Intensidad de correlacion por par de variables (|r|)");
                foreach (var p in r.Correlaciones)
                {
                    double absR = Math.Abs(p.R);
                    XColor colBarra = absR >= 0.9 ? Rojo : absR >= 0.8 ? XColor.FromArgb(234, 88, 12) : AzulPrinc;
                    string etiq = $"{p.ColA} / {p.ColB}";
                    DibujarBarraCorrelacion(etiq, absR, colBarra);
                }
            }
            else
            {
                DibujarTexto("No se encontraron pares con |r| >= 0.7.");
            }
            _y += 6;
        }

        private void SeccionConclusiones(ResultadoAnalisis r)
        {
            DibujarSeccion("6. Conclusiones y Recomendaciones");
            _y += 4;

            int num = 1;
            foreach (var c in r.Conclusiones)
            {
                VerificarSalto(30);

                // Linea separadora con suficiente espacio antes
                if (num > 1)
                {
                    _y += 6;
                    _gfx.DrawLine(new XPen(XColor.FromArgb(200, 208, 220), 0.5),
                        MargenIzq, _y, MargenIzq + AnchoUtil, _y);
                    _y += 8;
                }

                // Numero de punto
                _gfx.DrawString($"{num}.", FontNeg(9.5), new XSolidBrush(AzulOscuro),
                    new XRect(MargenIzq, _y, 18, 0), XStringFormats.TopLeft);

                // Texto en negro con sangria
                DibujarParrafoConWrap(c, FontReg(9), GrisTexto, MargenIzq + 18, AnchoUtil - 18);
                num++;
            }
            _y += 6;
        }

        /// <summary>Dibuja texto con salto de linea automatico dentro del ancho dado.</summary>
        private void DibujarParrafoConWrap(string texto, XFont fuente, XColor color, double x, double ancho)
        {
            var palabras = texto.Split(' ');
            var lineaActual = new System.Text.StringBuilder();
            double altLinea = fuente.Size * 1.45;

            foreach (var palabra in palabras)
            {
                string candidato = lineaActual.Length == 0 ? palabra : lineaActual + " " + palabra;
                double w = _gfx.MeasureString(candidato, fuente).Width;

                if (w > ancho && lineaActual.Length > 0)
                {
                    VerificarSalto(altLinea + 2);
                    _gfx.DrawString(lineaActual.ToString(), fuente, new XSolidBrush(color),
                        new XRect(x, _y, ancho, 0), XStringFormats.TopLeft);
                    _y += altLinea;
                    lineaActual.Clear();
                    lineaActual.Append(palabra);
                }
                else
                {
                    lineaActual.Clear();
                    lineaActual.Append(candidato);
                }
            }

            if (lineaActual.Length > 0)
            {
                VerificarSalto(altLinea + 2);
                _gfx.DrawString(lineaActual.ToString(), fuente, new XSolidBrush(color),
                    new XRect(x, _y, ancho, 0), XStringFormats.TopLeft);
                _y += altLinea;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private static string Fmt(double? v) => v.HasValue ? v.Value.ToString("G5") : "-";
        private static string Fmt(double v)  => v.ToString("G5");
    }
}
