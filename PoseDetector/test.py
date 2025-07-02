import numpy as np
import matplotlib.pyplot as plt

# Percorsi dei due file .npy da confrontare (uno reale, uno mediapipe)
npy_path_realz = "../incoming_data/output_right_realz.npy"
npy_path_mpz = "../incoming_data/output_right_mpz.npy"

# Carica gli array
arr_realz = np.load(npy_path_realz)
arr_mpz = np.load(npy_path_mpz)

# Prendi il primo frame di ciascun file (shape: [5, 3])
frame0_realz = arr_realz[0]
frame0_mpz = arr_mpz[0]

# Visualizza e confronta le z
landmarks = np.arange(frame0_realz.shape[0])
z_real = frame0_realz[:, 2]
z_mp = frame0_mpz[:, 2]

plt.figure(figsize=(8, 5))
plt.plot(landmarks, z_real, 'o-r', label='z reale (depth map)')
plt.plot(landmarks, z_mp, 's-b', label='z MediaPipe')
plt.title("Confronto z per ogni landmark (primo frame)")
plt.xlabel("Landmark index")
plt.ylabel("z (metri o unità MediaPipe)")
plt.legend()
plt.grid(True, linestyle='--', alpha=0.5)
plt.tight_layout()
plt.show()

# Stampa a console i valori z per confronto numerico
print("z reale (depth map):", z_real)
print("z MediaPipe:", z_mp)