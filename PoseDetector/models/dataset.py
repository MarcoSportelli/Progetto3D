import os
import numpy as np
import torch
from torch.utils.data import Dataset
import numpy as np

def align_pose_down(data):
    # data shape: (seq_len, n_landmarks, 3)
    # Prendi il primo frame (o la media dei frame)
    frame = data[0]
    # Vettore tra primo e ultimo landmark
    v = frame[-1, :2] - frame[0, :2]  # solo X,Y
    # Angolo tra v e asse Y negativo
    angle = np.arctan2(v[0], -v[1])  # atan2(x, -y)
    angle_deg = np.degrees(angle)
    # Ruota tutta la sequenza di -angle_deg gradi
    return rotate_landmarks(data, -angle_deg)

def rotation_matrix_z(angle_deg):
    angle_rad = np.radians(angle_deg)
    cos_a = np.cos(angle_rad)
    sin_a = np.sin(angle_rad)
    return np.array([
        [cos_a, -sin_a, 0],
        [sin_a,  cos_a, 0],
        [0,      0,     1]
    ])

def rotate_landmarks(data, angle_deg):
    R = rotation_matrix_z(angle_deg)
    return np.einsum('flc,cd->fld', data, R)

def translate_landmarks(data, max_shift=0.05):
    shift = np.random.uniform(-max_shift, max_shift, size=(1, 1, 3))
    return data + shift

def time_shift(data, max_shift=5):
    shift = np.random.randint(-max_shift, max_shift+1)
    if shift > 0:
        data = np.pad(data, ((shift,0),(0,0),(0,0)), mode='edge')[:-shift]
    elif shift < 0:
        data = np.pad(data, ((0,-shift),(0,0),(0,0)), mode='edge')[-shift:]
    return data

def dropout_landmarks(data, drop_prob=0.1):
    mask = np.random.rand(*data.shape[:2]) < drop_prob
    data[mask] = 0
    return data

def add_noise(data, std=0.01):
    noise = np.random.normal(0, std, size=data.shape)
    return data + noise

def scale_landmarks(data, scale_range=(0.9, 1.1)):
    scale = np.random.uniform(*scale_range)
    return data * scale

class PoseDataset3D(Dataset):
    def __init__(self, data_dir, label_map, seq_len=50, augment=False):
        self.samples = []
        self.labels = []
        self.seq_len = seq_len
        self.label_map = label_map
        self.augment = augment

        prefix_map = {
            'flessione_indietro_dx': '1d_',
            'flessione_indietro_sx': '1s_',
            'flessione_avanti_dx': '2d_',
            'flessione_avanti_sx': '2s_',
            'estensione_gamba_dx': '4d_',
            'estensione_gamba_sx': '4s_'
        }

        for base_label_name in ['flessione_indietro', 'flessione_avanti', 'estensione_gamba']:
            for side in ['dx', 'sx']:
                label_name = f"{base_label_name}_{side}"
                label = label_map[label_name]
                folder = os.path.join(data_dir, base_label_name)
                if not os.path.exists(folder):
                    print(f"⚠️ Folder missing: {folder}")
                    continue

                prefix = prefix_map[label_name]
                for fname in os.listdir(folder):
                    if fname.startswith(prefix) and fname.endswith('.npy'):
                        data = np.load(os.path.join(folder, fname))
                        if data.ndim != 3 or data.shape[2] != 3:
                            continue

                        if data.shape[0] >= seq_len:
                            data = data[:seq_len]
                        else:
                            padding = np.zeros((seq_len - data.shape[0], *data.shape[1:]))
                            data = np.concatenate([data, padding], axis=0)

                        mean = data.mean(axis=1, keepdims=True)
                        std = data.std(axis=1, keepdims=True) + 1e-6
                        data = (data - mean) / std
                        data = align_pose_down(data)
                        data = data.reshape(seq_len, -1).astype(np.float32)
                        self.samples.append(data)
                        self.labels.append(label)

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        sample = self.samples[idx].reshape(self.seq_len, -1, 3)
        label = self.labels[idx]

        # Augmentation solo in training
        # In __getitem__ del dataset
        if self.augment:
            angle = np.random.uniform(-45, 45) 
            sample = rotate_landmarks(sample, angle)
            sample = scale_landmarks(sample, (0.5, 1.5)) 
            sample = translate_landmarks(sample, max_shift=0.5)
            sample = add_noise(sample, std=0.01)
            sample = time_shift(sample, max_shift=5)
            sample = dropout_landmarks(sample, drop_prob=0.1)
        # Flatten per il modello
        sample = sample.reshape(self.seq_len, -1).astype(np.float32)
        return torch.tensor(sample), torch.tensor(label)