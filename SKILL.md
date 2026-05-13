# Project Skills

## Advanced PDF OCR & Vision
Cuando se active esta skill para analizar documentos (especialmente de baja calidad o con diagramas), ejecuta un entorno de Python con las siguientes directrices:

1. **Ingesta**: Convierte cada página del PDF a imagen de alta resolución (300+ DPI) usando `pdf2image`.
2. **Preprocesamiento con OpenCV (`cv2`)**:
    - **Grayscale**: Convertir a escala de grises.
    - **Noise Reduction**: Usar `fastNlMeansDenoising`.
    - **Thresholding**: Aplicar `cv2.adaptiveThreshold` con `GAUSSIAN_C` para manejar iluminación desigual.
    - **Morphology**: Operaciones de `OPEN` y `CLOSE` para eliminar motas de polvo en escaneos viejos.
3. **Extracción de Texto**:
    - Usar `pytesseract` con la configuración `--oem 3 --psm 6` (para bloques de texto uniformes).
    - En caso de tablas, alternar a `--psm 11`.
4. **Interpretación Visual**:
    - Detectar contornos de diagramas y extraerlos como sub-imágenes para análisis multimodal.
5. **Corrección de Errores**:
    - Aplicar un modelo de lenguaje (LLM) para post-procesar el texto extraído y corregir "typos" lógicos basados en el contexto técnico del proyecto.