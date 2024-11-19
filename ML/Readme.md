# Gesture Recognition System

This project allows you to record hand gestures using a webcam and recognize them in real-time using machine learning. It uses **MediaPipe** for hand landmark detection and **scikit-learn** to train a model that classifies gestures.

## Requirements

Before running the project, ensure you have Python 3.x installed. Then, install the required libraries:

- **opencv-python**: For accessing the webcam and processing the video stream.
- **mediapipe**: For detecting hand landmarks.
- **numpy**: For data manipulation.
- **scikit-learn**: For machine learning model training.
- **pillow**: For handling images if needed.

You can install the required libraries using a package manager like `pip`.

## How to Use

### Step 1: Record Gestures

1. Run the **DataCollection.py** script to start recording gestures.
2. Enter a name for the gesture (e.g., "Swipe Left").
3. Press the **Start Recording** button to begin capturing gesture samples in front of the webcam.
4. The program will automatically stop after recording a predefined number of frames (default: 50).
5. The recorded gesture data will be saved to a folder named `gesture_data` on your local machine.

### Step 2: Train the Model

Once you have recorded enough samples for each gesture (typically 50 samples per gesture), you can train the model. Run the training function in the **train_model.py** script to train the model with the recorded gesture data. This will save the trained model as a `.pkl` file.

### Step 3: Recognize Gestures in Real-Time

1. Run the **RealtimeRecognition.py** script.
2. The webcam will activate and start recognizing gestures in real-time.
3. The model will predict the gesture you make and display the prediction along with its confidence.
4. If the model is confident enough, it will display the detected gesture name; otherwise, it will show "Unknown."

## How it Works

1. **Model Training**:
   - The **DataCollection.py** script uses **MediaPipe** to detect hand landmarks and capture the position of key points in the hand. These landmarks are saved as feature vectors.
   - When recording, the program collects multiple samples of each gesture, which are saved in a structured format for later training.
   - The recorded gesture data is used to train a machine learning classifier (e.g., **RandomForestClassifier** or **MLPClassifier** from **scikit-learn**).
   - The model is trained using the features extracted from the hand landmarks, such as distances between key points, normalized positions, and other relevant metrics.
   - The trained model is saved as a `.pkl` file for future use in real-time recognition.

2. **Gesture Recognition**:
   - During real-time recognition, the **RealtimeRecognition.py** script continuously processes webcam frames, extracting hand landmarks in each frame.
   - The classifier uses these features to predict the gesture being performed.
   - The recognized gesture is displayed along with its confidence percentage. If the model is confident enough, the predicted gesture is shown on the screen.

## Files

- **DataCollection.py**: Script for recording gestures and saving data.
- **RealtimeRecognition.py**: Script for real-time gesture recognition.

## License

This project is open-source and can be freely used and modified for personal or educational purposes.
