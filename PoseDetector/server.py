import torch
import numpy as np
import time
import os
from models.model import PoseTransformer3D
from models.dataset import normalize_skeleton
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
MODEL_PATH = os.path.join(BASE_DIR, "model_weights_final_v3.pt")
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
     
    data = normalize_skeleton(data)

    mean = data.mean(axis=(0, 1), keepdims=True)
    std = data.std(axis=(0, 1), keepdims=True) + 1e-6
    data = (data - mean) / std
    
    
    return data.reshape(SEQ_LEN, -1).astype(np.float32)

def get_feedback(class_name, angle):
    # Physiological ranges (min, max)
    target_ranges = {
        'flessione_indietro': (135, 145),
        'flessione_avanti': (110, 140),
        'estensione_gamba': (160, 180),
        'squat': (90, 135)
    }
    min_target, max_target = target_ranges.get(class_name, (0, 180))
    if angle < min_target:
        missing = min_target - angle
        return f"Angle too small: {angle:.1f}° (Physiological range: {min_target}-{max_target}°) | Missing angle: {missing:.1f}°"
    elif angle > max_target:
        exceeding = angle - max_target
        return f"ERROR: Angle too large: {angle:.1f}° (Physiological range: {min_target}-{max_target}°) | Exceeding angle: {exceeding:.1f}°"
    else:
        return f"Correct movement! ({angle:.1f}° in range {min_target}-{max_target}°)"

def get_angles(sequence, class_name):
    """
    Calcola la sequenza di angoli per una sequenza di frame.
    Per 'estensione_gamba' restituisce l'angolo interno, per le altre classi 180-angolo (angolo esterno).
    """
    if sequence.ndim == 2:
        sequence = sequence.reshape(-1, 5, 3)
    angles = []
    for frame in sequence:
        hip, knee, ankle = frame[0], frame[1], frame[2]
        angle = calculate_angle(hip, knee, ankle)
        if class_name == "estensione_gamba":
            angles.append(angle)
        else:
            angles.append(180 - angle)
    return np.array(angles)

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

        # Feedback angolare con nuova logica
        angles = get_angles(data_raw, class_name)
        idx = int(np.argmax(angles))  # massimo angolo interno
        eval_frame = data_raw[idx]
        eval_angle = angles[idx]
        
        
        feedback = get_feedback(class_name, eval_angle)
        if feedback:
            print(f"📐 Feedback: {feedback} (Evaluated angle: {eval_angle:.2f}°)")

        # --- AGGIUNTA: scrivi file JSON con feedback ---
        if "right" in fname.lower():
            gamba = "dx"
        elif "left" in fname.lower():
            gamba = "sx"
        else:
            gamba = "?"

        output_json = {
            "predizione": class_name,
            "angolo": float(eval_angle),
            "gamba": gamba,
            "feedback": feedback
        }
        with open(PREDICTION_JSON, "w") as f:
            json.dump(output_json, f, indent=4)
        # --- FINE AGGIUNTA ---

    except Exception as e:
        print(f"❌ Error processing {fname}: {e}")
    finally:
        os.remove(fpath)

