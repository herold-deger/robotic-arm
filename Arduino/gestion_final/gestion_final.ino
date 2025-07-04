#include <WiFiS3.h>
#include <Wire.h>
// Server Pour arduino R4 wifi



const char* ssid = "XXXXX";
const char* password = "XXXXXXX";

WiFiServer server(8080);

void setup() {

  Wire.begin();  
  Serial.begin(9600);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connecté !");
  Serial.print("IP locale : ");
  Serial.println(WiFi.localIP());
  server.begin();
}
String extractAngles(String body) {
  int start = body.indexOf('[');
  int end = body.indexOf(']');
  if (start == -1 || end == -1 || end <= start) return "";
  String angles = body.substring(start + 1, end);
  angles.replace(" ", ""); 
  angles.replace(",", ":"); 
  return angles;
}

void sendServoCommand(String cmd) {
  String angles = extractAngles(cmd);
  if (angles.length() == 0) {
    Serial.println("Angles non trouvés !");
    return;
  }
  Wire.beginTransmission(8);  
  Wire.print(angles);         
  Wire.endTransmission();
  Serial.print("Envoyé : ");
  Serial.println(angles);
}
void loop() {
  WiFiClient client = server.available();  if (client) {
    String request = "";
    unsigned long timeout = millis() + 1000;
    while (client.connected() && millis() < timeout) {
      while (client.available()) {
        char c = client.read();
        request += c;
        if (request.endsWith("\r\n\r\n")) break;
      }
      if (request.endsWith("\r\n\r\n")) break;
    }
    Serial.println("Requête reçue :");
    Serial.println(request);

    String body = "";
    while (client.available()) {
      char c = client.read();
      body += c;
    }
    if (body.length() > 0) {
      Serial.println("Corps JSON :");
      Serial.println(body);

      
      sendServoCommand(body);
    }
  }

  client.println("HTTP/1.1 200 OK");
  client.println("Content-Type: text/plain");
  client.println("Connection: close");
  client.println();
  client.println("OK");
  client.stop();
}