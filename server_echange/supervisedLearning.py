import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from tensorflow import keras
from tensorflow.keras import layers
import numpy as np
import joblib


df = pd.read_csv("fusion_data_temp.csv")

X = df[["x", "y", "z"]].values        
y = df[["j1", "j2", "j3", "j4"]] 

# 3. Découpage apprentissage / validation / test
X_train, X_tmp, y_train, y_tmp = train_test_split(X, y, test_size=0.3, random_state=42)
X_val, X_test,  y_val, y_test  = train_test_split(X_tmp, y_tmp, test_size=0.5, random_state=42)

# 4. Normalisation
X_scaler = StandardScaler().fit(X_train)
y_scaler = StandardScaler().fit(y_train)

X_train_n = X_scaler.transform(X_train)
X_val_n   = X_scaler.transform(X_val)
X_test_n  = X_scaler.transform(X_test)

y_train_n = y_scaler.transform(y_train)
y_val_n   = y_scaler.transform(y_val)

# 5. Architecture réseau (Keras)
model = keras.Sequential([
    layers.Dense(256, activation="relu", input_shape=(3,)),
    layers.Dense(256, activation="relu"),
    layers.Dense(128, activation="relu"),
    layers.Dense(4,  activation="linear")
])

model.compile(optimizer=keras.optimizers.Adam(1e-3),
              loss="mse",
              metrics=[keras.metrics.RootMeanSquaredError()])

# 6. Entraînement avec Early Stopping
early = keras.callbacks.EarlyStopping(patience=30, restore_best_weights=True)

history = model.fit(
    X_train_n, y_train_n,
    validation_data=(X_val_n, y_val_n),
    epochs=1000, batch_size=64,
    callbacks=[early],
    verbose=2
)

# 7. Évaluation
test_loss, test_rmse = model.evaluate(X_test_n, y_scaler.transform(y_test))
print(f"RMSE test (normalisé) : {test_rmse:.3f}")

# Retour à l’échelle “angle” :
y_pred = y_scaler.inverse_transform(model.predict(X_test_n))
rmse_deg = np.sqrt(((y_pred - y_test)**2).mean(axis=0))
print("RMSE par articulation (°) :", rmse_deg)

for i in range(5):
    print("Prédit :", y_pred[i], "Réel :", y_test[i])

# Sauvegarde du modèle Keras
model.save("robotic_arm_keras.keras")

# Sauvegarde des scalers
joblib.dump(X_scaler, "X_scaler.save")
joblib.dump(y_scaler, "y_scaler.save")