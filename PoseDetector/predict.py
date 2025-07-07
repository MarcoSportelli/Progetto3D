import torch
import numpy as np
from models.model import PoseTransformer3D
from models.dataset import align_pose_down

# Parametri (devono essere uguali a quelli usati in training)
LABEL_MAP = {
    'flessione_indietro': 0,
    'flessione_avanti': 1,
    'estensione_gamba': 2,
    'squat': 3,
}
CLASS_NAMES = list(LABEL_MAP.keys())
SEQ_LEN = 50
MODEL_PATH = 'model_weights_final.pt'  # Cambia se necessario

# Imposta la dimensione giusta come nel training!
input_size = 15  # Esempio: 5 landmarks * 3 (xyz), modifica se usi più feature

# Carica il modello
model = PoseTransformer3D(input_size=input_size, num_classes=len(LABEL_MAP))
model.load_state_dict(torch.load(MODEL_PATH, map_location='cpu'))
model.eval()

# Carica il file .npy da classificare
data = np.load('./data/flessione_avanti/2d_16.npy')  # shape: (seq_len, n_landmarks, 3)

# Preprocessing identico al training
if data.shape[0] < SEQ_LEN:
    pad = np.zeros((SEQ_LEN - data.shape[0], *data.shape[1:]))
    data = np.concatenate([data, pad], axis=0)
else:
    data = data[:SEQ_LEN]

mean = data.mean(axis=1, keepdims=True)
std = data.std(axis=1, keepdims=True) + 1e-6
data = (data - mean) / std
data = align_pose_down(data)
data = data.reshape(SEQ_LEN, -1).astype(np.float32)

# Inference
with torch.no_grad():
    x = torch.tensor(data).unsqueeze(0)  # batch dimension
    outputs = model(x)
    pred = outputs.argmax(dim=1).item()
    print("Predizione:", CLASS_NAMES[pred])