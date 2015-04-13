#include <CmdMessenger.h>  // CmdMessenger




const int sensorPin                  = A0;      // Pin hvor analogt signal hentes
bool acquireData                     = false;   //bestemmer hvor vidt vi skal inhente data eller ikke
float sensorValue                    = 0;       //Verdi hentet fra sensorPin (mellom 0 og 1024) endre til float
float SensorValueInKG                = 0;       //omregnet verdi fra sensorPin til KG
const unsigned long sampleInterval   = 100;    // 0.1 sekunders interval, 10 Hz frequency
unsigned long previousSampleMillis   = 0;
long startAcqMillis                  = 0;       //startTid for messuring
long stopAcqMillis                   = 0;       //stopTid for messuring
unsigned long SERIAL_SPEED           = 115200;  //bestemmer hurtigheten på serial porten    



//Setter et nytt cmdMessenger object til Serial port
CmdMessenger cmdMessenger = CmdMessenger(Serial);


//liste over kommandoer som enten kan sendes eller mottas, for å motta må de knyttes til en callback funksjon
enum
{
  // Kommandoer
  kAcknowledge       	  ,   // Kommando for acknowledge at cmd ble mottatt (Arduino -> PC)
  kError               	  ,   // Kommando for å raportere errors (Arduino -> PC)
  kStartLogging      	  ,   // kommando for å starte logging( PC -> Arduino)
  kStopLogging            ,   // kommando for å stoppe logging( PC -> Arduino)
  kPlotDataPoint    	  ,   // kommando for å sende PlotData (Arduino -> PC)
};



// Komandoer som er sent fra pcen blir her knyttet til en callback funksjon
// Det blir laget en callbak funksjon for hver linje under
void attachCommandCallbacks()
{
  // Callback metoder
  cmdMessenger.attach(OnUnknownCommand);
  cmdMessenger.attach(kStartLogging, OnStartLogging);
  cmdMessenger.attach(kStopLogging, OnStopLogging);
  cmdMessenger.attach(kAcknowledge, OnArduinoReady);

}

// ------------------  C A L L B A C K S -----------------------


// Kalt når arduinoen får en komando som ikke er knyttet til en funksjon
void OnUnknownCommand()
{
  cmdMessenger.sendCmd(kError, "Command without attached callback");
}

// Kalt når arduinoen er oppe å går
void OnArduinoReady()
{
  cmdMessenger.sendCmd(kAcknowledge, "Arduino ready");
}

// Kalt når arduinoen mottar signal om å starte logging av data
void OnStartLogging()
{
  startAcqMillis = millis();
  acquireData    = true;
  cmdMessenger.sendCmd(kAcknowledge, "Start Logging");

}

// Kalt når arduinoen motar signal om å starte logging av data
void OnStopLogging()
{
  stopAcqMillis = millis();
  acquireData    = false;
  cmdMessenger.sendCmd(kAcknowledge, "Stop Logging");
  //kan sende varighet på trenings sesjonen

}

// ------------------ M A I N  ----------------------

// Setup funksjonen
void setup()
{
 
  // Lytter til serielt etter beskjeder fra pcen
  Serial.begin(SERIAL_SPEED);

  
  // Legger til ny linje på hver komando
  cmdMessenger.printLfCr();

  
  // knytter enums til callback metodene
  attachCommandCallbacks();

  // Sender status om til dataen om at arduinoen er ferdig booted
  cmdMessenger.sendCmd(kAcknowledge, "Arduino has started!");
  //Testverdi
  randomSeed(analogRead(0));
}


// Returnerer hvis det har gått mer enn intervall (i ms) siden forgje. 
bool hasExpired(unsigned long &prevTime, unsigned long interval) {
  if (  millis() - prevTime > interval ) {
    prevTime = millis();
    return true;
  } else
    return false;
}

// Loop funksjon
void loop()
{
  
  // prosseserer innkommende seriel data, og utfører callbacks
  cmdMessenger.feedinSerialData();
  
 
   //TODO: Gjerne bruke Library istede for funksjonen
  // Gjør kun messure etter et hvist sampleIntervall
  // Her bestemmes hvor ofte det skal samples verdier
  if (hasExpired(previousSampleMillis, sampleInterval))
  {
    if (acquireData) {
      SendPlotData();
    }
  }
}

// ------------------ F U N C T I O N S------------------



float getVoltage (int pin){
  return (analogRead(pin));   //Leser inn sensorverdi fra bagasjevekt
                             // analogRead(pin) * .004882814; For å konvertere om til volt (0 - 5 Volt). 
}

void SendPlotData(){
  
  float valueInKG = ((getVoltage(sensorPin)-179)/38,67);  //formel for omregning av sensorvalue til KG
  
  float seconds =  (millis() - startAcqMillis) / 1000.0; // gir ut tiden i sekunder siden start logging ble sent.
  
  if (SensorValueInKG >= 0) {       //sjekker hvis verdien er over 0 og sender kun da. 
    cmdMessenger.sendCmdStart(kPlotDataPoint);            // starter sending
    cmdMessenger.sendCmdBinArg((float)seconds);           // knytter sensorValueInKG til sendingen som et argument
    cmdMessenger.sendCmdBinArg((float)valueInKG);   // knytter sensorValueInKG til sendingen som et argument
    cmdMessenger.sendCmdEnd();                            // Avslutter og sender argumentet til PC
  }
  else{
    SensorValueInKG = 0;
    cmdMessenger.sendCmdStart(kPlotDataPoint);            // starter sending
    cmdMessenger.sendCmdBinArg((float)seconds);           // knytter sensorValueInKG til sendingen som et argument
    cmdMessenger.sendCmdBinArg((float)valueInKG);   // knytter sensorValueInKG til sendingen som et argument
    cmdMessenger.sendCmdEnd();                            // Avslutter og sender argumentet til PC
    }

}



