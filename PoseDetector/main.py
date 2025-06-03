import torch
from torch.utils.data import DataLoader
from torch import nn, optim
import datetime
import numpy as np
import matplotlib.pyplot as plt
from collections import Counter

from models.dataset import PoseDataset3D
from models.model import PoseTransformer3D
from train import train_epoch, validate
from utils import plot_confusion_matrix, print_class_distribution, plot_sample, plot_landmarks_matrix

LABEL_MAP = {
    'flessione_indietro_dx': 0,
    'flessione_indietro_sx': 1,
    'flessione_avanti_dx': 2,
    'flessione_avanti_sx': 3,
    'estensione_gamba_dx': 4,
    'estensione_gamba_sx': 5
}
CLASS_NAMES = list(LABEL_MAP.keys())

# Parametri principali
DATA_DIR = 'data'
SEQ_LEN = 50
BATCH_SIZE = 8
NUM_EPOCHS = 50
LR = 1e-3
WEIGHT_DECAY = 5e-4
MODEL_NAME = "model_weights.pt"

# 1. Crea il dataset completo SENZA augmentation
full_dataset = PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=False)

# 2. Suddividi in train/val/test (es: 70% train, 15% val, 15% test)
total_len = len(full_dataset)
train_size = int(0.7 * total_len)
val_size = int(0.15 * total_len)
test_size = total_len - train_size - val_size

torch.manual_seed(42)  # Per riproducibilità
indices = torch.randperm(total_len)
train_indices = indices[:train_size]
val_indices = indices[train_size:train_size+val_size]
test_indices = indices[train_size+val_size:]

# Controllo overlap tra i set
print("Overlap train/val:", len(set(train_indices.tolist()) & set(val_indices.tolist())))
print("Overlap train/test:", len(set(train_indices.tolist()) & set(test_indices.tolist())))
print("Overlap val/test:", len(set(val_indices.tolist()) & set(test_indices.tolist())))

# 3. Crea i dataset train/val/test con augmentation SOLO per il train
train_dataset = torch.utils.data.Subset(
    PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=True), train_indices)
val_dataset = torch.utils.data.Subset(
    PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=False), val_indices)
test_dataset = torch.utils.data.Subset(
    PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=False), test_indices)

train_loader = DataLoader(train_dataset, batch_size=BATCH_SIZE, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=BATCH_SIZE, shuffle=False)
test_loader = DataLoader(test_dataset, batch_size=BATCH_SIZE, shuffle=False)


print_class_distribution(train_dataset, "train")
print_class_distribution(val_dataset, "val")
print_class_distribution(test_dataset, "test")



plot_sample(train_dataset, 0, "Train sample (augmented)")
plot_sample(val_dataset, 0, "Validation sample (no augmentation)")



# Esempio d'uso:
plot_landmarks_matrix(train_dataset, idx=0, frame=0, title="Train sample - primo frame")
plot_landmarks_matrix(val_dataset, idx=0, frame=0, title="Validation sample - primo frame")

# Determina la dimensione dell'input
sample, _ = full_dataset[0]
input_size = sample.shape[1]

# Inizializza modello, device e pesi per classi sbilanciate
model = PoseTransformer3D(input_size=input_size, num_classes=len(LABEL_MAP))
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model.to(device)

num_classes = len(LABEL_MAP)
class_counts = np.bincount([label for _, label in full_dataset], minlength=num_classes)
weights = torch.tensor([sum(class_counts) / c if c > 0 else 1 for c in class_counts], dtype=torch.float).to(device)

# Loss, ottimizzatore e scheduler
criterion = nn.CrossEntropyLoss(weight=weights)
optimizer = optim.Adam(model.parameters(), lr=LR, weight_decay=WEIGHT_DECAY)
scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, mode='min', patience=3)

best_val_loss = float('inf')

train_losses = []
val_losses = []
train_accuracies = []
val_accuracies = []

for epoch in range(NUM_EPOCHS):
    train_loss, train_acc = train_epoch(model, train_loader, criterion, optimizer, device)
    val_loss, val_acc = validate(model, val_loader, criterion, device)

    train_losses.append(train_loss)
    val_losses.append(val_loss)
    train_accuracies.append(train_acc)
    val_accuracies.append(val_acc)
    scheduler.step(val_loss)
    print(f"Epoch {epoch+1}/{NUM_EPOCHS} - Train Acc: {train_acc:.2f}, Val Acc: {val_acc:.2f}")
    if val_loss < best_val_loss:
        best_val_loss = val_loss
        torch.save(model.state_dict(), MODEL_NAME)
        print(f"✅ Modello salvato a epoch {epoch+1} con val_loss {val_loss:.4f}")

# Valutazione finale sul test set
test_loss, test_acc = validate(model, test_loader, criterion, device)
print(f"Test accuracy: {test_acc:.2f}")

# Stampa la confusion matrix sul test set
plot_confusion_matrix(model, test_loader, device, CLASS_NAMES)

# Plot delle curve di loss e accuracy
plt.figure(figsize=(10,4))
plt.subplot(1,2,1)
plt.plot(train_losses, label='Train Loss')
plt.plot(val_losses, label='Val Loss')
plt.xlabel('Epoch')
plt.ylabel('Loss')
plt.legend()
plt.title('Loss Curve')

plt.subplot(1,2,2)
plt.plot(train_accuracies, label='Train Acc')
plt.plot(val_accuracies, label='Val Acc')
plt.xlabel('Epoch')
plt.ylabel('Accuracy')
plt.legend()
plt.title('Accuracy Curve')

plt.tight_layout()
plt.show()