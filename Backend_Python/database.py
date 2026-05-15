import os
import mysql.connector
from datetime import datetime

class Database:
    def __init__(self):
        # Configurar en el .env las credenciales
        self.host = os.getenv("MYSQL_HOST", "localhost")
        self.user = os.getenv("MYSQL_USER", "root")
        self.password = os.getenv("MYSQL_PASSWORD", "")
        self.database = os.getenv("MYSQL_DB", "entrevistas_db")

    def connect(self):
        """Crea y retorna la conexion a la BD. Crea la base de datos si no existe."""
        conn = mysql.connector.connect(
            host=self.host,
            user=self.user,
            password=self.password
        )
        cursor = conn.cursor()
        cursor.execute(f"CREATE DATABASE IF NOT EXISTS {self.database}")
        cursor.close()
        conn.close()

        return mysql.connector.connect(
            host=self.host,
            user=self.user,
            password=self.password,
            database=self.database
        )

    def init_db(self):
        """Crea las tablas según el modelo ER si no existen."""
        conn = self.connect()
        cursor = conn.cursor()

        # Tabla Postulante
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS Postulante (
            IDPostulante VARCHAR(12) PRIMARY KEY,
            NombrePostulante VARCHAR(50)
        )
        ''')

        # Tabla Simulacion
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS Simulacion (
            IDSimulacion INT AUTO_INCREMENT PRIMARY KEY,
            IDPostulante VARCHAR(12),
            TiempoInicio DATETIME,
            TiempoFin DATETIME,
            Dificultad VARCHAR(20),
            FOREIGN KEY (IDPostulante) REFERENCES Postulante(IDPostulante)
        )
        ''')

        # Tabla Turno
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS Turno (
            IDTurno INT AUTO_INCREMENT PRIMARY KEY,
            IDSimulacion INT,
            NumTurno INT,
            RespuestaIA VARCHAR(3000),
            TextoUsuario VARCHAR(3000),
            EmocionDominanteVoz VARCHAR(50),
            EmocionDominanteCamara VARCHAR(50),
            FOREIGN KEY (IDSimulacion) REFERENCES Simulacion(IDSimulacion)
        )
        ''')

        # Tabla Reporte
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS Reporte (
            IDReporte INT AUTO_INCREMENT PRIMARY KEY,
            IDSimulacion INT,
            PuntajeGlobal FLOAT,
            Resumen VARCHAR(3000),
            FOREIGN KEY (IDSimulacion) REFERENCES Simulacion(IDSimulacion)
        )
        ''')

        conn.commit()
        cursor.close()
        conn.close()

    def create_postulante(self, rut, nombre):
        conn = self.connect()
        cursor = conn.cursor()
        # INSERT IGNORE por si ya existe el postulante
        cursor.execute('INSERT IGNORE INTO Postulante (IDPostulante, NombrePostulante) VALUES (%s, %s)', (rut, nombre))
        conn.commit()
        cursor.close()
        conn.close()

    def start_simulacion(self, id_postulante, dificultad):
        conn = self.connect()
        cursor = conn.cursor()
        tiempo_inicio = datetime.now()
        cursor.execute(
            'INSERT INTO Simulacion (IDPostulante, TiempoInicio, Dificultad) VALUES (%s, %s, %s)',
            (id_postulante, tiempo_inicio, dificultad)
        )
        conn.commit()
        id_simulacion = cursor.lastrowid
        cursor.close()
        conn.close()
        return id_simulacion

    def finish_simulacion(self, id_simulacion):
        conn = self.connect()
        cursor = conn.cursor()
        tiempo_fin = datetime.now()
        cursor.execute(
            'UPDATE Simulacion SET TiempoFin = %s WHERE IDSimulacion = %s',
            (tiempo_fin, id_simulacion)
        )
        conn.commit()
        cursor.close()
        conn.close()

    def insert_turno(self, id_simulacion, num_turno, respuesta_ia, texto_usuario, emocion_voz, emocion_camara):
        conn = self.connect()
        cursor = conn.cursor()
        cursor.execute(
            '''INSERT INTO Turno (IDSimulacion, NumTurno, RespuestaIA, TextoUsuario, EmocionDominanteVoz, EmocionDominanteCamara)
               VALUES (%s, %s, %s, %s, %s, %s)''',
            (id_simulacion, num_turno, respuesta_ia, texto_usuario, emocion_voz, emocion_camara)
        )
        conn.commit()
        cursor.close()
        conn.close()

    def insert_reporte(self, id_simulacion, puntaje_global, resumen):
        conn = self.connect()
        cursor = conn.cursor()
        cursor.execute(
            'INSERT INTO Reporte (IDSimulacion, PuntajeGlobal, Resumen) VALUES (%s, %s, %s)',
            (id_simulacion, puntaje_global, resumen)
        )
        conn.commit()
        cursor.close()
        conn.close()

    def get_historial(self):
        conn = self.connect()
        cursor = conn.cursor(dictionary=True)
        cursor.execute('''
            SELECT S.IDSimulacion, P.NombrePostulante, S.TiempoInicio, S.Dificultad, R.PuntajeGlobal, R.Resumen
            FROM Simulacion S
            JOIN Postulante P ON S.IDPostulante = P.IDPostulante
            LEFT JOIN Reporte R ON S.IDSimulacion = R.IDSimulacion
            ORDER BY S.TiempoInicio DESC
        ''')
        resultados = cursor.fetchall()
        cursor.close()
        conn.close()
        return resultados

    def get_reporte(self, id_simulacion):
        conn = self.connect()
        cursor = conn.cursor(dictionary=True)
        cursor.execute('''
            SELECT S.IDSimulacion, P.NombrePostulante, P.IDPostulante, S.Dificultad, R.PuntajeGlobal, R.Resumen
            FROM Simulacion S
            JOIN Postulante P ON S.IDPostulante = P.IDPostulante
            JOIN Reporte R ON S.IDSimulacion = R.IDSimulacion
            WHERE S.IDSimulacion = %s
        ''', (id_simulacion,))
        resultado = cursor.fetchone()
        cursor.close()
        conn.close()
        return resultado

db = Database()
