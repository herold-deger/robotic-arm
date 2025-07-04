import asyncio
import websockets
import json
import requests
from flask import Flask
import threading


import model_joint


ARDUINO_IP = "192.168.1.33"
ARDUINO_PORT = 8080

clients = set()
app = Flask(__name__)


async def handler(websocket):
    clients.add(websocket)
    try:
        async for message in websocket:
            data = json.loads(message)
            print(f"Received from Meta: {data}")
            position = data.get("Target_position")
            position = [x * 1 for x in position] 
            
            if not position:
                print("Aucune position cible fournie.")
                continue
            print(f"Position cible reçue : {position}")
            joint_angles = model_joint.compute_angles(position)

            # Envoie à tous les clients WebSocket
            for client in clients:
                await client.send(json.dumps({"Target_position": joint_angles}))

            # Envoie les angles à l'Arduino en HTTP POST
            try:
                url = f"http://{ARDUINO_IP}:{ARDUINO_PORT}/"
                headers = {"Content-Type": "application/json"}

                joint_angles[0] = int(min(max(550+joint_angles[0]*15,550),2400))
                joint_angles[1] = int(min(max(1233+joint_angles[1]*8.66,800),2100))
                joint_angles[2] = int(min(max(2065+joint_angles[2]*9.65,1100),2500))
                joint_angles[3] = int(min(max(2000-joint_angles[3]*10,700),2400))

                requests.post(url, headers=headers, json={"angles": joint_angles}, timeout=1)
                print(f"Envoyé à Arduino : {joint_angles}")
            except Exception as e:
                print(f"Erreur envoi Arduino : {e}")
    finally:
        clients.remove(websocket)

def run_flask():
    app.run(host="0.0.0.0", port=5000)

async def main():
    print("Serveur WebSocket démarré sur ws://0.0.0.0:8765")
    async with websockets.serve(handler, "0.0.0.0", 8765):
        await asyncio.Future()  # Run forever

if __name__ == "__main__":
    flask_thread = threading.Thread(target=run_flask, daemon=True)
    flask_thread.start()
    asyncio.run(main())
