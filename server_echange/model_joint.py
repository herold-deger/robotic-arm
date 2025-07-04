import tensorflow as tf


from tensorflow import keras
import joblib
import numpy as np
import sys




model = keras.models.load_model("robotic_arm_keras.keras", compile=False)
X_scaler = joblib.load("X_scaler.save")
y_scaler = joblib.load("y_scaler.save")

def compute_angles(target_position):
    
    
    target_position_n = X_scaler.transform([target_position])
    angles_n = model.predict(target_position_n)
    angles = y_scaler.inverse_transform(angles_n)
    
    return angles[0].tolist()



