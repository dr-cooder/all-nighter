const int inputPin = A2;
const int ledPin = 2;

int inByte;

void setup() {
  pinMode(inputPin, INPUT);
  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, LOW);
  Serial.begin(9600);
}

void loop() {
  Serial.println(analogRead(inputPin));

  if (Serial.available() > 0) {
    inByte = Serial.read();
    digitalWrite(ledPin, (inByte == 48) ? LOW : HIGH);
  }

  delay(10);
}
