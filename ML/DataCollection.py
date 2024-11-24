import cv2
import mediapipe as mp
import numpy as np
import json
import os
import tkinter as tk
from tkinter import ttk, messagebox
from PIL import Image, ImageTk
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.preprocessing import StandardScaler
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import LabelEncoder
import pickle
from sklearn.neural_network import MLPClassifier


class GestureRecorderGUI:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Gesture Data Recorder")
        self.root.geometry("1200x700")

        # MediaPipe setup
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )
        self.mp_draw = mp.solutions.drawing_utils

        # Data storage
        self.data_dir = "gesture_data"
        os.makedirs(self.data_dir, exist_ok=True)
        self.recorded_gestures = self.load_existing_data()
        self.current_gesture = None
        self.recording = False
        self.samples_count = 0
        self.DEFAULT_SAMPLES = 50
        self.MAX_SAMPLES = self.DEFAULT_SAMPLES

        # Model components
        self.classifier = None
        self.label_encoder = LabelEncoder()

        # Video capture
        self.cap = cv2.VideoCapture(0)
        self.camera_active = True

        self.setup_gui()
        self.update_gesture_list()
        self.update_camera()

    def setup_gui(self):
        # Main layout
        left_frame = ttk.Frame(self.root, padding="10")
        left_frame.pack(side=tk.LEFT, fill=tk.Y)

        right_frame = ttk.Frame(self.root, padding="10")
        right_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        # Gesture list section
        ttk.Label(left_frame, text="Recorded Gestures:").pack(anchor=tk.W)
        self.gesture_listbox = tk.Listbox(left_frame, width=30, height=15)
        self.gesture_listbox.pack(pady=5)

        # Buttons for gesture management
        ttk.Button(left_frame, text="Delete Selected", command=self.delete_gesture).pack(fill=tk.X, pady=2)
        ttk.Button(left_frame, text="Train Model", command=self.train_model).pack(fill=tk.X, pady=2)

        # Sample count configuration
        sample_frame = ttk.LabelFrame(left_frame, text="Sample Configuration", padding="5")
        sample_frame.pack(fill=tk.X, pady=5)

        ttk.Label(sample_frame, text="Number of Samples:").pack(anchor=tk.W)

        sample_input_frame = ttk.Frame(sample_frame)
        sample_input_frame.pack(fill=tk.X, pady=2)

        self.sample_var = tk.StringVar(value=str(self.DEFAULT_SAMPLES))
        self.sample_entry = ttk.Entry(sample_input_frame, textvariable=self.sample_var, width=10)
        self.sample_entry.pack(side=tk.LEFT, padx=2)

        ttk.Button(sample_input_frame, text="Set", command=self.update_sample_count).pack(side=tk.LEFT, padx=2)
        ttk.Button(sample_input_frame, text="Reset", command=self.reset_sample_count).pack(side=tk.LEFT, padx=2)

        # New gesture section
        ttk.Label(left_frame, text="New Gesture:").pack(anchor=tk.W, pady=(10, 0))
        self.new_gesture_entry = ttk.Entry(left_frame)
        self.new_gesture_entry.pack(fill=tk.X, pady=2)

        self.record_button = ttk.Button(left_frame, text="Start Recording", command=self.toggle_recording)
        self.record_button.pack(fill=tk.X, pady=2)

        # Status section
        self.status_label = ttk.Label(left_frame, text="Status: Ready")
        self.status_label.pack(pady=10)

        self.progress_label = ttk.Label(left_frame, text=f"Samples: 0/{self.MAX_SAMPLES}")
        self.progress_label.pack()

        # Add Quit button
        quit_button = ttk.Button(left_frame, text="Quit", command=self.quit_application, style="Accent.TButton")
        quit_button.pack(fill=tk.X, pady=(20, 2))

        # Create accent style for quit button
        self.root.style = ttk.Style()
        self.root.style.configure("Accent.TButton", foreground="red")

        # Camera view
        self.camera_label = ttk.Label(right_frame)
        self.camera_label.pack(expand=True, fill=tk.BOTH)

    def quit_application(self):
        if self.recording:
            if not messagebox.askyesno("Warning", "Recording is in progress. Are you sure you want to quit?"):
                return
            self.toggle_recording()  # Stop recording and save data

        self.camera_active = False
        self.cap.release()
        cv2.destroyAllWindows()
        self.root.quit()
        self.root.destroy()

    def update_sample_count(self):
        try:
            new_count = int(self.sample_var.get())
            if new_count <= 0:
                raise ValueError("Sample count must be positive")
            self.MAX_SAMPLES = new_count
            self.progress_label.config(text=f"Samples: {self.samples_count}/{self.MAX_SAMPLES}")
            messagebox.showinfo("Success", f"Sample count updated to {new_count}")
        except ValueError as e:
            messagebox.showerror("Error", "Please enter a valid positive number")
            self.sample_var.set(str(self.MAX_SAMPLES))

    def reset_sample_count(self):
        self.MAX_SAMPLES = self.DEFAULT_SAMPLES
        self.sample_var.set(str(self.DEFAULT_SAMPLES))
        self.progress_label.config(text=f"Samples: {self.samples_count}/{self.MAX_SAMPLES}")
        messagebox.showinfo("Success", "Sample count reset to default (50)")

    def load_existing_data(self):
        data_file = os.path.join(self.data_dir, "gesture_data.json")
        if os.path.exists(data_file):
            with open(data_file, 'r') as f:
                data = json.load(f)
                return {k: [np.array(x) for x in v] for k, v in data.items()}
        return {}

    def save_data(self):
        # Save JSON data for backup
        data_file = os.path.join(self.data_dir, "gesture_data.json")
        with open(data_file, 'w') as f:
            json.dump({k: [x.tolist() for x in v] for k, v in self.recorded_gestures.items()}, f)


    def train_model(self):
        if not self.recorded_gestures:
            messagebox.showerror("Error", "No gesture data available for training!")
            return

        try:
            # Prepare training data
            X, y = [], []
            for gesture, samples in self.recorded_gestures.items():
                X.extend(samples)
                y.extend([gesture] * len(samples))
            X, y = np.array(X), np.array(y)
            y_encoded = self.label_encoder.fit_transform(y)

            # Use MLPClassifier instead of RandomForest for better performance on high-dimensional data
            self.classifier = MLPClassifier(hidden_layer_sizes=(100, 50), max_iter=300, random_state=42)
            self.classifier.fit(X, y_encoded)

            # Save model
            with open(os.path.join(self.data_dir, "gesture_model.pkl"), 'wb') as f:
                pickle.dump((self.classifier, self.label_encoder), f)
            messagebox.showinfo("Success", "Model trained and saved successfully!")
        except Exception as e:
            messagebox.showerror("Error", f"Failed to train model: {str(e)}")


    def update_gesture_list(self):
        self.gesture_listbox.delete(0, tk.END)
        for gesture in sorted(self.recorded_gestures.keys()):
            self.gesture_listbox.insert(tk.END, f"{gesture} ({len(self.recorded_gestures[gesture])} samples)")

    def delete_gesture(self):
        selection = self.gesture_listbox.curselection()
        if selection:
            gesture_name = self.gesture_listbox.get(selection[0]).split(" (")[0]
            if messagebox.askyesno("Confirm Delete", f"Delete gesture '{gesture_name}'?"):
                del self.recorded_gestures[gesture_name]
                self.save_data()
                self.update_gesture_list()


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

    def extract_hand_features2(self, landmarks):
        if landmarks is None:
            return None

        # Extract raw points
        points = [[lm.x, lm.y, lm.z] for lm in landmarks.landmark]

        # Get the wrist point as origin
        wrist = points[0]

        # Calculate bounding box in x-y plane
        min_x = min(p[0] for p in points)
        max_x = max(p[0] for p in points)
        min_y = min(p[1] for p in points)
        max_y = max(p[1] for p in points)

        # Calculate scale factors for x-y normalization
        x_scale = max_x - min_x
        y_scale = max_y - min_y
        scale = max(x_scale, y_scale)  # Use the larger scale for uniform scaling

        if scale == 0:  # Avoid division by zero
            scale = 1.0

        # Normalize points:
        # 1. Translate to origin (wrist-relative)
        # 2. Scale x-y coordinates to be invariant to hand size
        # 3. Reduce z-axis influence by scaling it down
        normalized_points = []
        for point in points:
            nx = (point[0] - wrist[0]) / scale  # Normalize x
            ny = (point[1] - wrist[1]) / scale  # Normalize y
            nz = (point[2] - wrist[2]) * 0.1  # Reduce z-axis influence
            normalized_points.append([nx, ny, nz])

        # Flatten the normalized coordinates
        features = np.array(normalized_points).flatten()

        # Add pairwise distances in x-y plane only
        for i in range(len(normalized_points)):
            for j in range(i + 1, len(normalized_points)):
                # Calculate distance in x-y plane only
                dx = normalized_points[i][0] - normalized_points[j][0]
                dy = normalized_points[i][1] - normalized_points[j][1]
                dist = np.sqrt(dx * dx + dy * dy)  # Euclidean distance in x-y plane
                features = np.append(features, dist)

        return features

    def toggle_recording(self):
        gesture_name = self.new_gesture_entry.get().strip()
        if self.recording:
            self.recording = False
            self.record_button.config(text="Start Recording")
            self.status_label.config(text="Status: Ready")
            self.save_data()
        elif gesture_name:
            if gesture_name not in self.recorded_gestures:
                self.recorded_gestures[gesture_name] = []
            self.recording = True
            self.current_gesture = gesture_name
            self.samples_count = len(self.recorded_gestures[gesture_name])
            self.record_button.config(text="Stop Recording")
            self.status_label.config(text=f"Status: Recording '{gesture_name}'")
        else:
            messagebox.showerror("Error", "Please enter a gesture name")

    def update_camera(self):
        if self.camera_active:
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

                    if self.recording and self.current_gesture:
                        features = self.extract_hand_features(results.multi_hand_landmarks[0])
                        if features is not None:
                            self.recorded_gestures[self.current_gesture].append(features)
                            self.samples_count = len(self.recorded_gestures[self.current_gesture])
                            self.progress_label.config(text=f"Samples: {self.samples_count}/{self.MAX_SAMPLES}")

                            if self.samples_count >= self.MAX_SAMPLES:
                                self.toggle_recording()
                                self.update_gesture_list()

                # Convert frame to PhotoImage
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
    app = GestureRecorderGUI()
    app.run()