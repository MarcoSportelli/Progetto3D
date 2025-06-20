import os
os.environ["KMP_DUPLICATE_LIB_OK"] = "TRUE"
import numpy as np
import matplotlib.pyplot as plt

from models.dataset import align_pose_down, normalize_skeleton

# Carica un file .npy di esempio (modifica il path con uno dei tuoi file)
sample_path = "../incoming_data/output.npy"

#sample_path = "data/flessione_indietro/1d_1.npy"
data = np.load(sample_path)  # shape: (seq_len, n_landmarks, 3)

# Prendi il primo frame

#data = align_pose_down(data)
#data = normalize_skeleton(data)

mean = data.mean(axis=(0, 1), keepdims=True)
std = data.std(axis=(0, 1), keepdims=True) + 1e-6
data = (data - mean) / std
    
frame_orig = data[0] 


data_aligned = align_pose_down(data)
data_aligned = normalize_skeleton(data_aligned)
mean_aligned = data_aligned.mean(axis=(0, 1), keepdims=True)
std_aligned = data_aligned.std(axis=(0, 1), keepdims=True) + 1e-6
data_aligned = (data_aligned - mean_aligned) / std_aligned

frame_aligned = data_aligned[0]


for i in range(data.shape[0]):
    frame_orig = data[i]
    frame_aligned = data_aligned[i]

    plt.figure(figsize=(10, 5))

    plt.subplot(1, 2, 1)
    plt.title(f"Original - Frame {i}")
    plt.scatter(frame_orig[:, 0], frame_orig[:, 1], c='b')
    for j, (x, y) in enumerate(frame_orig[:, :2]):
        plt.text(x, y, str(j))
    plt.axis('equal')

    plt.subplot(1, 2, 2)
    plt.title(f"Allineato - Frame {i}")
    plt.scatter(frame_aligned[:, 0], frame_aligned[:, 1], c='r')
    for j, (x, y) in enumerate(frame_aligned[:, :2]):
        plt.text(x, y, str(j))
    plt.axis('equal')

    plt.show()