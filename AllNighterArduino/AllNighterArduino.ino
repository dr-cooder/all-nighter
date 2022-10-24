const int inputPin = A2;
const int ledPin = 2;

const int numReadings = 10;
int readings[numReadings];
int readIndex = 0;
int total = 0;
int average = 0;

int inByte;

void setup() {
  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, LOW);
  for (int i = 0; i < numReadings; i++) {
    readings[i] = 0;
  }
  Serial.begin(9600);
}

void loop() {
  total -= readings[readIndex];
  readings[readIndex] = analogRead(inputPin);
  total += readings[readIndex];
  readIndex = readIndex + 1;
  if (readIndex >= numReadings) {
    readIndex = 0;
  }
  average = total / numReadings;

  Serial.println(average);

  if (Serial.available() > 0) {
    inByte = Serial.read();
    digitalWrite(ledPin, (inByte == 48) ? LOW : HIGH);
  }

  delay(10);
}
