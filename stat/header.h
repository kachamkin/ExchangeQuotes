#pragma once

#include <iostream>
#include <fstream> 
#include <map>
#include <boost/property_tree/xml_parser.hpp>
#include <filesystem>
#include <boost/signals2.hpp>

#ifndef WIN32

#include <boost/array.hpp>
#include <boost/bind/bind.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/asio.hpp>

#else

#include <WinSock2.h>

#endif

#define BUFFER_LENGTH 16
#define SOCKET_BUFFER_SIZE 10485760
#define BLACK_BACKGROUND 7
#define DARK_GREY_BACKGROUND 135

using namespace std;
using namespace boost::placeholders;

void UpdateData(char* pData);

#ifndef WIN32

using boost::asio::ip::udp;

class udp_server
{
public:
	udp_server(boost::asio::io_service& io_service);
private:
	void start_receive();
	void handle_receive(const boost::system::error_code& error);
	udp::socket socket_;
	udp::endpoint remote_endpoint_;
	boost::array<char, BUFFER_LENGTH> recv_buffer_;
};

#else

void Listen();
void Print();

#endif