using System.Data;
using System.Text;
using System.Text.Json;
using ExcelDataReader;

namespace TFG_APPBusinessIntelligence.Services
{
    // ── Modelos de resultado ──────────────────────────────────────────────────

    public class ColumnaInfo
    {
        public string Nombre      { get; set; } = "";
        public string Tipo        { get; set; } = "";   // Numerica | Categorica | Fecha | Booleano
        public int    TotalFilas  { get; set; }
        public int    Nulos       { get; set; }
        public double PctNulos    { get; set; }
        public int    Unicos      { get; set; }
        public string Ejemplo     { get; set; } = "";

        // Solo numericas
        public double? Media      { get; set; }
        public double? Desv       { get; set; }
        public double? Min        { get; set; }
        public double? P25        { get; set; }
        public double? Mediana    { get; set; }
        public double? P75        { get; set; }
        public double? Max        { get; set; }
        public double? Sesgo      { get; set; }
        public double? Curtosis   { get; set; }
        public List<double> Outliers { get; set; } = new();

        // Solo categoricas
        public List<(string Valor, int Conteo)> TopValores { get; set; } = new();
    }

    public class ParCorrelacion
    {
        public string ColA { get; set; } = "";
        public string ColB { get; set; } = "";
        public double R    { get; set; }
    }

    public class ResultadoAnalisis
    {
        public string NombreArchivo   { get; set; } = "";
        public string RutaArchivo     { get; set; } = "";
        public long   TamanoBytes     { get; set; }
        public int    Filas           { get; set; }
        public int    Columnas        { get; set; }
        public int    NulosTotales    { get; set; }
        public double PctNulosTotales { get; set; }
        public int    Duplicados      { get; set; }
        public long   MemoriaEstimada { get; set; }

        public List<ColumnaInfo>     Columnas_Info     { get; set; } = new();
        public List<ParCorrelacion>  Correlaciones     { get; set; } = new();
        public List<string>          Conclusiones      { get; set; } = new();

        public DateTime FechaAnalisis { get; set; } = DateTime.Now;
    }

    // ── Servicio principal ────────────────────────────────────────────────────

    public class DatasetAnalyzerService
    {
        // ── Carga ────────────────────────────────────────────────────────────

        public DataTable CargarArchivo(string ruta)
        {
            string ext = Path.GetExtension(ruta).ToLowerInvariant();
            return ext switch
            {
                ".csv"          => CargarCsv(ruta),
                ".tsv"          => CargarCsv(ruta, '\t'),
                ".json"         => CargarJson(ruta),
                ".xls" or ".xlsx" => CargarExcel(ruta),
                _ => throw new NotSupportedException($"Formato no soportado: {ext}. Usa CSV, TSV, Excel o JSON.")
            };
        }

        private static DataTable CargarCsv(string ruta, char? separadorForzado = null)
        {
            var lineas = File.ReadAllLines(ruta, Encoding.UTF8);
            if (lineas.Length == 0) return new DataTable();

            char sep = separadorForzado ?? DetectarSeparador(lineas[0]);
            var tabla = new DataTable();

            var cabeceras = ParsearLinea(lineas[0], sep);
            foreach (var h in cabeceras)
                tabla.Columns.Add(string.IsNullOrWhiteSpace(h) ? $"Col{tabla.Columns.Count}" : h.Trim());

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                var campos = ParsearLinea(lineas[i], sep);
                var fila = tabla.NewRow();
                for (int c = 0; c < tabla.Columns.Count && c < campos.Count; c++)
                    fila[c] = campos[c];
                tabla.Rows.Add(fila);
            }
            return tabla;
        }

        private static char DetectarSeparador(string linea)
        {
            foreach (char c in new[] { ',', ';', '\t', '|' })
                if (linea.Contains(c)) return c;
            return ',';
        }

        private static List<string> ParsearLinea(string linea, char sep)
        {
            var resultado = new List<string>();
            bool enComillas = false;
            var actual = new StringBuilder();
            foreach (char c in linea)
            {
                if (c == '"') { enComillas = !enComillas; continue; }
                if (c == sep && !enComillas) { resultado.Add(actual.ToString()); actual.Clear(); continue; }
                actual.Append(c);
            }
            resultado.Add(actual.ToString());
            return resultado;
        }

        private static DataTable CargarJson(string ruta)
        {
            var json = File.ReadAllText(ruta);
            var tabla = new DataTable();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Acepta array de objetos o un objeto con propiedad array
            JsonElement array = root.ValueKind == JsonValueKind.Array
                ? root
                : root.EnumerateObject().FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Array).Value;

            if (array.ValueKind != JsonValueKind.Array) throw new InvalidDataException("JSON no contiene un array de objetos.");

            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var fila = tabla.NewRow();
                foreach (var prop in item.EnumerateObject())
                {
                    if (!tabla.Columns.Contains(prop.Name))
                        tabla.Columns.Add(prop.Name);
                    fila[prop.Name] = prop.Value.ToString();
                }
                tabla.Rows.Add(fila);
            }
            return tabla;
        }

        private static DataTable CargarExcel(string ruta)
        {
            // ExcelDataReader requiere este registro en .NET (no Windows)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(ruta, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var config = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };
            var ds = reader.AsDataSet(config);
            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        // ── Analisis ──────────────────────────────────────────────────────────

        public ResultadoAnalisis Analizar(string ruta)
        {
            var tabla = CargarArchivo(ruta);
            var info  = new FileInfo(ruta);

            var resultado = new ResultadoAnalisis
            {
                NombreArchivo = Path.GetFileName(ruta),
                RutaArchivo   = ruta,
                TamanoBytes   = info.Length,
                Filas         = tabla.Rows.Count,
                Columnas      = tabla.Columns.Count,
            };

            // Nulos totales y memoria estimada
            long nulos = 0;
            long memoria = 0;
            foreach (DataColumn col in tabla.Columns)
            {
                foreach (DataRow fila in tabla.Rows)
                {
                    var v = fila[col];
                    if (v == null || v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString()))
                        nulos++;
                    else
                        memoria += Encoding.UTF8.GetByteCount(v.ToString()!);
                }
            }
            resultado.NulosTotales    = (int)nulos;
            resultado.PctNulosTotales = resultado.Filas * resultado.Columnas > 0
                ? Math.Round(nulos * 100.0 / (resultado.Filas * resultado.Columnas), 2) : 0;
            resultado.MemoriaEstimada = memoria;

            // Duplicados (usando hash de fila completa)
            var hashes = new HashSet<string>();
            int dupes   = 0;
            foreach (DataRow fila in tabla.Rows)
            {
                var hash = string.Join("|", fila.ItemArray.Select(v => v?.ToString() ?? ""));
                if (!hashes.Add(hash)) dupes++;
            }
            resultado.Duplicados = dupes;

            // Analisis por columna
            var colsNumericas = new List<(string nombre, List<double> valores)>();

            foreach (DataColumn col in tabla.Columns)
            {
                var info_col = AnalizarColumna(tabla, col);
                resultado.Columnas_Info.Add(info_col);

                if (info_col.Tipo == "Numerica" && info_col.Media.HasValue)
                {
                    var vals = ObtenerValoresNumericos(tabla, col);
                    colsNumericas.Add((col.ColumnName, vals));
                }
            }

            // Correlaciones (Pearson)
            if (colsNumericas.Count >= 2)
            {
                for (int i = 0; i < colsNumericas.Count; i++)
                    for (int j = i + 1; j < colsNumericas.Count; j++)
                    {
                        double r = PearsonCorrelation(colsNumericas[i].valores, colsNumericas[j].valores);
                        if (!double.IsNaN(r) && Math.Abs(r) >= 0.7)
                            resultado.Correlaciones.Add(new ParCorrelacion
                            {
                                ColA = colsNumericas[i].nombre,
                                ColB = colsNumericas[j].nombre,
                                R    = Math.Round(r, 3)
                            });
                    }
            }

            // Conclusiones
            resultado.Conclusiones = GenerarConclusiones(resultado);
            return resultado;
        }

        private static ColumnaInfo AnalizarColumna(DataTable tabla, DataColumn col)
        {
            var todos    = tabla.Rows.Cast<DataRow>().Select(r => r[col]?.ToString() ?? "").ToList();
            int nulos    = todos.Count(v => string.IsNullOrWhiteSpace(v));
            var noNulos  = todos.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            int unicos   = noNulos.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            string ejemplo = noNulos.FirstOrDefault() ?? "-";

            var ci = new ColumnaInfo
            {
                Nombre     = col.ColumnName,
                TotalFilas = tabla.Rows.Count,
                Nulos      = nulos,
                PctNulos   = tabla.Rows.Count > 0 ? Math.Round(nulos * 100.0 / tabla.Rows.Count, 1) : 0,
                Unicos     = unicos,
                Ejemplo    = ejemplo.Length > 30 ? ejemplo[..30] + "..." : ejemplo
            };

            var numericos = ObtenerValoresNumericos(tabla, col);

            if (numericos.Count >= (int)(noNulos.Count * 0.8) && numericos.Count >= 2)
            {
                ci.Tipo     = "Numerica";
                ci.Media    = Math.Round(numericos.Average(), 4);
                ci.Desv     = Math.Round(DesviacionEstandar(numericos), 4);
                ci.Min      = numericos.Min();
                ci.Max      = numericos.Max();
                ci.Mediana  = Math.Round(Percentil(numericos, 50), 4);
                ci.P25      = Math.Round(Percentil(numericos, 25), 4);
                ci.P75      = Math.Round(Percentil(numericos, 75), 4);
                ci.Sesgo    = Math.Round(Sesgo(numericos), 4);
                ci.Curtosis = Math.Round(Curtosis(numericos), 4);
                ci.Outliers = DetectarOutliers(numericos);
            }
            else if (noNulos.All(v => DateTime.TryParse(v, out _)))
            {
                ci.Tipo = "Fecha";
            }
            else if (noNulos.All(v => v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                      v.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                                      v == "1" || v == "0"))
            {
                ci.Tipo = "Booleano";
            }
            else
            {
                ci.Tipo = "Categorica";
                ci.TopValores = noNulos
                    .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .Select(g => (g.Key, g.Count()))
                    .ToList();
            }

            return ci;
        }

        private static List<double> ObtenerValoresNumericos(DataTable tabla, DataColumn col)
        {
            var lista = new List<double>();
            foreach (DataRow fila in tabla.Rows)
            {
                var v = fila[col]?.ToString();
                if (!string.IsNullOrWhiteSpace(v) && double.TryParse(
                        v.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double d))
                    lista.Add(d);
            }
            return lista;
        }

        // ── Estadisticas ──────────────────────────────────────────────────────

        private static double DesviacionEstandar(List<double> v)
        {
            if (v.Count < 2) return 0;
            double media = v.Average();
            return Math.Sqrt(v.Sum(x => Math.Pow(x - media, 2)) / (v.Count - 1));
        }

        private static double Percentil(List<double> sorted_v, double p)
        {
            var s = sorted_v.OrderBy(x => x).ToList();
            if (s.Count == 0) return 0;
            double idx = (p / 100.0) * (s.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = (int)Math.Ceiling(idx);
            return lo == hi ? s[lo] : s[lo] + (idx - lo) * (s[hi] - s[lo]);
        }

        private static double Sesgo(List<double> v)
        {
            if (v.Count < 3) return 0;
            double media = v.Average();
            double desv  = DesviacionEstandar(v);
            if (desv == 0) return 0;
            double n = v.Count;
            return (n / ((n - 1) * (n - 2))) * v.Sum(x => Math.Pow((x - media) / desv, 3));
        }

        private static double Curtosis(List<double> v)
        {
            if (v.Count < 4) return 0;
            double media = v.Average();
            double desv  = DesviacionEstandar(v);
            if (desv == 0) return 0;
            double n = v.Count;
            return (n * (n + 1) / ((n - 1) * (n - 2) * (n - 3)))
                   * v.Sum(x => Math.Pow((x - media) / desv, 4))
                   - 3 * Math.Pow(n - 1, 2) / ((n - 2) * (n - 3));
        }

        private static List<double> DetectarOutliers(List<double> v)
        {
            double q1  = Percentil(v, 25);
            double q3  = Percentil(v, 75);
            double iqr = q3 - q1;
            double lo  = q1 - 1.5 * iqr;
            double hi  = q3 + 1.5 * iqr;
            return v.Where(x => x < lo || x > hi).Distinct().OrderBy(x => x).Take(10).ToList();
        }

        private static double PearsonCorrelation(List<double> x, List<double> y)
        {
            int n = Math.Min(x.Count, y.Count);
            if (n < 2) return double.NaN;
            double mx = x.Take(n).Average(), my = y.Take(n).Average();
            double num = 0, dx = 0, dy = 0;
            for (int i = 0; i < n; i++)
            {
                num += (x[i] - mx) * (y[i] - my);
                dx  += Math.Pow(x[i] - mx, 2);
                dy  += Math.Pow(y[i] - my, 2);
            }
            double den = Math.Sqrt(dx * dy);
            return den == 0 ? double.NaN : num / den;
        }

        // ── Conclusiones automaticas ──────────────────────────────────────────

        private static List<string> GenerarConclusiones(ResultadoAnalisis r)
        {
            var c = new List<string>();
            int numNumericas   = r.Columnas_Info.Count(ci => ci.Tipo == "Numerica");
            int numCategoricas = r.Columnas_Info.Count(ci => ci.Tipo == "Categorica");
            int numFechas      = r.Columnas_Info.Count(ci => ci.Tipo == "Fecha");

            // --- Calidad general ---
            if (r.PctNulosTotales == 0)
                c.Add("Integridad de datos: el dataset no presenta valores nulos, lo que indica una calidad de datos optima. No se requieren estrategias de imputacion.");
            else if (r.PctNulosTotales < 5)
                c.Add($"Valores nulos: el {r.PctNulosTotales}% de los registros contienen valores ausentes. Este nivel es manejable; se recomienda aplicar imputacion por media/mediana en variables numericas y por moda en categoricas, o eliminar las filas afectadas si su proporcion es reducida.");
            else if (r.PctNulosTotales < 20)
                c.Add($"Valores nulos elevados: el {r.PctNulosTotales}% de los datos son nulos. Es aconsejable revisar el origen de los datos, aplicar tecnicas avanzadas de imputacion (KNN, regresion) y valorar la eliminacion de columnas con mas del 50% de nulos.");
            else
                c.Add($"Calidad critica: el {r.PctNulosTotales}% de los valores son nulos. Este porcentaje compromete la fiabilidad del analisis. Se recomienda una revision exhaustiva del proceso de captura de datos antes de continuar.");

            // --- Duplicados ---
            if (r.Duplicados > 0)
            {
                double pctDup = Math.Round(r.Duplicados * 100.0 / Math.Max(r.Filas, 1), 1);
                c.Add($"Duplicados: se identificaron {r.Duplicados:N0} filas duplicadas ({pctDup}% del total). Su presencia puede sesgar modelos estadisticos y de machine learning. Se recomienda eliminarlas con un paso de deduplicacion previo al modelado.");
            }
            else
            {
                c.Add("Unicidad de registros: no se detectaron filas duplicadas, lo que garantiza la integridad de cada observacion en el dataset.");
            }

            // --- Tamano del dataset ---
            if (r.Filas < 100)
                c.Add($"Volumen insuficiente: con {r.Filas:N0} filas el dataset tiene un tamano muy reducido. Los resultados estadisticos pueden no ser representativos y los modelos de aprendizaje automatico podrian sobreajustarse. Considera ampliar la muestra.");
            else if (r.Filas < 1_000)
                c.Add($"Volumen bajo: el dataset contiene {r.Filas:N0} filas, suficiente para analisis exploratorio basico, pero limitado para tecnicas de ML complejas. Se recomienda validacion cruzada estricta.");
            else if (r.Filas > 100_000)
                c.Add($"Volumen alto: con {r.Filas:N0} filas el dataset tiene un volumen excelente para entrenamiento de modelos de machine learning robustos y generalizables.");
            else
                c.Add($"Volumen adecuado: el dataset cuenta con {r.Filas:N0} filas, un tamano apropiado para la mayoria de tecnicas de analisis estadistico y modelado predictivo.");

            // --- Estructura de columnas ---
            c.Add($"Composicion del dataset: {r.Columnas} columnas en total - {numNumericas} numericas, {numCategoricas} categoricas" +
                  (numFechas > 0 ? $" y {numFechas} temporales" : "") +
                  ". Esta distribucion " + (numNumericas > numCategoricas
                      ? "favorece el uso de modelos de regresion y analisis de correlacion."
                      : "sugiere la aplicacion de tecnicas de codificacion (one-hot, label encoding) antes del modelado."));

            // --- Sesgo ---
            var sesgadas = r.Columnas_Info
                .Where(ci => ci.Sesgo.HasValue && Math.Abs(ci.Sesgo.Value) > 1)
                .ToList();
            if (sesgadas.Any())
            {
                string cols = string.Join(", ", sesgadas.Take(5).Select(ci => $"{ci.Nombre} (s={ci.Sesgo:F2})"));
                c.Add($"Distribucion asimetrica: las siguientes columnas presentan sesgo elevado (|sesgo| > 1): {cols}. Se recomienda aplicar transformaciones logaritmicas, Box-Cox o raiz cuadrada para normalizar su distribucion antes del modelado.");
            }

            // --- Outliers ---
            var conOutliers = r.Columnas_Info.Where(ci => ci.Outliers.Any()).ToList();
            if (conOutliers.Any())
            {
                string cols = string.Join(", ", conOutliers.Take(5).Select(ci => $"{ci.Nombre} ({ci.Outliers.Count} valores)"));
                c.Add($"Valores atipicos (metodo IQR): se detectaron outliers en {cols}. Evalua si representan errores de medicion (eliminar) o casos extremos legitimos (mantener y usar modelos robustos como arboles de decision o Random Forest).");
            }
            else
            {
                c.Add("Valores atipicos: no se detectaron outliers significativos mediante el metodo IQR. Las distribuciones numericas son estables.");
            }

            // --- Correlaciones ---
            if (r.Correlaciones.Any())
            {
                var altasPos = r.Correlaciones.Where(p => p.R >= 0.7).ToList();
                var altasNeg = r.Correlaciones.Where(p => p.R <= -0.7).ToList();
                string detalle = string.Join("; ", r.Correlaciones.Take(3).Select(p => $"{p.ColA} <-> {p.ColB} (r={p.R:F2})"));
                c.Add($"Multicolinealidad: se encontraron {r.Correlaciones.Count} pares con correlacion alta (|r| >= 0.7): {detalle}. " +
                      "Esto puede inflar la varianza de los coeficientes en modelos lineales. Considera aplicar PCA, seleccion de caracteristicas o usar modelos que gestionen la colinealidad internamente (Ridge, Lasso).");
            }
            else
            {
                c.Add("Independencia de variables: no se detectaron correlaciones lineales fuertes (|r| >= 0.7) entre las variables numericas, lo que reduce el riesgo de multicolinealidad en modelos de regresion.");
            }

            // --- Recomendacion final ---
            bool listoParaML = r.PctNulosTotales < 10 && r.Duplicados == 0 && r.Filas >= 500;
            if (listoParaML)
                c.Add("Valoracion global: el dataset presenta una calidad suficiente para proceder directamente al modelado predictivo. Se recomienda como siguiente paso la seleccion de caracteristicas (feature selection) y la division en conjuntos de entrenamiento y prueba (train/test split).");
            else
                c.Add("Valoracion global: antes de aplicar modelos predictivos es necesario completar el preprocesamiento: tratar nulos, eliminar duplicados y garantizar un volumen minimo representativo. Se recomienda documentar cada transformacion para asegurar la reproducibilidad del pipeline de datos.");

            return c;
        }
    }
}
