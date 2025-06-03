import numpy as np
import torch
from sklearn.metrics import confusion_matrix, ConfusionMatrixDisplay
import matplotlib.pyplot as plt
from collections import Counter


def plot_confusion_matrix(model, dataloader, device, class_names):
    model.eval()
    all_preds, all_labels = [], []

    with torch.no_grad():
        for inputs, labels in dataloader:
            inputs, labels = inputs.to(device), labels.to(device)
            outputs = model(inputs)
            preds = torch.argmax(outputs, dim=1)
            all_preds.append(preds.cpu())
            all_labels.append(labels.cpu())

    all_preds = torch.cat(all_preds)
    all_labels = torch.cat(all_labels)

    cm = confusion_matrix(all_labels, all_preds)

    # Get the unique class indices present in the actual data and predictions
    unique_labels = np.unique(np.concatenate([all_labels, all_preds]))

    # Select the corresponding class names for display
    display_class_names = [class_names[i] for i in unique_labels]

    # Initialize ConfusionMatrixDisplay with only the relevant class names
    disp = ConfusionMatrixDisplay(confusion_matrix=cm, display_labels=display_class_names)
    disp.plot(cmap="Blues", values_format='d')
    plt.title("Confusion Matrix")
    plt.show()

def debug_predictions_distribution(model, dataloader, device):
    model.eval()
    all_preds = []

    with torch.no_grad():
        for X, _ in dataloader:
            X = X.to(device)
            outputs = model(X)
            preds = torch.argmax(outputs, dim=1)
            all_preds.extend(preds.cpu().numpy())

    pred_counts = Counter(all_preds)
    print("Distribuzione delle predizioni sul validation set:", pred_counts)
# Distribuzione delle classi
def print_class_distribution(subset, name):
    labels = [subset.dataset.labels[i] for i in subset.indices]
    print(f"Distribuzione classi in {name}:", Counter(labels))

# Visualizza un sample dal train (con augmentation) e dal validation (senza)
def plot_sample(subset, idx, title):
    sample, _ = subset[idx]
    sample = sample.numpy().reshape(-1, 3)
    plt.scatter(sample[:,0], sample[:,1])
    plt.title(title)
    plt.show()
    
def plot_landmarks_matrix(subset, idx=0, frame=0, title="Landmark positions"):
    sample, _ = subset[idx]
    # Ricostruisci la matrice (seq_len, n_landmarks, 3)
    n_landmarks = sample.shape[0] // 3 if len(sample.shape) == 1 else sample.shape[1] // 3
    sample = sample.numpy().reshape(-1, n_landmarks, 3)
    frame_mat = sample[frame]  # frame: shape (n_landmarks, 3)
    plt.figure(figsize=(5,5))
    plt.scatter(frame_mat[:,0], frame_mat[:,1], c='b')
    for i, (x, y) in enumerate(zip(frame_mat[:,0], frame_mat[:,1])):
        plt.text(x, y, str(i), fontsize=9, color='red')
    plt.title(title + f" (frame {frame})")
    plt.axis('equal')
    plt.xlabel("X")
    plt.ylabel("Y")
    plt.show()

def plot_predictions(model, dataloader, device, num_batches=1):
    model.eval()
    inputs_shown = 0

    with torch.no_grad():
        for inputs, labels in dataloader:
            inputs, labels = inputs.to(device), labels.to(device)
            outputs = model(inputs)
            preds = torch.argmax(outputs, dim=1)

            labels = labels.cpu().numpy()
            preds = preds.cpu().numpy()

            batch_size = inputs.shape[0]
            for i in range(batch_size):
                print(f"Sample {inputs_shown + 1}:")
                print(f"  True Label: {labels[i]}")
                print(f"  Predicted : {preds[i]}")
                print("-" * 30)

                inputs_shown += 1
                if inputs_shown >= batch_size * num_batches:
                    return

def count_classes_in_dataset(dataset):
    labels = []
    # Iterate through the dataset using its __getitem__ method
    for i in range(len(dataset)):
        # dataset[i] returns a tuple (sample, label)
        _, label = dataset[i]
        # Ensure label is a standard Python int for counting
        labels.append(label.item())
    counts = Counter(labels)
    return counts
