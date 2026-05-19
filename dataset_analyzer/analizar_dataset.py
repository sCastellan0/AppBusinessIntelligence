"""
Analizador de Datasets - AppBusinessIntelligence
Genera un informe PDF detallado a partir de un archivo CSV, Excel o JSON.

Dependencias:
    pip install pandas matplotlib seaborn fpdf2 openpyxl scipy
"""

import sys
import os
import io
import warnings
import pandas as pd
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.ticker as mticker
import seaborn as sns
from fpdf import FPDF
from datetime import datetime
from scipy import stats

warnings.filterwarnings("ignore")

# ─────────────────────────────────────────────
# CONFIGURACIÓN
# ─────────────────────────────────────────────
PALETA      = "#3B82F6"   # azul principal
PALETA_SEC  = "#1E3A5F"   # azul oscuro
FONDO       = "#F8FAFC"
MAX_COLS_CORR = 20        # límite de columnas para la matriz de correlación
MAX_CATS      = 15        # máximo categorías en gráficos de barras


# ─────────────────────────────────────────────
# UTILIDADES
# ─────────────────────────────────────────────
def cargar_dataset(ruta: str) -> pd.DataFrame:
    ext = os.path.splitext(ruta)[1].lower()
    if ext == ".csv":
        # Detecta separador automáticamente
        df = pd.read_csv(ruta, sep=None, engine="python")
    elif ext in (".xls", ".xlsx"):
        df = pd.read_excel(ruta)
    elif ext == ".json":
        df = pd.read_json(ruta)
    else:
        raise ValueError(f"Formato no soportado: {ext}. Usa CSV, Excel o JSON.")
    return df


def fig_a_bytes(fig) -> bytes:
    buf = io.BytesIO()
    fig.savefig(buf, format="png", dpi=150, bbox_inches="tight", facecolor=FONDO)
    plt.close(fig)
    buf.seek(0)
    return buf.read()


# ─────────────────────────────────────────────
# GENERACIÓN DE GRÁFICOS
# ─────────────────────────────────────────────
def grafico_nulos(df: pd.DataFrame) -> bytes:
    nulos = df.isnull().sum()
    nulos = nulos[nulos > 0].sort_values(ascending=False)
    if nulos.empty:
        return None

    fig, ax = plt.subplots(figsize=(10, max(3, len(nulos) * 0.45)), facecolor=FONDO)
    bars = ax.barh(nulos.index, nulos.values, color=PALETA)
    ax.bar_label(bars, fmt="%d", padding=4, fontsize=9)
    ax.set_title("Valores nulos por columna", fontsize=13, color=PALETA_SEC, pad=10)
    ax.set_xlabel("Cantidad de nulos")
    ax.invert_yaxis()
    fig.tight_layout()
    return fig_a_bytes(fig)


def grafico_tipos(df: pd.DataFrame) -> bytes:
    tipos = df.dtypes.astype(str).value_counts()
    fig, ax = plt.subplots(figsize=(6, 4), facecolor=FONDO)
    colores = plt.cm.Blues(np.linspace(0.4, 0.85, len(tipos)))
    wedges, texts, autotexts = ax.pie(
        tipos.values, labels=tipos.index, autopct="%1.1f%%",
        colors=colores, startangle=90, textprops={"fontsize": 10}
    )
    ax.set_title("Distribución de tipos de datos", fontsize=13, color=PALETA_SEC)
    return fig_a_bytes(fig)


def grafico_distribucion_numerica(serie: pd.Series) -> bytes:
    datos = serie.dropna()
    if len(datos) < 2:
        return None

    fig, axes = plt.subplots(1, 2, figsize=(10, 4), facecolor=FONDO)
    # Histograma + KDE
    axes[0].hist(datos, bins="auto", color=PALETA, edgecolor="white", alpha=0.85)
    axes[0].set_title(f"Histograma — {serie.name}", fontsize=11, color=PALETA_SEC)
    axes[0].set_ylabel("Frecuencia")
    # Boxplot
    axes[1].boxplot(datos, vert=True, patch_artist=True,
                    boxprops=dict(facecolor=PALETA, alpha=0.7),
                    medianprops=dict(color="white", linewidth=2))
    axes[1].set_title(f"Boxplot — {serie.name}", fontsize=11, color=PALETA_SEC)
    axes[1].set_xticklabels([serie.name])
    fig.tight_layout()
    return fig_a_bytes(fig)


def grafico_categorica(serie: pd.Series) -> bytes:
    conteo = serie.value_counts().head(MAX_CATS)
    fig, ax = plt.subplots(figsize=(10, max(3, len(conteo) * 0.45)), facecolor=FONDO)
    bars = ax.barh(conteo.index.astype(str), conteo.values, color=PALETA)
    ax.bar_label(bars, fmt="%d", padding=4, fontsize=9)
    ax.set_title(f"Top valores — {serie.name}", fontsize=11, color=PALETA_SEC)
    ax.invert_yaxis()
    fig.tight_layout()
    return fig_a_bytes(fig)


def grafico_correlacion(df: pd.DataFrame) -> bytes:
    numericas = df.select_dtypes(include="number")
    if numericas.shape[1] < 2:
        return None
    if numericas.shape[1] > MAX_COLS_CORR:
        numericas = numericas.iloc[:, :MAX_COLS_CORR]

    corr = numericas.corr()
    size = max(8, corr.shape[0] * 0.7)
    fig, ax = plt.subplots(figsize=(size, size * 0.85), facecolor=FONDO)
    sns.heatmap(corr, annot=True, fmt=".2f", cmap="Blues", ax=ax,
                linewidths=0.5, annot_kws={"size": 8},
                cbar_kws={"shrink": 0.8})
    ax.set_title("Matriz de correlación", fontsize=13, color=PALETA_SEC, pad=10)
    fig.tight_layout()
    return fig_a_bytes(fig)


# ─────────────────────────────────────────────
# PDF
# ─────────────────────────────────────────────
class PDF(FPDF):
    def __init__(self, titulo: str):
        super().__init__()
        self._titulo = titulo
        self.set_auto_page_break(auto=True, margin=18)
        self.add_page()
        self._pagina_portada()

    # ── Cabecera y pie ──────────────────────────
    def header(self):
        if self.page_no() == 1:
            return
        self.set_fill_color(30, 58, 95)
        self.rect(0, 0, 210, 12, "F")
        self.set_font("Helvetica", "B", 9)
        self.set_text_color(255, 255, 255)
        self.set_y(2)
        self.cell(0, 8, f"AppBusinessIntelligence — Informe: {self._titulo}", align="C")
        self.ln(8)

    def footer(self):
        if self.page_no() == 1:
            return
        self.set_y(-12)
        self.set_font("Helvetica", "", 8)
        self.set_text_color(130, 130, 130)
        self.cell(0, 10, f"Página {self.page_no()} — Generado el {datetime.now().strftime('%d/%m/%Y %H:%M')}", align="C")

    # ── Portada ─────────────────────────────────
    def _pagina_portada(self):
        # Fondo gradiente simulado con rectángulos
        for i, alpha in enumerate(range(60, 0, -3)):
            azul = int(30 + i * 2.5)
            self.set_fill_color(azul, min(azul + 40, 130), 200)
            self.rect(0, i * 3, 210, 3, "F")

        self.set_y(55)
        self.set_font("Helvetica", "B", 28)
        self.set_text_color(255, 255, 255)
        self.cell(0, 12, "AppBusinessIntelligence", align="C")
        self.ln(14)

        self.set_font("Helvetica", "", 16)
        self.cell(0, 10, "Informe de Análisis de Dataset", align="C")
        self.ln(10)

        # Tarjeta blanca central
        self.set_fill_color(255, 255, 255)
        self.set_draw_color(59, 130, 246)
        self.set_line_width(0.5)
        self.rect(30, 110, 150, 65, "FD")

        self.set_y(118)
        self.set_font("Helvetica", "B", 13)
        self.set_text_color(30, 58, 95)
        nombre_corto = self._titulo[:40] + ("…" if len(self._titulo) > 40 else "")
        self.cell(0, 9, f"Archivo: {nombre_corto}", align="C")
        self.ln(10)

        self.set_font("Helvetica", "", 11)
        self.set_text_color(80, 80, 80)
        self.cell(0, 8, f"Fecha: {datetime.now().strftime('%d de %B de %Y, %H:%M')}", align="C")
        self.ln(8)
        self.cell(0, 8, "Generado automáticamente por AppBusinessIntelligence", align="C")

    # ── Helpers de contenido ────────────────────
    def nueva_seccion(self, titulo: str):
        self.ln(4)
        self.set_fill_color(59, 130, 246)
        self.set_text_color(255, 255, 255)
        self.set_font("Helvetica", "B", 12)
        self.cell(0, 9, f"  {titulo}", fill=True, ln=True)
        self.ln(3)
        self.set_text_color(0, 0, 0)

    def subseccion(self, titulo: str):
        self.set_font("Helvetica", "B", 11)
        self.set_text_color(30, 58, 95)
        self.cell(0, 8, titulo, ln=True)
        self.set_draw_color(59, 130, 246)
        self.set_line_width(0.3)
        self.line(self.get_x(), self.get_y(), self.get_x() + 180, self.get_y())
        self.ln(3)
        self.set_text_color(0, 0, 0)

    def fila_tabla(self, etiqueta: str, valor: str, sombreado: bool = False):
        if sombreado:
            self.set_fill_color(235, 245, 255)
        else:
            self.set_fill_color(255, 255, 255)
        self.set_font("Helvetica", "B", 9)
        self.set_text_color(30, 58, 95)
        self.cell(65, 7, etiqueta, border=1, fill=True)
        self.set_font("Helvetica", "", 9)
        self.set_text_color(40, 40, 40)
        self.cell(115, 7, str(valor)[:80], border=1, fill=True, ln=True)

    def insertar_imagen(self, imagen_bytes: bytes, ancho: int = 180):
        tmp = f"_tmp_img_{id(imagen_bytes)}.png"
        with open(tmp, "wb") as f:
            f.write(imagen_bytes)
        self.image(tmp, x=(210 - ancho) / 2, w=ancho)
        os.remove(tmp)
        self.ln(4)

    def texto(self, contenido: str):
        self.set_font("Helvetica", "", 10)
        self.set_text_color(50, 50, 50)
        self.multi_cell(0, 6, contenido)
        self.ln(2)


# ─────────────────────────────────────────────
# ANÁLISIS
# ─────────────────────────────────────────────
def analizar_y_generar_pdf(ruta_archivo: str) -> str:
    print(f"[1/6] Cargando dataset: {ruta_archivo}")
    df = cargar_dataset(ruta_archivo)

    nombre_archivo = os.path.basename(ruta_archivo)
    nombre_base    = os.path.splitext(nombre_archivo)[0]
    ruta_pdf       = os.path.join(os.path.dirname(ruta_archivo), f"informe_{nombre_base}.pdf")

    print(f"[2/6] Dataset cargado — {df.shape[0]} filas × {df.shape[1]} columnas")

    cols_numericas    = df.select_dtypes(include="number").columns.tolist()
    cols_categoricas  = df.select_dtypes(include=["object", "category", "bool"]).columns.tolist()
    cols_fecha        = df.select_dtypes(include=["datetime"]).columns.tolist()

    # ── Crear PDF ──────────────────────────────
    pdf = PDF(nombre_archivo)
    pdf.add_page()

    # ── 1. Resumen general ─────────────────────
    pdf.nueva_seccion("1. Resumen General del Dataset")

    filas, columnas = df.shape
    tam_mb = os.path.getsize(ruta_archivo) / (1024 * 1024)
    nulos_total = df.isnull().sum().sum()
    pct_nulos   = round(nulos_total / (filas * columnas) * 100, 2)
    duplicados  = df.duplicated().sum()

    datos_resumen = [
        ("Archivo",               nombre_archivo),
        ("Filas",                 f"{filas:,}"),
        ("Columnas",              f"{columnas}"),
        ("Tamaño en disco",       f"{tam_mb:.3f} MB"),
        ("Valores nulos totales", f"{nulos_total:,} ({pct_nulos}%)"),
        ("Filas duplicadas",      f"{duplicados:,}"),
        ("Columnas numéricas",    f"{len(cols_numericas)}"),
        ("Columnas categóricas",  f"{len(cols_categoricas)}"),
        ("Columnas de fecha",     f"{len(cols_fecha)}"),
        ("Uso en memoria",        f"{df.memory_usage(deep=True).sum() / 1024**2:.2f} MB"),
    ]
    for i, (k, v) in enumerate(datos_resumen):
        pdf.fila_tabla(k, v, sombreado=(i % 2 == 0))

    # ── 2. Tipos y nulos ───────────────────────
    pdf.nueva_seccion("2. Tipos de Datos y Valores Nulos")
    print("[3/6] Generando gráficos de estructura...")

    img_tipos = grafico_tipos(df)
    pdf.insertar_imagen(img_tipos, ancho=120)

    img_nulos = grafico_nulos(df)
    if img_nulos:
        pdf.subseccion("Columnas con valores nulos")
        pdf.insertar_imagen(img_nulos)
    else:
        pdf.texto("✅ El dataset no contiene valores nulos.")

    # Tabla detallada de columnas
    pdf.subseccion("Detalle por columna")
    pdf.set_font("Helvetica", "B", 9)
    pdf.set_fill_color(30, 58, 95)
    pdf.set_text_color(255, 255, 255)
    for cabecera, ancho in [("Columna", 55), ("Tipo", 30), ("Nulos", 22), ("% Nulos", 22), ("Únicos", 22), ("Ejemplo", 39)]:
        pdf.cell(ancho, 7, cabecera, border=1, fill=True)
    pdf.ln()

    for i, col in enumerate(df.columns):
        fill = i % 2 == 0
        if fill:
            pdf.set_fill_color(235, 245, 255)
        else:
            pdf.set_fill_color(255, 255, 255)
        pdf.set_text_color(40, 40, 40)
        pdf.set_font("Helvetica", "", 8)

        nulos_col = df[col].isnull().sum()
        pct_col   = round(nulos_col / filas * 100, 1)
        unicos    = df[col].nunique()
        ejemplo   = str(df[col].dropna().iloc[0]) if df[col].dropna().shape[0] > 0 else "—"

        for valor, ancho in [
            (str(col)[:25], 55),
            (str(df[col].dtype), 30),
            (str(nulos_col), 22),
            (f"{pct_col}%", 22),
            (str(unicos), 22),
            (ejemplo[:22], 39),
        ]:
            pdf.cell(ancho, 6, valor, border=1, fill=fill)
        pdf.ln()

    # ── 3. Análisis estadístico numérico ───────
    if cols_numericas:
        pdf.nueva_seccion("3. Estadísticas Descriptivas — Variables Numéricas")
        print("[4/6] Analizando variables numéricas...")

        desc = df[cols_numericas].describe(percentiles=[0.05, 0.25, 0.5, 0.75, 0.95]).T
        desc["skewness"] = df[cols_numericas].skew()
        desc["kurtosis"] = df[cols_numericas].kurt()

        # Cabecera
        pdf.set_font("Helvetica", "B", 8)
        pdf.set_fill_color(30, 58, 95)
        pdf.set_text_color(255, 255, 255)
        headers = ["Columna", "Media", "Std", "Min", "p25", "p50", "p75", "Max", "Skew", "Kurt"]
        anchos  = [45,        22,      22,    18,    18,    18,    18,    18,    18,    15]
        for h, a in zip(headers, anchos):
            pdf.cell(a, 7, h, border=1, fill=True)
        pdf.ln()

        for i, col in enumerate(desc.index):
            fill = i % 2 == 0
            color = (235, 245, 255) if fill else (255, 255, 255)
            pdf.set_fill_color(*color)
            pdf.set_text_color(40, 40, 40)
            pdf.set_font("Helvetica", "", 8)
            fila = [
                str(col)[:22],
                f"{desc.loc[col,'mean']:.3g}",
                f"{desc.loc[col,'std']:.3g}",
                f"{desc.loc[col,'min']:.3g}",
                f"{desc.loc[col,'25%']:.3g}",
                f"{desc.loc[col,'50%']:.3g}",
                f"{desc.loc[col,'75%']:.3g}",
                f"{desc.loc[col,'max']:.3g}",
                f"{desc.loc[col,'skewness']:.2f}",
                f"{desc.loc[col,'kurtosis']:.2f}",
            ]
            for v, a in zip(fila, anchos):
                pdf.cell(a, 6, v, border=1, fill=fill)
            pdf.ln()

        # Gráficos individuales (máx 10 columnas)
        pdf.subseccion("Distribuciones y outliers")
        for col in cols_numericas[:10]:
            img = grafico_distribucion_numerica(df[col])
            if img:
                pdf.insertar_imagen(img)

        # Correlación
        img_corr = grafico_correlacion(df)
        if img_corr:
            pdf.nueva_seccion("4. Matriz de Correlación")
            pdf.insertar_imagen(img_corr, ancho=190)

            # Pares con alta correlación
            corr_m = df[cols_numericas].corr()
            pares_altos = []
            for i in range(len(corr_m.columns)):
                for j in range(i + 1, len(corr_m.columns)):
                    v = corr_m.iloc[i, j]
                    if abs(v) >= 0.7:
                        pares_altos.append((corr_m.columns[i], corr_m.columns[j], round(v, 3)))

            if pares_altos:
                pdf.subseccion("Pares con correlación alta (|r| ≥ 0.7)")
                for a, b, r in sorted(pares_altos, key=lambda x: -abs(x[2])):
                    pdf.texto(f"  • {a}  ↔  {b}:  r = {r}")

    # ── 4. Análisis categórico ─────────────────
    if cols_categoricas:
        pdf.nueva_seccion("5. Análisis de Variables Categóricas")
        print("[5/6] Analizando variables categóricas...")

        for col in cols_categoricas[:8]:
            pdf.subseccion(f"Columna: {col}")
            vc = df[col].value_counts()
            pdf.texto(
                f"Valores únicos: {df[col].nunique()}   |   "
                f"Moda: '{vc.index[0]}' ({vc.iloc[0]:,} veces, "
                f"{round(vc.iloc[0]/filas*100,1)}%)   |   "
                f"Nulos: {df[col].isnull().sum()}"
            )
            img_cat = grafico_categorica(df[col])
            if img_cat:
                pdf.insertar_imagen(img_cat)

    # ── 5. Conclusiones automáticas ────────────
    pdf.nueva_seccion("6. Conclusiones y Recomendaciones Automáticas")
    print("[6/6] Generando conclusiones...")

    conclusiones = []

    if pct_nulos == 0:
        conclusiones.append("✅ El dataset está completo: no se detectaron valores nulos.")
    elif pct_nulos < 5:
        conclusiones.append(f"⚠️  Hay un {pct_nulos}% de valores nulos. Se recomienda imputación o eliminación de filas/columnas afectadas.")
    else:
        conclusiones.append(f"🔴 El {pct_nulos}% de los valores son nulos. Revisar la calidad del origen de datos.")

    if duplicados > 0:
        conclusiones.append(f"⚠️  Se encontraron {duplicados:,} filas duplicadas. Considerar eliminarlas antes del análisis.")
    else:
        conclusiones.append("✅ No se detectaron filas duplicadas.")

    if cols_numericas:
        sesgadas = [c for c in cols_numericas if abs(df[c].skew()) > 1]
        if sesgadas:
            conclusiones.append(f"📊 Columnas con distribución muy sesgada (|skew| > 1): {', '.join(sesgadas[:5])}. Considera aplicar transformación logarítmica.")

    if pares_altos if 'pares_altos' in dir() else False:
        conclusiones.append(f"🔗 Se detectaron {len(pares_altos)} pares de variables altamente correlacionadas. Posible multicolinealidad.")

    if filas < 100:
        conclusiones.append("⚠️  El dataset tiene menos de 100 filas. Los resultados estadísticos pueden no ser representativos.")
    elif filas > 100_000:
        conclusiones.append(f"✅ Dataset grande ({filas:,} filas). Buen volumen para modelos de machine learning.")

    for c in conclusiones:
        pdf.texto(c)

    # ── Guardar ────────────────────────────────
    pdf.output(ruta_pdf)
    print(f"\n✅ Informe generado correctamente:\n   {ruta_pdf}")
    return ruta_pdf


# ─────────────────────────────────────────────
# PUNTO DE ENTRADA
# ─────────────────────────────────────────────
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Uso: python analizar_dataset.py <ruta_del_archivo>")
        print("Formatos soportados: .csv, .xlsx, .xls, .json")
        sys.exit(1)

    ruta = sys.argv[1]
    if not os.path.exists(ruta):
        print(f"Error: No se encontró el archivo '{ruta}'")
        sys.exit(1)

    analizar_y_generar_pdf(ruta)
