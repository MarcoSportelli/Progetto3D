import os
import numpy as np
import torch
from torch.utils.data import Dataset
import numpy as np
def align_pose_down(data):
    # Usa primo frame per allineare rispetto al corpo
    frame = data[0]
    origin = frame[0, :2]  # es: hip
    target = frame[-1, :2]  # es: foot

    v = target - origin
    angle = np.arctan2(v[0], -v[1])  # rotazione verso basso
    angle_deg = np.degrees(angle)

    # Sposta l'origine al primo punto
    data_xy = data[..., :2] - origin
    data_z = data[..., 2:]

    # Ruota XY
    R = rotation_matrix_z(-angle_deg)[:2, :2]
    data_xy_rot = np.einsum('flc,cd->fld', data_xy, R)

    # Ricostruisci con Z invariato
    data_rot = np.concatenate([data_xy_rot, data_z], axis=-1)
    return data_rot



def normalize_skeleton(data):
    # usa distanza hip-ankle del primo frame
    hip = data[0, 0]
    ankle = data[0, 2]
    norm_len = np.linalg.norm(ankle - hip) + 1e-6
    return data / norm_len


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

        for base_label_name in ['flessione_indietro', 'flessione_avanti', 'estensione_gamba', 'squat']:
            label = label_map[base_label_name]
            folder = os.path.join(data_dir, base_label_name)
            if not os.path.exists(folder):
                print(f"⚠️ Folder missing: {folder}")
                continue

            for fname in os.listdir(folder):
                if fname.endswith('.npy'):
                    data = np.load(os.path.join(folder, fname))
                    if data.ndim != 3 or data.shape[2] != 3:
                        continue

                    if data.shape[0] >= seq_len:
                        data = data[:seq_len]
                    else:
                        padding = np.zeros((seq_len - data.shape[0], *data.shape[1:]))
                        data = np.concatenate([data, padding], axis=0)
                    
                    data = align_pose_down(data)
                    data = normalize_skeleton(data)

                    mean = data.mean(axis=(0, 1), keepdims=True)
                    std = data.std(axis=(0, 1), keepdims=True) + 1e-6
                    data = (data - mean) / std
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
            angle = np.random.uniform(-60, 60) 
            sample = rotate_landmarks(sample, angle)
            sample = scale_landmarks(sample, (0.5, 1.5)) 
            sample = translate_landmarks(sample, max_shift=0.5)
            sample = add_noise(sample, std=0.01)
            sample = time_shift(sample, max_shift=5)
            sample = dropout_landmarks(sample, drop_prob=0.5)
        # Flatten per il modello
        sample = sample.reshape(self.seq_len, -1).astype(np.float32)
        return torch.tensor(sample), torch.tensor(label)