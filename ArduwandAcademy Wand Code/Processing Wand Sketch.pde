import processing.serial.*;

Serial myPort;
float accelX, accelY, accelZ;
float gyroX, gyroY, gyroZ;
float sensitivityFactor = 0.1;  // Sensitivity adjustment factor for less rotation

void setup() {
  size(600, 600, P3D);

  // Print all available serial ports to the console
  println("Available Serial Ports:");
  println(Serial.list());  // List all available ports
  
  // Select the correct port based on the output from Serial.list()
  String portName = "/dev/cu.usbmodem101";  // Replace [0] with the correct index for your port
  myPort = new Serial(this, portName, 9600);
  myPort.bufferUntil('\n');
}

void draw() {
  background(200);

  // Apply sensitivity adjustment to accelerometer values
  float angleX = accelX * sensitivityFactor * 0.001;
  float angleY = accelY * sensitivityFactor * 0.001;
  float angleZ = accelZ * sensitivityFactor * 0.001;

  // Center the star in the window and apply rotations
  translate(width / 2, height / 2, 0);
  rotateX(angleX);
  rotateY(angleY);
  rotateZ(angleZ);

  // Draw the 3D star
  fill(150, 0, 150);
  noStroke();
  draw3DStar(0, 0, 0, 100, 50);

  // Display gyroscope data
  fill(0);
  textSize(15);
  text("Gyro X: " + nf(gyroX, 1, 2) + " dps", -280, -250);
  text("Gyro Y: " + nf(gyroY, 1, 2) + " dps", -280, -230);
  text("Gyro Z: " + nf(gyroZ, 1, 2) + " dps", -280, -210);
}

// Function to draw a 3D star
void draw3DStar(float x, float y, float z, float outerRadius, float innerRadius) {
  int numPoints = 5;  // Number of points for the star
  float angle = TWO_PI / numPoints;

  beginShape(TRIANGLES);
  for (int i = 0; i < numPoints; i++) {
    // Outer vertex
    float x1 = x + cos(i * angle) * outerRadius;
    float y1 = y + sin(i * angle) * outerRadius;
    float z1 = z;

    // Inner vertex
    float x2 = x + cos((i + 0.5) * angle) * innerRadius;
    float y2 = y + sin((i + 0.5) * angle) * innerRadius;
    float z2 = z;

    // Next outer vertex
    float x3 = x + cos((i + 1) * angle) * outerRadius;
    float y3 = y + sin((i + 1) * angle) * outerRadius;
    float z3 = z;

    // Create two triangles for each point of the star
    vertex(x, y, z);       // Center point
    vertex(x1, y1, z1);    // Outer point
    vertex(x2, y2, z2);    // Inner point

    vertex(x, y, z);       // Center point
    vertex(x2, y2, z2);    // Inner point
    vertex(x3, y3, z3);    // Next outer point
  }
  endShape();
}

// Read serial data from Arduino and parse it
void serialEvent(Serial myPort) {
  String data = myPort.readStringUntil('\n');
  if (data != null) {
    data = trim(data);
    String[] values = split(data, ',');
    if (values.length == 6) {
      // Parse data and convert to float
      accelX = float(values[0]);
      accelY = float(values[1]);
      accelZ = float(values[2]);
      gyroX = float(values[3]);
      gyroY = float(values[4]);
      gyroZ = float(values[5]);
    }
  }
}
