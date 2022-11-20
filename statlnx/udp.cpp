#include "header.h"

extern int port;
extern string groupAddress;
extern bool exitApp;

extern boost::signals2::signal<void(char*)> dataReceived;

void Listen()
{
	struct sockaddr_in addr;
	memset(&addr, 0, sizeof(addr));
	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = htonl(INADDR_ANY);
	addr.sin_port = htons(port);

	int listen_socket = socket(AF_INET, SOCK_DGRAM, 0);
	if (listen_socket <  0)
		return;

	if (::bind(listen_socket, (struct sockaddr*)&addr, sizeof(addr)) < 0)
		return;

	unsigned long* addrBuff = (unsigned long*)malloc(8);
	if (!addrBuff)
		return;

	struct ip_mreq mreq {};
	inet_pton(AF_INET, groupAddress.data(), addrBuff);
	mreq.imr_multiaddr.s_addr = *addrBuff;
	mreq.imr_interface.s_addr = htonl(INADDR_ANY);
	free(addrBuff);

	if (setsockopt(listen_socket, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char*)&mreq, sizeof(mreq)) < 0)
		return;

	int size = SOCKET_BUFFER_SIZE;
	if (setsockopt(listen_socket, SOL_SOCKET, SO_RCVBUF, (char*)&size, sizeof(size)) < 0)
		return;

	char buff[BUFFER_LENGTH]{};
	socklen_t addrlen = sizeof(addr);

	while (true)
	{
		if (exitApp)
			break;
		if (recvfrom(listen_socket, buff, BUFFER_LENGTH, 0, (struct sockaddr*)&addr, &addrlen) > 0)
			dataReceived(buff);
	};
}