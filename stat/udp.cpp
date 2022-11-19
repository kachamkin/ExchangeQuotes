#include "header.h"

extern int port;
extern string groupAddress;
extern int medianeInterval;
extern int modeStep;

extern boost::signals2::signal<void(char*)> dataReceived;

#ifndef WIN32

udp_server::udp_server(boost::asio::io_service& io_service)
	: socket_(io_service, udp::endpoint(udp::v4(), port))
{
	socket_.set_option(boost::asio::socket_base::receive_buffer_size(SOCKET_BUFFER_SIZE));
	socket_.set_option(boost::asio::ip::multicast::join_group(boost::asio::ip::address::from_string(groupAddress).to_v4()));
	start_receive();
}

void udp_server::start_receive()
{
	socket_.async_receive_from(
		boost::asio::buffer(recv_buffer_), remote_endpoint_,
		boost::bind(&udp_server::handle_receive, this,
			placeholders::error,
			placeholders::bytes_transferred));
}

void udp_server::handle_receive(const boost::system::error_code& error,
	std::size_t cbBytes)
{
	if (!error || error == boost::asio::error::message_size)
	{
		dataReceived(recv_buffer_.data());
		start_receive();
	}
}

#else

void Listen()
{
	struct sockaddr_in addr;
	memset(&addr, 0, sizeof(addr));
	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = htonl(INADDR_ANY); 
	addr.sin_port = htons(port);

	SOCKET listen_socket = socket(AF_INET, SOCK_DGRAM, 0);
	if (listen_socket == INVALID_SOCKET) 
		return;

	if (::bind(listen_socket, (struct sockaddr*)&addr, sizeof(addr)) <  0)
	{
		closesocket(listen_socket);
		return;
	}

	ULONG* addrBuff = (ULONG*)malloc(8);
	if (!addrBuff)
	{
		closesocket(listen_socket);
		return;
	}

	struct ip_mreq mreq {};
	inet_pton(AF_INET, groupAddress.data(), addrBuff);
	mreq.imr_multiaddr.s_addr = *addrBuff; 
	mreq.imr_interface.s_addr = htonl(INADDR_ANY);
	free(addrBuff);

	if (setsockopt(listen_socket, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char*)&mreq, sizeof(mreq)) < 0)
	{
		closesocket(listen_socket);
		return;
	}

	int size = SOCKET_BUFFER_SIZE;
	if (setsockopt(listen_socket, SOL_SOCKET, SO_RCVBUF, (char*)&size, sizeof(size)) < 0)
	{
		closesocket(listen_socket);
		return;
	}

	int bytesRead = 0;
	char buff[BUFFER_LENGTH]{};

	int addrlen = sizeof(addr);
	while (true)
	{
		bytesRead = recvfrom(listen_socket, buff, BUFFER_LENGTH, 0, (struct sockaddr*)&addr, &addrlen);
		if (bytesRead > 0)
			dataReceived(buff);
	};
}

#endif

