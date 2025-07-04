import asyncio
import websockets
import json

async def test_client():
    uri = "ws://localhost:8765"
    async with websockets.connect(uri) as websocket:
        positions = [
            [10, 20, 30],
            [40, 50, 60],
            [70, 80, 90],
            [100, 110, 120]
        ]
        for pos in positions:
            message = {"hand_position": pos}
            await websocket.send(json.dumps(message))
            print(f"Message envoyé : {message}")
            try:
                response = await asyncio.wait_for(websocket.recv(), timeout=5)
                print(f"Réponse reçue : {response}")
            except asyncio.TimeoutError:
                print("Aucune réponse du serveur.")
            await asyncio.sleep(30)  # Attend 30 secondes avant d'envoyer le suivant

asyncio.run(test_client())