# 🦿 Progetto 3D – Rilevamento Pose e Classificazione

Sistema completo per l’acquisizione, l’elaborazione e la classificazione di movimenti riabilitativi tramite camera RealSense, **MediaPipe**, **Unity 3D** e un modello **Transformer-based**.

---

## 🚀 Funzionalità principali

- ✅ Estrazione dei landmark 3D da video `.bag` (Intel RealSense) usando **MediaPipe**
- ✅ Salvataggio dei dati in formato `.npy` per uso automatico
- ✅ Classificazione dei movimenti tramite rete neurale **PoseTransformer3D**
- ✅ Automazione del flusso tramite **server locale** che monitora una cartella di input
- ✅ Interfaccia sviluppata in **Unity 3D** per interazione utente e feedback visivo

---

## 🧠 Panoramica del sistema

Il sistema guida l’utente attraverso un flusso semplice e automatico:

1. **Avvio dell’interfaccia Unity**  
   L’utente può scegliere tra due opzioni: avviare una **nuova registrazione** o consultare lo **storico delle acquisizioni precedenti**.

2. **Registrazione del movimento**  
   Viene mostrato un countdown che permette al paziente di posizionarsi correttamente (vista laterale, arto interamente visibile).  
   La registrazione dura circa **7 secondi**, durante i quali l’utente riceve **feedback visivo in tempo reale**.

3. **Elaborazione e classificazione**  
   Il video viene salvato in `.bag` e inviato a un **back-end locale**, che:
   - Estrae i **landmark 3D** con MediaPipe
   - Classifica il movimento con un modello Transformer
   - Calcola gli **angoli articolari** frame per frame

4. **Output**  
   L’utente riceve:
   - Il tipo di gesto riconosciuto
   - L’angolo massimo rilevato
   - Un **feedback personalizzato**
   - Una rappresentazione 3D animata del movimento corretto

5. **Storico**  
   Ogni esecuzione viene registrata con **data, ora e feedback**, consultabile dallo stesso utente.

---

## 📁 Struttura del progetto

```

📦 Progetto 3D
├── App/ → Interfaccia grafica realizzata in Unity 3D
├── landmark_extraction/ → Script per estrazione landmark da video .bag
├── PoseDetector/ → Modello, training, server di classificazione
├── prediction/ → Output generati dal server (classificazioni, angoli, feedback)
├── storico/ → Archivio delle acquisizioni utente (video + dati)
├── requirements.txt → Dipendenze Python
└── .gitignore → File ignorati da Git

````

---

## ⚙️ Setup ambiente

### 1. Clona il repository

```bash
git clone https://github.com/tuo-utente/progetto-riabilitazione-3d.git
````

### 2. Crea un ambiente virtuale

```bash
cd landmark_extraction
python -m venv mp_env
mp_env\Scripts\activate        # Su Windows
```

### 3. Installa le dipendenze principali

```bash
pip install mediapipe opencv-python pyrealsense2
pip install -r ../requirements.txt
```
---

## 🧪 Uso – Training e Classificazione

### 1. Prepara i dati in `data/`

Assicurati che la struttura e le etichette siano coerenti con quelle usate nel training.

### 2. Avvia il training del modello

```bash
python PoseDetector/main.py
```

### 3. Avvia il server per la classificazione automatica

```bash
python PoseDetector/server.py
```

📂 Il server monitorerà la cartella `incoming_data/` e classificherà automaticamente ogni nuovo file `.npy` inserito.

---

## ❗ Problemi comuni (MediaPipe su Windows)

### Errore: `ImportError: DLL load failed while importing _framework_bindings`

**Soluzioni:**

* Usa **Python 64-bit**
* Crea l’ambiente virtuale con `venv` (non `conda`)
* Aggiorna pip e setuptools:

  ```bash
  python -m pip install --upgrade pip setuptools wheel
  ```
* Installa una versione stabile:

  ```bash
  pip install mediapipe==0.10.9
  ```
* Riavvia il PC dopo l’installazione

### Errore: `ModuleNotFoundError: No module named 'pyrealsense2'`

**Soluzioni:**

* Installa con:

  ```bash
  pip install pyrealsense2
  ```
* Verifica che l’ambiente virtuale sia attivo

---

## 💡 Suggerimenti

* Se usi **VS Code**, seleziona l’interprete Python corretto (`mp_env`) in basso a destra
* Attiva sempre l’ambiente virtuale prima di lanciare gli script
* Se lavori in team, puoi creare un file `dev-requirements.txt` separato per gli strumenti di sviluppo

---

## 🧾 Crediti

* Interfaccia utente sviluppata in **Unity 3D**
* Modelli 3D animati generati con **Mixamo** e perfezionati in **Blender**
* Modello di classificazione basato su architettura **Transformer**

---
