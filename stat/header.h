#pragma once

#include <boost/array.hpp>

#ifndef WIN32
#include <boost/bind.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/asio.hpp>
#endif

#define BUFFER_LENGTH 16
#define SOCKET_BUFFER_SIZE 10485760

using namespace std;

#ifndef WIN32

using boost::asio::ip::udp;

class udp_server
{
public:
	udp_server(boost::asio::io_service& io_service);
private:
	void start_receive();
	void handle_receive(const boost::system::error_code& error,
		std::size_t cbBytes);
	udp::socket socket_;
	udp::endpoint remote_endpoint_;
	boost::array<char, BUFFER_LENGTH> recv_buffer_;
};

#endif