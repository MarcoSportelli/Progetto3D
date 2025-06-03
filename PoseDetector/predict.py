import torch
import numpy as np
from models.model import PoseTransformer3D
from models.dataset import align_pose_down

# Parametri (devono essere uguali a quelli usati in training)
LABEL_MAP = {
    'flessione_indietro_dx': 0,
    'flessione_indietro_sx': 1,
    'flessione_avanti_dx': 2,
    'flessione_avanti_sx': 3,
    'estensione_gamba_dx': 4,
    'estensione_gamba_sx': 5
}
CLASS_NAMES = list(LABEL_MAP.keys())
SEQ_LEN = 50
MODEL_PATH = 'best_model_3d_YYYYMMDD_HHMMSS.pt'  # Cambia con il nome del tuo file salvato

# Carica il modello
input_size = ... # imposta la dimensione giusta come nel training
model = PoseTransformer3D(input_size=input_size, num_classes=len(LABEL_MAP))
model.load_state_dict(torch.load(MODEL_PATH, map_location='cpu'))
model.eval()

# Carica il tuo nuovo video (deve essere già preprocessato come un array numpy)
data = np.load('path_al_tuo_nuovo_video.npy')  # shape: (seq_len, n_landmarks, 3)

# Preprocessing come nel training
if data.shape[0] >= SEQ_LEN:
    data = data[:SEQ_LEN]
else:
    padding = np.zeros((SEQ_LEN - data.shape[0], *data.shape[1:]))
    data = np.concatenate([data, padding], axis=0)

mean = data.mean(axis=1, keepdims=True)
std = data.std(axis=1, keepdims=True) + 1e-6
data = (data - mean) / std
data = align_pose_down(data)
data = data.reshape(SEQ_LEN, -1).astype(np.float32)

# Inference
with torch.no_grad():
    x = torch.tensor(data).unsqueeze(0)  # aggiungi batch dimension
    outputs = model(x)
    pred = outputs.argmax(dim=1).item()
    print("Predizione:", CLASS_NAMES[pred])