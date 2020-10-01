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

        //the following two structs need to be seperated because
        //some of the data needs to be set via simVars and others as parameters to simEvents.
        struct AirCraftControlSimVars
        {
            public double aileron;
            public double elevator;
            public bool master_battery;
            public bool gear_position;
        }

        struct AirCraftControl_EventData
        {
            public uint throttlePos;
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

            THROTTLE_SET //0-16383 dword
        }

        enum groups
        {
            group1
        }



        public struct NonSettableData
        {
            public bool LIGHT_STROBE;
            public bool LIGHT_TAXI;
            public bool LIGHT_BEACON;
            public bool LIGHT_NAV;
            public bool LIGHT_PANEL;
            public bool LIGHT_CABIN_LIGHTS;
            public bool LIGHT_LANDING;
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

            //hookup a handler for getting data out of the sim.
            connection.OnRecvSimobjectData += Connection_OnRecvSimobjectData; ;

            //build control data definition schema.
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "AILERON POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "ELEVATOR POSITION", "position", SIMCONNECT_DATATYPE.FLOAT64, 0, 1);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "ELECTRICAL MASTER BATTERY", "bool", SIMCONNECT_DATATYPE.INT32, 0, 2);
            connection.AddToDataDefinition(DEFINITIONS.AirCraftControl, "GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32, 0, 3);

            connection.RegisterDataDefineStruct<AirCraftControlSimVars>(DEFINITIONS.AirCraftControl);
            AirCraftControlSimVars controlSimVarsStruct;
            AirCraftControl_EventData controlSimEventStruct;

            //build the return data schema - we'll use this data to determine if we should toggle lights (or other events) or not.
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_STROBE).Replace("_"," "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_BEACON).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_CABIN_LIGHTS).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_LANDING).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_NAV).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_PANEL).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);
            connection.AddToDataDefinition(DEFINITIONS.NonSettableData, nameof(lightStruct.LIGHT_TAXI).Replace("_", " "), "bool", SIMCONNECT_DATATYPE.INT32, 0, 0);

            connection.RegisterDataDefineStruct<NonSettableData>(DEFINITIONS.NonSettableData);
            
            //map all events to events with same names
            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_MASTER_ALTERNATOR, nameof(EVENTS.TOGGLE_MASTER_ALTERNATOR));
            connection.MapClientEventToSimEvent(EVENTS.STROBES_TOGGLE, nameof(EVENTS.STROBES_TOGGLE));
            connection.MapClientEventToSimEvent(EVENTS.LANDING_LIGHTS_TOGGLE, nameof(EVENTS.LANDING_LIGHTS_TOGGLE));
            connection.MapClientEventToSimEvent(EVENTS.PANEL_LIGHTS_TOGGLE, nameof(EVENTS.PANEL_LIGHTS_TOGGLE));
            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_BEACON_LIGHTS, nameof(EVENTS.TOGGLE_BEACON_LIGHTS));
            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_CABIN_LIGHTS, nameof(EVENTS.TOGGLE_CABIN_LIGHTS));
            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_NAV_LIGHTS, nameof(EVENTS.TOGGLE_NAV_LIGHTS));
            connection.MapClientEventToSimEvent(EVENTS.TOGGLE_TAXI_LIGHTS, nameof(EVENTS.TOGGLE_TAXI_LIGHTS));

            connection.MapClientEventToSimEvent(EVENTS.ALL_LIGHTS_TOGGLE, nameof(EVENTS.ALL_LIGHTS_TOGGLE));

            connection.MapClientEventToSimEvent(EVENTS.THROTTLE_SET, nameof(EVENTS.THROTTLE_SET));


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

                    controlSimVarsStruct.aileron = ran.Next(-1, 2) * ran.NextDouble();
                    controlSimVarsStruct.elevator = ran.Next(-1, 2) * ran.NextDouble();
                    controlSimVarsStruct.master_battery = ran.Next(2) == 0 ? false : true;
                    controlSimVarsStruct.gear_position = ran.Next(2) == 0 ? false : true;
                    controlSimEventStruct.throttlePos = (uint)ran.Next(0, 16383);
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



                        connection.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.THROTTLE_SET, controlSimEventStruct.throttlePos, groups.group1, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

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

#if DEBUG
            //create a modal dialog for testing our panel
            //without actual serial coms.
            form.Controls.Add(checkbox);
            form.ShowDialog();
#else
            task.Wait();
#endif

        }



        private static void Connection_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            Debug.WriteLine("recieving data from sim");
            lightStruct = (NonSettableData)data.dwData[0];
            
        }
    }
}
