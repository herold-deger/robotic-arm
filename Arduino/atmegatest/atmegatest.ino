#include <Arduino.h>
#include <Wire.h>
#include <Servo.h>


#define Base_pin 2
#define Shoulder_pin 3
#define Elbow_pin 4
#define Wrist_pin 10
#define Speaker_pin 5

Servo Base;
Servo Shoulder;
Servo Elbow;
Servo Wrist;

volatile bool commandeRecue = false;
String commande = "";

void setup() {
  Wire.begin(8); // Adresse esclave 8
  Wire.onReceive(receiveEvent);
  Serial.begin(9600);
  Base.attach(Base_pin);
  Shoulder.attach(Shoulder_pin);
  Elbow.attach(Elbow_pin);
  Wrist.attach(Wrist_pin);
  pinMode(LED_BUILTIN, OUTPUT);
  tone(Speaker_pin, 220, 1000);
}

void receiveEvent(int howMany) {
  commande = "";
  while (Wire.available()) {
    char c = Wire.read();
    commande += c;
  }
  commandeRecue = true;
}

void analyseCommande(String command) {
  // Attend une chaîne du type "2400:2100:2500:2400"
  int angles[4] = {1500, 1500, 1500, 1500};
  int idx = 0;
  int lastIdx = 0;
  for (int i = 0; i < 4; i++) {
    idx = command.indexOf(':', lastIdx);
    String val;
    if (idx == -1 && i < 3) {
      
      Serial.println("Commande incomplète !");
      return;
    }
    if (idx == -1) {
      val = command.substring(lastIdx);
    } else {
      val = command.substring(lastIdx, idx);
      lastIdx = idx + 1;
    }
    angles[i] = val.toInt();
  }

  Base.writeMicroseconds(angles[0]);
  Shoulder.writeMicroseconds(angles[1]);
  Elbow.writeMicroseconds(angles[2]);
  Wrist.writeMicroseconds(angles[3]);
  delay(300);
  Serial.print("Angles appliqués : ");
  Serial.print(angles[0]); Serial.print(" ");
  Serial.print(angles[1]); Serial.print(" ");
  Serial.print(angles[2]); Serial.print(" ");
  Serial.println(angles[3]);
}

void loop() {
  if (commandeRecue) {
    Serial.print("Commande reçue : ");
    Serial.println(commande);
    analyseCommande(commande);
    tone(Speaker_pin, 220, 100);
    commandeRecue = false;
  }
  digitalWrite(LED_BUILTIN, HIGH);
  delay(200);
  digitalWrite(LED_BUILTIN, LOW);
  delay(500);
}