# serve_model.py
import torch
import numpy as np
import time
import os
from models.model import PoseTransformer3D
from models.dataset import align_pose_down 

LABEL_MAP = {
    'flessione_indietro_dx': 0,
    'flessione_indietro_sx': 1,
    'flessione_avanti_dx': 2,
    'flessione_avanti_sx': 3,
    'estensione_gamba_dx': 4,
    'estensione_gamba_sx': 5
}

SEQ_LEN = 50
INPUT_FEATURE_SIZE = 5 * 3  # 5 landmark * 3 coords (x,y,z)
MODEL_PATH = "model_weights.pt"
WATCH_DIR = "../incoming_data"

model = PoseTransformer3D(input_size=INPUT_FEATURE_SIZE, num_classes=len(LABEL_MAP))
model.load_state_dict(torch.load(MODEL_PATH, map_location='cpu'))
model.eval()

def preprocess(data):
    # Padding o taglio
    if data.shape[0] >= SEQ_LEN:
        data = data[:SEQ_LEN]
    else:
        padding = np.zeros((SEQ_LEN - data.shape[0], *data.shape[1:]))
        data = np.concatenate([data, padding], axis=0)

    # Normalizzazione frame-wise
    mean = data.mean(axis=1, keepdims=True)
    std = data.std(axis=1, keepdims=True) + 1e-6
    data = (data - mean) / std

    # Allineamento pose
    # da togliere se non necessario
    
    data = align_pose_down(data)

    # Flatten per il modello
    return data.reshape(SEQ_LEN, -1).astype(np.float32)


print("✅ Model server running. Watching for .npy files...")


while True:
    files = [f for f in os.listdir(WATCH_DIR) if f.endswith(".npy")]
    for fname in files:
        fpath = os.path.join(WATCH_DIR, fname)
        try:
            data = np.load(fpath)
            data = preprocess(data)
            input_tensor = torch.tensor([data])
            with torch.no_grad():
                output = model(input_tensor)
                pred = torch.argmax(output, dim=1).item()
                class_name = [k for k, v in LABEL_MAP.items() if v == pred][0]
                print(f"[{fname}] → Predicted: {class_name}")
        except Exception as e:
            print(f"❌ Error processing {fname}: {e}")
        finally:
            os.remove(fpath)
    time.sleep(1)
