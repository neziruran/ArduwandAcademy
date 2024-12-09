#include <Wire.h>

#define SENSOR_ADDRESS 0x6A  // I2C address for LSM6DS3

// Register addresses for accelerometer and gyroscope
#define CTRL1_XL 0x10
#define CTRL2_G  0x11

void setup() {
  Serial.begin(9600);
  Wire.begin();

  // Initialize the accelerometer
  writeRegister(SENSOR_ADDRESS, CTRL1_XL, 0x60);  // Enable accelerometer
  writeRegister(SENSOR_ADDRESS, CTRL2_G, 0x60);   // Enable gyroscope
}

void loop() {
  // Read accelerometer data
  int16_t accelX = readRegister16(SENSOR_ADDRESS, 0x28);
  int16_t accelY = readRegister16(SENSOR_ADDRESS, 0x2A);
  int16_t accelZ = readRegister16(SENSOR_ADDRESS, 0x2C);

  // Read gyroscope data
  int16_t gyroX = readRegister16(SENSOR_ADDRESS, 0x22);
  int16_t gyroY = readRegister16(SENSOR_ADDRESS, 0x24);
  int16_t gyroZ = readRegister16(SENSOR_ADDRESS, 0x26);

  // Send data to Processing in CSV format: accelX,accelY,accelZ,gyroX,gyroY,gyroZ
  Serial.print(accelX); Serial.print(",");
  Serial.print(accelY); Serial.print(",");
  Serial.print(accelZ); Serial.print(",");
  Serial.print(gyroX); Serial.print(",");
  Serial.print(gyroY); Serial.print(",");
  Serial.println(gyroZ);

  delay(50);  // Adjust the delay for desired data rate
}

// Helper functions to write to and read from registers
void writeRegister(uint8_t address, uint8_t reg, uint8_t value) {
  Wire.beginTransmission(address);
  Wire.write(reg);
  Wire.write(value);
  Wire.endTransmission();
}

int16_t readRegister16(uint8_t address, uint8_t reg) {
  Wire.beginTransmission(address);
  Wire.write(reg);
  Wire.endTransmission(false);  // Send repeated start

  Wire.requestFrom(address, (uint8_t)2);
  int16_t value = Wire.read() | (Wire.read() << 8);
  return value;
}
