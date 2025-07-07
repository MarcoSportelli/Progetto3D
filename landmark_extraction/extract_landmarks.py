import sys
import os
import pyrealsense2 as rs
import numpy as np
import cv2
import mediapipe as mp

# === Argomenti da terminale ===
# if len(sys.argv) != 4:
#     print("Usage: python extract_leg_landmarks.py input_file.bag output_file.npy [left|right]")
#     sys.exit(1)

bag_path = sys.argv[1]
output_path = sys.argv[2]
leg = sys.argv[3].lower()

#bag_path = "./2s_test.bag"  # Cambia con il tuo file
#output_path = "../incoming_data/output.npy"
#leg = "left"  # Cambiacon "left" o "right"

SEQ_LEN = 50
verbose = False  # Imposta a True per vedere stampe e immagini

if leg not in ["left", "right"]:
    if verbose:
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

# === Setup RealSense ===
pipeline = rs.pipeline()
config = rs.config()

if not os.path.exists(bag_path):
    if verbose:
        print(f"❌ File non trovato: {bag_path}")
    sys.exit(1)

config.enable_device_from_file(bag_path, repeat_playback=False)
pipeline_profile = pipeline.start(config)
align = rs.align(rs.stream.color)

collected_frames = []
frame_idx = 0

try:
    while True:
        try:
            frames = pipeline.wait_for_frames()
        except RuntimeError:
            if verbose:
                print("✅ Fine file .bag")
            break

        aligned = align.process(frames)
        color_frame = aligned.get_color_frame()
        depth_frame = aligned.get_depth_frame()

        if not color_frame or not depth_frame:
            continue

        color_image = np.asanyarray(color_frame.get_data())
        depth_image = np.asanyarray(depth_frame.get_data())

        depth_in_mm = depth_image.astype(np.uint16)
        mask = np.where((depth_in_mm > 0) & (depth_in_mm < 3000), 255, 0).astype(np.uint8)
        kernel = np.ones((5, 5), np.uint8)
        mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)
        masked = cv2.bitwise_and(color_image, color_image, mask=mask)

        image_rgb = cv2.cvtColor(masked, cv2.COLOR_BGR2RGB)
        results = pose.process(image_rgb)

        if results.pose_landmarks:
            h, w, _ = color_image.shape
            frame_landmarks = []

            for idx in landmarks_ids:
                lm = results.pose_landmarks.landmark[idx]
                cx, cy = int(lm.x * w), int(lm.y * h)
              
                if 0 <= cy < depth_in_mm.shape[0] and 0 <= cx < depth_in_mm.shape[1]:
                    real_z = depth_in_mm[cy, cx] / 1000.0 
                    if real_z == 0:
                        real_z = float(lm.z) 
                else:
                    real_z = float(lm.z) 

                frame_landmarks.append([lm.x, lm.y, real_z])  
                if verbose:
                    cv2.circle(color_image, (cx, cy), 6, (0, 255, 0), -1)

            collected_frames.append(frame_landmarks)
            if verbose:
                print(f"✅ Frame {frame_idx}: landmarks {leg}")
        else:
            if verbose:
                print(f"❌ Nessun landmark - Frame {frame_idx}")

        if verbose:
            cv2.imshow("Landmarks", color_image)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

        frame_idx += 1

finally:
    pipeline.stop()
    pose.close()
    if verbose:
        cv2.destroyAllWindows()

# === Salvataggio ===
if collected_frames:
    arr = np.array(collected_frames)  # [frame, 5, 3]
    # Semplificazione/padding a SEQ_LEN
    if arr.shape[0] > SEQ_LEN:
        idx = np.linspace(0, arr.shape[0] - 1, SEQ_LEN).astype(int)
        arr = arr[idx]
    elif arr.shape[0] < SEQ_LEN:
        pad = np.zeros((SEQ_LEN - arr.shape[0], arr.shape[1], arr.shape[2]))
        arr = np.concatenate([arr, pad], axis=0)
    # Costruisci il nome file con il lato
    base, ext = os.path.splitext(output_path)
    output_path_leg = f"{base}_{leg}{ext}"
    np.save(output_path_leg, arr)
    if verbose:
        print(f"✅ Salvato {arr.shape} in {output_path_leg}")
else:
    if verbose:
        print("❌ Nessun frame utile trovato.")