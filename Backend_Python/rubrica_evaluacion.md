# Rúbrica de Evaluación - Simulador de Entrevistas

## Puntaje Total: 100 puntos

El puntaje final del candidato se calcula de forma **híbrida**: una parte es matemática
(calculada por Python sin intervención de la IA) y otra es evaluada por Gemini siguiendo
métricas estrictas. Esto garantiza objetividad y consistencia entre evaluaciones.

---

## Pilar 1: Estabilidad Emocional (30 puntos) — Cálculo Automático por Python

Se parte de una base perfecta de **30 puntos**, asumiendo que el candidato se mantiene
emocionalmente estable durante toda la entrevista. Python revisará matemáticamente el
registro de emociones capturadas por la cámara y aplicará penalizaciones fijas.

| Concepto                  | Puntos   |
|---------------------------|----------|
| Base inicial              | 30 pts   |
| Penalización por Momento Crítico | **-5 pts** por cada uno |
| Puntaje mínimo posible    | 0 pts    |

### ¿Qué es un "Momento Crítico"?
Un momento crítico se registra automáticamente cuando el sistema detecta que el candidato
muestra **miedo (fear)** o **tristeza (sad)** durante una pregunta de la entrevista.
Cada momento crítico registrado resta 5 puntos de este pilar.

### Ejemplos:
- **0 momentos críticos:** 30/30 (candidato tranquilo toda la entrevista)
- **2 momentos críticos:** 20/30 (nerviosismo en 2 preguntas)
- **4 momentos críticos:** 10/30 (alta ansiedad sostenida)
- **6 o más momentos críticos:** 0/30 (puntaje mínimo emocional)

---

## Pilar 2: Calidad de Comunicación (70 puntos) — Evaluado por Gemini con Rúbrica Estricta

Gemini evalúa la transcripción completa del candidato y asigna un puntaje numérico
a cada una de las 3 métricas siguientes. **No puede inventar criterios nuevos ni
dar puntajes fuera de los rangos definidos.**

### Métrica A: Claridad y Coherencia (0 a 25 puntos)
¿El candidato respondió de forma directa, clara y comprensible?

| Rango       | Descripción                                                      |
|-------------|------------------------------------------------------------------|
| 21 - 25 pts | Respuestas claras, concisas y perfectamente alineadas al tema    |
| 16 - 20 pts | Generalmente claro, con desviaciones menores                     |
| 11 - 15 pts | Algunas respuestas confusas o fuera de tema                      |
| 6 - 10 pts  | Frecuentemente incoherente o divagante                           |
| 0 - 5 pts   | Respuestas incomprensibles o completamente fuera de contexto     |

### Métrica B: Competencia y Resolución (0 a 25 puntos)
¿Las respuestas demuestran conocimiento técnico o habilidades de resolución de problemas?

| Rango       | Descripción                                                      |
|-------------|------------------------------------------------------------------|
| 21 - 25 pts | Demuestra dominio sólido y capacidad analítica destacada         |
| 16 - 20 pts | Buen nivel de conocimiento con áreas menores por mejorar         |
| 11 - 15 pts | Conocimiento básico, respuestas superficiales                    |
| 6 - 10 pts  | Conocimiento insuficiente, respuestas vagas                      |
| 0 - 5 pts   | No demuestra competencia alguna en los temas evaluados           |

### Métrica C: Profesionalismo (0 a 20 puntos)
¿El candidato mantuvo un vocabulario adecuado y actitud profesional?

| Rango       | Descripción                                                      |
|-------------|------------------------------------------------------------------|
| 17 - 20 pts | Vocabulario excelente, tono profesional impecable                |
| 13 - 16 pts | Profesional en general, deslices menores                         |
| 9 - 12 pts  | Tono informal o actitud pasiva en varias respuestas              |
| 5 - 8 pts   | Vocabulario inapropiado o actitud negativa frecuente             |
| 0 - 4 pts   | Comportamiento no profesional durante la entrevista              |

---

## Fórmula Final

```
Puntaje Final = Pilar 1 (Estabilidad Emocional) + Pilar 2 (Calidad de Comunicación)
Puntaje Final = max(0, 30 - (momentos_criticos * 5)) + claridad + competencia + profesionalismo
```

### Escala de Interpretación del Puntaje Final

| Rango       | Evaluación     |
|-------------|----------------|
| 90 - 100    | Excelente      |
| 75 - 89     | Bueno          |
| 60 - 74     | Aceptable      |
| 40 - 59     | Necesita Mejorar |
| 0 - 39      | Deficiente     |
