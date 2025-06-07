import numpy as np
import csv

# Carica il file .npy (shape = [F, 5, 3])
data = np.load("../PoseDetector/data/estensione_gamba/4d_1.npy")

# Landmark target da fissare (landmark 0)
target_pos = np.array([0.48, 0.67, 0])

with open("output_leg.csv", "w", newline="") as f:
    writer = csv.writer(f)
    for frame in data:
        origin = frame[0]  # landmark 0 posizione attuale
        normalized_frame = []

        for point in frame:
            # Trasla i punti in modo che landmark 0 sia nell'origine (0,0,0)
            shifted = point - origin
            # Poi trasla al target richiesto
            normalized = shifted + target_pos
            normalized_frame.extend(normalized.tolist())

        writer.writerow(normalized_frame)
