import torch
import numpy as np
import time
import os
from models.model import PoseTransformer3D
from models.dataset import normalize_skeleton, align_pose_down, rotation_matrix_z
import json

LABEL_MAP = {
    'flessione_indietro': 0,
    'flessione_avanti': 1,
    'estensione_gamba': 2,
    'squat': 3,
}

SEQ_LEN = 50
INPUT_FEATURE_SIZE = 5 * 3  
# Ottieni la directory dove si trova questo script
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

# Percorsi assoluti
MODEL_PATH = os.path.join(BASE_DIR, "model_weights_final_v2.pt")
WATCH_DIR = os.path.abspath(os.path.join(BASE_DIR, "../incoming_data"))
PREDICTION_DIR = os.path.abspath(os.path.join(BASE_DIR, "../prediction"))
PREDICTION_JSON = os.path.join(PREDICTION_DIR, "prediction.json")


model = PoseTransformer3D(input_size=INPUT_FEATURE_SIZE, num_classes=len(LABEL_MAP))
model.load_state_dict(torch.load(MODEL_PATH, map_location='cpu'))
model.eval()


def calculate_angle(a, b, c):
    v1 = a - b
    v2 = c - b
    cos_theta = np.dot(v1, v2) / (np.linalg.norm(v1) * np.linalg.norm(v2) + 1e-8)
    cos_theta = np.clip(cos_theta, -1.0, 1.0)
    return np.degrees(np.arccos(cos_theta))

def extract_movement_sequence(data):
    """
    Estrae la sequenza di movimento più significativa da un array (N, 5, 3),
    trovando la finestra dove l’angolo del ginocchio varia di più.
    Stampa il primo e l’ultimo angolo della sequenza estratta.
    """
    hip_idx, knee_idx, ankle_idx = 0, 1, 2  # Sempre i primi tre landmark

    angles = np.array([calculate_angle(f[hip_idx], f[knee_idx], f[ankle_idx]) for f in data])

    # Calcola la differenza assoluta tra frame consecutivi
    diffs = np.abs(np.diff(angles))

    # Trova il punto di variazione massima
    if len(diffs) == 0:
        print(f"Primo angolo: {angles[0]:.2f}°, Ultimo angolo: {angles[-1]:.2f}°")
        return data

    max_var_idx = np.argmax(diffs)
    window_size = min(SEQ_LEN, len(data))

    # Centra la finestra sulla variazione massima
    start_idx = max(0, max_var_idx - window_size // 2)
    end_idx = min(len(data), start_idx + window_size)

    movement = data[start_idx:end_idx]
    movement_angles = angles[start_idx:end_idx]
    if len(movement_angles) > 0:
        print(f"Primo angolo: {movement_angles[0]:.2f}°, Ultimo angolo: {movement_angles[-1]:.2f}°")
    else:
        print("Nessun angolo estratto.")
    return movement

def preprocess(data):
    if data.shape[0] > SEQ_LEN:
        idx = np.linspace(0, data.shape[0] - 1, SEQ_LEN).astype(int)
        data = data[idx]
    # Se è più corta, pad con zeri
    elif data.shape[0] < SEQ_LEN:
        pad = np.zeros((SEQ_LEN - data.shape[0], *data.shape[1:]))
        data = np.concatenate([data, pad], axis=0)
    else:
        print(f"✅ Sequenza già di lunghezza {SEQ_LEN}")
     
    #data = align_pose_down(data)
    data = normalize_skeleton(data)

    mean = data.mean(axis=(0, 1), keepdims=True)
    std = data.std(axis=(0, 1), keepdims=True) + 1e-6
    data = (data - mean) / std
    
    
    return data.reshape(SEQ_LEN, -1).astype(np.float32)

def get_feedback(class_name, frame):
    target_angles = {
        'flessione_indietro': 45.0,
        'flessione_avanti': 60.0,
        'estensione_gamba': 170.0,
        'squat': 90.0
    }

    hip, knee, ankle = frame[0], frame[1], frame[2]


    angle = calculate_angle(hip, knee, ankle)
    target = target_angles.get(class_name, 90)
    diff = target - angle

    if diff > 1:
        return f"Ti mancano {diff:.1f}° di {class_name.replace('_', ' ')}"
    return "Movimento corretto!"


print("✅ Model server running. Watching for .npy files...")


files = [f for f in os.listdir(WATCH_DIR) if f.endswith(".npy")]
for fname in files:
    fpath = os.path.join(WATCH_DIR, fname)
    try:
        data_raw = np.load(fpath)
        if data_raw.shape[0] < 5:
            raise ValueError("Not enough frames.")

        # Preprocessing per il modello
        input_data = preprocess(data_raw)
        input_tensor = torch.from_numpy(np.expand_dims(input_data, axis=0))
        # Prediction
        with torch.no_grad():
            output = model(input_tensor)
            pred = torch.argmax(output, dim=1).item()
            class_name = [k for k, v in LABEL_MAP.items() if v == pred][0]
            print(f"[{fname}] → Predicted: {class_name}")

        # Feedback angolare
        angles = [calculate_angle(f[0], f[1], f[2]) for f in data_raw]
        if class_name.startswith("estensione"):
            idx = int(np.argmax(angles))
        else:
            idx = int(np.argmin(angles))
        eval_frame = data_raw[idx]
        eval_angle = angles[idx]
        feedback = get_feedback(class_name, eval_frame)
        if feedback:
            print(f"📐 Feedback: {feedback} (Angolo valutato: {eval_angle:.2f}°)")

        # --- AGGIUNTA: scrivi file JSON ---
        # Estrai il lato dal nome file (es: "right" o "left" nel nome)
        if "right" in fname.lower():
            gamba = "dx"
        elif "left" in fname.lower():
            gamba = "sx"
        else:
            gamba = "?"

        output_json = {
            "predizione": class_name,
            "angolo": float(eval_angle),
            "gamba": gamba
        }
        with open(PREDICTION_JSON, "w") as f:
            json.dump(output_json, f, indent=4)
        # --- FINE AGGIUNTA ---

    except Exception as e:
        print(f"❌ Error processing {fname}: {e}")
    finally:
        os.remove(fpath)

