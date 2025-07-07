import torch
from torch.utils.data import DataLoader
from torch import nn, optim
import datetime
import numpy as np
import matplotlib.pyplot as plt
from collections import Counter
from sklearn.model_selection import StratifiedShuffleSplit
import random

from models.dataset import PoseDataset3D
from models.model import PoseTransformer3D
from train import train_epoch, validate
from utils import plot_confusion_matrix, print_class_distribution, plot_sample, plot_landmarks_matrix
from sklearn.utils.class_weight import compute_class_weight
from sklearn.model_selection import StratifiedShuffleSplit
from sklearn.metrics import confusion_matrix

random.seed(42)
np.random.seed(42)
torch.manual_seed(42)
torch.cuda.manual_seed_all(42)
torch.backends.cudnn.deterministic = True
torch.backends.cudnn.benchmark = False

LABEL_MAP = {
    'flessione_indietro': 0,
    'flessione_avanti': 1,
    'estensione_gamba': 2,
    'squat': 3,
}
CLASS_NAMES = list(LABEL_MAP.keys())

# Parametri principali
DATA_DIR = 'data'
SEQ_LEN = 50
BATCH_SIZE = 8
NUM_EPOCHS = 100
LR = 5e-4
WEIGHT_DECAY = 5e-4
MODEL_NAME = "model_weights_final_v4.pt"

# 1. Crea il dataset completo SENZA augmentation
full_dataset = PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=False)

labels = full_dataset.labels
sss = StratifiedShuffleSplit(n_splits=1, test_size=0.3, random_state=42)
train_idx, temp_idx = next(sss.split(np.zeros(len(labels)), labels))

# Ora splitta temp_idx in val e test, sempre stratificato
temp_labels = [labels[i] for i in temp_idx]
sss2 = StratifiedShuffleSplit(n_splits=1, test_size=0.5, random_state=42)
val_idx, test_idx = next(sss2.split(np.zeros(len(temp_labels)), temp_labels))
val_indices = [temp_idx[i] for i in val_idx]
test_indices = [temp_idx[i] for i in test_idx]

# Controllo overlap tra i set
print("Overlap train/val:", len(set(train_idx) & set(val_indices)))
print("Overlap train/test:", len(set(train_idx) & set(test_indices)))
print("Overlap val/test:", len(set(val_indices) & set(test_indices)))

# 3. Crea i dataset train/val/test con augmentation SOLO per il train
train_dataset = torch.utils.data.Subset(
    PoseDataset3D(DATA_DIR, LABEL_MAP, seq_len=SEQ_LEN, augment=True), train_idx)
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



#plot_sample(train_dataset, 0, "Train sample (augmented)")
#plot_sample(val_dataset, 0, "Validation sample (no augmentation)")



# Esempio d'uso:
#plot_landmarks_matrix(train_dataset, idx=0, frame=0, title="Train sample - primo frame")
#plot_landmarks_matrix(val_dataset, idx=0, frame=0, title="Validation sample - primo frame")

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

labels = full_dataset.labels
sss = StratifiedShuffleSplit(n_splits=1, test_size=0.3, random_state=42)
train_idx, temp_idx = next(sss.split(np.zeros(len(labels)), labels))

# Ora splitta temp_idx in val e test, sempre stratificato
temp_labels = [labels[i] for i in temp_idx]
sss2 = StratifiedShuffleSplit(n_splits=1, test_size=0.5, random_state=42)
val_idx, test_idx = next(sss2.split(np.zeros(len(temp_labels)), temp_labels))
val_indices = [temp_idx[i] for i in val_idx]
test_indices = [temp_idx[i] for i in test_idx]

# Ora hai:
# train_indices, val_indices, test_indices

labels = [train_dataset.dataset.labels[i] for i in train_dataset.indices]
class_weights = compute_class_weight('balanced', classes=np.unique(labels), y=labels)
criterion = nn.CrossEntropyLoss(weight=torch.tensor(class_weights, dtype=torch.float32).to(device))

# Loss, ottimizzatore e scheduler
#criterion = nn.CrossEntropyLoss()
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


all_preds = []
all_labels = []
model.eval()
with torch.no_grad():
    for x, y in test_loader:
        x = x.to(device)
        y = y.to(device)
        logits = model(x)
        preds = torch.argmax(logits, dim=1)
        all_preds.extend(preds.cpu().numpy())
        all_labels.extend(y.cpu().numpy())

cm = confusion_matrix(all_labels, all_preds)
print("Confusion Matrix (righe=veri, colonne=predetti):")
print(cm)

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