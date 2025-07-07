import torch
import numpy as np
from torch.utils.data import DataLoader
from models.dataset import PoseDataset3D
from models.model import PoseTransformer3D
import matplotlib.pyplot as plt

LABEL_MAP = {
    'flessione_indietro': 0,
    'flessione_avanti': 1,
    'estensione_gamba': 2,
    'squat': 3,
}
CLASS_NAMES = list(LABEL_MAP.keys())

DATA_DIR = 'data'
SEQ_LEN = 50
BATCH_SIZE = 8

# Carica il dataset di test (stesso split del training)
full_dataset = PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=False)
labels = full_dataset.labels

from sklearn.model_selection import StratifiedShuffleSplit
sss = StratifiedShuffleSplit(n_splits=1, test_size=0.3, random_state=42)
_, temp_idx = next(sss.split(np.zeros(len(labels)), labels))
temp_labels = [labels[i] for i in temp_idx]
sss2 = StratifiedShuffleSplit(n_splits=1, test_size=0.5, random_state=42)
_, test_idx = next(sss2.split(np.zeros(len(temp_labels)), temp_labels))
test_indices = [temp_idx[i] for i in test_idx]

test_dataset = torch.utils.data.Subset(full_dataset, test_indices)
test_loader = DataLoader(test_dataset, batch_size=1, shuffle=False)  # batch_size=1 per analisi frame per frame

# Carica il modello già addestrato
sample, _ = full_dataset[0]
input_size = sample.shape[1]
model = PoseTransformer3D(input_size=input_size, num_classes=len(LABEL_MAP))
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model.load_state_dict(torch.load("model_weights_final_v2.pt", map_location=device))
model.to(device)
model.eval()


def calculate_angle(a, b, c):
    v1 = a - b
    v2 = c - b
    cos_theta = np.dot(v1, v2) / (np.linalg.norm(v1) * np.linalg.norm(v2) + 1e-8)
    cos_theta = np.clip(cos_theta, -1.0, 1.0)
    return np.degrees(np.arccos(cos_theta))

def get_angles(sequence, class_name):
    if sequence.ndim == 2:
        sequence = sequence.reshape(-1, 5, 3)
    angles = []
    for frame in sequence:
        hip, knee, ankle = frame[0], frame[1], frame[2]
        angle = calculate_angle(hip, knee, ankle)
        # Per estensione_gamba usa l'angolo interno, per gli altri 180-angolo (angolo esterno)
        if class_name == "estensione_gamba":
            angles.append(angle)
        else:
            angles.append(180 - angle)
    return np.array(angles)



def interpolate_angles(angles, target_len):
    """Interpola la sequenza di angoli alla lunghezza target_len."""
    x_old = np.linspace(0, 1, len(angles))
    x_new = np.linspace(0, 1, target_len)
    return np.interp(x_new, x_old, angles)

def truncate_on_return(angles, tol=3):
    """
    Tronca la sequenza di angoli quando il movimento ritorna al punto iniziale (entro una tolleranza).
    Il punto iniziale è il primo valore della sequenza.
    """
    start = angles[0]
    for i in range(1, len(angles)):
        # Se ritorna vicino al valore iniziale (entro tol gradi) e il movimento è durato almeno 3 frame
        if abs(angles[i] - start) < tol and i >= 3:
            return angles[:i+1]
    return angles

def truncate_on_constant_90(angles, min_len=3, tol=2):
    """
    Tronca la sequenza di angoli quando la parte finale è costante a circa 90° per almeno min_len frame.
    Restituisce la sequenza fino all'inizio della parte piatta a 90°.
    """
    n = len(angles)
    for i in range(n - min_len + 1):
        tail = angles[i:]
        if len(tail) >= min_len and np.allclose(tail, 90, atol=tol):
            return angles[:i]
    return angles

angles_per_class = {k: [] for k in CLASS_NAMES}

with torch.no_grad():
    for i, (x, y) in enumerate(test_loader):
        seq = x[0].cpu().numpy()
        label = y.item()
        class_name = CLASS_NAMES[label]
        x_device = x.to(device)
        logits = model(x_device)
        pred = torch.argmax(logits, dim=1).item()
        if pred == label:
            angles = get_angles(seq, class_name)
            angles_trunc = truncate_on_constant_90(angles)
            if len(angles_trunc) == 0:
                print(f"Sample vuoto dopo troncamento per {class_name}, saltato.")
                continue  # Salta questo sample
            angles_interp = interpolate_angles(angles_trunc, SEQ_LEN)
            angles_per_class[class_name].append(angles_interp)

# Rimuovi il sample di estensione_gamba che ha tutti (o quasi tutti) gli angoli a 90°
filtered_angles = []
for angles in angles_per_class["estensione_gamba"]:
    if not (np.allclose(np.mean(angles), 90, atol=2) and np.std(angles) < 2):
        filtered_angles.append(angles)
    else:
        print("Rimosso un sample di estensione_gamba con angoli ≈ 90°")
angles_per_class["estensione_gamba"] = filtered_angles

PLOT_NAMES = {
    'flessione_indietro': 'Backward knee flexion',
    'flessione_avanti': 'Standing hip flexion',
    'estensione_gamba': 'Seated leg extension',
    'squat': 'Squat'
}

# Plot all classes in a single figure with 2x2 subplots
fig, axs = plt.subplots(2, 2, figsize=(15, 7))
plt.subplots_adjust(wspace=0.25, hspace=0.8)  # Più spazio tra le due righe di plot

MAX_SAMPLES = 5
plot_order = ['flessione_indietro', 'flessione_avanti', 'estensione_gamba', 'squat']

for idx, class_name in enumerate(plot_order):
    row, col = divmod(idx, 2)
    ax = axs[row, col]
    all_angles = angles_per_class[class_name]
    if all_angles:
        for i, angles in enumerate(all_angles[:MAX_SAMPLES]):
            ax.plot(angles, alpha=0.5, label=f'Sample {i+1}')
        # Titolo in grassetto
        ax.set_title(f"{PLOT_NAMES[class_name]}", fontsize=14, fontweight='bold')
        # Etichette x/y in grassetto, senza spazio extra su x
        ax.set_xlabel("Frame", fontsize=12)
        ax.set_ylabel("Angle (°)", fontsize=12, labelpad=16)
        ax.grid(True)
    else:
        ax.set_title(f"{PLOT_NAMES[class_name]}\n(No correct samples)", fontsize=14, fontweight='bold')
        ax.axis('off')

plt.tight_layout(rect=[0, 0.03, 1, 0.95])
plt.show()