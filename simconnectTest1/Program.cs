using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace simconnectTest1
{
    class Program
    {

        public enum Requests
        {
            REQUEST1
        }

        public enum DEFINITIONS
        {
            AirCraftControl,
            NonSettableData
            
        }

        struct AirCraftControl
        {
            public double aileron;
            public double elevator;
            public bool master_battery;
            public bool gear_position;

        }

        enum EVENTS
        {
            TOGGLE_MASTER_ALTERNATOR,

            STROBES_TOGGLE,
            TOGGLE_TAXI_LIGHTS,
            TOGGLE_BEACON_LIGHTS,
            TOGGLE_NAV_LIGHTS,
            PANEL_LIGHTS_TOGGLE,
            TOGGLE_CABIN_LIGHTS,
            LANDING_LIGHTS_TOGGLE,
            ALL_LIGHTS_TOGGLE,
        }

        enum groups
        {
            group1
        }



        public struct NonSettableData
        {
            public bool LIGHT_STROBE;
           /* bool LIGHT_TAXI;
            bool LIGHT_BEACON;
            bool LIGHT_NAV;
            bool LIGHT_PANEL;
            bool LIGHT_CABIN_LIGHTS;
            bool LIGHT_LANDING;
            bool ALL_LIGHTS_TOGGLE; //if this changes, disregard state of all other light values.
           */
        }

   
        private static EventWaitHandle waitHandle = new AutoResetEvent(false);
        private static Random ran= new Random();
        public static NonSettableData lightStruct;
        static void Main(string[] args)
        {

            bool panelStrobeState = false;

            Console.WriteLine("Connecting to AVR panel controller");
            //var serialPort = new SerialPort("")



            Console.WriteLine("Hello World!");
            var connection = new SimConnect(
                Process.GetCurrentProcess().ProcessName,
                new IntPtr(0),
                0x0402,
                null,
                0);

            connection.OnRecvSimobjectData += Connection_OnRecvSimobjectData; ;

            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "AILERON POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "ELEVATOR POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0, 1);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "ELECTRICAL MASTER BATTERY", "bool", SIMCONNECT_DATATYPE.INT32, 0, 2);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32, 0, 3);

            connection.RegisterDataDefineStruct<AirCraftControl>(DEFINITIONS.AirCraftControl);
            AirCraftControl x;


            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, "LIGHT STROBE", "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.RegisterDataDefineStruct<NonSettableData>(DEFINITIONS.NonSettableData);
            

            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_MASTER_ALTERNATOR, "TOGGLE_MASTER_ALTERNATOR");
            connection.MapClientEventToSimEvent(EVENTS.STROBES_TOGGLE, nameof(EVENTS.STROBES_TOGGLE));
            connection.MapClientEventToSimEvent(EVENTS.ALL_LIGHTS_TOGGLE, nameof(EVENTS.ALL_LIGHTS_TOGGLE));


            connection.RequestDataOnSimObject(
                Requests.REQUEST1,
                DEFINITIONS.NonSettableData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SIM_FRAME,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0, 0, 0);

            var task =Task.Run(() =>
            {
                while (true)
                {
                    //waitHandle.WaitOne();

                    System.Threading.Thread.Sleep(300);
                    connection.ReceiveMessage();

                    x.aileron = ran.Next(-1, 2) * ran.NextDouble();
                    x.elevator = ran.Next(-1, 2) * ran.NextDouble();
                    x.master_battery = ran.Next(2) == 0 ? false : true;
                    x.gear_position = ran.Next(2) == 0 ? false : true;
                    //Debug.WriteLine(x.aileron);
                   // Debug.WriteLine(x.elevator);
                    try
                    {
                        //connection.SetDataOnSimObject(SettableData.AirCraftControl, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, x);
                        //connection.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.TOGGLE_MASTER_ALTERNATOR, 0, groups.group1, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        //connection.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.STROBES_TOGGLE, 0, groups.group1, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        //connection.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.ALL_LIGHTS_TOGGLE, 0, groups.group1, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if(panelStrobeState != lightStruct.LIGHT_STROBE)
                        {
                            connection.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.STROBES_TOGGLE, 0, groups.group1, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            });
            var form = new System.Windows.Forms.Form();
            var checkbox = new CheckBox();
            checkbox.CheckedChanged += (o, e) =>
            {
                panelStrobeState = checkbox.Checked;
            };

            form.Controls.Add(checkbox);
            form.ShowDialog();


        }



        private static void Connection_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            Console.WriteLine("here");
            lightStruct = (NonSettableData)data.dwData[0];
            
        }
    }
}
