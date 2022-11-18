#include <iostream>
#include <fstream> 
#include <boost/property_tree/xml_parser.hpp>
#include <boost/foreach.hpp>
#include <filesystem>
#include "header.h"

int port;
string groupAddress;
int medianeInterval;
int modeStep;

int64_t messagesCount = 0;
int64_t lostMessagesCount = 0;
double average = 0;
double deviationSum = 0;
double mediane = 0;
double mode = 0;

#if defined WIN32
void Listen();
#endif

bool ReadXML(string path)
{
	boost::property_tree::ptree propertyTree;

	try
	{
		ifstream settings(path + "/Settings.xml");
		boost::property_tree::read_xml(settings, propertyTree);
		settings.close();

		BOOST_FOREACH(auto & v, propertyTree.get_child("settings"))
		{
			string first = v.first;
			string second = v.second.data();

			if (first == "GroupAddress")
				groupAddress = second;
			else if (first == "Port")
				port = atoi(second.data());
			else if (first == "MedianeInterval")
				medianeInterval = atoi(second.data());
			else if (first == "ModeStep")
				modeStep = atoi(second.data());
		}
		return true;
	}
	catch (...)
	{
		return false;
	}
}

void Output()
{
	int c = getchar();
	if (c == '\n')
	{
		cout << "\nTotal messages received: ";
		cout << messagesCount << endl;
		cout << "Total messages \"lost\":   ";
		cout << lostMessagesCount << endl;
		cout << "Average:                 ";
		cout << average << endl;
		cout << "Standard deviation:      ";
		cout << sqrt(deviationSum / (messagesCount + 1)) << endl;
		cout << "Mediane:                 ";
		cout << mediane << endl;
		cout << "Mode:                    ";
		cout << mode << endl;
		cout << endl;
		for (int i = 0; i < 60; i++)
			cout << '*';
	}
	Output();
}

int main(int argc, char* argv[])
{
	filesystem::path p = filesystem::path(argv[0]).parent_path();
	if (!ReadXML(filesystem::path(argv[0]).parent_path().string()))
		cout << "Failed to read settings!\n";

	thread(Output).detach();
#if defined WIN32
	Listen();
#else
	try
	{
		boost::asio::io_service io_service;
		udp_server server(io_service);
		io_service.run();
	}
	catch (std::exception& e)
	{
		std::cerr << e.what() << std::endl;
	}
#endif
}