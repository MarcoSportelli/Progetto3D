import sys
import os
import numpy as np
import cv2
import mediapipe as mp

# === Argomenti da terminale ===
# if len(sys.argv) != 4:
#     print("Usage: python extract_leg_landmarks.py input_file.mov output_file.npy [left|right]")
#     sys.exit(1)

# video_path = sys.argv[1]
# output_path = sys.argv[2]
# leg = sys.argv[3].lower()

video_path = "video.mp4"  # Cambia con il tuo file video
output_path = "../incoming_data/output.npy"
leg = "right"  # Cambia con "left" o "right"

if leg not in ["left", "right"]:
    print("❌ Lato non valido: usa 'left' o 'right'")
    sys.exit(1)

# === Setup MediaPipe ===
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(
    static_image_mode=False,
    model_complexity=2,
    smooth_landmarks=True,
    enable_segmentation=True,
    min_detection_confidence=0.2,
    min_tracking_confidence=0.2
)

# === Landmark IDs ===
if leg == "right":
    landmarks_ids = [
        mp_pose.PoseLandmark.RIGHT_HIP.value,
        mp_pose.PoseLandmark.RIGHT_KNEE.value,
        mp_pose.PoseLandmark.RIGHT_ANKLE.value,
        mp_pose.PoseLandmark.RIGHT_HEEL.value,
        mp_pose.PoseLandmark.RIGHT_FOOT_INDEX.value,
    ]
else:
    landmarks_ids = [
        mp_pose.PoseLandmark.LEFT_HIP.value,
        mp_pose.PoseLandmark.LEFT_KNEE.value,
        mp_pose.PoseLandmark.LEFT_ANKLE.value,
        mp_pose.PoseLandmark.LEFT_HEEL.value,
        mp_pose.PoseLandmark.LEFT_FOOT_INDEX.value,
    ]

# === Caricamento video ===
if not os.path.exists(video_path):
    print(f"❌ File non trovato: {video_path}")
    sys.exit(1)

cap = cv2.VideoCapture(video_path)

collected_frames = []
frame_idx = 0

try:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            print("✅ Fine del video")
            break

        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = pose.process(image_rgb)

        if results.pose_landmarks:
            h, w, _ = frame.shape
            frame_landmarks = []

            for idx in landmarks_ids:
                lm = results.pose_landmarks.landmark[idx]
                frame_landmarks.append([lm.x, lm.y, lm.z])

            collected_frames.append(frame_landmarks)
            print(f"✅ Frame {frame_idx}: landmarks {leg}")
        else:
            print(f"❌ Nessun landmark - Frame {frame_idx}")

        frame_idx += 1

finally:
    cap.release()
    pose.close()

# === Salvataggio ===
if collected_frames:
    arr = np.array(collected_frames)  # [frame, 5, 3]
    np.save(output_path, arr)
    print(f"✅ Salvato {arr.shape} in {output_path}")
else:
    print("❌ Nessun frame utile trovato.")
