#include <stddef.h>
#include <stdbool.h>
#include <stdlib.h>
#include <iostream>
#include <cstring>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>

#define RPC_GATEWAY_SERVICE_PORT 1471

using namespace std;

int main(int argc, char ** argv) {
   int nRPCClientSocket = -1;
   struct sockaddr_in oRPCServiceSocket;

   try {
      if (argc >= 3) {
         nRPCClientSocket = socket(AF_INET, SOCK_STREAM, 0);
         if (nRPCClientSocket != -1) {
            oRPCServiceSocket.sin_addr.s_addr = inet_addr(argv[1]);
            oRPCServiceSocket.sin_family = AF_INET;
            oRPCServiceSocket.sin_port = htons(RPC_GATEWAY_SERVICE_PORT);

            struct timeval tv;
            tv.tv_sec = 30;
            tv.tv_usec = 0;
	         setsockopt(nRPCClientSocket, SOL_SOCKET, SO_RCVTIMEO, (const char*) &tv, sizeof tv);

            if (connect(nRPCClientSocket, (struct sockaddr *) &oRPCServiceSocket, sizeof(oRPCServiceSocket)) >= 0) {
               string sCommand = "";
               for(int i = 2; i < argc; i++) {
                  if (i == 2) sCommand = string(argv[i]);
                  else sCommand += " " + string(argv[i]);
               }
               
               sCommand += "<EOC>";

               if (send(nRPCClientSocket, sCommand.c_str(), strlen(sCommand.c_str()), 0) >= 0) {
                  char sRPCServiceSocketData[32768];
		            string sRPCServiceSocketDataOutput = "";

                  bool bRPCServiceSocketDataAllCommand = false;
                  for(;!bRPCServiceSocketDataAllCommand;) {
                     try {
                        int nRPCServiceSocketBytesRecieve = recv(nRPCClientSocket, sRPCServiceSocketData, 32768, 0);
                        sRPCServiceSocketDataOutput += string(sRPCServiceSocketData).substr(0, nRPCServiceSocketBytesRecieve);
                        
			               if (sRPCServiceSocketDataOutput.find("<EOC>") != string::npos) {
                           bRPCServiceSocketDataAllCommand = true;
                        }
                     } catch (exception& oException) {
                        bRPCServiceSocketDataAllCommand = true;
                     }
                  }

                  if (sRPCServiceSocketDataOutput.find("<EOC>") != string::npos) {
                     sRPCServiceSocketDataOutput.erase(sRPCServiceSocketDataOutput.find("<EOC>"), strlen("<EOC>"));
                     cout << sRPCServiceSocketDataOutput;
                  }
                  else cout << sRPCServiceSocketDataOutput;
               }
               else cout << "[ ERROR ] El cliente no ha podido enviar el comando " << sCommand << " al socket remoto.\r\n" << endl; 
               
               close(nRPCClientSocket);
            }
            else cout << "[ ERROR ] El cliente no ha podido establecer conexion con la direccion IP remota: " << argv[1] << " y puerto: " << RPC_GATEWAY_SERVICE_PORT << "\r\n" << endl;
         }
         else cout << "[ ERROR ] El cliente no ha podido crear el socket\r\n" << endl;
      } 
      else cout << "[ ERROR ] El numero de argumentos es incorrecto.\r\n\n\t* Argumentos: IP_Address Command\r\n" << endl;
   } catch(exception& oException) {
      if (nRPCClientSocket != -1) close(nRPCClientSocket);

      cout << oException.what() << endl;  
   }
}