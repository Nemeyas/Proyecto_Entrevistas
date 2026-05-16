from database import db
import json

historial = db.get_historial()
print("--- HISTORIAL ACTUAL EN BD ---")
for h in historial:
    print(f"ID: {h['IDSimulacion']} - {h['NombrePostulante']} - Puntaje: {h['PuntajeGlobal']}")
print("-------------------------------")
