using CommandMessenger;
using CommandMessenger.TransportLayer;

namespace CmdMessegerArgTest
{

    #region Enums

    // Kommandoene må være i lik rekkefølge på PC og arduino
    // Kommandoene som mottas må knyttes til en callback
    internal enum Command
    {
        Acknowledge, // 0: Komando for å annerkjenne et mottatt signal (Arduino -> PC)
        Error, // 1: Komando for feil som oppstår  (Arduino -> PC)
        StartLogging, // 2: Komando for å starte logging     (typically PC -> Arduino)
        StopLogging, // 3: Komando for å stoppe logging      (typically PC -> Arduino)
        PlotDataPoint // 4: Komando for å etterspør datapoint plotting (typically Arduino -> PC)
    };

    #endregion

    internal class DataLogging
    {
        #region Variables

        private SerialTransport _serialTransport;
        private ArduinoConnect _arduinoConnect;
        private CmdMessenger _cmdMessenger;
        private ChartForm _controllerForm;

        private string _portName; //name of port, will be set if found in the arduino detect function

        #endregion

        // ------------------ MAIN  ----------------------
        #region Setup

        public void Setup(ChartForm controllerForm)
        {
            // Oppdaterer arduinoen med siste sketch
            _arduinoConnect = new ArduinoConnect();
            //_arduinoConnect.AvrUpdate();
            //_portName = _arduinoConnect.PortName;
            _portName = "COM4";
            //starter logging av sesjonen
            Logger.Log("\nNew TEST\n");

            _controllerForm = controllerForm;

            _controllerForm.SetupChart();

            

            //TODO: Legge inn automatisk deteksjon av com port
            // Lager et Serial Port objekt
            _serialTransport = new SerialTransport
            {
                CurrentSerialSettings = {PortName = _portName, BaudRate = 115200, DtrEnable = false}
                // object initializer
            };

            // Initialiserer command messenger med en Serial Port transport layer
            _cmdMessenger = new CmdMessenger(_serialTransport)
            {
                BoardType = BoardType.Bit16
            };

            // Forteller CmdMessenger at den skal bruke "invoke" komandoer på tråden som kjører GUI
            _cmdMessenger.SetControlToInvokeOn(_controllerForm);

            // Setter command strategi til alltid å fjerne kommandoer på receive queuen som er eldre en 
            // 1 sekund(1000ms). Dette gjør at hvis det kommer data inn fortere en det kan bli plottet så vil 
            // grafen ikke hakke
            _cmdMessenger.AddReceiveCommandStrategy(new StaleGeneralStrategy(1000));

            // Knytter callbacks til Command Messenger
            AttachCommandCallBacks();

            // Knytter NewLinesReceived til logging
            _cmdMessenger.NewLineReceived += NewLineReceived;

            // Knytter NewLineSent til logging 
            _cmdMessenger.NewLineSent += NewLineSent;

            // Starter lytting
            _cmdMessenger.StartListening();
            // Sender acknowledge til arduinoen om at PCen er klar
            var command = new SendCommand((int) Command.Acknowledge);
            _cmdMessenger.SendCommand(command);
        }

        #endregion

        #region Exit

        // Exit funksjon
        public void Exit()
        {
            // Slutter lytting
            _cmdMessenger.StopListening();

            // Dispose _cmdMessenger
            _cmdMessenger.Dispose();

            // Dispose Serial Port object
            _serialTransport.Dispose();

            // Logging Avsluttes
            Logger.Log("Application quit");
        }

        #endregion

        //------------CALLBACKS----------------------
        #region Callbacks

        // Her knyttes mottatte enums opp mot svarfunksjoner
        private void AttachCommandCallBacks()
        {
            _cmdMessenger.Attach((int) Command.Acknowledge, OnAcknowledge);
            _cmdMessenger.Attach((int) Command.Error, OnError);
            _cmdMessenger.Attach((int) Command.PlotDataPoint, OnPlotDataPoint);
        }


        // Hvis det forekommer en feil fra arduinoen vil det logges her 
        private static void OnError(ReceivedCommand receivedcommand)
        {
            //MessageBox.Show(@"Arduino has encountered an error");
            Logger.Log(receivedcommand.ReadStringArg());
        }

        // Her mottas acknowlages fra arduinoen og blir loggført
        private void OnAcknowledge(ReceivedCommand receivedcommand)
        {
            Logger.Log(receivedcommand.ReadStringArg());
            //MessageBox.Show(@" Arduino is ready");
        }

        // Her mottar vi plotdata fra arduino som sendes til funksjonen updategraph i chartForm
        private void OnPlotDataPoint(ReceivedCommand receivedcommand)
        {
            //TODO: legge inn metode for lagring av plotdata
            var time = receivedcommand.ReadBinFloatArg();
            var sensorValueinKg = receivedcommand.ReadBinFloatArg();
            _controllerForm.UpdateGraph(time, sensorValueinKg);
        }

        #endregion

        //-------------------COMMANDS TO SEND----------------------
        #region Commands To send

        // Sender signal til Arduinoen om å starte sending av data
        public void StartLogging()
        {
            var command = new SendCommand((int) Command.StartLogging, (int) Command.Acknowledge, 50);
            Logger.Log(@"Send command - Start Logging");
            var receivedCommand = _cmdMessenger.SendCommand(command, SendQueue.ClearQueue, ReceiveQueue.ClearQueue);
            if (!receivedCommand.Ok)
            {
                Logger.Log(@" Failure > no OK received from controller");
            }
        }

        // Sender signal til arduinoen om å stoppe Logging
        public void StopLogging()
        {
            var command = new SendCommand((int) Command.StopLogging, (int) Command.Acknowledge, 50);
                //må være castet til int for å fungere
            // Send command
            //_cmdMessenger.SendCommand(command);
            Logger.Log(@"Send command - Stop Logging");
            var receivedCommand = _cmdMessenger.SendCommand(command, SendQueue.ClearQueue, ReceiveQueue.ClearQueue);
            if (!receivedCommand.Ok)
            {
                Logger.Log(@" Failure > no OK received from controller");
            }
        }

        #endregion

        //--------------------SEE Commands SENT/RECIVED----------------
        #region SEE Commands SENT/RECIVED

        //Logger alle linjer som blir sent til arduinoen
        private void NewLineSent(object sender, NewLineEvent.NewLineArgs e)
        {
            //MessageBox.Show(@"Sent > " + e.Command.CommandString());
            Logger.Log(@"Sent > " + e.Command.CommandString());
        }

        //Logger alle linjer som blir mottatt fra arduinoen
        private void NewLineReceived(object sender, NewLineEvent.NewLineArgs e)
        {
            // MessageBox.Show(@"Received > " + e.Command.CommandString());
            Logger.Log(@"Received > " + e.Command.CommandString());
        }
    }

    #endregion
}