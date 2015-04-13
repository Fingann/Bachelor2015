using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CommandMessenger;
using ZedGraph;

namespace CmdMessegerArgTest
{
    public partial class ChartForm : Form
    {
        // ------------------ MAIN  ----------------------

        #region Initialisation

        public ChartForm()
        {
            InitializeComponent();
            
            // lager et datalogging objekt og kjører Setup metode i DataLogging.cs
            _dataLogging = new DataLogging();
            _dataLogging.Setup(this);
        }

        #endregion

        #region Initialisering av graf

        // initialiserer grafen
        public void SetupChart()
        {
            var masterPane = zedGraphControl1.MasterPane;
            masterPane.PaneList.Clear();


            // lager en ny graphPane og bestemmer tittel, xAkse og yAkse
            _myPane = new GraphPane(new Rectangle(5, 5, 890, 350),
                "KG controller",
                "Time (s)",
                "Kilo (KG)");
            masterPane.Add(_myPane);


            //lager en data-array som holder plottkordinasjonene fra arduinoen
            _analog1List = new RollingPointPairList(NumberOfDatapoints);

            _analog1List.Clear();

            _myPane.XAxis.Scale.Min = 0;
            _myPane.XAxis.Scale.Max = 10;
            _myPane.XAxis.Scale.MinorStep = 1;
            _myPane.XAxis.Scale.MajorStep = 5;

            _myPane.XAxis.Type = AxisType.Linear;

            // Gjør linjen glatt og rødfarget 
            var myCurve1 = _myPane.AddCurve("KG Liftet", _analog1List, Color.Red, SymbolType.None);
            myCurve1.Line.Width = 2;
            myCurve1.Line.IsSmooth = true;
            myCurve1.Line.SmoothTension = 0.2f;

            SetChartScale(0);
        }

        #endregion

        // ------------------ GRAPH CONTROLL  ----------------------

        #region UpdateGraph

        // oppgraderer grafen med plottdata fra arduinoen 
        public void UpdateGraph(double time, double valueInKg)
        {
            _arrayCount ++;
            //test
            label2.Text = "";
            label2.Text = time.ToString(CultureInfo.InvariantCulture);
            label1.Text = "";
            label1.Text = valueInKg.ToString(CultureInfo.InvariantCulture);


            // legger plottet og tiden til i Arryen. Logger evt. NullExceptions i logg
            try
            {
                _array[_arrayCount, 0] = time;
                _array[_arrayCount, 1] = valueInKg;

                _analog1List.Add(time, valueInKg);
            }
            catch (NullReferenceException ex)
            {
                Logger.Log(ex.ToString());
            }

            // Stadig oppdatering av grafen bruker mye cpu-kraft hvis det blir mange plot points
            // Derfor blir dette bare gjort vært 10ms, 100 Hz 
            if (!TimeUtils.HasExpired(ref _previousChartUpdate, 10)) return;


            SetChartScale(time);
        }

        #endregion

        #region SetChartScale

        private void SetChartScale(double time)
        {
            // setter window width
            // TODO:legge inn dynamisk størrelse
            const double windowWidth = 30.0;

            // Henter og opdaterer vinduet til å passe med x-aksens verdier.
            var xScaleTemp = _myPane.XAxis.Scale;

            if (time < windowWidth)
            {
                xScaleTemp.Max = windowWidth;
                xScaleTemp.Min = 0;
            }
            else
            {
                xScaleTemp.Max = time + xScaleTemp.MajorStep;
                xScaleTemp.Min = xScaleTemp.Max - windowWidth;
            }


            // Reskalerer aksene til å passe med data
            zedGraphControl1.AxisChange();

            // Tvinger grafen til å oppdatere fortløpende
            zedGraphControl1.Invalidate();
        }

        #endregion

        #region Reset Graph

        public void ClearGraph()
        {
            // Klarerer arrayListen
            _analog1List.Clear();
            // Tvinger grafen til å oppdatere fortløpende
            zedGraphControl1.Invalidate();
        }

        #endregion

        #region Dispose

        // Disposer alle brukte resursser
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataLogging.Exit();
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        private void zedGraphControl1_Load(object sender, EventArgs e)
        {
        }

        #region variabler

        private readonly DataLogging _dataLogging;
        private long _previousChartUpdate;
        private IPointListEdit _analog1List;
        private GraphPane _myPane;

        private const int NumberOfDatapoints = 30000;
        private int _arrayCount;
        private readonly double[,] _array = new double[NumberOfDatapoints, 2];

        #endregion

        // ------------------ Button Clicks  ----------------------

        #region Button Clicks

        //Starter Loggingen av plotdata 
        private void start_btn_Click(object sender, EventArgs e)
        {
            ClearGraph();
            _dataLogging.StartLogging();
        }

        //Avslutter loggingen av data
        private void stop_btn_Click(object sender, EventArgs e)
        {
            //_sessionlist.Add(_array);

            _dataLogging.StopLogging();
        }


        private void clearGraph_btn_Click(object sender, EventArgs e)
        {
            ClearGraph();
        }

        #endregion
    }
}