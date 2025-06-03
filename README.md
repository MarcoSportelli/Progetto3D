# Progetto 3D - Rilevamento Pose e Classificazione

Questo progetto permette di:
- Estrarre landmark 3D da video RealSense (`.bag`) usando MediaPipe
- Salvare i landmark in file `.npy`
- Classificare le sequenze di pose tramite un modello deep learning (PoseTransformer3D)
- Automatizzare la classificazione tramite un server che monitora una cartella di input

---

## Struttura del progetto

- `landmark_extraction/`  
  Script per estrarre landmark da file `.bag` (usa MediaPipe e RealSense)
- `PoseDetector/`  
  Codice per training, validazione e serving del modello di classificazione
- `requirements.txt`  
  Tutte le dipendenze Python necessarie
- `.gitignore`  
  File e cartelle ignorate da git

---

## Setup ambiente

1. **Clona il repository**
2. **Crea un ambiente virtuale nella root del progetto:**
   ```sh
   cd landmark_extraction
   python -m venv mp_env
   mp_env\Scripts\activate
   pip install mediapipe opencv-python pyrealsense2
   ```
3. **Installa tutte le dipendenze:**
   ```sh
   pip install -r requirements.txt
   ```

---

## Uso: Estrazione Landmark

1. Posiziona il file `.bag` nella cartella desiderata.
2. Modifica i percorsi in `landmark_extraction/extract_landmarks.py`:
   ```python
   bag_path = r"percorso\al\tuo\file.bag"
   output_path = r"percorso\output.npy"
   leg = "left"  # oppure "right"
   ```
3. Esegui lo script:
   ```sh
   python landmark_extraction/extract_landmarks.py
   ```
   Verrà generato un file `.npy` con i landmark della gamba selezionata.

---

## Uso: Training e Server

1. Prepara i dati nella cartella `data/` seguendo la struttura prevista.
2. Esegui il training:
   ```sh
   python PoseDetector/main.py
   ```
3. Avvia il server per la classificazione automatica:
   ```sh
   python PoseDetector/server.py
   ```
   Il server monitorerà la cartella `incoming_data/` e classificherà ogni nuovo file `.npy`.

---

## Problemi comuni con MediaPipe su Windows

### Errore: `ImportError: DLL load failed while importing _framework_bindings`
- **Soluzione:**  
  - Assicurati di usare **Python 64 bit** (non 32 bit).
  - Usa un ambiente virtuale creato con `venv`, **non conda**.
  - Aggiorna pip:  
    `python -m pip install --upgrade pip setuptools wheel`
  - Prova a installare una versione precedente di mediapipe:  
    `pip install mediapipe==0.10.9`
  - Riavvia il PC dopo l’installazione.

### Errore: `ModuleNotFoundError: No module named 'pyrealsense2'`
- **Soluzione:**  
  - Installa il pacchetto con:  
    `pip install pyrealsense2`
  - Assicurati che l’ambiente virtuale sia attivo.

### Altri consigli
- Se usi VS Code, seleziona l’interprete Python giusto (`mp_env`) in basso a destra.
- Se hai più ambienti virtuali, attiva sempre quello corretto prima di lanciare gli script.

---

## Contatti

Per problemi o domande, apri una issue o contatta il maintainer.