# Herramientas de ShareX 22.4.0 conservadas en shareW

Inventario obtenido de `ToolbarCustomizationItemViewModel.CreateToolDefinition` y el filtro original de `RegionCaptureToolbar.CreateItems`. Incluye seleccionar como herramienta. Las 19 primeras se ofrecen durante la captura; recortar y cortar se ofrecen en el editor. Todas están pendientes de validación de funcionamiento en Windows.

| Identificador original | Herramienta | Durante la captura | En el editor |
| --- | --- | --- | --- |
| Select | Seleccionar y manipular objetos | Sí | Sí |
| Rectangle | Rectángulo | Sí | Sí |
| Ellipse | Elipse | Sí | Sí |
| Line | Línea | Sí | Sí |
| Arrow | Flecha | Sí | Sí |
| Freehand | Dibujo a mano alzada | Sí | Sí |
| Text | Texto | Sí | Sí |
| SpeechBalloon | Bocadillo de diálogo | Sí | Sí |
| Step | Numeración de pasos | Sí | Sí |
| Image | Insertar imagen local | Sí | Sí |
| Clone | Clonar | Sí | Sí |
| Emoji | Insertar emoji | Sí | Sí |
| Cursor | Insertar cursor | Sí | Sí |
| Highlight | Resaltar | Sí | Sí |
| SmartEraser | Borrador inteligente | Sí | Sí |
| Blur | Desenfocar | Sí | Sí |
| Pixelate | Pixelar | Sí | Sí |
| Magnify | Ampliar una zona | Sí | Sí |
| Spotlight | Destacar una zona | Sí | Sí |
| Crop | Recortar imagen | No, como en 22.4.0 | Sí |
| CutOut | Cortar una franja de la imagen | No, como en 22.4.0 | Sí |

También se conservan las opciones de las herramientas, su personalización y atajos, la selección de región, la lupa y los controles originales de anotación. Las opciones de fondo, tamaño del lienzo y efectos del editor no forman parte de estos 21 identificadores, pero sus implementaciones locales están incluidas.

No hay botón, callback ni comando de subida en el editor adaptado. La importación por URL se ha retirado. No se incluye la grabación de vídeo ni el conjunto de herramientas generales de la aplicación ShareX.
