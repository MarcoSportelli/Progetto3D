import sys
import os
import numpy as np
import cv2
import mediapipe as mp



video_path = sys.argv[1]
output_path = sys.argv[2]
leg = sys.argv[3].lower()


#video_path = "../App/data/20250625_153816_right.mp4"
#output_path = "../incoming_data/output.npy"
#leg = "right"  
SEQ_LEN = 50
verbose = False  

if leg not in ["left", "right"]:
    if verbose:
        print("❌ Lato non valido: usa 'left' o 'right'")
    sys.exit(1)

mp_pose = mp.solutions.pose
pose = mp_pose.Pose(
    static_image_mode=False,
    model_complexity=2,
    smooth_landmarks=True,
    enable_segmentation=True,
    min_detection_confidence=0.2,
    min_tracking_confidence=0.2
)

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

if not os.path.exists(video_path):
    if verbose:
        print(f"❌ File non trovato: {video_path}")
    sys.exit(1)

cap = cv2.VideoCapture(video_path)
frame_count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))

# Scegli 50 indici equidistanti
if frame_count >= SEQ_LEN:
    selected_indices = np.linspace(0, frame_count - 1, SEQ_LEN).astype(int)
else:
    selected_indices = np.arange(frame_count)

collected_frames = []
frame_idx = 0
selected_ptr = 0

try:
    while cap.isOpened() and selected_ptr < len(selected_indices):
        ret, frame = cap.read()
        if not ret:
            if verbose:
                print("✅ Fine del video")
            break

        if frame_idx == selected_indices[selected_ptr]:
            image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = pose.process(image_rgb)

            if results.pose_landmarks:
                h, w, _ = frame.shape
                frame_landmarks = []
                for idx in landmarks_ids:
                    lm = results.pose_landmarks.landmark[idx]
                    frame_landmarks.append([lm.x, lm.y, lm.z])
                    # Disegna il punto sul frame
                    cx, cy = int(lm.x * w), int(lm.y * h)
                    if verbose:
                        cv2.circle(frame, (cx, cy), 6, (0, 255, 0), -1)

                collected_frames.append(frame_landmarks)
                if verbose:
                    print(f"✅ Frame {frame_idx}: landmarks {leg}")
            else:
                if verbose:
                    print(f"❌ Nessun landmark - Frame {frame_idx}")

            # Visualizza solo i frame selezionati
            if verbose:
                cv2.imshow("Landmarks", frame)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break

            selected_ptr += 1

        frame_idx += 1

finally:
    cap.release()
    pose.close()
    if verbose:
        cv2.destroyAllWindows()

# === Salvataggio ===
if collected_frames:
    arr = np.array(collected_frames)  # [SEQ_LEN, 5, 3] o meno se video corto
    # Padding se meno di SEQ_LEN
    if arr.shape[0] < SEQ_LEN:
        pad = np.zeros((SEQ_LEN - arr.shape[0], arr.shape[1], arr.shape[2]))
        arr = np.concatenate([arr, pad], axis=0)
    np.save(output_path, arr)
    
    print(f"✅ Salvato {arr.shape} in {output_path}")
else:
   
    print("❌ Nessun frame utile trovato.")