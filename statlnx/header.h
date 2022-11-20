#pragma once

#include <iostream>
#include <fstream> 
#include <map>
#include <thread>
#include <cmath>
#include <boost/property_tree/xml_parser.hpp>
#include <filesystem>
#include <boost/signals2.hpp>

#include <sys/socket.h>
#include <arpa/inet.h>

#define BUFFER_LENGTH 16
#define SOCKET_BUFFER_SIZE 10485760
#define BLACK_BACKGROUND 7
#define DARK_GREY_BACKGROUND 135

using namespace std;

void UpdateData(char* pData);
void Print();
void Listen();
