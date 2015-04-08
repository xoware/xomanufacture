#include "mnotftp.h"


#include <stdio.h>
#include <winsock2.h>
#include <process.h>
#include <time.h>
#include <tchar.h>
#include <ws2tcpip.h>
#include <limits.h>
#include <iphlpapi.h>
#include <math.h>
#include "opentftpds.h"


Cpp::UpdateCallback PrintCB;
void mnotftp_main()
{
	OSVERSIONINFO osvi;
	osvi.dwOSVersionInfoSize = sizeof(osvi);
	bool result = GetVersionEx(&osvi);

	runProg();
}

void  Cpp::TftpMT::nRun(Cpp::UpdateCallback callback) {
	char *messg = "Hello From Native World";
	//printf("pointer is : 0x%p\n", messg);
	//callback(messg);
	PrintCB = callback;
	mnotftp_main();
}

// TFTPServer.cpp



//Global Variables
char serviceName[] = "TFTPServer";
char displayName[] = "Open TFTP Server, MultiThreaded";
char sVersion[] = "Open TFTP Server MultiThreaded Version 1.64 Windows Built 2001";
char iniFile[_MAX_PATH];
char logFile[_MAX_PATH];
char lnkFile[_MAX_PATH];
char tempbuff[256];
char extbuff[256];
char logBuff[512];
char fileSep = '\\';
char notFileSep = '/';
MYWORD blksize = 65464;
char verbatim = 0;
MYWORD timeout = 3;
MYWORD loggingDay;
data1 network;
data1 newNetwork;
data2 cfig;
//ThreadPool Variables
HANDLE tEvent;
HANDLE cEvent;
HANDLE sEvent;
HANDLE lEvent;
MYBYTE currentServer = UCHAR_MAX;
MYWORD totalThreads = 0;
MYWORD minThreads = 1;
MYWORD activeThreads = 0;

//Service Variables
SERVICE_STATUS serviceStatus;
SERVICE_STATUS_HANDLE serviceStatusHandle = 0;
HANDLE stopServiceEvent = 0;



void runProg()
{
	verbatim = true;

	if (_beginthread(init, 0, 0) == 0)
	{
		if (cfig.logLevel)
		{
			sprintf(logBuff, "Thread Creation Failed");
			logMess(logBuff, 1);
		}
		exit(-1);
	}

	fd_set readfds;
	timeval tv;
	int fdsReady = 0;
	tv.tv_sec = 20;
	tv.tv_usec = 0;

	printf("\naccepting requests..\n");

	do
	{
		network.busy = false;

		//printf("Active=%u Total=%u\n",activeThreads, totalThreads);

		if (!network.tftpConn[0].ready || !network.ready)
		{
			Sleep(1000);
			continue;
		}

		FD_ZERO(&readfds);

		for (int i = 0; i < MAX_SERVERS && network.tftpConn[i].ready; i++)
			FD_SET(network.tftpConn[i].sock, &readfds);

		fdsReady = select(network.maxFD, &readfds, NULL, NULL, &tv);

		if (!network.ready)
			continue;

		//errno = WSAGetLastError();

		//if (errno)
		//	printf("%d\n", errno);

		for (int i = 0; fdsReady > 0 && i < MAX_SERVERS && network.tftpConn[i].ready; i++)
		{
			if (network.ready)
			{
				network.busy = true;

				if (FD_ISSET(network.tftpConn[i].sock, &readfds))
				{
					//printf("%d Requests Waiting\n", fdsReady);

					WaitForSingleObject(sEvent, INFINITE);

					currentServer = i;

					if (!totalThreads || activeThreads >= totalThreads)
					{
						_beginthread(
							processRequest,             	// thread function
							0,                        	// default security attributes
							NULL);          				// argument to thread function
					}
					SetEvent(tEvent);

					//printf("thread signalled=%u\n",SetEvent(tEvent));

					WaitForSingleObject(sEvent, INFINITE);
					fdsReady--;
					SetEvent(sEvent);
				}
			}
		}
	} while (true);

	closeConn();

	WSACleanup();
}

void closeConn()
{
	for (int i = 0; i < MAX_SERVERS && network.tftpConn[i].loaded; i++)
		if (network.tftpConn[i].ready)
			closesocket(network.tftpConn[i].sock);
}

void processRequest(void *lpParam)
{
	//printf("New Thread %u\n",GetCurrentThreadId());

	request req;

	WaitForSingleObject(cEvent, INFINITE);
	totalThreads++;
	SetEvent(cEvent);

	do
	{
		WaitForSingleObject(tEvent, INFINITE);
		//printf("In Thread %u\n",GetCurrentThreadId());

		WaitForSingleObject(cEvent, INFINITE);
		activeThreads++;
		SetEvent(cEvent);

		if (currentServer >= MAX_SERVERS || !network.tftpConn[currentServer].port)
		{
			SetEvent(sEvent);
			req.attempt = UCHAR_MAX;
			continue;
		}

		memset(&req, 0, sizeof(request));
		req.sock = INVALID_SOCKET;

		req.clientsize = sizeof(req.client);
		req.sockInd = currentServer;
		currentServer = UCHAR_MAX;
		req.knock = network.tftpConn[req.sockInd].sock;

		if (req.knock == INVALID_SOCKET)
		{
			SetEvent(sEvent);
			req.attempt = UCHAR_MAX;
			continue;
		}

		errno = 0;
		req.bytesRecd = recvfrom(req.knock, (char*)&req.mesin, sizeof(message), 0, (sockaddr*)&req.client, &req.clientsize);
		errno = WSAGetLastError();

		//printf("socket Signalled=%u\n",SetEvent(sEvent));
		SetEvent(sEvent);

		if (!errno && req.bytesRecd > 0)
		{
			if (cfig.hostRanges[0].rangeStart)
			{
				MYDWORD iip = ntohl(req.client.sin_addr.s_addr);
				bool allowed = false;

				for (int j = 0; j <= 32 && cfig.hostRanges[j].rangeStart; j++)
				{
					if (iip >= cfig.hostRanges[j].rangeStart && iip <= cfig.hostRanges[j].rangeEnd)
					{
						allowed = true;
						break;
					}
				}

				if (!allowed)
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(2);
					strcpy(req.serverError.errormessage, "Access Denied");
					logMess(&req, 1);
					sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
					req.attempt = UCHAR_MAX;
					continue;
				}
			}

			if ((htons(req.mesin.opcode) == 5))
			{
				sprintf(req.serverError.errormessage, "Error Code %i at Client, %s", ntohs(req.clientError.errorcode), req.clientError.errormessage);
				logMess(&req, 2);
				req.attempt = UCHAR_MAX;
				continue;
			}
			else if (htons(req.mesin.opcode) != 1 && htons(req.mesin.opcode) != 2)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(5);
				sprintf(req.serverError.errormessage, "Unknown Transfer Id");
				logMess(&req, 2);
				sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
				req.attempt = UCHAR_MAX;
				continue;
			}
		}
		else
		{
			sprintf(req.serverError.errormessage, "Communication Error");
			logMess(&req, 1);
			req.attempt = UCHAR_MAX;
			continue;
		}

		req.blksize = 512;
		req.timeout = timeout;
		req.expiry = time(NULL) + req.timeout;
		bool fetchAck = false;

		req.sock = socket(PF_INET, SOCK_DGRAM, IPPROTO_UDP);

		if (req.sock == INVALID_SOCKET)
		{
			req.serverError.opcode = htons(5);
			req.serverError.errorcode = htons(0);
			strcpy(req.serverError.errormessage, "Thread Socket Creation Error");
			sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
			logMess(&req, 1);
			req.attempt = UCHAR_MAX;
			continue;
		}

		sockaddr_in service;
		service.sin_family = AF_INET;
		service.sin_addr.s_addr = network.tftpConn[req.sockInd].server;

		if (cfig.minport)
		{
			for (MYWORD comport = cfig.minport;; comport++)
			{
				service.sin_port = htons(comport);

				if (comport > cfig.maxport)
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(0);
					strcpy(req.serverError.errormessage, "No port is free");
					sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
					logMess(&req, 1);
					req.attempt = UCHAR_MAX;
					break;
				}
				else if (bind(req.sock, (sockaddr*)&service, sizeof(service)) == -1)
					continue;
				else
					break;
			}
		}
		else
		{
			service.sin_port = 0;

			if (bind(req.sock, (sockaddr*)&service, sizeof(service)) == -1)
			{
				strcpy(req.serverError.errormessage, "Thread failed to bind");
				sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
			}
		}

		if (req.attempt >= 3)
			continue;

		if (connect(req.sock, (sockaddr*)&req.client, req.clientsize) == -1)
		{
			req.serverError.opcode = htons(5);
			req.serverError.errorcode = htons(0);
			strcpy(req.serverError.errormessage, "Connect Failed");
			sendto(req.knock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0, (sockaddr*)&req.client, req.clientsize);
			logMess(&req, 1);
			req.attempt = UCHAR_MAX;
			continue;
		}

		//sprintf(req.serverError.errormessage, "In Temp, Socket");
		//logMess(&req, 1);

		char *inPtr = req.mesin.buffer;
		*(inPtr + (req.bytesRecd - 3)) = 0;
		req.filename = inPtr;

		if (!strlen(req.filename) || strlen(req.filename) > UCHAR_MAX)
		{
			req.serverError.opcode = htons(5);
			req.serverError.errorcode = htons(4);
			strcpy(req.serverError.errormessage, "Malformed Request, Invalid/Missing Filename");
			send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
			req.attempt = UCHAR_MAX;
			logMess(&req, 1);
			continue;
		}

		inPtr += strlen(inPtr) + 1;
		req.mode = inPtr;

		if (!strlen(req.mode) || strlen(req.mode) > 25)
		{
			req.serverError.opcode = htons(5);
			req.serverError.errorcode = htons(4);
			strcpy(req.serverError.errormessage, "Malformed Request, Invalid/Missing Mode");
			send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
			req.attempt = UCHAR_MAX;
			logMess(&req, 1);
			continue;
		}

		inPtr += strlen(inPtr) + 1;

		for (MYDWORD i = 0; i < strlen(req.filename); i++)
			if (req.filename[i] == notFileSep)
				req.filename[i] = fileSep;

		tempbuff[0] = '.';
		tempbuff[1] = '.';
		tempbuff[2] = fileSep;
		tempbuff[3] = 0;

		if (strstr(req.filename, tempbuff))
		{
			req.serverError.opcode = htons(5);
			req.serverError.errorcode = htons(2);
			strcpy(req.serverError.errormessage, "Access violation");
			send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
			logMess(&req, 1);
			req.attempt = UCHAR_MAX;
			continue;
		}

		if (req.filename[0] == fileSep)
			req.filename++;

		if (!cfig.homes[0].alias[0])
		{
			if (strlen(cfig.homes[0].target) + strlen(req.filename) >= sizeof(req.path))
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(4);
				sprintf(req.serverError.errormessage, "Filename too large");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			strcpy(req.path, cfig.homes[0].target);
			strcat(req.path, req.filename);
		}
		else
		{
			char *bname = strchr(req.filename, fileSep);

			if (bname)
			{
				*bname = 0;
				bname++;
			}
			else
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				sprintf(req.serverError.errormessage, "Missing directory/alias");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			for (int i = 0; i < 8; i++)
			{
				//printf("%s=%i\n", req.filename, cfig.homes[i].alias[0]);
				if (cfig.homes[i].alias[0] && !strcasecmp(req.filename, cfig.homes[i].alias))
				{
					if (strlen(cfig.homes[i].target) + strlen(bname) >= sizeof(req.path))
					{
						req.serverError.opcode = htons(5);
						req.serverError.errorcode = htons(4);
						sprintf(req.serverError.errormessage, "Filename too large");
						send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}

					strcpy(req.path, cfig.homes[i].target);
					strcat(req.path, bname);
					break;
				}
				else if (i == 7 || !cfig.homes[i].alias[0])
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(2);
					sprintf(req.serverError.errormessage, "No such directory/alias %s", req.filename);
					send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
					logMess(&req, 1);
					req.attempt = UCHAR_MAX;
					break;
				}
			}
		}

		if (req.attempt >= 3)
			continue;

		if (ntohs(req.mesin.opcode) == 1)
		{
			if (!cfig.fileRead)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "GET Access Denied");
				logMess(&req, 1);
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				req.attempt = UCHAR_MAX;
				continue;
			}

			if (*inPtr)
			{
				char *tmp = inPtr;

				while (*tmp)
				{
					if (!strcasecmp(tmp, "blksize"))
					{
						tmp += strlen(tmp) + 1;
						MYDWORD val = atol(tmp);

						if (val < 512)
							val = 512;
						else if (val > blksize)
							val = blksize;

						req.blksize = val;
						break;
					}

					tmp += strlen(tmp) + 1;
				}
			}

			errno = 0;

			if (!strcasecmp(req.mode, "netascii") || !strcasecmp(req.mode, "ascii"))
				req.file = fopen(req.path, "rt");
			else
				req.file = fopen(req.path, "rb");

			if (errno || !req.file)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(1);
				strcpy(req.serverError.errormessage, "File not found or No Access");
				logMess(&req, 1);
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				req.attempt = UCHAR_MAX;
				continue;
			}
		}
		else
		{
			if (!cfig.fileWrite && !cfig.fileOverwrite)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "PUT Access Denied");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			req.file = fopen(req.path, "rb");

			if (req.file)
			{
				fclose(req.file);
				req.file = NULL;

				if (!cfig.fileOverwrite)
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(6);
					strcpy(req.serverError.errormessage, "File already exists");
					send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
					logMess(&req, 1);
					req.attempt = UCHAR_MAX;
					continue;
				}
			}
			else if (!cfig.fileWrite)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "Create File Access Denied");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			errno = 0;

			if (!strcasecmp(req.mode, "netascii") || !strcasecmp(req.mode, "ascii"))
				req.file = fopen(req.path, "wt");
			else
				req.file = fopen(req.path, "wb");

			if (errno || !req.file)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "Invalid Path or No Access");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}
		}

		setvbuf(req.file, NULL, _IOFBF, 5 * req.blksize);

		if (*inPtr)
		{
			fetchAck = true;
			char *outPtr = req.mesout.buffer;
			req.mesout.opcode = htons(6);
			MYDWORD val;
			while (*inPtr)
			{
				//printf("%s\n", inPtr);
				if (!strcasecmp(inPtr, "blksize"))
				{
					strcpy(outPtr, inPtr);
					outPtr += strlen(outPtr) + 1;
					inPtr += strlen(inPtr) + 1;
					val = atol(inPtr);

					if (val < 512)
						val = 512;
					else if (val > blksize)
						val = blksize;

					req.blksize = val;
					sprintf(outPtr, "%u", val);
					outPtr += strlen(outPtr) + 1;
				}
				else if (!strcasecmp(inPtr, "tsize"))
				{
					strcpy(outPtr, inPtr);
					outPtr += strlen(outPtr) + 1;
					inPtr += strlen(inPtr) + 1;

					if (ntohs(req.mesin.opcode) == 1)
					{
						if (!fseek(req.file, 0, SEEK_END))
						{
							if (ftell(req.file) >= 0)
							{
								req.tsize = ftell(req.file);
								sprintf(outPtr, "%u", req.tsize);
								outPtr += strlen(outPtr) + 1;
							}
							else
							{
								req.serverError.opcode = htons(5);
								req.serverError.errorcode = htons(2);
								strcpy(req.serverError.errormessage, "Invalid Path or No Access");
								send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
								logMess(&req, 1);
								req.attempt = UCHAR_MAX;
								break;
							}
						}
						else
						{
							req.serverError.opcode = htons(5);
							req.serverError.errorcode = htons(2);
							strcpy(req.serverError.errormessage, "Invalid Path or No Access");
							send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
							logMess(&req, 1);
							req.attempt = UCHAR_MAX;
							break;
						}
					}
					else
					{
						req.tsize = 0;
						sprintf(outPtr, "%u", req.tsize);
						outPtr += strlen(outPtr) + 1;
					}
				}
				else if (!strcasecmp(inPtr, "timeout"))
				{
					strcpy(outPtr, inPtr);
					outPtr += strlen(outPtr) + 1;
					inPtr += strlen(inPtr) + 1;
					val = atoi(inPtr);

					if (val < 1)
						val = 1;
					else if (val > UCHAR_MAX)
						val = UCHAR_MAX;

					req.timeout = val;
					req.expiry = time(NULL) + req.timeout;
					sprintf(outPtr, "%u", val);
					outPtr += strlen(outPtr) + 1;
				}

				inPtr += strlen(inPtr) + 1;
				//printf("=%u\n", val);
			}

			if (req.attempt >= 3)
				continue;

			errno = 0;
			req.bytesReady = (MYDWORD)outPtr - (MYDWORD)&req.mesout;
			//printf("Bytes Ready=%u\n", req.bytesReady);
			send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
			errno = WSAGetLastError();
		}
		else if (htons(req.mesin.opcode) == 2)
		{
			req.acout.opcode = htons(4);
			req.acout.block = htons(0);
			errno = 0;
			req.bytesReady = 4;
			send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
			errno = WSAGetLastError();
		}

		if (errno)
		{
			sprintf(req.serverError.errormessage, "Communication Error");
			logMess(&req, 1);
			req.attempt = UCHAR_MAX;
			continue;
		}
		else if (ntohs(req.mesin.opcode) == 1)
		{
			errno = 0;
			req.pkt[0] = (packet*)calloc(1, req.blksize + 4);
			req.pkt[1] = (packet*)calloc(1, req.blksize + 4);

			if (errno || !req.pkt[0] || !req.pkt[1])
			{
				sprintf(req.serverError.errormessage, "Memory Error");
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			long ftellLoc = ftell(req.file);

			if (ftellLoc > 0)
			{
				if (fseek(req.file, 0, SEEK_SET))
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(2);
					strcpy(req.serverError.errormessage, "File Access Error");
					send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
					logMess(&req, 1);
					req.attempt = UCHAR_MAX;
					continue;
				}
			}
			else if (ftellLoc < 0)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "File Access Error");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			errno = 0;
			req.pkt[0]->opcode = htons(3);
			req.pkt[0]->block = htons(1);
			req.bytesRead[0] = fread(&req.pkt[0]->buffer, 1, req.blksize, req.file);

			if (errno)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "Invalid Path or No Access");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			if (req.bytesRead[0] == req.blksize)
			{
				req.pkt[1]->opcode = htons(3);
				req.pkt[1]->block = htons(2);
				req.bytesRead[1] = fread(&req.pkt[1]->buffer, 1, req.blksize, req.file);
				if (req.bytesRead[1] < req.blksize)
				{
					fclose(req.file);
					req.file = 0;
				}
			}
			else
			{
				fclose(req.file);
				req.file = 0;
			}

			if (errno)
			{
				req.serverError.opcode = htons(5);
				req.serverError.errorcode = htons(2);
				strcpy(req.serverError.errormessage, "Invalid Path or No Access");
				send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			while (req.attempt <= 3)
			{
				if (fetchAck)
				{
					FD_ZERO(&req.readfds);
					req.tv.tv_sec = 1;
					req.tv.tv_usec = 0;
					FD_SET(req.sock, &req.readfds);
					select(req.sock + 1, &req.readfds, NULL, NULL, &req.tv);

					if (FD_ISSET(req.sock, &req.readfds))
					{
						errno = 0;
						req.bytesRecd = recv(req.sock, (char*)&req.mesin, sizeof(message), 0);
						errno = WSAGetLastError();
						if (req.bytesRecd <= 0 || errno)
						{
							sprintf(req.serverError.errormessage, "Communication Error");
							logMess(&req, 1);
							req.attempt = UCHAR_MAX;
							break;
						}
						else if (req.bytesRecd >= 4 && ntohs(req.mesin.opcode) == 4)
						{
							if (ntohs(req.acin.block) == req.block)
							{
								req.block++;
								req.fblock++;
								req.attempt = 0;
							}
							else if (req.expiry > time(NULL))
								continue;
							else
								req.attempt++;
						}
						else if (ntohs(req.mesin.opcode) == 5)
						{
							sprintf(req.serverError.errormessage, "Client %s:%u, Error Code %i at Client, %s", inet_ntoa(req.client.sin_addr), ntohs(req.client.sin_port), ntohs(req.clientError.errorcode), req.clientError.errormessage);
							logMess(&req, 1);
							req.attempt = UCHAR_MAX;
							break;
						}
						else
						{
							req.serverError.opcode = htons(5);
							req.serverError.errorcode = htons(4);
							sprintf(req.serverError.errormessage, "Unexpected Option Code %i", ntohs(req.mesin.opcode));
							send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
							logMess(&req, 1);
							req.attempt = UCHAR_MAX;
							break;
						}
					}
					else if (req.expiry > time(NULL))
						continue;
					else
						req.attempt++;
				}
				else
				{
					fetchAck = true;
					req.acin.block = 1;
					req.block = 1;
					req.fblock = 1;
				}

				if (req.attempt >= 3)
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(0);

					if (req.fblock && !req.block)
						strcpy(req.serverError.errormessage, "Large File, Block# Rollover not supported by Client");
					else
						strcpy(req.serverError.errormessage, "Timeout");

					logMess(&req, 1);
					send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
					req.attempt = UCHAR_MAX;
					break;
				}
				else if (!req.fblock)
				{
					errno = 0;
					send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
					errno = WSAGetLastError();
					if (errno)
					{
						sprintf(req.serverError.errormessage, "Communication Error");
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
					req.expiry = time(NULL) + req.timeout;
				}
				else if (ntohs(req.pkt[0]->block) == req.block)
				{
					errno = 0;
					send(req.sock, (const char*)req.pkt[0], req.bytesRead[0] + 4, 0);
					errno = WSAGetLastError();
					if (errno)
					{
						sprintf(req.serverError.errormessage, "Communication Error");
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
					req.expiry = time(NULL) + req.timeout;

					if (req.file)
					{
						req.tblock = ntohs(req.pkt[1]->block) + 1;
						if (req.tblock == req.block)
						{
							req.pkt[1]->block = htons(++req.tblock);
							req.bytesRead[1] = fread(&req.pkt[1]->buffer, 1, req.blksize, req.file);

							if (errno)
							{
								req.serverError.opcode = htons(5);
								req.serverError.errorcode = htons(4);
								sprintf(req.serverError.errormessage, strerror(errno));
								send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
								logMess(&req, 1);
								req.attempt = UCHAR_MAX;
								break;
							}
							else if (req.bytesRead[1] < req.blksize)
							{
								fclose(req.file);
								req.file = 0;
							}
						}
					}
				}
				else if (ntohs(req.pkt[1]->block) == req.block)
				{
					errno = 0;
					send(req.sock, (const char*)req.pkt[1], req.bytesRead[1] + 4, 0);
					errno = WSAGetLastError();
					if (errno)
					{
						sprintf(req.serverError.errormessage, "Communication Error");
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}

					req.expiry = time(NULL) + req.timeout;

					if (req.file)
					{
						req.tblock = ntohs(req.pkt[0]->block) + 1;
						if (req.tblock == req.block)
						{
							req.pkt[0]->block = htons(++req.tblock);
							req.bytesRead[0] = fread(&req.pkt[0]->buffer, 1, req.blksize, req.file);
							if (errno)
							{
								req.serverError.opcode = htons(5);
								req.serverError.errorcode = htons(4);
								sprintf(req.serverError.errormessage, strerror(errno));
								send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
								logMess(&req, 1);
								req.attempt = UCHAR_MAX;
								break;
							}
							else if (req.bytesRead[0] < req.blksize)
							{
								fclose(req.file);
								req.file = 0;
							}
						}
					}
				}
				else
				{
					sprintf(req.serverError.errormessage, "%u Blocks Served", req.fblock - 1);
//					logMess(&req, 2);
					cbMess(&req, 2);
					req.attempt = UCHAR_MAX;
					break;
				}
			}
		}
		else if (ntohs(req.mesin.opcode) == 2)
		{
			errno = 0;
			req.pkt[0] = (packet*)calloc(1, req.blksize + 4);

			if (errno || !req.pkt[0])
			{
				sprintf(req.serverError.errormessage, "Memory Error");
				logMess(&req, 1);
				req.attempt = UCHAR_MAX;
				continue;
			}

			while (req.attempt <= 3)
			{
				FD_ZERO(&req.readfds);
				req.tv.tv_sec = 1;
				req.tv.tv_usec = 0;
				FD_SET(req.sock, &req.readfds);
				select(req.sock + 1, &req.readfds, NULL, NULL, &req.tv);

				if (FD_ISSET(req.sock, &req.readfds))
				{
					errno = 0;
					req.bytesRecd = recv(req.sock, (char*)req.pkt[0], req.blksize + 4, 0);
					errno = WSAGetLastError();

					if (errno)
					{
						sprintf(req.serverError.errormessage, "Communication Error");
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
				}
				else
					req.bytesRecd = 0;

				if (req.bytesRecd >= 4)
				{
					if (ntohs(req.pkt[0]->opcode) == 3)
					{
						req.tblock = req.block + 1;

						if (ntohs(req.pkt[0]->block) == req.tblock)
						{
							req.acout.opcode = htons(4);
							req.acout.block = req.pkt[0]->block;
							req.block++;
							req.fblock++;
							req.bytesReady = 4;
							req.expiry = time(NULL) + req.timeout;

							errno = 0;
							send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
							errno = WSAGetLastError();

							if (errno)
							{
								sprintf(req.serverError.errormessage, "Communication Error");
								logMess(&req, 1);
								req.attempt = UCHAR_MAX;
								break;
							}

							if (req.bytesRecd > 4)
							{
								errno = 0;
								if (fwrite(&req.pkt[0]->buffer, req.bytesRecd - 4, 1, req.file) != 1 || errno)
								{
									req.serverError.opcode = htons(5);
									req.serverError.errorcode = htons(3);
									strcpy(req.serverError.errormessage, "Disk full or allocation exceeded");
									send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
									logMess(&req, 1);
									req.attempt = UCHAR_MAX;
									break;
								}
								else
									req.attempt = 0;
							}
							else
								req.attempt = 0;

							if ((MYWORD)req.bytesRecd < req.blksize + 4)
							{
								fclose(req.file);
								req.file = 0;
								sprintf(req.serverError.errormessage, "%u Blocks Received", req.fblock);
								logMess(&req, 2);
								req.attempt = UCHAR_MAX;
								break;
							}
						}
						else if (req.expiry > time(NULL))
							continue;
						else if (req.attempt >= 3)
						{
							req.serverError.opcode = htons(5);
							req.serverError.errorcode = htons(0);

							if (req.fblock && !req.block)
								strcpy(req.serverError.errormessage, "Large File, Block# Rollover not supported by Client");
							else
								strcpy(req.serverError.errormessage, "Timeout");

							logMess(&req, 1);
							send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
							req.attempt = UCHAR_MAX;
							break;
						}
						else
						{
							req.expiry = time(NULL) + req.timeout;
							errno = 0;
							send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
							errno = WSAGetLastError();
							req.attempt++;

							if (errno)
							{
								sprintf(req.serverError.errormessage, "Communication Error");
								logMess(&req, 1);
								req.attempt = UCHAR_MAX;
								break;
							}
						}
					}
					else if (req.bytesRecd > (int)sizeof(message))
					{
						req.serverError.opcode = htons(5);
						req.serverError.errorcode = htons(4);
						sprintf(req.serverError.errormessage, "Error: Incoming Packet too large");
						send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
					else if (ntohs(req.pkt[0]->opcode) == 5)
					{
						sprintf(req.serverError.errormessage, "Error Code %i at Client, %s", ntohs(req.pkt[0]->block), &req.pkt[0]->buffer);
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
					else
					{
						req.serverError.opcode = htons(5);
						req.serverError.errorcode = htons(4);
						sprintf(req.serverError.errormessage, "Unexpected Option Code %i", ntohs(req.pkt[0]->opcode));
						send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
				}
				else if (req.expiry > time(NULL))
					continue;
				else if (req.attempt >= 3)
				{
					req.serverError.opcode = htons(5);
					req.serverError.errorcode = htons(0);

					if (req.fblock && !req.block)
						strcpy(req.serverError.errormessage, "Large File, Block# Rollover not supported by Client");
					else
						strcpy(req.serverError.errormessage, "Timeout");

					logMess(&req, 1);
					send(req.sock, (const char*)&req.serverError, strlen(req.serverError.errormessage) + 5, 0);
					req.attempt = UCHAR_MAX;
					break;
				}
				else
				{
					req.expiry = time(NULL) + req.timeout;
					errno = 0;
					send(req.sock, (const char*)&req.mesout, req.bytesReady, 0);
					errno = WSAGetLastError();
					req.attempt++;

					if (errno)
					{
						sprintf(req.serverError.errormessage, "Communication Error");
						logMess(&req, 1);
						req.attempt = UCHAR_MAX;
						break;
					}
				}
			}
		}
	} while (cleanReq(&req));

	WaitForSingleObject(cEvent, INFINITE);
	totalThreads--;
	SetEvent(cEvent);

	//printf("Thread %u Killed\n",GetCurrentThreadId());
	_endthread();
	return;
}

bool cleanReq(request* req)
{
	//printf("cleaning\n");

	if (req->file)
		fclose(req->file);

	if (!(req->sock == INVALID_SOCKET))
	{
		//printf("Here\n");
		closesocket(req->sock);
	}

	if (req->pkt[0])
		free(req->pkt[0]);

	if (req->pkt[1])
		free(req->pkt[1]);

	WaitForSingleObject(cEvent, INFINITE);
	activeThreads--;
	SetEvent(cEvent);

	//printf("cleaned\n");

	return (totalThreads <= minThreads);
}

char *IP2String(char *target, MYDWORD ip)
{
	data15 inaddr;
	inaddr.ip = ip;
	sprintf(target, "%u.%u.%u.%u", inaddr.octate[0], inaddr.octate[1], inaddr.octate[2], inaddr.octate[3]);
	return target;
}


void init(void *lpParam)
{
	memset(&cfig, 0, sizeof(cfig));

	GetModuleFileNameA(NULL, extbuff, _MAX_PATH);
	char *fileExt = strrchr(extbuff, '.');
	*fileExt = 0;
	sprintf(iniFile, "%s.ini", extbuff);
	sprintf(lnkFile, "%s.url", extbuff);
	fileExt = strrchr(extbuff, '\\');
	*fileExt = 0;
	fileExt++;
	sprintf(logFile, "%s\\log\\%s%%Y%%m%%d.log", extbuff, fileExt);

	FILE *f = NULL;
	char raw[512];
	char name[512];
	char value[512];

	if (verbatim)
	{
		cfig.logLevel = 2;
		printf("%s\n\n", sVersion);
	}


	MYWORD wVersionRequested = MAKEWORD(1, 1);
	WSAStartup(wVersionRequested, &cfig.wsaData);

	if (cfig.wsaData.wVersion != wVersionRequested)
	{
		sprintf(logBuff, "WSAStartup Error");
		logMess(logBuff, 1);
	}


	if (!cfig.homes[0].target[0])
	{
		GetModuleFileNameA(NULL, cfig.homes[0].target, UCHAR_MAX);
		char *iniFileExt = strrchr(cfig.homes[0].target, fileSep);
		*(++iniFileExt) = 0;
	}

	cfig.fileRead = true;


	{
		MYDWORD rs = 0;
		MYDWORD re = 0;
		rs = htonl(my_inet_addr("169.254.254.0"));
		re = htonl(my_inet_addr("169.254.254.255"));

		cfig.hostRanges[0].rangeStart = rs;
		cfig.hostRanges[0].rangeEnd = re;

	}


	if (verbatim)
	{
		printf("starting TFTP...\n");
	}
	else
	{
		sprintf(logBuff, "starting TFTP service");
		logMess(logBuff, 1);
	}

	for (int i = 0; i < MAX_SERVERS; i++)
		if (cfig.homes[i].target[0])
		{
			sprintf(logBuff, "alias /%s is mapped to %s", cfig.homes[i].alias, cfig.homes[i].target);
			logMess(logBuff, 1);
		}

	if (cfig.hostRanges[0].rangeStart)
	{
		char temp[128];

		for (MYWORD i = 0; i <= sizeof(cfig.hostRanges) && cfig.hostRanges[i].rangeStart; i++)
		{
			sprintf(logBuff, "%s", "permitted clients: ");
			sprintf(temp, "%s-", IP2String(tempbuff, htonl(cfig.hostRanges[i].rangeStart)));
			strcat(logBuff, temp);
			sprintf(temp, "%s", IP2String(tempbuff, htonl(cfig.hostRanges[i].rangeEnd)));
			strcat(logBuff, temp);
			logMess(logBuff, 1);
		}
	}
	else
	{
		sprintf(logBuff, "%s", "permitted clients: all");
		logMess(logBuff, 1);
	}

	if (cfig.minport)
	{
		sprintf(logBuff, "server port range: %u-%u", cfig.minport, cfig.maxport);
		logMess(logBuff, 1);
	}
	else
	{
		sprintf(logBuff, "server port range: all");
		logMess(logBuff, 1);
	}

	sprintf(logBuff, "max blksize: %u", blksize);
	logMess(logBuff, 1);
	sprintf(logBuff, "default blksize: %u", 512);
	logMess(logBuff, 1);
	sprintf(logBuff, "default timeout: %u", timeout);
	logMess(logBuff, 1);
	sprintf(logBuff, "file read allowed: %s", cfig.fileRead ? "Yes" : "No");
	logMess(logBuff, 1);
	sprintf(logBuff, "file create allowed: %s", cfig.fileWrite ? "Yes" : "No");
	logMess(logBuff, 1);
	sprintf(logBuff, "file overwrite allowed: %s", cfig.fileOverwrite ? "Yes" : "No");
	logMess(logBuff, 1);

	if (!verbatim)
	{
		sprintf(logBuff, "logging: %s", cfig.logLevel > 1 ? "all" : "errors");
		logMess(logBuff, 1);
	}

	lEvent = CreateEvent(
		NULL,                  // default security descriptor
		FALSE,                 // ManualReset
		TRUE,                  // Signalled
		TEXT("AchalTFTServerLogEvent"));  // object name

	if (lEvent == NULL)
	{
		printf("CreateEvent error: %d\n", GetLastError());
		exit(-1);
	}
	else if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		sprintf(logBuff, "CreateEvent opened an existing Event\nServer May already be Running");
		logMess(logBuff, 0);
		exit(-1);
	}

	tEvent = CreateEvent(
		NULL,                  // default security descriptor
		FALSE,                 // ManualReset
		FALSE,                 // Signalled
		TEXT("AchalTFTServerThreadEvent"));  // object name

	if (tEvent == NULL)
	{
		printf("CreateEvent error: %d\n", GetLastError());
		exit(-1);
	}
	else if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		sprintf(logBuff, "CreateEvent opened an existing Event\nServer May already be Running");
		logMess(logBuff, 0);
		exit(-1);
	}

	sEvent = CreateEvent(
		NULL,                  // default security descriptor
		FALSE,                 // ManualReset
		TRUE,                  // Signalled
		TEXT("AchalTFTServerSocketEvent"));  // object name

	if (sEvent == NULL)
	{
		printf("CreateEvent error: %d\n", GetLastError());
		exit(-1);
	}
	else if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		sprintf(logBuff, "CreateEvent opened an existing Event\nServer May already be Running");
		logMess(logBuff, 0);
		exit(-1);
	}

	cEvent = CreateEvent(
		NULL,                  // default security descriptor
		FALSE,                 // ManualReset
		TRUE,                  // Signalled
		TEXT("AchalTFTServerCountEvent"));  // object name

	if (cEvent == NULL)
	{
		printf("CreateEvent error: %d\n", GetLastError());
		exit(-1);
	}
	else if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		sprintf(logBuff, "CreateEvent opened an existing Event\nServer May already be Running");
		logMess(logBuff, 0);
		exit(-1);
	}

	if (minThreads)
	{
		for (int i = 0; i < minThreads; i++)
		{
			_beginthread(
				processRequest,             	// thread function
				0,                        	// default security attributes
				NULL);          				// argument to thread function
		}

		sprintf(logBuff, "thread pool size: %u", minThreads);
		logMess(logBuff, 1);
	}

	for (int i = 0; i < MAX_SERVERS && network.tftpConn[i].port; i++)
	{
		sprintf(logBuff, "listening on: %s:%i", IP2String(tempbuff, network.tftpConn[i].server), network.tftpConn[i].port);
		logMess(logBuff, 1);
	}

	do
	{
		memset(&newNetwork, 0, sizeof(data1));

		bool ifSpecified = false;
		bool bindfailed = false;



		if (!cfig.ifspecified)
		{
			sprintf(logBuff, "detecting Interfaces..");
			logMess(logBuff, 1);
			getInterfaces(&newNetwork);

			for (MYBYTE n = 0; n < MAX_SERVERS && newNetwork.staticServers[n]; n++)
			{
				newNetwork.listenServers[n] = newNetwork.staticServers[n];
				newNetwork.listenPorts[n] = 69;
			}
		}

		MYBYTE i = 0;

		for (int j = 0; j < MAX_SERVERS && newNetwork.listenPorts[j]; j++)
		{
			int k = 0;

			for (; k < MAX_SERVERS && network.tftpConn[k].loaded; k++)
			{
				if (network.tftpConn[k].ready && network.tftpConn[k].server == newNetwork.listenServers[j] && network.tftpConn[k].port == newNetwork.listenPorts[j])
					break;
			}

			if (network.tftpConn[k].ready && network.tftpConn[k].server == newNetwork.listenServers[j] && network.tftpConn[k].port == newNetwork.listenPorts[j])
			{
				memcpy(&(newNetwork.tftpConn[i]), &(network.tftpConn[k]), sizeof(tftpConnType));

				if (newNetwork.maxFD < newNetwork.tftpConn[i].sock)
					newNetwork.maxFD = newNetwork.tftpConn[i].sock;

				network.tftpConn[k].ready = false;
				//printf("%d, %s found\n", i, IP2String(tempbuff, newNetwork.tftpConn[i].server));
				i++;
				continue;
			}
			else
			{
				newNetwork.tftpConn[i].sock = socket(PF_INET, SOCK_DGRAM, IPPROTO_UDP);

				if (newNetwork.tftpConn[i].sock == INVALID_SOCKET)
				{
					bindfailed = true;
					sprintf(logBuff, "Failed to Create Socket");
					logMess(logBuff, 1);
					continue;
				}

				//printf("Socket %u\n", newNetwork.tftpConn[i].sock);

				errno = 0;
				newNetwork.tftpConn[i].addr.sin_family = AF_INET;
				newNetwork.tftpConn[i].addr.sin_addr.s_addr = newNetwork.listenServers[j];
				newNetwork.tftpConn[i].addr.sin_port = htons(newNetwork.listenPorts[j]);
				int nRet = bind(newNetwork.tftpConn[i].sock, (sockaddr*)&newNetwork.tftpConn[i].addr, sizeof(struct sockaddr_in));

				if (nRet == SOCKET_ERROR || errno)
				{
					bindfailed = true;
					closesocket(newNetwork.tftpConn[i].sock);
					sprintf(logBuff, "%s Port %i bind failed", IP2String(tempbuff, newNetwork.listenServers[j]), newNetwork.listenPorts[j]);
					logMess(logBuff, 1);
					continue;
				}

				newNetwork.tftpConn[i].loaded = true;
				newNetwork.tftpConn[i].ready = true;
				newNetwork.tftpConn[i].server = newNetwork.listenServers[j];
				newNetwork.tftpConn[i].port = newNetwork.listenPorts[j];

				//printf("%d, %s created\n", i, IP2String(tempbuff, newNetwork.tftpConn[i].server));

				if (newNetwork.maxFD < newNetwork.tftpConn[i].sock)
					newNetwork.maxFD = newNetwork.tftpConn[i].sock;

				if (!newNetwork.listenServers[j])
					break;

				i++;
			}
		}

		if (bindfailed)
			cfig.failureCount++;
		else
			cfig.failureCount = 0;

		closeConn();
		memcpy(&network, &newNetwork, sizeof(data1));

		//printf("%i %i %i\n", network.tftpConn[0].ready, network.dnsUdpConn[0].ready, network.dnsTcpConn[0].ready);

		if (!network.tftpConn[0].ready)
		{
			sprintf(logBuff, "No Static Interface ready, Waiting...");
			logMess(logBuff, 1);
			continue;
		}

		for (int i = 0; i < MAX_SERVERS && network.tftpConn[i].loaded; i++)
		{
			sprintf(logBuff, "Listening On: %s:%d", IP2String(tempbuff, network.tftpConn[i].server), network.tftpConn[i].port);
			logMess(logBuff, 1);
		}

		network.ready = true;

	} while (detectChange());

	//printf("Exiting Init\n");

	_endthread();
	return;
}

bool detectChange()
{
	if (!cfig.failureCount)
	{
		if (cfig.ifspecified)
			return false;
	}

	MYDWORD eventWait = UINT_MAX;

	if (cfig.failureCount)
		eventWait = 10000 * pow((double)2, (double)cfig.failureCount);

	OVERLAPPED overlap;
	MYDWORD ret;
	HANDLE hand = NULL;
	overlap.hEvent = WSACreateEvent();

	ret = NotifyAddrChange(&hand, &overlap);

	if (ret != NO_ERROR)
	{
		if (WSAGetLastError() != WSA_IO_PENDING)
		{
			printf("NotifyAddrChange error...%d\n", WSAGetLastError());
			return true;
		}
	}

	if (WaitForSingleObject(overlap.hEvent, eventWait) == WAIT_OBJECT_0)
		WSACloseEvent(overlap.hEvent);

	network.ready = false;

	while (network.busy)
		Sleep(1000);

	if (cfig.failureCount)
	{
		sprintf(logBuff, "Retrying failed Listening Interfaces..");
		logMess(logBuff, 1);
	}
	else
	{
		sprintf(logBuff, "Network changed, re-detecting Interfaces..");
		logMess(logBuff, 1);
	}

	return true;
}

void getInterfaces(data1 *network)
{
	memset(network, 0, sizeof(data1));

	SOCKET sd = WSASocket(PF_INET, SOCK_DGRAM, 0, 0, 0, 0);

	if (sd == INVALID_SOCKET)
		return;

	INTERFACE_INFO InterfaceList[MAX_SERVERS];
	unsigned long nBytesReturned;

	if (WSAIoctl(sd, SIO_GET_INTERFACE_LIST, 0, 0, &InterfaceList,
		sizeof(InterfaceList), &nBytesReturned, 0, 0) == SOCKET_ERROR)
		return;

	int nNumInterfaces = nBytesReturned / sizeof(INTERFACE_INFO);

	for (int i = 0; i < nNumInterfaces; ++i)
	{
		sockaddr_in *pAddress = (sockaddr_in*)&(InterfaceList[i].iiAddress);
		u_long nFlags = InterfaceList[i].iiFlags;

		if (pAddress->sin_addr.s_addr)
		{
			addServer(network->allServers, pAddress->sin_addr.s_addr);

			if (!(nFlags & IFF_POINTTOPOINT) && (nFlags & IFF_UP))
			{
				addServer(network->staticServers, pAddress->sin_addr.s_addr);
			}
		}
	}

	closesocket(sd);
}

bool addServer(MYDWORD *array, MYDWORD ip)
{
	for (MYBYTE i = 0; i < MAX_SERVERS; i++)
	{
		if (!ip || array[i] == ip)
			return 0;
		else if (!array[i])
		{
			array[i] = ip;
			return 1;
		}
	}
	return 0;
}

MYDWORD *findServer(MYDWORD *array, MYDWORD ip)
{
	if (ip)
	{
		for (MYBYTE i = 0; i < MAX_SERVERS && array[i]; i++)
		{
			if (array[i] == ip)
				return &(array[i]);
		}
	}
	return 0;
}

void logMess(char *logBuff, MYBYTE logLevel)
{
	return;

	WaitForSingleObject(lEvent, INFINITE);

	if (verbatim) {
		//printf("%s\n", logBuff);
		PrintCB(logBuff);
	}
	else if (cfig.logfile && logLevel <= cfig.logLevel)
	{
		time_t t = time(NULL);
		tm *ttm = localtime(&t);

		if (ttm->tm_yday != loggingDay)
		{
			loggingDay = ttm->tm_yday;
			strftime(extbuff, sizeof(extbuff), logFile, ttm);
			fprintf(cfig.logfile, "Logging Continued on file %s\n", extbuff);
			fclose(cfig.logfile);
			cfig.logfile = fopen(extbuff, "at");

			if (cfig.logfile)
			{
				fprintf(cfig.logfile, "%s\n\n", sVersion);
				WritePrivateProfileStringA("InternetShortcut", "URL", extbuff, lnkFile);
				WritePrivateProfileStringA("InternetShortcut", "IconIndex", "0", lnkFile);
				WritePrivateProfileStringA("InternetShortcut", "IconFile", extbuff, lnkFile);
			}
			else
				return;
		}

		strftime(extbuff, sizeof(extbuff), "%d-%b-%y %X", ttm);
		fprintf(cfig.logfile, "[%s] %s\n", extbuff, logBuff);
		fflush(cfig.logfile);
	}
	SetEvent(lEvent);
}

void cbMess(request *req, MYBYTE logLevel)
{
	WaitForSingleObject(lEvent, INFINITE);

	char tempbuff[256];
	char cbuffer[512];

	if (verbatim)
	{
		if (!req->serverError.errormessage[0])
			sprintf(req->serverError.errormessage, strerror(errno));

		if (req->path[0])
			_snprintf(cbuffer, 512, "Client %s:%u %s, %s\n", IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->path, req->serverError.errormessage);
		else
			_snprintf(cbuffer, 512, "Client %s:%u, %s\n", IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->serverError.errormessage);
		return;
		PrintCB(cbuffer);
	}
	SetEvent(lEvent);
}


void logMess(request *req, MYBYTE logLevel)
{
	return;

	WaitForSingleObject(lEvent, INFINITE);

	char tempbuff[256];
	char cbuffer[512];

	if (verbatim)
	{
		if (!req->serverError.errormessage[0])
			sprintf(req->serverError.errormessage, strerror(errno));

		if (req->path[0])
			_snprintf(cbuffer, 512, "Client %s:%u %s, %s\n", IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->path, req->serverError.errormessage);
		else
			_snprintf(cbuffer, 512, "Client %s:%u, %s\n", IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->serverError.errormessage);
		PrintCB(cbuffer);
	}
	else if (cfig.logfile && logLevel <= cfig.logLevel)
	{
		time_t t = time(NULL);
		tm *ttm = localtime(&t);

		if (ttm->tm_yday != loggingDay)
		{
			loggingDay = ttm->tm_yday;
			strftime(extbuff, sizeof(extbuff), logFile, ttm);
			fprintf(cfig.logfile, "Logging Continued on file %s\n", extbuff);
			fclose(cfig.logfile);
			cfig.logfile = fopen(extbuff, "at");

			if (cfig.logfile)
			{
				fprintf(cfig.logfile, "%s\n\n", sVersion);
				WritePrivateProfileStringA("InternetShortcut", "URL", extbuff, lnkFile);
				WritePrivateProfileStringA("InternetShortcut", "IconIndex", "0", lnkFile);
				WritePrivateProfileStringA("InternetShortcut", "IconFile", extbuff, lnkFile);
			}
			else
				return;
		}

		strftime(extbuff, sizeof(extbuff), "%d-%b-%y %X", ttm);

		if (req->path[0])
			fprintf(cfig.logfile, "[%s] Client %s:%u %s, %s\n", extbuff, IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->path, req->serverError.errormessage);
		else
			fprintf(cfig.logfile, "[%s] Client %s:%u, %s\n", extbuff, IP2String(tempbuff, req->client.sin_addr.s_addr), ntohs(req->client.sin_port), req->serverError.errormessage);

		fflush(cfig.logfile);
	}
	SetEvent(lEvent);
}
