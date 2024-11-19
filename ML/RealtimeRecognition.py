import cv2
import mediapipe as mp
import numpy as np
import socket
import os
import pickle
import tkinter as tk
from tkinter import ttk, messagebox
from PIL import Image, ImageTk


class GestureRecognizer:
    def __init__(self):
        # GUI setup
        self.root = tk.Tk()
        self.root.title("Gesture Recognition")
        self.root.geometry("800x600")

        # MediaPipe setup
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )
        self.mp_draw = mp.solutions.drawing_utils

        # Model components
        self.data_dir = "gesture_data"
        self.classifier = None
        self.label_encoder = None
        self.load_model()

        # Video capture
        self.cap = cv2.VideoCapture(0)
        self.camera_active = True

        # UDP client setup
        self.udp_client = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.unity_ip = "127.0.0.1"  # Localhost if Unity and Python are on the same machine
        self.unity_port = 5052  # Port used in Unity script

        # Initialize GUI
        self.setup_gui()
        self.update_camera()

    def setup_gui(self):
        # Main layout
        main_frame = ttk.Frame(self.root, padding="10")
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Camera view
        self.camera_label = ttk.Label(main_frame)
        self.camera_label.pack(expand=True, fill=tk.BOTH)

        # Recognition status
        self.status_frame = ttk.Frame(main_frame)
        self.status_frame.pack(fill=tk.X, pady=10)

        self.gesture_label = ttk.Label(self.status_frame, text="Detected Gesture: None", font=("Arial", 14))
        self.gesture_label.pack(side=tk.LEFT)

        self.confidence_label = ttk.Label(self.status_frame, text="Confidence: 0%", font=("Arial", 12))
        self.confidence_label.pack(side=tk.RIGHT)

        # Quit button
        self.quit_button = ttk.Button(main_frame, text="Quit", command=self.quit_application)
        self.quit_button.pack(pady=10)

    def quit_application(self):
        self.camera_active = False
        self.cap.release()
        cv2.destroyAllWindows()
        self.root.quit()
        self.root.destroy()

    def load_model(self):
        model_file = os.path.join(self.data_dir, "gesture_model.pkl")
        if os.path.exists(model_file):
            try:
                with open(model_file, 'rb') as f:
                    self.classifier, self.label_encoder = pickle.load(f)
                return True
            except Exception as e:
                messagebox.showerror("Error", f"Failed to load model: {str(e)}")
        else:
            messagebox.showerror("Error", "No trained model found! Please record gestures first.")
        return False

    def extract_hand_features(self, landmarks):
        if landmarks is None:
            return None

        # Extract raw points
        points = [[lm.x, lm.y, lm.z] for lm in landmarks.landmark]

        # Get the wrist point as origin
        wrist = points[0]

        # Calculate the palm center using points 0, 5, 17 (wrist, index MCP, pinky MCP)
        palm_center = np.mean([points[0], points[5], points[17]], axis=0)

        # Calculate the palm normal vector using cross product of two palm vectors
        palm_vector_1 = np.array(points[5]) - np.array(points[0])  # wrist to index MCP
        palm_vector_2 = np.array(points[17]) - np.array(points[0])  # wrist to pinky MCP
        palm_normal = np.cross(palm_vector_1, palm_vector_2)
        palm_normal = palm_normal / np.linalg.norm(palm_normal)

        # Project all points onto the palm plane
        projected_points = []
        for point in points:
            point_vector = np.array(point) - palm_center
            # Project the point onto the palm plane
            projection = point_vector - np.dot(point_vector, palm_normal) * palm_normal
            projected_points.append(projection)

        # Calculate bounding box in the projected space
        projected_points = np.array(projected_points)
        min_coords = np.min(projected_points, axis=0)
        max_coords = np.max(projected_points, axis=0)
        scale = np.max(max_coords - min_coords)

        if scale == 0:
            scale = 1.0

        # Normalize the projected points
        normalized_points = (projected_points - palm_center) / scale

        # Create feature vector
        features = []

        # 1. Add normalized coordinates
        features.extend(normalized_points.flatten())

        # 2. Add pairwise distances between key points
        key_points_indices = [
            0,  # wrist
            4,  # thumb tip
            8,  # index tip
            12,  # middle tip
            16,  # ring tip
            20  # pinky tip
        ]

        for i in range(len(key_points_indices)):
            for j in range(i + 1, len(key_points_indices)):
                idx1, idx2 = key_points_indices[i], key_points_indices[j]
                dist = np.linalg.norm(normalized_points[idx1] - normalized_points[idx2])
                features.append(dist)

        # 3. Add angles between finger vectors
        for i in range(5):  # For each finger
            base_idx = i * 4  # Base of each finger (MCP joints)
            tip_idx = base_idx + 4  # Tip of each finger
            if i == 0:  # Thumb has a different structure
                base_idx = 2
                tip_idx = 4

            finger_vector = normalized_points[tip_idx] - normalized_points[base_idx]
            finger_vector = finger_vector / np.linalg.norm(finger_vector)

            # Calculate angle with palm normal
            angle = np.arccos(np.clip(np.dot(finger_vector, palm_normal), -1.0, 1.0))
            features.append(angle)

        return np.array(features)

    def extract_hand_features_2(self, landmarks):
        if landmarks is None:
            return None

        points = [[lm.x, lm.y, lm.z] for lm in landmarks.landmark]
        wrist = points[0]
        points = [[p[0] - wrist[0], p[1] - wrist[1], p[2] - wrist[2]] for p in points]
        features = np.array(points).flatten()

        for i in range(len(points)):
            for j in range(i + 1, len(points)):
                dist = np.linalg.norm(np.array(points[i]) - np.array(points[j]))
                features = np.append(features, dist)

        return features

    def predict_gesture(self, landmarks, confidence_threshold=93):
        if self.classifier is None or landmarks is None:
            return "Unknown", 0

        # Extract features from the hand landmarks
        features = self.extract_hand_features(landmarks)
        if features is not None:
            # Get the prediction probability for each gesture class
            prediction_proba = self.classifier.predict_proba([features])

            # Determine the highest confidence score
            confidence = np.max(prediction_proba) * 100
            if confidence >= confidence_threshold:
                # Only return the gesture if the confidence is above the threshold
                prediction = self.classifier.predict([features])
                return self.label_encoder.inverse_transform(prediction)[0], confidence
            else:
                # Return "Unknown" if the confidence is below the threshold
                return "Unknown", confidence
        return "Unknown", 0

    def send_gesture_to_unity(self):
        # Format the gesture and confidence into a string
        gesture_data = f"{self.gesture}|{self.confidence}"
        # Send the data over UDP to Unity
        self.udp_client.sendto(gesture_data.encode(), (self.unity_ip, self.unity_port))

    def update_camera(self):
        if self.camera_active and self.classifier is not None:
            ret, frame = self.cap.read()
            if ret:
                frame = cv2.flip(frame, 1)
                rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                results = self.hands.process(rgb_frame)

                if results.multi_hand_landmarks:
                    self.mp_draw.draw_landmarks(
                        frame,
                        results.multi_hand_landmarks[0],
                        self.mp_hands.HAND_CONNECTIONS
                    )

                    # Get gesture and confidence from the model
                    gesture, confidence = self.predict_gesture(results.multi_hand_landmarks[0])
                    self.gesture = gesture
                    self.confidence = confidence

                    # Update the GUI labels
                    self.gesture_label.config(text=f"Detected Gesture: {gesture}")
                    self.confidence_label.config(text=f"Confidence: {confidence:.1f}%")

                    # Send gesture data to Unity over UDP
                    self.send_gesture_to_unity()

                else:
                    self.gesture_label.config(text="Detected Gesture: None")
                    self.confidence_label.config(text="Confidence: 0%")

                # Convert frame to PhotoImage for displaying in the Tkinter GUI
                image = Image.fromarray(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
                photo = ImageTk.PhotoImage(image=image)
                self.camera_label.configure(image=photo)
                self.camera_label.image = photo

        self.root.after(10, self.update_camera)

    def run(self):
        try:
            self.root.mainloop()
        finally:
            self.camera_active = False
            self.cap.release()
            cv2.destroyAllWindows()


if __name__ == "__main__":
    app = GestureRecognizer()
    app.run()
